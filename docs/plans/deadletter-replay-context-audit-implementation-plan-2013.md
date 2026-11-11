# Implementation Plan: Dead Letter Replay Under the Stored Context and Audited Operator Actions

> **Issue**: [#2013](https://github.com/dlrivada/Encina/issues/2013)
> **Type**: Feature
> **Related**: #583 (persistent dead letter stores, closed), #1705 (one request identity model), #1164 (persisted request context, SPEC-002 REQ-015), #2012 (replay claim, closed), #3442 (inbox dead letter replay), #2229 (admin API, consumer of the audited actions), #135 (security violations call the audit port), #2239 and #2382 (module requirements), ADR-036, ADR-046, ADR-050, ADR-054
> **Complexity**: High (core contract, audit port and adapter, 10 provider schemas)
> **Estimated Scope**: ~1,300-1,800 lines of production code + ~3,500 lines of tests; above one worker pull request, split proposed in Phase 1

---

## Summary

### What exists today (read on 2026-11-11, main `68d08ddae`)

- `DeadLetterManager` (`src/Encina.Messaging/DeadLetter/DeadLetterManager.cs`) replays through `ReplayAsync` (`:172`), `ClaimAndReplayAsync` (`:221`), `ReplayStoredMessageAsync` (`:312`), `ReplayRequestAsync` (`:383`; deserialization at `:360`) and sends through `IEncina` under the operator's ambient context. Tenant scoping exists (`AmbientTenantId` `:100`, `DenyTenantRequired`), identity restoring does not.
- `IDeadLetterMessage` has `TenantId` and `CorrelationId`, and no `ActorId`, `CausationId` or kind. Capture is `DeadLetterOrchestrator` (`:132`, `:165`, `:208`; tenant from `_requestContextAccessor` `:300`) with `DeadLetterContext` (`:31`) and `DeadLetterData`.
- The restore API exists: `PersistedRequestIdentity(Kind, ActorId, TenantId, CorrelationId, CausationId)` (`src/Encina/Identity/PersistedRequestIdentity.cs:25`; `ToString()` hides the actor), `IRequestContextScopeFactory`, `RunRestoredAsync` (`RequestContextScopeFactoryExtensions.cs:101`), `PersistedIdentitySource` `Internal` and `External`, `IdentityKind` `Anonymous`, `User`, `Service`. `IRequestContext` carries `CausationId` and `Identity`.
- No audit exists for replay, replay-all, delete or delete-all (`:172`, `:430`, `:556`, `:601`). `IOperationAuditStore.RecordAsync` exists (`src/Encina.Security.Audit/Abstractions/IOperationAuditStore.cs`); `Encina.Security.Audit` references `Encina.Messaging`, never the reverse.
- The 10 providers each have `DeadLetter/` (message, factory, store) and `Scripts/029_CreateDeadLetterMessagesTable.sql` (`Encina.ADO.*`, `Encina.Dapper.*`), `Encina.EntityFrameworkCore/DeadLetter/` (`DeadLetterMessageConfiguration`) and `Encina.MongoDB/DeadLetter/`.
- Start-up requirement precedent: `DelayedRetryStartupCheck : IHostedService` (`src/Encina.Messaging/Recoverability/DelayedRetryStartupCheck.cs`); the generic validator is #2382 (open).

### What this plan delivers

Capture stores the identity (`ActorId`, `CausationId`, and the kind per Design Choice 3); replay restores it through `RunRestoredAsync` and rejects an unresolvable tenant; a small audit port records intent before and outcome after every operator replay and delete; the dead letter registration requires the port's adapter.

- Affected packages: `Encina.Messaging` (record, capture, manager, port, start-up check), `Encina.Security.Audit` (adapter), `Encina.ADO.{SqlServer,PostgreSQL,MySQL}`, `Encina.Dapper.{SqlServer,PostgreSQL,MySQL}`, `Encina.EntityFrameworkCore`, `Encina.MongoDB`, `Encina.Testing.Fakes`.
- Provider category: Database (10), `AGENTS.md` section 5.

---

## Design Choices

Choices 1 and 2 were decided by the maintainer (comments of 2026-10-09 and 2026-10-16 on #2013) and are recorded in `## Maintainer Decisions`.

<details>
<summary><strong>1. Audit placement, audit order, restored context, delete and unresolvable tenant</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Audit port in `Encina.Messaging`, adapter in `Encina.Security.Audit` over `IOperationAuditStore`; intent audited before dispatch (fail closed) and outcome after; original tenant, actor, correlation and cause restored; hard delete plus a payload-free audit entry; an unresolvable tenant rejects the replay with an error code** | Dependency direction kept; a failed intent write replays nothing; GDPR erasure honoured | The queue requires the adapter (start-up failure without it) |
| **B) Direct dependency on `IOperationAuditStore` from `Encina.Messaging`; soft delete; restore the tenant only** | Fewer types | Inverts the package dependency; keeps erased payloads; breaks causal tracing |

### Chosen Option: **A — Port and adapter, intent before outcome, full restore, hard delete** (decided by the maintainer, 2026-10-16)

### Rationale

- The queue requires the adapter under the module-requires-module rule of #2239 (ADR-050). The operator is recorded in the audit entry; the replay result and the audit carry outcome codes only (`AGENTS.md` section 3).

**Class:** Settled
**Settled by:** maintainer decision of 2026-10-16 on #2013, SPEC-002 REQ-015 and REQ-019, SPEC-002 DEC-006, ADR-036, ADR-050, GDPR Art. 17 and Art. 5(1)(e)

</details>

<details>
<summary><strong>2. Columns, privacy and schema change</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Nullable `ActorId` (max 256) and `CausationId` (max 256) added to the `029` create script of each provider; `ActorId` never in logs, tags or errors; deleted with the row by retention** | Pre-1.0 has no existing user, so one script per provider and no migration helper; matches the 2026-10-09 decision | Applications that already ran the script recreate the table (none exist) |
| **B) A separate `ALTER` script per provider** | Keeps the history of the table | A migration layer `AGENTS.md` section 1 forbids |

