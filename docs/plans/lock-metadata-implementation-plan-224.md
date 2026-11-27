# Implementation Plan: Lock Metadata (`LockMetadata`, `ILockHandle.Owner/InstanceId/Metadata`, `GetLockInfoAsync`)

> **Issue**: [#224](https://github.com/dlrivada/Encina/issues/224)
> **Type**: Feature
> **Complexity**: High in breadth (a new member on two public interfaces, seven provider implementations, one storage design per backend); low in logic
> **Estimated Scope**: ~800 lines of production code + ~1,500 lines of tests in the core and the three existing providers, plus one pull request per planned provider (about 6,900 lines over 12 pull requests; the per-PR estimates in "Estimated File Count" sum to it)
> **Specification**: [SPEC-000](../specifications/SPEC-000-encina-1.0-baseline-and-release-scope.md) REQ-027, DEC-009, AC-027; [ADR-052](../architecture/adr/052-distributed-lock-failure-contract.md); [ADR-054](../architecture/adr/054-observability-conventions-and-trace-propagation.md) D3, D4, D6.3; [ADR-062](../architecture/adr/062-error-category-contract.md)
> **Depends on**: [#2667](https://github.com/dlrivada/Encina/issues/2667) (ADR-052 implementation; open, not a pending maintainer decision); PRs 6 to 9 need the packages of [#207](https://github.com/dlrivada/Encina/issues/207), [#208](https://github.com/dlrivada/Encina/issues/208), [#209](https://github.com/dlrivada/Encina/issues/209), [#210](https://github.com/dlrivada/Encina/issues/210) (plans on main, not written); [#2405](https://github.com/dlrivada/Encina/issues/2405) (open bug: the SQL Server lock ignores its expiry, and provider-level `ExtendAsync` extends a lock it does not own) for the SQL Server stale-row rule
> **Related**: family `lock-pipeline` (#220, #221, #223, #225); #218 (fencing token: also adds a member to `ILockHandle`), #724, #741, #226, #2653 (duplicate lock providers in the caching packages)

---

## Summary

When a lock is stuck, the first question is "who holds it and doing what". Today a lock is a name and a GUID. This plan adds **metadata** to every lock provider of SPEC-000 DEC-009:

1. **Types.** `LockMetadata` (`Owner`, `InstanceId`, `OperationName`, `CorrelationId`, `CustomProperties`) and `LockInfo` (the lock id, the times and the metadata of a held lock).
2. **Defaults.** `ILockMetadataProvider` supplies defaults at acquisition (owner and instance from the machine and process, the correlation id from `IRequestContextAccessor`); an ambient `ILockMetadataScope` lets the pipeline behavior of #220 add the operation name (Design Choice 2).
3. **Handle.** `ILockHandle` gains `Owner`, `InstanceId` and `Metadata`.
4. **Query.** `IDistributedLockProvider.GetLockInfoAsync(resource, ct)` returns `Either<EncinaError, Option<LockInfo>>`: a fault is `Left`, "not locked" is `Right(None)` (ADR-052 shape, not the issue's `LockInfo?`).
5. **Storage.** Each backend stores the metadata beside the lock in its own way (Provider Matrix); a stale record never reports a lock that is no longer held (Design Choice 4).
6. **Limits and privacy.** Bounded sizes; custom properties are application data and never reach logs, tags or telemetry (Design Choice 5).

**Standards**: ADR-052; ADR-054 D6.3 (no data-bearing names); ADR-062; `AGENTS.md` section 3 (ROP, async database calls with a `CancellationToken`, `EncinaError.Message` never logged), section 5 (all seven lock providers consistent) and section 6.

**Affected packages**: `Encina.DistributedLock` (types, interfaces, defaults), `Encina.DistributedLock.InMemory`, `.Redis`, `.SqlServer`, the four planned packages (#207 to #210), `Encina` (EventId registration only when this plan is the first of the family with a log), and every other implementer of the two interfaces (table below); test projects `Encina.UnitTests`, `Encina.GuardTests`, `Encina.ContractTests`, `Encina.IntegrationTests`, `Encina.TestInfrastructure`.

**Provider category**: Distributed locks (`AGENTS.md` section 5): all seven providers (InMemory, Redis, SQL Server, PostgreSQL, MySQL, Azure Blob, DynamoDB) implement it; the feature is complete for 1.0 only when contract tests cover all seven. Database (10) does not apply to the lock stores (the SQL schemas follow the SQL differences of `AGENTS.md` section 5).

**What the code does today** (`origin/main` at `f9063538e`):

- `ILockHandle` has `Resource`, `LockId`, `AcquiredAtUtc`, `ExpiresAtUtc`, `IsReleased`, `ExtendAsync`; `IDistributedLockProvider` has `TryAcquireAsync`, `AcquireAsync`, `IsLockedAsync`, `ExtendAsync` (`src/Encina.DistributedLock/`); #2667 (open) changes their return types to the ADR-052 `Either` shapes and makes every provider return `ILockHandle`.
- **Every implementer of the members this plan adds** (declaration-form multi-line search of `src/` and `tests/`; no test class, fake or NSubstitute-based double implements either interface):

| Member added to | Implementer | State | Who adds the member, and when |
|---|---|---|---|
| `ILockHandle` | private `LockHandle` in `InMemoryDistributedLockProvider.cs:250` | exists | PR 3 (property on the class first) |
| | private `LockHandle` in `Encina.DistributedLock.Redis/RedisDistributedLockProvider.cs:223` | exists | PR 4 |
| | private `LockHandle` in `SqlServerDistributedLockProvider.cs:242` | exists | PR 5 |
| | the handles of #207, #208, #209, #210 | not written | PRs 6 to 9, each after its package merged |
| | the RedLock handle of #226 (extends the Redis package) | not written | the #226 PR that adds it, or PR 10 after it merged |
| | `InstrumentedLockHandle` (#724), the auto-extend wrapper (#223), the fencing member of #218 lands in the same file | not written | PR 10 |
| | the two caching packages' private `LockHandle` (`MemoryDistributedLockProvider.cs:222`, `Encina.Caching.Redis/RedisDistributedLockProvider.cs:208`) implement `IAsyncDisposable` only | exists | out of this interface until #2653 and #2667 move them to `ILockHandle`; PR 10 includes them if they implement it by then |
| `IDistributedLockProvider` | `InMemoryDistributedLockProvider.cs:18`, `Encina.DistributedLock.Redis/RedisDistributedLockProvider.cs:17`, `SqlServerDistributedLockProvider.cs:19` | exist | PRs 3, 4, 5 (public method on the class first) |
| | `Encina.Caching.Memory/MemoryDistributedLockProvider.cs:21`, `Encina.Caching.Redis/RedisDistributedLockProvider.cs:17` | exist | PR 10, or removed by #2653 |
| | the providers of #207 to #210, the RedLock provider of #226 | not written | PRs 6 to 9; #226's PR |
| | the decorators of #724 (`InstrumentedDistributedLockProvider`), #741 (tenant scoping), #223 (`AutoExtendDistributedLockProvider`) | not written | PR 10 (they forward, #741 maps the scoped resource name back) |

- Constructor sites of the three existing providers (an optional trailing `ILockMetadataProvider? metadataProvider = null` parameter, after the existing optional `TimeProvider? timeProvider = null`, so no site changes and CS1737 is not triggered): `new InMemoryDistributedLockProvider(` 5 sites in 3 files, `new RedisDistributedLockProvider(` 43 sites in 7 files (both Redis packages share the type name; the Encina.Caching.Redis ones are untouched), `new SqlServerDistributedLockProvider(` 14 sites in 5 files. Each changed constructor needs its PublicAPI line (remove the old, add the new) and a guard test that a `null` metadata provider is valid.
- `Encina.DistributedLock` has no `InternalsVisibleTo` for `Encina.GuardTests` (public types only). `tests/Encina.ContractTests` references no `Encina.DistributedLock*` project and has no Redis or SQL Server collection; `tests/Encina.IntegrationTests` has the shared `ADO-SqlServer` collection and no Redis collection.
- `.github/coverage-manifest/Encina.DistributedLock.json`: package targets guard 20, unit 60; the Redis manifest has per-file targets for `RedisDistributedLockProvider.cs` (unit 95, guard 14, integration 70) that this plan never lowers.

---

## Design Choices

<details>
<summary><strong>1. How the interface members are introduced</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Interface last, as #218 plans it: PRs 3 to 9 add the property or method to each class first; PR 10 adds the members to `ILockHandle` and `IDistributedLockProvider` only when every implementer on `main` has them** | Every PR builds and passes alone; no default interface member (a compatibility layer, forbidden by `AGENTS.md` section 3) | PR 10 waits for #207 to #210; two plans (#218 and this one) edit `ILockHandle.cs`, the second to land rebases |
| **B) Interface first, every implementer in one lockstep PR** | One PR, the contract exists at once | The four planned packages do not exist; the PR would exceed 15 files; they cannot be written to a contract they have not seen |
| **C) Separate capability interfaces (`ILockInfoProvider` for the provider, handle members on a second interface)** | No ordering problem, optional per provider | A provider can silently not support it: weaker than the seven-provider consistency rule; callers cast |

### Chosen Option: **A - interface last** (recommended, pending the maintainer)

### Rationale

- A wins for 1.0 consistency and green builds; C wins if #207 to #210 slip so far that the interface is needed earlier. The same sequencing is chosen for #218 (its Design Choice 4), so the maintainer answers both once.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Where acquisition metadata comes from</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ILockMetadataProvider` (singleton) builds the defaults; a public ambient `ILockMetadataScope` (singleton `AsyncLocal`, the pattern of `IRequestContextAccessor`) lets the caller or the #220 behavior add `OperationName` and custom properties; per-call values win over defaults; no signature change on `TryAcquireAsync` or `AcquireAsync`** | Providers can be changed one at a time; the call sites stay as they are | Metadata is ambient, not explicit at the call |
| **B) A new optional parameter `LockMetadata? metadata = null` after `cancellationToken` on both acquire methods** | Explicit at the call | A signature change on the interface forces every implementer in one lockstep PR (an implementer with the extra parameter no longer implements the old method): it contradicts Design Choice 1 option A |

### Chosen Option: **A** (recommended, pending the maintainer)

### Rationale

- A wins while the planned providers are unwritten; B wins if the maintainer prefers explicit parameters and accepts the lockstep PR after #207 to #210 merge. Each provider takes `ILockMetadataProvider?` as an optional trailing constructor parameter and falls back to a built-in default provider, so existing sites compile unchanged. Defaults: `Owner` = `LockMetadataOptions.Owner` or the machine name; `InstanceId` = `LockMetadataOptions.InstanceId` or `{machine}:{process id}`; `CorrelationId` = the ambient request context's, or null.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Redis storage</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A second key `{prefix}:lock:{resource}:meta` holding the metadata JSON, written in the same Lua script as the acquire (`SET NX PX`, then `SET meta PX`) and removed in the release and extend scripts** | Only commands the scripts already use; one atomic acquire; the lock value stays the plain lock id the existing ownership scripts compare | Two keys per lock; on Redis Cluster both keys must share a slot (hash tag), a change of the key format |
| **B) Put an envelope (lock id and metadata) in the lock value and decode it in Lua with `cjson`** | One key | The ownership scripts compare the whole value today; `cjson` availability on the Redis-compatible servers the README lists (Valkey, Dragonfly, Garnet, KeyDB) is not verified here |
| **C) A Redis hash per lock** | Native fields | The acquire needs `SET NX` on a string key anyway; a hash cannot carry `NX` plus a TTL in one command |

### Chosen Option: **A** (recommended, pending the maintainer)

### Rationale

- A wins on portability and atomicity; B wins when a second key per lock is unacceptable and `cjson` is confirmed on every server the package supports. A changes the acquire from `StringSetAsync(When.NotExists)` to a script, so PR 4 re-measures the integration target of `RedisDistributedLockProvider.cs` (70) and keeps it.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. A stale record never reports a lock</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `GetLockInfoAsync` returns `Some` only when the lock is currently held: it probes the lock with the provider's own `IsLockedAsync` (SQL Server, PostgreSQL, MySQL, where the lock lives in a session and the info row can outlive a crashed holder) or reads the info that shares the lock's TTL (Redis, InMemory, DynamoDB item expiry check, Azure Blob lease state); a row of a lock that is gone is removed best effort** | Never reports a ghost holder; ADR-052 shape is respected | One extra probe on the SQL backends |
| **B) Return the stored row as is** | One read | A crashed holder is reported as holding forever |

