# Implementation Plan: Advanced cache serialization (per-type serializers, compression, MemoryPack)

> **Issue**: [#274](https://github.com/dlrivada/Encina/issues/274)
> **Type**: Feature
> **Complexity**: High (8 pull requests of 15, 7, 10, 8, 14, 12, 12 and 4 files)
> **Estimated Scope**: about 1,100 lines of production code, about 2,300 lines of tests
> **Milestone**: v0.18.0 — Integration & Platform Capabilities

---

## Summary

Issue #274 asks for an `ICacheSerializer` abstraction with per-type registration (`options.AddSerializer<T>(...)`), a MemoryPack serializer in a new `Encina.Caching.MemoryPack` package, extra compression algorithms (Zstd, Brotli, Snappy beside LZ4), size-based "smart" compression and benchmarks. Re-checked against `origin/main` (f484cdf4f):

| Premise of the issue | State in the code |
|----------------------|-------------------|
| "Encina supports SystemTextJson and MessagePack with optional LZ4 compression, configured globally" | False. `CachingOptions.SerializerOptions` (`src/Encina.Caching/Configuration/CachingOptions.cs`: `SerializerType`, `EnableCompression`, `CompressionThreshold`, enum `CacheSerializerType.MessagePack`) is copied by `AddEncinaCaching` but read by no provider. `RedisCacheProvider` hard-codes `System.Text.Json` (camelCase) and stores a JSON string (`StringSetAsync`, `src/Encina.Caching.Redis/RedisCacheProvider.cs`, lines 85-88 and 208). MessagePack appears in `Directory.Packages.props` only as a transitive pin for Aspire (#2338), and no compression library is referenced. |
| A serializer abstraction exists to extend | No. The Memcached plan (#277) records the same gap and defers the abstraction to a follow-up; this plan is that abstraction. |
| Per-type serializers by `typeof(T)` can serve `[Cache]` | The behavior caches `CacheEntry<TResponse>` (`internal sealed`, `QueryCachingPipelineBehavior.cs`), so a registration for `LargeReport` never sees `LargeReport` at the provider: it sees `CacheEntry<LargeReport>` (Design Choice 3). |
| All cache providers serialize | Only those that cross a process boundary. `MemoryCacheProvider` stores object references (no serialization); `HybridCacheProvider` delegates L2 serialization to `HybridCache`; Redis, Valkey, Dragonfly, Garnet and KeyDB share `RedisCacheProvider`; Memcached (planned, #277) will serialize. |
| MemoryPack "~10x faster than MessagePack" | An unverified claim of the issue; the benchmark task of this plan produces the numbers, and the documentation cites them by benchmark name and never types them. |

The plan adds, in `Encina.Caching`: `ICacheSerializer`, `ICacheCompressor`, an internal `CacheValueCodec` with a self-describing header, `SystemTextJsonCacheSerializer`, `BrotliCacheCompressor` (BCL, no dependency) and a serialization options model that replaces the dead `CacheSerializerOptions`; then the Redis-family provider and a Hybrid adapter use the codec; three new opt-in packages hold the third-party code (`Encina.Caching.Compression` for LZ4, Zstd and Snappy, `Encina.Caching.MessagePack`, `Encina.Caching.MemoryPack`).

**Shared pieces and ownership (families cache-patterns-1 and -2):** #266 owns the `CacheEntry<T>` envelope fields and `CachePatternSettings`; this plan changes how the envelope is encoded, not which fields it has. #277 (Memcached) is the second consumer of the codec and starts with STJ; it adopts the codec in its own later pull request. The EventIds are this plan's range `CachingSerialization` 508-513 (block 500-549 of the plan batch registry).

**Affected packages:** `Encina.Caching` (core), `Encina.Caching.Redis`, `Encina.Caching.Hybrid`, `Encina` (EventId registry), and three new packages. **Provider category:** Caching (8): Redis, Valkey, Dragonfly, Garnet, KeyDB through one provider; Hybrid through an adapter; Memory not applicable (no serialization); Memcached through #277.

---

## Design Choices

<details>
<summary><strong>1. Shape of the serializer abstraction (Dilemma D1)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The issue's shape: `byte[] Serialize<T>(T value)`, `T Deserialize<T>(ReadOnlySpan<byte> data)`, `bool SupportsType(Type type)`, plus `string Name` and a one-byte `Id` for the header** | Simple; MemoryPack, MessagePack and STJ all offer array and span overloads; matches the issue | One array allocation per write and per read |
| **B) `IBufferWriter<byte>` for writes and `ReadOnlySequence<byte>` for reads** | Zero-copy with pooled buffers; what MemoryPack and MessagePack are designed for | Harder to implement and test; compression needs the whole payload anyway, so the gain is partial |

### Chosen Option: **A - array and span** (recommended, pending the maintainer)

### Rationale

- The compression step needs a contiguous payload, which removes most of B's benefit for compressed entries; the benchmark task measures both on the Redis path before anyone optimizes. Option B wins if the measured allocation per entry matters for the hot path.
- `Deserialize` takes a `ReadOnlySpan<byte>` (the issue's `byte[]` parameter would force a copy out of a `RedisValue`).
- Class: Dilemma

</details>

<details>
<summary><strong>2. Which providers use the codec (Dilemma D2 on Hybrid)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Redis family (one provider, five products) uses the codec; Hybrid gets an `IHybridCacheSerializerFactory` adapter so its L2 uses the same serializer and compression; Memory is not applicable; Memcached adopts it in #277's follow-up** | One serialization setting for every provider that serializes (AGENTS.md section 5: consistent across the category) | One more class and test set in the Hybrid package; HybridCache calls factories per type |
| **B) Redis family only; Hybrid keeps `HybridCache`'s own serializer configuration, documented** | Smaller | Two serialization settings for one application that mixes Redis and Hybrid |

### Chosen Option: **A - Redis family and a Hybrid adapter** (recommended, pending the maintainer)

### Rationale

- Option B wins for an application that already configures `HybridCache` serializers itself and does not want Encina to touch them.
- The adapter is registered only when the application opts in with the serialization options; otherwise `HybridCache` is untouched (pay-for-what-you-use).
- Class: Dilemma

</details>

<details>
<summary><strong>3. Per-type resolution for `[Cache]` entries (Dilemma D3)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The codec encodes the envelope itself: an internal `ICacheEntryEnvelope<TSelf>` (static abstract `FromParts`) exposes the entry's value and its metadata as small STJ JSON; the value is encoded by the serializer resolved for the response type `X` of `CacheEntry<X>`** | `AddSerializer<LargeReport>` works for `[Cache]` queries; MemoryPack and MessagePack never need to annotate `CacheEntry<T>`; new envelope fields of the sibling plans flow with no codec change | The codec builds a closed generic per response type (`MakeGenericType`), a limit for NativeAOT unless the response types are rooted |
| **B) Serialize the whole `CacheEntry<T>` with the serializer registered for `T`** | No special case | Impossible for MemoryPack and MessagePack: `CacheEntry<T>` is internal and unannotated, and the serializer would need the envelope's members |
| **C) Per-type serializers apply only to direct `ICacheProvider` use; `[Cache]` entries always use the default** | Smallest | The issue's own example (`LargeReport` cached by a query) would not work |

### Chosen Option: **A - the codec owns the envelope** (recommended, pending the maintainer)

### Rationale

- Registration is by exact type or by assignability (`AddSerializer<T>` matches `T` and, through `ICacheSerializer.SupportsType`, derived types), resolved once per closed type into an instance-level dictionary of the resolver (never a static field of a generic class, which cannot hold per-instance options).
- Option C wins if the maintainer judges the envelope special case too costly for the first version.
- Class: Dilemma

</details>

<details>
<summary><strong>4. Packaging of third-party code (Dilemma D4)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Core holds the contracts, STJ and Brotli (BCL only); `Encina.Caching.Compression` (LZ4, Zstd, Snappy), `Encina.Caching.MessagePack` and `Encina.Caching.MemoryPack` are separate opt-in packages** | No third-party dependency unless used (pay-for-what-you-use); matches the issue's `Encina.Caching.MemoryPack` | Three new packages, each with README, manifest, PublicAPI and slnx entries |
| **B) One package per algorithm** | Finest granularity | Five packages for what is one registration idea |
| **C) Everything in `Encina.Caching`** | One package | Four third-party dependencies for every caching user |

