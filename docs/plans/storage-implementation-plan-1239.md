# Implementation Plan: `Encina.Storage` — `IBlobStore`, Envelope Encryption, Retention/Hold/Blocking Hooks and Providers

> **Issue**: [#1239](https://github.com/dlrivada/Encina/issues/1239) (SPEC-002 REQ-046, AC-039 and AC-043, scenario S11, tracking id P-37, priority P0; part of #1186)
> **Type**: Feature
> **Complexity**: Very high (new package family: core, file system, S3, Azure Blob, compliance adapters; 11 pull requests)
> **Estimated Scope**: ~2,200-2,800 lines of production code + ~3,500-4,500 lines of tests
> **Related plans**: #1250 (owns `IKeyWrapper` and the tenant data keys), #122 (builds `Encina.ClaimCheck.Storage` on this package)

---

## Summary

Add a new package `Encina.Storage` with the file abstraction `IBlobStore` (put, get, delete, list by prefix, metadata), partitioned by tenant, with decorators for envelope encryption, audit, resilience and telemetry, and a lifecycle guard through which retention floors, legal holds and blocked state can refuse a delete or a read. The providers are the three of SPEC-002 DEC-016: the file system (in the core package), one S3-compatible provider (`Encina.Storage.S3`) and Azure Blob (`Encina.Storage.AzureBlob`). Google Cloud Storage is post-1.0 and is the recorded gap of the cloud triangle (SPEC-000 §6.1).

### Shared names (this plan owns them)

| Type | Package | Role |
|------|---------|------|
| `IBlobStore`, `BlobAddress`, `BlobInfo`, `BlobMetadata`, `BlobPutOptions`, `BlobContent`, `BlobPage` | `Encina.Storage` | The contract; consumed by `Encina.ClaimCheck.Storage` (#122 plan) |
| `IBlobLifecycleGuard`, `BlobOperation` | `Encina.Storage` | The hook for retention, hold and blocking |
| `Encina.Storage.Compliance` | new package | Adapters from the guard to `ILegalHoldService` now, `IRetentionFloorQuery` (#1187) and blocking (#1189) when they exist |
| Consumed from #1250 | `Encina.Security.Encryption` | `ITenantDataKeyService` (the tenant's data key, itself wrapped by a KMS through `IKeyWrapper`) or `IKeyWrapper` directly, per choice 6 |

### What the code does today (2026-11-11, `main` at `7a0049c07`)

Nothing of #1239 exists: `src/` has no `Encina.Storage` package and no `IBlobStore` type. What exists and shapes the design:

1. **Cloud SDKs and fixtures.** `Azure.Storage.Blobs` 12.30.1 is in `Directory.Packages.props`; `AWSSDK.S3` is not (the SQS, SNS, KMS and Secrets Manager SDKs are). The test fixtures `AzuriteFixture` (Blob, Queue and Table services; collection `Cloud-Azurite`) and `LocalStackFixture` (collection `Cloud-LocalStack`) are in `tests/Encina.TestInfrastructure/Fixtures/`.
2. **Tenancy.** Two tenant sources exist. The core package `Encina` has `IRequestContextAccessor` (`src/Encina/Abstractions/IRequestContextAccessor.cs`), whose `IRequestContext.TenantId` is null when there is no tenant; `Encina.Tenancy` has `ITenantProvider.GetCurrentTenantId()` (`src/Encina.Tenancy/Abstractions/ITenantProvider.cs`), and `Encina.Tenancy` references `Encina.Messaging`. `MultiTenancyMarker` (`src/Encina/Abstractions/MultiTenancyMarker.cs`) is the signal that tenancy is on so that readers fail closed. This is the fail-closed pattern of the dead-letter manager (`src/Encina.Messaging/DeadLetter/DeadLetterManager.cs`, which reads the core accessor) and the ABAC decision audit. Which source this package uses is choice 10.
3. **Hold, retention and blocking.** `ILegalHoldService` exists (`src/Encina.Compliance.Retention/Abstractions/ILegalHoldService.cs`: `HasActiveHoldsAsync`, `GetActiveHoldsForEntityAsync`). The retention floor query `IRetentionFloorQuery` is planned by the #1187 plan and the blocked state (`IBlockedDataFilterContext`, audited disclosure) by the #1189 plan; neither exists in `src/` today.
4. **Audit.** The issue names `IAuditStore`; the interfaces in `src/Encina.Security.Audit/Abstractions/` are `IOperationAuditStore` and `IReadAuditStore` (disclosure reads). The plan uses those.
5. **Encryption primitives.** The existing KMS providers return a fresh key per call (#2386), so envelope encryption needs `IKeyWrapper` of the #1250 plan (wrap and unwrap a given key). `ITenantKeyResolver` (`src/Encina.Messaging.Encryption/Abstractions/ITenantKeyResolver.cs`) shows the tenant-to-key-id convention. The .NET 10 BCL has AES Key Wrap with padding (RFC 5649) only: the `net10.0` reference pack (Microsoft.NETCore.App.Ref 10.0.15, `System.Security.Cryptography.xml`) has `Aes.EncryptKeyWrapPadded`, `Aes.DecryptKeyWrapPadded`, `Aes.TryDecryptKeyWrapPadded` and `Aes.GetKeyWrapPaddedLength` (with their `*Core` methods), and no unpadded RFC 3394 `EncryptKeyWrap`/`DecryptKeyWrap`; the 11.0.0 pack has the same members (both checked 2026-11-12). A file-based app targeting `net10.0` reproduced the RFC 5649 section 6 test vector for the 20-octet key (SDK 10.0.404). Spike #3661 lists AES Key Wrap among its .NET 11 follow-ups; the padded variant this plan uses does not need the platform move.
6. **No other blob store.** `#600` (cloud blob implementations of `IClaimCheckStore`) is open and the #122 plan builds the claim-check blob backends on this package, so `IBlobStore` is the single storage primitive.

### Scope

- **Affected packages**: new `Encina.Storage` (core and file system), `Encina.Storage.S3`, `Encina.Storage.AzureBlob`, `Encina.Storage.Compliance`; `Encina.Testing.Fakes` (`FakeBlobStore`); `Directory.Packages.props`, `Encina.slnx`.
- **Provider category**: Cloud (AWS and Azure; the file system is the local provider). Not a database feature; the 10-provider matrix does not apply. The category rule asks for the same contract on every storage provider: put, get, delete, list, metadata, tenant partition, guard and encryption decorators, health check.
- **Out of scope**: Google Cloud Storage (post-1.0); database BLOB storage (rejected in the issue); the key management (#1250); a content delivery or presigned URL feature.
- **Estimated files**: ~45 production files, ~55 test files, ~20 documentation and manifest files. Eleven pull requests (see "Pull request cut" under Implementation Phases).

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
| **A) A `TenantBlobStore` decorator sets on every address the tenant resolved from the tenant source of choice 10 (fail closed under `MultiTenancyMarker`, fixed default tenant when tenancy is off), with an explicit `IBlobStore ForTenant(string tenantId)` scope for code without an ambient tenant** | Calling code does not carry a tenant; the safe path is the default; background jobs use `ForTenant` explicitly | An implicit dependency on the ambient context; code that forgets `ForTenant` in a job gets `Left(storage.tenant_required)` (the intended fail-closed result) |
| **B) The tenant is a required parameter of every method (`BlobAddress` includes `TenantId`)** | Nothing implicit; trivial to audit | Every caller threads the tenant; a caller can pass the wrong one, which the ambient model prevents |

### Chosen Option: **A — ambient tenant with an explicit `ForTenant` scope** (recommended, pending the maintainer)

### Rationale

**Class:** Dilemma. Option A wins in request-scoped services where the tenant is already resolved by the pipeline. Option B wins in tooling that works across tenants (a retention sweeper, an export job) and where reviewers want the tenant visible at each call.

- We recommend A with `ForTenant` because it keeps the common path safe and still gives background code an explicit, auditable way to name a tenant.
- Physical layout: `{tenantId}/{name}`; the prefixes of two tenants never overlap because tenant ids are validated (ASCII letters, digits, `-`, `_`, no separators); a property test generates random tenant ids and names and asserts that a physical key maps back to exactly one tenant (AC-043). Under tenancy the id `default` is reserved for the default partition (choice 7) and refused as a tenant id, compared case-insensitively (`Default` and `DEFAULT` are refused too); the same property test asserts that no accepted tenant id under tenancy maps to the default partition.
- The isolation proof names the comparison each backend uses. S3 object keys and Azure blob names are case-sensitive, so `{tenantId}/{name}` is used verbatim there and `Acme` and `acme` are two prefixes. The default file systems of Windows (NTFS) and macOS (APFS) are case-preserving but case-insensitive: written verbatim, tenants `Acme` and `acme` would share `<root>/acme/` and read, overwrite or delete each other's blobs, and `Doc.pdf` and `doc.pdf` would be one file. The tenant and the name alphabets keep both cases (the tenant layer compares tenant ids ordinally), so the file system provider encodes the physical path instead (Design Choice 2a). The container is the Azure container name, which Azure accepts in lower case only; the container rule of choice 7 is therefore lower case on every provider and the question does not arise for it.
- **2a. File system path encoding (recommended, pending the maintainer).** Options: (A) `FileSystemBlobStore` writes each tenant and name segment through a case escape: every upper-case ASCII letter `X` becomes `^x`; `^` is outside both alphabets, so the mapping is injective, and its output holds no upper-case letter, so two different segments stay different under a case-insensitive comparison; the tree stays readable and the length grows by at most one character per upper-case letter. (B) Restrict tenant ids and names to lower case on every provider, as the #1246 plan does for Key Vault names: no encoding, but a breaking rule for callers whose names are case-sensitive on S3 and Azure. (C) Refuse at start-up a root on a case-insensitive volume (probe: create a file and look it up with another case): safe, but the provider then does not run on the default Windows and macOS file systems, the developer machines of the local provider. **Class:** Dilemma; A wins when names come from users or external systems with mixed case, B when the application controls every name, C when deployments are Linux only. The plan proceeds with A. Tests: a unit test writes tenants `Acme` and `acme` and names `Doc.pdf` and `doc.pdf` on the file system provider and reads four distinct blobs; a property-test clause asserts that two accepted tenant ids, or two accepted names, never map to the same physical path when the paths are compared case-insensitively.
- Names are validated by `BlobAddress`: no `..`, no backslash, no leading `/`, no control characters, at most 512 characters (choice 7). The file system provider additionally resolves the full path and refuses anything outside the root, and keeps its metadata sidecars and temporary files outside the blob tree (Phase 3), so no valid name can reach them.
- `TenantBlobStore` does not rewrite the name: it sets the tenant on the address (`BlobAddress.TenantId`), and the innermost provider composes the physical key `{tenantId}/{name}`. The decorators in between, including the encrypting one, therefore see the tenant and the logical name separately (choice 4, AAD).
- Callers cannot name a tenant through the address: `BlobAddress.TenantId` and `BlobAddress.TenantScope` (`Ambient`, `Explicit` or `DefaultPartition`) have a public getter and an `internal init`, so only `TenantBlobStore` sets them (provider satellites read them). An address that reaches `TenantBlobStore` with a tenant already set (possible only from code inside `Encina.Storage`) and different from the resolved one is refused with `Left(storage.invalid_address)`.
- Where the tenant reaches the chain: `TenantBlobStore` is the outermost layer (Phase 2, decorator order), so the instrumented, audited, guarded and encrypting layers all receive an address with the tenant already set, on the ambient path as on the explicit one.
- Where `ForTenant` lives: on the public interface `IBlobTenantScope` (`IBlobStore ForTenant(string tenantId)`), implemented by `TenantBlobStore`; `AddEncinaStorage` registers the same instance as `IBlobStore` and `IBlobTenantScope`. The returned store is a `TenantBlobStore` bound to the given tenant over the same inner chain; providers and inner decorators do not implement the interface. The tenant id is validated as in the physical layout rule above.
- `ForTenant` is auditable: addresses it produces carry `TenantScope = Explicit`, and `AuditedBlobStore` records the scope with the tenant on every operation, so a background job's cross-request access is visible in the audit.
- The default partition under tenancy (choice 7): `ForTenant("default")` and an ambient tenant `default`, in any case (`StringComparison.OrdinalIgnoreCase`), are refused with `Left(storage.invalid_address)` while tenancy is on, so no real tenant shares the partition. Background code that must reach it (the #122 claim-check sweeper and its system-scope payloads, data written before tenancy was switched on) calls `IBlobStore ForDefaultPartition()` on `IBlobTenantScope`; its addresses carry `TenantScope = DefaultPartition`, recorded by the audit like `Explicit`. With tenancy off the ambient path already uses the default partition and `ForDefaultPartition()` returns the same partition.

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
- The guard decides before the provider deletes, so a hold placed between the decision and the delete is not seen; choice 9 covers that window.

</details>

<details>
<summary><strong>4. Envelope encryption — one AES-GCM pass per blob with a per-blob data key, or a chunked streaming format</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Single-shot AES-GCM per blob: a fresh 256-bit data key per blob, wrapped per choice 6, header `0x01 | wrapping info | wrappedKey length | wrappedKey | nonce | tag`, default size cap 64 MiB (`MaxEncryptedBlobBytes`, choice 7)** | Simple; a per-blob key avoids any limit on the number of encryptions per key; deleting the tenant key makes every blob unreadable (crypto-shredding of files); the format has a version byte for a later chunked version | The blob is buffered in memory for the cipher (bounded by the cap) |
| **B) Chunked AEAD stream (64 KiB chunks, counter-derived nonces, a final-chunk flag)** | Constant memory for very large files; streaming reads | A new format to design, test and review; no consumer needs it (consents and clinical documents are small) |

### Chosen Option: **A — single-shot AES-GCM with a per-blob wrapped data key and a size cap** (recommended, pending the maintainer)

### Rationale

**Class:** Dilemma. Option A wins for documents measured in kilobytes to a few megabytes, the use case of the reference application. Option B wins if the product stores videos or archives through `IBlobStore`.

- We recommend A with the version byte so that B can ship as format version 2 without a migration.
- The wrapped key lives in the blob header, so rotation of the tenant key re-wraps by rewriting the header with a metadata-only update where the provider allows it, or by a copy in place; the exact mechanism per provider is an open question (Research).
- Encryption is a decorator (`EncryptingBlobStore`). Whether it is on by default with a start-up failure when no key source is registered, or opt-in, is choice 8.
- The AAD is `0x01 | tenantId | container | name`, where `name` is the logical name the caller passed (without the tenant prefix) and `tenantId` is the address's tenant set by `TenantBlobStore` (choice 2). The tenant appears once, a blob copied to another name, container or tenant does not decrypt, and a change of the physical layout (the `{tenantId}/{name}` key composed by the provider) does not break decryption.

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

<details>
<summary><strong>6. How the per-blob data key is wrapped — a KMS call per blob, or AES Key Wrap under the tenant data key</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `IKeyWrapper` (#1250) wraps every per-blob key with the tenant's KMS key** | One key level; the KMS sees and can audit every blob key | One KMS round trip per put and per get (latency, cost, KMS quotas, and a KMS outage stops every read); the wrapped key size varies per KMS |
| **B) AES Key Wrap with padding (RFC 5649) of the per-blob key under the tenant data key of #1250** (`ITenantDataKeyService.GetCurrentKeyAsync(tenantId, DataKeyPurpose.Storage)`, itself KMS-wrapped and cached by #1250; the `Storage` member is a cross-plan dependency, see below), with `Aes.EncryptKeyWrapPadded` and `Aes.DecryptKeyWrapPadded` from the .NET 10 BCL; the header stores the tenant key version (4 bytes) and the 40-byte wrapped key | One KMS call per tenant key per cache period; the BCL primitive has its own integrity check, so a tampered wrapped key fails to unwrap; deleting the tenant data key still shreds every blob of the tenant; rotation re-wraps headers with the new key version and no KMS call per blob | Two key levels; the per-blob key is protected by a key that lives in process memory while cached (the same exposure as the column keys of #1250) |

### Chosen Option: **B — RFC 5649 under the tenant data key** (recommended, pending the maintainer)

### Rationale

**Class:** Dilemma. Option B wins on latency, cost and availability for a store whose reads happen on every document view. Option A wins when policy requires that every data key is wrapped by the KMS itself and the KMS logs every unwrap.

- The primitive is in the `net10.0` reference pack (fact 5), so option B does not wait for the .NET 11 move discussed in spike #3661.
- Unit tests of the wrap use the two RFC 5649 section 6 vectors (the 20-octet and the 7-octet key data under the same 192-bit key-encryption key) through `Aes.EncryptKeyWrapPadded`/`Aes.DecryptKeyWrapPadded`, a 32-byte key that wraps to 40 bytes (`Aes.GetKeyWrapPaddedLength(32) == 40`), and a tamper test (a flipped bit in the wrapped key fails to unwrap). RFC 3394 vectors are not used: the BCL has no unpadded key wrap (fact 5), and RFC 5649 uses a different initial value (`A65959A6 || MLI`), so the padded calls cannot reproduce them.
- **Cross-plan dependency (option B): key separation.** `DataKeyPurpose` of #1250 has `Column` and `BlindIndex` only. Option B needs a third member, `Storage`, so that the blob key-encryption key is a separate key per tenant: without it the column key, which #1250 choice 8 uses as HKDF input, would also serve as an AES Key Wrap key, one key for two algorithms. **Explicit dependency on an edit of the merged #1250 plan.** The #1250 plan is already on main, and there its shared-names table (`DataKeyPurpose` (`Column`, `BlindIndex`), line 20) and its PR 3 row (where `DataKeyPurpose` is created, line 305) do not carry `Storage`. The orchestrator adds the member to both places in a docs edit of the #1250 plan before the first #1250 worker brief; PR 3 of this plan waits for #1250 PR 3 with the member. No pull request of this plan changes the public enum of `Encina.Security.Encryption`: if #1250 PR 3 ships without the member, adding it is a #1250 follow-up pull request, and PR 3 of this plan stays blocked until it merges.
- With option A, the header keeps `wrappedKey length | wrappedKey` from the KMS and the #1250 `WrappedKey` type; with option B, the header carries `keyVersion | wrappedKey` (fixed 40 bytes for a 256-bit key).

</details>

<details>
<summary><strong>7. Defaults and public names</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The values below** | Each has a stated reason; all are options the application can change | They are proposals, not derived from a rule |
| **B) Other values chosen by the maintainer** | Fits a deployment the maintainer knows | Needs a reason recorded per value |

### Chosen Option: **A — the values below** (recommended, pending the maintainer)

### Rationale

**Class:** Dilemma (values that no rule fixes).

| Default or name | Value | Reason |
|---|---|---|
| `MaxEncryptedBlobBytes` | 64 MiB | Bounds the memory of the single-shot cipher (choice 4); documents and consents are far smaller |
| `StorageOptions.MaxBlobBytes` | 64 MiB | Same as the encryption cap, so an unencrypted store does not accept blobs the encrypted one would refuse |
| `StorageOptions.PageSize` | 1000 | The maximum page of S3 `ListObjectsV2`; Azure allows more, the file system has no limit |
| Blob name | at most 512 characters; ASCII letters (both cases), digits, `-`, `_`, `.`, `/` as a separator; no `..` segment, no leading `/`, no control characters; the file system provider applies the case escape of choice 2a | Portable across S3 and Azure Blob verbatim and across case-insensitive file systems through the escape, which needs a character (`^`) outside the alphabet |
| Container | 3 to 63 characters; lower-case ASCII letters, digits and `-`; starts with a letter or digit; no `--` | The Azure container name rule; lower case only, so a container never collides by case on any provider |
| Tenant id when tenancy is off (the default partition) | `default`, reserved: under tenancy (`MultiTenancyMarker` registered) `default`, compared case-insensitively, is refused as a tenant id, by `TenantBlobStore` for an ambient tenant and by `ForTenant`, with `Left(storage.invalid_address)`; the default partition is reached under tenancy only through `IBlobTenantScope.ForDefaultPartition()` | The partition outlives the switch to tenancy: data written while tenancy was off stays there, and the #122 claim-check adapter uses it for the sweep and for system-scope payloads. A real tenant named `default` would share that partition with pre-tenancy and system data, so the name is reserved; keeping `default` rather than a name outside the tenant alphabet leaves the tenancy-off layout unchanged |
| `BlobPutOptions.Overwrite` | `false` | An accidental overwrite of a document is a data loss; overwriting is explicit |
| `RefuseWhenOwnerUnknown` (Compliance) | `true` | Fail closed: a blob without an owner cannot be checked for holds (AGENTS.md §3) |
| Log level of `AllowUnencrypted = true` at start-up | Warning, once per start | Visible in every deployment that turns encryption off |

</details>

<details>
<summary><strong>8. Encryption on by default with a start-up failure, or opt-in</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) On by default: `AddEncinaStorage` fails at start-up when no key source of choice 6 is registered, unless `AllowUnencrypted = true` (explicit and logged, choice 7)** | No blob is stored in plaintext by accident; matches REQ-046 ("`IBlobStore` with envelope encryption") as part of the store | A new module requirement: `Encina.Storage` requires `Encina.Security.Encryption` and a key source, which ADR-050 rule 1 only allows through an amendment of its closed list; every dependent package inherits it (the `Encina.ClaimCheck.Storage` adapter of #122) |
| **B) Opt-in: `UseEncryption()` adds the decorator; without it blobs are stored as the provider stores them** | Pay-for-what-you-use (AGENTS.md §1); no ADR-050 change | Plaintext by default for a store that will hold health documents; the gap is silent unless the application opts in |

### Chosen Option: **A — on by default, start-up failure, explicit opt-out** (recommended, pending the maintainer)

### Rationale

**Class:** Dilemma. Both options deliver the encryption REQ-046 asks for; they differ on the default. Option A wins for an application that stores personal data, which is the reference use case. Option B wins for an application that stores only public assets and wants no key management.

- With option A, Phase 11 adds the ADR-050 amendment: a new row `Storage (Encina.Storage) | a key source for envelope encryption (Encina.Security.Encryption, #1250) | | Required unless the explicit, logged AllowUnencrypted is set | Startup validation | #1239`. Without the amendment the requirement does not exist (ADR-050 rule 1).

</details>

<details>
<summary><strong>9. A hold placed while a delete is in flight</strong></summary>

`GuardedBlobStore` reads the blob info, asks the guards, then deletes. A legal hold is stored by `ILegalHoldService`, not on the blob, so a conditional delete on the blob's ETag does not detect it: the blob does not change when a hold is placed.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Accept and document the window; audit both events** | No new mechanism; the window is milliseconds; the audit shows the delete decision and the hold with their times, so a dispute is resolvable | A hold placed inside the window does not stop that delete |
| **B) A distributed lock on the owner entity, taken by the hold placement and by the blob delete** | Closes the window | Requires a lock provider for every storage user (an ADR-050 requirement), and a change to `Encina.Compliance.Retention` to take the same lock |
| **C) Two-phase delete: mark pending, re-check the guard after a grace period, then delete** | Closes the window without a lock | A sweeper and a pending state on every provider; deletes are no longer immediate, which conflicts with erasure deadlines only if the grace period is long |

