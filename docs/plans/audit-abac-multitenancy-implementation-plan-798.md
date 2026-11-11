# Implementation Plan: Tenant isolation for `Encina.Security.Audit` and `Encina.Security.ABAC`

> **Issue**: [#798](https://github.com/dlrivada/Encina/issues/798)
> **Type**: Feature
> **Complexity**: High (10 phases, 10 database providers, Marten, 5 worker pull requests)
> **Estimated Scope**: ~91 files (about 45 production and script files, 34 test files, 8 documentation files); split into five pull requests (see Next Steps)
> **Specification**: [SPEC-002](../specifications/SPEC-002-eu-regulatory-readiness.md) REQ-061, INV-008, AC-043, DEC-006, DEC-009; Related plans: `abac-decision-audit-implementation-plan-751.md`, `audit-marten-implementation-plan-800.md`, `multitenancy-spec002-implementation-plan-1257.md` (owns the shared tenant-scope primitive used here)

---

## Summary

Make the two Security stores tenant-safe without a manual `WHERE` at every call site: the operation audit trail (`IOperationAuditStore`, entry `OperationAuditEntry`) and the read audit trail (`IReadAuditStore`) must never return, count or export another tenant's rows, and the ABAC policy store (`IPolicyStore`) must hold and evaluate policies per tenant.

**State of the code on `7a0049c07` (corrects the issue text).**

- The issue speaks of `IAuditStore`, `AuditEntry`, `ITenantContext` and 13 providers. Today the store is `IOperationAuditStore` (ADR-036), the row is `OperationAuditEntry` (`src/Encina.Security.Audit/OperationAuditEntry.cs:86` has `string? TenantId`), the query is `OperationAuditQuery` (`TenantId` at `OperationAuditQuery.cs:49`), `ITenantContext` and a `TenantId` type do not exist, and the database matrix is 10 providers (SQLite is out, `AGENTS.md` section 5). The tenant abstraction is `IRequestContext.TenantId` read through `IRequestContextAccessor`, and "multi-tenancy is on" is the container signal `MultiTenancyMarker` (`src/Encina/Abstractions/MultiTenancyMarker.cs`) that `AddEncinaTenancy` registers.
- All audit stores already persist `TenantId` and already filter `OperationAuditQuery.TenantId` in `QueryAsync` (ADO, Dapper, EF Core, MongoDB, Marten with an indexed read model, and `InMemoryOperationAuditStore`). What is missing is the gate: nothing forces the ambient tenant, `GetByEntityAsync`, `GetByUserAsync` and `GetByCorrelationIdAsync` have no tenant parameter, and `IReadAuditStore` is in the same situation. `DefaultOperationAuditEntryFactory` already stamps `TenantId = context.TenantId` (`DefaultOperationAuditEntryFactory.cs:114`).
- `Policy`, `PolicySet`, `PolicyEntity`, `PolicySetEntity` and the tables `abac_policy_sets` and `abac_policies` (scripts `021` and `022` in six packages) carry no tenant, and `Id` alone is the primary key. `IPolicyStore` has 12 methods and is consumed only by `PersistentPolicyAdministrationPoint`, which receives the store through `ResolvePolicyStore` (`src/Encina.Security.ABAC/ServiceCollectionExtensions.cs:309`). `CachingPolicyStoreDecorator` builds keys as `{prefix}:policy-sets:all`, `{prefix}:policy:{id}` and so on, with no tenant.
- Maintainer decisions that bind this plan: compliance controls are never optional (2026-10-30, comment on #686), so the issue's opt-in `EnableTenantIsolation` switch is replaced by activation on `MultiTenancyMarker`; cross-tenant access is never an `AllTenants` boolean but only the audited platform-operator permission (2972-D22, recorded in #3271), which is outside this issue; tenant telemetry follows ADR-054 D4 (tag only when multi-tenancy is on, helper owned by #2676).

**Scope.** (1) a tenant gate for `IOperationAuditStore` and `IReadAuditStore` that forces the ambient tenant on reads and stamps it on writes; (2) a tenant column and a tenant parameter in the ABAC policy store on all 8 provider classes (10 database providers), per-tenant policies in the in-memory PAP, and a tenant-partitioned cache; (3) fail-closed behavior when a multi-tenant application has no tenant; (4) a two-tenant isolation test per provider and Marten.

**Out of scope.** `IAuditLogStore` (entity-change audit, #1778, ADR-036); per-tenant audit retention (`PurgeEntriesAsync` stays an operator operation, see Design Choice 6); the platform-operator cross-tenant permission (#3271, #2972); the shared tenant telemetry helper (#2676); `ModuleId` (#1636).

**Affected packages**: `Encina.Security.Audit`, `Encina.Security.ABAC`, the audit registrations of `Encina.ADO.*`, `Encina.Dapper.*`, `Encina.EntityFrameworkCore`, `Encina.MongoDB` and `Encina.Audit.Marten`, and the ABAC stores of the same packages (except Marten). **Provider category**: Database (10 providers) plus Marten for audit; Caching (provider-agnostic, through `ICacheProvider`).

---

## Design Choices

<details>
<summary><strong>1. Tenant source and activation: the ambient request-context tenant, switched on by `MultiTenancyMarker`</strong></summary>

**Class:** Settled
**Settled by:** [#686 maintainer comment of 2026-10-30](https://github.com/dlrivada/Encina/issues/686#issuecomment-6521534413) (compliance controls are never optional), SPEC-002 INV-008 and DEC-006, `AGENTS.md` section 3 (gates fail closed), and the shipped precedent `ABACDecisionAuditReader.ResolveTenant` (`src/Encina.Security.ABAC/DecisionAudit/ABACDecisionAuditReader.cs`) and `DeadLetterManager` (`src/Encina.Messaging/DeadLetter/DeadLetterManager.cs`).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Read `IRequestContextAccessor.RequestContext.TenantId`; the gate is always on, and "multi-tenant" is `MultiTenancyMarker` in the container** | Same rule as the decision-audit reader, the dead letter manager and Consent; no switch to forget; no new abstraction | Needs the marker to be registered by applications that resolve tenants themselves (documented) |
| **B) Opt-in `EnableTenantIsolation` option (the issue text)** | Pay-for-what-you-use looks explicit | A compliance control behind a switch (rejected 2026-10-30); a forgotten switch is a cross-tenant leak |
| **C) Introduce `ITenantContext` and a `TenantId` type first** | Strong typing | Does not exist; #456 is open; widens this issue to the whole tenancy core |

### Chosen Option: **A — Ambient tenant, always on, activated by `MultiTenancyMarker`**

### Rationale

- The gate is a compliance control: only territorial non-applicability may disable one, never a configuration tier (maintainer principle of 2026-10-30).
- Single-tenant applications register no marker, see no ambient tenant and pay nothing: reads run with a null tenant filter exactly as today (`AGENTS.md` section 1, pay-for-what-you-use).
- `ITenantContext` stays out: when #456 delivers a typed tenant, the gate's single call to the shared resolver (Design Choice 1 of the #1257 plan) changes in one place.

</details>

<details>
<summary><strong>2. Where the audit gate lives: a decorator re-applied by the registration helper</strong></summary>

**Class:** Dilemma (recommended, pending the maintainer)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `TenantScopedOperationAuditStore` and `TenantScopedReadAuditStore` decorators (the issue's chosen alternative); `OperationAuditStoreRegistration` gains a helper that re-applies them after a provider removes the in-memory default** | No store implementation changes; one gate for 10 providers, Marten and in-memory; the five reads are rewritten once | A provider registered after `AddEncinaAudit` removes the decorated default (`OperationAuditStoreRegistration.RemoveInMemoryDefault` strips decorators too), so 9 registrations must call the helper (a one-line change each); an application that builds a store by hand bypasses the gate |
| **B) Filter inside each store (`ITenantScopeResolver` injected into every store class)** | Cannot be bypassed by DI order or by hand construction | 13 implementations change their constructors and queries; constructor churn on the three families and Marten; the five reads keep per-provider SQL for the tenant |
| **C) Keep the stores raw and add a tenant-aware reader service (`IOperationAuditReader`) like the ABAC decision reader** | Raw store stays a cross-tenant operator port; pattern already used for decision audit | Applications that inject `IOperationAuditStore` directly are not protected; a second public API for the same data |

### Chosen Option: **A — Decorator plus a registration helper** (recommended, pending the maintainer)

### Rationale

- A is the issue's own chosen alternative and keeps the diff small. Its one weakness is registration order, which `AGENTS.md` section 3 (registration completeness) already names; Phase 4 closes it with a helper called from all 9 registrations and a `ValidateOnBuild` test that registers the provider before and after `AddEncinaAudit`.
- B wins when the store is constructed outside DI (a custom host) and the application must be safe anyway; C wins when the raw store is meant to stay a cross-tenant operator port. Neither condition is in the 1.0 scope, and #3271 will give the operator port its own audited path.
- The decorator never rewrites a `Left` into success and never swallows a store failure (`AGENTS.md` section 3).

</details>

<details>
<summary><strong>3. Policies of no tenant: tenant policies plus shared "global" policies, or strict tenant-only</strong></summary>

**Class:** Dilemma (recommended, pending the maintainer)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Strict: every policy belongs to exactly one tenant; a tenant sees only its own rows** | Simplest isolation story and test; no shared mutable rule | Baseline rules every tenant must obey (for example an operator-wide deny) must be copied into each tenant; startup seeding (`ABACPolicySeedingHostedService`) runs with no tenant and could not seed anything in a multi-tenant application |
| **B) Tenant policies plus global policies (`TenantId` null): a tenant read returns its own rows and the global ones; a global row can be written only when there is no ambient tenant** | Operator baseline and seeding keep working; a tenant cannot change a shared rule | Two row classes to test; a global change must invalidate every tenant's cache; a global policy is visible to all tenants by design |
| **C) B with a per-policy flag that a tenant may opt out of a global policy** | Flexibility | A tenant can switch off an operator baseline; against the never-optional principle of Design Choice 1 |