### Chosen Option: **A - three opt-in packages** (recommended, pending the maintainer)

### Rationale

- Each third-party candidate (K4os.Compression.LZ4, ZstdSharp.Port, Snappier, MessagePack, MemoryPack) is verified for licence, .NET 10 support and maintenance by the worker of its pull request before it enters `Directory.Packages.props`; this plan names candidates, not decisions. Option B wins if a team wants Zstd alone without Snappy's assembly.
- `MemoryPack` for #274 is in 1.0 scope: SPEC-000 DEC-009 (2026-10-23) widened the scope to everything previously deferred except AI/LLM and cloud services without a local emulator; the "MemoryPack stays post-1.0" remark on #277 (2026-09-21) concerned a cache provider candidate and predates DEC-009.
- Class: Dilemma

</details>

<details>
<summary><strong>5. Wire format (Dilemma D5)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Every stored value has a two-byte header (format version, compression algorithm id) followed by the payload; Redis stores bytes** | One format; any node decodes any entry whatever its own settings are (a rolling deploy that changes the algorithm keeps working); `redis-cli` shows binary | JSON is no longer readable in Redis with the default settings |
| **B) No header while the serializer is STJ and compression is off (plain JSON as today), a header byte outside the JSON alphabet only otherwise** | Readable by default; a reader tells the two apart by the first byte | Two formats to test forever; a JSON document can never start with a control byte, which is what the discrimination relies on |

### Chosen Option: **A - always a header** (recommended, pending the maintainer)

### Rationale

- Existing Redis entries are plain JSON strings: after the change they fail the header check, are treated as a miss (EventId 511) and are overwritten by the next write; AGENTS.md section 1 (pre-1.0, no compatibility, no migration) allows this and a cache is rebuildable.
- Option B wins for operators who inspect Redis directly and for a mixed-version rolling deploy.
- Class: Dilemma

</details>

<details>
<summary><strong>6. Shape of the compression configuration (Dilemma D6)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) An ordered tier list: `Compression.Tiers = [(MinBytes: 0, None), (MinBytes: 1024, Lz4), (MinBytes: 102400, Zstd, Level: 3)]`; one tier is the issue's single algorithm with a threshold** | One concept covers the issue's `CompressionAlgorithm`, `CompressionLevel`, `CompressionThresholdBytes` and `SmartCompression`; no contradictory combinations | Differs from the issue's flat properties |
| **B) The issue's flat properties plus `SmartCompressionOptions` with fixed small, medium and large slots** | Matches the issue text | Two overlapping ways (`CompressionThresholdBytes` and `SmartCompression`) to say the same thing; validation rules for conflicts |

### Chosen Option: **A - tier list** (recommended, pending the maintainer)