### Chosen Option: **A — document and audit** (recommended, pending the maintainer)

### Rationale

**Class:** Dilemma. Option A wins when holds are placed by people (seconds to days ahead of deletion) and the audit is the evidence. Option B or C wins when automated processes place holds and delete concurrently.

- The feature guide states the window; the S11 integration test covers a hold placed before the delete.

</details>

<details>
<summary><strong>10. Tenant source — the core request context, or <code>ITenantProvider</code> of <code>Encina.Tenancy</code></strong></summary>

`TenantBlobStore` needs the current tenant. The choice adds or avoids a package edge from `Encina.Storage` core to `Encina.Tenancy` (and, through it, to `Encina.Messaging`, fact 2) for every storage user.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Core abstractions: `IRequestContextAccessor.RequestContext?.TenantId` plus `MultiTenancyMarker`** (both in `Encina`), fail closed as `DeadLetterManager` does | No package edge to `Encina.Tenancy`; the same choice as the sibling plans (#1250 choice 9, #1246 choice 7, #599 choice 9), so the whole family reads the tenant the same way | The tenant is whatever the pipeline put in the request context; an application that resolves tenants only through `Encina.Tenancy` relies on its pipeline to fill the context |
| **B) `ITenantProvider` of `Encina.Tenancy`** | Direct use of the tenancy package's resolution | Every storage user takes `Encina.Tenancy` and `Encina.Messaging`, multi-tenant or not; differs from the sibling plans |

