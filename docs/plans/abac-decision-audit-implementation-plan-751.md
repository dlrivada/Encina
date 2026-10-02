# Implementation Plan: ABAC Decision Audit Trail (`Encina.Security.ABAC`) — write-ahead, fail-closed, through `IAuditStore`

> **Issue**: [#751](https://github.com/dlrivada/Encina/issues/751)
> **Type**: Feature (labels: `p0-mandatory`, `area-audit`, `area-authorization`, `area-compliance`)
> **Milestone**: v0.14.0 — Hardening (see Open Questions: SPEC-002 DEC-014 reserves this milestone for defects)
> **Specification**: [SPEC-002](../specifications/SPEC-002-eu-regulatory-readiness.md) (row "Audit trail for ABAC decisions", REQ-007 related, line 789)
> **Depends on**: a new `[BUG]` issue that registers `IAuditStore` in ADO.NET x3, Dapper x3 and MongoDB (Phase 0 below)
> **Related**: #750 (saga audit), #752 (sharding/tenancy audit), #749, #753, #798 (tenant filtering), #924 (benchmarks), #1193 (evidential read audit), SPEC-002 REQ-051 (break-the-glass)
> **Complexity**: Medium-High (9 phases, no new store, ~16 new files, ~10 modified)
> **Estimated Scope**: ~900 lines of production code + ~1,600 lines of tests

---

## Summary

Issue #751 asks for an opt-in, queryable audit trail of ABAC decisions: who (subject) asked for what (resource, action, environment), which policies decided, and why. Today the only audit story is a user-written `IObligationHandler` for an `audit-access` obligation (`src/Encina.Security.ABAC/README.md:142-155`), and `PersistentPolicyAdministrationPoint` audits policy *changes* only (`Administration/PersistentPolicyAdministrationPoint.cs:587-664`). Nothing records the access decisions themselves.

This plan records **one `AuditEntry` per evaluated decision** at the Policy Enforcement Point (`ABACPipelineBehavior`), written **before** the protected handler runs (write-ahead), awaited, isolated from the request transaction, and **fail-closed** by default. It reuses `Encina.Security.Audit.IAuditStore` (already referenced by the package, `Encina.Security.ABAC.csproj:14`), so no new store, table or provider code is needed. A typed reader and a JSON Lines export give the "queryable by subject, resource, action" and "compliance export" criteria.

What the plan also fixes, because the audit cannot be trustworthy without it (all verified in the code):

- `nextStep()` runs inside the evaluation `try` (`ABACPipelineBehavior.cs:138-174`, `:264`), so a handler exception is reported as `abac.evaluation_failed`. The split moves the handler call outside.
- A null security context becomes `userId ""` (`:390`) instead of denying; `ABACErrors.MissingContext` is defined but unused (`ABACErrors.cs:295`). AGENTS.md section 3 requires fail-closed.
- Resource attributes receive `default!` instead of the request (`:397`).
- `AddEncinaABAC` registers the PEP with `TryAddTransient` on the open generic `IPipelineBehavior<,>` (`ServiceCollectionExtensions.cs:244`), which is silently skipped when `Encina.Security` registered its behavior first (`Encina.Security/ServiceCollectionExtensions.cs:81`): no enforcement and no audit. Switch to `TryAddEnumerable`.
- The persistent PAP factory resolves a possibly scoped `IAuditStore` from the root provider (`ServiceCollectionExtensions.cs:203`), uses `DateTimeOffset.UtcNow` (`PersistentPolicyAdministrationPoint.cs:601`) and logs `error.Message` (`:657-658`).
- `error.Message` / `ex.Message` reach logs and the activity status (`ABACPipelineBehavior.cs:168,229-231,289-291,421-423`), against AGENTS.md section 3.

**Mapping of the issue's events**: `AccessGranted` / `AccessDenied` become the enforced outcome of the record; `PolicyEvaluated` becomes the evaluation trace carried inside the record (not one row per policy); `PolicyCacheHit` is not an audit event (it has no subject and no compliance meaning, and the policy-store cache gives no hit signal on the PDP hot path, `Persistence/CachingPolicyStoreDecorator.cs:247-301`): it becomes the metric `abac.policy_cache.lookups{result}`.

**Affected packages**: `Encina.Security.ABAC` (main), `Encina.Security.Audit` (public `AuditRequestConventions`, doc fix), provider packages only in the prerequisite bug (Phase 0, separate issue).
**Provider category**: Database (10), by reuse of `IAuditStore`; Marten through `MartenAuditStore`. Caching, transports, locks, validation, cloud: not touched.

---

## Design Choices

<details>
<summary><strong>1. Interception point — inside the PEP (<code>ABACPipelineBehavior</code>), decide / record / enforce</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Inside the PEP: split `Handle` into decide, record, enforce** | Only place that knows the enforced outcome (obligation override, Warn mode, NotApplicable default, exceptions); transient, so scoped stores resolve; receives `IRequestContext` | Refactors an existing file; constructor gains parameters |
| **B) Decorator around `IPolicyDecisionPoint`** | Small seam (one-method interface); also audits direct PDP callers | PDP is a singleton; records `Success` for a Permit the PEP later overrides to Deny (`ABACPipelineBehavior.cs:222-247`); no record when attribute collection fails; recomputes Warn/NotApplicable logic; cannot see `IRequestContext` |
| **C) PEP raises an `AccessDecisionEnforced` notification persisted by a subscriber** | Extensible (SIEM, break-glass subscribers) | `Publish` with zero handlers returns `Right` (silent fail-open); every app handler becomes part of the gate; re-entrancy guard; largest surface |
| **D) Separate audit pipeline behavior** | Isolated | Cannot see the `PolicyDecision`; depends on registration order |

### Chosen Option: **A — Inside the PEP**

### Rationale

- The audit must describe what was actually enforced. Only the PEP knows it.
- `Handle` becomes: `DecideAsync` (collect attributes, call PDP, run obligations and advice, apply NotApplicable default and Warn mode; never calls `nextStep`) returns an internal `ABACEnforcementVerdict { Allow, Error, Record }`; then record (only when enabled); then enforce (`Allow ? await nextStep() : Left(Error)`).
- The split lowers cyclomatic complexity (CRAP <= 10, AGENTS.md section 9) and fixes the handler-exception misreport.
- Decisions made by callers that use `IPolicyDecisionPoint` directly (for example UI permission checks) are **not** audited. This is documented; a public helper that submits to the recorder is a deferred follow-up.
- The notification design (C) is rejected for the first cut: its extension value is real but it adds a silent fail-open path and a hot-path dispatch.