### Rationale

- Defaults are pay-for-what-you-use: no tier above `None`, so nothing is compressed unless configured. A level outside the algorithm's range, an empty list, an unsorted or duplicate `MinBytes`, or a tier whose algorithm has no registered `ICacheCompressor` fails at startup validation. Option B wins if the issue's public vocabulary is a requirement.
- Class: Dilemma

</details>

<details>
<summary><strong>7. Decoding failures and decompression limits</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Decoding never throws to the caller: an unknown header, a corrupt payload, an unsupported algorithm or a payload that would exceed `MaxDecompressedBytes` returns a `Left` inside the codec and the provider treats it as a cache miss (logged by code, never with the payload)** | A poisoned or foreign entry cannot take the request down or exhaust memory; fail closed | The corrupt entry stays until it expires or is overwritten |
| **B) Let the exception propagate to the behavior's existing catch** | Less code | A decompression bomb allocates before any limit applies |

### Chosen Option: **A - miss, never throw, bounded output**

### Rationale

- **Class:** Settled
- **Settled by:** AGENTS.md section 3 (security gates fail closed; errors are logged by code, `EncinaError.Message` never reaches logs; ROP returns `Either`) and the OWASP Deserialization Cheat Sheet (bound untrusted input before decoding).
- Codes: `cache.serialization_failed` (`ErrorCategory.Internal`: the value cannot be encoded, a programming error), `cache.deserialization_failed` and `cache.decompression_limit_exceeded` (`ErrorCategory.UpstreamFailure`: the stored bytes come from the cache server); constant messages. `MaxDecompressedBytes` defaults to 16 MiB (an option).

</details>

<details>
<summary><strong>8. The dead `CacheSerializerOptions` is replaced and defaults stay minimal</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Remove `CacheSerializerOptions` and `CacheSerializerType` (no provider reads them) and add `CachingOptions.Serialization` with a default of STJ and no compression** | No dead configuration; defaults equal today's behavior | A public type disappears |
| **B) Keep the old types beside the new ones** | Nothing is removed | Two configuration surfaces, one inert |

### Chosen Option: **A - replace**

### Rationale

- **Class:** Settled
- **Settled by:** AGENTS.md section 3 ("NEVER add `[Obsolete]`, legacy code, compatibility aliases ... Every line serves a current purpose") and section 1 (pre-1.0, breaking changes encouraged); the grep in the Summary shows the options are read by no provider.

</details>

---

## Implementation Phases

### Phase 1: Contracts, the codec, STJ and Brotli (pull request 1)

<details>
<summary><strong>Tasks</strong></summary>

PR 1 holds exactly these 15 files:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Caching/Serialization/ICacheSerializer.cs` | new (public) |
| 2 | `src/Encina.Caching/Serialization/ICacheCompressor.cs` | new (public) |
| 3 | `src/Encina.Caching/Serialization/SystemTextJsonCacheSerializer.cs` | new (public sealed) |
| 4 | `src/Encina.Caching/Serialization/BrotliCacheCompressor.cs` | new (public sealed, `System.IO.Compression`) |
| 5 | `src/Encina.Caching/Serialization/CacheValueCodec.cs` | new (internal sealed: header, tiers, limits, envelope) |
| 6 | `src/Encina.Caching/Serialization/CacheSerializationOptions.cs` | new (public: default serializer, per-type map, compression tiers, limits) |
| 7 | `src/Encina.Caching/Diagnostics/CacheSerializationLog.cs` | new (EventIds 508-513) |
| 8 | `src/Encina/Diagnostics/EventIdRanges.cs` | modify (`CachingSerialization = (508, 513)`) |
| 9 | `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` | modify (map `Encina.Caching`) |
| 10 | `src/Encina/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`: the new `EventIdRanges` field) |
| 11 | `src/Encina.Caching/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 12 | `tests/Encina.UnitTests/Caching/CacheValueCodecTests.cs` | new (codec and options validation) |
| 13 | `tests/Encina.GuardTests/Caching/CacheSerializationGuardTests.cs` | new |
| 14 | `.github/coverage-manifest/Encina.Caching.json` | modify (`mechanical-fixer`) |
| 15 | `changelog.d/274-cache-serialization-core.added.md` | new (`mechanical-fixer`) |

Nothing is registered or read by a provider in this PR: the codec is internal and unused until PR 3, and the dead `CacheSerializerOptions` is replaced in PR 2, so the build is whole after each PR. The codec's exact member shape is fixed by the unit tests of this PR; PR 3 consumes it as written.

Tasks:

