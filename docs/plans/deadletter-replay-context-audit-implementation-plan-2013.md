# Implementation Plan: Dead Letter Replay Under the Stored Context and Audited Operator Actions

> **Issue**: [#2013](https://github.com/dlrivada/Encina/issues/2013)
> **Type**: Feature
> **Related**: #583 (persistent dead letter stores, closed), #1705 (one request identity model), #1164 (persisted request context, SPEC-002 REQ-015), #2012 (replay claim, closed), #3442 (inbox dead letter replay), #2229 (admin API, consumer of the audited actions), #135 (security violations call the audit port: this plan defines the member `RecordQuarantineAsync` that #135 needs), #2239 and #2382 (module requirements; #2382 also closes the in-memory audit store gap), ADR-036, ADR-046, ADR-050, ADR-054, ADR-062 (merged: error category contract)
> **Complexity**: High (core contract, audit port and adapter, 10 provider schemas)
> **Estimated Scope**: ~1,300-1,800 lines of production code + ~3,500 lines of tests; fifteen pull requests of at most 15 files and 1,500 lines each (table in Research, compile order in Phase 1)

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
| **A) No tenant tag or span attribute on the dead letter metrics (the issue's rewritten text)** | No tenant cardinality; no tenant in telemetry | Contradicts ADR-054 D4, which rejects option 4a, and SPEC-002 AC-044 |
| **B) Tenant tag and span attribute only when multi-tenancy is on (ADR-054 D4)** | One rule across packages; per-tenant replay failure rates | Cardinality risk (documented in the observability guide, #906) |

### Chosen Option: **B — Tenant tag only when multi-tenancy is on**

### Rationale

- ADR-054 D4 (accepted 2026-10-22): "when multi-tenancy is enabled, the tenant id is a span attribute and a metric tag in every package", the rule of SPEC-002 AC-044; the ADR rejects option 4a. Option A therefore does not comply. The actor is never a dimension.

**Class:** Settled
**Settled by:** ADR-054 D4 and SPEC-002 AC-044

</details>

<details>
<summary><strong>7. How a queue without the audit adapter is detected</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `DeadLetterManager` takes `IDeadLetterAuditSink` as a constructor dependency (a provider built with `ValidateOnBuild` fails), and a start-up check resolves it at host start so a host without `ValidateOnBuild` also fails before the first replay** | The failure is early and names the missing piece; no default sink exists to satisfy it by accident | Every existing test that enables the queue registers a sink first (the test waves T1 and T2 of the split) |
| **B) The dead letter registration adds a fail-closed default sink that denies every call, and the start-up check fails when that default is the one resolved** | No test wave; the container always builds | A resolved service that only denies; a second place that must stay in step with the check |

### Chosen Option: **A — Constructor dependency plus start-up check** (recommended, pending the maintainer)

### Rationale

- Decision 1 of 2026-10-16 says registering without the adapter fails at start-up (rule decided on #2239); both options end in a start-up failure, so the choice is where the failure comes from. A keeps the failure in the container.
- B wins when the test wave of A is judged too costly for the same outcome.

**Class:** Dilemma

</details>

<details>
<summary><strong>8. Audit order of a delete</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One entry (`RecordDeleteAsync`) written before the hard delete; a `Left` stops the delete; the same for the batch entry of `DeleteAllAsync` under Design Choice 4 option C** | Fail closed (SPEC-002 REQ-019): a failed audit write deletes nothing | The entry records the attempt; a delete that then fails leaves an entry without a deletion, visible in the result and in `status` of the metric |
| **B) Intent before and outcome after, as replay does (two entries)** | Same shape as replay; the trail says whether the delete happened | `RecordDeleteAsync` becomes two members; a delete whose outcome write fails has an unmatched intent |

### Chosen Option: **A — One entry written before the delete** (recommended, pending the maintainer)

### Rationale

- Both options write before the destructive step, so both fail closed; the difference is whether the outcome is also recorded. The issue and decision 1 ask only for a payload-free entry.
- B wins when auditors must tell attempted deletes from completed ones.

**Class:** Dilemma

</details>

