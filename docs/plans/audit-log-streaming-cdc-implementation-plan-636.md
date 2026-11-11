# Implementation Plan: Audit Log Streaming via CDC

> **Issue**: [#636](https://github.com/dlrivada/Encina/issues/636) (`area-cdc`, `priority-low`, `complexity-low`, `p1-recommended`)
> **Type**: Feature
> **Complexity**: Medium (the issue is labelled low; the dispatcher hook, redaction that fails closed and the write-through rule add scope)
> **Estimated Scope**: ~1,100-1,500 lines of production code + ~2,000-2,700 lines of tests, delivered in three pull requests
> **Related plans of the same family (#633, #634, #635, #2393)**: independent of #633. #634 and #635 add typed CDC handlers; this plan adds a dispatcher-level observer instead (Design Choice 4) and owns the rule that infrastructure tables are never audited. Sibling plans are referred to by issue number only.

---

## Summary

Changes captured by CDC are streamed as structured audit entries to external sinks, so the audit trail is independent of the application's write path, includes changes made by any source (not only the application), and does not share a database with the data it audits.

### What the code does today (2026-11-11, `main` at `942c0f124`)

1. **Entity audit data lives in the application database.** `IAuditable` is filled by `AuditInterceptor` (`src/Encina.EntityFrameworkCore/Auditing`) and change entries go to `IAuditLogStore.LogAsync(AuditLogEntry)` (`src/Encina.DomainModeling/Auditing/IAuditLogStore.cs`), which returns a bare `Task`; its rename and redesign are tracked by #1778 (ADR-036). `AuditLogEntry` already has `EntityType`, `EntityId`, `Action`, `UserId`, `TimestampUtc`, `OldValues`, `NewValues`, `CorrelationId`. Operation and read audit have their own stores in `Encina.Security.Audit` (`IOperationAuditStore`, `IReadAuditStore`); `IPiiMasker` there masks typed request objects, not JSON rows.
2. **Nothing streams CDC changes.** There is no `IAuditSink` or `CdcAuditEntry` in the repository.
3. **The dispatcher is the only place that sees every event, and it has limits.** `CdcDispatcher.DispatchAsync` returns early with `NoHandlerForTable` when a table has no mapping and handler (`CdcDispatcher.cs:79-83`); `ICdcEventInterceptor` runs only after a successful handler dispatch, its `Either` result is discarded and an exception is only logged (`CdcDispatcher.cs:264-285`). One handler is allowed per entity type (`CdcDispatcher.cs:61-70`), and an update without a before image is a `DeserializationFailed` `Left` (`CdcDispatcher.cs:189`). A typed handler also drops every column that is not on the entity class.
4. **What the connectors hand over.** `ChangeEvent.Before` and `After` are `object?`: a `JsonElement` from the SQL Server and Debezium connectors, dictionaries from the PostgreSQL and MySQL connectors, and a JSON `string` from MongoDB (`change.FullDocument?.ToJson()`, `MongoCdcConnector.cs:244-245`). SQL Server Change Tracking supplies no before image for an update (`SqlServerCdcConnector.cs`, "Change Tracking limitation"). `ChangeMetadata` has `Position`, `CapturedAtUtc`, `TransactionId`, `SourceDatabase`, `SourceSchema`.
5. **A `Left` from the dispatcher is neither retried nor dead-lettered** (#2343, open); the issue's "a sink failure fails the CDC operation" holds only after it.
6. **Infrastructure tables are also tables.** The outbox (`OutboxMessages`), inbox, saga (`SagaStates`), scheduled message, dead letter and (after #2393) `CdcPositions` tables change constantly. An `OutboxAuditSink` writes into the outbox table, which is itself a change that CDC captures.

### Scope

- **Implements**: #636 with three built-in sinks (`ConsoleAuditSink`, `FileAuditSink`, `OutboxAuditSink`) in `Encina.Cdc`. Elasticsearch, Azure Monitor and S3 sinks are separate packages outside this issue (each cloud-tied one is subject to SPEC-000 section 6).
- **Affected packages**: `Encina.Cdc` (abstractions, observer, projection, redaction, sinks, configuration, diagnostics), `Encina.Messaging` (read-only: `OutboxOrchestrator`), `Encina.OpenTelemetry` (subscription).
- **Provider category**: Database through the four CDC connectors (SQL Server, PostgreSQL, MySQL, MongoDB; Debezium carries the same shapes). The streaming code is written once against `ChangeEvent`; the shapes per connector are covered by tests.
- **Corrections to the issue text**: the sink abstraction is `ICdcAuditSink` (avoids confusion with the `Encina.Security.Audit` and #1778 vocabulary); `BatchSize` and `FlushInterval` are dropped (Design Choice 5); the console sink writes metadata only; compliance references other than the EU and Spain (HIPAA, PCI-DSS) are out of scope.
- **Split into pull requests**: PR 1 = Phases 1-3 (abstractions, observer, projection and redaction, about 13 files); PR 2 = Phases 4-5 (sinks, configuration, about 11 files); PR 3 = Phases 6-9 (provider shapes, observability, integration tests, documentation).

---

## Design Choices

<details>
<summary><strong>1. What makes the stream tamper-evident</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Rely on the sink's immutability** (append-only storage, object lock); Encina adds no chain | No key management in Encina; honest about what a file sink is | A plain file or table sink is not tamper-evident by itself |
| **B) A hash chain in `CdcAuditEntry`** (each entry carries the HMAC-SHA256 of the previous entry and its own fields) | Tamper evidence independent of the sink | Needs an HMAC key held outside the database (a plain hash can be recomputed by someone who can rewrite the file), strict ordering across instances, and a verifier |
| **C) Both, the chain optional** | Serves managed immutable stores and plain files | Two paths to build and document |

### Chosen Option: **A — sink immutability; the chain is not built in this issue** (recommended, pending the maintainer)

### Rationale

A chain without key custody gives little: an attacker who can rewrite the file can rewrite the chain. B or C win for a SOC 2 or ISO/IEC 27001 style audit where sinks are plain files or databases and a key custodian exists. The documentation states plainly that the built-in file sink is append-only by convention, not by guarantee.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. How sensitive fields are kept out of the stream</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Deny list per table** (`RedactFields`, the proposal) | Easy to adopt | A new sensitive column is written in clear until someone lists it |
| **B) Allow list per table: only listed fields are written, the rest become `[REDACTED]`; `AllowAllFields()` is an explicit, logged opt-out** | Fails closed for new columns; fits personal data (GDPR Art. 5(1)(c), 32) | More configuration |
| **C) Deny list plus a startup warning for fields classified as personal data and not redacted** | Light configuration | Needs data classification attributes on the entity, which the streaming path (raw rows) does not read |

### Chosen Option: **B — allow list, fail closed** (recommended, pending the maintainer)

### Rationale

A wins for a few well-known entities in a non-regulated deployment; C wins where entities already carry classification attributes. Audit data is a second copy of personal data in another system; default-deny matches the fail-closed rule for compliance gates, and the explicit opt-out is logged.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Whether the sink abstraction is new or an audit store implementation</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A new `ICdcAuditSink` in `Encina.Cdc`** | Independent of `Encina.Security.Audit` and `Encina.DomainModeling` auditing; ROP return types | A second audit vocabulary |
| **B) Implement the stream as another `IAuditLogStore` / `IEntityChangeAuditStore`** | One audit model for all sources | `IAuditLogStore.LogAsync` returns a bare `Task` and is being renamed and redesigned (#1778, ADR-036); `AuditLogEntry` has no source position; ties CDC to a moving contract |

### Chosen Option: **A — `ICdcAuditSink`** (recommended, pending the maintainer)

### Rationale

A wins while #1778 is in flight and when the stream must run without `Encina.Security.Audit`. B wins once ADR-036 lands and one audit model is wanted across all sources; a sink adapter over the new store is then a small class. `CdcAuditEntry` keeps `Operation`, `Before`, `After`, `Position` and `Source`, which the entity-change entry lacks.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Where the audit tap hooks in</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A typed handler per entity** (`AuditStreamCdcHandler<TEntity>`, the proposal) | Fits the handler model | Only one handler per entity type, a table needs a mapping and a handler, the typed row loses every unmapped column, and an update with no before image fails the event |
| **B) `ICdcEventInterceptor`** | Exists today | Runs only after a handler dispatch, ignores the `Either` and swallows exceptions, so a sink failure cannot fail the event |
| **C) A new `ICdcChangeObserver` that the dispatcher awaits for every event before handler lookup; a `Left` fails the event** | Sees the raw row with all columns; independent of handlers; failure semantics match the acceptance criterion | A small, new dispatcher contract |

