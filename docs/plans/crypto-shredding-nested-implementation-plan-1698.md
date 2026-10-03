# Implementation Plan: `Encina.Marten.GDPR` — Nested crypto-shredding through the System.Text.Json contract

> **Issue**: [#1698](https://github.com/dlrivada/Encina/issues/1698) — [DEBT] Nested [CryptoShredded] properties are stored in plaintext because only the event type is scanned
> **Type**: Technical debt (security; maintainer decision: implement full nested encryption, option B, not rejection)
> **Milestone**: v0.14.0 — Hardening
> **Complexity**: High (9 phases, one package rewritten on its serialization path, no database provider matrix)
> **Estimated Scope**: ~3,200-4,000 lines of production code (about 55% replaces existing code) + ~4,500-5,500 lines of new tests, plus ~5,900 lines of existing Marten.GDPR tests touched, ~3,200 of them rewritten or deleted (see "Existing test migration")
> **Prerequisite**: [#1699](https://github.com/dlrivada/Encina/issues/1699) (PostgreSQL first-key race) lands before, or together with, the core PR; see "Dependencies on open issues"
> **Executor**: one `issue-worker` on Opus (security-critical path, personal data, design-heavy rewrite), docs pages by `docs-writer`

---

## Summary

Today `CryptoShredderSerializer` decorates Marten's `ISerializer`. It scans only the top-level event type with `Public|Instance` flags. Before serialization it overwrites the event's `[CryptoShredded]` strings with envelopes, and it restores them afterwards. Because of this:

- a `[CryptoShredded]` property on a nested object, a collection element, a dictionary value or a non-public member is stored in plaintext;
- an open generic subject-id type is rejected at startup;
- misconfiguration reports only property names, never the reason.

This plan replaces that mechanism with a **System.Text.Json (STJ) contract modifier** installed on every `JsonSerializerOptions` of Marten's `SystemTextJsonSerializer`.

- **Encryption** happens in a wrapped `JsonPropertyInfo.Get`. The wrapper receives the declaring object, reads the subject id from that object's `SubjectIdProperty` sibling, and returns a ciphertext token. The caller's object is never mutated.
- **Decryption** starts in `JsonTypeInfo.OnDeserialized`. That callback fires for every constructed owner at any depth, including constructor-bound record properties. It enqueues the owner into a per-call read scope, and the wrapper decrypts through compiled setters (async with a `CancellationToken` on the async path).

STJ recurses on its own, so the same contract covers several shapes with no graph walk of our own:

- nested objects;
- lists, arrays and immutable collections;
- dictionary values;
- `object`-typed and `[JsonDerivedType]` members;
- records and `init` properties;
- closed generics;
- `SnapshotEnvelope<T>.State`.

**One classifier** (reflection rules plus STJ-contract rules, returning `[Flags]` problems) is shared by the runtime modifier, the startup validator, the 8459 log and a new `CryptoShreddingConfigurationException`. It rejects every observable shape that would otherwise store plaintext or fail to round-trip. The probes in this issue and in the plan review showed plaintext under a naive modifier for: a type-level `[JsonConverter]`, `new` hiding, an interface- or base-typed member whose runtime type carries the attribute only on the implementation or override, and a source-generated `JsonSerializerContext` in `Serialization` mode. They showed broken reads for a normalizing or validating constructor, an `IJsonOnDeserialized` callback and a record owner inside a `HashSet<T>`.

**Installation is verified by identity and by output.** At startup, in the health check and lazily on the first write, the serializer checks that each captured `JsonSerializerOptions` still holds the exact resolver instance it installed (so a later `UseTypeInfoResolver(context)` that puts a context ahead of the modifier is caught), that the modifier registered a plan for every closed candidate type, and that a nested canary instance serializes to a `cs2` token.

**The read path fails closed.** Only a genuinely forgotten subject reads as the placeholder. A `cs2:erased` tombstone lets snapshots and projections of erased subjects be saved again; on read it is accepted only after the key provider confirms the subject is forgotten. `ConfigureMartenCryptoShredding` turns off Marten's `SkipSerializationErrors` for the async daemon, so a decryption failure pauses the projection shard instead of being dead-lettered.

**Consumers follow.** The DSR locator reports nested paths (`Contact.Email`, `Items[].Note`), decrypts only the requested subject's fields, and is guaranteed to take part in DSR requests in any registration order. Erasure becomes idempotent and Marten locations always reach `CryptoShredErasureStrategy` through a routing strategy, whatever other `IDataErasureStrategy` the application registers. Keys are reached through `IServiceScopeFactory`, which removes the scoped-into-singleton capture of the PostgreSQL key provider. `Encina.DomainModeling` base types (`AggregateBase`, `Entity<TId>`) gain the attributes that make them serializable owners, so encrypted snapshots work.

**Standards covered**: GDPR Art. 17 (erasure by crypto-shredding), Art. 25 (data protection by design), Art. 32(1)(a) (encryption of personal data), Art. 5(1)(f) (integrity and confidentiality), Art. 15/20 (access and portability through the locator).

**Affected packages**:

- `Encina.Marten.GDPR` (main);
- `Encina.Compliance.DataSubjectRights`: documentation of `PersonalDataLocation.FieldName` and `ErasureScope.SpecificFields`; one behaviour change, no API change: `CompositePersonalDataLocator` returns `Left` when any inner locator fails (today it logs and returns the partial inventory), plus an internal accessor for its inner locators;
- `Encina.DomainModeling`: `[JsonIgnore]` on `AggregateBase.UncommittedEvents` and `Entity<TId>.DomainEvents`, `[JsonInclude]` on the protected `Id` setters;
- `Encina.Marten`: no code change; `MartenEventMetadataQuery` and `MartenProjectionManager` are named as affected consumers of the fail-closed read (documented and tested, see "Consumers");
- `Encina.BenchmarkTests` (`Encina.Marten.GDPR.Benchmarks`);
- the test projects.

**Provider category**: none of the §5 categories. Event sourcing (Marten) is the only provider, and Marten 9.38 core ships only System.Text.Json.

---

## Design Choices

<details>
<summary><strong>1. Mechanism — STJ contract modifier (Get-encrypt, OnDeserialized-decrypt) instead of a graph-walking decorator</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Keep the `ISerializer` decorator; walk a per-type plan graph and mutate-then-restore behind a process-wide gate** | Serializer-agnostic; a converter that reads the property writes ciphertext; computed getters see ciphertext | A process-wide lock around inner STJ serialization caps throughput for every PII write; the caller's live graph is still mutated (other application threads see envelopes); the walk follows members STJ never writes (private fields of foreign runtime types); a stable-reference rule rejects common `AsReadOnly()` aggregate shapes; copy-yielding collections stay an undetectable plaintext hole; the walk overhead spreads to most documents |
| **B) STJ contract modifier with a call scope, returning early for `Kind != Object`** | No mutation, no lock; STJ coverage of nesting, collections and polymorphism for free | Verified plaintext path: a type-level `[JsonConverter]` gives `Kind=None` and its converter writes the PII in clear; nothing detects a modifier dropped by a later `UseTypeInfoResolver`; two gates (pass-through vs installed Get wrapper) can disagree with no defined unscoped encryption path |
| **C) STJ contract modifier with one reflection+contract classifier, canary, fail-closed reads, tombstone** | Everything B gives, plus rejection of every shape the probes showed leaking, cached by STJ as a permanent fail-closed; installation verification (resolver identity plus canary output); fail-closed reads | STJ-only; residual plaintext paths that bypass serialization (Marten `Patch`, duplicated fields) need docs and a guard; wide scope |

### Chosen Option: **C, with grafts from A and B**

### Rationale

- **Why A lost.** Mutation can protect what a converter or a computed getter reads, but that is the only advantage, and it costs a lock around Marten's serialization CPU and a visible mutation of the caller's object. Its residual holes (collections that yield copies, unstable navigation getters) can only be documented, never verified closed. A also kept the fail-open read path, which breaks AGENTS §3 ("errors are never swallowed in background infrastructure").
- **Why B lost.** It has the right architecture but leaks through its own early return: `ti.Kind != Object` covers exactly the converter case a probe showed (`{"Val":"v"}` written in clear; throwaway probe during the plan review, 2026-10-03; not kept in the repository). It also has no runtime proof that the modifier is still installed.
- **Why C.** Its classifier names the leaking shapes and throws from the modifier. STJ caches that throw, so the type stays rejected for the life of the process, and the startup validator reuses the same code path. The installation check (resolver identity per options object, a registered plan for every closed candidate type, and a nested canary whose output must hold a `cs2` token) closes the "modifier silently dropped or bypassed" hole; a canary alone does not, because a user `JsonSerializerContext` placed ahead of the modifier never resolves the internal canary type. It needs no lock and does not mutate the caller's object, and types without `[CryptoShredded]` anywhere keep untouched contracts.
- **Grafts.**
  - From B: a pooled staging buffer for the `WriteTo*` entry points, so a failed write leaves the caller's buffer untouched; key access through `IServiceScopeFactory` with a lazy DI scope per call; zeroing key copies at scope end; an explicit behaviour when a Get wrapper runs with no ambient scope.
  - From A: the path format with no index or key values; rejection of dictionary key types that reach `[CryptoShredded]`; each problem reported once, on its declaring type; a read-back check after decryption.
  - Closing C's own gaps: a new contract rule rejects *computed* members (serialized but not round-trippable) beside `[CryptoShredded]` properties, so `string Masked => Email[..3]` cannot leak. Another rule rejects *container* converters, so a converter on a type whose members reach a crypto owner can no longer hand-write a child in plaintext.
  - Closing the gaps found in plan review: rules reject an attributed property that implements an interface member or overrides a base member without the attribute (an `IContact`-typed member would otherwise be written through the unattributed contract), a source-generated contract on the crypto graph, a constructor that receives a `[CryptoShredded]` value and may transform it, an owner implementing `IJsonOnDeserialized`, a value-equality owner inside a hashed or sorted set, and a computed member on any type whose graph reaches an owner (an error, not a warning).

</details>

<details>
<summary><strong>2. Subject resolution — sibling on the declaring object, at every depth</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Sibling `SubjectIdProperty` on the object that declares the property (today's rule, applied at every depth)** | One meaning for encrypt, decrypt and the locator; the Get wrapper and `OnDeserialized` both receive exactly that object; a value object shared by two subjects is unambiguous | A nested value object must carry its own subject-id property |
| **B) Ancestor lookup (for example the event's `PatientId` for a nested `ContactInfo`)** | No repeated id in value objects | Needs a path or scope syntax; `Get` and `OnDeserialized` do not see ancestors; a shared value object has an ambiguous subject; the locator and decryption would need the ancestor chain |

### Chosen Option: **A — sibling on the declaring object**

### Rationale

- This is the documented contract of `CryptoShreddedAttribute` ("a sibling property on the same declaring type"). STJ hooks also expose only the declaring object.
- Lookup walks the `BaseType` chain with `DeclaredOnly|Public|NonPublic|Instance`, and the most-derived declaration wins. This removes the `AmbiguousMatchException` that `GetProperty(name)` throws today when `new` hides a property with a different type (`CryptoShreddedPropertyCache.cs:184-186`, `CryptoShreddingAutoRegistrationHostedService.cs:202-204`).
- A nested object for a different subject than its root (for example a therapist block inside a patient event) uses its own subject's key and is erased independently.

</details>

<details>
<summary><strong>3. Shape policy — encrypt what STJ writes, reject at startup what would leak or not round-trip</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Coverage equals the STJ contract; every unobservable or unsafe shape is a classifier problem** | Coverage is provable; each rejection has a reason code; persistent fail-closed through STJ's cached modifier failure | Some legitimate shapes need a small change by the user (`[JsonIgnore]` on computed members, a class instead of a struct) |
| **B) Support every shape, including structs and string collections, by rebuilding values** | Fewer user changes | Structs reach `OnDeserialized` boxed, so a write-back is impossible without a custom converter; collections would have to be rebuilt per concrete type; much larger surface |
| **C) Reject nested types entirely (the issue's first proposal)** | Smallest change | Rejected by the maintainer (option B of #1698) |

### Chosen Option: **A**

### Rationale

- Nesting, collections, dictionaries, polymorphism, records and generics come from STJ itself.
- The rejected shapes are the ones where the probes or the code show plaintext or data loss: converter owners and containers, `new` hiding, attributes only on an interface, attributes only on an implementation or override of an unattributed declaration, source-generated contracts, constructor-bound values outside compiler-synthesized positional records, `IJsonOnDeserialized` owners, value-equality owners in hashed or sorted sets, computed members (on the owner and on any type whose graph reaches one), structs, getter-only properties, non-public members without `[JsonInclude]`, property converters and dictionary keys.
- An interface that declares a `[CryptoShredded]` property has no safe form (the attribute on the interface member is rejected as `DeclaredOnInterface`, the attribute only on the implementation as `ImplementsUnattributedMember`). The documented fix is to keep personal data off interfaces and type such members with the concrete class. A base class can carry the attribute on its own declaration, which overrides inherit.
- `[CryptoShredded]` on `List<string>`/`string[]` stays `NotString` in this change. A follow-up `[FEATURE]` issue covers string collections (Follow-up issues, item 2).

</details>

<details>
<summary><strong>4. Wire format — compact v2 token with AES-GCM associated data, strict parsing, tombstone</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Keep the v1 JSON-in-a-string envelope (`{"__enc":true,"kid":"subject:{id}:v{n}",...}`)** | No change | Subject id survives erasure inside the immutable event (`kid`); `StartsWith` prefix detection misreads plaintexts that begin with the marker; `MemoryStream` + `Utf8JsonWriter` + `JsonDocument` per field |
| **B) Object-valued JSON node** | Structured | Changes the property contract from `string` to object and breaks constructor parameter binding |
| **C) Compact string token `cs2:{version}:{base64url(nonce‖ciphertext‖tag)}` with AAD `encina:cs2:{subjectId}:{version}` and tombstone `cs2:erased`** | No subject id stored in the token; a token copied onto another subject's object fails integrity; strict detection by contract; smaller and allocation-light | No v1 reader (none needed pre-1.0) |

### Chosen Option: **C**

### Rationale

- Pre-1.0 with no users, so no v1 reader is needed (AGENTS §1).
- Detection is by contract. Every non-null value of a `[CryptoShredded]` property must parse as a v2 token or tombstone, otherwise the read throws `EnvelopeMalformed`, so no prefix guessing remains.
- Associated data binds subject and key version. It does not bind the property name, so renaming a property never breaks decryption.
- **Key scope is not reserved in the token.** Open issues [#1144](https://github.com/dlrivada/Encina/issues/1144) (keys per subject and category instead of one key per subject) and [#1191](https://github.com/dlrivada/Encina/issues/1191) (tenant-scoped keys, AC-043; KMS wrapping; durable default store) will change the key model. Whichever design they choose will add its scope to the token, the AAD and the per-call cache keys; reserving a segment now would guess that design. Pre-1.0 with no users, that later format change costs nothing (AGENTS §1), so v2 stays scope-free and #1144/#1191 own the change.

</details>

<details>
<summary><strong>5. Read-path semantics — fail closed except for a genuinely forgotten subject; tombstone on re-save</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Keep today's behaviour: every key `Left`, exception or tag mismatch reads as `"[REDACTED]"`** | Reads always succeed | A key-store outage during a projection rebuild bakes the placeholder into read models and, now that snapshots are encrypted, a re-save permanently overwrites real data; tampering looks like erasure. Violates AGENTS §3 |
| **B) Only `crypto.subject_forgotten` (or a tombstone on a subject the provider confirms forgotten) reads as the placeholder; everything else throws `CryptoShreddingDecryptionException`** | Outages surface; with `SkipSerializationErrors` turned off by the configurator the async daemon pauses the shard and resumes after recovery instead of dead-lettering the event; tampering is reported as `IntegrityCheckFailed` | Reads stop during a key-store outage; a lost key (#1699 race, in-memory store after a restart) blocks the affected stream until fixed |

### Chosen Option: **B**

### Rationale

- AGENTS §3 already decides this: errors are never swallowed in background infrastructure, and compliance gates fail closed. This is a rule, not an open product decision.
- **Re-save of a forgotten subject.** A placeholder-carrying snapshot or read model would otherwise throw `subject_forgotten` forever on its next save. When the key provider returns `Left(crypto.subject_forgotten)` **and** the value equals `AnonymizedPlaceholder` (ordinal), the Get wrapper writes the tombstone `cs2:erased` instead of throwing. Nothing personal is stored and no key is created, so Art. 17 is respected. Any other value for a forgotten subject still throws `KeyUnavailable`.
- Forgotten detection uses the provider error code and an explicit `DecryptOutcome`, never a comparison of the decrypted string with the placeholder (today's `RecordDecryptedField` miscounts a real plaintext equal to `"[REDACTED]"`).
- **The tombstone is not trusted on its own.** It is a constant with no associated data, so a tombstone copied onto a live subject's field (database edit, bug, copied row) would otherwise read silently as the placeholder and fire a false Art. 17 notification. On read, the subject id is checked first (missing → `SubjectIdMissing`), then the provider must confirm the subject is forgotten (`IsSubjectForgottenAsync`, once per subject per call, cached in the frame). A live subject throws `IntegrityCheckFailed`; a provider error throws `KeyUnavailable`. Forgetting is permanent in both key providers (`SubjectForgottenMarker` in PostgreSQL, the forgotten set in memory), so a legitimate tombstone always passes.
- **Async daemon.** Marten 9.38 defaults `Projections.Errors.SkipSerializationErrors` and `SkipApplyErrors` to `true` for continuous projections, so a failed event deserialization is dead-lettered and the projection permanently misses it (verified by probe on `new StoreOptions()`; `EventDeserializationFailureException` is governed by `SkipSerializationErrors` per Marten.xml). `ConfigureMartenCryptoShredding` therefore sets `Projections.Errors.SkipSerializationErrors = false`, and the startup validator fails with `ProjectionSkipsSerializationErrors` when a later configuration turns it back on. `SkipApplyErrors` stays the application's choice: decryption happens during deserialization, before `Apply`. Rebuilds already stop on errors (`RebuildErrors.SkipSerializationErrors = false`).
- **In-memory key store.** `InMemorySubjectKeyProvider` (still the default) loses every key on restart while events persist. Under this design every older crypto field then throws `KeyUnavailable` (or `IntegrityCheckFailed` after a new v1 key is created), instead of silently reading as the placeholder, and a subject forgotten before the restart is no longer known as forgotten. Making a durable store the default belongs to #1191; this change logs Warning 8481 at startup when the resolved provider is `InMemorySubjectKeyProvider`, reports `keyProviderType` in the health check data, documents the restart behaviour, and tests it.

</details>

<details>
<summary><strong>6. Key access and lifetimes — <code>IServiceScopeFactory</code>, per-call caches, no cross-call cache</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Keep `ISubjectKeyProvider` injected into the singleton `ConfigureMartenCryptoShredding`** | No change | Scoped `PostgreSqlSubjectKeyProvider` (holding an `IDocumentSession`) is captured by a singleton; the session needs the `IDocumentStore` being built (construction cycle); unproven by any DI test |
| **B) Make `PostgreSqlSubjectKeyProvider` a singleton that opens its own session** | Removes the capture | Changes a store's lifetime and its session handling in the same change |
| **C) Serializer resolves `ISubjectKeyProvider` and `IForgottenSubjectHandler` through `IServiceScopeFactory`, in a DI scope created lazily on the first key need of a call** | No capture, no cycle (the store exists by the time a call needs a key), Scoped providers work unchanged, re-entrant `SaveChanges` of the key store runs in its own scope | One extra DI scope per serializer call that needs a key |