### Chosen Option: **A**

### Rationale

- Only A answers the issue's own question ("who holds this lock") truthfully; the probe and the row delete run with the caller's `CancellationToken` and asynchronous database calls only (`AGENTS.md` section 3). On SQL Server the lock is an app lock held by a connection that disappears with it (#2405: expiry is ignored, so the row is the only record of an intended expiry; the probe, not the time, decides).

**Class:** Settled
**Settled by:** issue #224 Motivation ("Debugging: Who holds this lock?") with `AGENTS.md` section 3 (database calls asynchronous with a `CancellationToken`)

</details>

<details>
<summary><strong>5. Limits and privacy of the metadata</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Options-bound limits (`MaxCustomProperties` 16, key 64 chars, value 256 chars, total serialized 4 KB) checked before acquiring; a violation is `Left` `lock_metadata.too_large` (`Validation`, ADR-062 R3); the metadata is never copied into logs, metric tags or span attributes except `InstanceId` and `OperationName` (low cardinality, no personal data) on logs and spans; `Owner`, `CorrelationId` and custom properties live only in the backend and in `LockInfo`** | Bounded storage; no application data in telemetry | A caller needing a bigger payload keeps it elsewhere |
| **B) No limits, everything on spans as the issue's matrix suggests ("enrich spans with lock metadata")** | Most debugging detail | Unbounded backend rows; custom properties and correlation ids in telemetry |

