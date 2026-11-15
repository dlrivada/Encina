# Implementation Plan: Per-Saga Distributed Lock Scope for Saga Execution

> **Issue**: [#715](https://github.com/dlrivada/Encina/issues/715)
> **Type**: Feature
> **Complexity**: High (4 pull requests; one scope primitive and one adoption that waits for the saga commit owner)
> **Estimated Scope**: ~200-260 lines of production code + ~1,000-1,300 lines of tests
> **Specification**: [SPEC-000](../specifications/SPEC-000-encina-1.0-baseline-and-release-scope.md) REQ-027 (lock providers); [ADR-050](../architecture/adr/050-modules-may-require-modules.md) (the Saga row: concurrency control on the saga row, and a lock when timeouts run on several instances); [ADR-052](../architecture/adr/052-distributed-lock-failure-contract.md); [ADR-054](../architecture/adr/054-observability-conventions.md) D4; [ADR-062](../architecture/adr/062-error-category-contract.md)
> **Depends on**: the shared contract of the #713 plan (PRs 1 to 3) and #2667; PR 2 of this plan also needs the commit owner `SagaOrchestrator.CommitAsync` of the #720 plan
> **Related**: #713 (owns the shared piece), #720 (saga compensation transaction and commit owner), #2213 (timeout is an unconditional read-modify-write), #2208 (nothing enforces saga timeouts), #1237 (correlation, decided by compare-and-set), #297 (state machine), #738 (tenant id on sagas), #750 (saga audit), #218 (fencing), #223 (auto-extend)

---

## Summary

A saga row is changed by read-modify-write calls on `SagaOrchestrator` (`AdvanceAsync`, `CompleteAsync`, `StartCompensationAsync`, `CompensateStepAsync`, `FailAsync`, `TimeoutAsync`), each of which loads the state, checks its status and writes it back. Two actors on the same saga (a running saga and a timeout, a retry and a resume) can read the same state, and the later write overwrites the earlier one. This plan adds an **opt-in exclusive scope per saga** built on the shared messaging lock:

1. **A scope primitive, not a lock inside each orchestrator method.** `SagaLockScope.RunExclusiveAsync<T>(sagaId, sagaType, tenantId, work, ct)` acquires `encina:saga:{sagaId}` (32 lowercase hex characters of the Guid), runs `work`, and releases the lock after `work` returned. The caller puts the whole sequence inside `work`: load, execute the step, update, and the commit. The orchestrator methods stay lock-free, so a call made inside `work` never tries to take a lock its own caller holds (the lock providers are not reentrant).
2. **Why the scope and not per-method locks.** `SagaOrchestrator` never calls `SaveChangesAsync`; the commit owner proposed by the #720 plan is `SagaOrchestrator.CommitAsync`, called by the runner. A lock released at the end of an orchestrator method would be released **before** the commit on stores that track changes or enlist in the request's transaction, so the next actor would still read the old row. Only a scope that encloses the commit closes the window.
3. **Per-saga-type lifetime.** `SagaOptions.LockExpiry` (default 5 minutes) with `LockExpiryBySagaType` overriding it per saga type name, and `LockWait` (default 10 seconds), validated.
4. **Outcomes.** Contention after the wait returns `Left` `saga.lock_contended` (category `Conflict`, ADR-062); a lock `Left` is returned unchanged; `work` never runs without the lock when the flag is on. With the flag off the scope runs `work` directly.
5. **Adoption.** `SagaRunner` runs its steps and compensations inside the scope after the saga row exists (PR 2, after #720's commit owner). The future timeout processor (#2208), the compensation retry (#720) and the correlation resume (#1237) use the same primitive when their plans choose to.
6. **Limits stated.** The lock is a lease without fencing (#218): a holder that outlives `LockExpiry` can still write, which is why the status guards in the orchestrator methods and the conditional update of #2213 remain the second line.

**Standards**: ADR-050; ADR-052; ADR-054 D4 (tenant tag where a tenant exists); ADR-062 (`saga.lock_contended` is `Conflict`); `AGENTS.md` section 3.

**Estimated scope**: 1 new production file, 6 modified, 2 `PublicAPI`/manifest files, 9 test files plus 11 integration files, 4 documentation files; 4 pull requests.

**Affected packages**: `Encina.Messaging` only. The ten saga stores are not edited.

**Provider category**: Database (10) as consumers (`ISagaStore`: ADO.NET x3, Dapper x3, EF Core, MongoDB) with one integration proof per provider; Distributed locks as the engine through the #713 adapter.

**What the code does today** (`origin/main` at `db1e7f688`):

- `SagaOrchestrator` (`src/Encina.Messaging/Sagas/SagaOrchestrator.cs`) takes the store, a serializer, `SagaOptions`, a logger and a time provider (18 construction sites in `src/` and `tests/`), and has the six read-modify-write methods named above, each guarded by a status check (`SagaStatus.Running`, `Compensating`) that returns `saga.invalid_status` when the saga moved on. It never calls `SaveChangesAsync`.
- **No production code calls `TimeoutAsync`** (grep over `src/`: the callers are `SagaOrchestratorTests` and `SagaOrchestratorGuardTests`); #2208 records that nothing enforces saga timeouts. `SagaRunner` (`src/Encina.Messaging/Sagas/LowCeremony/SagaRunner.cs`) starts a **new** saga for every `RunAsync` (`StartAsync`, then `ExecuteStepsAsync`, `HandleStepFailedAsync`, `CompensateAsync`), so today two actors never touch the same row in one process. The concurrency this plan addresses appears with the timeout processor (#2208), the compensation retry (#720), the correlation resume (#1237) and the state machine (#297).
- `SagaRunner` takes `(SagaOrchestrator, IRequestContextAccessor, ILogger<SagaRunner>)` (14 construction sites in 5 test files, none in `src/`); #720 changes its constructor too.
- `SagaOptions` (same file, line 628) has `StuckSagaThreshold`, `StuckSagaBatchSize`, `StuckSagaCheckInterval`, `DefaultSagaTimeout`, `ExpiredSagaBatchSize`; it is registered as a singleton (`MessagingServiceCollectionExtensions.cs` line 362). `SagaErrorCodes` is in `Sagas/SagaErrorCodes.cs`.
- The saga plans on `main` that touch this area: #720 adds `SagaOrchestrator.CommitAsync` as the one commit owner; #1237 settles concurrent resumes with an atomic claim and a compare-and-set and adds no lock (its Design Choice 9); #297 defers concurrent triggers to #2213; #738 and #750 add no lock.
- The shared contract (`IMessagingLockProvider`, `MessagingLockGate`, `MessagingLockMarker`, `MessagingLockNames`, EventIds 7500-7503, the `MessagingLocking` range) is delivered by the #713 plan; #714 adds 7504; this plan adds 7505.
- The issue says "All 13 database providers": 10 in `AGENTS.md` section 5. Manifest `Encina.Messaging.json`: `Sagas/SagaOrchestrator.cs` unit 90, guard 45, integration 30; `Sagas/LowCeremony/SagaRunner.cs` unit 95, guard 10.

---

## Design Choices

<details>
<summary><strong>1. What the lock protects and where it is taken: a scope the caller wraps around its sequence, a lock inside each orchestrator method, or none (compare-and-set)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `SagaLockScope.RunExclusiveAsync`: the caller encloses load, step, update and commit; orchestrator methods stay lock-free** | Spans the commit, the only way to close the window on tracked or enlisted stores; no reentrancy problem; matches the issue's "load state, execute step, save state within lock scope" | Every caller must opt in; nothing is protected until a caller (runner, timeout processor, retry) uses it |
| **B) A lock inside each of the six orchestrator methods** | Every caller is protected without changing it | Released before the commit (the orchestrator never commits), so the window stays open on EF-style stores; a call made inside a caller's own lock would wait for itself (no reentrancy); six acquires per saga run |
| **C) No lock: the status guards plus a conditional update (#2213) and the atomic claim and compare-and-set of #1237** | Correct on every provider without a lock service; no lease to expire | The issue is not delivered; needs a column or predicate on ten stores; #2213 has no plan yet |

### Chosen Option: **A — an exclusive scope the caller encloses its sequence in** (recommended, pending the maintainer)

### Rationale

- A wins when concurrent actors exist in one deployment and the application runs a lock service (timeout processor, retry, resume on several replicas); C wins for deployments that will not run a lock service, and it is the database-level safety net that makes a lost lease harmless; B is rejected on its own terms (commit window, no reentrancy).
- A and C are complementary and the plan says so: the lock avoids wasted work and serialises the sequence, the conditional update guarantees correctness if the lease expires. The maintainer's answer decides whether Phases 1 and 2 are built.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Adopting the scope in SagaRunner: now, after the commit owner exists, or not at all</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) After #720's `SagaOrchestrator.CommitAsync` exists: the runner wraps steps and compensations (and its commit) in the scope** | The scope then really encloses the commit; the runner's compensations are exclusive against an external timeout; one adoption point | Waits for #720 PR 12; the runner constructor changes again (an optional trailing parameter) |
| **B) Now, around the existing steps** | Earlier | Released before the request's commit when the runner is called inside a handler on a tracked or enlisted store; claims more than it delivers |
| **C) Provide the primitive only; each future caller adopts it in its own plan** | No runner change; no coupling to #720 | A new saga's run is not exclusive against a timeout processor until #2208 adopts it from the other side (it cannot exclude a holder that does not lock) |

