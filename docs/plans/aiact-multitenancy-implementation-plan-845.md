# Implementation Plan: Per-tenant AI Act enforcement for `Encina.Compliance.AIAct`

> **Issue**: [#845](https://github.com/dlrivada/Encina/issues/845)
> **Type**: Feature
> **Complexity**: Medium (8 phases, no database provider code, two pull requests)
> **Estimated Scope**: ~14 production files and ~14 test files; two pull requests
> **Specification**: [SPEC-002](../specifications/SPEC-002-eu-regulatory-readiness.md) REQ-061, INV-008, DEC-006; **Sibling plans** (shared files and shared decisions, see Prerequisites): `aiact-marten-event-sourcing-implementation-plan-847.md`, `aiact-record-keeping-implementation-plan-842.md`, `compliance-applicability-resolver-implementation-plan-686.md`, `multitenancy-spec002-implementation-plan-1257.md` (owns `ITenantScopeResolver`)

---

## Summary

Let the AI Act gate of `Encina.Compliance.AIAct` behave per tenant: a tenant that is subject to the AI Act gets the global enforcement; a tenant that is territorially outside it can be declared so explicitly, with a recorded justification; a multi-tenant application with no tenant on the request is denied, never silently run with the permissive global mode.

**State of the code on `7a0049c07` (confirms and extends the issue's own "State today").**

- `AIActCompliancePipelineBehavior<,>` (Transient, `src/Encina.Compliance.AIAct/AIActCompliancePipelineBehavior.cs`) reads `AIActOptions.EnforcementMode` once at construction (`_options = options.Value`) and returns `nextStep()` when it is `Disabled`; prohibited practices block in `Block` and `Warn` alike (Art. 5). `AIActOptions` (`AIActOptions.cs`) has `EnforcementMode`, `AutoRegisterFromAttributes`, `AddHealthCheck`, `AssembliesToScan`. `BlockProhibitedUses` and `EnforceHumanOversight` of the issue's example do not exist.
- `IAISystemRegistry` has five async members plus the synchronous `bool IsRegistered(string systemId)` that the validator and the auto-registration use; the only implementation is `InMemoryAISystemRegistry` (`TryAddSingleton`). Nothing carries a tenant: `AISystemRegistration`, `HumanDecisionRecord`, `AISystemReclassifiedNotification(SystemId, Reason, OccurredAtUtc)`, `ProhibitedUseBlockedNotification`.
- `ITenantContext` and a `TenantId` type do not exist. The shipped fail-closed pattern is the ambient `IRequestContext.TenantId` plus `MultiTenancyMarker` (`DefaultConsentService.TryResolveWriteTenantScope`, `ABACDecisionAuditReader.ResolveTenant`, `DeadLetterManager`). `Encina.Compliance.AIAct` references only `Encina`, so using this pattern needs no new project reference (the issue's `ITenantProvider` route would add one to `Encina.Tenancy`).
- **Overlap that changes the scope.** The merged plan for #847 (`aiact-marten-event-sourcing-implementation-plan-847.md`, p0-mandatory) replaces `IAISystemRegistry` and `IHumanOversightEnforcer` by `IAISystemService` and `IHumanOversightService`, stamps `TenantId` on every event, aggregate and read model, derives stream ids from `(tenantId, systemId)`, segments cache keys by tenant and fails closed without a tenant (its Design Choice 7A), and leaves per-tenant behavior to this issue. The merged plan for #686 gates the AI Act through `IComplianceApplicabilityResolver` and allows a skip only as binary territorial non-applicability, recorded and audited (its Design Choices 8 and 9). The maintainer's rewrite of this issue on 2026-11-11 already settles that `Disabled` per tenant is "an explicit, logged and audited tenant configuration for territorial non-applicability, never a convenience tier".

**Scope.** (1) `ConfigureTenant` per-tenant overrides on `AIActOptions`, resolved per request; (2) the gate resolves the tenant, fails closed when it is required and absent; (3) a recorded skip for a tenant declared non-applicable; (4) `TenantId` on the notifications and records that survive #847; (5) the tenant in metrics, activity tags and logs; (6) resolver integration when #686 lands; (7) the tenant-scoped system lookup, which is delivered by #847 (Design Choice 1, recommended) and has a fallback task list if the maintainer chooses otherwise.

**Out of scope.** A persistent tenant registry (#847); module-scoped enforcement (#846); dynamic per-tenant configuration from a store (the resolver of #686 owns tenant declarations); AI Act date logic (SPEC-002 INV-003 applies to defaults, none change here).

**Affected packages**: `Encina.Compliance.AIAct` only. **Provider category**: none (no database, cache, transport or lock provider code).

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
<summary><strong>2. Where the tenant comes from: the ambient request context through the shared scope resolver</strong></summary>

**Class:** Trivial
**Settled by:** `DefaultConsentService.TryResolveWriteTenantScope` (`src/Encina.Compliance.Consent/Services/DefaultConsentService.cs`), `ABACDecisionAuditReader.ResolveTenant` (`src/Encina.Security.ABAC/DecisionAudit/ABACDecisionAuditReader.cs`), `MultiTenancyMarker` (`src/Encina/Abstractions/MultiTenancyMarker.cs`) and plan #847 Design Choice 7A.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ITenantScopeResolver` (core, owned by the #1257 plan) over `IRequestContextAccessor` and `MultiTenancyMarker`** | No new project reference; the same rule as Consent, ABAC and dead letters; single place to change when #456 delivers a typed tenant | Needs the 1257 primitive first |
| **B) `ITenantProvider` from `Encina.Tenancy` (the issue's wording)** | Existing | Adds a project reference from a compliance package to `Encina.Tenancy`; string ids only |
| **C) `ITenantContext` and `TenantId`** | Typed | Do not exist; #456 is open |

### Chosen Option: **A — The shared scope resolver**

### Rationale

- The issue's own Settled choice 1 ("resolve through the abstraction that exists") is honoured: the abstraction that exists is the request context; `ITenantProvider` reads the same value (`DefaultTenantProvider`) but costs a dependency.
- With no marker (single-tenant application) the resolver returns a null tenant and the global options apply: every feature stays opt-in (`AGENTS.md` section 1).

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

- The error code `aiact.tenant_context_required` and the option `AIActOptions.RequireTenantContext` are introduced by #847 (its Phase 4 and 6); this plan reuses the exact names and adds them only if #847 has not landed, so the two plans do not collide.

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

> **Goal**: the per-tenant option types and validation. Prerequisite: `ITenantScopeResolver` (PR 1257-A).

<details>
<summary><strong>Tasks</strong></summary>

1. **Create `src/Encina.Compliance.AIAct/AIActTenantOptions.cs`**: `public sealed class AIActTenantOptions` with `AIActEnforcementMode? EnforcementMode { get; set; }` (null inherits; only `Disabled` is accepted), `string? NotApplicableReason`, `string? TerritorialBasis`, and `ToString()`.
2. **Modify `AIActOptions.cs`**: `public AIActOptions ConfigureTenant(string tenantId, Action<AIActTenantOptions> configure)`, an internal read-only map `TenantOverrides` (case-sensitive ordinal, blank ids rejected), and (only if #847 has not landed) `bool? RequireTenantContext`.
3. **Modify `AIActOptionsValidator.cs`**: a tenant override with `Warn` fails; `Disabled` without `NotApplicableReason` and `TerritorialBasis` fails; duplicate or blank tenant ids fail; the failure messages never print the reason text.
4. **Modify `AIActErrors.cs`**: `TenantContextRequiredCode = "aiact.tenant_context_required"` and its factory (only if absent).
5. PublicAPI lines (mechanical-fixer).

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

> **Goal**: one class that answers "what applies to this request".

<details>
<summary><strong>Tasks</strong></summary>

1. **Create `AIActEffectiveOptionsResolver.cs`** (`internal sealed`): constructor `(IOptions<AIActOptions> options, ITenantScopeResolver tenants)`; `Either<EncinaError, AIActEffectiveOptions> Resolve()` where `AIActEffectiveOptions` is a `readonly record struct (AIActEnforcementMode Mode, string? TenantId, bool NotApplicable, string? ReasonCode)`.
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

> **Goal**: the gate uses the effective options per request.

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify `AIActCompliancePipelineBehavior.cs`**: inject `AIActEffectiveOptionsResolver` (constructor parameter, null checks); in `DispatchAsync` resolve first (the existing `_options.EnforcementMode == Disabled` check becomes `effective.Mode == Disabled || effective.NotApplicable`); a `Left` from the resolver is returned as the result of the request (fail closed); a not-applicable tenant records the skip (Phase 5) and calls `nextStep()`.
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

> **Goal**: the tenant on the events and records that stay after #847.

<details>
<summary><strong>Tasks</strong></summary>

1. Add `string? TenantId` to `ProhibitedUseBlockedNotification`, `HumanOversightRequiredNotification` and `HumanDecisionRecord`; `AISystemReclassifiedNotification` gains it only if #847 keeps the type (the #847 events carry the tenant themselves).
2. **Create `Notifications/AIActNotApplicableNotification.cs`**: `sealed record (string RequestTypeName, string TenantId, string ReasonCode, DateTimeOffset OccurredAtUtc) : INotification` published for each skipped request through the `IEncina` already used for reclassification.
3. Fill the tenant at the publishing sites with the resolved scope.

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

> **Goal**: systems are scoped by tenant. Option A (recommended): delivered by #847, nothing to build here beyond a test.

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
- docs/plans/aiact-marten-event-sourcing-implementation-plan-847.md (Phase 6)
- src/Encina.Compliance.AIAct/InMemoryAISystemRegistry.cs
```

</details>

### Phase 6: Applicability Resolver Integration (conditional on #686)

> **Goal**: consult `IComplianceApplicabilityResolver` when it exists.

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
- docs/plans/compliance-applicability-resolver-implementation-plan-686.md
```

</details>

### Phase 7: Observability

> **Goal**: the tenant in tags, one counter, log messages.

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify `Diagnostics/AIActDiagnostics.cs`**: the tenant tag on the existing activity and counters, only when a tenant is resolved (ADR-054 D4: no tag when multi-tenancy is off), through the key of the shared helper of #2676; until it exists, `TagTenantId = "encina.tenant_id"`, the key proposed by the #1257 plan. New counter `aiact.tenant.not_applicable` (`Counter<long>`, lowercase dot name per ADR-054, tag `aiact.request_type`).
2. **Create `Diagnostics/AIActTenantLogMessages.cs`** (`[LoggerMessage]`, EventIds 9548-9555, inside the registered `ComplianceAIAct = (9500, 9599)`; #847 uses 9513-9529 and #842 uses 9530-9547): tenant required, tenant not applicable (code only), tenant override loaded at start-up, invalid override.
3. Log scopes carry the tenant id only in structured fields already allowed for tenant ids; never the reason text.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```text
CONTEXT:
- ADR-021 (EventIds), ADR-054 D4 (tenant tag only when multi-tenancy is on).

TASK:
Add the tag, the counter and the log messages. Check EventIdRanges.cs and the two sibling plans for the next free id before numbering.

KEY RULES:
- EventIds packed sequentially; no new range is needed.
- Never log EncinaError.Message or the free-text reason.

REFERENCE FILES:
- src/Encina.Compliance.AIAct/Diagnostics/AIActDiagnostics.cs
- src/Encina.Compliance.AIAct/Diagnostics/AIActLogMessages.cs
- src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

### Phase 8: Testing and Documentation

> **Goal**: per-flag targets met; documents and fragments written.

<details>
<summary><strong>Tasks</strong></summary>

| Test type | Scope | Location |
|-----------|-------|----------|
| Unit | resolver (5 cases), behavior with tenant cases, validator rules, options map, notification fields | `tests/Encina.UnitTests/Compliance/AIAct/` |
| Guard | new constructors and public methods (`ConfigureTenant`) | `tests/Encina.GuardTests/Compliance/AIAct/` |
| Contract | gate behavior for two tenants with real registry/classifier/validator (extends the existing behavior contract tests) | `tests/Encina.ContractTests/Compliance/AIAct/` |
| Property | justification `.md`: resolution is a lookup with a fixed order covered by unit tests | `tests/Encina.PropertyTests/Compliance/AIAct/MultiTenancy.md` |
| Integration | justification `.md`: in-memory registry, no external dependency | `tests/Encina.IntegrationTests/Compliance/AIAct/MultiTenancy.md` |
| Load | justification `.md`: not a concurrent hot path | `tests/Encina.LoadTests/Compliance/AIAct/MultiTenancy.md` |
| Benchmark | justification `.md`: one dictionary lookup | `tests/Encina.BenchmarkTests/Compliance/AIAct/MultiTenancy.md` |

**Per-file targets** (proposed; measured values replace them, never lower; existing targets from `.github/coverage-manifest/Encina.Compliance.AIAct.json`):

| File | Flags | Justification |
|------|-------|---------------|
| `AIActCompliancePipelineBehavior.cs` (unit 97, guard 8, contract 95) | unit 97, guard 8, contract 95 | new branches are covered by the tenant cases of the same suites |
| `AIActOptionsValidator.cs` (guard 95, property 95) | unit 98, guard 95, property 95 | every new rule has a test; the property suite adds tenant combinations |
| `AIActEffectiveOptionsResolver.cs` (new) | unit 100, guard 90 | five paths, each tested |
| `AIActOptions.cs` (guard 95) | unit 95, guard 95 | `ConfigureTenant` guard clauses |
| `AIActTenantOptions.cs` (new) | unit 95 | settings class with `ToString` |
| `Diagnostics/AIActDiagnostics.cs` (unit 95) | unit 95 | the existing listener tests extend to the tag |

Documentation: XML docs, `src/Encina.Compliance.AIAct/README.md` and the configuration guide (docs-writer), `changelog.d/845-aiact-multitenancy.added.md` (mechanical-fixer), `PublicAPI.Unshipped.txt`, the manifest update, `docs/INVENTORY.md`; build with 0 warnings.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```text
CONTEXT:
- Per-flag coverage (AGENTS.md section 9). Property, integration, load and benchmark are justified with the .md files listed; unit, guard and contract are never justified away.

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
| AIActLogMessages (EventIds 9500-9512) | src/Encina.Compliance.AIAct/Diagnostics/AIActLogMessages.cs | existing log style; new ids start at 9548 |
| ProhibitedUseBlockedNotification, HumanOversightRequiredNotification, AISystemReclassifiedNotification | src/Encina.Compliance.AIAct/Notifications/ | gain the tenant |
| HumanDecisionRecord | src/Encina.Compliance.AIAct/Model/HumanDecisionRecord.cs | gains the tenant |
| MultiTenancyMarker and IRequestContextAccessor | src/Encina/Abstractions/MultiTenancyMarker.cs, src/Encina/Abstractions/IRequestContextAccessor.cs | multi-tenancy signal and ambient tenant |
| DefaultConsentService.TryResolveWriteTenantScope | src/Encina.Compliance.Consent/Services/DefaultConsentService.cs | fail-closed tenant pattern already shipped |
| ABACDecisionAuditReader.ResolveTenant | src/Encina.Security.ABAC/DecisionAudit/ABACDecisionAuditReader.cs | same pattern in the ABAC package |
| Plan #847 (AI system service with tenant-scoped streams and cache keys) | docs/plans/aiact-marten-event-sourcing-implementation-plan-847.md | delivers the tenant-scoped system registry |
| Plan #686 (IComplianceApplicabilityResolver) | docs/plans/compliance-applicability-resolver-implementation-plan-686.md | applicability decision per tenant, AIAct is one gated capability |
| AIAct coverage manifest | .github/coverage-manifest/Encina.Compliance.AIAct.json | per-file targets to raise |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Compliance.AIAct` | 9548-9555 | inside the registered `ComplianceAIAct = (9500, 9599)`; 9500-9512 existing, 9513-9529 reserved by #847, 9530-9547 by #842; re-check before numbering |

### Estimated File Count

| Category | Files | Notes |
| :--- | :--- | :--- |
| Production, options, resolver, behavior, validator, errors, DI | 7 | modified 5, new 2 (`AIActTenantOptions`, `AIActEffectiveOptionsResolver`) |
| Production, diagnostics and log messages | 2 | modified 1, new 1 |
| Production, notifications and records (tenant field) | 5 | modified 4, new 1 |
| Tests (unit, guard, contract, justification .md) | 14 | new and modified |
| Documentation (README, configuration guide, changelog fragment, PublicAPI, manifest) | 5 | docs-writer and mechanical-fixer |
| Total | 33 | two pull requests |

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
6. Resolver integration after #686. 7. Tag, counter, log messages 9548-9555. 8. Tests and documentation.

KEY PATTERNS:
- Ambient tenant wins; marker and no tenant is denied; no marker uses the global options.
- Logs and notifications carry codes, never the free-text reason or EncinaError.Message.
- Coverage per flag; justification .md only for property, integration, load, benchmark.

REFERENCE FILES:
- src/Encina.Compliance.AIAct/AIActCompliancePipelineBehavior.cs
- src/Encina.Compliance.Consent/Services/DefaultConsentService.cs
- docs/plans/aiact-marten-event-sourcing-implementation-plan-847.md
- docs/plans/compliance-applicability-resolver-implementation-plan-686.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ⏭️ | Registry caching with tenant-segmented keys is delivered by #847 (its Design Choice 6C); no cache is added here. The deferred item is covered by #847, so no new issue. |
| 2 | OpenTelemetry | ✅ | Tenant tag on the compliance activity and counters when a tenant is resolved; new `aiact.tenant.not_applicable` counter (Phase 7). |
| 3 | Structured Logging | ✅ | Ids 9548-9555 inside `ComplianceAIAct`; tenant ids as structured fields, codes only for reasons. |
| 4 | Health Checks | ❌ | Tenant overrides are static options validated at start-up; `AIActHealthCheck` is unchanged. |
| 5 | Validation | ✅ | `AIActOptionsValidator` rejects Warn overrides, missing justification, blank and duplicate tenant ids. |
| 6 | Resilience | ❌ | No external call. |
| 7 | Distributed Locks | ❌ | No shared mutable state is introduced. |
| 8 | Transactions | ❌ | Stateless resolution; the registry transactions belong to #847. |
| 9 | Idempotency | ❌ | Deterministic resolution; the skip notification is per request by design. |
| 10 | Multi-Tenancy | ✅ | This feature (SPEC-002 REQ-061 for the AI Act module). |
| 11 | Module Isolation | ⏭️ | Module-scoped enforcement is #846 (open). |
| 12 | Audit Trail | ✅ | `TenantId` on the surviving notifications and records; every not-applicable skip publishes `AIActNotApplicableNotification` for audit handlers (#842 record keeping consumes it). |

---

## Prerequisites & Dependencies

- **Hard**: `ITenantScopeResolver` (PR 1257-A). The issue's "soft dependency on #456" is replaced by this primitive.
- **Sequencing with #847**: both plans edit `AIActOptions.cs`, `AIActErrors.cs`, `AIActDiagnostics.cs`, the notifications and `ServiceCollectionExtensions.cs`. Land #847 first (its Phases 1-6), then this plan; the shared names (`RequireTenantContext`, `aiact.tenant_context_required`) belong to #847. #842 reserves 9530-9547 before this plan's 9548-9555.
- **Soft**: #686 (Phase 6), #1164 (background jobs get their tenant back), #2676 (tag helper), #846 (module scope).

---

## Next Steps

1. Maintainer answers Design Choices 1 and 3 when work on the issue starts (recorded in `## Maintainer Decisions`); #847's own Design Choices (6, 7) are answered with it.
2. Two worker pull requests, each at most 15 files and 1500 lines:
   - **PR 1**: Phases 1-3 (options, validator, resolver, behavior, DI, errors) plus their unit, guard and contract tests: about 14 files.
   - **PR 2**: Phases 4, 5, 7, 8 (notifications and records, diagnostics, justification files, documentation): about 14 files; Phase 6 is a third small PR after #686.
3. Link this plan from #845.
