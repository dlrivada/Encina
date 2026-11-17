# Implementation Plan: `Encina.Messaging` idempotent re-execution of scheduled message dispatch

> **Issue**: [#735](https://github.com/dlrivada/Encina/issues/735)
> **Type**: Feature
> **Verified against**: origin/main `ec129cae5` (2026-11-17)
> **Complexity**: Medium (4 phases, tests on all 10 database providers, ~38 files in 5 pull requests)
> **Estimated Scope**: ~300-420 lines of production code + ~900-1,200 lines of tests

---

## Summary

A scheduled message can be dispatched twice: the process crashes after the handler ran and before the message is marked processed, or another host reads the same due row. Row claiming ([#1251](https://github.com/dlrivada/Encina/issues/1251)) and the cycle lock ([#716](https://github.com/dlrivada/Encina/issues/716)) reduce concurrent double dispatch; neither closes the crash window, and the #1251 plan states it ("delivery stays at-least-once; consumer-side deduplication is #1220 (outbox) and #735 (scheduled)"). Issue #735 is the consumer-side half for scheduling: a dispatch must carry a stable identity so a duplicate is recognised, a re-dispatch must be detectable, and the module must not run without that protection (ADR-050, Scheduling row: "dispatch idempotency is required, startup validation").

**What the code does today (verified on `origin/main` `ec129cae5`)**:

- `ScheduledMessageProcessor` (`src/Encina.Messaging/Scheduling/ScheduledMessageProcessor.cs:151`) calls `SchedulerOrchestrator.ProcessDueMessagesAsync` with the callback `(msg, type, req, ct) => dispatcher.DispatchAsync(type, req, ct)`. The dispatch carries **no identity at all**: `IScheduledMessageDispatcher.DispatchAsync(Type, object, CancellationToken)` has no context, and `CompiledExpressionScheduledMessageDispatcher` compiles `IEncina.Send(request, ct)` or `IEncina.Publish(notification, ct)`, which build their own context with no idempotency key.
- The outcome of a successful dispatch is written afterwards: `CompleteDispatchedMessageAsync` marks a one-shot message processed or reschedules a recurring one (`RescheduleRecurringMessageAsync` sets `ScheduledAtUtc` to the next occurrence and resets `RetryCount`, for example `src/Encina.EntityFrameworkCore/Scheduling/ScheduledMessageStoreEF.cs:130`), and `SaveBatchOutcomesAsync` saves them after the batch (`SchedulerOrchestrator.cs`). A crash anywhere between the handler returning and that save redelivers the message.
- Two downstream mechanisms can already deduplicate a dispatch that carries `IRequestContext.IdempotencyKey`: `InboxPipelineBehavior` (requests marked `IIdempotentRequest`) and `DistributedIdempotencyPipelineBehavior` (requests marked `IDistributedIdempotentRequest`); notifications get one only through [#733](https://github.com/dlrivada/Encina/issues/733). Nothing connects them to the scheduler.
- `SchedulingOptions` has `ProcessingInterval`, `BatchSize`, `MaxRetries`, `BaseRetryDelay`, `EnableProcessor`, `EnableRecurringMessages`; there is no idempotency setting and no startup check for scheduling (the pattern exists: `src/Encina.Messaging/Recoverability/DelayedRetryStartupCheck.cs`, an `IHostedService` asking `IServiceProviderIsService`).
- The Hangfire and Quartz adapters are future work (AGENTS.md section 3, Scheduling), so `IScheduledMessageDispatcher` has one implementer and one production caller (verified by grep over `src/` and `tests/`).

What it implements:

- A stable dispatch key `ScheduledDispatchKey.Compose(message.Id, message.ScheduledAtUtc)`, set as `IRequestContext.IdempotencyKey` on every dispatch: `IScheduledMessageDispatcher.DispatchAsync` takes the context and `CompiledExpressionScheduledMessageDispatcher` uses the `Send(request, context, ct)` and `Publish(notification, context, ct)` overloads.
- Re-dispatch detection: a message dispatched again after an unfinished earlier dispatch is counted and logged (Design Choice 5).
- A startup check that a host using Scheduling has a consumer able to deduplicate the key, or has accepted at-least-once dispatch explicitly (Design Choice 3, ADR-050).

**Delivery guarantee**: at-least-once dispatch with deduplication by the consumer, never exactly-once. The key makes a duplicate dispatch harmless for requests and notifications that are marked for the Inbox or the cache idempotency behavior; an unmarked handler still runs twice after a crash, and the documentation says so. No text of this plan says "atomic" or "dispatched exactly once".

Standards and specifications: ADR-050 (Scheduling row: dispatch idempotency required, startup validation); ADR-001 and ADR-006 (ROP); ADR-048 (inbox record versus business transaction); ADR-054 D3 and D4 (package-named meter, tenant tag when tenancy is on); ADR-062 (error category set where the error is created); AGENTS.md section 3 (a store `Left` fails the operation; background loops never let an exception escape; gates fail closed; `TimeProvider` for time); the idempotent consumer pattern (named comparable products: MassTransit and Wolverine inbox deduplication).

Estimated scope: about 38 distinct files (4 new and 7 modified production files, 7 metadata files, 18 test files, 2 documentation files), shipped as five pull requests (A to E; see "Pull request split").

Affected packages: `Encina.Messaging` (key, dispatcher contract, processor, options, startup check, telemetry, registration) and `Encina` (`EventIdRanges.cs` only). **No provider package changes**: the key is derived from columns every store already persists (`Id`, `ScheduledAtUtc`); the 10 providers are proven by tests. The issue says "All 13 database provider packages"; AGENTS.md section 5 has 10.

Provider category: Database (10): ADO.NET x3, Dapper x3, EF Core x3 (one implementation `ScheduledMessageStoreEF`, verified on three databases) and MongoDB, through the stores' own `GetDueMessagesAsync` and `RescheduleRecurringMessageAsync`; Scheduling (built-in `Encina.Messaging`).

---

## Design Choices

<details>
<summary><strong>1. The mechanism — propagate a stable key to the consumer, wrap the dispatch in the Inbox, or keep a dispatch ledger</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Propagate `IdempotencyKey` through the dispatch context; the consumer deduplicates (Inbox or cache behavior for marked requests; the notification gate of [#733](https://github.com/dlrivada/Encina/issues/733))** | No new table and no new store method on 10 providers; the same key a retry and a crash redelivery produce; consistent with outbox dispatch ([#1220](https://github.com/dlrivada/Encina/issues/1220)) | An unmarked handler is still at-least-once; the guarantee depends on the consumer's marker |
| **B) The scheduler wraps every dispatch in `InboxOrchestrator.ProcessAsync` with the key as message id** | Deduplicates unmarked handlers too, with no change to handlers | Scheduler retries and the inbox both count failures ([#2084](https://github.com/dlrivada/Encina/issues/2084)); the inbox stores a handler `Left` as the permanent response ([#3441](https://github.com/dlrivada/Encina/issues/3441)), so a failed scheduled message would be replayed as failed on every scheduler retry and never re-run; requires the Inbox for every scheduler user |
| **C) A dispatch ledger (`IScheduledDispatchLedger`) on the 10 providers: write "dispatched" before or with the dispatch, check it first** | Scheduler-owned, independent of handlers | About 35 provider files and a new table; a record written before the dispatch loses the message on a crash, one written after has the same crash window as the processed mark itself |

### Chosen Option: **A — propagate a stable key** (recommended, pending the maintainer)

### Rationale

- A wins when the handlers that matter (payments, notifications) already use the Inbox or the cache idempotency behavior, or are naturally idempotent, which is what the issue's "defense-in-depth layer" describes. B wins once #3441 ships a retryable error class and the maintainer wants unmarked handlers protected too. C wins only if neither the Inbox nor the cache behavior is acceptable to the application and a durable dispatch history is a requirement of its own.
- The issue's acceptance "scheduled messages dispatched at most once" cannot hold in any option for a crash after the side effect and before the record; under A the maintainer's answer settles that criterion's wording (at-least-once with deduplication).
- **Class:** Dilemma

</details>

<details>
<summary><strong>2. The key — message id plus the occurrence's scheduled time</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `"{Id:D}:{ScheduledAtUtc.Ticks}"` (the GUID has a fixed length, so the two parts cannot shift into each other)** | Distinct per occurrence of a recurring message; the same for the retries and the crash redelivery of one occurrence (a failed dispatch does not change `ScheduledAtUtc`; a reschedule sets the next occurrence and resets `RetryCount`) | The key changes when an operator edits `ScheduledAtUtc` of a pending row (a new occurrence by definition) |
| **B) `"{Id:D}"` only** | Simplest | The second occurrence of a recurring message would be deduplicated against the first: recurring messages stop running |
| **C) `"{Id:D}:{RetryCount}"`** | Distinguishes attempts | A crash redelivery with the same `RetryCount` is not the problem; a failed-then-retried dispatch gets a new key, so a duplicate side effect of the first attempt is not recognised |

### Chosen Option: **A — id and occurrence ticks**

### Rationale

- B fails the recurring-message requirement and C fails the redelivery requirement; only A satisfies both. The tenant is not part of the dispatch key: it is composed by the consumer (the Inbox key of [#3437](https://github.com/dlrivada/Encina/issues/3437) or the cache key of [#2452](https://github.com/dlrivada/Encina/issues/2452)) from the context's tenant, which scheduled messages will carry once [#739](https://github.com/dlrivada/Encina/issues/739) lands.
- **Class:** Settled
- **Settled by:** `src/Encina.EntityFrameworkCore/Scheduling/ScheduledMessageStoreEF.cs:130` (`RescheduleRecurringMessageAsync` sets `ScheduledAtUtc` to the next occurrence and resets `RetryCount` and `NextRetryAtUtc`; a failed dispatch goes through `SchedulerOrchestrator.MarkAsFailedAsync`, which records the retry state and leaves `ScheduledAtUtc` as it is) and the #1220 plan, Design Choice 1 (the delivery's own id is the idempotency key)

</details>

<details>
<summary><strong>3. The startup check required by ADR-050 — what must exist, and the explicit opt-out</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `SchedulingDispatchStartupCheck` (an `IHostedService`, like `DelayedRetryStartupCheck`) fails the host start when Scheduling is on and neither the Inbox (`UseInbox`) nor the cache idempotency behavior (`EnableDistributedIdempotency`) is registered, unless `SchedulingOptions.AcceptAtLeastOnceDispatch = true`; the opt-out is explicit and logged at startup** | Meets ADR-050 ("enabling the module without it is a startup error"); fail-closed with an explicit, logged opt-out (AGENTS.md section 3) | A host with naturally idempotent handlers must set one option |
| **B) No check; the key is always propagated** | No configuration | ADR-050's startup validation is not met; a host with no consumer believes it is protected |
| **C) A flag `UseScheduledMessageIdempotency` as the issue proposes** | Matches the issue's wording | ADR-050 makes the protection required, not opt-in; a flag that can be off contradicts "required" |

### Chosen Option: **A — startup check with an explicit opt-out** (recommended, pending the maintainer)

### Rationale

- A wins when the maintainer reads ADR-050 literally ("Scheduling requires idempotent dispatch"); B wins if ADR-050 is amended to "key propagation is built in and always on, nothing to validate"; C is rejected by ADR-050 itself. What counts as "a consumer able to deduplicate" (the two behaviors, and the #733 gate) is a closed list in the check, extended when a new mechanism ships.
- The ADR-050 table row for Scheduling is amended by the documentation pull request (mechanism column: "startup check `SchedulingDispatchStartupCheck`").
- **Class:** Dilemma

</details>

<details>
<summary><strong>4. The dispatcher contract — carry the context in the signature</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Replace the signature: `DispatchAsync(Type requestType, object request, IRequestContext context, CancellationToken cancellationToken)`** | Same parameter order as `IEncina.Send(request, context, ct)`; no compatibility layer; one implementer and one caller | 4 test files change (the dispatcher's unit, guard and contract tests and `ErrorMessageLeakTests`) |
| **B) Keep the old method and add an overload with the context** | Old tests untouched | Two entry points for one operation; forbidden pre-1.0 |
| **C) A nullable optional context parameter** | One method | A caller can forget the context and silently lose the key (fail open) |

### Chosen Option: **A — replace the signature**

### Rationale

- A required context makes it impossible for a dispatch to lose its key without a compile error.
- **Class:** Settled
- **Settled by:** AGENTS.md section 3 (no compatibility layers or aliases: if something must change, change it completely) and `src/Encina/Abstractions/IEncina.cs` (the existing order is request, context, cancellation token)

</details>

<details>
<summary><strong>5. Detecting an incomplete dispatch — the lease reclaim of #1251, or a persisted "dispatch started" marker</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Use the row claim of [#1251](https://github.com/dlrivada/Encina/issues/1251): a message claimed again after its lease expired unfinished is a re-dispatch; count it (`encina.messaging.scheduling.redispatches`) and log it with the message id** | No new column on 10 providers; the information is already in the claim state | Depends on #1251's claim result exposing "reclaimed after expiry" (if it does not, PR C adds that bit, one file); before #1251 lands nothing can be detected |
| **B) A new `DispatchStartedAtUtc` column on the scheduled message tables of the 10 providers, written before the dispatch** | Detectable without #1251 | A schema change on 10 providers (about 35 files); a write before every dispatch; the same information as the lease |

### Chosen Option: **A — detect through the claim of #1251** (recommended, pending the maintainer)

### Rationale

- A wins because #1251 is a required module of Scheduling (ADR-050) and the lease already marks "claimed and not finished"; B wins if the maintainer wants detection to work for a host that cannot use row claiming.
- Detection is observability, not prevention: prevention is the key (Design Choice 1).
- **Class:** Dilemma

</details>

---

## Implementation Phases

### Phase 1: Core Models and the Dispatcher Contract

<details>
<summary><strong>Tasks</strong></summary>

1. **`src/Encina.Messaging/Scheduling/ScheduledDispatchKey.cs`** (new, internal static): `string Compose(Guid messageId, DateTime scheduledAtUtc)` returning `"{messageId:D}:{scheduledAtUtc.Ticks}"` (`Ticks` ignores `Kind`, which stores return as `Unspecified` on some providers).
2. **`src/Encina.Messaging/Scheduling/IScheduledMessageDispatcher.cs`** (modify): `DispatchAsync(Type requestType, object request, IRequestContext context, CancellationToken cancellationToken)`.
3. **`src/Encina.Messaging/Scheduling/CompiledExpressionScheduledMessageDispatcher.cs`** (modify): the compiled delegates take the context and call `Send(request, context, ct)` and `Publish(notification, context, ct)` (the `Single(m => m.Name == ... && GetParameters().Length == 2)` lookups become 3-parameter lookups); `ArgumentNullException.ThrowIfNull(context)`.
4. **`src/Encina.Messaging/Scheduling/ScheduledMessageProcessor.cs`** (modify): the callback builds the context for each message: the ambient context of the scope when there is one, otherwise a fresh anonymous `RequestContext.Create(timeProvider)`, then `.WithIdempotencyKey(ScheduledDispatchKey.Compose(msg.Id, msg.ScheduledAtUtc))`. The identity is unchanged from today (a scheduled dispatch runs as the anonymous or ambient identity it ran as before); running scheduled work under a declared service identity belongs to [#739](https://github.com/dlrivada/Encina/issues/739) and the webhook identity decision of [#3388](https://github.com/dlrivada/Encina/issues/3388).
5. Existing callers and tests of the old signature change in the same pull request: the dispatcher's unit, guard and contract tests and `ErrorMessageLeakTests` (4 files, found by grep on `origin/main`).
6. `PublicAPI.Unshipped.txt` of `Encina.Messaging`: the changed `DispatchAsync` signature (RS0017 and RS0016).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT: Encina is pre-1.0, .NET 10 / C# 14, nullable enabled, no backward compatibility. Issue #735 gives every
scheduled dispatch a stable idempotency key; the consumer (Inbox, cache idempotency, the #733 gate) deduplicates.
TASK: Add ScheduledDispatchKey.Compose, change IScheduledMessageDispatcher.DispatchAsync to take IRequestContext (before
the cancellation token), use the Send/Publish context overloads in the compiled delegates, and build the context with the
key in ScheduledMessageProcessor's callback.
KEY RULES: replace the signature, never keep it beside a new one; update every caller and test in the same pull request;
XML docs; guard clauses; the dispatch key and EncinaError.Message never reach logs; PublicAPI.Unshipped.txt; zero warnings.
REFERENCE FILES: src/Encina.Messaging/Scheduling/CompiledExpressionScheduledMessageDispatcher.cs,
src/Encina.Messaging/Scheduling/ScheduledMessageProcessor.cs, src/Encina/Abstractions/IEncina.cs,
tests/Encina.UnitTests/Messaging/Scheduling/CompiledExpressionScheduledMessageDispatcherTests.cs
```

</details>

### Phase 2: Configuration, Startup Check and Registration

<details>
<summary><strong>Tasks</strong></summary>

1. **`src/Encina.Messaging/Scheduling/SchedulingOptions.cs`** (modify): `AcceptAtLeastOnceDispatch` (bool, default `false`) with XML docs stating the delivery guarantee and that setting it is logged at every start.
2. **`src/Encina.Messaging/Scheduling/SchedulingDispatchStartupCheck.cs`** (new, internal sealed `IHostedService`, same shape as `DelayedRetryStartupCheck`): at `StartAsync`, when Scheduling is registered, ask the built container (`IServiceProviderIsService`) whether the Inbox store, the `DistributedIdempotencyPipelineBehavior` registration or the #733 gate exist; if none and `AcceptAtLeastOnceDispatch` is false, fail the start with a message naming the three ways out (enable the Inbox, enable cache idempotency, set the option); when the option is set, log the opt-out (EventId 7840; the failure itself is 7841). The check needs no scoped service.
3. **`src/Encina.Messaging/MessagingServiceCollectionExtensions.cs`** (modify): register the check in `RegisterScheduling` as `TryAddEnumerable` hosted service; the DI test builds the provider with `ValidateOnBuild` and `ValidateScopes`.
4. The check runs only with a generic host (as `DelayedRetryStartupCheck` does); a host without one is documented in the guide.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT: issue #735, Phase 2. ADR-050 requires dispatch idempotency of the Scheduling module with startup validation.
TASK: Add SchedulingOptions.AcceptAtLeastOnceDispatch and SchedulingDispatchStartupCheck (model: DelayedRetryStartupCheck),
register it with the scheduling registration.
KEY RULES: fail closed: no consumer and no explicit option means the host does not start; the opt-out is logged; no scoped
service in a singleton; the check never throws from a background loop (it runs once at start); XML docs.
REFERENCE FILES: src/Encina.Messaging/Recoverability/DelayedRetryStartupCheck.cs,
src/Encina.Messaging/MessagingServiceCollectionExtensions.cs, docs/architecture/adr/050-modules-may-require-modules.md
```

</details>

### Phase 3: Observability and Detection

<details>
<summary><strong>Tasks</strong></summary>

1. **`src/Encina.Messaging/Diagnostics/ScheduledDispatchIdempotencyMetrics.cs`** (new, internal static): meter `Encina.Messaging` (ADR-054 D3, no `_total`), counter `encina.messaging.scheduling.redispatches` with the tag `tenant` only when the `MultiTenancyMarker` is registered (ADR-054 D4); no message id or request type as a tag.
2. **`src/Encina.Messaging/Diagnostics/SchedulingProcessorLog.cs`** is not edited (its ids belong to the existing `MessagingScheduling` range); the new entries go to **`ScheduledDispatchIdempotencyLog.cs`** (new, `[LoggerMessage]`): EventIds 7840 (`AtLeastOnceDispatchAccepted`, Warning, written by PR B), 7841 (`NoDeduplicatingConsumer`, Error, the startup failure, written by PR B), 7842 (`RedispatchDetected`, Warning, message id only), in the new range `MessagingScheduledIdempotency = (7840, 7849)` registered first in `src/Encina/Diagnostics/EventIdRanges.cs` and mapped to `Encina.Messaging` in the `AssemblyRanges` map of `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs`.
3. **`src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs`** (modify, after #1251): when the claim result marks a message as reclaimed after an expired lease, call the counter and the log entry before dispatching it; the dispatch itself is unchanged (the key makes it safe).
4. The orchestrator method that gets the new branch stays under CRAP 10: the detection is its own private method.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT: issue #735, Phase 3. ADR-054 D3/D4 govern meters and tenant tags; ADR-021 governs EventIds. Row claiming
(#1251) is merged and its claim result says whether a row was reclaimed after an expired lease.
TASK: Add ScheduledDispatchIdempotencyMetrics and ScheduledDispatchIdempotencyLog (EventIds 7840-7842), register the range
MessagingScheduledIdempotency (7840, 7849) first, and count and log a reclaimed message in SchedulerOrchestrator.
KEY RULES: [LoggerMessage] source generator; ids packed from 7840 without gaps; never log the key, the payload or
EncinaError.Message; bounded metric dimensions; update the allocation test map; background errors are never swallowed.
REFERENCE FILES: src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs, src/Encina.Messaging/Diagnostics/SchedulingProcessorMetrics.cs,
src/Encina/Diagnostics/EventIdRanges.cs, docs/plans/processor-row-claiming-implementation-plan-1251.md
```

</details>

### Phase 4: Testing and Documentation

<details>
<summary><strong>Tasks</strong></summary>

1. Unit (`tests/Encina.UnitTests/Messaging/Scheduling/`): `ScheduledDispatchKeyTests` (distinct per occurrence, identical across retries, `Kind` independent), the dispatcher tests rewritten for the context (the compiled delegates pass the context and the key reaches a recording `IEncina`), processor tests (the callback sets the key; a recurring message's two occurrences get two keys), `SchedulingDispatchStartupCheckTests` (no consumer fails, each consumer passes, opt-out passes and logs), redispatch detection (counter and log).
2. Guard (`Encina.GuardTests`): dispatcher `DispatchAsync` null context, startup check constructor, key composer is internal (no guard test reachable).
3. Contract: the existing `CompiledExpressionScheduledMessageDispatcherContractTests` updated for the context.
4. Integration (`Encina.IntegrationTests`, shared `[Collection]` fixtures): one shared scenario `ScheduledDispatchKeyScenario` run on the 10 providers through the existing scheduling store and cycle test files (ADO.NET x3, Dapper x3, EF Core x3, MongoDB): schedule a recurring message, dispatch it twice without saving the outcome (the simulated crash) and assert the same key both times; save, reschedule, and assert the next occurrence has another key; with an Inbox-marked request the handler runs once.
5. DI: the Messaging DI test extended with the startup check (`ValidateOnBuild`, `ValidateScopes`).
6. Load and benchmark: justification files (one string composition and one context per dispatch).
7. Documentation through `docs-writer` and `mechanical-fixer`: XML docs, changelog fragment `changelog.d/735-scheduled-dispatch-idempotency.added.md`, the scheduling guide (the delivery guarantee, the three ways to satisfy the check), the ADR-050 table row for Scheduling, the `ScheduledMessageProcessor` remarks, `docs/INVENTORY.md`.
8. Verification: `dotnet build --configuration Release` (0 warnings), `dotnet test`, per-flag coverage against the table below, `crap-gate` table for the changed methods.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT: issue #735. Tests must execute real package code. Database features need integration tests on real databases
through shared [Collection] fixtures on all 10 providers; a .md justification is never allowed for them.
TASK: Write the unit, guard, contract, integration and DI tests listed in the plan and the justification files for load
and benchmark; hand documentation to docs-writer.
KEY RULES: Shouldly through Encina.Testing.Shouldly; FakeTimeProvider; no Thread.Sleep; fixtures through the constructor
with ClearAllDataAsync in InitializeAsync; builders for test data; a test never swallows an exception from the code it drives.
REFERENCE FILES: tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/Scheduling/SchedulerCycleScenarios.cs,
tests/Encina.UnitTests/Messaging/Scheduling/CompiledExpressionScheduledMessageDispatcherTests.cs, docs/en/guides/TESTING.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Requirement | How this feature meets it |
|----------|-------------|---------------------------|
| ADR-050 (Scheduling row) | Dispatch idempotency required; startup validation | `SchedulingDispatchStartupCheck` (Design Choice 3) |
| ADR-001, ADR-006 | Operations return `Either<EncinaError, T>` | Unchanged: the dispatcher still returns `Either` |
| ADR-048 | Inbox record versus business transaction | The delivery guarantee paragraph; the key is the Inbox message id of a marked request |
| ADR-054 D3, D4 | Package-named meter, no `_total`; tenant tag when tenancy is on | `ScheduledDispatchIdempotencyMetrics` |
| ADR-021 | EventId ranges are registered before use | `MessagingScheduledIdempotency` 7840-7849 |
| ADR-062 | Error category set where the error is created | No new error code: the startup check fails the host start, and consumers own their errors |
| Idempotent consumer pattern | The consumer deduplicates a redelivery by a stable id | The dispatch key |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `ScheduledMessageProcessor` | `src/Encina.Messaging/Scheduling/ScheduledMessageProcessor.cs` | Builds the dispatch context in its callback |
| `IScheduledMessageDispatcher`, `CompiledExpressionScheduledMessageDispatcher` | `src/Encina.Messaging/Scheduling/` | Carry the context to `Send` and `Publish` |
| `SchedulerOrchestrator` | `src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs` | Re-dispatch detection (after #1251) |
| `DelayedRetryStartupCheck` | `src/Encina.Messaging/Recoverability/DelayedRetryStartupCheck.cs` | Model of the startup check |
| `InboxPipelineBehavior`, `DistributedIdempotencyPipelineBehavior` | `src/Encina.Messaging/Inbox/`, `src/Encina.Caching/Behaviors/` | The consumers that deduplicate by the key |
| `IRequestContext.WithIdempotencyKey` | `src/Encina/Abstractions/IRequestContext.cs` | Carries the key |
| `SchedulingProcessorMetrics` | `src/Encina.Messaging/Diagnostics/SchedulingProcessorMetrics.cs` | Pattern for the counter class |
| Scheduling integration tests | `tests/Encina.IntegrationTests/**/Scheduling/`, `.../Scheduling/SchedulerCycleScenarios.cs` | Provider matrix of the scenario |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Messaging` | **7840-7842** used of the new `MessagingScheduledIdempotency` = (7840, 7849) | From the block 7800-7849 assigned to this family (7800-7819 is #334, 7830-7839 is #733); 7843-7849 stay free; the existing `MessagingScheduling` range (2300-2399) is untouched, its free ids are claimed by other plans (#1222, #146, #150, #133, #147) |

### Per-file coverage targets

| File | Unit | Guard | Contract | Integration | Justification |
|------|-----:|------:|---------:|------------:|---------------|
| `Scheduling/ScheduledDispatchKey.cs` | 100 | 0 | 0 | 70 | A pure function asserted exhaustively; internal static, no `InternalsVisibleTo` for `Encina.GuardTests`; the integration scenario exercises it on 10 providers |
| `Scheduling/SchedulingDispatchStartupCheck.cs` | 92 | 70 | 0 | 0 | Every branch (each consumer present or absent, opt-out) is unit-testable with a built container; guard covers the constructor |
| `Scheduling/CompiledExpressionScheduledMessageDispatcher.cs` (modified) | current | current | current | 0 | The existing targets stay (they never go down); the context path is covered by the rewritten dispatcher tests |
| `Scheduling/ScheduledMessageProcessor.cs` (modified) | 80 | current | 0 | 60 | The manifest has no per-file target yet; the callback and its context are covered by processor unit tests and the cycle scenario; targets are added with the first pull request |
| `Scheduling/SchedulerOrchestrator.cs` (modified) | 90 | 30 | 0 | 60 | The current unit 90 and guard 30 stay; detection is covered by unit tests with a store fake and by the integration scenario |
| `Scheduling/SchedulingOptions.cs` (modified) | 100 | 0 | 0 | 0 | Defaults and setters, as the other options classes; no argument validation |
| `Diagnostics/ScheduledDispatchIdempotencyMetrics.cs`, `ScheduledDispatchIdempotencyLog.cs` | 85 | 0 | 0 | 0 | Recorded through a `MeterListener` and a fake logger; internal static, no guard-reachable parameters |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Production source, new | 4 | `ScheduledDispatchKey.cs`, `SchedulingDispatchStartupCheck.cs`, `ScheduledDispatchIdempotencyMetrics.cs`, `ScheduledDispatchIdempotencyLog.cs` |
| Production source, modified | 7 | `IScheduledMessageDispatcher.cs`, `CompiledExpressionScheduledMessageDispatcher.cs`, `ScheduledMessageProcessor.cs`, `SchedulerOrchestrator.cs`, `SchedulingOptions.cs`, `MessagingServiceCollectionExtensions.cs`, `EventIdRanges.cs` |
| Metadata | 7 | `PublicAPI.Unshipped.txt` of `Encina.Messaging` and `Encina`, manifests `Encina.Messaging.json` and `Encina.json`, changelog fragment, `docs/INVENTORY.md`, README of `Encina.Messaging` |
| Tests | 18 | unit (key, startup check, dispatcher rewritten, processor, detection), guard (dispatcher rewritten, check), contract updated, `ErrorMessageLeakTests` updated, integration (the shared scenario plus the provider test files extended: counted as 5 files: ADO.NET, Dapper, EF Core, MongoDB registrations and the scenario), DI test extended, allocation map, justification files (2) |
| Documentation | 2 | scheduling guide, ADR-050 table row |
| **Total** | **~38 distinct** | Five pull requests (the PR table below sums to 43 because the `PublicAPI` files and manifests are touched in several) |

### Pull request split

| PR | Content | Files (approx.) |
|----|---------|-----------------|
| A | Phase 1: `ScheduledDispatchKey`, dispatcher contract, compiled dispatcher, processor callback, `PublicAPI` and manifest of `Encina.Messaging`, the key unit test, the rewritten dispatcher unit, guard and contract tests, `ErrorMessageLeakTests`, processor unit test | 12 |
| B | Phase 2: `AcceptAtLeastOnceDispatch`, `SchedulingDispatchStartupCheck`, registration, `ScheduledDispatchIdempotencyLog` with its first two entries (7840, 7841), `EventIdRanges.cs`, the allocation test map, the startup check unit and guard tests, DI test extended, `PublicAPI` of `Encina.Messaging` and `Encina`, manifests `Encina.Messaging.json` and `Encina.json` | 13 |
| C | Phase 3: the metrics class, the third log entry (7842), `SchedulerOrchestrator` detection after [#1251](https://github.com/dlrivada/Encina/issues/1251), unit tests (metrics, detection), manifest `Encina.Messaging.json` | 6 |
| D | Phase 4 integration: the shared scenario on the 10 providers (5 test files) | 5 |
| E | Load and benchmark justification files, changelog fragment, README, scheduling guide, ADR-050 row, `docs/INVENTORY.md` | 7 |

Order: A, B, C, D, E. A is independent of #1251 and #716; C starts after #1251. Each pull request builds alone: A changes the dispatcher contract together with its only implementer, caller and tests; B adds the check and the options (a host that does not enable the check keeps starting); C adds telemetry and a branch that is inactive without a claim result; D and E add tests and documentation. The check of PR B fails hosts that use Scheduling without a consumer, so B is the behavior-changing pull request and its changelog line goes in the `changed` section.

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
PROJECT CONTEXT:
Encina is pre-1.0, .NET 10 / C# 14, nullable enabled, no backward compatibility (no [Obsolete], no aliases).
Issue #735 makes scheduled message dispatch safe to repeat: every dispatch carries a stable idempotency key, a re-dispatch
is detected, and the Scheduling module does not start without a consumer able to deduplicate (ADR-050) unless the
application accepts at-least-once dispatch explicitly. Prerequisites: #1251 (row claiming) for detection only; #733 for
notifications.

IMPLEMENTATION OVERVIEW:
1. Encina.Messaging/Scheduling: ScheduledDispatchKey ("{Id:D}:{ScheduledAtUtc.Ticks}"), IScheduledMessageDispatcher.DispatchAsync
   takes IRequestContext, the compiled dispatcher uses the Send/Publish context overloads, the processor builds the context
   with WithIdempotencyKey (identity unchanged).
2. SchedulingOptions.AcceptAtLeastOnceDispatch and SchedulingDispatchStartupCheck (IHostedService, model
   DelayedRetryStartupCheck): fail the start with no deduplicating consumer unless the option is set; the opt-out is logged.
3. Observability: ScheduledDispatchIdempotencyMetrics (counter encina.messaging.scheduling.redispatches, tenant tag only when
   tenancy is on) and ScheduledDispatchIdempotencyLog EventIds 7840-7842 in the new range MessagingScheduledIdempotency
   (7840, 7849) registered first in EventIdRanges.cs; SchedulerOrchestrator counts a reclaimed message.
4. Tests on all 10 providers plus unit, guard, contract and DI; per-file targets from the plan.
5. Docs: XML docs, changelog fragments, scheduling guide, ADR-050 row, INVENTORY, PublicAPI files.

KEY PATTERNS:
- ROP: the dispatcher returns Either; a store Left fails the operation; background loops never let an exception escape.
- At-least-once with deduplication by the consumer; never write "dispatched exactly once" or "atomic".
- TimeProvider for time; no DateTime.UtcNow. Keys and EncinaError.Message never reach logs or tags.
- Replace the dispatcher signature and every caller in one pull request; no overload kept beside it.
- Provider coherence: no provider-specific code in Encina.Messaging; the 10 providers are proven by tests.
- CRAP <= 10 on every changed method; targets never go down; one pull request per phase group.

REFERENCE FILES:
docs/plans/scheduled-dispatch-idempotency-implementation-plan-735.md, src/Encina.Messaging/Scheduling/ScheduledMessageProcessor.cs,
src/Encina.Messaging/Scheduling/CompiledExpressionScheduledMessageDispatcher.cs, src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs,
src/Encina.Messaging/Recoverability/DelayedRetryStartupCheck.cs, docs/architecture/adr/050-modules-may-require-modules.md,
docs/plans/processor-row-claiming-implementation-plan-1251.md, src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | The dispatch key is passed to downstream behaviors; no cache is added here |
| 2 | OpenTelemetry | ✅ | Counter `encina.messaging.scheduling.redispatches` counts messages dispatched again after an expired claim, with the tag `tenant` only when multi-tenancy is on (ADR-054 D4) |
| 3 | Structured Logging | ✅ | `[LoggerMessage]` ids 7840-7849 in the new range `MessagingScheduledIdempotency`, message id and counts only |
| 4 | Health Checks | ❌ | No new dependency; a startup check (not a health check) enforces ADR-050 |
| 5 | Validation | ✅ | The startup check validates the module configuration (a deduplicating consumer or an explicit opt-out) and fails closed |
| 6 | Resilience | ❌ | The dispatch retry policy is unchanged |
| 7 | Distributed Locks | ❌ | Concurrent dispatch is prevented by row claiming ([#1251](https://github.com/dlrivada/Encina/issues/1251)) and by the lock plan of [#716](https://github.com/dlrivada/Encina/issues/716) |
| 8 | Transactions | ❌ | The dispatch happens outside any store transaction and the window between dispatch and the processed mark stays at-least-once; the key is what makes it harmless |
| 9 | Idempotency | ✅ | This is idempotent re-execution of scheduled dispatch |
| 10 | Multi-Tenancy | ✅ | The dispatch context carries the tenant of the scheduled message once [#739](https://github.com/dlrivada/Encina/issues/739) adds it, and the consumer composes the tenant into its own key; the tenant tag follows ADR-054 D4 |
| 11 | Module Isolation | ❌ | Scheduled messages carry no module id |
| 12 | Audit Trail | ❌ | A redispatch is an operational signal covered by telemetry and logs, not a compliance event |

---

## Prerequisites & Dependencies

| Issue | Relation | Notes |
|-------|----------|-------|
| [#1251](https://github.com/dlrivada/Encina/issues/1251) | Blocks PR C (detection) | Row claiming; its claim result must say "reclaimed after an expired lease" (Design Choice 5) |
| [#716](https://github.com/dlrivada/Encina/issues/716) | Related (merged plan) | The cycle lock; reduces concurrent dispatch, does not close the crash window |
| [#733](https://github.com/dlrivada/Encina/issues/733) | Related | The notification gate that deduplicates a scheduled notification by the key; the startup check lists it once it exists |
| [#3441](https://github.com/dlrivada/Encina/issues/3441) and [#3439](https://github.com/dlrivada/Encina/issues/3439) | Related | A retry of one occurrence reuses the key; whether a failed attempt is retryable under it is decided by those two fixes (inbox and cache) |
| [#739](https://github.com/dlrivada/Encina/issues/739) | Related | Tenant on scheduled messages |
| [#1220](https://github.com/dlrivada/Encina/issues/1220) | Sibling (merged plan) | Same principle for outbox dispatch (the outbox id is the key) |
| [#2401](https://github.com/dlrivada/Encina/issues/2401) | Related | Atomic claim of the Inbox, which the key relies on for concurrent redeliveries |

---

## Next Steps

1. Open PR A first; it needs nothing from #1251, #716 or #733.
2. At pick-up: re-check the Design Choices against today's code, ask the maintainer Dilemmas 1, 3 and 5 (mechanism, the startup check and its opt-out, detection through the claim) and record the answers in `## Maintainer Decisions`; then run `check-plan.ps1` without `-Draft`.
3. Confirm the `MessagingScheduledIdempotency` range (7840-7849) is still free when PR B starts.
4. Measure per-flag coverage and the CRAP table for the touched files before each pull request.
