# Implementation Plan: Per-tenant AI Act enforcement for `Encina.Compliance.AIAct`

> **Issue**: [#845](https://github.com/dlrivada/Encina/issues/845)
> **Type**: Feature
> **Complexity**: Medium (8 phases, no database provider code, five pull requests plus one conditional)
> **Estimated Scope**: ~16 production files and ~18 test files; five pull requests (a sixth after #686)
> **Specification**: [SPEC-002](../specifications/SPEC-002-eu-regulatory-readiness.md) REQ-061, INV-008, DEC-006; **Sibling plans** (shared files and shared decisions, see Prerequisites): #847, #842, #686, #1257 (owns `ITenantScopeResolver`)

---

## Summary

Let the AI Act gate of `Encina.Compliance.AIAct` behave per tenant: a tenant that is subject to the AI Act gets the global enforcement; a tenant that is territorially outside it can be declared so explicitly, with a recorded justification; a multi-tenant application with no tenant on the request is denied, never silently run with the permissive global mode.

**State of the code on `7a0049c07` (confirms and extends the issue's own "State today").**

- `AIActCompliancePipelineBehavior<,>` (Transient, `src/Encina.Compliance.AIAct/AIActCompliancePipelineBehavior.cs`) reads `AIActOptions.EnforcementMode` once at construction (`_options = options.Value`) and returns `nextStep()` when it is `Disabled`; prohibited practices block in `Block` and `Warn` alike (Art. 5). `AIActOptions` (`AIActOptions.cs`) has `EnforcementMode`, `AutoRegisterFromAttributes`, `AddHealthCheck`, `AssembliesToScan`. `BlockProhibitedUses` and `EnforceHumanOversight` of the issue's example do not exist.
- `IAISystemRegistry` has five async members plus the synchronous `bool IsRegistered(string systemId)` that the validator and the auto-registration use; the only implementation is `InMemoryAISystemRegistry` (`TryAddSingleton`). Nothing carries a tenant: `AISystemRegistration`, `HumanDecisionRecord`, `AISystemReclassifiedNotification(SystemId, Reason, OccurredAtUtc)`, `ProhibitedUseBlockedNotification`.
- `ITenantContext` and a `TenantId` type do not exist. The shipped fail-closed pattern is the ambient `IRequestContext.TenantId` plus `MultiTenancyMarker` (`DefaultConsentService.TryResolveWriteTenantScope`, `ABACDecisionAuditReader.ResolveTenant`, `DeadLetterManager`). `Encina.Compliance.AIAct` references only `Encina`, so using this pattern needs no new project reference (the issue's `ITenantProvider` route would add one to `Encina.Tenancy`).
- **Overlap that changes the scope.** The merged plan for #847 (`aiact-marten-event-sourcing-implementation-plan-847.md`, p0-mandatory) replaces `IAISystemRegistry` and `IHumanOversightEnforcer` by `IAISystemService` and `IHumanOversightService`, stamps `TenantId` on every event, aggregate and read model, derives stream ids from `(tenantId, systemId)`, segments cache keys by tenant and fails closed without a tenant (its Design Choice 7A), and leaves per-tenant behavior to this issue. The merged plan for #686 gates the AI Act through `IComplianceApplicabilityResolver` and allows a skip only as binary territorial non-applicability, recorded and audited (its Design Choices 8 and 9). The maintainer's rewrite of this issue on 2026-11-11 already settles that `Disabled` per tenant is "an explicit, logged and audited tenant configuration for territorial non-applicability, never a convenience tier".

**Scope.** (1) `ConfigureTenant` per-tenant overrides on `AIActOptions`, resolved per request; (2) the gate resolves the tenant, fails closed when it is required and absent; (3) a recorded skip for a tenant declared non-applicable; (4) `TenantId` on the notifications and records that survive #847; (5) the tenant in metrics, activity tags and logs; (6) resolver integration when #686 lands; (7) the tenant-scoped system lookup, which is delivered by #847 (Design Choice 1, recommended) and has a fallback task list if the maintainer chooses otherwise.

**Out of scope.** A persistent tenant registry (#847); module-scoped enforcement (#846); dynamic per-tenant configuration from a store (the resolver of #686 owns tenant declarations); AI Act date logic (SPEC-002 INV-003 applies to defaults, none change here); **the AI Act satellite packages** (`Encina.Compliance.AIAct.BiasDetection` #838, `.TechnicalDocumentation` #840, `.Transparency` #841, `.GPAI` #843): this plan changes `Encina.Compliance.AIAct` only and the satellite plans did not receive a tenant deliverable from it. Each satellite carries its own tenant decision in its plan (the #843 plan integrates `TenantId` in its Marten registry; #840 and #841 persist nothing and inherit tenant scoping of the system lookup from the #847 service), and the follow-up issue file `artifacts/issues/aiact-satellites-per-tenant-enforcement.md` records the remaining gap: per-tenant `ConfigureTenant` overrides and the skip record do not reach the satellites' own gates until it is done.

**Affected packages**: `Encina.Compliance.AIAct` only (plus `src/Encina/Diagnostics/EventIdRanges.cs` for the new range of Phase 7). **Provider category**: none (no database, cache, transport or lock provider code).

---

## Design Choices

<details>
<summary><strong>1. Who delivers the tenant-scoped AI system lookup: #847 (Marten services) or a new `ITenantAwareAISystemRegistry` here</strong></summary>

**Class:** Dilemma (recommended, pending the maintainer)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) #847 delivers the tenant-scoped lookup (stream id from tenant and system id, list queries filtered, cache keys segmented, global fallback); #845 builds per-tenant options and the gate on top** | No throw-away code; one API for systems; the issue's acceptance items 1, 2 and 5 are met by `IAISystemService` | #845's registry items wait for #847; if #847's recommendation is changed by the maintainer this plan's Phase 5 changes |
| **B) Add `ITenantAwareAISystemRegistry` and `InMemoryTenantAwareAISystemRegistry` now (the issue text), keep `IAISystemRegistry`** | Independent of #847; small | #847 Design Choice 1A deletes `IAISystemRegistry`, so the new interface is rewritten or removed within the same milestone; two registries for a while ("no compatibility layers", `AGENTS.md` section 3) |
| **C) Fold the tenant-scoped lookup of #847 and the per-tenant options of #845 into one delivery** | One review | Two large features in one pull request over the size limits |

### Chosen Option: **A — #847 delivers the lookup; this plan adds the options and the gate** (recommended, pending the maintainer)

### Rationale

- A wins when #847 is in 1.0, which it is (`p0-mandatory`); B wins only if #847 slips beyond the 1.0 line or its Design Choice 1 is rejected, and then Phase 5 of this plan lists the B tasks.
- Per-tenant options and the gate do not depend on how systems are stored: the gate needs the tenant and the effective mode, not the registry.

</details>

<details>
<summary><strong>2. Where the tenant comes from: the shared scope resolver of #1257 or `ITenantProvider`</strong></summary>

**Class:** Dilemma (recommended, pending the maintainer)

This entry contradicts a Settled entry of the issue: the maintainer's rewrite of #845 on 2026-11-11 records, as Settled choice 1, "resolve the tenant through the abstraction that exists, `ITenantProvider` with string ids". An earlier draft of this plan substituted `ITenantScopeResolver` and called it Trivial; it is put to the maintainer here instead.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ITenantScopeResolver` (core, owned by the #1257 plan, not on `main` yet) over `IRequestContextAccessor` and `MultiTenancyMarker`** | No new project reference; the same rule as Consent, ABAC and dead letters; one place to change when #456 delivers a typed tenant | A hard prerequisite on an open plan (#1257): Phases 1-2 start only after its primitive merges; departs from the issue's Settled choice 1 |
| **B) `ITenantProvider` from `Encina.Tenancy` (exists on `main`, `src/Encina.Tenancy/Abstractions/ITenantProvider.cs`), as the issue's Settled choice 1 says; the resolver is switched to the shared primitive when #1257 lands** | Follows the maintainer's Settled choice; no wait for #1257; string ids | Adds a project reference from a compliance package to `Encina.Tenancy`; the fail-closed rule (marker and no tenant) must be coded here until the primitive exists |
| **C) `ITenantContext` and `TenantId`** | Typed | Do not exist; #456 is open |

