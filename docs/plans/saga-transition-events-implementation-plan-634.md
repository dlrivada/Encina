# Implementation Plan: Saga State Transition Events via CDC

> **Issue**: [#634](https://github.com/dlrivada/Encina/issues/634) (`area-messaging-patterns`, `area-cdc`, `priority-low`, `p1-recommended`)
> **Type**: Feature
> **Complexity**: Medium (one handler, one filter, two publish paths; the cost is in the ten provider shapes and the CDC prerequisites)
> **Estimated Scope**: ~700-1,000 lines of production code + ~1,500-2,000 lines of tests, delivered in two pull requests
> **Related plans of the same family (#633, #635, #636, #2393)**: independent of #633 and #2393. #635 and #636 add other CDC handlers through the same `CdcConfiguration` seam (Design Choice 5); sibling plans are referred to by issue number only.

---

## Summary

External systems (dashboards, alerting, analytics, compliance logs) learn about saga lifecycle changes without polling the saga table and without coupling to `SagaOrchestrator`. A CDC handler watches the saga state table, compares the before and after images, and publishes a `SagaTransitionEvent` for each transition that passes a configurable filter.

### What the code does today (2026-11-11, `main` at `942c0f124`)

1. **The saga row.** `ISagaState` has `SagaId`, `SagaType`, `Data`, `Status` (a `string`), `CurrentStep` (an `int`), `StartedAtUtc`, `CompletedAtUtc`, `ErrorMessage`, `LastUpdatedAtUtc`, `TimeoutAtUtc` (`src/Encina.Messaging/Sagas/ISagaState.cs`). It is implemented by one `SagaState` class per provider package: `Encina.ADO.SqlServer|PostgreSQL|MySQL`, `Encina.Dapper.SqlServer|PostgreSQL|MySQL`, `Encina.EntityFrameworkCore` (table `SagaStates`, `SagaStateConfiguration.cs:14`) and `Encina.MongoDB` (document with `[BsonId]` on `SagaId` and camelCase `[BsonElement]` names). `Data` is the serialized saga payload and can hold personal data; `ErrorMessage` holds exception text.
2. **Six statuses, not four.** `SagaStatus` defines `Running`, `Completed`, `Compensating`, `Compensated`, `Failed` and `TimedOut` (`src/Encina.Messaging/Sagas/SagaOrchestrator.cs:579-609`). The issue lists four.
3. **A step advance is not a status change.** The orchestrator advances `CurrentStep` while the status stays `Running`. The issue's goal list includes "step completed", but its sketch publishes only when `before.Status != after.Status`.
4. **The handler contract.** `IChangeEventHandler<TEntity>` has `HandleInsertAsync`, `HandleUpdateAsync(before, after, context)` and `HandleDeleteAsync`, each returning `ValueTask<Either<EncinaError, Unit>>` (`src/Encina.Cdc/Abstractions/IChangeEventHandler.cs`). The dispatcher routes a table to one entity type through `CdcConfiguration.WithTableMapping` and to exactly one handler per entity type: `_entityToHandlerMap` is built with `ToFrozenDictionary` keyed by entity type (`src/Encina.Cdc/Processing/CdcDispatcher.cs:61-70`).
5. **An update without a before image fails.** `DispatchUpdateAsync` returns `CdcErrors.DeserializationFailed` when `before is null || after is null` (`CdcDispatcher.cs:189`). SQL Server Change Tracking never supplies a before image for an update (`SqlServerCdcConnector.cs`: "Change Tracking limitation: Before values are not available"), so no typed update handler can run on the SQL Server connector today. MongoDB supplies it only when pre-images are enabled (`change.FullDocumentBeforeChange`, `MongoCdcConnector.cs:244`), PostgreSQL only with `REPLICA IDENTITY FULL` (and the connector has a defect on old tuples, #2776), MySQL binlog does supply it.
6. **A `Left` from the dispatcher is not retried.** The processor counts it as a failure and the position moves on (#2343, open). The issue's acceptance criterion "a publish failure fails the CDC operation (retry or dead letter)" holds only after #2343.
7. **Publishing options that exist.** `IEncina.Publish` (used by `CdcMessagingBridge`, `src/Encina.Cdc/Messaging/CdcMessagingBridge.cs`) and `OutboxOrchestrator.AddAsync<TNotification>` (`src/Encina.Messaging/Outbox/OutboxOrchestrator.cs:87`). `ICdcEventInterceptor` results are ignored by the dispatcher, so an interceptor cannot be the publisher.

### Scope

- **Implements**: #634 for the ten `SagaState` shapes through one generic handler `SagaTransitionCdcHandler<TSagaState> where TSagaState : class, ISagaState`.
- **Affected packages**: `Encina.Cdc` (handler, options, filter, configuration, registration, diagnostics), `Encina.Messaging` (read-only: `ISagaState`, `SagaStatus`, `OutboxOrchestrator`), `Encina.OpenTelemetry` (subscription).
- **Provider category**: Database (saga stores, 10 providers) plus the CDC connectors (SQL Server, PostgreSQL, MySQL, MongoDB; Debezium reads the same tables). The handler is written once; the ten shapes are covered by a contract test that instantiates each real `SagaState`.
- **Corrections to the issue text**: `CurrentStep` is an `int`; the status set has six values; step advances are a separate decision (Design Choice 4); "Event" is published as an `INotification`.
- **Split into pull requests**: PR 1 = Phases 1-4 (about 11 files, handler and registration); PR 2 = Phases 5-8 (observability, tests, documentation).
- **Hard prerequisites** (Prerequisites & Dependencies): #2343 and the dispatcher issue drafted with this plan (updates without a before image, several handlers per table).

---

## Design Choices

<details>
<summary><strong>1. The previous status when there is no before image</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Require the before image; fail startup validation on a connector that cannot provide it** | Exact transitions | Impossible on the SQL Server Change Tracking connector, which never supplies a before image; PostgreSQL needs `REPLICA IDENTITY FULL`; MongoDB needs pre-images |
| **B) `PreviousStatus` is nullable; the event always carries `CurrentStatus`; a filter with a specific `From` does not match an unknown previous status, `*->X` does** | Works on every connector; consumers that need only the new state are served | A consumer that needs the exact `From` gets `null` on SQL Server |
| **C) Keep the last known status per saga in a store owned by the handler** | Exact `PreviousStatus` everywhere | Extra state and a store on the 10 providers, itself a CDC position-style persistence problem |

### Chosen Option: **B — nullable `PreviousStatus`** (recommended, pending the maintainer)

### Rationale

B is the only option that delivers the feature on all four connectors without a new store. A wins for deployments that control the database option and need exact `From->To` filters; C wins when exact transitions are needed on SQL Server. B needs the dispatcher to accept an update with no before image (prerequisite). When the before image exists, the event carries it and the filters match exactly.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Whether the event carries the failure text</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Include `ErrorMessage`** | Consumers see the cause | The column holds arbitrary exception text that may contain personal data; events leave the process |
| **B) Never include it; the event says only that the saga is `Failed` or `TimedOut`** | Consistent with the rule that error messages do not leave their store; also drops `Data` | A dashboard must read the saga store for the cause |
| **C) Configurable, default off** | Serves both audiences | One more switch that can leak when turned on; the text is still unclassified |

