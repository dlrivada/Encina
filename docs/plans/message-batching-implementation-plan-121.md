# Implementation Plan: Message Batching and Batch Handlers (batching modes, payload-size trigger, batch outbox dispatch, inbox composition)

> **Issue**: [#121](https://github.com/dlrivada/Encina/issues/121)
> **Type**: Feature
> **Complexity**: Medium (5 phases, 4 pull requests of 12, 12, 11 and 14 files)
> **Estimated Scope**: about 600 lines of production code and about 1,300 lines of tests, split over four pull requests
> **Family**: msg-batching (#121, #252, #302, #339, #349). #302's plan owns `IBatchHandler<TRequest, TResponse>` (one interface; #121's different signature of the same name is dropped), the collector and the `Encina.Batching` package; #349 and #339 own batch wrappers; #252 owns bulk publishing to transports. This plan owns `BatchingMode`, the payload-size trigger, `IBatchNotificationHandler<TNotification>`, the batch dispatch of the outbox and the inbox composition test.

---

## Summary

The issue asks for handlers that receive many messages in one call, configurable by count, time, payload size or the first of them, integrated with the outbox (batch publishing) and the inbox (batch consuming), with the batch wrapped by the pipeline. What exists on `origin/main` (`b50b8c981`) and what this plan adds:

| Issue item | Today | Owner |
|------------|-------|-------|
| `IBatchHandler<TRequest, TResponse>` | Missing; #302 owns the single definition | #302 |
| Count and time triggers | Planned by #302 (`MaxBatchSize`, `MaxDelay`) | #302 |
| `BatchingMode` (`CountBased`, `TimeBased`, `SizeBased`, `TimeOrSize`) and the payload-size trigger | Missing | This plan, Phase 1 |
| `config.Batching.*` configuration | Missing | #302's `AddEncinaBatching` options; the mode and byte limit are added here (Design Choice 3) |
| Outbox "batch publishing" | `OutboxBatchProcessor` (`src/Encina.Messaging/Outbox/OutboxBatchProcessor.cs`, shared by `OutboxProcessorBase` and `OutboxOrchestrator`) fetches up to `OutboxOptions.BatchSize` messages and still publishes one at a time through a per-message `publish` callback | This plan, Phases 2 and 3 |
| Inbox "batch consuming" | `InboxPipelineBehavior` deduplicates per request by `IRequestContext.IdempotencyKey`; no batch variant | This plan, Phase 4: composition by pipeline order, proven by a test (Design Choice 8) |
| Bulk publish to brokers | Kafka and SQS have partial batch methods; the rest none | #252 |

Delivered: in `Encina.Batching` `BatchingMode`, `IBatchItemSize`, the byte trigger in the collector and `IBatchNotificationHandler<TNotification>`; in `Encina.Messaging` a `BatchNotificationDispatcher` and an opt-in `OutboxOptions.BatchDispatch` that hands runs of consecutive same-type outbox messages to a batch handler while every message keeps its own success, retry, exhausted and dead-letter path; log messages (EventIds 2973-2976 in the existing `Messaging` range); documentation.

**Provider category**: Database (10) only in the sense that the outbox is stored by the 10 providers. The change is in the shared `OutboxBatchProcessor` and calls the same `IOutboxStore` methods per message, so no provider code or SQL changes; the proof still runs on all 10 providers (Phase 4).

---

## Design Choices

<details>
<summary><strong>1. The modes map to enabled triggers</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `BatchingMode { CountBased, TimeBased, SizeBased, TimeOrSize }` selects which triggers are active; `TimeOrSize` (default) flushes on the first of count, delay or bytes that is configured** | Matches the issue's table of modes; the default equals #302's behavior when no byte limit is set | `CountBased` and `SizeBased` have no delay, so an item waits for the count or the bytes (or shutdown) |
| **B) Independent booleans per trigger** | More combinations | Not the enum the issue defines; invalid combinations (none enabled) |

### Chosen Option: **A, the enum of the issue**

### Rationale

The issue defines the four members and their meaning. A mode without a delay is bounded by #302's `MaxPendingItems` (an enqueue past it fails with `batching.queue_full`) and flushed at disposal, and options validation rejects `SizeBased` without `MaxBatchBytes`.

**Class:** Settled
**Settled by:** #121 body (section "Batching Modes" and the `BatchingMode` enum) and #302 Design Choice 8 (bounded pending items).

</details>

<details>
<summary><strong>2. How the payload size of an item is known (Dilemma D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A request that wants the byte trigger implements `IBatchItemSize { int SizeInBytes { get; } }`; a request without it counts as zero bytes** | Explicit and free; the application knows its payload | The author must implement it; a wrong size makes a wrong flush |
| **B) The collector serializes each request with `IMessageSerializer` to measure it** | No author effort | Every request is serialized twice (once to measure, once if the handler persists it); the serializer lives in `Encina.Messaging`, which `Encina.Batching` does not reference |
| **C) Estimate by reflection over the object graph** | No interface | Slow, inexact, allocation-heavy |

