# Implementation Plan: Resilience policies for the Marten event-store operations of `Encina.Compliance.DPIA`

> **Issue**: [#764](https://github.com/dlrivada/Encina/issues/764)
> **Type**: Feature
> **Related**: #742 (owns `IProviderResilience` and the provider-keyed pipeline this plan consumes), ADR-055 (resilience family), ADR-019 and ADR-027 (Marten for event-sourced compliance), ADR-029 (error classification), ADR-062 (error categories), ADR-054 (telemetry), #1965 (the `Encina.Compliance.AIAct` sibling, consumes the shared decorators), #847 (AIAct on Marten), #763 (DPIA reminder lock), #728 (resilience telemetry), #326 (Marten tenancy), #3787 (closed: failed save ejects the session)
> **Complexity**: Medium (one executor consumer, two decorators, one classifier, one DPIA fix; no persistence of its own)
> **Estimated Scope**: ~650 lines of production code + ~1,900 lines of tests + ~250 lines of documentation, in three pull requests of at most 14 files and 1,000 lines each

---

## Summary

### What exists today (read on 2026-11-25, `origin/main` `3bccacc1c`)

- **The premise of the original issue is gone.** `IDPIAStore` and `IDPIAAuditStore` do not exist. `Encina.Compliance.DPIA` is event sourced on Marten: `DefaultDPIAService` takes `IAggregateRepository<DPIAAggregate>` and `IReadModelRepository<DPIAReadModel>` (`src/Encina.Compliance.DPIA/Services/DefaultDPIAService.cs:35-36`), and `AddDPIAAggregates()` registers `AddAggregateRepository<DPIAAggregate>()` and `AddProjection<DPIAProjection, DPIAReadModel>()` (`DPIAMartenExtensions.cs:49-55`). There is no InMemory DPIA store and none is planned (`AGENTS.md` section 3).
- **No resilience exists on the Marten path.** `Encina.Marten` references only `Encina`, `Encina.DomainModeling` and `Encina.Messaging` (`Encina.Marten.csproj:29-31`); `git grep -n -i -E "Polly|Resilien|Retry" -- src/Encina.Marten` is empty. The database circuit breaker (`DatabaseCircuitBreakerPipelineBehavior`, `src/Encina.Polly/Behaviors/DatabaseCircuitBreakerPipelineBehavior.cs:84`) reads `IDatabaseHealthMonitor.IsCircuitOpen`, and the only implementers are `DatabaseHealthMonitorBase` (`Encina.Messaging`) and `MongoDbDatabaseHealthMonitor`; there is none for Marten.
- **The Marten repositories never throw; they return a `Left`.** `MartenAggregateRepository<T>` catches every exception: load as `marten.load_failed` (`MartenAggregateRepository.cs:116`, `:161`), save as `marten.save_failed` (`:257`) or `marten.concurrency_conflict` (`:222`), create as `marten.create_failed` (`:322`) or `marten.stream_already_exists` (`:312`); the exception travels inside the `EncinaError` (`EncinaErrors.FromException`). `MartenReadModelRepository<T>` does the same with `ProjectionErrorCodes.QueryFailed`, `StoreFailed` and `DeleteFailed` (ten `catch (Exception ex)` blocks, `MartenReadModelRepository.cs:61` to `:318`; the projection codes are upper case, `"{Prefix}_QUERY_FAILED"`, unlike the lower-case `marten.*` codes).
- **A failed append is safe to repeat.** `SaveChangesOrEjectAsync` ejects every pending change of the session before rethrowing (`DocumentSessionFlushExtensions.cs:22-35`, #3787), and a failed save does not clear `aggregate.UncommittedEvents` (`ClearUncommittedEvents` runs only after the flush, `MartenAggregateRepository.cs:211`); `SaveAsync` appends with the expected stream version (`:205`), so a second append of the same events cannot duplicate them, and `CreateAsync` starts a stream with an id the caller chose.
- **The repositories turn the caller's cancellation into a `Left`.** `LoadAsync`, `SaveAsync` and `CreateAsync` catch `Exception` with no `OperationCanceledException` branch, so a cancelled token arrives as `marten.load_failed` or `marten.save_failed` (same blocks as above). This is recorded in the issue file `marten-repositories-cancellation-as-left` (read from code, nothing run).
- **`DefaultDPIAService` reports a store failure as "not found".** Every write method maps any `Left` of `LoadAsync` to `DPIAErrors.AssessmentNotFound(assessmentId)` (`DefaultDPIAService.cs:164, 222, 271, 318, 364, 412, 458`) and `GetAssessmentAsync` does the same for the read model (`:503`). A transient Marten failure is therefore reported as `dpia.assessment_not_found`, which hides it from every retry layer and from the caller. `GetAssessmentByRequestTypeAsync` is already correct: it passes the `Left` through (`:550`), and the pipeline behavior and the auto-registration both distinguish the not-found code from a store error (`DPIARequiredPipelineBehavior.cs:249-290`, `DPIAAutoRegistrationHostedService.cs:180-186`).
- **The gate fails closed in Block mode.** A store error in `Block` returns the `Left` and blocks the request; in `Warn` mode it logs and proceeds, the explicit and logged opt-out of the enforcement mode (`DPIARequiredPipelineBehavior.cs:266-290`). The review reminder is a `PeriodicTimer` loop whose cycle catches every failure and keeps running (`DPIAReviewReminderService.cs:77-84`, `:149-156`), so it needs no policy of its own (#763 owns its lock).
- `Encina.Resilience` does **not** exist in `src/`, and ADR-055 R1 replaces `Encina.Polly` and `Encina.Extensions.Resilience` with it. The plan of #742 (on `main`) defines `IProviderResilience.ExecuteAsync<T>(provider, operation, action, ct)` for decorators of this kind; this plan consumes it and does not duplicate it.

### What this plan delivers

1. **PR-1, a fix that needs nothing else**: `DefaultDPIAService` stops reporting a store failure as not found; only `MartenErrorCodes.AggregateNotFound` and `ProjectionErrorCodes.ReadModelNotFound` map to `dpia.assessment_not_found`, every other `Left` is returned unchanged.
2. **PR-2 and PR-3, opt-in resilience for the Marten path**, in `Encina.Marten` so that every Marten-backed compliance module (DPIA, and AIAct through #1965) shares it: `ResilientAggregateRepository<T>` and `ResilientReadModelRepository<T>` (decorators that run each operation through the #742 executor under the provider name `marten`), `MartenErrorClassifier` (tells the retry layer which `marten.*` codes are transient and which are permanent), and `AddEncinaMartenResilience()` (order-independent registration). Nothing changes for an application that does not call it.

Failure semantics: a transient `Left` of a load, query, save or create is retried up to the policy's `MaxAttempts`; a `marten.concurrency_conflict`, `marten.stream_already_exists`, `marten.aggregate_not_found`, a non-transient PostgreSQL error and every projection-apply error is permanent and returned at once; after the last attempt the caller gets the last `Left`, never a pass; an open circuit returns the foundation's coded `Left` (`Unavailable`); the caller's cancellation is never retried and never a `Left`. In `Block` mode the DPIA gate therefore denies the request with a store error instead of a false "assessment required" or a false pass.

- **Affected packages**: `Encina.Compliance.DPIA` (PR-1), `Encina.Marten` (PR-2, PR-3), `Encina.Resilience` (planned, consumed). The issue also ticks `Encina.Polly`: it gets no code (ADR-055 R1 removes it).
- **Provider category**: event sourcing (Marten on PostgreSQL, `AGENTS.md` section 5, "Other categories"); the 10-provider database matrix and the caching, transport, lock and validation categories do not apply.
- **Shared pieces**: this plan owns `ResilientAggregateRepository<T>`, `ResilientReadModelRepository<T>`, `MartenErrorClassifier`, `MartenResilienceOptions`, `AddEncinaMartenResilience()` and EventId 2746; #1965 consumes them and adds only its own wiring test. #742 owns `IProviderResilience`.

---

## Design Choices

<details>
<summary><strong>1. Which mechanism: decorators over the provider executor, a Marten health monitor, or Npgsql retry only</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A Marten `IDatabaseHealthMonitor` so the existing database circuit breaker applies** | Reuses a breaker that exists | The breaker is a request pipeline behavior of `Encina.Polly` (removed by ADR-055 R1); it wraps a whole application request, not the Marten call, so one DPIA read inside a request trips the breaker for unrelated requests; it has no retry; `IDatabaseHealthMonitor` is documented for the 13-provider relational set with pool statistics Marten does not expose |
| **B) Decorators on `IAggregateRepository<T>` and `IReadModelRepository<T>` that run each operation through the #742 executor (retry, breaker, timeout) under the provider name `marten`** | Wraps exactly the Marten call; one options model and one telemetry (ADR-055 R3, R5, R7); the `Left` stays the contract; the same decorators serve AIAct (#1965) | Depends on the foundation and on #742's executor; two public decorators |
| **C) Rely on Npgsql's connection resiliency only** | No code | Npgsql does not retry a failed command; a transient failure surfaces as a `Left` at once and never reaches a retry layer |

### Chosen Option: **B — Decorators over the provider executor** (recommended, pending the maintainer)

### Rationale

- ADR-055 R7 puts provider resilience in decorators over the R2 registry, keyed by provider name; B is that rule applied to the event store. A wins only for an application that already runs the database circuit breaker and wants no retry; C wins for a deployment that accepts a single failed attempt as final.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Where the code lives while `Encina.Resilience` does not exist (shared with #742, #136 and the resilience family)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Wait for the foundation and #742's executor; PR-1 does not need them and ships now** | One implementation in its final home; nothing that ADR-055 R1 deletes | PR-2 and PR-3 are blocked until the foundation and #742 PR-1 merge |
| **B) Build the retry now inside `Encina.Marten` with Polly** | Marten resilience earlier | A second options model and a second pipeline that R2 and R7 reject (option 7b); deleted when the foundation lands |
| **C) PR-2 creates a minimal `Encina.Resilience` project** | Unblocks the plan | The first plan to land defines the foundation's shape for every capability |

### Chosen Option: **A — Wait for the foundation; ship PR-1 now** (recommended, pending the maintainer)

### Rationale

- The question is asked once for the resilience family (the shared Dilemma 1 of #742 and #136). B wins if Marten resilience must ship before the foundation; C wins if the foundation should grow out of this capability.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Which package owns the decorators</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Encina.Marten`, folder `Resilience/`** | Same place as the repositories it wraps; AIAct and every other Marten module opt in with one call; the decorator needs `internal` access to nothing, so it stays thin | `Encina.Marten` takes a `ProjectReference` to `Encina.Resilience` even for applications that never opt in (a compile-time edge, no runtime cost) |
| **B) A new package `Encina.Marten.Resilience`** | Applications without resilience carry no reference | One more package with README, manifest and tests; ADR-055 R7 puts each transport's decorator inside the transport package, not a satellite |
| **C) Inside each compliance module** | No change to `Encina.Marten` | The same decorator copied for DPIA, AIAct and every later module |