### Chosen Option: **B — `ITenantProvider` now, the shared resolver when #1257 lands** (recommended, pending the maintainer)

### Rationale

- We recommend B because it honours the issue's Settled choice 1 and has no ordering dependency on an unmerged plan; A wins if the maintainer prefers not to reference `Encina.Tenancy` from a compliance package or #1257 merges first, in which case the plan below is unchanged (Phase 2 already names `ITenantScopeResolver`) and Phase 1 only waits for the first PR of #1257.
- Under B, `AIActEffectiveOptionsResolver` takes `ITenantProvider` and the `MultiTenancyMarker` check itself (marker present and no tenant: `Left(aiact.tenant_context_required)`), and the dependency on `Encina.Tenancy` is declared in Phase 1 (project reference, `PublicAPI` unaffected). The resolver is internal, so swapping the source later changes one class and its tests.
- With no marker (single-tenant application) a null tenant is returned and the global options apply under both options: every feature stays opt-in (`AGENTS.md` section 1).

</details>

<details>
<summary><strong>3. What a tenant may override: the issue's Block, Warn or Disabled, or only inherit-or-not-applicable</strong></summary>

**Class:** Dilemma (recommended, pending the maintainer)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `AIActTenantOptions.EnforcementMode` accepts Block, Warn and Disabled (the issue's motivation: "Tenant B only needs logging")** | Flexible for SaaS operators | A Warn tier for a tenant that is inside the EU is a commercial relaxation of a legal duty; against the never-optional principle |
| **B) A tenant either inherits the global mode or is declared not applicable (`Disabled`), which requires a `NotApplicableReason` and a `TerritorialBasis` and is recorded on every skip; Warn is not available per tenant** | Matches the maintainer principle of 2026-10-30 (DC9 of the #686 plan: binary, no WarnOnly for in-scope subjects) and the issue's Settled choice 2 | A tenant that wants log-only handling for evaluation must use a separate non-production deployment |
| **C) B plus a per-tenant Warn during a documented transition period** | Eases rollout | A time-bounded exemption is still a relaxation; needs its own approval trail |

### Chosen Option: **B — Inherit or not-applicable with a recorded justification** (recommended, pending the maintainer)

### Rationale

- A wins when the operator's contract covers non-EU deployments only and no EU tenant exists; B wins whenever any tenant can be inside the EU, which is the application the specifications target (SPEC-002 section 4.1).
- Under B, `AIActTenantOptions` has only `EnforcementMode` restricted to `Inherit` (null) or `Disabled`, `NotApplicableReason` and `TerritorialBasis`; the validator rejects `Warn`, a missing reason and a blank tenant id.

</details>

<details>
<summary><strong>4. The issue's example options `BlockProhibitedUses` and `EnforceHumanOversight` are not added</strong></summary>