### Chosen Option: **A — adopt in the runner after the commit owner exists** (recommended, pending the maintainer)

### Rationale

- A wins because exclusion needs both actors to lock: the timeout processor and the runner. B wins only if the runner commits itself today, which it does not; C wins if the runner will never run concurrently with another actor on its saga.
- PR 2 is therefore the last code PR; PR 1 (the primitive) is useful to #2208, #720 and #1237 on its own.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Where the per-saga-type lifetime is configured</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `SagaOptions.LockExpiryBySagaType` (a dictionary keyed by the saga type name) over a default `LockExpiry`** | Operators tune lifetimes without code changes; the issue's "configurable per saga type"; the options class already holds the saga timings | The key is a string that must match `SagaDefinition.SagaType` exactly |
| **B) A fluent `WithLockExpiry` on the saga definition** | Next to the steps that know their duration | Operators cannot change it without a deploy; one more definition member |

### Chosen Option: **A — options dictionary** (recommended, pending the maintainer)

### Rationale

- A wins when lifetimes are an operational setting; B wins when the saga author owns step durations. Both resolve to the same `RunExclusiveAsync` argument, so switching later is local.
- Validation: every value positive, `LockWait` not above the smallest expiry (the default or an override).

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Lock name</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `encina:saga:` plus the saga Guid in `N` format (32 lowercase hex characters)** | The Guid is the saga's identity in the store; fixed length, no escaping, injective; no load needed to build it | Differs from the issue's `{saga-type}:{saga-id}` |
| **B) `encina:saga:{type}:{id}`** | Readable | Needs the type before locking; a type name needs escaping to stay injective; adds nothing to identity |