</details>

<details>
<summary><strong>2. Store — reuse <code>IAuditStore</code> / <code>AuditEntry</code>, no new <code>IAbacAuditStore</code></strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Reuse `IAuditStore` with a structured `AuditEntry` mapping** | Implementations already exist for ADO x3, Dapper x3, EF Core, MongoDB, Marten, InMemory; `AuditQuery` already filters by user, entity, action, outcome, tenant, date; OTel decorator; retention services | Policy ids and trace live in `Metadata` (not queryable); no `ModuleId` column |
| **B) New `IAbacAuditStore`** | Queryable policy fields, `ModuleId` | New store on all 10 providers (AGENTS.md section 5), new schemas, scripts, tests; provider coherence cost |
| **C) Extend the read-audit shape of #1193** | Shared "evidential" model | Different semantics (reads), plan not implemented yet |

### Chosen Option: **A — Reuse `IAuditStore`**

### Rationale

- The package already depends on `Encina.Security.Audit`, and the NIS2 behavior (`Encina.Compliance.NIS2/NIS2CompliancePipelineBehavior.cs:275-333`) is the precedent for a decision record in `IAuditStore`.
- Mapping (public static `ABACDecisionAuditEntryMapper.ToAuditEntry`, constants in `ABACDecisionAuditSchema`): `Action = "ABACDecision"` (one indexed filter for all ABAC decisions, `AuditEntryEntityConfiguration.cs:133`), `EntityType` = request type name (the XACML action), `EntityId` = resource id, `UserId` = subject, `TenantId`, `CorrelationId`, `Outcome`, `ErrorMessage` = reason **code only**, timestamps from `TimeProvider`, `Metadata` = string values only (so EF JSON, MongoDB and Marten encryption round-trip identically).
- `AuditEntry.Id` = `DecisionId` (`Guid.CreateVersion7`), which gives idempotent retries (see decision 4).
- A queryable policy/module column is deferred: a `ModuleId` on `AuditEntry`/`AuditQuery` for all providers is a separate `[FEATURE]`.

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
- Attribute names come from the raw `IAttributeProvider` dictionaries, captured before `ToBag`. Values are stored only for keys on `RecordedAttributeValues` (empty by default).
- Never stored: `DecisionStatus.StatusMessage` (it can contain `ex.Message`, `XACMLPolicyDecisionPoint.cs:170`), any `EncinaError.Message`, request or response payloads.
- Warn mode: `Outcome` stays `Success` because the request did run; metadata says `abac.enforced=false` and carries the would-deny verdict. "Who actually accessed X" is then one query with `Outcome = Success`. Reviewers filter `abac.enforced` to separate them.

</details>

<details>
<summary><strong>4. Failure semantics and transaction isolation — awaited, own scope, fail-closed by default</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Awaited write in an isolated DI scope with `TransactionScope(Suppress)`, `FailClosed` default, `BestEffort` explicit opt-out** | Meets AGENTS.md section 3 (no swallowed errors, fail-closed); deny rows survive business rollback; no captive scoped dependency | One extra scope + insert per protected request; audit-store outage denies requests |
| **B) Fire-and-forget (PAP / NIS2 precedent)** | Zero latency | Swallows errors (forbidden in background infrastructure), disposed `DbContext` risk, access proceeds without evidence |
| **C) Buffered channel + background writer** | Lowest latency | Loss window, not write-ahead, much more code |

### Chosen Option: **A**

### Rationale

- `AuditStoreEF` saves through the request's shared `DbContext` (`AuditStoreEF.cs:67-68`) and the EF `TransactionPipelineBehavior` rolls back on `Left` (`EntityFrameworkCore/TransactionPipelineBehavior.cs:118-126`); an ABAC deny returns `Left`. A record written in the request scope would vanish. The default recorder `AuditStoreABACDecisionRecorder` (singleton) opens `IServiceScopeFactory.CreateAsyncScope()`, suppresses ambient transactions, resolves `IAuditStore` and awaits `RecordAsync` with its own `CancellationTokenSource(WriteTimeout, timeProvider)`, not linked to the client token, so a disconnect cannot erase the evidence of a denied attempt.
- `FailClosed`: when the request would proceed and the write returns `Left`, throws or times out, the PEP returns `ABACErrors.DecisionAuditFailed(requestType, storeErrorCode)` (`abac.decision_audit_failed`, fixed message, code in details). When the request is denied anyway the original deny stands and the failure is logged.
- `BestEffort` proceeds and logs; a startup Warning (EventId 9077) makes the opt-out visible (SPEC-002 DEC-006).
- Idempotency: on an ambiguous `Left`, the recorder checks `GetByCorrelationIdAsync` for an entry with the same `Id` and treats a match as success.
- Over-limit values (`Action`/`TenantId` 128, others 256: `AuditEntryEntityConfiguration.cs:40-64`) become `sha256:<hex>` and are listed in `abac.hashed_fields`, so a long id can never fail the insert and deny access.
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

- Options: `Enabled` (false), `Outcomes` (`[Flags]` Granted, Denied, NotEnforced; All), `FailureMode` (FailClosed), `IncludeEvaluationTrace` (true), `MaxTraceEntries` (64), `ResourceIdAttributeName` ("resourceId"), `RecordedAttributeValues` (empty), `WriteTimeout` (5 s). `options.AuditDecisions(o => ...)` is the issue's `AuditAbacDecisions = true`.
- `ABACOptionsValidator` (uses `IServiceProviderIsService`) fails when audit is enabled and: no `IAuditStore` registered with the default recorder, `EnforcementMode` is `Disabled` (the PEP never calls the PDP, `ABACPipelineBehavior.cs:112-115`), `ISecurityContextAccessor` is not registered (registered by `Encina.Security`, not by ABAC), or timeout/trace bounds are invalid.
- A one-time Warning per request type when `Disabled` is configured without audit is the logged opt-out required by AGENTS.md section 3.
- When `Enabled` is false nothing is built: no record, no time read, no trace flag, no recorder call (pay-for-what-you-use).

</details>