### Chosen Option: **B — no error text and no `Data` in the event** (recommended, pending the maintainer)

### Rationale

`ErrorMessage` and `Data` are the two columns that can carry personal data; the row has no error code column, so there is nothing safe to forward in their place. A wins for purely internal dashboards; C wins when both internal and external audiences exist and an operator accepts the risk knowingly.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Publish path — direct publish or the outbox</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Outbox by default (`PublishViaOutbox = true`)** via `OutboxOrchestrator.AddAsync` | Durable second hop; ordered with the database | Requires the outbox module (opt-in under ADR-050); a second persistence step for an at-least-once source |
| **B) Direct `IEncina.Publish`; the outbox is opt-in (`PublishViaOutbox = true`)** | No extra module; a `Left` fails the CDC event so CDC's own retry and dead letter apply | Depends on CDC retry working (#2343); a slow notification handler delays the CDC loop |

### Chosen Option: **B — direct publish by default, outbox opt-in** (recommended, pending the maintainer)

### Rationale

CDC is already an at-least-once, position-tracked source, so a second durable hop is redundant by default. A wins when events must survive a long outage of the notification handlers without blocking the CDC position, or when the application already runs the outbox for everything. Either way a failure returns `Left`.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Which changes are transitions — status changes only, or also step advances</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Status changes only** (the sketch) | Few events; simple filter | Never reports "step completed", which the issue's summary lists |
| **B) Status changes, plus `Running -> Running` with a higher `CurrentStep` when `IncludeStepAdvances = true` (default false)** | Opt-in progress events for dashboards; default volume unchanged | One more option; step advances need the before image (on SQL Server a step advance cannot be told from any other update) |