### Chosen Option: **A — `Encina.Marten`** (recommended, pending the maintainer)

### Rationale

- A matches the precedent of #742 (decorator in the provider package, executor in `Encina.Resilience`). B wins if the maintainer wants `Encina.Marten` free of any resilience reference; C is listed for completeness and loses in every scenario because the code would be duplicated.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. What is wrapped: the aggregate repository only, or the read model reads too</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Both: every `IAggregateRepository<T>` operation, and the reads of `IReadModelRepository<T>` (`GetByIdAsync`, `GetByIdsAsync`, `QueryAsync`, `ExistsAsync`, both `CountAsync`); the projection writes (`StoreAsync`, `StoreManyAsync`, `DeleteAsync`, `DeleteAllAsync`) pass through unwrapped** | The DPIA gate reads the read model, not the aggregate: `GetAssessmentByRequestTypeAsync` calls `_readModelRepository.QueryAsync` (`DefaultDPIAService.cs:532`), so protecting only the aggregate leaves the hot path of every request unprotected | Retries add latency to a gate that runs on every guarded request; two decorators |
| **B) The aggregate repository only** | Smaller change; the aggregate is the audit-relevant write | The request path (read model) and the reminder (`GetExpiredAssessmentsAsync`, `:569`) stay without retry |

### Chosen Option: **A — Both, with a switch** (recommended, pending the maintainer)

### Rationale