**Class:** Settled
**Settled by:** Regulation (EU) 2024/1689 Art. 5 (prohibited practices are banned, not configurable) and Art. 14 (human oversight for high-risk systems), the maintainer principle of 2026-10-30 on [#686](https://github.com/dlrivada/Encina/issues/686#issuecomment-6521534413), and the shipped behavior that blocks `compliance.IsProhibited` in Block and Warn alike (`AIActCompliancePipelineBehavior.cs`).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Do not add the two switches; prohibited uses always block, oversight stays attribute-driven** | No switch that disables a legal duty | The issue's example snippet changes |
| **B) Add both as per-tenant booleans** | Matches the example | A tenant could switch off the blocking of a prohibited practice |

### Chosen Option: **A — No new switches**

### Rationale

- The only valid reason to skip a control is that the law does not apply, which is what the not-applicable declaration of Design Choice 3 expresses.

</details>

<details>
<summary><strong>5. Resolution rule and error behavior</strong></summary>

**Class:** Settled
**Settled by:** the issue's Settled choice 3 (from `AGENTS.md` section 3, fail closed when context is missing), SPEC-002 DEC-006, and the shipped Consent behavior (`ConsentErrors.TenantRequired`).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) No marker: global options. Marker plus tenant: the tenant's override or, without one, the global options. Marker and no tenant: a `Left` with `aiact.tenant_context_required`, no fallback** | Fail closed; single-tenant apps pay nothing | A background job with no tenant must set one (#1164 rebuilds it) |
| **B) Marker and no tenant: fall back to the global mode** | Never blocks | A permissive fallback in a compliance gate |

### Chosen Option: **A — Fail closed**

### Rationale

- The error code `aiact.tenant_context_required` and the option `AIActOptions.RequireTenantContext` are introduced by #847 (its Phase 4 and 6). The order is fixed: #847 lands first and this plan reuses the exact names; it adds no conditional production branch, so the content of each pull request does not depend on merge order.

</details>

<details>
<summary><strong>6. Per-tenant configuration is static, in `AIActOptions.ConfigureTenant`</strong></summary>

**Class:** Trivial
**Settled by:** the issue's Proposed Solution (`options.ConfigureTenant("tenant-eu", ...)`) and the tenant list shape of `TenancyOptions.Tenants` (`src/Encina.Tenancy/Configuration/TenancyOptions.cs`).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Static overrides in `AIActOptions`, validated at start-up** | Simple, testable; no store | A tenant change needs a deployment |
| **B) Read overrides from `ITenantStore`/`TenantInfo` at request time** | Dynamic | Couples a compliance package to `Encina.Tenancy`; the resolver of #686 already owns tenant declarations |

### Chosen Option: **A — Static overrides**

### Rationale