### Chosen Option: **B — opt-in step advances**

### Rationale

The option is off by default, so pay-for-what-you-use holds; where the before image is missing, step advances are not reported and a startup log says so.

**Class:** Trivial
**Settled by:** AGENTS.md section 1 (pay for what you use: every feature opt-in)

</details>

<details>
<summary><strong>5. Registration seam and handler shape</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `CdcConfiguration.WithSagaTransitionEvents<TSagaState>(opts)` registers the table mapping and the generic handler for the application's provider `SagaState` type** | One handler for ten shapes; matches `WithMessagingBridge` and `WithCacheInvalidation` | The application names its provider's `SagaState` type once |
| **B) One handler per provider package** | No generic argument | Ten near-identical handlers |
| **C) A handler over `JsonElement` (as `OutboxCdcHandler`)** | No provider type | `JsonElement` is already the key of the outbox handler; the dispatcher allows one handler per entity type |

### Chosen Option: **A — generic handler registered through `WithSagaTransitionEvents<TSagaState>`**

### Rationale

The fluent style of `CdcConfiguration` is the established seam. The table name defaults to `SagaStates` (the default of the ADO.NET and Dapper stores and the EF Core configuration) and the MongoDB collection name comes from `config.Collections.Sagas`. The registration lives in a private `RegisterSagaTransitionServices` method so that the sibling CDC plans, which edit the same three files, do not conflict.

**Class:** Trivial
**Settled by:** AGENTS.md section 3 (registration completeness) and the existing `CdcConfiguration` fluent style

</details>

<details>
<summary><strong>6. Duplicate delivery</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The event carries `DeduplicationKey = {SagaId}:{CDC position}`; consumers (or the inbox) deduplicate** | CDC is at-least-once; the key is stable across replays | Consumers must use it |
| **B) The handler remembers the last event per saga** | Fewer duplicates | State that is lost on restart and does not help across instances |

### Chosen Option: **A — deduplication key on the event**

### Rationale

Idempotent processing belongs to the inbox on the consumer side.

**Class:** Settled
**Settled by:** AGENTS.md section 6 (function 9, idempotency) and section 3 (Inbox: idempotent exactly-once processing)

</details>

---

## Implementation Phases

### Phase 1: Models, options and the transition filter