- `MartenResilienceOptions.WrapReadModels` (default `true`) lets an application keep the read path unwrapped when gate latency matters more than availability; the projection writes are unwrapped because they run after the commit inside the repository itself (`InlineProjectionRelay`), where a retry of the whole save is the wrong unit. B wins for an application that already caches the read model so aggressively that a read-model failure never reaches the gate.

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Retrying writes: version-checked retry, or reads only</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Retry `SaveAsync` and `CreateAsync` too. The retry cannot duplicate events: the append carries the expected stream version and a create names its stream id. When an attempt after the first ends in `marten.concurrency_conflict` (save) or `marten.stream_already_exists` (create), the first attempt may have committed before the connection failed; the decorator returns that `Left` unchanged, logs EventId 2746 (operation, code, attempt count) and does not guess** | A transient failure while saving an assessment (Article 35(7) records) is not lost; no duplicate event is possible | The caller of an ambiguous outcome sees a conflict although its events are stored; it reloads and finds them (a DPIA caller does) |
| **B) Retry reads only; a write returns its first `Left`** | No ambiguity | Every transient write failure reaches the user, which is the problem of the issue |
| **C) After an ambiguous outcome, load the stream and treat "events already stored" as success** | Hides the ambiguity | A stream written by another process looks like our write; needs an event-content comparison; the decorator cannot know the aggregate's identity rules (it is generic for every module) |

### Chosen Option: **A — Version-checked retry, ambiguity logged and returned** (recommended, pending the maintainer)

### Rationale

- A keeps the repository's own contract (the conflict `Left` is how a stale writer is told to reload) and never reports a success it did not see. DPIA ids come from `Guid.NewGuid()` before the call (`DefaultDPIAService.cs:95`) and are stable across attempts. B wins if the maintainer prefers that a write is never repeated by infrastructure; C wins if a module owns its identity rules and supplies a comparer (a later extension, not this plan).

**Class:** Dilemma

</details>

<details>
<summary><strong>6. Which failures are transient: a classifier for the `marten.*` codes</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `MartenErrorClassifier : IErrorClassifier` that wraps the registered classifier: `marten.concurrency_conflict`, `marten.stream_already_exists`, `marten.aggregate_not_found`, `marten.no_events_to_create`, `marten.invalid_query`, `marten.event_not_found` and `ProjectionErrorCodes.ApplyFailed` are `Permanent`; for `marten.load_failed`, `save_failed`, `create_failed` and the projection query code, when the exception chain holds an `NpgsqlException` (a `PostgresException` is one) the answer is its `IsTransient` (`Transient` or `Permanent`); everything else, including `TimeoutException` and `IOException`, is delegated to the wrapped classifier** | `DefaultErrorClassifier` treats an unmatched code as `Unknown`, which counts as transient (`IErrorClassifier.cs:103`), and it does not read `NpgsqlException.IsTransient` (it matches `TimeoutException`, `IOException` and the inner chain only), so without this wrapper a stale concurrency conflict or a PostgreSQL constraint error would be retried and fail the same way each time; the permanent codes are correct for every other retry layer too (ADR-029) | Changes classification process-wide for `marten.*` codes (which is the intent); needs Npgsql's `IsTransient` (verify against Npgsql 10.0.2 in the PR) |
| **B) A per-provider classifier on `ProviderResilienceOptions.ForProvider("marten", ...)`** | Scope limited to this decorator | Needs an additive member on #742's `ProviderPolicy`; the mediator retry layer would still misclassify the same codes |

### Chosen Option: **A — A classifier wrapped around the registered one** (recommended, pending the maintainer)

### Rationale

- The classification of `marten.*` codes belongs to the package that defines them (`MartenErrorCodes`), and ADR-029 makes `IErrorClassifier` the single contract of every retry layer. The wrapper resolves the inner classifier lazily so registration order does not matter. B wins when the maintainer wants no process-wide effect.

**Class:** Dilemma

</details>

<details>
<summary><strong>7. Registration: an order-independent factory, or an explicit call per aggregate</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `AddAggregateRepository<T>()`, `AddProjection<,>()`, `AddSnapshotableAggregate<T>()` and the open-generic registrations register the concrete repository plus a factory for the interface; the factory wraps it in the decorator when `AddEncinaMartenResilience()` registered the marker, at provider build time, in any registration order** | One call turns resilience on for every Marten module, in any order; satisfies the registration-completeness rule (a DI test with `ValidateOnBuild` and `ValidateScopes`) | Edits three registration sites of `Encina.Marten` |
| **B) `AddEncinaMartenResilience<DPIAAggregate>()` per aggregate, decorating the registered descriptor** | Explicit per module | Order dependent (it fails or does nothing if called before the repository is registered); one call per aggregate and per read model |

### Chosen Option: **A — Marker plus factory** (recommended, pending the maintainer)

### Rationale

- An application that registers its own `IAggregateRepository<T>` after Encina's still wins (the factory exists only inside Encina's own registration). B wins when the maintainer wants resilience on one aggregate type only; with A the same effect is `ForProvider` options per operation, not per type.

**Class:** Dilemma

</details>

<details>
<summary><strong>8. What a caller's cancellation becomes</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Propagate: when the caller's token is cancelled, the decorator throws `OperationCanceledException`, never retries it and never counts it against the breaker** | The code that already treats cancellation as non-error keeps working: `DefaultDPIAService` rethrows it (`catch ... when (ex is not OperationCanceledException)`), the reminder loop ends cleanly (`DPIAReviewReminderService.cs:149`); matches #742 (executor throws on a cancelled caller token) | A host that wants a coded result needs its own catch |
| **B) Return a `Left` with a `*.cancelled` code (ADR-062 R5, category `Cancelled`)** | No exception | Counts a cancellation as an error in metrics and in the DPIA catch blocks, which then log it as a store failure |

### Chosen Option: **A — Propagate** (recommended, pending the maintainer)

### Rationale

- The repositories today turn a cancellation into `marten.load_failed` (see Summary); the decorator therefore checks the caller's token after a `Left` and throws, so cancellation is never retried even though the inner repository reports it as a failure. B wins for a host that maps cancellation to HTTP 499 through a result.

**Class:** Dilemma

</details>

<details>
<summary><strong>9. What a circuit breaker is keyed on</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One key, the provider name `marten`** | The dependency (the PostgreSQL behind the Marten store) is one; one breaker protects it whichever module calls; bounded keys | A tenant-specific database (#326 explores per-tenant stores) would open the circuit for other tenants |
| **B) One key per aggregate or read-model type (`marten:DPIAAggregate`)** | A slow projection table does not stop the aggregates | Keys multiply with modules; the same database failing opens many breakers in turn |
| **C) One key per (provider, tenant)** | A bad tenant database is isolated | Unbounded key growth with tenants; needs eviction and an injective tuple key (a missing tenant is its own key, never the reserved `default` segment) |