### Chosen Option: **C — `ICdcChangeObserver`** (recommended, pending the maintainer)

### Rationale

C is the only option that satisfies "every change reaches the sink and a sink failure fails the CDC operation" for tables that have no handler. A wins when an application already maps the entity and wants typed access to its fields; it can be built on C later. The observer is also usable by other cross-table consumers.

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Write-through instead of timed batching</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Each observed event is written to the sinks before the observer returns; `WriteBatchAsync` exists for sinks and callers that already hold a batch** | A crash cannot lose an entry that CDC has already moved past; satisfies the acceptance criterion | One write per event; sinks decide whether to buffer internally and must flush before returning |
| **B) Buffer in memory and flush on `BatchSize` or `FlushInterval` (the proposal)** | Fewer writes | The processor saves the position after the observer returns, so a crash drops buffered entries for good |
| **C) Buffer, and hold the position until the flush** | Batching without loss | Needs a batch-commit hook in the processor (not present: positions are saved per event) |

### Chosen Option: **A — write-through; `BatchSize` and `FlushInterval` are dropped**

### Rationale

The acceptance criteria say no entry is dropped and a sink failure fails the CDC operation; with per-event position saves (`CdcProcessor.cs:161`) only a write-through observer meets both.

**Class:** Settled
**Settled by:** #636 (acceptance criteria: a sink failure fails the CDC operation, no entry is dropped); AGENTS.md section 3 (errors are never swallowed in background infrastructure)