### Chosen Option: **A — the Guid only**

### Rationale

- `ISagaStore.GetAsync(Guid sagaId)` identifies a saga by the Guid alone, so equal Guids are the same saga and different Guids are different sagas. The type is passed to the scope only to pick the lifetime. The name has no tenant segment: the store key has none, and an application that lists the name in the tenant scoping of #741 gets `@{tenant}:{name}` with that plan's rules (a tenant `default` is an ordinary tenant).

**Class:** Trivial
**Settled by:** `src/Encina.Messaging/Sagas/ISagaStore.cs` (`GetAsync(Guid sagaId, ...)`), `MessagingLockNames` of the #713 plan, #741 (tenant derivation)

</details>

<details>
<summary><strong>5. Failure semantics</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A lock `Left` is returned unchanged and `work` does not run; contention after the wait is `Left` `saga.lock_contended`; with the flag on the lock is never bypassed** | ADR-052 and the fail-closed rule; the caller decides (retry, leave the saga for the stuck-saga handling) | A broken lock backend stops the callers that adopted the scope |
| **B) On a lock `Left`, run `work` unlocked** | Sagas continue | Lost transitions exactly when coordination is down |

### Chosen Option: **A — fail closed**

### Rationale

- A run that fails to take the lock after its saga row was created leaves the saga `Running`; the existing stuck-saga and timeout handling (`GetStuckSagasAsync`, #2208) is the recovery, as for a failed `StartCompensationAsync` today (`SagaRunner` comment "recoverable by timeout/stuck-saga handling"). Logs carry the code and the saga type, never `EncinaError.Message`.

**Class:** Settled
**Settled by:** ADR-052 (contention versus fault, cancellation propagates), `AGENTS.md` section 3 (gates fail closed; errors are never swallowed)

</details>

<details>
<summary><strong>6. Error code and category</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `saga.lock_contended` in `SagaErrorCodes`, created with `ErrorCategory.Conflict`** | Mapped by category, never by code text (ADR-062 R3); the saga is being advanced by another actor | One more code |
| **B) Reuse `saga.invalid_status`** | No new code | Wrong meaning: the status is fine, the saga is busy |