### Chosen Option: **A — Per provider name** (recommended, pending the maintainer)

### Rationale

- `Encina.Marten` has no tenant isolation today (the plan of #326 adds it as conjoined tenancy in one database), so one database is one dependency. C wins if #326 chooses a database per tenant; B wins when one table is known to fail alone.

**Class:** Dilemma

</details>

<details>
<summary><strong>10. A store failure is no longer reported as "assessment not found"</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Map only `MartenErrorCodes.AggregateNotFound` (aggregate) and `ProjectionErrorCodes.ReadModelNotFound` (read model) to `dpia.assessment_not_found`; return every other `Left` unchanged, the form `GetAssessmentByRequestTypeAsync` already uses (`:550`)** | A store failure is reported as a store failure and keeps its code, so the retry layer and the caller can tell it from a missing assessment | The callers that match `dpia.assessment_not_found` (`DPIAEndpointExtensions.cs:232, 393, 441`, `DPIAAutoRegistrationHostedService.cs:183`, `DPIARequiredPipelineBehavior.cs:253`, found by `git grep AssessmentNotFoundCode -- src`) now see it only for a real absence: an endpoint answers the store error (500, ADR-062 R4) instead of a false 404; the PR re-runs the grep and the endpoint tests |
| **B) Keep mapping every `Left` to not found** | No change | ADR-062 R1 and R4: not found is a missing runtime entity, a store failure is `Internal`; the retry layer never sees the failure |

### Chosen Option: **A — Map only the not-found codes**

### Rationale

- Eight sites in one file apply one rule; the change is one private helper called from each. It holds whatever the other choices decide, because the `Left` it passes through is the repository's own.

**Class:** Trivial
**Settled by:** ADR-062 R1 and R4 (not found is a missing runtime entity; store failures are `Internal`); `DefaultDPIAService.cs:550` (the pass-through form already used in the same file)

</details>

---

## Implementation Phases

Three pull requests, in this order: **PR-1** Phase 1 (no prerequisite, can merge first); **PR-2** Phases 2 and 5 (Phase 5 is only the log method); **PR-3** Phases 3, 4 and 7. PR-2 and PR-3 need the foundation and #742's executor merged (choice 2 option A); under B or C the split is recounted. Each pull request is at most 15 files and 1,500 lines.

### Phase 1: DPIA reports store failures as store failures

<details>
<summary>Tasks</summary>