1. **Create** `ICacheSerializer` (Design Choice 1), `ICacheCompressor` (`byte Id`, `string Name`, `int Compress(ReadOnlySpan<byte>, int level, IBufferWriter<byte>)`, `bool TryDecompress(ReadOnlySpan<byte>, int maxBytes, IBufferWriter<byte>)`), `SystemTextJsonCacheSerializer` (camelCase, not indented, like `RedisCacheProvider` today; an optional `JsonSerializerOptions` for source-generated contexts) and `BrotliCacheCompressor`.
2. **Create** `CacheValueCodec` (internal): `Either<EncinaError, byte[]> Encode<T>(T value)` and `Either<EncinaError, T> Decode<T>(ReadOnlySpan<byte> data)` per Designs 3, 5, 6 and 7; the header is two bytes (format version `1`, compression id); tiers choose the algorithm by payload size; a compressed payload larger than the raw one is stored raw (EventId 513); the resolver keeps an instance-level `ConcurrentDictionary<Type, ICacheSerializer>`.
3. **Create** `CacheSerializationOptions`: `DefaultSerializer` (STJ), `AddSerializer<T>(ICacheSerializer)` (null guard), `Compression` (tiers, `MaxDecompressedBytes`) and a `Validate()` method per Design Choice 6 (startup validation, throws `InvalidOperationException` naming the rule, never a value).
4. **Create** `CacheSerializationLog` with EventIds 508 serialization failed, 509 deserialization failed, 510 decompression limit exceeded, 511 unknown format header, 512 serializer does not support the type (fell back to the default, logged once per type), 513 compressed payload not smaller (stored raw): by exception type or code only.
5. **Register** the range in `EventIdRanges.cs` and the assembly map (`Encina.Caching` maps to `Caching` and `CachingSerialization`; if #272 merged first the map already holds `CachingStrategies`).
6. **Tests (unit):** round trips for STJ with and without Brotli, each tier boundary, the unknown header, a truncated payload, a bomb over `MaxDecompressedBytes`, a `CacheEntry<T>`-shaped envelope with a per-type serializer, `SupportsType` fallback, the options validation, the log does not contain the payload. Guard tests (`CacheSerializationGuardTests.cs`, same PR): `SystemTextJsonCacheSerializer`, `BrotliCacheCompressor` and `CacheSerializationOptions.AddSerializer` are public with validated parameters (null serializer, null buffer writer); a nullable `JsonSerializerOptions` parameter that accepts null gets a valid-result test, not an exception test.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT:
Encina is a .NET 10 / C# 14 library, pre-1.0. Issue #274 adds a serializer and compression layer to caching. Today RedisCacheProvider
hard-codes System.Text.Json and stores a JSON string; CachingOptions.SerializerOptions is read by no provider. The behavior caches
CacheEntry<TResponse> (internal, QueryCachingPipelineBehavior.cs).

TASK:
1. Create ICacheSerializer, ICacheCompressor, SystemTextJsonCacheSerializer, BrotliCacheCompressor and the internal CacheValueCodec.
2. Create CacheSerializationOptions (default serializer, per-type map, compression tiers, limits, Validate()); nothing else reads it yet.
3. Create CacheSerializationLog (508-513) and register CachingSerialization = (508, 513) in EventIdRanges.cs and the assembly map.
4. Write the unit and guard tests; ask mechanical-fixer for the PublicAPI files, the manifest rows and the changelog fragment.

KEY RULES:
- Decoding never throws to the caller: Left inside the codec, a miss in the provider; output bounded by MaxDecompressedBytes.
- Error codes with constant messages: cache.serialization_failed (Internal), cache.deserialization_failed and
  cache.decompression_limit_exceeded (UpstreamFailure); create them with EncinaErrors.Create and a named category.
- No static field of a generic class holds per-instance options; randomness none; time none.
- Follow the maintainer's answers to Design Choices 1, 3, 5 and 6, given in your brief.

REFERENCE FILES:
- src/Encina.Caching.Redis/RedisCacheProvider.cs
- src/Encina.Caching/Configuration/CachingOptions.cs
- src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs
- src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

### Phase 2: The options replace the dead ones (pull request 2)

<details>
<summary><strong>Tasks</strong></summary>

PR 2 holds exactly these 7 files:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Caching/Configuration/CachingOptions.cs` | modify (remove `CacheSerializerOptions` and `CacheSerializerType`; add `Serialization` of type `CacheSerializationOptions`) |
| 2 | `src/Encina.Caching/ServiceCollectionExtensions.cs` | modify (copy `Serialization`, validate it, register the codec as a singleton) |
| 3 | `src/Encina.Caching/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`: removed lines, RS0017, and the new property) |
| 4 | `tests/Encina.UnitTests/Caching/CachingOptionsTests.cs` | modify (the old options leave) |
| 5 | `tests/Encina.UnitTests/Caching/ServiceCollectionExtensionsTests.cs` | modify (the option reaches `IOptions<CachingOptions>`; an invalid tier list fails at startup; DI built with `ValidateOnBuild` and `ValidateScopes`) |
| 6 | `.github/coverage-manifest/Encina.Caching.json` | modify (`mechanical-fixer`) |
| 7 | `changelog.d/274-cache-serialization-options.changed.md` | new (`mechanical-fixer`) |

Tasks: replace the two dead types by the new `Serialization` property (Design Choice 8); `AddEncinaCaching` copies it (the option copy list is hand-written: a missing line silently drops the option), runs `Validate()` and registers the internal codec with `TryAddSingleton`. No guard test: `AddEncinaCaching` has no guard test project coverage today and this PR adds no new public method.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT:
Phase 1 is merged: CacheSerializationOptions and the codec exist. CachingOptions.SerializerOptions is read by no provider.

TASK:
Replace CacheSerializerOptions/CacheSerializerType by CachingOptions.Serialization, copy and validate it in AddEncinaCaching and
register the codec; update the tests; ask mechanical-fixer for PublicAPI, the manifest rows and the fragment.

KEY RULES:
- No [Obsolete], no alias: the old types are removed completely.
- Defaults equal today's behavior: STJ, no compression.
- Build the provider with ValidateOnBuild and ValidateScopes in the DI test.

REFERENCE FILES:
- src/Encina.Caching/Configuration/CachingOptions.cs
- src/Encina.Caching/ServiceCollectionExtensions.cs
- tests/Encina.UnitTests/Caching/ServiceCollectionExtensionsTests.cs
```

</details>

### Phase 3: The Redis-family provider uses the codec (pull request 3)

<details>
<summary><strong>Tasks</strong></summary>

PR 3 holds exactly these 10 files:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Caching.Redis/RedisCacheProvider.cs` | modify (constructor takes the codec, reads and writes bytes, sliding metadata keeps STJ) |
| 2 | `src/Encina.Caching.Redis/ServiceCollectionExtensions.cs` | modify (resolve the codec) |
| 3 | `src/Encina.Caching.Redis/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`: the constructor line changes, RS0017) |
| 4 | `tests/Encina.UnitTests/Caching/Redis/RedisCacheProviderTests.cs` | modify |
| 5 | `tests/Encina.UnitTests/Caching/Redis/ServiceCollectionExtensionsTests.cs` | modify |
| 6 | `tests/Encina.GuardTests/Caching/RedisCacheProviderGuardTests.cs` | modify (new constructor parameter) |
| 7 | `tests/Encina.IntegrationTests/Infrastructure/Caching/RedisCacheProviderIntegrationTests.cs` | modify (round trips with compression against the shared Redis container) |
| 8 | `tests/Encina.IntegrationTests/Infrastructure/Caching/ServiceCollectionExtensionsIntegrationTests.cs` | modify (registration built with `ValidateOnBuild` and `ValidateScopes`) |
| 9 | `.github/coverage-manifest/Encina.Caching.Redis.json` | modify (`mechanical-fixer`) |
| 10 | `changelog.d/274-cache-serialization-redis.changed.md` | new (`mechanical-fixer`) |

Tasks:

1. `RedisCacheProvider` takes the internal `CacheValueCodec` of PR 1 (visible to `Encina.Caching.Redis` through the existing `InternalsVisibleTo`) as an optional trailing constructor parameter defaulting to a codec with the STJ serializer and no compression (the existing call sites keep compiling; DI always supplies the configured one). `GetAsync<T>` reads `RedisValue` bytes and calls `Decode`; a `Left` is a miss; `SetAsync` and the sliding variant call `Encode`; a `Left` from `Encode` is logged and the write is skipped (the value cannot be stored; never throws a message). The sliding metadata key keeps its small STJ JSON (it is not a cached value).
2. Valkey, Dragonfly, Garnet and KeyDB reuse `AddEncinaRedisCache` and need no change; the Redis registration test of each wrapper proves it picks the configured codec (the four wrapper test files are not edited when the Redis registration test already builds each wrapper; the worker counts them and adds them to the PR only if they fail).
3. **Tests:** unit with a fake `IDatabase`, integration with `RedisFixture` (`[Collection("Redis")]`-style shared fixture): STJ default, Brotli tier, a stored legacy JSON string is a miss and is overwritten.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
Phases 1 and 2 are merged: ICacheSerializer, the codec and CachingOptions.Serialization exist. RedisCacheProvider (shared by Valkey, Dragonfly,
Garnet and KeyDB) serializes with System.Text.Json into a string.

TASK:
1. Make RedisCacheProvider read and write codec bytes; register the configured codec in AddEncinaRedisCache.
2. Update the unit, guard and Redis integration tests; ask mechanical-fixer for PublicAPI.Unshipped.txt, the manifest rows and the fragment.

KEY RULES:
- A decode Left is a miss; the provider never throws a payload or a message.
- The new constructor parameter is optional and trailing (30+ call sites compile unchanged).
- Integration tests use the shared Redis collection fixture; never a per-class container.

REFERENCE FILES:
- src/Encina.Caching.Redis/RedisCacheProvider.cs
- src/Encina.Caching.Redis/ServiceCollectionExtensions.cs
- tests/Encina.IntegrationTests/Infrastructure/Caching/Fixtures/RedisFixture.cs
```

</details>

### Phase 4: The Hybrid adapter (pull request 4)

<details>
<summary><strong>Tasks</strong></summary>

PR 4 holds exactly these 8 files: `src/Encina.Caching.Hybrid/EncinaHybridCacheSerializerFactory.cs` (new, internal, `IHybridCacheSerializerFactory`), `src/Encina.Caching.Hybrid/ServiceCollectionExtensions.cs` (modify), `src/Encina.Caching.Hybrid/PublicAPI.Unshipped.txt` (`mechanical-fixer`), `tests/Encina.UnitTests/Caching/Hybrid/EncinaHybridCacheSerializerFactoryTests.cs` (new), `tests/Encina.UnitTests/Caching/Hybrid/ServiceCollectionExtensionsTests.cs` (modify), `tests/Encina.GuardTests/Caching/HybridCacheProviderGuardTests.cs` (modify when a public signature changes), `.github/coverage-manifest/Encina.Caching.Hybrid.json` (`mechanical-fixer`), `changelog.d/274-cache-serialization-hybrid.added.md` (`mechanical-fixer`).

Tasks: the factory returns an `IHybridCacheSerializer<T>` that wraps `ICacheSerializer` and the compression tiers (`TryCreateSerializer<T>` returns false when the options leave the defaults, so `HybridCache`'s own serializer applies); `AddEncinaHybridCache` registers the factory only when serialization options were configured (Design Choice 2). Tests: the adapter round trips through a real `HybridCache` built by `AddHybridCache`, and a build with `ValidateOnBuild`. The `HybridCache` L1 keeps object references: only L2 bytes are encoded.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT:
Phases 1-3 are merged. HybridCacheProvider (src/Encina.Caching.Hybrid) delegates serialization to Microsoft HybridCache.

TASK:
Create EncinaHybridCacheSerializerFactory (IHybridCacheSerializerFactory) over the codec and register it only when serialization
options are configured; write the tests; ask mechanical-fixer for PublicAPI, manifest and fragment.

KEY RULES:
- Opt-in: without configured options HybridCache is untouched.
- Failed decode is a miss, never an exception with the payload.

REFERENCE FILES:
- src/Encina.Caching.Hybrid/HybridCacheProvider.cs
- src/Encina.Caching.Hybrid/ServiceCollectionExtensions.cs
```

</details>

### Phase 5: `Encina.Caching.Compression` (pull request 5)

<details>
<summary><strong>Tasks</strong></summary>

PR 5 holds exactly these 14 files: `src/Encina.Caching.Compression/Encina.Caching.Compression.csproj`, `Lz4CacheCompressor.cs`, `ZstdCacheCompressor.cs`, `SnappyCacheCompressor.cs`, `ServiceCollectionExtensions.cs` (`AddEncinaCachingCompression()` registers the three compressors with `TryAddEnumerable`), `README.md` (`docs-writer`), `PublicAPI.Shipped.txt` and `PublicAPI.Unshipped.txt` (`mechanical-fixer`), `Directory.Packages.props` (the three versions), `Encina.slnx`, `tests/Encina.UnitTests/Caching/Compression/CompressorTests.cs`, `tests/Encina.GuardTests/Caching/CompressionGuardTests.cs`, `.github/coverage-manifest/Encina.Caching.Compression.json` (`mechanical-fixer`), `changelog.d/274-cache-compression-package.added.md`.

Tasks: one compressor per algorithm with the level range of the library (Zstd 1-22, LZ4 levels, Snappy none) validated in `Compress`; ids are fixed constants of the wire format (`1` Brotli, `2` LZ4, `3` Zstd, `4` Snappy) and never reused; the package target is .NET 10 only with `Nullable` and the public-API analyzer like `Encina.Caching.Memory`. Tests: round trip, level bounds, an adversarial payload (random bytes) never throws out of `TryDecompress`, and the limit is honored. `InternalsVisibleTo` for `Encina.UnitTests` only when an internal type needs it.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
CONTEXT:
Phase 1 is merged: ICacheCompressor exists with Brotli in core. This package adds the third-party algorithms.

TASK:
Create the Encina.Caching.Compression package (csproj, three compressors, registration, tests, guard tests); verify licence, .NET 10
support and maintenance of each NuGet candidate before adding its version; ask mechanical-fixer for PublicAPI, the manifest and the
fragment and docs-writer for the README.

KEY RULES:
- Compressor ids are wire-format constants; never renumber.
- TryDecompress never throws and never writes more than the limit.
- Add the package to Encina.slnx; no .slnf.

REFERENCE FILES:
- src/Encina.Caching.Memory/Encina.Caching.Memory.csproj
- src/Encina.Caching/Serialization/ICacheCompressor.cs
- Directory.Packages.props
```

</details>

### Phase 6: `Encina.Caching.MessagePack` and `Encina.Caching.MemoryPack` (pull requests 6 and 7)

<details>
<summary><strong>Tasks</strong></summary>

Each of PR 6 (`MessagePack`) and PR 7 (`MemoryPack`) holds the same 12 files for its own name (`MessagePack` or `MemoryPack`): `src/Encina.Caching.<Name>/Encina.Caching.<Name>.csproj`, `<Name>CacheSerializer.cs`, `ServiceCollectionExtensions.cs` (`UseMessagePackSerialization(this CachingOptions)` / `UseMemoryPackSerialization(this CachingOptions)` and `AddSerializer`-style helpers), `README.md` (`docs-writer`), the two PublicAPI files, `Directory.Packages.props`, `Encina.slnx`, `tests/Encina.UnitTests/Caching/<Name>/<Name>CacheSerializerTests.cs`, `tests/Encina.GuardTests/Caching/<Name>GuardTests.cs`, the manifest `.github/coverage-manifest/Encina.Caching.<Name>.json` and the fragment `changelog.d/274-cache-<name>-package.added.md`.

Tasks: `SupportsType` is true only for types the library can serialize (MemoryPack: types with the generated `IMemoryPackable<T>` implementation; MessagePack: annotated types or those a configured resolver accepts), so an unsupported type falls back to the default serializer with EventId 512 instead of throwing. MessagePack security: the package uses `MessagePackSecurity.UntrustedData` (bounded depth and hash-collision-resistant) because cache bytes are untrusted input; MemoryPack has no untrusted-data mode, which the README states. The existing transitive MessagePack pin for Aspire (#2338) stays and the direct reference reuses the same version entry. Tests: round trip of an annotated type, an unsupported type reports `SupportsType` false, malformed bytes return a `Left` through the codec.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
CONTEXT:
Phase 1 is merged. Each serializer package implements ICacheSerializer for one library.

TASK:
Create Encina.Caching.MessagePack (PR 6) and Encina.Caching.MemoryPack (PR 7) as the plan lists; ask mechanical-fixer for PublicAPI,
manifests and fragments and docs-writer for the READMEs.

KEY RULES:
- Cache bytes are untrusted: bounded, never throwing to the caller; MessagePack uses UntrustedData security.
- SupportsType false falls back to the default serializer.

REFERENCE FILES:
- src/Encina.Caching/Serialization/ICacheSerializer.cs
- Directory.Packages.props
```

</details>

### Phase 7: Benchmarks and documentation (pull request 8)

<details>
<summary><strong>Tasks</strong></summary>

PR 8 holds exactly these 4 files: `tests/Encina.BenchmarkTests/Encina.Caching.Benchmarks/SerializationBenchmarks.cs` (new: STJ, MessagePack and MemoryPack across small, medium and large payloads and each compression algorithm, run through `BenchmarkSwitcher`), `tests/Encina.BenchmarkTests/Encina.Caching.Benchmarks/Encina.Caching.Benchmarks.csproj` (modify: the three `ProjectReference` entries), `docs/features/query-caching.md` (`docs-writer`: serializers, compression tiers, the loss of readable JSON in Redis, the benchmark cited by name) and `docs/features/caching-serialization.md` (`docs-writer`, new). It has no user-visible change of its own, so no changelog fragment. `src/Encina.Caching` has no README today and creating one is outside this plan. Verify the filter with `--list flat --filter` before a full run; results go to `artifacts/performance/`. The issue's performance table is replaced by the measured result cited, never typed.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
CONTEXT:
All code PRs are merged.

TASK:
Add SerializationBenchmarks to Encina.Caching.Benchmarks and ask docs-writer for the two documentation pages.

KEY RULES:
- BenchmarkSwitcher.FromAssembly(...).Run(args, config), never BenchmarkRunner.Run<T>().
- No hand-typed figures in documentation.

REFERENCE FILES:
- tests/Encina.BenchmarkTests/Encina.Caching.Benchmarks/Encina.Caching.Benchmarks.csproj
- docs/features/query-caching.md
```

</details>

### Phase 8: Cross-cutting integration, observability and coverage targets

<details>
<summary><strong>Tasks</strong></summary>

1. **EventIds:** `CachingSerialization` 508-513 only (508 serialization failed, 509 deserialization failed, 510 decompression limit exceeded, 511 unknown format header, 512 serializer does not support the type, 513 compressed payload not smaller); verified free by a repo-wide grep of `src/Encina.Caching` (used 3500-3513); emitted only by `Encina.Caching` because the codec lives there.
2. **Error codes (ADR-062):** `cache.serialization_failed` `Internal`; `cache.deserialization_failed` and `cache.decompression_limit_exceeded` `UpstreamFailure`; constant messages; they stay inside the codec and the provider turns them into a miss.
3. **Telemetry:** serializer size and duration metrics are deferred to #273 and #723 (open); no tenant data is emitted by this feature (ADR-054 D4 does not apply).
4. **Coverage targets (per flag, set in the PR that adds each file):**

| File | Unit | Guard | Contract | Integration | Justification |
|------|:----:|:-----:|:--------:|:-----------:|---------------|
| `Encina.Caching/Serialization/CacheValueCodec.cs` | 95 | 0 | 0 | 0 | Every header, tier and limit path is unit tested; internal, no `InternalsVisibleTo` for the guard project |
| `Encina.Caching/Serialization/SystemTextJsonCacheSerializer.cs`, `BrotliCacheCompressor.cs` | 100 | 60 | 0 | 0 | Small classes; guard tests reach the argument checks |
| `Encina.Caching/Diagnostics/CacheSerializationLog.cs` | 90 | 0 | 0 | 0 | Generated bodies run in the codec tests; internal |
| `Encina.Caching/Serialization/ICacheSerializer.cs`, `ICacheCompressor.cs` | 0 | 0 | 0 | 0 | Interfaces have no executable lines |
| `Encina.Caching.Redis/RedisCacheProvider.cs` | keep | keep | 0 | 60 | Existing targets are never lowered; the integration flag runs the byte path against a real Redis |
| `Encina.Caching.Hybrid/EncinaHybridCacheSerializerFactory.cs` | 95 | 0 | 0 | 0 | Internal, driven through a real `HybridCache` in unit tests |
| `Encina.Caching.Compression/*.cs`, `Encina.Caching.MessagePack/*.cs`, `Encina.Caching.MemoryPack/*.cs` | 95 | 70 | 0 | 0 | Each algorithm and serializer round trips; guard tests reach the argument checks of the public methods |

   The worker measures every flag with `coverage-report.cs` per PR; no target is lowered (`target-ratchet`).
5. **Contract and property tests:** the shared `ICacheProvider` contract suite is owned by #277; a round-trip property (any byte payload through each compressor) is a unit-level grid in PR 4, not a separate flag.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```
CONTEXT:
The code PRs are merged. Verification remains.

TASK:
Verify each PR carried its per-flag targets and the EventId registration; fix omissions in a small PR through mechanical-fixer; run
--check-justifications.

KEY RULES:
- Never lower an existing target (target-ratchet).
- Add no log, metric or error code beyond the plan.

REFERENCE FILES:
- .github/coverage-manifest/Encina.Caching.json
- docs/testing/coverage-measurement-methodology.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Relevance |
|----------|-----------|
| AGENTS.md section 5 (caching category) | Serialization is consistent across the providers that serialize |
| AGENTS.md sections 1 and 3 | Pre-1.0 replacement of the dead options; fail closed; no message in logs |
| SPEC-000 DEC-009 | MemoryPack in 1.0 scope |
| ADR-062 | Categories of the three error codes |
| OWASP Deserialization Cheat Sheet | Untrusted cache bytes: bounded, type-safe decoding |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in This Feature |
|-----------|----------|----------------------|
| `RedisCacheProvider` | `src/Encina.Caching.Redis/RedisCacheProvider.cs` | The one place the five Redis-protocol products serialize |
| `HybridCacheProvider` | `src/Encina.Caching.Hybrid/HybridCacheProvider.cs` | Adapter target |
| `CacheEntry<T>` | `src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs` | The envelope the codec splits |
| `CachingOptions` and `AddEncinaCaching` | `src/Encina.Caching` | Options replaced and copied |
| `Encina.Caching.Benchmarks` | `tests/Encina.BenchmarkTests/Encina.Caching.Benchmarks` | Home of the serialization benchmarks |
| `RedisFixture` | `tests/Encina.IntegrationTests/Infrastructure/Caching/Fixtures/RedisFixture.cs` | Shared Redis container |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Caching` (new range `CachingSerialization`, block 500-549 of the plan batch registry) | 508-513 | This plan: 508 serialization failed, 509 deserialization failed, 510 decompression limit exceeded, 511 unknown format header, 512 unsupported type fallback, 513 compressed payload not smaller. Family: #272 500-507, #275 514-521, #278 522-533, #276 534-538; 539-549 free. The existing `Caching` range 3500-3549 is untouched |

### Estimated File Count

| Category | New Files | Modified Files | Notes |
|----------|-----------|----------------|-------|
| Production code | 20 | 8 | Core (7), three packages (about 4 each plus READMEs), Hybrid adapter; modified: options, DI x3, Redis provider, `EventIdRanges.cs`, Hybrid DI |
| Tests | 10 | 8 | Unit, guard and benchmarks; Redis integration tests modified |
| Documentation | 5 | 2 | Three READMEs, two feature pages, `query-caching.md` modified |
| Configuration | 12 | 8 | Fragments, manifests, PublicAPI files, `Directory.Packages.props`, `Encina.slnx`, csproj files |
| **Total** | **47** | **26** | 8 pull requests of 15, 7, 10, 8, 14, 12, 12 and 4 files, each at most 15 files and 1500 lines |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Complete Implementation Prompt - All Phases</strong></summary>

```
PROJECT CONTEXT:
Encina is a .NET 10 / C# 14 library, pre-1.0, no backward compatibility. Issue #274 adds a serializer and compression layer to the
caching providers that serialize (Redis family, Hybrid; Memcached via #277; Memory has no serialization). Today Redis hard-codes
System.Text.Json and CachingOptions.SerializerOptions is dead.

IMPLEMENTATION OVERVIEW:
1. PR 1: ICacheSerializer, ICacheCompressor, CacheValueCodec, STJ, Brotli, CacheSerializationOptions, EventIds 508-513.
2. PR 2: CachingOptions.Serialization replaces the dead options. PR 3: RedisCacheProvider uses the codec (Valkey, Dragonfly, Garnet, KeyDB follow).
3. PR 4: Hybrid adapter. PR 5: Encina.Caching.Compression. PR 6: MessagePack. PR 7: MemoryPack. PR 8: benchmarks and docs.

KEY PATTERNS:
- Cache bytes are untrusted: decoding never throws to the caller, output is bounded, a failure is a miss.
- Third-party code only in opt-in packages; defaults are STJ and no compression.
- Header ids are wire-format constants. No static field of a generic class holds per-instance options.
- Every public symbol has PublicAPI lines and guard tests; new packages have manifest, README, slnx and package version entries.
- Follow the maintainer's answers to Design Choices 1 to 6, given in your brief.

REFERENCE FILES:
- src/Encina.Caching.Redis/RedisCacheProvider.cs
- src/Encina.Caching/Configuration/CachingOptions.cs
- src/Encina/Diagnostics/EventIdRanges.cs
- Directory.Packages.props
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ✅ Include | Extends the providers that serialize; Memory is not applicable (no serialization). |
| 2 | OpenTelemetry | ⏭️ Defer | Serializer size and duration metrics are tracked by #273 and #723 (open). |
| 3 | Structured Logging | ✅ Include | `CacheSerializationLog`, EventIds 508-513 in `CachingSerialization`. |
| 4 | Health Checks | ❌ N/A | Serialization is in-process with no backend; provider health is #754. |
| 5 | Validation | ✅ Include | Startup validation of tiers, levels and registered compressors (Design Choice 6). |
| 6 | Resilience | ❌ N/A | Serialization is deterministic; a failed decode degrades to a miss. |
| 7 | Distributed Locks | ❌ N/A | No shared resource. |
| 8 | Transactions | ❌ N/A | Cache operations are independent of units of work. |
| 9 | Idempotency | ❌ N/A | Encoding is deterministic. |
| 10 | Multi-Tenancy | ❌ N/A | The encoding is tenant-agnostic; keys keep the tenant segment of the key generator and no tenant data enters the header or any log or metric. |
| 11 | Module Isolation | ❌ N/A | Module-agnostic; module-scoped keys are #746. |
| 12 | Audit Trail | ❌ N/A | Infrastructure, not a compliance event. |

**Deferred items:** serializer metrics (#273, #723, open).

---

## Prerequisites & Dependencies

| Prerequisite | Status | Notes |
|--------------|--------|-------|
| `RedisCacheProvider`, `HybridCacheProvider`, `CachingOptions` | ✅ Available | On `origin/main` |
| #266 PR 1 (`CacheEntry<T>` envelope fields) | Open | PR 1 of this plan adapts to whatever the envelope holds when it starts |
| Block 500-549 of the EventId registry | ✅ Assigned | Plan batch registry |
| Licence, .NET 10 support and maintenance of each NuGet candidate | Pending | Checked by the worker of PR 5, 6 and 7 |
| Maintainer answers to Design Choices 1 to 6 | Pending | Asked when work starts |

---

## Next Steps

1. The orchestrator asks the maintainer the Dilemmas (Design Choices 1 to 6) when work starts, records the answers under `## Maintainer Decisions`, then briefs the workers in PR order.
2. After PR 3 merges, comment on #277 that the codec is on `main` for the Memcached provider.