### Chosen Option: **B — Tenant plus global policies, global writes only without an ambient tenant** (recommended, pending the maintainer)

### Rationale

- A wins when the regulator or the contract forbids any shared rule between tenants; B wins for a SaaS operator that ships a baseline and seeds it at startup, which is the shape of the reference application.
- Global rows are not another tenant's data, so INV-008 holds: a tenant never reads, acts on or caches a row of a different tenant.
- C is excluded by the maintainer principle of 2026-10-30.

</details>

<details>
<summary><strong>4. The policy store port: a `tenantId` parameter supplied by a wrapper, not ambient reads inside 8 store classes</strong></summary>

**Class:** Trivial
**Settled by:** `OperationAuditQuery.TenantId` (`src/Encina.Security.Audit/OperationAuditQuery.cs:49`) and `DeadLetterFilter` (`src/Encina.Messaging/DeadLetter/DeadLetterFilter.cs`) pass the tenant as a value to the store, and `AGENTS.md` section 5 (abstractions and options live in the core, providers differ only in implementation).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Add `string? tenantId` to the read, delete, exists and count methods of `IPolicyStore`; writes take it from `Policy.TenantId`; a `TenantScopedPolicyStore` wrapper resolves the ambient tenant and calls the port** | Provider classes stay pure and testable with plain SQL parameters; same shape as the audit query | Breaking change of the port (pre-1.0, acceptable); the wrapper is a new class |
| **B) Each of the 8 store classes injects the accessor and the marker** | No port change | Ambient reads in eight provider classes; the provider code mixes request context into persistence |
| **C) A second port `ITenantPolicyStore`** | Old port untouched | Two ports for one table; "no compatibility layers" (`AGENTS.md` section 3) |

### Chosen Option: **A — Tenant parameter on the port, supplied by the wrapper**

### Rationale

- The wrapper sits in `ResolvePolicyStore`, below the caching decorator, so the cache sees tenant-scoped reads and the wrapper cannot be bypassed by registration order (the PAP resolves it per operation scope).
- `tenantId == null` means "rows with no tenant" and never "every tenant"; there is no `AllTenants` parameter (2972-D22).
- `Policy` and `PolicySet` gain `string? TenantId { get; init; }` as the issue proposes; the entity column is the source of truth and the mapper overwrites the model value on read, so a serialized `PolicyJson` cannot smuggle another tenant.

</details>

<details>
<summary><strong>5. Cache partition: a tenant segment in every key, pattern invalidation for global changes</strong></summary>

**Class:** Trivial
**Settled by:** the Consent fix #1315 and the segmented keys of `docs/plans/aiact-marten-event-sourcing-implementation-plan-847.md` (Design Choice 6C, `aiact:tenant:{segment}:system:{id}`); open bug class #1949 (LawfulBasis cache keys ignore the tenant).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Keys `{prefix}:t:{segment}:policy-sets:all` and so on; global rows cached under segment `_`; a global write calls `RemoveByPatternAsync` for every tenant segment; the Pub/Sub message carries the tenant** | One cache; works on every `ICacheProvider` that supports patterns | Pattern removal needs provider support (all 8 caches implement `RemoveByPatternAsync`) |
| **B) A separate `ICacheProvider` per tenant** | Hard separation | Unbounded cache instances; breaks the singleton cache registration |
| **C) No cache in a multi-tenant application** | Nothing to leak | Removes the stampede protection the decorator exists for |

### Chosen Option: **A — Tenant-segmented keys and pattern invalidation**

### Rationale