### Chosen Option: **A — Columns in the create script** (decided by the maintainer, 2026-10-09)

### Rationale

- `ActorId` is personal data when it is a person (SPEC-002 REQ-062); `PersistedRequestIdentity.ToString()` already hides it. Time in audit entries comes from `TimeProvider`; every database call is asynchronous with a `CancellationToken`.

**Class:** Settled
**Settled by:** maintainer decision of 2026-10-09 on #2013, AGENTS.md section 1 (pre-1.0, no migration support) and section 3, SPEC-002 REQ-062

</details>

<details>
<summary><strong>3. How the identity kind is stored</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A third nullable column `ActorKind` (short string)** | Exact, no inference; same shape as `PersistedRequestIdentity`; kinds can grow | A third column on 10 providers |
| **B) Derive the kind from `ActorId` (`service:` prefix of `RequestIdentity.ServiceSubjectPrefix`, null for anonymous)** | Only the two decided columns | Inference breaks if an id format changes; cannot represent a kind with no marker |

### Chosen Option: **A — `ActorKind` column** (recommended, pending the maintainer)

### Rationale

- `IdentityKind` has three values today; the 2026-10-09 decision deferred the kind until `PersistedRequestIdentity` merged, and it has.
- B wins if only user, service and anonymous will ever exist and a third column on 10 providers is judged not worth it.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. How a batch replay or delete is audited</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One entry (intent and outcome) per message, for both operations** | Complete trail per message | `DeleteAllAsync` has no bound; `DeadLetterFilter.All` could mean millions of entries |
| **B) One batch entry (filter, count, outcome) plus per-message entries only for failures** | Compact | A replayed message has no entry of its own |
| **C) Per message for replay (bounded by `maxMessages`, each dispatch has its own outcome), one batch entry for delete-all** | Replay trail complete; delete audit bounded | Two rules to document |

### Chosen Option: **C — Per message for replay, one batch entry for delete-all** (recommended, pending the maintainer)

### Rationale