### Chosen Option: **A**

### Rationale

- Application text can carry personal data; ADR-054 D6.3 forbids data-bearing names in telemetry and `AGENTS.md` section 3 forbids unredacted values in logs. `GetLockInfoAsync` returns holder details to its caller: authorization of who may call it is the caller's (no HTTP endpoint is added here), documented in the README.

**Class:** Settled
**Settled by:** ADR-054 D6.3 (no data-bearing names) and `AGENTS.md` section 3 (`EncinaError.Message` and unredacted values never reach logs)

</details>

<details>
<summary><strong>6. Storage for backends with no place to put data</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) An opt-in info table for the three session-lock backends (SQL Server `sp_getapplock`, PostgreSQL advisory locks, MySQL `GET_LOCK`), created by an idempotent script when `CreateInfoTable` is true (default false: the lock works without it and `GetLockInfoAsync` returns `Left` `lock_metadata.store_unavailable`); native storage elsewhere (Azure Blob lease blob metadata, DynamoDB item attributes, Redis, InMemory)** | Pay for what you use; no schema change unless asked | Without the table `GetLockInfoAsync` is unavailable on those three backends |
| **B) Always create the table** | The feature always works | Schema creation at first lock for every user, including those who never query |

### Chosen Option: **A** (recommended, pending the maintainer)

