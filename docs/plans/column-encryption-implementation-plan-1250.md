# Implementation Plan: Column-Level Encryption at Rest — `[EncryptedField]` on the 10 Database Providers

> **Issue**: [#1250](https://github.com/dlrivada/Encina/issues/1250) (SPEC-002 REQ-055 and REQ-020, AC-039 and AC-020, tracking id P-46, priority P0; part of #1186; closes the orphan attribute #857)
> **Type**: Feature
> **Complexity**: Very high (key management with KMS wrapping, 10 providers, a key table, 2 KMS packages; about 11 pull requests)
> **Estimated Scope**: ~3,000-3,800 lines of production code + ~5,000-6,500 lines of tests
> **Shared with**: `storage-implementation-plan-1239.md` (#1239) (blob envelope encryption uses `IKeyWrapper` from this plan)

---

## Summary

Make `[EncryptedField]` do what its name says: a marked property is ciphertext in the table and plaintext through the application. Encryption is AES-GCM from the .NET BCL. The data keys are generated locally, wrapped by a KMS key per tenant, stored wrapped, and kept unwrapped only in a bounded in-process cache. Rotation re-wraps data keys without re-encrypting data. Queries on an encrypted column are equality-only through an optional HMAC blind index, or not supported (documented). The mechanism is delivered for all 10 database providers: EF Core (3) with value converters, Dapper (3) and ADO.NET (3) through the entity mapping layer, and MongoDB with a member serializer.

### Shared names (this plan owns them; #1239 consumes the first two)

| Type | Package | Role |
|------|---------|------|
| `IKeyWrapper`, `WrappedKey` | `Encina.Security.Encryption` | Wrap and unwrap a data key under a KMS key (used by columns here, by blobs in #1239) |
| `ITenantDataKeyService`, `DataKey`, `DataKeyPurpose` (`Column`, `BlindIndex`) | `Encina.Security.Encryption` | Current and historical data keys per tenant and purpose; rotation |
| `IWrappedKeyStore`, `WrappedKeyRecord` | `Encina.Security.Encryption` (contract), 10 provider stores | Persistence of wrapped keys with atomic first-key creation |
| `IColumnCipher`, `ColumnContext`, `IColumnKeyCache`, `ColumnKeyWarmupService` | `Encina.Security.Encryption` | Synchronous encrypt and decrypt for converters and serializers |
| `EncryptedFieldAttribute` (moved) | `Encina.Security.Encryption` | The marker (Design Choice 4) |

### What the code does today (2026-11-11, `main` at `7a0049c07`)

1. **The marker is an orphan.** `EncryptedFieldAttribute` is defined in `src/Encina.Messaging.Encryption/Attributes/EncryptedFieldAttribute.cs` for message fields, and no code reads it (`Select-String` over `src/` finds the definition only). #857 tracks it as an orphan attribute (open).
2. **The existing encryption is payload-level.** `EncryptionPipelineBehavior` encrypts request and response properties marked `[Encrypt]` (`src/Encina.Security.Encryption/EncryptionPipelineBehavior.cs`), and `AesGcmFieldEncryptor` (`Algorithms/AesGcmFieldEncryptor.cs`, internal, 12-byte nonce, `ValueTask<Either<...>>` API over `IFieldEncryptor`) encrypts values. Nothing touches a table column.
3. **The existing KMS key providers cannot serve stored data.** `AwsKmsKeyProvider.GetKeyAsync` calls `GenerateDataKey` and returns the plaintext of a fresh data key on every call, discarding the encrypted copy (`src/Encina.Messaging.Encryption.AwsKms/AwsKmsKeyProvider.cs`); the Azure Key Vault provider wraps a fresh local key on each call in the same way. Anything encrypted with such a key cannot be decrypted later. That is the open bug #2386 for message encryption; for columns it would be silent data loss, so this plan does not reuse `IKeyProvider` and adds `IKeyWrapper` (wrap and unwrap of a given key), which the fix of #2386 can also use.
4. **Encryption APIs are asynchronous, ORM hooks are synchronous.** `IFieldEncryptor`, `IKeyProvider` and the KMS clients return `ValueTask`. EF Core `ValueConverter`, Dapper `SqlMapper.TypeHandler` and the MongoDB `IBsonSerializer` are synchronous. Design Choice 1 resolves it.
5. **Tenant context exists.** `ITenantProvider.GetCurrentTenantId()` returns null when no context is available (`src/Encina.Tenancy/Abstractions/ITenantProvider.cs`), and `MultiTenancyMarker` (`src/Encina/Abstractions/MultiTenancyMarker.cs`) signals that tenancy is on so that readers fail closed. `ITenantKeyResolver` (`src/Encina.Messaging.Encryption/Abstractions/ITenantKeyResolver.cs`) maps a tenant to a KMS key id.
6. **Per-provider mapping hooks exist.** EF Core has value converters (`src/Encina.EntityFrameworkCore/Converters/`), Dapper has type handlers (`src/Encina.Dapper.*/TypeHandlers/`) and an entity mapping layer (`Repository/EntityMappingBuilder.cs`, `IEntityMapping.cs`, `FunctionalRepositoryDapper.cs`), MongoDB has serialization conventions. Dapper and ADO.NET type handlers select by CLR type, not by property, which shapes Design Choice 5.
7. **No range exists for column encryption.** `Encina.Security.Encryption` registers no EventIds today; a new range is needed (Event ID table).

### Scope

- **Affected packages**: `Encina.Security.Encryption` (core), new `Encina.Security.Encryption.AwsKms` and `Encina.Security.Encryption.AzureKeyVault` (Design Choice 3), `Encina.Messaging.Encryption` (the attribute leaves it), `Encina.EntityFrameworkCore`, `Encina.Dapper.{SqlServer,PostgreSQL,MySQL}`, `Encina.ADO.{SqlServer,PostgreSQL,MySQL}`, `Encina.MongoDB`, `Encina.Testing.*` fakes where needed.
- **Provider category**: Database, all 10 providers (AGENTS.md §5). SQLite is out of the matrix (ADR-024).
- **Out of scope**: encryption of existing plaintext data (a migration helper is forbidden pre-1.0 by AGENTS.md §3; the guide documents re-save); searchable encryption beyond equality; the fix of #2386 (its own issue, but it should use `IKeyWrapper`); key management UI.
- **Estimated files**: ~55 production files, ~70 test files, ~20 documentation, script and manifest files. About 11 pull requests (Phase 12).

---

## Design Choices

<details>
<summary><strong>1. Synchronous ORM hooks over asynchronous KMS — a warmed synchronous key cache, blocking, or asynchronous interceptors</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Converters and serializers call a synchronous `IColumnCipher` that reads unwrapped data keys from a bounded in-process cache; asynchronous hooks fill the cache before use (`ColumnKeyWarmupService` at start-up for known tenants, and an `EnsureKeysAsync` call from the EF `SaveChangesAsync`/query interceptor and the Encina repositories' async paths)** | No blocking on the KMS in the hot path; one code path for all providers; the KMS is called once per key version per host, not per row | A cache miss in a purely synchronous path (a hand-written Dapper query for a tenant never warmed) cannot fetch the key: it returns a typed failure (an exception at the ORM boundary, where `Either` cannot be returned), never plaintext |
| **B) Blocking on the KMS call (`GetAwaiter().GetResult()`) at a cache miss** | Always works | Sync-over-async on the thread pool (starvation under load); the same class of defect as the synchronous database calls AGENTS.md §3 forbids |
| **C) No converters: EF `SaveChangesInterceptor` and materialization hooks doing asynchronous crypto, and entity-level protectors elsewhere** | Fully asynchronous | EF materialization has no asynchronous hook, so reads would need a second pass over tracked entities; queries that project columns directly would bypass it; three implementations of the crypto call sites |

### Chosen Option: **A — warmed synchronous cache plus asynchronous ensure hooks** (recommended, pending the maintainer)

### Rationale

**Class:** Dilemma. Option A wins in services that run in request scopes with a known tenant set and need predictable latency. Option B wins only for a rarely used batch tool that cannot be warmed and where a few blocked threads are acceptable. Option C wins if a team forbids any ambient cache of key material.

- We recommend A because the KMS is the slow, rate-limited dependency; unwrapping per row is not viable, and blocking violates the async rule.
- The cache holds unwrapped keys for `KeyCacheDuration` (default 15 minutes, absolute) and is in-process only: key material is never put in `ICacheProvider` or a distributed cache. Buffers are zeroed on eviction (`CryptographicOperations.ZeroMemory`).
- A miss throws `ColumnEncryptionKeyUnavailableException` at the ORM boundary (the only place in this feature where an exception is unavoidable, because converters return values); the exception carries the error code and tenant id only, and the pipeline above converts it to `Left` where it can. Plaintext is never written as a fallback (fail closed, AGENTS.md §3).

</details>

<details>
<summary><strong>2. Where wrapped data keys are stored — database tables on the 10 providers, or the secrets store</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A key table in the application database (`IWrappedKeyStore` with a store per provider: ADO.NET 3, Dapper 3, EF Core 3, MongoDB 1), unique on (tenant, purpose, version)** | First-key creation is atomic through the unique constraint, so two hosts starting together cannot create two current keys; keys move with backups; no extra service | Ten stores and four DDL variants; the table lives next to the data it protects (the wrapped key is useless without the KMS, so this is acceptable) |
| **B) The secrets store (`ISecretWriter` of `Encina.Security.Secrets`: Azure Key Vault, AWS Secrets Manager, Vault, Google)** | No new tables; keys are in a separate trust domain | No portable create-if-absent, so first-key creation needs a distributed lock (function 7) and still races across regions; applications with no secrets manager cannot use the feature; no cross-provider transaction with the data |
| **C) The wrapped key travels inside every ciphertext (no store)** | No store at all | Rotation would have to rewrite every row, which contradicts "rotation re-wraps without re-encrypting data" |