> **Goal**: pure types with no I/O.

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.Cdc/Sagas/SagaTransitionEvent.cs`: `sealed record SagaTransitionEvent(Guid SagaId, string SagaType, string? PreviousStatus, string CurrentStatus, int CurrentStep, DateTime TransitionAtUtc, string DeduplicationKey) : INotification`.
2. `SagaTransitionOptions`: `PublishViaOutbox` (false), `IncludeTransitions` (empty means all), `ExcludeSagaTypes`, `IncludeStepAdvances` (false), `SagaTableName` (default `SagaStates`); validated by `IValidateOptions`.
3. `SagaTransitionFilter`: parses `From->To`, `*->To`, `From->*`, `None->To` (`None` matches a null previous status), compiled once; rejects an unknown status name using `SagaStatus` constants.
4. `SagaTransitionErrors` (error codes `encina.cdc.saga_transition_publish_failed`, `...invalid_filter`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT: ISagaState/SagaStatus live in Encina.Messaging.Sagas; Encina.Cdc already references Encina.Messaging.
TASK: Add SagaTransitionEvent, SagaTransitionOptions with its validator, SagaTransitionFilter and the error codes.
KEY RULES: AtUtc suffix; no Data and no ErrorMessage field on the event; Either returns; XML docs; the six
SagaStatus values are the vocabulary; the filter is pure and thread-safe.
REFERENCE FILES: src/Encina.Messaging/Sagas/SagaOrchestrator.cs (SagaStatus), src/Encina.Cdc/Messaging/CdcMessagingOptions.cs,
src/Encina.Cdc/Errors/CdcErrorCodes.cs.
```

</details>

### Phase 2: The CDC handler

> **Goal**: turn row changes into transitions.

<details>
<summary><strong>Tasks</strong></summary>

1. `SagaTransitionCdcHandler<TSagaState> : IChangeEventHandler<TSagaState>`: `HandleInsertAsync` publishes a `null -> Status` transition (the first state, usually `Running`); `HandleUpdateAsync` publishes when `before.Status != after.Status`, or on a step advance when enabled; `HandleDeleteAsync` returns `Right(unit)`.
2. Maps `ISagaState` to the event; the deduplication key uses `context.Metadata.Position.ToString()`.
3. A `Left` from the publisher is returned unchanged; no `catch` that turns it into success.
4. `TransitionAtUtc` is `after.LastUpdatedAtUtc` when set, otherwise `context.Metadata.CapturedAtUtc` (no clock read).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT: The dispatcher gives the handler a typed before and after entity. SQL Server updates have no before image.
TASK: Implement SagaTransitionCdcHandler<TSagaState> against ISagaTransitionPublisher (Phase 3); handle a missing
before image by publishing with PreviousStatus null once the dispatcher prerequisite lands.
KEY RULES: never swallow a Left; never log ErrorMessage or Data; ids and statuses only in logs; TimeProvider where
a clock is needed; the handler is scoped (the dispatcher resolves it from DI).
REFERENCE FILES: src/Encina.Cdc/Caching/QueryCacheInvalidationCdcHandler.cs, src/Encina.Cdc/Messaging/OutboxCdcHandler.cs,
src/Encina.Cdc/Processing/CdcDispatcher.cs.
```

</details>

### Phase 3: Publish paths

> **Goal**: direct publish and outbox publish behind one small abstraction.

<details>
<summary><strong>Tasks</strong></summary>

1. `ISagaTransitionPublisher` (internal) with `PublishAsync(SagaTransitionEvent, CancellationToken)` returning `Either<EncinaError, Unit>`.
2. `DirectSagaTransitionPublisher` (`IEncina.Publish`) and `OutboxSagaTransitionPublisher` (`OutboxOrchestrator.AddAsync`).
3. The outbox variant is selected only when `PublishViaOutbox` is true; selecting it without the outbox module registered fails at startup validation with a registration error.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT: CdcMessagingBridge publishes through IEncina.Publish; OutboxOrchestrator.AddAsync writes the outbox.
TASK: Implement the two publishers and the startup check for the outbox option.
KEY RULES: return the Either unchanged; no exception for business failures; the outbox module stays opt-in (ADR-050).
REFERENCE FILES: src/Encina.Cdc/Messaging/CdcMessagingBridge.cs, src/Encina.Messaging/Outbox/OutboxOrchestrator.cs.
```

</details>

### Phase 4: Configuration, DI and startup validation

> **Goal**: `WithSagaTransitionEvents<TSagaState>` works and fails loudly when misconfigured.

<details>
<summary><strong>Tasks</strong></summary>