### Chosen Option: **A — core request context and `MultiTenancyMarker`** (recommended, pending the maintainer)

### Rationale

**Class:** Dilemma. Option A wins for a lean dependency graph, for single-tenant users and for one tenant model across the family. Option B wins when every deployment is multi-tenant through `Encina.Tenancy` and the maintainer prefers its resolution over the request context.

- Background code (sweepers, the #122 claim-check adapter) uses `ForTenant` in both options (choice 2).

</details>

<details>
<summary><strong>11. Package edges of the audit and encryption decorators — in the core package, or in satellites</strong></summary>

`AuditedBlobStore` uses `IOperationAuditStore` and `IReadAuditStore` (`Encina.Security.Audit`, which references `Encina.Messaging`) and `EncryptingBlobStore` uses `ITenantDataKeyService` (`Encina.Security.Encryption`, #1250). Where they live decides which packages every storage user takes.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Both decorators in `Encina.Storage` core**, which references `Encina.Security.Audit` and `Encina.Security.Encryption` | One package to install; the same layout as `Encina.Security.Secrets`, whose core hosts its audited decorators (`src/Encina.Security.Secrets/Auditing/`) and references `Encina.Security.Audit`; option A of choice 8 can validate the key source without a satellite | Every storage user takes the audit and encryption packages (and `Encina.Messaging` through the audit package), even one that stores public assets |
| **B) Satellites `Encina.Storage.Audit` and `Encina.Storage.Encryption`**, registered by their own `AddEncinaStorage*` calls | Strict pay-for-what-you-use at package level | Two more packages (csproj, README, PublicAPI, manifest each); with option A of choice 8, the start-up validation must detect a missing satellite rather than a missing key source; the audit of disclosure reads (SPEC-002) would be optional by package |

### Chosen Option: **A — both decorators in the core package** (recommended, pending the maintainer)

### Rationale

**Class:** Dilemma. Option A wins when the store will hold personal data, where audit and encryption are part of the feature (REQ-046, AC-044), and for consistency with `Encina.Security.Secrets`. Option B wins when the maintainer wants storage for public assets without the security packages.

- The decision interacts with choice 8: option B of this choice with option A of choice 8 makes the encryption satellite mandatory in practice.

</details>

---

## Implementation Phases

**Pull request cut.** Phases 7-10 are not pull requests of their own: each item lands with the code it concerns. Counts include tests, PublicAPI files and coverage manifests; every pull request stays within 15 files and 1,500 changed lines. The decorator chain is registered in PR 5, after the last decorator exists.

| PR | Content | Files | Est. lines |
|----|---------|-------|-----------|
| 1 | Phase 1: csproj, `IBlobStore`, `BlobAddress`, `BlobInfo`, `BlobMetadata`, `BlobPutOptions`, `BlobContent`, `BlobPage`, `StorageErrors`, `PublicAPI.Shipped.txt`, `PublicAPI.Unshipped.txt`, `Encina.slnx`; unit and guard tests; manifest `Encina.Storage.json` | 15 | ~900 |
| 2 | Phase 2 (tenant and guard): `StorageOptions`, `IBlobLifecycleGuard`, `BlobOperation`, `CompositeBlobLifecycleGuard`, `TenantBlobStore`, `IBlobTenantScope`, `BlobTenantScope`, `BlobAddress` (`TenantScope` added), `GuardedBlobStore`, PublicAPI; unit tests (composite, tenant with a caller-set tenant refused, reserved `default` refused in any case under tenancy, `ForDefaultPartition`, guarded), guard test, property test (addresses never traverse, tenant prefixes never overlap, no accepted tenant id maps to the default partition); manifest | 15 | ~1,200 |
| 3 | Phase 2 (encryption): `EncryptingBlobStore`, `BlobEncryptionHeader`, `StorageOptions` (encryption options), PublicAPI; unit test (header, tamper, the two RFC 5649 section 6 vectors), property test (round trip); manifest | 7 | ~800 |
| 4 | Phase 2 (audit, resilience, telemetry) and Phase 9: `AuditedBlobStore`, `ResilientBlobStore`, `InstrumentedBlobStore`, `StorageActivitySource`, `StorageMetrics`, `StorageLog` (6050-6079), `EventIdRanges.cs` (`StorageCore`, `StorageProviders`), `EncinaEventIdAllocationTests.cs`, PublicAPI; unit tests (audited, resilient, instrumented with the AC-044 exporter test); manifest | 14 | ~1,300 |
| 5 | Phase 7: `ServiceCollectionExtensions` (`AddEncinaStorage`, chain in the order of Phase 2), `StorageStartupValidator` (choice 8), PublicAPI; DI test (`ValidateOnBuild`, `ValidateScopes`), validator unit test; manifest | 6 | ~600 |
| 6 | Phase 3: `FileSystemBlobStore`, `FileSystemStorageOptions`, `FileSystemBlobStoreHealthCheck`, registration, PublicAPI; unit and guard tests; `IBlobStore` contract base and its file-system subclass (ContractTests); integration test on a temp directory; manifest | 11 | ~1,300 |
| 7 | Phase 4: `Encina.Storage.S3` csproj, `S3BlobStore`, `S3StorageOptions` (with `Validate`), registration, `S3StorageLog` (6100-6124), health check, README, 2 PublicAPI files, `Directory.Packages.props` (`AWSSDK.S3`), `Encina.slnx`, `EncinaEventIdAllocationTests.cs`; unit and guard tests; manifest | 15 | ~1,100 |
| 8 | Phase 5: `Encina.Storage.AzureBlob` (same shape, log 6125-6149, no package version change) | 14 | ~1,000 |
| 9 | Integration of the cloud providers and Phase 8: `LocalStackFixture` (S3 client), contract subclass and integration test for S3 (LocalStack) and Azure Blob (Azurite), two-tenant isolation test on every provider (AC-043); `FakeBlobStore`, its PublicAPI, contract subclass and manifest in `Encina.Testing.Fakes` | 10 | ~1,100 |
| 10 | Phase 6: `Encina.Storage.Compliance` csproj, `LegalHoldBlobGuard`, registration, `StorageComplianceLog` (6080-6099), README, 2 PublicAPI files, `Encina.slnx`, `EncinaEventIdAllocationTests.cs`; unit and guard tests, S11 integration test; manifest | 13 | ~1,000 |
| 11 | Phase 11: ADR 067, ADR-050 amendment (choice 8, option A), `docs/features/storage.md`, `docs/INVENTORY.md`, `ROADMAP.md`, changelog fragment, load and benchmark justification files | 8 | ~700 |

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
2. **`BlobAddress.cs`** (`Container`, `Name`, validation per Design Choice 2; `TenantId` with a public getter and an `internal init`, set only by `TenantBlobStore`, choice 2; PR 2 adds `TenantScope` the same way), **`BlobInfo.cs`** (address, `SizeBytes`, `ContentType`, `CreatedAtUtc`, `ModifiedAtUtc`, `ETag`, `BlobMetadata`), **`BlobMetadata.cs`** (`OwnerEntityType`, `OwnerEntityId`, `RetentionCategory`, `Tags`), **`BlobPutOptions.cs`** (`ContentType`, `Overwrite`, `Metadata`), **`BlobContent.cs`** (`Stream`, `BlobInfo`, `IAsyncDisposable`), **`BlobPage.cs`**.
3. **`StorageErrors.cs`** (codes of Design Choice 5). **`StorageOptions.cs`** (`DefaultContainer`, `MaxBlobBytes`, `AllowUnencrypted`, `PageSize`, defaults of choice 7) with `Validate` lands in PR 2.
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
- the plan of #1239 (linked from the issue)
```

</details>

---

### Phase 2: Decorators and the lifecycle guard

> **Goal**: Tenant partition, guard, encryption, audit, resilience and telemetry as composable decorators.

<details>
<summary><strong>Tasks</strong></summary>

1. **`IBlobLifecycleGuard.cs`**, **`BlobOperation.cs`** (`Read`, `Overwrite`, `Delete`), **`CompositeBlobLifecycleGuard.cs`** (ordered, first `Left` wins, a thrown exception becomes `Left(storage.delete_refused|read_refused)`).
2. **`TenantBlobStore.cs`** (Design Choices 2 and 10), **`IBlobTenantScope.cs`** (`IBlobStore ForTenant(string tenantId)`, `IBlobStore ForDefaultPartition()`) and **`BlobTenantScope.cs`** (`Ambient`, `Explicit`, `DefaultPartition`): sets `BlobAddress.TenantId` and `TenantScope` from the request context (choice 10), from `ForTenant` or from `ForDefaultPartition`; refuses an address that arrives with a different tenant already set (`storage.invalid_address`); under tenancy refuses the reserved tenant id `default`, compared with `StringComparison.OrdinalIgnoreCase`, from the ambient context and from `ForTenant` (`storage.invalid_address`, choice 7); the name stays logical and the provider composes the physical key. Tests: `ForTenant("default")`, `ForTenant("Default")` and an ambient `DEFAULT` under tenancy return the `Left`; `ForDefaultPartition()` reaches the partition written with tenancy off.
3. **`GuardedBlobStore.cs`**: `GetInfoAsync` then `AuthorizeAsync` before delete, overwrite and read; the window between the decision and the delete is choice 9.
4. **`EncryptingBlobStore.cs`**: header format of Design Choice 4, per-blob key wrapped per choice 6 (recommended: `Aes.EncryptKeyWrapPadded` under the tenant data key of #1250's `ITenantDataKeyService`), AAD over the tenant and the logical name; refuses to read an unencrypted blob unless `AllowUnencrypted`.
5. **`AuditedBlobStore.cs`**: deletes and refusals to `IOperationAuditStore`; disclosure reads to `IReadAuditStore`; the audit carries tenant, tenant scope (`Ambient`, `Explicit` or `DefaultPartition`, so `ForTenant` and `ForDefaultPartition` use is recorded), container, a hash of the name and the outcome, never the name or content.
6. **`ResilientBlobStore.cs`**: retry transient errors (pattern `ResilientSecretReaderDecorator`, ADR-055).
7. Decorator order, outermost to innermost: tenant, instrumented, audited, guarded, encrypting, resilient, provider. The tenant layer is outermost so that every other layer sees the tenant (choice 2); its own `storage.tenant_required` refusals are logged by code and counted in `StorageMetrics` by `TenantBlobStore` itself.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of issue #1239 in Encina: the IBlobStore decorators and the lifecycle guard.

CONTEXT:
- IBlobStore and the models exist (Phase 1). ITenantDataKeyService and IKeyWrapper come from the #1250 plan (src/Encina.Security.Encryption/).
- AES Key Wrap with padding (RFC 5649) is in the .NET 10 BCL: Aes.EncryptKeyWrapPadded / Aes.DecryptKeyWrapPadded. Test it with the
  RFC 5649 section 6 vectors; a tampered wrapped key must fail to unwrap and surface as Left(storage.encryption_failed).
- The AAD is 0x01 | tenantId | container | logical name (TenantBlobStore sets TenantId; the name is never prefixed before the provider).
- IRequestContextAccessor.RequestContext?.TenantId and MultiTenancyMarker decide the tenant (choice 10, recommended; do not
  reference Encina.Tenancy); ForTenant lives on IBlobTenantScope, implemented by TenantBlobStore, the outermost layer.
  Under tenancy the tenant id "default" is reserved (refused from the ambient context and from ForTenant with
  Left(storage.invalid_address)); background code reaches the default partition through IBlobTenantScope.ForDefaultPartition().
  BlobAddress.TenantId and TenantScope are internal init. IOperationAuditStore and IReadAuditStore are in src/Encina.Security.Audit/Abstractions/.
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
- src/Encina/Abstractions/IRequestContextAccessor.cs and src/Encina.Messaging/DeadLetter/DeadLetterManager.cs (ambient tenant, fail closed)
```

</details>

---

### Phase 3: File system provider

> **Goal**: A local provider with the same contract, safe against path traversal.

<details>
<summary><strong>Tasks</strong></summary>

1. **`FileSystemBlobStore.cs`**: root directory from `FileSystemStorageOptions.RootPath` (validated: exists or is created, absolute); the full path of every address is resolved with `Path.GetFullPath` and refused when it is not under the root; tenant and name segments go through the case escape of Design Choice 2a; writes go to a temporary file under `<root>/.encina/tmp/` and are moved atomically (same volume); metadata in a sidecar `<root>/.encina/meta/<escaped path>.json` (written atomically before the data file is made visible). The `.encina/` tree is outside the blob tree: the first path segment of a blob is a container or tenant id, whose alphabets have no `.`, so no valid name reaches a sidecar or a temporary file, and listing walks only the blob tree, so it never returns them. Listing by prefix with a continuation token; `TimeProvider` for timestamps. Tests: tenants `Acme` and `acme` and names `Doc.pdf` and `doc.pdf` are four distinct blobs (choice 2a); a blob named `report.pdf.meta.json` put with `Overwrite = true` next to `report.pdf` leaves the metadata of `report.pdf` unchanged, and listing returns the two blobs and no sidecar or temporary file.
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
- NTFS and APFS are case-insensitive: tenant and name segments go through the case escape (upper-case X becomes ^x, plan
  choice 2a), so Acme/acme and Doc.pdf/doc.pdf stay distinct; test both pairs.
- Sidecars and temporary files live under <root>/.encina/, outside the blob tree; listing never returns them; test that a
  blob named report.pdf.meta.json does not touch the metadata of report.pdf.
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
- the plans of #1187 (retention floor) and #1189 (blocked data state), linked from those issues
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
4. Choice 8 (recommended option A): encryption enabled by default when the key source of choice 6 is registered; without it and without `AllowUnencrypted`, start-up fails with a message naming the option. This is a new requirement in the closed list of ADR-050 (rule 1), so it ships together with the ADR-050 amendment of Phase 11. If the maintainer picks option B, this task becomes the `UseEncryption()` registration and no amendment is needed.

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
- Start-up fails (not a silent default) when encryption is required but not available (choice 8, option A, with the ADR-050
  amendment row of Phase 11).

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

1. **EventIds**: register `StorageCore = (6050, 6099)` and `StorageProviders = (6100, 6149)` in `EventIdRanges.cs`; add `Encina.Storage`, `Encina.Storage.S3` (6100-6124), `Encina.Storage.AzureBlob` (6125-6149) and `Encina.Storage.Compliance` (6080-6099, inside `StorageCore`; the core uses 6050-6079) to `EncinaEventIdAllocationTests`.
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
| Property | encrypt-decrypt round trip, tenant prefixes never overlap, two accepted tenant ids or names never map to the same file system path compared case-insensitively (choice 2a), no accepted tenant id in any case under tenancy maps to the default partition, addresses never traverse | 75: invariants |
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
3. ADR (next free number; 067 on 2026-11-11, reserved in the ADR index when work starts): the guard hook, tenant model, envelope format and key wrapping (choice 6), package layout, the hold window (choice 9).
4. ADR-050 amendment (only with choice 8, option A): the row `Storage (Encina.Storage)` requiring a key source for envelope encryption unless `AllowUnencrypted` is set, mechanism start-up validation, source #1239; the amendment note names #122 as an inheriting dependent.
5. READMEs for the four packages; `docs/features/storage.md` (configuration, providers, tenant partition, retention and hold behavior including the window of choice 9, encryption, limits); `docs/INVENTORY.md`; `ROADMAP.md`.
6. Follow-up issues: retention floor and blocked-state guards (after #1187 and #1189), Google Cloud Storage (post-1.0), chunked encryption format version 2 (if needed).
7. `dotnet build --configuration Release` with 0 warnings, `dotnet test`, every flag at target.

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
| RFC 5649 (NIST SP 800-38F, KWP) | AES Key Wrap with padding; `Aes.EncryptKeyWrapPadded`, `Aes.DecryptKeyWrapPadded` and `Aes.GetKeyWrapPaddedLength` in the .NET 10 BCL (no unpadded RFC 3394 variant, fact 5) | Per-blob key wrapping (choice 6) and its section 6 test vectors |
| ADR-050 | Rule 1: the closed list of module requirements; a new requirement is an amendment | Choice 8 |
| Comparable products | Azure Blob Storage SDK, AWS SDK for .NET S3, MinIO, Apache Commons VFS | Contract shape; server-side encryption is complementary, not a replacement |
| AGENTS.md §3, §5 | Fail closed, ROP, registration completeness; cloud category | Rules applied |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| IRequestContextAccessor and MultiTenancyMarker | src/Encina/Abstractions/IRequestContextAccessor.cs, src/Encina/Abstractions/MultiTenancyMarker.cs | tenant of every blob address (choice 10, recommended; `ITenantProvider` in src/Encina.Tenancy/Abstractions/ITenantProvider.cs is option B) |
| MultiTenancyMarker | src/Encina/Abstractions/MultiTenancyMarker.cs | signal that tenancy is on, so a missing tenant fails closed |
| ILegalHoldService | src/Encina.Compliance.Retention/Abstractions/ILegalHoldService.cs | hold check before delete |
| IRetentionFloorQuery (planned by #1187) | the plan of #1187 | retention floor check before delete |
| IBlockedDataFilterContext (planned by #1189) | the plan of #1189 | blocked state and the audited disclosure scope |
| IOperationAuditStore and IReadAuditStore | src/Encina.Security.Audit/Abstractions/ | audit of deletes, refusals and disclosures |
| ITenantDataKeyService, IKeyWrapper and WrappedKey (planned by #1250) | the plan of #1250 | tenant data key that wraps the per-blob key (choice 6) |
| Aes.EncryptKeyWrapPadded / DecryptKeyWrapPadded | .NET 10 BCL, System.Security.Cryptography | RFC 5649 wrap of the per-blob key |
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
| Family of plans | 6000-6049 claim check (#599, #122), 6150-6199 claim check providers (#122), 6200-6249 column encryption (#1250) | Disjoint, in the family block 6000-6249 of the batch registry |

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
| Total | 93 | Distinct files; the per-PR table under Implementation Phases counts 125 file changes over 11 pull requests, because some files (PublicAPI, manifests, `EncinaEventIdAllocationTests.cs`, `Encina.slnx`) change in several |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
PROJECT CONTEXT:
You are implementing issue #1239 in Encina (.NET 10, C# 14, nullable enabled, pre-1.0, no backward compatibility): the Encina.Storage
package family with IBlobStore (SPEC-002 REQ-046). Rules: AGENTS.md. ITenantDataKeyService and IKeyWrapper come from the #1250 plan;
Encina.ClaimCheck.Storage (#122) builds on this package.

IMPLEMENTATION OVERVIEW:
1. Contract and models: IBlobStore, BlobAddress (validated), BlobInfo, BlobMetadata, BlobPutOptions, BlobContent, BlobPage, StorageErrors, StorageOptions.
2. Decorators: tenant partition, outermost (ambient tenant from IRequestContextAccessor + MultiTenancyMarker per choice 10, ForTenant and ForDefaultPartition on
   IBlobTenantScope, "default" reserved under tenancy, TenantId internal init), lifecycle guard (composite), single-shot AES-GCM envelope encryption with a per-blob
   key wrapped by RFC 5649 (Aes.EncryptKeyWrapPadded, .NET 10 BCL; no RFC 3394 API exists) under the tenant data key of #1250
   (DataKeyPurpose.Storage, choice 6), audit (records the tenant scope), resilience, telemetry.
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
| 10 | Multi-Tenancy | ✅ | Phase 2: tenant prefix (tenant source of choice 10), fail closed, default tenant when tenancy is off and `default` reserved (refused as a tenant id in any case) under tenancy, case-escaped file system paths (choice 2a), `ForTenant` and `ForDefaultPartition` for background code recorded in the audit, caller-set tenants refused; two-tenant test on every provider (AC-043) |
| 11 | Module Isolation | ❌ | SPEC-002 requires no module scoping; `ModuleId` in messaging stays with #747 |
| 12 | Audit Trail | ✅ | Phase 2: deletes, hold refusals and disclosures through `IOperationAuditStore` and `IReadAuditStore` |

---

## Prerequisites & Dependencies

### Advisable before or with this work

- #1250 plan, by pull request: PR 3 of this plan (the encryption decorator, choice 6 B) needs #1250 PR 1 (`IKeyWrapper`, `WrappedKey`), PR 3 (`ITenantDataKeyService`, `DataKeyPurpose` with the `Storage` member of choice 6, the in-memory key store) and PR 4 (the key cache and `LocalKeyWrapper`, which the unit tests use); PR 5 of this plan (start-up validation of choice 8) needs #1250 PR 5 (its `ServiceCollectionExtensions`, which registers the key service). The KMS wrappers (#1250 PR 6 and PR 7) are needed only by deployments, not by any pull request of this plan. This matches #1250's own note that its PR 1-5 come first. The other pull requests of this plan do not wait for #1250.
- #1250 shared names: `DataKeyPurpose` gains the `Storage` member (choice 6, cross-plan dependency). The merged #1250 plan on main lists `Column` and `BlindIndex` only (shared-names table and PR 3 row), so the orchestrator edits that plan before the first #1250 worker brief; PR 3 of this plan is blocked until #1250 PR 3 (or a #1250 follow-up pull request) ships the member, and no pull request of this plan adds it.
- #1187 (retention floor) and #1189 (blocked data state): the floor and blocking adapters of Phase 6 wait for them; the legal hold adapter does not.
- #1257 (multi-tenancy of the SPEC-002 capabilities): REQ-061 for blob storage is implemented here; #1257 tracks the cross-feature verification.

### Parallel and follow-up work

- #122: `Encina.ClaimCheck.Storage` builds on `IBlobStore` and inherits the encryption default of choice 8; #600 is covered by it.
- Google Cloud Storage: post-1.0, the recorded gap of the cloud triangle (follow-up issue at Phase 11).

---

## Next Steps

1. The maintainer answers the Dilemmas of this plan (choices 2, 3, 4, 6, 7, 8, 9, 10 and 11); the orchestrator adds the Maintainer Decisions section.
2. Link this plan from #1239 and reserve the ADR number in `docs/architecture/adr/index.md`.
3. Order: PR 1, PR 2, then PR 3 (after #1250 PR 1, PR 3 and PR 4) and PR 4 in parallel, PR 5 after PR 2-4; PR 6-8 after PR 5 in parallel; PR 9 after PR 6-8; PR 10 after PR 5; PR 11 last.
4. Open the follow-up issues of Phase 11 task 6 when the corresponding dependencies are merged.
