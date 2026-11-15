# Implementation Plan: `Encina.DistributedLock.AzureBlob` — distributed lock provider on Azure Blob Storage leases

> **Issue**: [#209](https://github.com/dlrivada/Encina/issues/209)
> **Type**: Feature
> **Complexity**: Medium (one new provider package, 4 pull requests)
> **Estimated Scope**: ~700-850 lines of production code + ~1,200-1,500 lines of tests
> **Specification**: [SPEC-000](../specifications/SPEC-000-encina-1.0-baseline-and-release-scope.md) REQ-027, DEC-003, DEC-009, AC-027
> **Decision**: [ADR-052](../architecture/adr/052-distributed-lock-failure-contract.md) (lock failure contract), [ADR-062](../architecture/adr/062-error-category-contract.md) (error categories)
> **Depends on**: [#2667](https://github.com/dlrivada/Encina/issues/2667) (implements ADR-052; open, not a pending maintainer decision; it blocks the other lock providers)
> **Related**: [#207](https://github.com/dlrivada/Encina/issues/207), [#208](https://github.com/dlrivada/Encina/issues/208), [#210](https://github.com/dlrivada/Encina/issues/210) (same family), [#226](https://github.com/dlrivada/Encina/issues/226) (Redis RedLock), [#741](https://github.com/dlrivada/Encina/issues/741) (tenant-scoped locks), [#218](https://github.com/dlrivada/Encina/issues/218) (fencing tokens), [#223](https://github.com/dlrivada/Encina/issues/223) (auto-extend), [#724](https://github.com/dlrivada/Encina/issues/724) (lock observability), [#755](https://github.com/dlrivada/Encina/issues/755) (health checks), [#2405](https://github.com/dlrivada/Encina/issues/2405), [#2724](https://github.com/dlrivada/Encina/issues/2724)

---

## Summary

Add the package `Encina.DistributedLock.AzureBlob`, an `IDistributedLockProvider` over **Azure Blob Storage leases**. It is the Azure entry of the seven lock providers SPEC-000 DEC-009 fixes for 1.0; its tests run against Azurite (SPEC-000 AC-027).

1. **One blob per resource, one lease per hold.** The lock key maps to a zero-length blob in a lock container; `BlobLeaseClient.AcquireAsync` takes the exclusive lease; a `409 LeaseAlreadyPresent` is contention.
2. **Renewal inside the handle.** A lease lasts 15 to 60 seconds (or infinite), but the caller's `expiry` may be longer. The handle takes a short lease (`LeaseDuration`, default 30 s), renews it every `RenewInterval` until its own deadline (`ExpiresAtUtc`), and then releases it. A crashed process stops renewing, and Azure frees the lock within one lease duration.
3. **Poll, do not block.** Acquire retries every `retry` until the `wait` deadline; delays use `TimeProvider`.
4. **ADR-052 outcomes.** Acquired is `Right(Some(handle))`, contention or the wait elapsing is `Right(None)`, a storage fault is `Left`, cancellation propagates.
5. **Non-intrusive probe.** `IsLockedAsync` reads the blob's `LeaseState`; it never takes the lease to test it.
6. **Operations.** A health check, `[LoggerMessage]` EventIds 7570-7578, an `ActivitySource` and a `Meter`; the Azure SDK retry policy handles transient storage errors.

**Standards**: Azure Storage "Lease Blob" REST operation and the blob lease documentation (lease duration 15 to 60 seconds or infinite; acquire, renew, release, break; lease id); ADR-052; SPEC-000 REQ-027 / AC-027.

**Estimated scope**: 10 new production files (csproj, options, options validator, blob name, gateway seam and its Azure adapter, provider, diagnostics, health check, registration), 4 modified metadata files, about 14 test files, README and changelog fragment; 4 pull requests.

**Affected packages**: `Encina.DistributedLock.AzureBlob` (new); `Encina` (EventId registry and its `PublicAPI.Unshipped.txt`); test projects `Encina.UnitTests`, `Encina.GuardTests`, `Encina.ContractTests`, `Encina.IntegrationTests`, `Encina.PropertyTests` (project references and tests). `Encina.DistributedLock` is consumed, not changed.

**Provider category**: Distributed locks (`AGENTS.md` section 5); it is also a cloud-tied provider, so it follows the "tested with a local emulator" rule of the lock table (Azurite). Matrix in Research.

**What the code does today** (`origin/main` at `db1e7f688`):

- The package does not exist. `Azure.Storage.Blobs` 12.30.1 and `Azure.Identity` 1.21.0 are in `Directory.Packages.props` and no `src/` project references `Azure.Storage.Blobs` yet; `Testcontainers.Azurite` 4.15.0 is there too.
- `AzuriteFixture` (`tests/Encina.TestInfrastructure/Fixtures/AzuriteFixture.cs`, image `azurite:3.37.0`, started with `--skipApiVersionCheck`, exposes `ConnectionString`, `BlobEndpoint`, `BlobServiceClient`) and the collection `Cloud-Azurite` (`tests/Encina.IntegrationTests/Infrastructure/Azurite/AzuriteCollection.cs`) exist.
- `IDistributedLockProvider` still has the pre-ADR-052 shapes; [#2667](https://github.com/dlrivada/Encina/issues/2667) changes them and creates `LockErrorCodes`. **This plan is written to the ADR-052 contract; implementation starts after #2667 merges.**
- `SqlServerDistributedLockProvider` is the structural model (handle per acquisition, `GetLockKey`, health registration). The plans of [#207](https://github.com/dlrivada/Encina/issues/207) and [#208](https://github.com/dlrivada/Encina/issues/208) design the database providers with the same registry and expiry rules.
- No contract test class for locks exists yet; #2667 creates the suite.

---

## Design Choices

<details>
<summary><strong>1. Package placement and name</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) New package `Encina.DistributedLock.AzureBlob`** | The name SPEC-000 REQ-027 and the issue use; the Azure SDK only for those who ask; same shape as the other provider packages | One more package, README and manifest |
| **B) Inside a future Azure storage package** | Shares the Azure client with other Azure features | No such package exists; mixes a coordination feature into a storage package |

### Chosen Option: **A — new package `Encina.DistributedLock.AzureBlob`**

### Rationale

- SPEC-000 REQ-027 names Azure Blob (#209) in the lock set, and every provider is its own `Encina.DistributedLock.{Backend}` package.

**Class:** Settled
**Settled by:** SPEC-000 REQ-027 and DEC-009; `AGENTS.md` section 5 (lock table lists `Azure Blob (#209)`)

</details>

<details>
<summary><strong>2. Lease renewal — inside each handle, in one hosted service, or an infinite lease</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Each handle renews its own lease with a `TimeProvider` timer every `RenewInterval` and releases at its deadline** | No hosted service to register (the registration stays complete on its own); the renewal lives and dies with the handle; `FakeTimeProvider` tests it | One timer per live handle; a process with thousands of simultaneous locks has thousands of timers |
| **B) One `BackgroundService` renews every registered handle** (the issue's wording) | One loop for any number of locks | A hosted service must be registered and started for the provider to be correct; handles need a shared registry and a lifetime that the service owns; a stopped service silently stops renewing (the lease then expires under a live holder) |
| **C) An infinite lease (`-1`), released explicitly, with the deadline kept client-side** | No renewal at all | A crashed process leaves the blob leased forever until an operator breaks it: the lock never auto-releases |

### Chosen Option: **A — renewal inside each handle** (recommended, pending the maintainer)

### Rationale

- The interface promises auto-release (the process crash case) and the provider must work without a hosted service (registration completeness, `AGENTS.md` section 3). Option B wins when an application holds thousands of concurrent locks per process; the maintainer can pick it when work starts, and the handle then registers with the service instead of arming a timer.
- The timer callback catches every exception and logs only the exception type (`AGENTS.md` section 3). A renewal that fails with `LeaseIdMismatch` or `LeaseLost` marks the handle lost: `IsReleased` becomes true, a Warning is logged, and `ExtendAsync` answers `Right(false)`. A transient renewal failure is retried on the next tick while the lease is still valid by the handle's clock.
- Lease duration: `LeaseDuration` defaults to 30 s and is validated in 15..60 s; `RenewInterval` defaults to 10 s and must be below a third of the lease. When the caller's `expiry` is shorter than the lease, the lease is still the minimum 15 s and the handle releases explicitly at its deadline; a crash then frees the lock after at most the lease duration, which is longer than the `expiry` and is documented.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Blob name and container</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Blob name `{LockPrefix}/{SHA-256 hex}` of the logical key `{KeyPrefix}:lock:{resource}`; one container (`ContainerName`, default `encina-locks`)** | Fits the 1024-character blob-name limit and the character rules for any resource; injective up to the hash (collision about 2^-256); a tenant-qualified name composed by #741 works unchanged | Names are opaque in the portal; operators correlate through the log |
| **B) The raw resource as the blob name with percent-encoding** | Readable | A long tenant-qualified resource can exceed 1024 characters; blob-name normalization (trailing dots and slashes, case) can map two resources to one blob |

### Chosen Option: **A — hashed blob name in one container**

### Rationale

- A collision would only make two resources share a lock (safe direction). Tenant isolation is by key: per-tenant containers (the issue's deferred idea) are not needed because the decorator of [#741](https://github.com/dlrivada/Encina/issues/741) qualifies the key with the reserved `default` tenant segment and an injective encoding.
- The `LockPrefix` is the fixed folder `locks`; the options `KeyPrefix` is part of the hashed key, as in every provider.

**Class:** Trivial
**Settled by:** `src/Encina.DistributedLock.SqlServer/SqlServerDistributedLockProvider.cs` (`GetLockKey` format shared by the family); Azure Storage naming rules for blobs (1024-character limit)

</details>

<details>
<summary><strong>4. Authentication and client registration</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Two registration overloads: a connection string, and an application-supplied `BlobServiceClient`** | The connection string suits Azurite and shared-key setups; a supplied client carries managed identity (`DefaultAzureCredential`) or any `TokenCredential` and its own `RetryOptions` | Two overloads to test |
| **B) Connection string only** | One overload | No managed identity, which production Azure deployments use; the secret would be the only credential |

### Chosen Option: **A — connection string and `BlobServiceClient` overloads**

### Rationale

- The options class holds the connection string as a secret: `[JsonIgnore]` and a `ToString()` that never prints it (`AGENTS.md` section 3). A supplied client keeps credentials out of Encina's options entirely.

**Class:** Settled
**Settled by:** `AGENTS.md` section 3 (options with a connection string or key: `[JsonIgnore]`, `ToString()` override)

</details>

<details>
<summary><strong>5. Container and blob creation</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Create the container once (`CreateIfNotExists`, option `CreateContainerIfNotExists`, default true) and the blob lazily on the first acquire (an `If-None-Match: *` upload of zero bytes)** | Works out of the box, as the issue's task "auto-create blob if not exists" asks; the same default as the DynamoDB provider of [#210](https://github.com/dlrivada/Encina/issues/210) | The identity needs create-container permission unless the flag is set to false |
| **B) The container must exist; only the blob is created** | Least privilege: the identity needs blob-level rights only | A missing container is a startup failure the application must provision first |

### Chosen Option: **A — create the container once, the blob lazily** (recommended, pending the maintainer)

### Rationale

- The default follows the issue. Option B wins in locked-down environments where infrastructure code owns containers; with `CreateContainerIfNotExists = false` the provider behaves as B, so the maintainer's choice is only the default value.
- A missing blob on acquire (404) is not an error: the provider creates it and retries once. A missing container with the flag false is a `Left` (provisioning failure), never a silent "free".

**Class:** Dilemma

</details>

<details>
<summary><strong>6. Probe, provider-level extend, seam and fencing</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `IsLockedAsync` reads `BlobProperties.LeaseState`; provider `ExtendAsync(resource)` extends only a handle this provider instance holds, else `Right(false)`; an `internal` seam `IBlobLeaseGateway` with one Azure adapter holds all SDK calls; the lease id is not offered as a fencing token** | No lease is taken to test one; ownership is real; renewal, expiry and error mapping run in unit tests against a fake gateway | One more internal file |
| **B) Probe by trying to acquire and releasing; `ExtendAsync` answers `Right(true)`; SDK types used inline; the lease id exposed as the fencing token** | Fewer files | The probe causes false contention (the defect of #2722 elsewhere); `Right(true)` for an unchecked lock is #2405; `BlobClient` calls are only testable through Azurite; a lease id is a client-proposed GUID, not a monotonic number, so it cannot order holders |

### Chosen Option: **A — `LeaseState` probe, local registry, gateway seam, fencing left to #218**

### Rationale

- ADR-052 defines `ExtendAsync` as `Right(false)` when the lock is "not held". Fencing tokens are the subject of [#218](https://github.com/dlrivada/Encina/issues/218); a monotonic token on Azure would need a counter in the blob metadata updated under the lease, which is input for that issue and not part of this plan.
- The blocking `crap-gate` needs the provider logic covered by fast tests (`AGENTS.md` section 9).

**Class:** Settled
**Settled by:** ADR-052 ("not held" is `Right(false)`); #2405 and #2722 (the defects option B reproduces); #218 (fencing tokens are its scope)

</details>

<details>
<summary><strong>7. Error mapping and retries</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Classify once in the gateway: a `RequestFailedException` with status 409 and error code `LeaseAlreadyPresent` is `Right(None)`; status 404 on acquire creates the blob and retries once; any other storage failure (network, authentication, 5xx, throttling after the SDK retries) is `Left` with the backend-fault code of `LockErrorCodes`; cancellation propagates; the SDK `RetryOptions` of the client handle transient errors** | One mapping; contention is never a failure; resilience comes from the SDK retry policy | Needs the exact code names of #2667 (read before editing) |
| **B) Catch `Exception` in the provider and wrap Polly around it** | Smaller | Collapses cancellation and programming errors into `Left`; doubles the retries of the SDK |

### Chosen Option: **A — classify in the gateway, the SDK retries transient failures**

### Rationale

- This plan adds **no new error code**: every `Left` carries the backend-fault code #2667 defines, whose category follows ADR-062 rule R4 (provider failures are `Internal`); cancellation is never a `Left` (R5). The error never carries the exception message; logs carry the exception type through `ForLogging()` and the storage error code string, never the response body.

**Class:** Trivial
**Settled by:** ADR-052; ADR-062 rules R4 and R5; `AGENTS.md` section 3 (`EncinaError.Message` rule)

</details>

---

## Implementation Phases

Four pull requests, each at most 15 files and 1500 changed lines counting tests, `PublicAPI.*.txt`, the coverage manifest, project references, the README and the changelog fragment (`AGENTS.md` section 11). **PRs 2 to 4 build only after [#2667](https://github.com/dlrivada/Encina/issues/2667) merges**; PR 1 does not depend on it.

| PR | Phases | Production and metadata files | Test and doc files | Files / lines (approx.) |
|---|---|---|---|---|
| 1 | Phase 1 and the EventId registration of Phase 5 | csproj, `AzureBlobLockOptions`, `AzureBlobLockName`, two `PublicAPI` files, `Encina.slnx`, `EventIdRanges.cs`, `Encina` `PublicAPI.Unshipped.txt`, manifest (9) | `EncinaEventIdAllocationTests`, `Encina.UnitTests.csproj` reference, `AzureBlobLockOptionsTests`, `AzureBlobLockNameTests` (4) | 13 files / 650 |
| 2 | Phases 2 and 5 (logs, diagnostics) | `IBlobLeaseGateway`, `AzureBlobLeaseGateway`, `AzureBlobDistributedLockProvider`, `AzureBlobLockDiagnostics`, `PublicAPI.Unshipped.txt`, manifest (6) | `FakeBlobLeaseGateway`, provider unit tests, handle and renewal unit tests, guard tests, `Encina.GuardTests.csproj` reference (5) | 11 files / 1,350 |
| 3 | Phase 3 | `AzureBlobLockOptionsValidator`, `ServiceCollectionExtensions`, `Health/AzureBlobDistributedLockHealthCheck`, `PublicAPI.Unshipped.txt`, manifest (5) | DI test with `ValidateOnBuild`/`ValidateScopes` (all overloads), validator and health check unit tests, guard tests (3) | 8 files / 850 |
| 4 | Phases 4, 6 and 7 | none | contract class and `Encina.ContractTests.csproj` reference, integration tests on `Cloud-Azurite` and `Encina.IntegrationTests.csproj` reference, property test of the blob name and `Encina.PropertyTests.csproj` reference, README, changelog fragment, two load/benchmark justification files, manifest (11) | 11 files / 900 |

If a PR measures over 15 files or 1,500 lines, the worker splits it again and never asks for `size-exception-approved`. PR 2 has the least slack: if the renewal tests grow, they move to PR 3.

### Phase 1: Package skeleton, options and blob name

> **Goal**: The package builds with its configuration surface and the name function, with their tests.

<details>
<summary><strong>Tasks</strong></summary>

#### `src/Encina.DistributedLock.AzureBlob/`

1. **`Encina.DistributedLock.AzureBlob.csproj`** (new) — modeled on `Encina.DistributedLock.SqlServer.csproj`: `ProjectReference` to `Encina.DistributedLock`, `PackageReference` `Azure.Storage.Blobs` (central version), PublicApiAnalyzers, `InternalsVisibleTo` for `Encina.UnitTests`, `Encina.IntegrationTests`, `Encina.ContractTests`, `Encina.PropertyTests`; a new GUID
2. **`AzureBlobLockOptions.cs`** (new, public sealed) — `: DistributedLockOptions`; `[JsonIgnore] string? ConnectionString`; `string ContainerName = "encina-locks"`; `bool CreateContainerIfNotExists = true`; `TimeSpan LeaseDuration = 30 s`; `TimeSpan RenewInterval = 10 s`; `ToString()` prints the container and lease settings, never the connection string; an `IValidateOptions<AzureBlobLockOptions>` rule set in Phase 3 (lease 15..60 s, renew below a third of the lease, valid container name)
3. **`AzureBlobLockName.cs`** (new, internal static) — `Compose(prefix, resource)` (the `lock:` format of the SQL Server provider) and `ToBlobName(composedKey)` (`locks/` plus lower-case hex SHA-256)
4. **`PublicAPI.Shipped.txt`** (new, `#nullable enable`) and **`PublicAPI.Unshipped.txt`**
5. **`Encina.slnx`** (modify) and **`.github/coverage-manifest/Encina.DistributedLock.AzureBlob.json`** (new, written by `mechanical-fixer` from the target table of Phase 6)
6. **`tests/Encina.UnitTests/Encina.UnitTests.csproj`** (modify) — project reference
7. **`tests/Encina.UnitTests/DistributedLock/AzureBlob/AzureBlobLockOptionsTests.cs`** and **`AzureBlobLockNameTests.cs`** (new) — defaults, `ToString` hides the connection string, name shape (`locks/` plus 64 hex characters), determinism, prefix changes the name

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of issue #209: the skeleton of Encina.DistributedLock.AzureBlob.

CONTEXT:
- New package src/Encina.DistributedLock.AzureBlob next to src/Encina.DistributedLock.SqlServer; same layout.
- The blob name is "locks/" plus the lower-case hex SHA-256 of "{KeyPrefix}:lock:{resource}" (no prefix:
  "lock:{resource}"), design choice 3.
- The options hold a connection string: [JsonIgnore] and a ToString() that never prints it.

TASK:
Create the csproj, AzureBlobLockOptions, the internal AzureBlobLockName, the two PublicAPI files, the slnx line,
the unit test project reference and the unit tests of options and name. Ask mechanical-fixer for the manifest and
the Unshipped lines.

KEY RULES:
- No behavior beyond options and name. Zero warnings (CA and RS rules).
- XML documentation on every public member.

REFERENCE FILES:
- src/Encina.DistributedLock.SqlServer/Encina.DistributedLock.SqlServer.csproj
- src/Encina.DistributedLock.SqlServer/SqlServerLockOptions.cs
- src/Encina.DistributedLock.SqlServer/SqlServerDistributedLockProvider.cs (GetLockKey)
```

</details>

### Phase 2: Gateway seam, provider and renewal

> **Goal**: The provider on the ADR-052 contract, fully testable without Azure.

<details>
<summary><strong>Tasks</strong></summary>

#### `src/Encina.DistributedLock.AzureBlob/`

1. **`IBlobLeaseGateway.cs`** (new, internal) — `Task<Either<EncinaError, LeaseOutcome>> AcquireAsync(string blobName, TimeSpan leaseDuration, CancellationToken ct)` (`LeaseOutcome`: `Acquired(leaseId)` or `Contended`), `RenewAsync(blobName, leaseId, ct)` returning `Either<EncinaError, bool>` (false when the lease is gone), `ReleaseAsync(blobName, leaseId, ct)`, `IsLeasedAsync(blobName, ct)`, `EnsureContainerAsync(ct)`; each returns `Either<EncinaError, ...>`
2. **`AzureBlobLeaseGateway.cs`** (new, internal sealed) — wraps `BlobContainerClient` and `BlobLeaseClient`; the blob creation with `If-None-Match: *`; the classification of design choice 7 (409 `LeaseAlreadyPresent`, 404, others)
3. **`AzureBlobDistributedLockProvider.cs`** (new, public sealed partial) — `IDistributedLockProvider`; constructor `(IOptions<AzureBlobLockOptions> options, ILogger<AzureBlobDistributedLockProvider> logger, TimeProvider? timeProvider = null)` plus an internal constructor taking the gateway; `TryAcquireAsync` (retry loop until `wait`), `AcquireAsync` (same loop, no deadline), `IsLockedAsync`, `ExtendAsync(resource, extension, ct)` through the registry of live handles; a private `LockHandle : ILockHandle` that renews the lease on a `TimeProvider` timer (callback catches everything), releases at its deadline, marks itself lost when a renewal reports the lease gone, and moves its deadline on `ExtendAsync`
4. **`AzureBlobLockDiagnostics.cs`** (new, internal static) — `ActivitySource` and `Meter` named `Encina.DistributedLock.AzureBlob`; counter `encina.distributed_lock.acquisitions` with tags `provider` (`azure_blob`) and `outcome` (`acquired`, `contended`, `failed`); counter `encina.distributed_lock.renewals` with tag `outcome` (`renewed`, `lost`, `failed`); no tenant tag (the decorator of #741 adds `encina.tenant_id`, ADR-054 D4)
5. **`[LoggerMessage]` methods** inside the provider (`partial`), EventIds 7570-7578 (Phase 5)
6. **`PublicAPI.Unshipped.txt`** and the manifest (targets in Phase 6)
7. **Tests**: `FakeBlobLeaseGateway` (scripted results and a call log), provider unit tests (acquired, contended until the deadline with `FakeTimeProvider`, 404 then create, fault on acquire, cancellation, registry extend held and unheld, expiry release, double dispose), renewal tests (renews every interval, lease lost marks the handle, transient failure retried, deadline stops renewing), guard tests, `Encina.GuardTests.csproj` project reference

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of issue #209: the Azure Blob lock provider on the ADR-052 contract.

CONTEXT:
- #2667 has merged: IDistributedLockProvider returns Either<EncinaError, Option<ILockHandle>> (TryAcquireAsync),
  Either<EncinaError, ILockHandle> (AcquireAsync), Either<EncinaError, bool> (IsLockedAsync, ExtendAsync);
  LockErrorCodes exists. Read their real signatures and codes before editing.
- One lease per handle; the handle renews it on a TimeProvider timer until its own deadline (design choice 2);
  IsLockedAsync reads LeaseState (design choice 6).

TASK:
Write IBlobLeaseGateway and its Azure adapter, the provider with its private handle and renewal, the diagnostics
class and the log methods (EventIds 7570-7578). Write the fake gateway and the unit and guard tests of the Tasks.

KEY RULES:
- Contention (409 LeaseAlreadyPresent) is Right(None), never a failure in logs or metrics. A fault is Left; the
  error and every log carry no exception message (ForLogging, exception type and storage error code only).
- OperationCanceledException propagates; never wrap it.
- The renewal timer callback never throws. Time only from TimeProvider. Methods small enough for CRAP <= 10.

REFERENCE FILES:
- docs/architecture/adr/052-distributed-lock-failure-contract.md
- src/Encina.DistributedLock.SqlServer/SqlServerDistributedLockProvider.cs
- src/Encina.DistributedLock/IDistributedLockProvider.cs and ILockHandle.cs
- tests/Encina.TestInfrastructure/Fixtures/AzuriteFixture.cs
```

</details>

### Phase 3: Configuration, DI and health check

> **Goal**: A registration that is complete on its own, and a health check.

<details>
<summary><strong>Tasks</strong></summary>

#### `src/Encina.DistributedLock.AzureBlob/`

1. **`AzureBlobLockOptionsValidator.cs`** (new, internal sealed) — `IValidateOptions<AzureBlobLockOptions>`: lease 15..60 s, `RenewInterval` below a third of the lease, a valid container name (3 to 63 lower-case letters, digits and hyphens); registered with `ValidateOnStart`
2. **`ServiceCollectionExtensions.cs`** (new, public static) — `AddEncinaDistributedLockAzureBlob(this IServiceCollection, string connectionString)`, the overload with `Action<AzureBlobLockOptions>`, and `AddEncinaDistributedLockAzureBlob(this IServiceCollection, BlobServiceClient client, Action<AzureBlobLockOptions>? configure = null)`; `TryAddSingleton(TimeProvider.System)`, `Configure<AzureBlobLockOptions>`, the validator, `TryAddSingleton<IBlobLeaseGateway>`, `TryAddSingleton<IDistributedLockProvider, AzureBlobDistributedLockProvider>`; registers the health check when `ProviderHealthCheck.Enabled`
3. **`Health/AzureBlobDistributedLockHealthCheck.cs`** (new, public sealed) — `IEncinaHealthCheck`; `public const string DefaultName = "azure-blob-distributed-lock"`; tags `["azure", "blob", "distributed-lock", "ready"]`; reads the container properties (and, when the container is missing, reports `Unhealthy`); returns the exception type only, never the connection string or the response body
4. **`PublicAPI.Unshipped.txt`**, the manifest
5. **Tests**: DI test with `ValidateOnBuild` and `ValidateScopes` for both overloads (registration completeness), a registration-order test (an application's own provider registration is not overridden), validator tests, health check unit tests with the fake gateway, guard tests of the public types

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are implementing Phase 3 of issue #209: registration and health check of the Azure Blob lock provider.

CONTEXT:
- Phase 2 produced AzureBlobDistributedLockProvider and IBlobLeaseGateway.
- The registration must resolve everything the provider needs: options (validated), TimeProvider, the gateway, the
  BlobServiceClient (from the connection string or supplied), logger.

TASK:
Write the options validator, ServiceCollectionExtensions (three overloads) and AzureBlobDistributedLockHealthCheck;
add the DI test with ValidateOnBuild and ValidateScopes, the validator, health check and guard tests.

KEY RULES:
- TryAdd* registrations so an application registration wins in any order.
- Invalid lease settings or a missing connection string fail at startup, not at the first lock (fail closed).
- The health check output never contains the connection string or an exception message.

REFERENCE FILES:
- src/Encina.DistributedLock.SqlServer/ServiceCollectionExtensions.cs
- src/Encina.DistributedLock.SqlServer/Health/SqlServerDistributedLockHealthCheck.cs
- tests/Encina.UnitTests/DistributedLock/SqlServer/ServiceCollectionExtensionsTests.cs
```

</details>

### Phase 4: Cross-cutting integration

> **Goal**: The decisions of the 12-function matrix that need code or a statement in tests.

<details>
<summary><strong>Tasks</strong></summary>

1. **Tenancy**: no code in this package. The key is tenant-qualified by the decorator of [#741](https://github.com/dlrivada/Encina/issues/741) (reserved `default` tenant segment, injective encoding); the blob name hashes the whole composed key. A unit test asserts the provider never reads a tenant.
2. **Fail closed**: a storage fault never reports a held lock: `Left`; a lease that cannot be renewed makes the handle lost, and `ExtendAsync` is `Right(false)`.
3. **Resilience**: the Azure SDK retry policy of the `BlobServiceClient` handles transient errors; the README shows how to tune `BlobClientOptions.Retry`; no Polly policy.
4. **README note** (through `docs-writer`): lease duration versus expiry, the crash recovery bound (one lease duration), the permissions needed (container create unless `CreateContainerIfNotExists = false`, blob read, write and lease).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
You are implementing Phase 4 of issue #209: the cross-cutting statements of the Azure Blob lock provider.

CONTEXT:
- Tenant scoping lives above the provider (issue #741); the provider hashes the composed name it is given.
- A fault is Left, never "held" and never "free".

TASK:
Add the unit test that proves the provider is tenant-agnostic, the tests that prove a fault is Left on every public
method, and the README paragraph through docs-writer.

KEY RULES:
- No new public API in this phase.
- Documentation goes through docs-writer; changelog and PublicAPI through mechanical-fixer.

REFERENCE FILES:
- issue #741 (the tenant decorator; read the issue, its plan is not on main yet)
- src/Encina.DistributedLock.AzureBlob/AzureBlobDistributedLockProvider.cs
```

</details>

### Phase 5: Observability — logs, diagnostics and EventIds

> **Goal**: Operational visibility with EventIds registered first (ADR-021).

<details>
<summary><strong>Tasks</strong></summary>

1. **`src/Encina/Diagnostics/EventIdRanges.cs`** (modify, PR 1) — `public static readonly (int Min, int Max) DistributedLockAzureBlob = (7570, 7579);` with the XML summary `Encina.DistributedLock.AzureBlob — Azure Blob lease lock provider`; the new field line in `src/Encina/PublicAPI.Unshipped.txt`
2. **`tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs`** (modify, PR 1) — `["Encina.DistributedLock.AzureBlob"] = [nameof(EventIdRanges.DistributedLockAzureBlob)]`
3. **Log messages** (PR 2) — 7570 lock acquired (Debug), 7571 lock released (Debug), 7572 wait elapsed, lock contended (Debug), 7573 acquire fault (Warning, exception type and storage error code), 7574 release failed, the lease expires on its own (Warning), 7575 lease lost during renewal (Warning), 7576 renewal failed transiently (Warning), 7577 deadline reached, lock released by the handle (Information), 7578 lock container created (Information). 7579 stays spare. Contention is never an error level.
4. **Metrics and spans** (PR 2) — `AzureBlobLockDiagnostics`: spans `DistributedLock.Acquire` and `DistributedLock.Release`, counters of Phase 2; names align with the shared instruments of [#724](https://github.com/dlrivada/Encina/issues/724) and move onto them if it lands first.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
You are implementing Phase 5 of issue #209: EventIds, logs and instruments of the Azure Blob lock provider.

CONTEXT:
- ADR-021: register the range in src/Encina/Diagnostics/EventIdRanges.cs before using an id; map the assembly in
  EncinaEventIdAllocationTests. The family block 7550-7599 of the plan batch gives this package 7570-7579.
- Contention is not a failure: Debug level and outcome "contended".

TASK:
Register DistributedLockAzureBlob = (7570, 7579), map the assembly, add the [LoggerMessage] methods with the ids in
the Tasks, and the diagnostics class. Run the architecture tests.

KEY RULES:
- Packed ids, no gaps. Log the exception through ForLogging(), never its message or a response body.
- No tenant tag in the provider (ADR-054 D4 is applied by the #741 decorator).

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
- src/Encina.DistributedLock.SqlServer/SqlServerDistributedLockProvider.cs (log methods)
```

</details>

### Phase 6: Testing

> **Goal**: Every flag reaches its own target; the same contract scenarios as the other providers.

<details>
<summary><strong>Tasks</strong></summary>

Per-file targets (the manifest lists them with these sentences; the numbers are a demanding, realistic start and the worker raises them to what it measures):

| File | unit | guard | contract | integration | property | Justification |
|---|---:|---:|---:|---:|---:|---|
| `AzureBlobDistributedLockProvider.cs` | 90 | 14 | 45 | 70 | 0 | The retry loop, registry, renewal timer, handle and error mapping run against the fake gateway; the contract suite adds the shared scenarios; integration adds Azurite; property 0: no randomized logic beyond the name |
| `AzureBlobLeaseGateway.cs` | 5 | 5 | 0 | 90 | 0 | Needs Azurite; unit and guard reach only the guards; contract 0 because the contract class runs on the shared Azurite container (integration flag) |
| `AzureBlobLockName.cs` | 100 | 0 | 0 | 0 | 95 | Pure function; guard 0: internal type without `InternalsVisibleTo` for `Encina.GuardTests`; FsCheck covers determinism, shape and no exception for any string |
| `AzureBlobLockOptions.cs` | 100 | 0 | 0 | 0 | 0 | Property bag; guard 0: no parameters |
| `AzureBlobLockOptionsValidator.cs` | 100 | 0 | 0 | 0 | 0 | Pure rules; guard 0: internal type without `InternalsVisibleTo` for `Encina.GuardTests` |
| `ServiceCollectionExtensions.cs` | 95 | 90 | 0 | 0 | 0 | DI with a validated provider; guard covers each overload |
| `Health/AzureBlobDistributedLockHealthCheck.cs` | 85 | 85 | 0 | 75 | 0 | Fake gateway for unit; the integration test runs it against Azurite |
| `AzureBlobLockDiagnostics.cs` | 80 | 0 | 0 | 0 | 0 | Instruments exercised through the provider tests; guard 0: no parameters |

1. **Unit** — the scenarios of Phases 1 to 3 with `FakeTimeProvider` and the fake gateway.
2. **Guard** — constructor (null options, null logger, null gateway on the internal constructor), each method (null or whitespace resource, non-positive expiry as #2724 decides), and the three registration overloads (null services, null or empty connection string, null client).
3. **Contract** — one derived class of the suite #2667 creates, `AzureBlobDistributedLockContractTests`, on the `Cloud-Azurite` collection, running the same scenarios as the other providers (free, contended, expiry, extend held and unheld, double dispose, cancellation); `Encina.ContractTests.csproj` reference.
4. **Property** — FsCheck on `AzureBlobLockName`: always `locks/` plus 64 lower-case hex characters, deterministic, no exception for any string.
5. **Integration** — `[Collection("Cloud-Azurite")]` on `AzuriteFixture` (the collection already exists in `Encina.IntegrationTests`), `[Trait("Category", "Integration")]`: two providers on one container block each other; release frees the lease; the renewal keeps a 30 s lease alive past its duration; a lease broken by another client marks the handle lost; the lease of an abandoned handle expires on its own after the lease duration (the one slow test, at least 15 s of real time, justified because Azurite owns the lease clock); `IsLockedAsync` does not disturb a contender; the container is created when missing; health check against Azurite.
6. **Load and benchmark** — `.md` justification files under `tests/Encina.NBomber/Scenarios/Locking/` and `tests/Encina.BenchmarkTests/Encina.DistributedLock.Benchmarks/`: the provider is a thin client over storage operations whose latency is the network, covered by the shared lock scenarios once its factory is added by the issue that owns those scenarios; a missing `.md` would mean "not evaluated" (`AGENTS.md` section 9).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
You are implementing Phase 6 of issue #209: the tests of the Azure Blob lock provider.

CONTEXT:
- Unit and guard tests were written with each phase; this phase adds contract, property, integration and the
  justification files, and raises the manifest targets to the measured values.
- Integration tests use the shared collection Cloud-Azurite (AzuriteFixture); never a per-class container.

TASK:
Write the contract class, the FsCheck name property, the integration tests of the Tasks, the two justification
files, and measure every flag from the repository root with `dotnet test tests\Encina.<Flag>Tests --collect
"XPlat Code Coverage"` and `coverage-report.cs`.

KEY RULES:
- Tests execute real package code; assertions with Shouldly through Encina.Testing.Shouldly.
- No Thread.Sleep: FakeTimeProvider for unit tests; polling with timeouts for integration.
- The PR reports measured per-flag coverage against the manifest targets.

REFERENCE FILES:
- tests/Encina.TestInfrastructure/Fixtures/AzuriteFixture.cs
- tests/Encina.IntegrationTests/Infrastructure/Azurite/AzuriteCollection.cs
- tests/Encina.IntegrationTests/Infrastructure/DistributedLock/SqlServerDistributedLockIntegrationTests.cs
```

</details>

### Phase 7: Documentation and Finalization

> **Goal**: The package is documented and the 1.0 inventory is true.

<details>
<summary><strong>Tasks</strong></summary>

1. **XML documentation** on every public member (summary, remarks, param, returns, one example per registration overload).
2. **`src/Encina.DistributedLock.AzureBlob/README.md`** (new, `docs-writer`) — what the provider does, lease versus expiry, the crash recovery bound, the permissions, managed identity with a supplied `BlobServiceClient`, a `Match` example with `await using`.
3. **`changelog.d/209-lock-azure-blob.added.md`** (`mechanical-fixer`).
4. **`AGENTS.md` section 5**: no edit (the lock table already lists Azure Blob, #209); `docs/INVENTORY.md` gets the package row when the file lists packages (`docs-writer` checks).
5. **ADR**: none; the decisions are the lock family's ADR-052 and this plan's choices.
6. **Build and test verification**: `dotnet build --configuration Release` with zero warnings; every flag at its manifest target; `check-plan.ps1` for this file.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
You are finishing issue #209: documentation and verification of Encina.DistributedLock.AzureBlob.

CONTEXT:
- Phases 1 to 6 are merged; the package, tests and manifest exist.

TASK:
Complete the XML documentation, ask docs-writer for the package README, ask mechanical-fixer for the changelog
fragment and the PublicAPI lines, and run the full verification.

KEY RULES:
- Documentation never types coverage figures by hand (covref markers, SPEC-001).
- Public text never names a private client application.

REFERENCE FILES:
- changelog.d/README.md
- src/Encina.DistributedLock.SqlServer/ (README pattern of sibling lock packages, when present)
```

</details>

---

## Research

### Relevant Standards and Specifications

| Source | Provision | Relevance |
|--------|-----------|-----------|
| Azure Storage documentation, "Lease Blob" and blob leases | Lease duration 15 to 60 seconds or infinite; acquire, renew, release, break; the lease id is proposed by the client or generated; a leased blob refuses writes and deletes without it; `409 LeaseAlreadyPresent` on a second acquire | The mechanism and the contention signal |
| Azurite documentation | Emulates the blob lease operations; started with `--skipApiVersionCheck` in the repository fixture | The test target required by SPEC-000 AC-027 |
| ADR-052 | Lock providers return `Either`; contention is `Right(None)`; a fault is `Left` | Outcome mapping of every method |
| ADR-062 | Error categories; R4 provider failures are `Internal`; R5 cancellation | Categories of the `Left` cases |
| SPEC-000 REQ-027, DEC-003, DEC-009, AC-027 | Seven lock providers for 1.0, contract tests over them, Azure Blob tested with Azurite | This provider joins the contract suite |
| `AGENTS.md` sections 3, 5, 6, 7, 9 | Secrets, time, errors, EventIds, coverage, lock table | Constraints used throughout |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|----------------------|
| `IDistributedLockProvider`, `ILockHandle`, `DistributedLockOptions` | `src/Encina.DistributedLock/` | The contract (after #2667) and the base options |
| `SqlServerDistributedLockProvider` and its registration | `src/Encina.DistributedLock.SqlServer/` | Structural model: handle, `GetLockKey`, log methods, health registration |
| `SqlServerDistributedLockHealthCheck` | `src/Encina.DistributedLock.SqlServer/Health/` | Model of the health check |
| `IEncinaHealthCheck`, `ProviderHealthCheckOptions` | `src/Encina.Messaging/Health/` | Health check contract and options |
| `EventIdRanges`, `EncinaEventIdAllocationTests` | `src/Encina/Diagnostics/EventIdRanges.cs`, `tests/Encina.UnitTests/Testing/Architecture/` | Registration of 7570-7579 and the assembly map |
| `AzuriteFixture` and the `Cloud-Azurite` collection | `tests/Encina.TestInfrastructure/Fixtures/AzuriteFixture.cs`, `tests/Encina.IntegrationTests/Infrastructure/Azurite/AzuriteCollection.cs` | Integration tests on the shared emulator |
| `Azure.Storage.Blobs` 12.30.1, `Azure.Identity` 1.21.0 | `Directory.Packages.props` | The SDK and the identity library for callers |
| `ForLogging()` | `src/Encina/Diagnostics/` | Exception logging without the message |

### Provider Matrix (distributed locks, SPEC-000 DEC-009)

| Provider | This plan | Notes |
|----------|:-:|-------|
| InMemory | N/A | Single-process testing provider; changed by #2667 |
| Redis | N/A | Quorum mode is [#226](https://github.com/dlrivada/Encina/issues/226) |
| SqlServer | N/A | Structural model; defects are #2405, #2720, #2722 |
| PostgreSQL (#207) | N/A | Own plan; same registry, expiry and hashing rules |
| MySQL (#208) | N/A | Own plan; same rules |
| Azure Blob (#209) | Included | The subject of this plan |
| DynamoDB (#210) | N/A | Own plan; the other cloud provider, tested with LocalStack |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.DistributedLock.AzureBlob` (new range `DistributedLockAzureBlob`) | 7570-7579 | From the block 7550-7599 assigned to the family `lock-providers-1`; 7570-7578 used, 7579 spare. Siblings: 7550-7559 PostgreSQL (#207), 7560-7569 MySQL (#208), 7580-7589 DynamoDB (#210) |
| `Encina.DistributedLock.SqlServer` (existing, for reference) | 3800-3849 | Not touched |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Production (new) | 10 | csproj, options, validator, name, gateway seam, Azure adapter, provider, diagnostics, health check, registration (the `LeaseOutcome` type lives inside the gateway file) |
| Metadata (new and modified) | 7 | two `PublicAPI` files, `Encina.slnx`, `EventIdRanges.cs`, `Encina` `PublicAPI.Unshipped.txt`, manifest (`Directory.Packages.props` is not touched: the Azure packages are already there) |
| Unit tests | 7 | options, name, provider, handle and renewal, validator, DI, health check (plus the fake gateway) |
| Guard tests | 2 | provider, registration and health check |
| Contract and property tests | 2 | contract class, name property |
| Integration tests | 1 | one class on the shared Azurite collection |
| Test project references | 5 | UnitTests, GuardTests, ContractTests, IntegrationTests, PropertyTests |
| Documentation and changelog | 4 | README, fragment, two justification files |
| **Total** | **~39 touches** | Over 4 pull requests of at most 15 files each (13, 11, 8 and 11) |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
You are implementing issue #209: Encina.DistributedLock.AzureBlob, a lock provider on Azure Blob Storage leases.

PROJECT CONTEXT:
- .NET 10 / C# 14, nullable enabled, pre-1.0: best solution first, no compatibility layers, no [Obsolete].
- Lock contract: ADR-052, implemented by #2667 before this work: TryAcquireAsync returns
  Either<EncinaError, Option<ILockHandle>>, AcquireAsync returns Either<EncinaError, ILockHandle>, IsLockedAsync and
  ExtendAsync return Either<EncinaError, bool>, cancellation propagates, LockErrorCodes holds the Left codes.
- Time only from TimeProvider. EncinaError.Message never reaches logs, tags or health checks.

IMPLEMENTATION OVERVIEW:
1. Package skeleton: csproj (Azure.Storage.Blobs), AzureBlobLockOptions ([JsonIgnore] connection string,
   ContainerName, CreateContainerIfNotExists, LeaseDuration 30 s, RenewInterval 10 s), AzureBlobLockName
   ("locks/" plus the SHA-256 hex of "{prefix}:lock:{resource}").
2. IBlobLeaseGateway seam and AzureBlobLeaseGateway adapter; AzureBlobDistributedLockProvider: retry loop until
   wait, one lease per handle, the handle renews the lease on a TimeProvider timer that never throws, releases at
   its deadline, marks itself lost when the lease is gone; provider ExtendAsync through a registry of live handles;
   IsLockedAsync through BlobProperties.LeaseState; 409 LeaseAlreadyPresent is Right(None).
3. AddEncinaDistributedLockAzureBlob (connection string, options, BlobServiceClient overloads, TryAdd*), options
   validator (ValidateOnStart), health check, DI test with ValidateOnBuild and ValidateScopes.
4. EventIds 7570-7578 in range DistributedLockAzureBlob (7570-7579), diagnostics, ActivitySource and Meter.
5. Tests: unit (fake gateway, FakeTimeProvider), guard, contract (suite of #2667, Cloud-Azurite collection),
   property (name), integration on Azurite; documentation through docs-writer; changelog and PublicAPI through
   mechanical-fixer.

KEY PATTERNS:
- Contention is Right(None); a storage fault is Left; a lost lease is Right(false) on ExtendAsync.
- Satellite DI with TryAdd*; startup validation of the options.
- Four pull requests of at most 15 files; each builds and passes alone.

REFERENCE FILES:
- docs/architecture/adr/052-distributed-lock-failure-contract.md
- src/Encina.DistributedLock.SqlServer/SqlServerDistributedLockProvider.cs
- src/Encina.DistributedLock.SqlServer/ServiceCollectionExtensions.cs
- tests/Encina.TestInfrastructure/Fixtures/AzuriteFixture.cs
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Lock state is ephemeral coordination state; caching it would be a defect |
| 2 | OpenTelemetry | ✅ | Phase 5: spans for acquire and release, acquisitions and renewals counters with `provider` and `outcome` tags; no tenant tag here, the #741 decorator adds `encina.tenant_id` (ADR-054 D4); names align with [#724](https://github.com/dlrivada/Encina/issues/724) |
| 3 | Structured Logging | ✅ | Phase 5: `[LoggerMessage]` EventIds 7570-7578 in the registered range, exception type and storage error code only |
| 4 | Health Checks | ✅ | Phase 3: `AzureBlobDistributedLockHealthCheck` reads the container properties |
| 5 | Validation | ✅ | Phase 3: options validator at startup (lease 15..60 s, renew interval, container name); resource and expiry arguments validated as [#2724](https://github.com/dlrivada/Encina/issues/2724) decides |
| 6 | Resilience | ✅ | Phase 4: the Azure SDK retry policy handles transient storage errors; the renewal retries on the next tick while the lease is valid; no Polly policy on top |
| 7 | Distributed Locks | ❌ | This is the distributed lock feature itself |
| 8 | Transactions | ❌ | A lock is not a transactional resource; no unit of work is involved |
| 9 | Idempotency | ❌ | A repeated acquire is a new attempt by design |
| 10 | Multi-Tenancy | ⏭️ | Tenant-scoped keys are composed above the provider by [#741](https://github.com/dlrivada/Encina/issues/741) (existing issue); per-tenant containers are not needed because the key is qualified |
| 11 | Module Isolation | ❌ | Isolation is reached with `KeyPrefix`; the provider holds no module data |
| 12 | Audit Trail | ❌ | Acquire and release are coordination events, not data access or state changes; logs and metrics cover them |

---

## Prerequisites & Dependencies

| Item | State | Effect on this plan |
|------|-------|---------------------|
| [#2667](https://github.com/dlrivada/Encina/issues/2667) — lock providers return `Either` (ADR-052) | Open, implementation issue (no pending maintainer decision) | PR 1 can start before it; PRs 2 to 4 start after it merges |
| [#2724](https://github.com/dlrivada/Encina/issues/2724) — argument validation of lock providers | Open | The guard tests follow its decision for empty resources and non-positive expiry |
| [#741](https://github.com/dlrivada/Encina/issues/741) — tenant-scoped locks | Open (family plan) | Owns tenant key composition; provider unchanged |
| [#755](https://github.com/dlrivada/Encina/issues/755) — health checks of the other lock providers | Open | Adds the first lock reference to the contract test project; whichever lands first, the other drops its line |
| [#724](https://github.com/dlrivada/Encina/issues/724) — lock observability | Open | Phase 5 reuses shared instruments if they exist |
| [#218](https://github.com/dlrivada/Encina/issues/218), [#223](https://github.com/dlrivada/Encina/issues/223) — fencing tokens, auto-extend | Open | Out of scope; the lease id is not a fencing token; renewal inside the handle is this provider's own need, not the generic auto-extend |

---

## Next Steps

1. Link this plan from issue #209 and start the PR 1 brief; PRs 2 to 4 wait for [#2667](https://github.com/dlrivada/Encina/issues/2667).
2. When work on the issue starts, ask the maintainer to answer Dilemma 2 (renewal inside the handle or in a hosted service) and Dilemma 5 (container creation default), record the answers in a `## Maintainer Decisions` section placed last, and have the adversarial-reviewer verify the Settled and Trivial choices.
3. One commit per phase; the last commit of PR 4 references `Fixes #209`.
