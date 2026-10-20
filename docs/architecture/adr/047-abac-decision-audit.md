# ADR-047: ABAC Decisions Are Audited Write-Ahead at the Enforcement Point, Fail-Closed

## Status

**Accepted** - decided by the maintainer on 2026-10-03 (fail-closed default, tenant gate) and 2026-10-09 (design choices D1 to D7 and refinements A1 to A9 of issue #751). Implemented by #751 (phases 1 to 7, documentation in phase 8), with unit, guard, contract, property and integration tests; there is no benchmark or load test of the decision audit yet (to be done in #2339). The record layout, options, error codes and health states are described in the [decision audit reference](../../features/abac/reference/decision-audit.md); the phase-by-phase work is in the [implementation plan](../../plans/abac-decision-audit-implementation-plan-751.md).

## Context

Before #751 the only audit story of `Encina.Security.ABAC` was a user-written `IObligationHandler` for an `audit-access` obligation, and `PersistentPolicyAdministrationPoint` audited policy *changes*, never the access decisions themselves. A regulated application (SPEC-002, the reference psychology-practice application handles health data) must be able to answer "who asked for what, which policies decided, and was access granted", and the answer must not depend on the operation audit store being healthy at that moment.

Several facts of the existing code constrain the design:

- Only the Policy Enforcement Point (`ABACPipelineBehavior`) knows what was enforced: it overrides a Permit to Deny when a mandatory obligation fails, relaxes definite denials in Warn mode, and denies on Indeterminate or on exceptions.
- `IOperationAuditStore` (ADR-036) is the general write sink and already names the planned ABAC decision audit as one of its writers. The EF Core implementation saves through the request's `DbContext`, and the transaction behavior rolls back on `Left`, so a record written inside the request scope would vanish exactly when a request is denied.
- The reference NIS2 and policy-administration writers are fire-and-forget. AGENTS.md section 3 forbids swallowing errors in background infrastructure and requires compliance and security gates to fail closed.
- `ABACDecisionAuditSchema` has to fit the bounded columns of `OperationAuditEntry` on 10 database providers plus Marten, and the audit trail itself holds access history, which is personal data in the reference application.

## Decision

1. **Interception inside the enforcement point.** `ABACPipelineBehavior.Handle` is split into decide, record and enforce. The decision step never calls the next behavior; the record step runs only when decision audit is enabled; the enforce step then proceeds or returns the denial. This also fixes the misreport of a handler exception as `abac.evaluation_failed`, because the handler call moves outside the evaluation `try`. Decisions made by callers that use `IPolicyDecisionPoint` directly are not audited.
2. **No new store: reuse the operation audit store.** One `OperationAuditEntry` per decision goes to `IOperationAuditStore` (ADR-036 decision 2). `Action` is the constant `ABACDecision`, `EntityType` the request type name (the XACML action), `EntityId` the resource id, `UserId` the subject, `ErrorMessage` the reason code only, and `Metadata` string values only. There is no `IAbacAuditStore`. The mapper is `ABACDecisionAuditEntryMapper`; the constants are in `ABACDecisionAuditSchema`.
3. **What a record carries.** The enforced outcome (Granted, Denied, or DeniedNotEnforced, the last recorded as `Success` with `abac.enforced=false`), and the evaluation trace of the policies, carried *inside* the one record (capped by `MaxTraceEntries`), not one row per policy. Attributes are recorded by name only; values only for names listed in `RecordedAttributeValues` (empty by default). The built-in subject attributes `subject-id` and `identity-kind` are excluded from the name list, and the identity kind is stored as the metadata key `abac.identity_kind`. Never stored: `DecisionStatus.StatusMessage`, any `EncinaError.Message`, request or response payloads. Three timestamps: `StartedAtUtc` at decision start; `CompletedAtUtc` and `TimestampUtc` from one `TimeProvider` read at the end. Every value that exceeds a column limit is hashed, dropped or truncated by a per-column rule, so a long value can never fail the insert and deny access; the markers say which fields changed.
4. **Awaited write in an isolated scope, fail-closed by default.** The default recorder, `AuditStoreABACDecisionRecorder` (a singleton), opens its own DI scope, suppresses the ambient transaction, resolves `IOperationAuditStore` and awaits `RecordAsync`, before the protected handler runs (write-ahead). `ABACDecisionAuditFailureMode.FailClosed` is the default: when the request would proceed and the write fails, the request is denied with `abac.decision_audit_failed`. `BestEffort` is the explicit opt-out; it proceeds, logs the failure, and a startup warning makes the choice visible (SPEC-002 DEC-006). The write is bounded by `WriteTimeout` (default 5 seconds) through a `WaitAsync` race as well as a token, because a store that ignores its token would otherwise hang a fail-closed request. The write is never linked to the client's cancellation token, so a disconnect cannot erase the evidence of a denied attempt. The entry `Id` is the decision id, which makes an ambiguous failure checkable: the recorder looks for the entry before denying.
5. **Configuration is nested and off by default.** `ABACOptions.DecisionAudit` (`ABACDecisionAuditOptions`) holds `Enabled`, `Outcomes`, `FailureMode`, `IncludeEvaluationTrace`, `MaxTraceEntries`, `ResourceIdAttributeName`, `RecordedAttributeValues`, `WriteTimeout`, `HealthFailureWindow` and `AllowCrossTenantQueries`; `ABACOptions.AuditDecisions(...)` is the shortcut that enables it. The pipeline gates on the resolved options at runtime, so registration never depends on `Enabled` and nothing registered by `AddEncinaABAC` has a constructor dependency on `IOperationAuditStore`; an application with audit disabled and no operation audit store passes `ValidateOnBuild`. `ABACOptionsValidator` rejects `Enabled` together with `EnforcementMode.Disabled` and invalid bounds at startup, and the hosted startup check `ABACDecisionAuditStartupCheck` fails startup when decision audit is enabled and no operation audit store is registered. There is no per-request-type warning and no pipeline-order warning (the order is documented and pinned by a DI test; the contract is #2184, whose ADR number 049 is reserved).
6. **Resource identity is explicit.** `EntityId` comes only from `IABACResourceIdentity` or the resource attribute named by `ResourceIdAttributeName`. There is no `AuditRequestConventions` and no guess from an `Id` suffix. A resource that declares neither leaves `EntityId` empty, and the documentation says how to declare it. ABAC rows and `AuditPipelineBehavior` rows join by `CorrelationId` only.
7. **A typed reader and a JSON Lines export.** `IABACDecisionAuditReader` queries with server-side filters and exports `ABACDecisionAuditRecord` as JSON Lines (schema `encina.abac.decision/1`, codes only). The reader forces the ambient tenant when there is one; when multi-tenancy is enabled and no tenant can be resolved it returns a `Left` instead of querying across tenants; `AllowCrossTenantQueries` is the explicit, logged opt-out for operator tooling. Error codes come in three families: tenant denials under `encina.authorization.*` (`abac_audit_tenant_required`, `abac_audit_tenant_mismatch`, answered 403), query validation under `validation.*` (`validation.abac_decision_audit_query_invalid`, 400), and server-side failures under `abac.*` (`abac.decision_audit_failed`, `abac.decision_audit_store_unavailable`, `abac.decision_audit_record_unreadable`, `abac.decision_audit_export_incomplete`). Reasons come from the `ABACErrors.*Code` constants, never from literals. The reader is a plain service; the application must put it behind its own authorization.
8. **Service identities.** A `service:<name>` identity is stored in clear in the audit columns: a service name describes a function, never a person, and the actor kind is explicit in `abac.identity_kind`. A user id keeps the personal-data regime, and the whole row stays health-related data for retention and access purposes.

### Reuse

The decision is deliberately reusable. The saga audit (#750), the sharding and tenancy audit (#752) and the break-the-glass access record (SPEC-002 REQ-051) can take the same shape: write-ahead through `IOperationAuditStore` with a distinct `Action` and `Metadata` schema, a fail-closed default with an explicit logged opt-out, a bounded write in an isolated scope, and a typed reader with the same tenant gate.

## Rationale

- The audit must describe what was enforced, and only the enforcement point knows it; a decorator around the PDP would record `Success` for a Permit that the enforcement point later overrides.
- An awaited, fail-closed write is the only option that satisfies both "errors are never swallowed" and "gates fail closed" (AGENTS.md section 3), and it is write-ahead: no handler runs without the evidence of its authorization.
- Reusing the operation audit store costs no provider work and keeps the ten database providers coherent (AGENTS.md section 5); the cost, policy ids and the trace living in `Metadata` rather than in queryable columns, is accepted.
- Names, not values, keep personal data out of the audit trail by default and make a value an opt-in decision per attribute.

## Alternatives rejected

- **A decorator around `IPolicyDecisionPoint`:** rejected; it records the PDP's answer, not the enforced outcome, records nothing when attribute collection fails, and cannot see the request context.
- **A notification persisted by a subscriber:** rejected for the first cut; `Publish` with zero handlers returns `Right`, which is a silent fail-open, and every application handler would become part of the gate.
- **A separate audit pipeline behavior:** rejected; it cannot see the `PolicyDecision` and depends on registration order.
- **A new `IAbacAuditStore`:** rejected; it needs a store, schema, scripts and tests on all 10 providers for columns that only a later `ModuleId` feature would justify.
- **Fire-and-forget write (the policy-administration precedent):** rejected; it swallows errors, risks a disposed `DbContext`, and lets access proceed without evidence.
- **A buffered channel with a background writer:** deferred; it is not write-ahead and has a loss window, so it is only conceivable as an option inside `BestEffort`, after a benchmark shows the awaited write is too slow (#2339).
- **Recording all attribute values:** rejected; relational stores keep metadata in plaintext, and only Marten encrypts.
- **Convention-based resource ids (`AuditRequestConventions`):** rejected; a guessed id is not evidence.
- **A pipeline-order startup warning:** dropped in favor of documenting the order and the execution-order contract (#2184, reserved ADR-049).

## Consequences

- **Positive**: every ABAC decision has a queryable, evidential record before the handler runs; an operation audit store outage denies requests instead of silently losing evidence; deny rows survive a business rollback; no new store or schema; the reader and export give the compliance trail; the pattern is reusable by #750, #752 and REQ-051.
- **Negative**: one extra scope and insert per protected request when enabled (to be benchmarked in #2339; no measurement exists yet); with `FailClosed` an operation audit store outage is an application outage for protected requests; policy ids, rule ids and the trace are not queryable columns; no row means "not evaluated or not recorded", not that access was granted (the `Outcomes` filter, a failed `BestEffort` write and a write abandoned at `WriteTimeout` also leave no row, although an abandoned write can still commit late).
- **Neutral**: decisions of callers that use `IPolicyDecisionPoint` directly are not audited; auditing the reads of the audit trail is deferred to #1639; store-level tenant filtering stays in #798; the record is the ABAC decision, not the final outcome of the request, so a later behavior can still deny.

## Related

- [Decision audit reference](../../features/abac/reference/decision-audit.md) and [implementation plan #751](../../plans/abac-decision-audit-implementation-plan-751.md)
- [ADR-036](036-three-audit-stores.md) (the operation audit store this decision writes to)
- [ADR-015](015-xacml-3.0-abac-foundation.md) (XACML 3.0 ABAC foundation)
- [ADR-018](018-cross-cutting-integration-principle.md) (audit trail as cross-cutting function 12)
- [ADR-001](001-railway-oriented-programming.md) and [ADR-006](006-pure-rop-exception-handling.md) (`Either` contracts)
- [SPEC-002](../../specifications/SPEC-002-eu-regulatory-readiness.md) (DEC-006 fail closed, REQ-007, REQ-051, REQ-061)
- Issues: #751 (this decision), #750 (saga audit), #752 (sharding and tenancy audit), #1193 (evidential read audit), #1639 (audit reads of the decision-audit trail), #798 (tenant filtering), #924 (generic ABAC benchmarks), #2339 (decision-audit load test and benchmark), #1633 (operation store registration), #1634 (requirement evaluation), #1676, #1705 (request identity), #1984 (error-code family), #2184 (pipeline execution order)

## Date

2026-10-15