- The segment is the tenant id encoded so that it cannot contain the separator (a padded or ':'-bearing tenant id must not alias another tenant, the #3147 bug class).
- `PolicyCachePubSubHostedService` forwards the tenant so other nodes drop the same keys.

</details>

<details>
<summary><strong>6. Behavior without a tenant, on writes, and for purge</strong></summary>

**Class:** Settled
**Settled by:** `AGENTS.md` section 3 (compliance gates fail closed; background loops carry no request), SPEC-002 DEC-006, the shipped `ABACDecisionAuditReader` and `DefaultConsentService.TryResolveWriteTenantScope` (`src/Encina.Compliance.Consent/Services/DefaultConsentService.cs`), and 2972-D22 as recorded in #3271.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Reads: ambient tenant wins; a different requested tenant is denied; no ambient tenant in a multi-tenant application is denied. Writes: stamp the ambient tenant; an explicit different tenant is denied; no tenant writes a null-tenant row. Purge passes through** | Evidence is never lost (a background writer has no tenant); reads fail closed | A null-tenant audit row is visible only to the operator path |
| **B) Writes also fail when there is no tenant** | Every row has a tenant | System actors (startup seeding, outbox processors) could not be audited, which loses evidence |
| **C) Purge per tenant now** | Matches REQ-061 "retention per tenant" | The retention service is a background loop with no tenant; per-tenant retention belongs to the retention module |

### Chosen Option: **A — Fail closed on reads, never lose a write, purge stays an operator operation**

### Rationale

- Single-tenant applications (no marker) pass through unchanged.
- The platform-operator cross-tenant read is out of scope and will come from #3271 with a mandatory reason; no boolean switch is added here.
- `PurgeEntriesAsync` deletes by age, not by tenant; the follow-up issue file `audit-per-tenant-retention` records the per-tenant variant.

</details>

<details>
<summary><strong>7. Marten audit store: manual `TenantId` filtering, not Marten's native tenancy</strong></summary>

**Class:** Trivial
**Settled by:** the shipped design of `Encina.Audit.Marten`: `OperationAuditEntryRecordedEvent.TenantId` is stored in plaintext and indexed (`src/Encina.Audit.Marten/Projections/ConfigureMartenOperationAuditProjections.cs:105`), and `MartenOperationAuditStore.ApplyUserTenantAndTypeFilters` filters it.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Keep the manual `TenantId` column filter plus the gate** | No change to the events; crypto-shredding scope unchanged | One more place that relies on the shared gate |
| **B) Marten conjoined tenancy** | Engine-level isolation | Changes document sessions and the projection; breaks the single-store design and encryption scope |

### Chosen Option: **A — Manual filtering behind the shared gate**

### Rationale

- Marten needs no code change for the reads, only the registration helper of Phase 4 (the registration uses `services.Replace`, which would drop a decorator).

</details>

---

## Implementation Phases

### Phase 1: Core Models, Errors & Options

> **Goal**: add the tenant to the policy models and entities, the error codes and the (small) option surface. Prerequisite: the `ITenantScopeResolver` primitive of the #1257 plan (PR 1257-A) is merged.

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify `src/Encina.Security.ABAC/Model/Policy.cs`** and **`PolicySet.cs`**: add `public string? TenantId { get; init; }` with XML docs ("null means a global policy shared by every tenant").
2. **Modify `src/Encina.Security.ABAC/Persistence/PolicyEntity.cs`** and **`PolicySetEntity.cs`**: add `public string? TenantId { get; set; }`.
3. **Modify `src/Encina.Security.ABAC/Persistence/PolicyEntityMapper.cs`**: write the model tenant to the column; on read set the model `TenantId` from the column (the column wins over any value inside `PolicyJson`).
4. **Modify `src/Encina.Security.ABAC/ABACErrors.cs`**: add `PolicyStoreTenantRequiredCode = "encina.authorization.abac_policy_tenant_required"` and `PolicyStoreTenantMismatchCode = "encina.authorization.abac_policy_tenant_mismatch"` with factories that carry a fixed message and no tenant value (same shape as `DecisionAuditTenantRequired`).
5. **Modify `src/Encina.Security.Audit/OperationAuditErrors.cs`** and **`ReadAuditErrors.cs`**: add `TenantRequired` and `TenantMismatch` factories (codes `encina.audit.tenant_required`, `encina.audit.tenant_mismatch`).
6. **PublicAPI**: `PublicAPI.Unshipped.txt` lines for every new public member (mechanical-fixer).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
- Encina.Security.ABAC and Encina.Security.Audit, issue #798. Policy/PolicySet are sealed records; PolicyEntity/PolicySetEntity are sealed classes persisted by 8 store classes.
- The tenant abstraction is IRequestContext.TenantId; ITenantContext does not exist. Errors are EncinaError values with fixed messages.

TASK:
Add TenantId to the four policy types, update PolicyEntityMapper (column wins on read), add the four error codes and factories. No store or gate code yet.

KEY RULES:
- ROP: Either<EncinaError, T>; error messages never contain a tenant id; codes only in logs.
- XML docs on every public member; PublicAPI.Unshipped.txt lines are written by mechanical-fixer.
- Zero warnings; no [Obsolete]; no compatibility layers.

REFERENCE FILES:
- src/Encina.Security.ABAC/ABACErrors.cs (DecisionAuditTenantRequired, line ~736)
- src/Encina.Security.ABAC/Persistence/PolicyEntityMapper.cs
- src/Encina.Security.Audit/OperationAuditErrors.cs
```

</details>

### Phase 2: Core Interfaces & Abstractions

> **Goal**: the tenant parameter on the policy store port; no change to `IOperationAuditStore`.

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify `src/Encina.Security.ABAC/Persistence/IPolicyStore.cs`**: add `string? tenantId` to `GetAllPolicySetsAsync`, `GetPolicySetAsync`, `DeletePolicySetAsync`, `ExistsPolicySetAsync`, `GetPolicySetCountAsync`, `GetAllStandalonePoliciesAsync`, `GetPolicyAsync`, `DeletePolicyAsync`, `ExistsPolicyAsync`, `GetPolicyCountAsync`. `SavePolicySetAsync` and `SavePolicyAsync` take the tenant from the model. XML docs state: `tenantId == null` returns rows with no tenant; a non-null tenant returns its own rows plus the global ones (Design Choice 3, recommended); `Id` is unique per tenant.
2. **Modify `src/Encina.Security.ABAC/Administration/PersistentPolicyAdministrationPoint.cs`**: pass the tenant of the `PolicyActor` (already captured, line ~651) to the store calls and stamp it on added policies; reject an add whose `TenantId` differs from the actor's tenant, and a global add (`TenantId` null) when the actor has a tenant.
3. **Modify `src/Encina.Security.ABAC/Administration/InMemoryPolicyAdministrationPoint.cs`**: replace the three `ConcurrentDictionary` fields with dictionaries keyed by `(tenant, id)`, same visibility rule as the store.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
- Phase 1 is merged. IPolicyStore has 12 methods and one consumer, PersistentPolicyAdministrationPoint (singleton; resolves the store per operation scope through ResolvePolicyStore).
- Design Choice 4 (tenant parameter on the port) and 3 (tenant plus global rows).

TASK:
Change IPolicyStore and its two PAP consumers. The PAP uses the PolicyActor tenant. Do not implement the 8 store classes here.

KEY RULES:
- A null tenantId means "rows with no tenant", never "all tenants". No AllTenants parameter.
- A tenant-scoped actor can never write or delete a global row.
- Singleton PAP must not capture scoped services.

REFERENCE FILES:
- src/Encina.Security.ABAC/Persistence/IPolicyStore.cs
- src/Encina.Security.ABAC/Administration/PersistentPolicyAdministrationPoint.cs (PolicyActor at line 98)
- src/Encina.Security.ABAC/Administration/InMemoryPolicyAdministrationPoint.cs
```

