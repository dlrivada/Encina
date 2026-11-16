# Implementation Plan: Distributed Lock Around the Scheduled Message Dispatch Cycle

> **Issue**: [#716](https://github.com/dlrivada/Encina/issues/716)
> **Type**: Feature
> **Complexity**: Medium (3 pull requests; the shared lock contract comes from the #713 plan)
> **Estimated Scope**: ~120-160 lines of production code + ~700-900 lines of tests
> **Specification**: [SPEC-000](../specifications/SPEC-000-encina-1.0-baseline-and-release-scope.md) REQ-027 (lock providers); [ADR-050](../architecture/adr/050-modules-may-require-modules.md) (the Scheduling row lists "row claiming or a lock"); [ADR-052](../architecture/adr/052-distributed-lock-failure-contract.md) (lock failure contract); [ADR-054](../architecture/adr/054-observability-conventions-and-trace-propagation.md) D4 (tenant tag)
> **Depends on**: the shared contract of the #713 plan (PRs 1 to 3: port, gate, marker, adapter, flag) and #2667 (ADR-052 implementation)
> **Related**: #713 (outbox, owns the shared piece), #717 (leader election), #1251 (row claiming), #735 (idempotent re-execution), #739 (tenant id on scheduled messages), #749 (audit of processors), #741 (tenant-scoped locks)

---

## Summary

`ScheduledMessageProcessor` polls `IScheduledMessageStore` for due messages and dispatches them. With several replicas, each replica reads the same due rows and executes the same scheduled command, which is dangerous for side effects (payments, notifications). This plan adds the **scheduling adoption of the messaging lock**: a replica runs a dispatch cycle only while it holds `encina:scheduled:process`.

1. **Cycle gate.** `ScheduledMessageProcessor.ProcessOnceAsync` acquires the lock right after it creates the cycle scope (before the activity is started) and disposes it after `SchedulerOrchestrator.ProcessDueMessagesAsync` returned. That method already ends with `IScheduledMessageStore.SaveChangesAsync` (`SchedulerOrchestrator.cs` lines 323 to 329), so the handle outlives the save of the batch outcomes.
2. **Outcomes.** Contention (`Right(None)`, ADR-052) skips the cycle and is counted; a lock `Left` fails the cycle visibly (error code logged through the existing `SchedulingProcessorLog.BatchFailed`-style path, counted) and never dispatches without the lock.
3. **Options.** `SchedulingOptions.LockExpiry` (default 5 minutes) and `LockWait` (default zero), validated; the shared flag `config.UseDistributedLocking` and the registration `AddEncinaMessagingDistributedLocking()` are the ones of #713.
4. **What it does not claim.** The lock is a lease without fencing (#218). A crash after a message was dispatched and before the outcomes were saved re-dispatches it: idempotent re-execution is #735, not this plan. The lock reduces concurrent double-firing; it does not make dispatch exactly-once.

**Standards**: ADR-052 (contention is `Right(None)`); ADR-050 (the lock is a listed alternative for the Scheduling row); `AGENTS.md` section 3 (background errors are never swallowed; gates fail closed).

**Estimated scope**: 0 new production files, 2 modified (`SchedulingOptions`, `ScheduledMessageProcessor`), 1 `PublicAPI` file, 1 manifest, 3 test files for the code plus 11 integration files, 4 documentation files; 3 pull requests.

**Affected packages**: `Encina.Messaging` only. The ten database stores are not edited; the processor is provider independent and the integration tests prove it on each store.

**Provider category**: Database (10) as consumers, through the one shared processor (`IScheduledMessageStore` implementations: ADO.NET x3, Dapper x3, EF Core, MongoDB, `src/Encina.*/Scheduling/ScheduledMessageStore*.cs`, plus the instrumented wrapper and `FakeScheduledMessageStore`, which need no change); Distributed locks as the engine (the adapter of #713 works over every `IDistributedLockProvider`).

**What the code does today** (`origin/main` at `e694eb032`):

- `ScheduledMessageProcessor : BackgroundService` (`src/Encina.Messaging/Scheduling/ScheduledMessageProcessor.cs`) takes `(IServiceProvider, SchedulingOptions, ILogger<ScheduledMessageProcessor>, TimeProvider?)`, loops `ProcessOnceAsync` then `Task.Delay(ProcessingInterval)`, and creates one `AsyncScope` per cycle from which it resolves `SchedulerOrchestrator` and `IScheduledMessageDispatcher`. Its remarks name the gap: "Without a lock, multi-replica deployments may process the same message concurrently. See Issue #716." (line 40).
- `SchedulerOrchestrator.ProcessDueMessagesAsync` (`SchedulerOrchestrator.cs` line 305) reads due messages, dispatches each, applies the retry policy and recurring rescheduling, and saves the outcomes (`SaveBatchOutcomesAsync`, line 368). It is called only by the processor (verified by grep over `src/`).
- `SchedulingOptions` has `ProcessingInterval` (30 s), `BatchSize`, `MaxRetries`, `BaseRetryDelay`, `EnableProcessor`, `EnableRecurringMessages` and no validation method.
- `IScheduledMessage` carries no tenant id (#739 adds it), so the cycle is cross-tenant.
- The shared lock contract (`IMessagingLockProvider`, `MessagingLockGate`, `MessagingLockMarker`, `MessagingLockNames.ScheduledProcess`, EventIds 7500-7503, the `MessagingLocking` range) does not exist yet: it is delivered by the #713 plan.
- The issue says "All 13 database providers": the matrix of `AGENTS.md` section 5 has 10.
- Manifest: `.github/coverage-manifest/Encina.Messaging.json` has `Scheduling/SchedulerOrchestrator.cs` (unit 90, guard 30) and no entry for `ScheduledMessageProcessor.cs`.

---

## Design Choices

<details>
<summary><strong>1. Granularity: one lock per cycle, one per message, or both</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One lock per cycle, `encina:scheduled:process`** | One acquire per cycle; the same shape as the outbox (#713); a replica that does not hold it does no store work at all | One replica at a time dispatches; a slow dispatch delays every due message of the batch |
| **B) One lock per message, `encina:scheduled:{id}`** | Replicas dispatch different messages in parallel | One acquire per due message; the due query still returns the same rows on every replica, so every replica polls and then contends per message; needs row claiming (#1251) to avoid the repeated read |
| **C) Both** | Cycle lock for polling, message lock for the dispatch | Two mechanisms and two lifetimes for one problem |

### Chosen Option: **A — one lock per cycle** (recommended, pending the maintainer)

### Rationale

- A wins for the single-writer setups this opt-in targets; B wins with many replicas and slow dispatches, and then row claiming (#1251) is the better tool because it also removes the repeated read.
- A cycle lock is the only option that holds across `SaveChangesAsync`, which is the one call that persists the outcomes.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Where the gate lives: the processor or the orchestrator</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) In `ScheduledMessageProcessor.ProcessOnceAsync`** | The processor owns the cycle and its scope; `SchedulerOrchestrator` (27 construction sites in tests, one scoped registration) is untouched; the only caller of `ProcessDueMessagesAsync` is the processor | A future caller of the orchestrator method is not covered |
| **B) In `SchedulerOrchestrator.ProcessDueMessagesAsync`** | Covers any caller | A new constructor dependency on a scoped orchestrator; the orchestrator would acquire a lock that spans its own save, which mixes two concerns |

### Chosen Option: **A — in the processor** (recommended, pending the maintainer)

### Rationale

- A wins while the processor is the only caller (today's code); B wins if a second entry point, for example an adapter that runs due messages from a Hangfire or Quartz job, is added; that adapter would then take the same gate helper from the processor's code path.
- The processor resolves the port and the marker from the cycle scope, so no singleton takes a scoped dependency.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Failure semantics of the gate</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Contention skips the cycle and is counted; a lock `Left` fails the cycle visibly (code logged, counted, retried at the next interval); never dispatch without the lock** | ADR-052 and the fail-closed rule; nothing is swallowed | A broken lock backend pauses scheduled dispatch |
| **B) On a lock `Left`, dispatch without the lock** | Dispatch continues | Double-firing exactly when coordination is unhealthy |
| **C) Contention is an error** | Simple | ADR-052 rejected it |

### Chosen Option: **A — skip on contention, fail visibly on a fault**

### Rationale

- The outer loop already catches non-cancellation exceptions per iteration and logs `CycleFailed`; a lock `Left` does not throw, it is logged by error code and counted like the existing `BatchFailed` path.
- Cancellation propagates, as in `EitherHelpers.TryAsync` (ADR-052).

**Class:** Settled
**Settled by:** ADR-052 (contention `Right(None)`, fault `Left`, cancellation propagates), `AGENTS.md` section 3 (errors are never swallowed in background infrastructure; gates fail closed)

</details>

<details>
<summary><strong>4. Lock lifetime for a dispatch cycle</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Fixed `SchedulingOptions.LockExpiry` (5 minutes, greater than `ProcessingInterval`, checked at startup only when the lock is on); EventId 7503 when the handle expired before the cycle ended** | No renewal loop | A batch longer than the expiry loses the lock while it runs |
| **B) Renew during the cycle (#223)** | Long batches keep the lock | A renewal task per cycle; `ProcessDueMessagesAsync` cannot be interrupted between messages today |

### Chosen Option: **A — fixed expiry, loss made visible** (recommended, pending the maintainer)

### Rationale

- The same question as Dilemma 4 of the #713 plan; the maintainer's answer there applies to this plan too. A wins while `BatchSize` (100) dispatches finish inside the expiry; B wins for slow dispatchers, and #223 then serves every consumer.

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Lock name</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `encina:scheduled:process`, a constant of `MessagingLockNames`** | One namespace with the other messaging locks; no clash with the `encina:leader:*` names of #717 | The issue's `:leader` suffix is dropped |
| **B) `encina:scheduled:process:leader` (the issue's text)** | Literal | Reads as a leader election lock, which is #717's job; a second name for the same resource if both land |

### Chosen Option: **A — `encina:scheduled:process`**

### Rationale

- The cycle is cross-tenant (`IScheduledMessage` has no tenant id until #739), so the name has no tenant segment and no tenant tag is emitted (ADR-054 D4 applies where a tenant exists). When #739 lands and an application wants per-tenant cycles, it lists the name in the tenant scoping of #741, which derives `@{tenant}:{name}` with the reserved-character and `default` tenant rules defined there.

**Class:** Trivial
**Settled by:** `MessagingLockNames` of the #713 plan (the one class that defines the family's names), ADR-054 D4, #741 (tenant derivation)

</details>

---

## Implementation Phases

### Phase 1: Options and the Cycle Gate — PR 1

> **Goal**: `ScheduledMessageProcessor` dispatches only while it holds the lock. Starts after PRs 1 to 3 of the #713 plan.

<details>
<summary><strong>Tasks</strong></summary>

1. **`Scheduling/SchedulingOptions.cs`** (modify) — `TimeSpan LockExpiry` (5 minutes) and `TimeSpan LockWait` (zero) with positive and non-negative setter checks; an internal `Validate(string paramName)` rejecting a `LockWait` greater than `LockExpiry`, called from the processor constructor (both properties are new, so no existing configuration changes meaning). `Validate` does NOT compare `LockExpiry` with `ProcessingInterval`: with the 5-minute default, a configured `ProcessingInterval` of 5 minutes or more would throw for an application that never enables locking (pay-for-what-you-use). That comparison lives in task 1b.
1b. **`Locking/MessagingLockStartupCheck.cs`** (modify, file of #713) — when the marker exists and `SchedulingOptions` is registered, also fail at `StartAsync` with the check's `InvalidOperationException` (naming both values) when `SchedulingOptions.LockExpiry` is not greater than `ProcessingInterval`; with the flag off nothing is compared. A test case is added to the check's unit tests (`MessagingLockStartupCheckTests`, file of #713).
2. **`Scheduling/ScheduledMessageProcessor.cs`** (modify) — in `ProcessOnceAsync`, after `CreateAsyncScope()`: resolve `IMessagingLockProvider?` and `MessagingLockMarker?` from `scope.ServiceProvider`, call `MessagingLockGate.TryEnterAsync(provider, marker, "scheduling", MessagingLockNames.ScheduledProcess, _options.LockExpiry, _options.LockWait, _logger, ct)`. `Left`: log the error code and return; `None`: return; `Some(handle)`: `await using` it around the existing body (activity start to the result handling), so the code order is: acquire, then start the activity, then run the orchestrator, then stop the activity in its own `finally` (the existing code), and only then dispose the handle; a skipped cycle (`None` or `Left`) returns before `StartProcessingCycle`, so it starts no activity. Log EventId 7503 when `handle.ExpiresAtUtc` is earlier than `_timeProvider.GetUtcNow()` after the body. The constructor signature does not change (6 construction sites in 2 test files stay valid), so no new `T?` constructor parameter needs a `= null` default. The call passes no `tenantId`: the optional trailing parameter of the gate (delivered by #713 PR 2) stays null because the cycle is cross-tenant, so this plan does not depend on #714. Update the remarks: the "Distributed locks" bullet now says "opt-in through `UseDistributedLocking` (#716)".
3. **`PublicAPI.Unshipped.txt`** (Messaging): the two options properties, through `mechanical-fixer`.
4. **Cross-cutting**: no new log or metric type; the gate of #713 records `encina.messaging.lock.attempts_total{pattern="scheduling"}` and logs 7500, 7501 and 7503.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of issue #716: the lock around the scheduled message dispatch cycle.

CONTEXT:
- The shared contract (IMessagingLockProvider, MessagingLockGate, MessagingLockMarker, MessagingLockNames, UseDistributedLocking,
  EventIds 7500-7503) is delivered by the plan of #713; do not recreate it.
- ScheduledMessageProcessor creates one AsyncScope per cycle and SchedulerOrchestrator.ProcessDueMessagesAsync saves the
  batch outcomes before it returns. The handle must be disposed after that call returns.

TASK:
Add LockExpiry and LockWait to SchedulingOptions with validation, and gate ProcessOnceAsync as listed in the Phase 1
tasks. Unit tests with a fake store, a substituted port and a FakeTimeProvider: flag off (no port call), acquired (the
orchestrator runs, handle disposed after it), contended (no store call), Left (no store call, code logged), expired
handle (7503), a throwing orchestrator still disposes the handle, cancellation.

KEY RULES:
- Fail closed: a Left never leads to dispatch without the lock. Do not call the lock atomic or transactional.
- Resolve the port from the cycle scope; log codes, never EncinaError.Message.

REFERENCE FILES: src/Encina.Messaging/Scheduling/ScheduledMessageProcessor.cs, src/Encina.Messaging/Scheduling/SchedulingOptions.cs,
tests/Encina.UnitTests/Messaging/Processors, docs/plans/outbox-distributed-lock-implementation-plan-713.md.
```

</details>

### Phase 2: Observability and Cross-Cutting Integration

> **Goal**: Record what is reused and what is not added.

<details>
<summary><strong>Tasks</strong></summary>

1. No new EventIds and no new error code (so nothing to categorize under ADR-062): this plan uses 7500-7503 of the `MessagingLocking` range registered by the #713 plan (PR 2 of that plan merges first).
2. The cycle activity (`SchedulingActivitySource.StartProcessingCycle`) is started only after the lock is held, so a skipped cycle creates no span; a lock failure sets no activity status because none exists yet, and is visible through the counter and log 7500.
3. No tenant tag: the cycle is cross-tenant (Design Choice 5).
4. Health, resilience, audit and idempotency are evaluated in the matrix; none adds code here.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are verifying Phase 2 of issue #716: observability reuse.

CONTEXT:
- The lock metrics and logs come from the #713 plan; the scheduling cycle span exists in SchedulingActivitySource.

TASK:
Confirm with tests that a skipped cycle starts no activity and that the gate's counter carries pattern "scheduling" for
acquired, contended and failed. Add no new log or metric type.

KEY RULES:
- Tags are bounded; never put a lock name with an id or an error message in a tag or log.

REFERENCE FILES: src/Encina.Messaging/Scheduling/ScheduledMessageProcessor.cs, src/Encina.Messaging/Diagnostics/SchedulingProcessorMetrics.cs,
src/Encina.Messaging/Diagnostics/SchedulingProcessorLog.cs.
```

</details>

### Phase 3: Testing — PRs 1, 2 and 3

> **Goal**: Every flag reaches its target and every store proves the adoption.

<details>
<summary><strong>Tasks</strong></summary>

1. **Unit and guard** (PR 1): `ScheduledMessageProcessorLockTests`, `SchedulingOptionsLockTests` (setters, `Validate`), guard cases for the new setters in the existing guard file for `SchedulingOptions` (new file `SchedulingOptionsLockGuardTests` if none exists).
2. **Contract**: none new; the port is contract-tested by the #713 plan. **Property**: not applicable (no new logic beyond comparisons).
3. **Integration** (PRs 2 and 3): an abstract `ScheduledMessageLockIntegrationTestsBase` under `tests/Encina.IntegrationTests/Messaging/` runs two processors over one store and one Redis lock with N due messages and asserts that each message is dispatched once while both run; one concrete class per provider in its `[Collection("<Family>-<Database>")]`: PR 2 has ADO and Dapper on SqlServer, PostgreSQL and MySQL (six classes), PR 3 has EF Core on the same three and MongoDB (four). All ten providers are covered.
4. **Load and benchmark**: justified in `.md` files (one acquire per cycle).
5. **Per-file targets**:

| File | Unit | Guard | Contract | Property | Integration | Justification |
|------|-----:|------:|---------:|---------:|------------:|---------------|
| `Scheduling/ScheduledMessageProcessor.cs` | 90 | 40 | 0 | 0 | 60 | New entry (no target exists today); every gate branch is unit-testable and the two-processor test runs it on each store; contract and property 0: no public interface and no pure logic |
| `Scheduling/SchedulingOptions.cs` | 95 | 40 | 0 | 0 | 0 | Setters and `Validate` are directly drivable; guard covers the setter checks |
| `Locking/MessagingLockStartupCheck.cs` (changed lines) | 90 | 0 | 0 | 0 | 0 | The target set by #713 is kept; the new rule adds unit cases; internal, so guard 0 |

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are implementing Phase 3 of issue #716: tests across the ten providers.

CONTEXT:
- Integration tests use shared [Collection("<Family>-<Database>")] fixtures and ClearAllDataAsync in InitializeAsync; the
  Redis lock comes from RedisFixture. Tests execute real code and assert with Shouldly through Encina.Testing.Shouldly.

TASK:
Write the abstract two-processor base and the ten concrete classes, the unit and guard files, and the .md justifications
for load and benchmark tests. Report the measured per-flag coverage of the two files against the Phase 3 targets.

KEY RULES:
- No Thread.Sleep; a bounded poll for the dispatch count.
- Never call a fixture's DisposeAsync from a test.

REFERENCE FILES: tests/Encina.IntegrationTests/Messaging, tests/Encina.UnitTests/Messaging/Scheduling/SchedulerOrchestratorTests.cs,
docs/testing/integration-tests.md.
```

</details>

### Phase 4: Documentation and Finalization — PR 3

> **Goal**: Users know when to enable the lock and what it does not guarantee.

<details>
<summary><strong>Tasks</strong></summary>

1. XML docs on `LockExpiry` and `LockWait`, naming the flag and the registration.
2. `changelog.d/716-scheduled-message-distributed-lock.added.md`, through `mechanical-fixer`.
3. The scheduling section of the messaging locks page created by #713 PR 5 (this step starts after #713 PR 5 merges), and `src/Encina.Messaging/README.md`, through `docs-writer`: the single-writer limits and the relation to #735 and #1251.
4. Build and test verification: zero warnings, per-flag targets met.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
You are finishing issue #716: documentation and verification.

CONTEXT:
- Documentation goes through docs-writer; the fragment and PublicAPI lines through mechanical-fixer.

TASK:
Complete the XML docs, request the fragment and the documentation, run the build and the tests.

KEY RULES:
- State the limit: a crash after dispatch and before the save re-dispatches the message (#735); the lock is a lease.
- Write "the reference application", never a private client name.

REFERENCE FILES: changelog.d/README.md, src/Encina.Messaging/README.md.
```

</details>

---

## Research

### Relevant Standards and Specifications

| Source | Relevance |
|--------|-----------|
| ADR-052 | Contention `Right(None)`, fault `Left`, cancellation propagates |
| ADR-050 | The Scheduling row lists "row claiming or a lock" (#1251) |
| ADR-054 D4 | No tenant tag while the cycle has no tenant |
| SPEC-000 REQ-027 / DEC-009 | The seven lock providers behind the adapter |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `ScheduledMessageProcessor` | `src/Encina.Messaging/Scheduling/ScheduledMessageProcessor.cs` | The adoption point |
| `SchedulerOrchestrator.ProcessDueMessagesAsync` | `src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs` | Ends with the save of the batch outcomes |
| `MessagingLockGate`, `MessagingLockMarker`, `MessagingLockNames` | #713 plan, `src/Encina.Messaging/Locking/` | The shared contract |
| `SchedulingActivitySource`, `SchedulingProcessorLog` | `src/Encina.Messaging/Diagnostics/` | Cycle span and error log |
| `RedisFixture` | `tests/Encina.TestInfrastructure/RedisFixture.cs` | Real lock in integration tests |

### Provider Matrix (database stores as consumers)

| Family | Providers | Notes |
|--------|-----------|-------|
| ADO.NET | SqlServer, PostgreSQL, MySQL | `ScheduledMessageStoreADO`; one integration class each |
| Dapper | SqlServer, PostgreSQL, MySQL | `ScheduledMessageStoreDapper`; one integration class each |
| EF Core | SqlServer, PostgreSQL, MySQL | One `ScheduledMessageStoreEF`; three integration classes (one per database) |
| MongoDB | MongoDB | `ScheduledMessageStoreMongoDB` |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Messaging` (`MessagingLocking`, registered by the #713 plan) | 7500-7503 | Reused: acquire failed, not acquired, release failed, expired before completion; this plan adds no id |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Production (modified) | 3 | `SchedulingOptions`, `ScheduledMessageProcessor`, `MessagingLockStartupCheck` (file of #713) |
| `PublicAPI.Unshipped.txt` and manifest | 2 | Messaging |
| Tests | 15 | Unit (2), guard (1), `MessagingLockStartupCheckTests` (modified, file of #713) (1), integration base plus ten classes (11) |
| Documentation | 5 | Changelog fragment, page section, README, two `.md` justifications |

PR split (each at most 15 files and 1500 lines, each building and passing alone):

| PR | Content | Files | Lines (est.) |
|----|---------|------:|-------------:|
| 1 | Phases 1 and 2: options, processor gate, startup-check rule, unit and guard tests, startup-check test case, `PublicAPI`, manifest; starts after PRs 1 to 3 of the #713 plan | 9 | 550 |
| 2 | Phase 3 integration: base plus six ADO and Dapper classes | 7 | 600 |
| 3 | Phase 3 integration: EF Core x3 and MongoDB; Phase 4: fragment, README, page section, justifications | 9 | 600 |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Combined prompt — all phases of issue #716</strong></summary>

```
PROJECT CONTEXT:
Encina is a pre-1.0 .NET 10 / C# 14 library with Railway Oriented Programming (Either<EncinaError, T>). Issue #716 adds
the scheduling adoption of the messaging lock: ScheduledMessageProcessor dispatches only while it holds
encina:scheduled:process. The shared contract is delivered by the plan of #713. Read AGENTS.md sections 3, 5, 6 and 9,
ADR-050 and ADR-052 first.

IMPLEMENTATION OVERVIEW:
1. SchedulingOptions.LockExpiry and LockWait with validation.
2. ScheduledMessageProcessor.ProcessOnceAsync: gate after the scope is created, dispose after the orchestrator (which
   saves the outcomes) returns; contention skips, Left fails visibly, no dispatch without the lock.
3. Unit and guard tests; a two-processor integration base and ten provider classes; docs and fragment through specialists.

KEY PATTERNS:
- Fail closed; log codes only; contention is not an error; the lock is a lease without fencing and never "atomic".
- Resolve the port from the cycle scope; TimeProvider for clock reads; Shouldly via Encina.Testing.Shouldly.

REFERENCE FILES: src/Encina.Messaging/Scheduling/ScheduledMessageProcessor.cs, src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs,
docs/plans/outbox-distributed-lock-implementation-plan-713.md.
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Coordination state is not cacheable |
| 2 | OpenTelemetry | ✅ | Phase 2: the counter of #713 with `pattern="scheduling"`; the cycle span starts after the lock; no tenant tag (cross-tenant cycle) |
| 3 | Structured Logging | ✅ | Reuses 7500-7503; no new id |
| 4 | Health Checks | ⏭️ | Processor liveness is #2396; no new checkable dependency |
| 5 | Validation | ✅ | Phase 1: `SchedulingOptions.Validate` for the two new options; the `LockExpiry` versus `ProcessingInterval` rule in the startup check, only with the flag on |
| 6 | Resilience | ❌ | A failed acquire is retried at the next interval by design |
| 7 | Distributed Locks | ✅ | The feature itself |
| 8 | Transactions | ❌ | The lock is not a transaction; the orchestrator's save is unchanged |
| 9 | Idempotency | ⏭️ | Re-execution after a crash is #735 (open) |
| 10 | Multi-Tenancy | ⏭️ | The cycle is cross-tenant until #739 adds a tenant id to scheduled messages; #741 scopes the name per tenant if wanted |
| 11 | Module Isolation | ❌ | Not applicable |
| 12 | Audit Trail | ⏭️ | Processor audit events are #749 (open) |

---

## Prerequisites & Dependencies

- The #713 plan's PRs 1 to 3 (port, gate, marker, flag, adapter, startup check, EventId range) must merge first; PR 3 of that plan needs #2667. The documentation step of PR 3 here also needs #713 PR 5 (the messaging locks page).
- Not blocked by another issue's open maintainer decision: the dilemmas that matter here (lock lifetime, flag) are #713's and are answered once for both plans.
- No follow-up issue file is needed: #735, #739, #749, #2396, #223 and #218 are open issues named above.

## Next Steps

1. Open the plan PR; when work on #716 starts, ask the maintainer to answer Dilemmas 1, 2 and 4 and record them in `## Maintainer Decisions`.
2. Brief PR 1 only after PR 3 of the #713 plan merged; briefs for PRs 2 and 3 follow.