<details>
<summary><strong>6. Resource identity and context — <code>IABACResourceIdentity</code>, <code>IRequestContext</code>, public conventions</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Optional `IABACResourceIdentity` on requests, then attribute `resourceId`, then `AuditRequestConventions` (promoted from `RequestMetadataExtractor`)** | Explicit first, convention fallback; rows line up with `AuditPipelineBehavior` rows on `EntityType`/`EntityId`/`CorrelationId` | Convention fallback (a property ending in `Id`, `RequestMetadataExtractor.cs:129-135`) can pick an unintended id |
| **B) Attribute provider only** | Simple | `DefaultAttributeProvider` returns empty dictionaries (`Providers/DefaultAttributeProvider.cs:36-66`): no resource id by default |
| **C) Reflection on request** | Automatic | Hot-path reflection, ambiguous |

### Chosen Option: **A**

### Rationale

- The PEP passes the real `request` to `GetResourceAttributesAsync` (fixing `default!`).
- `CorrelationId` and `TenantId` come from `IRequestContext` (unused today, `:107`), `ModuleId` from `context.GetModuleName()`, subject from `ISecurityContextAccessor`; a null context denies with `ABACErrors.MissingContext`.
- Documentation names the `Id`-suffix fallback weakness; `[Auditable]` overrides it.

</details>

<details>
<summary><strong>7. Query and export — typed reader over <code>IAuditStore.QueryAsync</code></strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `IABACDecisionAuditReader` returning an `ABACDecisionAuditRecord` projection; JSON Lines export; tenant forced** | Typed, server-side filters (`Action` constant, `UserId`, `EntityType`, `EntityId`, `Outcome`, `TenantId`, dates); export pages at `MaxPageSize` | A new public reader |
| **B) Extension methods on `IAuditStore` returning raw `AuditEntry`** | Tiny | Callers parse metadata themselves; no tenant guard |
| **C) Export only** | Smallest | No query API (acceptance criterion) |

### Chosen Option: **A**

### Rationale

- The reader forces the ambient tenant and rejects an explicit mismatching tenant with `Left(InvalidDecisionAuditQuery("tenant_mismatch"))` (SPEC-002 REQ-061); store-level automatic tenant filtering stays in #798.
- Queries validate page size (<= 1000) and `FromUtc <= ToUtc`, returning `Left`.
- Export is JSON Lines, schema `encina.abac.decision/1`, codes only.

</details>

---

## Implementation Phases

### Phase 0: Prerequisite (separate `[BUG]` issue, not part of this PR)

> **Goal**: `IAuditStore` is registered on every database provider and the `SecurityAuditEntries` table can be created.

<details>
<summary>Tasks</summary>

Verified gap: only EF Core (`src/Encina.EntityFrameworkCore/ServiceCollectionExtensions.cs:410-416`) and Marten (`src/Encina.Audit.Marten/ServiceCollectionExtensions.cs:118`) register `IAuditStore`. ADO x3 and Dapper x3 register only `IAuditLogStore` and `IReadAuditStore` (for example `Encina.ADO.SqlServer/ServiceCollectionExtensions.cs:80-103`). MongoDB exposes `UseSecurityAuditStore` (`EncinaMongoDbOptions.cs:93`) but never reads it (`ServiceCollectionExtensions.cs:634-654`). No script creates `SecurityAuditEntries`. `docs/features/audit-tracking.md:1031,1168` names a nonexistent `config.UseAuditStore`.

1. Honour `MessagingConfiguration.UseSecurityAuditStore` in the 6 ADO/Dapper packages and `EncinaMongoDbOptions.UseSecurityAuditStore` in MongoDB; use the #1269 pattern (remove the in-memory default, then `TryAddScoped`) so the database store wins in any registration order without overriding the application's own registration (AGENTS.md section 3).
2. Add `NNN_CreateSecurityAuditEntriesTable.sql` to the 6 relational `Scripts/` folders and to `000_CreateAllTables.sql`, with the column limits of `AuditEntryEntityConfiguration.cs:32-134`.
3. Per-provider DI tests with `ValidateOnBuild` and `ValidateScopes`; fix the docs.

Until it merges, ABAC decision audit is durable on EF Core x3 and Marten; on the other 7 providers the startup check warns (9080) about the non-durable `InMemoryAuditStore` fallback or fails (9081) when no store exists.

</details>

<details>
<summary>Prompt for AI Agents — Phase 0</summary>

```text
CONTEXT: Encina (.NET 10, pre-1.0). IAuditStore (src/Encina.Security.Audit/Abstractions/IAuditStore.cs) has implementations AuditStoreADO (SqlServer/PostgreSQL/MySQL), AuditStoreDapper (same), AuditStoreEF, AuditStoreMongoDB, but only EF Core and Marten register it in DI.
TASK: Register IAuditStore in the 6 ADO/Dapper packages and MongoDB when UseSecurityAuditStore is set; add SecurityAuditEntries SQL scripts; add DI tests.
KEY RULES: AGENTS.md section 3 registration completeness (DB store wins over InMemory in any order, never override the app's registration; ValidateOnBuild + ValidateScopes test); section 5 all 10 providers; async DB calls only.
REFERENCE FILES: src/Encina.EntityFrameworkCore/ServiceCollectionExtensions.cs:410-416, src/Encina.ADO.SqlServer/ServiceCollectionExtensions.cs:80-103, src/Encina.MongoDB/EncinaMongoDbOptions.cs:86-93, src/Encina.EntityFrameworkCore/Auditing/AuditEntryEntityConfiguration.cs.
```

</details>

---

### Phase 1: Core Models, Trace and Errors

> **Goal**: The data model that lets the PEP describe a decision, and the PDP trace.

<details>
<summary>Tasks</summary>

New namespace `Encina.Security.ABAC.DecisionAudit` unless noted.