<details>
<summary><strong>9. Result when the outcome audit fails after a successful dispatch</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Right(ReplayResult)` that carries the code `dlq.audit_failed`; a log by code, the metric `status=fail_audit` and an `Outcome` the caller can read** | The message was dispatched and is marked replayed, and the result says so; an operator retry cannot replay twice | A caller that only checks `IsRight` misses the audit failure |
| **B) `Left(dlq.audit_failed)`** | The operator sees a failure and the audit gap cannot be missed | Reports failure for a message that was in fact replayed and marked replayed; a retry returns `dlq.already_replayed` |

### Chosen Option: **A — Success result carrying the audit failure code** (recommended, pending the maintainer)

### Rationale

- `ReplayResult` already exists (`IDeadLetterManager.cs:127`); the code (category per ADR-062: `ErrorCategory.Internal`) is data on it. The intent entry was written (fail closed), so only the outcome is missing.
- B wins when an unaudited outcome must be treated as a failed operation.

**Class:** Dilemma

</details>

<details>
<summary><strong>10. Capture when ActorId or CausationId exceeds 256 characters</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Store `null` for the over-long value, keep the dead letter and log the code `dlq.identity_too_long`; replay restores what is stored (an absent actor restores as `Anonymous`)** | The failed message is never lost; no truncated identifier that points at someone else | A replay of that row runs without the original actor |
| **B) Truncate to 256** | The row keeps an identifier | A truncated actor id can match another actor |
| **C) Reject the capture** | No wrong identity stored | The failed message is lost (a capture failure leaves it un-terminated, ADR-046 decision 13) |

### Chosen Option: **A — Null plus a log by code** (recommended, pending the maintainer)

### Rationale

- C loses data and B stores a misleading value; A is the only option that keeps the message without a wrong identity. B or C win only if an identity must always be present.

**Class:** Dilemma

</details>

### Register of audit semantics

| Id | Choice | Class | Basis |
|----|--------|-------|-------|
| A1 | Audit entry mapping: `TenantId` is the stored tenant of the message (the data owner), `CorrelationId` is the operator's current correlation id (the entry is required to have one) and the stored correlation id goes in `Metadata`; `Outcome` is the call's `AuditOutcome`; `ErrorMessage` stays null | Dilemma (recommended, pending the maintainer) | the operator-versus-message choice for `TenantId` and `CorrelationId`; `ErrorMessage` null is Settled by AGENTS.md section 3 |
| A2 | `DeadLetterAuditStartupCheck` and the constructor dependency: where the failure comes from | Dilemma | Design Choice 7 |
| A3 | Port members: `RecordReplayIntentAsync`, `RecordReplayOutcomeAsync`, `RecordDeleteAsync`, `RecordQuarantineAsync` (the last one for #135) | Trivial | one member per audited operation, as ADR-036 audits operations |
| A4 | The audit entries carry ids and outcome codes only, never payload or message text | Settled | AGENTS.md section 3; SPEC-002 REQ-062 |

---

## Implementation Phases

### Phase 1: Record, Capture, Port and Split

> **Goal**: Core contract changes in `Encina.Messaging` before any provider work.

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** `IDeadLetterMessage.cs`, `DeadLetterData` (`IDeadLetterMessageFactory.cs`) and `DeadLetterContext` (`DeadLetterOrchestrator.cs:31`): add `ActorId`, `CausationId` and, per Design Choice 3, `ActorKind`. The capture (`DeadLetterOrchestrator` and `DeadLetterSourceCapture`) fills them from `IRequestContextAccessor.RequestContext` (`Identity.ToPersisted(tenantId, correlationId, causationId)`); an `ActorId` or `CausationId` above 256 characters is stored as `null` and logged by code (Design Choice 10), never rejected. In the compile order this task is split: the capture side and `DeadLetterData` in P1, the interface members in P6 (Phase 1 task 4).
2. **Create** `DeadLetter/IDeadLetterAuditSink.cs` with four members, each `ValueTask<Either<EncinaError, Unit>>`: `RecordReplayIntentAsync`, `RecordReplayOutcomeAsync`, `RecordDeleteAsync` and `RecordQuarantineAsync` (the member the plan for #135 calls for a security violation; input record `DeadLetterQuarantineAuditInput`: message id, source pattern, tenant, poison class, error code). Input records carry message id, source pattern, operator id (replay and delete), tenant, outcome code, never payload or message text. The member list of this plan and of the plan for #135 is the same.
3. **Modify** `DeadLetterErrorCodes.cs`: `dlq.tenant_unresolved` (`ErrorCategory.Forbidden`), `dlq.audit_failed` (`Internal`), `dlq.identity_too_long` (`InvalidRequest`, Design Choice 10); created with the ADR-062 category overload of `EncinaErrors.Create`.
4. **Split and compile order**: the table in Research lists every pull request with its files and lines. Because `IDeadLetterMessage` has eight provider implementers (ADO x3, Dapper x3, EF Core, MongoDB) plus `FakeDeadLetterMessage`, the order is: P1 capture side (`DeadLetterData` gains trailing optional members, so existing factories compile); P2-P5 each provider adds the properties, columns and mapping to its entity, factory, store and script (plain properties, not yet on the interface); P6 adds the members to `IDeadLetterMessage` and the fake, which every implementer already satisfies; then the port (P7a), the adapter (P7b), the test waves that register a sink (T1, T2), the manager (P8), the start-up check (P9), observability (P10), integration tests (P11) and documentation (P12). No pull request uses a type created by a later one.

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

1. **Modify** `DeadLetterManager.cs`: inject `IRequestContextScopeFactory` and `IDeadLetterAuditSink`; `ReplayStoredMessageAsync` (`:312`) builds `PersistedRequestIdentity` from the row (source `External` when `SourcePattern == Inbox`, else `Internal`; related defect #3442), records the intent (a `Left` stops the replay), runs `ReplayRequestAsync` inside `RunRestoredAsync`, records the outcome; `ReplayAllAsync` and `DeleteAsync`/`DeleteAllAsync` audit per Design Choices 4 and 8 (the delete entry, and the batch entry of `DeleteAllAsync`, are written before the hard delete; a `Left` deletes nothing); an outcome-write failure after dispatch follows Design Choice 9; a refusal returns `dlq.tenant_unresolved` and dispatches nothing. The audit entry mapping is in the register below.
2. **Create** `DeadLetter/DeadLetterAuditStartupCheck.cs` (`IHostedService`, like `DelayedRetryStartupCheck`): it resolves `IDeadLetterAuditSink` at host start and fails naming module, missing piece and ADR-050 when the queue is registered without one (Design Choice 7). The DI proof has two tests: a provider built with `ValidateOnBuild` and `ValidateScopes` fails to build because of the `IDeadLetterAuditSink` constructor dependency of `DeadLetterManager`, and, separately, the host start fails from the check when the provider was built without validation. When #2382 lands, the row moves into its validator.
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

1. **Create** `src/Encina.Security.Audit/DeadLetter/OperationAuditDeadLetterSink.cs`: maps each call to an `OperationAuditEntry` (`Action` `deadletter.replay.intent`, `deadletter.replay.outcome`, `deadletter.delete`, `deadletter.quarantine`; `EntityType` `DeadLetterMessage`; `EntityId` the message id; `UserId` the operator (absent for a quarantine); `Outcome` the `AuditOutcome` of the call; `ErrorMessage` always null, because `EncinaError.Message` never reaches plaintext storage (AGENTS.md section 3); `Metadata` of strings only; timestamps from `TimeProvider`). `TenantId` and `CorrelationId` follow the register row A1.
2. **Modify** `AddEncinaAudit` to `TryAddScoped<IDeadLetterAuditSink, OperationAuditDeadLetterSink>()`; add a fake sink to `Encina.Testing.Fakes` (P7a).
3. Durability gap, stated: `AddEncinaAudit` registers `TryAddSingleton<IOperationAuditStore, InMemoryOperationAuditStore>()` (`src/Encina.Security.Audit/ServiceCollectionExtensions.cs:96`), so the adapter and the start-up check are satisfied in production by a non-durable store and the fail-closed audit is not durable. This plan does not close the gap: #2382 (open, verified with `gh api`) rejects test-only implementations outside Development and is the follow-up that closes it.

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
3. **Create** a new log class `DeadLetterReplayLog.cs` (P10, the first pull request that logs): ten `[LoggerMessage]` EventIds 6660-6669 packed sequentially (replay started, succeeded, failed, tenant unresolved, intent audit failed, outcome audit failed, delete audited, delete failed, identity too long, start-up check failed); register `MessagingDeadLetterReplay = (6660, 6669)` in `EventIdRanges.cs` first, adjacent to #135's `(6650, 6659)` inside the block 6650-6689 that the registry assigns to #135 and #2013, and map it to `Encina.Messaging` in `EncinaEventIdAllocationTests`. Error codes only.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT:
DeadLetterMetrics uses meter "Encina" and encina.dlq.*_total names. EventIds must be registered first (ADR-021).

TASK:
1. Register MessagingDeadLetterReplay (6660-6669), then add the logs, the metric tags and the two spans.

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

Tests and per-file manifest targets (one sentence of justification each) ship in the pull request of the code they cover; the table in Research lists them per pull request. Only the cross-provider integration tests are their own pull request (P11).

1. **Unit tests**: restore path, fail-closed branches (intent write fails, tenant unresolved, delete audit fails deletes nothing), audit order, inbox as external, adapter mapping, outcome-audit failure result.
2. **Guard tests** for new public members; **contract tests** for `IDeadLetterAuditSink` (P7a) and the new `IDeadLetterMessage` members over all 10 stores (P6).
3. **Property tests** (FsCheck): capture-to-restore identity round trip (P1).
4. **Integration tests** (P11) on the shared collection fixtures for all 10 providers: capture, restore, cross-tenant isolation, audit in the real `IOperationAuditStore`; the DI tests of the start-up requirement are in P9.
5. Load and benchmark: justification files (operator-initiated); a contract-test justification is never allowed.

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
| `Encina.Messaging` replay and audit | 6660-6669 (new `MessagingDeadLetterReplay`, inside the block 6650-6689 of #135 and #2013, adjacent to #135's 6650-6659) | 6660-6669 all used (ten messages, Phase 5); the adapter logs nothing (errors are returned as `Left`) |

### Estimated File Count

Every count includes tests, coverage manifest edits, PublicAPI files and changelog. Provider pull requests carry no PublicAPI line (the provider `DeadLetterMessage` classes are not listed in their PublicAPI files, verified with `git grep`). The existing test callers of the changed signatures are in the Edited tests column: the seven files that call `new DeadLetterManager(` (P8) and the up to 23 other test files that enable the queue and must register the sink (waves T1 and T2, from `git grep -l "AddDeadLetterQueueServices\|UseDeadLetterQueue" -- tests`).

| PR | Content | New src | Changed src | New tests | Edited tests | Manifest | Other | Files | Lines (est.) |
|----|---------|---------|-------------|-----------|--------------|----------|-------|-------|--------------|
| P1 | Capture side: `DeadLetterData` trailing members, orchestrator and capture fill them, length rule | 0 | 5 | 2 (capture unit, property round trip) | 1 | 1 | 0 | 9 | ~700 |
| P2 | ADO.SqlServer, ADO.PostgreSQL: entity, factory, store, script | 0 | 8 | 0 | 2 (store integration tests) | 2 | 0 | 12 | ~700 |
| P3 | ADO.MySQL, Dapper.SqlServer | 0 | 8 | 0 | 2 | 2 | 0 | 12 | ~700 |
| P4 | Dapper.PostgreSQL, Dapper.MySQL | 0 | 8 | 0 | 2 | 2 | 0 | 12 | ~700 |
| P5 | EF Core (entity, factory, store, configuration), MongoDB (entity, factory, store) | 0 | 7 | 0 | 4 (3 EF, 1 Mongo) | 2 | 0 | 13 | ~800 |
| P6 | `IDeadLetterMessage` members, `FakeDeadLetterMessage`, `FakeDeadLetterStore`, contract cases over the 10 stores | 0 | 5 (interface, two fakes, 2 PublicAPI) | 0 | 3 (`DeadLetterOrchestratorTests` double, two contract files) | 2 | 0 | 10 | ~500 |
| P7a | Port `IDeadLetterAuditSink` and input records, error codes, `FakeDeadLetterAuditSink` | 3 | 3 (`DeadLetterErrorCodes.cs`, 2 PublicAPI) | 2 (contract, guard) | 0 | 2 | 0 | 10 | ~600 |
| P7b | Adapter `OperationAuditDeadLetterSink`, `AddEncinaAudit` registration | 1 | 2 | 3 (mapping, DI, guard) | 0 | 1 | 0 | 7 | ~500 |
| T1 | Unit tests that enable the queue register `FakeDeadLetterAuditSink` | 0 | 0 | 0 | 15 | 0 | 0 | 15 | ~300 |
| T2 | Integration tests that enable the queue register it | 0 | 0 | 0 | 8 | 0 | 0 | 8 | ~200 |
| P8 | `DeadLetterManager`: restore, audit, tenant refusal; constructor change | 0 | 2 | 2 (restore and audit unit tests) | 7 (the `new DeadLetterManager(` callers) | 1 | 0 | 12 | ~1,000 |
| P9 | `DeadLetterAuditStartupCheck`, registration, DI tests | 1 | 2 | 2 (check, DI) | 0 | 1 | 0 | 6 | ~400 |
| P10 | Metrics, spans, log class and EventIds | 1 | 5 | 1 | 2 | 2 | 0 | 11 | ~700 |
| P11 | Integration tests on the 10 providers; load and benchmark justifications | 0 | 0 | 12 (10 integration, 2 `.md`) | 0 | 0 | 0 | 12 | ~1,200 |
| P12 | Documentation | 0 | 0 | 0 | 0 | 0 | 7 (READMEs of `Encina.Messaging` and `Encina.Security.Audit`, configuration guide, `docs/INVENTORY.md`, ADR, ADR index, `changelog.d/2013-deadletter-replay-context.changed.md`) | 7 | ~400 |
| **Total** | | **6** | **55** | **24** | **46** | **18** | **7** | **156** | **~9,400** |

The columns sum to the total (6 + 55 + 24 + 46 + 18 + 7 = 156) and every statement of the total in this plan is 156 files in fifteen pull requests. The largest is 15 files and none exceeds 1,500 lines. The earlier "~96 files, seven pull requests" is superseded. The edited-test counts of T1 and T2 are upper bounds from the grep; a file that needs no change drops out of the wave.

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
4. 10 provider schemas; 5. metrics, spans, EventIds 6660-6669; 6. tests in the pull request of the code; 7. docs, ADR. Fifteen pull requests, see the table.

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
| 3 | Structured Logging | ✅ Phase 5 | EventIds 6660-6669; codes only, `ForLogging()` for exceptions |
| 4 | Health Checks | ❌ N/A | Operator-initiated action; the existing dead letter health check is unchanged |
| 5 | Validation | ✅ Phases 1-2 | Stored identity lengths validated at capture and restore |
| 6 | Resilience | ❌ N/A | The replay keeps its timeout; no external call is added |
| 7 | Distributed Locks | ❌ N/A | The replay claim is already atomic (`TryClaimForReplayAsync`, #2012) |
| 8 | Transactions | ✅ Phase 2 | Intent before and outcome after; the audit store may be in another database, so no shared transaction is assumed; an outcome write failing after dispatch follows Design Choice 9 (recommended: a `Right` result carrying `dlq.audit_failed`, pending the maintainer) |
| 9 | Idempotency | ✅ Phase 2 | Restored correlation and cause keep the idempotency key stable; inbox rows restore as external (#3442 related) |
| 10 | Multi-Tenancy | ✅ Phase 2 | The stored tenant is restored or the replay is rejected |
| 11 | Module Isolation | ⏭️ #747 | A stored `ModuleId`, if present, is restored under the module isolation issue |
| 12 | Audit Trail | ✅ Phases 2-3 | Operator replay and delete audited through the port and adapter; the default in-memory `IOperationAuditStore` makes the audit non-durable until #2382 lands (stated gap, Phase 3) |

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

1. Link this plan from #2013 and ask the maintainer to answer Design Choices 3, 4, 5 and 7 to 10 and register row A1 when work starts (Design Choice 6 is Settled by ADR-054 D4).
2. Implement P1 to P12 as the pull requests of the Estimated File Count table, `Fixes #2013` on P12. The plan for #135 depends on P7a and P7b (the port and adapter).

---

## Maintainer Decisions

1. (2026-10-16) Audit placement, order, restored context, delete and unresolvable tenant (Design Choice 1): a small audit port in `Encina.Messaging` implemented by an adapter in `Encina.Security.Audit` on `IOperationAuditStore` (ADR-036); the dead letter queue requires the adapter and registering without it fails at start-up (rule decided on #2239); intent audited before dispatch (fail closed, SPEC-002 REQ-019) and outcome after; hard delete plus an audit entry without payload; original tenant, actor and correlation restored and the operator recorded in the audit entry; an unresolvable tenant rejects the replay with an error code.
2. (2026-10-09) Columns and privacy (Design Choice 2): nullable `ActorId` (max 256) and `CausationId` (max 256) on all 10 providers, filled from the request context at capture; `ActorId` never in logs or telemetry and deleted with the row by the DLQ retention; no `Kind` column until `PersistedRequestIdentity` merged (Design Choice 3 reopens it).