### Chosen Option: **A, an interface on the request** (recommended, pending the maintainer)

### Rationale

A wins when the request knows its size (a document or file reference) and the hot path must stay cheap; B wins when payloads are opaque and double serialization is acceptable; C wins in no scenario worth its cost. The byte count is clamped to zero or more, and a request whose size exceeds `MaxBatchBytes` alone is flushed alone, never rejected.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Configuration enters through `AddEncinaBatching`</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Mode` and `MaxBatchBytes` are added to #302's `BatchingOptions` and to `[BatchOptions]`, bound by `AddEncinaBatching`** | One registration entry per package; per-type override through the attribute | The issue's `config.Batching.*` syntax is not used |
| **B) A `Batching` property on core `EncinaConfiguration`** | Matches the issue text | Core would hold options of a satellite package, or reference it |

### Chosen Option: **A, the satellite options**

### Rationale

The plan prompt requires satellite registration as `AddEncina{Feature}`, and #302 already defines the options type.

**Class:** Settled
**Settled by:** `docs/engineering/prompts/implementation-plan-prompt.md` section 3 (satellite DI) and #302.

</details>

<details>
<summary><strong>4. Direction of the package reference (Dilemma D4)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Encina.Messaging` references `Encina.Batching` (the notification handler contract lives in `Encina.Batching`; the outbox dispatch lives in `Encina.Messaging`)** | `Encina.Batching` keeps depending on `Encina` only (#302); #349's transaction adapter points the same way; an application without messaging never sees outbox code | `Encina.Messaging` gains a reference to a package most users of messaging may not use |
| **B) `Encina.Batching` references `Encina.Messaging`** | The batch dispatch lives next to the collector | Batching would pull in the whole messaging package for the outbox types, and #349's adapter would form a cycle |
| **C) The contract in core `Encina`, the dispatch in `Encina.Messaging`** | No new reference | Core carries a contract of an optional feature |

### Chosen Option: **A, Messaging references Batching** (recommended, pending the maintainer)

### Rationale

A wins when batching stays a standalone package; C wins when the maintainer wants no assembly reference from messaging to batching and accepts a core contract. B is rejected by the cycle with #349. Whichever of this plan's pull request 2 and #349's pull request 5 lands first adds the `ProjectReference`; the second finds it present (verified at work start).

**Class:** Dilemma

</details>

<details>
<summary><strong>5. A notification batch handler is its own contract</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `IBatchNotificationHandler<TNotification> where TNotification : INotification` with `Task<IReadOnlyList<Either<EncinaError, Unit>>> HandleBatch(IReadOnlyList<TNotification>, CancellationToken)`** | Outbox messages are notifications, which have no response | A second batch interface next to `IBatchHandler` |
| **B) Reuse `IBatchHandler<TNotification, Unit>`** | One interface | `IBatchHandler` constrains `TRequest : IRequest<TResponse>` (#302), and `INotification` is not a request, so it cannot be used without dropping the constraint that #302 specifies |

### Chosen Option: **A, a separate notification contract**

### Rationale

`src/Encina/Abstractions/INotification.cs` declares a marker without the request contract, and #302's `IBatchHandler` is constrained to `IRequest<TResponse>`.

**Class:** Settled
**Settled by:** #302 body (`IBatchHandler ... where TRequest : IRequest<TResponse>`) and `src/Encina/Abstractions/INotification.cs`.

</details>

<details>
<summary><strong>6. Which outbox messages form a batch (Dilemma D6)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Runs of consecutive messages of one notification type in the fetched list, in fetch order; only a type that has a registered `IBatchNotificationHandler` is batched, others keep the per-message path** | The publication order of the fetched messages never changes | A mixed stream (A, B, A, B) produces runs of one, so little batching | 
| **B) Group every message of a type in the fetched list** | Best batch sizes | Messages of different types change relative order, which can break consumers that rely on it (ordering by partition key is #469's work, not this one's) |

### Chosen Option: **A, consecutive runs** (recommended, pending the maintainer)

### Rationale

A wins when relative order of messages matters to consumers, which the outbox does not promise to break; B wins for high-volume single-type streams where order across types is irrelevant. A run of one goes through the per-message path. When #737 adds a tenant to outbox messages, a run is also cut where the tenant changes (so a batch never spans tenants); that is a one-line addition to the run key and belongs to #737's plan.

**Class:** Dilemma

</details>

<details>
<summary><strong>7. Every message keeps its own outcome, and the dispatch is opt-in</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `OutboxOptions.BatchDispatch` (default `false`); a run's per-message results feed the same success, `PublishFailedAsync`, exhausted, dead-letter and store-error code as today; a result count different from the run size fails every message of the run with `outbox.batch_result_mismatch` (category `Internal`)** | Retry, backoff, exhaustion and dead letter behave exactly as before; nothing is reported as delivered without a `Right` | A handler must return one result per message |
| **B) One result for the whole run** | Simplest | One bad message fails or acknowledges the whole run: either redelivers successes or hides failures |

