# Implementation Plan: ABAC Decision Audit Trail (`Encina.Security.ABAC`) — write-ahead, fail-closed, through `IOperationAuditStore`

> **Issue**: [#751](https://github.com/dlrivada/Encina/issues/751)
> **Type**: Feature (labels: `p0-mandatory`, `area-audit`, `area-authorization`, `area-compliance`)
> **Milestone**: v0.14.0 — Hardening, kept by maintainer decision of 2026-10-03 as an explicit exception to SPEC-002 DEC-014 (maintainer decision 2026-10-03: P0 feature for the reference application); see "Maintainer Decisions"
> **Specification**: [SPEC-002](../specifications/SPEC-002-eu-regulatory-readiness.md) (row "Audit trail for ABAC decisions", REQ-007 related, line 789)
> **Depends on**: none open. The store naming spike #1674 and the store registration bug #1633 are merged (ADR-036; the operation store is `IOperationAuditStore`, entry `OperationAuditEntry`, query `OperationAuditQuery`). **Prerequisite split (used everywhere in this plan)**: the hard prerequisites #1634 (PR #1650), #1676, #1635 (PR #1781), #1633 and #1705 Phase 4 (PR #1982) are all merged. Related but not prerequisites: #1677 (merged), #1591, #1685, #1686 (coordinated, non-blocking), see "Adjacent defects that left this plan". Sequencing with open issues: #1984 first, then #751 phase by phase; #1700 and #1993 after #751 Phase 1; #1783 in parallel (see "Maintainer Decisions", item 10)
> **Related**: #750 (saga audit), #752 (sharding/tenancy audit), #749, #753, #798 (tenant filtering), #924 (benchmarks), #1193 (evidential read audit), #1678 (named pipeline stages), #1783 (execution-order contract), #1984 (error-code family), SPEC-002 REQ-051 (break-the-glass)
> **Complexity**: Medium-High (8 phases, no new store, ~17 new and ~14 modified production files; see the file-count table)
> **Estimated Scope**: ~800 lines of production code + ~1,400 lines of tests

**Naming note**: ADR-036 is applied. This plan uses `IOperationAuditStore`, `OperationAuditEntry` and `OperationAuditQuery` throughout (no `IAuditStore` type is left in `src/`). **Phase 0 was removed**: #1633 (registration of the operation-audit store on ADO.NET x3, Dapper x3 and MongoDB, and the `OperationAuditEntries` DDL) is merged.

---

## Summary

Issue #751 asks for an opt-in, queryable audit trail of ABAC decisions: who (subject) asked for what (resource, action, environment), which policies decided, and why. Today the only audit story is a user-written `IObligationHandler` for an `audit-access` obligation (`src/Encina.Security.ABAC/README.md:142-155`), and `PersistentPolicyAdministrationPoint` audits policy *changes* only (`Administration/PersistentPolicyAdministrationPoint.cs:587-664`). Nothing records the access decisions themselves.

This plan records **one `OperationAuditEntry` per evaluated decision** at the Policy Enforcement Point (`ABACPipelineBehavior`), written **before** the protected handler runs (write-ahead), awaited, isolated from the request transaction, and **fail-closed** by default. It reuses `Encina.Security.Audit.IOperationAuditStore` (already referenced by the package, `Encina.Security.ABAC.csproj:14`), so no new store, table or provider code is needed. A typed reader and a JSON Lines export give the "queryable by subject, resource, action" and "compliance export" criteria.

The one defect this plan still fixes because the split of the PEP requires it: `nextStep()` runs inside the evaluation `try`, so a handler exception is reported as `abac.evaluation_failed`. The split moves the handler call outside (PR #1650, merged 2026-10-03, rewrote `Handle`; line numbers below are those of today's PEP).

**Adjacent defects that left this plan** (maintainer decision of 2026-10-03, question 5): they are fixed now as their own issues, and this plan only records the dependency. The audit is trustworthy only once they are fixed, so each is a prerequisite or a coordinated change, not a task here:

| Defect | Issue | Dependency note |
|---|---|---|
| A null security context becomes `userId ""` instead of denying (`ABACErrors.MissingContext` unused); the PDP decides on partial policies when standalone-policy retrieval fails | #1676 | The decision-path table assumes both fail closed. The unauthenticated-caller row (`encina.authorization.unauthenticated`, #1676 then #1705 Phase 4) is produced by the PEP; this plan only records it |
| Open-generic `TryAddTransient` of `IPipelineBehavior<,>` silently skips behaviors registered after another one (about 20 packages, including ABAC and `Encina.Security`) | #1635 | This plan does not change how `Encina.Security` registers its behavior; it only adds the order marker of Phase 4 task 5. Its DI test asserts the pipeline order that #1635 produces and the order documented in Design 3 |
| Persistent PAP: captive scoped store, fire-and-forget audit, `DateTimeOffset.UtcNow` | #1677 (merged) | The `PersistentPolicyAdministrationPoint` constructor change is no longer in this plan |
| Exception and error messages in logs, activity tags and returned errors | #1591 (repo-wide), #1685 (PEP activity status and `EvaluationFailed` carry exception messages; the PR #1650 follow-up) | This plan's new code logs codes and exception types only (AGENTS.md section 3) but does not rewrite existing sites |
| `EELCompiler` recompiles a malformed expression on every request under a global lock (PR #1650 follow-up); PAP by-id top-level lookup (`EvaluatePolicyAsync` loads the whole store); docs examples that match `action.name` to verbs the PEP never produces | #1686; #1687; #1688 | Related, not prerequisites. #1686 matters here only through request latency under a `FailClosed` write; #1687 through PDP latency of the evaluator this plan traces; #1688 through the docs this plan rewrites in Phase 8 |
| `AuditPipelineBehavior` stores `error.Message` | PR #1606 (#1557) | Merged; classification by message text is #1642 |

Fixed in PR #1650 (merged 2026-10-03) and outside this plan: resource attributes receiving `default!` instead of the request is fixed by #1634 (PR #1650), which also makes `[RequirePolicy]` and `[RequireCondition]` enforce as documented and lets `ObligationExecutor` catch handler exceptions (see "What PR #1650 changed").

**Mapping of the issue's events**: `AccessGranted` / `AccessDenied` become the enforced outcome of the record; `PolicyEvaluated` becomes the evaluation trace carried inside the record (not one row per policy); `PolicyCacheHit` is not an audit event (it has no subject and no compliance meaning, and the policy-store cache gives no hit signal on the PDP hot path, `Persistence/CachingPolicyStoreDecorator.cs:247-301`): it becomes the metric `abac.policy_cache.lookups{result}`, deferred to #1640 (not part of this plan's phases).

**Affected packages**: `Encina.Security.ABAC` (main), `Encina.Security.Audit` (doc fix only), core `Encina` (multi-tenancy marker type), `Encina.Tenancy` (registers the marker); no provider package changes (#1633 is merged).
**Provider category**: Database (10), by reuse of `IOperationAuditStore`; Marten through `MartenOperationAuditStore`. Caching, transports, locks, validation, cloud: not touched.

---

## Design Choices

<details>
<summary><strong>1. Interception point — inside the PEP (<code>ABACPipelineBehavior</code>), decide / record / enforce</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Inside the PEP: split `Handle` into decide, record, enforce** | Only place that knows the enforced outcome (obligation override, Warn mode, missing required policy, unmet condition, exceptions); transient, so scoped stores resolve; receives `IRequestContext` | Refactors an existing file; constructor gains parameters |
| **B) Decorator around `IPolicyDecisionPoint`** | Small seam (one-method interface); also audits direct PDP callers | PDP is a singleton; records `Success` for a Permit the PEP later overrides to Deny (`ABACPipelineBehavior.cs:222-247`, main before PR #1650; at `7c092bf2` `HandlePermitAsync` is at 220-270); no record when attribute collection fails; recomputes the Warn-mode logic; cannot see `IRequestContext` |
| **C) PEP raises an `AccessDecisionEnforced` notification persisted by a subscriber** | Extensible (SIEM, break-glass subscribers) | `Publish` with zero handlers returns `Right` (silent fail-open); every app handler becomes part of the gate; re-entrancy guard; largest surface |
| **D) Separate audit pipeline behavior** | Isolated | Cannot see the `PolicyDecision`; depends on registration order |

### Chosen Option: **A — Inside the PEP**

### Rationale

- The audit must describe what was actually enforced. Only the PEP knows it.
- After PR #1650 (#1634) the PEP evaluates `[RequirePolicy]` and `[RequireCondition]` through `ABACRequirementEvaluator` (policy sets and standalone policies by id through `IPolicyDecisionPoint.EvaluatePolicyAsync`, AND/OR groups, conditions evaluated per request) and yields an `ABACRequirementVerdict` (decision plus an optional `DenyError` such as `encina.authorization.abac_policy_not_found` or `encina.authorization.abac_condition_not_met`). `Handle` becomes: `DecideAsync` (collect attributes, evaluate the requirements, run obligations and advice, apply Warn mode; never calls `nextStep`) returns an internal `ABACEnforcementVerdict { Allow, Error, Record }`; then record (only when enabled); then enforce (`Allow ? await nextStep() : Left(Error)`). The record's trace comes from the PDP calls the evaluator makes, so the trace flag is passed through `EvaluatePolicyAsync` as well.
- The split lowers cyclomatic complexity (CRAP <= 10, AGENTS.md section 9) and fixes the handler-exception misreport.
- Decisions made by callers that use `IPolicyDecisionPoint` directly (for example UI permission checks) are **not** audited. This is documented; a public helper that submits to the recorder is a deferred follow-up.
- The notification design (C) is rejected for the first cut: its extension value is real but it adds a silent fail-open path and a hot-path dispatch.

</details>

<details>
<summary><strong>2. Store — reuse <code>IOperationAuditStore</code> / <code>OperationAuditEntry</code>, no new <code>IAbacAuditStore</code></strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Reuse `IOperationAuditStore` with a structured `OperationAuditEntry` mapping** | Implementations already exist for ADO x3, Dapper x3, EF Core, MongoDB, Marten, InMemory; `OperationAuditQuery` already filters by user, entity, action, outcome, tenant, date; OTel decorator; retention services | Policy ids and trace live in `Metadata` (not queryable); no `ModuleId` column |
| **B) New `IAbacAuditStore`** | Queryable policy fields, `ModuleId` | New store on all 10 providers (AGENTS.md section 5), new schemas, scripts, tests; provider coherence cost |
| **C) Extend the read-audit shape of #1193** | Shared "evidential" model | Different semantics (reads), plan not implemented yet |

### Chosen Option: **A — Reuse `IOperationAuditStore`** (decided 2026-10-09; ADR-036 decision 2 names the ABAC decision audit as a writer of the operation store, distinguished by `Action` and `Metadata`)

### Rationale

- The package already depends on `Encina.Security.Audit`, and the NIS2 behavior (`Encina.Compliance.NIS2/NIS2CompliancePipelineBehavior.cs:275-333`) is the precedent for a decision record in the operation audit store.
- Mapping (public static `ABACDecisionAuditEntryMapper.ToOperationAuditEntry`, constants in `ABACDecisionAuditSchema`): `Action = "ABACDecision"` (one indexed filter for all ABAC decisions, `OperationAuditEntryEntityConfiguration.cs:133`), `EntityType` = request type name (the XACML action), `EntityId` = resource id, `UserId` = subject, `TenantId`, `CorrelationId`, `Outcome`, `ErrorMessage` = reason **code only**, `Metadata` = string values only (so EF JSON, MongoDB and Marten encryption round-trip identically). **Three timestamps** (maintainer decision A9, 2026-10-09): `StartedAtUtc` is taken from `TimeProvider` at decision start; `CompletedAtUtc` and the legacy-typed `TimestampUtc` (`TimestampUtc = CompletedAtUtc.UtcDateTime`) come from **one** `TimeProvider.GetUtcNow()` read at the end, consistent with `AuditPipelineBehavior`.
- `OperationAuditEntry.Id` = `DecisionId` (`Guid.CreateVersion7`), which gives idempotent retries (see decision 4).
- A queryable policy/module column is deferred: a `ModuleId` on `OperationAuditEntry`/`OperationAuditQuery` for all providers is a separate `[FEATURE]`.

</details>

<details>
<summary><strong>3. What a record carries — outcome, reasoning trace, attributes (names first, values by allow-list)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Enforced outcome + opt-in PDP evaluation trace + attribute names; values only on an allow-list** | Answers "which policies matched or failed"; minimal personal data; works with today's attribute bag (names are dropped by `AttributeContextBuilder.ToBag`, `:71-92`) | PDP gets a trace parameter; `RuleId` is a representative rule for `*-overrides` algorithms |
| **B) Only `PolicyDecision` as returned today** | No PDP change | `RuleId` and `Reason` are never set; per-policy results are discarded (`XACMLPolicyDecisionPoint.cs:74,269,377-405`); no reasoning |
| **C) Record all attribute values** | Complete evidence | Personal data in plaintext JSON on relational stores (only Marten encrypts, `AuditEventEncryptor.cs:99-122`) |

### Chosen Option: **A**

### Rationale