1. `src/Encina.Compliance.DPIA/Services/DefaultDPIAService.cs` (modified): add `private static EncinaError LoadFailure(EncinaError error, Guid assessmentId)` returning `DPIAErrors.AssessmentNotFound(assessmentId)` when `error.GetCode()` is `MartenErrorCodes.AggregateNotFound` or `ProjectionErrorCodes.ReadModelNotFound`, and `error` otherwise; replace the eight `Left: _ => DPIAErrors.AssessmentNotFound(assessmentId)` lambdas (lines 164, 222, 271, 318, 364, 412, 458, 503) with `Left: error => LoadFailure(error, assessmentId)`. No constructor or public member changes.
2. `tests/Encina.UnitTests/Compliance/DPIA/Services/DefaultDPIAServiceStoreFailureTests.cs` (new): for each of the eight methods, one test where the substituted `IAggregateRepository` (or `IReadModelRepository`) returns `marten.aggregate_not_found` and the method returns `dpia.assessment_not_found`, and one where it returns `marten.load_failed` and the method returns that `Left` unchanged.
3. `tests/Encina.IntegrationTests/Compliance/DPIA/DPIAAggregateIntegrationTests.cs` (modified, collection `MartenCollection`): one test that a missing id through real Marten still returns `dpia.assessment_not_found`. Before the PR, re-run `git grep AssessmentNotFoundCode -- src tests` and extend the endpoint tests of `Encina.AspNetCore` if one asserts a 404 for a store failure.
4. `.github/coverage-manifest/Encina.Compliance.DPIA.json` (`mechanical-fixer`): `Services/DefaultDPIAService.cs` unit 50 to 65; guard stays 4.
5. `changelog.d/764-dpia-store-failure-not-reported-as-not-found.fixed.md` (`mechanical-fixer`).

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT: Encina issue #764, plan docs/plans/dpia-marten-resilience-implementation-plan-764.md, Phase 1. DefaultDPIAService maps any Left of LoadAsync and GetByIdAsync to dpia.assessment_not_found (lines 164, 222, 271, 318, 364, 412, 458, 503), hiding store failures.
TASK: Add the private helper LoadFailure and use it at the eight sites, with the unit and integration tests of the Phase 1 Tasks.
KEY RULES: only MartenErrorCodes.AggregateNotFound and ProjectionErrorCodes.ReadModelNotFound map to not found; every other Left is returned unchanged; no exception message in logs; the changed methods must stay at CRAP <= 10 (each Left branch tested); tests mock IAggregateRepository with NSubstitute, the integration test uses MartenCollection; Shouldly through Encina.Testing.Shouldly.
REFERENCE FILES: src/Encina.Compliance.DPIA/Services/DefaultDPIAService.cs, src/Encina.Marten/MartenErrorCodes.cs, src/Encina.Marten/Projections/ProjectionErrorCodes.cs, tests/Encina.UnitTests/Compliance/DPIA/Services/DefaultDPIAServiceTests.cs
```

</details>

### Phase 2: The Marten classifier and the aggregate repository decorator

<details>
<summary>Tasks</summary>

1. `src/Encina.Marten/Encina.Marten.csproj` (modified): `ProjectReference` to `..\Encina.Resilience\Encina.Resilience.csproj`.
2. `src/Encina.Marten/Resilience/MartenErrorClassifier.cs` (public sealed, `IErrorClassifier`): constructor `(IErrorClassifier inner)`; `Classify(EncinaError error, Exception? exception)` per choice 6; walks `exception` and its `InnerException` chain to a fixed depth of 5 looking for an `NpgsqlException`; no allocation on the permanent-code path; every other case is delegated to `inner`.
3. `src/Encina.Marten/Resilience/ResilientAggregateRepository.cs` (public sealed, `IAggregateRepository<TAggregate>`, `where TAggregate : class, IAggregate, new()`): constructor `(IAggregateRepository<TAggregate> inner, IProviderResilience resilience, ILogger<ResilientAggregateRepository<TAggregate>> logger)`, all null-guarded; each of the four methods calls `resilience.ExecuteAsync("marten", "<operation>", ct => new ValueTask<...>(inner.Method(..., ct)), cancellationToken)`; the provider name is the constant `MartenResilience.ProviderName`; after the executor returns a `Left` and `cancellationToken.IsCancellationRequested` is true, throw `OperationCanceledException` (choice 8); a per-call counter of attempts drives the EventId 2746 log of choice 5.
4. `src/Encina.Marten/Resilience/MartenResilienceServiceCollectionExtensions.cs` (public static): `AddEncinaMartenResilience(this IServiceCollection services)` registers the marker, `TryAddEnumerable` of the classifier decoration (resolving the previous `IErrorClassifier` descriptor lazily, never capturing a scoped service in a singleton), and `AddEncinaProviderResilience()` from #742 with `TryAdd`.
5. `src/Encina.Marten/ServiceCollectionExtensions.cs` (modified): the open-generic registration at line 52, `AddAggregateRepository<T>` (line 143) and `AddSnapshotableAggregate<T>` (line 372) register the concrete repository and a factory for `IAggregateRepository<T>` that wraps it when the marker is present (choice 7); unchanged behavior without the marker.
6. `src/Encina.Marten/Log.cs` (modified): EventId 2746 `RetriedWriteEndedInConflict(operation, errorCode, attempts)`, warning level, `[LoggerMessage]`, error code and counts only.
7. Tests (new): `tests/Encina.UnitTests/Marten/Resilience/ResilientAggregateRepositoryTests.cs`, `MartenErrorClassifierTests.cs`, `MartenResilienceRegistrationTests.cs` (provider built with `ValidateOnBuild` and `ValidateScopes`, resilience registered before and after the repository, application override wins); `tests/Encina.GuardTests/Marten/Resilience/MartenResilienceGuardTests.cs`; `tests/Encina.ContractTests/Marten/Core/ResilientAggregateRepositoryContractTests.cs` (the decorator over a fake inner honours the `IAggregateRepository` contract); `tests/Encina.IntegrationTests/Infrastructure/Marten/ResilientAggregateRepositoryIntegrationTests.cs` (collection `MartenCollection`): a fault-injecting wrapper over the real repository fails the first attempts with a transient `Left`, and a second case commits through the real repository and then returns a transient `Left` (a lost acknowledgement), asserting the stream holds each event once and the caller sees the conflict.
8. `.github/coverage-manifest/Encina.Marten.json` and `changelog.d/764-marten-resilience.added.md` (`mechanical-fixer`). `Encina.Marten` has no `PublicAPI` files (none exist on `origin/main`).

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT: Issue #764, plan docs/plans/dpia-marten-resilience-implementation-plan-764.md, Phase 2. The foundation (Encina.Resilience) and IProviderResilience of #742 are merged; if missing, stop and report. MartenAggregateRepository never throws: it returns Left with the exception attached, and it reports a cancellation as marten.load_failed or marten.save_failed.
TASK: Create MartenErrorClassifier, ResilientAggregateRepository, AddEncinaMartenResilience and the factory registrations, with the tests of the Phase 2 Tasks.
KEY RULES: opt-in, nothing changes without AddEncinaMartenResilience; a caller's cancellation throws OperationCanceledException and is never retried or counted by the breaker; a Left is never converted into an exception to drive the retry; no EncinaError.Message in logs, only the error code (ForLogging for exceptions); constant messages; no scoped service captured by a singleton; time only through TimeProvider (the executor owns it); every public constructor argument guarded; a DI test with ValidateOnBuild and ValidateScopes; unit tests mock IAggregateRepository (no InMemory store), integration tests use Marten on PostgreSQL through MartenCollection; CRAP <= 10 on every added method.
REFERENCE FILES: src/Encina.Marten/MartenAggregateRepository.cs, src/Encina.Marten/DocumentSessionFlushExtensions.cs, src/Encina.Marten/ServiceCollectionExtensions.cs, src/Encina/Recoverability/IErrorClassifier.cs, docs/plans/transport-resilience-implementation-plan-742.md, tests/Encina.IntegrationTests/Compliance/DPIA/DPIAAggregateIntegrationTests.cs
```

</details>

### Phase 3: The read model decorator and its options

<details>
<summary>Tasks</summary>

