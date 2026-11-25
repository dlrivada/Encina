# Implementation Plan: Retry-After on `AllReplicasUnhealthy` (the time until the replica health tracker re-offers a replica)

> **Issue**: [#4345](https://github.com/dlrivada/Encina/issues/4345)
> **Type**: Feature
> **Complexity**: Low (4 phases, 5 pull requests of 10, 4, 11, 13 and 8 files at most; see the split table)
> **Estimated Scope**: ~140 lines of production code + ~900 lines of tests
> **Family**: sharding read/write separation, error categories epic (#3419, #3424). The category of `AllReplicasUnhealthy` is already decided (maintainer, 2026-11-25, #3728 D11 = B, `Unavailable`); this plan adds only the retry hint.

---

## Summary

`ShardingErrorCodes.AllReplicasUnhealthy` (`src/Encina/Sharding/ShardingErrorCodes.cs:120`) carries no category on `origin/main` (the 7 call sites use the code-only `EncinaErrors.Create`, so the HTTP mapping falls back to 500). The category `Unavailable` (HTTP 503) is decided (#3728 D11 = B) and is being applied by the category slices #3728 (PR #4277, open and unmerged), #3729 and #3726 (issues open, no pull request yet). This plan assumes those slices are merged and adds the missing `Retry-After` header to the 503. `ErrorCategoryHttp.GetResponseHeaders` (`src/Encina/Errors/ErrorCategoryHttp.cs`) writes the header for 429 and 503 when the error details hold a non-negative `TimeSpan` under `EncinaErrorMetadataKeys.RetryAfter` (`src/Encina/Errors/EncinaErrorMetadataKeys.cs:22`); ADR-062 forbids guessing it. The time is known only to the tracker: `ReplicaHealthTracker.GetAvailableReplicas` (`src/Encina/Sharding/ReplicaSelection/ReplicaHealthTracker.cs`) re-offers a replica when `now - LastFailure >= recoveryDelay`, and `_recoveryDelay` is private to the tracker.

State of `origin/main` (`4b0a600c7`), verified by reading. Facts about the category are marked as the state after the slices, because main does not have them yet:

| Fact | Where |
|------|-------|
| 7 call sites build the error with the code-only overload `EncinaErrors.Create(code, message)` (no category on main; the slices #3728, #3729 and #3726 add `ErrorCategory.Unavailable` to the same expression): 3 ADO (`src/Encina.ADO.{SqlServer,PostgreSQL,MySQL}/Sharding/ShardedReadWriteConnectionFactory.cs:242`), 3 Dapper (same file name, line 165), 1 EF Core (`src/Encina.EntityFrameworkCore/Sharding/ShardedReadWriteDbContextFactory.cs:179`); each in a private `SelectReplicaConnectionString(ShardInfo)` | `grep AllReplicasUnhealthy src` |
| MongoDB has no read/write sharded factory with replica selection: `src/Encina.MongoDB/Sharding/ShardedReadWriteMongoCollectionFactory.cs` contains no `AllReplicasUnhealthy`, no `IReplicaHealthTracker` and no replica logic, so the 10-provider rule applies to the 9 relational providers through the 7 factories (the three EF Core providers share one factory) | `Select-String` of that file |
| The default tracker is built from `options.UnhealthyReplicaRecoveryDelay` (for example `src/Encina.ADO.MySQL/Sharding/ShardingServiceCollectionExtensions.cs:155`, `src/Encina.EntityFrameworkCore/Sharding/ShardingServiceCollectionExtensions.cs:394`), but an application can register its own `ReplicaHealthTracker(customDelay)` or another `IReplicaHealthTracker`, so the factories cannot know the delay from the options | those registrations |
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
| 1 | The tracker computes the remaining time: a new member `TimeSpan? GetTimeUntilReplicaAvailable(string shardId, IReadOnlyList<string> allReplicas)` on `IReplicaHealthTracker`, implemented by `ReplicaHealthTracker` from `LastFailure + _recoveryDelay - now` with its `TimeProvider`; the minimum over the replicas that are unhealthy and not yet recovered. The delay belongs to the tracker, not to `ShardedReadWriteOptions`: the DI registration builds the default tracker from `options.UnhealthyReplicaRecoveryDelay` (`src/Encina.ADO.MySQL/Sharding/ShardingServiceCollectionExtensions.cs:155` and its siblings), but an application can register its own `ReplicaHealthTracker(customDelay)` or another `IReplicaHealthTracker`, so computing the time in the factories from the options (or from `GetAllHealthStates` plus the options delay) would give a wrong `Retry-After` for that tracker and would copy the arithmetic 7 times | Settled | `AGENTS.md` section 1 (best solution first, never a compromise for compatibility; the interface has one implementer) and section 5 (abstractions in `Encina`, providers differ only in implementation); `AGENTS.md` section 3 (time from `TimeProvider`) | pending |
| 2 | The result is `null` when no replica of the list is still waiting: a replica that is healthy, never failed, or already past its delay is ignored; the helper then omits the key and the response has no `Retry-After` header | Settled | ADR-062 "Header rules" (`Retry-After` only when the key is present, never guessed) and `src/Encina/Errors/ErrorCategoryHttp.cs` | pending |
| 3 | A replica that is still waiting contributes `LastFailure + _recoveryDelay - now`; the member returns the minimum of those values. Only a strictly positive remaining time counts: if a replica recovered between `GetAvailableReplicas` and this call, its remaining time is `<= 0`, it is ignored like in row 2, and when no replica is left the result is `null` (no key, no header). The member never returns zero or a negative value, so `Retry-After: 0` is never emitted; the header writer rounds a positive value up to whole seconds | Settled | ADR-062 "Header rules" (`Retry-After` only when the key is present, never guessed: an invented `0` would be a guess), `AGENTS.md` section 3 (time from `TimeProvider`) and `ErrorCategoryHttp.GetResponseHeaders` (accepts a non-negative `TimeSpan` and rounds up) | pending |
| 4 | One core helper builds the error: `ShardingErrors.AllReplicasUnhealthy(string shardId, IReplicaHealthTracker tracker, IReadOnlyList<string> replicas)` in `Encina.Sharding`, returning an `EncinaError` with `ErrorCategory.Unavailable` (D11 = B), the existing message (shard id only) and the details. The factory expression `EncinaErrors.Create(code, category, message)` becomes one call, so `SelectReplicaConnectionString` gains no branch (complexity stays 10, so its changed lines need 100 percent line coverage for `CRAP = 10`; with the branch inside the core helper the method stays at its current complexity and the helper's own complexity is 2) and the category lives in one place instead of 7 | Settled | `AGENTS.md` section 9 (CRAP <= 10 on changed methods) and section 5 (provider coherence: no provider-specific code in abstractions, one behavior for all) | pending |
| 5 | The category stays `Unavailable` (not re-decided here) | Settled | Maintainer decision of 2026-11-25 on #3728 (D11 = B), repeated on [#3729](https://github.com/dlrivada/Encina/issues/3729#issuecomment-7160661760) and [#3726](https://github.com/dlrivada/Encina/issues/3726#issuecomment-7160662228) (both comments read on 2026-11-25) | pending |
| 6 | The message keeps only the shard id; the remaining time goes in the details, never in the message, logs or tags | Settled | `AGENTS.md` section 3 (`EncinaError.Message` never reaches logs; no connection string in a message) | pending |
| 7 | The change is added to `IReplicaHealthTracker` in place, with no compatibility layer or default interface member | Trivial | `AGENTS.md` sections 1 and 3 (pre-1.0, no compatibility layers) | pending |
| 8 | Five pull requests, declared now: 1 core (tracker, helper, their unit, guard and error-category tests, 10 files), 1b core (contract and property tests, load and benchmark justification files, 4 files), 2 ADO (3 factories), 3 Dapper (3 factories), 4 EF Core (1 factory); each at most 15 files and 1500 lines, and each of the ADO and Dapper pull requests is one declared identical sweep over its 3 factories | Trivial | `AGENTS.md` section 11 (a worker's issue is one pull request, one package or provider or one declared sweep, at most 15 files and 1500 lines) | pending |
| 9 | Integration tests (not a `.md` justification) assert the 503 plus `Retry-After` round trip for the 9 relational providers, in each provider pull request, on the shared `[Collection]` fixtures. The change is in database read/write factories, so integration is required and a justification file is forbidden | Settled | `AGENTS.md` section 9 (integration required for database features, "NEVER a `.md` justification"; shared `[Collection("<Family>-<Database>")]` fixtures; tests of a provider-dependent feature cover all database providers) | pending |
| 10 | The new tests extend the category-slice test files instead of creating parallel ones: `tests/Encina.UnitTests/ADO/AdoErrorCategoryTests.cs` (PR #4277, helpers `CreateReadWriteFactory(provider, ...)` and `Tracker(params string[] available)`) and the Dapper (#3729) and EF Core (#3726) equivalents; pull requests 2 to 4 wait for those slices and reuse their helpers | Settled | `AGENTS.md` section 9 (tests execute real code, one behavior per test, independent; no duplicated fixtures) and section 3 (provider coherence across the family) | pending |
| 11 | `ShardingErrors.AllReplicasUnhealthy` and the new tracker member check arguments with `ArgumentException.ThrowIfNullOrWhiteSpace(shardId)` and `ArgumentNullException.ThrowIfNull` for the other parameters, as the issue's guard matrix states (null and blank `shardId`). The siblings (`GetAvailableReplicas`) accept a blank id with `ThrowIfNull` only; the new members are stricter on purpose and the asymmetry is limited to them | Settled | Issue #4345 (Test Matrix, GuardTests row: guard tests for null and blank `shardId`) and `AGENTS.md` section 9 (every public method with parameters has guard tests) | pending |

---

## Implementation Phases

### Phase 1: Tracker member and error helper (pull request 1, `Encina`)

> **Goal**: The tracker answers "how long until a replica returns" and one helper builds the error.

<details>
<summary>Tasks</summary>

1. `src/Encina/Sharding/ReplicaSelection/IReplicaHealthTracker.cs`: add `TimeSpan? GetTimeUntilReplicaAvailable(string shardId, IReadOnlyList<string> allReplicas);` with XML docs: the minimum remaining time until a replica of `allReplicas` that is currently unhealthy is re-offered; `null` when none is waiting; never negative.
2. `src/Encina/Sharding/ReplicaSelection/ReplicaHealthTracker.cs`: implement it next to `GetAvailableReplicas`: `ArgumentException.ThrowIfNullOrWhiteSpace(shardId)` and `ArgumentNullException.ThrowIfNull(allReplicas)` (row 11), `now` from `_timeProvider`, loop over `allReplicas`, skip replicas with no state, healthy replicas and replicas with no recorded failure, `remaining = state.LastFailure.Value + _recoveryDelay - now`, ignore `remaining <= TimeSpan.Zero` (row 3), keep the minimum of the positive values, return `null` when none is left. Keep complexity at 8 or less.
3. `src/Encina/Sharding/ShardingErrors.cs` (new, `public static class ShardingErrors`, namespace `Encina.Sharding`): `public static EncinaError AllReplicasUnhealthy(string shardId, IReplicaHealthTracker tracker, IReadOnlyList<string> replicas)` with `ArgumentException.ThrowIfNullOrWhiteSpace(shardId)` and `ArgumentNullException.ThrowIfNull` for the others, returning `EncinaErrors.Create(ShardingErrorCodes.AllReplicasUnhealthy, ErrorCategory.Unavailable, $"All replicas for shard '{shardId}' are currently unhealthy.", details: ...)` where the details hold `EncinaErrorMetadataKeys.RetryAfter` only when the tracker returns a value (complexity 2). XML docs with an example.
4. `src/Encina/PublicAPI.Unshipped.txt`: four lines: `Encina.Sharding.ReplicaSelection.IReplicaHealthTracker.GetTimeUntilReplicaAvailable(string! shardId, System.Collections.Generic.IReadOnlyList<string!>! allReplicas) -> System.TimeSpan?`, `Encina.Sharding.ReplicaSelection.ReplicaHealthTracker.GetTimeUntilReplicaAvailable(...) -> System.TimeSpan?` (the member of the public sealed class), `Encina.Sharding.ShardingErrors` (the type) and `static Encina.Sharding.ShardingErrors.AllReplicasUnhealthy(string! shardId, Encina.Sharding.ReplicaSelection.IReplicaHealthTracker! tracker, System.Collections.Generic.IReadOnlyList<string!>! replicas) -> Encina.EncinaError`. Check the exact line format against neighbouring entries.
5. `.github/coverage-manifest/Encina.json`: see the targets table of Phase 4 (`ReplicaHealthTracker.cs` and `ShardingErrors.cs` get targets; `IReplicaHealthTracker.cs` keeps `defaultTests: []` and gets none).
6. Tests (all in PR 1 unless marked 1b):
   - Unit, extend `tests/Encina.UnitTests/Core/Sharding/ReplicaSelection/ReplicaHealthTrackerTests.cs` (uses `FakeTimeProvider`): no failures gives `null`; one failure gives the delay; two replicas give the minimum; "recovered replica ignored" (one past its delay, one still waiting: the result is the waiting one); "race: non-positive remaining -> null -> no header" (every unhealthy replica is exactly at or past its delay: `null`); advancing the clock lowers the value; a custom recovery delay is honored.
   - Unit, new `tests/Encina.UnitTests/Core/Sharding/ShardingErrorsTests.cs`: category `Unavailable`, key present as a `TimeSpan` or absent, message holds only the shard id.
   - Unit, extend `tests/Encina.UnitTests/Core/ErrorCategoryHttpTests.cs` (cross-cutting function 6): `ErrorCategoryHttp.GetResponseHeaders(error, 503)` for the helper's error returns `Retry-After` with the remaining time rounded up to whole seconds, and no `Retry-After` header when the tracker returns `null`.
   - Guard, extend `tests/Encina.GuardTests/Database/Sharding/ReplicaSelectionGuardTests.cs` (it already holds the `ReplicaHealthTracker` null checks) for the new member and add `tests/Encina.GuardTests/Core/Sharding/ShardingErrorsGuardTests.cs` for the helper: `shardId` null and blank (both throw), `allReplicas`, `tracker`, `replicas` null.
   - Pull request 1b, contract, extend `tests/Encina.ContractTests/Database/Sharding/ReplicaSelectionContractTests.cs`: the real `ReplicaHealthTracker` never returns zero or a negative value and returns `null` when nothing waits. Property, extend `tests/Encina.PropertyTests/Database/Sharding/ReplicaSelectionPropertyTests.cs`: for any set of failure offsets the value is positive or `null`, equals the minimum remaining time, and never grows as the clock advances.

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

1. Pull request 2 (ADO): in `src/Encina.ADO.{SqlServer,PostgreSQL,MySQL}/Sharding/ShardedReadWriteConnectionFactory.cs`, `SelectReplicaConnectionString` has, once the slice #3728 (PR #4277) is merged, an `EncinaErrors.Create(ShardingErrorCodes.AllReplicasUnhealthy, ErrorCategory.Unavailable, ...)` expression (on main today it is the code-only overload); replace that expression with `ShardingErrors.AllReplicasUnhealthy(shard.ShardId, _healthTracker, shard.ReplicaConnectionStrings)`.
2. Pull request 3 (Dapper): the same in `src/Encina.Dapper.{SqlServer,PostgreSQL,MySQL}/Sharding/ShardedReadWriteConnectionFactory.cs`, after the slice #3729.
3. Pull request 4 (EF Core): the same in `src/Encina.EntityFrameworkCore/Sharding/ShardedReadWriteDbContextFactory.cs` (one factory for the three EF Core providers), after the slice #3726.
4. Unit tests extend the category-slice test files instead of creating parallel ones (row 10). Pull request 2 extends `tests/Encina.UnitTests/ADO/AdoErrorCategoryTests.cs` (created by PR #4277: helpers `CreateReadWriteFactory(provider, ...)` and `Tracker(params string[] available)`, tests for fallback on and off, all unhealthy and the selected replica, over the three ADO providers); pull requests 3 and 4 extend the equivalent files that the slices #3729 (Dapper) and #3726 (EF Core, next to `tests/Encina.UnitTests/EntityFrameworkCore/Sharding/ShardedReadWriteDbContextFactoryTests.cs`) create, whose names are fixed when those slices merge. Pull requests 2 to 4 therefore wait for their slice and reuse its helpers. Cases added per family (every provider of the family): all replicas unhealthy with a tracker substitute that returns a known remaining time, `FallbackToPrimaryWhenNoReplicas = false`: the `Left` carries `ErrorCategory.Unavailable` and `EncinaErrorMetadataKeys.RetryAfter` equal to that time; the same with the substitute returning `null`: no key; and the real `ReplicaHealthTracker` with a `FakeTimeProvider` after `MarkUnhealthy`: the key equals the remaining recovery time. The `Left` is returned before any connection is created (`GetReadConnectionAsync` for ADO and Dapper, `CreateReadContext` for EF Core).
5. Coverage of `SelectReplicaConnectionString` (complexity 10, `CRAP = 10` only at 100 percent line coverage, `AGENTS.md` section 9): the branch that decides the retry hint lives in the core helper (complexity 2, covered by the PR 1 tests), so the factory method keeps its current complexity and gains no line besides the one-line helper call. The tests that bring the method to 100 percent lines are the slice tests already listed (no replicas with fallback on and off, available replicas selected, all unhealthy with fallback on, all unhealthy with fallback off) plus the cases of item 4, which cover the helper call itself; the worker measures the method with the local `crap-gate` table and, if a line is not covered, adds the missing case to the same file.
6. Integration tests (row 9), in the same pull request, for the 9 relational providers (MongoDB has no call site, see the state table): ADO and Dapper, each over SqlServer, PostgreSQL and MySQL, in `tests/Encina.IntegrationTests/{ADO,Dapper}/{SqlServer,PostgreSQL,MySQL}/Sharding/ReplicaRetryAfter<Family>IntegrationTests.cs`; EF Core over the three providers in `tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/{SqlServer,PostgreSQL,MySQL}/Sharding/ReplicaRetryAfterEF<Provider>IntegrationTests.cs`. Each class uses the shared `[Collection("<Family>-<Database>")]` fixture (`SqlServerFixture`, `PostgreSqlFixture`, `MySqlFixture`, `EFCore*Fixture`), `[Trait("Category", "Integration")]` and `[Trait("Database", "<Db>")]`, clears data in `InitializeAsync()` and builds a shard whose primary is the fixture's connection string and whose replica is another connection string of the same container. The test marks the replica unhealthy in the real `ReplicaHealthTracker` (fake clock), sets `FallbackToPrimaryWhenNoReplicas = false`, asks the real factory for a read connection or context and asserts the `Left` is `AllReplicasUnhealthy`, then runs it through `ErrorCategoryHttp` and asserts status 503 and a `Retry-After` header equal to the recovery time rounded up. A second case after the clock passes the delay asserts the factory opens the replica connection (the recovery path).
7. Per-file targets for each touched factory in its package manifest (Phase 4 table).
8. Changelog: each provider pull request adds `changelog.d/4345-replica-retry-after-<family>.changed.md` (`ado`, `dapper`, `efcore`), one line: a 503 answered for an all-unhealthy shard now carries `Retry-After` with the replica recovery time. The visible effect is a header on a response, so the section is `changed` and the fragment is not conditional; pull request 1 adds none because it changes no response.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT:
- Issue #4345; Phase 1 is merged (IReplicaHealthTracker.GetTimeUntilReplicaAvailable and ShardingErrors.AllReplicasUnhealthy exist). The category slice of your family (#3728 PR #4277 for ADO, #3729 for Dapper, #3726 for EF Core) is merged, so the factory already declares ErrorCategory.Unavailable and its test file and helpers exist. Do not start before that.

TASK:
In the factories of one family (ADO, Dapper or EF Core), replace the inline AllReplicasUnhealthy error with ShardingErrors.AllReplicasUnhealthy, extend the slice's unit test file with the cases listed and add the integration tests for the family's providers.

KEY RULES:
- Identical behavior in every provider of the family; no branch added to SelectReplicaConnectionString (complexity stays 10, so its lines need 100 percent coverage).
- Unit tests: the Left is returned before any connection opens; substitute tracker, FallbackToPrimaryWhenNoReplicas = false. Integration tests: shared [Collection] fixtures only, never a per-class fixture, never IClassFixture, never dispose the fixture.
- Per-file targets with a one-sentence justification in the package manifest; targets never go down.

REFERENCE FILES:
- src/Encina.ADO.SqlServer/Sharding/ShardedReadWriteConnectionFactory.cs
- src/Encina.EntityFrameworkCore/Sharding/ShardedReadWriteDbContextFactory.cs
- tests/Encina.UnitTests/ADO/AdoErrorCategoryTests.cs (after PR #4277) and the Dapper and EF Core slice test files
- tests/Encina.IntegrationTests/ADO/SqlServer/ReadWriteSeparation/ReadWriteSeparationADOIntegrationTests.cs (fixture usage)
- .github/coverage-manifest/Encina.ADO.SqlServer.json
```

</details>

---

### Phase 3: Cross-Cutting Integration

<details>
<summary>Tasks</summary>

1. Resilience (function 6): the header lets the caller's retry policy wait the exact time; no new policy is added. The outcome is proven by two tests created in Phases 1 and 2: the `ErrorCategoryHttp.GetResponseHeaders(error, 503)` unit test of PR 1 (`Retry-After` with the rounded-up seconds for the helper's error, no header when the key is absent) and the integration tests of PRs 2 to 4 (the 503 plus `Retry-After` round trip per provider).
2. All other functions are not applicable (see the matrix). The change adds no store, no background service and no external integration, and it is not a security gate: the error is still a `Left`, never swallowed (`AGENTS.md` section 3).

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT:
- Issue #4345; the cross-cutting matrix of the plan is the contract.

TASK:
Check that the Integrate row of the matrix (resilience) exists in code and in the tests named in Phase 3 (the ErrorCategoryHttp unit test of PR 1 and the per-provider integration tests); if one is missing, add it.

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

1. **Unit, Guard, Contract, Property** as listed in Phases 1 and 2. **Integration**: real tests, not a justification file (row 9, `AGENTS.md` section 9): the 9 integration test classes of Phase 2 task 6, one per relational provider, on the shared `[Collection]` fixtures. **Load**: justification file `tests/Encina.LoadTests/Sharding/ReplicaRetryAfter.md` (a minimum over a few replicas on the error path, no concurrency change; allowed for load, pull request 1b). **Benchmark**: justification file `tests/Encina.BenchmarkTests/Encina.Benchmarks/Sharding/ReplicaRetryAfter.md` (only runs on the error path; allowed for benchmarks, pull request 1b). Both follow the justification-file format of `AGENTS.md` section 9.
2. **Per-file targets** (each flag with a one-sentence justification written in the manifest; baselines measured on 2026-11-25 in this worktree with `dotnet test tests\Encina.<Flag>Tests --collect "XPlat Code Coverage"` filtered to the existing tests of each file; a target is the measured value or higher and never lower than the manifest today, which has no `targets` for any of these files):

   | File | Unit | Guard | Contract | Property | Integration | Justification |
   |------|:---:|:---:|:---:|:---:|:---:|---------------|
   | `ReplicaHealthTracker.cs` (whole file) | 97 | measured at PR 1 (set to the measured value; today 14/111 lines) | - | - | - | Unit: measured 108 of 111 lines (97 percent) on 2026-11-25 with the existing tracker tests, and the new member is fully covered by the fake-clock tests, so the file must not fall below it. Guard: measured 14 of 111 lines (12 percent), because the guard flag reaches only the argument checks that precede the body; the new member's two checks keep the file at or above it. No contract or property target: those tests exercise one member, not the whole file |
   | `ShardingErrors.cs` (new) | 100 | - | - | - | - | Unit only, as the precedent `IdGenerationErrors.cs` (`*Errors.cs`, `defaultTests: [unit]`, unit 100): the helper is a branch-free build plus one key/no-key condition, and the unit tests run both outcomes. The guard tests only prove the argument checks and have no target on a file whose lines sit after them |
   | `IReplicaHealthTracker.cs` | - | - | - | - | - | No target: an interface has no coverable lines; the manifest keeps `defaultTests: []` ("Interfaces have no implementation to test") |
   | `ShardedReadWriteConnectionFactory.cs` of ADO SqlServer, PostgreSQL, MySQL | 25 | 11 | - | - | 40 | Guard: measured 16 of 137 lines (11 percent) for SqlServer on 2026-11-25, PostgreSQL and MySQL are measured at pull request 2 and the target is each file's own value, never lower. Unit: baseline 0 (no unit test class today); the slice tests plus these reach the constructor, `SelectReplicaConnectionString` and the `Left` returns that precede any connection, about a quarter of the file, and the worker raises it to the measured value. Integration: the new classes open the replica connection on the recovery path |
   | `ShardedReadWriteConnectionFactory.cs` of Dapper SqlServer, PostgreSQL, MySQL | 25 | 15 | - | - | 40 | Guard: measured 16 of 101 lines (15 percent) for SqlServer on 2026-11-25, the others measured at pull request 3 and never lower. Unit and integration as for ADO |
   | `ShardedReadWriteDbContextFactory.cs` (EF Core, 3 providers) | 60 | 0 | - | - | 40 | Unit: the existing `ShardedReadWriteDbContextFactoryTests` (31 tests) already exercise construction and context creation, and the file is 93 lines; the unit run to measure the exact value failed on 2026-11-25 because of a locked test host, so the worker measures it at pull request 4 and sets `max(60, measured)`. Guard: no guard test class for this factory matches today (0 of 93 lines measured), so the manifest keeps its flag and the target stays 0 with this reason. Integration: the three new classes (one per provider) create contexts against the real container |

3. Documentation: the sharding read/write guide gains one paragraph stating that `AllReplicasUnhealthy` answers 503 with `Retry-After` (through `docs-writer`; find the page with `Select-String -Path docs -Pattern 'FallbackToPrimaryWhenNoReplicas'`); XML docs on the new public members; `PublicAPI.Unshipped.txt`.
4. `dotnet build --configuration Release` with 0 warnings; `dotnet test` with every flag at its target; `crap-gate` table for the changed methods measured locally before the pull request.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT:
- Issue #4345 is implemented in the code of Phases 1 and 2. Per-flag coverage model of AGENTS.md section 9.

TASK:
Write the missing tests, the load and benchmark justification files, the manifest entries and the documentation paragraph; run the build and the tests.

KEY RULES:
- Shouldly through Encina.Testing.Shouldly; FakeTimeProvider for time; tests execute real code and never swallow an exception.
- Justification files only for load and benchmark; never for unit, guard, contract or integration of database features.
- Pages through docs-writer; changelog fragments through mechanical-fixer; never edit CHANGELOG.md.

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
| 1 | `IReplicaHealthTracker.cs`, `ReplicaHealthTracker.cs`, `ShardingErrors.cs`, `PublicAPI.Unshipped.txt`, `Encina.json` manifest, extended `ReplicaHealthTrackerTests.cs`, new `ShardingErrorsTests.cs`, extended `ErrorCategoryHttpTests.cs`, extended `ReplicaSelectionGuardTests.cs`, new `ShardingErrorsGuardTests.cs` (`Encina`) | 10 | 600 |
| 1b | Extended `ReplicaSelectionContractTests.cs` and `ReplicaSelectionPropertyTests.cs`, the load and benchmark justification files (declared now so pull request 1 stays far below the limit) | 4 | 250 |
| 2 | ADO: 3 factories, 3 manifests, extended `AdoErrorCategoryTests.cs`, 3 integration test classes, 1 changelog fragment (one declared identical sweep) | 11 | 650 |
| 3 | Dapper: 3 factories, 3 manifests, the slice's unit test file or files (1 to 3), 3 integration test classes, 1 changelog fragment (one declared identical sweep) | 11 to 13 | 700 |
| 4 | EF Core: factory, manifest, extended unit test, 3 integration test classes, documentation paragraph, changelog fragment | 8 | 450 |

Total 44 to 46 files across 5 pull requests. Pull request 1b needs pull request 1. Pull requests 2 to 4 need pull request 1 and their category slice (#3728 PR #4277, #3729, #3726) merged, and reuse the helpers of the slice's test file. If a measured count goes above 15 or 1500 lines, the worker splits the integration test classes into a follow-up pull request of the same family before opening it, never exceeding the limit.

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
| Public API, manifests and fragments | 12 | `PublicAPI.Unshipped.txt`, 8 manifests (`Encina.json`, 3 ADO, 3 Dapper, 1 EF Core), 3 changelog fragments (one per provider pull request) |
| Unit and guard tests | 8 to 10 | Tracker (extended), `ShardingErrorsTests` (new), `ErrorCategoryHttpTests` (extended), `ReplicaSelectionGuardTests` (extended), `ShardingErrorsGuardTests` (new), ADO slice file (extended), Dapper slice files (1 to 3), EF Core (extended) |
| Contract and property tests | 2 | Both extended |
| Integration tests | 9 | One class per relational provider (3 ADO, 3 Dapper, 3 EF Core) |
| Justification files | 2 | Load and benchmark (no integration justification) |
| Documentation | 1 | One guide paragraph |
| **Total** | **44 to 46 across 5 pull requests** | See the split table |

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
2. The 7 factories call the helper, in three pull requests by family (after the category slices), with unit tests that extend the slice files and integration tests for the 9 relational providers (Phase 2).
3. Cross-cutting check, manifests, load and benchmark justification files (pull request 1b), one documentation paragraph (Phases 3 and 4).

KEY PATTERNS:
- Remaining time = min over unhealthy replicas still waiting of LastFailure + recoveryDelay - now, from the tracker's TimeProvider; replicas with remaining <= 0 are ignored; null when nothing waits (never zero, never negative, no Retry-After: 0).
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
| 6 | Resilience | ✅ | Integrate now: `Retry-After` feeds the caller's retry or gateway policy with the exact recovery time; proven by the `ErrorCategoryHttp.GetResponseHeaders(error, 503)` unit test and the per-provider integration tests |
| 7 | Distributed Locks | ❌ | Not applicable: the tracker is an in-process lock-free structure |
| 8 | Transactions | ❌ | Not applicable: no data operation |
| 9 | Idempotency | ❌ | Not applicable: the read path receives no duplicates; the computation is deterministic for a clock value |
| 10 | Multi-Tenancy | ❌ | Not applicable: no tenant data; shard and replica identity only |
| 11 | Module Isolation | ❌ | Not applicable: no module scoping |
| 12 | Audit Trail | ❌ | Not applicable: no compliance or security implication; the error is an availability signal |

---

## Prerequisites & Dependencies

- The category slices #3728 (PR #4277, ADO, open), #3729 (Dapper, no pull request yet) and #3726 (EF Core, no pull request yet) must merge first: they put `ErrorCategory.Unavailable` on the 7 call sites, create the unit test files that this plan extends and touch the same lines; pull requests 2 to 4 start after their slice. Pull requests 1 and 1b do not depend on them.
- #4344 (the factories never mark a replica unhealthy) is independent: without it the error appears only when the application feeds the tracker, but this feature is correct either way. Both touch the same factories, so the second to start rebases.
- #3419 and #3424 are the epics of error categories and resilience statuses.

---

## Next Steps

1. When work on #4345 starts, `adversarial-reviewer` tries to break each register row and sets its `Verified` date; the plan is then checked without `-Draft` (`pwsh tools/ai/plans/check-plan.ps1 -Path docs/plans/replica-retry-after-implementation-plan-4345.md`).
2. Pull request 1 (core), then 1b, then 2, 3 and 4 in any order, each after its category slice is merged.