</details>

<details>
<summary><strong>6. Infrastructure tables are never audited</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Tables are included only by an explicit allow list; a startup validation rejects the infrastructure tables (outbox, inbox, saga, scheduled message, dead letter, CDC positions, the audit tables)** | No feedback loop through `OutboxAuditSink`; no audit noise | The list of infrastructure tables is maintained in one place |
| **B) Audit all tables except a deny list** | Less configuration | A forgotten table creates a loop (outbox row, audit entry, outbox row) |

### Chosen Option: **A — allow list plus validation**

### Rationale

An outbox sink that writes to a table that is itself audited never ends; the allow list prevents it by construction and the validation names the offending table.

**Class:** Settled
**Settled by:** #636 (the proposal's own `ExcludeEntities<AuditLog>()` rule against recursive auditing); AGENTS.md section 1 (every feature is opt-in)

</details>

---

## Implementation Phases

### Phase 1: Models and the sink abstraction

> **Goal**: pure types and the sink contract.

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.Cdc/Auditing/CdcAuditEntry.cs`: `EntityType`, `EntityId`, `Operation`, `Before`, `After` (JSON strings after redaction), `TimestampUtc` (`ChangeMetadata.CapturedAtUtc`), `Source` (`SourceDatabase.SourceSchema`), `Position` (`ToString()`, opaque), `ChangedBy`, `DeduplicationKey` (`{ConnectorId}:{Position}`).
2. `ICdcAuditSink`: `WriteAsync(CdcAuditEntry, CancellationToken)` and `WriteBatchAsync(IReadOnlyList<CdcAuditEntry>, CancellationToken)`, both `Task<Either<EncinaError, Unit>>`.
3. `CdcAuditOptions`: `Tables` (allow list), per-table `AuditFieldPolicy`, `IncludeOperations` / `ExcludeOperations`, `EntityIdField` per table, `ChangedByField` per table; validated by `IValidateOptions`; sink options holding paths or keys are `[JsonIgnore]` with a `ToString()` override.
4. `CdcAuditErrors` (`encina.cdc.audit_sink_failed`, `encina.cdc.audit_policy_invalid`, `encina.cdc.audit_infrastructure_table`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT: Entries must carry redacted JSON, an opaque position and a deduplication key; ROP return types everywhere.
TASK: Add CdcAuditEntry, ICdcAuditSink, CdcAuditOptions with validator and the error codes.
KEY RULES: AtUtc suffix; Either returns; XML docs with examples; options with paths or secrets are [JsonIgnore] with a
ToString() override; no [Obsolete]; nothing names a private application.
REFERENCE FILES: src/Encina.DomainModeling/Auditing/AuditLogEntry.cs, src/Encina.Cdc/ChangeEvent.cs,
src/Encina.Cdc/Messaging/CdcMessagingOptions.cs.
```

</details>

### Phase 2: The dispatcher observer