- `PolicyEvaluationContext.IncludeEvaluationTrace` (default false, next to `IncludeAdvice`, `Model/PolicyEvaluationContext.cs:87`) makes `XACMLPolicyDecisionPoint` pass a nullable trace sink through `EvaluatePolicySet` / `EvaluatePolicy`. Off means zero allocation.
- `PolicyDecision` gains `EvaluatedPolicies` (`PolicyEvaluationTrace` nodes: policy id, `IsPolicySet`, effect, reason `Evaluated | Disabled | TargetNotMatched | TargetIndeterminate`, decisive rule ids, children) and `RuleId` is finally populated by following the children whose effect equals the decision, the same rule `DenyOverridesAlgorithm.BuildCombinedResult` uses for `PolicyId` (`:160`). The trace is capped by `MaxTraceEntries`; truncation logs a Debug event.
- **Trace assembly** (several PDP calls, one record): there is **one record per decision**, however many `EvaluatePolicyAsync` calls `ABACRequirementEvaluator` makes. The trace lists each required policy's evaluation subtree in **declaration order** (the order of the requirements, AND/OR groups flattened in that order), then each condition's outcome. A policy or condition that was not evaluated because the earlier policies already failed (AND/OR short-circuit) is recorded as `not evaluated`, so the trace states what was evaluated and what was skipped. `MaxTraceEntries` caps the **whole record**, not each PDP call; truncation logs the Debug event 9088. The record's `PolicyId` is the **deciding policy**: for a deny, the first denying policy of the AND group in declaration order; for a permit, the first deciding policy in declaration order (the full list of deciding policies is in the trace); for `encina.authorization.abac_condition_not_met` (no PDP policy) it is the token `condition:<declaration index>` (zero-based order of the `[RequireCondition]` attributes on the request type), never the expression text (`RequireConditionAttribute` has only `Expression`, which is unbounded and may hold literals). `RuleId` is the representative rule of that policy, as today (`RuleId` is representative for `*-overrides` algorithms).
- Attribute names come from the raw `IAttributeProvider` dictionaries, captured before `ToBag`. Values are stored only for keys on `RecordedAttributeValues` (empty by default). **Built-in subject attributes (maintainer decision A5, 2026-10-09)**: since #1705 Phase 4 the subject dictionary always contains `subject-id` and `identity-kind` (`ABACSubjectAttributes.WithBuiltIns`); both are excluded from the stored attribute-name list, so the names describe what the provider supplied. The identity kind (from the single `RequestIdentity` snapshot returned by `ResolveCaller`) is stored as the metadata key `abac.identity_kind`, defined in `ABACDecisionAuditSchema`, so it is queryable without parsing a list. For a service identity the stored `UserId` is `service:<name>`.
- Never stored: `DecisionStatus.StatusMessage` (it can contain `ex.Message`, `XACMLPolicyDecisionPoint.cs:170`), any `EncinaError.Message`, request or response payloads.
- Warn mode: `Outcome` stays `Success` because ABAC did not block the request; metadata says `abac.enforced=false` and carries the would-deny verdict. `Success` means "ABAC let it through", not "the handler ran": a later behavior or the handler can still fail, and proof that the handler ran needs the handler's own audit row joined by `CorrelationId`. Reviewers filter `abac.enforced` to separate enforced from would-deny rows.
- Decision-path table (the single source for the mapper and for "outcome mapping is total"). Every path below is **always recorded** when audit is enabled and the `Outcomes` flag matches; the failure paths (rows marked `always`) ignore the `Outcomes` filter, so a filter of `Denied` can never hide an error or an override.

