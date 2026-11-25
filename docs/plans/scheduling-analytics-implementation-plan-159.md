# Implementation Plan: Scheduled Message Analytics (statistics snapshot, gauges and an operations endpoint)

> **Issue**: [#159](https://github.com/dlrivada/Encina/issues/159)
> **Type**: Feature (observability and operations; `status:proposal`, part of EPIC #3405)
> **Complexity**: Medium-High (10 database providers for one read member, a snapshot service, gauges, one endpoint; 16 pull requests of 5 to 15 files)
> **Estimated Scope**: ~700 lines of production code + ~1,900 lines of tests, SQL and manifests; about 124 file touches in 16 pull requests (PR 1, 2 to 9, 10, 11a to 11c, 12, 13a to 13c), none above 15 files
> **Family**: scheduling-3. Siblings are cited by issue number only: #152 (spans and the three execution instruments; defers the pending gauge to this plan), #3994 (operations endpoints group in `Encina.AspNetCore`), #2229 (shared endpoint policy helper), #739 (tenant on the row), #1251 (row claiming), #906 (observability guide), #160 (webhooks, blocked on #3389)

---

## Summary

### What exists today (read on 2026-11-25, `origin/main` `1c067b45b`)

- There is no analytics API. `IScheduledMessageStore` (`src/Encina.Messaging/Scheduling/IScheduledMessageStore.cs`) has seven members (`AddAsync`, `GetDueMessagesAsync`, `MarkAsProcessedAsync`, `MarkAsFailedAsync`, `RescheduleRecurringMessageAsync`, `CancelAsync`, `SaveChangesAsync`) and no count or aggregate. The only count is `SchedulerOrchestrator.GetPendingCountAsync`, which calls `GetDueMessagesAsync(int.MaxValue, ...)` and materializes every due row.
- The issue's "health check" exists: `SchedulingHealthCheck` and `SchedulingHealthCheckOptions` (`src/Encina.Messaging/Health/SchedulingHealthCheck.cs`: tolerance 5 minutes, warning 10, critical 50), registered in `Encina.AspNetCore` (`HealthCheckBuilderExtensions.cs`).
- The row (`ScheduledMessages`, `src/Encina.ADO.SqlServer/Scripts/004_CreateScheduledMessagesTable.sql` and the other nine) holds `ScheduledAtUtc`, `CreatedAtUtc`, `ProcessedAtUtc`, `LastExecutedAtUtc`, `ErrorMessage`, `RetryCount`, `NextRetryAtUtc`, `IsRecurring`, `CronExpression`. It holds **no processing duration and no per-execution history**: a recurring row is overwritten by each run, and a retried row keeps one `RetryCount`. A dead-lettered row stays in the table (`RetryCount >= MaxRetries`, `ProcessedAtUtc` null) and its copy goes to the dead letter queue with source `Scheduling`. No purge covers scheduled rows (`MessagePurgeCycle` handles outbox and inbox).
- The gauge pattern is unwired today: `MessagingStoreMetricsCallbacks` is never registered in production code (only tests construct it), so `encina.outbox.pending_count` and `encina.saga.active_count` have no values. A gauge callback is synchronous, so it cannot await a store.
- `IScheduledMessageStore` has 10 implementers (8 provider stores: `ScheduledMessageStoreADO` x3, `ScheduledMessageStoreDapper` x3, `ScheduledMessageStoreEF`, `ScheduledMessageStoreMongoDB`; `InstrumentedScheduledMessageStore`; `FakeScheduledMessageStore`); no test class implements it. `Encina.Messaging` grants `InternalsVisibleTo` to the unit, guard, contract, property and integration projects and to the data packages.
- EventIds: `MessagingScheduling` 2300-2399 uses 2300-2306 and 2320-2325; the batch registry holds 2326-2389; 2307-2319 are free. `AspNetCore` 200-249: 200-207 used, claims up to 240 (#3994 233-240).

### What the issue proposes and what changes

The issue proposes `IScheduledMessageMetrics` with `Task<long>` and `Task<TimeSpan>` methods (no `Either`, against `AGENTS.md` section 3), averages, a p95 of processing time, skipped counts and failure trends. Most of that cannot be answered from the table (no duration, no history, no "skipped" state). This plan delivers what the persisted state can answer exactly on all 10 providers, and records the rest as a deferred section of this plan (Next Steps, "Deferred: scheduled-message execution history"), with the per-instance timing histograms coming from #152. The maintainer rule is that a `[FEATURE]` follow-up goes inside the plan, so no separate issue is opened for it.

### What this plan delivers

- `SchedulingStatisticsQuery` and `SchedulingStatistics` (public records) and one store member `GetStatisticsAsync(SchedulingStatisticsQuery, CancellationToken)` on `IScheduledMessageStore`: pending, due, overdue, failing and dead-lettered counts, processed rows in a window, the oldest due time and pending counts by request type.
- `ISchedulingAnalytics` and `SchedulingAnalytics` returning `SchedulingHealthSnapshot` (counts, failure rate, `HealthStatus` from the existing `SchedulingHealthCheckOptions`, `GeneratedAtUtc` from `TimeProvider`).
- Opt-in gauges (`encina.scheduling.pending`, `.overdue`, `.dead_lettered`, `.oldest_due_age`) fed by a cached snapshot refreshed by a hosted service, which also closes the pending gauge #152 deferred.
- `MapSchedulingStatistics(authorizationPolicy, prefix)` in `Encina.AspNetCore`: a fail-closed read endpoint.
- Shared pieces and owner: #152 owns the spans and the three execution instruments; #3994 and #2229 own the endpoint policy helper and the `Encina.AspNetCore` meter; #739 owns the tenant column (the tenant predicate lands in the last pull request); this plan owns the query and snapshot types, the store member, the cache and the four gauges.
- Affected packages: `Encina.Messaging`, the 8 provider packages, `Encina.OpenTelemetry`, `Encina.AspNetCore`, `Encina.Testing.Fakes`. Provider category: Database (10): ADO.NET x3, Dapper x3, EF Core x3 (one store class), MongoDB.
- Not delivered, with the reason: average and p95 processing time, trends and a skipped count (no persisted data; the deferred section in Next Steps); a webhook (#160); a purge of old scheduled rows (not asked; the deferred section records that the history needs one).

---

## Design Choices

<details>
<summary><strong>1. What the analytics can answer (Dilemma)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) State-derived snapshot: counts by state, the oldest due time and pending by type, computed by one portable query set on the existing columns; timing comes from the #152 histograms** | No schema change; identical semantics on 10 providers (`COUNT`, `MIN`, `GROUP BY` only); exact | No average processing time, p95, trend or skipped count in the API |
| **B) A new `ScheduledMessageExecutions` table written at every run on 10 providers (duration, outcome) and aggregated by the store** | Answers every metric of the issue, cluster-wide | A second write per dispatch, a purge policy, a schema on 10 providers: a feature of its own |
| **C) Aggregate the OpenTelemetry instruments in process** | No store work | Per instance only, lost on restart, no cluster view |

### Chosen Option: **A — state-derived snapshot** (recommended, pending the maintainer)

### Rationale

- A wins when the operator question is "is the backlog growing and is anything stuck"; B wins if the reference application needs cluster-wide latency percentiles. B is recorded as the deferred section "scheduled-message execution history" in Next Steps.

</details>

<details>
<summary><strong>2. The return shape: `Either`, not bare values</strong></summary>

**Class:** Settled
**Settled by:** `AGENTS.md` section 3 (Railway Oriented Programming: operations return `Either<EncinaError, T>`) and ADR-001 and ADR-006.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Task<Either<EncinaError, SchedulingHealthSnapshot>> GetSnapshotAsync(TimeSpan? window = null, CancellationToken cancellationToken = default)`; a null window means 24 hours** | Store failure is a `Left`, never an exception; one call gives a consistent snapshot | One type instead of twelve methods |
| **B) The issue's twelve `Task<long>` and `Task<TimeSpan>` methods** | Literal | Failures become exceptions; twelve round trips to the database |

### Chosen Option: **A — one snapshot call with `Either`**

### Rationale

- Only A complies with the ROP rule. The nullable window accepts null and returns the 24-hour result (a valid-result test, not an exception test).

</details>

<details>
<summary><strong>3. Where the query lives (Dilemma)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One new member `GetStatisticsAsync(SchedulingStatisticsQuery, CancellationToken)` on `IScheduledMessageStore`** | Stores share one interface across providers (`AGENTS.md` section 5, provider coherence); the decorator and the fake extend in place; one registration | 10 implementers change; the interface grows to eight members |
| **B) A separate read interface `ISchedulingStatisticsStore` implemented by the same 8 classes** | The mutating interface stays small; the decorator and the fake are untouched | A second registration per provider package (8 `ServiceCollectionExtensions` edits) and a second decorator for ADR-054 D6 |

### Chosen Option: **A — a member on the existing interface** (recommended, pending the maintainer)

### Rationale

- A wins on fewer registrations and one decorated surface; B wins if the maintainer wants read models kept out of the write interface (the outbox plans keep their counts on the store interface, as `GetPendingCountAsync` does). The split below is implementers first, interface last (Design Choice 7), so the build never breaks between pull requests.

</details>

<details>
<summary><strong>4. How the gauges get their values (Dilemma)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A hosted `SchedulingStatisticsRefresher` in `Encina.Messaging`, registered only when `SchedulingOptions.PublishStatisticsGauges` is `true` (default `false`), polls `GetStatisticsAsync` every `StatisticsRefreshInterval` (default 30 seconds) in a fresh scope and stores the result in a singleton cache; the gauge callbacks read the cache** | The callback never touches the database; the cost exists only when asked for; the loop catches every failure and logs the code | Values are up to one interval old; each instance polls |
| **B) The refresher in `Encina.OpenTelemetry`, registered by `WithEncina()`** | No option | Every host that adds OpenTelemetry pays a query per interval even when nobody scrapes |
| **C) No gauges; the snapshot endpoint and the health check only** | No loop | The pending gauge of the issue stays missing |
| **D) The callback blocks on the async store** | Fresh values | Sync-over-async in a metrics callback: thread-pool starvation risk (`AGENTS.md` section 3 on asynchronous database calls) |

### Chosen Option: **A — opt-in refresher with a cache** (recommended, pending the maintainer)

### Rationale

- A wins on pay-for-what-you-use (`AGENTS.md` section 1); B wins if the maintainer prefers zero configuration; D is rejected by the asynchronous database rule. The refresher is a `BackgroundService` over `PeriodicTimer`: it catches per iteration, lets only the stopping token's cancellation end the loop, never throws out of a cycle, logs the error code through `ForLogging()` (ADR-052 and `AGENTS.md` section 3, background loops), and uses `IServiceScopeFactory` because the store is scoped.

</details>

<details>
<summary><strong>5. The HTTP endpoint (Dilemma)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `MapSchedulingStatistics(this IEndpointRouteBuilder, string authorizationPolicy, string prefix = "/encina/scheduling")` in `Encina.AspNetCore`, `GET {prefix}/statistics`, fail closed: the policy is required and checked at mapping time with the shared helper of #2229; #3994's group may call it** | Independent of the group's schedule; same fail-closed model as `MapDPIAEndpoints` | A second map method |
| **B) Add the route to the #3994 group `MapEncinaOperationsEndpoints`** | One map call for operations | Blocked until #3994 merges; its scope is writes plus counts |
| **C) The issue's `MapSchedulingMetrics(pattern)` without a policy** | Literal | Exposes backlog and request-type names unauthenticated: fails closed nowhere |

### Chosen Option: **A — its own fail-closed map method** (recommended, pending the maintainer)

### Rationale

- A wins on independence and security; B wins if the maintainer wants one operations surface (then the method body moves into the group). C is rejected by the fail-closed rule (`AGENTS.md` section 3). The response is the snapshot DTO with the request type shortened to its type name (no assembly qualifier) and never contains content or `ErrorMessage`.

</details>

<details>
<summary><strong>6. Tenant scope of the statistics (Dilemma)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Cross-tenant until #739; after #739 the query takes a tenant: `ITenantScopeResolver.ResolveRead` (#1257) decides it, a multi-tenant caller without a tenant is denied, and the cross-tenant view needs an explicit platform policy** | Fails closed once the column exists; one rule with the other SPEC-002 stores | The endpoint is operator-only until the last pull request |
| **B) Always cross-tenant (an operator view)** | Simple | A tenant administrator cannot be given the endpoint; SPEC-002 AC-043 (queries filter by tenant) is not met |

### Chosen Option: **A — tenant-scoped after #739** (recommended, pending the maintainer)

### Rationale

- A wins on AC-043; B wins if only platform operators will ever call it. The tenant predicate is `TenantId = @TenantId` with the binary collation of #739; under the reserved `default` tenant (#1257) the filter is injective with real tenant ids. The tenant tag on the gauges is not emitted (the cache is global); per-tenant gauges would be unbounded cardinality.

</details>

<details>
<summary><strong>7. Build order of the pull request split (Dilemma)</strong></summary>

**Class:** Dilemma

`AGENTS.md` section 8 (zero warnings, the full solution builds) and section 11 (one pull request is at most 15 files and 1500 lines and builds and passes alone) are the rules; more than one order satisfies them, so the choice is the maintainer's (the same choice is a Dilemma in #739).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Types first, then each provider adds the concrete member, then the interface, the decorator and the fake, then the service** | Every pull request compiles and carries its own tests | The member exists on classes before the interface has it |
| **B) Interface first with a default interface member as a scaffold** | Natural order; the interface is visible early | A compatibility layer with untested defaults merges (`AGENTS.md` section 1) |
| **C) Interface first, implemented by the 10 classes in one pull request** | No intermediate state | Over the file and line limits (`size-exception-approved` needed) |

### Chosen Option: **A — implementers first, interface last** (recommended, pending the maintainer)

### Rationale

- A wins because it leaves `main` building after every merge without a scaffold or a limit exception; it is the order of #737, #738 and #1222. C wins if the maintainer prefers one large review; B is rejected by the compatibility rule, which is the maintainer's to relax.

</details>

---

## Implementation Phases

### Phase 1: Core types (PR 1)

> **Goal**: The public records with their validation, before any implementer needs them.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina.Messaging/Scheduling/SchedulingStatisticsQuery.cs` (public sealed record; properties `Window` (`TimeSpan`, greater than zero), `MaxRetries` (zero or more), `OverdueTolerance` (`TimeSpan`, zero or more), `TopRequestTypes` (1 to 100); a factory `Create(...)` throws `ArgumentOutOfRangeException` on a bad value, as `GetDueMessagesAsync` throws `ArgumentException` for a bad batch size).
2. **Create** `src/Encina.Messaging/Scheduling/SchedulingStatistics.cs` (public sealed record: `PendingCount`, `DueCount`, `OverdueCount`, `FailingCount`, `DeadLetteredCount`, `ProcessedInWindowCount` (all `long`), `OldestDueAtUtc` (`DateTime?`), `PendingByRequestType` (`IReadOnlyDictionary<string, long>`)). Definitions, identical on every provider: pending = `ProcessedAtUtc` null and `RetryCount < MaxRetries`; due = pending and (`NextRetryAtUtc` or, when null, `ScheduledAtUtc`) not after now; overdue = due and that time before now minus the tolerance; failing = pending with `RetryCount > 0`; dead-lettered = `ProcessedAtUtc` null and `RetryCount >= MaxRetries`; processed in window = `ProcessedAtUtc` not before now minus the window (a recurring series counts when it ends; its runs are telemetry).
3. Tests (new): unit tests of the validation, guard tests for `Create` (each out-of-range value), manifest and `PublicAPI.Unshipped.txt` through `mechanical-fixer`. PR 1 is 6 files: the two records, the unit test, the guard test, `PublicAPI.Unshipped.txt` and `Encina.Messaging.json`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT:
- Issue #159, plan docs/plans/scheduling-analytics-implementation-plan-159.md. Dilemmas 1, 3, 4, 5, 6 and 7 are recommended, pending the maintainer.

TASK:
Create the two public records with the exact state definitions of the plan and their validation.

KEY RULES:
- Pre-1.0: no compatibility layer. Public API gets XML docs and PublicAPI.Unshipped.txt lines.
- Time never comes from DateTime.UtcNow; the store reads its TimeProvider.

REFERENCE FILES:
- src/Encina.Messaging/Scheduling/IScheduledMessageStore.cs
- src/Encina.Messaging/Health/SchedulingHealthCheck.cs
```

</details>

### Phase 2: Provider implementations (PRs 2 to 9)

<details>
<summary><strong>Tasks</strong></summary>

Per package, one pull request each: `Encina.ADO.SqlServer`, `Encina.ADO.PostgreSQL`, `Encina.ADO.MySQL`, `Encina.Dapper.SqlServer`, `Encina.Dapper.PostgreSQL`, `Encina.Dapper.MySQL`, `Encina.EntityFrameworkCore` (one store, three databases), `Encina.MongoDB`.

1. `Scheduling/ScheduledMessageStoreADO.cs` or `ScheduledMessageStoreDapper.cs` (or `ScheduledMessageStoreEF.cs`, `ScheduledMessageStoreMongoDB.cs`): the concrete public method `GetStatisticsAsync(SchedulingStatisticsQuery query, CancellationToken cancellationToken = default)` (null query throws `ArgumentNullException`). SQL Server: conditional `COUNT_BIG` aggregates in one statement and `SELECT TOP (@Top)` for the by-type group; PostgreSQL and MySQL: `COUNT(*) FILTER (WHERE ...)` and `SUM(CASE WHEN ... THEN 1 ELSE 0 END)` respectively, `LIMIT @Top`; EF Core: `CountAsync` and `GroupBy` over `Set<ScheduledMessage>()`; MongoDB: `CountDocumentsAsync` with filters and an aggregation `$group` with `$sort` and `$limit`. Asynchronous calls with the token, `DbLease.Enlisted` like the existing members, the failure code `scheduling.statistics_failed` with ADR-062 category `Unavailable` (a database failure while reading; 503 at the endpoint): `EitherHelpers.TryAsync` takes no category, so the member catches the exception itself and builds the error with the categorized overload `EncinaErrors.FromException("scheduling.statistics_failed", ErrorCategory.Unavailable, exception)`; an `OperationCanceledException` from the caller's token propagates. The request type key of the dictionary is the stored string; the endpoint shortens it.
2. Index review: the due and pending predicates use the existing `IX_ScheduledMessages_ScheduledAt_Processed` (SQL Server) and its peers; no new index is planned, and each pull request records the measured plan on the integration database in its description.
3. Per package: `PublicAPI.Unshipped.txt` line, manifest entry, an integration test in the shared collection (`[Collection("ADO-SqlServer")]` and its peers, `ClearAllDataAsync` in `InitializeAsync`, never `IClassFixture`): rows in every state give the exact counts, an empty table gives zeros and a null `OldestDueAtUtc`, `TopRequestTypes` truncates, a row exactly at the tolerance boundary is not overdue; a guard test edit (null query). Each provider pull request is 5 files (the store, its integration test class, the guard test edit, `PublicAPI.Unshipped.txt` and the package manifest); the EF Core pull request is 7 (three integration test classes, one per database collection, instead of one). The eight pull requests total 42 files. No schema change, so no inline test DDL edit is needed.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT:
- Phase 1 merged. IScheduledMessageStore is NOT changed yet: this pull request adds the member to ONE package's concrete class so it builds against the old interface.

TASK:
Implement GetStatisticsAsync for the one package of this pull request with the state definitions of the plan.

KEY RULES:
- Eight implementations behave identically and differ only in dialect: SQL Server @param, TOP (@n); PostgreSQL LIMIT @n and FILTER; MySQL LIMIT @n, backticks.
- Asynchronous database calls with a CancellationToken; never IDbConnection.Open().
- Integration tests use the shared [Collection] fixtures; cover one case per state.

REFERENCE FILES:
- src/Encina.ADO.SqlServer/Scheduling/ScheduledMessageStoreADO.cs
- src/Encina.EntityFrameworkCore/Scheduling/ScheduledMessageStoreEF.cs
- src/Encina.MongoDB/Scheduling/ScheduledMessageStoreMongoDB.cs
```

</details>

### Phase 3: Interface, decorator and fake (PR 10)

<details>
<summary><strong>Tasks</strong></summary>

1. `IScheduledMessageStore.GetStatisticsAsync` (Design Choice 3, option A) with XML docs.
2. `src/Encina.OpenTelemetry/MessagingStores/InstrumentedScheduledMessageStore.cs`: pass-through with a span `encina.scheduling.store.get_statistics` (the naming of #152), error code only.
3. `src/Encina.Testing.Fakes/Stores/FakeScheduledMessageStore.cs`: compute the statistics in memory from its list with the same definitions (a `TimeProvider` is already used by the fake or added as an optional last parameter).
4. Tests: decorator unit test, fake unit test, a contract test that the fake and the real EF Core store return the same statistics for the same rows. PublicAPI lines (Messaging, Fakes) and the manifests of `Encina.Testing.Fakes` and `Encina.OpenTelemetry` through `mechanical-fixer`. PR 10 is 11 files: the interface, the decorator, the fake, the three tests, two `PublicAPI.Unshipped.txt` files and two manifests (the interface has no executable code, so no Messaging manifest entry).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
- All 8 provider stores have the concrete member. Implementers of IScheduledMessageStore: the 8 stores, InstrumentedScheduledMessageStore, FakeScheduledMessageStore (grep again on origin/main).

TASK:
Add the interface member and update the decorator and the fake in the same pull request.

KEY RULES:
- The decorator returns the inner result unchanged; the span carries the error code only.
- The fake uses the same state definitions as the stores.

REFERENCE FILES:
- src/Encina.Messaging/Scheduling/IScheduledMessageStore.cs
- src/Encina.OpenTelemetry/MessagingStores/InstrumentedScheduledMessageStore.cs
- src/Encina.Testing.Fakes/Stores/FakeScheduledMessageStore.cs
```

</details>

### Phase 4: Snapshot service, options, cache, gauges and refresher (PRs 11a, 11b and 11c)

> **Goal**: The old single pull request (17 to 19 files by its own list) is three pull requests, each building and passing alone: **11a** the snapshot service (task 1 and the `TryAddScoped` line of task 3), **11b** the options, the cache, the gauges and the meter subscription (task 2 without the refresher, the gauge registration of task 3 and the `AddMeter` line), **11c** the refresher, its log and the EventIds (the refresher of task 2, task 4 and the hosted-service registration). Per-pull-request file lists: **11a, 10 files**: `ISchedulingAnalytics.cs`, `SchedulingAnalytics.cs`, `SchedulingHealthSnapshot.cs`, `MessagingServiceCollectionExtensions.cs`, the unit test, the guard test, the property test, the DI test, `PublicAPI.Unshipped.txt` (the three public types) and `Encina.Messaging.json`. **11b, 10 files**: `SchedulingOptions.cs`, `SchedulingStatisticsCache.cs`, `SchedulingStatisticsGauges.cs`, `MessagingServiceCollectionExtensions.cs`, `Encina.OpenTelemetry` `ServiceCollectionExtensions.cs`, the cache-and-gauges test, the DI test (both settings), `PublicAPI.Unshipped.txt` (the two options members), `Encina.Messaging.json` and `Encina.OpenTelemetry.json`. **11c, 7 files**: `SchedulingStatisticsRefresher.cs`, `SchedulingStatisticsLog.cs`, `MessagingServiceCollectionExtensions.cs`, `docs/engineering/eventid-block-registry.md` (the claim row), the refresher test, the DI test edit and `Encina.Messaging.json`.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina.Messaging/Scheduling/ISchedulingAnalytics.cs` and `SchedulingAnalytics.cs` (public; constructor `(IScheduledMessageStore store, SchedulingOptions options, SchedulingHealthCheckOptions? health = null, TimeProvider? timeProvider = null)`; `GetSnapshotAsync` builds the query, calls the store and returns `SchedulingHealthSnapshot`), and **create** `SchedulingHealthSnapshot.cs` (public sealed record: the counts, `FailureRate` = (failing + dead-lettered) divided by (pending + dead-lettered + processed in window), 0 when the denominator is 0; `Status` Unhealthy at the critical overdue threshold, Degraded at the warning threshold, else Healthy, using `SchedulingHealthCheckOptions`; `GeneratedAtUtc`).
2. `SchedulingOptions`: `PublishStatisticsGauges` (false) and `StatisticsRefreshInterval` (30 seconds). **Create** `SchedulingStatisticsCache` (internal singleton, volatile snapshot) and `SchedulingStatisticsRefresher` (internal `BackgroundService`, Design Choice 4) and `SchedulingStatisticsGauges` (internal, `Meter("Encina.Messaging")`, four `ObservableGauge<long>` reading the cache; no tags).
3. `src/Encina.Messaging/MessagingServiceCollectionExtensions.cs` `RegisterScheduling` (line 377): `TryAddScoped<ISchedulingAnalytics, SchedulingAnalytics>()`; when `PublishStatisticsGauges`, the cache, the gauges and `AddHostedService<SchedulingStatisticsRefresher>()` (singleton using `IServiceScopeFactory`). A DI test builds the provider with `ValidateOnBuild` and `ValidateScopes` for both settings. `Encina.OpenTelemetry` `WithEncina` adds `AddMeter("Encina.Messaging")` if #152 or #721 has not.
4. EventIds 2307-2308 (Design: Event ID Allocation), packed without a gap: 2307 refresher started (Information, interval), 2308 a refresh failed (Warning, error code through `ForLogging()` or the code only); a "gauges disabled after repeated `Left`" event is not added: failures keep the last good snapshot and log 2308 each cycle. The two ids are `MessagingScheduling` ids; `SchedulingStatisticsLog` is a new internal log class; `EncinaEventIdAllocationTests` needs no map change (the range exists).
5. Tests: unit (snapshot arithmetic, thresholds, null window means 24 hours, a store `Left` is returned, the refresher keeps the last snapshot on `Left` and never throws out of a cycle: use `FakeTimeProvider` and assert the failure is logged and the loop continues), guard (constructor parameters), DI test, a property test (FailureRate is in [0, 1] and monotone in dead-lettered).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT:
- The interface member exists (Phase 3). Registration lives in RegisterScheduling of MessagingServiceCollectionExtensions. Three pull requests: 11a snapshot service, 11b options, cache, gauges and AddMeter, 11c refresher, log and EventIds.

TASK:
Add the snapshot service (11a), then the options, cache and gauges (11b), then the refresher (11c), each as its own pull request.

KEY RULES:
- The refresher is a singleton: resolve the scoped store through IServiceScopeFactory per cycle.
- A background loop never lets an exception escape except the stopping token's cancellation; a Left is logged by error code and the last snapshot is kept.
- Optional constructor parameters last; options are opt-in and default off.

REFERENCE FILES:
- src/Encina.Messaging/MessagingServiceCollectionExtensions.cs
- src/Encina.Messaging/Scheduling/ScheduledMessageProcessor.cs
- src/Encina.Messaging/Health/SchedulingHealthCheck.cs
```

</details>

### Phase 5: Endpoint (PR 12)

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina.AspNetCore/SchedulingStatisticsEndpointExtensions.cs` (`MapSchedulingStatistics`, Design Choice 5): policy required and checked at mapping time (the shared helper of #2229; if it has not merged, the pull request waits for it), `GET {prefix}/statistics?windowHours=` (1 to 720, default 24), the DTO with shortened request-type names, `ToProblemDetails` for a `Left` (the status comes from the error category: `scheduling.statistics_failed` is `Unavailable`, so 503). EventIds 241-242 in `AspNetCore` (241 request failed, error code only; 242 window rejected): registered range, 241 and 242 free on `origin/main` (re-grep when work starts); the two events live in a new internal log class `src/Encina.AspNetCore/Diagnostics/SchedulingStatisticsEndpointLog.cs` and the claim row goes in `docs/engineering/eventid-block-registry.md`.
2. Tests: unit (policy missing throws at mapping time), integration through `TestServer` (401 without the policy, 200 with it, the body never contains content or an error message), guard test, `PublicAPI.Unshipped.txt` and manifest through `mechanical-fixer`.
3. PR 12 is 11 files: the endpoint extension, the log class, the registry row, the unit, integration and guard tests, `PublicAPI.Unshipped.txt` and `Encina.AspNetCore.json`, plus the three files of Phase 8: the feature page, the observability guide cross-reference and the changelog fragment.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
CONTEXT:
- ISchedulingAnalytics is registered. The model endpoint is MapDPIAEndpoints in src/Encina.AspNetCore/DPIAEndpointExtensions.cs.

TASK:
Add MapSchedulingStatistics with the fail-closed policy and the DTO.

KEY RULES:
- Missing policy: throw at mapping time; never map an open endpoint.
- Response and logs carry counts and type names only; never content, ErrorMessage or the assembly qualifier.

REFERENCE FILES:
- src/Encina.AspNetCore/DPIAEndpointExtensions.cs
- src/Encina.Messaging/Scheduling/SchedulingAnalytics.cs
```

</details>

### Phase 6: Cross-cutting integration, tenant scope (PRs 13a, 13b and 13c, after #739 and #1257)

<details>
<summary><strong>Tasks</strong></summary>

1. `SchedulingStatisticsQuery` gains `TenantId` (`string?`, null only for the platform view); the eight stores add `AND TenantId = @TenantId` when it is set (binary collation, as the column of #739); `SchedulingAnalytics.GetSnapshotAsync` takes the tenant from `ITenantScopeResolver.ResolveRead` (#1257); a multi-tenant call without a tenant returns `Left(encina.tenancy.tenant_required)` (`InvalidRequest`); the platform view requires the explicit policy of the endpoint.
2. Tests: a two-tenant integration test on the 10 providers (tenant A never counts tenant B's rows; a tenant named `default` is a real value), unit tests of the denial.
3. The change touches the eight stores again, so it is three pull requests (the original single pull request was about 27 files), in this order, each at most 15 files and building alone because the query's new member is optional:
   - **13a, 9 files**: `SchedulingStatisticsQuery.cs` (`TenantId`), its unit test edit, `PublicAPI.Unshipped.txt` (Messaging), and the three ADO.NET stores with their three integration test classes edited (the two-tenant cases).
   - **13b, 6 files**: the three Dapper stores and their three integration test classes edited.
   - **13c, 12 files**: `ScheduledMessageStoreEF.cs` and its three integration test classes, `ScheduledMessageStoreMongoDB.cs` and its integration test, `SchedulingAnalytics.cs` (the optional trailing `ITenantScopeResolver? tenantScopeResolver = null` constructor parameter, with its line edited in `PublicAPI.Unshipped.txt`), `SchedulingAnalyticsTenantTests.cs` (new: denial and resolver cases), the guard test edit (a null resolver is accepted and valid) and `Encina.Messaging.json`.
   The manifests of the eight store packages are not edited in 13a to 13c: the tenant predicate adds cases to files whose targets were entered in pull requests 2 to 9, and a target that must change moves to its own follow-up pull request of the same package.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
CONTEXT:
- #739 and #1257 have merged: TenantId on ScheduledMessage and ITenantScopeResolver exist.

TASK:
Add the tenant predicate and the resolver call.

KEY RULES:
- Fail closed: a multi-tenant request without a tenant is denied, never widened.
- Tenant ids compare ordinally (binary collation); no raw tenant id in any tag or log.

REFERENCE FILES:
- src/Encina.Messaging/Scheduling/SchedulingAnalytics.cs
- the plan of #739 (column and collation)
```

</details>

### Phase 7: Per-file coverage targets (entered in the PR that carries the tests)

<details>
<summary><strong>Tasks</strong></summary>

| File | PR | Flag and target | Justification |
| --- | --- | --- | --- |
| `Scheduling/SchedulingStatisticsQuery.cs` | 1 | unit 95, guard 90 | Every validation branch is a test. |
| `Scheduling/SchedulingStatistics.cs` | 1 | unit 0, guard 0 | A property-only record: no executable branch to cover. |
| `Scheduling/SchedulingAnalytics.cs`, `Scheduling/SchedulingHealthSnapshot.cs` | 11a | unit 95, guard 90 | Arithmetic, thresholds and the `Left` path run against a substitute store. |
| `Scheduling/SchedulingStatisticsRefresher.cs` | 11c | unit 85 | `FakeTimeProvider` drives cycles, failure and cancellation. |
| `Scheduling/SchedulingStatisticsCache.cs`, `Diagnostics/SchedulingStatisticsGauges.cs` | 11b | unit 90 | A `MeterListener` reads the gauges. |
| `Diagnostics/SchedulingStatisticsLog.cs` | 11c | unit 0, guard 0 | `[LoggerMessage]` declarations with no parameters to guard; the generated bodies are not in the file. |
| `ScheduledMessageStoreADO.cs` of the three ADO.NET packages | 2 to 4 | integration 90, guard 10 (current, not lowered) | Integration tests on real databases cover the SQL; guard covers the null query. |
| `ScheduledMessageStoreDapper.cs` of the three Dapper packages | 5 to 7 | integration 90, guard 9 (current, not lowered) | Same cases. |
| `ScheduledMessageStoreEF.cs` | 8 | integration 90 (no per-file target today; the package default is 25, raised), guard 10 | Three database collections cover the new member. |
| `ScheduledMessageStoreMongoDB.cs` | 9 | unit 92, guard 10, integration 95 (current, not lowered) | The MongoDB fixture covers the aggregation. |
| `SchedulingStatisticsEndpointExtensions.cs` | 12 | unit 85, integration 80, guard 80 | `TestServer` drives 401, 200 and a `Left`. |
| `FakeScheduledMessageStore.cs` | 10 | unit 60, guard 15 (current, not lowered) | The in-memory statistics are unit-tested with the same cases as the stores. |
| `InstrumentedScheduledMessageStore.cs` | 10 | unit 76, guard 71 (current, not lowered) | The new pass-through and span are unit-tested. |

Measure with `dotnet test tests\Encina.<Flag>Tests --collect "XPlat Code Coverage" --results-directory artifacts\coverage\<Flag>Tests` and `dotnet run --file .github/scripts/coverage-report.cs -- --input artifacts/coverage --output artifacts/coverage-report`. CRAP: each store member stays at comp 10 or less by extracting the query building.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
CONTEXT:
- Targets live in .github/coverage-manifest/{Package}.json; they never go down.

TASK:
Enter the per-file targets of the table, with one-sentence justifications, in the pull request that carries the tests.

KEY RULES:
- Numbers, never "current" without reading the manifest; tests execute real code.

REFERENCE FILES:
- .github/coverage-manifest/Encina.Messaging.json
- docs/testing/coverage-measurement-methodology.md
```

</details>

### Phase 8: Documentation and finalization (in PR 12)

<details>
<summary><strong>Tasks</strong></summary>

1. XML docs with an `<example>` on every public type; the scheduling feature page (what the snapshot answers and does not, the gauge option and its one-interval staleness, the endpoint and its policy) and the observability guide cross-reference through `docs-writer`; `changelog.d/159-scheduling-analytics.added.md` through `mechanical-fixer`. Files `PublicAPI.Unshipped.txt` in each package.
2. The deferred execution-history feature (Design Choice 1, option B) is recorded in Next Steps; no issue file is written.
3. Build verification (`dotnet build --configuration Release`, zero warnings) and the tests of every flag.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```
CONTEXT:
- Documentation pages go through docs-writer; changelog fragments and PublicAPI lines through mechanical-fixer.

TASK:
Finish the documentation, the fragment and the verification.

KEY RULES:
- Never type coverage figures by hand in documentation.
- No mention of any private client application.

REFERENCE FILES:
- changelog.d/README.md
- .claude/skills/encina-docs/SKILL.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Requirement | Key Details |
|----------|-------------|-------------|
| `AGENTS.md` section 3 | ROP, async database calls, background loops, fail-closed gates, no `EncinaError.Message` in output | `Either` snapshot, refresher loop, endpoint policy |
| `AGENTS.md` section 5 | Database features on all 10 providers | Phase 2 |
| SPEC-002 AC-043, AC-044 | Tenant-filtered queries; tenant tag when tenancy is on | Phase 6; no tenant tag on global gauges |
| ADR-054 D3, D6 | Package-named meter; Encina-level spans on every store | `Encina.Messaging` meter; decorator span |
| ADR-062 | `ErrorCategory` per error code | `scheduling.statistics_failed` is `Unavailable` (passed at the call site, the endpoint answers 503); the tenant denial is `InvalidRequest` |
| ADR-052 | Background loops and cancellation | Refresher |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|----------------------|
| `SchedulingHealthCheck`, `SchedulingHealthCheckOptions` | `src/Encina.Messaging/Health/SchedulingHealthCheck.cs` | Thresholds reused for the snapshot status; check unchanged |
| `ScheduledMessageStore*` (8) | provider packages `Scheduling/` | Receive the member |
| `MapDPIAEndpoints` | `src/Encina.AspNetCore/DPIAEndpointExtensions.cs` | Fail-closed endpoint model |
| `IDeadLetterStore.GetCountAsync` with source `Scheduling` | `src/Encina.Messaging/DeadLetter/` | Not used: the row-based count is exact and replay-independent |
| `RegisterScheduling` | `src/Encina.Messaging/MessagingServiceCollectionExtensions.cs` | Registration |
| Shared integration collections | `tests/Encina.IntegrationTests/` | Per-provider tests |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Messaging` (`MessagingScheduling`, existing 2300-2399) | **2307-2308** | Free on `origin/main` (used: 2300-2306 and 2320-2325; the registry holds 2326-2389); packed after 2306 without a gap, only the two events the refresher needs; 2309-2319 stay free |
| `Encina.AspNetCore` (existing 200-249) | **241-242** | After #3994's 233-240; re-grep the open plan branches when work starts |
| New range | none | No block is needed |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Production | 24 | 14 new (2 records, 3 snapshot and service types, cache, refresher, gauges, 2 log classes, endpoint, `ISchedulingAnalytics`), 10 modified (8 stores, interface, options and registration counted in the 10); 38 touches |
| Tests | 38 | Per provider 1 integration plus 1 guard edit (16) and the tenant cases of PR 13 (10 edits), core unit, guard, property, DI, endpoint, fake, decorator, contract |
| SQL scripts | 0 | No schema change |
| Coverage manifests | 14 | One per touched package per pull request |
| PublicAPI files | 14 | Per pull request that adds or edits public API |
| Registry and documentation | 3 | Two registry rows, the page, the guide cross-reference, the fragment (counted in PR 11c and 12) |
| **Total touches** | **about 124** | In 16 pull requests, one by one: 6, 5, 5, 5, 5, 5, 5, 7, 5 (PR 1 to 9), 11 (PR 10), 10, 10, 7 (PR 11a to 11c), 11 (PR 12), 9, 6, 12 (PR 13a to 13c); none above 15 files |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
PROJECT CONTEXT:
- Encina is a pre-1.0 .NET 10 / C# 14 library; Either<EncinaError, T>; no compatibility layers; time from TimeProvider; database features on all 10 providers.
- Issue #159: analytics for scheduled messages; only state-derived statistics are possible without history.

IMPLEMENTATION OVERVIEW:
PR1 records; PR2-9 one provider each (concrete GetStatisticsAsync, integration tests); PR10 interface, decorator, fake; PR11a snapshot service; PR11b options, cache, gauges, AddMeter; PR11c refresher, log, EventIds; PR12 fail-closed endpoint and docs; PR13a ADO.NET and the query's tenant, PR13b Dapper, PR13c EF Core, MongoDB and the analytics service (tenant predicate, after #739).

KEY PATTERNS:
- Implementers first, interface last. Optional parameters last.
- Refresher: singleton plus IServiceScopeFactory, catches per cycle, logs error codes only.
- Endpoint: policy required at mapping time; counts and type names only in the body.

REFERENCE FILES:
- src/Encina.Messaging/Scheduling/IScheduledMessageStore.cs
- src/Encina.ADO.SqlServer/Scheduling/ScheduledMessageStoreADO.cs
- src/Encina.Messaging/MessagingServiceCollectionExtensions.cs
- src/Encina.AspNetCore/DPIAEndpointExtensions.cs
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ✅ | The gauge cache (Phase 4); caching the endpoint result is not planned (a query per call is acceptable for an operator endpoint) |
| 2 | OpenTelemetry | ✅ | Four gauges and the decorator span; execution instruments are #152's |
| 3 | Structured Logging | ✅ | Events 2307-2308 and 241-242, error codes only |
| 4 | Health Checks | ✅ | The snapshot status reuses the existing check's thresholds; `SchedulingHealthCheck` itself is unchanged |
| 5 | Validation | ✅ | Phase 1: query and window bounds; the endpoint rejects a bad window |
| 6 | Resilience | ❌ | Local database queries; no external endpoint |
| 7 | Distributed Locks | ❌ | Read-only; each instance may poll its own cache |
| 8 | Transactions | ❌ | Read-only queries; no commit owner is involved |
| 9 | Idempotency | ❌ | Queries are idempotent by nature |
| 10 | Multi-Tenancy | ✅ | Phase 6 (after #739): strict tenant predicate and the resolver |
| 11 | Module Isolation | ⏭️ | Module-scoped statistics belong with #747's column; recorded there |
| 12 | Audit Trail | ⏭️ | Read audit of an operator endpoint is covered by ADR-036's read audit if the host enables it; the endpoint adds no audit of its own |

---

## Prerequisites & Dependencies

| Item | Relation | Note |
|------|----------|------|
| #152 | Sibling | Defers the pending gauge here; decorator span naming |
| #2229 | Hard for PR 12 | Shared policy helper in `Encina.AspNetCore` |
| #3994 | Sibling | May mount the endpoint in its group |
| #739, #1257 | Hard for PRs 13a to 13c | Tenant column, resolver |
| #1251 | Sibling | Row claiming does not change the state definitions |
| #3389 | Not a dependency | Blocks #160 only |

---

## Next Steps

1. Review this plan; the Dilemmas (choices 1, 3, 4, 5, 6 and 7: what the analytics can answer, where the query lives, the gauge source, the endpoint, the tenant scope and the build order) are answered by the maintainer when work starts; the answers are then recorded in a `## Maintainer Decisions` section added at that time.
2. Implement PR 1 to PR 12 in order (PR 2 to 9 in any order among themselves, 11a to 11c in order); PRs 13a to 13c after #739 and #1257, in order.
3. Link the plan from #159.

### Deferred: scheduled-message execution history

A later feature, not part of this plan's pull requests and not a separate issue (the maintainer rule keeps a `[FEATURE]` follow-up inside the plan that motivates it; it gets its own issue and plan only if the maintainer takes Design Choice 1, option B). It persists one record per scheduled-message run (start, duration, outcome `executed`, `failed` or `dead_lettered`, error code, tenant id after #739) so the snapshot can answer average processing time, p95, a skipped count and failure trends cluster-wide.

- Shape: a `ScheduledMessageExecutions` table on the 10 providers written by the orchestrator after each dispatch through a new store member, `Task<Either<EncinaError, Unit>> AddExecutionAsync(ScheduledMessageExecution execution, CancellationToken cancellationToken = default)`, and an aggregate member `GetTimingStatisticsAsync(SchedulingStatisticsQuery query, CancellationToken cancellationToken = default)` returning `SchedulingTimingStatistics`, with a retention purge.
- Questions it would carry as Dilemmas: the write cost (a second write per dispatch in the caller's commit, or best effort), percentiles in the database or in process (MySQL has no percentile function; an in-process computation over a bounded window is portable) and the retention default.
- Cross-cutting: the cluster-wide purge needs one runner (a distributed lock); the per-instance histograms of #152 stay; #749 audits processing and this table is operational history; the tenant column follows #739.