### Chosen Option: **C**

### Rationale

- The configurator takes only `IServiceScopeFactory`, `IOptions<CryptoShreddingOptions>` and a logger. Provider lifetimes stay as they are, and a `ValidateOnBuild`/`ValidateScopes` DI test proves the registration for both key stores (AGENTS §3, registration completeness).
- **Key caching.** Keys are cached per call only: `subject → active key` on write, `(subject, version) → result` on read. The forgotten handler runs once per subject per call. Nothing is cached across calls, so a key deleted by Art. 17 erasure is never served from a cache.
- **Key ownership.** Providers return their own stored arrays: `InMemorySubjectKeyProvider.cs:106,173,184` and `PostgreSqlSubjectKeyProvider.cs:95,161,176` hand out the arrays they keep, and the in-memory provider zeroes those same arrays on erasure (`:222`, `:418`). The frame therefore never caches a provider's `SubjectEncryptionKey` or `byte[]`. It validates the material (version ≥ 1, length exactly 32, on write **and** on read), builds the `AesGcm` immediately (`AesGcm` copies the key at construction, verified by probe) and caches only `(version, AesGcm)`. If a copy is ever needed it is a frame-owned buffer. At scope end the frame zeroes only frame-owned buffers (`CryptographicOperations.ZeroMemory`) and disposes the `AesGcm` instances (one per key per call). It never zeroes an array a provider returned.
- **Key creation races.** Concurrent first writes for one subject and writes concurrent with an erasure are races inside `PostgreSqlSubjectKeyProvider` (#1699 and its extension, see "Dependencies on open issues"). A per-call cache cannot fix them, and under the fail-closed read a lost key blocks the stream instead of reading as the placeholder, so #1699 is a prerequisite.

</details>

<details>
<summary><strong>7. Error model — three exceptions, one <code>[Flags]</code> problem enum, one issue record</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Keep `CryptoShreddingEncryptionException` with `PropertyMisconfigured` and comma-joined names** | No API change | No per-property reason (the gap named in #1698); configuration and runtime failures mixed |
| **B) One `CryptoShreddingException` with an `Operation` enum and every reason** | One catch type | Mixes configuration errors (fix the code) with runtime errors (retry or investigate) |
| **C) `CryptoShreddingConfigurationException` (problems with the model or wiring), `CryptoShreddingEncryptionException` (write-time data failures), `CryptoShreddingDecryptionException` (read-time failures); `CryptoShreddedPropertyProblems` flags + `CryptoShreddedPropertyIssue` record** | Each exception has one meaning and one handling policy; every problem of a property is reported at once; structured, no personal data | Larger public surface |

### Chosen Option: **C**

### Rationale

- Configuration problems are found at startup and must stop the host. Encryption and decryption failures are per document and must abort or surface the operation. Separate types let callers and logs treat them differently.
- Messages carry type names, property names, problem flags and error codes only. They never carry a subject id, a value, an `EncinaError.Message` or an inner exception message, and no inner exception is attached (rule kept from #1646).

</details>

<details>
<summary><strong>8. Serializer scope — System.Text.Json only; any other inner serializer fails closed</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) STJ only; a non-STJ inner serializer throws at configuration** | Matches Marten 9.38 core (STJ only, `Marten.Newtonsoft` referenced nowhere); one test matrix | Users of `Marten.Newtonsoft` cannot use crypto-shredding until a follow-up |
| **B) Ship a Newtonsoft adapter in this change (`IContractResolver` + `IValueProvider` + `OnDeserializedCallbacks`)** | Parity | New package dependency, second integration matrix, no current user |
| **C) Pass a non-STJ serializer through unencrypted** | — | Fail-open; forbidden |

### Chosen Option: **A**

### Rationale

- No project references `Marten.Newtonsoft`. A follow-up `[FEATURE]` issue (Follow-up issues, item 1) carries the adapter design: `IValueProvider.GetValue(target)` receives the declaring object exactly like STJ's `Get`.
- The stale `UseNewtonsoftForSerialization` remarks in `CryptoShredderSerializerFactory` and `ConfigureMartenCryptoShredding` are corrected.
- **Marten's raw `JsonDocument` upcasters are not supported.** `Marten.Services.Json.Transformations.SystemTextJson.JsonTransformations` casts the store serializer to the concrete `SystemTextJsonSerializer` and throws `MartenException("Cannot use SystemTextJson upcaster with serializer of type ...")` otherwise; `JsonDocumentFromJson` is public but not virtual, so neither the wrapper nor a subclass can intercept it. With crypto-shredding enabled, a stream that uses such an upcaster fails to load (fail closed, no plaintext). Encina's own versioning is not affected: `EventUpcasterBase` and `LambdaEventUpcaster` derive from Marten's CLR-typed `EventUpcaster<TFrom,TTo>`, which deserializes through `ISerializer`. Phase 5 checks whether Marten 9.38 exposes the registered upcasters on `StoreOptions.Events`; if it does, the startup validator fails with `JsonDocumentUpcasterNotSupported`, otherwise the limitation is documented. Either way a test shows the behaviour, and a follow-up spike (Follow-up issues, item 8) studies a design that keeps the real `SystemTextJsonSerializer` registered and moves the frame and staging duties elsewhere.

</details>

<details>
<summary><strong>9. DSR integration — path <code>FieldName</code>, idempotent erasure, deterministic strategy registration</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Add a `Path` member to `PersonalDataLocation`** | Explicit | Public API change in `Encina.Compliance.DataSubjectRights` for one consumer |
| **B) Put the path in `FieldName` (`Email`, `Contact.Email`, `Items[].Note`, `Notes{}.Text`)** | No DSR API change; exporters and `ErasureScope.SpecificFields` already treat `FieldName` as a label | `SpecificFields` must match the full path (documented) |

### Chosen Option: **B**, plus erasure routing, idempotency and fail-closed participation

### Rationale