### Rationale

- A wins for opt-in (`AGENTS.md` section 1: pay-for-what-you-use); B wins if the maintainer wants the query to work out of the box. The unavailable case is a `Left` with category `Internal` (R1: a configuration gap), because ADR-062 R2 forbids `FailedPrecondition` before #3420.

**Class:** Dilemma

</details>

---

## Implementation Phases

### Phase 1: Types, Limits and Options (PR 1)

> **Goal**: The data model, testable alone.

<details>
<summary><strong>Tasks</strong></summary>

1. **`Metadata/LockMetadata.cs`** and **`LockInfo.cs`** (new, public sealed records, `src/Encina.DistributedLock/`) - properties of the summary; `LockInfo(Resource, LockId, Owner, InstanceId, AcquiredAtUtc, ExpiresAtUtc, Metadata)` (UTC `AtUtc` names per `AGENTS.md` section 4).
2. **`Metadata/LockMetadataOptions.cs`**, **`LockMetadataOptionsValidator.cs`** (new) - `Owner`, `InstanceId`, the limits of Design Choice 5.
3. **`Metadata/LockMetadataRules.cs`** (new, internal static) - validates and merges (per-call over defaults), returns `Either<EncinaError, LockMetadata>`; **`LockMetadataErrorCodes.cs`** (new, public static): `lock_metadata.too_large` (`Validation`), `lock_metadata.store_unavailable` (`Internal`), `lock_metadata.backend_failure` (`Internal`, R4).
4. **Tests** - limits at the boundary, merge precedence, serialized size, options validator, guard test of the validator; PublicAPI lines and manifest (`mechanical-fixer`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of issue #224: the lock metadata types, limits and options.

CONTEXT:
- Encina.DistributedLock has no InternalsVisibleTo for Encina.GuardTests: guard-test public types only.
- Errors are EncinaErrors.Create(code, ErrorCategory, message); categories are fixed in the plan (ADR-062).

TASK:
Create LockMetadata, LockInfo, LockMetadataOptions and its validator, LockMetadataRules and LockMetadataErrorCodes with tests.

KEY RULES:
- UTC timestamps end in AtUtc; XML docs on every public member; no [Obsolete].
- Never put a metadata value in an error message.

REFERENCE FILES:
- src/Encina.DistributedLock/ILockHandle.cs, docs/architecture/adr/062-error-category-contract.md
```

</details>

### Phase 2: Defaults and Ambient Scope (PR 2)

> **Goal**: The sources of metadata and their registration.

<details>
<summary><strong>Tasks</strong></summary>

1. **`Metadata/ILockMetadataProvider.cs`** (new) - `LockMetadata CreateDefaults()`; **`DefaultLockMetadataProvider.cs`** (new, internal; reads `IRequestContextAccessor`, a singleton); **`ILockMetadataScope.cs`** and **`LockMetadataScope.cs`** (new; `IDisposable Begin(LockMetadata overrides)`, `AsyncLocal`).
2. **`ServiceCollectionExtensions.cs`** (modify) - `TryAdd` of the options (validated), provider and scope in `AddEncinaDistributedLock(...)` and a new `AddEncinaDistributedLockMetadata(Action<LockMetadataOptions>?)`; the three providers resolve the provider through their optional constructor parameter, so registration completeness is proven by a DI test with `ValidateOnBuild` and `ValidateScopes`.
3. **Tests** - defaults, scope nesting and disposal across `await`, DI test, guard tests. README section, changelog fragment `changelog.d/224-lock-metadata.added.md` (specialists).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of issue #224: ILockMetadataProvider, the default provider and the ambient scope.

CONTEXT:
- IRequestContextAccessor is a singleton AsyncLocal accessor: use it, never a scoped service in a singleton.

TASK:
Create the provider interface, the default, the scope, the registration and the tests.

KEY RULES:
- Restore the ambient value when the scope is disposed; never log metadata values.

REFERENCE FILES:
- src/Encina/Abstractions/IRequestContextAccessor.cs, src/Encina.DistributedLock/ServiceCollectionExtensions.cs
```

</details>

### Phase 3: InMemory, Redis and SQL Server (PRs 3, 4, 5)

> **Goal**: The three existing providers store and report metadata.

<details>
<summary><strong>Tasks</strong></summary>

1. **PR 3, `InMemoryDistributedLockProvider.cs`** - metadata in `LockEntry`; the private handle gets `Owner`, `InstanceId`, `Metadata`; public `GetLockInfoAsync`; optional constructor parameter; expired entries are not reported (cleanup as for `IsLockedAsync`).
2. **PR 4, `RedisDistributedLockProvider.cs`** (Encina.DistributedLock.Redis) - Design Choice 3 option A: acquire script with the meta key, release and extend scripts remove or refresh it; `GetLockInfoAsync` reads both keys in one script; Redis faults are `Left` (`lock_metadata.backend_failure`).
3. **PR 5, `SqlServerDistributedLockProvider.cs`** + **`SqlServerLockInfoSchema.cs`** (new) + `SqlServerLockOptions.cs` (`CreateInfoTable`, table name; connection strings keep their `[JsonIgnore]`/`ToString()` rule) - parameterized `@param` SQL, `OpenAsync`/`ExecuteNonQueryAsync` with the token, the probe of Design Choice 4, the unavailable case of Design Choice 6.
4. **Tests per PR** - unit (the Redis one mocks `IDatabase` as the existing tests do), guard (null metadata provider is valid), integration in the shared fixtures (`ADO-SqlServer`; Redis through the shared fixture added by #220 PR 5, or by PR 4 when first), manifest rows that keep every existing target, PublicAPI lines of the changed constructors and the new methods.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are implementing Phase 3 of issue #224: metadata in the InMemory, Redis and SQL Server lock providers (one pull request each).

CONTEXT:
- Interface members are NOT added yet (Design Choice 1 option A): add public members on the provider and the private handle classes.
- SQL Server ignores lock expiry until #2405: the probe, not the time, decides whether a row is live.

TASK:
Implement the provider changes of the tasks and their tests; one PR per provider.

KEY RULES:
- Database calls asynchronous with the CancellationToken; parameterized SQL; never log metadata values or EncinaError.Message.
- Do not lower any manifest target; the new constructor parameter is optional and last.

REFERENCE FILES:
- src/Encina.DistributedLock.InMemory/, src/Encina.DistributedLock.Redis/, src/Encina.DistributedLock.SqlServer/
```

</details>

### Phase 4: The Four Planned Providers (PRs 6 to 9)

> **Goal**: Seven providers, one contract.

<details>
<summary><strong>Tasks</strong></summary>

1. One PR per package after it merged: PostgreSQL #207 (info table, `LIMIT`, lowercase identifiers, advisory-lock probe), MySQL #208 (info table, backtick identifiers, `GET_LOCK` probe), Azure Blob #209 (lease blob metadata, size limit of blob metadata checked against Design Choice 5), DynamoDB #210 (attributes on the lock item, TTL attribute). Each adds the handle properties, `GetLockInfoAsync`, tests, manifest rows and PublicAPI lines; the packages' own plans do not carry this work.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
You are implementing Phase 4 of issue #224: metadata in one planned lock provider.

CONTEXT:
- The package of #207, #208, #209 or #210 has merged; read its handle and provider before changing them.

TASK:
Add the handle properties, GetLockInfoAsync, the storage of Design Choices 4 and 6, the tests and the contract subclass.

KEY RULES:
- SQL differences of AGENTS.md section 5; asynchronous database calls with the token; limits of Design Choice 5.

REFERENCE FILES:
- docs/plans/lock-postgresql-implementation-plan-207.md (and 208, 209, 210), the merged package
```

</details>

### Phase 5: Interface Completion (PR 10)

> **Goal**: The members become part of the contract.

<details>
<summary><strong>Tasks</strong></summary>

1. **`ILockHandle.cs`**, **`IDistributedLockProvider.cs`** (modify) - add `Owner`, `InstanceId`, `Metadata` and `GetLockInfoAsync` (`Task<Either<EncinaError, Option<LockInfo>>>`); core PublicAPI lines.
2. Every remaining implementer of the table: `InstrumentedLockHandle` and `InstrumentedDistributedLockProvider` (#724), the tenant decorator (#741: forwards the scoped name and maps `LockInfo.Resource` back to the caller's name), `AutoExtendingLockHandle` and `AutoExtendDistributedLockProvider` (#223), the RedLock handle and provider (#226), the two caching providers if they implement `ILockHandle` by then. The PR is split in two (core plus in-repo implementers; decorators) when the count would pass 15 files.
3. **Tests** - the contract suite of Phase 7; build verification over the whole solution.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
You are implementing Phase 5 of issue #224: adding the members to ILockHandle and IDistributedLockProvider.

CONTEXT:
- Every implementer already has the members as class members; the plan's implementer table lists them. A compile of Encina.slnx must be green.

TASK:
Add the interface members, the PublicAPI lines and the forwarding in every decorator and wrapper.

KEY RULES:
- No default interface member; grep every implementer (declaration form, multi-line) before and after.

REFERENCE FILES:
- docs/plans/lock-fencing-token-implementation-plan-218.md (the same sequencing), the plan's implementer table
```

</details>

### Phase 6: Observability (PR 11)

> **Goal**: Metadata is visible without leaking application data.

<details>
<summary><strong>Tasks</strong></summary>

1. **`Diagnostics/LockMetadataLog.cs`** (new, `internal static partial`) - 30614 lock info query failed (Warning: error code), 30615 metadata provider threw so defaults were used (Warning: exception type via `ForLogging()`), 30616 metadata rejected for size (Warning: counts only). Ids 30614-30616 of the family range `DistributedLockPipeline` (30600-30649). The acquire Debug lines of the providers add `InstanceId` and `OperationName` only.
2. **Instruments** - counter `encina.distributed_lock.info_queries` (tag `encina.lock.outcome`: `held`, `not_held`, `failed`) on the shared `DistributedLockDiagnostics` meter; no `_total`; no resource name; tenant tag only with multi-tenancy (#2676). The span attribute `encina.lock.operation` is added to the spans of #724.
3. **EventId registration** only if this PR is the first of the family with a log (`EventIdRanges.cs` field `DistributedLockPipeline = (30600, 30649)`, core `PublicAPI.Unshipped.txt`, one registry row with the same bounds, `AssemblyRanges` entry; owned by #220 PR 4 by default).
4. **Tests** - `MeterListener` and log tests, load and benchmark justification `.md` files.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
You are implementing Phase 6 of issue #224: telemetry of lock metadata.

CONTEXT:
- Ids 30614-30616 belong to the family range DistributedLockPipeline, registered by #220 PR 4 or by the first family PR with a log.

TASK:
Write LockMetadataLog and the info-query counter, emit them, and add the tests.

KEY RULES:
- Never log or tag Owner, CorrelationId, custom properties, resource names or EncinaError.Message.

REFERENCE FILES:
- docs/engineering/eventid-block-registry.md, docs/plans/distributed-lock-opentelemetry-implementation-plan-724.md
```

</details>

### Phase 7: Contract Scenarios (PR 12)

> **Goal**: One behavior on every provider that exists.

<details>
<summary><strong>Tasks</strong></summary>

1. **`tests/Encina.ContractTests/DistributedLock/LockMetadataContractTests.cs`** (new, abstract base plus an InMemory subclass) and **`Encina.ContractTests.csproj`** (modify: two `ProjectReference` lines unless #220 PR 5 added them; the project has none today and no Redis or SQL Server fixture): held lock reports owner and instance, released lock reports `None`, a crashed holder reports `None`, limits rejected. Redis and SQL Server subclasses live in `Encina.IntegrationTests` (they own the shared fixtures); each planned provider adds one subclass in its own PR.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
You are implementing Phase 7 of issue #224: contract scenarios of lock metadata.

CONTEXT:
- Contract tests instantiate real implementations; Redis and SQL Server need Docker fixtures that only IntegrationTests has.

TASK:
Write the abstract contract base with the InMemory subclass and the integration subclasses.

KEY RULES:
- Shared [Collection] fixtures only; FakeTimeProvider only on InMemory.

REFERENCE FILES:
- tests/Encina.IntegrationTests/Infrastructure/DistributedLock/
```

</details>

---

## Research

### Relevant Standards and Specifications

| Standard | Relevance |
|----------|-----------|
| SPEC-000 REQ-027, DEC-009, AC-027 | Seven lock providers, contract over all |
| ADR-052, ADR-054 D6.3, ADR-062 | Either shape, no data-bearing names, categories |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage |
|-----------|----------|-------|
| `IRequestContextAccessor` | `src/Encina/Abstractions/IRequestContextAccessor.cs` | Correlation id default and scope pattern |
| Lock providers and handles | `src/Encina.DistributedLock*` | Where metadata is stored |
| `DistributedLockDiagnostics`, decorators | #724, #741 | Meter, forwarding |

### Provider Matrix (distributed locks, SPEC-000 DEC-009)

| Provider | Where metadata lives | Status | Proven by |
|----------|----------------------|--------|-----------|
| InMemory | `LockEntry` | exists | PR 3 unit, PR 12 contract |
| Redis | second key with the lock's TTL, written in the acquire script | exists | PR 4 integration |
| SQL Server | opt-in info table, probed with `sp_getapplock` (expiry ignored until #2405) | exists | PR 5 integration |
| PostgreSQL | opt-in info table, advisory-lock probe | planned (#207) | PR 6 |
| MySQL | opt-in info table, `GET_LOCK` probe | planned (#208) | PR 7 |
| Azure Blob | lease blob metadata (Azurite) | planned (#209) | PR 8 |
| DynamoDB | item attributes (LocalStack) | planned (#210) | PR 9 |

### Event ID Allocation

| Package | Range | Ids |
|---------|-------|-----|
| `Encina.DistributedLock` (`DistributedLockPipeline`, registered by #220 PR 4; first free 50-slot after 30599, no claim on main, in an open plan head or in the registry, checked 2026-11-27) | `30600-30649` | #224 uses 30614-30616; one registry row with identical bounds; no id below 10000 |

### Estimated File Count

| PR | Content | New | Modified | Tests, manifests, PublicAPI, docs | Total | Lines (est.) |
|----|---------|-----|----------|------------------------------------|-------|--------------|
| 1 | Types, limits, options | 6 | 1 (PublicAPI) | 4 (rules tests, options tests, validator guard test, manifest) | 11 | 900 |
| 2 | Defaults, scope, registration | 4 | 3 (`ServiceCollectionExtensions.cs`, PublicAPI, README) | 5 (provider tests, scope tests, DI test, manifest, fragment) | 12 | 800 |
| 3 | InMemory | 0 | 2 (provider, PublicAPI) | 4 (unit, guard, manifest, constructor sites of the unit test) | 6 | 350 |
| 4 | Redis | 0 | 2 (provider, PublicAPI) | 5 (unit, guard, integration, manifest, Redis fixture 2 if first) | 7 (9) | 500 |
| 5 | SQL Server | 1 (`SqlServerLockInfoSchema.cs`) | 3 (provider, options, PublicAPI) | 4 (unit, guard, integration, manifest) | 8 | 600 |
| 6 to 9 | One PR per planned provider | 1 to 2 | 2 to 3 | 4 | about 8 each | about 500 each |
| 10 | Interface completion | 0 | 3 core (2 interfaces, PublicAPI) + up to 8 implementers | 2 | up to 13 (split in two if over 15) | 600 |
| 11 | Log, instruments (+4 if first of the family) | 1 | 2 | 5 (log tests, metrics tests, manifest, 2 `.md`) | 8 (12) | 500 |
| 12 | Contract | 2 (contract base, InMemory subclass) | 1 (ContractTests csproj) | 0 | 3 | 350 |

Total about 6,900 lines over 12 PRs (the per-provider PRs 6 to 9 are 2,000 of it). Every PR is at most 15 files and 1,500 lines and builds and passes alone (PR 10 waits for #207 to #210).

### Per-file coverage targets (numeric, per flag)

| File | Unit | Guard | Contract | Integration | Justification |
|------|------|-------|----------|-------------|---------------|
| `LockMetadataRules.cs`, `DefaultLockMetadataProvider.cs`, `LockMetadataScope.cs` | 95 | 0 | 0 | 0 | Pure logic; internal or ambient types with no `InternalsVisibleTo` for `Encina.GuardTests`, so guard tests cannot reach them |
| `LockMetadataOptionsValidator.cs`, `LockMetadataOptions.cs` | 95 | 40 | 0 | 0 | Pure validation |
| `InMemoryDistributedLockProvider.cs` (changed part) | 95 | 20 | 60 | 0 | Every branch on a fake clock; guard covers the new optional parameter |
| `RedisDistributedLockProvider.cs` | 95 | 14 | 0 | 70 | Existing targets kept (manifest values unit 95, guard 14, integration 70); the script and info read run against a real server |
| `SqlServerDistributedLockProvider.cs`, `SqlServerLockInfoSchema.cs` | 90 | 15 | 0 | 60 | SQL text and probe covered by unit with a mocked connection factory where the code allows and by the shared `ADO-SqlServer` fixture |
| `LockMetadata.cs`, `LockInfo.cs`, `LockMetadataErrorCodes.cs`, `ILockMetadataProvider.cs`, `ILockMetadataScope.cs`, `LockMetadataLog.cs` | 0 | 0 | 0 | 0 | Records, constants, interfaces and source-generated log methods: no executable logic; covered through the providers |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Combined prompt — all phases</strong></summary>

```
PROJECT CONTEXT:
Encina is pre-1.0 (.NET 10, C# 14, nullable enabled). Rules are in AGENTS.md. Issue #224 adds lock metadata and GetLockInfoAsync to all
seven lock providers; the interface members land last (PR 10) after every implementer has them.

IMPLEMENTATION OVERVIEW:
1. Phase 1: LockMetadata, LockInfo, limits, options. Phase 2: ILockMetadataProvider, default, ambient scope, registration.
2. Phase 3: InMemory, Redis, SQL Server (one PR each, members on the classes). Phase 4: the four planned providers.
3. Phase 5: ILockHandle and IDistributedLockProvider members plus every decorator and wrapper. Phase 6: log 30614-30616. Phase 7: contract.

KEY PATTERNS:
- ADR-052: GetLockInfoAsync returns Either<EncinaError, Option<LockInfo>>; a stale row never reports a held lock.
- Never log or tag Owner, CorrelationId, custom properties, resource names or EncinaError.Message.
- Dilemmas 1, 2, 3 and 6 are pending the maintainer: implement the recommended options only after he answers.
- SQL Server ignores lock expiry until #2405: state it, never claim expiry-based info there.

REFERENCE FILES:
- docs/plans/lock-metadata-implementation-plan-224.md, docs/plans/lock-fencing-token-implementation-plan-218.md, src/Encina.DistributedLock*/
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ N/A | Metadata is stored with the lock, not cached separately |
| 2 | OpenTelemetry | ✅ Include | Phase 6: info-query counter; span attribute `encina.lock.operation` only; no owner, correlation id or custom properties |
| 3 | Structured Logging | ✅ Include | Phase 6: `LockMetadataLog` 30614-30616; acquire lines add `InstanceId` and `OperationName` only |
| 4 | Health Checks | ❌ N/A | Metadata is informational, not a service dependency |
| 5 | Validation | ✅ Include | Size and count limits (Design Choice 5); invalid metadata is `Left` with category `Validation` |
| 6 | Resilience | ❌ N/A | Metadata storage is co-located with the lock (same call or same backend) |
| 7 | Distributed Locks | ❌ N/A | This is a lock feature, not a consumer |
| 8 | Transactions | ❌ N/A | Locks are separate from transactions |
| 9 | Idempotency | ❌ N/A | A repeated query returns the same facts |
| 10 | Multi-Tenancy | ⏭️ Defer | The decorator of #741 scopes the resource name and maps it back in `GetLockInfoAsync` (PR 10); missing tenant fails closed there |
| 11 | Module Isolation | ❌ N/A | Modules are separated with `KeyPrefix` (the position of the #741 plan) |
| 12 | Audit Trail | ❌ N/A | The metadata holds machine, process and operation names, not personal data by contract; custom properties are the application's and must not carry personal data (README) |

---

## Prerequisites & Dependencies

| Item | State | Needed for |
|------|-------|------------|
| #2667 ADR-052 implementation | open | All PRs |
| #207, #208, #209, #210 packages | plans on main, not written | PRs 6 to 9, then PR 10 |
| #218 (`FencingToken` on `ILockHandle`), #724, #741, #223, #226 | plans on main | PR 10 (decorators and wrappers forward the members) |
| #2653 duplicate caching lock providers | open | PR 10 (their handles must be `ILockHandle`) |
| #2405 SQL Server expiry | open bug | Any SQL Server expiry statement |
| Family EventId range | registered by #220 PR 4 or the first family PR with a log | PR 11 |

---

## Next Steps

1. After #2667 merges open PR 1 and PR 2, then PRs 3, 4, 5; PRs 6 to 9 as each package merges; PR 10 after them; PR 11 and PR 12 after PR 5.
2. The maintainer answers Dilemmas 1, 2, 3 and 6 when work on the issue starts; PRs are written for the recommended answers.