1. `Model/PolicyEvaluationTrace.cs`: `sealed record PolicyEvaluationTrace` (`required string PolicyId`, `required bool IsPolicySet`, `required Effect Effect`, `required PolicyTraceReason Reason`, `IReadOnlyList<string> DecisiveRuleIds`, `IReadOnlyList<PolicyEvaluationTrace> Children`, `string? Version`); `enum PolicyTraceReason`.
2. `Model/PolicyDecision.cs`: add `EvaluatedPolicies { get; init; }` (default `[]`); `RuleId` populated when traced.
3. `Model/PolicyEvaluationContext.cs`: add `bool IncludeEvaluationTrace { get; init; }`.
4. `Evaluation/XACMLPolicyDecisionPoint.cs`: thread a nullable trace sink through `EvaluatePolicySet` / `EvaluatePolicy` (`:182-305`); `BuildDecision` (`:377-405`) fills `EvaluatedPolicies` and `RuleId` via a helper `PolicyEvaluationTraceResolver.ResolveDecisiveRuleId` (extracted so CRAP stays <= 10). Cap at `MaxTraceEntries`.
5. `DecisionAudit/ABACDecisionRecord.cs` (`sealed record`: identity, context, request, decision, trace, obligations/advice ids, attribute names, recorded values, timing), `ABACEnforcedOutcome` (Granted, Denied, DeniedNotEnforced), `IABACResourceIdentity`.
6. `ABACErrors.cs`: `DecisionAuditFailedCode = "abac.decision_audit_failed"`, `DecisionAuditFailed(Type, string?)`, `InvalidDecisionAuditQuery(string)`; fixed messages, codes only in details.
7. `DecisionAudit/ABACDecisionAuditSchema.cs`: `Action`, `SchemaVersion`, metadata keys, column-limit constants.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT: src/Encina.Security.ABAC. The PDP (Evaluation/XACMLPolicyDecisionPoint.cs) discards per-policy and per-rule results and never sets PolicyDecision.RuleId/Reason.
TASK: Add the opt-in evaluation trace (PolicyEvaluationContext.IncludeEvaluationTrace -> PolicyDecision.EvaluatedPolicies + RuleId), the ABACDecisionRecord model, ABACEnforcedOutcome, IABACResourceIdentity, ABACDecisionAuditSchema constants and the two new ABACErrors. Trace off must allocate nothing.
KEY RULES: C# 14, nullable, XML docs on public APIs, no [Obsolete], Either<EncinaError,T>, error factories use fixed messages and codes only (AGENTS.md section 3), CRAP <= 10 on changed methods (extract helpers), add every public symbol to PublicAPI.Unshipped.txt via mechanical-fixer.
REFERENCE FILES: src/Encina.Security.ABAC/Model/PolicyDecision.cs, Model/PolicyEvaluationContext.cs, Evaluation/XACMLPolicyDecisionPoint.cs:74-405, CombiningAlgorithms/DenyOverridesAlgorithm.cs:160, ABACErrors.cs.
```

</details>

---

### Phase 2: PEP Refactor (decide, record, enforce)

> **Goal**: The PEP records the enforced outcome write-ahead and fixes the fail-open gaps found on the way.

<details>
<summary>Tasks</summary>

1. `ABACPipelineBehavior.cs`: constructor gains `IABACDecisionRecorder decisionRecorder` and `TimeProvider timeProvider`. Split `Handle` into `DecideAsync` (internal `ABACEnforcementVerdict`), `RecordAsync` (applies `Outcomes` filter and `FailureMode`), and enforcement; `nextStep()` only in the last step, outside any try/catch.
2. Missing security context: deny with `ABACErrors.MissingContext` (log 9079); it still passes through record + enforcement.
3. Attribute collection (`:386-410`): pass `request` to `GetResourceAttributesAsync`; capture attribute names before `ToBag`; read `IABACResourceIdentity`, then `ResourceIdAttributeName`, then `AuditRequestConventions`; take `CorrelationId`/`TenantId` from `IRequestContext`.
4. Logs and activity: replace `{ErrorMessage}` by `{ErrorCode}` in 9004, 9005, 9014 and the activity status `ex.Message` (`:168`) by the exception type (AGENTS.md section 3).
5. Warn mode: record `Outcome = Success` plus `abac.enforced=false`; failed mandatory obligation stays Denied.
6. `Encina.Security.Audit`: promote `RequestMetadataExtractor` to public `AuditRequestConventions` (`ExtractFromTypeName`, `TryExtractEntityId`, `[Auditable]`-aware `Resolve`); fix the `AuditEntry.ErrorMessage` XML doc (`AuditEntry.cs:133-136`) and the `IAuditStore.cs:43-44` remark that contradicts fail-closed.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT: ABACPipelineBehavior<TRequest,TResponse> (src/Encina.Security.ABAC/ABACPipelineBehavior.cs) calls nextStep() inside its evaluation try block (:264) and swallows context gaps.
TASK: Refactor Handle into decide -> record -> enforce as described in the plan (Design Choice 1, 3, 4, 6). Handler exceptions must propagate unchanged. Disabled mode stays a bypass with a once-per-request-type Warning when audit is configured. Keep behavior byte-identical when DecisionAudit.Enabled is false except for the listed fixes.
KEY RULES: fail closed on missing context (AGENTS.md section 3); TimeProvider, never DateTime.UtcNow; codes only in logs/activity; EventIds 9072-9083 registered in ABACLogMessages (inside SecurityABAC 9000-9099); CRAP <= 10; existing ABACPipelineBehaviorTests must be updated, not deleted.
REFERENCE FILES: ABACPipelineBehavior.cs:105-431, AttributeContextBuilder.cs:71-103, ABACErrors.cs:295, src/Encina.Security.Audit/RequestMetadataExtractor.cs, DefaultAuditEntryFactory.cs:89-97.
```

</details>

---

### Phase 3: Recorder, Mapper, Reader and Export

> **Goal**: Persist through `IAuditStore` and read it back.

<details>
<summary>Tasks</summary>

