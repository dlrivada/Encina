# Implementation Plan: MongoDB distributed lock provider (atomic upsert with a lease, TTL index for cleanup)

> **Issue**: [#609](https://github.com/dlrivada/Encina/issues/609)
> **Type**: Feature
> **Complexity**: Medium (extends one existing package, 3 pull requests)
> **Estimated Scope**: ~550-700 lines of production code + ~1,000-1,300 lines of tests
> **Specification**: [SPEC-000](../specifications/SPEC-000-encina-1.0-baseline-and-release-scope.md) REQ-027, DEC-009, AC-027 (the lock set of 1.0 is closed at seven providers; see Design Choice 1)
> **Decision**: [ADR-052](../architecture/adr/052-distributed-lock-failure-contract.md) (lock failure contract), [ADR-062](../architecture/adr/062-error-category-contract.md) (error categories)
> **Depends on**: [#2667](https://github.com/dlrivada/Encina/issues/2667) (implements ADR-052; open, not a pending maintainer decision)
> **Related**: [#207](https://github.com/dlrivada/Encina/issues/207), [#208](https://github.com/dlrivada/Encina/issues/208), [#209](https://github.com/dlrivada/Encina/issues/209), [#210](https://github.com/dlrivada/Encina/issues/210) (the four new lock providers, same family; #210 owns the heartbeat design this plan mirrors), [#226](https://github.com/dlrivada/Encina/issues/226) (Redis RedLock), [#741](https://github.com/dlrivada/Encina/issues/741) (tenant-scoped locks), [#218](https://github.com/dlrivada/Encina/issues/218) (fencing tokens), [#223](https://github.com/dlrivada/Encina/issues/223) (auto-extend), [#724](https://github.com/dlrivada/Encina/issues/724) (lock observability), [#2405](https://github.com/dlrivada/Encina/issues/2405), [#2724](https://github.com/dlrivada/Encina/issues/2724)

---

## Summary

Add an `IDistributedLockProvider` over **MongoDB** to the existing `Encina.MongoDB` package, opt-in with a new feature flag `EncinaMongoDbOptions.UseDistributedLock` like every other Mongo feature (outbox, inbox, sagas, scheduling, dead letters, audit).

1. **One document per resource.** The lock key is the `_id` of a document in a lock collection (`OwnerId`, `AcquiredAtUtc`, `ExpiresAtUtc`). Acquire is one atomic `UpdateOne` upsert whose filter is `{ _id: key, ExpiresAtUtc: { $lte: now } }`: a missing document is inserted, an expired one is taken over, and a held one fails the upsert with a duplicate key error, which is contention.
2. **Correctness never depends on the TTL index.** MongoDB deletes expired documents in a background pass (about every 60 seconds), so the filter compares `ExpiresAtUtc` itself; the TTL index (`ExpireAfter = 0`) only cleans the collection.
3. **Heartbeat inside the handle.** The handle writes a short lease (`3 x HeartbeatInterval`, default 30 s) and renews it with a conditional `UpdateOne` on its own `OwnerId` until its own deadline, then deletes the document. A crashed process stops renewing and the lock frees itself within one lease.
4. **Poll, do not block.** Acquire retries every `retry` until the `wait` deadline; delays use `TimeProvider`.
5. **ADR-052 outcomes.** Acquired is `Right(Some(handle))`, contention or the wait elapsing is `Right(None)`, a MongoDB fault is `Left`, cancellation propagates.
6. **Non-intrusive probe.** `IsLockedAsync` is a `Find` of an unexpired document; it never writes.
7. **Operations.** The existing `MongoDbHealthCheck` already covers connectivity; `[LoggerMessage]` EventIds 3190-3198 (a sub-range reserved for this plan) inside the registered MongoDB range, an `ActivitySource` and a `Meter`.

The issue's sample (`DistributedLockProviderMongoDB`, `IDistributedLock`, `DateTime.UtcNow`, an unexpired-lock filter that treats a missing owner as free) does not match the code: `IDistributedLockProvider` has no `IDistributedLock`, time must come from `TimeProvider` (`AGENTS.md` section 3), and the real contract is ADR-052. This plan uses the real names.

**Standards**: MongoDB manual (atomic single-document updates, upsert and the duplicate key error, TTL indexes with `expireAfterSeconds: 0`, write concern `majority`); ADR-052; SPEC-000 REQ-027.

**Estimated scope**: 7 new production files in a new `Locking/` folder of `src/Encina.MongoDB/`, 5 modified production files, about 9 test files, README and changelog fragment; 3 pull requests.

**Affected packages**: `Encina.MongoDB` (new `Locking/` folder; a new project reference to `Encina.DistributedLock`); `Encina` is not touched (the logs use the existing `MongoDB` range 3100-3199). `Encina.DistributedLock` is consumed, not changed.

**Provider category**: Distributed locks (`AGENTS.md` section 5), implemented on the MongoDB provider of the database family. Matrix in Research.

**What the code does today** (`origin/main` at `0b941beeb`):

- `src/Encina.MongoDB` has no lock code and no reference to `Encina.DistributedLock` (it references `Encina`, `Encina.Messaging`, the compliance packages and others). Every feature there is gated by a flag of `EncinaMongoDbOptions` (`UseOutbox`, `UseInbox`, `UseSagas`, `UseScheduling`, `UseDeadLetterQueue`, `UseAuditLogStore`, ...), has collection names in `MongoDbCollectionNames`, and creates its indexes in `MongoDbIndexCreator.FeatureIndexCreators()`.
- EventIds 3100-3166 are used in `src/Encina.MongoDB/Log.cs` (3162 lives in `ReadWriteSeparation/ReadWriteRoutingPipelineBehavior.cs`; 3128 is an unused gap); 3167-3199 are free inside the registered `MongoDB = (3100, 3199)` range, but not exclusive: the plans of #594 and #590 claim "3167+" for their MongoDB log entries, so this plan takes the top of the range, 3190-3198, and leaves 3167-3189 to them.
- `MongoDbHealthCheck` (`DefaultName = "encina-mongodb"`) exists; `MongoDbFixture` (`mongo:7`, collection name `MongoDB`) and its `ClearAllDataAsync` exist.
- `IDistributedLockProvider` still has the pre-ADR-052 shapes; [#2667](https://github.com/dlrivada/Encina/issues/2667) changes them and creates `LockErrorCodes`. **This plan is written to the ADR-052 contract; implementation starts after #2667 merges.**
- `Encina.UnitTests`, `Encina.GuardTests`, `Encina.PropertyTests` and `Encina.IntegrationTests` reference both `Encina.MongoDB` and `Encina.DistributedLock`; `Encina.ContractTests` references `Encina.MongoDB`, which will bring `Encina.DistributedLock` in transitively. No contract test class for locks exists yet; #2667 creates the suite.

---

## Design Choices

<details>
<summary><strong>1. Scope — a MongoDB lock in the 1.0 contract, or after it</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Implement it for 1.0 as an eighth lock provider** | MongoDB-only deployments get a lock without Redis or a SQL database; the provider contract suite makes the marginal cost small | SPEC-000 REQ-027 and AC-027 and the `AGENTS.md` section 5 lock table say "exactly these 7"; adding an eighth needs a maintainer amendment of both, and one more provider to keep in step with every future lock change |
| **B) Keep it outside the 1.0 seven: the plan stays ready, the issue moves to a post-1.0 milestone** | Honors the closed list the maintainer fixed on 2026-10-23 after reviewing the open issues (#609 was open then and was not added) | A MongoDB-only deployment must use Redis, SQL Server, PostgreSQL, MySQL or a cloud lock until after 1.0 |

### Chosen Option: **B — outside the 1.0 seven** (recommended, pending the maintainer)

### Rationale

- The seven providers are listed by name in SPEC-000 REQ-027 and AC-027, and the lock table of `AGENTS.md` section 5 says "exactly these 7"; the issue sits in the milestone `v0.14.0 — Hardening`, not in the provider milestone `v0.19.0` where #207 to #210 are. This plan does not decide the scope: it is written so that either option can start from it.
- Option A wins if the reference application or another adopter runs on MongoDB alone and needs a lock before 1.0; the maintainer can pick it when work starts, and the amendment of SPEC-000 and `AGENTS.md` is part of the first pull request then.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Placement — inside `Encina.MongoDB` behind a flag, or a new package `Encina.DistributedLock.MongoDB`</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A `Locking/` folder in `Encina.MongoDB` with `UseDistributedLock`** | The issue's wording; reuses `IMongoClient`, `EncinaMongoDbOptions`, the index creator, the health check and `MongoDbFixture`; one registration call (`AddEncinaMongoDB`) like every other Mongo feature | `Encina.MongoDB` gains a project reference to `Encina.DistributedLock` (a small assembly that already depends on `Encina.Messaging`), which every Mongo user restores |
| **B) A new package `Encina.DistributedLock.MongoDB`** | The `Encina.DistributedLock.{Backend}` naming of the seven; no new dependency for other Mongo users | A second `IMongoClient` registration story (or a requirement that the application registers one); its own options, index creation and health check; a package outside the database provider family |

### Chosen Option: **A — inside `Encina.MongoDB` behind `UseDistributedLock`** (recommended, pending the maintainer)

### Rationale

- Every other Mongo feature follows this pattern, the options and the client are already there, and `AGENTS.md` section 5 keeps provider-dependent database code in the database provider packages. Option B wins if the maintainer wants the `Encina.DistributedLock.*` set to be uniform packages, or wants Mongo users not to restore the lock assembly.
- The folder is `Locking/` and the namespace `Encina.MongoDB.Locking`, not `DistributedLock`: a namespace `Encina.MongoDB.DistributedLock` would shadow `Encina.DistributedLock` inside `Encina.MongoDB`.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Time source for the lease — the application clock, or the server clock</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `TimeProvider`: `now` is computed in the application and written into the filter and the update** | `AGENTS.md` section 3 (time from `TimeProvider`); `FakeTimeProvider` tests every expiry path; the same model as the DynamoDB provider of [#210](https://github.com/dlrivada/Encina/issues/210) | Correct only when the clocks of the instances differ by less than `ClockSkewAllowance` (default 2 s, added to the takeover condition); the README states the NTP assumption |
| **B) The server clock: the update is an aggregation pipeline using `$$NOW` for the comparison and the new deadline** | One clock for all instances; no skew problem | Tests cannot advance a fake clock (expiry tests need real waiting); the lease deadline known to the handle must be read back or approximated; a pipeline update is a heavier operation |

### Chosen Option: **A — `TimeProvider` with a skew allowance** (recommended, pending the maintainer)

### Rationale

- The rule of `AGENTS.md` section 3 is stated for production code reading the clock, and A keeps every deadline computation in one testable place. Option B wins where instances cannot be time-synchronized; the maintainer can pick it when work starts.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Heartbeat — inside each handle, in one hosted service, or none</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Each handle renews its own lease with a `TimeProvider` timer every `HeartbeatInterval` and deletes the document at its deadline** | No hosted service to register (registration stays complete on its own); the heartbeat lives and dies with the handle; `FakeTimeProvider` tests it; the same design as [#209](https://github.com/dlrivada/Encina/issues/209) and [#210](https://github.com/dlrivada/Encina/issues/210) | One timer per live handle |
| **B) One `BackgroundService` renews every registered handle** | One loop for any number of locks | A hosted service must run for the provider to be correct; a stopped service silently lets leases lapse under live holders |
| **C) No heartbeat: the document expires at the caller's deadline** | Simplest | A crash frees the lock only at the caller's `expiry`, which may be minutes |

### Chosen Option: **A — heartbeat inside each handle, `UseHeartbeat = false` giving option C** (recommended, pending the maintainer)

### Rationale

- The interface promises auto-release on a crash and the provider must work without a hosted service (`AGENTS.md` section 3, registration completeness). The timer callback catches every exception and logs only the exception type; a renewal whose filter matches nothing marks the handle lost (`IsReleased` becomes true, a Warning is logged, `ExtendAsync` answers `Right(false)`). The lease is `3 x HeartbeatInterval` clamped to the caller's `expiry`.

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Acquire operation and the TTL index</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `UpdateOne` with `IsUpsert = true`, filter `{ _id: key, ExpiresAtUtc: { $lte: now - skew } }`; a duplicate key error (`E11000`) is contention; `UpsertedId` or `ModifiedCount == 1` is acquired; a TTL index on `ExpiresAtUtc` with `ExpireAfter = 0` created by `MongoDbIndexCreator` when the flag is on** | One atomic single-document operation; no read of the document is needed, so `findAndModify` (the issue's wording) adds nothing; the TTL index is only housekeeping | The duplicate key case must be recognized for both `MongoWriteException` and `MongoBulkWriteException` shapes |
| **B) `FindOneAndUpdate` with `ReturnDocument.After` and a comparison of the returned owner** | The issue's wording | A second code path to decide ownership; the same duplicate key handling; a larger reply |

### Chosen Option: **A — upsert `UpdateOne`**

### Rationale

- The two operations are the same atomic server primitive for one document, and the result of A answers the only question asked ("did my write take the document"). The sample of the issue treats a document with no owner as free; here the only free states are "no document" and "expired", so a document never exists without an owner.
- Other Mongo features of the package avoid TTL indexes because their cleanup loops keep metrics honest (comment in `MongoDbIndexCreator`); a lock document has no metrics, expires by design, and the TTL index is the issue's explicit task, so it is housekeeping here.

**Class:** Trivial
**Settled by:** MongoDB manual on `updateOne` (https://www.mongodb.com/docs/manual/reference/method/db.collection.updateOne/: a single-document update is atomic, and an upsert on a unique `_id` that loses a race fails with a duplicate key error); `src/Encina.MongoDB/MongoDbIndexCreator.cs` (feature-flag index creation)

</details>

<details>
<summary><strong>6. Write and read concern</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The lock collection is obtained `WithWriteConcern(WriteConcern.WMajority)` and `WithReadConcern(ReadConcern.Majority)`, reads on the primary** | A lock grant survives a primary failover; a probe never reads a rolled-back write | A higher latency on replica sets; on a standalone server majority means one node and costs nothing |
| **B) The client defaults** | No code | A default of `w:1` can lose an acknowledged lock grant on failover and give the lock to two holders |

### Chosen Option: **A — majority write and read concern**

### Rationale

- Mutual exclusion is the whole value of a lock, so the lock collection never uses a weaker concern than majority; the options expose no knob to lower it.

**Class:** Trivial
**Settled by:** MongoDB manual on write concern (https://www.mongodb.com/docs/manual/reference/write-concern/: a `w:1` write can be rolled back after a failover, a `w:majority` write cannot)

</details>

<details>
<summary><strong>7. Probe, provider-level extend and the testability seam</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `IsLockedAsync` is a `Find` of the document with `ExpiresAtUtc > now`; provider `ExtendAsync(resource)` extends only a handle this provider instance holds, else `Right(false)`; an `internal` seam `IMongoLockStore` with one adapter over `IMongoCollection<LockDocument>` holds all driver calls** | No write to test a lock; ownership is real; the retry loop, heartbeat and error mapping run in unit tests against a fake | One more internal file |
| **B) Probe by trying to acquire and deleting; `ExtendAsync` answers `Right(true)`; driver calls inline** | Fewer files | The probe causes false contention; `Right(true)` for an unchecked lock is #2405; the driver is only testable through Docker |

### Chosen Option: **A — `Find` probe, local registry, store seam**

### Rationale

- ADR-052 defines `ExtendAsync` as `Right(false)` when the lock is "not held"; the blocking `crap-gate` needs the provider logic covered by fast tests (`AGENTS.md` section 9).

**Class:** Settled
**Settled by:** ADR-052 ("not held" is `Right(false)`); #2405 (the defect option B reproduces); `AGENTS.md` section 9

</details>

<details>
<summary><strong>8. Error mapping and categories</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Classify once in the store: a duplicate key on the acquire upsert is `Right(None)`; any other `MongoException` (connection, timeout, write failure) is `Left` with the backend-fault code of `LockErrorCodes`; cancellation propagates** | One mapping; contention is never a failure; cancellation is not an error | Needs the exact code names of #2667 (read before editing) |
| **B) Catch `Exception` in the provider** | Smaller | Collapses cancellation and programming errors into `Left` |

### Chosen Option: **A — classify in the store**

### Rationale

- This plan adds **no new error code**: every `Left` carries the backend-fault code #2667 defines, whose category follows ADR-062 rule R4 (provider failures are `Internal`); cancellation is never a `Left` (R5). The error never carries the exception message; logs carry the exception type through `ForLogging()`.

**Class:** Trivial
**Settled by:** ADR-052; ADR-062 rules R4 and R5; `AGENTS.md` section 3 (`EncinaError.Message` rule)

</details>

---

## Implementation Phases

Three pull requests, each at most 15 files and 1500 changed lines counting tests, `PublicAPI.Unshipped.txt`, the coverage manifest, the README and the changelog fragment (`AGENTS.md` section 11). **PRs 2 and 3 build only after [#2667](https://github.com/dlrivada/Encina/issues/2667) merges**; PR 1 does not depend on it. The work starts only after the maintainer answers Design Choice 1.

| PR | Phases | Production and metadata files | Test and doc files | Files / lines (approx.) |
|---|---|---|---|---|
| 1 | Phase 1 | `Locking/MongoDbLockOptions`, `Locking/LockDocument`, `Locking/MongoLockTiming`, `EncinaMongoDbOptions.cs`, `MongoDbIndexCreator.cs`, `Log.cs`, `Encina.MongoDB.csproj`, `PublicAPI.Unshipped.txt`, manifest (9) | unit `MongoDbLockOptionsTests` and `MongoLockTimingTests`, the index creator unit test (extended) (3) | 12 files / 650 |
| 2 | Phases 2 and 5 | `Locking/IMongoLockStore`, `Locking/MongoLockStore`, `Locking/MongoDbDistributedLockProvider`, `Locking/MongoDbLockDiagnostics`, `Log.cs`, `PublicAPI.Unshipped.txt`, manifest (7) | `FakeMongoLockStore`, provider unit tests, handle and heartbeat unit tests, guard tests (4) | 11 files / 1,300 |
| 3 | Phases 3, 4, 6 and 7 | `ServiceCollectionExtensions.cs`, `PublicAPI.Unshipped.txt`, manifest (3) | DI test with `ValidateOnBuild`/`ValidateScopes`, `tests/Encina.GuardTests/MongoDB/ServiceCollectionExtensionsGuardTests.cs` (existing file, modified: null checks of the lock registration), contract class, the `MongoDB` collection definition in `Encina.ContractTests` (new file), integration tests, property test of the timing helper, `README.md` of `Encina.MongoDB`, changelog fragment, two load/benchmark justification files (10) | 13 files / 1,150 |

If a PR measures over 15 files or 1,500 lines, the worker splits it again and never asks for `size-exception-approved`. PR 2 has the least line slack: if the heartbeat tests grow, they move to PR 3.

### Phase 1: Options, document and timing helper

> **Goal**: The configuration surface, the stored shape and the pure time functions, with the TTL index.

<details>
<summary><strong>Tasks</strong></summary>

#### `src/Encina.MongoDB/`

1. **`Locking/MongoDbLockOptions.cs`** (new, public sealed) — `: DistributedLockOptions`; `bool UseHeartbeat = true`; `TimeSpan HeartbeatInterval = 10 s`; `TimeSpan ClockSkewAllowance = 2 s`; XML docs; no secret (the connection string stays in `EncinaMongoDbOptions`, which already protects it); `ToString()` prints the heartbeat settings only
2. **`Locking/LockDocument.cs`** (new, internal sealed) — `[BsonId] string Id`, `string OwnerId`, `DateTime AcquiredAtUtc`, `DateTime ExpiresAtUtc` (UTC kind, millisecond precision)
3. **`Locking/MongoLockTiming.cs`** (new, internal static) — `Compose(prefix, resource)` (the `lock:` format of the SQL Server provider; the key is stored unhashed in `_id`, so tenant-qualified keys of #741 stay injective), `LeaseFor(heartbeatInterval, expiry)` (`3 x interval` clamped to the expiry), `TakeoverThreshold(now, skew)`
4. **`EncinaMongoDbOptions.cs`** (modify) — `bool UseDistributedLock`, `MongoDbLockOptions DistributedLockOptions { get; } = new()`, and `MongoDbCollectionNames.DistributedLocks = "distributed_locks"`
5. **`MongoDbIndexCreator.cs`** (modify) — a `(UseDistributedLock, CreateDistributedLockIndexesAsync)` entry that creates the TTL index on `ExpiresAtUtc` with `ExpireAfter = TimeSpan.Zero` and logs `Log.CreatedDistributedLockIndexes` (EventId 3190, added to `Log.cs`; Phase 5)
6. **`Encina.MongoDB.csproj`** (modify) — `ProjectReference` to `..\Encina.DistributedLock\Encina.DistributedLock.csproj`
7. **`PublicAPI.Unshipped.txt`** (the options members) and the manifest entries (targets in Phase 6)
8. **Tests**: `MongoDbLockOptionsTests` (defaults, `ToString`), `MongoLockTimingTests` (key format, lease clamp, threshold), and the existing index creator unit test extended for the new entry

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of issue #609: the MongoDB distributed lock options, document and timing helper.

CONTEXT:
- Existing package src/Encina.MongoDB: every feature has a flag in EncinaMongoDbOptions, collection names in
  MongoDbCollectionNames and its indexes in MongoDbIndexCreator.FeatureIndexCreators().
- The lock document is { _id: "{prefix}:lock:{resource}", OwnerId, AcquiredAtUtc, ExpiresAtUtc }; the key is stored
  unhashed. A TTL index on ExpiresAtUtc (ExpireAfter = 0) is housekeeping only; correctness compares ExpiresAtUtc.
- Use the folder Locking/ and the namespace Encina.MongoDB.Locking (not DistributedLock: it would shadow
  Encina.DistributedLock).

TASK:
Add the options, the document, the timing helper, the flag and collection name, the TTL index entry in the index
creator, the project reference and the unit tests. Ask mechanical-fixer for the PublicAPI lines and the manifest.

KEY RULES:
- No behavior beyond options, document, pure timing functions and the index. Zero warnings (CA and RS rules).
- XML documentation on every public member. Time is not read in this phase.

REFERENCE FILES:
- src/Encina.MongoDB/EncinaMongoDbOptions.cs
- src/Encina.MongoDB/MongoDbIndexCreator.cs
- src/Encina.MongoDB/Encina.MongoDB.csproj
- src/Encina.DistributedLock/DistributedLockOptions.cs
```

</details>

### Phase 2: Store seam and provider

> **Goal**: The provider on the ADR-052 contract, fully testable without MongoDB.

<details>
<summary><strong>Tasks</strong></summary>

#### `src/Encina.MongoDB/Locking/`

1. **`IMongoLockStore.cs`** (new, internal) — `Task<Either<EncinaError, bool>> TryAcquireAsync(string key, string ownerId, DateTime nowUtc, DateTime thresholdUtc, DateTime expiresAtUtc, CancellationToken ct)` (true acquired, false contended), `RenewAsync(key, ownerId, nowUtc, newExpiresAtUtc, ct)` (false when the document is no longer ours or its lease has passed), `ReleaseAsync(key, ownerId, ct)`, `IsLockedAsync(key, nowUtc, ct)`; each returns `Either<EncinaError, bool>`
2. **`MongoLockStore.cs`** (new, internal sealed) — wraps `IMongoCollection<LockDocument>` taken from `IMongoClient`/`EncinaMongoDbOptions` with majority concerns (design choice 6); the upsert of design choice 5 and the duplicate key recognition (`MongoWriteException` with `ServerErrorCategory.DuplicateKey` and the bulk-write shape); the classification of design choice 8
3. **`MongoDbDistributedLockProvider.cs`** (new, public sealed partial) — `IDistributedLockProvider`; the public constructor is `(IMongoClient client, IOptions<EncinaMongoDbOptions> options, ILogger<MongoDbDistributedLockProvider> logger, TimeProvider? timeProvider = null)` and an internal constructor takes the `IMongoLockStore` for the tests; `TryAcquireAsync` (retry loop until `wait`), `AcquireAsync` (same loop, no deadline), `IsLockedAsync`, `ExtendAsync(resource, extension, ct)` through the registry of live handles; a private `LockHandle : ILockHandle` that renews the lease on a `TimeProvider` timer (callback catches everything), deletes the document at its deadline, marks itself lost when a renewal matches nothing, and moves its deadline on `ExtendAsync`; `OwnerId` is a new GUID per acquisition
4. **`MongoDbLockDiagnostics.cs`** (new, internal static) — `ActivitySource` and `Meter` named `Encina.MongoDB.Locking`; counter `encina.distributed_lock.acquisitions` with tags `provider` (`mongodb`) and `outcome` (`acquired`, `contended`, `failed`); counter `encina.distributed_lock.renewals` with tag `outcome` (`renewed`, `lost`, `failed`); no tenant tag (the decorator of #741 adds `encina.tenant_id`, [ADR-054](../architecture/adr/054-observability-conventions-and-trace-propagation.md) D4)
5. **`src/Encina.MongoDB/Log.cs`** (modify) — the `[LoggerMessage]` methods, EventIds 3191-3198 (Phase 5)
6. **`PublicAPI.Unshipped.txt`** (the provider and its constructor) and the manifest (targets in Phase 6)
7. **Tests**: `FakeMongoLockStore` (scripted results and a call log), provider unit tests (acquired, contended until the deadline with `FakeTimeProvider`, skew allowance respected, fault on acquire, cancellation, registry extend held and unheld, deadline release, double dispose), heartbeat tests (renews every interval, lost document marks the handle, transient failure retried, `UseHeartbeat = false` writes the full expiry once), guard tests of the public constructor and the four methods

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of issue #609: the MongoDB lock provider on the ADR-052 contract.

CONTEXT:
- #2667 has merged: IDistributedLockProvider returns Either<EncinaError, Option<ILockHandle>> (TryAcquireAsync),
  Either<EncinaError, ILockHandle> (AcquireAsync), Either<EncinaError, bool> (IsLockedAsync, ExtendAsync);
  LockErrorCodes exists. Read their real signatures and codes before editing.
- One document per resource; acquire is an atomic upsert whose duplicate key error is contention; the handle renews
  its lease on a TimeProvider timer and deletes the document at its deadline (design choices 4, 5, 7).

TASK:
Write IMongoLockStore and its driver adapter, the provider with its private handle and heartbeat, the diagnostics
class and the log methods (EventIds 3191-3198 in Log.cs). Write the fake store and the unit and guard tests of the
Tasks.

KEY RULES:
- Contention is Right(None), never a failure in logs or metrics. A fault is Left; the error and every log carry no
  exception message (ForLogging, exception type only).
- OperationCanceledException propagates; never wrap it. The heartbeat timer callback never throws.
- Time only from TimeProvider. Methods small enough for CRAP <= 10.

REFERENCE FILES:
- docs/architecture/adr/052-distributed-lock-failure-contract.md
- src/Encina.MongoDB/Log.cs
- src/Encina.DistributedLock/IDistributedLockProvider.cs and ILockHandle.cs
- src/Encina.DistributedLock.SqlServer/SqlServerDistributedLockProvider.cs
```

</details>

### Phase 3: Configuration and DI

> **Goal**: `UseDistributedLock = true` registers a complete provider, in any registration order.

<details>
<summary><strong>Tasks</strong></summary>

#### `src/Encina.MongoDB/`

1. **`ServiceCollectionExtensions.cs`** (modify) — when `UseDistributedLock` is true: `TryAddSingleton<IMongoLockStore>` (from `IMongoClient` and the options), `TryAddSingleton<IDistributedLockProvider, MongoDbDistributedLockProvider>`, an options validation (positive `HeartbeatInterval`, non-negative `ClockSkewAllowance` below the lease) with `ValidateOnStart`; `TimeProvider` is already registered by `AddEncinaMongoDB`; the existing `MongoDbHealthCheck` covers connectivity, so no new health check is added
2. **`PublicAPI.Unshipped.txt`** and the manifest, if the registration adds public members
3. **Tests**: DI test building the provider with `ValidateOnBuild` and `ValidateScopes` (registration completeness, `AGENTS.md` section 3), a registration-order test (an application's own `IDistributedLockProvider` registration is not overridden), a test that the flag off registers no lock service (pay-for-what-you-use)

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are implementing Phase 3 of issue #609: registration of the MongoDB lock provider.

CONTEXT:
- Phase 2 produced MongoDbDistributedLockProvider and IMongoLockStore.
- AddEncinaMongoDB already registers IMongoClient, TimeProvider and the options; the lock services are added only
  when EncinaMongoDbOptions.UseDistributedLock is true.

TASK:
Extend AddEncinaMongoDB with the lock registration and the options validation, and add the DI test with
ValidateOnBuild and ValidateScopes, the order test and the flag-off test.

KEY RULES:
- TryAdd* registrations so an application registration wins in any order.
- Invalid options fail at startup, not at the first lock (fail closed).
- No lock service is registered when the flag is off.

REFERENCE FILES:
- src/Encina.MongoDB/ServiceCollectionExtensions.cs
- src/Encina.DistributedLock.SqlServer/ServiceCollectionExtensions.cs
- tests/Encina.UnitTests/DistributedLock/SqlServer/ServiceCollectionExtensionsTests.cs
```

</details>

### Phase 4: Cross-cutting integration

> **Goal**: The decisions of the 12-function matrix that need code or a statement in tests.

<details>
<summary><strong>Tasks</strong></summary>

1. **Tenancy**: no code in the provider. The key is tenant-qualified by the decorator of [#741](https://github.com/dlrivada/Encina/issues/741) (reserved `default` tenant segment, injective encoding) and stored unhashed as `_id`, so distinct tenant and resource pairs are distinct documents. A unit test asserts the provider never reads a tenant.
2. **Fail closed**: a MongoDB fault never reports a held lock: `Left`; a lease that cannot be renewed makes the handle lost, and `ExtendAsync` is `Right(false)`.
3. **Resilience**: the driver's retryable writes and server selection timeout apply; the provider adds no Polly policy (its wait/retry loop is the retry).
4. **README note** (through `docs-writer`, in the `Encina.MongoDB` README): the flag, the collection, the NTP assumption, the replica set recommendation (majority concern), the crash recovery bound (one lease).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
You are implementing Phase 4 of issue #609: the cross-cutting statements of the MongoDB lock provider.

CONTEXT:
- Tenant scoping lives above the provider (issue #741); the key it receives is already tenant-qualified.
- A fault is Left, never "held" and never "free".

TASK:
Add the unit test that proves the provider is tenant-agnostic, the tests that prove a fault is Left on every public
method, and the README paragraph through docs-writer.

KEY RULES:
- No new public API in this phase.
- Documentation goes through docs-writer; changelog and PublicAPI through mechanical-fixer.

REFERENCE FILES:
- issue #741 (the tenant decorator; read the issue, its plan is not on main yet)
- src/Encina.MongoDB/Locking/MongoDbDistributedLockProvider.cs
```

</details>

### Phase 5: Observability — logs, diagnostics and EventIds

> **Goal**: Operational visibility with EventIds inside the registered MongoDB range (ADR-021).

<details>
<summary><strong>Tasks</strong></summary>

1. **Range**: no registration is needed. `EventIdRanges.MongoDB = (3100, 3199)` and the architecture test mapping of `Encina.MongoDB` already exist; ids 3190 to 3198 are free on `origin/main` at `0b941beeb` (`git grep` of `src/` finds none of 3167-3199 in use) and are reserved for this plan at the top of the range, because the plans of #594 and #590 claim "3167+" (grep of `docs/plans`); they take 3167-3189 between them.
2. **Log messages** — PR 1: 3190 distributed lock indexes created (Information, `Log.CreatedDistributedLockIndexes`, called by the index creator). PR 2, in `Log.cs`: 3191 lock acquired (Debug), 3192 lock released (Debug), 3193 wait elapsed, lock contended (Debug), 3194 acquire fault (Warning, exception type), 3195 release failed, the lease expires on its own (Warning), 3196 lease lost during heartbeat (Warning), 3197 heartbeat failed transiently (Warning), 3198 deadline reached, lock released by the handle (Information). 3199 stays spare. The sub-range is fixed (3190-3198, packed without gaps inside it), so the merge order of this plan against #594 and #590 does not change any id.
3. **Metrics and spans** (PR 2) — `MongoDbLockDiagnostics`: spans `DistributedLock.Acquire` and `DistributedLock.Release`, counters of Phase 2; names align with the shared instruments of [#724](https://github.com/dlrivada/Encina/issues/724) and move onto them if it lands first.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
You are implementing Phase 5 of issue #609: EventIds, logs and instruments of the MongoDB lock provider.

CONTEXT:
- ADR-021: ids must sit inside the package's registered range. Encina.MongoDB owns 3100-3199 and uses 3100-3166;
  the lock messages take the reserved sub-range 3190-3198, packed without gaps inside it.
- Contention is not a failure: Debug level and outcome "contended".

TASK:
Add the [LoggerMessage] methods to Log.cs with the ids in the Tasks (the index message in PR 1 is 3190, the
provider messages are 3191 to 3198), and the diagnostics class. Run the architecture tests.

KEY RULES:
- Packed ids, no gaps. Log the exception through ForLogging(), never its message.
- No tenant tag in the provider (ADR-054 D4 is applied by the #741 decorator).

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs
- src/Encina.MongoDB/Log.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

### Phase 6: Testing

> **Goal**: Every flag reaches its own target; the same contract scenarios as the other providers.

<details>
<summary><strong>Tasks</strong></summary>

Per-file targets (the manifest lists them with these sentences; the numbers are a demanding, realistic start and the worker raises them to what it measures):

| File | unit | guard | contract | integration | property | Justification |
|---|---:|---:|---:|---:|---:|---|
| `Locking/MongoDbDistributedLockProvider.cs` | 90 | 14 | 45 | 70 | 0 | The retry loop, registry, heartbeat timer, handle and error mapping run against the fake store; the contract suite adds the shared scenarios; integration adds a real MongoDB; property 0: no randomized logic beyond the timing helper |
| `Locking/MongoLockStore.cs` | 5 | 5 | 0 | 90 | 0 | Needs a real MongoDB; unit and guard reach only the guards; contract 0 because the contract class runs on the shared container (integration flag) |
| `Locking/MongoLockTiming.cs` | 100 | 0 | 0 | 0 | 95 | Pure functions; guard 0: internal type without `InternalsVisibleTo` for `Encina.GuardTests`; FsCheck covers the lease clamp and the threshold |
| `Locking/MongoDbLockOptions.cs` | 100 | 0 | 0 | 0 | 0 | Property bag; guard 0: no parameters |
| `Locking/MongoDbLockDiagnostics.cs` | 80 | 0 | 0 | 0 | 0 | Instruments exercised through the provider tests; guard 0: no parameters |
| `ServiceCollectionExtensions.cs` (changed lines) | 90 | 90 | 0 | 0 | 0 | The registration branch is covered by the DI tests; guard covers the null checks |
| `MongoDbIndexCreator.cs` (changed lines) | 80 | 0 | 0 | 85 | 0 | The new entry runs in the unit test; integration checks that the TTL index exists on a real server; guard 0: internal type, no new parameters |

1. **Unit** — the scenarios of Phases 1 to 3 with `FakeTimeProvider` and the fake store.
2. **Guard** — the public constructor (null client, null options, null logger) and each method (null or whitespace resource, non-positive expiry as #2724 decides).
3. **Contract** — `MongoDbDistributedLockContractTests`, on the `MongoDB` collection, which `Encina.ContractTests` does not define (the collection of `MongoDbFixture` is used from `Encina.IntegrationTests`), so PR 3 adds `tests/Encina.ContractTests/DistributedLock/MongoDB/MongoDbCollection.cs` (`[CollectionDefinition("MongoDB")]` with `ICollectionFixture<MongoDbFixture>`, the shape of `LocalStackCollection.cs` in that project). #2667 does not state a reusable abstract base class: PR 3 reads the suite as #2667 merged it and either derives from its base or, when none exists, extracts one (a new file; PR 3 stays at 14 files). The class runs the same scenarios as the other providers (free, contended, expiry, extend held and unheld, double dispose, cancellation).
4. **Property** — FsCheck on `MongoLockTiming`: lease never above the expiry, threshold never after `now`, key format round trip.
5. **Integration** — `[Collection("MongoDB")]` on `MongoDbFixture`, `[Trait("Category", "Integration")]`, `[Trait("Database", "MongoDB")]`: two providers on one database block each other; release deletes the document; the heartbeat keeps a short lease alive past its duration; a document deleted behind the handle's back marks it lost; an abandoned handle's lease lapses and a contender takes the lock; concurrent acquirers (many tasks, one key) produce exactly one holder; the TTL index exists; `IsLockedAsync` does not disturb a contender; cancellation inside a wait.
6. **Load and benchmark** — `.md` justification files under `tests/Encina.NBomber/Scenarios/Locking/` and `tests/Encina.BenchmarkTests/Encina.DistributedLock.Benchmarks/`: the provider is a thin client over single-document writes whose latency is the network, covered by the shared lock scenarios once its factory is added by the issue that owns those scenarios; a missing `.md` would mean "not evaluated" (`AGENTS.md` section 9).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
You are implementing Phase 6 of issue #609: the tests of the MongoDB lock provider.

CONTEXT:
- Unit and guard tests were written with each phase; this phase adds contract, property, integration and the
  justification files, and raises the manifest targets to the measured values.
- Integration tests use the shared collection MongoDB (MongoDbFixture); never a per-class container; the test
  class calls ClearAllDataAsync in InitializeAsync.

TASK:
Write the contract class, the FsCheck timing property, the integration tests of the Tasks, the two justification
files, and measure every flag from the repository root with `dotnet test tests\Encina.<Flag>Tests --collect
"XPlat Code Coverage"` and `coverage-report.cs`.

KEY RULES:
- Tests execute real package code; assertions with Shouldly through Encina.Testing.Shouldly.
- No Thread.Sleep: FakeTimeProvider for unit tests; polling with timeouts for integration.
- The PR reports measured per-flag coverage against the manifest targets.

REFERENCE FILES:
- tests/Encina.TestInfrastructure/Fixtures/MongoDbFixture.cs
- tests/Encina.IntegrationTests/Infrastructure/DistributedLock/SqlServerDistributedLockIntegrationTests.cs
- .github/coverage-manifest/Encina.MongoDB.json
```

</details>

### Phase 7: Documentation and Finalization

> **Goal**: The feature is documented and the inventory is true.

<details>
<summary><strong>Tasks</strong></summary>

1. **XML documentation** on every public member (summary, remarks, param, returns, one example on `UseDistributedLock`).
2. **`src/Encina.MongoDB/README.md`** (modify, `docs-writer`) — a section on the lock provider: the flag, the collection, the lease and heartbeat, the NTP assumption, the replica set recommendation, a `Match` example with `await using`.
3. **`changelog.d/609-lock-mongodb.added.md`** (`mechanical-fixer`).
4. **`AGENTS.md` section 5, SPEC-000 and the lock table**: edited only if the maintainer picks option A of Design Choice 1 (the first pull request then amends REQ-027, AC-027 and the table to eight providers); otherwise no edit.
5. **ADR**: none; the decisions are the lock family's ADR-052 and this plan's choices.
6. **Build and test verification**: `dotnet build --configuration Release` with zero warnings; every flag at its manifest target; `check-plan.ps1` for this file.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
You are finishing issue #609: documentation and verification of the MongoDB lock provider.

CONTEXT:
- Phases 1 to 6 are merged; the feature, tests and manifest exist.

TASK:
Complete the XML documentation, ask docs-writer for the README section, ask mechanical-fixer for the changelog
fragment and the PublicAPI lines, and run the full verification.

KEY RULES:
- Documentation never types coverage figures by hand (covref markers, SPEC-001).
- Public text never names a private client application.

REFERENCE FILES:
- changelog.d/README.md
- src/Encina.MongoDB/README.md
```

</details>

---

## Research

### Relevant Standards and Specifications

| Source | Provision | Relevance |
|--------|-----------|-----------|
| MongoDB manual, atomicity of single-document operations and upserts | A single-document update is atomic; an upsert on a unique `_id` that loses a race fails with a duplicate key error | The acquire primitive and the contention signal |
| MongoDB manual, TTL indexes | `expireAfterSeconds: 0` expires documents at the indexed date; the background monitor runs about every 60 seconds | Why the TTL index is housekeeping and the filter compares the date |
| MongoDB manual, write and read concern | `w:majority` and `readConcern: majority` survive a primary failover | Design Choice 6 |
| ADR-052 | Lock providers return `Either`; contention is `Right(None)`; a fault is `Left` | Outcome mapping of every method |
| ADR-062 | Error categories; R4 provider failures are `Internal`; R5 cancellation | Categories of the `Left` cases |
| SPEC-000 REQ-027, DEC-009, AC-027 | Seven lock providers for 1.0; MongoDB is not among them | Design Choice 1 |
| `AGENTS.md` sections 3, 5, 6, 7, 9 | Time, errors, EventIds, coverage, lock table, provider coherence | Constraints used throughout |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|----------------------|
| `IDistributedLockProvider`, `ILockHandle`, `DistributedLockOptions` | `src/Encina.DistributedLock/` | The contract (after #2667) and the base options |
| `EncinaMongoDbOptions`, `MongoDbCollectionNames` | `src/Encina.MongoDB/EncinaMongoDbOptions.cs` | The flag, the lock options and the collection name |
| `MongoDbIndexCreator` | `src/Encina.MongoDB/MongoDbIndexCreator.cs` | The TTL index entry in `FeatureIndexCreators()` |
| `AddEncinaMongoDB` | `src/Encina.MongoDB/ServiceCollectionExtensions.cs` | The registration of the lock services behind the flag |
| `Log` | `src/Encina.MongoDB/Log.cs` | The `[LoggerMessage]` methods in the registered range |
| `MongoDbHealthCheck` | `src/Encina.MongoDB/Health/MongoDbHealthCheck.cs` | Connectivity check, reused |
| `SqlServerDistributedLockProvider` | `src/Encina.DistributedLock.SqlServer/` | Structural model: handle, `GetLockKey` |
| `EventIdRanges.MongoDB` | `src/Encina/Diagnostics/EventIdRanges.cs` | Range 3100-3199, already registered and mapped |
| `MongoDbFixture` and the `MongoDB` collection | `tests/Encina.TestInfrastructure/Fixtures/MongoDbFixture.cs` | Integration tests on the shared container |
| `ForLogging()` | `src/Encina/Diagnostics/` | Exception logging without the message |

### Provider Matrix (distributed locks, SPEC-000 DEC-009)

| Provider | This plan | Notes |
|----------|:-:|-------|
| InMemory | N/A | Single-process testing provider; changed by #2667 |
| Redis | N/A | Quorum mode is [#226](https://github.com/dlrivada/Encina/issues/226) |
| SqlServer | N/A | Structural model |
| PostgreSQL (#207) | N/A | Own plan |
| MySQL (#208) | N/A | Own plan |
| Azure Blob (#209) | N/A | Own plan; the heartbeat design is shared |
| DynamoDB (#210) | N/A | Own plan; the heartbeat and skew design are shared |
| MongoDB (#609) | Included | The subject of this plan; outside the seven (Design Choice 1) |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.MongoDB` (existing range `MongoDB`) | 3190-3198 | Inside 3100-3199, above the last used id 3166; no new range and nothing taken from the family block 7550-7599. Reserved at the top of the range because the plans of #594 and #590 claim "3167+"; they take 3167-3189, 3199 stays spare. The index creation message of PR 1 takes 3190 and the provider messages 3191-3198 |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Production (new) | 7 | options, document, timing helper, store seam, store adapter, provider, diagnostics |
| Production (modified) | 5 | `EncinaMongoDbOptions.cs`, `MongoDbIndexCreator.cs`, `ServiceCollectionExtensions.cs`, `Log.cs`, `Encina.MongoDB.csproj` |
| Metadata | 2 | `PublicAPI.Unshipped.txt`, `Encina.MongoDB.json` manifest (touched in each PR) |
| Unit tests | 6 | options, timing, provider, heartbeat, DI, index creator (extended) (plus the fake store) |
| Guard tests | 2 | provider (new, PR 2) and the existing `ServiceCollectionExtensionsGuardTests.cs` (modified, PR 3) |
| Contract, property and integration tests | 4 | one class each, plus the `MongoDB` collection definition for `Encina.ContractTests` |
| Documentation and changelog | 4 | README section, fragment, two justification files |
| **Total** | **~34 touches** | Over 3 pull requests of at most 15 files each (12, 11 and 13) |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
You are implementing issue #609: a MongoDB lock provider inside the existing Encina.MongoDB package.

PROJECT CONTEXT:
- .NET 10 / C# 14, nullable enabled, pre-1.0: best solution first, no compatibility layers, no [Obsolete].
- Lock contract: ADR-052, implemented by #2667 before this work: TryAcquireAsync returns
  Either<EncinaError, Option<ILockHandle>>, AcquireAsync returns Either<EncinaError, ILockHandle>, IsLockedAsync and
  ExtendAsync return Either<EncinaError, bool>, cancellation propagates, LockErrorCodes holds the Left codes.
- Time only from TimeProvider. EncinaError.Message never reaches logs, tags or health checks.
- The maintainer must answer Design Choice 1 (1.0 scope) before work starts.

IMPLEMENTATION OVERVIEW:
1. Options and storage shape: EncinaMongoDbOptions.UseDistributedLock, MongoDbLockOptions, LockDocument
   ({ _id, OwnerId, AcquiredAtUtc, ExpiresAtUtc }), MongoLockTiming, collection "distributed_locks", a TTL index
   (ExpireAfter = 0) from MongoDbIndexCreator; folder Locking/, namespace Encina.MongoDB.Locking.
2. IMongoLockStore seam and MongoLockStore adapter (majority concerns): acquire is UpdateOne with upsert and
   filter { _id, ExpiresAtUtc <= now - skew }; a duplicate key error is contention. MongoDbDistributedLockProvider:
   retry loop until wait, the handle renews its lease on a TimeProvider timer that never throws and deletes the
   document at its deadline, marks itself lost when a renewal matches nothing; provider ExtendAsync through a
   registry of live handles; IsLockedAsync through Find.
3. Registration in AddEncinaMongoDB behind the flag (TryAdd*, ValidateOnStart), DI test with ValidateOnBuild and
   ValidateScopes; the existing MongoDbHealthCheck covers connectivity.
4. EventIds 3190-3198 in Log.cs inside the registered MongoDB range, diagnostics, ActivitySource and Meter.
5. Tests: unit (fake store, FakeTimeProvider), guard, contract (suite of #2667), property (timing), integration on
   the MongoDB collection; documentation through docs-writer; changelog and PublicAPI through mechanical-fixer.

KEY PATTERNS:
- Contention is Right(None); a MongoDB fault is Left; a lost lease is Right(false) on ExtendAsync.
- Correctness compares ExpiresAtUtc; the TTL index only cleans the collection.
- Three pull requests of at most 15 files; each builds and passes alone.

REFERENCE FILES:
- docs/architecture/adr/052-distributed-lock-failure-contract.md
- src/Encina.MongoDB/EncinaMongoDbOptions.cs
- src/Encina.MongoDB/MongoDbIndexCreator.cs
- src/Encina.MongoDB/ServiceCollectionExtensions.cs
- src/Encina.DistributedLock.SqlServer/SqlServerDistributedLockProvider.cs
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Lock state is ephemeral coordination state; caching it would be a defect |
| 2 | OpenTelemetry | ✅ | Phase 5: spans for acquire and release, acquisitions and renewals counters with `provider` and `outcome` tags; no tenant tag here, the #741 decorator adds `encina.tenant_id` ([ADR-054](../architecture/adr/054-observability-conventions-and-trace-propagation.md) D4); names align with [#724](https://github.com/dlrivada/Encina/issues/724) |
| 3 | Structured Logging | ✅ | Phase 5: `[LoggerMessage]` EventIds 3190-3198 in `Log.cs`, inside the registered `MongoDB` range, exception type only |
| 4 | Health Checks | ✅ | The existing `MongoDbHealthCheck` (`encina-mongodb`) covers the connectivity the issue asks for; no lock-specific check is added |
| 5 | Validation | ✅ | Phase 3: options validated at startup (heartbeat, skew); resource and expiry arguments validated as [#2724](https://github.com/dlrivada/Encina/issues/2724) decides |
| 6 | Resilience | ✅ | Phase 4: the driver's retryable writes and server selection apply; the heartbeat retries on the next tick while the lease is valid; no Polly policy on top |
| 7 | Distributed Locks | ❌ | This is the distributed lock feature itself |
| 8 | Transactions | ❌ | A lock is not a transactional resource; a single-document atomic update needs no multi-document transaction |
| 9 | Idempotency | ❌ | A repeated acquire is a new attempt by design |
| 10 | Multi-Tenancy | ⏭️ | Tenant-scoped keys are composed above the provider by [#741](https://github.com/dlrivada/Encina/issues/741) (existing issue); the key is stored unhashed, so tenant/resource pairs stay injective |
| 11 | Module Isolation | ❌ | Isolation is reached with `KeyPrefix`; the provider holds no module data |
| 12 | Audit Trail | ❌ | Acquire and release are coordination events, not data access or state changes; logs and metrics cover them |

---

## Prerequisites & Dependencies

| Item | State | Effect on this plan |
|------|-------|---------------------|
| Maintainer answer to Design Choice 1 (1.0 scope) | Pending | No pull request starts before it; with option A the first PR also amends SPEC-000 REQ-027 / AC-027 and the `AGENTS.md` lock table |
| [#2667](https://github.com/dlrivada/Encina/issues/2667) — lock providers return `Either` (ADR-052) | Open, implementation issue (no pending maintainer decision) | PR 1 can start before it; PRs 2 and 3 start after it merges |
| [#2724](https://github.com/dlrivada/Encina/issues/2724) — argument validation of lock providers | Open | The guard tests follow its decision for empty resources and non-positive expiry |
| [#741](https://github.com/dlrivada/Encina/issues/741) — tenant-scoped locks | Open (family plan) | Owns tenant key composition; provider unchanged |
| [#2382](https://github.com/dlrivada/Encina/issues/2382) — startup requirements validator | Open | Owns the check that a production host has a real lock provider, so registration order among providers is not solved here |
| [#724](https://github.com/dlrivada/Encina/issues/724) — lock observability | Open | Phase 5 reuses shared instruments if they exist |
| [#218](https://github.com/dlrivada/Encina/issues/218), [#223](https://github.com/dlrivada/Encina/issues/223) — fencing tokens, auto-extend | Open | Out of scope; the heartbeat inside the handle is this provider's own need, not the generic auto-extend |

---

## Next Steps

1. Link this plan from issue #609. Do not brief a worker before the maintainer answers Design Choice 1; if the answer is B, the issue moves out of the 1.0 milestones and the plan waits.
2. When work on the issue starts, ask the maintainer to answer Dilemmas 1 (scope), 2 (placement), 3 (time source) and 4 (heartbeat placement), record the answers in a `## Maintainer Decisions` section placed last, and have the adversarial-reviewer verify the Settled and Trivial choices.
3. One commit per phase; the last commit of PR 3 references `Fixes #609`.