- Paths never contain an index or a dictionary key, because both can be data. A top-level property keeps its bare name.
- **Crypto-shred erasure is subject-wide.** `CryptoShredErasureStrategy.EraseFieldAsync` deletes every key of the subject whatever `FieldName` is (`Erasure/CryptoShredErasureStrategy.cs:78-80`); open bug [#1144](https://github.com/dlrivada/Encina/issues/1144) reports exactly this and is resolved through SPEC-002 (#1186, #1191). This change does not alter that scope. The docs say that, for crypto-shredded Marten data, a `SpecificFields` scope or a single path shreds every field of the subject until #1144 is resolved, and they never present path matching as field-scoped erasure. The new tests assert only whole-subject erasure, which holds under either fix #1144 proposes.
- **Idempotent erasure.** `CryptoShredErasureStrategy` maps `Left(crypto.subject_forgotten)` from `DeleteSubjectKeysAsync` to success. Today `DefaultDataErasureExecutor` calls it once per location, so N locations report N−1 failures, and nesting multiplies N. Idempotency is independent of #1144: several locations share one subject key under either key model.
- **Erasure routing by provenance.** `DefaultDataErasureExecutor` resolves exactly one `IDataErasureStrategy` and applies it to every location (`DefaultDataErasureExecutor.cs:36,152`), and `DataSubjectRights/ServiceCollectionExtensions.cs:72` documents registering a custom strategy. A custom strategy would receive Marten crypto locations, never call `DeleteSubjectKeysAsync`, and report success while the data stays decryptable (Art. 17 fail-open); conversely, `CryptoShredErasureStrategy` as the only strategy deletes subject keys and reports success for locations of other locators. So `AddEncinaMartenGdpr` registers an internal `CryptoShredRoutingErasureStrategy` as the `IDataErasureStrategy`:
  - it replaces any `IDataErasureStrategy` descriptor already registered and keeps it as its *inner* strategy (resolved from that descriptor; `HardDeleteErasureStrategy` from DSR's `TryAdd` is inner when DSR registers first; when DSR registers later its `TryAdd` is a no-op);
  - locations produced by `MartenEventPersonalDataLocator` are recorded in a static `ConditionalWeakTable<PersonalDataLocation, object>` (reference identity; the executor passes the locator's instances unchanged) and routed to `CryptoShredErasureStrategy`; every other location goes to the inner strategy, or fails with `crypto.erasure_strategy_missing` when there is none;
  - a strategy registered with `Add`/`Replace` **after** `AddEncinaMartenGdpr` bypasses the router; the startup validator resolves `IDataErasureStrategy` in a scope and fails with `ErasureStrategyBypassed` when it is not the router, with a message saying to register custom strategies before `AddEncinaMartenGdpr`.
  - Proven by DI tests: DSR before and after, a custom strategy before (it becomes inner and receives only non-Marten locations) and after (startup fails).
- **Locator participation.** DSR consumers (`DefaultDataErasureExecutor.cs:46`, `DefaultDSRService.cs:64`, `DefaultDataPortabilityExporter.cs:48`) inject a single `IPersonalDataLocator`, nothing registers `CompositePersonalDataLocator`, and `AddEncinaMartenGdpr` adds its locator with a plain `AddScoped`, so the last registration wins and the Marten data can silently drop out of access and erasure. The startup validator fails with `PersonalDataLocatorBypassed` when more than one `IPersonalDataLocator` is registered and the resolved one is neither the Marten locator nor a `CompositePersonalDataLocator` containing it. `CompositePersonalDataLocator` today logs a failing locator and returns `Right` with partial results whenever another locator succeeded (`Locators/CompositePersonalDataLocator.cs:66-97`, `Left` only when `failedLocators > 0 && allLocations.Count == 0`); it changes to return `Left` when any locator fails, so the Marten locator's `Left` is never swallowed (AGENTS §3: compliance gates fail closed). The misleading registration comment ("CompositePersonalDataLocator aggregates all") is corrected.

</details>

---

## Shape coverage

Coverage is exactly what STJ writes with Marten's options (`IncludeFields=false`, `MaxDepth` 64). Each declaring type is classified on its own wherever it appears. A rejected shape makes the startup validator fail, and its first runtime use throws `CryptoShreddingConfigurationException` from the modifier before any byte is produced. STJ caches that failure for the type.

| Shape | Result | Problem flag (if rejected) |
|-------|--------|----------------------------|
| `[CryptoShredded]` string on a class or record class, top level | Encrypted | — |
| Same, on a nested object at any depth (null nested object is written as null with no key lookup) | Encrypted | — |
| Owner as element of `List<T>`, `T[]`, `IReadOnlyList<T>`, `IEnumerable<T>`, `ImmutableArray<T>`, `ImmutableList<T>` (null elements skipped) | Encrypted; on read the element instance is updated, never the collection | — |
| Owner as element of a hashed or sorted set (`HashSet<T>`, `ISet<T>`, `IReadOnlySet<T>`, `ImmutableHashSet<T>`, `SortedSet<T>`, `ImmutableSortedSet<T>`, `FrozenSet<T>`) when the owner has value equality (a record, an overridden `Equals`/`GetHashCode`, or `IComparable`/`IComparable<T>`) | Rejected (probe: STJ inserts the element while it holds the token; decrypting in place changes its hash, so `Contains` fails for the same instance and equal elements both stay) | `OwnerInHashedCollection` |
| Same sets with a reference-equality owner (plain class, no overrides, no comparer-relevant interface) | Encrypted; hash and order do not depend on the property | — |
| Owner as `Dictionary<K,V>` / `IReadOnlyDictionary<K,V>` value | Encrypted (values only) | — |
| Owner type reachable from a dictionary **key** type | Rejected | `DictionaryKeyCarriesCryptoShredded` |
| Owner held in an `object`-typed member, or behind `[JsonDerivedType]` polymorphism | Encrypted (STJ writes the runtime contract). Read-back of an `object` member yields a `JsonElement` holding the token (unreadable, not plaintext); documented, use `[JsonDerivedType]` for round trips | — |
| Owner behind a non-sealed declared base without polymorphism configuration, when the base does **not** declare the attributed member | Derived-only members are not written at all (no leak, data not stored); documented | — |
| Member typed as an interface or base class that **declares** the property, with `[CryptoShredded]` only on the implementing class or the override (`IContact Contact`, `List<IContact>`, `BaseContact Item` with an attributed override) | Rejected on the implementing or overriding type (probe: STJ writes the declared type's contract, whose property has no attribute, so the runtime value is written in clear) | `ImplementsUnattributedMember` |
| Positional record, constructor-bound property, primary constructor synthesized by the compiler (the parameter is stored unchanged into the property's backing field) | Encrypted; decrypted after construction through the compiled `init` setter | — |
| Any other constructor receiving a `[CryptoShredded]` value (`[JsonConstructor]`, a record whose property is explicitly declared with an initializer such as `= Email.Trim()`) | Rejected (probe: the constructor receives the token; normalizing it corrupts the token so the data becomes unreadable, validating it throws during deserialization, while the write succeeds) | `BoundToConstructorParameter` |
| Owner implementing `IJsonOnDeserialized` | Rejected (probe: the callback runs before the deferred decryption and sees the token) | `OwnerImplementsOnDeserialized` |
| Contract on the crypto graph (owner or container) produced by a source-generated `JsonSerializerContext` (`JsonTypeInfo.OriginatingResolver is JsonSerializerContext`), or an owner contract that lacks an attributed property the reflection shape says is serialized | Rejected (probe: a `GenerationMode=Serialization` context writes the owner through its fast path with an empty property list, root and nested, in clear, while the modifier and a type-only canary report success) | `SourceGeneratedContract` |
| `init` or `private set` property with `[JsonInclude]` | Encrypted | — |
| Non-public (`private`/`protected`/`internal`) property with `[JsonInclude]`, including private properties declared on a base class | Encrypted (setter compiled on the declaring type) | — |
| Non-public property without `[JsonInclude]`, or public with `[JsonIgnore]` | Rejected (attribute has no effect, the value is never stored) | `NotSerialized` |
| In the contract but `Set == null` and no constructor parameter (public get, private set, no `[JsonInclude]`) | Rejected (cannot round-trip) | `NotDeserializable` |
| Getter-only property (including constructor-bound getter-only) | Rejected (no hook can write the plaintext back) | `NoSetter` |
| Virtual override (attribute inherited) | Encrypted once (deduplicated by `GetBaseDefinition()`) | — |
| `new` property without the attribute hiding an attributed base property | Rejected (probe: derived plaintext written) | `HiddenByDerivedProperty` |
| Attribute declared on an interface member | Rejected | `DeclaredOnInterface` |
| Interface member attributed, implementing property not attributed | Rejected on the implementing type | `AttributeOnlyOnInterface` |
| Property declared on a `struct` / `record struct` (any depth, including struct elements) | Rejected (`OnDeserialized` receives a boxed copy) | `DeclaredOnValueType` |
| Property not of type `string` (including `List<string>`, `string[]`) | Rejected | `NotString` |
| Property without `[PersonalData]` | Rejected | `MissingPersonalData` |
| Property not readable / indexer | Rejected | `NotReadable` / `Indexer` |
| Owner type written by a type-level `[JsonConverter]` or a converter in `options.Converters` (`Kind=None`), or owner implementing `IEnumerable` (`Kind=Enumerable`) | Rejected (probe: plaintext or dropped) | `OwnerNotSerializedAsObject` |
| **Container** type whose reflection member graph reaches an owner type but whose contract is written by a converter, or a property with `[JsonConverter]` whose type reaches an owner | Rejected (the converter would hand-write the child in clear) | `ConverterOverCryptoGraph` |
| `[JsonConverter]` on the `[CryptoShredded]` property itself | Rejected | `CustomConverterOnProperty` |
| Serialized member of an owner type that cannot be read back (no setter, no constructor parameter): typically a computed getter such as `Masked => Email[..3]` | Rejected; fix with `[JsonIgnore]` or make it settable | `ComputedMemberBesideCryptoShredded` |
| Same computed shape on an **ancestor or container** type whose graph reaches an owner (for example `string Summary => Contact.Email` on the event) | Rejected, like on the owner; fix with `[JsonIgnore]` or make it settable. A *settable* property whose value the application derives from PII cannot be detected; documented | `ComputedMemberOverCryptoGraph` |
| Encina's `AggregateBase` / `AggregateRoot` / `Entity<TId>` subclasses as owners (snapshot state, read models) | Encrypted after Phase 6 adds `[JsonIgnore]` to `AggregateBase.UncommittedEvents` and `Entity<TId>.DomainEvents` and `[JsonInclude]` to the protected `Id` setters. Without that change every such owner fails `ComputedMemberBesideCryptoShredded`, and `Id` as subject id fails `SubjectIdPropertyNotRoundTripped` (probe on Marten 9.38: `UncommittedEvents` serialized with no setter; `Id` lost on round trip, which is also a snapshot bug today) | — |
| Subject-id sibling missing / unreadable / unsupported declared type / not serialized or not deserializable / itself `[CryptoShredded]` | Rejected | `SubjectIdPropertyNotFound`, `SubjectIdPropertyNotReadable`, `SubjectIdTypeUnsupported`, `SubjectIdPropertyNotRoundTripped`, `SubjectIdPropertyIsCryptoShredded` |
| Open generic owner (`E<TId>`) seen by the startup scan | Reflection rules only; subject-id type, setter compilation and contract rules deferred (8470); every closed type (`E<Guid>`) is fully checked at its first use and by the scan's contract walk | — |
| Closed generic with an unsupported subject type (`E<object>`) | Rejected | `SubjectIdTypeUnsupported` |
| Cycles, shared references | STJ's `ReferenceHandler`/`MaxDepth` apply exactly as without crypto; shared instances are encrypted per occurrence with fresh nonces, no mutation; on read owners are deduplicated by reference | — |
| Members Marten writes without serialization (`Patch().Set`, duplicated fields, `FlatTableProjection` columns) | Not covered by any serializer hook; Phase 5 adds a startup check for duplicated fields if the Marten 9.38 mapping API exposes them (`DuplicatedByMarten` flag), otherwise a follow-up issue; `Patch` documented as forbidden | `DuplicatedByMarten` (conditional) |

---

## Subject resolution rule

- **Rule.** A `[CryptoShredded]` property is encrypted with the key of the subject whose id is the value of the property named by `SubjectIdProperty` on **the same object instance that declares it**, at every depth. There is no ancestor lookup.
- **Lookup.**
  - The sibling is searched on the declaring type and then up the `BaseType` chain with `DeclaredOnly|Public|NonPublic|Instance`; the most-derived declaration wins.
  - It must be readable, and its **declared** type must pass `SubjectIdConversion.IsSupportedType` (string, `Guid`, integer types or their nullable forms, or a strongly-typed id).
  - It must be in the STJ contract and deserializable (setter or constructor parameter), and it must not itself carry `[CryptoShredded]`.
- **Generic subject ids.** When the declared subject type `ContainsGenericParameters` (open `E<TId>` seen by the startup scan), the type check is deferred and logged at 8470 (Debug). The modifier only sees closed types and applies the full check there.
- **Value.** The value is read through a compiled `Func<object, object?>` and converted with `SubjectIdConversion.ToInvariantString`. `null`, `Guid.Empty` and blank strings mean missing, which fails closed on write (`SubjectIdMissing`) and on read (`SubjectIdMissing`). An unsupported **runtime** value type maps to `SubjectIdInvalid`, never to a raw `InvalidOperationException`.
- **Read side.** The key is (subject from the sibling of the constructed owner, version from the token). The AAD binds both.
- **Pseudonymous ids.** A subject-id property that itself carries `[PersonalData]` produces startup warning 8475. It is not an error, because a GUID user id may legitimately be marked as personal data. The docs require pseudonymous ids.

---

## Serializer behaviour

**System.Text.Json (supported).** `CryptoShredderSerializerFactory.Apply` requires `options.Serializer()` to be `Marten.Services.SystemTextJsonSerializer`. It calls `Configure(o => o.TypeInfoResolver = (o.TypeInfoResolver ?? new DefaultJsonTypeInfoResolver()).WithAddedModifier(modifier.Modify))`, which the probes show runs once for each of Marten's four internal option sets (`_options`, `_clean`, `_withTypes`, `_optionsDeserialize`). Behaviour:

- Each captured options object is recorded in a `ConditionalWeakTable<JsonSerializerOptions, InstalledResolver>` that keeps the exact `IJsonTypeInfoResolver` instance assigned and a snapshot of `TypeInfoResolverChain` (count and order). An already-installed modifier makes `Apply` a no-op (8460 not repeated).
- A resolver the user set before `Apply` (`UseSystemTextJsonForSerialization(configure: o => o.TypeInfoResolver = ...)` or `UseTypeInfoResolver(context)` called earlier) is kept and wrapped, so the modifier sees its contracts (probe: a default-mode context installed before the modifier is encrypted). A source-generated context in `Serialization` mode is still rejected per type by `SourceGeneratedContract`.
- **Ordering.** Marten's `UseTypeInfoResolver(context)` installs `JsonTypeInfoResolver.Combine(context, existing)`, putting the context **ahead** of the modified resolver (decompiled Marten 9.38 `SystemTextJsonSerializer.UseTypeInfoResolver`; probe: the context's types are written in clear and the modifier never runs for them). It must therefore run before `AddEncinaMartenGdpr`'s configurator; the docs say so, and the installation check below catches the wrong order.
- An already-wrapped serializer is a no-op.
- Options that are already read-only throw `CryptoShreddingConfigurationException(ContractModifierMissing)`.

Every `ISerializer` member is covered. Members outside `ISerializer` that Marten reaches by casting to the concrete `SystemTextJsonSerializer` (the `JsonDocument` upcasters) are not, and fail closed (Design Choice 8). All write members, including both `WriteToParameter` overloads, push a write scope. All `FromJson*`/`FromJsonAsync*` members push a read scope. `EnumStorage`, `Casing` and `ValueCasting` delegate to the inner serializer.

`WriteTo`, `WriteToCleanJson` and `WriteToJsonWithTypes` stage through a `[ThreadStatic]` pooled `ArrayBufferWriter<byte>` and copy to the caller's writer only on success, and only when the root type's contract graph can reach a crypto owner. That condition is cached per type: a crypto owner reachable, or an open slot such as an `object` member or a polymorphic declared type. `WriteToParameter` leaves `Value` null on failure (verified by probe), so it needs no staging.

**Newtonsoft (not supported).** A non-STJ inner serializer makes `Apply` log 8468 and throw `CryptoShreddingConfigurationException(SerializerNotSupported)`. It is never passed through unencrypted.

**Marten coverage.** The modifier applies to everything that `StoreOptions` serializes:

- events (except streams read through a raw `JsonDocument` upcaster, which throw);
- documents and read models;
- `SnapshotEnvelope<TAggregate>.State`, which becomes encrypted for the first time;
- the key store's own documents, which carry no attribute and are untouched.

**Installation check** (`CryptoShredderSerializer.VerifyContractModifierInstalled()`, run by the startup validator, the health check and lazily on the first write of the process):

1. `store.Options.Serializer()` is still a `CryptoShredderSerializer`, which catches a later `opts.Serializer(x)` override.
2. For each captured options object, `ReferenceEquals(options.TypeInfoResolver, installed)` holds and `TypeInfoResolverChain` is unchanged (count and order). This catches a later `UseTypeInfoResolver(context)`, a `TypeInfoResolverChain.Insert(0, ...)` or any wholesale replacement. A behavioural canary alone cannot: the internal canary type is never in a user context, so it keeps passing while the context's types are written in clear (probe).
3. A nested canary (`CryptoShreddingCanaryHolder { CryptoShreddingCanary Inner }`) is serialized through each captured options object inside a frame pre-seeded with an ephemeral random key for the canary subject (no provider call, nothing stored). The output must contain a `cs2` token and not the canary plaintext.
4. The startup validator additionally requires, for every closed candidate type, that `ResolveContract(type)` made the modifier register a plan (or report issues) for that type; a type whose contract the modifier never saw fails with `ContractModifierMissing`.

Any failure throws `CryptoShreddingConfigurationException(ContractModifierMissing)` (or `SerializerNotWrapped`) and logs 8468. Dirty-tracked sessions see crypto documents as changed on every save (random nonce). That behaviour already exists today and is documented.

**No ambient scope.** If a Get wrapper or `OnDeserialized` runs without a scope (options used outside the wrapper), it opens an implicit one-shot scope for that value, logs 8473 (Debug) and applies the same fail-closed rules. The behaviour is defined, never a silent pass-through.

---

## Decryption and forgotten subjects

The read sequence is: inner deserialization, then `OnDeserialized` enqueues owners (deduplicated by reference), then `DecryptPending` (sync) or `DecryptPendingAsync(ct)` (async, with the token passed to `GetSubjectKeyAsync` and `HandleForgottenSubjectAsync`). The sync path is sync-over-async with no token, because Marten's `ISerializer.FromJson` is synchronous; the existing `SuppressMessage` justification is kept.

| Stored value | Outcome | Log | Counter |
|--------------|---------|-----|---------|
| `null` | stays `null`, no key lookup | — | — |
| v2 token, key found, tag valid | plaintext set through the compiled setter, then read-back check (getter still returns the token → `CryptoShreddingDecryptionException(PropertyNotWritable)`) | 8451 (Debug) | `crypto.decryption.total` |
| v2 token, provider `Left(crypto.subject_forgotten)` | `AnonymizedPlaceholder`; `IForgottenSubjectHandler` once per subject per call | 8454 | `crypto.forgotten_access.total` |
| tombstone `cs2:erased`, subject id present, provider confirms the subject is forgotten (`IsSubjectForgottenAsync`, once per subject per call, cached in the frame) | `AnonymizedPlaceholder`, no key lookup; handler once per subject per call | 8454 | `crypto.forgotten_access.total` |
| tombstone on a subject the provider reports as **not** forgotten | throws `IntegrityCheckFailed` (`crypto.integrity_check_failed`) | 8456 | `crypto.decryption.failed{crypto.failure_reason}` |
| tombstone, provider error while checking | throws `KeyUnavailable` | 8456 + `ForLogging()` | same |
| any other `Left` (key not found, key store error) or provider exception | throws `CryptoShreddingDecryptionException(KeyUnavailable, errorCode)` | 8456 + `ForLogging()` | same |
| key returned but unusable (version < 1 or length ≠ 32; `AesGcm` would silently accept 16- or 24-byte keys) | throws `KeyUnavailable` | 8456 | same |
| tag or AAD mismatch (`AuthenticationTagMismatchException` only; a plain `CryptographicException` from `AesGcm` cannot happen after the length check and, if it does, maps to `KeyUnavailable`) | throws `IntegrityCheckFailed` (`crypto.integrity_check_failed`) | 8456 | same |
| non-null value that is not a v2 token | throws `EnvelopeMalformed` (`crypto.envelope_malformed`) | 8456 | same |
| subject id missing on the constructed owner (checked before every other branch, including the tombstone) | throws `SubjectIdMissing` | 8466 (Operation=Decrypt) | same |

Exceptions thrown by the forgotten handler are logged at 8472 with `ForLogging()` and swallowed: the handler is a notification hook and must not break reads. The placeholder stays a single string, because only strings can be shredded.

**Write side for a forgotten subject.** New personal data throws `CryptoShreddingEncryptionException(KeyUnavailable, crypto.subject_forgotten)`. A value equal to the placeholder is written as the tombstone (8471).

**Key rotation.** The token carries the key version. Providers keep rotated keys, so `vN` and `vN+1` tokens decrypt in the same call.

**Doc fix.** The text of `DefaultForgottenSubjectHandler` ("returned as null") is corrected to say "the placeholder", and its unnumbered `LogInformation` becomes 8474 `ForgottenSubjectEncountered`.

**Handler arguments.** `IForgottenSubjectHandler.HandleForgottenSubjectAsync` is called once per subject per call, so its arguments are renamed to say what they carry for nested owners: `eventType` becomes `documentType` (the root type being deserialized: event, snapshot envelope or document) and `propertyName` becomes `fieldPath` (the path, in the locator's `FieldName` format, of the first field of that subject met in the call). Pre-1.0, so the rename is done completely (Public API changes, Changed).

**Restart with the in-memory key store.** A new `InMemorySubjectKeyProvider` reads every earlier token as `KeyUnavailable`; once a new v1 key has been created for that subject, its earlier tokens read as `IntegrityCheckFailed`; neither reads as the placeholder (unit test in Phase 4).

---

## Startup validation

`CryptoShreddingStartupValidationHostedService` replaces `CryptoShreddingAutoRegistrationHostedService`. `CryptoShreddingOptions.AutoRegisterFromAttributes` becomes `ValidateOnStartup` (default `true`), `CryptoShreddingAutoRegistrationDescriptor` becomes `CryptoShreddingValidationDescriptor`, and `AssembliesToScan` is unchanged.

1. If `ValidateOnStartup` is `false`: log 8462 (Warning, now actually called: it is the explicit, logged opt-out of AGENTS §3) and return. The installation check still runs lazily on the first write.
2. Resolve `IDocumentStore`. Its serializer must be a `CryptoShredderSerializer`; otherwise throw `SerializerNotWrapped` (8468).
3. `CryptoShredderSerializer.VerifyContractModifierInstalled()`: resolver identity and chain per captured options object, then the nested canary output (see "Installation check" under "Serializer behaviour"). Otherwise throw `ContractModifierMissing` (8468).
4. Infrastructure checks, each a `CryptoShreddingConfigurationProblem` logged at 8468:
   - `store.Options.Projections.Errors.SkipSerializationErrors` is `true` → `ProjectionSkipsSerializationErrors`;
   - the `IDataErasureStrategy` resolved in a scope is not `CryptoShredRoutingErasureStrategy` → `ErasureStrategyBypassed`;
   - more than one `IPersonalDataLocator` is registered and the resolved one is neither `MartenEventPersonalDataLocator` nor a `CompositePersonalDataLocator` containing it → `PersonalDataLocatorBypassed`;
   - a raw `JsonDocument` upcaster is registered, if Marten 9.38 exposes them (Phase 5 task 6) → `JsonDocumentUpcasterNotSupported`.
   - The resolved `ISubjectKeyProvider` is `InMemorySubjectKeyProvider` → Warning 8481 (not an error; tests and development use it; #1191 owns the durable default).
5. Candidate types come from `GetTypes()` of every scanned assembly (`ReflectionTypeLoadException` is handled: the loaded types are kept and 8469 is logged as a Warning). A candidate is a type that declares or inherits an attributed property, or whose reflection member graph reaches such a type. Container types are included so that `ConverterOverCryptoGraph`, `ComputedMemberOverCryptoGraph` and `SourceGeneratedContract` can be detected on them.
6. Each candidate is checked:
   - **Open generic definition:** reflection rules only, with the deferrals logged at 8470.
   - **Closed type:** `serializer.ResolveContract(type)` asks every captured options object for the contract. For the installed resolver this runs the real modifier; the validator then requires that the modifier registered a plan or issues for that type (otherwise `ContractModifierMissing`, because a resolver ahead of the modifier produced the contract). Then it walks the STJ contract graph (`JsonPropertyInfo.PropertyType`, `ElementType`, `KeyType`, `PolymorphismOptions.DerivedTypes`) with a `HashSet<Type>` visited set. Each `CryptoShreddingConfigurationException` is caught and its issues collected. This also validates closed generics such as `E<Guid>` used as property types, and types from assemblies that are not scanned.
   - The contract walk follows declared types only. `ImplementsUnattributedMember` is therefore a reflection rule on the implementing or overriding type (found through `GetInterfaceMap` and `GetBaseDefinition`), which the scan reaches because that type declares an attributed property.
7. Issues are deduplicated by (declaring type, property). Each issue is reported once, on its declaring type, not once per subclass.
8. Warning: 8475 (subject-id property is `[PersonalData]`).
9. If any issue exists: log 8459 per issue (`{DeclaringType}.{PropertyName}: {Problems}`) and 8467 (summary), then throw one `CryptoShreddingConfigurationException(MisconfiguredProperties, issues)` so the host does not start. The unnumbered `LogError("{ValidationError}")` is deleted.
10. On success: 8461 (`TypeCount` = types with crypto-shredded properties).

Types outside the scanned assemblies are validated by the modifier on first use, and the append fails closed. A nested owner reached only through a source-generated container is caught on the container (`SourceGeneratedContract`), because the modifier runs for the container even when the generated fast path never resolves the owner.

---

## Error codes and EventIds

**Error codes** (`CryptoShreddingErrors`):

- Existing codes are kept: `crypto.subject_forgotten`, `crypto.encryption_failed`, `crypto.decryption_failed`, `crypto.key_rotation_failed`, `crypto.key_store_error`, `crypto.invalid_subject_id`, `crypto.key_already_exists`, `crypto.serialization_error`, `crypto.attribute_misconfigured`.
- New codes: `crypto.envelope_malformed`, `crypto.integrity_check_failed`, `crypto.serializer_unsupported`, `crypto.erasure_strategy_missing` (a non-Marten location reached the routing strategy with no inner strategy registered).
- `AttributeMisconfigured(propertyName, declaringType, reason)` is now called by the validator, with `reason` = the flag names (never a message).

**Range**: `EventIdRanges.MartenGDPRCryptoShredding = (8450, 8499)`, already registered in `src/Encina/Diagnostics/EventIdRanges.cs:325` and mapped to `Encina.Marten.GDPR` in `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs:90`. Neither file changes.

Used today: 8450-8466, all `LoggerMessage.Define` in `Diagnostics/CryptoShreddingLogMessages.cs`. Of these, 8458 `MetadataCacheBuilt`, 8462 `AutoRegistrationSkipped`, 8464 `KeyRotationScheduled` and 8465 `ReEncryptionStarted` are defined but never called (verified by grep).

| EventId | Name | Level | Change |
|---------|------|-------|--------|
| 8450 | `PiiFieldEncrypted` | Debug | template: `DeclaringType`, `PropertyName` (was `PropertyName`, `EventType`; the root type is on the Activity tag) |
| 8451 | `PiiFieldDecrypted` | Debug | template: `DeclaringType`, `PropertyName` |
| 8454 | `ForgottenSubjectAccessed` | Information | template: `DeclaringType`, `PropertyName` |
| 8455 | `EncryptionFailed` | Error | template: `DeclaringType`, `PropertyName`, `Reason`, `ErrorCode` (+ `ForLogging()`) |
| 8456 | `DecryptionFailed` | Error | template: `DeclaringType`, `PropertyName`, `Reason`, `ErrorCode` (+ `ForLogging()`) |
| 8458 | `CryptoContractBuilt` | Debug | **repurposed** (was dead `MetadataCacheBuilt`): `DeclaringType`, `FieldCount` |
| 8459 | `AttributeMisconfigured` | Error | template: `DeclaringType`, `PropertyName`, `Problems`; once per issue |
| 8460 | `SerializerWrapped` | Information | now "contract modifier installed": `OptionsCount` |
| 8461 | `AutoRegistrationCompleted` | Information | renamed `StartupValidationCompleted`: `TypeCount` |
| 8462 | `AutoRegistrationSkipped` | Debug | renamed `StartupValidationSkipped`, now **called**, level raised to **Warning** (it records the explicit opt-out) |
| 8463 | `HealthCheckCompleted` | Debug | template: `Status`, `CryptoContractCount`, `MisconfiguredTypeCount` (was `CachedTypeCount`; the metadata cache is deleted) |
| 8466 | `EncryptionSubjectIdMissing` | Error | renamed `SubjectIdMissing`: `Operation`, `DeclaringType`, `PropertyName`, `SubjectIdProperty` |
| **8467** | `StartupValidationFailed` | Error | new: `IssueCount`, `TypeCount` |
| **8468** | `CryptoShreddingInfrastructureInvalid` | Critical | new: `Problem` (any `CryptoShreddingConfigurationProblem` except `MisconfiguredProperties`), `ComponentType` (serializer, strategy or locator type name) |
| **8469** | `StartupTypeLoadPartial` | Warning | new: `AssemblyName`, `LoaderExceptionCount` (replaces unnumbered LogWarning in the hosted service) |
| **8470** | `OpenGenericCheckDeferred` | Debug | new: `TypeName`, `PropertyName` |
| **8471** | `ForgottenSubjectTombstoneWritten` | Debug | new: `DeclaringType`, `PropertyName` |
| **8472** | `ForgottenSubjectHandlerFailed` | Warning | new (replaces the two unnumbered `LogWarning` calls), exception through `ForLogging()` |
| **8473** | `ImplicitCryptoScopeUsed` | Debug | new: `Operation`, `DeclaringType` |
| **8474** | `ForgottenSubjectEncountered` | Information | new (replaces the unnumbered `LogInformation` in `DefaultForgottenSubjectHandler`): `DocumentType`, `FieldPath`; never the subject id |
| **8475** | `SubjectIdPropertyIsPersonalData` | Warning | new: `DeclaringType`, `SubjectIdProperty` |
| **8476** | `ErasureRequested` | Debug | new (replaces unnumbered LogDebug in `CryptoShredErasureStrategy`): `EntityType`, `FieldName` |
| **8477** | `ErasureSubjectAlreadyForgotten` | Debug | new: `EntityType`, `FieldName` |
| **8478** | `PersonalDataLocateStarted` | Debug | new (replaces locator LogDebug) |
| **8479** | `PersonalDataLocateCompleted` | Debug | new: `Count` |
| **8480** | `PersonalDataLocateFailed` | Error | new (replaces locator LogError), exception through `ForLogging()`; `EventSequence` when the failure is Marten's `EventDeserializationFailureException` (verified in Phase 6); never the stream id, which an application may key by subject |
| **8481** | `InMemoryKeyStoreInUse` | Warning | new: logged once at startup when the resolved `ISubjectKeyProvider` is `InMemorySubjectKeyProvider` |

- **Logging style:** existing messages keep `LoggerMessage.Define` with one literal `new EventId(n, ...)` each (#1125). The new messages 8467-8481 go in a new `internal static partial class CryptoShreddingLog` (`Diagnostics/CryptoShreddingLog.cs`) using `[LoggerMessage]`. Phase 3 creates the class; each phase adds the messages and template changes it calls first (see the phases).
- **What is never logged:** a subject id, a value, a dictionary key, a collection index or an `EncinaError.Message`, nor a Marten stream id (an application may key streams by subject; the event sequence number identifies a failing event instead).
- **Free after this change:** 8482-8499. 8464 and 8465 stay as they are (key-rotation area; their removal or wiring goes to Follow-up issues, item 3).

---

## Consumers

- **`MartenEventPersonalDataLocator`**
  - It takes the serializer from `session.DocumentStore.Options.Serializer()`. If that is not a `CryptoShredderSerializer`, it returns `Left(crypto.serializer_unsupported)`: a DSR request fails closed instead of reporting nothing.
  - It walks each deserialized (already decrypted) event body with `CryptoShreddedGraphWalker`, which iterates over `options.GetTypeInfo(runtimeType)`:
    - Object kind: crypto fields are yielded, and the other properties recurse through the original getters.
    - Enumerable kind: elements.
    - Dictionary kind: values only.
    - The walk uses a `ReferenceEqualityComparer` visited set and stops at the `MaxDepth` bound.
  - It emits one `PersonalDataLocation` per non-null field whose own sibling subject equals the requested subject (ordinal):
    - `EntityType` = root event type, `EntityId` = subject id (unchanged);
    - `FieldName` = path;
    - `Category`, `IsErasable`, `IsPortable` and `HasLegalRetention` come from the cached `[PersonalData]`;
    - `CurrentValue` = the plaintext or the placeholder.
  - **Subject-filtered decryption.** The locator loads every raw event of the store (`MartenEventPersonalDataLocator.cs:66-69`, filtered in memory at `:73-78`), so decrypting everything would let one unreadable event of subject B (missing key, #1699 race, tampered token, in-memory store after a restart) block access, portability and erasure for every other subject. The locator therefore runs its query inside `CryptoShreddingCallScope.FilterToSubject(subjectId)`, an `AsyncLocal` ambient filter that read frames inherit: owners whose sibling subject differs are not decrypted and keep their tokens (the locator never reports them), and no key of another subject is fetched. Each location records its provenance for the routing strategy (Design Choice 9).
  - A read of the requested subject's data that throws (a key-store outage) makes the locator return `Left(crypto.key_store_error)`. It no longer reports a partial inventory. 8480 identifies the failing event by its sequence number when Marten wraps the failure in `EventDeserializationFailureException`.
- **Other readers of the whole event store** (`Encina.Marten`, no code change in this issue): `MartenEventMetadataQuery` (`QueryAllRawEvents` at `MartenEventMetadataQuery.cs:167,203,272,329,348`, any exception mapped to `Left(QueryFailed)` at `:182`, `Data` returned at `:392`) and `MartenProjectionManager` deserialize through the crypto serializer. With fail-closed reads they return `Left` when any returned event cannot be decrypted, where they used to return the placeholder. That is documented, covered by one integration test each, and a follow-up (Follow-up issues, item 4) adds a mode that reads metadata without decrypting.
- **`CryptoShredErasureStrategy`**: idempotent on `crypto.subject_forgotten` (8477). It is reached through the internal `CryptoShredRoutingErasureStrategy` that `AddEncinaMartenGdpr` registers (Design Choice 9): Marten locations go to it, every other location to the strategy that was registered before.
- **`CompositePersonalDataLocator`** (DSR): returns `Left` when any inner locator fails; an internal accessor exposes its inner locators to the startup check.
- **`CryptoShreddingHealthCheck`**
  - Removed: the stale `IFieldEncryptor` prerequisite (the serializer never uses it) and the meaningless "empty cache, so Degraded" rule.
  - Unhealthy when the store serializer is not wrapped, when the installation check fails (resolver identity, chain or nested canary), or when `ISubjectKeyProvider` cannot be resolved in a scope.
  - Data: `keyProviderType`, `cryptoContractCount` (contracts with crypto fields actually built) and `misconfiguredTypeCount` (types rejected at runtime since start).
- **Diagnostics** (`CryptoShreddingDiagnostics`)
  - One `Activity` per serializer call, started lazily on the first crypto field, so documents without PII get none. Tags: `crypto.event_type` (root type `Name`), `crypto.outcome`, and `crypto.failure_reason` (enum name) on failure.
  - `RecordFailed` takes the reason or the exception type name, never `ex.Message`. This fixes the serializer call sites (`CryptoShredderSerializer.cs:329, 649, 689` today). The key providers' call sites go to Follow-up issues, item 3.
  - Counters gain the low-cardinality tag `crypto.failure_reason`.
  - The declared but never-set tag `TagPropertyName` is deleted.
  - No subject id, path index or value appears in any tag.
- **Removed types**:
  - `Events/PiiEncryptionFailedEvent`: never published, and it carries `SubjectId` and `ErrorMessage`, which the rules forbid.
  - `Model/CryptoShreddedFieldMetadata`: produced by nothing.
  - `Metadata/CryptoShreddedPropertyCache`, `Metadata/CryptoShreddedFieldInfo` and `Serialization/EncryptedFieldJsonConverter`: replaced.
  - The uses of `EncryptedValue` and `IFieldEncryptor`. The `Encina.Security.Encryption` project reference **stays**: the key providers return `EncryptionErrors.KeyNotFound` (`InMemorySubjectKeyProvider.cs:151,169,180,301,316`, `PostgreSqlSubjectKeyProvider.cs:158,171,307`) and `CryptoShreddingErrors.cs:1,14` imports the namespace.

---

## Dependencies on open issues

| Issue | State | Relation to #1698 | Handling |
|-------|-------|-------------------|----------|
| [#1699](https://github.com/dlrivada/Encina/issues/1699) `[BUG]` PostgreSqlSubjectKeyProvider can lose the first key when two callers race | Open | `GetOrCreateSubjectKeyAsync` queries, then `_session.Store(doc)` (an upsert) with the fixed id `subject:{id}:v1` and no lock or unique insert (`PostgreSqlSubjectKeyProvider.cs:87-116`); rotation repeats the pattern (`:316-337`). This design opens one DI scope and session per serializer call and makes reads fail closed, so a lost key turns into a permanently unreadable stream. Separately, a writer that passed the forgotten-marker check (`:77-81`) before an erasure committed can create a key after it, and `DeleteSubjectKeysAsync` stops at the marker (`:197-204`), so a repeated, now idempotent, erasure never deletes that key | **Prerequisite**: #1699 lands before the core PR, or in it with its own changelog fragment. Before briefing, the orchestrator extends #1699's acceptance criteria (or opens a linked `[BUG]`, Follow-up issues, item 10) to cover: insert semantics (conflict → reload the winner) for creation and rotation; per-subject serialization of creation and erasure (for example `pg_advisory_xact_lock(hashtext(subjectId))` in one transaction); `DeleteSubjectKeysAsync` deleting remaining keys even when the marker exists; concurrent first-write and write-during-erasure integration tests |
| [#1144](https://github.com/dlrivada/Encina/issues/1144) `[BUG]` Crypto-shredding a single field deletes all keys of the data subject | Open | Phase 6 edits the same `CryptoShredErasureStrategy.EraseFieldAsync` | Idempotency here is independent of #1144; this change keeps subject-wide erasure and documents it (Design Choice 9). The orchestrator sequences #1144 after #1698 (or rebases it) |
| [#1191](https://github.com/dlrivada/Encina/issues/1191) SPEC-002 key management (tenant-scoped keys AC-043, KMS wrapping, durable default store) | Open, P0 | Owns tenant-scoped keys (matrix row 10) and the durable default key store (in-memory restart behaviour, Design Choice 5) | Referenced instead of opening a new tenant-key issue; #1191 will extend the v2 token and AAD (Design Choice 4) |

## Existing test migration

The switchover (Phases 3-7) rewrites or deletes every existing Marten.GDPR test that uses the old constructor, the v1 envelope, the deleted cache or the renamed hosted service, so the solution builds again at the end of Phase 7. New test files are listed in the phases.

| File | Lines | Action | Phase |
|------|-------|--------|-------|
| `UnitTests/Marten/GDPR/CryptoShreddedPropertyCacheTests.cs` | 217 | Delete (cases move to `CryptoShreddedPropertyClassifierTests`) | 3 |
| `UnitTests/Marten/GDPR/EncryptedFieldJsonConverterTests.cs` | 192 | Delete (cases move to `CryptoShreddingTokenTests`) | 3 |
| `UnitTests/Marten/GDPR/CryptoShredderSerializerTests.cs` | 466 | Rewrite against the new constructor and v2 token | 3-4 |
| `UnitTests/Marten/GDPR/CryptoShredderSerializerSubjectIdTests.cs` | 298 | Rewrite (sibling resolution at depth; `TryParse` → `CryptoShreddingToken`) | 3 |
| `UnitTests/Marten/GDPR/CryptoShredderSerializerFailClosedTests.cs` | 527 | Rewrite (Phase 3 task 8) | 3 |
| `UnitTests/Marten/GDPR/CryptoShreddingAutoRegistrationHostedServiceTests.cs` | 435 | Rename and rewrite as `CryptoShreddingStartupValidationHostedServiceTests` | 5 |
| `UnitTests/Marten/GDPR/CryptoShreddingHealthCheckTests.cs` | 91 | Rewrite | 5 |
| `UnitTests/Marten/GDPR/CryptoShreddingDiagnosticsSubjectIdLeakTests.cs` | 582 | Update constructor; extend (Phase 7) | 3, 7 |
| `UnitTests/Marten/GDPR/CryptoShreddingErrorsTests.cs`, `CryptoShreddingErrorsSubjectIdLeakTests.cs` | 260 | Add the new codes | 4 |
| `UnitTests/Marten/GDPR/DefaultForgottenSubjectHandlerTests.cs` | 51 | Update (renamed parameters, 8474) | 4 |
| `UnitTests/Marten/GDPR/CryptoShredErasureStrategyTests.cs` | 127 | Extend (idempotency, routing) | 6 |
| `UnitTests/Marten/GDPR/MartenEventPersonalDataLocatorTests.cs` | 208 | Rewrite (paths, subject filter) | 6 |
| `UnitTests/Marten/GDPR/CryptoShreddedAttributeTests.cs`, `CryptoShreddingOptionsValidatorTests.cs`, `InMemorySubjectKeyProviderTests.cs` | 521 | Keep (options rename only) | 5 |
| `GuardTests/Marten/GDPR/CryptoShredderSerializerGuardTests.cs` | 90 | Rewrite (new constructor) | 3 |
| `GuardTests/Marten/GDPR/CryptoShreddingEncryptionExceptionGuardTests.cs` | 53 | Rewrite (new constructor) | 3 |
| `GuardTests/Marten/GDPR/CryptoShreddingHappyPathGuardTests.cs` | 465 | Remove deleted types (`:235-237`, `:325-327`), new constructor | 3, 6 |
| `GuardTests/Marten/GDPR/CryptoShredErasureStrategyGuardTests.cs`, `InMemorySubjectKeyProviderGuardTests.cs` | 232 | Keep | — |
| `ContractTests/Marten/GDPR/IDataErasureStrategyContractTests.cs` | 188 | Update (`:127`, idempotency) | 6 |
| `ContractTests/Marten/GDPR/ISubjectKeyProviderContractTests.cs` | 242 | Keep | — |
| `PropertyTests/Marten/GDPR/CryptoShredderSerializerPropertyTests.cs` | 275 | Rewrite on the real inner serializer | 3-4 |
| `PropertyTests/Marten/GDPR/CryptoShreddingPropertyTests.cs` | 336 | Rewrite (v2 token invariants; `:254-304` use the deleted converter) | 3 |
| `PropertyTests/Marten/GDPR/CryptoShreddingErrorsPropertyTests.cs` | 100 | Add the new codes | 4 |
| `PropertyTests/Marten/GDPR/CryptoShreddingOptionsValidatorPropertyTests.cs` | 197 | Keep (options rename only) | 5 |
| `IntegrationTests/Infrastructure/Marten/GDPR/CryptoShredderSerializerIntegrationTests.cs` | 381 | Update (token assertions, cache removal `:42`) | 3-4 |
| `IntegrationTests/Infrastructure/Marten/GDPR/ForgetSubjectIntegrationTests.cs` | 211 | Update (cache removal `:37`, placeholder semantics) | 4 |
| `IntegrationTests/Infrastructure/Marten/GDPR/DSRIntegrationTests.cs` | 166 | Update (cache removal `:37`, paths, routing) | 6 |
| `IntegrationTests/Infrastructure/Marten/GDPR/PostgreSqlSubjectKeyProviderIntegrationTests.cs` | 272 | Keep (#1699 extends it) | — |
| `BenchmarkTests/Encina.Marten.GDPR.Benchmarks/CryptoShredderSerializerBenchmarks.cs` | 107 | Update constructor (`:34`, `:64`); extended in Phase 8 | 3, 8 |
| `LoadTests/Marten/GDPR/CryptoShredding.md` | 52 | Replace by a concurrency load test (Phase 8 task 6) | 8 |

## Implementation Phases

**Build rule.** Phases 1 and 2 are purely additive (new internal types and their tests, no deletion, no signature change) and each leaves the solution building with zero warnings. Phases 3-7 are one **switchover**: the serializer, factory, configurator, startup validator, health check, locator and erasure change together, and every existing caller in `src/` and in the five test projects and the benchmark project is migrated (see "Existing test migration"). The solution must build with zero warnings and all tests pass at the end of Phase 7; inside the switchover, a phase is checkpointed by its own new unit tests, not by a full build. Old types are deleted only in the switchover close (Phase 7, task 6). Measure the local CRAP table for Phases 1-2 at their end and for the switchover at the end of Phase 7. Do not push until Phase 9 is complete and every flag reaches its target.

### Phase 1: Classifier and reason model

<details>
<summary>Tasks</summary>

1. `src/Encina.Marten.GDPR/CryptoShreddedPropertyProblems.cs`: public `[Flags] enum CryptoShreddedPropertyProblems` with `None = 0` plus the members:
   - `NotString`, `MissingPersonalData`, `NotReadable`, `Indexer`, `NoSetter`;
   - `DeclaredOnValueType`, `DeclaredOnInterface`, `AttributeOnlyOnInterface`, `ImplementsUnattributedMember`, `HiddenByDerivedProperty`;
   - `BoundToConstructorParameter`, `OwnerImplementsOnDeserialized`, `OwnerInHashedCollection`;
   - `OwnerNotSerializedAsObject`, `ConverterOverCryptoGraph`, `SourceGeneratedContract`, `NotSerialized`, `NotDeserializable`, `CustomConverterOnProperty`, `ComputedMemberBesideCryptoShredded`, `ComputedMemberOverCryptoGraph`, `DictionaryKeyCarriesCryptoShredded`;
   - `SubjectIdPropertyNotFound`, `SubjectIdPropertyNotReadable`, `SubjectIdTypeUnsupported`, `SubjectIdPropertyNotRoundTripped`, `SubjectIdPropertyIsCryptoShredded`;
   - `DuplicatedByMarten`, kept only if Phase 5 confirms the Marten API.
2. `src/Encina.Marten.GDPR/CryptoShreddedPropertyIssue.cs`: public `sealed record CryptoShreddedPropertyIssue(string DeclaringTypeName, string PropertyName, CryptoShreddedPropertyProblems Problems)` with `string Describe()`. It returns one actionable sentence per set flag through `DescribeFlag(CryptoShreddedPropertyProblems)`, marked `// crap-exempt: single-question switch — problem flag to explanation`.
3. `src/Encina.Marten.GDPR/Metadata/CryptoShreddedTypeShape.cs`: internal `sealed record CryptoShreddedTypeShape(Type Type, ImmutableArray<CryptoShreddedField> Fields, ImmutableArray<CryptoShreddedPropertyIssue> Issues, bool ReachesCryptoOwner)`.
4. `src/Encina.Marten.GDPR/Metadata/CryptoShreddedField.cs`: internal sealed class with:
   - `PropertyInfo Property`, `Type DeclaringType`, `string Name`;
   - `Func<object, object?> Getter`, `Action<object, object?> Setter` (compiled on `Property.DeclaringType`, so private, `init` and base-declared accessors work);
   - `PropertyInfo SubjectIdProperty`, `Func<object, object?> SubjectIdGetter`;
   - `PersonalDataAttribute PersonalData`;
   - `string? ResolveSubjectId(object owner)`.
5. `src/Encina.Marten.GDPR/Metadata/CryptoShreddedPropertyClassifier.cs`: internal static.
   - `GetShape(Type)`: static `ConcurrentDictionary` cache, reflection rules only. The hierarchy walk uses `DeclaredOnly|Public|NonPublic|Instance`, deduplicates overrides through `GetBaseDefinition()`, detects interface maps and `new` hiding, and resolves the subject id from the most-derived declaration.
     - `ImplementsUnattributedMember`: an attributed property whose getter implements an interface member (`GetInterfaceMap`) or overrides a base declaration (`GetBaseDefinition() != getter`) that lacks the attribute.
     - `OwnerImplementsOnDeserialized`: the owner type implements `IJsonOnDeserialized` (`IJsonOnDeserializing` runs before population and is allowed).
     - `BoundToConstructorParameter`: a reflection pre-check marks attributed properties that a constructor parameter can bind to (name match, ordinal-ignore-case); the contract rule decides (below).
   - `ClassifyContract(JsonTypeInfo, CryptoShreddedTypeShape)`: the contract rules.
     - `SourceGeneratedContract`: `ti.OriginatingResolver is JsonSerializerContext` for an owner or a container that `ReachesCryptoOwner`, or an owner contract missing an attributed property that the shape says is serialized.
     - `BoundToConstructorParameter`: `jp.AssociatedParameter` (or the `ti.CreateObject`/constructor parameter list) binds the property, and the constructor is not the compiler-synthesized primary constructor of a positional record. Synthesized means: the type is a record (`<Clone>$` exists), and the constructor IL only stores each parameter unchanged into the matching `<Name>k__BackingField` before calling the base constructor. Phase 1 confirms this IL shape with a probe in Release on .NET 10; if it cannot be distinguished reliably, the rule rejects every constructor-bound `[CryptoShredded]` property and the docs tell users to declare `init` properties.
     - `OwnerInHashedCollection`: a container contract of kind `Enumerable` whose type is or implements `ISet<T>`/`IReadOnlySet<T>` (covers `HashSet<T>`, `SortedSet<T>`, `ImmutableHashSet<T>`, `ImmutableSortedSet<T>`, `FrozenSet<T>`) and whose element type is an owner with value equality (record, an `Equals(object)` or `GetHashCode` override below `object`, or `IComparable`/`IComparable<T>`). Reported on the declaring type of the member typed as the set.
     - `ComputedMemberOverCryptoGraph`: a serialized member with no setter and no constructor parameter on a type whose graph reaches an owner (the owner itself keeps `ComputedMemberBesideCryptoShredded`).
   - `ReachesCryptoOwner(Type)`: a reflection graph over property types, generic arguments and array elements, with an in-progress set to stop at cycles.
   - `internal static void ResetForTests()`.
   - Each rule is a `static CryptoShreddedPropertyProblems RuleX(in PropertyContext)` in a `static readonly` rule array, combined with `|`, so every problem is reported and each method stays at CRAP ≤ 10.
   - Open generics: the subject-type, setter and contract rules are deferred and recorded in `CryptoShreddedTypeShape.DeferredChecks`.
6. No deletion in this phase: `Metadata/CryptoShreddedPropertyCache.cs` and `Metadata/CryptoShreddedFieldInfo.cs` are deleted in the switchover close (Phase 7, task 6).
7. Unit tests: `CryptoShreddedPropertyClassifierTests` (one test per flag, several flags on one property, override dedup, private base property, `new` hiding with a different type resolves without `AmbiguousMatchException`, interface-only, struct, open generic deferral, `E<Guid>` passes and `E<object>` fails; an `IContact`-typed member, a `List<IContact>` member and a `BaseContact` member whose override alone is attributed are rejected as `ImplementsUnattributedMember`; a positional record passes while a record with `= Email.Trim()` initializer, a `[JsonConstructor]` that calls `ToLowerInvariant()` and a validating constructor are rejected as `BoundToConstructorParameter`; an `IJsonOnDeserialized` owner is rejected; `HashSet<RecordOwner>`, `SortedSet<ComparableOwner>` and `ImmutableHashSet<RecordOwner>` are rejected while `HashSet<PlainClassOwner>` passes; a `GenerationMode=Serialization` context, root and nested, is rejected as `SourceGeneratedContract`; a computed `Summary => Contact.Email` on the event is rejected as `ComputedMemberOverCryptoGraph`), `CryptoShreddedPropertyIssueTests` (`Describe` covers every flag).

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT: Encina (.NET 10, C# 14, pre-1.0, breaking changes welcome). Package src/Encina.Marten.GDPR implements crypto-shredding of [CryptoShredded] string properties in Marten events. Issue #1698: nested, collection, non-public properties are stored in plaintext. Plan: docs/plans/crypto-shredding-nested-implementation-plan-1698.md (read "Shape coverage" and "Subject resolution rule").
TASK: Phase 1 — build the single classifier that every consumer will use. Create CryptoShreddedPropertyProblems ([Flags], public), CryptoShreddedPropertyIssue (public record with Describe()), CryptoShreddedField, CryptoShreddedTypeShape and CryptoShreddedPropertyClassifier (internal) under the paths in the plan. Reflection rules in GetShape(Type); contract rules in ClassifyContract(JsonTypeInfo, shape); rules as small static functions in a rule array.
KEY RULES: this phase is purely additive (no deletion, no signature change; the solution builds at its end); walk BaseType with DeclaredOnly|Public|NonPublic|Instance; dedupe overrides via GetBaseDefinition(); reject an attributed property that implements or overrides an unattributed declaration (GetInterfaceMap/GetBaseDefinition); contract rules also cover source-generated contracts (OriginatingResolver is JsonSerializerContext), constructor binding outside synthesized positional-record constructors (probe the IL shape first), IJsonOnDeserialized owners, value-equality owners in ISet/IReadOnlySet containers, and computed members on any type whose graph reaches an owner; compile setters on Property.DeclaringType (Expression trees; init and private setters work; getter-only fails); subject-id sibling = most-derived declaration; SubjectIdConversion.IsSupportedType on the declared type, deferred when ContainsGenericParameters; never put values or subject ids in issues; CRAP <= 10 per method (single-question switch may carry the crap-exempt marker); XML docs on public types; PublicAPI.Unshipped.txt lines; tests use Shouldly via Encina.Testing.Shouldly; PowerShell only.
REFERENCE FILES: src/Encina.Marten.GDPR/Metadata/CryptoShreddedPropertyCache.cs (rules to replace), src/Encina.Marten.GDPR/CryptoShreddingAutoRegistrationHostedService.cs:136-240 (reason texts), src/Encina.Compliance.DataSubjectRights/SubjectIdConversion.cs, tests/Encina.UnitTests/Marten/GDPR/CryptoShreddingAutoRegistrationHostedServiceTests.cs (fixture style).
```

</details>

### Phase 2: Cipher and v2 token

<details>
<summary>Tasks</summary>

1. `src/Encina.Marten.GDPR/Serialization/CryptoShreddingToken.cs`: internal `readonly struct`.
   - Members: `static string Format(int version, ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> ciphertext, ReadOnlySpan<byte> tag)`, `static bool TryParse(string value, out CryptoShreddingToken token)`, `static bool IsTombstone(string value)`, `const string Tombstone = "cs2:erased"`.
   - Strict format `cs2:{version>=1}:{base64url}` with payload ≥ 28 bytes. Uses `System.Buffers.Text.Base64Url` and `ArrayPool<byte>`.
2. `src/Encina.Marten.GDPR/Serialization/CryptoShreddingFieldCipher.cs`: internal sealed. Members: `string Encrypt(AesGcm aes, string subjectId, int version, string plaintext)` and `string Decrypt(AesGcm aes, string subjectId, in CryptoShreddingToken token)`. AES-256-GCM with a random 12-byte nonce and a 16-byte tag; AAD = UTF-8 `encina:cs2:{subjectId}:{version}`.
3. No deletion in this phase: `Serialization/EncryptedFieldJsonConverter.cs` is deleted in the switchover close (Phase 7, task 6), because the old serializer and property tests still call it until then.
4. Unit tests:
   - `CryptoShreddingTokenTests`: format/parse round trip; rejects a bad prefix, version 0, a negative version, bad base64 and a short payload; recognises the tombstone.
   - `CryptoShreddingFieldCipherTests`: round trip; a swapped subject or version fails with `AuthenticationTagMismatchException`; a fresh nonce per call; 16-, 24- and 31-byte keys are rejected before `AesGcm` is built.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT: Encina.Marten.GDPR, issue #1698, plan docs/plans/crypto-shredding-nested-implementation-plan-1698.md (Design Choice 4). The v1 envelope (JSON object inside a JSON string, kid with the subject id, StartsWith detection) is replaced; pre-1.0, no v1 reader.
TASK: Phase 2 — CryptoShreddingToken (strict cs2 token + tombstone) and CryptoShreddingFieldCipher (AES-256-GCM, AAD binds subject and version). Purely additive: EncryptedFieldJsonConverter is deleted later, in the switchover close.
KEY RULES: no subject id in the token; the cipher accepts only 32-byte keys (AesGcm silently accepts 16/24) and maps only AuthenticationTagMismatchException to an integrity failure; strict parsing (no prefix guessing); pooled buffers, no MemoryStream/JsonDocument; never log or throw with key material, plaintext or subject id; CRAP <= 10; tests Shouldly, deterministic (inject nonces through an internal overload for tests if needed).
REFERENCE FILES: src/Encina.Marten.GDPR/Serialization/CryptoShredderSerializer.cs:426-454 and 791-803 (current AES code), src/Encina.Marten.GDPR/Serialization/EncryptedFieldJsonConverter.cs.
```

</details>

### Phase 3: Contract modifier, scopes and serializer write path

<details>
<summary>Tasks</summary>

0. Diagnostics foundation (first, because the rest of the switchover calls it): create `Diagnostics/CryptoShreddingLog.cs` (`internal static partial class`, XML doc `Event IDs: 8467-8481 (see EventIdRanges.MartenGDPRCryptoShredding)`) with the messages this phase uses (8467, 8468, 8471, 8473); add the counter `crypto.configuration.misconfigured.total` and `RecordFailed(Activity?, string reason)` to `CryptoShreddingDiagnostics`; change the templates of 8450, 8455, 8458 and 8459 that this phase calls. Phases 4-6 add the messages and template changes they call first in the same way; Phase 7 only finalizes.
1. `src/Encina.Marten.GDPR/Serialization/CryptoShreddingCallScope.cs`: internal.
   - A frame stack, so re-entrant `SaveChanges` of the key store works. Write frames are `[ThreadStatic]`; read frames are `AsyncLocal`.
   - Each frame holds:
     - the root type;
     - a lazily created DI scope (`IServiceScopeFactory.CreateScope()` on the sync path, `CreateAsyncScope()` on the async path) that resolves `ISubjectKeyProvider` and `IForgottenSubjectHandler`;
     - the write key cache `Dictionary<string, (int Version, AesGcm Aes)>` and the read key cache `Dictionary<(string, int), DecryptKeyEntry>` (an `AesGcm` or the forgotten/failed outcome): the `AesGcm` is built as soon as the provider returns, after the 32-byte and version checks, and the provider's `SubjectEncryptionKey`/`byte[]` is never kept (Design Choice 6, key ownership);
     - the forgotten-subject confirmations for tombstones (`Dictionary<string, bool>`);
     - an optional subject filter inherited from `CryptoShreddingCallScope.FilterToSubject(subjectId)` (used by the locator, Phase 6);
     - an optional pre-seeded canary key (installation check only);
     - the pending owners (`HashSet<object>(ReferenceEqualityComparer.Instance)` plus an ordered list);
     - the set of forgotten subjects already notified;
     - a lazy `Activity`.
   - `Dispose`/`DisposeAsync` zeroes frame-owned buffers only (never an array returned by a provider), disposes the `AesGcm` instances and the DI scope, and records the duration.
2. `src/Encina.Marten.GDPR/Serialization/CryptoShreddingContractModifier.cs`: internal sealed, `void Modify(JsonTypeInfo ti)`.
   - Get the shape, then the contract issues.
   - If there are issues: log 8459 per issue, increment the misconfigured counter, and throw `CryptoShreddingConfigurationException(MisconfiguredProperties, issues)`. STJ caches the throw.
   - Otherwise: wrap `jp.Get` of each crypto field (`owner => engine.EncryptForWrite(owner, field, (string?)prev(owner))`), chain `ti.OnDeserialized`, register the plan, and log 8458.
   - Record every type it was called for (with or without crypto fields) in a per-modifier `ConcurrentDictionary<Type, byte>`, so the startup validator can require that the modifier saw each closed candidate type.
   - The rules are evaluated for every `Kind`, so `OwnerNotSerializedAsObject` and `ConverterOverCryptoGraph` are raised for `Kind=None` and `Kind=Enumerable` types.
3. `src/Encina.Marten.GDPR/Serialization/CryptoShreddingEngine.cs`: internal sealed, `string? EncryptForWrite(object owner, CryptoShreddedField field, string? value)`.
   - A null value returns null.
   - Subject: missing → 8466 + `SubjectIdMissing`; invalid runtime type → `SubjectIdInvalid`.
   - Key: from the frame cache, filled by `GetOrCreateSubjectKeyAsync` (the `AesGcm` is built immediately; the provider's array is not kept). A forgotten subject with the placeholder value returns the tombstone (8471). Any other `Left`, an exception or an unusable key (version < 1 or length ≠ 32) → 8455 + `KeyUnavailable`.
   - With no frame: an implicit one-shot frame (8473).
4. Rewrite `src/Encina.Marten.GDPR/Serialization/CryptoShredderSerializer.cs` (public sealed, implements `ISerializer`).
   - Constructor: `(SystemTextJsonSerializer inner, IServiceScopeFactory scopeFactory, ILogger<CryptoShredderSerializer> logger, string anonymizedPlaceholder = "[REDACTED]")`. It installs the modifier through `inner.Configure(...)` and captures the options.
   - Write members: push a frame, call `inner`, and pop the frame in `finally`.
   - `WriteTo*`: stage when `ReachesCryptoOwner(rootType)` is true.
   - Internal `JsonTypeInfo ResolveContract(Type)` (asks every captured options object and reports whether the modifier saw the type) and `void VerifyContractModifierInstalled()`: resolver identity and `TypeInfoResolverChain` snapshot per captured options object, then the nested canary serialized through each options object in a frame pre-seeded with an ephemeral key; the output must hold a `cs2` token and not the canary plaintext. Runs once lazily on the first write of the process as well as at startup and in the health check.
   - The `HasCryptoShreddedProperties` gate and the mutate/restore code are deleted.
5. `src/Encina.Marten.GDPR/Metadata/CryptoShreddingCanary.cs`: internal sealed classes `CryptoShreddingCanary { string SubjectId; [PersonalData][CryptoShredded] string? Value }` and `CryptoShreddingCanaryHolder { CryptoShreddingCanary Inner }`.
6. Rewrite `src/Encina.Marten.GDPR/CryptoShreddingEncryptionException.cs`.
   - Constructor: `(Type documentType, Type declaringType, string propertyName, CryptoShreddingEncryptionFailureReason reason, string? errorCode = null)`.
   - `CryptoShreddingEncryptionFailureReason` becomes `{ SubjectIdMissing, SubjectIdInvalid, KeyUnavailable }`.
   - Properties: `DocumentTypeName` (`"unknown"` outside a frame), `DeclaringTypeName`, `PropertyName`, `Reason`, `ErrorCode`.
7. New `src/Encina.Marten.GDPR/CryptoShreddingConfigurationException.cs` with `CryptoShreddingConfigurationProblem { MisconfiguredProperties, SerializerNotSupported, SerializerNotWrapped, ContractModifierMissing }` and `IReadOnlyList<CryptoShreddedPropertyIssue> Issues`.
8. Unit tests (real `SystemTextJsonSerializer` from `new StoreOptions().Serializer()`, then `Apply`; NSubstitute or `InMemorySubjectKeyProvider` over `FakeTimeProvider`; `FakeLogger`):
   - `CryptoShreddingContractModifierTests`;
   - `CryptoShredderSerializerNestedEncryptTests`: a `[Theory]` over all 8 write members; nested, list, array, `ImmutableArray`, `IReadOnlyList`, dictionary values (keys untouched), `object` member, `[JsonDerivedType]`, positional record, `init`, private `[JsonInclude]`, `Gen<Guid>`, a different subject per level, a shared instance twice, null nested, null element, null value with no key lookup; the raw JSON contains no plaintext and the caller's graph is unchanged (deep snapshot);
   - `CryptoShredderSerializerFailClosedTests` (rewritten): each reason at depth 0 and depth 2 and inside a list element; `WriteTo` leaves `WrittenCount == 0` on failure; `WriteToParameter` leaves `Value` null; logs contain no subject id or sentinel message;
   - `CryptoShreddingCallScopeTests`: re-entrancy, AsyncLocal flow across awaits, lazy DI scope, implicit scope; key ownership with the real `InMemorySubjectKeyProvider`: two consecutive serializer calls for one subject both round-trip and the provider's stored key bytes are unchanged after the first call ends;
   - `CryptoShredderSerializerInstallationTests`: `Apply` then `UseTypeInfoResolver(context)` (a `[JsonSerializable(typeof(Owner))]` context) fails the installation check while a type-only canary would pass; `UseTypeInfoResolver(context)` before `Apply` with a default-mode context encrypts; a `TypeInfoResolverChain.Insert(0, ...)` after `Apply` fails; the nested canary output holds a `cs2` token.
9. Migrate the existing tests assigned to Phase 3 in "Existing test migration" (constructor, v1 envelope, converter and cache uses).

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT: Encina.Marten.GDPR, issue #1698, plan docs/plans/crypto-shredding-nested-implementation-plan-1698.md (Design Choices 1, 6, 7; "Serializer behaviour"). Phases 1-2 delivered the classifier and the cipher. Probes (throwaway probes during the plan review, 2026-10-03; not kept in the repository) verified: SystemTextJsonSerializer.Configure runs once per each of its 4 internal JsonSerializerOptions; TypeInfoResolver is null on all; exceptions thrown from JsonPropertyInfo.Get propagate unwrapped; an exception thrown from a modifier is cached by STJ for the type; WriteTo leaves partial bytes in the caller's buffer; WriteToParameter leaves Value null; a type with [JsonConverter] has Kind=None and is written in plaintext by a naive modifier.
TASK: Phase 3 — first phase of the switchover (Phases 3-7 build green only at the end of Phase 7; see "Build rule"). Diagnostics foundation, contract modifier (Get-wrapping encryption), call-scope frames, engine write path, CryptoShredderSerializer rewrite, installation check (resolver identity + chain + nested canary), encryption and configuration exceptions, and the Phase 3 rows of "Existing test migration".
KEY RULES: never mutate the caller's object; fail closed before any byte reaches the caller (staging buffer for WriteTo*); per-call key cache only, never across calls (Art. 17); keys through IServiceScopeFactory in a lazy DI scope; build AesGcm as soon as a key arrives and never keep or zero a provider-owned array (providers return their stored arrays); zero only frame-owned buffers at scope end; Marten's UseTypeInfoResolver puts a context AHEAD of the modifier, so the installation check compares resolver identity, not just canary behaviour; exception messages and logs carry type/property names, flags and error codes only (never subject id, value, EncinaError.Message, inner exception message; no inner exception attached); exceptions passed to loggers via ForLogging(); existing LoggerMessage.Define messages keep one literal EventId; new ones use [LoggerMessage] in CryptoShreddingLog inside 8467-8481 as listed in the plan, created in this phase; CRAP <= 10; PublicAPI.Unshipped.txt updated; Shouldly; PowerShell only.
REFERENCE FILES: src/Encina.Marten.GDPR/Serialization/CryptoShredderSerializer.cs (current), src/Encina.Marten.GDPR/Serialization/CryptoShredderSerializerFactory.cs, src/Encina.Marten.GDPR/CryptoShreddingEncryptionException.cs, tests/Encina.UnitTests/Marten/GDPR/CryptoShredderSerializerFailClosedTests.cs (Invoke(entryPoint) theory and leak sentinels).
```

</details>

### Phase 4: Read path, forgotten subjects and tombstone

<details>
<summary>Tasks</summary>

1. `CryptoShreddingEngine.OnOwnerDeserialized(object owner, CryptoShreddingTypePlan plan)`: enqueue into the read frame, or decrypt inline in an implicit frame (8473). When the frame has a subject filter, an owner whose sibling subject differs is not enqueued (it keeps its tokens).
2. `CryptoShreddingEngine.DecryptPending(frame)` / `DecryptPendingAsync(frame, CancellationToken)`, split into small helpers so each stays at CRAP ≤ 10:
   - subject id first (missing → `SubjectIdMissing`), then group by (subject, version);
   - `FetchDecryptKey` / `FetchDecryptKeyAsync` (32-byte and version check on read too; a bad key → `KeyUnavailable`);
   - `ConfirmForgotten` / `ConfirmForgottenAsync` for tombstones (`IsSubjectForgottenAsync`, cached per subject; not forgotten → `IntegrityCheckFailed`; provider error → `KeyUnavailable`);
   - `ApplyDecryptOutcome(DecryptOutcome)` with `enum DecryptOutcome { Decrypted, Forgotten, Tombstone }`, failures thrown;
   - `NotifyForgottenOnce`;
   - a read-back check after the setter (`PropertyNotWritable`).
3. New `src/Encina.Marten.GDPR/CryptoShreddingDecryptionException.cs`: `DocumentTypeName`, `DeclaringTypeName`, `PropertyName`, `Reason`, `ErrorCode`, with `CryptoShreddingDecryptionFailureReason { SubjectIdMissing, SubjectIdInvalid, KeyUnavailable, IntegrityCheckFailed, EnvelopeMalformed, PropertyNotWritable }`.
4. All 4 `FromJson` and 4 `FromJsonAsync` overloads: push a read frame, deserialize, complete the decryptions (async overloads await with the token), and pop the frame in `finally`.
5. `CryptoShreddingErrors`: add `EnvelopeMalformedCode`, `IntegrityCheckFailedCode` and `SerializerUnsupportedCode` with their factories.
6. `DefaultForgottenSubjectHandler`: correct the XML docs and log text to say "placeholder"; its log becomes 8474 `ForgottenSubjectEncountered`. `IForgottenSubjectHandler.HandleForgottenSubjectAsync` parameters are renamed `documentType` (root type) and `fieldPath` (first field of that subject in the call); add 8472 and 8474 to `CryptoShreddingLog` and change the templates of 8451, 8454 and 8456.
7. Unit tests in `CryptoShredderSerializerNestedDecryptTests` (sync and async parity):
   - forgotten → placeholder at every level, handler called once per subject with the root `documentType` and a path;
   - tombstone on a forgotten subject → placeholder; tombstone on a live subject → `IntegrityCheckFailed`; tombstone with a missing subject id → `SubjectIdMissing`; tombstone with a provider error → `KeyUnavailable`; one `IsSubjectForgottenAsync` per subject per call;
   - a 16-byte and a 31-byte key returned on read → `KeyUnavailable`, not `IntegrityCheckFailed`;
   - in-memory restart: a token written with one `InMemorySubjectKeyProvider` instance and read with a new one → `KeyUnavailable`; after a new write for that subject → `IntegrityCheckFailed`; never the placeholder;
   - a subject filter leaves other subjects' owners untouched and fetches none of their keys;
   - transient `Left` → `KeyUnavailable`;
   - provider exception → `KeyUnavailable`, logged through `ForLogging`;
   - tag mismatch → `IntegrityCheckFailed`;
   - malformed token → `EnvelopeMalformed`;
   - subject missing on read;
   - one `GetSubjectKeyAsync` per (subject, version) per call;
   - `v1` and `v2` key versions in one call;
   - the async path passes the token (`Received` with token);
   - a plaintext equal to the placeholder is counted as decrypted;
   - a forgotten subject's placeholder written again → tombstone (8471), while any other value → `KeyUnavailable`.
8. Migrate the existing tests assigned to Phase 4 in "Existing test migration".

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT: Encina.Marten.GDPR, issue #1698, plan docs/plans/crypto-shredding-nested-implementation-plan-1698.md (Design Choice 5, "Decryption and forgotten subjects"). Phase 3 installed the modifier and the write path. JsonTypeInfo.OnDeserialized fires once per constructed owner at any depth, including ctor-bound records; JsonPropertyInfo.Set is NOT called for ctor-bound parameters, so decryption happens after construction through compiled setters.
TASK: Phase 4 — deferred, batched decryption through the read frame; fail-closed read outcomes; forgotten placeholder and tombstone; CryptoShreddingDecryptionException; new error codes; handler doc fix.
KEY RULES: only crypto.subject_forgotten, or a tombstone on a subject the provider confirms forgotten (IsSubjectForgottenAsync, cached per call; subject id checked first), yields AnonymizedPlaceholder; a tombstone on a live subject is IntegrityCheckFailed; key length/version checked on read too, only AuthenticationTagMismatchException maps to IntegrityCheckFailed; a subject filter from the ambient scope skips other subjects' owners; every other failure throws (AGENTS §3: errors never swallowed in background infrastructure); IForgottenSubjectHandler once per subject per call, its exceptions logged at 8472 via ForLogging() and swallowed; outcome by DecryptOutcome enum, never by comparing with the placeholder; async path passes the CancellationToken; CRAP <= 10 (split helpers); no subject id/value in logs, tags or messages.
REFERENCE FILES: src/Encina.Marten.GDPR/Serialization/CryptoShredderSerializer.cs:509-864 (current read path), src/Encina.Marten.GDPR/DefaultForgottenSubjectHandler.cs, src/Encina.Marten.GDPR/CryptoShreddingErrors.cs.
```

</details>

### Phase 5: Wiring, DI, startup validation and health

<details>
<summary>Tasks</summary>

1. `CryptoShredderSerializerFactory.Apply(StoreOptions options, IServiceScopeFactory scopeFactory, ILogger<CryptoShredderSerializer> logger, string anonymizedPlaceholder = "[REDACTED]")`:
   - already wrapped → no-op;
   - not `SystemTextJsonSerializer` → 8468 + `SerializerNotSupported`;
   - otherwise wrap, call `options.Serializer(wrapper)` and log 8460.
2. `ConfigureMartenCryptoShredding`: constructor `(IServiceScopeFactory, IOptions<CryptoShreddingOptions>, ILogger<CryptoShredderSerializer>)`. It also sets `options.Projections.Errors.SkipSerializationErrors = false` (exact member path confirmed against Marten 9.38 in this phase), documented in its remarks. Correct the Newtonsoft remarks.
3. `ServiceCollectionExtensions.AddEncinaMartenGdpr`:
   - register the new configurator;
   - register the internal `CryptoShredRoutingErasureStrategy` as the `IDataErasureStrategy`: remove any existing `IDataErasureStrategy` descriptor and keep it as the router's inner strategy (re-registered under an internal keyed service), register `CryptoShredErasureStrategy` as a concrete scoped service; DSR's later `TryAdd` is then a no-op;
   - keep `MartenEventPersonalDataLocator` as an additional `IPersonalDataLocator` and correct the comment that claims a composite aggregates it;
   - register `CryptoShreddingStartupValidationHostedService` when `ValidateOnStartup`.
   - The existing double invocation of `configure` (once into `IOptions`, once on a local instance to read the registration flags) stays: the flags decide which services are registered, so `IOptions` cannot serve them, and the pattern is shared across the repository (45 `configure?.Invoke(` calls in 40 `ServiceCollectionExtensions.cs` files under `src/`); changing it is outside #1698.
4. `CryptoShreddingOptions`: rename `AutoRegisterFromAttributes` to `ValidateOnStartup` and update the remarks. `CryptoShreddingOptionsValidator` is unchanged.
5. Rename `CryptoShreddingAutoRegistrationDescriptor` to `CryptoShreddingValidationDescriptor`. Rewrite the hosted service as `CryptoShreddingStartupValidationHostedService`, following "Startup validation" above, with each step a separate small method.
6. Check two Marten 9.38 APIs and record the outcome in the PR:
   - whether `StoreOptions.Storage.AllDocumentMappings` / `DocumentMapping.DuplicatedFields` is reachable: if it is, add the `DuplicatedByMarten` rule to the validator; if not, remove the flag and record the follow-up (Follow-up issues, item 6);
   - whether the registered event upcasters are reachable on `StoreOptions.Events` and distinguishable as raw `JsonDocument` transformations: if they are, add `CryptoShreddingConfigurationProblem.JsonDocumentUpcasterNotSupported` to the validator; if not, remove that member and keep the documented limitation (Design Choice 8).
7. Rewrite `Health/CryptoShreddingHealthCheck.cs` as described under "Consumers". Add the template change of 8463 and the messages 8467-8470 and 8481 that this phase calls; 8461 and 8462 renamed, 8462 raised to Warning.
8. Unit tests:
   - `CryptoShredderSerializerFactoryTests`: idempotent; a non-STJ inner throws; read-only options throw.
   - `ConfigureMartenCryptoShreddingTests`: `SkipSerializationErrors` is `false` after configuration.
   - `CryptoShreddingStartupValidationHostedServiceTests`: aggregated issues reported once per declaring type; open generic `E<TId>` accepted with 8470; `E<object>` rejected; a container converter rejected; `SerializerNotWrapped`; `ContractModifierMissing` for a resolver replaced after `Apply`, for `UseTypeInfoResolver(context)` called after `Apply`, and for a closed candidate type the modifier never saw; `ProjectionSkipsSerializationErrors` when re-enabled; `ErasureStrategyBypassed` for a custom strategy added after `AddEncinaMartenGdpr`; `PersonalDataLocatorBypassed` for an application locator registered after (and before) `AddEncinaMartenGdpr` without a composite; warnings 8475 and 8481; disabled → 8462 at Warning; `ReflectionTypeLoadException` 8469.
   - `CryptoShreddingHealthCheckTests`: unhealthy on a wrong resolver identity and on a failing nested canary.
   - `AddEncinaMartenGdprRegistrationTests`: `ValidateOnBuild` and `ValidateScopes` for the InMemory and PostgreSQL key stores; `IDocumentStore` resolvable; the resolved `IDataErasureStrategy` is the router when `AddEncinaDataSubjectRights` runs before and after; a custom strategy registered before becomes the inner strategy and receives only non-Marten locations; a custom strategy registered after fails startup; both registration orders of an application `IPersonalDataLocator`.
9. Migrate the existing tests assigned to Phase 5 in "Existing test migration".

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT: Encina.Marten.GDPR, issue #1698, plan docs/plans/crypto-shredding-nested-implementation-plan-1698.md ("Startup validation", Design Choices 6, 8, 9). The serializer now needs IServiceScopeFactory instead of ISubjectKeyProvider/IForgottenSubjectHandler, which removes the scoped PostgreSqlSubjectKeyProvider captured by the singleton ConfigureMartenCryptoShredding.
TASK: Phase 5 — factory and configurator wiring (including SkipSerializationErrors = false), AddEncinaMartenGdpr changes (routing erasure strategy that keeps any earlier strategy as inner), option and descriptor renames, startup validation hosted service (installation check, infrastructure checks, recursive STJ contract walk, aggregated per-property issues), health check rewrite, DI tests.
KEY RULES: AGENTS §3 registration completeness — DI test with ValidateOnBuild and ValidateScopes for both key stores; startup validation throws one CryptoShreddingConfigurationException with all issues and stops the host; every issue reported once on its declaring type; non-STJ inner serializer fails closed (never pass-through); Marten locations must always reach CryptoShredErasureStrategy and the Marten locator must take part in DSR in any registration order (fail startup otherwise); no unnumbered logs in touched files (8459, 8461, 8462, 8467-8470, 8475, 8481); health check data never includes subject ids; CRAP <= 10; PublicAPI.Unshipped.txt.
REFERENCE FILES: src/Encina.Marten.GDPR/ServiceCollectionExtensions.cs, ConfigureMartenCryptoShredding.cs, CryptoShreddingAutoRegistrationHostedService.cs, CryptoShreddingOptions.cs, Health/CryptoShreddingHealthCheck.cs, src/Encina.Compliance.DataSubjectRights/ServiceCollectionExtensions.cs:104-105, src/Encina.Marten/ServiceCollectionExtensions.cs:103-110 (IConfigureMarten bridge).
```

</details>

### Phase 6: Consumers — locator and erasure

<details>
<summary>Tasks</summary>

1. `src/Encina.Marten.GDPR/Locator/CryptoShreddedGraphWalker.cs`: internal static `IEnumerable<CryptoShreddedOccurrence> Walk(object root, JsonSerializerOptions options, CryptoShreddingTypePlanRegistry registry)`.
   - Iterative stack, `ReferenceEqualityComparer` visited set, `MaxDepth` bound (64 when 0).
   - Paths: dots for members, `[]` for sequence elements, `{}` for dictionary values; never an index or a key.
   - `readonly record struct CryptoShreddedOccurrence(object Owner, CryptoShreddedField Field, string Path)`.
2. `MartenEventPersonalDataLocator`:
   - take the serializer from the session's store; run the query inside `CryptoShreddingCallScope.FilterToSubject(subjectId)` so only the requested subject's owners are decrypted; walk with the walker;
   - match per-owner subjects; `FieldName` = path; record each produced `PersonalDataLocation` in the provenance table read by the routing strategy;
   - `Left(crypto.serializer_unsupported)` when the serializer is not wrapped, `Left(crypto.key_store_error)` when a read of the requested subject throws; unwrap `EventDeserializationFailureException` to log its event sequence (verify the member in Marten 9.38);
   - logs 8478-8480 (added to `CryptoShreddingLog` here).
3. `CryptoShredErasureStrategy`: map `Left(crypto.subject_forgotten)` to `Right(unit)` (8477); 8476 replaces the unnumbered `LogDebug`. New internal `Erasure/CryptoShredRoutingErasureStrategy.cs`: Marten-provenance locations → `CryptoShredErasureStrategy`; others → inner strategy, or `Left(crypto.erasure_strategy_missing)` when there is none.
4. `Encina.Compliance.DataSubjectRights`: `CompositePersonalDataLocator` returns `Left` when any inner locator fails (its existing log line stays), and exposes its inner locators through an internal accessor (`InternalsVisibleTo` `Encina.Marten.GDPR`); update its unit tests and add a `changelog.d` fragment (`changed`).
5. `Encina.DomainModeling`: `[JsonIgnore]` on `AggregateBase.UncommittedEvents` (`AggregateBase.cs:73`) and `Entity<TId>.DomainEvents` (`Entity.cs:65`); `[JsonInclude]` on `AggregateBase.Id` (`:67`, `protected set`) and `Entity<TId>.Id` (`:53`, `protected init`). Unit tests: a round trip through Marten's `SystemTextJsonSerializer` keeps `Id` and writes no `UncommittedEvents`/`DomainEvents`; a `[CryptoShredded]` property on an `AggregateBase` subclass passes the classifier. This also fixes the snapshot `Id` loss (Follow-up issues, item 9).
6. Mark `Events/PiiEncryptionFailedEvent.cs` and `Model/CryptoShreddedFieldMetadata.cs` for deletion in the switchover close. The `Encina.Security.Encryption` project reference stays (see "Consumers", Removed types).
7. Unit tests:
   - `CryptoShreddedGraphWalkerTests`: paths, dictionary values only, cycle, shared instance, depth bound, polymorphic runtime type.
   - `MartenEventPersonalDataLocatorTests` (updated): nested paths, filtering by the nested subject, a different subject per level, unsupported serializer; an unreadable event of another subject does not fail the request and none of that subject's keys is fetched.
   - `CryptoShredErasureStrategyTests`: already forgotten → success + 8477.
   - `CryptoShredRoutingErasureStrategyTests`: Marten locations reach crypto-shredding even with a custom inner strategy; other locations reach the inner strategy and never delete subject keys; no inner → `crypto.erasure_strategy_missing`.
8. Migrate the existing tests assigned to Phase 6 in "Existing test migration".

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT: Encina.Marten.GDPR, issue #1698, plan docs/plans/crypto-shredding-nested-implementation-plan-1698.md ("Consumers", Design Choice 9). DSR's DefaultDataErasureExecutor calls IDataErasureStrategy once per PersonalDataLocation; ErasureScope.SpecificFields matches FieldName exactly (OrdinalIgnoreCase); exporters print FieldName.
TASK: Phase 6 — CryptoShreddedGraphWalker over STJ contracts; locator decrypts only the requested subject (ambient subject filter), reports nested paths per owner subject and fails closed; idempotent CryptoShredErasureStrategy behind a provenance-routing strategy; CompositePersonalDataLocator fails when any locator fails; Encina.DomainModeling serialization attributes; mark PiiEncryptionFailedEvent and CryptoShreddedFieldMetadata for deletion.
KEY RULES: no change to the public API of Encina.Compliance.DataSubjectRights (the composite change is behavioural, with a changelog fragment); crypto-shred erasure stays subject-wide (#1144), docs and tests never claim field-scoped erasure; paths never contain indexes or dictionary keys; never log a Marten stream id; EntityId stays the subject id; no subject id or value in logs (8476-8480 only); CRAP <= 10; PublicAPI.Unshipped.txt lines removed for deleted types; update the IDataErasureStrategy contract test at tests/Encina.ContractTests/Marten/GDPR/IDataErasureStrategyContractTests.cs:127.
REFERENCE FILES: src/Encina.Marten.GDPR/Locator/MartenEventPersonalDataLocator.cs, src/Encina.Marten.GDPR/Erasure/CryptoShredErasureStrategy.cs, src/Encina.Compliance.DataSubjectRights/Model/PersonalDataLocation.cs, src/Encina.Compliance.DataSubjectRights/Erasure/DefaultDataErasureExecutor.cs:140-175, src/Encina.Compliance.DataSubjectRights/Locators/CompositePersonalDataLocator.cs:66-97, src/Encina.DomainModeling/AggregateBase.cs, src/Encina.DomainModeling/Entity.cs.
```

</details>

### Phase 7: Observability and switchover close

<details>
<summary>Tasks</summary>

1. Check that every row of the EventId table is implemented as listed (messages and template changes were added by the phase that first called them, Phases 3-6): 8450, 8451, 8454, 8455, 8456, 8458-8463 and 8466 in `CryptoShreddingLogMessages.cs` with one literal `new EventId(n, nameof(...))` per `Define`; 8467-8481 in `CryptoShreddingLog.cs`.
2. `Diagnostics/CryptoShreddingDiagnostics.cs`: the lazy `Activity` per call, `crypto.failure_reason` on the counters, and deletion of the unused `TagPropertyName`. (`RecordFailed(Activity?, string reason)` and the misconfiguration counter were added in Phase 3.)
3. Run the architecture tests (`EncinaEventIdAllocationTests`, `EventIdUniquenessRule`) unchanged.
4. Extend `CryptoShreddingDiagnosticsSubjectIdLeakTests`: nested subjects, decryption failures, dictionary keys, tombstones and stream ids never appear in logs, tags, metrics or exception messages; `RecordFailed` never carries an exception message.
5. Check that no row of "Existing test migration" assigned to Phases 3-7 is left.
6. **Switchover close**: delete `Metadata/CryptoShreddedPropertyCache.cs`, `Metadata/CryptoShreddedFieldInfo.cs`, `Serialization/EncryptedFieldJsonConverter.cs`, `Events/PiiEncryptionFailedEvent.cs`, `Model/CryptoShreddedFieldMetadata.cs` and the old hosted service, with their `PublicAPI.Unshipped.txt` lines. Then `dotnet build Encina.slnx --configuration Release` with zero warnings, all tests passing, and the local CRAP table for every method changed in Phases 3-7.

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```text
CONTEXT: Encina.Marten.GDPR, issue #1698, plan docs/plans/crypto-shredding-nested-implementation-plan-1698.md ("Error codes and EventIds"). Range 8450-8499 is already registered (src/Encina/Diagnostics/EventIdRanges.cs:325) and mapped (tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs:90).
TASK: Phase 7 — check the EventId table against the code (messages were added by Phases 3-6), finalize diagnostics tags, extend leak tests, and close the switchover: delete the old types, confirm the test migration is complete, full Release build with zero warnings, all tests green, CRAP table for Phases 3-7.
KEY RULES: ADR-021 — only ids in the registered range, packed sequentially, one literal EventId per Define; never log subject id, value, collection index, dictionary key or EncinaError.Message; exceptions via ForLogging(); RecordFailed never receives ex.Message; low-cardinality tags only.
REFERENCE FILES: src/Encina.Marten.GDPR/Diagnostics/CryptoShreddingLogMessages.cs, src/Encina.Marten.GDPR/Diagnostics/CryptoShreddingDiagnostics.cs, tests/Encina.UnitTests/Marten/GDPR/CryptoShreddingDiagnosticsSubjectIdLeakTests.cs.
```

</details>

### Phase 8: Testing (remaining flags, benchmarks, load)

<details>
<summary>Tasks</summary>

1. **Guard** (`tests/Encina.GuardTests/Marten/GDPR/`, public API only):
   - `CryptoShredderSerializerGuardTests` (rewritten): constructor nulls, a non-STJ inner, a blank placeholder, null writer, parameter and type arguments.
   - `CryptoShredderSerializerFactoryGuardTests`.
   - `CryptoShreddingEncryptionExceptionGuardTests` (new constructor).
   - `CryptoShreddingDecryptionExceptionGuardTests`.
   - `CryptoShreddingConfigurationExceptionGuardTests`.
   - `CryptoShreddedPropertyIssueGuardTests`.
   - `CryptoShredErasureStrategyGuardTests`.
   - `CryptoShreddingHappyPathGuardTests`: remove the deleted types and add a happy path through `Apply`.
2. **Contract** (`tests/Encina.ContractTests/Marten/GDPR/`):
   - new `CryptoShredderSerializerContractTests`: for documents without PII, every write and read member gives output byte-identical to the inner serializer; `EnumStorage`, `Casing` and `ValueCasting` delegate; PII documents round-trip.
   - `IDataErasureStrategyContractTests`: erasing the same subject twice succeeds.
   - `ISubjectKeyProviderContractTests`: unchanged.
3. **Property** (`tests/Encina.PropertyTests/Marten/GDPR/`):
   - `CryptoShredderSerializerNestedPropertyTests` (FsCheck). Generators build graphs of depth 0-4 with lists, dictionaries, nulls, shared references and 1-3 subjects, with sentinel-prefixed plaintexts. Invariants:
     - the JSON contains no generated plaintext;
     - the token count equals the non-null field count;
     - the caller's graph is unchanged;
     - the round trip equals the original;
     - forgetting a random subset of subjects turns exactly their fields into the placeholder;
     - `GetOrCreateSubjectKeyAsync` is called once per distinct subject;
     - a missing subject anywhere throws and leaves the caller's buffer empty.
   - `CryptoShredderSerializerPropertyTests` moves to the real inner serializer.
   - `CryptoShreddingPropertyTests` covers the v2 token invariants.
4. **Integration** (`tests/Encina.IntegrationTests/Infrastructure/Marten/GDPR/`, `[Collection(MartenCollection.Name)]`, `[Trait("Category", "Integration")]`, `[Trait("Database", "PostgreSQL")]`, a unique `DatabaseSchemaName` per store, never call the fixture's `DisposeAsync`):
   - `CryptoShreddingNestedIntegrationTests`, using a real host with `AddMarten` + `AddEncinaMartenGdpr(UsePostgreSqlKeyStore = true)`:
     - append and fetch a nested object, a list of records, dictionary values, a polymorphic payload, a positional record, `E<Guid>` and private `[JsonInclude]`;
     - raw `mt_events.data` contains `cs2:` and no plaintext; fetch returns plaintext;
     - forgetting a nested subject that differs from the root redacts only that subject;
     - a missing nested subject id and a misconfigured nested type each store zero events;
     - a key rotation round trip;
     - re-saving a forgotten subject's read model writes the tombstone;
     - a `SnapshotEnvelope<T>.State` of a real `AggregateBase` subclass with a `[CryptoShredded]` property (through `MartenSnapshotStore`) and a read-model document are encrypted in `mt_doc_*`, and the snapshot restores `Id`;
     - a key-store failure on read throws `CryptoShreddingDecryptionException`;
     - a tombstone written into a live subject's row reads as `IntegrityCheckFailed`;
     - a stream read through a raw `JsonDocument` upcaster throws Marten's `MartenException` and stores or returns no plaintext (or the startup validator rejects the upcaster, per Phase 5 task 6).
   - `CryptoShreddingAsyncDaemonIntegrationTests`: the key provider fails while a continuous projection runs; the shard pauses (no dead letter) and catches up with no skipped event after the provider recovers.
   - `MartenEventPersonalDataLocatorIntegrationTests`: the real locator returns nested paths; an event of another subject with a deleted key does not fail the request; the locator registered after an application locator still takes part (or startup fails).
   - `CryptoShreddingErasureIntegrationTests`: `DefaultDataErasureExecutor` over N nested locations of one subject reports N erased and 0 failed (whole-subject erasure, valid under either #1144 fix); with a custom inner strategy, Marten locations are still crypto-shredded.
   - `MartenEventMetadataQueryCryptoIntegrationTests` (in the Marten integration folder): a metadata query over an event whose key is missing returns `Left(QueryFailed)` (documented behaviour until Follow-up issues, item 4).
   - `CryptoShreddingHostStartupIntegrationTests`: startup validation runs on a real host; a misconfigured scanned type stops the host; `UseTypeInfoResolver(context)` registered after `AddEncinaMartenGdpr` stops the host.
   - Existing integration tests: as assigned in "Existing test migration".
5. **Benchmark** (`tests/Encina.BenchmarkTests/Encina.Marten.GDPR.Benchmarks/CryptoShredderSerializerBenchmarks.cs`):
   - keep `InnerSerializer_NonPii` (baseline), `CryptoSerializer_NonPii` and `CryptoSerializer_PiiEvent`;
   - add `CryptoSerializer_NestedPiiEvent` (a nested object plus a list of 10, 2 subjects), `CryptoSerializer_NonPiiNested` and `CryptoDeserializer_NestedPiiEvent`;
   - add `.github/perf-manifest/Encina.Marten.GDPR.Benchmarks.json` entries and DocRef ids `bench:gdpr/crypto-serialize-nested-pii`, `bench:gdpr/crypto-serialize-nonpii-nested` and `bench:gdpr/crypto-deserialize-nested-pii`;
   - validate with `--list flat --filter` and `--job dry`.
6. **Load**: add `tests/Encina.LoadTests/Marten/GDPR/CryptoShredderSerializerConcurrencyLoadTests.cs` and delete the stale justification `CryptoShredding.md` (it rests on the #322 "stateless serializer" design). The new design is not stateless: `[ThreadStatic]` write frames and staging buffers, `AsyncLocal` read frames, static `ConcurrentDictionary` shape caches and the lazy installation check. The test shares one `CryptoShredderSerializer` (real inner, `InMemorySubjectKeyProvider`) across N parallel writers and readers with distinct subjects, nested graphs and async reads crossing awaits, and asserts: every round trip returns its own plaintext, no token decrypts under another subject (no frame or key bleed), each call's provider lookups match its own subjects, and no exception. It runs locally with `DOTNET_JitObjectStackAllocationConditionalEscape=0` like the other load tests.
7. **Coverage**:
   - `dotnet run .github/scripts/generate-coverage-manifest.cs -- --append-only` for the new files; remove or rename the entries of deleted and renamed files (`CryptoShreddingAutoRegistrationHostedService`, `CryptoShreddingAutoRegistrationDescriptor`, `CryptoShreddedPropertyCache`, `CryptoShreddedFieldInfo`, `EncryptedFieldJsonConverter`, `PiiEncryptionFailedEvent`, `CryptoShreddedFieldMetadata`), and check the manifests of `Encina.Compliance.DataSubjectRights` and `Encina.DomainModeling` for their changed files;
   - check that each flag reaches its target in `.github/coverage-manifest/Encina.Marten.GDPR.json` (unit 40, guard 10, property 10);
   - produce the local CRAP table for every changed method.

</details>

<details>
<summary>Prompt for AI Agents — Phase 8</summary>

```text
CONTEXT: Encina.Marten.GDPR, issue #1698, plan docs/plans/crypto-shredding-nested-implementation-plan-1698.md (Phase 8 task list). Phases 1-7 delivered the implementation with unit tests.
TASK: Phase 8 — guard, contract, property, integration (including the async-daemon pause, AggregateBase snapshot, routing and locator subject-filter cases), benchmark and concurrency load test obligations; coverage manifest and local CRAP table.
KEY RULES: tests execute real package code (real SystemTextJsonSerializer inner, never reflection-only); integration tests use [Collection(MartenCollection.Name)] with Category/Database traits, unique schema per store, no per-class fixture, no fixture DisposeAsync, real PostgreSqlSubjectKeyProvider through AddEncinaMartenGdpr; Shouldly via Encina.Testing.Shouldly, FsCheck via Encina.Testing.FsCheck, FakeTimeProvider; no Thread.Sleep; outputs under artifacts/; BenchmarkSwitcher, never BenchmarkRunner.Run<T>(); per-flag targets from .github/coverage-manifest/Encina.Marten.GDPR.json; do not push before every flag reaches its target.
REFERENCE FILES: tests/Encina.IntegrationTests/Infrastructure/Marten/GDPR/CryptoShredderSerializerIntegrationTests.cs (helpers ReadStoredEventJsonAsync, CountStoredEventsAsync, FindEncryptionFailure), tests/Encina.IntegrationTests/Infrastructure/Marten/Fixtures/MartenFixture.cs, tests/Encina.PropertyTests/Marten/GDPR/CryptoShredderSerializerPropertyTests.cs, tests/Encina.BenchmarkTests/Encina.Marten.GDPR.Benchmarks/CryptoShredderSerializerBenchmarks.cs.
```

</details>

### Phase 9: Documentation and finalization

<details>
<summary>Tasks</summary>

1. XML docs on every new or changed public API: the three exceptions, the enums, `CryptoShreddedPropertyIssue`, `CryptoShredderSerializer`, `CryptoShredderSerializerFactory`, the renamed option and descriptor, `IForgottenSubjectHandler` (renamed parameters). Also the `CryptoShreddedAttribute` remarks (sibling on the declaring object at any depth; non-public needs `[JsonInclude]`; interface, implementation/override, hiding, constructor, `IJsonOnDeserialized`, hashed-set and computed-member rules; open generics), the `DefaultForgottenSubjectHandler` text, and the `ISubjectKeyProvider` remarks (`ISubjectKeyProvider.cs:62-65,89-93,116,157-162`): the version is no longer written into a `kid`/key id (the v2 token carries only the version), and the claims that `DeleteSubjectKeysAsync`/`RotateSubjectKeyAsync` publish `SubjectForgottenEvent`/`SubjectKeyRotatedEvent` are removed until Follow-up issues, item 3 wires or deletes them.
2. `src/Encina.Marten.GDPR/PublicAPI.Unshipped.txt`: add, change and remove lines exactly as in "Public API changes" below.
3. Delegated to `docs-writer` with the `encina-docs` skill, one Diátaxis quadrant per page:
   - `docs/features/crypto-shredding.md`: Limitations rewritten (Patch, LINQ `Select`, duplicated fields, container converters, non-polymorphic bases, `object` members, structs, string collections, personal data declared on interfaces, raw `JsonDocument` upcasters, source-generated contexts in `Serialization` mode, constructors that transform values); ordering (`UseTypeInfoResolver` before `AddEncinaMartenGdpr`); Encryption Flow and DSR Integration Flow mermaid diagrams; Requirements table; Validation list with the problem flags and configuration problems; Fail-closed tables for the three exceptions; read semantics and tombstone (confirmed against the provider); async daemon (`SkipSerializationErrors` turned off; a key-store outage pauses the shard); in-memory key store loses keys on restart and reads then fail (#1191 owns the durable default); erasure is subject-wide for crypto-shredded data whatever `FieldName`/`SpecificFields` say (#1144); metadata queries over undecryptable events fail; projections, snapshots and read models (now encrypted; a copy without the attribute is not; `AggregateBase` subclasses supported); custom `IDataErasureStrategy` and `IPersonalDataLocator` registration order; Structured Logging (8459, 8467-8481); FAQ; Best Practices (pseudonymous subject ids, value objects carry their own subject-id property, streams not keyed by subject id); Testing table.
   - `src/Encina.Marten.GDPR/README.md`: lines 72-74 and 207-223, the Performance docref table, and the whole options table (`AutoRegisterFromAttributes` at `:114` becomes `ValidateOnStartup`; `PublishEvents` at `:116` and `:285` says the events are not published yet and points at Follow-up issues, item 3).
   - `ErasureScope.SpecificFields` and `PersonalDataLocation.FieldName` docs in `Encina.Compliance.DataSubjectRights`: they match the full path; for crypto-shredded data the erasure still covers the whole subject (#1144). `CompositePersonalDataLocator` docs: any failing locator fails the request.
4. New `docs/architecture/adr/034-crypto-shredding-through-the-stj-contract.md` (superseding the mechanism part of ADR-030 for Marten). ADR numbers 032 and 033 are reserved by #1187 and #1189 (`docs/architecture/adr/index.md`, "Reserved numbers"), so 034 is the next number that is neither used nor reserved. **The plan PR itself** (its first commit) adds the reservation row `| 034 | [crypto-shredding-nested-implementation-plan-1698.md](../../plans/crypto-shredding-nested-implementation-plan-1698.md) (#1698) | Crypto-shredding through the System.Text.Json contract | Reserved, ADR not written |` to that table; if another plan has taken 034 by then, the next free number is reserved instead and every "034" in this plan is updated. In Phase 9 the row moves from "Reserved numbers" to the main ADR table when the ADR is written.
5. Changelog fragments (via `mechanical-fixer`), validated by `dotnet run .github/scripts/changelog-fragments.cs -- --check`:
   - `changelog.d/1698-nested-crypto-shredding.security.md`: nested, collection and non-public PII encrypted; read path fails closed; tombstones confirmed against the provider; installation check; async daemon no longer skips decryption failures;
   - `changelog.d/1698-crypto-shredding-api.changed.md`: exceptions; option and hosted service renames; v2 token; STJ only; keys through `IServiceScopeFactory`; `FieldName` paths; `IForgottenSubjectHandler` parameter renames; erasure routing; startup infrastructure checks;
   - `changelog.d/1698-composite-locator-fails-closed.changed.md`: `CompositePersonalDataLocator` returns `Left` when any locator fails;
   - `changelog.d/1698-crypto-shredding-dead-api.removed.md`: `PiiEncryptionFailedEvent`, `CryptoShreddedFieldMetadata`;
   - `changelog.d/1698-erasure-idempotent.fixed.md`: idempotent erasure; Marten locations always crypto-shredded whatever strategy is registered;
   - `changelog.d/<snapshot-bug>-aggregate-snapshot-id.fixed.md`: `AggregateBase`/`Entity<TId>` keep `Id` and drop uncommitted/domain events in serialized snapshots (Follow-up issues, item 9).
6. Knowledge records (SPEC-003), checked with `dotnet run .github/scripts/knowledge-records.cs -- --check` before Phase 9 closes (it fails on a `status: done` destination whose target file does not exist, `knowledge-records.cs:263-264`):
   - `docs/knowledge/issues/1698.md` (`prs` filled by the orchestrator) with the new decisions: STJ contract mechanism, v2 token, fail-closed reads with confirmed tombstones, installation check by identity, erasure routing.
   - `docs/knowledge/issues/1174.md`: retarget the done destinations at `:28` (`Metadata/CryptoShreddedFieldInfo.cs` → `Metadata/CryptoShreddedField.cs`), `:40` (the old hosted service → `CryptoShreddingStartupValidationHostedService.cs`) and `:43` (its tests → `CryptoShreddingStartupValidationHostedServiceTests.cs`); update the file list at `:80-81`; set the rule that names the auto-registration scan to `current: no`.
   - `docs/knowledge/issues/1646.md`: retarget the test destination at `:59`; set to `current: no` the gotcha clause about the nested type not being scanned and the superseded decisions (1: thrown before the inner serializer runs; 2: names the event type and the `PropertyMisconfigured` reason; 3: version written into the key id `subject:{subjectId}:v{version}`), each pointing at 1698.md.
7. `docs/INVENTORY.md` if it lists files of this package.
8. Verification:
   - `dotnet build Encina.slnx --configuration Release` gives 0 errors and 0 warnings.
   - `dotnet test` with all tests passing, results in `artifacts/test-results`.
   - Every flag reaches its target.
   - The local CRAP table shows no changed method above 10.

</details>

<details>
<summary>Prompt for AI Agents — Phase 9</summary>

```text
CONTEXT: Encina.Marten.GDPR, issue #1698, plan docs/plans/crypto-shredding-nested-implementation-plan-1698.md. Implementation and tests are done.
TASK: Phase 9 — XML docs (including ISubjectKeyProvider remarks), PublicAPI.Unshipped.txt, feature page and README options table (delegate to docs-writer with the encina-docs skill), ADR-034 (move its row from "Reserved numbers" to the ADR table in docs/architecture/adr/index.md), changelog fragments (mechanical-fixer), knowledge records 1698.md plus the 1174.md and 1646.md retargets and current: no markings (knowledge-records.cs --check must pass), final build/test/coverage/CRAP verification.
KEY RULES: English only; never edit CHANGELOG.md [Unreleased]; never type coverage figures by hand (covref/mutref markers); docs say value objects carry their own subject-id property, the read path fails closed, Patch/duplicated fields/container converters are forbidden on crypto data; no AI attribution anywhere.
REFERENCE FILES: docs/features/crypto-shredding.md, src/Encina.Marten.GDPR/README.md, docs/architecture/adr/030-encryption-at-the-serializer-level.md, docs/architecture/adr/index.md, changelog.d/README.md, changelog.d/1646-cryptoshredder-fail-closed.security.md, docs/knowledge/issues/1174.md, docs/knowledge/issues/1646.md, .github/scripts/knowledge-records.cs, src/Encina.Marten.GDPR/Abstractions/ISubjectKeyProvider.cs, .claude/skills/encina-docs/SKILL.md.
```

</details>

---

## Public API changes

All lines go in `src/Encina.Marten.GDPR/PublicAPI.Unshipped.txt`, because `Shipped` is effectively empty. This is pre-1.0, so there is no `[Obsolete]` and no shim.

- **Added**
  - `CryptoShreddingConfigurationException` (`Problem`, `Issues`) and `CryptoShreddingConfigurationProblem` (`MisconfiguredProperties`, `SerializerNotSupported`, `SerializerNotWrapped`, `ContractModifierMissing`, `ProjectionSkipsSerializationErrors`, `ErasureStrategyBypassed`, `PersonalDataLocatorBypassed`, and `JsonDocumentUpcasterNotSupported` if Phase 5 task 6 confirms the Marten API).
  - `CryptoShreddingDecryptionException` and `CryptoShreddingDecryptionFailureReason`.
  - `CryptoShreddedPropertyProblems` (`[Flags]`) and `CryptoShreddedPropertyIssue`.
  - `CryptoShreddingEncryptionException.DocumentTypeName` and `.DeclaringTypeName`.
  - `CryptoShreddingErrors.EnvelopeMalformedCode`, `IntegrityCheckFailedCode`, `SerializerUnsupportedCode` and `ErasureStrategyMissingCode`, with their factories.
  - `CryptoShreddingOptions.ValidateOnStartup`.
- **Changed**
  - The `CryptoShreddingEncryptionException` constructor; `CryptoShreddingEncryptionFailureReason` becomes `{ SubjectIdMissing, SubjectIdInvalid, KeyUnavailable }`.
  - The `CryptoShredderSerializer` constructor takes `SystemTextJsonSerializer` and `IServiceScopeFactory`.
  - The `CryptoShredderSerializerFactory.Apply` signature.
  - `CryptoShreddingAutoRegistrationDescriptor` is renamed `CryptoShreddingValidationDescriptor`, if public.
  - `IForgottenSubjectHandler.HandleForgottenSubjectAsync` (and `DefaultForgottenSubjectHandler`): parameters `eventType` → `documentType`, `propertyName` → `fieldPath`.
- **Removed**
  - `CryptoShreddingEncryptionException.EventTypeName`.
  - `CryptoShreddingEncryptionFailureReason.PropertyMisconfigured`.
  - `CryptoShreddingOptions.AutoRegisterFromAttributes`.
  - `PiiEncryptionFailedEvent`.
  - `CryptoShreddedFieldMetadata`.
- **Behavioural**
  - The v2 token replaces the v1 envelope.
  - Reads throw on failures other than a forgotten subject.
  - `PersonalDataLocation.FieldName` is a path for nested fields.
  - Erasing an already forgotten subject succeeds.
  - Marten locations are always erased by crypto-shredding; any other registered `IDataErasureStrategy` receives only non-Marten locations.
  - `CompositePersonalDataLocator` (DSR) fails when any locator fails.
  - The handler receives the root document type and a field path.
  - A tombstone reads as the placeholder only for a subject the provider confirms forgotten.
  - The async daemon pauses on a decryption failure instead of dead-lettering the event (`SkipSerializationErrors = false`).
  - `AggregateBase`/`Entity<TId>` serialize `Id` and no longer serialize `UncommittedEvents`/`DomainEvents` (`Encina.DomainModeling`).
  - Only System.Text.Json is supported; raw `JsonDocument` upcasters fail closed.

---

## Research

### Standards and specifications

| Reference | Relevance |
|-----------|-----------|
| GDPR Art. 17 | Erasure by destroying the subject key; tombstone avoids storing new personal data for erased subjects |
| GDPR Art. 25, Art. 32(1)(a), Art. 5(1)(f) | Encryption by default at every depth; integrity (AES-GCM tag + AAD) |
| GDPR Art. 15, Art. 20 | Locator reports nested fields for access and portability |
| NIST SP 800-38D | AES-GCM: 96-bit random nonce, 128-bit tag, associated data |
| RFC 4648 §5 | base64url encoding of the token payload |
| System.Text.Json contract customization (`JsonTypeInfo`, `JsonPropertyInfo.Get`, `OnDeserialized`, `WithAddedModifier`, `OriginatingResolver`) | Mechanism; behaviour verified by throwaway probes during the plan review, 2026-10-03 (not kept in the repository) |
| Plan review probes (2026-10-03, Marten 9.38.0, throwaway file-based apps, deleted) | `UseTypeInfoResolver(context)` after the modifier bypasses it while a type-only canary passes; a `Serialization`-mode context writes plaintext with an empty owner contract; interface- and base-typed members write the runtime value through the unattributed contract; constructors and `IJsonOnDeserialized` see the token; a record in `HashSet<T>` breaks after in-place decryption; continuous daemon defaults skip serialization errors; `AesGcm` accepts 16/24-byte keys and copies its key; `AggregateBase.Id` is lost and `UncommittedEvents` serialized on a Marten STJ round trip |

### Existing Encina infrastructure to leverage

| Component | Location | Usage |
|-----------|----------|-------|
| `SubjectIdConversion` (internal, via InternalsVisibleTo) | `src/Encina.Compliance.DataSubjectRights/SubjectIdConversion.cs` | Supported subject-id types and invariant conversion |
| `PersonalDataAttribute`, `PersonalDataLocation`, `IDataErasureStrategy`, `DefaultDataErasureExecutor` | `src/Encina.Compliance.DataSubjectRights/` | Required companion attribute; locator output; erasure loop |
| `ISubjectKeyProvider`, `InMemorySubjectKeyProvider`, `PostgreSqlSubjectKeyProvider` | `src/Encina.Marten.GDPR/Abstractions`, `KeyStore/` | Unchanged by #1698 except XML remarks; reached through `IServiceScopeFactory`; `IsSubjectForgottenAsync` confirms tombstones; PostgreSQL race fixes come from #1699 (prerequisite) |
| `CompositePersonalDataLocator` | `src/Encina.Compliance.DataSubjectRights/Locators/` | Fails when any locator fails; checked by the startup validator |
| `AggregateBase`, `Entity<TId>` | `src/Encina.DomainModeling/` | Serialization attributes so they can be crypto owners |
| `AddEncinaMartenStoreOptionsBridge` | `src/Encina.Marten/ServiceCollectionExtensions.cs:103-110` | Applies `ConfigureMartenCryptoShredding` to Marten |
| `SnapshotEnvelope<T>`, `MartenSnapshotStore` | `src/Encina.Marten/Snapshots/` | Snapshots become encrypted with no extra code |
| `ForLogging()` | Encina core | Exceptions passed to loggers |
| `MartenFixture`, `MartenCollection` | `tests/Encina.IntegrationTests/Infrastructure/Marten/Fixtures/` | Shared PostgreSQL container |
| `RedactedExceptionLogAssert`, `FakeLogger` | `tests/Encina.UnitTests/Support/` | Leak assertions |

### Event ID allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Marten.GDPR` | 8450-8499 (`EventIdRanges.MartenGDPRCryptoShredding`, registered and mapped) | 8450-8466 existing (templates changed for 8450, 8451, 8454-8456, 8459-8463, 8466; 8458 repurposed); new 8467-8481; free 8482-8499 |

### Estimated file count

| Category | New | Modified | Deleted |
|----------|-----|----------|---------|
| Production (`src/Encina.Marten.GDPR`) | ~13 | ~16 | 6 (incl. the old hosted service) |
| Production (other packages) | 0 | ~5 (DSR composite + XML docs, `AggregateBase.cs`, `Entity.cs`) | 0 |
| Unit tests | ~13 | ~14 | 2 (+1 renamed) |
| Guard / Contract / Property tests | ~6 / 1 / 1 | ~3 / 1 / 4 | 0 |
| Integration tests | 6 | 3 | 0 |
| Benchmarks / load / manifests | 1 (load test) | 4 | 1 (load `.md`) |
| Docs, ADR, changelog, knowledge | 8 | 7 (incl. ADR index, 1174.md, 1646.md) | 0 |

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT: Encina — .NET 10 / C# 14, pre-1.0, no users, breaking changes welcome, best design always (AGENTS.md). Package src/Encina.Marten.GDPR encrypts [CryptoShredded] string properties (with [PersonalData]) of everything Marten serializes, with a per-subject key; deleting the key makes the data unreadable (GDPR Art. 17). Issue #1698: nested, collection, non-public properties are stored in plaintext; a generic subject id is rejected at startup; misconfiguration has no per-property reason. Maintainer decision: full nested encryption.

IMPLEMENTATION OVERVIEW: replace the mutate-and-restore ISerializer decorator with a System.Text.Json contract modifier installed through Marten's SystemTextJsonSerializer.Configure on all four internal option sets. Encrypt in a wrapped JsonPropertyInfo.Get (receives the declaring object; subject from its SubjectIdProperty sibling; returns a cs2 token with AES-GCM and AAD binding subject and version). Decrypt after deserialization: JsonTypeInfo.OnDeserialized enqueues owners into a per-call read frame; the wrapper decrypts in batches per (subject, version) through compiled setters. One classifier (reflection + contract rules → [Flags] CryptoShreddedPropertyProblems) feeds the modifier, the startup validator, logs and CryptoShreddingConfigurationException; it rejects every shape that would leak or not round-trip (converter owners and containers, source-generated contracts, new-hiding, interface-only attributes, attributes only on an implementation/override of an unattributed declaration, constructor binding outside synthesized positional records, IJsonOnDeserialized owners, value-equality owners in hashed/sorted sets, computed members on any type reaching an owner, structs, getter-only, non-serialized, non-deserializable, property converters, dictionary keys, bad subject ids). Installation is verified by resolver identity and chain per options object, a plan registered for every closed candidate type, and a nested canary whose output holds a cs2 token. Reads fail closed except for a forgotten subject (placeholder) and the cs2:erased tombstone (written when a forgotten subject's placeholder is saved again; accepted on read only after the provider confirms the subject is forgotten). The configurator turns off Marten's SkipSerializationErrors so the daemon pauses instead of dead-lettering. Keys through IServiceScopeFactory, cached per call only as AesGcm instances (never a provider-owned array). Locator decrypts only the requested subject, walks STJ contracts and reports path FieldNames; a routing erasure strategy sends Marten locations to idempotent crypto-shredding whatever other strategy is registered; startup fails if the Marten locator or the router is bypassed. STJ only; any other serializer and raw JsonDocument upcasters fail closed. Prerequisite: #1699.

KEY PATTERNS: fail closed everywhere (AGENTS §3); never mutate the caller's object; no subject id, value, index, dictionary key, EncinaError.Message or exception message in logs, tags, metrics or exception messages; ForLogging() for exceptions; EventIds 8450-8499 only (new 8467-8481 via [LoggerMessage]); no Marten stream id in logs; CRAP <= 10 on every changed method (rule arrays, small helpers); Phases 1-2 additive, Phases 3-7 one switchover that builds green at its end; ROP Either in locator and strategy; TimeProvider for time; async with CancellationToken where the API allows; per-flag tests (unit, guard, contract, property, integration on Marten/PostgreSQL via [Collection(MartenCollection.Name)]); PublicAPI.Unshipped.txt; PowerShell or C# file-based apps only; English only; no AI attribution.

REFERENCE FILES: docs/plans/crypto-shredding-nested-implementation-plan-1698.md; src/Encina.Marten.GDPR/** ; src/Encina.Compliance.DataSubjectRights/SubjectIdConversion.cs; src/Encina.Marten/Snapshots/SnapshotEnvelope.cs; tests/Encina.*Tests/**/Marten/GDPR/**; docs/features/crypto-shredding.md; docs/architecture/adr/030-encryption-at-the-serializer-level.md; AGENTS.md §3, §6, §7, §9.
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ✅ | Per-call key cache only (one fetch per subject on write, per subject and version on read); never across calls, so Art. 17 deletion is never masked |
| 2 | OpenTelemetry | ✅ | Lazy Activity per call, `crypto.failure_reason` tag, misconfiguration counter; no subject id or value in tags |
| 3 | Structured Logging | ✅ | Changed templates for existing ids, 8467-8481 via `[LoggerMessage]`, unnumbered logs in touched files replaced (including `DefaultForgottenSubjectHandler`, 8474) |
| 4 | Health Checks | ✅ | Checks wrapper + installation check (resolver identity, chain, nested canary) + key provider resolution; reports `keyProviderType`; drops stale `IFieldEncryptor` prerequisite |
| 5 | Validation | ✅ | Startup validator with installation check, infrastructure checks (daemon error policy, erasure router, locator participation, upcasters), recursive STJ contract walk and aggregated per-property issues |
| 6 | Resilience | ⏭️ | Retries belong to the key provider. Reads now surface failures, and the configurator sets `SkipSerializationErrors = false` so the async daemon pauses the shard and resumes after recovery instead of dead-lettering (Marten's continuous default skips). Follow-up issue: a resilience pipeline for `PostgreSqlSubjectKeyProvider` (Follow-up issues, item 7) |
| 7 | Distributed Locks | ⏭️ | The serializer has no shared mutable state after the redesign. Key creation and erasure races are **not** handled by `PostgreSqlSubjectKeyProvider` today (read-then-upsert, no lock): tracked by #1699, a prerequisite of this change, extended to rotation and erasure (see "Dependencies on open issues") |
| 8 | Transactions | ❌ | The serializer runs inside Marten's unit of work; a throw aborts the append; a key created for a failed append is a harmless orphan |
| 9 | Idempotency | ✅ | `CryptoShredErasureStrategy` treats an already forgotten subject as success |
| 10 | Multi-Tenancy | ⏭️ | Subject keys are global, so equal subject ids in two tenants share one key and one erasure shreds both; tracked by #1191 (SPEC-002 AC-043, tenant-scoped keys), which will extend the token and AAD |
| 11 | Module Isolation | ❌ | Encryption is per subject, not per module; no module-scoped data in the serializer |
| 12 | Audit Trail | ❌ | Erasure audit stays in the DSR package (`DefaultDataErasureExecutor`), which now always reaches crypto-shredding for Marten locations through the routing strategy; the serializer has no state changes to audit beyond logs and metrics |

---

## Follow-up issues to open (orchestrator)

1. `[FEATURE]` Crypto-shredding adapter for `Marten.Newtonsoft` (`IContractResolver` + `IValueProvider` + `OnDeserializedCallbacks`, same classifier).
2. `[FEATURE]` `[CryptoShredded]` on string collections (`List<string>`, `string[]`, `IReadOnlyList<string>`, `Dictionary<string,string>` values).
3. `[DEBT]` Key providers: replace `RecordFailed(ex.Message)` (`PostgreSqlSubjectKeyProvider.cs:248, 357`, `InMemorySubjectKeyProvider.cs:246, 355`) and the unnumbered `LogDebug("Created initial encryption key")`. Wire or remove the dead 8464 `KeyRotationScheduled` and 8465 `ReEncryptionStarted`. Wire or remove `CryptoShreddingOptions.PublishEvents` (default `true`) together with `SubjectForgottenEvent` and `SubjectKeyRotatedEvent`, which nothing in the package publishes (grep for `new SubjectForgottenEvent`, `new SubjectKeyRotatedEvent`, `Publish(`, `PublishAsync(` finds nothing).
4. `[DEBT]` `Encina.Marten` metadata readers decrypt every event: `MartenEventMetadataQuery` and `MartenProjectionManager` fail with `Left` when any returned event cannot be decrypted, although they rarely need personal data; add a mode that reads metadata without decrypting (for example an ambient scope built like the locator's `FilterToSubject`).
5. `[FEATURE]` Asynchronous key prefetch (`IDocumentSessionListener.BeforeSaveChangesAsync` and per-batch prefetch in the async daemon), to remove sync-over-async from the write path.
6. `[SPIKE]` Guard against Marten paths that bypass serialization (`Patch().Set`, duplicated fields if Phase 5 cannot reach them, `FlatTableProjection` columns).
7. `[FEATURE]` Resilience pipeline for `PostgreSqlSubjectKeyProvider` (matrix item 6).
8. `[SPIKE]` Support Marten's raw `JsonDocument` upcasters with crypto-shredding: keep the real `SystemTextJsonSerializer` registered (the modifier works without the wrapper) and move the frame and staging duties elsewhere, for example a session listener.
9. `[BUG]` `AggregateBase.Id` is lost and `UncommittedEvents` is serialized when an aggregate snapshot round-trips through Marten's STJ serializer (`Entity<TId>.Id`/`DomainEvents` likewise). Fixed in this change (Phase 6, task 5); opened so the fix is traceable, closed by the #1698 PR.
10. `[BUG]` Only if #1699 cannot be extended: `PostgreSqlSubjectKeyProvider` key rotation upserts `v(n+1)` without a conflict check, a key can be created after an erasure committed, and `DeleteSubjectKeysAsync` returns at the forgotten marker without deleting remaining keys.

Tenant-scoped keys are not a new issue: #1191 (AC-043) tracks them. Check for duplicates before opening (`open-issue` skill).

---

## Maintainer decisions

None. The choices that looked like product decisions are already settled:

- **Read-path fail-closed**: settled by AGENTS §3.
- **Sibling subject id**: settled by the documented attribute contract and by what STJ hooks can see.
- **STJ only**: Marten 9.38 core ships no other serializer, and a Newtonsoft adapter is a follow-up issue.
- **Tombstone for re-saved placeholders**: required by Art. 17 together with the fail-closed write rule; confirming it against the provider on read follows from fail-closed reads.
- **Turning off `SkipSerializationErrors`, failing startup when the erasure router or the Marten locator is bypassed, and making `CompositePersonalDataLocator` fail when any locator fails**: settled by AGENTS §3 (compliance gates fail closed; errors are never swallowed in background infrastructure).
- **Rejecting `[CryptoShredded]` behind interfaces, transforming constructors, `IJsonOnDeserialized`, hashed sets and computed members on the graph**: each shape either leaks plaintext or makes stored data unreadable (verified by probe), so rejection is the only fail-closed option; each rejection has a documented fix (concrete-typed members instead of interfaces, `init` properties instead of transforming constructors, `[JsonIgnore]` or a setter for computed members, a list or a reference-equality owner instead of a hashed set).
- **Durable default key store and key scope (tenant, category)**: owned by #1191 and #1144 (SPEC-002), not by this issue.
- **#1699 as a prerequisite**: an ordering decision for the orchestrator, not a product decision.