> **Goal**: every change event reaches the audit tap, and a failure fails the event.

<details>
<summary><strong>Tasks</strong></summary>

1. `ICdcChangeObserver` (public): `ValueTask<Either<EncinaError, Unit>> ObserveAsync(ChangeEvent, CancellationToken)`.
2. `CdcDispatcher.DispatchAsync`: resolve the observers (`GetServices<ICdcChangeObserver>()`) and await them in order before the handler lookup, so a table with no handler is still observed; the first `Left` is returned and the handler is not invoked (the event fails as a whole); an exception becomes `CdcErrors.HandlerFailed` through the existing path.
3. The sharded processor path calls the same dispatcher, so it is covered; `ICdcEventInterceptor` is left unchanged.
4. Contract and unit tests prove ordering, short-circuit on `Left`, and that tables without a handler are observed.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT: CdcDispatcher returns early when a table has no handler (NoHandlerForTable); interceptors run after a
successful handler dispatch and their results are discarded.
TASK: Add ICdcChangeObserver and call it from CdcDispatcher before TryResolveHandler.
KEY RULES: keep the change minimal because #2343 and the dispatcher issue drafted with these plans edit the same file;
a Left from an observer fails the event; no swallowing; existing behavior is unchanged when no observer is registered.
REFERENCE FILES: src/Encina.Cdc/Processing/CdcDispatcher.cs, src/Encina.Cdc/Messaging/ICdcEventInterceptor.cs,
src/Encina.Cdc/Processing/CdcProcessor.cs.
```

</details>

### Phase 3: Projection, redaction and filters

> **Goal**: turn a raw `ChangeEvent` into a safe `CdcAuditEntry`.

<details>
<summary><strong>Tasks</strong></summary>

1. `AuditRowProjector`: normalizes `object?` to a `JsonElement` (`JsonElement` as is; `string` parsed as JSON, the MongoDB shape; dictionaries serialized); a malformed payload is a `Left`, never an exception.
2. `AuditFieldPolicy`: allow-list projection (fields not listed become `"[REDACTED]"`), with `RedactFields` as an additional deny list and an explicit `AllowAllFields()` that logs once at startup; case-insensitive field names.
3. `EntityId` and `ChangedBy` extraction from the configured fields (defaults `Id`, `id`, `_id`; `ModifiedBy`, `DeletedBy`, `CreatedBy`).
4. `AuditStreamObserver : ICdcChangeObserver`: filters by table allow list and operation, projects, redacts, builds the entry, writes to every sink and returns the first `Left`; an update with no before image produces `Before = null`.
5. `AuditInfrastructureTables`: the single list of infrastructure table names used by the validator (Design Choice 6).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT: Connectors hand over JsonElement, dictionaries or a JSON string; SQL Server updates have no before image.
TASK: Implement the projector, the allow-list redaction, the observer and the infrastructure table list.
KEY RULES: fail closed: a field not on the allow list is redacted; never throw on a malformed row; never log row
contents or EncinaError.Message; TimeProvider where a clock is needed (use ChangeMetadata.CapturedAtUtc for the entry).
REFERENCE FILES: src/Encina.Cdc.MongoDb/MongoCdcConnector.cs (MapChangeEvent), src/Encina.Cdc.SqlServer/SqlServerCdcConnector.cs
(ReadChangeEvent), src/Encina.Cdc/Messaging/OutboxCdcHandler.cs.
```

</details>

### Phase 4: Built-in sinks

> **Goal**: `ConsoleAuditSink`, `FileAuditSink`, `OutboxAuditSink`.

<details>
<summary><strong>Tasks</strong></summary>

