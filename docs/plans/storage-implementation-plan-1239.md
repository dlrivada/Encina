# Implementation Plan: `Encina.Storage` — `IBlobStore`, Envelope Encryption, Retention/Hold/Blocking Hooks and Providers

> **Issue**: [#1239](https://github.com/dlrivada/Encina/issues/1239) (SPEC-002 REQ-046, AC-039 and AC-043, scenario S11, tracking id P-37, priority P0; part of #1186)
> **Type**: Feature
> **Complexity**: Very high (new package family: core, file system, S3, Azure Blob, compliance adapters; 7 pull requests)
> **Estimated Scope**: ~2,200-2,800 lines of production code + ~3,500-4,500 lines of tests
> **Related plans**: [`column-encryption-implementation-plan-1250.md`](column-encryption-implementation-plan-1250.md) (owns `IKeyWrapper`), [`claim-check-implementation-plan-122.md`](claim-check-implementation-plan-122.md) (builds `Encina.ClaimCheck.Storage` on this package)

---

## Summary

Add a new package `Encina.Storage` with the file abstraction `IBlobStore` (put, get, delete, list by prefix, metadata), partitioned by tenant, with decorators for envelope encryption, audit, resilience and telemetry, and a lifecycle guard through which retention floors, legal holds and blocked state can refuse a delete or a read. The providers are the three of SPEC-002 DEC-016: the file system (in the core package), one S3-compatible provider (`Encina.Storage.S3`) and Azure Blob (`Encina.Storage.AzureBlob`). Google Cloud Storage is post-1.0 and is the recorded gap of the cloud triangle (SPEC-000 §6.1).

### Shared names (this plan owns them)

| Type | Package | Role |
|------|---------|------|
| `IBlobStore`, `BlobAddress`, `BlobInfo`, `BlobMetadata`, `BlobPutOptions`, `BlobContent`, `BlobPage` | `Encina.Storage` | The contract; consumed by `Encina.ClaimCheck.Storage` (#122 plan) |
| `IBlobLifecycleGuard`, `BlobOperation` | `Encina.Storage` | The hook for retention, hold and blocking |
| `Encina.Storage.Compliance` | new package | Adapters from the guard to `ILegalHoldService` now, `IRetentionFloorQuery` (#1187) and blocking (#1189) when they exist |
| Consumed from #1250 | `Encina.Security.Encryption` | `IKeyWrapper` and `WrappedKey` for the per-blob data key |

### What the code does today (2026-11-11, `main` at `7a0049c07`)

Nothing of #1239 exists: `src/` has no `Encina.Storage` package and no `IBlobStore` type. What exists and shapes the design:

1. **Cloud SDKs and fixtures.** `Azure.Storage.Blobs` 12.30.1 is in `Directory.Packages.props`; `AWSSDK.S3` is not (the SQS, SNS, KMS and Secrets Manager SDKs are). The test fixtures `AzuriteFixture` (Blob, Queue and Table services; collection `Cloud-Azurite`) and `LocalStackFixture` (collection `Cloud-LocalStack`) are in `tests/Encina.TestInfrastructure/Fixtures/`.
2. **Tenancy.** `ITenantProvider.GetCurrentTenantId()` returns null when there is no context (`src/Encina.Tenancy/Abstractions/ITenantProvider.cs`); `MultiTenancyMarker` (`src/Encina/Abstractions/MultiTenancyMarker.cs`) is the signal that tenancy is on so that readers fail closed. This is the fail-closed pattern of the dead-letter manager and the ABAC decision audit.
3. **Hold, retention and blocking.** `ILegalHoldService` exists (`src/Encina.Compliance.Retention/Abstractions/ILegalHoldService.cs`: `HasActiveHoldsAsync`, `GetActiveHoldsForEntityAsync`). The retention floor query `IRetentionFloorQuery` is planned by the #1187 plan and the blocked state (`IBlockedDataFilterContext`, audited disclosure) by the #1189 plan; neither exists in `src/` today.
4. **Audit.** The issue names `IAuditStore`; the interfaces in `src/Encina.Security.Audit/Abstractions/` are `IOperationAuditStore` and `IReadAuditStore` (disclosure reads). The plan uses those.
5. **Encryption primitives.** The existing KMS providers return a fresh key per call (#2386), so envelope encryption needs `IKeyWrapper` of the #1250 plan (wrap and unwrap a given key). `ITenantKeyResolver` (`src/Encina.Messaging.Encryption/Abstractions/ITenantKeyResolver.cs`) shows the tenant-to-key-id convention.
6. **No other blob store.** `#600` (cloud blob implementations of `IClaimCheckStore`) is open and the #122 plan builds the claim-check blob backends on this package, so `IBlobStore` is the single storage primitive.

### Scope

- **Affected packages**: new `Encina.Storage` (core and file system), `Encina.Storage.S3`, `Encina.Storage.AzureBlob`, `Encina.Storage.Compliance`; `Encina.Testing.Fakes` (`FakeBlobStore`); `Directory.Packages.props`, `Encina.slnx`.
- **Provider category**: Cloud (AWS and Azure; the file system is the local provider). Not a database feature; the 10-provider matrix does not apply. The category rule asks for the same contract on every storage provider: put, get, delete, list, metadata, tenant partition, guard and encryption decorators, health check.
- **Out of scope**: Google Cloud Storage (post-1.0); database BLOB storage (rejected in the issue); the key management (#1250); a content delivery or presigned URL feature.
- **Estimated files**: ~45 production files, ~55 test files, ~20 documentation and manifest files. Seven pull requests (Phase 11).

---

## Design Choices

<details>
<summary><strong>1. Package layout and providers — core with the file system, S3-compatible and Azure Blob satellites</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Encina.Storage` (abstractions, decorators, file system), `Encina.Storage.S3`, `Encina.Storage.AzureBlob`, `Encina.Storage.Compliance`** | One satellite per cloud SDK, as the other provider families (`Encina.Security.Secrets.*`); the file system needs no SDK so it lives in the core; applications pull only the SDK they use | Four packages |
| **B) One package with all three providers** | Fewer packages | Every adopter references both cloud SDKs; violates pay-for-what-you-use |

### Chosen Option: **A — core with the file system plus S3, Azure Blob and Compliance satellites**

### Rationale

**Class:** Settled. **Settled by:** SPEC-002 DEC-016 (a) (providers: the file system, one S3-compatible provider, Azure Blob) and the maintainer's comment of 2026-10-17 on #1239; AGENTS.md §1 (pay-for-what-you-use) and §5 (cloud: AWS and Azure; GCP post-1.0 noted as the gap).

- The S3 provider uses `AWSSDK.S3` with a configurable service URL and path-style addressing, so it serves AWS S3 and S3-compatible services (MinIO and others). Integration tests run against LocalStack, the fixture the repository already has; MinIO is not added.
- Satellite DI follows `AddEncinaStorage<Provider>()` with `TryAdd`, called before the core registration (prompt rule).

</details>

<details>
<summary><strong>2. Tenant partitioning — resolved from the ambient tenant, or an explicit tenant on every call</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A `TenantBlobStore` decorator prefixes every key with the tenant resolved from `ITenantProvider` (fail closed under `MultiTenancyMarker`, fixed default tenant when tenancy is off), with an explicit `IBlobStore ForTenant(string tenantId)` scope for code without an ambient tenant** | Calling code does not carry a tenant; the safe path is the default; background jobs use `ForTenant` explicitly | An implicit dependency on the ambient context; code that forgets `ForTenant` in a job gets `Left(storage.tenant_required)` (the intended fail-closed result) |
| **B) The tenant is a required parameter of every method (`BlobAddress` includes `TenantId`)** | Nothing implicit; trivial to audit | Every caller threads the tenant; a caller can pass the wrong one, which the ambient model prevents |

### Chosen Option: **A — ambient tenant with an explicit `ForTenant` scope** (recommended, pending the maintainer)

### Rationale

**Class:** Dilemma. Option A wins in request-scoped services where the tenant is already resolved by the pipeline. Option B wins in tooling that works across tenants (a retention sweeper, an export job) and where reviewers want the tenant visible at each call.

- We recommend A with `ForTenant` because it keeps the common path safe and still gives background code an explicit, auditable way to name a tenant.
- Physical layout: `{tenantId}/{name}`; the prefixes of two tenants never overlap because tenant ids are validated (letters, digits, `-`, `_`, no separators); a property test generates random tenant ids and names and asserts that a physical key maps back to exactly one tenant (AC-043).
- Names are validated by `BlobAddress`: no `..`, no backslash, no leading `/`, no control characters, at most 512 characters. The file system provider additionally resolves the full path and refuses anything outside the root.

</details>

<details>
<summary><strong>3. Lifecycle hooks — a guard interface in the core, adapters in <code>Encina.Storage.Compliance</code> or in the compliance packages</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `IBlobLifecycleGuard` in `Encina.Storage`; adapters in a new package `Encina.Storage.Compliance` (retention floor, legal hold, blocked state)** | `Encina.Storage` references no compliance package and the compliance packages do not reference storage; an application opts in by adding the one package; the guard chain is composite and ordered | One more package |
| **B) The adapters live in `Encina.Compliance.Retention` and the future `Encina.Compliance.Blocking`, each referencing `Encina.Storage`** | No extra package; an application using retention already has it | Every retention user takes a storage dependency (the abstractions package is small, but it is an edge for an optional feature); two packages must agree on the guard contract |

### Chosen Option: **A — `Encina.Storage.Compliance` hosts the adapters** (recommended, pending the maintainer)

### Rationale

**Class:** Dilemma. Option A wins when the dependency graph of the compliance packages must stay free of storage. Option B wins for a team that wants the fewest packages and already deploys the retention package everywhere. The issue lists `Encina.Compliance.Retention` and the blocking of P-03 as affected; this plan treats that as the integration points, not as the package that holds the code.

- The guard is called by `GuardedBlobStore` before `Delete`, before an overwrite (an overwrite is a delete and a create, so a held blob cannot be replaced) and before `Get`: `ValueTask<Either<EncinaError, Unit>> AuthorizeAsync(BlobOperation operation, BlobInfo info, CancellationToken)`. A `Left` stops the operation with `storage.delete_refused` or `storage.read_refused` and the reason code (`retention_floor`, `legal_hold`, `blocked`).
- The guard fails closed: when a guard throws or cannot decide (lookup fails), the operation is refused (AGENTS.md §3, SPEC-002 DEC-006).
- Link from a blob to the data it belongs to: `BlobMetadata` carries `OwnerEntityType`, `OwnerEntityId` and `RetentionCategory`. The retention adapter asks `ILegalHoldService.GetActiveHoldsForEntityAsync` now and `IRetentionFloorQuery` when the #1187 plan is implemented; the blocking adapter uses the `IBlockedDataFilterContext` and the audited disclosure scope of the #1189 plan and is built in a later pull request after #1189 (Phase 6, last task).
- Reads of a blocked blob are allowed only inside a disclosure scope and are recorded through `IReadAuditStore`.

</details>

<details>
<summary><strong>4. Envelope encryption — one AES-GCM pass per blob with a per-blob data key, or a chunked streaming format</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Single-shot AES-GCM per blob: a fresh 256-bit data key per blob wrapped by the tenant key through `IKeyWrapper` (#1250), header `0x01 | wrappedKey length | wrappedKey | nonce | tag`, default size cap 64 MiB (`MaxEncryptedBlobBytes`)** | Simple; a per-blob key avoids any limit on the number of encryptions per key; deleting the tenant key makes every blob unreadable (crypto-shredding of files); the format has a version byte for a later chunked version | The blob is buffered in memory for the cipher (bounded by the cap) |
| **B) Chunked AEAD stream (64 KiB chunks, counter-derived nonces, a final-chunk flag)** | Constant memory for very large files; streaming reads | A new format to design, test and review; no consumer needs it (consents and clinical documents are small) |

### Chosen Option: **A — single-shot AES-GCM with a per-blob wrapped data key and a size cap** (recommended, pending the maintainer)

### Rationale

**Class:** Dilemma. Option A wins for documents measured in kilobytes to a few megabytes, the use case of the reference application. Option B wins if the product stores videos or archives through `IBlobStore`.

- We recommend A with the version byte so that B can ship as format version 2 without a migration.
- The wrapped key lives in the blob header, so rotation of the tenant key re-wraps by rewriting the header with a metadata-only update where the provider allows it, or by a copy in place; the exact mechanism per provider is an open question (Research).
- Encryption is a decorator (`EncryptingBlobStore`) that is on by default when `Encina.Security.Encryption` and a key wrapper are registered; turning it off is an explicit, logged option (`AllowUnencrypted`), because the requirement says encryption is part of the store.
- The AAD is `tenantId | container | name`, so a blob copied to another name or tenant does not decrypt.

</details>

<details>
<summary><strong>5. Results, streaming and failure semantics</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ValueTask<Either<EncinaError, T>>` for every operation; content as `Stream` in and `BlobContent` (disposable) out; list returns a page with a continuation token** | Follows ROP and the claim-check and secrets APIs; streams avoid double buffering outside encryption | `BlobContent` must be disposed by the caller (documented, analyzer-friendly `IAsyncDisposable`) |
| **B) `Task<byte[]>` and exceptions** | Simplest to write | Violates ROP (AGENTS.md §3) and holds whole files in memory |

### Chosen Option: **A — `Either` results, stream content, paged listing**

### Rationale

**Class:** Trivial. **Settled by:** AGENTS.md §3 (Railway Oriented Programming, ADR-001, ADR-006; database and cloud calls async with a `CancellationToken`; errors never swallowed).

- Error codes: `storage.not_found`, `storage.already_exists`, `storage.tenant_required`, `storage.invalid_address`, `storage.too_large`, `storage.delete_refused`, `storage.read_refused`, `storage.encryption_failed`, `storage.provider_unavailable`. Logs carry the code and the exception type only, never blob names or content, never `EncinaError.Message` (AGENTS.md §3).
- Timestamps in `BlobInfo` end in `AtUtc` and come from the provider or from `TimeProvider` (file system and fake).

</details>

---

## Implementation Phases

Pull request cut: PR 1 = Phase 1 and the DI of Phase 7 (contract, models, options); PR 2 = Phase 2 (decorators); PR 3 = Phase 3 (file system); PR 4 = Phase 4 (S3); PR 5 = Phase 5 (Azure Blob); PR 6 = Phase 6 (compliance adapters); PR 7 = Phases 8-11 remainder (cross-cutting, observability, docs). Tests are written in each pull request for the code it adds.

### Phase 1: Core models and the `IBlobStore` contract

> **Goal**: The contract and value types, with validation of addresses.

<details>
<summary><strong>Tasks</strong></summary>

1. **`src/Encina.Storage/IBlobStore.cs`**:
   - `ValueTask<Either<EncinaError, BlobInfo>> PutAsync(BlobAddress address, Stream content, BlobPutOptions options, CancellationToken)`.
   - `ValueTask<Either<EncinaError, BlobContent>> GetAsync(BlobAddress address, CancellationToken)`.
   - `ValueTask<Either<EncinaError, BlobInfo>> GetInfoAsync(BlobAddress address, CancellationToken)`.
   - `ValueTask<Either<EncinaError, Unit>> DeleteAsync(BlobAddress address, CancellationToken)`.
   - `ValueTask<Either<EncinaError, bool>> ExistsAsync(BlobAddress address, CancellationToken)`.
   - `ValueTask<Either<EncinaError, BlobPage>> ListAsync(string container, string prefix, string? continuationToken, int pageSize, CancellationToken)`.
2. **`BlobAddress.cs`** (`Container`, `Name`, validation per Design Choice 2), **`BlobInfo.cs`** (address, `SizeBytes`, `ContentType`, `CreatedAtUtc`, `ModifiedAtUtc`, `ETag`, `BlobMetadata`), **`BlobMetadata.cs`** (`OwnerEntityType`, `OwnerEntityId`, `RetentionCategory`, `Tags`), **`BlobPutOptions.cs`** (`ContentType`, `Overwrite`, `Metadata`), **`BlobContent.cs`** (`Stream`, `BlobInfo`, `IAsyncDisposable`), **`BlobPage.cs`**.
3. **`StorageErrors.cs`** (codes of Design Choice 5), **`StorageOptions.cs`** (`DefaultContainer`, `MaxBlobBytes`, `AllowUnencrypted`, `PageSize`) with `Validate`.
4. `PublicAPI.Unshipped.txt`; add the project to `Encina.slnx`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of issue #1239 in Encina: the Encina.Storage package contract and models.

CONTEXT:
- New package src/Encina.Storage/ (namespace Encina.Storage); satellite packages and decorators follow in later phases.
- Result type is ValueTask<Either<EncinaError, T>> (LanguageExt); error factories follow src/Encina.Security.Secrets/SecretsErrors.cs.
- Address validation is a security control (path traversal, tenant prefix collisions).

TASK:
Create IBlobStore, BlobAddress (with validation), BlobInfo, BlobMetadata, BlobPutOptions, BlobContent, BlobPage, StorageErrors and
StorageOptions, with unit, guard and property tests (addresses never contain traversal; names round trip; tenant ids never overlap).

KEY RULES:
- Timestamps end in AtUtc; no DateTime.UtcNow; options with secrets are [JsonIgnore] with ToString().
- Public APIs have XML docs with an example; PublicAPI.Unshipped.txt; zero warnings.
- No payload, blob name or EncinaError.Message in logs or exception messages.

REFERENCE FILES:
- src/Encina.Security.Secrets/SecretsErrors.cs and SecretsOptions.cs
- src/Encina.Security.Secrets/Abstractions/ISecretReader.cs (API documentation style)
- docs/plans/storage-implementation-plan-1239.md
```

</details>

---

### Phase 2: Decorators and the lifecycle guard

> **Goal**: Tenant partition, guard, encryption, audit, resilience and telemetry as composable decorators.

<details>
<summary><strong>Tasks</strong></summary>

1. **`IBlobLifecycleGuard.cs`**, **`BlobOperation.cs`** (`Read`, `Overwrite`, `Delete`), **`CompositeBlobLifecycleGuard.cs`** (ordered, first `Left` wins, a thrown exception becomes `Left(storage.delete_refused|read_refused)`).
2. **`TenantBlobStore.cs`** (Design Choice 2) and `ForTenant`.
3. **`GuardedBlobStore.cs`**: `GetInfoAsync` then `AuthorizeAsync` before delete, overwrite and read.
4. **`EncryptingBlobStore.cs`**: header format of Design Choice 4, `IKeyWrapper` from the #1250 plan; refuses to read an unencrypted blob unless `AllowUnencrypted`.
5. **`AuditedBlobStore.cs`**: deletes and refusals to `IOperationAuditStore`; disclosure reads to `IReadAuditStore`; the audit carries tenant, container, a hash of the name and the outcome, never the name or content.
6. **`ResilientBlobStore.cs`**: retry transient errors (pattern `ResilientSecretReaderDecorator`, ADR-055).
7. Decorator order, outermost to innermost: instrumented, audited, guarded, tenant, encrypting, resilient, provider.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of issue #1239 in Encina: the IBlobStore decorators and the lifecycle guard.

CONTEXT:
- IBlobStore and the models exist (Phase 1). IKeyWrapper comes from the #1250 plan (src/Encina.Security.Encryption/).
- ITenantProvider / MultiTenancyMarker decide the tenant; IOperationAuditStore and IReadAuditStore are in src/Encina.Security.Audit/Abstractions/.
- Decorator registration pattern: DecorateService in src/Encina.Security.Secrets/ServiceCollectionExtensions.cs.

TASK:
Create the guard interface and composite, TenantBlobStore, GuardedBlobStore, EncryptingBlobStore, AuditedBlobStore and
ResilientBlobStore, and test them with an in-memory fake store: a held blob cannot be deleted or overwritten, a blocked blob is
unreadable outside a disclosure scope, tenant A cannot see tenant B, tamper with the header or the AAD and decryption fails.

KEY RULES:
- Fail closed: a guard that throws or cannot decide refuses the operation.
- No blob name, content, key material or EncinaError.Message in logs, audit records or tags; names are hashed in audit.
- Either results; no exceptions for business failures; every call async with a CancellationToken.
- Decorators never swallow a Left.

REFERENCE FILES:
- src/Encina.Security.Secrets/Resilience/ResilientSecretReaderDecorator.cs
- src/Encina.Security.Secrets/Auditing/AuditedSecretReaderDecorator.cs
- src/Encina.Tenancy/Abstractions/ITenantProvider.cs
```

</details>

---

### Phase 3: File system provider

> **Goal**: A local provider with the same contract, safe against path traversal.

<details>
<summary><strong>Tasks</strong></summary>

1. **`FileSystemBlobStore.cs`**: root directory from `FileSystemStorageOptions.RootPath` (validated: exists or is created, absolute); the full path of every address is resolved with `Path.GetFullPath` and refused when it is not under the root; writes go to a temporary file and are moved atomically; metadata in a sidecar `*.meta.json` (written atomically before the data file is made visible); listing by prefix with a continuation token; `TimeProvider` for timestamps.
2. **`FileSystemStorageOptions.cs`**, **`AddEncinaStorageFileSystem()`** registering the store as `IBlobStore` with `TryAdd` and the decorator chain.
3. **`FileSystemBlobStoreHealthCheck`**: writes and deletes a probe file under the root.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are implementing Phase 3 of issue #1239 in Encina: the file system provider of Encina.Storage.

CONTEXT:
- IBlobStore, BlobAddress validation and the decorators exist (Phases 1-2). The file system store is the local provider of the cloud category.
- Tests use an isolated temp directory per test class; the fixture owns its cleanup.

TASK:
Implement FileSystemBlobStore with atomic writes, sidecar metadata, safe path resolution and paged listing, plus options, DI, a health
check, the ContractTests base for IBlobStore (reused by S3 and Azure Blob) and integration tests on a temp directory.

KEY RULES:
- Refuse any path that resolves outside the root (symbolic links included); never trust the address alone.
- All I/O async with CancellationToken; no sync File.* calls on the hot path.
- Time via TimeProvider; Either results; no blob names in logs.

REFERENCE FILES:
- tests/Encina.ContractTests/ (layout of existing contract tests)
- src/Encina.Security.Secrets/Health/SecretsHealthCheck.cs
```

</details>

---

### Phase 4: S3-compatible provider

> **Goal**: Amazon S3 and S3-compatible services behind the same contract.

<details>
<summary><strong>Tasks</strong></summary>

1. **`src/Encina.Storage.S3/`**: `S3BlobStore : IBlobStore` over `IAmazonS3` (`PutObject`, `GetObject`, `GetObjectMetadata`, `DeleteObject`, `ListObjectsV2` with continuation token), `S3StorageOptions` (bucket, region, `ServiceUrl`, `ForcePathStyle`; credentials come from the SDK chain; any secret key property is `[JsonIgnore]` with a `ToString()` override), validator, `AddEncinaStorageS3()`, log messages (6100-6124), README, PublicAPI.
2. **`Directory.Packages.props`**: add `AWSSDK.S3` at the 4.x version compatible with the other `AWSSDK` packages (4.0.2.17 line); check at implementation time.
3. Exceptions map to codes: `NoSuchKey` to `storage.not_found`, throttling and 5xx to `storage.provider_unavailable`.
4. Health check: `HeadBucket`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
You are implementing Phase 4 of issue #1239 in Encina: Encina.Storage.S3.

CONTEXT:
- Contract and base contract tests exist (Phases 1-3). Fixture: LocalStackFixture (collection Cloud-LocalStack,
  tests/Encina.TestInfrastructure/Fixtures/LocalStackFixture.cs); it currently exposes SQS and SNS clients, so extend it with an S3 client.
- Satellite layout model: src/Encina.Security.Secrets.AzureKeyVault/ (options, validator, DI, log messages, README).

TASK:
Create the package, the store, options and validator, DI, health check, log messages and the integration tests against LocalStack
(subclass the IBlobStore contract tests; list by prefix and paging; not found; large blob).

KEY RULES:
- Secrets in options are [JsonIgnore] with ToString() overrides; the credentials come from the SDK chain.
- Async everywhere with CancellationToken; map provider exceptions to storage.* codes; never log blob names or EncinaError.Message.
- The integration test clients are owned by the fixture; do not dispose them.
- Register the assembly in EncinaEventIdAllocationTests and the range in EventIdRanges first.

REFERENCE FILES:
- tests/Encina.TestInfrastructure/Fixtures/LocalStackFixture.cs
- src/Encina.Security.Secrets.AwsSecretsManager/ (AWS SDK client wiring and error mapping)
```

</details>

---

### Phase 5: Azure Blob provider

> **Goal**: Azure Blob Storage behind the same contract.

<details>
<summary><strong>Tasks</strong></summary>

1. **`src/Encina.Storage.AzureBlob/`**: `AzureBlobStore : IBlobStore` over `BlobServiceClient` (`UploadAsync`, `DownloadStreamingAsync`, `GetPropertiesAsync`, `DeleteIfExistsAsync`, `GetBlobsByHierarchyAsync` or `GetBlobsAsync` with a prefix and continuation token), `AzureBlobStorageOptions` (connection string `[JsonIgnore]` with `ToString()` override, or account URI with `DefaultAzureCredential`), validator, `AddEncinaStorageAzureBlob()`, log messages (6125-6149), README, PublicAPI.
2. Errors: 404 to `storage.not_found`, 409 on create-only to `storage.already_exists`, 429/5xx to `storage.provider_unavailable`.
3. Health check: `GetPropertiesAsync` on the container.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
You are implementing Phase 5 of issue #1239 in Encina: Encina.Storage.AzureBlob.

CONTEXT:
- Contract and base contract tests exist (Phases 1-3). Fixture: AzuriteFixture (collection Cloud-Azurite,
  tests/Encina.TestInfrastructure/Fixtures/AzuriteFixture.cs) exposing a BlobServiceClient.
- Azure.Storage.Blobs 12.30.1 is already in Directory.Packages.props.

TASK:
Create the package, the store, options and validator, DI, health check, log messages and integration tests against Azurite
(contract subclass; listing and paging; not found; overwrite refusal when Overwrite is false).

KEY RULES:
- The connection string option is [JsonIgnore] with a ToString() override; prefer DefaultAzureCredential with an account URI.
- Async with CancellationToken; storage.* codes; never log blob names, content or EncinaError.Message.
- Fixture clients are owned by the fixture; do not dispose them.

REFERENCE FILES:
- tests/Encina.TestInfrastructure/Fixtures/AzuriteFixture.cs
- src/Encina.Security.Secrets.AzureKeyVault/ (options validator, DI, log messages)
```

</details>

---

### Phase 6: Compliance adapters

> **Goal**: Held, floor-protected and blocked blobs cannot be deleted; blocked blobs are unreadable outside a disclosure.

<details>
<summary><strong>Tasks</strong></summary>

1. **`src/Encina.Storage.Compliance/`**: `LegalHoldBlobGuard : IBlobLifecycleGuard` (`ILegalHoldService.GetActiveHoldsForEntityAsync(OwnerEntityType, OwnerEntityId)`; no owner means the guard cannot decide for the retention category and the policy option `RefuseWhenOwnerUnknown` (default true) decides), `AddEncinaStorageCompliance()`.
2. **`RetentionFloorBlobGuard`**: compiled only when `IRetentionFloorQuery` exists (#1187 implemented); until then the task is tracked by the follow-up issue.
3. **`BlockedBlobGuard`**: uses `IBlockedDataFilterContext` and the disclosure scope (#1189 implemented); tracked likewise.
4. Integration tests with the real services: the S11 scenario on every storage provider: a held blob cannot be deleted (file system, Azurite, LocalStack), with the retention and legal hold stores of `Encina.Compliance.Retention` backed by their test infrastructure.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
You are implementing Phase 6 of issue #1239 in Encina: the compliance adapters for blob lifecycle.

CONTEXT:
- IBlobLifecycleGuard (Phase 2) is the hook; ILegalHoldService is in src/Encina.Compliance.Retention/Abstractions/ILegalHoldService.cs.
- IRetentionFloorQuery (#1187) and IBlockedDataFilterContext (#1189) do not exist in src/ yet; check before writing those adapters and
  skip them (with a note in the pull request) if they are missing.

TASK:
Create Encina.Storage.Compliance with LegalHoldBlobGuard and, when the dependencies exist, the retention floor and blocked guards, DI,
README, and the S11 integration test on each storage provider (held blob: delete and overwrite refused, audited).

KEY RULES:
- Fail closed: lookup failure or exception refuses the operation.
- Refusals are audited without blob names; reasons are codes (legal_hold, retention_floor, blocked).
- Test with real stores, not mocks of the guard itself.

REFERENCE FILES:
- src/Encina.Compliance.Retention/Abstractions/ILegalHoldService.cs
- docs/plans/retention-floor-implementation-plan-1187.md and blocked-data-state-implementation-plan-1189.md
```

</details>

---

### Phase 7: Configuration and DI

> **Goal**: One registration builds the decorated store; every dependency is registered.

<details>
<summary><strong>Tasks</strong></summary>

1. **`AddEncinaStorage(Action<StorageOptions>)`** in `Encina.Storage`: options with start-up validation, the decorator chain in the order of Phase 2, `CompositeBlobLifecycleGuard` collecting every registered `IBlobLifecycleGuard`.
2. Each provider registers `IBlobStore` with `TryAdd`, called before `AddEncinaStorage` (prompt rule); a store registered by the application wins.
3. DI tests with `ValidateOnBuild` and `ValidateScopes` for each provider and for the chain.
4. Encryption enabled by default when `IKeyWrapper` is registered; without it and without `AllowUnencrypted`, start-up fails with a message naming the option (the guarantee is false without it, ADR-050 rule 3).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
You are implementing Phase 7 of issue #1239 in Encina: registration for Encina.Storage.

CONTEXT:
- AGENTS.md §3 registration completeness: an AddEncina* method registers every option type and dependency it resolves, proven by a
  ValidateOnBuild/ValidateScopes DI test; the application's own registrations win.
- Decorator wiring pattern: src/Encina.Security.Secrets/ServiceCollectionExtensions.cs.

TASK:
Add AddEncinaStorage, the provider registrations, the options validation and the DI tests described.

KEY RULES:
- TryAdd for defaults; satellites are called before the core registration.
- Start-up fails (not a silent default) when encryption is required but not available.

REFERENCE FILES:
- src/Encina.Security.Secrets/ServiceCollectionExtensions.cs
- docs/architecture/adr/050-modules-may-require-modules.md
```

</details>

---

### Phase 8: Cross-cutting integration

> **Goal**: The matrix items marked included.

<details>
<summary><strong>Tasks</strong></summary>

1. **Health checks**: one per provider (Phases 3-5) and an aggregate registration.
2. **Resilience**: `ResilientBlobStore` (Phase 2) registered when `Encina.Resilience` is present.
3. **Audit**: `AuditedBlobStore` (Phase 2).
4. **Validation**: options validators at start-up; addresses validated by `BlobAddress`.
5. **Multi-tenancy**: `TenantBlobStore` (Phase 2); the two-tenant test of AC-043 runs on every provider.
6. **`FakeBlobStore`** in `Encina.Testing.Fakes` (in-memory, `TimeProvider`-based) for consumers' tests.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```
You are implementing Phase 8 of issue #1239 in Encina: the remaining cross-cutting pieces and the test fake.

CONTEXT:
- Fakes live in src/Encina.Testing.Fakes/ (see FakeOutboxStore for the style).

TASK:
Add FakeBlobStore, the aggregate health check registration and the two-tenant isolation test that runs against every provider.

KEY RULES:
- The fake behaves like the contract (same codes, same ordering of list results); it uses TimeProvider.
- Health results carry codes only.

REFERENCE FILES:
- src/Encina.Testing.Fakes/Stores/
- src/Encina.Messaging/Health/IEncinaHealthCheck.cs
```

</details>

---

### Phase 9: Observability

> **Goal**: Spans, metrics and logs, with the tenant and no content.

<details>
<summary><strong>Tasks</strong></summary>

1. **EventIds**: register `StorageCore = (6050, 6099)` and `StorageProviders = (6100, 6149)` in `EventIdRanges.cs`; add `Encina.Storage`, `Encina.Storage.S3` (6100-6124), `Encina.Storage.AzureBlob` (6125-6149) and `Encina.Storage.Compliance` (6050-6099 shared with the core, from 5580) to `EncinaEventIdAllocationTests`.
2. **`StorageActivitySource`** (`Encina.Storage`): spans `encina.storage.put`, `get`, `delete`, `list` with `encina.tenant_id` and `encina.storage.provider`; no name or content.
3. **`StorageMetrics`**: counters `encina.storage.operations` (provider, operation, outcome) and `encina.storage.delete_refused` (reason code), histogram of sizes.
4. **Test** (AC-044): in-memory exporter asserts the tenant attribute and the absence of names, content and subject identifiers in tags, metrics and logs.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 9</strong></summary>

```
You are implementing Phase 9 of issue #1239 in Encina: observability for Encina.Storage.

CONTEXT:
- ADR-021 / AGENTS.md §7: register ranges first; EventIds packed; assemblies in EncinaEventIdAllocationTests.
- Model: src/Encina.Messaging/Diagnostics/ and src/Encina.Security.Secrets/Diagnostics/.

TASK:
Register StorageCore 6050-6099 and StorageProviders 6100-6149, write log messages, the activity source and the metrics, wire them
into the instrumented decorator, and add the exporter test.

KEY RULES:
- Tag values are bounded (provider, operation, outcome, reason code); never names, content or subject identifiers.
- Never outside the plan's ranges; no gaps.

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

---

### Phase 10: Testing

> **Goal**: Every flag at its target; one contract across all providers.

<details>
<summary><strong>Tasks</strong></summary>

| Flag | Scope | Per-file target and justification |
|------|-------|-----------------------------------|
| Unit | models, validation, decorators, guards, file system store, options | 90 for the decorators and `BlobAddress`: pure logic with a fake store; 80 for provider glue |
| Guard | every public method with parameters | 90 |
| Contract | one `IBlobStore` contract run against the file system, Azurite and LocalStack stores and the fake | 85: real implementations |
| Property | encrypt-decrypt round trip, tenant prefixes never overlap, addresses never traverse | 75: invariants |
| Integration | file system, Azurite, LocalStack S3: round trip, listing, tenant isolation, S11 (held and blocked blob cannot be deleted), encryption at rest (the stored bytes are not the plaintext) | real services, shared `[Collection]` fixtures |
| Load, Benchmark | `.md` justifications: not a concurrent hot path | |

- Update the manifests of the four new packages and `Encina.Testing.Fakes`; measure each flag and the CRAP of the changed methods (AGENTS.md §9).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 10</strong></summary>

```
You are implementing Phase 10 of issue #1239 in Encina: the test suite for the storage packages.

CONTEXT:
- Per-flag coverage targets live in .github/coverage-manifest/{Package}.json; tests must execute real package code.
- Fixtures: AzuriteFixture (Cloud-Azurite), LocalStackFixture (Cloud-LocalStack), temp directories for the file system.

TASK:
Complete the tests of the table, write the load and benchmark justification files, update the manifests, and report measured
per-flag coverage and the CRAP table for the changed methods.

KEY RULES:
- One IBlobStore contract base reused by every provider; shared [Collection] fixtures; no Thread.Sleep; no reflection-only tests.
- Justification files follow AGENTS.md §9 (title, Status, Justification, adequate coverage, Date, Issue).

REFERENCE FILES:
- docs/testing/coverage-measurement-methodology.md
- tests/Encina.IntegrationTests/Infrastructure/Azurite/
```

</details>

---

### Phase 11: Documentation and finalization

> **Goal**: Everything the prompt lists, including the ADR.

<details>
<summary><strong>Tasks</strong></summary>

1. XML docs on all new public APIs; `PublicAPI.Unshipped.txt` for the four packages.
2. `changelog.d/1239-encina-storage.added.md`.
3. ADR (next free number; 067 on 2026-11-11, reserved in the ADR index when work starts): the guard hook, tenant model, envelope format, package layout.
4. READMEs for the four packages; `docs/features/storage.md` (configuration, providers, tenant partition, retention and hold behavior, encryption, limits); `docs/INVENTORY.md`; `ROADMAP.md`.
5. Follow-up issues: retention floor and blocked-state guards (after #1187 and #1189), Google Cloud Storage (post-1.0), chunked encryption format version 2 (if needed).
6. `dotnet build --configuration Release` with 0 warnings, `dotnet test`, every flag at target.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 11</strong></summary>

```
You are implementing Phase 11 of issue #1239 in Encina: documentation and finalization.

CONTEXT:
- Documentation is written by docs-writer; the changelog fragment and PublicAPI lines by mechanical-fixer.

TASK:
Produce the documentation list, verify every identifier against src/, run the build and the tests.

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
| SPEC-002 | REQ-046, AC-039, scenario S11, DEC-016 (a), note F, R15 | Scope, providers, acceptance: a held or blocked blob cannot be deleted |
| SPEC-002 | REQ-061 / AC-043, REQ-062 / AC-044, DEC-006 | Tenant partition, instrumentation, fail closed |
| SPEC-000 | §6.1 (GCP post-1.0) | Cloud triangle gap |
| GDPR | Art. 5(1)(e) storage limitation, Art. 17 erasure, Art. 32 security | Retention and encryption of files |
| NIST SP 800-38D, SP 800-57 | AES-GCM, key wrapping | Envelope format |
| Comparable products | Azure Blob Storage SDK, AWS SDK for .NET S3, MinIO, Apache Commons VFS | Contract shape; server-side encryption is complementary, not a replacement |
| AGENTS.md §3, §5 | Fail closed, ROP, registration completeness; cloud category | Rules applied |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| ITenantProvider | src/Encina.Tenancy/Abstractions/ITenantProvider.cs | tenant prefix of every blob key |
| MultiTenancyMarker | src/Encina/Abstractions/MultiTenancyMarker.cs | signal that tenancy is on, so a missing tenant fails closed |
| ILegalHoldService | src/Encina.Compliance.Retention/Abstractions/ILegalHoldService.cs | hold check before delete |
| IRetentionFloorQuery (planned by #1187) | docs/plans/retention-floor-implementation-plan-1187.md | retention floor check before delete |
| IBlockedDataFilterContext (planned by #1189) | docs/plans/blocked-data-state-implementation-plan-1189.md | blocked state and the audited disclosure scope |
| IOperationAuditStore and IReadAuditStore | src/Encina.Security.Audit/Abstractions/ | audit of deletes, refusals and disclosures |
| IKeyWrapper and WrappedKey (planned by #1250) | docs/plans/column-encryption-implementation-plan-1250.md | per-blob data key wrapped by the tenant key |
| ITenantKeyResolver | src/Encina.Messaging.Encryption/Abstractions/ITenantKeyResolver.cs | tenant key id convention |
| Azure.Storage.Blobs 12.30.1 | Directory.Packages.props | Azure Blob provider |
| AWSSDK.S3 | not yet in Directory.Packages.props | new version entry for the S3-compatible provider |
| AzuriteFixture and LocalStackFixture | tests/Encina.TestInfrastructure/Fixtures/ | integration tests (collections Cloud-Azurite, Cloud-LocalStack) |
| IEncinaHealthCheck | src/Encina.Messaging/Health/IEncinaHealthCheck.cs | per-provider health checks (the Encina.Security.Secrets health check shows the IHealthCheck pattern) |
| ResilientSecretReaderDecorator | src/Encina.Security.Secrets/Resilience/ | pattern for the resilient blob store decorator |
| EventIdRanges | src/Encina/Diagnostics/EventIdRanges.cs | register StorageCore 6050-6099 and StorageProviders 6100-6149 first |
| Encina.Testing.Fakes | src/Encina.Testing.Fakes/ | FakeBlobStore for consumers' tests |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Storage` and `Encina.Storage.Compliance` | `StorageCore` 6050-6099 (new) | Core 6050-6079, Compliance 6080-6099 |
| `Encina.Storage.S3` | `StorageProviders` 6100-6149 (new) | **6100-6124** |
| `Encina.Storage.AzureBlob` | `StorageProviders` 6100-6149 | **6125-6149** |
| Family of plans | 6000-6049 claim check (#599, #122), 6150-6199 claim check providers (#122), 6200-6249 column encryption (#1250), `SecuritySecrets` 5151-5170 (#1246) | Disjoint, in the family block 6000-6249 (registry eventid-blocks.md) |

### Open Questions to Verify During Implementation

| Question | How to verify |
|----------|---------------|
| Which `AWSSDK.S3` 4.x version matches the other `AWSSDK` packages? | Restore with the 4.0.2.x family and run the LocalStack test |
| Can the wrapped key be re-wrapped by a metadata update on S3 and Azure Blob, or does it need a copy in place? | Azure `SetMetadataAsync` works; S3 needs `CopyObject` onto itself with replaced metadata; if the header holds the key, rewrite the object; decide at Phase 2 and record in the ADR |
| Does `LocalStackFixture` start the S3 service? | Read `LocalStackFixture.cs`; add the service and an `IAmazonS3` client if missing |
| Does the retention floor of #1187 take a blob category? | Read the implemented `IRetentionFloorQuery`; adapt the adapter task |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Encina.Storage abstractions and models (IBlobStore, BlobAddress, BlobInfo, BlobMetadata, errors, options, lifecycle guard interface) | 9 | |
| Encina.Storage decorators (guarded store, tenant partitioning, envelope encryption, audited, resilient, instrumented) | 6 | |
| Encina.Storage file system provider | 3 | |
| Encina.Storage.S3 package (provider, options, validator, DI, log messages, csproj, README, PublicAPI) | 8 | |
| Encina.Storage.AzureBlob package (same shape) | 8 | |
| Encina.Storage.Compliance (retention and hold guard, DI) | 2 | plus csproj, README, PublicAPI counted in documentation |
| Diagnostics (log messages, activity source, metrics) and health checks | 5 | |
| Modified existing files (EventIdRanges, EncinaEventIdAllocationTests, Directory.Packages.props, Encina.slnx, Encina.Testing.Fakes) | 6 | |
| Unit tests | 16 | |
| Guard tests | 4 | |
| Contract tests | 4 | |
| Property tests | 3 | |
| Integration tests | 5 | file system, Azurite, S3 (LocalStack), tenant isolation, hold and blocking refusal |
| Load and benchmark justification files | 2 | |
| Documentation | 8 | |
| Coverage manifest edits | 4 | |
| Total | 93 | Split over seven pull requests |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
PROJECT CONTEXT:
You are implementing issue #1239 in Encina (.NET 10, C# 14, nullable enabled, pre-1.0, no backward compatibility): the Encina.Storage
package family with IBlobStore (SPEC-002 REQ-046). Rules: AGENTS.md. IKeyWrapper comes from docs/plans/column-encryption-implementation-plan-1250.md;
Encina.ClaimCheck.Storage (docs/plans/claim-check-implementation-plan-122.md) builds on this package.

IMPLEMENTATION OVERVIEW:
1. Contract and models: IBlobStore, BlobAddress (validated), BlobInfo, BlobMetadata, BlobPutOptions, BlobContent, BlobPage, StorageErrors, StorageOptions.
2. Decorators: tenant partition (ambient tenant, ForTenant scope), lifecycle guard (composite), single-shot AES-GCM envelope encryption with a per-blob
   wrapped key, audit, resilience, telemetry.
3. Providers: file system (core), Encina.Storage.S3 (S3-compatible, LocalStack tests), Encina.Storage.AzureBlob (Azurite tests).
4. Encina.Storage.Compliance: legal hold guard now; retention floor and blocked-state guards after #1187 and #1189.
5. Registration, health checks, FakeBlobStore, EventIds StorageCore 6050-6099 and StorageProviders 6100-6149, metrics, spans.
6. Tests per flag with one IBlobStore contract across providers; docs and ADR.

KEY PATTERNS:
- Either<EncinaError, T>; async with CancellationToken; errors never swallowed; no blob names, content, key material or EncinaError.Message in logs, audit or tags.
- Fail closed: missing tenant under MultiTenancyMarker, guard failure, undecidable lookups refuse the operation.
- Secrets in options are [JsonIgnore] with ToString(); satellites register with TryAdd before the core registration.
- Shared [Collection] fixtures (Cloud-Azurite, Cloud-LocalStack); per-flag coverage; CRAP <= 10 on changed methods.

REFERENCE FILES:
- src/Encina.Security.Secrets/ (decorators, options validators, health check, DI)
- tests/Encina.TestInfrastructure/Fixtures/AzuriteFixture.cs and LocalStackFixture.cs
- src/Encina.Compliance.Retention/Abstractions/ILegalHoldService.cs
- docs/plans/storage-implementation-plan-1239.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | No read path that benefits from caching: reads are guarded per call and files are large |
| 2 | OpenTelemetry | ✅ | Phase 9: spans per operation and counters with the tenant attribute; no names or content |
| 3 | Structured Logging | ✅ | Phase 9: EventIds in `StorageCore` and `StorageProviders`, registered first (ADR-021) |
| 4 | Health Checks | ✅ | Phases 3-5, 8: reachability per provider |
| 5 | Validation | ✅ | Options validated at start-up; `BlobAddress` validated on construction |
| 6 | Resilience | ✅ | Phase 2: `ResilientBlobStore` retries transient storage errors (ADR-055) |
| 7 | Distributed Locks | ❌ | No shared state updated by concurrent hosts; object stores provide atomic puts |
| 8 | Transactions | ❌ | Single-operation writes; no atomicity with other systems is offered |
| 9 | Idempotency | ❌ | Not a message or request entry point |
| 10 | Multi-Tenancy | ✅ | Phase 2: tenant prefix, fail closed, default tenant when tenancy is off; two-tenant test on every provider (AC-043) |
| 11 | Module Isolation | ❌ | SPEC-002 requires no module scoping; `ModuleId` in messaging stays with #747 |
| 12 | Audit Trail | ✅ | Phase 2: deletes, hold refusals and disclosures through `IOperationAuditStore` and `IReadAuditStore` |

---

## Prerequisites & Dependencies

### Advisable before or with this work

- #1250 plan, Phase 1 and Phase 3: `IKeyWrapper` and the KMS wrappers for the encryption decorator. Phases 1, 3, 4, 5 and the guard of this plan do not wait for it.
- #1187 (retention floor) and #1189 (blocked data state): the floor and blocking adapters of Phase 6 wait for them; the legal hold adapter does not.
- #1257 (multi-tenancy of the SPEC-002 capabilities): REQ-061 for blob storage is implemented here; #1257 tracks the cross-feature verification.

### Parallel and follow-up work

- [`claim-check-implementation-plan-122.md`](claim-check-implementation-plan-122.md): `Encina.ClaimCheck.Storage` builds on `IBlobStore`; #600 is covered by it.
- Google Cloud Storage: post-1.0, the recorded gap of the cloud triangle (follow-up issue at Phase 11).

---

## Next Steps

1. The maintainer answers the Dilemmas of this plan (choices 2, 3 and 4); the orchestrator adds the Maintainer Decisions section.
2. Link this plan from #1239 and reserve the ADR number in `docs/architecture/adr/index.md`.
3. Start with PR 1 and PR 2; PR 3 to PR 5 (providers) can run in parallel after PR 1.
4. Open the follow-up issues of Phase 11 task 5 when the corresponding dependencies are merged.