1. `CdcConfiguration.WithSagaTransitionEvents<TSagaState>(Action<SagaTransitionOptions>? configure = null)`: sets `CdcOptions.UseSagaTransitionEvents`, adds `TableMapping(opts.SagaTableName, typeof(TSagaState))` and `HandlerRegistration(typeof(TSagaState), typeof(SagaTransitionCdcHandler<TSagaState>))`.
2. `ServiceCollectionExtensions.AddEncinaCdc`: a private `RegisterSagaTransitionServices` registers options, validator, filter and the chosen publisher.
3. A startup validator rejects an empty or whitespace table name and a table that has another handler registered for the same entity type (until the dispatcher prerequisite allows several).
4. DI test with `ValidateOnBuild` and `ValidateScopes` for both publishers.
5. `PublicAPI.Unshipped.txt` lines.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT: Three CDC plans edit CdcConfiguration, CdcOptions and ServiceCollectionExtensions; keep each edit small and
isolated in a private method.
TASK: Add WithSagaTransitionEvents<TSagaState>, the CdcOptions flag, the registration method, the validator and the DI test.
KEY RULES: every option type and dependency the handler resolves is registered (registration completeness, AGENTS.md
section 3); disabled by default; no [Obsolete]; XML docs with an example.
REFERENCE FILES: src/Encina.Cdc/CdcConfiguration.cs (WithCacheInvalidation), src/Encina.Cdc/ServiceCollectionExtensions.cs
(RegisterCacheInvalidationServices), src/Encina.Cdc/CdcOptions.cs.
```

</details>

### Phase 5: Provider coverage (10 saga shapes, 4 connectors)

> **Goal**: the same behavior for every `SagaState`.

<details>
<summary><strong>Tasks</strong></summary>

1. Contract test over the ten real `SagaState` types (created through each package's `SagaStateFactory`): same events for the same status sequence.
2. MongoDB: the document id is `_id` (`[BsonId]` on `SagaId`) and the other fields are camelCase; verify what shape `MongoCdcConnector` hands to the dispatcher (`ToJson()`, `MongoCdcConnector.cs:244`) and add the mapping needed so `SagaId` is read from `_id`.
3. Debezium: the envelope's `before`/`after` carry the same columns; no code change expected, covered by a unit test.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
CONTEXT: Ten SagaState classes implement ISagaState; four connectors emit different JSON shapes.
TASK: Prove the handler behaves the same on all ten types and fix the MongoDB id mapping if the test shows a gap.
KEY RULES: tests instantiate the real SagaState and the real handler; no reflection-only tests.
REFERENCE FILES: src/Encina.MongoDB/Sagas/SagaState.cs, src/Encina.Cdc.MongoDb/MongoCdcConnector.cs,
src/Encina.ADO.SqlServer/Sagas/SagaStateFactory.cs.
```

</details>

### Phase 6: Observability

> **Goal**: published, filtered and failed transitions are visible.

<details>
<summary><strong>Tasks</strong></summary>

1. `SagaTransitionMetrics` (meter `Encina.Cdc.SagaTransitions`): counter `encina.cdc.saga_transitions` (tags `saga.type`, `saga.status`), counter `encina.cdc.saga_transition_errors` (tag `reason`).
2. `SagaTransitionActivitySource` span around the publish with `saga.id` and `saga.status`.
3. `SagaTransitionLog` (`[LoggerMessage]`, EventIds 4860-4869 in the existing `Cdc` range): published, filtered out, missing before image, publish failed (error code only).
4. `Encina.OpenTelemetry` subscription.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
CONTEXT: Operators need counts by saga type and status and the reason for a failure.
TASK: Add the metrics, the span, the log class and the OpenTelemetry subscription.
KEY RULES: EventIds 4860-4869 only (range Cdc, ADR-021); tags carry ids, types and statuses, never error text or Data;
Counter<long> with tags.
REFERENCE FILES: src/Encina.Cdc/Caching/Diagnostics/CacheInvalidationMetrics.cs, src/Encina.Cdc/Caching/CdcCacheInvalidationLog.cs.
```

</details>

### Phase 7: Testing

> **Goal**: each applicable flag reaches its manifest target.

<details>
<summary><strong>Tasks</strong></summary>

1. Unit: handler for each operation, missing before image, filter patterns, step advances, publisher failure returns `Left`.
2. Guard: public methods.
3. Contract: the ten `SagaState` types and both publishers.
4. Property: filter parsing never throws on valid grammar; a status sequence with filter `*->*` publishes exactly the number of changes.
5. Integration: connector-driven runs on PostgreSQL, MySQL, SQL Server and MongoDB through the shared fixtures; they depend on the connector integration work (#2861, #2862, #2839) for their fixtures. The handler-level tests run without it.
6. Load and benchmark: justification files (a pure comparison per event).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
CONTEXT: Coverage is per flag (AGENTS.md section 9).
TASK: Write the tests above and compare each flag with .github/coverage-manifest/Encina.Cdc.json.
KEY RULES: tests run real code; Shouldly via Encina.Testing.Shouldly; shared [Collection] fixtures; no Thread.Sleep.
REFERENCE FILES: tests/Encina.UnitTests (Cdc), docs/testing/integration-tests.md.
```