### Chosen Option: **A, per-message outcomes, opt-in**

### Rationale

`AGENTS.md` section 3 requires a `Left` from a processor to fail that operation visibly (retry or dead letter) and never report success, and every feature is opt-in.

**Class:** Settled
**Settled by:** `AGENTS.md` section 3 (errors are never swallowed in background infrastructure) and section 1 (every feature opt-in).

</details>

<details>
<summary><strong>8. The inbox composes by pipeline order, with no new inbox code</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `InboxPipelineBehavior` stays an outer per-item behavior and the batching behavior of #302 is innermost, so each item is deduplicated and recorded individually before and after its batch is flushed; a test proves it** | No change to the inbox store or to its 10 providers; reuses ADR-048's behavior | The inbox claim of an item stays open while the item waits for its batch (bounded by `MaxDelay`) |
| **B) A batch-aware inbox that registers a whole batch at once** | One store round trip per batch | New methods on `IInboxStore` and its implementations in all 10 providers for a marginal gain |

### Chosen Option: **A, composition by order**

### Rationale

#302 places per-item outer behaviors before the collector by design (its Design Choice 3), and `InboxPipelineBehavior` keys deduplication on `IRequestContext.IdempotencyKey` per request.

**Class:** Settled
**Settled by:** #302 Design Choice 3 and `src/Encina.Messaging/Inbox/InboxPipelineBehavior.cs` (per-request idempotency key).

</details>

---

## Implementation Phases

### Phase 1: Modes and the byte trigger (pull request 1)

<details>
<summary>Tasks</summary>