1. `DecisionAudit/IABACDecisionRecorder.cs`: `ValueTask<Either<EncinaError, Unit>> RecordAsync(ABACDecisionRecord, CancellationToken)`.
2. `DecisionAudit/AuditStoreABACDecisionRecorder.cs` (singleton): ctor `(IServiceScopeFactory, IOptions<ABACOptions>, TimeProvider, ILogger<...>)`; scope, `TransactionScope(Suppress, AsyncFlowOption.Enabled)`, own timeout, idempotent retry check, exceptions through `ex.ForLogging()`.
3. `DecisionAudit/ABACDecisionAuditEntryMapper.cs`: public static `ToAuditEntry`; string-only metadata; hash over-limit values; source-generated `ABACDecisionAuditJsonContext` for the trace JSON; IP and user agent from the ambient metadata keys used by `DefaultAuditEntryFactory.cs:229-239`.
4. `DecisionAudit/IABACDecisionAuditReader.cs` + `ABACDecisionAuditReader` (scoped), `ABACDecisionAuditQuery`, `ABACDecisionAuditRecord`; `QueryAsync` and `ExportAsync` (JSON Lines).

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT: Reuse Encina.Security.Audit.IAuditStore (RecordAsync, QueryAsync(AuditQuery)). EF Core and Marten register it scoped; the recorder is a singleton.
TASK: Implement the recorder, mapper, reader and export from the plan. Entry mapping: Action="ABACDecision", EntityType=request type, EntityId=resource id, UserId=subject, ErrorMessage=reason code only, Metadata string values only with keys abac.*.
KEY RULES: never store EncinaError.Message or DecisionStatus.StatusMessage; hash values over the EF column limits (AuditEntryEntityConfiguration.cs:40-64) instead of failing; timestamps from TimeProvider; async with CancellationToken; reader forces ambient tenant and returns Left on tenant mismatch or invalid paging.
REFERENCE FILES: src/Encina.Security.Audit/AuditEntry.cs, AuditQuery.cs, Encina.Compliance.NIS2/NIS2CompliancePipelineBehavior.cs:275-333, Encina.Security.ABAC/Administration/PersistentPolicyAdministrationPoint.cs:587-664.
```

</details>

---

### Phase 4: Configuration, DI and Startup Checks

> **Goal**: Opt-in registration that cannot silently skip the gate or the audit.

<details>
<summary>Tasks</summary>

1. `ABACOptions.cs`: `DecisionAudit`, `AuditDecisions(Action<ABACDecisionAuditOptions>?)`; `ABACDecisionAuditOptions`, `ABACDecisionAuditOutcomes`, `ABACDecisionAuditFailureMode`.
2. `ServiceCollectionExtensions.cs`: `TryAddSingleton(TimeProvider.System)`; `TryAddSingleton<IABACDecisionRecorder, AuditStoreABACDecisionRecorder>`; scoped reader; `TryAddEnumerable` for the behavior (replaces `TryAddTransient`, `:244`); `TryAddEnumerable<IValidateOptions<ABACOptions>, ABACOptionsValidator>` + `AddOptions<ABACOptions>().ValidateOnStart()`; nothing audit-related registered when disabled.
3. PAP captive-dependency fix: the factory at `:203` takes `IServiceScopeFactory` and resolves `IAuditStore` per write; `PersistentPolicyAdministrationPoint` uses `TimeProvider` (`:601`) and logs codes (`:657-658`).
4. `ABACDecisionAuditStartupCheck` (`IHostedService`): scope-resolves `IAuditStore`; Critical 9081 and throw when missing; Warning 9080 when `InMemoryAuditStore`; Warning 9077 for `BestEffort`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT: AddEncinaABAC (src/Encina.Security.ABAC/ServiceCollectionExtensions.cs:97-247) reads feature gates from a temporary options instance and registers with TryAdd.
TASK: Add the DecisionAudit options, validator, registrations, startup check and the PAP scope/time/log fix per the plan. Do not register an IAuditStore from ABAC (the app or a provider package chooses it); a missing store must fail ValidateOnBuild/startup, never silently no-op.
KEY RULES: AGENTS.md section 3 registration completeness and ValidateOnBuild + ValidateScopes DI test (including AddEncinaSecurity called before AddEncinaABAC and a scoped IAuditStore); options with secrets none here; TryAddEnumerable for the pipeline behavior.
REFERENCE FILES: ServiceCollectionExtensions.cs, ABACOptions.cs:49-257, PolicyCachingOptions.cs, Encina.Security/ServiceCollectionExtensions.cs:76-81, EELExpressionPrecompilationService.cs.
```

</details>

---

### Phase 5: Cross-Cutting Integration

> **Goal**: Everything marked integrate in the matrix below is wired.

<details>
<summary>Tasks</summary>

