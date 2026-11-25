# Implementation Plan: Retry-After on `AllReplicasUnhealthy` (the time until the replica health tracker re-offers a replica)

> **Issue**: [#4345](https://github.com/dlrivada/Encina/issues/4345)
> **Type**: Feature
> **Complexity**: Low (4 phases, 4 pull requests of 10, 9, 9 and 3 files)
> **Estimated Scope**: ~140 lines of production code + ~650 lines of tests
> **Family**: sharding read/write separation, error categories epic (#3419, #3424). The category of `AllReplicasUnhealthy` is already decided (maintainer, 2026-11-25, #3728 D11 = B, `Unavailable`); this plan adds only the retry hint.

---

## Summary

`ShardingErrorCodes.AllReplicasUnhealthy` (`src/Encina/Sharding/ShardingErrorCodes.cs:120`) answers HTTP 503 (`ErrorCategory.Unavailable`, #3728 D11 = B, applied by the slices #3728, #3729 and #3726) with no `Retry-After` header. `ErrorCategoryHttp.GetResponseHeaders` (`src/Encina/Errors/ErrorCategoryHttp.cs`) writes the header for 429 and 503 when the error details hold a non-negative `TimeSpan` under `EncinaErrorMetadataKeys.RetryAfter` (`src/Encina/Errors/EncinaErrorMetadataKeys.cs:22`); ADR-062 forbids guessing it. The time is known only to the tracker: `ReplicaHealthTracker.GetAvailableReplicas` (`src/Encina/Sharding/ReplicaSelection/ReplicaHealthTracker.cs`) re-offers a replica when `now - LastFailure >= recoveryDelay`, and `_recoveryDelay` is private to the tracker.

State of `origin/main` (`4b0a600c7`), verified by reading:

| Fact | Where |
|------|-------|
| 7 call sites build the error: 3 ADO (`src/Encina.ADO.{SqlServer,PostgreSQL,MySQL}/Sharding/ShardedReadWriteConnectionFactory.cs:242`), 3 Dapper (same file name, line 165), 1 EF Core (`src/Encina.EntityFrameworkCore/Sharding/ShardedReadWriteDbContextFactory.cs:179`); each in a private `SelectReplicaConnectionString(ShardInfo)` | `grep AllReplicasUnhealthy src` |
| The error is created only when `FallbackToPrimaryWhenNoReplicas` is false (default true, `src/Encina/Sharding/ShardedReadWriteOptions.cs:75`) and `GetAvailableReplicas` returns no replica | the 7 factories |
| `IReplicaHealthTracker` has one implementer in `src/` and none in `tests/` (`ReplicaHealthTracker`; the other users are NSubstitute substitutes) | `grep IReplicaHealthTracker` |
| `ReplicaHealthState` exposes `LastFailure` but the recovery delay lives only in the tracker constructor (`ReplicaHealthTracker.cs`, default 30 s) | `ReplicaHealthState.cs`, `ReplicaHealthTracker.cs` |
| `EncinaErrors.Create(code, category, message, exception, details)` accepts the details dictionary | `src/Encina/Results/EncinaErrors.cs` |
| `SelectReplicaConnectionString` has cyclomatic complexity 10 (the 2026-11-25 register of #3728); one more branch would make `CRAP = comp` exceed 10 even at 100 percent coverage (`AGENTS.md` section 9) | `artifacts/decisions/3728.md` |

**Delivered**: a tracker member that returns the minimum time until any replica of a shard is re-offered, one core helper that builds the `AllReplicasUnhealthy` error (category, message and `Retry-After` key in one place), and the 7 factories calling it.

**Not in scope**: marking replicas unhealthy when `OpenAsync` fails (#4344, a separate defect: today only the application feeds the tracker); the category itself (#3728, #3729, #3726); MongoDB (it has no `AllReplicasUnhealthy` call site).

**Affected packages**: `Encina`, `Encina.ADO.SqlServer`, `Encina.ADO.PostgreSQL`, `Encina.ADO.MySQL`, `Encina.Dapper.SqlServer`, `Encina.Dapper.PostgreSQL`, `Encina.Dapper.MySQL`, `Encina.EntityFrameworkCore`.

**Provider category**: database (10). The tracker is shared; the provider-specific part is the one-line call in each factory. The three EF Core providers share one factory (`ShardedReadWriteDbContextFactory<TContext>`), MongoDB has no call site.

---

## Design Choices

No open design choices. Every choice is Trivial or Settled by a rule or an earlier maintainer decision; the `Verified` column is filled by `adversarial-reviewer` when work starts (`pending` until then).

| Id | Choice | Class | Settled by | Verified |
|----|--------|-------|------------|----------|
| 1 | The tracker computes the remaining time: a new member `TimeSpan? GetTimeUntilReplicaAvailable(string shardId, IReadOnlyList<string> allReplicas)` on `IReplicaHealthTracker`, implemented by `ReplicaHealthTracker` from `LastFailure + _recoveryDelay - now` with its `TimeProvider`; the minimum over the replicas that are unhealthy and not yet recovered. Computing it in the factories would need the delay exposed and would copy the arithmetic 7 times | Settled | `AGENTS.md` section 1 (best solution first, never a compromise for compatibility; the interface has one implementer) and section 5 (abstractions in `Encina`, providers differ only in implementation); `AGENTS.md` section 3 (time from `TimeProvider`) | pending |
| 2 | The result is `null` when no replica of the list is waiting for recovery (healthy, or already past the delay, or never failed); the helper then omits the key | Settled | ADR-062 "Header rules" (`Retry-After` only when the key exists, never guessed) and `src/Encina/Errors/ErrorCategoryHttp.cs` | pending |
| 3 | A positive remaining time is returned as is; the value is clamped to `TimeSpan.Zero` when the clock passed the instant between the availability check and the computation; the header writer rounds up | Settled | RFC 9110 section 10.2.3 (delay-seconds is a non-negative integer) and `ErrorCategoryHttp.GetResponseHeaders` (accepts a non-negative `TimeSpan`) | pending |
| 4 | One core helper builds the error: `ShardingErrors.AllReplicasUnhealthy(string shardId, IReplicaHealthTracker tracker, IReadOnlyList<string> replicas)` in `Encina.Sharding`, returning an `EncinaError` with `ErrorCategory.Unavailable` (D11 = B), the existing message (shard id only) and the details. The factory expression `EncinaErrors.Create(code, category, message)` becomes one call, so `SelectReplicaConnectionString` gains no branch (complexity stays 10) and the category lives in one place instead of 7 | Settled | `AGENTS.md` section 9 (CRAP <= 10 on changed methods) and section 5 (provider coherence: no provider-specific code in abstractions, one behavior for all) | pending |
| 5 | The category stays `Unavailable` (not re-decided here) | Settled | Maintainer decision of 2026-11-25 on #3728 (D11 = B), repeated on #3729 and #3726 | pending |
| 6 | The message keeps only the shard id; the remaining time goes in the details, never in the message, logs or tags | Settled | `AGENTS.md` section 3 (`EncinaError.Message` never reaches logs; no connection string in a message) | pending |
| 7 | The change is added to `IReplicaHealthTracker` in place, with no compatibility layer or default interface member | Trivial | `AGENTS.md` sections 1 and 3 (pre-1.0, no compatibility layers) | pending |
| 8 | Four pull requests: core (tracker, helper), ADO (3), Dapper (3), EF Core (1); each at most 15 files and 1500 lines, and the three provider pull requests are one declared identical sweep each | Trivial | `AGENTS.md` section 11 (a worker's issue is one pull request, one package or provider or one declared sweep) | pending |

---

## Implementation Phases

### Phase 1: Tracker member and error helper (pull request 1, `Encina`)

> **Goal**: The tracker answers "how long until a replica returns" and one helper builds the error.

<details>
<summary>Tasks</summary>

1. `src/Encina/Sharding/ReplicaSelection/IReplicaHealthTracker.cs`: add `TimeSpan? GetTimeUntilReplicaAvailable(string shardId, IReadOnlyList<string> allReplicas);` with XML docs: the minimum remaining time until a replica of `allReplicas` that is currently unhealthy is re-offered; `null` when none is waiting; never negative.
2. `src/Encina/Sharding/ReplicaSelection/ReplicaHealthTracker.cs`: implement it next to `GetAvailableReplicas`: argument checks like the siblings (`ArgumentNullException.ThrowIfNull`), `now` from `_timeProvider`, loop over `allReplicas`, skip healthy replicas and replicas with no recorded failure, `remaining = state.LastFailure.Value + _recoveryDelay - now`, keep the minimum, clamp at `TimeSpan.Zero`. Keep complexity at 8 or less.
3. `src/Encina/Sharding/ShardingErrors.cs` (new, `public static class ShardingErrors`, namespace `Encina.Sharding`): `public static EncinaError AllReplicasUnhealthy(string shardId, IReplicaHealthTracker tracker, IReadOnlyList<string> replicas)` returning `EncinaErrors.Create(ShardingErrorCodes.AllReplicasUnhealthy, ErrorCategory.Unavailable, $"All replicas for shard '{shardId}' are currently unhealthy.", details: ...)` where the details hold `EncinaErrorMetadataKeys.RetryAfter` only when the tracker returns a value. XML docs with an example; null checks.
4. `src/Encina/PublicAPI.Unshipped.txt`: the two new symbols (`IReplicaHealthTracker.GetTimeUntilReplicaAvailable`, `ShardingErrors.AllReplicasUnhealthy`, plus the `ShardingErrors` type).
5. `.github/coverage-manifest/Encina.json`: per-file targets with one-sentence justifications for `ReplicaHealthTracker.cs`, `IReplicaHealthTracker.cs` and `ShardingErrors.cs` (measure the baseline of `ReplicaHealthTracker.cs` first with `dotnet test tests\Encina.UnitTests --collect "XPlat Code Coverage"` and `coverage-report.cs`; a target is the measured value or higher).
6. Tests: unit (`tests/Encina.UnitTests/Core/Sharding/ReplicaSelection/ReplicaHealthTrackerTests.cs`, extend: no failures gives `null`, one failure gives the delay, two replicas give the minimum, recovered replica ignored, clamped at zero, `FakeTimeProvider` advanced), unit for `ShardingErrors` (category `Unavailable`, key present as a `TimeSpan` or absent, message holds only the shard id), guard (null `shardId`, `allReplicas`, `tracker`), contract (the real `ReplicaHealthTracker` honors the member contract), property (the value is never negative and never grows as the clock advances).
7. `changelog.d/4345-replica-retry-after.added.md` through `mechanical-fixer`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT:
- Issue #4345; plan docs/plans/replica-retry-after-implementation-plan-4345.md. The category of AllReplicasUnhealthy is Unavailable (maintainer, #3728 D11 = B).
- ReplicaHealthTracker keeps _recoveryDelay and _timeProvider privately; GetAvailableReplicas is the model.

TASK:
Add IReplicaHealthTracker.GetTimeUntilReplicaAvailable, its implementation, ShardingErrors.AllReplicasUnhealthy, the PublicAPI lines, the manifest targets and every test listed.

KEY RULES:
- Time only from the tracker's TimeProvider; never DateTime.UtcNow.
- Retry-After only when a time is known; never guess (ADR-062); the details value is a non-negative TimeSpan under EncinaErrorMetadataKeys.RetryAfter.
- The message holds the shard id only; no connection string.
- No [Obsolete], no default interface member; zero warnings; CRAP <= 10 per changed method; XML docs with an example.

REFERENCE FILES:
- src/Encina/Sharding/ReplicaSelection/ReplicaHealthTracker.cs
- src/Encina/Errors/ErrorCategoryHttp.cs
- src/Encina/Errors/EncinaErrorMetadataKeys.cs
- tests/Encina.UnitTests/Core/Sharding/ReplicaSelection/ReplicaHealthTrackerTests.cs
```

</details>

---

### Phase 2: The seven factories (pull requests 2, 3 and 4)

> **Goal**: Every `AllReplicasUnhealthy` call site uses the helper.

<details>
<summary>Tasks</summary>

1. Pull request 2 (ADO): in `src/Encina.ADO.{SqlServer,PostgreSQL,MySQL}/Sharding/ShardedReadWriteConnectionFactory.cs` replace the `EncinaErrors.Create(ShardingErrorCodes.AllReplicasUnhealthy, ErrorCategory.Unavailable, ...)` expression in `SelectReplicaConnectionString` with `ShardingErrors.AllReplicasUnhealthy(shard.ShardId, _healthTracker, shard.ReplicaConnectionStrings)`.
2. Pull request 3 (Dapper): the same in `src/Encina.Dapper.{SqlServer,PostgreSQL,MySQL}/Sharding/ShardedReadWriteConnectionFactory.cs`.
3. Pull request 4 (EF Core): the same in `src/Encina.EntityFrameworkCore/Sharding/ShardedReadWriteDbContextFactory.cs` (one factory for the three EF Core providers).
4. Tests per pull request: a unit test per factory that builds it with a substitute tracker that reports no available replica and a known remaining time, `FallbackToPrimaryWhenNoReplicas = false`, calls `GetReadConnectionAsync` (ADO, Dapper; the `Left` is returned before any connection is created) or `CreateReadContext` (EF Core, extend `tests/Encina.UnitTests/EntityFrameworkCore/Sharding/ShardedReadWriteDbContextFactoryTests.cs`), and asserts the `Left` carries `ErrorCategory.Unavailable` and `EncinaErrorMetadataKeys.RetryAfter` equal to the substitute's time; plus a case with the tracker returning `null` (no key). New files `tests/Encina.UnitTests/{ADO,Dapper}/{SqlServer,PostgreSQL,MySQL}/Sharding/ShardedReadWriteConnectionFactoryTests.cs`.
5. Per-file targets for each touched factory in its package manifest (each factory today has `defaultTests: guard` with `integration` override; the new unit test file raises the unit flag, and the measured baseline of the touched method is the starting point; the method needs 100 percent line coverage to stay CRAP <= 10 at complexity 10).
6. Changelog: the fragment of pull request 1 already covers the feature; each provider pull request adds a one-line `changed` fragment only if its changed behavior is user visible (the header appears), `changelog.d/4345-replica-retry-after-<provider-family>.changed.md`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT:
- Issue #4345; Phase 1 is merged (IReplicaHealthTracker.GetTimeUntilReplicaAvailable and ShardingErrors.AllReplicasUnhealthy exist). The slices #3728, #3729 and #3726 are merged, so the factories already declare ErrorCategory.Unavailable.

TASK:
In the factories of one family (ADO, Dapper or EF Core), replace the inline AllReplicasUnhealthy error with ShardingErrors.AllReplicasUnhealthy and add the unit tests listed.

KEY RULES:
- Identical behavior in every provider of the family; no branch added to SelectReplicaConnectionString (complexity stays 10, so its changed lines need 100 percent coverage).
- The Left is returned before any connection opens; tests use a substitute tracker and FallbackToPrimaryWhenNoReplicas = false; never a real database.
- Per-file targets with a one-sentence justification in the package manifest; targets never go down.

REFERENCE FILES:
- src/Encina.ADO.SqlServer/Sharding/ShardedReadWriteConnectionFactory.cs
- src/Encina.EntityFrameworkCore/Sharding/ShardedReadWriteDbContextFactory.cs
- tests/Encina.UnitTests/EntityFrameworkCore/Sharding/ShardedReadWriteDbContextFactoryTests.cs
- .github/coverage-manifest/Encina.ADO.SqlServer.json
```

</details>

---

### Phase 3: Cross-Cutting Integration

<details>
<summary>Tasks</summary>

1. Resilience (function 6): the header lets the caller's retry policy wait the exact time; no new policy is added.
2. All other functions are not applicable (see the matrix). The change adds no store, no background service and no external integration, and it is not a security gate: the error is still a `Left`, never swallowed (`AGENTS.md` section 3).

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT:
- Issue #4345; the cross-cutting matrix of the plan is the contract.

TASK:
Check that the Integrate row of the matrix (resilience) exists in code and tests.

KEY RULES:
- The AllReplicasUnhealthy outcome stays a Left in every path; no exception is introduced.

REFERENCE FILES:
- AGENTS.md sections 3 and 6
```

</details>

---

### Phase 4: Testing, Documentation & Finalization

<details>
<summary>Tasks</summary>

1. **Unit, Guard, Contract, Property** as listed in Phases 1 and 2. **Integration**: justification file `tests/Encina.IntegrationTests/Sharding/ReplicaRetryAfter.md` (the logic is in-memory; no database behavior changes; the existing sharding integration tests cover connection opening). **Load**: justification file `tests/Encina.LoadTests/Sharding/ReplicaRetryAfter.md` (a minimum over a few replicas, no concurrency change). **Benchmark**: justification file `tests/Encina.BenchmarkTests/Sharding/ReplicaRetryAfter.md` (only runs on the error path).
2. **Per-file targets** (each flag with a one-sentence justification written in the manifest; the baseline is measured at work start and a target is the measured value or higher):

   | File | Unit | Guard | Contract | Property | Integration | Justification |
   |------|:---:|:---:|:---:|:---:|:---:|---------------|
   | `ReplicaHealthTracker.cs` (new member) | 95 | 90 | 90 | 80 | 0 | Real tracker with a fake clock covers every branch; guard covers the argument checks; no external dependency, so integration 0 |
   | `ShardingErrors.cs` | 100 | 100 | 0 | 0 | 0 | One pure factory with two argument branches and the key/no-key branch, all unit and guard tests |
   | `IReplicaHealthTracker.cs` | 0 | 0 | 90 | 0 | 0 | An interface has no executable code; the contract test exercises it through the real tracker |
   | the 7 factories | measured | measured | measured | 0 | measured | Unit tests reach `SelectReplicaConnectionString` through the public read method with substitutes; the target is the measured value after the new tests, never lower than the manifest today |

3. Documentation: the sharding read/write guide gains one paragraph stating that `AllReplicasUnhealthy` answers 503 with `Retry-After` (through `docs-writer`; find the page with `Select-String -Path docs -Pattern 'FallbackToPrimaryWhenNoReplicas'`); XML docs on the new public members; `PublicAPI.Unshipped.txt`.
4. `dotnet build --configuration Release` with 0 warnings; `dotnet test` with every flag at its target; `crap-gate` table for the changed methods measured locally before the pull request.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT:
- Issue #4345 is implemented in the code of Phases 1 and 2. Per-flag coverage model of AGENTS.md section 9.

TASK:
Write the missing tests, the three justification files, the manifest entries and the documentation paragraph; run the build and the tests.

KEY RULES:
- Shouldly through Encina.Testing.Shouldly; FakeTimeProvider for time; tests execute real code and never swallow an exception.
- Justification files only for integration, load and benchmark; never for unit, guard or contract.
- Pages through docs-writer; changelog fragment through mechanical-fixer; never edit CHANGELOG.md.

REFERENCE FILES:
- tests/Encina.UnitTests/Core/Sharding/ReplicaSelection/ReplicaHealthTrackerTests.cs
- .github/coverage-manifest/Encina.json
- changelog.d/README.md
- .claude/skills/encina-docs/SKILL.md
```

</details>

---

### Proposed split into pull requests (limit 15 files and 1500 lines each)

| PR | Content | Approx. files | Approx. lines |
|----|---------|---------------|---------------|
| 1 | `IReplicaHealthTracker.cs`, `ReplicaHealthTracker.cs`, `ShardingErrors.cs`, `PublicAPI.Unshipped.txt`, `Encina.json` manifest, tracker unit test (extended), `ShardingErrors` unit and guard tests, changelog fragment (`Encina`); the contract and property tests and the three justification files go with it | 15 | 700 |
| 2 | 3 ADO factories, 3 new unit test files, 3 manifests (one declared identical sweep) | 9 | 450 |
| 3 | 3 Dapper factories, 3 new unit test files, 3 manifests (one declared identical sweep) | 9 | 450 |
| 4 | EF Core factory, extended unit test, manifest, documentation paragraph | 4 | 150 |

Pull request 1 may exceed 15 files when the contract, property and justification files are counted; if the measured count is above the limit, move the three justification files and the property test into their own pull request after it. Pull requests 2 to 4 need pull request 1 and the category slices #3728, #3729 and #3726 merged.

---

## Research

### Relevant Standards & Specifications

| Reference | What it defines | Use in this feature |
|-----------|-----------------|---------------------|
| RFC 9110 section 10.2.3 (`Retry-After`) | A non-negative delay in seconds (or a date) after which to retry | The value of the header, rounded up by `ErrorCategoryHttp` |
| RFC 9110 section 15.6.4 (503 Service Unavailable) | Temporary unavailability; the server may send `Retry-After` | Why the header belongs on this error |
| gRPC status `UNAVAILABLE` | Transient condition, retry with backoff | The same category in gRPC hosts |
| ADR-062 (error category contract) | `Retry-After` only from `EncinaErrorMetadataKeys.RetryAfter`, never guessed | Rule 2 of the Design Choices |
| `AGENTS.md` sections 1, 3, 5, 6 and 9 | Pre-1.0 changes in place, time from `TimeProvider`, provider coherence, 12-function check, CRAP <= 10 | Design Choices 1, 4 and 7 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `IReplicaHealthTracker`, `ReplicaHealthTracker` | `src/Encina/Sharding/ReplicaSelection/` | Gains the remaining-time member |
| `ReplicaHealthState` | `src/Encina/Sharding/ReplicaSelection/ReplicaHealthState.cs` | `LastFailure` is the input of the computation |
| `ShardingErrorCodes.AllReplicasUnhealthy` | `src/Encina/Sharding/ShardingErrorCodes.cs:120` | The code of the error the helper builds |
| `EncinaErrorMetadataKeys.RetryAfter`, `ErrorCategoryHttp` | `src/Encina/Errors/` | The key and the header writer, unchanged |
| `EncinaErrors.Create` (categorized overload with details) | `src/Encina/Results/EncinaErrors.cs` | Builds the error in the helper |
| `ShardedReadWriteOptions` | `src/Encina/Sharding/ShardedReadWriteOptions.cs` | Delay (line 58) and fallback flag (line 75), unchanged |
| The 7 factories | `src/Encina.{ADO,Dapper}.{SqlServer,PostgreSQL,MySQL}/Sharding/`, `src/Encina.EntityFrameworkCore/Sharding/` | The call sites |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| none | none | The change logs nothing: it adds a key to an error that already exists; no `[LoggerMessage]` is added |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Production code | 10 | Interface, tracker, helper, 7 factories |
| Public API, manifests and fragments | 11 | `PublicAPI.Unshipped.txt`, 8 manifests, up to 3 changelog fragments |
| Tests | 11 | Tracker (extended), helper (unit and guard), contract, property, 3 ADO and 3 Dapper new files, EF Core (extended) |
| Justification files | 3 | Integration, load, benchmark |
| Documentation | 1 | One guide paragraph |
| **Total** | **~36 across 4 pull requests** | See the split table |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
PROJECT CONTEXT:
- Encina is a pre-1.0 .NET 10 / C# 14 library; Either<EncinaError, T>; no [Obsolete], no compatibility layers.
- Issue #4345; plan docs/plans/replica-retry-after-implementation-plan-4345.md. AllReplicasUnhealthy is Unavailable (maintainer, #3728 D11 = B); this plan adds Retry-After only.

IMPLEMENTATION OVERVIEW:
1. IReplicaHealthTracker.GetTimeUntilReplicaAvailable, its implementation and ShardingErrors.AllReplicasUnhealthy in Encina, with tests (Phase 1).
2. The 7 factories call the helper, in three pull requests by family, with unit tests (Phase 2).
3. Cross-cutting check, manifests, justification files, one documentation paragraph (Phases 3 and 4).

KEY PATTERNS:
- Remaining time = min over unhealthy replicas of LastFailure + recoveryDelay - now, from the tracker's TimeProvider, clamped at zero, null when nothing waits.
- The details key EncinaErrorMetadataKeys.RetryAfter holds a TimeSpan and is absent when the time is unknown (ADR-062).
- SelectReplicaConnectionString has complexity 10: add no branch, replace the error expression with the helper call, and cover every changed line.
- The message holds the shard id only.

REFERENCE FILES:
- src/Encina/Sharding/ReplicaSelection/ReplicaHealthTracker.cs
- src/Encina/Errors/ErrorCategoryHttp.cs
- src/Encina.ADO.SqlServer/Sharding/ShardedReadWriteConnectionFactory.cs
- .github/coverage-manifest/Encina.json
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Not applicable: no data is read or cached; the value is computed on the error path |
| 2 | OpenTelemetry | ❌ | Not applicable: an error detail adds no operation worth a span or metric; sharding metrics already read the tracker |
| 3 | Structured Logging | ❌ | Not applicable: nothing is logged by this change; the error is returned, not logged here |
| 4 | Health Checks | ❌ | Not applicable: `ShardReplicaHealthCheck` reads the same tracker and is unchanged |
| 5 | Validation | ❌ | Not applicable: no input is received; argument checks are guard clauses |
| 6 | Resilience | ✅ | `Retry-After` feeds the caller's retry or gateway policy with the exact recovery time |
| 7 | Distributed Locks | ❌ | Not applicable: the tracker is an in-process lock-free structure |
| 8 | Transactions | ❌ | Not applicable: no data operation |
| 9 | Idempotency | ❌ | Not applicable: the read path receives no duplicates; the computation is deterministic for a clock value |
| 10 | Multi-Tenancy | ❌ | Not applicable: no tenant data; shard and replica identity only |
| 11 | Module Isolation | ❌ | Not applicable: no module scoping |
| 12 | Audit Trail | ❌ | Not applicable: no compliance or security implication; the error is an availability signal |

---

## Prerequisites & Dependencies

- The category slices #3728 (PR #4277, ADO), #3729 (Dapper) and #3726 (EF Core) must merge first: they put `ErrorCategory.Unavailable` on the 7 call sites and touch the same lines; pull requests 2 to 4 start after them.
- #4344 (the factories never mark a replica unhealthy) is independent: without it the error appears only when the application feeds the tracker, but this feature is correct either way. Both touch the same factories, so the second to start rebases.
- #3419 and #3424 are the epics of error categories and resilience statuses.

---

## Next Steps

1. When work on #4345 starts, `adversarial-reviewer` tries to break each register row and sets its `Verified` date; the plan is then checked without `-Draft` (`pwsh tools/ai/plans/check-plan.ps1 -Path docs/plans/replica-retry-after-implementation-plan-4345.md`).
2. Pull request 1 (core), then 2, 3 and 4 in any order after the category slices are merged.