### Chosen Option: **A — `Conflict`**

### Rationale

- A test asserts `GetCategory` returns `Conflict`.

**Class:** Trivial
**Settled by:** ADR-062 (`Conflict` category, `src/Encina/Errors/ErrorCategory.cs`; hosts never parse code text)

</details>

---

## Implementation Phases

### Phase 1: The Scope Primitive, Options and Registration — PR 1

> **Goal**: `SagaLockScope` and its configuration, no adopter yet. Starts after PRs 1 and 2 of the #713 plan.

<details>
<summary><strong>Tasks</strong></summary>

1. **`Locking/MessagingLockNames.cs`** (modify, file of #713) — `static string Saga(Guid sagaId)` returning `"encina:saga:" + sagaId.ToString("N")`.
2. **`Sagas/SagaErrorCodes.cs`** (modify) — `const string LockContended = "saga.lock_contended"`.
3. **`Sagas/SagaOrchestrator.cs`** (modify, the `SagaOptions` class) — `TimeSpan LockExpiry` (5 minutes), `TimeSpan LockWait` (10 seconds), `IDictionary<string, TimeSpan> LockExpiryBySagaType` (ordinal comparer); an internal `Validate`.
4. **`Sagas/SagaLockScope.cs`** (new, public sealed, scoped) — constructor `(IMessagingLockProvider? lockProvider, MessagingLockMarker? marker, SagaOptions options, ILogger<SagaLockScope> logger)`; `ValueTask<Either<EncinaError, T>> RunExclusiveAsync<T>(Guid sagaId, string sagaType, string? tenantId, Func<CancellationToken, ValueTask<Either<EncinaError, T>>> work, CancellationToken cancellationToken)`: guards (`ArgumentException.ThrowIfNullOrWhiteSpace(sagaType)`, `ArgumentNullException.ThrowIfNull(work)`), flag off runs `work`; otherwise `MessagingLockGate.TryEnterAsync(..., "saga", MessagingLockNames.Saga(sagaId), expiryFor(sagaType), options.LockWait, tenantId, ...)`: `Left` returned, `None` returns `Left` `saga.lock_contended` (`ErrorCategory.Conflict`) and logs 7505, `Some(handle)`: `await using` around `work(ct)`, then log 7503 when the handle expired.
5. **`Locking/MessagingLockLog.cs`** (modify) — EventId 7505 `SagaLockContended` (Warning: saga type, saga id).
6. **`MessagingServiceCollectionExtensions.cs`** (modify) — when `UseSagas` and `UseDistributedLocking` are on: `services.AddScoped<SagaLockScope>()` next to `AddScoped<SagaOrchestrator>()` (line 365).
7. **`PublicAPI.Unshipped.txt`** (Messaging) through `mechanical-fixer`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of issue #715: the exclusive saga scope.

CONTEXT:
- The shared lock contract (gate, marker, names, flag, EventIds 7500-7504) comes from the plans of #713 and #714; do not
  recreate it. SagaOrchestrator never calls SaveChangesAsync and its methods must stay lock-free: the lock providers are
  not reentrant, so a method called inside the scope must not take the lock again.
- SagaOptions is a singleton; the scope is scoped and resolves the port and marker from DI per scope.

TASK:
Create the items of the Phase 1 tasks. Unit tests: flag off runs work with no port call; acquired runs work inside the lock
and releases after it returns (ordering log); contended after the wait returns Left with category Conflict and logs 7505; a
provider Left is returned unchanged and work never runs; expired handle logs 7503; a throwing work still releases;
cancellation; the expiry override by saga type; option validation. Guard tests for the public constructor and method
(null and whitespace). A DI test with ValidateOnBuild and ValidateScopes for the flags on and off.

KEY RULES:
- Never describe the scope as atomic or transactional; it encloses what the caller puts in work, including the commit.
- Log codes, saga type and saga id only; never EncinaError.Message.

REFERENCE FILES: src/Encina.Messaging/Sagas/SagaOrchestrator.cs, src/Encina.Messaging/Sagas/SagaErrorCodes.cs,
src/Encina.Messaging/MessagingServiceCollectionExtensions.cs, docs/plans/outbox-distributed-lock-implementation-plan-713.md.
```

</details>

### Phase 2: Adoption in SagaRunner — PR 2

> **Goal**: The runner's run is exclusive against any other actor that locks the same saga. Starts after PR 12 of the #720 plan (`SagaOrchestrator.CommitAsync`).

<details>
<summary><strong>Tasks</strong></summary>

1. **`Sagas/LowCeremony/SagaRunner.cs`** (modify) — an optional trailing constructor parameter `SagaLockScope? lockScope = null` (the 14 test construction sites stay valid); after `StartAsync` returned the saga id, `RunAsync` runs `ExecuteStepsAsync` and the failure handlers inside `lockScope.RunExclusiveAsync(sagaId, definition.SagaType, requestContext.TenantId, ...)` when a scope is registered, and calls `SagaOrchestrator.CommitAsync` of #720 inside it before returning. A `Left` from the scope is returned; the saga stays `Running` for the stuck-saga handling (Design Choice 5).
2. **`PublicAPI.Unshipped.txt`** (constructor change), through `mechanical-fixer`.
3. Coordinate with #720: its runner constructor change and this optional parameter land in whichever order merges, each as a trailing optional parameter, so neither breaks the other's construction sites.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of issue #715: SagaRunner runs inside the exclusive scope.

CONTEXT:
- #720 adds SagaOrchestrator.CommitAsync as the single commit owner and changes the SagaRunner constructor; this phase starts
  after it. The scope must enclose the commit, otherwise it releases before the row is persisted.
- RunAsync starts a new saga per run; the scope is taken after the saga id exists.

TASK:
Add the optional SagaLockScope parameter and wrap the steps, the failure handlers and the commit as described. Unit tests
in SagaRunnerLockTests: no scope (existing behavior, run the existing SagaRunner tests), scope acquired (steps and commit run
inside it, released after), contended or Left (the run returns Left and no step runs), compensation after a failed step
still runs inside the same acquisition.

KEY RULES:
- Optional trailing parameter only; do not change the existing parameter order.
- A Left from the scope is never swallowed and never leads to running steps unlocked.

REFERENCE FILES: src/Encina.Messaging/Sagas/LowCeremony/SagaRunner.cs, tests/Encina.UnitTests/Messaging/Sagas/SagaRunnerTests.cs,
docs/plans/saga-compensation-transaction-implementation-plan-720.md.
```

</details>

### Phase 3: Cross-Cutting Integration and Observability

> **Goal**: Record what is reused and what is not added.

<details>
<summary><strong>Tasks</strong></summary>

1. Telemetry reuses the counter of #713 with `pattern="saga"` and the `tenant.id` tag only when the caller passes a tenant (the runner passes the request context's tenant, which exists only when tenancy is on, #738 adds the saga-level tenant).
2. Logs: 7500, 7501, 7503 from the shared gate; 7505 here. No new meter or activity: `SagaActivitySource` spans of the run enclose the scope.
3. Audit (#750), tenant column (#738), correlation (#1237), state machine (#297): evaluated in the matrix; none adds code here.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are verifying Phase 3 of issue #715: observability reuse.

CONTEXT:
- The counter and gate come from #713; ADR-054 D4: the tenant tag only where a tenant exists.

TASK:
Test that the counter carries pattern "saga" and tenant.id only when a tenant was passed, and that no tag or log carries
EncinaError.Message.

KEY RULES:
- Bounded tags only.

REFERENCE FILES: src/Encina.Messaging/Diagnostics/SagaActivitySource.cs (under Encina.Messaging/Diagnostics),
docs/architecture/adr/054-observability-conventions.md.
```

</details>

### Phase 4: Testing — PRs 1 to 4

> **Goal**: Every flag reaches its target and every provider proves two actors serialised on one saga.

<details>
<summary><strong>Tasks</strong></summary>

1. **Unit, guard**: PRs 1 and 2 (files named above). **Contract**: none (a class, not an interface). **Property**: none (no pure logic beyond a name format; a short test of the name format sits in the unit file).
2. **Integration** (PRs 3 and 4): an abstract `SagaLockConcurrencyIntegrationTestsBase` under `tests/Encina.IntegrationTests/Messaging/` starts a saga, then runs `CompleteAsync` and `TimeoutAsync` for it at once, each inside `RunExclusiveAsync` with `ISagaStore.SaveChangesAsync` in `work` (the commit), over one store and a Redis lock; it asserts that exactly one transition wins, the other returns `saga.invalid_status`, and the persisted row is the winner's. One concrete class per provider in its `[Collection("<Family>-<Database>")]`: PR 3 has ADO and Dapper on SqlServer, PostgreSQL and MySQL (six classes), PR 4 has EF Core on the same three and MongoDB (four). All ten providers are covered.
3. **Load and benchmark**: justified in `.md` files (one acquire per saga run).
4. **Per-file targets**:

| File | Unit | Guard | Contract | Property | Integration | Justification |
|------|-----:|------:|---------:|---------:|------------:|---------------|
| `Sagas/SagaLockScope.cs` | 95 | 60 | 0 | 0 | 60 | Every branch is unit-testable; guard covers the public constructor and method; the two-actor test runs it on ten providers; contract and property 0: a class with no interface and no pure logic |
| `Sagas/LowCeremony/SagaRunner.cs` | 95 | 10 | 0 | 0 | 0 | Existing targets kept; the lock paths add unit cases; the runner is exercised by the scope's integration test indirectly, not driven there |
| `Sagas/SagaOrchestrator.cs` (`SagaOptions`) | 90 | 45 | 0 | 0 | 30 | Existing targets kept; the three new members add setter and `Validate` cases |
| `Sagas/SagaErrorCodes.cs` | 90 | 0 | 0 | 0 | 0 | A constant; the category test reads it; no parameters for guard tests |

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
You are implementing Phase 4 of issue #715: tests across the ten providers.

CONTEXT:
- Integration tests use shared [Collection("<Family>-<Database>")] fixtures, ClearAllDataAsync in InitializeAsync, and
  RedisFixture for the real lock. Tests execute real code and assert with Shouldly through Encina.Testing.Shouldly.

TASK:
Write the abstract two-actor base and the ten concrete classes, the unit and guard files, and the .md justifications for
load and benchmark. Report measured per-flag coverage against the Phase 4 targets.

KEY RULES:
- Start the two actors together with a barrier, never Thread.Sleep.
- Never call a fixture's DisposeAsync from a test.

REFERENCE FILES: tests/Encina.IntegrationTests/Messaging, tests/Encina.UnitTests/Messaging/Sagas/SagaOrchestratorTests.cs,
docs/testing/integration-tests.md.
```

</details>

### Phase 5: Documentation and Finalization — PR 4

> **Goal**: Users know when the scope helps and what it does not guarantee.

<details>
<summary><strong>Tasks</strong></summary>

1. XML docs on `SagaLockScope`, the three options and `SagaErrorCodes.LockContended`, with the commit-window explanation in the remarks.
2. `changelog.d/715-saga-distributed-lock.added.md`, through `mechanical-fixer`.
3. The saga section of the messaging locks page owned by #713 and `src/Encina.Messaging/README.md`, through `docs-writer`: how a caller uses the scope, the relation to #2213 and #1237, the limits.
4. Build and test verification: zero warnings, per-flag targets met.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
You are finishing issue #715: documentation and verification.

CONTEXT:
- Documentation goes through docs-writer; the fragment and PublicAPI lines through mechanical-fixer.

TASK:
Complete the XML docs, request the fragment and the documentation, run the build and tests.

KEY RULES:
- State the limits plainly: a lease without fencing; the scope encloses what the caller puts in it; the orchestrator commits nothing.
- Write "the reference application", never a private client name.

REFERENCE FILES: changelog.d/README.md, src/Encina.Messaging/README.md.
```

</details>

---

## Research

### Relevant Standards and Specifications

| Source | Relevance |
|--------|-----------|
| ADR-050 | Saga row: concurrency control on the saga row (#2213) and a lock when timeouts run on several instances |
| ADR-052 | Contention `Right(None)`, fault `Left`, cancellation propagates |
| ADR-054 D4 | Tenant tag only where a tenant exists |
| ADR-062 | `saga.lock_contended` is `Conflict` |
| Saga pattern (orchestrated, with compensation) | Why one actor at a time per saga instance matters |
| Lease-based locks (Kleppmann, "How to do distributed locking") | A lease is not a fence; the status guards and #2213 remain |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `SagaOrchestrator`, `SagaOptions` | `src/Encina.Messaging/Sagas/SagaOrchestrator.cs` | Methods the scope encloses; options home |
| `SagaRunner` | `src/Encina.Messaging/Sagas/LowCeremony/SagaRunner.cs` | The adoption point |
| `MessagingLockGate`, `MessagingLockNames`, `MessagingLockMarker` | #713 plan, `src/Encina.Messaging/Locking/` | The shared contract |
| `SagaErrorCodes`, `ErrorCategory` | `src/Encina.Messaging/Sagas/SagaErrorCodes.cs`, `src/Encina/Errors/ErrorCategory.cs` | New code and its category |
| `RedisFixture` | `tests/Encina.TestInfrastructure/RedisFixture.cs` | Real lock in integration tests |

### Provider Matrix (database stores as consumers)

| Family | Providers | Notes |
|--------|-----------|-------|
| ADO.NET | SqlServer, PostgreSQL, MySQL | `SagaStoreADO`; one integration class each |
| Dapper | SqlServer, PostgreSQL, MySQL | `SagaStoreDapper`; one integration class each |
| EF Core | SqlServer, PostgreSQL, MySQL | One `SagaStoreEF`; three integration classes; `SaveChangesAsync` in `work` is the commit |
| MongoDB | MongoDB | `SagaStoreMongoDB` |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Messaging` (`MessagingLocking`, registered by the #713 plan) | 7505 | `SagaLockContended`; 7500-7503 are #713's, 7504 is #714's; 7506-7539 stay unassigned |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Production (new) | 1 | `SagaLockScope` |
| Production (modified) | 6 | `MessagingLockNames`, `SagaErrorCodes`, `SagaOrchestrator.cs` (options), `MessagingLockLog`, `MessagingServiceCollectionExtensions`, `SagaRunner` |
| `PublicAPI.Unshipped.txt` and manifest | 2 | Messaging |
| Tests | 19 | Unit (5), guard (2), DI (1), integration base plus ten classes (11), name format in the unit file |
| Documentation | 4 | Fragment, page section, README, two `.md` justifications |

PR split (each at most 15 files and 1500 lines, each building and passing alone):

| PR | Content | Files | Lines (est.) |
|----|---------|------:|-------------:|
| 1 | Phase 1: scope, options, code, name, log 7505, registration, tests (unit, guard, DI), `PublicAPI`, manifest; starts after PRs 1 and 2 of the #713 plan | 13 | 1,000 |
| 2 | Phase 2: `SagaRunner` adoption, `SagaRunnerLockTests`, `PublicAPI`; starts after #720 PR 12 | 4 | 350 |
| 3 | Phase 4 integration: base plus six ADO and Dapper classes | 7 | 650 |
| 4 | Phase 4 integration: EF Core x3 and MongoDB; Phase 5: fragment, README, page section, justifications | 9 | 600 |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Combined prompt — all phases of issue #715</strong></summary>

```
PROJECT CONTEXT:
Encina is a pre-1.0 .NET 10 / C# 14 library with Railway Oriented Programming (Either<EncinaError, T>). Issue #715 adds an
opt-in exclusive scope per saga: SagaLockScope.RunExclusiveAsync takes encina:saga:{guid N} around the caller's whole
sequence, including the commit. The shared contract comes from the plan of #713. Read AGENTS.md sections 3, 5, 6 and 9
and ADR-050, ADR-052, ADR-054, ADR-062 first.

IMPLEMENTATION OVERVIEW:
1. MessagingLockNames.Saga, SagaErrorCodes.LockContended (Conflict), SagaOptions.LockExpiry/LockWait/LockExpiryBySagaType.
2. SagaLockScope (scoped): flag off runs work; acquired runs work then releases; contended returns Left; Left returned
   unchanged; EventId 7505; registration when UseSagas and UseDistributedLocking.
3. SagaRunner (after #720's CommitAsync): optional trailing SagaLockScope parameter; steps, compensation and commit inside.
4. Unit, guard and DI tests; a two-actor integration base and ten provider classes; docs by specialists.

KEY PATTERNS:
- The orchestrator methods stay lock-free (no reentrancy); the lock is a lease without fencing, never "atomic".
- Fail closed; log codes, saga type and id only; TimeProvider for clock reads; Shouldly via Encina.Testing.Shouldly.

REFERENCE FILES: src/Encina.Messaging/Sagas/SagaOrchestrator.cs, src/Encina.Messaging/Sagas/LowCeremony/SagaRunner.cs,
docs/plans/saga-compensation-transaction-implementation-plan-720.md, docs/plans/outbox-distributed-lock-implementation-plan-713.md.
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Saga state is not cacheable data |
| 2 | OpenTelemetry | ✅ | Phase 3: counter of #713 with `pattern="saga"` and `tenant.id` when a tenant is passed (ADR-054 D4) |
| 3 | Structured Logging | ✅ | Phase 1: 7505 plus the shared 7500-7503; saga type and id only |
| 4 | Health Checks | ⏭️ | `SagaHealthCheck` exists; a lock-held-too-long signal waits for the timeout processor (#2208) |
| 5 | Validation | ✅ | Phase 1: `SagaOptions.Validate`; the startup check is #713's |
| 6 | Resilience | ❌ | A contended saga is retried by its caller; no retry or breaker around the lock |
| 7 | Distributed Locks | ✅ | The feature itself |
| 8 | Transactions | ✅ | The scope encloses the caller's commit (#720's `CommitAsync`); it makes nothing atomic and is not a transaction |
| 9 | Idempotency | ❌ | Duplicate event handling for sagas is #1237's compare-and-set, not this lock |
| 10 | Multi-Tenancy | ✅ | Tenant tag when a tenant exists; the saga-level tenant column is #738; #741 can scope the name |
| 11 | Module Isolation | ❌ | Not applicable |
| 12 | Audit Trail | ⏭️ | Saga audit is #750; the lock adds no audit event |

---

## Prerequisites & Dependencies

- The #713 plan's PRs 1 and 2 before PR 1 here, and its PR 3 plus #2667 before the integration tests run.
- PR 2 needs #720's PR 12 (`SagaOrchestrator.CommitAsync`); PR 1 does not.
- #1237 chose compare-and-set over a lock for concurrent resumes and #2213 has no plan yet; both are compatible with the scope (Design Choice 1 states the complement); neither blocks it, and neither is a pending maintainer decision.
- Not blocked by another issue's open maintainer decision.
- No follow-up issue file is needed: #2208, #2213, #738, #750, #218 and #223 are open issues named above.

## Next Steps

1. Open the plan PR; when work on #715 starts, ask the maintainer to answer Dilemmas 1 to 3 and record them in `## Maintainer Decisions`.
2. Brief PR 1 after PR 2 of #713 merged; PR 2 after #720 PR 12.