1. `src/Encina.Marten/Resilience/ResilientReadModelRepository.cs` (public sealed, `IReadModelRepository<TReadModel>`): constructor `(IReadModelRepository<TReadModel> inner, IProviderResilience resilience)`, null-guarded; the reads of choice 4 go through `resilience.ExecuteAsync("marten", ...)`; the four write methods call the inner repository directly; same cancellation rule as Phase 2.
2. `src/Encina.Marten/Resilience/MartenResilienceOptions.cs` (public sealed): `bool WrapReadModels { get; set; } = true`; `AddEncinaMartenResilience(Action<MartenResilienceOptions>? configure = null)` binds it (Phase 2's extension is modified, no second method).
3. `src/Encina.Marten/ServiceCollectionExtensions.cs` (modified): the `AddProjection<,>` registration (lines 224-227) and the open-generic `IReadModelRepository<>` registration (line 187) get the same factory.
4. Tests (new): `tests/Encina.UnitTests/Marten/Resilience/ResilientReadModelRepositoryTests.cs`, `tests/Encina.ContractTests/Marten/Core/ResilientReadModelRepositoryContractTests.cs`; modified: `MartenResilienceRegistrationTests.cs` (both repositories, `WrapReadModels` off), `MartenResilienceGuardTests.cs`.
5. `.github/coverage-manifest/Encina.Marten.json` (`mechanical-fixer`).

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT: Issue #764, plan docs/plans/dpia-marten-resilience-implementation-plan-764.md, Phase 3. Phase 2 is merged: the classifier, the aggregate decorator, the marker and the factory registrations exist.
TASK: Add ResilientReadModelRepository and MartenResilienceOptions and extend the registrations and tests.
KEY RULES: only the reads are wrapped (choice 4); WrapReadModels defaults to true; the writes of the projections pass through unwrapped; same cancellation and logging rules as Phase 2; no new public method name next to AddEncinaMartenResilience.
REFERENCE FILES: src/Encina.Marten/Projections/IReadModelRepository.cs, src/Encina.Marten/Projections/MartenReadModelRepository.cs, src/Encina.Marten/Resilience/ResilientAggregateRepository.cs
```

</details>

### Phase 4: Cross-cutting integration (DPIA end to end)

<details>
<summary>Tasks</summary>

1. `tests/Encina.IntegrationTests/Compliance/DPIA/DPIAResilienceIntegrationTests.cs` (new, collection `MartenCollection`, `[Trait("Category", "Integration")]`): `AddEncinaDPIA` plus `AddDPIAAggregates()` plus `AddEncinaMartenResilience()` over real Marten with a fault-injecting wrapper at the repository seam: (a) a transient read-model failure in `Block` mode is retried and the request passes; (b) after the last attempt the gate returns the store error, not `dpia.assessment_required`; (c) a transient failure while creating an assessment is retried and the assessment exists once; (d) in `Warn` mode a store failure proceeds with the logged warning, unchanged.
2. No change to `DPIAMartenExtensions.cs` or `ServiceCollectionExtensions.cs` of the DPIA package: `AddDPIAAggregates()` already goes through `AddAggregateRepository` and `AddProjection`, which Phase 2 and 3 make resilience aware. Its manifest rows have no per-file targets today (`DPIAMartenExtensions.cs` has no `targets` row); the `ServiceCollectionExtensions.cs` row (unit 89, guard 2) is unchanged.
3. `DPIAHealthCheck` needs no change: it already reports `Unhealthy` when the service returns a store `Left` (`DPIAHealthCheck.cs:131-154`), which now includes an open circuit.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT: Issue #764, plan docs/plans/dpia-marten-resilience-implementation-plan-764.md, Phase 4. Phases 1 to 3 are merged.
TASK: Write the DPIA end-to-end integration test of the Phase 4 Tasks.
KEY RULES: collection fixtures only (MartenCollection, never IClassFixture); a fault injector at the repository seam, not a stopped container (the container belongs to the collection); FakeTimeProvider, never Thread.Sleep; assert codes, never messages; Block mode denies on a store failure, Warn mode proceeds.
REFERENCE FILES: tests/Encina.IntegrationTests/Compliance/DPIA/DPIAPipelineIntegrationTests.cs, src/Encina.Compliance.DPIA/DPIARequiredPipelineBehavior.cs
```

</details>

### Phase 5: Observability

<details>
<summary>Tasks</summary>

1. The retry, breaker and timeout telemetry is the executor's (#742 and #728): Polly's instruments enriched with provider name, operation, error code and, only when multi-tenancy is on, the tenant tag (ADR-054 D4); this plan creates no instrument.
2. EventId 2746 in `src/Encina.Marten/Log.cs` (Phase 2 task 6), inside the registered range `Marten` 2600-2799 (ids 2664-2700 are used on `main`; 2701-2745 are claimed by other plans in `artifacts/decisions/plan-batch/eventid-blocks.md`); the assembly is already mapped in `EncinaEventIdAllocationTests.cs`.
3. The span of the request gets no new attribute: no assessment id, no aggregate id, no request type of the guarded request, no error message (ADR-054 settled points; GDPR Art. 5(1)(c)).

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT: Issue #764, plan docs/plans/dpia-marten-resilience-implementation-plan-764.md, Phase 5 (part of PR-2).
TASK: Add the EventId 2746 log method and its test.
KEY RULES: [LoggerMessage] source generator; only the error code, the operation name and the counts; the exception through ForLogging(); 2746 is inside the registered Marten range; re-check 2746 against main before the PR.
REFERENCE FILES: src/Encina.Marten/Log.cs, src/Encina/Diagnostics/EventIdRanges.cs, docs/engineering/eventid-block-registry.md
```

</details>

### Phase 6: Testing

<details>
<summary>Tasks</summary>

1. Unit: the decorators with a fake `IProviderResilience` and NSubstitute inners: forwarding of arguments, the provider name, retry of a transient `Left`, no retry of a permanent code, the last `Left` after exhaustion, the attempt counter and the EventId 2746 case, cancellation throws and is not retried; the classifier over a table of every `MartenErrorCodes` and `ProjectionErrorCodes` constant, with transient and non-transient exception chains; the registration tests of Phase 2.
2. Guard (`tests/Encina.GuardTests/Marten/Resilience/`): the constructors of both decorators and of `MartenErrorClassifier` (null of each argument), `AddEncinaMartenResilience(null)`, and a null aggregate in `SaveAsync` and `CreateAsync`.
3. Contract (`tests/Encina.ContractTests/Marten/Core/`): each decorator over a fake inner honours the interface contract of the repository it wraps; runs real code.
4. Property: none; there is no complex invariant (the one invariant, no duplicate event, is asserted by the integration test against real Marten).
5. Integration: `ResilientAggregateRepositoryIntegrationTests` (Phase 2) and `DPIAResilienceIntegrationTests` (Phase 4), collection `MartenCollection`.
6. Load and benchmark: justification files (`tests/Encina.LoadTests/Marten/` and `tests/Encina.BenchmarkTests/`), not new code paths of concurrency; the added cost of one registry lookup per call is the executor's.

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT: Issue #764, plan docs/plans/dpia-marten-resilience-implementation-plan-764.md, Phase 6.
TASK: Complete the tests of the Phase 6 Tasks that the earlier phases did not write.
KEY RULES: tests execute real package code; no reflection-only tests; FakeTimeProvider, never Thread.Sleep; Shouldly through Encina.Testing.Shouldly; guard tests for every public constructor and method with parameters; per-flag targets as in the Research tables.
REFERENCE FILES: .github/coverage-manifest/Encina.Marten.json, tests/Encina.GuardTests/Marten/, tests/Encina.ContractTests/Marten/Core/
```

</details>

### Phase 7: Documentation and finalization

<details>
<summary>Tasks</summary>

1. XML documentation with an example on every new public type and on `AddEncinaMartenResilience`; the remarks state the ambiguity of choice 5 and the cancellation rule of choice 8.
2. `docs-writer`: `src/Encina.Compliance.DPIA/README.md` (a resilience section: what is retried, what the gate does in `Block` and `Warn` mode on a store failure), `docs/features/dpia.md` (the same, linked), `docs/features/marten-resilience.md` (new), `src/Encina.Marten/README.md` (new: the package has none; every satellite has one, `AGENTS.md` section 8).
3. `changelog.d/764-marten-resilience.added.md` (PR-2) and `changelog.d/764-dpia-store-failure-not-reported-as-not-found.fixed.md` (PR-1), through `mechanical-fixer`.
4. Build `dotnet build --configuration Release` with zero warnings; `dotnet test` for the five flags of the touched packages; per-flag measurement and `coverage-report.cs --check-justifications` as `AGENTS.md` section 9 describes.

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```text
CONTEXT: Issue #764, plan docs/plans/dpia-marten-resilience-implementation-plan-764.md, Phase 7 (PR-3).
TASK: Finish XML docs, the READMEs and the feature page, the changelog fragments, and run the verification commands.
KEY RULES: documentation goes to docs-writer, fragments to mechanical-fixer; documentation never types coverage figures by hand (covref markers); never name a client application; English only.
REFERENCE FILES: .claude/skills/encina-docs/SKILL.md, docs/features/dpia.md, docs/features/database-resilience.md, changelog.d/README.md
```

</details>

---

## Research

### Relevant Standards and Specifications

| Source | Relevance |
|--------|-----------|
| GDPR Art. 35(7) and 35(11) | The assessment record and its periodic review: a transient store failure must not lose a record or report success (Art. 35(7)), and the reminder keeps its own cycle (Art. 35(11)) |
| GDPR Art. 5(1)(c), Art. 25 | No assessment id, request type or error message in telemetry or logs |
| ADR-055 R2, R3, R5, R7 | One pipeline registry keyed by provider name, one options model, one telemetry, decorators in the provider package |
| ADR-019, ADR-027 | Marten is the event store of the compliance modules; no InMemory store |
| ADR-029 | `IErrorClassifier` is the single retry classification contract |
| ADR-062 R1, R4, R5 | Not found is a missing entity; store failures are `Internal`; cancellation is its own category |
| ADR-054 D4 | Tenant tag only when multi-tenancy is on |
| `AGENTS.md` sections 3, 5, 9 | Fail closed; Railway Oriented Programming; event-sourced modules and their test rule |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage |
|-----------|----------|-------|
| `IAggregateRepository<T>`, `MartenAggregateRepository<T>` | `src/Encina.Marten/` | Wrapped; the `Left` with the exception inside is the contract |
| `IReadModelRepository<T>`, `MartenReadModelRepository<T>` | `src/Encina.Marten/Projections/` | Reads wrapped |
| `DocumentSessionFlushExtensions.SaveChangesOrEjectAsync` | `src/Encina.Marten/DocumentSessionFlushExtensions.cs` | Why a retry after a failed save is safe |
| `IProviderResilience` (planned by #742) | `Encina.Resilience` | The executor |
| `IErrorClassifier`, `DefaultErrorClassifier` | `src/Encina/Recoverability/IErrorClassifier.cs` | Wrapped by `MartenErrorClassifier` |
| `DefaultDPIAService`, `DPIARequiredPipelineBehavior`, `DPIAHealthCheck`, `DPIAReviewReminderService` | `src/Encina.Compliance.DPIA/` | The consumers; their failure handling is verified above |
| `MartenCollection`, `MartenFixture` | `tests/Encina.IntegrationTests/Infrastructure/Marten/` | Integration collection |

Provider matrix: Marten on PostgreSQL only. The ten database providers (ADO.NET, Dapper and EF Core for SQL Server, PostgreSQL and MySQL, and MongoDB) do not take part: DPIA has no relational or document store (`AGENTS.md` section 3, event-sourced compliance modules).

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Marten` (`Marten`) | 2746 | One id inside the registered range 2600-2799; 2664-2700 are used on `main` and 2701-2745 are claimed by other plans; verified free by repository-wide search on `3bccacc1c`; re-check at PR time |
| `Encina.Compliance.DPIA` (`ComplianceDPIA` 8800-8899) | none | No new log: the existing `ServiceOperationError` and the executor's logs suffice |

### Estimated File Count

| PR | Files | Content |
|----|-------|---------|
| PR-1 | 5 | `DefaultDPIAService.cs`, `DefaultDPIAServiceStoreFailureTests.cs` (new), `DPIAAggregateIntegrationTests.cs` (modified), `Encina.Compliance.DPIA.json`, changelog fragment |
| PR-2 | 14 | `Encina.Marten.csproj`, `MartenErrorClassifier.cs`, `ResilientAggregateRepository.cs`, `MartenResilienceServiceCollectionExtensions.cs`, `ServiceCollectionExtensions.cs` (modified), `Log.cs` (modified), `Encina.Marten.json`, five tests (unit x3, guard, contract) plus the integration test, changelog fragment |
| PR-3 | 14 | `ResilientReadModelRepository.cs`, `MartenResilienceOptions.cs`, extension and registration file (both modified), manifest, unit and contract tests (new), DI and guard tests (modified), `DPIAResilienceIntegrationTests.cs`, DPIA README, `docs/features/dpia.md`, `docs/features/marten-resilience.md`, `src/Encina.Marten/README.md` |

Each PR is within 15 files and 1,500 lines (counted by file name). Implementers of changed interfaces: none (no interface changes). The test doubles of the decorators are NSubstitute substitutes and one local fault-injecting wrapper per integration test class; `new MartenAggregateRepository` and `new ResilientAggregateRepository` sites are the registrations (`src/Encina.Marten/ServiceCollectionExtensions.cs`) and the tests.

Per-file coverage targets (baseline from `.github/coverage-manifest/Encina.Marten.json` and `Encina.Compliance.DPIA.json` on `origin/main`; no target is lowered; each target is set by the PR that adds its tests):

| File | Unit | Guard | Contract | Integration | Justification |
|------|------|-------|----------|-------------|---------------|
| `Services/DefaultDPIAService.cs` (PR-1, modified; today unit 50, guard 4) | 65 | 4 | 0 | 0 | The eight changed Left branches are driven by mocks of `IAggregateRepository` and `IReadModelRepository`; the unit target rises by the share of those branches (today measured 54%); guard is the constructor checks only and stays; contract and integration rows do not exist for this file |
| `Resilience/ResilientAggregateRepository.cs` (PR-2, new) | 90 | 30 | 60 | 70 | Unit tests drive retry, exhaustion, ambiguity and cancellation with a fake executor; guard covers the three constructor checks and the two aggregate checks only; the contract class runs the interface contract over a fake inner; the fault-injecting integration test reaches the real retry and lost-acknowledgement paths |
| `Resilience/MartenErrorClassifier.cs` (PR-2, new) | 95 | 20 | 0 | 0 | Table-driven over every code; guard is the one constructor check; there is no contract suite for `IErrorClassifier` in `tests/Encina.ContractTests` and no infrastructure path |
| `Resilience/MartenResilienceServiceCollectionExtensions.cs` (PR-2, new) | 92 | 70 | 0 | 0 | The DI test builds providers in both registration orders; guard covers the null checks, the bulk of a small registration method |
| `Resilience/ResilientReadModelRepository.cs` (PR-3, new) | 90 | 30 | 60 | 45 | Same reasoning as the aggregate decorator; integration reaches the reads through the DPIA end-to-end test (the aggregate decorator's integration file is not repeated) |
| `Resilience/MartenResilienceOptions.cs` (PR-3, new) | 95 | 0 | 0 | 0 | A property bag; guard 0 because it has no method with parameters; reached through the registration test |
| `ServiceCollectionExtensions.cs` of `Encina.Marten` (PR-2 and PR-3, modified; row today unit 95, guard 65) | 95 | 65 | 0 | 0 | Unchanged: the new factory branches are covered by the registration tests, so the existing floors hold |
| `Log.cs` of `Encina.Marten` (PR-2, modified; no per-file row on `main`) | no row | no row | no row | no row | One generated method; no row is added; the log assertion is part of `ResilientAggregateRepositoryTests` |

The CRAP of each added method is at most 10: the decorators keep one method per operation of at most one decision, and the classifier splits "known code", "exception chain" and "delegate" into small methods.

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT: Encina (pre-1.0, .NET 10, C# 14). Issue #764: resilience for the Marten event-store operations of Encina.Compliance.DPIA (ADR-055 R7). AGENTS.md rules apply: ROP, fail closed, no InMemory store for event-sourced compliance modules, cancellation never a Left, no EncinaError.Message in logs. The foundation (Encina.Resilience) and IProviderResilience of #742 are merged first for PR-2 and PR-3; PR-1 needs neither.

IMPLEMENTATION OVERVIEW: (1) DefaultDPIAService maps only the two not-found codes to dpia.assessment_not_found; (2) MartenErrorClassifier wraps IErrorClassifier for the marten.* codes; (3) ResilientAggregateRepository and ResilientReadModelRepository run each operation through IProviderResilience under the name "marten"; (4) AddEncinaMartenResilience registers the marker and the classifier, and the Marten registrations build the decorators through order-independent factories; (5) EventId 2746 logs an ambiguous retried write; (6) DPIA end-to-end integration test; (7) READMEs, feature pages and fragments.

KEY PATTERNS: a Left drives the retry, never an exception; permanent codes are never retried; the caller's cancellation throws and is never counted; decorators and registrations are opt-in and order independent; every public constructor is guarded; unit tests mock IAggregateRepository, integration tests use Marten on PostgreSQL (MartenCollection) with a fault injector at the repository seam; specialists for changelog, manifest and documentation.

REFERENCE FILES: docs/architecture/adr/055-resilience-family.md, docs/plans/transport-resilience-implementation-plan-742.md, src/Encina.Marten/MartenAggregateRepository.cs, src/Encina.Compliance.DPIA/Services/DefaultDPIAService.cs, src/Encina/Recoverability/IErrorClassifier.cs
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | The DPIA service already caches read models through `ICacheProvider`; the decorators sit below it and add no cache |
| 2 | OpenTelemetry | ✅ | The executor's Polly instruments and span events (#742, #728) with provider and operation; tenant tag only when multi-tenancy is on (ADR-054 D4); no assessment id, request type or message |
| 3 | Structured Logging | ✅ | EventId 2746 in the `Marten` range; the executor's retry logs are #742's |
| 4 | Health Checks | ❌ | `DPIAHealthCheck` already reports a store `Left` as unhealthy, which now includes an open circuit; no new check |
| 5 | Validation | ✅ | Constructor and method arguments guarded; options validated by the executor's validator (#742) |
| 6 | Resilience | ✅ | This is the capability: retry, breaker and timeout around the Marten operations |
| 7 | Distributed Locks | ❌ | No shared state; the reminder's multi-instance lock is #763 |
| 8 | Transactions | ❌ | Marten's own session transaction is unchanged; a retried save starts from an ejected session (#3787) and a version-checked append |
| 9 | Idempotency | ✅ | The expected-version append and the caller-chosen stream id make a repeated write unable to duplicate events; the ambiguous outcome is logged (choice 5) |
| 10 | Multi-Tenancy | ❌ | The breaker keys on the one Marten dependency (choice 9); `Encina.Marten` has no tenant isolation today (#326 plans it); the reserved `default` segment does not apply |
| 11 | Module Isolation | ❌ | Applies uniformly to every Marten-backed module |
| 12 | Audit Trail | ✅ | The point of the issue: an assessment record is not lost or reported as success on a transient failure; no operation audit is added |

---

## Prerequisites & Dependencies

| Prerequisite | State | Effect |
|--------------|-------|--------|
| The foundation (`Encina.Resilience`, registry, options model, strategy factories, rejection-mapper hook) | Described in the plan of #742 (issue file of the resilience family); not on `main` | Blocks PR-2 and PR-3 (choice 2); PR-1 does not need it |
| `IProviderResilience` and its executor (#742 PR-1) | Plan on `main`, issue open | Consumed by both decorators |
| #1965 (AIAct resilience, open) | Open; needs #847 (AIAct on Marten, open) | Consumes the decorators; no work here |
| #763 (DPIA reminder lock, open) | Open | Independent |
| #326 (Marten multi-tenancy, open) | Open | Revisits choice 9 if it chooses a database per tenant |
| #4007 (EventId block migration, open) | Open | Moves `Marten` 2600-2799 to 11350-11549; the id moves with its range |
| Issue file `marten-repositories-cancellation-as-left` | Written with this plan, to be opened by the orchestrator | The repositories report a caller's cancellation as `marten.*_failed`; the decorator works around it (choice 8) until it is fixed |

---

## Next Steps

1. Commit this plan, open its PR and link it from #764.
2. When #764 is picked up, re-check the choices against today's code and ask the maintainer about the Dilemmas (choices 1 to 9).
3. Brief a worker for PR-1 immediately (no prerequisite), then for PR-2 and PR-3 after the foundation and #742 PR-1 are merged.