</details>

### Phase 8: Documentation and finalization

> **Goal**: users can enable and consume saga transition events.

<details>
<summary><strong>Tasks</strong></summary>

1. XML docs on all new public APIs.
2. `changelog.d/634-saga-transition-events.added.md`.
3. `src/Encina.Cdc/README.md` and `docs/features/cdc.md` (the per-connector before-image table, the filter grammar, the deduplication key, publish paths).
4. ADR number assigned when the ADR pull request opens only if Design Choice 1 or 3 changes shared CDC behavior.
5. `PublicAPI.Unshipped.txt`; `docs/INVENTORY.md`.
6. Verification: build with zero warnings, tests, per-flag coverage.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```
CONTEXT: The feature is code-complete after Phase 7.
TASK: Documentation, changelog fragment, PublicAPI lines, verification.
KEY RULES: never type coverage figures by hand; English only; follow the encina-docs skill; never edit CHANGELOG.md.
REFERENCE FILES: .claude/skills/encina-docs/SKILL.md, changelog.d/README.md, docs/features/cdc.md.
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Relevance |
|--------|-----------|
| AGENTS.md section 3 | Errors are never swallowed in background infrastructure; `EncinaError.Message` never reaches logs |
| AGENTS.md section 3 and ADR-050 | Messaging patterns are opt-in; the outbox is required only where the guarantee needs it |
| AGENTS.md section 6, function 9 | Idempotency: deduplication key on the event |
| Saga pattern (compensating transactions) | The six `SagaStatus` values define the lifecycle |
| SQL Server Change Tracking, PostgreSQL `REPLICA IDENTITY`, MySQL binlog row image, MongoDB change stream pre-images | Which connectors can supply a before image |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|----------------------|
| `IChangeEventHandler<T>`, `CdcDispatcher`, `CdcConfiguration` | `src/Encina.Cdc` | Handler contract, routing, fluent registration |
| `ISagaState`, `SagaStatus`, `SagaStateFactory` per provider | `src/Encina.Messaging/Sagas`, provider `Sagas/` folders | Row shape and test construction |
| `OutboxOrchestrator.AddAsync` | `src/Encina.Messaging/Outbox` | Optional publish path |
| `CdcMessagingBridge` | `src/Encina.Cdc/Messaging` | Direct publish pattern |
| `CdcHealthCheck`, `CdcDeadLetterStore` | `src/Encina.Cdc/Health`, `DeadLetter` | Connector health; dead letter of failed events |
| `CacheInvalidationMetrics` | `src/Encina.Cdc/Caching/Diagnostics` | Metrics pattern |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Cdc` (existing range 4800-4899) | 4860-4869 | `SagaTransitionLog`; 4854-4859 belong to the position store plan (#2393), 4870-4899 to the sibling CDC plans |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Production (`Encina.Cdc`) | 9 | event, options, filter, errors, handler, publishers (3), log, metrics, activity source |
| Edits to shared files | 3 | `CdcConfiguration`, `CdcOptions`, `ServiceCollectionExtensions` |
| Tests | ~14 | unit, guard, contract, property, integration |
| Docs, changelog, manifests | ~6 | README, feature page, fragment, manifest entries |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
PROJECT CONTEXT: Encina is a pre-1.0 .NET 10 / C# 14 library with nullable reference types. You are implementing
issue #634: publish saga state transition events detected through CDC. Read AGENTS.md first.

IMPLEMENTATION OVERVIEW: Phase 1 adds SagaTransitionEvent (an INotification without Data or error text), the options
and the transition filter. Phase 2 adds SagaTransitionCdcHandler<TSagaState>. Phase 3 adds the direct and outbox
publishers. Phase 4 adds CdcConfiguration.WithSagaTransitionEvents<TSagaState>, registration and startup validation.
Phase 5 proves the ten SagaState shapes and the MongoDB id mapping. Phase 6 adds metrics, spans and logs (EventIds
4860-4869). Phase 7 tests; Phase 8 documents. Deliver as two pull requests after the CDC prerequisites land.

KEY PATTERNS: a Left from publishing is returned, never swallowed; nullable PreviousStatus when the connector gives no
before image; opt-in everywhere; TimeProvider; ids and statuses in logs, never ErrorMessage or Data; registration
completeness proven by a DI test with ValidateOnBuild and ValidateScopes.

REFERENCE FILES: src/Encina.Cdc/CdcConfiguration.cs, src/Encina.Cdc/Processing/CdcDispatcher.cs,
src/Encina.Messaging/Sagas/ISagaState.cs, src/Encina.Messaging/Outbox/OutboxOrchestrator.cs,
src/Encina.Cdc/Messaging/CdcMessagingBridge.cs, .github/coverage-manifest/Encina.Cdc.json.
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Not applicable: event publishing, not a data read |
| 2 | OpenTelemetry | ✅ | Phase 6: counters by saga type and status, publish span |
| 3 | Structured Logging | ✅ | `SagaTransitionLog`, EventIds 4860-4869 |
| 4 | Health Checks | ✅ | The existing `CdcHealthCheck` covers the connector; no new dependency to check |
| 5 | Validation | ✅ | Options and filter validated at startup; event payload validation is #730 |
| 6 | Resilience | ✅ | Retry and dead letter of a failed event come from the CDC pipeline (#2343, #2344) |
| 7 | Distributed Locks | ⏭️ | One consumer per source is the leader election work of #1251 |
| 8 | Transactions | ❌ | Not applicable: CDC observes committed transactions |
| 9 | Idempotency | ✅ | `DeduplicationKey` on the event (Design Choice 6) |
| 10 | Multi-Tenancy | ⏭️ | Tenant-aware routing of transition events follows #1257; the saga row has no tenant column today |
| 11 | Module Isolation | ❌ | Not applicable: the handler is infrastructure-level |
| 12 | Audit Trail | ⏭️ | The external audit stream for saga rows is the generic audit streaming of #636; this plan publishes domain notifications only |

---

## Prerequisites & Dependencies

| Item | State | Effect |
|------|-------|--------|
| #2343 (a `Left` from the dispatcher is neither retried nor dead-lettered) | open | Required for the acceptance criterion "a publish failure fails the CDC operation" |
| Dispatcher issue drafted with this plan (updates without a before image; several handlers per table) | to be opened | Required on SQL Server, where no update has a before image; a second handler per saga table is not needed by this plan |
| #2861, #2862, #2839 (connector integration tests) | open | Fixtures for the connector-driven integration tests |
| #2776 (PostgreSQL connector fails updates that carry an old tuple) | open | Exact transitions on PostgreSQL |

---

## Next Steps

1. Review and approve this plan; the maintainer answers the three Dilemmas (choices 1, 2 and 3) when work on the issue starts.
2. Publish the plan link as a comment on #634.
3. Begin PR 1 (Phases 1-4) after #2343 and the dispatcher issue land.
4. Each phase is a self-contained commit; the final commit references `Fixes #634`.