1. `ConsoleAuditSink`: structured log of metadata only (entity type, id, operation, position, source), never `Before`/`After`; an `IncludeValues` development switch exists and is rejected outside Development by the startup validation.
2. `FileAuditSink`: JSON Lines, one file per UTC day, opened append-only with write-through; every `WriteAsync` flushes before it returns (Design Choice 5); path and permissions documented; the directory option is `[JsonIgnore]` with `ToString()`.
3. `OutboxAuditSink`: wraps the entry in `CdcAuditNotification : INotification` and calls `OutboxOrchestrator.AddAsync`; it requires the outbox module and fails startup validation with a registration error when it is missing (ADR-050).
4. Contract tests run the same cases against all three sinks.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT: Audit entries contain personal data unless redacted; sinks must not leak it into logs.
TASK: Implement the three sinks and their options.
KEY RULES: write-through (flush before returning); return Left on any failure, never throw for I/O; metadata-only logging
for the console sink; asynchronous I/O with the CancellationToken; the file sink creates no world-readable file on
platforms that support permissions.
REFERENCE FILES: src/Encina.Messaging/Outbox/OutboxOrchestrator.cs, src/Encina.Cdc/Messaging/CdcMessagingBridge.cs.
```

</details>

### Phase 5: Configuration, DI and validation

> **Goal**: `WithAuditStreaming` registers everything and rejects unsafe configurations.

<details>
<summary><strong>Tasks</strong></summary>

1. `CdcConfiguration.WithAuditStreaming(Action<CdcAuditOptions>)` with `AddSink<TSink>()`, `AuditTable(string name, Action<AuditTablePolicy>)` and `AuditEntity<TEntity>(...)` (resolved to the mapped table name through `WithTableMapping`).
2. `ServiceCollectionExtensions`: private `RegisterAuditStreamingServices` (options, validator, projector, observer, sinks).
3. Validator: at least one sink; every table is allow-listed and none is an infrastructure table; `IncludeValues` only in Development; the outbox sink needs the outbox module.
4. DI test with `ValidateOnBuild` and `ValidateScopes` for each sink.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
CONTEXT: Three CDC plans edit CdcConfiguration, CdcOptions and ServiceCollectionExtensions; keep each edit isolated.
TASK: Add WithAuditStreaming, the registration, the validator and the DI test.
KEY RULES: disabled by default; every dependency resolved is registered; the validator names the offending table;
no [Obsolete].
REFERENCE FILES: src/Encina.Cdc/CdcConfiguration.cs, src/Encina.Cdc/ServiceCollectionExtensions.cs.
```

</details>

### Phase 6: Provider shapes

> **Goal**: the same entries from every connector.

<details>
<summary><strong>Tasks</strong></summary>

1. A contract test builds `ChangeEvent`s in each connector's real shape (SQL Server `JsonElement`, PostgreSQL and MySQL dictionaries, MongoDB JSON string, Debezium envelope) and asserts the same `CdcAuditEntry` for the same logical change.
2. Document, per connector, whether `Before` is available (MySQL yes; PostgreSQL with `REPLICA IDENTITY FULL`; SQL Server only for deletes; MongoDB with pre-images).
3. The ten database providers are covered through the four connectors; the application's data access provider does not change the stream.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
CONTEXT: Four connectors, four payload shapes.
TASK: Write the cross-connector contract test and the shape documentation; fix any gap in the projector it exposes.
KEY RULES: tests instantiate the real projector and observer; no reflection-only tests.
REFERENCE FILES: src/Encina.Cdc.PostgreSql/PostgresCdcConnector.cs, src/Encina.Cdc.MySql/MySqlCdcConnector.cs,
src/Encina.Cdc.Debezium/DebeziumCdcConnector.cs.
```

</details>

### Phase 7: Observability

> **Goal**: volume, failures and redaction are visible.

<details>
<summary><strong>Tasks</strong></summary>

1. `CdcAuditMetrics` (meter `Encina.Cdc.Audit`): counters `encina.cdc.audit.entries` (tags `sink`, `operation`), `encina.cdc.audit.errors` (tag `sink`), `encina.cdc.audit.redacted_fields`.
2. `CdcAuditActivitySource` spans per entry with `sink` and `operation`.
3. `CdcAuditLog` (EventIds 4880-4891): entry written, filtered out, sink failed, malformed row, allow-all opt-out, infrastructure table rejected (error code and ids only, never entry contents).
4. `Encina.OpenTelemetry` subscription.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
CONTEXT: Operators must see that the stream is alive without reading personal data.
TASK: Add metrics, spans, logs and the OpenTelemetry subscription.
KEY RULES: EventIds 4880-4891 only (range Cdc, ADR-021); never log Before/After or EncinaError.Message; tags carry sink,
operation and table names only.
REFERENCE FILES: src/Encina.Cdc/Caching/Diagnostics/CacheInvalidationMetrics.cs, src/Encina.Cdc/Caching/CdcCacheInvalidationLog.cs.
```

</details>

### Phase 8: Testing

> **Goal**: each applicable flag reaches its manifest target.

<details>
<summary><strong>Tasks</strong></summary>