| PEP path (`ABACPipelineBehavior.cs`) | `ABACEnforcedOutcome` | `AuditOutcome` | `Outcomes` flag | Reason code | Proceeds |
|---|---|---|---|---|---|
| Permit, obligations ok (`HandlePermitAsync`) | Granted | Success | Granted | `abac.permit` | yes |
| Permit, mandatory obligation failed, or its handler threw (`ObligationExecutor` catches the exception and returns `abac.obligation_handler_exception`), override to Deny | Denied | Denied | Denied | `encina.authorization.abac_obligation_failed` | no |
| Deny from a required policy, Block mode (`HandleDenyAsync`, `ApplyEnforcementAsync`) | Denied | Denied | Denied | `encina.authorization.abac_access_denied` | no |
| Deny from a required policy, Warn mode | DeniedNotEnforced | Success (`abac.enforced=false`) | NotEnforced | `encina.authorization.abac_access_denied` | yes |
| Deny because a `[RequirePolicy]` id names no top-level policy set or standalone policy (`encina.authorization.abac_policy_not_found`), Block mode | Denied | Denied | Denied | `encina.authorization.abac_policy_not_found` | no |
| Same, Warn mode | DeniedNotEnforced | Success (`abac.enforced=false`) | NotEnforced | `encina.authorization.abac_policy_not_found` | yes |
| Deny because a `[RequireCondition]` evaluated to false for this request (`encina.authorization.abac_condition_not_met`), Block mode | Denied | Denied | Denied | `encina.authorization.abac_condition_not_met` | no |
| Same, Warn mode | DeniedNotEnforced | Success (`abac.enforced=false`) | NotEnforced | `encina.authorization.abac_condition_not_met` | yes |
| Indeterminate (policy lookup or evaluation failed, condition could not be compiled or evaluated), **every** enforcement mode | Denied | Error | always | `abac.indeterminate` | no |
| Exception during attribute collection or evaluation (`Handle` catch) | Denied | Error | always | `abac.evaluation_failed` | no |
| Unauthenticated caller (denial added by #1676, moved to the request identity by #1705 Phase 4: `ABACPipelineBehavior.HandleUnauthenticatedCaller :375`, `ABACErrors.UnauthenticatedCaller`); the subject is unknown, so the stored subject is none: `UserId` is `null` and only the reason code is stored | Denied | Denied | always | `encina.authorization.unauthenticated` | no |
| Cancellation (`OperationCanceledException` while the caller's token is cancelled; `Handle` rethrows) | n/a | n/a (nothing recorded) | n/a | none: cancellation propagates | no |
| Audit write failed under FailClosed, request would proceed | Denied | not recorded (the write failed) | n/a | `abac.decision_audit_failed` | no |

  After PR #1650 there is no NotApplicable row: `ABACOptions.DefaultNotApplicableEffect` is removed, and a required policy that does not apply is already a Deny verdict of the requirement evaluator (`ABACRequirementVerdict`), so it is one of the Deny rows above (the reason code stays the one the PEP reports). `ABACOptions.FailOnMissingObligationHandler` is also removed. Warn mode relaxes only **definite** verdicts (Deny, `encina.authorization.abac_policy_not_found`, `encina.authorization.abac_condition_not_met`): an Indeterminate result, an exception, a missing context and a failed mandatory obligation are the same in Block and Warn mode (always Denied, no pass-through). The table is total over the PEP's paths: Permit (1 row), obligation override (1), Deny (Block, Warn), policy not found (Block, Warn), condition not met (Block, Warn), Indeterminate (1, all modes), exception, missing context, cancellation, audit failure.

  > **State of PR #1650 (merged 2026-10-03; verified at head `7c092bf2`)**: Indeterminate denies in every enforcement mode (`HandleIndeterminate`), and `ABACOptions.FailOnMissingObligationHandler` and `DefaultNotApplicableEffect` are removed (no hit in `src` at that head), so a missing handler for a mandatory obligation always denies. `encina.authorization.abac_policy_not_found` is a **definite** denial: in Warn mode it is logged and the request proceeds, like `encina.authorization.abac_access_denied` and `encina.authorization.abac_condition_not_met`. **Codes (renamed by #1984, maintainer decision 2026-10-09, branch `fix/1984-abac-denial-codes`)**: the PEP denials are `encina.authorization.abac_access_denied`, `abac_condition_not_met`, `abac_obligation_failed` and `abac_policy_not_found` (the last is `ABACErrors.RequiredPolicyNotFoundCode`; the admin lookups become `abac.policy.not_found` and `abac.policy_set.not_found`). The codes in this plan are the post-#1984 ones; #1984 lands first. Codes #1984 does not rename keep their `abac.*` names (`abac.indeterminate`, `abac.evaluation_failed`, `abac.obligation_handler_exception`, `abac.decision_audit_failed`), so the stored reason codes are a mix of two families. The mapper takes every reason code from the `ABACErrors.*Code` constants, never from a literal, so a rename flows into the rows by construction.

  The Indeterminate row is `always` because an evaluation failure is evidence that must not be filtered out, unlike a regular Deny verdict; the same holds for the other `always` rows. `Disabled` enforcement and requests with no ABAC attributes (at `7c092bf2` the Disabled bypass is `ABACPipelineBehavior.cs:114-120`; main before PR #1650: `:112-121`) make no decision and record nothing, so **no row means "not evaluated", not "not accessed"** (the validator rejects `Enabled` together with `Disabled`).

  **Position in the pipeline**: the record is the **ABAC decision**, not the final outcome of the request. Behaviors run in registration order; with `AddEncinaABAC` before `AddEncinaSecurity` the ABAC record is written first and `SecurityPipelineBehavior` can still deny afterwards (for example `[DenyAnonymous]`, role or permission gates), and that later denial is not in the ABAC record. This is acceptable because the record answers "what did ABAC decide and enforce", every row says so (`abac.stage=pep`), and the other gate's denial is that gate's own concern. The documented order is `AddEncinaSecurity` first, then `AddEncinaABAC` (authentication and static gates run before ABAC, so ABAC rows exist only for requests that passed them). Maintainer decision A6 of 2026-10-09 (replacing the 2026-10-03 startup warning): there is **no** order warning and no EventId 9090; this plan only documents that `AddEncinaSecurity` is registered before `AddEncinaABAC`, and the DI test pins the resulting pipeline order for both registration orders so a change is visible. The execution-order contract is #1783 (p0; #1678 was merged into it), not this plan.

</details>

<details>
<summary><strong>4. Failure semantics and transaction isolation — awaited, own scope, fail-closed by default</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Awaited write in an isolated DI scope with `TransactionScope(Suppress)`, `FailClosed` default, `BestEffort` explicit opt-out** | Meets AGENTS.md section 3 (no swallowed errors, fail-closed); deny rows survive business rollback; no captive scoped dependency | One extra scope + insert per protected request; audit-store outage denies requests |
| **B) Fire-and-forget (PAP / NIS2 precedent)** | Zero latency | Swallows errors (forbidden in background infrastructure), disposed `DbContext` risk, access proceeds without evidence |
| **C) Buffered channel + background writer** | Lowest latency | Loss window, not write-ahead, much more code; deferred, see below |

### Chosen Option: **A**

### Rationale

- `OperationAuditStoreEF` saves through the request's shared `DbContext` (`OperationAuditStoreEF.cs:67-68`) and the EF `TransactionPipelineBehavior` rolls back on `Left` (`EntityFrameworkCore/TransactionPipelineBehavior.cs:118-126`); an ABAC deny returns `Left`. A record written in the request scope would vanish. The default recorder `AuditStoreABACDecisionRecorder` (singleton) opens `IServiceScopeFactory.CreateAsyncScope()`, suppresses ambient transactions, resolves `IOperationAuditStore` and awaits `RecordAsync` with its own `CancellationTokenSource(WriteTimeout, timeProvider)`.
- **Token linking (maintainer decision A3, 2026-10-09)**: the recorder write is **never** linked to the client token, so a disconnect cannot erase the evidence of a denied attempt. The `PersistentPolicyAdministrationPoint` differs on purpose (`RecordAuditAsync` links the caller token, `PersistentPolicyAdministrationPoint.cs:764-780`): an administrative change is user-visible, a decision record is evidence. Cancellation: when the caller token is already cancelled before the record step, the PEP skips the write and rethrows (nothing recorded, as the cancellation row says); a token cancelled during the write does not abort it, and `Handle` then throws `OperationCanceledException` after the write.
- **Bounded write (maintainer decision A2, 2026-10-09)**: the cooperative token alone does not bound a store that ignores it, and under `FailClosed` a hang is worse than a denial. The recorder therefore also races the write with `Task.WaitAsync(timeout, timeProvider, ct)` through a small internal helper that #1704 (item 1, the same flaw in the PAP) can adopt, and treats the timeout as an ambiguous failure that the idempotent re-check below then decides. The timeout is configurable as `ABACOptions.DecisionAudit.WriteTimeout`, **default 5 seconds** when not configured, validated at start. A test uses a store that ignores the token.
- `FailClosed`: when the request would proceed and the write returns `Left`, throws or times out, the PEP returns `ABACErrors.DecisionAuditFailed(requestType, storeErrorCode)` (`abac.decision_audit_failed`, fixed message, code in details). When the request is denied anyway the original deny stands and the failure is logged.
- `FailClosed` is the default (maintainer decision of 2026-10-03). `BestEffort` is the explicit, logged opt-out: it proceeds and logs, and a startup Warning (EventId 9084) makes it visible (SPEC-002 DEC-006).
- A buffered channel with a background writer (option C) is deferred: only after the #924 benchmark shows the awaited write is too slow, and only as an option **inside** `BestEffort`, because it is incompatible with `FailClosed` (it is not write-ahead and has a loss window).
- Idempotency: there are no retries on the request path (the Resilience row stays "timeout only"). The duplicate check protects against one ambiguous outcome: the write timed out or returned `Left` after the row was actually committed. The recorder then checks `GetByCorrelationIdAsync`, bounded by the same `WriteTimeout` budget through its own `WaitAsync` (the store may ignore the token too; the "store that ignores the token" test covers the re-check), for an entry with the same `Id` and treats a match as success instead of denying a request whose evidence is in fact stored. It is best-effort on Marten, whose audit reads come from an asynchronous projection (`Projections/ConfigureMartenOperationAuditProjections.cs:75,79`; `MartenOperationAuditStore.cs:128-129,149`): a committed event may not be visible yet, so on Marten an ambiguous failure can still deny (fail closed, never a false success).
- A long value can never fail the insert and deny access. The mapper applies a **per-column rule** to every bounded column of `OperationAuditEntryEntityConfiguration.cs` (`src/Encina.EntityFrameworkCore/Auditing/`; limits unchanged: 256 at `:42,47,60,64`, 128 TenantId `:51,56`, 2048 ErrorMessage `:71`, 45 IpAddress `:86`, 512 UserAgent `:90`, 64 payload hash `:95`, Action index `:133`) (`sha256:<64 hex>` is 71 characters, so a hash only replaces a value where it fits):

  | Column (limit) | Rule when the value exceeds the limit | Marker in metadata |
  |---|---|---|
  | `UserId` 256, `EntityType` 256, `EntityId` 256, `CorrelationId` 256, `TenantId` 128 (`OperationAuditEntryEntityConfiguration.cs:42-64`) | replaced by `sha256:<64 hex>` (71 characters, fits every one of them) | name added to `abac.hashed_fields` |
  | `Action` 128 | never over: it is the constant `ABACDecision` | none |
  | `ErrorMessage` 2048 (`:69-71`) | never over: it is a fixed reason code | none |
  | `IpAddress` 45 (`:84-86`) | a hash cannot fit and a truncated address is a different, misleading address: the value is stored as `null` when it is over 45 characters or does not parse as an IP address | name added to `abac.dropped_fields` |
  | `UserAgent` 512 (`:88-90`; attacker-controlled: `EncinaContextMiddleware.cs:255-261` copies the raw header) | truncated to its first 512 characters (a readable prefix is more useful than a hash) | name added to `abac.truncated_fields` |
  | `Metadata` | bounded by construction: trace nodes capped by `MaxTraceEntries`, attribute-name list capped at 128 names, values only from the allow-list and capped at 256 characters each | `abac.truncated_fields` when a cap applies |

  `RequestPayloadHash` (64) is never written (a 71-character hash would not fit it). The reader applies the same normalization to its `UserId`, `EntityType`, `EntityId` and tenant filters, so a value stored as a hash is still found (otherwise an over-length tenant would be silently invisible). #1633 (merged) created the same limits for ADO, Dapper and MongoDB. The property test generates over-limit values for **every** row of this table (not only ids) and asserts that no mapped field exceeds its limit, that hashed fields are exactly `sha256:` plus 64 hex characters, and that the markers list exactly the modified fields.
- Obligations run before the write (the record needs their result); if the write then fails closed, OnPermit side effects remain. Documented.

</details>

<details>
<summary><strong>5. Configuration model — nested <code>ABACOptions.DecisionAudit</code>, off by default, validated at start</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ABACOptions.DecisionAudit` (`ABACDecisionAuditOptions`) + `AuditDecisions(...)` fluent + `IValidateOptions` with `ValidateOnStart`** | Matches `PolicyCaching`; opt-in; invalid combinations fail at startup | Slightly larger options surface |
| **B) Flat `AuditAbacDecisions` bool (issue text)** | Literal | No place for failure mode, outcome filter, limits |
| **C) Separate `AddEncinaABACAudit()`** | Explicit | Splits registration, risk of PEP without recorder |

### Chosen Option: **A**

### Rationale

- Options: `Enabled` (false), `Outcomes` (`[Flags]` Granted, Denied, NotEnforced; All), `FailureMode` (FailClosed), `IncludeEvaluationTrace` (true), `MaxTraceEntries` (64), `ResourceIdAttributeName` ("resourceId"), `RecordedAttributeValues` (empty), `WriteTimeout` (default 5 seconds, validated at start; it bounds the whole write through the `WaitAsync` race of Design 4, not only the token). `options.AuditDecisions(o => ...)` is the issue's `AuditAbacDecisions = true` (maintainer-accepted: `DecisionAudit.Enabled` with the `AuditDecisions(...)` shortcut; attribute names only, values only through `RecordedAttributeValues`).
- Gating uses the **resolved** `IOptions<ABACOptions>` at runtime, never the throw-away options instance built by invoking only the configure lambda (`ServiceCollectionExtensions.cs:112-113`), which cannot see `Enabled` bound from `IConfiguration` or set by a later `Configure<ABACOptions>`. Registration therefore cannot depend on `Enabled`, and **nothing registered by `AddEncinaABAC` has a constructor dependency on `IOperationAuditStore`** (ABAC never registers that store, the application or a provider package does): the recorder (singleton) resolves the store per write in its own scope; the reader (scoped) takes `IServiceProvider` and resolves `IOperationAuditStore` lazily inside `QueryAsync`/`ExportAsync`, returning `Left(ABACErrors.DecisionAuditStoreUnavailable)` (`abac.decision_audit_store_unavailable`, fixed message) when none is registered; the startup check resolves it only when `Enabled`. The recorder, reader, `TimeProvider`, options validator and startup check are always registered and cost nothing when audit is off, and an ABAC app with audit disabled and no `IOperationAuditStore` passes `ValidateOnBuild` and `ValidateScopes`. The PEP checks `options.DecisionAudit.Enabled` per request (the per-request gate for the recorder stays). Registering conditionally was rejected: it needs the unresolved options, the very thing this design avoids.
- `ABACOptionsValidator` (uses `IServiceProviderIsService`) fails when audit is enabled and: no `IOperationAuditStore` registered with the default recorder, `EnforcementMode` is `Disabled` (the PEP never calls the PDP, Disabled bypass `ABACPipelineBehavior.cs:123-126`), or timeout/trace bounds are invalid. (Since #1705 Phase 4 the PEP reads the caller from `IRequestContext.Identity` and `AddEncinaABAC` registers the request identity model itself, so there is no identity service to validate.)
- **No per-request-type warning (maintainer decision A1, 2026-10-09)**: the startup warning for `Disabled` (EventId 9085, `ABACEnforcementModeStartupCheck`, logged once at startup since #1705 Phase 4) is the logged opt-out required by AGENTS.md section 3, and it stays. #751 adds no per-request call and no static per-type flag, so it uses no part of 9085; the validator's rejection of `Enabled` together with `Disabled` stays.
- When `Enabled` is false nothing is built per request: no record, no time read, no trace flag, no recorder call, and no `IOperationAuditStore` is resolved (pay-for-what-you-use). Only the idle singleton recorder, the lazily-resolving reader, the validator and the no-op startup check exist in the container.
- Added option: `AllowCrossTenantQueries` (false), the logged opt-out of the reader tenant gate, meant only for operator tooling in a multi-tenant application (Design 7). Single-tenant applications need no configuration. **How the reader learns that multi-tenancy is enabled (decided here)**: ABAC cannot name Tenancy types (`Encina.Security.ABAC.csproj` references only `Encina.Caching`, `Encina.Security` and `Encina.Security.Audit`, none of which references `Encina.Tenancy`, so ABAC cannot name any Tenancy type), and the only tenant concept in core is `IRequestContext.TenantId` (`src/Encina/Abstractions/IRequestContext.cs:125`). So: (1) core `Encina` defines a public marker type (name decided at implementation, for example `MultiTenancyMarker`); (2) `Encina.Tenancy`'s registration (`AddEncinaTenancy`) adds it to the container; (3) the reader checks it with `IServiceProviderIsService`. Resolution order in the reader: an ambient tenant (`IRequestContext.TenantId` through `IRequestContextAccessor`) is **always** forced when present; when none is present the reader denies with `tenant_required` if the marker is registered (multi-tenancy enabled), and queries without a tenant filter if it is not (single-tenant application); `AllowCrossTenantQueries` stays the logged opt-out for operator tooling. This fails closed: a multi-tenant application cannot forget an option, because the Tenancy registration sets the signal.

</details>

<details>
<summary><strong>6. Resource identity and context — <code>IABACResourceIdentity</code>, <code>IRequestContext</code>, explicit resource ids only</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Optional `IABACResourceIdentity` on requests, then attribute `resourceId`; no public conventions type** | Explicit ids only, so `EntityId` is exact; no new public type in `Encina.Security.Audit`; `CorrelationId` lines up with `AuditPipelineBehavior` rows | An undeclared resource leaves `EntityId` empty; no `EntityId` join with `AuditPipelineBehavior` rows |
| **B) Attribute provider only** | Simple | `DefaultAttributeProvider` returns empty dictionaries (`Providers/DefaultAttributeProvider.cs:36-66`): no resource id by default |
| **C) Reflection on request** | Automatic | Hot-path reflection, ambiguous |

### Chosen Option: **A**, in the explicit-ids form (maintainer decision A8, 2026-10-09)

### Rationale

- **No `AuditRequestConventions` promotion (A8)**: `RequestMetadataExtractor` stays `internal` in `Encina.Security.Audit` and is not promoted; ABAC does not use the `Id`-suffix convention guess. The resource id comes only from `IABACResourceIdentity` or the `resourceId` attribute (`ResourceIdAttributeName`). A resource that declares neither leaves `EntityId` empty, and the documentation explains how to declare it.
- The PEP already passes the real `request` to `GetResourceAttributesAsync` (fixed by #1634, PR #1650).
- **Caller from the single snapshot (read-once rule, #1892)**: `UserId` and the identity kind come from the `caller` value returned by `ResolveCaller` (`ABACPipelineBehavior.cs:369`), never from a second `context.Identity` read.
- `CorrelationId` and `TenantId` come from `IRequestContext`, `ModuleId` from `context.GetModuleName()`, the subject from `context.Identity` (read once; `UserId` and the `IdentityKind`, which the decision row can record); an anonymous identity denies with `ABACErrors.UnauthenticatedCaller` (code `encina.authorization.unauthenticated`, detail `gate=abac`; #1676 added the denial, #1705 Phase 4 moved it to the request identity; this plan records the resulting row).
- `EntityType` decision: ABAC rows keep `EntityType` = full request type **name** (the XACML action), because the query "by action" must be a server-side filter. `AuditPipelineBehavior` rows take `EntityType` from `RequestMetadataExtractor.ExtractFromTypeName` (verb and suffix stripped: `CreateOrderCommand` gives `Order`; `DefaultOperationAuditEntryFactory.cs:141`). The two row kinds therefore join by `CorrelationId` only, not by `EntityType` or `EntityId`. No "rows line up on EntityType" claim is made anywhere in this plan.
- Documentation explains how to declare the resource id (`IABACResourceIdentity` or the `resourceId` attribute) and that an undeclared resource leaves `EntityId` empty.

</details>

<details>
<summary><strong>7. Query and export — typed reader over <code>IOperationAuditStore.QueryAsync</code></strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `IABACDecisionAuditReader` returning an `ABACDecisionAuditRecord` projection; JSON Lines export; tenant forced when multi-tenancy is enabled** | Typed, server-side filters (`Action` constant, `UserId`, `EntityType`, `EntityId`, `Outcome`, `TenantId`, dates); export pages at `MaxPageSize` | A new public reader |
| **B) Extension methods on `IOperationAuditStore` returning raw `OperationAuditEntry`** | Tiny | Callers parse metadata themselves; no tenant guard |
| **C) Export only** | Smallest | No query API (acceptance criterion) |

### Chosen Option: **A**

### Rationale

- Tenant gate (fails closed, AGENTS.md section 3; maintainer decision of 2026-10-03; detection through the core marker, see Design 5): whenever an ambient tenant is present the reader forces it, and when multi-tenancy is enabled it rejects an explicit mismatching tenant with a `Left` carrying the `tenant_mismatch` denial (SPEC-002 REQ-061). When multi-tenancy is enabled and **no ambient tenant is resolvable**, the reader does not query across tenants: `OperationAuditQuery.TenantId == null` means "no filter" (`OperationAuditQuery.cs`; `OperationAuditStoreEF.cs:196`), so it returns a `Left` carrying the `tenant_required` denial. **Error family (maintainer decision A4, decided with #1984 option a, 2026-10-09)**: these two tenant denials use `encina.authorization.*` codes (so the host adapters answer 403 with no adapter change); `abac.decision_audit_failed` stays `abac.*` (a server-side failure, 500), and so do `abac.invalid_decision_audit_query` (for the non-tenant query validation) and `abac.decision_audit_store_unavailable`. Sequence the Phase 1 `ABACErrors.cs` edit after or with #1984 to avoid a conflict. A **single-tenant application** (multi-tenancy not enabled) queries without any configuration. `DecisionAudit.AllowCrossTenantQueries` (default false) remains only as the explicit, logged opt-out for operator tooling in a multi-tenant application. Store-level automatic tenant filtering stays in #798.
- Reader and export are **not** authorized by this feature: they expose access history, so the application must put them behind its own authorization (documented; the reader is a plain service, not an endpoint). Auditing the reads of the audit trail itself is deferred to #1193 (evidential read audit): issue to open (see the deferred table in the Cross-Cutting Integration Matrix).
- Queries validate page size (<= `OperationAuditQuery.MaxPageSize`, 1000; reuse the constant, do not hard-code it) and `FromUtc <= ToUtc`, returning `Left`. The reader resolves `IOperationAuditStore` lazily from an injected `IServiceProvider` (see Design 5) and returns `Left(DecisionAuditStoreUnavailable)` when it is missing, so it can be registered unconditionally.
- Export is JSON Lines, schema `encina.abac.decision/1`, codes only.

</details>

---

## Implementation Phases

### Phase 1: Core Models, Trace and Errors

> **Goal**: The data model that lets the PEP describe a decision, and the PDP trace.

<details>
<summary>Tasks</summary>

New namespace `Encina.Security.ABAC.DecisionAudit` unless noted.

1. `Model/PolicyEvaluationTrace.cs`: `sealed record PolicyEvaluationTrace` (`required string PolicyId`, `required bool IsPolicySet`, `required Effect Effect`, `required PolicyTraceReason Reason`, `IReadOnlyList<string> DecisiveRuleIds`, `IReadOnlyList<PolicyEvaluationTrace> Children`, `string? Version`); `enum PolicyTraceReason`.
2. `Model/PolicyDecision.cs`: add `EvaluatedPolicies { get; init; }` (default `[]`); `RuleId` populated when traced.
3. `Model/PolicyEvaluationContext.cs`: add `bool IncludeEvaluationTrace { get; init; }`.
4. `Evaluation/XACMLPolicyDecisionPoint.cs`: thread a nullable trace sink through `EvaluatePolicySet` / `EvaluatePolicy` and through the named-policy entry point `EvaluatePolicyAsync` that PR #1650 added to `IPolicyDecisionPoint` (re-read the file after #1650 for line numbers); `BuildDecision` fills `EvaluatedPolicies` and `RuleId` via a helper `PolicyEvaluationTraceResolver.ResolveDecisiveRuleId` (extracted so CRAP stays <= 10). Cap at `MaxTraceEntries`. The fail-closed fix for a failed standalone-policy retrieval and the removal of exception messages from the PDP are **not** in this task: they are #1676 and #1591 (plus #1685); this task only keeps `StatusMessage` out of the stored entry (the mapper never reads it).
5. `DecisionAudit/ABACDecisionRecord.cs` (`sealed record`: identity, context, request, decision, trace, obligations/advice ids, attribute names, recorded values, timing), `ABACEnforcedOutcome` (Granted, Denied, DeniedNotEnforced), `IABACResourceIdentity`.
6. `ABACErrors.cs`: `DecisionAuditFailedCode = "abac.decision_audit_failed"`, `DecisionAuditFailed(Type, string?)`, `InvalidDecisionAuditQueryCode = "abac.invalid_decision_audit_query"` / `InvalidDecisionAuditQuery(string)`, `DecisionAuditStoreUnavailableCode = "abac.decision_audit_store_unavailable"` / `DecisionAuditStoreUnavailable()`, and the reader's tenant denials `tenant_required` / `tenant_mismatch` under the `encina.authorization.*` family (403, A4); fixed messages, codes only in details.
7. `DecisionAudit/ABACDecisionAuditSchema.cs`: `Action`, `SchemaVersion`, metadata keys (including `abac.identity_kind`, A5), column-limit constants.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT: src/Encina.Security.ABAC. The PDP (Evaluation/XACMLPolicyDecisionPoint.cs) discards per-policy and per-rule results and never sets PolicyDecision.RuleId/Reason.
TASK: Add the opt-in evaluation trace (PolicyEvaluationContext.IncludeEvaluationTrace -> PolicyDecision.EvaluatedPolicies + RuleId, through EvaluatePolicyAsync as well), the ABACDecisionRecord model, ABACEnforcedOutcome, IABACResourceIdentity, ABACDecisionAuditSchema constants and the new ABACErrors. Trace off must allocate nothing. Do not touch the adjacent defects (#1676, #1635, #1677, #1591).
KEY RULES: C# 14, nullable, XML docs on public APIs, no [Obsolete], Either<EncinaError,T>, error factories use fixed messages and codes only (AGENTS.md section 3), CRAP <= 10 on changed methods (extract helpers), add every public symbol to PublicAPI.Unshipped.txt via mechanical-fixer.
REFERENCE FILES: src/Encina.Security.ABAC/Model/PolicyDecision.cs, Model/PolicyEvaluationContext.cs, Evaluation/XACMLPolicyDecisionPoint.cs:74-405, CombiningAlgorithms/DenyOverridesAlgorithm.cs:160, ABACErrors.cs.
```

</details>

---

### Phase 2: PEP Refactor (decide, record, enforce)

> **Goal**: The PEP records the enforced outcome write-ahead. Starting point is the PEP as merged by #1634 (PR #1650): `ABACRequirementEvaluator` already evaluates the requirements; the fail-open gaps found on the way are #1676 and #1635, not tasks here.

<details>
<summary>Tasks</summary>

1. `ABACPipelineBehavior.cs`: the constructor **already takes** `IOptions<ABACOptions>` (after #1705 Phase 4: pdp, attributeProvider, obligationExecutor, eelCompiler, options, logger; no security-context accessor, the caller comes from `context.Identity`); it gains only `IABACDecisionRecorder decisionRecorder` and `TimeProvider timeProvider` (the request context comes from the `IRequestContext` argument of `Handle`, no constructor change); both new dependencies are always registered (Phase 4). The existing `options` is read per request for `DecisionAudit.Enabled` (Design 5). Split `Handle` into `DecideAsync` (internal `ABACEnforcementVerdict`), `RecordAsync` (applies `Outcomes` filter and `FailureMode`), and enforcement; `nextStep()` only in the last step, outside any try/catch.
2. Unauthenticated caller: the denial (`ABACErrors.UnauthenticatedCaller`, code `encina.authorization.unauthenticated`) and its log event (9091 `UnauthenticatedCaller`) come from #1676 and #1705 Phase 4 (merged before this phase); this task makes that path pass through record + enforcement and map to the unauthenticated-caller row.
3. Attribute collection: capture attribute names before `ToBag`, excluding the built-in `subject-id` and `identity-kind` (A5); read `IABACResourceIdentity`, then `ResourceIdAttributeName`, and nothing else (explicit ids only, empty `EntityId` when undeclared, A8); take `CorrelationId`/`TenantId` from `IRequestContext`; take `UserId` and the identity kind from the `caller` of `ResolveCaller`. Read `TimeProvider` once for `StartedAtUtc` at decision start and once at the end for `CompletedAtUtc` and `TimestampUtc` (A9). (Passing `request` to `GetResourceAttributesAsync` is already done by #1634.)
4. Logs: every new event of this plan logs codes and exception types only (exceptions through `ForLogging()`), with a unit test that a throwing store and a throwing recorder put no exception or error message text in captured log output, activity status or the returned error. Rewriting the existing message sinks of the PEP and PDP is #1591 and #1685, not this task.
5. Warn mode: record `Outcome = Success` plus `abac.enforced=false` for definite verdicts only (Deny, `encina.authorization.abac_policy_not_found`, `encina.authorization.abac_condition_not_met`); Indeterminate, exceptions, missing context and failed mandatory obligations stay Denied in every mode (see the decision-path table and the note on PR #1650 head `7c092bf2`).
5b. No pipeline-order warning (A6, 2026-10-09): see Phase 4 task 5.
6. `Encina.Security.Audit`: no promotion of `RequestMetadataExtractor` (A8); fix the `OperationAuditEntry.ErrorMessage` XML doc (`OperationAuditEntry.cs`) and the `IOperationAuditStore.cs:44` remark ("Recording failures should not affect the original request processing") that contradicts fail-closed.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT: ABACPipelineBehavior<TRequest,TResponse> (src/Encina.Security.ABAC/ABACPipelineBehavior.cs, as merged by #1634) calls nextStep() from the permit and enforcement helpers inside its evaluation try block. #1676 (missing context and partial policy retrieval fail closed) and #1635 (registration collision) are hard prerequisites merged first, as is #1634 (PR #1650); do not redo them. #1677 and #1591 are related, non-blocking issues; do not redo them either.
TASK: Refactor Handle into decide -> record -> enforce as described in the plan (Design Choice 1, 3, 4, 6). Handler exceptions must propagate unchanged. Disabled mode stays a bypass (recording nothing; the validator rejects Enabled together with Disabled); the existing startup warning 9085 stays and no per-request warning is added (A1). Keep behavior byte-identical when DecisionAudit.Enabled is false except for the listed fixes.
KEY RULES: fail closed (AGENTS.md section 3); TimeProvider, never DateTime.UtcNow; codes only in logs/activity; EventIds 9079-9084 and 9086-9089 registered in ABACLogMessages (inside SecurityABAC 9000-9099; 9072-9078 are taken by #1634, 9085 and 9091-9097 by #1705 Phase 4 and #1677; skip any id taken meanwhile); CRAP <= 10; existing ABACPipelineBehaviorTests must be updated, not deleted.
REFERENCE FILES: ABACPipelineBehavior.cs (472 lines on main; Handle at 116-200, ResolveCaller :369, HandleUnauthenticatedCaller :375, CollectAttributesAsync :392), AttributeContextBuilder.cs:71-103, ABACErrors.cs:295, src/Encina.Security.Audit/DefaultOperationAuditEntryFactory.cs:141,249-258.
```

</details>

---

### Phase 3: Recorder, Mapper, Reader and Export

> **Goal**: Persist through `IOperationAuditStore` and read it back.

<details>
<summary>Tasks</summary>

1. `DecisionAudit/IABACDecisionRecorder.cs`: `ValueTask<Either<EncinaError, Unit>> RecordAsync(ABACDecisionRecord, CancellationToken)`.
2. `DecisionAudit/AuditStoreABACDecisionRecorder.cs` (singleton): ctor `(IServiceScopeFactory, IOptions<ABACOptions>, TimeProvider, ILogger<...>)`; scope, `TransactionScope(Suppress, AsyncFlowOption.Enabled)`, a token source never linked to the client token (A3: the PAP links it on purpose, the recorder does not), the write raced with `Task.WaitAsync(WriteTimeout, timeProvider, ct)` through a small internal helper #1704 can reuse (A2; `WriteTimeout` is configurable in `ABACOptions.DecisionAudit`, default 5 seconds, validated at start), idempotent retry check, exceptions through `ex.ForLogging()`. A unit test uses a store that ignores the token.
3. `DecisionAudit/ABACDecisionAuditEntryMapper.cs`: public static `ToOperationAuditEntry` (fills `StartedAtUtc`, `CompletedAtUtc` and `TimestampUtc` from the record's two `TimeProvider` reads, A9; stores `abac.identity_kind`, A5); string-only metadata; every reason code comes from the `ABACErrors.*Code` constants (never a literal; post-#1984 values); the per-column rule of Design 4 for **every** bounded column (hash for ids and tenant where the 71-character hash fits, `null` for an over-limit or unparseable `IpAddress`, truncation for `UserAgent`, capped metadata), with the markers `abac.hashed_fields`, `abac.dropped_fields`, `abac.truncated_fields` and `abac.stage=pep`; source-generated `ABACDecisionAuditJsonContext` for the trace JSON; IP and user agent from the ambient metadata keys used by `DefaultOperationAuditEntryFactory.cs:249-258` (`Encina.Audit.IpAddress`, `Encina.Audit.UserAgent`).
4. `DecisionAudit/IABACDecisionAuditReader.cs` + `ABACDecisionAuditReader` (scoped; ctor takes `IServiceProvider`, never `IOperationAuditStore`, and resolves the store inside each call, `Left(DecisionAuditStoreUnavailable)` when missing), `ABACDecisionAuditQuery`, `ABACDecisionAuditRecord`; `QueryAsync` and `ExportAsync` (JSON Lines). Tenant gate: the ambient tenant (`IRequestContext.TenantId`) is always forced when present; when none is present the reader returns `Left` (`encina.authorization.*` code for `tenant_required`, 403, A4) if the core multi-tenancy marker is registered (checked with `IServiceProviderIsService`) and queries without a tenant filter if it is not; a mismatch also returns `Left` (`tenant_mismatch`, same family); `DecisionAudit.AllowCrossTenantQueries` (logged) is only the opt-out for operator tooling in a multi-tenant application. The marker (core `Encina`, name decided at implementation) is added by `Encina.Tenancy`'s registration (Design 5). Tests: one per case (ambient tenant forced, multi-tenant without tenant denies, single-tenant without marker queries unfiltered, opt-out, mismatch). The XML docs state that callers must authorize access to the reader.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT: Reuse Encina.Security.Audit.IOperationAuditStore (RecordAsync, QueryAsync(OperationAuditQuery)). EF Core and Marten register it scoped (and, since #1633, ADO x3, Dapper x3 and MongoDB); the recorder is a singleton.
TASK: Implement the recorder, mapper, reader and export from the plan. Entry mapping: Action="ABACDecision", EntityType=request type, EntityId=resource id (explicit ids only, empty when undeclared), UserId=subject, ErrorMessage=reason code only, Metadata string values only with keys abac.* (including abac.identity_kind).
KEY RULES: never store EncinaError.Message or DecisionStatus.StatusMessage; apply the per-column over-limit rule of Design 4 (hash only where sha256:<64 hex> fits, null IpAddress, truncated UserAgent) instead of failing; StartedAtUtc at decision start, CompletedAtUtc and TimestampUtc from one TimeProvider read at the end; the audit write is never linked to the client token, is raced with WaitAsync and a configurable timeout (default 5 seconds); async with CancellationToken; reader always forces the ambient tenant when present and returns Left (encina.authorization.* codes, 403) on tenant mismatch, on a missing tenant when the core multi-tenancy marker (added by Encina.Tenancy's registration, checked with IServiceProviderIsService) is registered (without the marker the application is single-tenant and not gated), or on invalid paging. Add the marker type to core Encina and its registration to Encina.Tenancy.
REFERENCE FILES: src/Encina.Security.Audit/OperationAuditEntry.cs, OperationAuditQuery.cs, Encina.Compliance.NIS2/NIS2CompliancePipelineBehavior.cs:275-333, Encina.Security.ABAC/Administration/PersistentPolicyAdministrationPoint.cs:587-664.
```

</details>

---

### Phase 4: Configuration, DI and Startup Checks

> **Goal**: Opt-in registration that cannot silently skip the gate or the audit.

<details>
<summary>Tasks</summary>

1. `ABACOptions.cs`: `DecisionAudit`, `AuditDecisions(Action<ABACDecisionAuditOptions>?)`; `ABACDecisionAuditOptions` (including `WriteTimeout`, default 5 seconds, validated at start), `ABACDecisionAuditOutcomes`, `ABACDecisionAuditFailureMode`.
2. `ServiceCollectionExtensions.cs`: Always registered (the PEP requires them for every closed request type, and the gate is evaluated per request from the resolved `IOptions<ABACOptions>`, not from the temporary options instance, still how feature gates are read at registration: re-verify when editing; registrations are at `:118-236`). Nothing registered here depends on `IOperationAuditStore` at construction (Design 5): `TryAddSingleton(TimeProvider.System)`; `TryAddSingleton<IABACDecisionRecorder, AuditStoreABACDecisionRecorder>` (resolves the store per write); scoped `IABACDecisionAuditReader` (takes `IServiceProvider`, resolves `IOperationAuditStore` inside each call, `Left(DecisionAuditStoreUnavailable)` when missing); `TryAddEnumerable<IValidateOptions<ABACOptions>, ABACOptionsValidator>` + `AddOptions<ABACOptions>().ValidateOnStart()`. The startup check (task 4) is added as a hosted service always and no-ops when `DecisionAudit.Enabled` is false. The registration of the PEP and of `Encina.Security` behaviors is the one #1635 left (PR #1781, merged: both use `TryAddEnumerable`; #1705 Phase 4 kept it and added `AddEncinaRequestIdentity()` and the 9085 startup check to `AddEncinaABAC`); this plan changes nothing in how `AddEncinaSecurity` registers its behavior (task 5 is documentation only).
3. (Task intentionally absent: the persistent-PAP fix is #1677.)
4. `ABACDecisionAuditStartupCheck` (`IHostedService`, always registered, returns immediately when `DecisionAudit.Enabled` is false and never resolves `IOperationAuditStore` then): when enabled, scope-resolves `IOperationAuditStore`; Critical 9087 and throw when missing; Warning 9086 when `InMemoryOperationAuditStore`; Warning 9084 for `BestEffort`.
5. Pipeline order (maintainer decision A6, 2026-10-09, replacing the 2026-10-03 warning): no 9090 marker and no startup warning. Document that `AddEncinaSecurity` is registered before `AddEncinaABAC`; the execution-order contract is #1783 (p0, #1678 merged into it). The DI test pins the resulting pipeline order for both registration orders.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT: AddEncinaABAC (src/Encina.Security.ABAC/ServiceCollectionExtensions.cs:118-236) reads feature gates from a temporary options instance and registers with TryAdd.
TASK: Add the DecisionAudit options (WriteTimeout default 5 seconds, validated at start), validator, registrations and startup check per the plan; document that AddEncinaSecurity is registered before AddEncinaABAC (no order warning, #1783 owns the contract). The PAP scope/time fix (#1677), the behavior registration collision (#1635, PR #1781) and the PEP on the request identity (#1705 Phase 4, which also allocated 9085 in ABACLogMessages.EnforcementDisabled: reuse that method) are merged. Do not register an IOperationAuditStore from ABAC (the app or a provider package chooses it). No service registered by AddEncinaABAC may take IOperationAuditStore in its constructor: the recorder and the reader resolve it lazily per call (reader returns Left(DecisionAuditStoreUnavailable) when missing). With DecisionAudit.Enabled false and no IOperationAuditStore registered the container must still build under ValidateOnBuild + ValidateScopes; with Enabled true and no store the validator/startup check fails at startup, never a silent no-op.
KEY RULES: AGENTS.md section 3 registration completeness and ValidateOnBuild + ValidateScopes DI test (both registration orders of AddEncinaSecurity and AddEncinaABAC, audit enabled with a scoped IOperationAuditStore, audit disabled WITH and WITHOUT any IOperationAuditStore, enabled without a store fails, and a CLOSED IPipelineBehavior<TRequest,TResponse> resolved from a scope because ValidateOnBuild skips open generics; follow tests/Encina.UnitTests/Core/AddEncinaServiceGraphTests.cs); the recorder, TimeProvider and options validator are always registered, the feature gate is the resolved IOptions<ABACOptions>, never the temporary options instance; the pipeline order is pinned for both registration orders (no order warning exists).
REFERENCE FILES: ServiceCollectionExtensions.cs, ABACOptions.cs, PolicyCachingOptions.cs, EELExpressionPrecompilationService.cs, the DI test added by #1634 (ABACRegistrationTests).
```

</details>

---

### Phase 5: Cross-Cutting Integration

> **Goal**: Everything marked integrate in the matrix below is wired.

<details>
<summary>Tasks</summary>

1. Multi-tenancy: `TenantId` on every entry; reader tenant guard (ambient tenant always forced; `tenant_required` when the core marker is registered and no tenant resolves; store-level filtering remains #798). Add the public marker type to core `Encina` and have `AddEncinaTenancy` (`Encina.Tenancy`) register it.
2. Module isolation: `abac.module_id` metadata from `context.GetModuleName()`.
3. Health: `ABACHealthCheck` (`Health/ABACHealthCheck.cs:36-124`) reports decision-audit state: Unhealthy under `FailClosed` after a failed write, Degraded under `BestEffort` or `InMemoryOperationAuditStore` (the check already takes `IServiceProvider`, `Health/ABACHealthCheck.cs:56`, so it resolves the store lazily inside the check and only when audit is enabled); `ABACDecisionAuditHealthState` singleton. No write probe, so the state resets on the next successful write (a success clears the failure flag; a failure older than `HealthFailureWindow`, 5 minutes by default, reads as Degraded rather than Unhealthy). `ABACHealthCheck` is registered only when `AddHealthCheck` is true (`ServiceCollectionExtensions.cs:188-193`, default false, `ABACOptions.cs:100`): without it the failure is visible only in logs (9080-9083) and the `abac.decision_audit.failed` counter. Documented.
4. Validation: options validator and reader query validation (Phase 3/4).
5. Transactions: isolation by scope + `TransactionScope(Suppress)` (Phase 3).
6. Resilience: `WriteTimeout` (configurable, default 5 seconds, enforced by the `WaitAsync` race) only; no retries on the request path.
7. Idempotency: `DecisionId` as `OperationAuditEntry.Id` with duplicate check.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT: Phases 1-4 are done. Wire the cross-cutting functions marked integrate in the plan's matrix (multi-tenancy, module id, health, validation, transactions, resilience timeout, idempotency).
TASK: Extend ABACHealthCheck with a decision-audit step driven by ABACDecisionAuditHealthState; confirm tenant and module propagation end to end; verify the recorder's isolation with an EFCore-SqlServer integration test where a deny row survives an outer TransactionPipelineBehavior rollback.
KEY RULES: health check keeps DefaultName/Tags/CreateScope pattern; no write probe; no subject/tenant identifiers in health data.
REFERENCE FILES: Health/ABACHealthCheck.cs, Encina.EntityFrameworkCore/TransactionPipelineBehavior.cs:113-126, src/Encina/Modules/Isolation/IModuleExecutionContext.cs.
```

</details>

---

### Phase 6: Observability

> **Goal**: Logs, metrics and a span for the audit path.

<details>
<summary>Tasks</summary>

1. `Diagnostics/ABACLogMessages.cs`: EventIds 9079-9084 and 9086-9089 (table in Research; 9072-9078 are used by #1634, 9085 and 9091-9097 by #1705 Phase 4 and later work). Codes and exception types only.
2. `Diagnostics/ABACDiagnostics.cs` (existing `ActivitySource`/`Meter` "Encina.Security.ABAC"): counters `abac.decision_audit.recorded` (outcome, enforcement_mode), `abac.decision_audit.failed` (failure_mode, error.type); histogram `abac.decision_audit.duration`; span `ABAC.DecisionAudit.Record` as child of `ABAC.Evaluate`; tag `abac.decision_id` on `ABAC.Evaluate`.
3. The policy-cache hit/miss counter (`abac.policy_cache.lookups`) is **not** part of this plan: it is #1640.
4. No subject, resource, tenant or attribute value in tags or logs (SPEC-002 REQ-062).

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT: EventIdRanges.SecurityABAC = (9000, 9099) is registered (src/Encina/Diagnostics/EventIdRanges.cs:352) and mapped in tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs:106. Used ids include 9072-9078 (#1634), 9085 and 9091-9097 (re-check the highest id in SecurityABAC when implementing; skip ids taken by #1676, #1677 and #1635 in the meantime).
TASK: Add EventIds 9079-9084 and 9086-9089 (9085 exists, 9090 is not used) packed sequentially in ABACLogMessages with [LoggerMessage] and XML docs naming the range; add the metrics and the span (the policy-cache lookup counter is #1640, do not add it).
KEY RULES: AGENTS.md section 7; never log EncinaError.Message; exceptions via ForLogging(); run the EventId architecture tests; new PublicAPI lines only if fields are public.
REFERENCE FILES: Diagnostics/ABACLogMessages.cs, Diagnostics/ABACDiagnostics.cs:11-229.
```

</details>

---

### Phase 7: Testing

> **Goal**: Every applicable coverage flag reaches its target in `.github/coverage-manifest/Encina.Security.ABAC.json` (unit 70, guard 20, contract 15, property 15).

<details>
<summary>Tasks</summary>

1. **Unit** (`tests/Encina.UnitTests/Security/ABAC/`): PEP outcome matrix (every row of the decision-path table: Permit, obligation override, Deny, `encina.authorization.abac_policy_not_found`, `encina.authorization.abac_condition_not_met`, Indeterminate in every mode, exception, missing context; x Block/Warn x FailClosed/BestEffort x store Right/Left/throw/timeout); record written before `next`; handler exception propagates unchanged; disabled audit costs nothing; mapper (fields, hashing, no `StatusMessage`); recorder (child scope, suppression, duplicate check); PDP trace and `RuleId`; reader tenant forcing and export; options validator, startup check, health check; the audit write against a store that ignores the token (`WaitAsync` race, A2); logs and meters via `MeterListener`. Pattern: `ABACPipelineBehaviorTests.cs` and `ABACRequirementEnforcementTests.cs` (as rewritten by #1634).
2. **DI test** (new `ServiceCollectionExtensionsDecisionAuditTests`): `ValidateOnBuild` + `ValidateScopes`, scoped `IOperationAuditStore`, **both** orders of `AddEncinaSecurity` and `AddEncinaABAC` (both `SecurityPipelineBehavior` and `ABACPipelineBehavior` must be present in the closed pipeline, which #1635 guarantees; no warning exists for the opposite order, A6), audit enabled through the configure lambda, through `IConfiguration` binding and through a later `Configure<ABACOptions>`, audit disabled **with a scoped `IOperationAuditStore` and with no `IOperationAuditStore` registered at all** (the container must build and the closed behavior and the scoped reader must resolve; the reader then returns `Left(DecisionAuditStoreUnavailable)`), audit enabled without a store fails at startup (the test resolves `IOptions<ABACOptions>.Value`, which runs the validator, and calls `ABACDecisionAuditStartupCheck.StartAsync`, because a plain `BuildServiceProvider` runs neither `ValidateOnStart` nor hosted services), and the pipeline order for both registration orders is asserted (Design 3, "Position in the pipeline"). Because `ValidateOnBuild` skips open-generic descriptors, the test also resolves a **closed** `IPipelineBehavior<TRequest,TResponse>` (a request carrying `[RequirePolicy]`) from a scope, following `tests/Encina.UnitTests/Core/AddEncinaServiceGraphTests.cs:32,323`; without that the PEP's new dependencies would never be proven. #1634 added a first DI test (`ABACRegistrationTests`); extend it instead of duplicating it.
3. **Guard**: new constructors and public methods (`tests/Encina.GuardTests/Security/ABAC/`). Existing tests that construct the PEP directly must be updated for the new constructor: `ABACPipelineBehaviorTests` (8 sites), `tests/Encina.GuardTests/Security/ABAC/ABACPipelineBehaviorGuardTests.cs` (7) and `tests/Encina.ContractTests/Security/ABAC/ABACPipelineBehaviorContractTests.cs` (1). (Site counts are from before #1634 rewrote these files: recount after it merges.) `RequestMetadataExtractor` stays internal (A8), so its tests are untouched; the resource-id tests cover `IABACResourceIdentity`, the `resourceId` attribute and the empty `EntityId` of an undeclared resource. The tenant-gate cases, one test each (ambient tenant present is forced; marker registered with no tenant resolved denies with `tenant_required`; mismatch; opt-out for operator tooling; no marker, single-tenant application, queries without configuration and without a tenant; plus a test in `Encina.Tenancy` that `AddEncinaTenancy` registers the marker) and the `UserAgent` (truncation) and `IpAddress` (over-length and unparseable become null) over-limit cases are explicit unit tests, as is the reader normalizing its `UserId`, `EntityType`, `EntityId` and tenant filters with the mapper's rule (an over-limit subject stored as `sha256:...` must still be found).
4. **Contract**: `IABACDecisionRecorder` over `InMemoryOperationAuditStore`; exactly one record per evaluated request; fail-closed contract (pattern `tests/Encina.ContractTests/Security/ABAC/XACMLPolicyDecisionPointContractTests.cs`).
5. **Property (FsCheck)**: random `StatusMessage` never appears in an entry; values over each column limit follow the per-column rule of Design 4 (ids and tenant hashed to 71 characters, `IpAddress` null, `UserAgent` truncated, metadata capped), never raise, and markers list exactly the modified fields; outcome mapping is total; `Proceed` only for Granted and Warn pass-through; entry-to-record round trip loses nothing.
6. **Integration** (shared `[Collection]` fixtures, `ClearAllDataAsync` in `InitializeAsync`): recorder-to-reader round trip on ADO x3, Dapper x3, EF Core x3, MongoDB, plus Marten (outside the 10-provider rule); stores built directly as `tests/Encina.IntegrationTests/Security/Audit/OperationAuditStoreADOSqlServerIntegrationTests.cs` does. The 7 non-EF providers rely on #1633 (merged: store registration and `OperationAuditEntries` DDL). Never a `.md` justification for these. Marten tests wait for the async projection daemon before reading (`MartenOperationAuditStore.cs:149`).
7. **Load**: concurrent evaluations with audit on (denials under store failure). **Benchmark**: PEP with audit off and on, PDP with and without trace; results to open #924.
8. Run the local CRAP gate table for changed methods before reporting.

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```text
CONTEXT: Targets per flag come from .github/coverage-manifest/Encina.Security.ABAC.json (unit 70, guard 20, contract 15, property 15); new source files are added with `dotnet run --file .github/scripts/generate-coverage-manifest.cs` (append-only by default; `--dry-run` previews; mechanical-fixer).
TASK: Write the tests listed in the plan, executing real package code (no reflection-only tests). Use Shouldly via Encina.Testing.Shouldly, NSubstitute, FsCheck wrappers, FakeTimeProvider.
KEY RULES: AGENTS.md section 9 (AAA, deterministic, no Thread.Sleep, builders), integration via [Collection("ADO-SqlServer")] etc., never IClassFixture for DB fixtures, outputs under artifacts/.
REFERENCE FILES: tests/Encina.UnitTests/Security/ABAC/ABACPipelineBehaviorTests.cs:157-455, tests/Encina.ContractTests/Security/ABAC/, tests/Encina.IntegrationTests/Security/Audit/.
```

</details>

---

### Phase 8: Documentation and Finalization (always last)

> **Goal**: Docs, ADR, changelog, PublicAPI and manifest.

<details>
<summary>Tasks</summary>

1. XML docs on all new public APIs. Also update the XML docs of `AddEncinaServiceIdentity` (`src/Encina/Identity/RequestIdentityServiceCollectionExtensions.cs:99`) with the naming rule of A7: a service name describes a function, never a person, and is never assigned to one person.
2. `src/Encina.Security.ABAC/README.md`: replace the obligation-based audit recipe (`README.md:151`, `CanHandle(...) => obligationId == "audit-access"`) with the decision audit (README `:115` and `:180` already mention the caller denial and the EventId reservation: update them); `docs/features/security-authorization.md`, the existing ABAC reference pages `docs/features/abac/reference/configuration.md` (options), `observability.md` (metrics, EventIds) and `errors.md` (error codes), and a new page under `docs/features/abac/` (via `docs-writer`): the documented order (`AddEncinaSecurity` before `AddEncinaABAC`, with no startup warning and the pointer to the execution-order contract #1783; the ABAC record is the ABAC decision, not the final outcome; also in the ABAC README and `security-authorization.md`), Warn-mode semantics (`Success` means ABAC let it through), no row means "not evaluated", direct-PDP callers not audited, obligation side effects when the write fails, how to declare the resource id (`IABACResourceIdentity` or the `resourceId` attribute; an undeclared resource leaves `EntityId` empty), double rows with `AuditPipelineBehavior` (joined by `CorrelationId`), plaintext resource ids outside Marten, retention (`RetentionDays` 2555, `EnableAutoPurge` false), and the service-identity risk line below.
   - **Service identities in the docs risk list (A7, 2026-10-09; research `artifacts/research/service-identity-personal-data.md`)**: a `service:<name>` id is not personal data by itself and is stored in clear without the personal-data regime; a user id keeps that regime; the whole row is still health data for retention and access; never name a service after a person (`AddEncinaServiceIdentity` documents that a service name describes a function and is never assigned to one person); the record carries the single request identity, and a second identity is recorded once an on-behalf concept exists (Encina has none today); the actor kind is explicit (`abac.identity_kind`).
3. ADR "ABAC decisions are audited write-ahead at the enforcement point, fail-closed", numbered **ADR-047**, reserved for #751 in the "Reserved numbers" table of `docs/architecture/adr/index.md` by the plan-update PR (maintainer decision 11, 2026-10-09); reusable by #750, #752 and REQ-051. The ADR text says "operation audit store".
4. `changelog.d/751-abac-decision-audit.added.md` (the new capability). No `security` fragment from this plan: the fixes that needed one (missing context denial, registration collision, message removal) belong to #1676, #1635, #1591 and their own fragments.
5. `PublicAPI.Unshipped.txt` for every public symbol (below); `.github/coverage-manifest/Encina.Security.ABAC.json` via `--append-only` (no `Encina.Security.Audit` manifest change: `AuditRequestConventions` is not added, A8).
6. `docs/INVENTORY.md`, `ROADMAP.md`, `docs/releases/` if applicable; ABAC plan 401 cross-link.
7. `dotnet build -c Release` 0 warnings; `dotnet test`; coverage per flag; `changelog-fragments.cs -- --check`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 8</summary>

```text
CONTEXT: Phases 1-7 are done. Finish docs and bookkeeping for #751.
TASK: Hand documentation to docs-writer, changelog fragments, PublicAPI lines and coverage manifest entries to mechanical-fixer; write the ADR (ADR-047, reserved for #751 in docs/architecture/adr/index.md) following docs/architecture/adr/031-retention-erasure-port.md.
KEY RULES: never edit CHANGELOG.md; documentation never types coverage figures by hand (covref markers); English only; zero warnings.
REFERENCE FILES: changelog.d/README.md, AGENTS.md sections 8 and 11, docs/plans/evidential-read-audit-implementation-plan-1193.md (current plan shape).
```

</details>

---

## Research

### Standards and specifications

| Standard | Relevance |
|----------|-----------|
| GDPR Art. 32 (security of processing) | Access control with traceable decisions |
| SPEC-002 REQ-007 (related), REQ-051, REQ-061, REQ-062, REQ-019/DEC-006, INV-005 | Evidential audit, break-glass reuse, tenant awareness, no identifiers in telemetry, fail closed |
| Código Deontológico art. 46 (health reference app) | Records secure against outsiders: access control, audit |
| NIS2 Art. 21(2)(f)(i) | Audit trail and access control building blocks (framework role) |
| OASIS XACML 3.0 (ADR-015) | PEP/PDP model, obligations, Indeterminate semantics |
| ADR-018, ADR-021, ADR-001/006 | Cross-cutting check, EventIds, ROP |

### Existing Encina infrastructure to leverage

| Component | Location | Usage |
|-----------|----------|-------|
| `IOperationAuditStore`, `OperationAuditEntry`, `OperationAuditQuery`, `AuditOutcome` | `src/Encina.Security.Audit/` | Persistence and query |
| `OperationAuditStoreADO/Dapper/EF/MongoDB`, `MartenOperationAuditStore`, `InMemoryOperationAuditStore`, `InstrumentedOperationAuditStore` | provider packages, `Encina.Audit.Marten`, `Encina.OpenTelemetry/Audit` | Storage on all providers |
| `ABACPipelineBehavior`, `XACMLPolicyDecisionPoint`, `ObligationExecutor` | `src/Encina.Security.ABAC/` | PEP, PDP, obligations |
| `ABACDiagnostics`, `ABACLogMessages`, `ABACHealthCheck` | `src/Encina.Security.ABAC/Diagnostics`, `Health` | Observability |
| `NIS2CompliancePipelineBehavior` (audit part) | `src/Encina.Compliance.NIS2/` | Precedent for a decision record |
| `PersistentPolicyAdministrationPoint` audit | `src/Encina.Security.ABAC/Administration/` | Existing PAP audit of policy changes; its defects are #1677 |
| `IRequestContext` (`Identity`: subject and `IdentityKind`, since #1705), `IModuleExecutionContext` | core | Subject, tenant, module |
| `RequestMetadataExtractor` | `src/Encina.Security.Audit/` | Stays internal; ABAC does not use it (explicit resource ids only) |

### Event ID allocation

Range `SecurityABAC = (9000, 9099)` (`src/Encina/Diagnostics/EventIdRanges.cs:352`) is already registered and mapped; used 9000-9015, 9020-9022, 9030-9040, 9050-9078 (9072-9078 are taken by #1634, PR #1650: required-policy lookup, condition and obligation-handler events). Since the plan was written 9085 (`EnforcementDisabled`, #1705 Phase 4) and 9091-9097 are also used (9091-9093 unauthenticated caller, PAP retrieval, store error; 9094-9097 PAP and the seeding summary 9096). Free in this plan's block: 9079-9084 and 9086-9089 (`EventIdRanges.cs:350` still carries "Reserved: 9079-9090 for #751"; shrink that reservation to match when implementing). 9085 is not a #751 id, and 9090 is no longer used (A6). The missing-context event of the earlier draft is gone (#1676 allocates its own id). Before implementing, re-check the highest id in `ABACLogMessages` and `EventIdRanges.cs`. No new range needed.

| EventId | Level | Meaning |
|---------|-------|---------|
| 9079 | Debug | Decision recorded |
| 9080 | Error | Audit failed, access denied (FailClosed) |
| 9081 | Warning | Audit failed, proceeding (BestEffort) |
| 9082 | Error | Audit of an already denied request failed |
| 9083 | Error | Recorder threw (redacted exception) |
| 9084 | Warning | BestEffort configured (startup) |
| 9085 | Warning | Not a #751 id: `ABACLogMessages.EnforcementDisabled`, logged once at startup by `ABACEnforcementModeStartupCheck` since #1705 Phase 4; #751 adds no per-request call (A1) |
| 9086 | Warning | Non-durable `InMemoryOperationAuditStore` |
| 9087 | Critical | `IOperationAuditStore` not registered |
| 9088 | Debug | Trace truncated at `MaxTraceEntries` |
| 9089 | Debug | Duplicate decision id detected (idempotent retry) |
| 9090 | n/a | Not used (the pipeline-order warning was dropped, A6) |

### Estimated file count

| Category | New | Modified |
|----------|-----|----------|
| Model, trace, errors, options | 7 | 6 (`PolicyDecision`, `PolicyEvaluationContext`, `ABACOptions`, `ABACErrors`, PDP, `ABACRequirementEvaluator`) |
| Recorder, mapper, reader, export, startup check, validator | 8 | 0 |
| PEP, DI, health, diagnostics, logs | 1 (health state) | 5 |
| `Encina.Security.Audit` (doc fix only) | 0 | 2 |
| Multi-tenancy marker: type in core `Encina` (new), registration in `Encina.Tenancy` `AddEncinaTenancy` (modified) | 1 | 1 |
| **Production total** | **17** | **14** |
| Tests (unit, DI, guard, contract, property, integration, load, benchmark) | ~25 | ~3 (PEP tests in unit, guard and contract; recount at implementation) |
| Docs, ADR, changelog, PublicAPI, manifest | 4 | ~6 |

### Public API changes (`src/Encina.Security.ABAC/PublicAPI.Unshipped.txt`)

- `ABACOptions.DecisionAudit`, `ABACOptions.AuditDecisions(...)`, `ABACDecisionAuditOptions` (with `WriteTimeout`), `ABACDecisionAuditOutcomes`, `ABACDecisionAuditFailureMode`.
- `PolicyEvaluationContext.IncludeEvaluationTrace`, `PolicyDecision.EvaluatedPolicies`, `PolicyEvaluationTrace`, `PolicyTraceReason`.
- Namespace `Encina.Security.ABAC.DecisionAudit`: `ABACDecisionRecord`, `ABACEnforcedOutcome`, `IABACResourceIdentity`, `IABACDecisionRecorder`, `AuditStoreABACDecisionRecorder`, `ABACDecisionAuditEntryMapper`, `ABACDecisionAuditSchema`, `IABACDecisionAuditReader`, `ABACDecisionAuditQuery`, `ABACDecisionAuditRecord`.
- `ABACErrors.DecisionAuditFailedCode`, `DecisionAuditFailed`, `InvalidDecisionAuditQueryCode`, `InvalidDecisionAuditQuery`, `DecisionAuditStoreUnavailableCode`, `DecisionAuditStoreUnavailable`.
- `ABACDecisionAuditOptions` also exposes `AllowCrossTenantQueries` (false) and `HealthFailureWindow` (5 minutes).
- Breaking (pre-1.0, acceptable): `ABACPipelineBehavior` constructor gains `IABACDecisionRecorder` and `TimeProvider` (it already takes `IOptions<ABACOptions>`, and, after #1634, an `EELCompiler`).
- `Encina.Security.Audit`: no public API change (`AuditRequestConventions` is not added, A8).
- Core `Encina`: the public multi-tenancy marker type (name decided at implementation, for example `MultiTenancyMarker`; `PublicAPI.Unshipped.txt` of `Encina`); `Encina.Tenancy` only registers it (no new public symbol there unless the registration needs one).
- Internal: `ABACEnforcementVerdict`, `ABACOptionsValidator`, `ABACDecisionAuditStartupCheck`, `ABACDecisionAuditHealthState`, `ABACDecisionAuditJsonContext`.

### Provider matrix (AGENTS.md section 5)

| Provider | Store used | Status |
|----------|-----------|--------|
| ADO.NET SqlServer / PostgreSQL / MySQL | `OperationAuditStoreADO` | Registered and `OperationAuditEntries` DDL created by #1633 (merged; ADO `ServiceCollectionExtensions.cs:617,620`) |
| Dapper SqlServer / PostgreSQL / MySQL | `OperationAuditStoreDapper` | Same as ADO (#1633, merged) |
| EF Core SqlServer / PostgreSQL / MySQL | `OperationAuditStoreEF` (shared) | Registers when `UseOperationAuditStore` is set (`ServiceCollectionExtensions.cs:418`) |
| MongoDB | `OperationAuditStoreMongoDB` | Registered by #1633 (merged; `ServiceCollectionExtensions.cs:657`) |
| Marten (outside the 10) | `MartenOperationAuditStore` | Registers; encrypts `UserId` and `Metadata`, not `EntityId` |
| InMemory | `InMemoryOperationAuditStore` | Default; non-durable warning |

Caching (8), transports, locks, validation providers, cloud: not applicable. The decision cache is deliberately absent: a cached verdict would make the record describe stale attributes.

### Test matrix and coverage targets

| Flag | Target (manifest) | Plan |
|------|-------------------|------|
| Unit | 70 | Phase 7 item 1-2 |
| Guard | 20 | New constructors and public methods |
| Contract | 15 | Recorder and fail-closed contracts |
| Property | 15 | FsCheck invariants above |
| Integration | provider round trips | 10 database providers + Marten, shared collections |
| Load / Benchmark | implement (concurrent hot path) | Load: concurrent decisions; benchmark audit off/on (#924) |

### What PR #1650 (#1634) changed in the PEP

Read from `origin/fix/abac-require-policy-1634` at head `7c092bf2` on 2026-10-03; the PR merged that day. Historical section: the denial codes shown are the post-#1984 ones (renamed by #1984). The plan is written against this shape:

- `[RequirePolicy]` evaluates only top-level policy sets and standalone policies by id through `IPolicyDecisionPoint.EvaluatePolicyAsync`, with AND/OR groups (`ABACRequirementEvaluator`, `ABACRequirementCombiner`); an unknown id is `encina.authorization.abac_policy_not_found`.
- `[RequireCondition]` is evaluated per request through the EEL compiler; a false condition is `encina.authorization.abac_condition_not_met`.
- `ABACOptions.DefaultNotApplicableEffect` and `ABACOptions.FailOnMissingObligationHandler` are removed at `7c092bf2`, so the NotApplicable-default rows of the earlier decision-path table are gone.
- Indeterminate denies in every enforcement mode at `7c092bf2` (Warn relaxes only definite verdicts, including `encina.authorization.abac_policy_not_found`); see the note under the decision-path table.
- `ObligationExecutor` catches handler exceptions (`abac.obligation_handler_exception`); the PEP overrides a Permit to Deny when a mandatory obligation fails.
- EventIds 9072-9078 are used; this plan's block is 9079-9084 and 9086-9089 (see "Event ID allocation").
- Resource attributes receive the request (the old `default!` is fixed).

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
PROJECT CONTEXT:
Encina is pre-1.0, .NET 10 / C# 14, nullable enabled, ROP (Either<EncinaError,T>). Rules are in AGENTS.md; read it first. Package: src/Encina.Security.ABAC. Issue #751: opt-in, queryable audit trail of ABAC decisions.

IMPLEMENTATION OVERVIEW:
All hard prerequisites are merged (#1674, #1633 which registered the operation-audit store on every provider, #1634, #1676, #1635, #1705 Phase 4). Related, non-blocking: #1591, #1685, #1686. Order: #1984 first, then #751 phase by phase; #1700 and #1993 after Phase 1; #1783 in parallel.
Phase 1: trace model (PolicyEvaluationTrace, PolicyDecision.EvaluatedPolicies/RuleId, PolicyEvaluationContext.IncludeEvaluationTrace), ABACDecisionRecord, errors.
Phase 2: refactor ABACPipelineBehavior into decide -> record -> enforce (no per-request 9085 warning).
Phase 3: AuditStoreABACDecisionRecorder (own scope, TransactionScope Suppress, write never linked to the client token, WaitAsync race with a configurable timeout, idempotent), mapper to OperationAuditEntry, typed reader (tenant gate only when multi-tenancy is enabled) and JSON Lines export.
Phase 4: ABACOptions.DecisionAudit, validator + ValidateOnStart, startup check; document AddEncinaSecurity before AddEncinaABAC (no order warning; #1783 owns the contract).
Phase 5: tenant, module, health, validation, transactions, idempotency.
Phase 6: EventIds 9079-9084 and 9086-9089 (re-check the highest id in use), metrics, span (policy-cache lookup counter is #1640, not here).
Phase 7: tests per flag. Phase 8: docs (docs-writer), ADR-047 (reserved), changelog fragment and PublicAPI (mechanical-fixer).

KEY PATTERNS:
- Fail closed: a write failure denies a request that would proceed (FailureMode FailClosed); BestEffort is an explicit, logged opt-out.
- Codes only: never EncinaError.Message, StatusMessage or exception messages in logs, tags or stored entries.
- TimeProvider everywhere; Guid.CreateVersion7 for DecisionId.
- Opt-in: when DecisionAudit.Enabled is false nothing is built per request and no IOperationAuditStore is resolved; the idle recorder, the lazily-resolving reader, the validator and the no-op startup check are always registered, and nothing AddEncinaABAC registers takes IOperationAuditStore in its constructor (an app with audit off and no IOperationAuditStore must pass ValidateOnBuild/ValidateScopes).
- Registration completeness: ValidateOnBuild + ValidateScopes DI test.
- CRAP <= 10 on changed methods; extract helpers.

REFERENCE FILES:
src/Encina.Security.ABAC/ABACPipelineBehavior.cs, ServiceCollectionExtensions.cs, ABACOptions.cs, Enforcement/ABACRequirementEvaluator.cs, Evaluation/XACMLPolicyDecisionPoint.cs, Diagnostics/ABACLogMessages.cs, src/Encina.Security.Audit/OperationAuditEntry.cs, OperationAuditQuery.cs, Abstractions/IOperationAuditStore.cs, src/Encina.Compliance.NIS2/NIS2CompliancePipelineBehavior.cs:275-333, docs/plans/evidential-read-audit-implementation-plan-1193.md.
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ⏭️ | Decisions are not cached (stale-attribute evidence). The policy-cache hit/miss metric is deferred to #1640 (no change to `CachingPolicyStoreDecorator` in this plan). |
| 2 | OpenTelemetry | ✅ | Span `ABAC.DecisionAudit.Record`, counters and histogram on the existing "Encina.Security.ABAC" source/meter; no identifiers in tags (REQ-062). `Encina.OpenTelemetry` does not subscribe to that source today: deferred as `[DEBT]`. |
| 3 | Structured Logging | ✅ | EventIds 9079-9084 and 9086-9089 in `ABACLogMessages`, codes only; the existing sites that log messages are #1591 and #1685. |
| 4 | Health Checks | ✅ | `ABACHealthCheck` reports decision-audit state (Phase 5); no write probe. |
| 5 | Validation | ✅ | `ABACOptionsValidator` with `ValidateOnStart`; reader query validation. |
| 6 | Resilience | ✅ (timeout only) | `WriteTimeout` (configurable, default 5 seconds, `WaitAsync` race); retries are not applicable on the request path (latency, duplicate rows); store resilience belongs to providers. |
| 7 | Distributed Locks | ❌ | Append-only inserts with unique v7 ids; no shared mutable state. |
| 8 | Transactions | ✅ | By isolation: own scope + `TransactionScope(Suppress)`, so business rollback never removes a deny row. |
| 9 | Idempotency | ✅ | `DecisionId` = `OperationAuditEntry.Id`, duplicate check on an ambiguous timeout/`Left` after a committed write (no retries exist; best-effort on Marten's async projection). Each evaluation is otherwise a distinct event and must be recorded. |
| 10 | Multi-Tenancy | ✅ | `TenantId` on every entry, reader tenant gate (ambient tenant always forced; with the core marker registered by `Encina.Tenancy`, mismatch and missing tenant are denied and `AllowCrossTenantQueries` is only for operator tooling; without the marker, single-tenant applications are not gated); automatic store-level tenant filtering stays in #798 (v0.18.0). |
| 11 | Module Isolation | ⏭️ | `abac.module_id` in metadata now; a queryable `ModuleId` on `OperationAuditEntry`/`OperationAuditQuery` for all providers is deferred as `[FEATURE]` (related #753). |
| 12 | Audit Trail | ✅ | This is the feature. |

Deferred items and adjacent issues, all opened (#1633-#1640, #1642, #1674, #1676-#1678):

| Deferred item | Status |
|---|---|
| Provider registration and DDL | #1633, merged (ADO x3, Dapper x3 and MongoDB register `IOperationAuditStore`; `OperationAuditEntries` DDL) |
| Queryable `ModuleId` on audit entries | issue: "[FEATURE] Queryable ModuleId on OperationAuditEntry and OperationAuditQuery for all audit store providers" (#1636) |
| OTel subscription of the ABAC source and meter | issue: "[DEBT] Encina.OpenTelemetry does not subscribe to the Encina.Security.ABAC ActivitySource and Meter" (#1637) |
| Audit-stores coherence (names of the operation, entity-change and read-access audit stores; `IAnonymizationAuditStore` deleted) | issue: "[SPIKE] Coherent audit stores before 1.0: names, ADR and implementation order for operation, entity-change and read-access audit" (#1674; option A chosen 2026-10-03; closed, ADR-036 applied) |
| Repo-wide open-generic `TryAddTransient` collisions (includes ABAC and `Encina.Security`) | issue: "[BUG] Open-generic TryAddTransient of IPipelineBehavior silently skips behaviors registered after another one (about 20 packages)" (#1635; 21 sites in 20 files); fixed as its own issue now, no longer in this plan |
| Pipeline behavior order independent of registration order | #1783 (execution-order contract, p0; #1678 merged into it); this plan only documents the order, with no startup warning (A6) |
| Missing security context denies; partial policy retrieval fails closed | issue: "[BUG] ABAC PEP evaluates with an empty user id when the security context is missing, and the PDP decides on partial policies when standalone-policy retrieval fails" (#1676); no longer in this plan |
| Persistent PAP: captive scoped store, fire-and-forget audit, `DateTimeOffset.UtcNow` | issue: "[BUG] PersistentPolicyAdministrationPoint audits policy changes fire-and-forget through a captured scoped audit store and reads DateTimeOffset.UtcNow" (#1677, merged); no longer in this plan |
| Exception messages in activity tags and errors across packages | issue: "[DEBT] Exception messages still reach activity tags and EncinaErrors.Create/StoreError messages across packages" (#1591), plus #1685 (exception messages in the PEP activity status and `EvaluationFailed`; PR #1650 follow-up) |
| `EELCompiler` recompiling a malformed expression on every request | issue: "[DEBT] EELCompiler recompiles a malformed expression on every request under a global lock" (#1686) |
| `EvaluatePolicyAsync` loading the whole policy store per required policy (PAP by-id top-level lookup) | issue: "[DEBT] ABAC EvaluatePolicyAsync loads the whole policy store per required policy; add a by-id top-level lookup and a PAP contract test" (#1687) |
| ABAC docs examples matching `action.name` to verbs the PEP never produces | issue: "[DEBT] ABAC docs: policy examples match action.name to verbs like read that the PEP never produces" (#1688); related to the Phase 8 docs |
| `RequirePolicy.PolicyName`/`RequireCondition` ignored, `ObligationExecutor` uncaught handler exceptions, `default!` resource attributes | issue: "[BUG] ABAC ignores RequirePolicy.PolicyName and RequireCondition at request time, and ObligationExecutor lets handler exceptions escape" (#1634), PR #1650; merged before this plan is implemented |
| Audit of obligation overrides and direct PDP callers | issue: "[FEATURE] Audit ABAC obligation overrides and decisions of direct IPolicyDecisionPoint callers" (#1638) |
| Audit of reads of the decision-audit trail (relates to #1193) | issue: "[FEATURE] Audit reads of the ABAC decision-audit trail (reader and export)" (#1639) |
| Policy-cache hit/miss counter | issue: "[DEBT] CachingPolicyStoreDecorator exposes no cache hit/miss metric (abac.policy_cache.lookups)" (#1640) |
| `AuditPipelineBehavior` storing `error.Message`/`ex.Message` in `OperationAuditEntry.ErrorMessage` | no new issue: fixed by PR #1606 (#1557), merged into `main` |
| `AuditPipelineBehavior` outcome classification by words in `error.Message` (`MapErrorToOutcome`, `:194`) | #1642. **#751 does not depend on it**: the ABAC record's `AuditOutcome` comes from the PEP's own verdict (decision-path table), never from message text, so ABAC rows stay correct whether or not #1642 is fixed; only the sibling `AuditPipelineBehavior` rows can carry a wrong Denied/Failure outcome until then. |

---

## Prerequisites & Dependencies

### Risks

- **Availability and latency**: every protected request waits for one insert; with `FailClosed` an audit-store outage denies requests that would proceed. Mitigations: opt-in, `Outcomes` filter (for example Denied only), `WriteTimeout`, health check, benchmark under #924.
- **Store registration (history)**: #1633 (merged) registered the operation-audit store on ADO x3, Dapper x3 and MongoDB, so the integration tests on all 10 providers run in this PR. #1633 merged after the audit-stores spike #1674 (ADR-036), whose names this plan uses.
- **Behavior change riding along** (needs a test): handler exceptions are no longer reported as `abac.evaluation_failed`. The other behavior changes of the earlier draft (missing context denies, registration collision) belong to #1676 and #1635 with their own fragments.
- **Dependencies on separately fixed defects**: the hard prerequisites #1634 (PR #1650), #1676 (missing context and partial retrieval fail closed), #1635 (registration order) and #1633 (store registration) are all merged. #1677 (merged), #1591, #1685 and #1686 are related and do not block. The remaining order problem (ABAC registered before `Encina.Security`) is documented here and owned by #1783 (execution-order contract); this plan adds no warning.
- **Indeterminate and the removed options**: PR #1650 at head `7c092bf2` makes Indeterminate deny in every mode and removes `FailOnMissingObligationHandler` and `DefaultNotApplicableEffect` (see the note under the decision-path table). The PR is merged, so the table follows the merged behavior.
- **Reader and export expose access history**: not authorized by this feature (the application must gate them); reads of the trail are not audited (deferred to #1193 and #1639). The ambient tenant is always forced; `tenant_required` applies when the core marker (added by `Encina.Tenancy`'s registration) is present. In a single-tenant application (no marker) the reader queries without a tenant, which is correct there; enabling multi-tenancy later adds the marker and the reader then denies without a tenant. A multi-tenant application cannot forget an option, because the signal comes from the Tenancy registration (Design 5).
- **Personal data**: ids, `IpAddress` and `UserAgent` are plaintext on relational and MongoDB stores; Marten encrypts `UserId`, `IpAddress`, `UserAgent`, payloads and `Metadata` (`AuditEventEncryptor.cs:99-122`) but not `EntityId`/`TenantId`. Attribute values off by default; retention default 2555 days with `EnableAutoPurge` false; relational purge is a hard delete.
- **Warn mode**: would-deny is stored as `Success` with `abac.enforced=false`; auditors must filter on the metadata.
- **`RuleId` is representative** for `*-overrides` algorithms; the trace keeps all candidates.
- **Obligation side effects** remain if the write fails closed after OnPermit obligations ran.
- **Double rows** when `AuditPipelineBehavior` is also registered (different events, joined by `CorrelationId`).
- **Direct PDP callers** are not audited.
- **Isolated scope assumption**: tenant and connection resolution in a new scope must come from ambient state (`IRequestContextAccessor`, `ModuleExecutionContext`); a database-per-tenant setup holding the tenant in a scoped object would write to the default database (revisit with #798).
- **Out of scope defects** (own issues, see the table under the Summary): #1676, #1635, #1677, #1591 and #1685 (also #1686-#1688, related); `AuditPipelineBehavior` stored `error.Message` (fixed by PR #1606, #1557) and still classifies the outcome by message text (`MapErrorToOutcome`, #1642).

---

## Next Steps

1. Order of work (decision 10): #1984 first, then #751 phase by phase (one PR per phase); #1700 and #1993 after Phase 1; #1783 in parallel. All hard prerequisites are merged (#1634, #1676, #1635, #1633 and #1705 Phase 4 with 9085 allocated). #1633 merged, so there is no Phase 0.
2. Done: the deferred issues (#1634-#1640, #1642) and the issues of the maintainer's answers (#1674, #1676-#1678) are opened; the orchestrator links this plan from #751.
3. Before the brief, re-read the PEP (`ABACPipelineBehavior.cs`, 472 lines on main), `ABACLogMessages` and the DI tests this plan extends.
4. Implement Phases 1-8 in one worktree per phase, with `adversarial-reviewer` self-review and the local CRAP gate table before opening each PR.

---

## Maintainer Decisions

Asked on 2026-10-09 after the re-check of the plan against today's code (`artifacts/plans/751-recheck.md`); the answers are on issue #751 (comment "Maintainer decisions (2026-10-09)"). One dated entry per Design Choice, numbered like the choices (D1 to D7), then the new doubts A1 to A9 and the process items 10 to 12.

### D1 (2026-10-09) Interception inside the PEP

Confirmed as written, with the ADR-036 names (`IOperationAuditStore` / `OperationAuditEntry`).

### D2 (2026-10-09) Reuse `IOperationAuditStore`, no `IAbacAuditStore`

Confirmed as written, with the ADR-036 names.

### D3 (2026-10-09) What a record carries

Confirmed as written, with the ADR-036 names. Applied additions: A5 (built-in subject attributes excluded from the name list, `abac.identity_kind`) and A9 (three timestamps).

### D4 (2026-10-09) Awaited write, isolated scope, `FailClosed` default

Confirmed as written, with the ADR-036 names. Applied refinements: A2 (bounded write) and A3 (token linking).

### D5 (2026-10-09) Configuration model

Confirmed as written, with the ADR-036 names. Applied: A1 (no per-request 9085 warning) and the configurable `WriteTimeout` of A2.

### D6 (2026-10-09) Resource identity and context

Confirmed as written, with the ADR-036 names. Applied: A8 (explicit resource ids only, no `AuditRequestConventions`).

### D7 (2026-10-09) Typed reader and JSON Lines export

Confirmed as written, with the ADR-036 names. Applied: A4 (error family of the tenant denials).

### A1 (2026-10-09) Per-request-type 9085 warning

Option (b): drop the per-request-type warning; the startup warning stays. Applied: Design 5, Phase 2 task 1, Event ID table.

### A2 (2026-10-09) Cooperative write timeout

Option (b): bound the audit write with a race (`WaitAsync`) as well as the token, through a small internal helper #1704 can reuse, with a test for a store that ignores the token. The timeout is configurable in `ABACOptions.DecisionAudit` (validated at start), default 5 seconds when not configured. Applied: Design 4 and 5, Phase 3 task 2, Phase 4 task 1.

### A3 (2026-10-09) Client-token linking

Option (a): the recorder write is never linked to the client token; the plan records that the PAP differs on purpose. Applied: Design 4.

### A4 (2026-10-09) Error code family

Decided with #1984 (option a): the reader's `tenant_required` / `tenant_mismatch` use `encina.authorization.*` (403); `abac.decision_audit_failed` stays `abac.*` (500). Applied: Design 7, Phase 1 task 6, Phase 3 task 4.

### A5 (2026-10-09) Built-in subject attributes in the name list

Option (b): `subject-id` and `identity-kind` are excluded from the stored attribute-name list; the identity kind is stored as the metadata key `abac.identity_kind` in `ABACDecisionAuditSchema`. Applied: Design 3, Phase 1 task 7, Phase 2 task 3.

### A6 (2026-10-09) The 9090 pipeline-order warning

Option (b): no 9090 startup warning; only document that `AddEncinaSecurity` is registered before `AddEncinaABAC`. The execution-order contract (#1783, now p0, #1678 merged into it) replaces any order warning. Applied: Design 3, Phase 4 task 5, Event ID table.

### A7 (2026-10-09) Service identities in the audit columns

Option (b+) after research (`artifacts/research/service-identity-personal-data.md`): a `service:<name>` id is not personal data by itself and is stored in clear without the personal-data regime; a user id keeps that regime; the whole row is still health data for retention and access; the actor kind is explicit (A5); `AddEncinaServiceIdentity` documents that a service name describes a function, never a person, and is never assigned to one person; a service acting on behalf of a user records both. Applied: Phase 8 tasks 1 and 2. Note (review of PR #2027): Encina has no on-behalf-of identity today, so the record carries the single request identity and a second identity is recorded once an on-behalf concept exists.

### A8 (2026-10-09) `AuditRequestConventions`

Option (b): explicit resource ids only (`resourceId` attribute, `IABACResourceIdentity`); no public `AuditRequestConventions`; an undeclared resource leaves `EntityId` empty and the docs explain how to declare it. Applied: Design 6, Phase 2 tasks 3 and 6, public API, file counts.

### A9 (2026-10-09) Timestamps

Option (a): `StartedAtUtc` at decision start; `CompletedAtUtc` and `TimestampUtc` from one `TimeProvider` read at the end. Applied: Design 2, Phase 2 task 3, Phase 3 task 3.

### 10 (2026-10-09) Order of work

#1984 first, then #751 phase by phase; #1700 and #1993 after #751 Phase 1; #1783 in parallel.

### 11 (2026-10-09) ADR number

ADR 047 is reserved for #751 now, in the plan-update PR (row 047 of the "Reserved numbers" table in `docs/architecture/adr/index.md`).

### 12 (2026-10-09) Plan edits

The plan is updated before the brief: Maintainer Decisions per Design Choice, ADR-036 names, Phase 0 removed, line references refreshed where the re-check gives the new line, ADR 047 reserved.

### Earlier answers (2026-10-03)

The ten open questions of the first version of this plan were answered on 2026-10-03. Each answer and where it is applied; items 2, 7 and 10 are superseded by the 2026-10-09 decisions above (Phase 0 is merged, ADR 047 is reserved, the order warning is dropped):

1. **Milestone**: #751 stays in v0.14.0 — Hardening, as an explicit exception to SPEC-002 DEC-014 (maintainer decision 2026-10-03: P0 feature for the reference application). SPEC-002 section 13.3 carries the note in the "Proposed" cell ("kept in v0.14.0 by maintainer decision 2026-10-03 (exception to DEC-014)") and the DEC-014 row cross-references the exception. Applied: header (Milestone).
2. **Prerequisite sequencing**: Phase 0 (#1633) is a hard prerequisite that merges first. #1633 itself waits for the audit-stores spike #1674 (option A: three purpose-named, coherent audit stores; the operation audit is today `IAuditStore` and may be renamed; `IAnonymizationAuditStore` is deleted). The plan keeps reusing the operation-audit store and takes its final names from #1674. Sequence: #1674 -> #1633 -> #751. Applied: header (Depends on, Naming note), Phase 0, Risks, deferred table.
3. **Deviations from the issue text**: accepted. `PolicyEvaluated` is a trace inside one record; `PolicyCacheHit` is a metric only (#1640); `DecisionAudit.Enabled` with the `AuditDecisions(...)` shortcut replaces `AuditAbacDecisions`; only attribute names are recorded, values only through `RecordedAttributeValues`. Applied: Summary (mapping), Design 3 and Design 5.
4. **Fail-closed default**: `FailClosed` is the default; `BestEffort` is the explicit, logged opt-out with a startup warning (EventId 9084). Applied: Design 4.
5. **Adjacent fixes**: they leave #751 and are fixed as their own issues now: #1635 (open-generic `TryAddTransient`, about 20 packages, including ABAC and `Encina.Security`), #1676 (missing security context and partial policy retrieval fail closed), #1677 (persistent PAP: captive scoped store, fire-and-forget audit, `DateTimeOffset.UtcNow`), #1591 and #1685 (exception messages; #1685 is the PR #1650 follow-up). The resource-attributes `default!` fix is done by #1634 (PR #1650). Applied: Summary table, Phase 1 task 4, Phase 2 tasks 2 to 4, Phase 4 tasks 2 and 3, Phase 7, Phase 8 (no `security` fragment), file counts, public API, Risks.
6. **Buffered writer**: deferred, only after the #924 benchmark and only as an option inside `BestEffort` (it is incompatible with `FailClosed`). Applied: Design 4.
7. **ADR number** (superseded by decision 11: ADR 047 is reserved): ADR-NNN, the next number neither used nor reserved in the ADR index (`docs/architecture/adr/index.md`, "Reserved numbers"), assigned when the ADR is written; this plan reserves no number (maintainer decision 2026-10-03). The "Reserved numbers" table (026 #1043, 032 #1187, 033 #1189) comes from PR #1680, a separate docs PR. Applied: Phase 8 task 3.
8. **Plan prompt typo** (`implementation-plan-prompt.md:113`, "section g)" should be f)): fixed in a separate docs PR; nothing to change in this plan.
9. **Reader tenant gate** (changed from the plan): the reader always forces the ambient tenant; it denies with `tenant_required` only when multi-tenancy is enabled (core marker added by `Encina.Tenancy`) and no tenant is resolved; single-tenant applications query without configuration; `AllowCrossTenantQueries` stays only for operator tooling in multi-tenant applications. Applied: Design 5, Design 7, Phase 3 task 4, Phase 5 task 1, Phase 7 tests, cross-cutting matrix, Risks, file counts and public API.
10. **Pipeline position** (superseded by A6: no 9090 warning): document `AddEncinaSecurity` before `AddEncinaABAC` and add a startup warning (EventId 9090, Phase 4 task 5) when the opposite order is detected; the general fix is the spike #1678 (named pipeline stages). Applied: Design 3, Phase 4, Phase 7 DI test.