1. In `Encina.Batching`: `BatchingMode` enum, `IBatchItemSize` (D2); `BatchingOptions.Mode` (default `TimeOrSize`) and `MaxBatchBytes` (null by default; validated above zero), the same two members on `[BatchOptions]` (`Mode`, `MaxBatchBytes`, where an unset attribute value falls back to the option), and `BatchingOptions.Validate` rules (`SizeBased` requires `MaxBatchBytes`; `CountBased` requires `MaxBatchSize`; a mode never disables every trigger).
2. `BatchCollector` (edit): a per-buffer byte counter; the enqueue that reaches `MaxBatchBytes` flushes; a single item at or above the limit flushes alone; the delay timer starts only when the mode includes time; EventId reuse: no new ids (the flush reason `bytes` is a value of the existing flush log message of #302, 7751).
3. Tests: unit `BatchCollectorModesTests` (each mode, byte trigger with `FakeTimeProvider`, an oversized item, a request without `IBatchItemSize`), edit `BatchingOptionsTests` (validation rules), edit `BatchCollectorPropertyTests` (no flush ever exceeds `MaxBatchSize` items or, unless a single item is larger, `MaxBatchBytes`); PublicAPI.Unshipped (through `mechanical-fixer`), manifest, README edit (through `docs-writer`).

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT: #302 added the Encina.Batching collector with count and delay triggers. D1 (modes select triggers) and D2 (an interface supplies the payload size) apply; D2 as recommended until the maintainer answers.
TASK: Add BatchingMode, IBatchItemSize and the byte trigger with options validation and tests.
KEY RULES: Time from TimeProvider; the collector never throws from a timer callback; an oversized item flushes alone and is never rejected; options validated at startup; XML docs with examples on every public member; exception type only in logs.
REFERENCE FILES: src/Encina.Batching/BatchCollector.cs, src/Encina.Batching/BatchingOptions.cs, src/Encina.Batching/BatchOptionsAttribute.cs, tests/Encina.UnitTests/Batching.
```

</details>

### Phase 2: The notification handler contract and dispatcher (pull request 2)

<details>
<summary>Tasks</summary>

1. `src/Encina.Batching/IBatchNotificationHandler.cs` (D5).
2. `src/Encina.Messaging/Encina.Messaging.csproj`: `ProjectReference` to `Encina.Batching` (D4; skip if #349 pull request 5 already added it).
3. `src/Encina.Messaging/Outbox/BatchNotificationDispatcher.cs` (internal sealed, scoped; constructor `(IServiceProvider)` resolving the scope's handlers): `bool HasHandler(Type notificationType)` and `ValueTask<IReadOnlyList<Either<EncinaError, Unit>>> DispatchAsync(Type notificationType, IReadOnlyList<object> notifications, CancellationToken)`; the closed-generic invoker is created once per notification type and cached in a static `ConcurrentDictionary<Type, ...>` (the same technique as the request dispatcher's handler cache). A handler that throws returns one `Left` `outbox.batch_publish_failed` (`UpstreamFailure`, exception type only) per message; a wrong result count returns `outbox.batch_result_mismatch` (`Internal`) per message.
4. Registration: `TryAddScoped<BatchNotificationDispatcher>()` in `MessagingServiceCollectionExtensions` next to the existing outbox registrations; handlers are registered by the application (`IBatchNotificationHandler<T>` as scoped) or found by `AddEncinaBatching(assemblies)` scanning (#349's registrar extends to `IBatchNotificationHandler<>` in this pull request, one added scan clause).
5. Tests: unit `BatchNotificationDispatcherTests` (has handler, dispatch, throwing handler, count mismatch, invoker cache), DI `BatchNotificationRegistrationTests` with `ValidateOnBuild` and `ValidateScopes` in both registration orders, guard `BatchNotificationDispatcherGuardTests`; PublicAPI for `Encina.Batching` and `Encina.Messaging` (through `mechanical-fixer`), both manifests.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT: Outbox messages are notifications, which are not requests, so IBatchHandler cannot serve them (D5). D4 (Messaging references Batching) applies as recommended until the maintainer answers.
TASK: Add IBatchNotificationHandler, the project reference, the internal dispatcher with a cached invoker, its registration and tests.
KEY RULES: No scoped service injected into a singleton; a thrown exception becomes a per-message Left with the exception type only; never log or return a message text; ADR-062 category on every new error code; registration order independent, proven by a DI test.
REFERENCE FILES: src/Encina/Abstractions/INotificationHandler.cs, src/Encina/Dispatchers/Encina.NotificationDispatcher.cs, src/Encina.Messaging/MessagingServiceCollectionExtensions.cs, src/Encina.Messaging/Outbox/OutboxBatchProcessor.cs.
```

</details>

### Phase 3: Outbox batch dispatch (pull request 3)

<details>
<summary>Tasks</summary>

1. `OutboxOptions.BatchDispatch` (default `false`, D7) and `OutboxOptions.MaxBatchDispatchSize` (default 100; at most `BatchSize` is meaningful, validated above zero).
2. `OutboxBatchProcessor.ProcessAsync` gains an optional parameter `Func<Type, IReadOnlyList<object>, CancellationToken, ValueTask<IReadOnlyList<Either<EncinaError, Unit>>>>? publishBatch = null` and `Func<Type, bool>? hasBatchHandler = null` (an optional default keeps the `OutboxOrchestrator` and `OutboxProcessorBase` call sites and the four test files that call the processor compiling; callers found by `git grep -n OutboxBatchProcessor`: `OutboxProcessorBase`, `OutboxOrchestrator`, and the tests `OutboxBatchProcessorTests`, `OutboxRetryAfterCeilingTests`, `ErrorMessageLeakTests`, `RetryAfterCeilingProperties`). With the callbacks present and `BatchDispatch` on, the processor deserializes the fetched messages, splits them into runs (D6, at most `MaxBatchDispatchSize` per run), sends each run of two or more of a handled type through `publishBatch`, and feeds every returned result through the existing post-publish path (`MarkAsProcessedAsync`, `PublishFailedAsync`, exhausted, dead letter, store-error accounting); `ProcessMessageAsync` is split into a deserialize step and a completion step so that the batch and single paths share the completion code, each method at complexity 8 or less. A cancelled token stops without marking the interrupted run failed (the existing cancellation rule).
3. `OutboxProcessorBase.ProcessPendingMessagesAsync` and `OutboxOrchestrator.ProcessPendingMessagesAsync` pass the dispatcher callbacks when `BatchDispatch` is on and a `BatchNotificationDispatcher` is registered; with no dispatcher registered the option has no effect and a warning is logged once (EventId 2976 is reserved for it); the processor still saves after the whole batch with the existing `SaveChangesAsync` call (the batch dispatch does not change the save point and claims no atomicity).
4. `MessagingLog` (EventIds 2973-2976 in the existing `Messaging` range, free on `origin/main`): 2973 run dispatched as a batch (Debug, type and size), 2974 result count mismatch (Error), 2975 batch handler threw (Error, exception type through `ForLogging()`), 2976 `BatchDispatch` on without a registered dispatcher (Warning).
5. Tests: unit `OutboxBatchProcessorBatchDispatchTests` (runs cut at type changes, a run of one uses the per-message path, per-message success and failure feed retry, exhausted and dead letter exactly as before, count mismatch, throwing handler, cancellation, off by default leaves the existing tests unchanged), property `OutboxBatchRunsProperties` (runs preserve fetch order and cover every message exactly once), edit `OutboxBatchProcessorTests` only if a signature assertion needs it; PublicAPI.Unshipped for `Encina.Messaging` (`OutboxOptions` members), manifest, README edit, changelog fragment `changelog.d/121-outbox-batch-dispatch.added.md`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT: OutboxBatchProcessor is the single implementation of one outbox cycle and is shared by every provider's processor and by OutboxOrchestrator. D6 (consecutive same-type runs, only for types with a batch handler) and D7 (per-message outcomes, opt-in) apply; D6 as recommended until the maintainer answers.
TASK: Add the options, the optional batch callbacks, the run splitter, the shared completion path, the log messages and the tests.
KEY RULES: Every message keeps its success, retry, exhausted, dead-letter and store-error accounting; a Left or a count mismatch never reports success; the cycle's SaveChangesAsync stays where it is and no atomicity is claimed; cancellation does not consume a retry; method complexity 8 or less (CRAP <= 10); EventIds 2973-2976 only; optional parameters so existing callers compile.
REFERENCE FILES: src/Encina.Messaging/Outbox/OutboxBatchProcessor.cs, src/Encina.Messaging/Outbox/OutboxProcessorBase.cs, src/Encina.Messaging/Outbox/OutboxOrchestrator.cs, src/Encina.Messaging/MessagingLog.cs, tests/Encina.UnitTests/Messaging/Outbox/OutboxBatchProcessorTests.cs.
```

</details>

### Phase 4: Inbox composition and the 10-provider proof (pull request 4)

<details>
<summary>Tasks</summary>

1. Composition test `InboxBatchingCompositionTests` (unit, with an in-memory fake `IInboxStore`): a batchable request behind `InboxPipelineBehavior` is recorded once per idempotency key, a duplicate key never reaches the collector, and the batching behavior is innermost (D8); the registration helper asserts the pipeline order and fails the test when the batching behavior is registered outside the inbox behavior.
2. Shared integration scenario `tests/Encina.IntegrationTests/Messaging/Outbox/OutboxBatchDispatchScenario.cs` (next to the existing `OutboxAtomicityScenario.cs`): three pending messages of one type and one of another are processed with `BatchDispatch` on and a recording handler; the store shows every message processed, a failed message of the run is rescheduled with its retry count increased and the others stay processed.
3. Provider classes, each calling the scenario from its existing outbox test class (`[Collection("<Family>-<Database>")]`, shared fixture, `ClearAllDataAsync()` in `InitializeAsync()`): ADO.NET and Dapper on SqlServer, PostgreSQL, MySQL (`OutboxStoreADOTests`, `OutboxStoreDapperTests` of each database), EF Core (`OutboxStoreEFSqlServerTests`, `OutboxStoreEFPostgreSqlTests`, `OutboxStoreEFMySqlTests`) and MongoDB (`OutboxStoreMongoDBIntegrationTests`); that is the 10 providers, one added test method each, no new fixture.
4. Load and benchmark: justification files `tests/Encina.LoadTests/Messaging/OutboxBatchDispatch.md` and `tests/Encina.BenchmarkTests/Messaging/OutboxBatchDispatch.md`: the benefit is the handler's own bulk operation; the loop is not a concurrent path.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT: Batch dispatch is implemented in the shared outbox processor; AGENTS.md section 9 requires integration tests on real databases for database features, on all 10 providers, and forbids a .md justification for them.
TASK: Add the composition unit test, the shared integration scenario, one added test method in each of the 10 provider outbox test classes and the two justification files for load and benchmark.
KEY RULES: Shared [Collection] fixtures only, never IClassFixture or a per-class fixture; ClearAllDataAsync in InitializeAsync; never dispose the fixture; the MongoDB scenario needs the replica-set fixture already used by its existing class.
REFERENCE FILES: tests/Encina.IntegrationTests/Messaging/Outbox/OutboxAtomicityScenario.cs, tests/Encina.IntegrationTests/ADO/SqlServer/Outbox/OutboxStoreADOTests.cs, docs/testing/integration-tests.md, src/Encina.Messaging/Inbox/InboxPipelineBehavior.cs.
```

</details>

### Phase 5: Cross-Cutting Integration, testing and documentation

<details>
<summary>Tasks</summary>

1. Multi-tenancy (function 10): the outbox message has no `TenantId` today; #737 adds it and the run key gains the tenant there (D6). The collector's tenant-separated key (#302) applies to the byte trigger unchanged.
2. Errors (ADR-062): `outbox.batch_result_mismatch` `Internal`, `outbox.batch_publish_failed` `UpstreamFailure`; no `EncinaError.Message` or exception message reaches a log, tag or result.
3. Per-flag targets, entered in `.github/coverage-manifest/Encina.Batching.json` and `Encina.Messaging.json` in the pull request that adds each file; baselines of the edited files (`BatchCollector.cs`, `BatchingOptions.cs`, `OutboxBatchProcessor.cs`, `OutboxOptions.cs`, `OutboxProcessorBase.cs`, `OutboxOrchestrator.cs`, `MessagingLog.cs`) are measured at work start with `dotnet test tests\Encina.<Flag>Tests --collect "XPlat Code Coverage" --results-directory artifacts\coverage\<Flag>Tests` and `dotnet run --file .github/scripts/coverage-report.cs -- --input artifacts/coverage --output artifacts/coverage-report`; each target is the measured value or higher.

| File | unit | guard | contract | property | integration |
|------|:---:|:---:|:---:|:---:|:---:|
| `BatchNotificationDispatcher.cs` | 95 | 0 | 0 | 0 | 0 |
| `OutboxBatchProcessor.cs` (edited) | measured baseline or higher | 0 | 0 | measured baseline or higher | 70 |
| `OutboxOptions.cs`, `BatchingOptions.cs`, `BatchOptionsAttribute.cs` (edited) | 100 | measured baseline or higher | 0 | 0 | 0 |
| `BatchCollector.cs` (edited) | measured baseline or higher | 0 | 0 | measured baseline or higher | 0 |
| `OutboxProcessorBase.cs`, `OutboxOrchestrator.cs`, `MessagingLog.cs` (edited) | measured baseline or higher | measured baseline or higher | 0 | 0 | 0 |
| `BatchingMode.cs`, `IBatchItemSize.cs`, `IBatchNotificationHandler.cs` | 0 | 0 | 0 | 0 | 0 |

| File and flag | Justification |
|---------------|---------------|
| `BatchNotificationDispatcher.cs` unit 95 | Handler lookup, dispatch, exception and mismatch paths run with fakes; guard 0: internal type, with no `InternalsVisibleTo` for `Encina.GuardTests` (verified at work start; raised if present). |
| `OutboxBatchProcessor.cs` integration 70 | The 10-provider scenario reaches the batch path with real stores; the remaining lines are defensive store-error branches that need fault injection. |
| Options and attribute unit 100 | Pure data and validation. |
| Edited collector and messaging files | The new branches are covered by the new tests; other flags keep their measured baselines because no provider code changes. |
| Interfaces and enum 0 | No executable lines; contract tests of `IBatchNotificationHandler` arrive with its first implementer outside the tests. |

4. Documentation: `src/Encina.Messaging/README.md` (it names `OutboxBatchProcessor`) and `src/Encina.Batching/README.md` through `docs-writer`; the feature page `docs/features/request-batching.md` is owned by #349; changelog fragments `changelog.d/121-batching-modes.added.md` (pull request 1) and `changelog.d/121-outbox-batch-dispatch.added.md` (pull request 3) through `mechanical-fixer`.
5. `dotnet build --configuration Release` with zero warnings; all tests green; every flag at its target; CRAP of every changed method below 10.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT: Per-flag targets live in .github/coverage-manifest/Encina.Batching.json and Encina.Messaging.json; targets never go down; tests execute real code.
TASK: Enter the manifest entries with one-sentence justifications in the pull requests that add the tests, and write the README and changelog fragments through the specialists that own them.
KEY RULES: Shouldly through Encina.Testing.Shouldly; no Thread.Sleep; never edit CHANGELOG.md; no hand-typed coverage figures in docs.
REFERENCE FILES: .github/coverage-manifest/Encina.Messaging.json, src/Encina.Messaging/README.md, changelog.d/README.md, .claude/skills/encina-docs/SKILL.md.
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard / Spec | Relevance | Notes |
|-----------------|-----------|-------|
| `AGENTS.md` section 3 | No swallowed errors in background infrastructure; opt-in; no aliases | D5, D7 |
| `AGENTS.md` sections 5 and 9 | 10 database providers; integration tests on real databases | Phase 4 |
| ADR-048 | Inbox record and the business transaction | D8 |
| ADR-021, ADR-054, ADR-062 | EventIds, telemetry, error categories | Phase 3, Phase 5 |
| Wolverine batch handlers; Composed Message Processor (EIP) (named by the issue) | Comparable products and pattern | Issue references |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `OutboxBatchProcessor` | `src/Encina.Messaging/Outbox/OutboxBatchProcessor.cs` | The single place where outbox cycles run; edited |
| `OutboxProcessorBase`, `OutboxOrchestrator` | `src/Encina.Messaging/Outbox/` | Callers that pass the batch callbacks |
| `OutboxOptions` | `src/Encina.Messaging/Outbox/OutboxOptions.cs` | `BatchSize` already bounds the fetch; `BatchDispatch` added |
| `InboxPipelineBehavior` | `src/Encina.Messaging/Inbox/InboxPipelineBehavior.cs` | Per-item deduplication, unchanged |
| `Encina.Batching` collector, options, `AddEncinaBatching` | #302 | Modes and the byte trigger extend them |
| `OutboxAtomicityScenario` and provider outbox test classes | `tests/Encina.IntegrationTests/Messaging/Outbox/`, per-provider folders | Shared scenario style and the 10 classes to extend |
| `MessagingLog`, `EventIdRanges.Messaging` | `src/Encina.Messaging/MessagingLog.cs`, `src/Encina/Diagnostics/EventIdRanges.cs` | Log messages in the existing range |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Messaging` (`Messaging`, 2800-2999, existing) | 2973-2976, all four used | Free on `origin/main` (2971-2972 are claimed by #736); registered here for the batch; no new range, so no block of the batch registry is used |
| `Encina.Batching` | none new | The byte flush reason reuses #302's 7751 |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Production code | 15 | `BatchingMode`, `IBatchItemSize`, `IBatchNotificationHandler`, `BatchNotificationDispatcher` (new); `BatchingOptions`, `BatchOptionsAttribute`, `BatchCollector`, `BatchHandlerRegistrar`, `OutboxOptions`, `OutboxBatchProcessor`, `OutboxProcessorBase`, `OutboxOrchestrator`, `MessagingLog`, `MessagingServiceCollectionExtensions` (edited) and the Messaging csproj |
| Public API and manifest | 6 | PublicAPI.Unshipped for `Encina.Batching` (PR 1, PR 2) and `Encina.Messaging` (PR 2, PR 3); two manifests |
| Tests | 18 | unit 5, property 2, DI 1, guard 1, 10 provider classes edited, shared scenario |
| Justification files | 2 | load, benchmark |
| Documentation | 5 | 2 README edits, 2 changelog fragments |
| **Pull request 1** | **12** | 2 new and 3 edited production files (`BatchingMode`, `IBatchItemSize`; `BatchingOptions`, `BatchOptionsAttribute`, `BatchCollector`), PublicAPI, manifest, 3 tests (new modes test, edited options test, edited property test), README edit, changelog fragment; about 500 lines |
| **Pull request 2** | **12** | contract, Messaging csproj, dispatcher, registration edit, `BatchHandlerRegistrar` scan clause, 2 PublicAPI, 2 manifests, 3 tests; about 650 lines |
| **Pull request 3** | **11** | `OutboxOptions`, `OutboxBatchProcessor`, `OutboxProcessorBase`, `OutboxOrchestrator`, `MessagingLog`, PublicAPI, manifest, 2 tests (unit, property), README edit, changelog fragment; about 900 lines |
| **Pull request 4** | **14** | composition test, shared scenario, 10 provider test edits, 2 justification files; about 600 lines |

Callers and implementers, by grep on `origin/main` (2026-11-17): `OutboxBatchProcessor` is constructed in `OutboxProcessorBase.cs` and `OutboxOrchestrator.cs`, and used in the tests listed in Phase 3 task 2; the new callback parameters are optional, so none of them changes. `IBatchNotificationHandler` has no implementer yet. Every pull request adds production code together with its own tests and builds alone in the order 1, 2, 3, 4; pull request 1 needs #302 pull request 2, pull request 2 needs #302 and, for the scan clause, #349 pull request 2.

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT: Encina is a pre-1.0 .NET 10 / C# 14 library; no backward compatibility, zero warnings, ROP. #302 provides Encina.Batching with a collector (count and delay triggers); the outbox cycle is OutboxBatchProcessor (shared by every provider's processor and by OutboxOrchestrator), which publishes one message at a time; the inbox deduplicates per request through InboxPipelineBehavior.

IMPLEMENTATION OVERVIEW: PR1 BatchingMode, IBatchItemSize and the byte trigger in the collector with options validation; PR2 IBatchNotificationHandler, the Messaging-to-Batching project reference, BatchNotificationDispatcher with a cached invoker and registration; PR3 OutboxOptions.BatchDispatch and the optional batch callbacks in OutboxBatchProcessor (runs of consecutive same-type messages, per-message outcomes through the existing completion path), log 2973-2976; PR4 the inbox composition test, a shared integration scenario and one added test method in each of the 10 provider outbox test classes. Dilemmas D2, D4 and D6 are implemented as the maintainer answers, the recommended options until then.

KEY PATTERNS: Opt-in, off by default; a Left or a count mismatch never reports success; per-message retry, exhausted and dead-letter accounting unchanged; the cycle's SaveChangesAsync stays where it is and no atomicity is claimed; no scoped service in a singleton; exception type only in logs; ADR-062 category on every error code; targets never go down; CRAP <= 10.

REFERENCE FILES: src/Encina.Messaging/Outbox/OutboxBatchProcessor.cs, src/Encina.Messaging/Outbox/OutboxProcessorBase.cs, src/Encina.Messaging/Inbox/InboxPipelineBehavior.cs, src/Encina.Batching/BatchCollector.cs, tests/Encina.IntegrationTests/Messaging/Outbox/OutboxAtomicityScenario.cs, .github/coverage-manifest/Encina.Messaging.json.
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ⏭️ | State lookups of the inbox may benefit from a cache layer; that is the cache family's work (#695 for the inbox store), not part of batching. |
| 2 | OpenTelemetry | ✅ | The outbox keeps its per-message outcome counters through the shared completion path, the collector's counters and histograms (#302) gain the `bytes` flush reason, and the run size is logged (EventId 2973). |
| 3 | Structured Logging | ✅ | EventIds 2973-2976 in the existing `Messaging` range; error codes and exception types only. |
| 4 | Health Checks | ❌ | No new external dependency: the outbox store and processor already have their checks. |
| 5 | Validation | ❌ | Outbox payloads were validated when they were produced; options are validated at startup, which is configuration validation. |
| 6 | Resilience | ⏭️ | Retry, backoff and dead letter stay per message in the existing outbox code; a policy around a batch handler's own external calls is the resilience family's work (#742 for transports). |
| 7 | Distributed Locks | ⏭️ | Several processors competing for the same pending rows is #713's work (locks in outbox processing); batching changes no row ownership. |
| 8 | Transactions | ✅ | The cycle still saves once after the batch (`SaveChangesAsync`), unchanged, and a batch handler may use the cycle's scope; nothing here claims atomicity across messages. |
| 9 | Idempotency | ✅ | The inbox deduplicates each item before its batch is queued (Design Choice 8); outbox delivery stays at least once. |
| 10 | Multi-Tenancy | ⏭️ | Outbox messages have no `TenantId` yet (#737); runs are cut by tenant there. The collector's tenant-separated key (#302) covers the request path. |
| 11 | Module Isolation | ⏭️ | Module identifiers on messaging entities are #747's work; runs are cut by module there if it adds one to the message. |
| 12 | Audit Trail | ⏭️ | Auditing outbox and inbox processing is #749's work; batch dispatch does not change which events exist per message. |

---

## Prerequisites & Dependencies

- **#302, pull requests 1 and 2** (package, collector, options, `AddEncinaBatching`) before pull request 1 of this plan; **#349, pull request 2** (the registrar) for the notification handler scan in pull request 2.
- **Reference direction (D4)**: whichever of this plan's pull request 2 and #349 pull request 5 lands first adds the `Encina.Messaging` reference to `Encina.Batching`.
- **Related, not blocking**: #252 (bulk publish: a batch notification handler typically calls the transport's `PublishBatchAsync`), #737 and #747 (tenant and module on outbox messages), #713 (locks in outbox processing), #469 (ordering by partition key; D6's consecutive runs never reorder messages).
- **Order inside the family**: #302, #349, #339, #121; #252 is independent.

---

## Next Steps

1. Ask the maintainer D2, D4 and D6 when work on #121 starts; record the answers in `## Maintainer Decisions`.
2. Pull request 1: modes and the byte trigger (12 files), after #302 pull request 2.
3. Pull request 2: the notification handler contract and dispatcher (12 files).
4. Pull request 3: outbox batch dispatch (11 files).
5. Pull request 4: composition test and the 10-provider proof (14 files).