### Chosen Option: **A — `IWrappedKeyStore` with a database store per provider** (recommended, pending the maintainer)

### Rationale

**Class:** Dilemma. Option A wins when atomic key creation and offline-consistent backups matter, which is the default for the reference application on PostgreSQL. Option B wins when the organization requires all key material in a dedicated secrets manager and accepts a lock for first-key creation.

- We recommend A because the correctness property (one current key per tenant and purpose) is guaranteed by a database constraint instead of by a lock, and the database stores must exist for the 10 providers anyway for the converters' integration tests.
- Table `EncinaDataKeys` (`TenantId`, `Purpose`, `Version`, `KekId`, `WrappedKey`, `Algorithm`, `State` (`Current`, `Retired`), `CreatedAtUtc`, `RetiredAtUtc`). Store names follow AGENTS.md §4: `WrappedKeyStoreADO`, `WrappedKeyStoreDapper`, `WrappedKeyStoreEF`, `WrappedKeyStoreMongoDB`, in a `ColumnEncryption/` folder of each provider package (named after the feature, AGENTS.md §4).
- `IWrappedKeyStore` is an abstraction, so a secrets-store implementation can be added later without changing the service.

</details>

<details>
<summary><strong>3. Home of the KMS wrappers — the existing message-encryption satellites, or new packages</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Add `AwsKmsKeyWrapper` and `AzureKeyVaultKeyWrapper` to `Encina.Messaging.Encryption.AwsKms` and `.AzureKeyVault`** | Reuses the existing KMS client wiring, options and README | Column encryption users would reference messaging encryption packages (and through them the messaging stack); the names mislead |
| **B) New packages `Encina.Security.Encryption.AwsKms` and `Encina.Security.Encryption.AzureKeyVault`** | Pay-for-what-you-use at package level; names match the feature; the message packages can later depend on the same wrapper to fix #2386 | Two more packages (csproj, README, manifest, options, validators) |

### Chosen Option: **B — new `Encina.Security.Encryption.AwsKms` and `.AzureKeyVault`** (recommended, pending the maintainer)

### Rationale

**Class:** Dilemma. Option B wins when column encryption is adopted without messaging. Option A wins when a team already runs the message encryption satellites and wants the smallest change.

- We recommend B because a provider package must not drag an unrelated pattern into its dependency graph (AGENTS.md §1, pay-for-what-you-use).
- Cloud triangle (AGENTS.md §5): AWS and Azure are covered; Google Cloud KMS is post-1.0 (SPEC-000 §6.1) and is noted as the gap. A `LocalKeyWrapper` (a configured master key, enabled only by an explicit development option and refused outside Development) serves tests and local runs.
- Integration tests: AWS KMS against LocalStack (`LocalStackFixture`, collection `Cloud-LocalStack`); Azure Key Vault keys through a fake `CryptographyClient`, because no emulator for Key Vault keys exists.

</details>

<details>
<summary><strong>4. Home of the marker — <code>Encina.Security.Encryption</code></strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Move `EncryptedFieldAttribute` to `Encina.Security.Encryption`** | Every provider package already may reference it without taking the messaging stack; one marker for messages and columns (`Encina.Messaging.Encryption` already depends on `Encina.Security.Encryption`) | Breaking move of a public type; pre-1.0 allows it |
| **B) Keep it in `Encina.Messaging.Encryption`** | No move | Provider packages would reference the messaging encryption package |

### Chosen Option: **A — move to `Encina.Security.Encryption`**

### Rationale

**Class:** Trivial. **Settled by:** AGENTS.md §3 (abstractions live in the lowest package that all consumers reference; provider packages never reference unrelated patterns) and AGENTS.md §1 (pre-1.0: change it completely, no aliases); acceptance criterion AC-020 of #1250 (the attribute is consumed as the marker).

- New members: `bool BlindIndex` (default false) and `string? Purpose`. The XML docs of the old message wording are rewritten; the message-level meaning stays valid because the same attribute still marks message fields.
- #857 closes with the pull request that adds the first consumer (Phase 6).

</details>

<details>
<summary><strong>5. Dapper and ADO.NET — the entity mapping layer applies the cipher; type handlers cannot select a property</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A protector in the entity mapping layer: `EntityMappingBuilder` records `[EncryptedField]` columns, and `FunctionalRepositoryDapper` (and the ADO.NET repositories) call `IColumnCipher` on those columns when binding parameters and mapping rows; a public `ColumnProtector<T>` serves hand-written queries** | The marker stays the single declaration; works for string and byte properties; no wrapper types in the domain model | Hand-written Dapper or ADO.NET code outside the repositories calls the protector itself |
| **B) A wrapper type `EncryptedString` with a Dapper `TypeHandler<EncryptedString>` and ADO.NET parameter helpers** | Fits Dapper's type-based handler model; works in any query | The marker is unused outside EF Core and MongoDB; entities change shape (`EncryptedString` instead of `string`), which the issue and AC-020 do not ask for |

### Chosen Option: **A — protector in the entity mapping layer, with a public helper for custom queries** (recommended, pending the maintainer)

### Rationale

**Class:** Dilemma. Option A wins when the entities keep plain properties and queries go through Encina repositories. Option B wins in a codebase that hand-writes most Dapper SQL and prefers explicit encrypted types.

- We recommend A because Dapper's `SqlMapper.AddTypeHandler` is keyed by CLR type, so a `string` handler would encrypt every string; the property-level marker needs the mapping layer.
- The EF Core and MongoDB paths are property-level natively (converter by property, member serializer by member).

