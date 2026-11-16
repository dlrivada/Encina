# Implementation Plan: `Encina.DistributedLock.DynamoDB` — distributed lock provider on DynamoDB conditional writes

> **Issue**: [#210](https://github.com/dlrivada/Encina/issues/210)
> **Type**: Feature
> **Complexity**: Medium (one new provider package, 4 pull requests)
> **Estimated Scope**: ~700-850 lines of production code + ~1,200-1,500 lines of tests
> **Specification**: [SPEC-000](../specifications/SPEC-000-encina-1.0-baseline-and-release-scope.md) REQ-027, DEC-003, DEC-009, AC-027
> **Decision**: [ADR-052](../architecture/adr/052-distributed-lock-failure-contract.md) (lock failure contract), [ADR-062](../architecture/adr/062-error-category-contract.md) (error categories)
> **Depends on**: [#2667](https://github.com/dlrivada/Encina/issues/2667) (implements ADR-052; open, not a pending maintainer decision; it blocks the other lock providers)
> **Related**: [#207](https://github.com/dlrivada/Encina/issues/207), [#208](https://github.com/dlrivada/Encina/issues/208), [#209](https://github.com/dlrivada/Encina/issues/209) (same family), [#589](https://github.com/dlrivada/Encina/issues/589) (the DynamoDB dead letter store, which also adds the `AWSSDK.DynamoDBv2` package), [#226](https://github.com/dlrivada/Encina/issues/226) (Redis RedLock), [#741](https://github.com/dlrivada/Encina/issues/741) (tenant-scoped locks), [#218](https://github.com/dlrivada/Encina/issues/218) (fencing tokens), [#223](https://github.com/dlrivada/Encina/issues/223) (auto-extend), [#724](https://github.com/dlrivada/Encina/issues/724) (lock observability), [#755](https://github.com/dlrivada/Encina/issues/755) (health checks), [#2405](https://github.com/dlrivada/Encina/issues/2405), [#2724](https://github.com/dlrivada/Encina/issues/2724)

---

## Summary

Add the package `Encina.DistributedLock.DynamoDB`, an `IDistributedLockProvider` over **DynamoDB conditional writes**. It is the AWS entry of the seven lock providers SPEC-000 DEC-009 fixes for 1.0; its tests run against LocalStack (SPEC-000 AC-027).

1. **One item per resource.** The lock is an item in a lock table (`LockKey` partition key). Acquire is a `PutItem` with the condition `attribute_not_exists(LockKey) OR ExpiresAtMs < :now`; a `ConditionalCheckFailedException` is contention.
2. **Correctness never depends on the DynamoDB TTL.** DynamoDB deletes expired items lazily (up to days after expiry), so the condition compares `ExpiresAtMs` (epoch milliseconds, the lease deadline) and the separate `Ttl` attribute (epoch seconds) only cleans the table.
3. **Heartbeat inside the handle.** The handle writes a short lease (`3 x HeartbeatInterval`, default 30 s) and renews it with a conditional `UpdateItem` on its own `LockId` until its own deadline, then deletes the item. A crashed process stops renewing and the lock frees itself within one lease. With `UseHeartbeat = false` the item expires at the caller's deadline and nothing renews it.
4. **Poll, do not block.** Acquire retries every `retry` until the `wait` deadline; delays use `TimeProvider`.
5. **ADR-052 outcomes.** Acquired is `Right(Some(handle))`, contention or the wait elapsing is `Right(None)`, a DynamoDB fault is `Left`, cancellation propagates.
6. **Non-intrusive probe.** `IsLockedAsync` is a consistent `GetItem`; it never writes.
7. **Operations.** A health check, `[LoggerMessage]` EventIds 7580-7588, an `ActivitySource` and a `Meter`; the AWS SDK retry policy absorbs throttling.

**Standards**: Amazon DynamoDB Developer Guide (conditional writes with condition expressions, consistent reads, Time To Live, on-demand capacity); the AWS "DynamoDB Lock Client" and its lease model (https://github.com/awslabs/amazon-dynamodb-lock-client); ADR-052; SPEC-000 REQ-027 / AC-027.

**Estimated scope**: 10 new production files (csproj, options, options validator, schema helper, gateway seam and its DynamoDB adapter, provider, diagnostics, health check, registration), 5 modified metadata files, about 14 test files, README and changelog fragment; 4 pull requests.

**Affected packages**: `Encina.DistributedLock.DynamoDB` (new); `Encina` (EventId registry and its `PublicAPI.Unshipped.txt`); `Directory.Packages.props` (`AWSSDK.DynamoDBv2`); test projects `Encina.UnitTests`, `Encina.GuardTests`, `Encina.ContractTests`, `Encina.IntegrationTests`, `Encina.PropertyTests` (project references and tests). `Encina.DistributedLock` is consumed, not changed.

**Provider category**: Distributed locks (`AGENTS.md` section 5); it is also a cloud-tied provider, so it follows the "tested with a local emulator" rule of the lock table (LocalStack). Matrix in Research.

**What the code does today** (`origin/main` at `0b941beeb`):

- The package does not exist. `Directory.Packages.props` has `Testcontainers.LocalStack`, `AWSSDK.SQS`, `AWSSDK.SimpleNotificationService`, `AWSSDK.SecretsManager` and `AWSSDK.KeyManagementService`, but **not** `AWSSDK.DynamoDBv2`. The plan of [#589](https://github.com/dlrivada/Encina/issues/589) adds it in its first pull request; whichever lands first adds the line (the same AWSSDK 4 major as the other entries) and the other drops it.
- `LocalStackFixture` (`tests/Encina.TestInfrastructure/Fixtures/LocalStackFixture.cs`, image `localstack/localstack:4.14.0`, exposes `Endpoint`) and the collection `Cloud-LocalStack` (`tests/Encina.IntegrationTests/MessageBrokers/AmazonSQS/LocalStackCollection.cs`, and one in `Encina.ContractTests`) exist. The fixture's `ClearAllDataAsync` clears SQS and SNS only, so the DynamoDB tests clean their own table.
- `IDistributedLockProvider` still has the pre-ADR-052 shapes; [#2667](https://github.com/dlrivada/Encina/issues/2667) changes them and creates `LockErrorCodes`. **This plan is written to the ADR-052 contract; implementation starts after #2667 merges.**
- `SqlServerDistributedLockProvider` is the structural model (handle, `GetLockKey`, health registration). The plans of [#207](https://github.com/dlrivada/Encina/issues/207), [#208](https://github.com/dlrivada/Encina/issues/208) and [#209](https://github.com/dlrivada/Encina/issues/209) use the same registry and expiry rules; the heartbeat of this plan is the lease renewal of #209.
- No contract test class for locks exists yet; #2667 creates the suite.

---

## Design Choices

<details>
<summary><strong>1. Package placement and name</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) New package `Encina.DistributedLock.DynamoDB`** | The name SPEC-000 REQ-027 and the issue use; the AWS SDK only for those who ask; same shape as the other provider packages | One more package, README and manifest |
| **B) Inside a future shared DynamoDB package with the dead letter store of #589** | One DynamoDB client registration | No such package exists; couples a coordination feature to a messaging store |

### Chosen Option: **A — new package `Encina.DistributedLock.DynamoDB`**

### Rationale

- SPEC-000 REQ-027 names DynamoDB (#210) in the lock set, and every provider is its own `Encina.DistributedLock.{Backend}` package. An application that uses both DynamoDB features registers one `IAmazonDynamoDB` and passes it to both.

**Class:** Settled
**Settled by:** SPEC-000 REQ-027 and DEC-009; `AGENTS.md` section 5 (lock table lists `DynamoDB (#210)`)

</details>

<details>
<summary><strong>2. Expiry model — wall-clock lease deadline with a skew allowance, or the observation model of the AWS lock client</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The item stores `ExpiresAtMs`; a contender takes the lock when `ExpiresAtMs < now - ClockSkewAllowance` on its own `TimeProvider` clock** | Stateless and one conditional write; a unit test advances `FakeTimeProvider`; the allowance (default 2 s) only delays a takeover | Correct only when the clocks of the instances differ by less than the allowance; the README states the NTP assumption |
| **B) The AWS lock client model: a record version number changes on every write; a contender takes the lock only after it has observed the same version for a full lease on its own monotonic clock** | No dependence on synchronized wall clocks | Each contender keeps per-key observation state; a takeover waits a full lease after the first look; more code and a harder test story |

### Chosen Option: **A — wall-clock deadline with a skew allowance** (recommended, pending the maintainer)

### Rationale

- Option A is the same model as the Redis (`PX`) and Azure leases from the caller's view and keeps the provider stateless. Option B wins where instances cannot be time-synchronized (edge devices, mixed hosts); the maintainer can pick it when work starts.
- `ClockSkewAllowance` is an option, validated non-negative and below the lease.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Heartbeat — inside each handle, in one hosted service, or none</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Each handle renews its own lease with a `TimeProvider` timer every `HeartbeatInterval` and deletes the item at its deadline** | No hosted service to register (the registration stays complete on its own); the heartbeat lives and dies with the handle; `FakeTimeProvider` tests it | One timer per live handle |
| **B) One `BackgroundService` renews every registered handle** (the issue's wording, "heartbeat background service") | One loop for any number of locks | A hosted service must be registered and running for the provider to be correct; a stopped service silently lets leases lapse under live holders |
| **C) No heartbeat: the item expires at the caller's deadline** | Simplest | A crash frees the lock only at the caller's `expiry`, which may be minutes |

### Chosen Option: **A — heartbeat inside each handle, with `UseHeartbeat = false` giving option C** (recommended, pending the maintainer)

### Rationale

- The interface promises auto-release on a crash and the provider must work without a hosted service (registration completeness, `AGENTS.md` section 3). Option B wins when an application holds thousands of concurrent locks per process.
- The timer callback catches every exception and logs only the exception type (`AGENTS.md` section 3). A renewal that fails its condition (`LockId` no longer ours or the lease already past) marks the handle lost: `IsReleased` becomes true, a Warning is logged, `ExtendAsync` answers `Right(false)`. A transient failure is retried on the next tick while the lease is still valid on the handle's clock. The lease is `3 x HeartbeatInterval` clamped to the caller's `expiry`.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Table creation</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `CreateTableIfNotExists` (default true, as the issue): create the table once (on-demand billing, key `LockKey`, TTL enabled on `Ttl`) and wait until it is ACTIVE** | Works out of the box; the same default as the Azure container of [#209](https://github.com/dlrivada/Encina/issues/209) | The identity needs `dynamodb:CreateTable` and `UpdateTimeToLive` unless the flag is false |
| **B) The table must exist; only items are written** | Least privilege | A missing table is a startup failure the application must provision first |

### Chosen Option: **A — create the table once, default true** (recommended, pending the maintainer)

### Rationale

- The default follows the issue. Option B wins in locked-down accounts where infrastructure code owns tables; with the flag false the provider behaves as B, so the maintainer's choice is only the default value. A missing table with the flag false is a `Left` (provisioning failure), never a silent "free". Concurrent creation by two instances treats `ResourceInUseException` as success.

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Release behavior and the version attribute</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Release deletes the item with the condition `LockId = :id`; `Version` is a counter incremented on acquire** | A clean table; no item survives its lock; the `Version` is available for [#218](https://github.com/dlrivada/Encina/issues/218) while the item exists | The counter restarts after a release, so it is not a fencing token across lifetimes |
| **B) Release marks the item released (`ExpiresAtMs = 0`) and keeps it; `Version` keeps counting** | A monotonic counter across lifetimes, usable as a fencing token | Items accumulate (one per resource ever locked) until the TTL attribute removes them, which resets the counter anyway |

### Chosen Option: **A — delete on release** (recommended, pending the maintainer)

### Rationale

- Fencing tokens are the scope of [#218](https://github.com/dlrivada/Encina/issues/218), and a counter that the TTL can reset is not a sound token; the issue's `Version` attribute is stored so that #218 can decide. Option B wins if the maintainer wants fencing tokens from DynamoDB in 1.0 and accepts the item growth.
- A release whose condition fails (the lease had lapsed and someone else holds the item) is not an error: it is logged at Debug and the delete is skipped.

**Class:** Dilemma

</details>

<details>
<summary><strong>6. Client registration</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Two registration paths: an application-supplied `IAmazonDynamoDB`, or options (`Region`, optional `ServiceUrl` for LocalStack) that build a client with the default credential chain** | A supplied client carries any credential, retry and endpoint setup; the options path is enough for the common case and for the emulator | Two paths to test |
| **B) Options only** | One path | No way to share the client with other AWS features or to set credentials explicitly |

### Chosen Option: **A — supplied client or options**

### Rationale

- The options hold no secret (no access key is ever a property; credentials come from the SDK chain or from the supplied client), so no connection string rule applies; `ToString()` still prints only the table, the region and the heartbeat settings.

**Class:** Trivial
**Settled by:** `AGENTS.md` section 3 (options with a password, connection string, token or key are protected; these options hold none); `src/Encina.Messaging.Encryption.AwsKms/` (named sibling of the repository that takes an AWS client from the application)

</details>

<details>
<summary><strong>7. Probe, provider-level extend and the testability seam</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `IsLockedAsync` is a consistent `GetItem` (locked when the item exists and `ExpiresAtMs > now`); provider `ExtendAsync(resource)` extends only a handle this provider instance holds, else `Right(false)`; an `internal` seam `IDynamoLockGateway` with one adapter holds all SDK calls** | No write to test a lock; ownership is real; the retry loop, heartbeat and error mapping run in unit tests against a fake | One more internal file |
| **B) Probe by trying to acquire and deleting; `ExtendAsync` answers `Right(true)`; SDK types used inline** | Fewer files | The probe causes false contention; `Right(true)` for an unchecked lock is #2405; the SDK client is only testable through LocalStack |

### Chosen Option: **A — consistent `GetItem` probe, local registry, gateway seam**

### Rationale

- ADR-052 defines `ExtendAsync` as `Right(false)` when the lock is "not held"; the blocking `crap-gate` needs the provider logic covered by fast tests (`AGENTS.md` section 9).

**Class:** Settled
**Settled by:** ADR-052 ("not held" is `Right(false)`); #2405 (the defect option B reproduces); `AGENTS.md` section 9

</details>

<details>
<summary><strong>8. Error mapping and retries</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Classify once in the gateway: `ConditionalCheckFailedException` on acquire is `Right(None)` (on renew it is "lost", on release "already gone"); `ResourceNotFoundException` creates the table when allowed, else `Left`; any other `AmazonServiceException` or `AmazonClientException` (including throttling after the SDK retries, `MaxErrorRetry`) is `Left` with the backend-fault code of `LockErrorCodes`; cancellation propagates** | One mapping; contention is never a failure; resilience comes from the SDK retry policy | Needs the exact code names of #2667 (read before editing) |
| **B) Catch `Exception` in the provider and wrap Polly around it** | Smaller | Collapses cancellation and programming errors into `Left`; doubles the retries of the SDK |

### Chosen Option: **A — classify in the gateway, the SDK retries throttling**

### Rationale

- This plan adds **one** error code, for caller input: a resource whose composed key exceeds the 2048-byte partition-key limit is `lock.dynamodb.key_too_long` with `ErrorCategory.Validation` (ADR-062 rule R3, a field rule; it is not a backend fault), an internal constant of `DynamoDBLockSchema`, returned by the provider before any network call (it follows the argument validation of [#2724](https://github.com/dlrivada/Encina/issues/2724) and is replaced by the #2724 code if that issue already defines one for oversized keys). Every other `Left` carries the backend-fault code #2667 defines, whose category follows ADR-062 rule R4 (provider failures are `Internal`); cancellation is never a `Left` (R5). The error never carries the exception message; logs carry the exception type through `ForLogging()` and the AWS error code, never the response body.

**Class:** Trivial
**Settled by:** ADR-052; ADR-062 rules R3, R4 and R5; `AGENTS.md` section 3 (`EncinaError.Message` rule)

</details>

---

## Implementation Phases

Four pull requests, each at most 15 files and 1500 changed lines counting tests, `PublicAPI.*.txt`, the coverage manifest, project references, `Directory.Packages.props`, the README and the changelog fragment (`AGENTS.md` section 11). **PRs 2 to 4 build only after [#2667](https://github.com/dlrivada/Encina/issues/2667) merges**; PR 1 does not depend on it.

| PR | Phases | Production and metadata files | Test and doc files | Files / lines (approx.) |
|---|---|---|---|---|
| 1 | Phase 1 and the EventId registration of Phase 5 | csproj, `DynamoDBLockOptions`, `DynamoDBLockSchema`, two `PublicAPI` files, `Directory.Packages.props`, `Encina.slnx`, `EventIdRanges.cs`, `Encina` `PublicAPI.Unshipped.txt`, manifest (10) | `EncinaEventIdAllocationTests`, `Encina.UnitTests.csproj` reference, `DynamoDBLockOptionsTests`, `DynamoDBLockSchemaTests` (4) | 14 files / 650 |
| 2 | Phases 2 and 5 (logs, diagnostics) | `IDynamoLockGateway`, `DynamoDBLockGateway`, `DynamoDBDistributedLockProvider`, `DynamoDBLockDiagnostics`, `PublicAPI.Unshipped.txt`, manifest (6) | `FakeDynamoLockGateway`, provider unit tests, handle and heartbeat unit tests, guard tests, `Encina.GuardTests.csproj` reference (5) | 11 files / 1,350 |
| 3 | Phase 3 | `DynamoDBLockOptionsValidator`, `ServiceCollectionExtensions`, `Health/DynamoDBDistributedLockHealthCheck`, `PublicAPI.Unshipped.txt`, manifest (5) | DI test with `ValidateOnBuild`/`ValidateScopes` (all overloads), validator and health check unit tests, guard tests (3) | 8 files / 850 |
| 4 | Phases 4, 6 and 7 | none | contract class and `Encina.ContractTests.csproj` reference, integration tests on `Cloud-LocalStack` and `Encina.IntegrationTests.csproj` reference, property test of the schema helper and `Encina.PropertyTests.csproj` reference, README, changelog fragment, two load/benchmark justification files, manifest (11) | 11 files / 900 |

If a PR measures over 15 files or 1,500 lines, the worker splits it again and never asks for `size-exception-approved`. PR 1 has the least file slack (14 of 15): if `Directory.Packages.props` was already changed by #589, the PR has 13 files. PR 2 has the least line slack: if the heartbeat tests grow, they move to PR 3.

### Phase 1: Package skeleton, options and schema helper

> **Goal**: The package builds with its configuration surface and the pure schema functions, with their tests.

<details>
<summary><strong>Tasks</strong></summary>

#### `src/Encina.DistributedLock.DynamoDB/`

1. **`Encina.DistributedLock.DynamoDB.csproj`** (new) — modeled on `Encina.DistributedLock.SqlServer.csproj`: `ProjectReference` to `Encina.DistributedLock`, `PackageReference` `AWSSDK.DynamoDBv2` (central version), PublicApiAnalyzers, `InternalsVisibleTo` as `AssemblyAttribute` items for `Encina.UnitTests`, `Encina.IntegrationTests`, `Encina.ContractTests`, `Encina.PropertyTests` (the pattern of `Encina.Messaging.csproj`; the SQL Server csproj has none); a new GUID
2. **`DynamoDBLockOptions.cs`** (new, public sealed) — `: DistributedLockOptions`; `string TableName = "encina-locks"`; `string? Region`; `string? ServiceUrl` (the emulator endpoint); `bool CreateTableIfNotExists = true`; `bool UseHeartbeat = true`; `TimeSpan HeartbeatInterval = 10 s`; `TimeSpan ClockSkewAllowance = 2 s`; `ToString()` prints table, region and heartbeat settings
3. **`DynamoDBLockSchema.cs`** (new, internal static) — attribute names (`LockKey`, `LockId`, `Owner`, `ExpiresAtMs`, `Ttl`, `Version`), `Compose(prefix, resource)` (the `lock:` format of the SQL Server provider), `ToEpochMilliseconds(DateTimeOffset)`, `ToTtlSeconds(expiresAtMs)` (seconds, rounded up, plus a one-hour grace so the table cleanup never races a valid lease), and `LeaseFor(heartbeatInterval, expiry)` (`3 x interval` clamped to the expiry)
4. **`PublicAPI.Shipped.txt`** (new, `#nullable enable`) and **`PublicAPI.Unshipped.txt`**
5. **`Directory.Packages.props`** (modify) — `AWSSDK.DynamoDBv2`, the same AWSSDK 4 major as the other entries (resolved with `dotnet package search` at implementation; skipped if #589 landed first)
6. **`Encina.slnx`** (modify) and **`.github/coverage-manifest/Encina.DistributedLock.DynamoDB.json`** (new, written by `mechanical-fixer` from the target table of Phase 6)
7. **`tests/Encina.UnitTests/Encina.UnitTests.csproj`** (modify) — project reference
8. **`tests/Encina.UnitTests/DistributedLock/DynamoDB/DynamoDBLockOptionsTests.cs`** and **`DynamoDBLockSchemaTests.cs`** (new) — defaults, `ToString`, key format, epoch conversion, TTL grace, lease clamp

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of issue #210: the skeleton of Encina.DistributedLock.DynamoDB.

CONTEXT:
- New package src/Encina.DistributedLock.DynamoDB next to src/Encina.DistributedLock.SqlServer; same layout.
- The item key is the logical key "{KeyPrefix}:lock:{resource}" (no prefix: "lock:{resource}"). ExpiresAtMs is the
  lease deadline in epoch milliseconds and is what correctness compares; Ttl (epoch seconds plus a one-hour grace)
  only lets DynamoDB clean the table (design choice 2 and the summary).
- AWSSDK.DynamoDBv2 is not in Directory.Packages.props yet (issue #589 also adds it): add it only if it is missing.

TASK:
Create the csproj, DynamoDBLockOptions, the internal DynamoDBLockSchema, the two PublicAPI files, the package
version line, the slnx line, the unit test project reference and the unit tests of options and schema. Ask
mechanical-fixer for the manifest and the Unshipped lines.

KEY RULES:
- No behavior beyond options and pure schema functions. Zero warnings (CA and RS rules).
- XML documentation on every public member. Time is not read in this phase.

REFERENCE FILES:
- src/Encina.DistributedLock.SqlServer/Encina.DistributedLock.SqlServer.csproj
- src/Encina.DistributedLock.SqlServer/SqlServerLockOptions.cs
- src/Encina.DistributedLock.SqlServer/SqlServerDistributedLockProvider.cs (GetLockKey)
- Directory.Packages.props
```

</details>

### Phase 2: Gateway seam, provider and heartbeat

> **Goal**: The provider on the ADR-052 contract, fully testable without AWS.

<details>
<summary><strong>Tasks</strong></summary>

#### `src/Encina.DistributedLock.DynamoDB/`

1. **`IDynamoLockGateway.cs`** (new, internal) — `Task<Either<EncinaError, AcquireOutcome>> TryPutAsync(string key, string lockId, long nowMs, long expiresAtMs, CancellationToken ct)` (`AcquireOutcome`: `Acquired(version)` or `Contended`), `RenewAsync(key, lockId, nowMs, newExpiresAtMs, ct)` returning `Either<EncinaError, bool>` (false when the item is no longer ours), `DeleteAsync(key, lockId, ct)`, `GetAsync(key, ct)` returning the stored `ExpiresAtMs` or none, `EnsureTableAsync(ct)`; each returns `Either<EncinaError, ...>`
2. **`DynamoDBLockGateway.cs`** (new, internal sealed) — wraps `IAmazonDynamoDB`; the condition expressions of design choices 2 and 3 (`attribute_not_exists(LockKey) OR ExpiresAtMs < :threshold` on put; `LockId = :id AND ExpiresAtMs > :now` on renew; `LockId = :id` on delete); `GetItem` with `ConsistentRead = true`; table creation (on-demand billing, `UpdateTimeToLive` on `Ttl`, wait for ACTIVE, `ResourceInUseException` as success); the classification of design choice 8
3. **`DynamoDBDistributedLockProvider.cs`** (new, public sealed partial) — `IDistributedLockProvider`; constructor `(IOptions<DynamoDBLockOptions> options, ILogger<DynamoDBDistributedLockProvider> logger, TimeProvider? timeProvider = null)` plus an internal constructor taking the gateway; `TryAcquireAsync` (retry loop until `wait`), `AcquireAsync` (same loop, no deadline), `IsLockedAsync`, `ExtendAsync(resource, extension, ct)` through the registry of live handles; a private `LockHandle : ILockHandle` that renews the lease on a `TimeProvider` timer (callback catches everything), deletes the item at its deadline, marks itself lost when a renewal reports the item gone, and moves its deadline on `ExtendAsync`; `LockId` is a new GUID per acquisition and `Owner` a GUID per provider instance (never a host name)
4. **`DynamoDBLockDiagnostics.cs`** (new, internal static) — `ActivitySource` and `Meter` named `Encina.DistributedLock.DynamoDB`; counter `encina.distributed_lock.acquisitions` with tags `provider` (`dynamodb`) and `outcome` (`acquired`, `contended`, `failed`); counter `encina.distributed_lock.renewals` with tag `outcome` (`renewed`, `lost`, `failed`); no tenant tag (the decorator of #741 adds `encina.tenant_id`, [ADR-054](../architecture/adr/054-observability-conventions-and-trace-propagation.md) D4)
5. **`[LoggerMessage]` methods** inside the provider (`partial`), EventIds 7580-7588 (Phase 5)
6. **`PublicAPI.Unshipped.txt`** and the manifest (targets in Phase 6)
7. **Tests**: `FakeDynamoLockGateway` (scripted results and a call log), provider unit tests (acquired, contended until the deadline with `FakeTimeProvider`, skew allowance respected, table missing then created, fault on put, cancellation, registry extend held and unheld, deadline release, double dispose), heartbeat tests (renews every interval, lost item marks the handle, transient failure retried, `UseHeartbeat = false` writes the full expiry once), guard tests, `Encina.GuardTests.csproj` project reference

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of issue #210: the DynamoDB lock provider on the ADR-052 contract.

CONTEXT:
- #2667 has merged: IDistributedLockProvider returns Either<EncinaError, Option<ILockHandle>> (TryAcquireAsync),
  Either<EncinaError, ILockHandle> (AcquireAsync), Either<EncinaError, bool> (IsLockedAsync, ExtendAsync);
  LockErrorCodes exists. Read their real signatures and codes before editing.
- One item per resource; acquire is a conditional PutItem; the handle renews its lease on a TimeProvider timer and
  deletes the item at its deadline (design choices 2, 3, 5); IsLockedAsync is a consistent GetItem (choice 7).

TASK:
Write IDynamoLockGateway and its DynamoDB adapter, the provider with its private handle and heartbeat, the
diagnostics class and the log methods (EventIds 7580-7588). Write the fake gateway and the unit and guard tests of
the Tasks.

KEY RULES:
- Contention (ConditionalCheckFailedException on put) is Right(None), never a failure in logs or metrics. A fault is
  Left; the error and every log carry no exception message (ForLogging, exception type and AWS error code only).
- OperationCanceledException propagates; never wrap it.
- The heartbeat timer callback never throws. Time only from TimeProvider. Methods small enough for CRAP <= 10.

REFERENCE FILES:
- docs/architecture/adr/052-distributed-lock-failure-contract.md
- src/Encina.DistributedLock.SqlServer/SqlServerDistributedLockProvider.cs
- src/Encina.DistributedLock/IDistributedLockProvider.cs and ILockHandle.cs
- tests/Encina.TestInfrastructure/Fixtures/LocalStackFixture.cs
```

</details>

### Phase 3: Configuration, DI and health check

> **Goal**: A registration that is complete on its own, and a health check.

<details>
<summary><strong>Tasks</strong></summary>

#### `src/Encina.DistributedLock.DynamoDB/`

1. **`DynamoDBLockOptionsValidator.cs`** (new, internal sealed) — `IValidateOptions<DynamoDBLockOptions>`: a valid table name (3 to 255 characters of letters, digits, `_`, `-`, `.`), positive `HeartbeatInterval`, non-negative `ClockSkewAllowance` below the lease; registered with `ValidateOnStart`
2. **`ServiceCollectionExtensions.cs`** (new, public static) — `AddEncinaDistributedLockDynamoDB(this IServiceCollection, Action<DynamoDBLockOptions>? configure = null)` (builds the client from `Region` and `ServiceUrl` with the default credential chain) and `AddEncinaDistributedLockDynamoDB(this IServiceCollection, IAmazonDynamoDB client, Action<DynamoDBLockOptions>? configure = null)`; `TryAddSingleton(TimeProvider.System)`, `Configure<DynamoDBLockOptions>`, the validator, `TryAddSingleton<IDynamoLockGateway>`, `TryAddSingleton<IDistributedLockProvider, DynamoDBDistributedLockProvider>`; registers the health check when `ProviderHealthCheck.Enabled`; a client created by the registration is disposed with the container, a supplied client is not
3. **`Health/DynamoDBDistributedLockHealthCheck.cs`** (new, public sealed) — `IEncinaHealthCheck`; `public const string DefaultName = "dynamodb-distributed-lock"`; tags `["aws", "dynamodb", "distributed-lock", "ready"]`; `DescribeTable` and reports the status (`ACTIVE` healthy; missing or creating unhealthy); returns the exception type only, never credentials or the response body
4. **`PublicAPI.Unshipped.txt`**, the manifest
5. **Tests**: DI test with `ValidateOnBuild` and `ValidateScopes` for both overloads (registration completeness), a registration-order test (an application's own provider registration is not overridden), validator tests, health check unit tests with the fake gateway, guard tests of the public types

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are implementing Phase 3 of issue #210: registration and health check of the DynamoDB lock provider.

CONTEXT:
- Phase 2 produced DynamoDBDistributedLockProvider and IDynamoLockGateway.
- The registration must resolve everything the provider needs: options (validated), TimeProvider, the gateway, the
  IAmazonDynamoDB (built from options or supplied), logger.

TASK:
Write the options validator, ServiceCollectionExtensions (two overloads) and DynamoDBDistributedLockHealthCheck; add
the DI test with ValidateOnBuild and ValidateScopes, the validator, health check and guard tests.

KEY RULES:
- TryAdd* registrations so an application registration wins in any order.
- Invalid settings fail at startup, not at the first lock (fail closed).
- The health check output never contains credentials, endpoints or an exception message.

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

1. **Tenancy**: no code in this package. The key is tenant-qualified by the decorator of [#741](https://github.com/dlrivada/Encina/issues/741) (reserved `default` tenant segment, injective encoding) and stored as the partition key, so distinct tenant and resource pairs are distinct items with no hashing. Per-tenant tables (the issue's deferred idea) are not needed. A unit test asserts the provider never reads a tenant. A key longer than the 2048-byte partition-key limit (UTF-8 bytes) is caller input, not a backend fault: the provider returns `Left` with `lock.dynamodb.key_too_long`, category `Validation` (ADR-062 R3, design choice 8), before any network call, never a silent truncation.
2. **Fail closed**: a DynamoDB fault never reports a held lock: `Left`; a lease that cannot be renewed makes the handle lost, and `ExtendAsync` is `Right(false)`.
3. **Resilience**: the AWS SDK retry policy (`MaxErrorRetry`, throttling backoff) of the client handles transient errors; the README shows how to tune it; no Polly policy.
4. **README note** (through `docs-writer`): the IAM permissions (`PutItem`, `UpdateItem`, `DeleteItem`, `GetItem`, `DescribeTable`, plus `CreateTable` and `UpdateTimeToLive` when `CreateTableIfNotExists`), the NTP assumption of design choice 2, the crash recovery bound (one lease), on-demand billing.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
You are implementing Phase 4 of issue #210: the cross-cutting statements of the DynamoDB lock provider.

CONTEXT:
- Tenant scoping lives above the provider (issue #741); the key it receives is already tenant-qualified.
- A fault is Left, never "held" and never "free".

TASK:
Add the unit test that proves the provider is tenant-agnostic, the tests that prove a fault is Left on every public
method (including a key over the partition-key limit), and the README paragraph through docs-writer.

KEY RULES:
- No new public API in this phase.
- Documentation goes through docs-writer; changelog and PublicAPI through mechanical-fixer.

REFERENCE FILES:
- issue #741 (the tenant decorator; read the issue, its plan is not on main yet)
- src/Encina.DistributedLock.DynamoDB/DynamoDBDistributedLockProvider.cs
```

</details>

### Phase 5: Observability — logs, diagnostics and EventIds

> **Goal**: Operational visibility with EventIds registered first (ADR-021).

<details>
<summary><strong>Tasks</strong></summary>

1. **`src/Encina/Diagnostics/EventIdRanges.cs`** (modify, PR 1) — `public static readonly (int Min, int Max) DistributedLockDynamoDB = (7580, 7589);` with the XML summary `Encina.DistributedLock.DynamoDB — DynamoDB conditional-write lock provider`; the new field line in `src/Encina/PublicAPI.Unshipped.txt`
2. **`tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs`** (modify, PR 1) — `["Encina.DistributedLock.DynamoDB"] = [nameof(EventIdRanges.DistributedLockDynamoDB)]`
3. **Log messages** (PR 2) — 7580 lock acquired (Debug), 7581 lock released (Debug), 7582 wait elapsed, lock contended (Debug), 7583 acquire fault (Warning, exception type and AWS error code), 7584 release failed, the lease expires on its own (Warning), 7585 lease lost during heartbeat (Warning), 7586 heartbeat failed transiently (Warning), 7587 deadline reached, lock released by the handle (Information), 7588 lock table created (Information). 7589 stays spare. Contention is never an error level.
4. **Metrics and spans** (PR 2) — `DynamoDBLockDiagnostics`: spans `DistributedLock.Acquire` and `DistributedLock.Release`, counters of Phase 2; names align with the shared instruments of [#724](https://github.com/dlrivada/Encina/issues/724) and move onto them if it lands first.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
You are implementing Phase 5 of issue #210: EventIds, logs and instruments of the DynamoDB lock provider.

CONTEXT:
- ADR-021: register the range in src/Encina/Diagnostics/EventIdRanges.cs before using an id; map the assembly in
  EncinaEventIdAllocationTests. The family block 7550-7599 of the plan batch gives this package 7580-7589.
- Contention is not a failure: Debug level and outcome "contended".

TASK:
Register DistributedLockDynamoDB = (7580, 7589), map the assembly, add the [LoggerMessage] methods with the ids in
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
| `DynamoDBDistributedLockProvider.cs` | 90 | 14 | 45 | 70 | 0 | The retry loop, registry, heartbeat timer, handle and error mapping run against the fake gateway; the contract suite adds the shared scenarios; integration adds LocalStack; property 0: no randomized logic beyond the schema helper |
| `DynamoDBLockGateway.cs` | 5 | 5 | 0 | 90 | 0 | Needs LocalStack; unit and guard reach only the guards; contract 0 because the contract class runs on the shared LocalStack container (integration flag) |
| `DynamoDBLockSchema.cs` | 100 | 0 | 0 | 0 | 95 | Pure functions; guard 0: internal type without `InternalsVisibleTo` for `Encina.GuardTests`; FsCheck covers epoch conversion, TTL grace and lease clamp |
| `DynamoDBLockOptions.cs` | 100 | 0 | 0 | 0 | 0 | Property bag; guard 0: no parameters |
| `DynamoDBLockOptionsValidator.cs` | 100 | 0 | 0 | 0 | 0 | Pure rules; guard 0: internal type without `InternalsVisibleTo` for `Encina.GuardTests` |
| `ServiceCollectionExtensions.cs` | 95 | 90 | 0 | 0 | 0 | DI with a validated provider; guard covers each overload |
| `Health/DynamoDBDistributedLockHealthCheck.cs` | 85 | 85 | 0 | 75 | 0 | Fake gateway for unit; the integration test runs it against LocalStack |
| `DynamoDBLockDiagnostics.cs` | 80 | 0 | 0 | 0 | 0 | Instruments exercised through the provider tests; guard 0: no parameters |

1. **Unit** — the scenarios of Phases 1 to 3 with `FakeTimeProvider` and the fake gateway.
2. **Guard** — the public constructor (null options, null logger; the constructor taking the internal `IDynamoLockGateway` is not reachable from `Encina.GuardTests`, which is not in the `InternalsVisibleTo` list, so its null check is a unit test), each method (null or whitespace resource, non-positive expiry as #2724 decides), and the registration overloads (null services, null client).
3. **Contract** — `DynamoDBDistributedLockContractTests`, on the `Cloud-LocalStack` collection (the contract test project already defines it), running the same scenarios as the other providers (free, contended, expiry, extend held and unheld, double dispose, cancellation); `Encina.ContractTests.csproj` reference. `LocalStackFixture.ClearAllDataAsync` clears SQS and SNS only, so the class deletes its own table in `InitializeAsync` (a table name distinct from the integration tests' table), otherwise contract and integration tests would share state across runs. #2667 does not state a reusable abstract base class: PR 4 reads the suite as #2667 merged it and either derives from its base or, when none exists, extracts one (a new file; PR 4 then splits if it passes 15 files).
4. **Property** — FsCheck on `DynamoDBLockSchema`: epoch conversion round trip, `Ttl` never earlier than `ExpiresAtMs`, lease never above the expiry.
5. **Integration** — `[Collection("Cloud-LocalStack")]` on `LocalStackFixture` (the collection exists in `Encina.IntegrationTests`), `[Trait("Category", "Integration")]`; the class creates its client on the fixture endpoint and deletes the lock table in `InitializeAsync`: two providers on one table block each other; release deletes the item; the heartbeat keeps a short lease alive past its duration; an item deleted behind the handle's back marks it lost; an abandoned handle's lease lapses and a contender takes the lock; the table is created when missing; `IsLockedAsync` does not disturb a contender; two instances creating the table concurrently both succeed; health check against LocalStack.
6. **Load and benchmark** — `.md` justification files under `tests/Encina.NBomber/Scenarios/Locking/` and `tests/Encina.BenchmarkTests/Encina.DistributedLock.Benchmarks/`: the provider is a thin client over conditional writes whose latency is the network, covered by the shared lock scenarios once its factory is added by the issue that owns those scenarios; a missing `.md` would mean "not evaluated" (`AGENTS.md` section 9).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
You are implementing Phase 6 of issue #210: the tests of the DynamoDB lock provider.

CONTEXT:
- Unit and guard tests were written with each phase; this phase adds contract, property, integration and the
  justification files, and raises the manifest targets to the measured values.
- Integration tests use the shared collection Cloud-LocalStack (LocalStackFixture); never a per-class container.
  The fixture clears SQS and SNS only: the test class cleans its own table.

TASK:
Write the contract class, the FsCheck schema property, the integration tests of the Tasks, the two justification
files, and measure every flag from the repository root with `dotnet test tests\Encina.<Flag>Tests --collect
"XPlat Code Coverage"` and `coverage-report.cs`.

KEY RULES:
- Tests execute real package code; assertions with Shouldly through Encina.Testing.Shouldly.
- No Thread.Sleep: FakeTimeProvider for unit tests; polling with timeouts for integration.
- The PR reports measured per-flag coverage against the manifest targets.

REFERENCE FILES:
- tests/Encina.TestInfrastructure/Fixtures/LocalStackFixture.cs
- tests/Encina.IntegrationTests/MessageBrokers/AmazonSQS/LocalStackCollection.cs
- tests/Encina.IntegrationTests/Infrastructure/DistributedLock/SqlServerDistributedLockIntegrationTests.cs
```

</details>

### Phase 7: Documentation and Finalization

> **Goal**: The package is documented and the 1.0 inventory is true.

<details>
<summary><strong>Tasks</strong></summary>

1. **XML documentation** on every public member (summary, remarks, param, returns, one example per registration overload).
2. **`src/Encina.DistributedLock.DynamoDB/README.md`** (new, `docs-writer`) — what the provider does, the table layout, the lease and heartbeat, the NTP assumption, the permissions, LocalStack use, a `Match` example with `await using`.
3. **`changelog.d/210-lock-dynamodb.added.md`** (`mechanical-fixer`).
4. **`AGENTS.md` section 5**: no edit (the lock table already lists DynamoDB, #210); `docs/INVENTORY.md` gets the package row when the file lists packages (`docs-writer` checks).
5. **ADR**: none; the decisions are the lock family's ADR-052 and this plan's choices.
6. **Build and test verification**: `dotnet build --configuration Release` with zero warnings; every flag at its manifest target; `check-plan.ps1` for this file.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
You are finishing issue #210: documentation and verification of Encina.DistributedLock.DynamoDB.

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
| Amazon DynamoDB Developer Guide, condition expressions and conditional writes | `PutItem`, `UpdateItem` and `DeleteItem` with a `ConditionExpression` are atomic; a failed condition raises `ConditionalCheckFailedException` | The acquire, renew and release primitives |
| Amazon DynamoDB Developer Guide, Time To Live | Expired items are deleted in the background, typically within days of expiry; the TTL attribute is epoch seconds | Why correctness compares `ExpiresAtMs` and TTL only cleans |
| Amazon DynamoDB Developer Guide, read consistency | `GetItem` with `ConsistentRead` returns the latest committed write | The non-intrusive probe |
| AWS DynamoDB Lock Client (awslabs/amazon-dynamodb-lock-client) | Lease duration, heartbeat, record version number and local-clock observation | The model of Dilemmas 2 and 3 |
| ADR-052 | Lock providers return `Either`; contention is `Right(None)`; a fault is `Left` | Outcome mapping of every method |
| ADR-062 | Error categories; R4 provider failures are `Internal`; R5 cancellation | Categories of the `Left` cases |
| SPEC-000 REQ-027, DEC-003, DEC-009, AC-027 | Seven lock providers for 1.0, DynamoDB tested with LocalStack or DynamoDB Local | This provider joins the contract suite |
| `AGENTS.md` sections 3, 5, 6, 7, 9 | Secrets, time, errors, EventIds, coverage, lock table | Constraints used throughout |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|----------------------|
| `IDistributedLockProvider`, `ILockHandle`, `DistributedLockOptions` | `src/Encina.DistributedLock/` | The contract (after #2667) and the base options |
| `SqlServerDistributedLockProvider` and its registration | `src/Encina.DistributedLock.SqlServer/` | Structural model: handle, `GetLockKey`, log methods, health registration |
| `SqlServerDistributedLockHealthCheck` | `src/Encina.DistributedLock.SqlServer/Health/` | Model of the health check |
| `IEncinaHealthCheck`, `ProviderHealthCheckOptions` | `src/Encina.Messaging/Health/` | Health check contract and options |
| `EventIdRanges`, `EncinaEventIdAllocationTests` | `src/Encina/Diagnostics/EventIdRanges.cs`, `tests/Encina.UnitTests/Testing/Architecture/` | Registration of 7580-7589 and the assembly map |
| `LocalStackFixture` and the `Cloud-LocalStack` collection | `tests/Encina.TestInfrastructure/Fixtures/LocalStackFixture.cs`, `tests/Encina.IntegrationTests/MessageBrokers/AmazonSQS/LocalStackCollection.cs`, `tests/Encina.ContractTests/MessageBrokers/AmazonSQS/LocalStackCollection.cs` | Integration and contract tests on the shared emulator |
| `Encina.Messaging.Encryption.AwsKms` | `src/Encina.Messaging.Encryption.AwsKms/` | Sibling that takes an AWS client from the application |
| `AWSSDK.*` 4 entries, `Testcontainers.LocalStack` | `Directory.Packages.props` | Version alignment for the new `AWSSDK.DynamoDBv2` entry |
| `ForLogging()` | `src/Encina/Diagnostics/` | Exception logging without the message |

### Provider Matrix (distributed locks, SPEC-000 DEC-009)

| Provider | This plan | Notes |
|----------|:-:|-------|
| InMemory | N/A | Single-process testing provider; changed by #2667 |
| Redis | N/A | Quorum mode is [#226](https://github.com/dlrivada/Encina/issues/226) |
| SqlServer | N/A | Structural model; defects are #2405, #2720, #2722 |
| PostgreSQL (#207) | N/A | Own plan; same registry, expiry and error rules |
| MySQL (#208) | N/A | Own plan; same rules |
| Azure Blob (#209) | N/A | Own plan; the other cloud provider, the heartbeat is its lease renewal |
| DynamoDB (#210) | Included | The subject of this plan |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.DistributedLock.DynamoDB` (new range `DistributedLockDynamoDB`) | 7580-7589 | From the block 7550-7599 assigned to the family `lock-providers-1`; 7580-7588 used, 7589 spare. Siblings: 7550-7559 PostgreSQL (#207), 7560-7569 MySQL (#208), 7570-7579 Azure Blob (#209) |
| `Encina.DistributedLock.SqlServer` (existing, for reference) | 3800-3849 | Not touched |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Production (new) | 10 | csproj, options, validator, schema helper, gateway seam, DynamoDB adapter, provider, diagnostics, health check, registration (the `AcquireOutcome` type lives inside the gateway file) |
| Metadata (new and modified) | 8 | two `PublicAPI` files, `Directory.Packages.props`, `Encina.slnx`, `EventIdRanges.cs`, `Encina` `PublicAPI.Unshipped.txt`, manifest |
| Unit tests | 7 | options, schema, provider, handle and heartbeat, validator, DI, health check (plus the fake gateway) |
| Guard tests | 2 | provider, registration and health check |
| Contract and property tests | 2 | contract class, schema property |
| Integration tests | 1 | one class on the shared LocalStack collection |
| Test project references | 5 | UnitTests, GuardTests, ContractTests, IntegrationTests, PropertyTests |
| Documentation and changelog | 4 | README, fragment, two justification files |
| **Total** | **~39 touches** | Over 4 pull requests of at most 15 files each (14, 11, 8 and 11) |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
You are implementing issue #210: Encina.DistributedLock.DynamoDB, a lock provider on DynamoDB conditional writes.

PROJECT CONTEXT:
- .NET 10 / C# 14, nullable enabled, pre-1.0: best solution first, no compatibility layers, no [Obsolete].
- Lock contract: ADR-052, implemented by #2667 before this work: TryAcquireAsync returns
  Either<EncinaError, Option<ILockHandle>>, AcquireAsync returns Either<EncinaError, ILockHandle>, IsLockedAsync and
  ExtendAsync return Either<EncinaError, bool>, cancellation propagates, LockErrorCodes holds the Left codes.
- Time only from TimeProvider. EncinaError.Message never reaches logs, tags or health checks.

IMPLEMENTATION OVERVIEW:
1. Package skeleton: csproj (AWSSDK.DynamoDBv2, added to Directory.Packages.props unless #589 did), 
   DynamoDBLockOptions (TableName, Region, ServiceUrl, CreateTableIfNotExists, UseHeartbeat, HeartbeatInterval,
   ClockSkewAllowance), DynamoDBLockSchema (attribute names, key, epoch, TTL grace, lease clamp).
2. IDynamoLockGateway seam and DynamoDBLockGateway adapter; DynamoDBDistributedLockProvider: conditional PutItem
   with "attribute_not_exists(LockKey) OR ExpiresAtMs < :threshold", retry loop until wait, one item per resource,
   the handle renews its lease on a TimeProvider timer that never throws and deletes the item at its deadline,
   marks itself lost when the item is gone; provider ExtendAsync through a registry of live handles;
   IsLockedAsync through a consistent GetItem; ConditionalCheckFailedException on put is Right(None).
3. AddEncinaDistributedLockDynamoDB (options and IAmazonDynamoDB overloads, TryAdd*), options validator
   (ValidateOnStart), health check (DescribeTable), DI test with ValidateOnBuild and ValidateScopes.
4. EventIds 7580-7588 in range DistributedLockDynamoDB (7580-7589), diagnostics, ActivitySource and Meter.
5. Tests: unit (fake gateway, FakeTimeProvider), guard, contract (suite of #2667, Cloud-LocalStack collection),
   property (schema), integration on LocalStack; documentation through docs-writer; changelog and PublicAPI
   through mechanical-fixer.

KEY PATTERNS:
- Contention is Right(None); a DynamoDB fault is Left; a lost lease is Right(false) on ExtendAsync.
- Correctness compares ExpiresAtMs; the TTL attribute only cleans the table.
- Four pull requests of at most 15 files; each builds and passes alone.

REFERENCE FILES:
- docs/architecture/adr/052-distributed-lock-failure-contract.md
- src/Encina.DistributedLock.SqlServer/SqlServerDistributedLockProvider.cs
- src/Encina.DistributedLock.SqlServer/ServiceCollectionExtensions.cs
- tests/Encina.TestInfrastructure/Fixtures/LocalStackFixture.cs
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Lock state is ephemeral coordination state; caching it would be a defect |
| 2 | OpenTelemetry | ✅ | Phase 5: spans for acquire and release, acquisitions and renewals counters with `provider` and `outcome` tags; no tenant tag here, the #741 decorator adds `encina.tenant_id` (ADR-054 D4); names align with [#724](https://github.com/dlrivada/Encina/issues/724) |
| 3 | Structured Logging | ✅ | Phase 5: `[LoggerMessage]` EventIds 7580-7588 in the registered range, exception type and AWS error code only |
| 4 | Health Checks | ✅ | Phase 3: `DynamoDBDistributedLockHealthCheck` runs `DescribeTable` |
| 5 | Validation | ✅ | Phase 3: options validator at startup (table name, heartbeat, skew); resource and expiry arguments validated as [#2724](https://github.com/dlrivada/Encina/issues/2724) decides |
| 6 | Resilience | ✅ | Phase 4: the AWS SDK retry policy absorbs throttling and transient errors; the heartbeat retries on the next tick while the lease is valid; no Polly policy on top |
| 7 | Distributed Locks | ❌ | This is the distributed lock feature itself |
| 8 | Transactions | ❌ | A lock is not a transactional resource; no unit of work is involved |
| 9 | Idempotency | ❌ | A repeated acquire is a new attempt by design; the conditional writes are idempotent per `LockId` |
| 10 | Multi-Tenancy | ⏭️ | Tenant-scoped keys are composed above the provider by [#741](https://github.com/dlrivada/Encina/issues/741) (existing issue); the key is stored unhashed, so tenant/resource pairs stay injective; per-tenant tables are not needed |
| 11 | Module Isolation | ❌ | Isolation is reached with `KeyPrefix`; the provider holds no module data |
| 12 | Audit Trail | ❌ | Acquire and release are coordination events, not data access or state changes; logs and metrics cover them |

---

## Prerequisites & Dependencies

| Item | State | Effect on this plan |
|------|-------|---------------------|
| [#2667](https://github.com/dlrivada/Encina/issues/2667) — lock providers return `Either` (ADR-052) | Open, implementation issue (no pending maintainer decision) | PR 1 can start before it; PRs 2 to 4 start after it merges |
| [#589](https://github.com/dlrivada/Encina/issues/589) — DynamoDB dead letter store | Open (family plan elsewhere) | Also adds `AWSSDK.DynamoDBv2` to `Directory.Packages.props`; the second PR to land drops the line |
| [#2724](https://github.com/dlrivada/Encina/issues/2724) — argument validation of lock providers | Open | The guard tests follow its decision for empty resources and non-positive expiry |
| [#741](https://github.com/dlrivada/Encina/issues/741) — tenant-scoped locks | Open (family plan) | Owns tenant key composition; provider unchanged |
| [#755](https://github.com/dlrivada/Encina/issues/755) — health checks of the other lock providers | Open | Adds the first lock reference to the contract test project; whichever lands first, the other drops its line |
| [#724](https://github.com/dlrivada/Encina/issues/724) — lock observability | Open | Phase 5 reuses shared instruments if they exist |
| [#218](https://github.com/dlrivada/Encina/issues/218), [#223](https://github.com/dlrivada/Encina/issues/223) — fencing tokens, auto-extend | Open | Out of scope; the heartbeat inside the handle is this provider's own need, not the generic auto-extend |

---

## Next Steps

1. Link this plan from issue #210 and start the PR 1 brief; PRs 2 to 4 wait for [#2667](https://github.com/dlrivada/Encina/issues/2667).
2. When work on the issue starts, ask the maintainer to answer Dilemmas 2 (wall-clock lease or observation model), 3 (heartbeat placement), 4 (table creation default) and 5 (delete or keep on release), record the answers in a `## Maintainer Decisions` section placed last, and have the adversarial-reviewer verify the Settled and Trivial choices.
3. One commit per phase; the last commit of PR 4 references `Fixes #210`.