1. Unit: projector, allow-list and deny-list policy, filters, observer ordering and short-circuit, sinks (console metadata only, file append and flush, outbox call), validator.
2. Guard: all public methods.
3. Contract: `ICdcAuditSink` across the three sinks; the observer contract in the dispatcher.
4. Property: a configured field never appears in an entry for any generated row; entry order equals event order.
5. Integration: real database CDC to the file sink on PostgreSQL, MySQL, SQL Server and MongoDB through shared fixtures (they depend on the connector integration work, #2861, #2862, #2839); outbox sink delivery through the outbox on one SQL provider.
6. Benchmark: throughput of projection plus redaction per event (hot path). Load: justification file.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```
CONTEXT: Coverage is per flag (AGENTS.md section 9).
TASK: Write the tests above and compare each flag with .github/coverage-manifest/Encina.Cdc.json.
KEY RULES: real code under test; Shouldly via Encina.Testing.Shouldly; shared [Collection] fixtures; FakeTimeProvider;
BenchmarkSwitcher.FromAssembly(...) for benchmarks; no Thread.Sleep.
REFERENCE FILES: tests/Encina.UnitTests (Cdc), tests/Encina.BenchmarkTests, docs/testing/integration-tests.md.
```

</details>

### Phase 9: Documentation and finalization

> **Goal**: users can stream audit entries safely.

<details>
<summary><strong>Tasks</strong></summary>

1. XML docs on all new public APIs.
2. `changelog.d/636-audit-log-streaming-cdc.added.md`.
3. `src/Encina.Cdc/README.md` and `docs/features/cdc.md`: sinks, the allow list, the before-image table per connector, the immutability statement of Design Choice 1, and the relation to `IAuditLogStore` (#1778).
4. ADR number assigned when the ADR pull request opens (tamper evidence and the observer contract).
5. `PublicAPI.Unshipped.txt` lines; `docs/INVENTORY.md`.
6. Verification: build with zero warnings, tests, per-flag coverage.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 9</strong></summary>

```
CONTEXT: The feature is code-complete after Phase 8.
TASK: Documentation, changelog fragment, PublicAPI lines, verification.
KEY RULES: never type coverage figures by hand; English only; compliance texts cover Spain and the EU only; follow the
encina-docs skill; never edit CHANGELOG.md.
REFERENCE FILES: .claude/skills/encina-docs/SKILL.md, changelog.d/README.md, docs/features/cdc.md.
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Relevance |
|--------|-----------|
| GDPR Article 5(1)(c), 5(1)(f), 32 | Data minimisation, integrity and confidentiality: the stream is a second copy of personal data and is redacted by an allow list |
| GDPR Article 30 and 5(2) | Records and accountability: the stream is evidence of changes |
| Spanish Esquema Nacional de Seguridad (Real Decreto 311/2022) | Activity logging and traceability requirements for public-sector systems |
| ISO/IEC 27001:2022 Annex A (logging) | Logging and monitoring of security events |
| AGENTS.md section 3 | A `Left` fails the operation; secrets in options; fail-closed gates |
| ADR-036 and #1778 | The entity-change audit store this stream complements and does not replace |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|----------------------|
| `CdcDispatcher`, `ChangeEvent`, `ChangeMetadata` | `src/Encina.Cdc` | Observer hook and payload |
| `ICdcEventInterceptor`, `CdcMessagingBridge` | `src/Encina.Cdc/Messaging` | Pattern reference (not reused, Design Choice 4) |
| `OutboxOrchestrator.AddAsync` | `src/Encina.Messaging/Outbox` | `OutboxAuditSink` |
| `IAuditLogStore`, `AuditLogEntry` | `src/Encina.DomainModeling/Auditing` | Vocabulary reference |
| `CdcHealthCheck`, `CdcDeadLetterStore` | `src/Encina.Cdc/Health`, `DeadLetter` | Connector health; dead letter after #2343 |
| `CacheInvalidationMetrics` | `src/Encina.Cdc/Caching/Diagnostics` | Metrics pattern |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Cdc` (existing range 4800-4899) | 4880-4891 used, 4892-4899 spare | `CdcAuditLog`; 4854-4859 belong to #2393, 4860-4869 to #634, 4870-4879 to #635 |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Production (`Encina.Cdc`) | 17 | entry, sink interface, options, errors, observer interface and implementation, projector, field policy, infrastructure list, three sinks, log, metrics, activity source |
| Edits to shared files | 4 | `CdcDispatcher`, `CdcConfiguration`, `CdcOptions`, `ServiceCollectionExtensions` |
| Tests | ~28 | unit, guard, contract, property, integration, benchmark |
| Docs, changelog, manifests | ~8 | README, feature page, fragment, manifest entries |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
PROJECT CONTEXT: Encina is a pre-1.0 .NET 10 / C# 14 library with nullable reference types. You are implementing
issue #636: stream CDC change events as structured audit entries to external sinks. Read AGENTS.md first.

IMPLEMENTATION OVERVIEW: Phase 1 adds CdcAuditEntry, ICdcAuditSink and options. Phase 2 adds ICdcChangeObserver and calls it
from CdcDispatcher for every event. Phase 3 adds the row projector, the allow-list redaction and AuditStreamObserver.
Phase 4 adds the console, file and outbox sinks (write-through). Phase 5 adds WithAuditStreaming, registration and
validation. Phase 6 proves the four connector shapes. Phase 7 adds metrics, spans and logs (EventIds 4880-4891).
Phase 8 tests; Phase 9 documents. Deliver as three pull requests after the CDC prerequisites land.

KEY PATTERNS: fail closed (allow list; infrastructure tables rejected); a Left from a sink fails the CDC event; no
buffering past the position save; never log row contents or EncinaError.Message; opt-in; TimeProvider; registration
completeness proven by a DI test with ValidateOnBuild and ValidateScopes.

REFERENCE FILES: src/Encina.Cdc/Processing/CdcDispatcher.cs, src/Encina.Cdc/CdcConfiguration.cs, src/Encina.Cdc/ChangeEvent.cs,
src/Encina.Messaging/Outbox/OutboxOrchestrator.cs, src/Encina.DomainModeling/Auditing/AuditLogEntry.cs,
.github/coverage-manifest/Encina.Cdc.json.
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Not applicable: a write-only stream |
| 2 | OpenTelemetry | ✅ | Phase 7: entry, error and redaction counters, spans |
| 3 | Structured Logging | ✅ | `CdcAuditLog`, EventIds 4880-4891; never entry contents |
| 4 | Health Checks | ⏭️ | Sink health (file path writable, outbox reachable) joins the processor liveness work of #2396; connector health is the existing `CdcHealthCheck` |
| 5 | Validation | ✅ | Options and table allow list validated at startup; event schema validation is #730 |
| 6 | Resilience | ✅ | Failed writes fail the event and follow CDC retry and dead letter (#2343, #2344); sinks add no hidden retry |
| 7 | Distributed Locks | ⏭️ | Two CDC consumers would write each entry twice; coordination is #1251, and the `DeduplicationKey` lets sinks deduplicate meanwhile |
| 8 | Transactions | ❌ | Not applicable: CDC observes committed transactions |
| 9 | Idempotency | ✅ | `DeduplicationKey` on every entry (connector id and position) |
| 10 | Multi-Tenancy | ⏭️ | Per-tenant sinks and a tenant field per table follow #1257 |
| 11 | Module Isolation | ❌ | Not applicable: infrastructure-level |
| 12 | Audit Trail | ✅ | This feature is the external audit stream; it complements the entity-change audit store of #1778 |

---

## Prerequisites & Dependencies

| Item | State | Effect |
|------|-------|--------|
| #2343 (a `Left` from the dispatcher is neither retried nor dead-lettered) | open | Required for "a sink failure fails the CDC operation (retry or dead letter)" |
| Dispatcher issue drafted with this plan (updates without a before image; several handlers per table) | to be opened | The observer needs no handler, but updates on SQL Server must not fail before the observer runs; Phase 2 orders the observer first |
| #1778 (entity-change audit store, ADR-036) | open | Design Choice 3 revisits the sink abstraction once it lands |
| #2861, #2862, #2839 (connector integration tests) | open | Fixtures for the connector-driven integration tests |
| #2393 (persistent position store) | plan | Adds `CdcPositions`, which the infrastructure table list rejects |

---

## Next Steps

1. Review and approve this plan; the maintainer answers the four Dilemmas (choices 1, 2, 3 and 4) when work on the issue starts.
2. Publish the plan link as a comment on #636.
3. Begin PR 1 (Phases 1-3) after #2343 lands.
4. Each phase is a self-contained commit; the final commit references `Fixes #636`.