</details>

### Phase 3: The Tenant Gates

> **Goal**: `TenantScopedOperationAuditStore`, `TenantScopedReadAuditStore` and `TenantScopedPolicyStore`.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create `src/Encina.Security.Audit/TenantScopedOperationAuditStore.cs`**: `internal sealed class` implementing `IOperationAuditStore`, constructor `(IOperationAuditStore inner, ITenantScopeResolver tenants, ILogger<TenantScopedOperationAuditStore> logger)`.
   - `RecordAsync(entry)`: `tenants.ResolveWrite(entry.TenantId)`; stamp the tenant (`entry with { TenantId = scope.TenantId }`); deny on mismatch; forward.
   - `QueryAsync(query)`: `tenants.ResolveRead(query.TenantId)` then `query with { TenantId = scope.TenantId }`.
   - `GetByEntityAsync`, `GetByUserAsync`, `GetByCorrelationIdAsync`: rewrite to `QueryAsync` pages with the resolved tenant and the same ordering and date bounds (`GetByCorrelationIdAsync` reverses to ascending), looping pages until exhausted; document the cost. The unit tests prove equivalence with the inner `Get*` for the in-memory store.
   - `PurgeEntriesAsync`: forward unchanged (Design Choice 6).
2. **Create `src/Encina.Security.Audit/TenantScopedReadAuditStore.cs`**: same for `IReadAuditStore` (`LogReadAsync`, `GetAccessHistoryAsync`, `GetUserAccessHistoryAsync`, `QueryAsync`, `PurgeEntriesAsync`).
3. **Create `src/Encina.Security.ABAC/Persistence/TenantScopedPolicyStore.cs`**: implements `IPolicyStore` over an inner `IPolicyStore`; each read resolves `ResolveRead(null)` and passes `scope.TenantId` to the inner port; `Save*` stamps `Policy.TenantId`/`PolicySet.TenantId` from `ResolveWrite(policy.TenantId)`; a global save needs `scope.TenantId == null`.
4. **Log messages** (`TenantScopedOperationAuditStoreLog`, `[LoggerMessage]`, ids from Event ID Allocation): tenant denied (code only, never the tenant id), gate active, write without tenant.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT:
- ITenantScopeResolver and TenantScope come from the #1257 plan (core, namespace Encina): ResolveRead(string? requested) and ResolveWrite(string? explicitTenant) return Either<EncinaError, TenantScope>. Ambient tenant wins; multi-tenant (MultiTenancyMarker) with no tenant is denied; single-tenant passes with a null tenant.
- Design Choices 2, 3, 4 and 6 of the #798 plan.

TASK:
Implement the three decorators exactly as listed in the plan's Phase 3 tasks, with unit tests per method (two tenants, no tenant in multi-tenant, no tenant in single-tenant, mismatched explicit tenant).

KEY RULES:
- A Left from the inner store is returned as is; never swallowed (AGENTS.md section 3).
- Error messages and logs never carry the tenant id or EncinaError.Message; use the code.
- TimeProvider only where time is read; none is needed here.

REFERENCE FILES:
- src/Encina.Security.ABAC/DecisionAudit/ABACDecisionAuditReader.cs (ResolveTenant)
- src/Encina.Compliance.Consent/Services/DefaultConsentService.cs (TryResolveWriteTenantScope)
- src/Encina.OpenTelemetry/Audit/InstrumentedOperationAuditStore.cs (decorator shape)
```

</details>

### Phase 4: Configuration, DI & Registration Order

> **Goal**: the gates are registered in any order and cannot be lost by a provider registration.

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify `src/Encina.Security.Audit/OperationAuditStoreRegistration.cs`**: add `public static void AddTenantGate(IServiceCollection services)` that wraps the current `IOperationAuditStore` descriptor with `TenantScopedOperationAuditStore` unless it is already wrapped (use `IDecoratedServiceFactory`, `src/Encina/Core/IDecoratedServiceFactory.cs`), and the same helper for `IReadAuditStore`. `RemoveInMemoryDefault` stays; providers call `AddTenantGate` right after their `TryAddScoped`.
2. **Modify `src/Encina.Security.Audit/ServiceCollectionExtensions.cs`** (`AddEncinaAudit`): call `AddTenantGate` after the in-memory defaults.
3. **Modify the audit registrations of the 8 provider packages and Marten**: `Encina.ADO.SqlServer`, `ADO.PostgreSQL`, `ADO.MySQL`, `Dapper.SqlServer`, `Dapper.PostgreSQL`, `Dapper.MySQL`, `EntityFrameworkCore` (`ServiceCollectionExtensions.cs:463`), `MongoDB` (`:675`), `Audit.Marten` (`ServiceCollectionExtensions.cs:125`, which uses `Replace`): call `OperationAuditStoreRegistration.AddTenantGate(services)` after registering the store, and the read store.
4. **Modify `src/Encina.Security.ABAC/ServiceCollectionExtensions.cs`** `ResolvePolicyStore` (line 309): wrap the resolved `IPolicyStore` with `TenantScopedPolicyStore` first, then with `CachingPolicyStoreDecorator`.
5. **DI tests** (`ValidateOnBuild`, `ValidateScopes`): for each provider, register the provider before and after `AddEncinaAudit`; the resolved `IOperationAuditStore` is gated in both orders and an application-registered store is never removed.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT:
- OperationAuditStoreRegistration.RemoveInMemoryDefault strips the in-memory default and any decorator over it; a provider registered afterwards would lose a gate registered earlier. The OpenTelemetry decorator has the same limitation and tells users to register it last; a security gate cannot.
- Marten registers with services.Replace.

TASK:
Add AddTenantGate and call it from AddEncinaAudit and the 9 audit registrations; wrap in ResolvePolicyStore. Write the order-independence DI tests.

KEY RULES:
- Registration completeness (AGENTS.md section 3): ValidateOnBuild and ValidateScopes tests; a store registered by the application is never removed or unwrapped.
- TryAdd semantics; no duplicate wrapping when AddTenantGate is called twice.

REFERENCE FILES:
- src/Encina.Security.Audit/OperationAuditStoreRegistration.cs
- src/Encina/Core/IDecoratedServiceFactory.cs
- src/Encina.EntityFrameworkCore/ServiceCollectionExtensions.cs (line 463)
- tests/Encina.UnitTests/ADO/PostgreSQL/OperationAuditStoreRegistrationTests.cs
```