1. Multi-tenancy: `TenantId` on every entry; reader tenant guard (store-level filtering remains #798).
2. Module isolation: `abac.module_id` metadata from `context.GetModuleName()`.
3. Health: `ABACHealthCheck` (`Health/ABACHealthCheck.cs:36-124`) reports decision-audit state: Unhealthy under `FailClosed` after a failed write, Degraded under `BestEffort` or `InMemoryAuditStore`; `ABACDecisionAuditHealthState` singleton. No write probe.
4. Validation: options validator and reader query validation (Phase 3/4).
5. Transactions: isolation by scope + `TransactionScope(Suppress)` (Phase 3).
6. Resilience: `WriteTimeout` only; no retries on the request path.
7. Idempotency: `DecisionId` as `AuditEntry.Id` with duplicate check.

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

1. `Diagnostics/ABACLogMessages.cs`: EventIds 9072-9083 (table in Research). Codes and exception types only.
2. `Diagnostics/ABACDiagnostics.cs` (existing `ActivitySource`/`Meter` "Encina.Security.ABAC"): counters `abac.decision_audit.recorded` (outcome, enforcement_mode), `abac.decision_audit.failed` (failure_mode, error.type), `abac.policy_cache.lookups` (operation, result); histogram `abac.decision_audit.duration`; span `ABAC.DecisionAudit.Record` as child of `ABAC.Evaluate`; tag `abac.decision_id` on `ABAC.Evaluate`.
3. `Persistence/CachingPolicyStoreDecorator.cs:247-280`: miss detected when the `GetOrSetAsync` factory runs; hit otherwise.
4. No subject, resource, tenant or attribute value in tags or logs (SPEC-002 REQ-062).

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT: EventIdRanges.SecurityABAC = (9000, 9099) is registered (src/Encina/Diagnostics/EventIdRanges.cs:347) and mapped in tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs:106. Used ids end at 9071.
TASK: Add EventIds 9072-9083 packed sequentially in ABACLogMessages with [LoggerMessage] and XML docs naming the range; add the metrics, span and policy-cache lookup counter.
KEY RULES: AGENTS.md section 7; never log EncinaError.Message; exceptions via ForLogging(); run the EventId architecture tests; new PublicAPI lines only if fields are public.
REFERENCE FILES: Diagnostics/ABACLogMessages.cs, Diagnostics/ABACDiagnostics.cs:11-229, Persistence/CachingPolicyStoreDecorator.cs.
```

</details>

---

### Phase 7: Testing

> **Goal**: Every applicable coverage flag reaches its target in `.github/coverage-manifest/Encina.Security.ABAC.json` (unit 70, guard 20, contract 15, property 15).

<details>
<summary>Tasks</summary>

1. **Unit** (`tests/Encina.UnitTests/Security/ABAC/`): PEP outcome matrix (every effect x Block/Warn x NotApplicable default x FailClosed/BestEffort x store Right/Left/throw/timeout); record written before `next`; handler exception propagates unchanged; missing context denies; disabled audit costs nothing; mapper (fields, hashing, no `StatusMessage`); recorder (child scope, suppression, duplicate check); PDP trace and `RuleId`; reader tenant forcing and export; options validator, startup check, health check; logs and meters via `MeterListener`; PAP scope and `TimeProvider` fix. Pattern: `Persistence/PersistentPolicyAdministrationPointAuditTests.cs`, `ABACPipelineBehaviorTests.cs`.
2. **DI test** (new `ServiceCollectionExtensionsDecisionAuditTests`): `ValidateOnBuild` + `ValidateScopes`, scoped `IAuditStore`, `AddEncinaSecurity` before `AddEncinaABAC`, registration order, missing store fails. ABAC has no such test today.
3. **Guard**: new constructors and public methods (`tests/Encina.GuardTests/Security/ABAC/`).
4. **Contract**: `IABACDecisionRecorder` over `InMemoryAuditStore`; exactly one record per evaluated request; fail-closed contract (pattern `tests/Encina.ContractTests/Security/ABAC/XACMLPolicyDecisionPointContractTests.cs`).
5. **Property (FsCheck)**: random `StatusMessage` never appears in an entry; values over column limits are hashed, never raise; outcome mapping is total; `Proceed` only for Granted and Warn pass-through; entry-to-record round trip loses nothing.
6. **Integration** (shared `[Collection]` fixtures, `ClearAllDataAsync` in `InitializeAsync`): recorder-to-reader round trip on ADO x3, Dapper x3, EF Core x3, MongoDB, plus Marten (outside the 10-provider rule); stores built directly as `tests/Encina.IntegrationTests/Security/Audit/AuditStoreADOSqlServerIntegrationTests.cs` does. The 7 non-EF providers need Phase 0 merged. Never a `.md` justification for these.
7. **Load**: concurrent evaluations with audit on (denials under store failure). **Benchmark**: PEP with audit off and on, PDP with and without trace; results to open #924.
8. Run the local CRAP gate table for changed methods before reporting.

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```text
CONTEXT: Targets per flag come from .github/coverage-manifest/Encina.Security.ABAC.json (unit 70, guard 20, contract 15, property 15); new source files are added with `dotnet run --file .github/scripts/generate-coverage-manifest.cs -- --append-only` (mechanical-fixer).
TASK: Write the tests listed in the plan, executing real package code (no reflection-only tests). Use Shouldly via Encina.Testing.Shouldly, NSubstitute, FsCheck wrappers, FakeTimeProvider.
KEY RULES: AGENTS.md section 9 (AAA, deterministic, no Thread.Sleep, builders), integration via [Collection("ADO-SqlServer")] etc., never IClassFixture for DB fixtures, outputs under artifacts/.
REFERENCE FILES: tests/Encina.UnitTests/Security/ABAC/ABACPipelineBehaviorTests.cs:157-455, Persistence/PersistentPolicyAdministrationPointAuditTests.cs:152-472, tests/Encina.ContractTests/Security/ABAC/, tests/Encina.IntegrationTests/Security/Audit/.
```

</details>

---

### Phase 8: Documentation and Finalization (always last)

> **Goal**: Docs, ADR, changelog, PublicAPI and manifest.

<details>
<summary>Tasks</summary>

1. XML docs on all new public APIs.
2. `src/Encina.Security.ABAC/README.md`: replace the obligation-based audit recipe (`:142-155`) with the decision audit; `docs/features/security-authorization.md` and a new page under `docs/features/abac/` (via `docs-writer`): Warn-mode semantics, direct-PDP callers not audited, obligation side effects when the write fails, `Id`-suffix fallback, double rows with `AuditPipelineBehavior` (joined by `CorrelationId`), plaintext resource ids outside Marten, retention (`RetentionDays` 2555, `EnableAutoPurge` false).
3. ADR-032 "ABAC decisions are audited write-ahead at the enforcement point, fail-closed" (docs/architecture/adr/index.md currently ends at 031); reusable by #750, #752 and REQ-051.
4. `changelog.d/751-abac-decision-audit.added.md` (the new capability) and `changelog.d/751-abac-decision-audit-gates.security.md` (missing context now denies; PEP registration no longer skipped; messages removed from logs).
5. `PublicAPI.Unshipped.txt` for every public symbol (below); `.github/coverage-manifest/Encina.Security.ABAC.json` via `--append-only`.
6. `docs/INVENTORY.md`, `ROADMAP.md`, `docs/releases/` if applicable; ABAC plan 401 cross-link.
7. `dotnet build -c Release` 0 warnings; `dotnet test`; coverage per flag; `changelog-fragments.cs -- --check`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 8</summary>

```text
CONTEXT: Phases 1-7 are done. Finish docs and bookkeeping for #751.
TASK: Hand documentation to docs-writer, changelog fragments, PublicAPI lines and coverage manifest entries to mechanical-fixer; write ADR-032 following docs/architecture/adr/031-retention-erasure-port.md.
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
| `IAuditStore`, `AuditEntry`, `AuditQuery`, `AuditOutcome` | `src/Encina.Security.Audit/` | Persistence and query |
| `AuditStoreADO/Dapper/EF/MongoDB`, `MartenAuditStore`, `InMemoryAuditStore`, `InstrumentedAuditStore` | provider packages, `Encina.Audit.Marten`, `Encina.OpenTelemetry/Audit` | Storage on all providers |
| `ABACPipelineBehavior`, `XACMLPolicyDecisionPoint`, `ObligationExecutor` | `src/Encina.Security.ABAC/` | PEP, PDP, obligations |
| `ABACDiagnostics`, `ABACLogMessages`, `ABACHealthCheck` | `src/Encina.Security.ABAC/Diagnostics`, `Health` | Observability |
| `NIS2CompliancePipelineBehavior` (audit part) | `src/Encina.Compliance.NIS2/` | Precedent for a decision record |
| `PersistentPolicyAdministrationPoint` audit | `src/Encina.Security.ABAC/Administration/` | Existing PAP audit; fixed here |
| `IRequestContext`, `ISecurityContextAccessor`, `IModuleExecutionContext` | core, `Encina.Security` | Subject, tenant, module |
| `RequestMetadataExtractor` | `src/Encina.Security.Audit/` | Becomes `AuditRequestConventions` |

### Event ID allocation

Range `SecurityABAC = (9000, 9099)` (`src/Encina/Diagnostics/EventIdRanges.cs:347`) is already registered and mapped; used 9000-9015, 9020-9022, 9030-9040, 9050-9071; **new 9072-9083**, packed, 16 ids remain free (9084-9099). No new range needed.

| EventId | Level | Meaning |
|---------|-------|---------|
| 9072 | Debug | Decision recorded |
| 9073 | Error | Audit failed, access denied (FailClosed) |
| 9074 | Warning | Audit failed, proceeding (BestEffort) |
| 9075 | Error | Audit of an already denied request failed |
| 9076 | Error | Recorder threw (redacted exception) |
| 9077 | Warning | BestEffort configured (startup) |
| 9078 | Warning | Enforcement Disabled (once per request type) |
| 9079 | Warning | Missing security context, request denied |
| 9080 | Warning | Non-durable `InMemoryAuditStore` |
| 9081 | Critical | `IAuditStore` not registered |
| 9082 | Debug | Trace truncated at `MaxTraceEntries` |
| 9083 | Debug | Duplicate decision id detected (idempotent retry) |

### Estimated file count

| Category | New | Modified |
|----------|-----|----------|
| Model, trace, errors, options | 7 | 5 (`PolicyDecision`, `PolicyEvaluationContext`, `ABACOptions`, `ABACErrors`, PDP) |
| Recorder, mapper, reader, export, startup check, validator | 8 | 0 |
| PEP, DI, PAP, cache decorator, health, diagnostics, logs | 1 (health state) | 6 |
| `Encina.Security.Audit` (conventions, doc fix) | 1 | 3 |
| Tests (unit, DI, guard, contract, property, integration, load, benchmark) | ~25 | ~3 |
| Docs, ADR, changelog, PublicAPI, manifest | 4 | ~6 |

### Public API changes (`src/Encina.Security.ABAC/PublicAPI.Unshipped.txt`)

- `ABACOptions.DecisionAudit`, `ABACOptions.AuditDecisions(...)`, `ABACDecisionAuditOptions`, `ABACDecisionAuditOutcomes`, `ABACDecisionAuditFailureMode`.
- `PolicyEvaluationContext.IncludeEvaluationTrace`, `PolicyDecision.EvaluatedPolicies`, `PolicyEvaluationTrace`, `PolicyTraceReason`.
- Namespace `Encina.Security.ABAC.DecisionAudit`: `ABACDecisionRecord`, `ABACEnforcedOutcome`, `IABACResourceIdentity`, `IABACDecisionRecorder`, `AuditStoreABACDecisionRecorder`, `ABACDecisionAuditEntryMapper`, `ABACDecisionAuditSchema`, `IABACDecisionAuditReader`, `ABACDecisionAuditQuery`, `ABACDecisionAuditRecord`.
- `ABACErrors.DecisionAuditFailedCode`, `DecisionAuditFailed`, `InvalidDecisionAuditQueryCode`, `InvalidDecisionAuditQuery`.
- Breaking (pre-1.0, acceptable): `ABACPipelineBehavior` constructor gains `IABACDecisionRecorder` and `TimeProvider`.
- `Encina.Security.Audit`: `AuditRequestConventions` (`PublicAPI.Unshipped.txt` of that package).
- Internal: `ABACEnforcementVerdict`, `ABACOptionsValidator`, `ABACDecisionAuditStartupCheck`, `ABACDecisionAuditHealthState`, `ABACDecisionAuditJsonContext`.

### Provider matrix (AGENTS.md section 5)

| Provider | Store used | Status |
|----------|-----------|--------|
| ADO.NET SqlServer / PostgreSQL / MySQL | `AuditStoreADO` | Class exists; DI registration and DDL missing (Phase 0) |
| Dapper SqlServer / PostgreSQL / MySQL | `AuditStoreDapper` | Same as ADO (Phase 0) |
| EF Core SqlServer / PostgreSQL / MySQL | `AuditStoreEF` (shared) | Registers when `UseSecurityAuditStore` is set |
| MongoDB | `AuditStoreMongoDB` | Option never read (Phase 0) |
| Marten (outside the 10) | `MartenAuditStore` | Registers; encrypts `UserId` and `Metadata`, not `EntityId` |
| InMemory | `InMemoryAuditStore` | Default; non-durable warning |

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

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
PROJECT CONTEXT:
Encina is pre-1.0, .NET 10 / C# 14, nullable enabled, ROP (Either<EncinaError,T>). Rules are in AGENTS.md; read it first. Package: src/Encina.Security.ABAC. Issue #751: opt-in, queryable audit trail of ABAC decisions.

IMPLEMENTATION OVERVIEW:
Phase 0 (separate [BUG], prerequisite): register IAuditStore in ADO x3, Dapper x3, MongoDB; SecurityAuditEntries DDL.
Phase 1: trace model (PolicyEvaluationTrace, PolicyDecision.EvaluatedPolicies/RuleId, PolicyEvaluationContext.IncludeEvaluationTrace), ABACDecisionRecord, errors.
Phase 2: refactor ABACPipelineBehavior into decide -> record -> enforce; missing context denies; request passed as resource; codes in logs; AuditRequestConventions.
Phase 3: AuditStoreABACDecisionRecorder (own scope, TransactionScope Suppress, own timeout, idempotent), mapper to AuditEntry, typed reader and JSON Lines export.
Phase 4: ABACOptions.DecisionAudit, validator + ValidateOnStart, TryAddEnumerable for the behavior, startup check, PAP scope/TimeProvider/log fix.
Phase 5: tenant, module, health, validation, transactions, idempotency.
Phase 6: EventIds 9072-9083, metrics, span, policy-cache lookup counter.
Phase 7: tests per flag. Phase 8: docs (docs-writer), ADR-032, changelog fragments and PublicAPI (mechanical-fixer).

KEY PATTERNS:
- Fail closed: a write failure denies a request that would proceed (FailureMode FailClosed); BestEffort is an explicit, logged opt-out.
- Codes only: never EncinaError.Message, StatusMessage or exception messages in logs, tags or stored entries.
- TimeProvider everywhere; Guid.CreateVersion7 for DecisionId.
- Opt-in: nothing allocated or registered when DecisionAudit.Enabled is false.
- Registration completeness: ValidateOnBuild + ValidateScopes DI test.
- CRAP <= 10 on changed methods; extract helpers.

REFERENCE FILES:
src/Encina.Security.ABAC/ABACPipelineBehavior.cs, ServiceCollectionExtensions.cs, ABACOptions.cs, Evaluation/XACMLPolicyDecisionPoint.cs, Administration/PersistentPolicyAdministrationPoint.cs, Diagnostics/ABACLogMessages.cs, src/Encina.Security.Audit/AuditEntry.cs, AuditQuery.cs, Abstractions/IAuditStore.cs, src/Encina.Compliance.NIS2/NIS2CompliancePipelineBehavior.cs:275-333, docs/plans/evidential-read-audit-implementation-plan-1193.md.
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ⏭️ | Decisions are not cached (stale-attribute evidence). Policy-cache hit/miss is only a metric here (Phase 6, `CachingPolicyStoreDecorator`); no deferred issue needed beyond that metric. |
| 2 | OpenTelemetry | ✅ | Span `ABAC.DecisionAudit.Record`, counters and histogram on the existing "Encina.Security.ABAC" source/meter; no identifiers in tags (REQ-062). `Encina.OpenTelemetry` does not subscribe to that source today: deferred as `[DEBT]`. |
| 3 | Structured Logging | ✅ | EventIds 9072-9083 in `ABACLogMessages`, codes only; existing 9004, 9005, 9014 switch to codes. |
| 4 | Health Checks | ✅ | `ABACHealthCheck` reports decision-audit state (Phase 5); no write probe. |
| 5 | Validation | ✅ | `ABACOptionsValidator` with `ValidateOnStart`; reader query validation. |
| 6 | Resilience | ✅ (timeout only) | `WriteTimeout`; retries are not applicable on the request path (latency, duplicate rows); store resilience belongs to providers. |
| 7 | Distributed Locks | ❌ | Append-only inserts with unique v7 ids; no shared mutable state. |
| 8 | Transactions | ✅ | By isolation: own scope + `TransactionScope(Suppress)`, so business rollback never removes a deny row. |
| 9 | Idempotency | ✅ | `DecisionId` = `AuditEntry.Id`, duplicate check on ambiguous failure. Each evaluation is otherwise a distinct event and must be recorded. |
| 10 | Multi-Tenancy | ✅ | `TenantId` on every entry, reader tenant guard; automatic store-level tenant filtering stays in #798 (v0.18.0). |
| 11 | Module Isolation | ⏭️ | `abac.module_id` in metadata now; a queryable `ModuleId` on `AuditEntry`/`AuditQuery` for all providers is deferred as `[FEATURE]` (related #753). |
| 12 | Audit Trail | ✅ | This is the feature. |

Deferred issues proposed to the orchestrator (see `deferredIssues` in the report): provider registration `[BUG]` (Phase 0), `ModuleId` on audit entries, OTel subscription of ABAC, repo-wide `TryAddTransient` open-generic behavior collision, `AuditPipelineBehavior` storing message text, ABAC fail-open gaps, auditing of `ObligationExecutor` overrides and direct PDP callers.

---

## Risks

- **Availability and latency**: every protected request waits for one insert; with `FailClosed` an audit-store outage denies requests that would proceed. Mitigations: opt-in, `Outcomes` filter (for example Denied only), `WriteTimeout`, health check, benchmark under #924.
- **Hard dependency on the Phase 0 bug**: only EF Core and Marten register `IAuditStore` today.
- **Behavior changes riding along** (each needs a test and the `security` fragment): handler exceptions no longer reported as `abac.evaluation_failed`; missing context now denies; resource attributes now get the request; the ABAC behavior is no longer silently skipped when another open-generic behavior was registered first (apps that were unknowingly unprotected will start enforcing).
- **Repo-wide registration collision**: about 15 other packages use `TryAddTransient(typeof(IPipelineBehavior<,>), ...)`; only ABAC is fixed here.
- **Personal data**: ids are plaintext on relational and MongoDB stores; only Marten encrypts `UserId` and `Metadata`. Attribute values off by default; retention default 2555 days with `EnableAutoPurge` false; relational purge is a hard delete.
- **Warn mode**: would-deny is stored as `Success` with `abac.enforced=false`; auditors must filter on the metadata.
- **`RuleId` is representative** for `*-overrides` algorithms; the trace keeps all candidates.
- **Obligation side effects** remain if the write fails closed after OnPermit obligations ran.
- **Double rows** when `AuditPipelineBehavior` is also registered (different events, joined by `CorrelationId`).
- **Direct PDP callers** are not audited.
- **Isolated scope assumption**: tenant and connection resolution in a new scope must come from ambient state (`IRequestContextAccessor`, `ModuleExecutionContext`); a database-per-tenant setup holding the tenant in a scoped object would write to the default database (revisit with #798).
- **Out of scope defects** (own issues): a failed standalone-policy retrieval is non-fatal and can skip Deny policies (`XACMLPolicyDecisionPoint.cs:113-135`); `RequirePolicy.PolicyName` and `RequireCondition` are ignored at request time; `ObligationExecutor` does not catch handler exceptions and logs `error.Message` (`:115-117,200-202`); `AuditPipelineBehavior` stores `error.Message` and classifies by text (`AuditPipelineBehavior.cs:104,121,187-198`).

---

## Open Questions for the Maintainer

1. **Milestone**: SPEC-002 DEC-014 reserves v0.14.0 for defects, yet #751 is a P0 `[FEATURE]` kept there (SPEC-002 lines 555 and 789). Keep it, or move it (for example v0.17.0, together with REQ-051 break-glass)?
2. **Prerequisite sequencing**: may #751 merge before the Phase 0 `[BUG]` (durable only on EF Core and Marten, startup warning elsewhere), or must Phase 0 land first so all 10 providers are claimed?
3. **Deviations from the issue text**: `PolicyEvaluated` as a trace inside one record (not one row per policy), `PolicyCacheHit` as a metric only, `DecisionAudit.Enabled` instead of `AuditAbacDecisions`. Confirm.
4. **Fail-closed default**: confirm `FailClosed` as the default for an opt-in audit (AGENTS.md section 3, SPEC-002 section 8) rather than `BestEffort`.
5. **Scope of adjacent fixes**: this plan fixes missing-context denial, resource attributes, the PEP registration collision and the PAP scope/time/log issues inside #751. Confirm, or split them into separate issues.
6. **Buffered writer**: keep it as a follow-up gated on the #924 benchmark, not in the first cut. Confirm.
7. **ADR number**: ADR-032 is assumed free (index ends at 031 at base 5ea45982).
8. **Plan prompt typo**: `implementation-plan-prompt.md:113` says "section g)" for the matrix, which is section f). Fix separately?

---

## Next Steps

1. Maintainer answers the Open Questions and confirms the milestone.
2. Orchestrator opens the Phase 0 `[BUG]` and the deferred issues, then links this plan from #751.
3. Implement Phases 1-8 in one worktree, with `adversarial-reviewer` self-review and the local CRAP gate table before opening the PR.