- `ReplayAllAsync` is capped by `maxMessages` (default 100) and each message dispatches separately; `DeleteAllAsync` is a set-based statement whose ids are not loaded.
- A wins when auditors must reconstruct each deleted message and the volume is small; B wins for very large replays.

**Class:** Dilemma

</details>

<details>
<summary><strong>5. What "unresolvable tenant" means</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The stored tenant is missing, malformed or refused by the scope factory (`RunRestoredAsync` returns a refusal)** | No new dependency; `Encina.Messaging` stays free of tenancy | A tenant deleted since capture still passes |
| **B) Also ask a tenant-existence port that `Encina.Tenancy` implements (`ITenantProvider`)** | A deleted or disabled tenant is refused | A new port in Messaging and a Tenancy dependency in the registration |

### Chosen Option: **A — Refusal by the scope factory** (recommended, pending the maintainer)

### Rationale

- The scope factory already validates the persisted identity and external trust (`RequestContextScopeFactory.Restore.cs`), so no second check is needed to meet the decided rejection.
- B wins when tenants are created and removed at run time and a replay for a removed tenant must not run.

**Class:** Dilemma

</details>

<details>
<summary><strong>6. Is the tenant a telemetry dimension of replay and delete</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) No tenant tag or span attribute on the dead letter metrics (the issue's rewritten text)** | No tenant cardinality; no tenant in telemetry | Differs from ADR-054 D4 and SPEC-002 AC-044 |
| **B) Tenant tag and span attribute only when multi-tenancy is on (ADR-054 D4)** | One rule across packages; per-tenant replay failure rates | Cardinality risk (documented in the observability guide, #906) |

### Chosen Option: **B — Tenant tag only when multi-tenancy is on** (recommended, pending the maintainer)

### Rationale

- ADR-054 D4 applies the tag "in every package"; the actor is never a dimension in either option.
- A wins when tenant ids themselves are sensitive in the deployment or the tenant count makes the tag costly.

**Class:** Dilemma

</details>

---

## Implementation Phases

### Phase 1: Record, Capture, Port and Split

> **Goal**: Core contract changes in `Encina.Messaging` before any provider work.

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** `IDeadLetterMessage.cs`, `DeadLetterData` (`IDeadLetterMessageFactory.cs`) and `DeadLetterContext` (`DeadLetterOrchestrator.cs:31`): add `ActorId`, `CausationId` and, per Design Choice 3, `ActorKind`. The capture (`DeadLetterOrchestrator` and `DeadLetterSourceCapture`) fills them from `IRequestContextAccessor.RequestContext` (`Identity.ToPersisted(tenantId, correlationId, causationId)`); lengths validated in `DeadLetterInputs.cs`.
2. **Create** `DeadLetter/IDeadLetterAuditSink.cs` (`RecordReplayIntentAsync`, `RecordReplayOutcomeAsync`, `RecordDeleteAsync`, each `ValueTask<Either<EncinaError, Unit>>`) with input records carrying message id, source pattern, operator id, tenant, outcome code (never payload or message text).
3. **Modify** `DeadLetterErrorCodes.cs`: `dlq.tenant_unresolved`, `dlq.audit_failed`.
4. **Split**: S1 phases 1 to 3 (core, manager, port, start-up check); S2 adapter and fakes; S3-S5 ADO.NET, Dapper, EF Core plus MongoDB schema and mapping; S6 cross-provider tests; S7 observability and documentation. Each within 15 files and 1,500 lines.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
Encina (.NET 10, C# 14, pre-1.0, no backward compatibility). Issue #2013: replay must run under the stored identity
and operator actions must be audited. Apply the recorded answers in
docs/plans/deadletter-replay-context-audit-implementation-plan-2013.md.

TASK:
1. Add ActorId, CausationId (and ActorKind if Design Choice 3 is A) to IDeadLetterMessage, DeadLetterData and
   DeadLetterContext; fill them at capture from IRequestContextAccessor; validate lengths (256).
2. Create IDeadLetterAuditSink and its input records in src/Encina.Messaging/DeadLetter/.
3. Add the two error codes.

KEY RULES:
- ActorId never in logs, tags, errors or ToString(); EncinaError.Message never stored or logged.
- No [Obsolete]; change the contract completely; XML docs on every public member.

REFERENCE FILES:
- src/Encina.Messaging/DeadLetter/IDeadLetterMessage.cs, DeadLetterOrchestrator.cs, DeadLetterInputs.cs
- src/Encina/Identity/PersistedRequestIdentity.cs
```

</details>

---

### Phase 2: Manager Restore, Audit and Start-up Requirement

> **Goal**: Replay and delete behave as decided.

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** `DeadLetterManager.cs`: inject `IRequestContextScopeFactory` and `IDeadLetterAuditSink`; `ReplayStoredMessageAsync` (`:312`) builds `PersistedRequestIdentity` from the row (source `External` when `SourcePattern == Inbox`, else `Internal`; related defect #3442), records the intent (a `Left` stops the replay), runs `ReplayRequestAsync` inside `RunRestoredAsync`, records the outcome; `ReplayAllAsync` and `DeleteAsync`/`DeleteAllAsync` audit per Design Choice 4; a refusal returns `dlq.tenant_unresolved` and dispatches nothing.
2. **Create** `DeadLetter/DeadLetterAuditStartupCheck.cs` (`IHostedService`, like `DelayedRetryStartupCheck`): host start fails naming module, missing piece and ADR-050 when the queue is registered without an `IDeadLetterAuditSink`. When #2382 lands, the row moves into its validator.
3. **Modify** `MessagingServiceCollectionExtensions.AddDeadLetterQueueServices` to register the check and the options it needs.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
Phase 1 added the stored identity and the audit port. DeadLetterManager.ReplayStoredMessageAsync deserializes and
dispatches under the operator's context today. RunRestoredAsync is in
src/Encina/Identity/RequestContextScopeFactoryExtensions.cs.

TASK:
1. Restore the identity around dispatch; audit intent before (fail closed) and outcome after; audit delete.
2. Reject an unresolvable tenant with dlq.tenant_unresolved before any dispatch.
3. Add DeadLetterAuditStartupCheck and register it with the dead letter services.

KEY RULES:
- A Left from the audit sink or a store fails the operation, never reports success.
- Timestamps from TimeProvider; all database and sink calls take the CancellationToken.

REFERENCE FILES:
- src/Encina.Messaging/DeadLetter/DeadLetterManager.cs
- src/Encina.Messaging/Recoverability/DelayedRetryStartupCheck.cs
- src/Encina/Identity/RequestContextScopeFactory.Restore.cs
```

</details>

---

### Phase 3: Audit Adapter

> **Goal**: `IDeadLetterAuditSink` over `IOperationAuditStore`.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina.Security.Audit/DeadLetter/OperationAuditDeadLetterSink.cs`: maps each call to an `OperationAuditEntry` (`Action` `deadletter.replay.intent`, `deadletter.replay.outcome`, `deadletter.delete`; `EntityType` `DeadLetterMessage`; `EntityId` the message id; `UserId` the operator; `Metadata` of strings only; timestamps from `TimeProvider`).
2. **Modify** `AddEncinaAudit` to `TryAddScoped<IDeadLetterAuditSink, OperationAuditDeadLetterSink>()`; add a fake sink to `Encina.Testing.Fakes`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT:
Encina.Security.Audit references Encina.Messaging. IOperationAuditStore is implemented on the 10 providers.

TASK:
1. Create OperationAuditDeadLetterSink and map the three calls to OperationAuditEntry.
2. Register it with TryAdd in AddEncinaAudit; add a fake sink for tests.

KEY RULES:
- Entries carry ids and outcome codes only; no payload, no EncinaError.Message, no ActorId of the message owner.
- Registration proven by a DI test with ValidateOnBuild and ValidateScopes.

REFERENCE FILES:
- src/Encina.Security.Audit/Abstractions/IOperationAuditStore.cs, OperationAuditEntry.cs, ServiceCollectionExtensions.cs
```

</details>

---

### Phase 4: Provider Schemas and Stores (10 providers)

> **Goal**: The new columns persist and read on every provider.

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** per provider `DeadLetter/DeadLetterMessage.cs`, `DeadLetterMessageFactory.cs` and the store (`DeadLetterStoreADO`, `DeadLetterStoreDapper`, `DeadLetterStoreEF`, `DeadLetterStoreMongoDB`): insert and read the columns (`ActorId`, `CausationId`, `ActorKind`).
2. **Modify** the `029_CreateDeadLetterMessagesTable.sql` of the six SQL providers (nullable columns), `DeadLetterMessageConfiguration.cs` (EF Core, one package for three databases) and the Mongo document.
3. **Create** the cross-provider contract and integration cases (Phase 6).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT:
Each provider package has DeadLetter/ (message, factory, store) and a 029 script (SQL providers). SQL differences
(AGENTS.md section 5): SQL Server TOP and bit; PostgreSQL case-sensitive quoted identifiers; MySQL backticks.

TASK:
1. Add the three nullable columns to the entity, factory, script and store read/write of all 10 providers.
2. Keep identical observable behavior across providers (the contract tests run them side by side).

KEY RULES:
- Parameterized SQL; asynchronous calls with a CancellationToken; no ActorId in any log.

REFERENCE FILES:
- src/Encina.ADO.SqlServer/DeadLetter/DeadLetterStoreADO.cs, Scripts/029_CreateDeadLetterMessagesTable.sql
- src/Encina.EntityFrameworkCore/DeadLetter/, src/Encina.MongoDB/DeadLetter/
```

</details>

---

### Phase 5: Cross-Cutting Integration and Observability

> **Goal**: Metrics, span, logs.

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** `Diagnostics/DeadLetterMetrics.cs`: `encina.dlq.replay_duration` (histogram) and tag `status` values on the existing replay counter (`fail_tenant_unresolved`, `fail_audit`); the tenant tag follows Design Choice 6. Names follow the existing `encina.dlq.*` family and ADR-054; the issue's `encina.deadletter.*` proposals are renamed.
2. **Create** the span `encina.dlq.replay` (context restoration is an event inside it) and `encina.dlq.audit`.
3. **Modify** `DeadLetterLog.cs`/new log class: `[LoggerMessage]` EventIds 6670-6679 (register `MessagingDeadLetterReplay = (6670, 6689)` in `EventIdRanges.cs` first and map it to `Encina.Messaging` in `EncinaEventIdAllocationTests`). Error codes only.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT:
DeadLetterMetrics uses meter "Encina" and encina.dlq.*_total names. EventIds must be registered first (ADR-021).

TASK:
1. Register MessagingDeadLetterReplay (6670-6689), then add the logs, the metric tags and the two spans.

KEY RULES:
- Never ActorId, EncinaError.Message or payload in tags, spans or logs; exceptions via ForLogging().

REFERENCE FILES:
- src/Encina.Messaging/Diagnostics/DeadLetterMetrics.cs, src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

---

### Phase 6: Testing

> **Goal**: Every applicable flag reaches its manifest target.

<details>
<summary><strong>Tasks</strong></summary>

1. **Unit tests**: restore path, fail-closed branches (intent write fails, tenant unresolved), audit order, inbox as external, adapter mapping.
2. **Guard tests** for new public members; **contract tests** for `IDeadLetterAuditSink` and the new `IDeadLetterMessage` members over all 10 stores.
3. **Property tests** (FsCheck): capture-to-restore identity round trip.
4. **Integration tests** on the shared collection fixtures for all 10 providers: capture, restore, cross-tenant isolation, audit in the real `IOperationAuditStore`; DI test for the start-up requirement.
5. Load and benchmark: justification files (operator-initiated).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```text
CONTEXT:
Per-flag coverage (AGENTS.md section 9) for Encina.Messaging, Encina.Security.Audit and the 8 provider packages.

TASK:
1. Write the tests above with real implementations; measure per-flag coverage and CRAP with coverage-report.cs.

KEY RULES:
- Shouldly through Encina.Testing.Shouldly; shared [Collection] fixtures, never IClassFixture; tests cover all 10 providers.

REFERENCE FILES:
- tests/Encina.IntegrationTests (Collections.cs files), tests/Encina.ContractTests, .github/scripts/coverage-report.cs
```

</details>

---

### Phase 7: Documentation and Finalization

> **Goal**: Operators and integrators can rely on the replay and audit behavior.

<details>
<summary><strong>Tasks</strong></summary>

1. XML docs; `PublicAPI.Unshipped.txt` of `Encina.Messaging` and `Encina.Security.Audit`.
2. `changelog.d/2013-deadletter-replay-context.changed.md`; README sections; configuration guide for the adapter registration; `docs/INVENTORY.md`.
3. ADR for the restore and audit design (number assigned when the ADR pull request opens).
4. `dotnet build --configuration Release` with zero warnings; `dotnet test` with every flag at target.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```text
CONTEXT:
All code is implemented and tested. Documentation is written by the docs-writer agent following the encina-docs skill.

TASK:
1. Complete XML docs and PublicAPI files; write the changelog fragment, README sections and the ADR.

KEY RULES:
- English only; never edit CHANGELOG.md; no hand-typed coverage numbers.

REFERENCE FILES:
- changelog.d/README.md, .claude/skills/encina-docs/SKILL.md, docs/architecture/adr/046-persistent-dead-letter-queue.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | Use in this feature |
|--------|-----------|---------------------|
| SPEC-002 REQ-015, REQ-019, REQ-062, DEC-006, AC-044 | Persisted identity, fail-closed audit, personal data, tenant tag | Choices 1, 2, 6 |
| GDPR Art. 17, Art. 5(1)(e) | Erasure, storage limitation | Hard delete |
| ADR-036, ADR-046, ADR-050, ADR-054 | Audit stores, dead letter queue, module requirements, telemetry | Port, columns, check, names |
| #1164 and #1705 | Persisted request context, identity model | `RunRestoredAsync` |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `DeadLetterManager`, `DeadLetterOrchestrator`, `DeadLetterSourceCapture` | `src/Encina.Messaging/DeadLetter/` | Changed |
| `PersistedRequestIdentity`, `RunRestoredAsync` | `src/Encina/Identity/` | Restore |
| `IOperationAuditStore`, `OperationAuditEntry` | `src/Encina.Security.Audit/` | Adapter target |
| `DelayedRetryStartupCheck` | `src/Encina.Messaging/Recoverability/` | Start-up check pattern |
| Dead letter stores and scripts | `src/Encina.{ADO,Dapper}.*/DeadLetter/`, `Encina.EntityFrameworkCore/DeadLetter/`, `Encina.MongoDB/DeadLetter/` | Schema changes |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Messaging` replay and audit | 6670-6689 (new `MessagingDeadLetterReplay`, block assigned to this family) | 6670-6679 used; the adapter logs nothing (errors are returned as `Left`) |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| New production files | ~6 | Port, input records, start-up check, adapter, log, fake |
| Changed production files | ~45 | Contract, capture, manager, 10 provider stores, entities, factories, 6 scripts, registrations |
| Tests | ~35 | Unit, guard, contract, property, integration x 10, DI |
| Documentation | ~10 | READMEs, guide, ADR, INVENTORY, changelog |
| **Total** | **~96** | Seven pull requests (Phase 1 item 4) |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
You are implementing issue #2013 - dead letter replay under the stored identity and audited operator actions.

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library; pre-1.0, no backward compatibility, best solution always.
- Railway Oriented Programming: Either<EncinaError, T>; a Left from a store or sink fails the operation.
- Database category: 10 providers; SQLite is out (ADR-024). Package direction: Security.Audit references Messaging.

IMPLEMENTATION OVERVIEW:
1. Stored identity (ActorId, CausationId, kind) at capture; 2. manager restores via RunRestoredAsync, audits intent
then outcome, rejects an unresolvable tenant; 3. audit port in Messaging, adapter in Security.Audit, start-up check;
4. 10 provider schemas; 5. metrics, spans, EventIds 6670-6689; 6. tests; 7. docs, ADR.

KEY PATTERNS:
- Intent audited before dispatch (fail closed); delete is a hard delete with a payload-free entry.
- ActorId never in logs, tags or errors; TimeProvider; async calls with CancellationToken.

REFERENCE FILES:
- src/Encina.Messaging/DeadLetter/DeadLetterManager.cs
- src/Encina/Identity/RequestContextScopeFactoryExtensions.cs
- docs/plans/deadletter-replay-context-audit-implementation-plan-2013.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ N/A | Replay is an operator action on one row |
| 2 | OpenTelemetry | ✅ Phase 5 | Replay and audit spans, counters; the actor is never a dimension; tenant per Design Choice 6 |
| 3 | Structured Logging | ✅ Phase 5 | EventIds 6670-6679; codes only, `ForLogging()` for exceptions |
| 4 | Health Checks | ❌ N/A | Operator-initiated action; the existing dead letter health check is unchanged |
| 5 | Validation | ✅ Phases 1-2 | Stored identity lengths validated at capture and restore |
| 6 | Resilience | ❌ N/A | The replay keeps its timeout; no external call is added |
| 7 | Distributed Locks | ❌ N/A | The replay claim is already atomic (`TryClaimForReplayAsync`, #2012) |
| 8 | Transactions | ✅ Phase 2 | Intent before and outcome after; the audit store may be in another database, so no shared transaction is assumed; the outcome write failing after dispatch is logged and surfaced as `dlq.audit_failed` on the result |
| 9 | Idempotency | ✅ Phase 2 | Restored correlation and cause keep the idempotency key stable; inbox rows restore as external (#3442 related) |
| 10 | Multi-Tenancy | ✅ Phase 2 | The stored tenant is restored or the replay is rejected |
| 11 | Module Isolation | ⏭️ #747 | A stored `ModuleId`, if present, is restored under the module isolation issue |
| 12 | Audit Trail | ✅ Phases 2-3 | Operator replay and delete audited through the port and adapter |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | Status | Notes |
|-------------|--------|-------|
| Persistent dead letter stores (#583) | ✅ Closed | Table and stores exist; its plan left these columns here |
| Replay claim (#2012) | ✅ Closed | Atomic claim |
| `PersistedRequestIdentity`, `RunRestoredAsync` | ✅ Exists | #1705 stays open for later phases |

### Recommended (Not Blocking)

| Dependency | Issue | Notes |
|-----------|-------|-------|
| Module requirement validator | #2382 | The start-up check moves into it when it lands |
| Inbox dead letter replay defect | #3442 | Same external-source rule |
| Admin API | #2229 | Calls the manager, so it inherits restore and audit |

---

## Next Steps

1. Link this plan from #2013 and ask the maintainer to answer Design Choices 3 to 6 when work starts.
2. Implement Phases 1 to 7 as the pull requests of Phase 1 item 4, `Fixes #2013` on the last.

---

## Maintainer Decisions

1. (2026-10-16) Audit placement, order, restored context, delete and unresolvable tenant (Design Choice 1): a small audit port in `Encina.Messaging` implemented by an adapter in `Encina.Security.Audit` on `IOperationAuditStore` (ADR-036); the dead letter queue requires the adapter and registering without it fails at start-up (rule decided on #2239); intent audited before dispatch (fail closed, SPEC-002 REQ-019) and outcome after; hard delete plus an audit entry without payload; original tenant, actor and correlation restored and the operator recorded in the audit entry; an unresolvable tenant rejects the replay with an error code.
2. (2026-10-09) Columns and privacy (Design Choice 2): nullable `ActorId` (max 256) and `CausationId` (max 256) on all 10 providers, filled from the request context at capture; `ActorId` never in logs or telemetry and deleted with the row by the DLQ retention; no `Kind` column until `PersistedRequestIdentity` merged (Design Choice 3 reopens it).