</details>

### Phase 5: Persistence — Entities, Mappers & SQL Scripts

> **Goal**: the tenant column, key and indexes in every ABAC table.

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify the six `021_CreateAbacPolicySetsTable.sql` and six `022_CreateAbacPoliciesTable.sql`** (ADO and Dapper, SqlServer, PostgreSQL, MySQL): add `TenantId VARCHAR(128) NULL`; change the primary key to a unique index on `(COALESCE(TenantId, ''), Id)` (SQL Server and PostgreSQL: unique index with `ISNULL`/`COALESCE`; MySQL: generated column or `(IFNULL(TenantId, ''), Id)` functional index); add `IX_*_TenantId`. Edited in place: pre-1.0, no migration helpers (`AGENTS.md` sections 1 and 3).
2. **EF Core**: the entity configuration for `PolicyEntity`/`PolicySetEntity` in `src/Encina.EntityFrameworkCore` (find the configuration with `HasKey(x => x.Id)`): composite key `(TenantId, Id)` is not possible with a nullable part, so use a unique index plus a surrogate shadow key as the audit entity does; verify against `OperationAuditEntryEntityConfiguration.cs`.
3. **MongoDB**: `PolicyDocument`/`PolicySetDocument` (in `src/Encina.MongoDB/ABAC`): `TenantId` field, compound unique index `(TenantId, Id)` with a partial filter for null.
4. **Tenant id normalisation**: trim is rejected, not applied; a whitespace-only tenant is "no tenant" (decision of #3147).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT:
- abac_policy_sets / abac_policies: Id is the primary key in six SQL scripts, the EF configuration and the Mongo documents. Policy ids must become unique per tenant.
- TenantId is VARCHAR(128) NULL elsewhere (OperationAuditEntries).

TASK:
Add the tenant column, uniqueness and indexes to all providers' ABAC tables. Keep the SQL dialect rules (SqlServer TOP/bit, PostgreSQL quoted identifiers, MySQL backticks).

KEY RULES:
- Edit scripts in place; no ALTER scripts, no migration helpers.
- Case-sensitive tenant matching on every provider (collation noted in the script).
- PostgreSQL identifiers are quoted and case-sensitive.

REFERENCE FILES:
- src/Encina.ADO.PostgreSQL/Scripts/021_CreateAbacPolicySetsTable.sql
- src/Encina.ADO.PostgreSQL/Scripts/028_CreateOperationAuditEntriesTable.sql (TenantId column and filtered index)
- src/Encina.EntityFrameworkCore/Auditing/OperationAuditEntryEntityConfiguration.cs
```

</details>

### Phase 6: Multi-Provider ABAC Store Implementations (10 database providers)

> **Goal**: every provider class filters and stamps the tenant.

<details>
<summary><strong>Tasks</strong></summary>

| Provider | File | Change |
|----------|------|--------|
| ADO-SqlServer | `src/Encina.ADO.SqlServer/ABAC/PolicyStoreADO.cs` | `@TenantId` parameter, `WHERE ((TenantId = @TenantId) OR TenantId IS NULL)` for a tenant and `TenantId IS NULL` for none; insert/update stamp the column |
| ADO-PostgreSQL | `src/Encina.ADO.PostgreSQL/ABAC/PolicyStoreADO.cs` | same, quoted identifiers |
| ADO-MySQL | `src/Encina.ADO.MySQL/ABAC/PolicyStoreADO.cs` | same, backticks |
| Dapper-SqlServer | `src/Encina.Dapper.SqlServer/ABAC/PolicyStoreDapper.cs` | same with anonymous parameters |
| Dapper-PostgreSQL | `src/Encina.Dapper.PostgreSQL/ABAC/PolicyStoreDapper.cs` | same |
| Dapper-MySQL | `src/Encina.Dapper.MySQL/ABAC/PolicyStoreDapper.cs` | same |
| EFCore-SqlServer / PostgreSQL / MySQL | `src/Encina.EntityFrameworkCore/ABAC/PolicyStoreEF.cs` (one class for the three) | `Where(e => e.TenantId == tenantId || e.TenantId == null)` and the null-only variant; stamp on save |
| MongoDB | `src/Encina.MongoDB/ABAC/PolicyStoreMongo.cs` | `Filter.Or(Eq(TenantId, t), Eq(TenantId, null))`; stamp on upsert |

Every method: a `Left` is returned, never swallowed; asynchronous calls with `CancellationToken` only (`AGENTS.md` section 3). A tenant-scoped save of an existing global id is a conflict, not an overwrite.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```text
CONTEXT:
- Phase 5 columns exist. IPolicyStore now has tenantId parameters (Phase 2). The wrapper TenantScopedPolicyStore (Phase 3) is the only caller in production.

TASK:
Implement the tenant filter and stamp in the 8 store classes. One provider per commit; run that provider's unit and guard tests before the next.

KEY RULES:
- All 10 database providers in the same pull request (provider coherence, AGENTS.md section 5).
- Parameterised SQL only; no string concatenation of the tenant.
- Shared collection fixtures for integration tests; ClearAllDataAsync in InitializeAsync.

REFERENCE FILES:
- src/Encina.ADO.PostgreSQL/Auditing/OperationAuditStoreADO.cs (AddText tenant filter, line ~382)
- src/Encina.MongoDB/Auditing/OperationAuditStoreMongoDB.cs (line ~280)
- tests/Encina.IntegrationTests/Security/Audit/OperationAuditStoreADOPostgreSqlIntegrationTests.cs
```

</details>

### Phase 7: Caching

> **Goal**: tenant-segmented cache keys and invalidation.

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify `CachingPolicyStoreDecorator.cs`**: key builders take the tenant segment (`{prefix}:t:{segment}:policy-sets:all`, `...:policy-set:{id}`, `...:policies:all`, `...:policy:{id}`); `segment` is the tenant id percent-encoded, or `_` for none; the wrapper passes the tenant to the decorator through the same port parameter.
2. **Modify `PolicyCacheInvalidationMessage.cs`**: add `string? TenantId`; **modify `PolicyCachePubSubHostedService.cs`** to remove only that tenant's keys, and all tenants' keys (`RemoveByPatternAsync("{prefix}:t:*")`) for a global change.
3. **Tests**: tenant A's cached policy is never returned to tenant B; a global save invalidates both; the message round-trip carries the tenant.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```text
CONTEXT:
- CachingPolicyStoreDecorator uses ICacheProvider.GetOrSetAsync for stampede protection and publishes PolicyCacheInvalidationMessage over IPubSubProvider.

TASK:
Add the tenant segment to every key and the tenant to the invalidation message. Global changes invalidate by pattern.

KEY RULES:
- No key without a tenant segment; the segment cannot alias another tenant (encode the separator).
- Invalidation failures fail the mutation (AGENTS.md section 3, no swallowed errors).

REFERENCE FILES:
- src/Encina.Security.ABAC/Persistence/CachingPolicyStoreDecorator.cs
- src/Encina.Security.ABAC/Persistence/PolicyCachePubSubHostedService.cs
- tests/Encina.ContractTests/Security/ABAC/CachingPolicyStoreDecoratorContractTests.cs
```

</details>

### Phase 8: Cross-Cutting Integration

> **Goal**: the functions marked included in the matrix.

<details>
<summary><strong>Tasks</strong></summary>

1. **Transactions**: the gate adds no transaction; the existing store transactions are unchanged. A test proves a rolled-back audit write leaves no row for either tenant.
2. **Multi-tenancy**: `MultiTenancyMarker` is the signal; `OperationAuditStoreRegistration` documents that an application that resolves tenants without `Encina.Tenancy` registers the marker itself.
3. **Audit trail**: policy administration already writes `OperationAuditEntry` with the actor tenant (`PersistentPolicyAdministrationPoint.cs:806`); a test proves the tenant of a policy change equals the tenant of the policy row.
4. **Caching**: Phase 7.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```text
CONTEXT:
- Phases 1-7 are merged. Matrix items marked included: caching (Phase 7), OpenTelemetry (Phase 9), structured logging (Phase 9), transactions, multi-tenancy, audit.

TASK:
Write the cross-cutting tests and documentation hooks for transactions, multi-tenancy marker registration and audit attribution.

KEY RULES:
- Tenant ids are never logged in clear in messages; log codes.
- A test never swallows an exception thrown by the code under test.

REFERENCE FILES:
- src/Encina.Security.ABAC/Administration/PersistentPolicyAdministrationPoint.cs
- src/Encina/Abstractions/MultiTenancyMarker.cs
```

</details>

### Phase 9: Observability

> **Goal**: counters, tenant tag (through the shared helper) and log messages.

<details>
<summary><strong>Tasks</strong></summary>

1. **Metric**: `encina.audit.tenant_gate.denied` (`Counter<long>`, tag `reason` = `tenant_required` or `tenant_mismatch`) in `OperationAuditMeter` (`src/Encina.Security.Audit/Diagnostics/OperationAuditMeter.cs`); the same counter name family `encina.abac.policy_store.tenant_denied` in the ABAC meter. Instrument names follow ADR-054 settled point 4 (lowercase, dot separated, no `_total`).
2. **Tenant tag**: use the shared helper of #2676 (`SetTenantTag`/`WithTenantTag`) when it exists; until then no tag is emitted (ADR-054 D4: tag only when multi-tenancy is on). The tenant id is never a log or exception value.
3. **Logging**: Security.Audit ids 5013-5020 (existing range `SecurityAudit = (5000, 5099)`, used up to 5012); ABAC ids: 9000-9099 has 74 ids in use, so the ABAC part needs the new range `SecurityABACTenancy = (7400, 7419)` (register it in `src/Encina/Diagnostics/EventIdRanges.cs` first and add it to the `AssemblyRanges` entry of `Encina.Security.ABAC` in `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 9</strong></summary>

```text
CONTEXT:
- ADR-021 (register ranges first), ADR-054 D4 (tenant tag only when multi-tenancy is on; helper owned by #2676).

TASK:
Add the counters and [LoggerMessage] methods; register the ABAC range first; update the architecture test map; add PublicAPI lines.

KEY RULES:
- EventIds sequential, no gaps; ranges registered before use.
- Never log EncinaError.Message or the tenant id value.

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
- src/Encina.Security.Audit/Diagnostics/OperationAuditMeter.cs
```

</details>

### Phase 10: Testing — 7 Test Types

> **Goal**: per-flag targets in `.github/coverage-manifest/` and a two-tenant test on every provider.

<details>
<summary><strong>Tasks</strong></summary>

| Test type | Scope | Location |
|-----------|-------|----------|
| Unit | the three gates (all methods, both tenant modes), `PolicyEntityMapper`, in-memory PAP, caching keys | `tests/Encina.UnitTests/Security/Audit/`, `.../Security/ABAC/` |
| Guard | constructors and public methods of the new types | `tests/Encina.GuardTests/Security/` |
| Contract | `IOperationAuditStore`, `IReadAuditStore` and `IPolicyStore` tenant isolation contract run against every real implementation | `tests/Encina.ContractTests/Security/Audit/OperationAuditStoreContractTests.cs` (extend), new `PolicyStoreTenantContractTests.cs` |
| Property | FsCheck: for any two tenants and any entries, a query as A never returns an entry of B; policies likewise | `tests/Encina.PropertyTests/Security/Audit/OperationAuditStorePropertyTests.cs` (extend) |
| Integration | real databases in shared collections: write under tenants A and B, read as A, assert no B row; 10 providers plus Marten for audit, 10 providers for ABAC; Marten audit uses the PostgreSQL fixture | `tests/Encina.IntegrationTests/Security/Audit/`, `.../Security/ABAC/` |
| Load | justification `.md`: the gate adds one equality predicate | `tests/Encina.LoadTests/Security/Audit/TenantGate.md` |
| Benchmark | justification `.md`: the gate is a descriptor wrapper plus one comparison | `tests/Encina.BenchmarkTests/Security/Audit/TenantGate.md` |

**Per-file targets** (proposed; measured values replace them in the pull request, never lower):

| File | Flags | Justification |
|------|-------|---------------|
| `Security.Audit/TenantScopedOperationAuditStore.cs` | unit 98, guard 90, contract 90 | every branch of five reads and two writes is unit-testable; contract runs it over real stores |
| `Security.Audit/TenantScopedReadAuditStore.cs` | unit 98, guard 90 | same shape |
| `Security.Audit/OperationAuditStoreRegistration.cs` (existing unit 95) | unit 95 | the new helper adds two branches covered by the order tests |
| `Security.ABAC/Persistence/TenantScopedPolicyStore.cs` | unit 98, guard 90 | pure delegation with tenant resolution |
| `Security.ABAC/Persistence/CachingPolicyStoreDecorator.cs` | unit 90, guard 80, contract 85 | key building and invalidation are deterministic |
| `Security.ABAC/Administration/PersistentPolicyAdministrationPoint.cs` (existing unit 90, guard 5, integration 45) | unit 90, guard 5, integration 50 | integration rises with the two-tenant tests |
| the 8 provider stores | integration 80 | the integration flag is the only flag that reaches SQL |

CRAP gate: every method added stays at or below 10; extract the page loop of the `Get*` rewrites into a small private method.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 10</strong></summary>

```text
CONTEXT:
- Coverage is per flag (AGENTS.md section 9); tests must execute real package code; shared [Collection("<Family>-<Database>")] fixtures; Shouldly through Encina.Testing.Shouldly.

TASK:
Write the tests of the table above. The two-tenant integration test is one class per provider family and database, using the existing OperationAuditStore*IntegrationTests as the template. Update the manifests of Encina.Security.Audit and Encina.Security.ABAC (mechanical-fixer) with the targets measured locally.

KEY RULES:
- No FluentAssertions; no Thread.Sleep; builders for test data.
- Justification .md files only for load and benchmark.
- Run each flag with dotnet test --collect "XPlat Code Coverage" and coverage-report.cs before pushing.

REFERENCE FILES:
- tests/Encina.IntegrationTests/Security/Audit/OperationAuditStoreADOPostgreSqlIntegrationTests.cs
- tests/Encina.ContractTests/Security/Audit/OperationAuditStoreContractTests.cs
- tests/Encina.IntegrationTests/Compliance/Consent/ConsentTenantScopingIntegrationTests.cs
```

</details>

### Phase 11: Documentation & Finalization

> **Goal**: documents, public API, ADR, changelog.

<details>
<summary><strong>Tasks</strong></summary>

1. XML docs on every new public API (`<summary>`, `<remarks>`, `<example>` for the options and the registration helper).
2. `changelog.d/798-audit-abac-tenant-isolation.added.md` (bullets for the gates, the tenant-scoped policy store and the cache partition) and `changelog.d/798-policy-store-tenant-parameter.changed.md` (the `IPolicyStore` signature change), written by mechanical-fixer.
3. READMEs: `src/Encina.Security.Audit/README.md`, `src/Encina.Security.ABAC/README.md`; guide `docs/features/audit-tracking.md` and the ABAC guide (docs-writer): how the marker activates the gate, the registration helper, global policies.
4. ADR in `docs/architecture/adr/`: "Tenant gate as a decorator re-applied by the registration helper" once Design Choice 2 is answered.
5. `PublicAPI.Unshipped.txt` for both packages; `docs/INVENTORY.md` entry.
6. Build and test: `dotnet build Encina.slnx --configuration Release` with 0 warnings; every coverage flag at its target.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 11</strong></summary>

```text
CONTEXT:
- All code phases are merged; documentation and public API remain.

TASK:
Update documentation and tracking files through the specialists: docs-writer for pages and READMEs, mechanical-fixer for changelog fragments, PublicAPI lines and coverage manifests.

KEY RULES:
- Never edit CHANGELOG.md; fragments only.
- Documentation never types coverage figures by hand (covref markers).
- English only; no emojis.

REFERENCE FILES:
- changelog.d/README.md
- docs/features/audit-tracking.md
- src/Encina.Security.Audit/README.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Requirement | Use in this feature |
|--------|-------------|---------------------|
| SPEC-002 REQ-061, INV-008, AC-043 | every store tenant-aware; no query, export or background cycle crosses tenants; two-tenant test on 10 providers and Marten | scope and acceptance |
| SPEC-002 DEC-006, DEC-009 | gates fail closed; multi-tenancy required in 1.0 | Design Choices 1 and 6 |
| GDPR Art. 5(1)(f), Art. 28 | integrity and confidentiality; each practice is its own controller | tenant data segregation |
| ADR-036 | purpose-named audit stores | which stores are in scope |
| ADR-054 D4 | tenant tag only when multi-tenancy is on | Phase 9 |
| ADR-018 | 12-function evaluation | matrix below |
| 2972-D22 (#3271) | no AllTenants boolean | no cross-tenant switch |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
| :--- | :--- | :--- |
| OperationAuditEntry (record with `string? TenantId`) | src/Encina.Security.Audit/OperationAuditEntry.cs | the audit row; the tenant is already a column |
| OperationAuditQuery (`string? TenantId` filter, builder `ForTenant`) | src/Encina.Security.Audit/OperationAuditQuery.cs | the gate forces its TenantId |
| IOperationAuditStore | src/Encina.Security.Audit/Abstractions/IOperationAuditStore.cs | the port the gate wraps (4 reads plus `QueryAsync`, `RecordAsync`, `PurgeEntriesAsync`) |
| IReadAuditStore and ReadAuditQuery | src/Encina.Security.Audit/Abstractions/IReadAuditStore.cs, src/Encina.Security.Audit/ReadAuditQuery.cs | second audit store, same gate |
| OperationAuditStoreRegistration | src/Encina.Security.Audit/OperationAuditStoreRegistration.cs | removes the in-memory default and any decorator over it; the hook to re-apply the gate after a provider registers |
| IDecoratedServiceFactory | src/Encina/Core/IDecoratedServiceFactory.cs | lets the registration helper see through decorators |
| MultiTenancyMarker | src/Encina/Abstractions/MultiTenancyMarker.cs | container signal that multi-tenancy is on |
| IRequestContextAccessor | src/Encina/Abstractions/IRequestContextAccessor.cs | ambient request context with TenantId |
| ABACDecisionAuditReader (tenant gate: ambient wins, multi-tenant without tenant is denied) | src/Encina.Security.ABAC/DecisionAudit/ABACDecisionAuditReader.cs | reference behaviour for the new gates |
| DeadLetterManager and DefaultConsentService tenant scope | src/Encina.Messaging/DeadLetter/DeadLetterManager.cs, src/Encina.Compliance.Consent/Services/DefaultConsentService.cs | same fail-closed pattern already shipped |
| IPolicyStore | src/Encina.Security.ABAC/Persistence/IPolicyStore.cs | gains a tenant parameter |
| PolicyEntity and PolicySetEntity | src/Encina.Security.ABAC/Persistence/PolicyEntity.cs, PolicySetEntity.cs | gain a TenantId column |
| CachingPolicyStoreDecorator and PolicyCacheInvalidationMessage | src/Encina.Security.ABAC/Persistence/ | cache keys and invalidation gain the tenant segment |
| ServiceCollectionExtensions.ResolvePolicyStore | src/Encina.Security.ABAC/ServiceCollectionExtensions.cs | resolution-time wrapping point, order-proof |
| ICacheProvider.RemoveByPatternAsync | src/Encina.Caching/Abstractions/ICacheProvider.cs | invalidates every tenant key after a global policy change |
| InMemoryPolicyAdministrationPoint | src/Encina.Security.ABAC/Administration/InMemoryPolicyAdministrationPoint.cs | per-tenant dictionaries |
| PersistentPolicyAdministrationPoint (PolicyActor already carries TenantId) | src/Encina.Security.ABAC/Administration/PersistentPolicyAdministrationPoint.cs | the only consumer of IPolicyStore |
| Audit store implementations | src/Encina.ADO.*/Auditing, src/Encina.Dapper.*/Auditing, src/Encina.EntityFrameworkCore/Auditing, src/Encina.MongoDB/Auditing, src/Encina.Audit.Marten | already filter OperationAuditQuery.TenantId in QueryAsync |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Security.Audit` | 5013-5020 | inside the registered `SecurityAudit = (5000, 5099)`; 5000-5012 are in use |
| `Encina.Security.ABAC` | 7400-7419 (new `SecurityABACTenancy`) | `SecurityABAC = (9000, 9099)` has 74 of 100 ids in use; register the new range in `EventIdRanges.cs` first and map it to the ABAC assembly in `EncinaEventIdAllocationTests` |

### Estimated File Count

| Category | Files | Notes |
| :--- | :--- | :--- |
| Production, Security.Audit (gates, errors, options, registration hook, logging) | 6 | modified 3, new 3 |
| Production, audit registrations of the 8 provider packages and Marten | 9 | one line each in ServiceCollectionExtensions |
| Production, ABAC core (models, port, PAP, in-memory PAP, caching decorator, message, gate, errors, registration) | 12 | modified 10, new 2 |
| Production, ABAC providers (ADO x3, Dapper x3, EF, Mongo, EF configuration, Mongo document) | 10 | modified |
| SQL scripts (021 and 022, ADO x3 and Dapper x3) | 12 | modified in place (pre-1.0, no migration) |
| Tests (unit, guard, contract, property, integration, justification .md) | 34 | new and modified |
| Documentation (READMEs, docs/features, ADR, changelog fragments, PublicAPI) | 8 | docs-writer and mechanical-fixer |
| Total | 91 | split into five pull requests |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Combined prompt — all phases</strong></summary>

```text
PROJECT CONTEXT:
- Encina, .NET 10 / C# 14, pre-1.0, Railway Oriented Programming (Either<EncinaError, T>). Issue #798: tenant isolation for Encina.Security.Audit (IOperationAuditStore, IReadAuditStore) and Encina.Security.ABAC (IPolicyStore, Policy, PolicySet, PAP, cache).
- ITenantContext does not exist. The tenant is IRequestContext.TenantId via IRequestContextAccessor; multi-tenancy is on when MultiTenancyMarker is in the container. Missing tenant context fails closed.
- Prerequisite: ITenantScopeResolver / TenantScope from the #1257 plan (PR 1257-A).

IMPLEMENTATION OVERVIEW:
1. Add TenantId to Policy, PolicySet, PolicyEntity, PolicySetEntity, errors.
2. Add tenantId to IPolicyStore reads/deletes; adapt the two PAPs.
3. Add TenantScopedOperationAuditStore, TenantScopedReadAuditStore, TenantScopedPolicyStore.
4. Register gates in any order (AddTenantGate; ResolvePolicyStore wrapping); 9 audit registrations call the helper.
5. Add TenantId column/key/index to 12 SQL scripts, EF configuration, Mongo documents.
6. Implement the tenant filter in the 8 ABAC store classes.
7. Tenant-segment cache keys; tenant in the invalidation message.
8. Counters, log messages (ranges registered first), tenant tag through the #2676 helper.
9. Tests of all 7 types, two-tenant integration on 10 providers and Marten; manifests via mechanical-fixer.
10. Docs, ADR, changelog fragments (specialists).

KEY PATTERNS:
- Ambient tenant wins; multi-tenant with no tenant is denied; single-tenant passes with a null tenant filter; null tenantId means "rows with no tenant", never "all".
- No AllTenants switch (2972-D22). No EnableTenantIsolation switch.
- Decorators never swallow a Left; logs carry codes, never tenant ids or EncinaError.Message.
- Edit SQL scripts in place; no migration helpers; no [Obsolete].
- Shared [Collection] fixtures for integration tests; Shouldly via Encina.Testing.Shouldly.

REFERENCE FILES:
- src/Encina.Security.ABAC/DecisionAudit/ABACDecisionAuditReader.cs
- src/Encina.Security.Audit/OperationAuditStoreRegistration.cs
- src/Encina.Compliance.Consent/Services/DefaultConsentService.cs
- src/Encina.Security.ABAC/ServiceCollectionExtensions.cs (ResolvePolicyStore)
- docs/plans/abac-decision-audit-implementation-plan-751.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ✅ | Policy cache partitioned by tenant segment with pattern invalidation for global changes (Phase 7). |
| 2 | OpenTelemetry | ✅ | Denial counters now; tenant tag through the #2676 helper when it lands (ADR-054 D4). |
| 3 | Structured Logging | ✅ | `[LoggerMessage]` ids 5013-5020 and the new ABAC range 7400-7419; codes only, never tenant values. |
| 4 | Health Checks | ❌ | No new checkable dependency: the gates wrap existing stores. |
| 5 | Validation | ❌ | No new user input; tenant ids come from the request context and are compared, not parsed. |
| 6 | Resilience | ❌ | No new external call. |
| 7 | Distributed Locks | ❌ | No shared mutable state is introduced; cache invalidation is idempotent. |
| 8 | Transactions | ✅ | Gates add none; a test proves a rolled-back write leaves no row (Phase 8). |
| 9 | Idempotency | ❌ | Query filtering and append-only writes. |
| 10 | Multi-Tenancy | ✅ | This feature: SPEC-002 REQ-061 for the audit and ABAC stores. |
| 11 | Module Isolation | ⏭️ | `ModuleId` on audit rows is #1636; module-scoped policies are not requested anywhere. |
| 12 | Audit Trail | ✅ | The audit stores are the subject; policy changes keep recording the actor tenant. |

---

## Prerequisites & Dependencies

- **Hard**: the `ITenantScopeResolver` primitive (PR 1257-A of `multitenancy-spec002-implementation-plan-1257.md`). The issue's "Blocks on #357" is obsolete: #357 was closed as not planned and `ITenantContext` is not coming.
- **Soft**: #2676 (shared tenant telemetry helper), #3271 and #2972 (platform-operator cross-tenant access), #1164 (background tenant restore; the gates read the ambient context that #1164 rebuilds), #1778 (entity-change audit store, out of scope).
- **Conflicts to sequence**: #1636 (`ModuleId`) edits the same audit entry, query and store files; land #1636 PRs 1-2 or this plan's PR 1 first, not interleaved on one file.

---

## Next Steps

1. Maintainer answers Design Choices 2 and 3 when work on the issue starts (recorded in `## Maintainer Decisions`).
2. Split into five worker pull requests, each at most 15 files and 1500 lines:
   - **PR 1** (audit gates): Phases 1 (audit errors), 3 (audit gates), 4 (audit registrations: 9 provider files), 9 (audit part), tests, about 15 files.
   - **PR 2** (ABAC core): Phases 1 (ABAC), 2, 3 (policy store gate), 7, 4 (`ResolvePolicyStore`), about 14 files.
   - **PR 3** (ABAC ADO and Dapper): 6 stores plus 6 scripts, 12 files; tests in the same PR only if the limit allows, else PR 3b.
   - **PR 4** (ABAC EF Core and MongoDB): 4 files plus integration tests.
   - **PR 5** (contract, property, two-tenant integration suite and docs): tests, justification files, ADR, READMEs.
3. Follow-up issue file: `artifacts/issues/audit-per-tenant-retention.md` (per-tenant purge of audit entries, Design Choice 6).
4. Link this plan from #798.