</details>

<details>
<summary><strong>6. Ciphertext format, blind index and tenancy</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Binary value `0x01 | keyVersion(4, big endian) | nonce(12) | tag(16) | ciphertext`; AAD = tenant, table and column; blind index = HMAC-SHA256 under a separate `BlindIndex` key, stored in a shadow column `<Name>_bidx`; tenant from `ITenantProvider`, fail closed under `MultiTenancyMarker`, fixed default tenant when tenancy is off** | Key version in the value makes rotation and decrypt-by-version possible; AAD stops swapping ciphertext between columns or tenants; no new primitive | Binary columns (`varbinary(max)`, `bytea`, `LONGBLOB`, BSON binary); a tenant switch needs a re-encryption |
| **B) Text value (base64) in the original string column** | No column type change | 33% larger; the column keeps `NVARCHAR` limits; ambiguity with plaintext rows |

### Chosen Option: **A — binary format with AAD, HMAC blind index, tenant-bound keys**

### Rationale

**Class:** Settled. **Settled by:** the body and the maintainer's comment of 2026-10-17 on #1250 (AES-GCM from the .NET BCL, KMS-wrapped per-tenant keys, equality-only queries through an optional blind index otherwise unsupported and documented, "settled by elimination"); SPEC-002 REQ-055 and REQ-061 / AC-043 (a key of tenant A cannot decrypt tenant B's data; with tenancy off a fixed default tenant applies); AGENTS.md §3 (security gates fail closed; `MultiTenancyMarker` is the repository's signal).

- The 12-byte nonce and the tag layout follow `AesGcmFieldEncryptor`.
- Blind index scope: equality on the exact value, `Where(x => x.Field == value)` rewritten by an EF extension (`WhereEncryptedEquals`) and a documented helper for Dapper, ADO.NET and MongoDB. Range, `LIKE` and ordering are unsupported and the guide says so.

</details>

---

## Implementation Phases

Pull request cut (each at most 15 files and 1500 lines): PR 1 = Phase 1; PR 2 = Phases 2 and 4; PR 3 = Phase 3; PR 4 = key stores for the three ADO.NET providers; PR 5 = key stores for Dapper; PR 6 = key stores and converter for EF Core (Phase 6); PR 7 = MongoDB (Phase 9) ; PR 8 = Dapper protector (Phase 7); PR 9 = ADO.NET protector (Phase 8); PR 10 = Phases 10-11; PR 11 = Phase 13. Phase 5 is the key-store part of PRs 4-7.

### Phase 1: Core cipher, contracts and models

> **Goal**: The synchronous crypto core and the contracts, with no I/O.

<details>
<summary><strong>Tasks</strong></summary>

1. **Move `EncryptedFieldAttribute`** to `src/Encina.Security.Encryption/Attributes/` (namespace `Encina.Security.Encryption`), add `BlindIndex` and `Purpose`; update `Encina.Messaging.Encryption` references.
2. **`IKeyWrapper.cs`**: `ValueTask<Either<EncinaError, WrappedKey>> WrapAsync(ReadOnlyMemory<byte> dataKey, string kekId, CancellationToken)` and `ValueTask<Either<EncinaError, byte[]>> UnwrapAsync(WrappedKey wrapped, CancellationToken)`; `WrappedKey(string KekId, ImmutableArray<byte> Bytes, string Algorithm)`.
3. **`IColumnCipher.cs`, `ColumnCipher.cs`**: `byte[] Encrypt(ReadOnlySpan<byte> plaintext, in ColumnContext context)` and `byte[]? Decrypt(ReadOnlySpan<byte> value, in ColumnContext context)`; `ColumnContext(string TenantId, string Table, string Column)`; uses `AesGcm` and the format of Design Choice 6; reads keys from `IColumnKeyCache`; throws `ColumnEncryptionKeyUnavailableException` (code `encryption.column_key_unavailable`) on a miss and `ColumnEncryptionException` (code `encryption.column_decrypt_failed`) on a tag mismatch.
4. **`BlindIndex.cs`**: `byte[] Compute(ReadOnlySpan<byte> value, in ColumnContext context)` (HMAC-SHA256, key purpose `BlindIndex`).
5. **`ColumnEncryptionErrors.cs`, `ColumnEncryptionOptions.cs`** (`DefaultTenantId`, `KeyCacheDuration`, `KekIdTemplate`, `AllowLocalKeyWrapper`).
6. `PublicAPI.Unshipped.txt`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of issue #1250 in Encina: the synchronous column cipher and contracts.

CONTEXT:
- Package Encina.Security.Encryption (src/Encina.Security.Encryption/). AesGcmFieldEncryptor (Algorithms/AesGcmFieldEncryptor.cs) is the
  reference for AES-GCM usage and the 12-byte nonce; it is internal and asynchronous, so do not reuse it.
- EncryptedFieldAttribute currently lives in src/Encina.Messaging.Encryption/Attributes/EncryptedFieldAttribute.cs and is unused.
- Value format: 0x01 | keyVersion(4 BE) | nonce(12) | tag(16) | ciphertext; AAD = UTF-8(tenant NUL table NUL column).

TASK:
Move the attribute, create IKeyWrapper/WrappedKey, IColumnCipher/ColumnCipher/ColumnContext, BlindIndex, the options and the errors,
with unit, guard and property tests (round trip, nonce uniqueness per write, tampered tag, wrong tenant/table/column AAD, wrong key version).

KEY RULES:
- The cipher is synchronous and allocation-conscious; key buffers are zeroed with CryptographicOperations.ZeroMemory when evicted.
- Never fall back to plaintext on any failure; throw typed exceptions that carry the error code and tenant only.
- No EncinaError.Message or plaintext in logs, exceptions or activity tags.
- XML docs on all public APIs; PublicAPI.Unshipped.txt; zero warnings.

REFERENCE FILES:
- src/Encina.Security.Encryption/Algorithms/AesGcmFieldEncryptor.cs
- src/Encina.Messaging.Encryption/Attributes/EncryptedFieldAttribute.cs
- docs/plans/column-encryption-implementation-plan-1250.md (Design Choices 1, 4 and 6)
```

</details>

---

### Phase 2: Key management and cache

> **Goal**: Tenant data keys are created, wrapped, stored, cached and rotated.

<details>
<summary><strong>Tasks</strong></summary>

1. **`IWrappedKeyStore.cs`**: `GetAsync(tenantId, purpose, version)`, `GetCurrentAsync(tenantId, purpose)`, `TryAddCurrentAsync(WrappedKeyRecord)` (returns the existing current record when the unique constraint is hit), `RetireAsync(tenantId, purpose, version)`, `ListAsync(tenantId, purpose)`; all `ValueTask<Either<EncinaError, ...>>`.
2. **`InMemoryWrappedKeyStore.cs`** (tests and development; the event-sourced no-InMemory rule of AGENTS.md §3 does not apply to this package).
3. **`TenantDataKeyService.cs : ITenantDataKeyService`**: `GetCurrentKeyAsync` reads the current record, or generates a 256-bit key with `RandomNumberGenerator`, wraps it through `IKeyWrapper` with `KekIdTemplate`, `TryAddCurrentAsync`, and unwraps; `GetKeyAsync(version)`; `RotateAsync` creates a new current version and retires the old one; `RewrapAsync(tenantId, newKekId)` re-wraps every record without touching data.
4. **`ColumnKeyCache.cs : IColumnKeyCache`** (`TryGet(tenantId, purpose, version, out DataKey)`, `Set`, absolute expiry from `TimeProvider`) and **`ColumnKeyWarmupService.cs`** (`IHostedService`: warms the current key of every tenant returned by `ITenantStore` plus the default tenant; a failure is logged by code and retried at the next refresh, never fatal for the host unless `FailStartupOnWarmupError` is set).
5. **`ColumnKeyEnsurer.cs`**: `ValueTask EnsureAsync(string tenantId, CancellationToken)` used by the provider hooks (Phase 6-9).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of issue #1250 in Encina: tenant data key management.

CONTEXT:
- Phase 1 created IKeyWrapper, IColumnCipher, ColumnContext and the options in src/Encina.Security.Encryption/.
- ITenantStore lives in src/Encina.Tenancy/Abstractions/ITenantStore.cs; ITenantProvider and MultiTenancyMarker decide the tenant.
- The first current key of a tenant must be created atomically: the store's unique constraint decides the winner, the loser reads it.

TASK:
Create IWrappedKeyStore, InMemoryWrappedKeyStore, TenantDataKeyService (current/get/rotate/rewrap), ColumnKeyCache,
ColumnKeyWarmupService and ColumnKeyEnsurer, with unit tests using FakeTimeProvider and a fake IKeyWrapper (concurrent first-key
creation yields one current key; rotation keeps old versions readable; rewrap changes KekId only).

KEY RULES:
- Time from TimeProvider; all I/O async with CancellationToken; Either results.
- Key material only in the in-process cache, never in ICacheProvider; zero buffers on eviction.
- The warm-up service never lets an exception escape its loop or startup; failures are logged by error code.
- Tenancy off: use ColumnEncryptionOptions.DefaultTenantId; tenancy on and no tenant: Left(encryption.tenant_required).

REFERENCE FILES:
- src/Encina.Tenancy/Abstractions/ITenantStore.cs, src/Encina/Abstractions/MultiTenancyMarker.cs
- src/Encina.Security.Encryption/InMemoryKeyProvider.cs
- src/Encina.Security.Secrets/Rotation/SecretRotationCoordinator.cs
```

</details>

---

### Phase 3: KMS wrappers (AWS KMS and Azure Key Vault)

> **Goal**: Real KMS wrapping on the cloud triangle.

<details>
<summary><strong>Tasks</strong></summary>

1. **`src/Encina.Security.Encryption.AwsKms/`**: `AwsKmsKeyWrapper : IKeyWrapper` using `Encrypt` and `Decrypt` of the KMS client (data key sizes are well under the 4096-byte limit), `AwsKmsWrapperOptions` (region, endpoint override for LocalStack; no secrets in options, credentials come from the SDK chain), validator, `AddEncinaColumnEncryptionAwsKms()`, log messages, README, PublicAPI.
2. **`src/Encina.Security.Encryption.AzureKeyVault/`**: `AzureKeyVaultKeyWrapper` using `CryptographyClient.WrapKeyAsync` / `UnwrapKeyAsync` (`RsaOaep256`), options, validator, DI, README.
3. **`LocalKeyWrapper`** in the core package for development and tests; refuses to start unless `AllowLocalKeyWrapper` is set and the environment is Development.
4. Dependencies: `AWSSDK.KeyManagementService` and `Azure.Security.KeyVault.Keys` are already in `Directory.Packages.props`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are implementing Phase 3 of issue #1250 in Encina: the AWS KMS and Azure Key Vault key wrappers.

CONTEXT:
- IKeyWrapper (Phase 1) wraps a given data key; the existing providers in src/Encina.Messaging.Encryption.AwsKms and .AzureKeyVault
  generate a fresh key on every call (bug #2386) and must NOT be reused.
- Fixtures: LocalStackFixture (collection Cloud-LocalStack). Key Vault keys have no emulator: fake CryptographyClient.

TASK:
Create the two packages and LocalKeyWrapper with options validators, DI (TryAdd), log messages, READMEs, contract tests that wrap then
unwrap through each wrapper, and the LocalStack integration test for AWS KMS.

KEY RULES:
- Wrap and unwrap the SAME key bytes; a test proves unwrap(wrap(k)) == k and that two wraps of one key differ only in the wrapped bytes.
- Options hold no secret; if one is added it is [JsonIgnore] with a ToString() override.
- Provider failures return Left with encryption.* codes; log exception type via ForLogging(), never the message.
- Register the assemblies in EncinaEventIdAllocationTests if they log (SecurityColumnEncryption range).

REFERENCE FILES:
- src/Encina.Messaging.Encryption.AwsKms/AwsKmsKeyProvider.cs (client wiring only)
- src/Encina.Security.Secrets.AzureKeyVault/ (package layout, options validator)
- tests/Encina.TestInfrastructure/Fixtures/LocalStackFixture.cs
```

</details>

---

### Phase 4: Configuration, DI and health

> **Goal**: `AddEncinaColumnEncryption` registers everything it resolves.

<details>
<summary><strong>Tasks</strong></summary>

1. **`ServiceCollectionExtensions.AddEncinaColumnEncryption(Action<ColumnEncryptionOptions>)`** in `Encina.Security.Encryption`: registers options (validated at start-up), `IColumnCipher`, `IColumnKeyCache`, `ITenantDataKeyService`, `ColumnKeyEnsurer`, `ColumnKeyWarmupService`, `InMemoryWrappedKeyStore` as a `TryAdd` default that **a provider store registered in any order replaces** (AGENTS.md §3), and the health check.
2. **`ColumnEncryptionHealthCheck`** (model: `EncryptionHealthCheck`, `DefaultName = "encina-column-encryption"`, tags `["encina", "encryption", "ready"]`): wraps and unwraps a probe key; unhealthy returns the error code only.
3. DI test with `ValidateOnBuild` and `ValidateScopes`, store registered before and after.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
You are implementing Phase 4 of issue #1250 in Encina: registration and the health check for column encryption.

CONTEXT:
- Existing registration: src/Encina.Security.Encryption/ServiceCollectionExtensions.cs; health check: Health/EncryptionHealthCheck.cs.
- AGENTS.md §3: an AddEncina* method registers every option type and dependency it resolves; a database store wins over the in-memory
  default in any registration order without overriding the application's own registration.

TASK:
Add AddEncinaColumnEncryption, the options validation, the health check and the DI tests.

KEY RULES:
- TryAdd for defaults; the in-memory key store is replaced by a provider store regardless of order.
- Production start-up fails when only the in-memory store or LocalKeyWrapper is registered outside Development.
- Health results carry codes only.

REFERENCE FILES:
- src/Encina.Security.Encryption/ServiceCollectionExtensions.cs
- src/Encina.Security.Encryption/Health/EncryptionHealthCheck.cs
- src/Encina.Security.Secrets/Health/SecretsHealthCheck.cs
```

</details>

---

### Phase 5: Wrapped key stores and scripts (10 providers)

> **Goal**: `IWrappedKeyStore` on every database provider, with a unique constraint.

<details>
<summary><strong>Tasks</strong></summary>

1. **ADO.NET** (`Encina.ADO.SqlServer`, `.PostgreSQL`, `.MySQL`): `ColumnEncryption/WrappedKeyStoreADO.cs`; scripts add table `EncinaDataKeys` with `UNIQUE (TenantId, Purpose, Version)` and a partial unique index on the current row (`WHERE State = 'Current'`; MySQL uses a generated column or an application-level check inside the transaction).
2. **Dapper** (3): `ColumnEncryption/WrappedKeyStoreDapper.cs`, same SQL as ADO.NET adapted to Dapper.
3. **EF Core** (`Encina.EntityFrameworkCore`): `ColumnEncryption/WrappedKeyStoreEF.cs` and `WrappedKeyRecordConfiguration` (SqlServer, PostgreSQL, MySQL share the model).
4. **MongoDB**: `ColumnEncryption/WrappedKeyStoreMongoDB.cs` with a unique compound index created by `MongoDbIndexCreator`.
5. SQL differences follow AGENTS.md §5: SQL Server `@param`, `bit`, native GUID; PostgreSQL case-sensitive identifiers, `true/false`; MySQL backticks, `0/1`. First-key creation uses `INSERT` and treats a duplicate-key error as "someone else won", then reads.
6. All calls async with a `CancellationToken` (`OpenAsync`, `ExecuteNonQueryAsync`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
You are implementing Phase 5 of issue #1250 in Encina: IWrappedKeyStore for one provider family (the pull request you were given).

CONTEXT:
- IWrappedKeyStore and WrappedKeyRecord are in src/Encina.Security.Encryption/ (Phase 2).
- Store naming (AGENTS.md §4): WrappedKeyStoreADO / WrappedKeyStoreDapper / WrappedKeyStoreEF / WrappedKeyStoreMongoDB, in a
  ColumnEncryption/ folder of the provider package. The satellite DI AddEncinaColumnEncryption{Provider}() uses TryAdd.
- Provider schema scripts live in each package's Scripts/ folder (see the outbox scripts as a model).

TASK:
Implement the store, the DDL and the DI for each provider of your family, with unit tests and integration tests on the shared
database collection fixtures (concurrent first-key creation from two tasks yields exactly one current key).

KEY RULES:
- Async with CancellationToken; never IDbConnection.Open(); Either results; duplicate key is not an error but a lost race.
- Use the family's shared [Collection("<Family>-<Database>")] fixture, ClearAllDataAsync in InitializeAsync; never a per-class fixture.
- Keep all 3 relational dialects consistent; the wrapped key bytes are binary columns (varbinary/bytea/LONGBLOB).

REFERENCE FILES:
- src/Encina.ADO.SqlServer/Outbox/OutboxStoreADO.cs and Scripts/
- src/Encina.EntityFrameworkCore/Outbox/OutboxStoreEF.cs
- src/Encina.MongoDB/Outbox/OutboxStoreMongoDB.cs and MongoDbIndexCreator.cs
```

</details>

---

### Phase 6: EF Core converters (3 providers)

> **Goal**: `[EncryptedField]` properties are ciphertext in SqlServer, PostgreSQL and MySQL through EF Core.

<details>
<summary><strong>Tasks</strong></summary>

1. **`Encina.EntityFrameworkCore/ColumnEncryption/EncryptedFieldConvention.cs`**: `modelBuilder.UseColumnEncryption(serviceProvider)` scans entity properties for `[EncryptedField]` and applies `EncryptedStringConverter` (string to `byte[]`), `EncryptedBytesConverter` and, for `BlindIndex = true`, a shadow property `<Name>_bidx` kept in sync by the interceptor.
2. **Converters** call `IColumnCipher` with `ColumnContext(tenant, table, column)`; the tenant is read from `ITenantProvider` at conversion time; the converter instance holds the cipher through the model's service provider, not a static.
3. **`ColumnEncryptionInterceptor`** (`SaveChangesInterceptor` and `DbCommandInterceptor`): `SavingChangesAsync` and `ReaderExecutingAsync` await `ColumnKeyEnsurer.EnsureAsync(tenant)` before any conversion runs.
4. **`WhereEncryptedEquals`** extension for blind-index equality queries.
5. #857 is closed by this pull request.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
You are implementing Phase 6 of issue #1250 in Encina: EF Core value converters for [EncryptedField].

CONTEXT:
- Converter pattern: src/Encina.EntityFrameworkCore/Converters/UuidV7IdConverter.cs. Converters are synchronous (Design Choice 1):
  the interceptor ensures keys asynchronously before conversion; IColumnCipher reads the warmed cache.
- Tenant: ITenantProvider.GetCurrentTenantId(); MultiTenancyMarker means fail closed; tenancy off uses ColumnEncryptionOptions.DefaultTenantId.
- Integration fixtures: EFCoreSqlServerFixture, EFCorePostgreSqlFixture, EFCoreMySqlFixture ([Collection("EFCore-...")]).

TASK:
Implement the convention, the converters, the interceptor and WhereEncryptedEquals, plus tests: ciphertext in the table and plaintext
through the application on the three EF Core providers (scenario S20), tenant A key cannot decrypt tenant B's row, ciphertext differs
per write, blind-index equality query, and a missing key fails closed (no plaintext written).

KEY RULES:
- Never write plaintext when a key is unavailable; throw the typed exception; the interceptor converts nothing silently.
- Columns map to binary types (varbinary(max), bytea, longblob); the model snapshot/migrations are the application's concern.
- EF Core only: no provider-specific SQL in the converter; per-provider differences stay in column type mapping.

REFERENCE FILES:
- src/Encina.EntityFrameworkCore/Converters/
- src/Encina.EntityFrameworkCore/Caching/QueryCacheInterceptor.cs (interceptor registration style)
- tests/Encina.IntegrationTests/EFCore/ (collections and fixtures)
```

</details>

---

### Phase 7: Dapper (3 providers)

> **Goal**: The Dapper repositories encrypt and decrypt marked columns; custom queries have a helper.

<details>
<summary><strong>Tasks</strong></summary>

1. **`Encina.Dapper.{SqlServer,PostgreSQL,MySQL}/ColumnEncryption/ColumnProtectorDapper.cs`**: reads `[EncryptedField]` properties once per entity type (compiled accessors), exposes `Protect(entity)`/`Unprotect(entity)` and parameter helpers.
2. **`EntityMappingBuilder` / `IEntityMapping`** record the encrypted columns; **`FunctionalRepositoryDapper`** applies the protector when it binds insert and update parameters and after it maps rows, after awaiting `ColumnKeyEnsurer`.
3. Public `ColumnProtector<T>` for hand-written queries; XML docs with an example.
4. Blind index: parameter helper `BlindIndexParameter(value)`; shadow column `<Name>_bidx` written by `Protect`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
You are implementing Phase 7 of issue #1250 in Encina: column encryption in the Dapper providers (SqlServer, PostgreSQL, MySQL).

CONTEXT:
- Dapper type handlers are keyed by CLR type, so they cannot select a property (Design Choice 5); the entity mapping layer
  (src/Encina.Dapper.SqlServer/Repository/EntityMappingBuilder.cs, IEntityMapping.cs, FunctionalRepositoryDapper.cs) is the hook.
- Fixtures: [Collection("Dapper-SqlServer")], "Dapper-PostgreSQL", "Dapper-MySQL" with SqlServerFixture, PostgreSqlFixture, MySqlFixture.

TASK:
Implement ColumnProtectorDapper per provider and the repository integration; tests per provider: ciphertext in the table, plaintext
through the repository, tenant isolation, missing key fails closed, blind-index equality parameter.

KEY RULES:
- The three provider packages stay consistent (same public API, same behavior; only SQL dialect differs).
- Async only; await ColumnKeyEnsurer before the synchronous protect/unprotect.
- No reflection on the hot path after the first call per type.

REFERENCE FILES:
- src/Encina.Dapper.SqlServer/Repository/FunctionalRepositoryDapper.cs
- src/Encina.Dapper.SqlServer/TypeHandlers/ (handler registration pattern)
- tests/Encina.IntegrationTests/Dapper/
```

</details>

---

### Phase 8: ADO.NET (3 providers)

> **Goal**: The ADO.NET repositories and helpers encrypt marked columns.

<details>
<summary><strong>Tasks</strong></summary>

1. **`Encina.ADO.{SqlServer,PostgreSQL,MySQL}/ColumnEncryption/ColumnProtectorADO.cs`**: the same protector as Phase 7, with `DbParameter` and `DbDataReader` helpers (`AddEncrypted`, `GetEncryptedString(reader, ordinal)`).
2. The ADO.NET repositories apply it through the same mapping metadata used by the Dapper phase (read the ADO repository classes first; if they have no mapping layer, only the public helpers ship and the repositories gain the call at their bind and read sites).
3. Tests per provider as in Phase 7.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```
You are implementing Phase 8 of issue #1250 in Encina: column encryption in the ADO.NET providers (SqlServer, PostgreSQL, MySQL).

CONTEXT:
- Phase 7 created the protector shape for Dapper; ADO.NET has no ORM, so the protector offers DbParameter and DbDataReader helpers.
- Fixtures: [Collection("ADO-SqlServer")], "ADO-PostgreSQL", "ADO-MySQL".

TASK:
Implement ColumnProtectorADO per provider, apply it in the ADO repositories where they bind and read entity columns, and add
unit, guard, contract and integration tests mirroring Phase 7.

KEY RULES:
- Async DB calls with CancellationToken (OpenAsync, ExecuteReaderAsync); never IDbConnection.Open().
- Same public API and behavior across the three providers; dialect differences only in SQL.
- Fail closed; no plaintext fallback.

REFERENCE FILES:
- src/Encina.ADO.SqlServer/ (repository classes and Outbox/OutboxStoreADO.cs for the ADO style)
- docs/plans/column-encryption-implementation-plan-1250.md (Phase 7)
```

</details>

---

### Phase 9: MongoDB

> **Goal**: Marked members are BSON binary in the collection and plaintext in the application.

<details>
<summary><strong>Tasks</strong></summary>

1. **`Encina.MongoDB/ColumnEncryption/EncryptedFieldSerializer.cs`** (`IBsonSerializer<string>` and bytes variant) calling `IColumnCipher`; tenant and table (collection) names from the serialization context.
2. **`EncryptedFieldConventionPack`** registered by `AddEncinaColumnEncryptionMongoDB()`: member serializer for `[EncryptedField]` members; blind index stored in `<name>_bidx`.
3. **Ensure hook**: the Encina MongoDB repository awaits `ColumnKeyEnsurer` before operations; `MongoDbIndexCreator` creates the index on `<name>_bidx`.
4. Tests: S20 on MongoDB (ciphertext as BinData in the document), tenant isolation, fail closed.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 9</strong></summary>

```
You are implementing Phase 9 of issue #1250 in Encina: column encryption for MongoDB.

CONTEXT:
- Package Encina.MongoDB (src/Encina.MongoDB/); index creation: MongoDbIndexCreator.cs; fixture: MongoDbFixture (shared collection).
- BSON serializers are synchronous (Design Choice 1); keys are ensured asynchronously beforehand.

TASK:
Implement the serializer, the convention pack, DI, the ensure hook and the tests described.

KEY RULES:
- Register the convention pack once and idempotently; no global mutable state beyond that.
- Fail closed; no plaintext fallback; typed exceptions with code and tenant only.
- Shared [Collection] fixture; ClearAllDataAsync in InitializeAsync.

REFERENCE FILES:
- src/Encina.MongoDB/MongoDbIndexCreator.cs
- src/Encina.MongoDB/Outbox/OutboxStoreMongoDB.cs
```

</details>

---

### Phase 10: Cross-cutting integration

> **Goal**: The matrix items marked included.

<details>
<summary><strong>Tasks</strong></summary>

1. **Caching**: the bounded in-process key cache (done in Phase 2); no `ICacheProvider` use for key material.
2. **Resilience**: `ResilientKeyWrapper` decorator (retry and circuit breaker on transient KMS errors, pattern `ResilientSecretReaderDecorator`), registered when `Encina.Resilience` is present (ADR-055).
3. **Validation**: options validated at start-up (Phase 4); the attribute is validated by the convention (a non-string, non-byte property is a model error naming the property).
4. **Tenancy**: Phases 1-2.
5. **Audit trail**: key creation, rotation, rewrap and unwrap failures are audited through `IOperationAuditStore` (`src/Encina.Security.Audit/Abstractions/IOperationAuditStore.cs`) as events with tenant, purpose, key version and outcome; never key material.
6. **Deferred**: distributed locks (not needed, Design Choice 2), idempotency, module isolation, transactions (matrix).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 10</strong></summary>

```
You are implementing Phase 10 of issue #1250 in Encina: resilience and audit for the key wrapper and key service.

CONTEXT:
- Resilience decorator model: src/Encina.Security.Secrets/Resilience/ResilientSecretReaderDecorator.cs; audit: IOperationAuditStore.
- Audit events never carry key material or plaintext; they carry tenant id, purpose, version and outcome.

TASK:
Add ResilientKeyWrapper and the audit recording in TenantDataKeyService, with tests for transient failures and for audit content.

KEY RULES:
- A Left is never swallowed; exhausted retries return the last Left; audit failures are logged by code and do not hide the operation outcome.
- Log exception types via ForLogging(); never EncinaError.Message.

REFERENCE FILES:
- src/Encina.Security.Secrets/Auditing/SecretAuditRecorder.cs
- docs/architecture/adr/055-resilience-family.md
```

</details>

---

### Phase 11: Observability

> **Goal**: Spans, metrics and logs with the tenant as an attribute and nothing sensitive.

<details>
<summary><strong>Tasks</strong></summary>

1. **EventIds**: register `public static readonly (int Min, int Max) SecurityColumnEncryption = (6200, 6249);` in `EventIdRanges.cs` and add the assemblies (`Encina.Security.Encryption`, `.AwsKms`, `.AzureKeyVault`) to `EncinaEventIdAllocationTests`; `Diagnostics/ColumnEncryptionLogMessages.cs` packed from 6200.
2. **Meter/ActivitySource** (existing source `Encina.Security.Encryption` in `EncryptionDiagnostics.cs`): counters `encina.encryption.operations` (tags: provider family, operation), `encina.encryption.unwrap_failures`; span `encina.encryption.unwrap_key` with `encina.tenant_id`; no key ids that reveal secrets, no plaintext.
3. **Test**: an in-memory exporter test asserting the tenant attribute and the absence of plaintext, key material and data subject identifiers in tags, metrics and logs (AC-044).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 11</strong></summary>

```
You are implementing Phase 11 of issue #1250 in Encina: observability for column encryption.

CONTEXT:
- ADR-021 / AGENTS.md §7: register the range first; add the assemblies to EncinaEventIdAllocationTests; pack IDs sequentially.
- Existing diagnostics: src/Encina.Security.Encryption/Diagnostics/EncryptionDiagnostics.cs.

TASK:
Register SecurityColumnEncryption 6200-6249, write the log messages, counters and spans, wire them in the key service and the cipher
(cache hit/miss counters only, never per-row spans), and add the exporter test.

KEY RULES:
- No per-row activities (hot path); counters only for rows, spans only for KMS calls.
- Bounded tag values; no plaintext, key material, key ids of secrets or subject identifiers.

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

---

### Phase 12: Testing

> **Goal**: Every flag at its target on each package, 10 providers covered.

<details>
<summary><strong>Tasks</strong></summary>

| Flag | Scope | Per-file target and justification |
|------|-------|-----------------------------------|
| Unit | cipher, key service, cache, warm-up, wrappers, protectors, converters, serializers | 90 for `ColumnCipher`, `TenantDataKeyService`, `ColumnKeyCache`: pure logic with fakes and `FakeTimeProvider`; 80 for provider glue |
| Guard | every public method with parameters in all new types | 90 |
| Contract | `IWrappedKeyStore` across the 10 stores; `IKeyWrapper` across AWS, Azure, local; same round-trip contract on every provider | 85: real implementations |
| Property | encrypt-decrypt round trip, ciphertext differs per write, AAD mismatch always fails, blind index deterministic per key | 75: invariants of the crypto core |
| Integration | 10 providers: ciphertext in the table, plaintext through the application (S20), tenant isolation, concurrent first-key creation, rotation then read of old rows; AWS KMS on LocalStack | real databases, shared `[Collection]` fixtures, never a `.md` justification |
| Benchmark | converter overhead per field (hot path on every read and write) | implemented with `BenchmarkSwitcher.FromAssembly`, results in `artifacts/performance/` |
| Load | `.md` justification: no concurrent state beyond the cache, covered by the concurrency integration test | |

- Update the manifests of `Encina.Security.Encryption`, the two new packages and the nine provider packages plus `Encina.MongoDB`; run the coverage report and the CRAP gate on changed methods (AGENTS.md §9).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 12</strong></summary>

```
You are implementing Phase 12 of issue #1250 in Encina: the complete test suite across 10 providers.

CONTEXT:
- Per-flag coverage is measured independently from .github/coverage-manifest/{Package}.json; tests must execute real package code.
- Scenario S20 of SPEC-002: ciphertext in the table and plaintext through the application.

TASK:
Complete the tests described in the table, the benchmark and the load justification, update the manifests and report measured
per-flag coverage and CRAP for the changed methods.

KEY RULES:
- All 10 providers have integration tests on shared [Collection] fixtures; never IClassFixture for database fixtures.
- No Thread.Sleep; no reflection-only tests; builders for test data; Shouldly through Encina.Testing.Shouldly.
- Benchmarks use BenchmarkSwitcher.FromAssembly and materialize results.

REFERENCE FILES:
- tests/Encina.IntegrationTests/**/Collections.cs
- docs/testing/coverage-measurement-methodology.md
```

</details>

---

### Phase 13: Documentation and finalization

> **Goal**: Everything the prompt lists, including the ADR.

<details>
<summary><strong>Tasks</strong></summary>

1. XML docs on all new public APIs; `PublicAPI.Unshipped.txt` for every touched package.
2. `changelog.d/1250-column-encryption.added.md` and a `changed` fragment for the moved attribute.
3. ADR (next free number; 066 on 2026-11-11 per the ADR registry of the plan batch; reserved in the ADR index when work starts): the synchronous-cache model, key table, wrapper packages, format.
4. Package READMEs (core update, two new); `docs/features/column-encryption.md` (usage, query limits of the blind index, rotation, re-saving existing plaintext, KMS setup); `docs/INVENTORY.md`; `ROADMAP.md`.
5. Close #857 with the Phase 6 pull request; open a follow-up for #2386 to use `IKeyWrapper`.
6. `dotnet build --configuration Release` with 0 warnings, `dotnet test`, every flag at target.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 13</strong></summary>

```
You are implementing Phase 13 of issue #1250 in Encina: documentation and finalization.

CONTEXT:
- Documentation is written by docs-writer; changelog fragments and PublicAPI lines by mechanical-fixer.

TASK:
Produce the documentation list, verify every identifier against src/, and run the build and the tests.

KEY RULES:
- Never edit CHANGELOG.md; coverage figures only through covref markers; English only; no emojis; no private client names.

REFERENCE FILES:
- .claude/skills/encina-docs/SKILL.md
- docs/architecture/adr/index.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Reference | Relevance |
|--------|-----------|-----------|
| GDPR | Art. 32 (security of processing: encryption of personal data) | Therapist notes and OAuth refresh tokens must be ciphertext at rest |
| SPEC-002 | REQ-055, REQ-020, AC-039, AC-020, scenario S20, DEC-011 (e) | Scope and acceptance |
| SPEC-002 | REQ-061 / AC-043 | Keys scoped per tenant; a key of tenant A cannot decrypt tenant B |
| SPEC-002 | REQ-062 / AC-044 | Instrumentation without plaintext or identifiers |
| NIST SP 800-38D | AES-GCM: 96-bit nonce, 128-bit tag, associated data | Format and AAD |
| NIST SP 800-57 | Key management: key hierarchy, rotation, wrapping | DEK and KEK model |
| Comparable products | SQL Server Always Encrypted, PostgreSQL pgcrypto, AWS Encryption SDK, Azure Key Vault wrap | Provider-specific alternatives rejected for provider coherence (AGENTS.md §3) |
| AGENTS.md §3, §5 | Fail closed, async DB calls, registration completeness, 10 database providers | Rules applied |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| IFieldEncryptor and AesGcmFieldEncryptor | src/Encina.Security.Encryption/Abstractions/IFieldEncryptor.cs, src/Encina.Security.Encryption/Algorithms/AesGcmFieldEncryptor.cs | AES-GCM primitive (12-byte nonce) to mirror |
| IKeyProvider | src/Encina.Security.Encryption/Abstractions/IKeyProvider.cs | key lookup contract; not enough for wrapped keys |
| AwsKmsKeyProvider and AzureKeyVaultKeyProvider | src/Encina.Messaging.Encryption.AwsKms/AwsKmsKeyProvider.cs, src/Encina.Messaging.Encryption.AzureKeyVault/AzureKeyVaultKeyProvider.cs | KMS client wiring only; they return a fresh data key on every call (#2386) |
| ITenantKeyResolver | src/Encina.Messaging.Encryption/Abstractions/ITenantKeyResolver.cs | tenant to KMS key id convention |
| EncryptedFieldAttribute (orphan, #857) | src/Encina.Messaging.Encryption/Attributes/EncryptedFieldAttribute.cs | the marker; moves to Encina.Security.Encryption |
| EncryptionHealthCheck | src/Encina.Security.Encryption/Health/EncryptionHealthCheck.cs | model for the KMS reachability check |
| Converters | src/Encina.EntityFrameworkCore/Converters/ | EF Core value converter pattern |
| TypeHandlers, EntityMappingBuilder, FunctionalRepositoryDapper | src/Encina.Dapper.SqlServer/TypeHandlers/, src/Encina.Dapper.SqlServer/Repository/ (also PostgreSQL and MySQL) | Dapper mapping hook |
| ITenantProvider, ITenantStore and MultiTenancyMarker | src/Encina.Tenancy/Abstractions/, src/Encina/Abstractions/MultiTenancyMarker.cs | per-tenant keys, fail closed, warm-up tenant list |
| MongoDbIndexCreator | src/Encina.MongoDB/MongoDbIndexCreator.cs | unique index for the key collection and the blind index |
| ResilientSecretReaderDecorator and SecretAuditRecorder | src/Encina.Security.Secrets/Resilience/, src/Encina.Security.Secrets/Auditing/ | models for the wrapper decorator and audit recording |
| IOperationAuditStore | src/Encina.Security.Audit/Abstractions/IOperationAuditStore.cs | audit of key lifecycle events |
| LocalStackFixture and shared database fixtures | tests/Encina.TestInfrastructure/Fixtures/, tests/Encina.IntegrationTests/**/Collections.cs | AWS KMS test; ciphertext-in-table tests on 10 providers |
| EventIdRanges | src/Encina/Diagnostics/EventIdRanges.cs | register SecurityColumnEncryption 6200-6249 first |
| Security.Encryption coverage manifest | .github/coverage-manifest/Encina.Security.Encryption.json | per-file targets |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Security.Encryption` and its two KMS packages | `SecurityColumnEncryption` 6200-6249 (new) | Core 6200-6224, AWS KMS 6225-6236, Azure Key Vault 6237-6249 |
| Provider packages (ADO, Dapper, EF Core, MongoDB) | existing ranges (3000-3499) | Key stores log nothing new; failures return `Left` |
| Family of plans | 6000-6049 claim check (#599, #122), 6050-6149 storage (#1239), 6150-6199 claim check providers, `SecuritySecrets` 5151-5170 (#1246) | Disjoint, in the family block 6000-6249 (registry eventid-blocks.md) |

### Open Questions to Verify During Implementation

| Question | How to verify |
|----------|---------------|
| Do the ADO.NET repositories have a mapping layer like Dapper's? | Read `src/Encina.ADO.*/Repository/`; if not, only the public helpers ship (Phase 8, task 2) |
| Can MySQL enforce "one current row" with a unique index? | MySQL has no partial indexes; use a generated column on `State = 'Current'` or a check inside the insert transaction; test with the `mysql:9.1` fixture |
| Does EF Core's model cache keep one converter instance per model (so the cipher must come from the service provider)? | Test two contexts with different tenants in one process |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| New production files in Encina.Security.Encryption (column cipher, key service, key wrapper, wrapped key store abstraction and in-memory store, blind index, options, errors, key cache, warm-up service) | 12 | |
| KMS wrapper implementations (AWS KMS, Azure Key Vault) | 2 | plus package scaffolding counted in documentation |
| Wrapped key store implementations: ADO.NET 3, Dapper 3, EF Core 3, MongoDB 1 | 10 | |
| Converters and handlers: EF Core 1 package-level converter plus convention, Dapper 3 protectors, ADO.NET 3 protectors, MongoDB 1 serializer and convention | 9 | |
| SQL scripts for the key table (SqlServer, PostgreSQL, MySQL, ADO and Dapper share) | 6 | |
| Diagnostics (log messages, activity source, metrics, health check) | 4 | |
| Modified existing files (EncryptedFieldAttribute move, EventIdRanges, EncinaEventIdAllocationTests, ServiceCollectionExtensions, PublicAPI.Unshipped.txt) | 8 | |
| Unit tests | 20 | |
| Guard tests | 6 | |
| Contract tests | 4 | |
| Property tests | 3 | |
| Integration tests | 10 | one per provider |
| Benchmark tests | 1 | |
| Load justification file | 1 | |
| Documentation | 7 | |
| Coverage manifest edits | 12 | |
| Total | 115 | Split over about 11 pull requests |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
PROJECT CONTEXT:
You are implementing issue #1250 in Encina (.NET 10, C# 14, nullable enabled, pre-1.0, no backward compatibility): column-level
encryption at rest for [EncryptedField] properties on the 10 database providers (SPEC-002 REQ-055, REQ-020). Rules: AGENTS.md.
The blob envelope encryption of issue #1239 consumes IKeyWrapper from this plan (docs/plans/storage-implementation-plan-1239.md).

IMPLEMENTATION OVERVIEW:
1. Move EncryptedFieldAttribute to Encina.Security.Encryption; IKeyWrapper, IColumnCipher (synchronous AES-GCM with AAD), BlindIndex.
2. Key management: IWrappedKeyStore, TenantDataKeyService (create, rotate, rewrap), in-process ColumnKeyCache, warm-up service.
3. KMS wrappers: Encina.Security.Encryption.AwsKms and .AzureKeyVault, LocalKeyWrapper for development.
4. AddEncinaColumnEncryption, options validation, health check.
5. Wrapped key stores on the 10 providers (table EncinaDataKeys with a unique constraint).
6. EF Core convention, converters, interceptor; Dapper and ADO.NET protectors through the mapping layer; MongoDB serializer and convention.
7. Resilience decorator, audit of key lifecycle, observability (SecurityColumnEncryption 6200-6249), tests per flag and provider, docs and ADR.

KEY PATTERNS:
- Synchronous ORM hooks read a warmed in-process key cache; asynchronous ensure hooks fill it; a miss throws a typed exception, never plaintext.
- Value format 0x01 | keyVersion | nonce(12) | tag(16) | ciphertext, AAD = tenant, table, column.
- Either<EncinaError, T> everywhere except the ORM boundary; no EncinaError.Message or key material in logs, tags or exceptions.
- Fail closed under MultiTenancyMarker; fixed default tenant when tenancy is off; TimeProvider; CancellationToken; no sync DB calls.
- Provider coherence: same public API and behavior on the 10 providers; shared [Collection] fixtures; per-flag coverage; CRAP <= 10.

REFERENCE FILES:
- src/Encina.Security.Encryption/Algorithms/AesGcmFieldEncryptor.cs
- src/Encina.EntityFrameworkCore/Converters/, src/Encina.Dapper.SqlServer/Repository/, src/Encina.MongoDB/MongoDbIndexCreator.cs
- src/Encina.Messaging.Encryption.AwsKms/AwsKmsKeyProvider.cs (what not to reuse)
- docs/plans/column-encryption-implementation-plan-1250.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ✅ | Phase 2: unwrapped data keys in a bounded in-process cache with absolute expiry; deliberately not `ICacheProvider` (key material must not reach a distributed cache) |
| 2 | OpenTelemetry | ✅ | Phase 11: counters for operations and unwrap failures, span for KMS unwrap with the tenant id; no per-row spans |
| 3 | Structured Logging | ✅ | Phase 11: EventIds 6200-6249 in the new `SecurityColumnEncryption` range, registered first (ADR-021) |
| 4 | Health Checks | ✅ | Phase 4: KMS reachability by wrapping and unwrapping a probe key |
| 5 | Validation | ✅ | Options validated at start-up; the convention rejects a marked property of an unsupported type |
| 6 | Resilience | ✅ | Phase 10: `ResilientKeyWrapper` retries transient KMS errors (ADR-055) |
| 7 | Distributed Locks | ❌ | First-key creation is atomic through the database unique constraint (Design Choice 2), so no lock |
| 8 | Transactions | ❌ | Each key record is a single-statement write; the data rows use the application's own transaction |
| 9 | Idempotency | ❌ | Not a message or request entry point |
| 10 | Multi-Tenancy | ✅ | Phases 1-2: keys per tenant, AAD binds tenant, fail closed, default tenant when tenancy is off (two-tenant tests, AC-043) |
| 11 | Module Isolation | ❌ | SPEC-002 requires no module scoping; `ModuleId` in messaging stays with #747 |
| 12 | Audit Trail | ✅ | Phase 10: key creation, rotation, rewrap and unwrap failures audited through `IOperationAuditStore` without key material |

---

## Prerequisites & Dependencies

### Advisable before or with this work

- #2386 (the KMS key providers return a fresh key on every call): independent of this plan, but its fix should use `IKeyWrapper`; do not build column encryption on those providers.
- #857 (orphan attributes): closed by Phase 6.
- #1257 (multi-tenancy of the SPEC-002 capabilities): the tenant-scoped keys of this plan are part of the same requirement (REQ-061); it supplies the tenant enumeration used by the warm-up if it is merged first; otherwise `ITenantStore` is used.

### Parallel and follow-up work

- `storage-implementation-plan-1239.md` (#1239): consumes `IKeyWrapper` (blob envelope encryption); merge Phase 1 and Phase 3 of this plan first.
- Crypto-shredding subject keys (REQ-006, #1191) are a separate key hierarchy and not part of this plan.
- Google Cloud KMS: post-1.0 (SPEC-000 §6.1), recorded as the cloud triangle gap.

---

## Next Steps

1. The maintainer answers the Dilemmas of this plan (choices 1, 2, 3 and 5); the orchestrator adds the Maintainer Decisions section.
2. Open the follow-up issue for #2386 to use `IKeyWrapper` (or comment on it) when work starts.
3. Link this plan from #1250; reserve the ADR number in `docs/architecture/adr/index.md`.
4. Implement the pull requests in the order of the cut above; PR 1 and PR 3 unblock the #1239 blob encryption.