- Dynamic tenant declarations arrive with `TenantAwareComplianceResolver` (#686); when it lands the static map becomes the fallback, and a follow-up issue decides whether it stays.

</details>

---

## Implementation Phases

### Phase 1: Core Models & Options

> **Goal**: the per-tenant option types and validation (PR 1, with Phase 2). Prerequisites: the #847 core PR (names `RequireTenantContext`, `aiact.tenant_context_required`); the tenant source of Design Choice 2 (`ITenantProvider` under the recommended option B, `ITenantScopeResolver` of #1257 under A).

<details>
<summary><strong>Tasks</strong></summary>

1. **Create `src/Encina.Compliance.AIAct/AIActTenantOptions.cs`**: `public sealed class AIActTenantOptions` with `AIActEnforcementMode? EnforcementMode { get; set; }` (null inherits; only `Disabled` is accepted), `string? NotApplicableReason`, `string? TerritorialBasis`, and `ToString()`.
2. **Modify `AIActOptions.cs`**: `public AIActOptions ConfigureTenant(string tenantId, Action<AIActTenantOptions> configure)`, an internal read-only map `TenantOverrides` (case-sensitive ordinal, blank ids rejected); `RequireTenantContext` already exists (#847) and is only read.
3. **Modify `AIActOptionsValidator.cs`**: a tenant override with `Warn` fails; `Disabled` without `NotApplicableReason` and `TerritorialBasis` fails; duplicate or blank tenant ids fail; the failure messages never print the reason text.
4. `aiact.tenant_context_required` and its factory come from #847; nothing is added to `AIActErrors.cs`.
5. PublicAPI lines (mechanical-fixer). Under Design Choice 2 option B, add the project reference to `Encina.Tenancy`.
6. **Tests**: `AIActOptionsValidator` unit tests per rule; `AIActOptions` unit and guard tests (`ConfigureTenant`); a property test extending the existing validator property suite with generated tenant maps (any `Warn`, any `Disabled` without reason or basis, any blank or duplicate id is rejected; every valid map is accepted).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
- Encina.Compliance.AIAct; issue #845. AIActOptions is a sealed settings class with four properties; AIActOptionsValidator is an IValidateOptions<AIActOptions>.
- Design Choices 3 (inherit or not applicable), 4 (no new switches), 6 (static overrides).

TASK:
Add AIActTenantOptions, ConfigureTenant and the validation rules of the plan's Phase 1. Do not touch the behavior yet.

KEY RULES:
- Options with free text must override ToString() and never print the reason (AGENTS.md section 3).
- XML docs on every public member; no [Obsolete]; zero warnings.
- ConfigureTenant returns the options for chaining; blank tenant ids are rejected with ArgumentException.

REFERENCE FILES:
- src/Encina.Compliance.AIAct/AIActOptions.cs
- src/Encina.Compliance.AIAct/AIActOptionsValidator.cs
- src/Encina.Tenancy/Configuration/TenancyOptions.cs (Tenants list, ToString)
```

</details>

### Phase 2: The Effective-Options Resolver

> **Goal**: one class that answers "what applies to this request" (PR 1, with Phase 1). Tests: unit (five paths), guard, and a property test that resolution is total over every combination of marker present or absent, tenant present or absent and override absent, inheriting or `Disabled`, and never returns a permissive mode for marker-and-no-tenant.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create `AIActEffectiveOptionsResolver.cs`** (`internal sealed`): constructor `(IOptions<AIActOptions> options, ITenantScopeResolver tenants)` (under Design Choice 2 option B: `ITenantProvider` plus the `MultiTenancyMarker` check, same `Either` contract); `Either<EncinaError, AIActEffectiveOptions> Resolve()` where `AIActEffectiveOptions` is a `readonly record struct (AIActEnforcementMode Mode, string? TenantId, bool NotApplicable, string? ReasonCode)`.
2. Rules (Design Choice 5): scope from `tenants.ResolveRead(null)`; denied scope returns the `Left`; a tenant with an override whose mode is `Disabled` returns `NotApplicable = true`; otherwise the global mode.
3. Register as a singleton in `ServiceCollectionExtensions.cs` with `TryAddSingleton`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
- Phase 1 merged. ITenantScopeResolver.ResolveRead(string?) returns Either<EncinaError, TenantScope>; a null tenant means single-tenant.

TASK:
Implement AIActEffectiveOptionsResolver and its registration; unit tests for single-tenant, tenant with override, tenant without override, multi-tenant without tenant (Left), not-applicable tenant.

KEY RULES:
- The resolver is allocation-light: it is on the hot path of every decorated request.
- CRAP <= 10 per method: one decision per private method.
- No tenant id in error messages or logs.

REFERENCE FILES:
- src/Encina.Compliance.Consent/Services/DefaultConsentService.cs
- src/Encina.Compliance.AIAct/ServiceCollectionExtensions.cs
```

</details>

### Phase 3: Pipeline Behavior

> **Goal**: the gate uses the effective options per request (PR 3, after Phase 4 so that the skip notification exists).

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify `AIActCompliancePipelineBehavior.cs`**: inject `AIActEffectiveOptionsResolver` (constructor parameter, null checks); in `DispatchAsync` resolve first (the existing `_options.EnforcementMode == Disabled` check becomes `effective.Mode == Disabled || effective.NotApplicable`); a `Left` from the resolver is returned as the result of the request (fail closed); a not-applicable tenant records the skip (Phase 4 notification) and calls `nextStep()`.
2. Keep the static attribute cache; the tenant lookup happens after the attribute check so requests without AI attributes pay nothing.
3. Update the `AIActAutoRegistrationHostedService` note: global registrations keep a null tenant (see #847 `AutoRegistrationTenantId`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT:
- The behavior is Transient and caches attributes per generic type in a static dictionary; Handle receives the request context.

TASK:
Wire the resolver into DispatchAsync as described; extend the existing behavior unit and contract tests with tenant cases.

KEY RULES:
- Disabled for a tenant never skips the prohibited-practice check for another tenant.
- A Left from the resolver fails the request; never swallowed.
- Existing public signatures: only the constructor changes (pre-1.0).

REFERENCE FILES:
- src/Encina.Compliance.AIAct/AIActCompliancePipelineBehavior.cs
- tests/Encina.UnitTests/Compliance/AIAct (existing behavior tests)
```

</details>

### Phase 4: Notifications & Records

> **Goal**: the tenant on the events and records that stay after #847 (PR 2, before the behavior change of Phase 3).

<details>
<summary><strong>Tasks</strong></summary>

1. Add `string? TenantId` to `ProhibitedUseBlockedNotification`, `HumanOversightRequiredNotification` and `HumanDecisionRecord`; `AISystemReclassifiedNotification` is checked against `main` after #847 has landed: if the type still exists it gains the field, otherwise nothing is done (the #847 events carry the tenant themselves).
2. **Create `Notifications/AIActNotApplicableNotification.cs`**: `sealed record (string RequestTypeName, string TenantId, string ReasonCode, DateTimeOffset OccurredAtUtc) : INotification` published for each skipped request through the `IEncina` already used for reclassification.
3. Fill the tenant at the publishing sites with the resolved scope.
4. **Manifest**: the files this PR modifies have only default entries today (`Notifications/ProhibitedUseBlockedNotification.cs`, `Notifications/HumanOversightRequiredNotification.cs`, `Model/HumanDecisionRecord.cs`); add per-file targets with one-sentence justifications (Phase 8 table).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT:
- Notifications are sealed records implementing INotification; timestamps end in AtUtc.

TASK:
Add the tenant field and the new not-applicable notification; adjust the constructors at every call site; update tests.

KEY RULES:
- The reason code is a fixed code; the free-text NotApplicableReason is never published.
- If a Publish returns Left for the skip notification, the request fails (the skip must be recorded, DC15 of the #686 plan).

REFERENCE FILES:
- src/Encina.Compliance.AIAct/Notifications/ProhibitedUseBlockedNotification.cs
- src/Encina.Compliance.AIAct/InMemoryAISystemRegistry.cs (Publish at line 109)
```

</details>

### Phase 5: Tenant-Scoped System Lookup (Design Choice 1)

> **Goal**: systems are scoped by tenant. Option A (recommended): delivered by #847, nothing to build here beyond a test (PR 3).

<details>
<summary><strong>Tasks</strong></summary>

1. **If option A**: add `AIActTenantSystemScopingContractTests` that registers two tenants' systems through `IAISystemService` and proves the gate for tenant A never sees B's system; no production file.
2. **If option B (fallback)**: create `Abstractions/ITenantAwareAISystemRegistry.cs` (`GetSystemAsync(string tenantId, string systemId, ct)`, `RegisterSystemAsync(string tenantId, AISystemRegistration, ct)`, `GetAllSystemsAsync(string tenantId, ct)`, `IsRegistered(string tenantId, string systemId)`), `InMemoryTenantAwareAISystemRegistry.cs` (dictionary keyed by `(tenantId, systemId)`, global registrations under a null tenant, lookup tenant first then global) and its `TryAddSingleton` registration; the validator, classifier and documentation use the tenant overload.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT:
- Design Choice 1. Under option A the lookup is #847's IAISystemService (tenant in the stream id and in cache keys).

TASK:
Write the contract test of the chosen option (and the B classes only if the maintainer chose B).

KEY RULES:
- A system of another tenant is "not found", never returned.
- Under B, IsRegistered stays synchronous only for the in-memory implementation.

REFERENCE FILES:
- the #847 plan, Phase 6 (sibling plan under docs/plans/)
- src/Encina.Compliance.AIAct/InMemoryAISystemRegistry.cs
```

</details>

### Phase 6: Applicability Resolver Integration (conditional on #686)

> **Goal**: consult `IComplianceApplicabilityResolver` when it exists (PR 6, a separate pull request after #686 merges).

<details>
<summary><strong>Tasks</strong></summary>

1. When the contract of #686 has landed: in `AIActEffectiveOptionsResolver`, if an `IComplianceApplicabilityResolver` is registered, ask it first for the AI Act capability; its `NotApplicable` verdict (with traced evidence) is the skip; the tenant `Disabled` declaration of Design Choice 3 is consulted only when no resolver is registered.
2. Until then: no code; the phase is a task note in the issue.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```text
CONTEXT:
- #686 gates every compliance behavior through the resolver; AIAct is in its list (Design Choice 8, option B). The resolver is add-only except for traced territorial non-applicability.

TASK:
Only after #686 has merged: add the optional resolver dependency and tests; otherwise skip.

KEY RULES:
- Resolver verdicts and tenant declarations never widen what applies; a Left from the resolver fails closed.

REFERENCE FILES:
- the #686 plan (sibling plan under docs/plans/)
```

</details>

### Phase 7: Observability

> **Goal**: the tenant in tags, one counter, log messages, and the new EventId range (PR 4).

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify `Diagnostics/AIActDiagnostics.cs`**: the tenant tag on the existing activity and counters, only when a tenant is resolved (ADR-054 D4: no tag when multi-tenancy is off), through the key of the shared helper of #2676; until it exists, `TagTenantId = "encina.tenant_id"`, the key proposed by the #1257 plan. New counter `aiact.tenant.not_applicable` (`Counter<long>`, lowercase dot name per ADR-054, tag `aiact.request_type`).
2. **Register a new EventId range** in `src/Encina/Diagnostics/EventIdRanges.cs`: `public static readonly (int Min, int Max) ComplianceAIActGPAI = (5950, 5969);` (the block assigned in `eventid-blocks.md`; `ComplianceAIAct` 9500-9599 is fully allocated to the AI Act family, so no id is left there for these events). Add the field to `src/Encina/PublicAPI.Unshipped.txt`, and extend the assembly map of `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs`: `["Encina.Compliance.AIAct"] = [nameof(EventIdRanges.ComplianceAIAct), nameof(EventIdRanges.ComplianceAIActGPAI)]` (the log messages stay in the `Encina.Compliance.AIAct` assembly).
3. **Create `Diagnostics/AIActTenantLogMessages.cs`** (`[LoggerMessage]`, EventIds 5950-5957 packed, inside `ComplianceAIActGPAI`; XML line `Event IDs: 5950-5957 (see EventIdRanges.ComplianceAIActGPAI)`): tenant required, tenant not applicable (code only), tenant override loaded at start-up, invalid override, and four more as the worker needs them without leaving gaps.
4. Log scopes carry the tenant id only in structured fields already allowed for tenant ids; never the reason text.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```text
CONTEXT:
- ADR-021 (EventIds), ADR-054 D4 (tenant tag only when multi-tenancy is on).

TASK:
Add the tag, the counter, the new range ComplianceAIActGPAI (5950-5969) in EventIdRanges.cs with its PublicAPI line and the AssemblyRanges map entry, and the log messages 5950-5957.

KEY RULES:
- EventIds packed sequentially from 5950; ComplianceAIAct 9500-9599 is fully allocated (9500-9512 core, 9513-9529 #847, 9530-9547 #842, 9548-9561 #838, 9562-9599 #840/#841/#843) and is not used by this plan.
- Never log EncinaError.Message or the free-text reason.

REFERENCE FILES:
- src/Encina.Compliance.AIAct/Diagnostics/AIActDiagnostics.cs
- src/Encina.Compliance.AIAct/Diagnostics/AIActLogMessages.cs
- src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

### Phase 8: Testing and Documentation

> **Goal**: per-flag targets met; documents and fragments written. Tests ship with the PR that adds the code; PR 5 carries the three justification files and the documentation.

<details>
<summary><strong>Tasks</strong></summary>

| Test type | Scope | Location |
|-----------|-------|----------|
| Unit | resolver (5 cases), behavior with tenant cases, validator rules, options map, notification fields | `tests/Encina.UnitTests/Compliance/AIAct/` |
| Guard | new constructors and public methods (`ConfigureTenant`) | `tests/Encina.GuardTests/Compliance/AIAct/` |
| Contract | gate behavior for two tenants with real registry/classifier/validator (extends the existing behavior contract tests) | `tests/Encina.ContractTests/Compliance/AIAct/` |
| Property | implemented, not justified: the validator rules over generated tenant maps (`AIActValidatorPropertyTests.cs`, Phase 1) and resolver totality over the marker, tenant and override combinations (`AIActPropertyTests.cs`, Phase 2) | `tests/Encina.PropertyTests/Compliance/AIAct/` |
| Integration | justification `.md`: in-memory registry, no external dependency (the tenant-scoped Marten lookup is tested by #847) | `tests/Encina.IntegrationTests/Compliance/AIAct/MultiTenancy/MultiTenancy.md` |
| Load | justification `.md`: not a concurrent hot path | `tests/Encina.LoadTests/Compliance/AIAct/MultiTenancy/MultiTenancy.md` |
| Benchmark | justification `.md`: one dictionary lookup | `tests/Encina.BenchmarkTests/Compliance/AIAct/MultiTenancy/MultiTenancy.md` |

**Per-file targets** (proposed and demanding: each is above the current manifest value where the PR adds branches the tests will reach; measured values replace them, never lower; existing targets from `.github/coverage-manifest/Encina.Compliance.AIAct.json`):

| File | Flags | Justification |
|------|-------|---------------|
| `AIActCompliancePipelineBehavior.cs` (today unit 97, guard 8, contract 95) | unit 98, guard 10, contract 96 | the new branches are the resolver `Left`, the not-applicable skip and the skip-publish failure, each driven by a unit test, and the contract test adds two tenants; the new constructor null check raises the guard figure |
| `AIActOptionsValidator.cs` (today guard 95, property 95) | unit 98, guard 95, property 96 | every new rule (Warn, missing reason or basis, blank and duplicate ids) has a unit test and a generated-map property |
| `AIActEffectiveOptionsResolver.cs` (new) | unit 100, guard 90, property 90 | five paths each tested, plus the totality property; the guard figure covers the constructor null checks |
| `AIActOptions.cs` (today guard 95) | unit 95, guard 95 | `ConfigureTenant` guard clauses and the ordinal tenant map |
| `AIActTenantOptions.cs` (new) | unit 95, guard 90 | settings class with a `ToString` that must not print the reason, each asserted |
| `Notifications/ProhibitedUseBlockedNotification.cs`, `Notifications/HumanOversightRequiredNotification.cs`, `Model/HumanDecisionRecord.cs` (today default entries, no targets) | unit 90, guard 90 each | the records are constructed with and without a tenant at every publishing site and the init lines are executed by those tests |
| `Notifications/AIActNotApplicableNotification.cs` (new) | unit 95, guard 90 | published by the behavior test; the record carries only fixed codes |
| `ServiceCollectionExtensions.cs` (today guard 90) | unit 95, guard 90 | the DI test resolves the new resolver under `ValidateOnBuild` and `ValidateScopes` |
| `Diagnostics/AIActDiagnostics.cs` (today unit 95) | unit 96 | the existing listener tests extend to the tenant tag and the new counter |
| `Diagnostics/AIActTenantLogMessages.cs` (new) | unit 90 | each message is invoked by the instrumented code under test |

Documentation: XML docs, `src/Encina.Compliance.AIAct/README.md` and the configuration guide (docs-writer), `changelog.d/845-aiact-multitenancy.added.md` (mechanical-fixer), `PublicAPI.Unshipped.txt`, the manifest update, `docs/INVENTORY.md`; build with 0 warnings.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```text
CONTEXT:
- Per-flag coverage (AGENTS.md section 9). Integration, load and benchmark are justified with the .md files listed; unit, guard, contract and property are implemented (the property tests are extensions of the existing AIAct property suites).

TASK:
Write the tests and justification files; delegate pages to docs-writer and fragments, PublicAPI and manifests to mechanical-fixer; run the measurement commands of AGENTS.md section 9.

KEY RULES:
- Tests execute real package code; Shouldly through Encina.Testing.Shouldly; no Thread.Sleep.
- Justification files use the template of AGENTS.md section 9.

REFERENCE FILES:
- tests/Encina.UnitTests/Compliance/AIAct
- .github/coverage-manifest/Encina.Compliance.AIAct.json
- changelog.d/README.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Requirement | Use in this feature |
|--------|-------------|---------------------|
| Regulation (EU) 2024/1689 Art. 2 | territorial scope | the only valid reason for a tenant to be not applicable |
| Art. 5 and Art. 14 | prohibited practices; human oversight | no per-tenant switches (Design Choice 4) |
| Art. 4 as replaced, Art. 5(1)(ba) and (bb), Art. 50(2), Annex III dates (Reg. (EU) 2026/1744) | dates in the maintainer's comment on #845 | defaults follow the law applicable on the date (SPEC-002 INV-003); no default changes here |
| SPEC-002 REQ-061, INV-008, DEC-006 | tenant awareness; fail closed | Design Choices 2 and 5 |
| #686 maintainer principle (2026-10-30) | compliance never optional, only territorial non-applicability | Design Choice 3 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
| :--- | :--- | :--- |
| AIActOptions (EnforcementMode, AutoRegisterFromAttributes, AddHealthCheck, AssembliesToScan) | src/Encina.Compliance.AIAct/AIActOptions.cs | gains ConfigureTenant and the per-tenant overrides |
| AIActOptionsValidator | src/Encina.Compliance.AIAct/AIActOptionsValidator.cs | start-up validation of tenant overrides |
| AIActCompliancePipelineBehavior | src/Encina.Compliance.AIAct/AIActCompliancePipelineBehavior.cs | resolves the effective options of the current tenant before the Disabled check |
| IAISystemRegistry and InMemoryAISystemRegistry | src/Encina.Compliance.AIAct/Abstractions/IAISystemRegistry.cs, src/Encina.Compliance.AIAct/InMemoryAISystemRegistry.cs | registry the tenant-scoped lookup is built on or replaced by (#847) |
| AIActDiagnostics | src/Encina.Compliance.AIAct/Diagnostics/AIActDiagnostics.cs | counters and tags (aiact.request_type, aiact.outcome) gain the tenant |
| AIActLogMessages (EventIds 9500-9512) | src/Encina.Compliance.AIAct/Diagnostics/AIActLogMessages.cs | existing log style; new ids start at 5950 in the new range |
| ProhibitedUseBlockedNotification, HumanOversightRequiredNotification, AISystemReclassifiedNotification | src/Encina.Compliance.AIAct/Notifications/ | gain the tenant |
| HumanDecisionRecord | src/Encina.Compliance.AIAct/Model/HumanDecisionRecord.cs | gains the tenant |
| MultiTenancyMarker and IRequestContextAccessor | src/Encina/Abstractions/MultiTenancyMarker.cs, src/Encina/Abstractions/IRequestContextAccessor.cs | multi-tenancy signal and ambient tenant |
| DefaultConsentService.TryResolveWriteTenantScope | src/Encina.Compliance.Consent/Services/DefaultConsentService.cs | fail-closed tenant pattern already shipped |
| ABACDecisionAuditReader.ResolveTenant | src/Encina.Security.ABAC/DecisionAudit/ABACDecisionAuditReader.cs | same pattern in the ABAC package |
| Plan #847 (AI system service with tenant-scoped streams and cache keys) | the #847 plan | delivers the tenant-scoped system registry |
| Plan #686 (IComplianceApplicabilityResolver) | the #686 plan | applicability decision per tenant, AIAct is one gated capability |
| AIAct coverage manifest | .github/coverage-manifest/Encina.Compliance.AIAct.json | per-file targets to raise |

### Event ID Allocation

`ComplianceAIAct = (9500, 9599)` is fully allocated to the AI Act family and has no id left for this plan; this plan registers a new range from the block assigned in `eventid-blocks.md`.

| Range | Owner | Notes |
|---------|-------|-------|
| `ComplianceAIAct` 9500-9512 | core (existing) | on `main` |
| `ComplianceAIAct` 9513-9529 | #847 | |
| `ComplianceAIAct` 9530-9547 | #842 | |
| `ComplianceAIAct` 9548-9561 | #838 | |
| `ComplianceAIAct` 9562-9599 | #840 (9562-9575), #841 (9576-9587), #843 (9588-9599) | |
| **`ComplianceAIActGPAI` 5950-5969 (new)** | **#845: 5950-5957** | **new field in `EventIdRanges.cs`, new entry in the `Encina.Compliance.AIAct` item of `AssemblyRanges`, PublicAPI line (Phase 7)** |

### Estimated File Count

| Category | Files | Notes |
| :--- | :--- | :--- |
| Production, options, resolver, behavior, validator, errors, DI | 7 | modified 5, new 2 (`AIActTenantOptions`, `AIActEffectiveOptionsResolver`) |
| Production, diagnostics and log messages | 2 | modified 1, new 1 |
| Production, notifications and records (tenant field) | 5 | modified 4, new 1 |
| Production, EventId range | 2 | `EventIdRanges.cs` and `src/Encina/PublicAPI.Unshipped.txt` |
| Tests (unit, guard, contract, property, DI, architecture map) | 15 | new and modified |
| Justification files (integration, load, benchmark) | 3 | `.md` |
| Documentation (README, configuration guide, changelog fragment, PublicAPI, manifest, INVENTORY) | 6 | docs-writer and mechanical-fixer |
| Total | ~40 | five pull requests plus one conditional |

**Per-PR file lists** (test files, PublicAPI and manifest counted in every row; none exceeds 15 files or 1,500 lines):

| PR | Phases | Files | Count |
|----|--------|-------|-------|
| 1 | 1-2 | `AIActTenantOptions.cs`, `AIActOptions.cs`, `AIActOptionsValidator.cs`, `AIActEffectiveOptionsResolver.cs`, `ServiceCollectionExtensions.cs`, `Encina.Compliance.AIAct.csproj` (only under Design Choice 2 B), `PublicAPI.Unshipped.txt`, manifest, 6 test files (validator unit, options unit and guard, resolver unit and guard, validator and resolver property) | 14 |
| 2 | 4 | `ProhibitedUseBlockedNotification.cs`, `HumanOversightRequiredNotification.cs`, `HumanDecisionRecord.cs`, `AIActNotApplicableNotification.cs`, the publishing sites (2), `PublicAPI.Unshipped.txt`, manifest, 5 test files | 13 |
| 3 | 3 and 5 | `AIActCompliancePipelineBehavior.cs`, `AIActAutoRegistrationHostedService.cs` note, `PublicAPI.Unshipped.txt`, manifest, behavior unit, guard and contract tests, DI test, tenant-scoping contract test | 9 |
| 4 | 7 | `EventIdRanges.cs`, `src/Encina/PublicAPI.Unshipped.txt`, `EncinaEventIdAllocationTests.cs`, `AIActDiagnostics.cs`, `AIActTenantLogMessages.cs`, the instrumented sites (2), `PublicAPI.Unshipped.txt`, manifest, 2 test files | 11 |
| 5 | 8 | three justification `.md`, README, configuration guide, changelog fragment, INVENTORY, manifest | 9 |
| 6 (after #686) | 6 | resolver integration with its tests | about 6 |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Combined prompt — all phases</strong></summary>

```text
PROJECT CONTEXT:
- Encina, .NET 10 / C# 14, pre-1.0, ROP. Issue #845: per-tenant AI Act enforcement in Encina.Compliance.AIAct. ITenantContext does not exist; the tenant is IRequestContext.TenantId; multi-tenancy is on when MultiTenancyMarker is registered. Missing tenant context fails closed.
- Sibling plans: #847 (tenant-scoped AI systems and shared names: aiact.tenant_context_required, AIActOptions.RequireTenantContext), #686 (applicability resolver), #1257 (ITenantScopeResolver).

IMPLEMENTATION OVERVIEW:
1. AIActTenantOptions and AIActOptions.ConfigureTenant with validation (Inherit or Disabled with reason and territorial basis; no Warn; no BlockProhibitedUses or EnforceHumanOversight switches).
2. AIActEffectiveOptionsResolver over ITenantScopeResolver.
3. The pipeline behavior resolves per request; a Left fails the request; a not-applicable tenant is recorded and skipped.
4. TenantId on surviving notifications and records; AIActNotApplicableNotification.
5. Tenant-scoped systems come from #847 (fallback tasks if the maintainer chooses otherwise).
6. Resolver integration after #686. 7. Tag, counter, new range ComplianceAIActGPAI 5950-5969 and log messages 5950-5957. 8. Tests and documentation.

KEY PATTERNS:
- Ambient tenant wins; marker and no tenant is denied; no marker uses the global options.
- Logs and notifications carry codes, never the free-text reason or EncinaError.Message.
- Coverage per flag; justification .md only for property, integration, load, benchmark.

REFERENCE FILES:
- src/Encina.Compliance.AIAct/AIActCompliancePipelineBehavior.cs
- src/Encina.Compliance.Consent/Services/DefaultConsentService.cs
- the #847 and #686 plans (sibling plans under docs/plans/, found by issue number)
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ⏭️ | Registry caching with tenant-segmented keys is delivered by #847 (its Design Choice 6C); no cache is added here. The deferred item is covered by #847, so no new issue. |
| 2 | OpenTelemetry | ✅ | Tenant tag on the compliance activity and counters when a tenant is resolved; new `aiact.tenant.not_applicable` counter (Phase 7). |
| 3 | Structured Logging | ✅ | Ids 5950-5957 in the new range `ComplianceAIActGPAI` (Phase 7); tenant ids as structured fields, codes only for reasons. |
| 4 | Health Checks | ❌ | Tenant overrides are static options validated at start-up; `AIActHealthCheck` is unchanged. |
| 5 | Validation | ✅ | `AIActOptionsValidator` rejects Warn overrides, missing justification, blank and duplicate tenant ids. |
| 6 | Resilience | ❌ | No external call. |
| 7 | Distributed Locks | ❌ | No shared mutable state is introduced. |
| 8 | Transactions | ❌ | Stateless resolution; the registry transactions belong to #847. |
| 9 | Idempotency | ❌ | Deterministic resolution; the skip notification is per request by design. |
| 10 | Multi-Tenancy | ✅ | This feature (SPEC-002 REQ-061 for the core AI Act module). The satellite packages (#838, #840, #841, #843) are out of scope here; the follow-up issue `aiact-satellites-per-tenant-enforcement` records the per-tenant enforcement of their own gates. |
| 11 | Module Isolation | ⏭️ | Module-scoped enforcement is #846 (open). |
| 12 | Audit Trail | ✅ | `TenantId` on the surviving notifications and records; every not-applicable skip publishes `AIActNotApplicableNotification` for audit handlers (#842 record keeping consumes it). |

---

## Prerequisites & Dependencies

- **Hard**: #847 (its core PR lands first: shared names and the tenant-scoped system lookup). **Depends on Design Choice 2**: under the recommended option B nothing else blocks Phase 1; under option A, `ITenantScopeResolver` of #1257 (its first PR) is a hard prerequisite of Phases 1-2. The issue's "soft dependency on #456" is replaced by the tenant source chosen there.
- **Sequencing with #847**: both plans edit `AIActOptions.cs`, `AIActDiagnostics.cs`, the notifications and `ServiceCollectionExtensions.cs`. Land #847 first (its Phases 1-6), then this plan; the shared names (`RequireTenantContext`, `aiact.tenant_context_required`) belong to #847, and this plan edits neither `AIActErrors.cs` nor adds a conditional branch. The EventIds of this plan are independent of #842 and #847: they come from the new range `ComplianceAIActGPAI` 5950-5969, not from `ComplianceAIAct`.
- **Registry change needed (reported, not edited here)**: `artifacts/decisions/plan-batch/eventid-blocks.md` already lists 5950-5969 for #845; the free-range list in `AGENTS.md` section 7 is not updated by this plan.
- **Soft**: #686 (Phase 6), #1164 (background jobs get their tenant back), #2676 (tag helper), #846 (module scope).

---

## Next Steps

1. Maintainer answers Design Choices 1, 2 and 3 when work on the issue starts (recorded in `## Maintainer Decisions`); #847's own Design Choices (6, 7) are answered with it.
2. Five worker pull requests, each at most 15 files and 1500 lines (file lists in the Estimated File Count section): **PR 1** Phases 1-2 (options, validator, resolver, DI), **PR 2** Phase 4 (notifications and records), **PR 3** Phases 3 and 5 (behavior wiring and tenant-scoping contract test), **PR 4** Phase 7 (observability and the new EventId range), **PR 5** Phase 8 (justification files and documentation); **PR 6** is Phase 6, after #686.
3. Link this plan from #845.
