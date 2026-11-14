# Implementation Plan: Tenant isolation for `Encina.Security.Audit` and `Encina.Security.ABAC`

> **Issue**: [#798](https://github.com/dlrivada/Encina/issues/798)
> **Type**: Feature
> **Complexity**: High (11 phases, 10 database providers, Marten, 12 worker pull requests)
> **Estimated Scope**: ~108 files (about 55 production and script files, 42 test files, 11 documentation and tracking files); split into twelve pull requests (see Next Steps)
> **Specification**: [SPEC-002](../specifications/SPEC-002-eu-regulatory-readiness.md) REQ-061, INV-008, AC-043, DEC-006, DEC-009; Related plans (by issue number): #751, #800, #1257 (owns the shared tenant-scope primitive used here), #1636, #752, #847

---

## Summary

Make the two Security stores tenant-safe without a manual `WHERE` at every call site: the operation audit trail (`IOperationAuditStore`, entry `OperationAuditEntry`) and the read audit trail (`IReadAuditStore`) must never return, count or export another tenant's rows, and the ABAC policy store (`IPolicyStore`) must hold and evaluate policies per tenant.

**State of the code on `d1793713d` (current `origin/main`; the cited files and line numbers were re-read on it).**

- The issue speaks of `IAuditStore`, `AuditEntry`, `ITenantContext` and 13 providers. Today the store is `IOperationAuditStore` (ADR-036), the row is `OperationAuditEntry` (`src/Encina.Security.Audit/OperationAuditEntry.cs:86` has `string? TenantId`), the query is `OperationAuditQuery` (`TenantId` at `OperationAuditQuery.cs:49`), `ITenantContext` and a `TenantId` type do not exist, and the database matrix is 10 providers (SQLite is out, `AGENTS.md` section 5). The tenant abstraction is `IRequestContext.TenantId` read through `IRequestContextAccessor`, and "multi-tenancy is on" is the container signal `MultiTenancyMarker` (`src/Encina/Abstractions/MultiTenancyMarker.cs`) that `AddEncinaTenancy` registers.
- All audit stores already persist `TenantId` and already filter `OperationAuditQuery.TenantId` in `QueryAsync` (ADO, Dapper, EF Core, MongoDB, Marten with an indexed read model, and `InMemoryOperationAuditStore`). What is missing is the gate: nothing forces the ambient tenant, `GetByEntityAsync`, `GetByUserAsync` and `GetByCorrelationIdAsync` have no tenant parameter, and `IReadAuditStore` is in the same situation. `DefaultOperationAuditEntryFactory` already stamps `TenantId = context.TenantId` (`DefaultOperationAuditEntryFactory.cs:114`).
- `Policy`, `PolicySet`, `PolicyEntity`, `PolicySetEntity` and the tables `abac_policy_sets` and `abac_policies` (scripts `021` and `022` in six packages) carry no tenant, and `Id` alone is the primary key (`CONSTRAINT "PK_abac_policy_sets" PRIMARY KEY ("Id")`; EF `HasKey(x => x.Id)`; MongoDB `PolicyDocument.Id` is `[BsonId] [BsonElement("_id")]`). The upserts name that key: PostgreSQL `ON CONFLICT ("Id") DO UPDATE` (`PolicyStoreADO.cs:133`), SQL Server `MERGE ... ON Id`, MySQL `ON DUPLICATE KEY UPDATE` (relies on the primary key). `IPolicyStore` has 12 methods and is consumed only by `PersistentPolicyAdministrationPoint`, which receives the store through `ResolvePolicyStore` (`src/Encina.Security.ABAC/ServiceCollectionExtensions.cs:309`). `CachingPolicyStoreDecorator` builds keys as `{prefix}:policy-sets:all`, `{prefix}:policy:{id}` and so on, with no tenant.
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
- Single-tenant applications register no marker, see no ambient tenant and pay nothing beyond the resolver call (`AGENTS.md` section 1, pay-for-what-you-use). What a single-tenant read filters on is `scope.TenantId` as the #1257 resolver defines it: no tenant under #1257 Design Choice 2 A (shipped behavior, filter skipped), the default tenant under B. This plan never hard-codes `null` or `"default"`; the table below states both outcomes.
- **An ambient tenant with no marker is treated as multi-tenant** by the #1257 resolver (an application that resolves tenants itself and never registered the marker still gets the gate; the control is never off by configuration, the 2026-10-30 principle). The audit gate unit tests include that case (ambient tenant, no marker, reads and writes are gated; a different requested tenant is denied).
- `ITenantContext` stays out: when #456 delivers a typed tenant, the gate's single call to the shared resolver (Design Choice 1 of the #1257 plan) changes in one place.

| Behavior of this plan | Under #1257 Design Choice 2 A (null tenant) | Under B (reserved `default` tenant) |
|-----------------------|---------------------------------------------|-------------------------------------|
| Single-tenant read of the audit or policy stores | `scope.TenantId` is null: the tenant filter is skipped (audit) / only global rows exist (policies) | `scope.TenantId` is `default`: audit rows with tenant `default` and (policies) `default` rows plus global rows; rows written before the change under NULL need the #1257 alignment issue |
| Audit write with `allowTenantless: true`, single-tenant application | null-tenant row | `default`-tenant row |
| Audit write with no ambient tenant in a multi-tenant application | no-tenant row (operator or background; Design Choice 6) | the same no-tenant row |
| Policy seeding (`ABACPolicySeedingHostedService`) | single-tenant application: global rows (no tenant exists); multi-tenant: global rows | single-tenant application: `default` rows (`scope.TenantId`); multi-tenant: global rows. The gate decides "global write" from `scope.IsMultiTenant`, the absence of an ambient tenant and the caller's declared service identity, never from `scope.TenantId == null` |
| Cache key segment | `TenantCacheKey.Segment(null)` = `g` | `TenantCacheKey.Segment("default")` = `t.default`; global policies keep the explicit `g` namespace |
| Key of the policy tables | `(TenantId, Id)` with `TenantId` empty for a global row (Design Choice 7) | the same; `default` is an ordinary non-empty tenant value |

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

- The wrapper is built in `ResolvePolicyStore` as the outermost layer, above the caching decorator, so the scope is resolved before every call (a cache hit included) and the wrapper cannot be bypassed by registration order (the PAP resolves it per operation scope).
- `tenantId == null` means "rows with no tenant" and never "every tenant"; there is no `AllTenants` parameter (2972-D22).
- `Policy` and `PolicySet` gain `string? TenantId { get; init; }` as the issue proposes; the entity column is the source of truth and the mapper overwrites the model value on read, so a serialized `PolicyJson` cannot smuggle another tenant.

</details>

<details>
<summary><strong>5. Cache partition: a tenant segment in every key, pattern invalidation for global changes</strong></summary>

**Class:** Trivial
**Settled by:** the Consent fix #1315 and the segmented keys of the #847 plan (Design Choice 6C, `aiact:tenant:{segment}:system:{id}`); open bug class #1949 (LawfulBasis cache keys ignore the tenant); the shared segment builder `TenantCacheKey.Segment` of the #1257 plan.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Keys `{prefix}:t:{segment}:policy-sets:all` and so on; global rows cached under a disjoint `g` namespace; a generation counter in the cache protects against in-flight fills (Phase 7); the Pub/Sub message carries the tenant** | One cache; works on every `ICacheProvider` that supports patterns | Pattern removal needs provider support (all 8 caches implement `RemoveByPatternAsync`) |
| **B) A separate `ICacheProvider` per tenant** | Hard separation | Unbounded cache instances; breaks the singleton cache registration |
| **C) No cache in a multi-tenant application** | Nothing to leak | Removes the stampede protection the decorator exists for |

### Chosen Option: **A — Tenant-segmented keys and pattern invalidation**

### Rationale

- The segment is `TenantCacheKey.Segment(tenantId)` of the #1257 plan (percent-encoded, so a padded or ':'-bearing tenant id cannot alias another tenant, the #3147 bug class; injective).
- `PolicyCachePubSubHostedService` forwards the tenant so other nodes drop the same keys.

</details>

<details>
<summary><strong>6. Behavior without a tenant, on writes, and for purge</strong></summary>

**Class:** Dilemma (recommended, pending the maintainer)

Common to both options: reads fail closed (ambient tenant wins; a different requested tenant is denied; no ambient tenant in a multi-tenant application is denied), and `PurgeEntriesAsync` deletes by age across all tenants, so the gate denies it with `tenant_mismatch` whenever the request carries a tenant and lets it through only for the tenantless background caller; the audited platform-operator path is #3271, and per-tenant purge is the follow-up issue file `audit-per-tenant-retention`. The two options differ on **writes that have no tenant**. The shipped precedent returns an error there: `DefaultConsentService.TryResolveWriteTenantScope` (`src/Encina.Compliance.Consent/Services/DefaultConsentService.cs`) returns `ConsentErrors.TenantRequired` when no tenant resolves and tenancy is required, i.e. it denies the write.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A write with neither an ambient nor an explicit tenant in a multi-tenant application is stored as a no-tenant row (`ResolveWrite(entry.TenantId, allowTenantless: true)`); a mismatch is still denied** | Evidence is never lost: a background writer or system actor (startup seeding, outbox processors) has no tenant and is still audited | Departs from the shipped Consent precedent and from "gates fail closed" for writes; a request that lost its tenant (a bug upstream) also produces such rows and is mis-attributed to nobody; the no-tenant rows are readable only through the operator path (a gated tenant read never sees them), so a tenant cannot see the entries of its own lost-tenant requests |
| **B) The write fails with `tenant_required` (the Consent precedent and `AGENTS.md` section 3)** | Every row has a tenant; a request that lost its tenant is refused, never mis-attributed | System actors cannot be audited by the gated store, which loses evidence unless they run under an explicit tenant or through the operator path |

### Chosen Option: **A — No-tenant rows for tenantless writes, counted and logged** (recommended, pending the maintainer)

### Rationale

- A wins when evidence must never be lost, the reading in which an audit write that fails is worse than an unattributed row (system actors exist and have no tenant); B wins when attribution matters more, which is the Consent precedent and the stricter reading of fail closed. The choice is the maintainer's.
- Under A each such write increments `encina.audit.tenant_gate.untenanted_writes` and logs EventId 5014 by code, so a request that lost its tenant is visible; the no-tenant rows are readable only when the reader has no ambient tenant (the operator path, #3271), and the documentation says so.
- Single-tenant applications (no marker, no ambient tenant) pass through unchanged by the #1257 single-tenant scope.
- The platform-operator cross-tenant read is out of scope and will come from #3271 with a mandatory reason; no boolean switch is added here.

</details>

<details>
<summary><strong>7. The key of the policy tables: how `(tenant, id)` is unique with a global scope, and what the upserts target</strong></summary>

**Class:** Dilemma (recommended, pending the maintainer)

Today `Id` alone is the primary key and every upsert names it (State of the code). The tenant must become part of the key on every provider, and the upsert, the `MERGE` and the delete must name the new key, or a save by tenant B of an id that tenant A holds overwrites tenant A's policy (a cross-tenant write).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `TenantId VARCHAR(128) NOT NULL DEFAULT ''` (empty = global row, the model value null is mapped to and from the empty string, and a tenant id is never blank by the #1257 rule), primary key `(TenantId, Id)`. Conflict targets: PostgreSQL `ON CONFLICT ("TenantId","Id")`, SQL Server `MERGE ... ON t.TenantId = s.TenantId AND t.Id = s.Id`, MySQL `ON DUPLICATE KEY UPDATE` on that primary key, EF Core `HasKey(e => new { e.TenantId, e.Id })`, MongoDB `_id` a composite sub-document `{ TenantId, Id }` with the save filter on the whole `_id`** | One mechanism on every provider; no expression index (SQL Server cannot index an `ISNULL(...)` expression); the key is injective by construction (separate fields, no string joining); MongoDB uniqueness per tenant is real because `_id` itself is composite | A sentinel value for global rows in the column |
| **B) Nullable `TenantId`, a surrogate primary key, two filtered unique indexes (`(TenantId, Id) WHERE TenantId IS NOT NULL` and `(Id) WHERE TenantId IS NULL`; MySQL has no partial index and needs a generated column)** | The column stays null for a global row | Different mechanisms per provider; the upsert needs two conflict targets; MongoDB still needs a composite `_id` or a separate unique index, which does not constrain `_id` |
| **C) Separate tables for global and tenant policies** | No sentinel | Two tables on 10 providers; every read is a union |

### Chosen Option: **A — Composite key with an empty-string global scope** (recommended, pending the maintainer)

### Rationale

- A wins when one portable key and an injective identity matter most; B wins when a null global column is preferred over a sentinel and the per-provider index work is accepted.
- **A save of an id that already exists in the other scope is a conflict, not a second row** (tenant reads return the tenant's rows and the global ones, so one id must not name two policies): a tenant save is refused with `encina.authorization.abac_policy_id_conflict` (`ErrorCategory.Conflict`, ADR-062 R2) when a global row has the id, and a global save (operator path only) when any tenant row has it; the check and the write run in the same transaction (or the same statement) on every provider, and a test per provider proves tenant B's save of tenant A's id changes nothing of A.
- **Case-sensitive tenant matching on every provider**: the `TenantId` column uses a binary collation on SQL Server (`Latin1_General_100_BIN2`) and MySQL (`utf8mb4_bin`), whose defaults are case-insensitive; PostgreSQL and MongoDB are case-sensitive; the primary key inherits it; a test stores tenant `ACME` and `acme` and proves they are different tenants on every provider.

</details>

<details>
<summary><strong>8. Marten audit store: manual `TenantId` filtering, not Marten's native tenancy</strong></summary>

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

> **Goal**: add the tenant to the policy models and entities, the error codes and the (small) option surface. Prerequisite: the `ITenantScopeResolver` primitive of #1257 (PRs 1257-A1 and A2: resolver, `TenantScope`, `TenantCacheKey`, errors, range) is merged.

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify `src/Encina.Security.ABAC/Model/Policy.cs`** and **`PolicySet.cs`**: add `public string? TenantId { get; init; }` with XML docs ("null means a global policy shared by every tenant").
2. **Modify `src/Encina.Security.ABAC/Persistence/PolicyEntity.cs`** and **`PolicySetEntity.cs`**: add `public string? TenantId { get; set; }`.
3. **Modify `src/Encina.Security.ABAC/Persistence/PolicyEntityMapper.cs`**: write the model tenant to the column (null mapped to the empty string, Design Choice 7); on read set the model `TenantId` from the column (empty mapped to null; the column wins over any value inside `PolicyJson`).
4. **Modify `src/Encina.Security.ABAC/ABACErrors.cs`**: add four codes created with `EncinaErrors.Create(code, category, message)` (ADR-062), fixed messages, no tenant value (same shape as `DecisionAuditTenantRequired`): `encina.authorization.abac_policy_tenant_required` (`ErrorCategory.InvalidRequest`, R3: the request lacks a tenant), `encina.authorization.abac_policy_tenant_mismatch` (`Forbidden`), `encina.authorization.abac_policy_id_conflict` (`Conflict`, R2: Design Choice 7), `encina.authorization.abac_policy_tenant_unsupported` (`Internal`, R1: the staged refusal of Phase 2, removed with the last family). A `GetCategory` unit test per code.
5. **Modify `src/Encina.Security.Audit/OperationAuditErrors.cs`** and **`ReadAuditErrors.cs`**: add `TenantRequired` (`encina.audit.tenant_required`, `InvalidRequest`) and `TenantMismatch` (`encina.audit.tenant_mismatch`, `Forbidden`) factories, and in `OperationAuditErrors` `HistoryLimitExceeded` (`encina.audit.history_limit_exceeded`, `InvalidRequest`: the caller asked for more history than the cap, Phase 3); a `GetCategory` unit test per code.
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

1. **Modify `src/Encina.Security.ABAC/Persistence/IPolicyStore.cs`** (staged so every pull request builds, see Next Steps): add `string? tenantId = null` to `GetAllPolicySetsAsync`, `GetPolicySetAsync`, `DeletePolicySetAsync`, `ExistsPolicySetAsync`, `GetPolicySetCountAsync`, `GetAllStandalonePoliciesAsync`, `GetPolicyAsync`, `DeletePolicyAsync`, `ExistsPolicyAsync`, `GetPolicyCountAsync`. `SavePolicySetAsync` and `SavePolicyAsync` take the tenant from the model. XML docs state: `tenantId == null` returns rows with no tenant (global); a non-null tenant returns its own rows plus the global ones (Design Choice 3, recommended); `Id` is unique per `(tenant, id)` and an id never names a tenant row and a global row at once (Design Choice 7). **Staging (PR 3a):** the eight store classes and `CachingPolicyStoreDecorator` take the parameter in the same pull request as the interface; until their family pull request (PRs 4 to 6) implements the tenant, a store **refuses (fail closed, never ignores)** a non-null `tenantId` and a save of a `Policy`/`PolicySet` whose `TenantId` is not null with `Left(encina.authorization.abac_policy_tenant_unsupported)` (`ErrorCategory.Internal`, a server-side limit, R1), so no row can be silently written without its tenant; the last family pull request removes the refusal. The new parameter is the **last** one, after `cancellationToken`, and defaults to `null`, so a caller that passes the token positionally still compiles and the existing callers and tests compile unchanged: the implementers are the 8 store classes and `CachingPolicyStoreDecorator`; the callers verified on `origin/main` are `PersistentPolicyAdministrationPoint` (store calls, changed in PR 7b) and `ABACHealthCheck` (`GetPolicySetCountAsync` and `GetPolicyCountAsync`, which keep counting the global scope until PR 7b); the existing tests that call the old signatures (8 store guard tests, 9 store integration tests, the decorator unit, time, guard and contract tests, 4 PAP unit tests, `PersistentPAPContractTests`, `ABACHealthCheckTests`, `ABACRegistrationTests`) stay unchanged in PR 3a and are counted in no PR. Until PR 7b every store call from these callers carries no tenant, so no tenant-bearing policy can be created or read before PR 7b and the window between PRs 2 and 7b leaks nothing (Design Choice 7: a non-null tenant is refused until its family merges, and the cache keys of PR 3b are never reached with a tenant).
2. **Modify `src/Encina.Security.ABAC/Administration/PersistentPolicyAdministrationPoint.cs`** (PR 7b): pass the tenant of the `PolicyActor` (already captured, line ~98 and ~651) to the store calls as the named argument `tenantId:` and stamp it on added policies (PR 7b also changes `src/Encina.Security.ABAC/Health/ABACHealthCheck.cs` so its counts name the scope they count); reject an add whose `TenantId` differs from the actor's tenant, and a global add (`TenantId` null) when the actor has a tenant.
3. **Modify `src/Encina.Security.ABAC/Administration/InMemoryPolicyAdministrationPoint.cs`** (PR 2): replace the three `ConcurrentDictionary` fields with dictionaries keyed by the value tuple `(tenant, id)` (a tuple, not a joined string, so the key is injective), same visibility and same id-conflict rule as the store.

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

1. **Create `src/Encina.Security.Audit/TenantScopedOperationAuditStore.cs`**: `internal sealed class` implementing `IOperationAuditStore`, constructor `(IOperationAuditStore inner, ITenantScopeResolver tenants, TimeProvider timeProvider, ILogger<TenantScopedOperationAuditStore> logger)` (the `TimeProvider` is read once per `Get*` call for the snapshot bound below).
   - `RecordAsync(entry)` (Design Choice 6, recommended A): `tenants.ResolveWrite(entry.TenantId, allowTenantless: true)` (the #1257 resolver signature `ResolveWrite(string? explicitTenantId, bool allowTenantless = false)`; with `allowTenantless` it returns `TenantScope(null, true)` for a multi-tenant application with neither an ambient nor an explicit tenant, the single-tenant scope otherwise, and still denies a mismatch); stamp the tenant (`entry with { TenantId = scope.TenantId }`); deny on mismatch; count and log a write stamped with no tenant in a multi-tenant application; forward. Under Design Choice 6 B the call is `ResolveWrite(entry.TenantId)` and a missing tenant returns `tenant_required`.
   - `QueryAsync(query)`: `tenants.ResolveRead(query.TenantId)` then `query with { TenantId = scope.TenantId }`.
   - `GetByEntityAsync`, `GetByUserAsync`, `GetByCorrelationIdAsync`: rewrite to `QueryAsync` pages with the resolved tenant and the same ordering and date bounds (`GetByCorrelationIdAsync` reverses to ascending), looping pages until exhausted: the page size is 500 and the loop stops after at most 1,000 pages (500,000 rows) with `Left(encina.audit.history_limit_exceeded)`; **a `Left` from any page ends the loop and is returned as is** (never a partial list reported as success). To keep a complete history under concurrent audit writes, the first call pins an upper bound (`ToUtc` = `timeProvider.GetUtcNow()` read once, the same bound for every page) so rows recorded during the loop fall outside the snapshot, and the order is the stable (timestamp, id) order the stores already guarantee (`OperationAuditStableOrderCheck`); a concurrent purge can still shorten the result and is an operator operation. Document the cost. The unit tests prove equivalence with the inner `Get*` for the in-memory store, the cap, and the `Left`-on-a-later-page case.
   - `PurgeEntriesAsync`: `ResolveWrite(null, allowTenantless: true)`; when the scope carries a tenant return `tenant_mismatch`; with no ambient tenant forward (Design Choice 6).
2. **Create `src/Encina.Security.Audit/TenantScopedReadAuditStore.cs`**: same for `IReadAuditStore` (`LogReadAsync`, `GetAccessHistoryAsync`, `GetUserAccessHistoryAsync`, `QueryAsync`, `PurgeEntriesAsync`).
3. **Create `src/Encina.Security.ABAC/Persistence/TenantScopedPolicyStore.cs`**: implements `IPolicyStore` over an inner `IPolicyStore`; each read resolves `ResolveRead(null)` and passes `scope.TenantId` to the inner port; `Save*` stamps `Policy.TenantId`/`PolicySet.TenantId` from `ResolveWrite(policy.TenantId)`. **Permitted path for global writes** (Design Choice 3): the gate decides a global write from `scope.IsMultiTenant`, the absence of an ambient tenant and the caller's declared identity, never from `scope.TenantId == null` (so it is independent of the #1257 default-tenant Dilemma, table of Design Choice 1): a save with `TenantId == null` in a multi-tenant application is accepted only when there is no ambient tenant **and** the caller is a declared service or operator identity (`PolicyActor.Kind`, the identity of `ABACPolicySeedingHostedService`), using `ResolveWrite(null, allowTenantless: true)`; an anonymous or user caller without a tenant in a multi-tenant application is denied with `PolicyStoreTenantRequired`; in a single-tenant application the row takes `scope.TenantId` as the #1257 resolver defines it. Tests: seeding writes global rows in a multi-tenant application, a tenant caller cannot, an undeclared tenantless caller cannot, a single-tenant application seeds rows with the scope's tenant.
4. **Log messages** (two log classes): `TenantScopedAuditLog` in Security.Audit (ids from Event ID Allocation: 5013 tenant denied, 5014 write stamped with no tenant, 5015 history limit exceeded; codes only, never the tenant id) and `TenantScopedPolicyStoreLog` in Security.ABAC (7400 tenant denied, 7401 global write by a declared identity, 7402 policy id conflict).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT:
- ITenantScopeResolver and TenantScope come from the #1257 plan (core, namespace Encina): ResolveRead(string? requestedTenantId) and ResolveWrite(string? explicitTenantId, bool allowTenantless = false) return Either<EncinaError, TenantScope>. Ambient tenant wins; multi-tenant (MultiTenancyMarker or an ambient tenant) with no tenant is denied; single-tenant passes with the single-tenant scope (no tenant, or the default tenant, per the #1257 Dilemma: never hard-code either).
- Design Choices 2, 3, 4 and 6 of the #798 plan.

TASK:
Implement the three decorators exactly as listed in the plan's Phase 3 tasks, with unit tests per method (two tenants, no tenant in multi-tenant, no tenant in single-tenant, mismatched explicit tenant).

KEY RULES:
- A Left from the inner store is returned as is; never swallowed (AGENTS.md section 3).
- Error messages and logs never carry the tenant id or EncinaError.Message; use the code.
- TimeProvider is injected into TenantScopedOperationAuditStore only (the Get* snapshot bound); the other gates read no time.

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
4. **Modify `src/Encina.Security.ABAC/ServiceCollectionExtensions.cs`** `ResolvePolicyStore` (line 309): build the chain inner store, then `CachingPolicyStoreDecorator`, then `TenantScopedPolicyStore` as the **outermost** wrapper, so the scope is resolved before every call, a cache hit included (a null-tenant entry can never answer a tenantless multi-tenant request); the cache decorator keys by the tenant parameter the gate passes down. A test proves a warm cache still denies a tenantless request.
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

Each family changes its schema **together with its store's upsert** (PRs 4 to 6), because changing the key while an upsert still names `Id` breaks the statement. The design is Design Choice 7 A (recommended).

1. **The six `021_CreateAbacPolicySetsTable.sql` and six `022_CreateAbacPoliciesTable.sql`** (ADO and Dapper, SqlServer, PostgreSQL, MySQL): add `TenantId VARCHAR(128) NOT NULL DEFAULT ''` with a binary collation on SQL Server (`Latin1_General_100_BIN2`) and MySQL (`utf8mb4_bin`); the primary key becomes `(TenantId, Id)`; add `IX_*_TenantId`. The upsert, `MERGE`, delete and exists statements name the new key: PostgreSQL `ON CONFLICT ("TenantId","Id")`, SQL Server `MERGE ... ON t.TenantId = s.TenantId AND t.Id = s.Id`, MySQL `ON DUPLICATE KEY UPDATE` (on the composite primary key), and the id-conflict check of Design Choice 7 runs in the same transaction or statement. Edited in place: pre-1.0, no migration helpers (`AGENTS.md` sections 1 and 3).
2. **EF Core**: `PolicyEntityConfiguration` and `PolicySetEntityConfiguration` (`src/Encina.EntityFrameworkCore/ABAC`, today `HasKey(x => x.Id)`): `HasKey(e => new { e.TenantId, e.Id })`, `TenantId` required with max length 128 and a case-sensitive column collation per provider (binary on SQL Server and MySQL), the mapper turning the model's null tenant into the empty string.
3. **MongoDB**: `PolicyDocument`/`PolicySetDocument` (`src/Encina.MongoDB/ABAC`; `Id` is `[BsonId] [BsonElement("_id")]` today, so a compound index on `(TenantId, Id)` would **not** make ids unique per tenant: `_id` stays globally unique and the save filter `Eq(d.Id, ...)` upserts across tenants): `_id` becomes a composite sub-document `{ TenantId, Id }`, the save filter and the delete filter use the whole `_id`, `TenantId` is also stored as a field for the read filter. A test saves the same policy id under tenants A and B and proves two documents exist and that B's save never modifies A's.
4. **Tenant id normalisation**: trim is rejected, not applied; a whitespace-only tenant is "no tenant" (decision of #3147); the empty string is therefore never a tenant id and can be the global scope.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT:
- abac_policy_sets / abac_policies: Id is the primary key in six SQL scripts, the EF configuration and the Mongo documents, and every upsert names it. Policy ids must become unique per (tenant, id), with the empty string as the global scope (Design Choice 7 A).

TASK:
One family per pull request: change its scripts, entity or document, mapper and the store's upsert, merge, delete and exists statements to the composite key in the same pull request. Keep the SQL dialect rules (SqlServer TOP/bit, PostgreSQL quoted identifiers, MySQL backticks).

KEY RULES:
- Edit scripts in place; no ALTER scripts, no migration helpers.
- Case-sensitive tenant matching on every provider (binary collation on SQL Server and MySQL); a test with tenants ACME and acme per provider.
- MongoDB: a composite _id sub-document, never a compound index alone.
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
| ADO-SqlServer | `src/Encina.ADO.SqlServer/ABAC/PolicyStoreADO.cs` | `@TenantId` parameter. Reads: `WHERE (TenantId = @TenantId OR TenantId = '')` for a tenant and `TenantId = ''` for none. Delete, exists-for-update and save: exact ownership only (`TenantId = @TenantId`, or `= ''` for the global writer), never the combined predicate; the `MERGE` and the delete name `(TenantId, Id)`; insert/update stamp the column; the id-conflict check of Design Choice 7 |
| ADO-PostgreSQL | `src/Encina.ADO.PostgreSQL/ABAC/PolicyStoreADO.cs` | same, quoted identifiers; `ON CONFLICT ("TenantId","Id") DO UPDATE` (today `ON CONFLICT ("Id")`, line 133) |
| ADO-MySQL | `src/Encina.ADO.MySQL/ABAC/PolicyStoreADO.cs` | same, backticks; `ON DUPLICATE KEY UPDATE` now fires on the composite primary key |
| Dapper-SqlServer | `src/Encina.Dapper.SqlServer/ABAC/PolicyStoreDapper.cs` | same with anonymous parameters |
| Dapper-PostgreSQL | `src/Encina.Dapper.PostgreSQL/ABAC/PolicyStoreDapper.cs` | same |
| Dapper-MySQL | `src/Encina.Dapper.MySQL/ABAC/PolicyStoreDapper.cs` | same |
| EFCore-SqlServer / PostgreSQL / MySQL | `src/Encina.EntityFrameworkCore/ABAC/PolicyStoreEF.cs` (one class for the three) | reads: `Where(e => e.TenantId == tenantId \|\| e.TenantId == "")` and the global-only variant; delete and save: exact ownership `e.TenantId == tenantId` and the whole key `(TenantId, Id)`; stamp on save |
| MongoDB | `src/Encina.MongoDB/ABAC/PolicyStoreMongo.cs` | `Filter.Or(Eq(TenantId, t), Eq(TenantId, ""))`; upsert and delete filter on the whole composite `_id`; stamp on upsert |

Every method: a `Left` is returned, never swallowed; asynchronous calls with `CancellationToken` only (`AGENTS.md` section 3). A tenant-scoped save of an id that a global row holds is `abac_policy_id_conflict`, not a second row and not an overwrite (Design Choice 7).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```text
CONTEXT:
- Phase 5 columns exist. IPolicyStore now has tenantId parameters (Phase 2). The wrapper TenantScopedPolicyStore (Phase 3) is the only caller in production.

TASK:
Implement the tenant filter and stamp in the 8 store classes. One provider per commit; run that provider's unit and guard tests before the next.

KEY RULES:
- All 10 database providers, one family per pull request (PR 4 ADO.NET, PR 5 Dapper, PR 6 EF Core and MongoDB); the tenant gate of PR 7a is registered only after the last family merged, and until then each store refuses a non-null tenant (provider coherence, AGENTS.md section 5).
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

1. **Modify `CachingPolicyStoreDecorator.cs`** (PR 3b; in PR 3a, the port change, the decorator forwards the tenant and **bypasses the cache for a non-null tenant**, so no pull request can serve one tenant's cached policies to another and no tenant ever reaches a cache key before the segmented keys exist): key builders take the scope from the tenant parameter and use `TenantCacheKey.Segment(tenantId)` of #1257 (`t.{percent-encoded tenant}` for a tenant, `g` for none): tenant keys are `{prefix}:{segment}:{gen}:policy-sets:all`, `...:policy-set:{id}`, `...:policies:all`, `...:policy:{id}`, global keys are `{prefix}:g:{gen}:...`. The two namespaces are disjoint by their tag, so tenant id `_` or any other id cannot alias the global scope; the segment is injective.
2. **In-flight fills (generation counter)** (PR 3b): each scope has a generation number in the shared cache (`{prefix}:{segment}:gen`, `{prefix}:g:gen`). A read loads the generation before it reads the store and builds its keys with it; every save or delete increments the generation of its scope after the store write (a global change increments the global generation, and the tenant keys embed both generations). A fill that raced with a write lands under the old generation key and is never read again. This works across instances because the counter lives in the cache, and it needs no pattern removal for the stale-read guarantee (old keys expire by TTL).
3. **Modify `PolicyCacheInvalidationMessage.cs`** (PR 3b): add `string? TenantId`; **modify `PolicyCachePubSubHostedService.cs`** so other nodes re-read the generation of that scope (a global message re-reads the global one).
4. **Tests**: tenant A's cached policy is never returned to tenant B; tenant id `_` never reads the global entries; a global save makes both views fresh; the message round-trip carries the tenant; ordering test across two decorator instances sharing one cache: instance 1 reads the old policy, instance 2 saves and invalidates, instance 1 fills late, and the next read does not serve the old policy.

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

1. **Metric**: `encina.audit.tenant_gate.denied` (`Counter<long>`, tag `reason` = `tenant_required` or `tenant_mismatch`) and, under Design Choice 6 A, `encina.audit.tenant_gate.untenanted_writes` in `OperationAuditMeter` (`src/Encina.Security.Audit/Diagnostics/OperationAuditMeter.cs`; PR 1a); the counter `encina.abac.policy_store.tenant_denied` in the ABAC meter (PR 7a). Instrument names follow ADR-054 settled point 4 (lowercase, dot separated, no `_total`).
2. **Tenant tag**: use the shared helper of #2676 (`SetTenantTag`/`WithTenantTag`) when it exists; until then no tag is emitted (ADR-054 D4: tag only when multi-tenancy is on). The tenant id is never a log or exception value. The linked issue's `TenantId` in logging scopes is **not delivered** (this plan logs codes only, so a tenant id never reaches a log), and its metric `encina.audit.tenant_isolation.enabled` is replaced by the denial counters: with an always-on gate an "enabled" gauge carries no information.
3. **Logging**: Security.Audit ids **5013-5015 only** (three messages, the existing range `SecurityAudit = (5000, 5099)`, used up to 5012; 5016-5020 stay unallocated); ABAC ids **7400-7402** (three messages in the new range `SecurityABACTenancy = (7400, 7419)`, assigned in `artifacts/decisions/plan-batch/eventid-blocks.md`; `SecurityABAC = (9000, 9099)` has 74 ids in use). Register the new range in `src/Encina/Diagnostics/EventIdRanges.cs` first (PR 7) and add it to the `AssemblyRanges` entry of `Encina.Security.ABAC` in `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs`. Packed sequentially from the start of each block, no gaps.

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

"Baseline" is the per-file target in `.github/coverage-manifest/` on `d1793713d`; "none" means no per-file entry (the package default applies). A modified file keeps its baseline at least.

| File | Baseline | Flags | Justification |
|------|----------|-------|---------------|
| `Security.Audit/TenantScopedOperationAuditStore.cs` (new, 1a) | new | unit 98, guard 90, contract 90 | every branch of five reads and two writes is unit-testable; contract runs it over real stores. |
| `Security.Audit/TenantScopedReadAuditStore.cs` (new, 1a) | new | unit 98, guard 90 | same shape. |
| `Security.Audit/OperationAuditStoreRegistration.cs` (1a) | unit 95 | unit 95 | the new helper adds two branches covered by the order tests. |
| `Security.Audit/ServiceCollectionExtensions.cs` (1a) | unit 92, guard 82 | unchanged | one call in a covered method. |
| `Security.Audit/OperationAuditErrors.cs`, `ReadAuditErrors.cs` (1a) | unit 100, guard 20, contract 100; unit 100, guard 6 | unchanged | factories asserted with their category. |
| `Security.Audit/Diagnostics/OperationAuditMeter.cs` (1a) | unit 100, guard 0 | unchanged | two counters read through a meter listener. |
| `Security.Audit/Diagnostics/TenantScopedAuditLog.cs` (new, 1a) | new | unit 90 | generated logging methods exercised with a fake logger. |
| `Security.ABAC/Model/Policy.cs`, `PolicySet.cs`, `Persistence/PolicyEntity.cs`, `PolicySetEntity.cs`, `PolicyEntityMapper.cs` (2) | none | unit 90 | one property each; the mapper's null/empty mapping is unit-tested both ways. |
| `Security.ABAC/Administration/InMemoryPolicyAdministrationPoint.cs` (2) | none | unit 90 | the tuple-keyed dictionaries and the id-conflict rule are unit-tested. |
| `Security.ABAC/ABACErrors.cs` (2) | unit 85, guard 90 | unit 85, guard 90 | four factories asserted with their category. |
| `Security.ABAC/Persistence/IPolicyStore.cs` (3a) | none | unit 0 | an interface, no executable line. |
| `Security.ABAC/Persistence/CachingPolicyStoreDecorator.cs` (3a, 3b) | none | unit 90, guard 80, contract 85 | key building, the bypass and invalidation are deterministic. |
| `Security.ABAC/Persistence/PolicyCacheInvalidationMessage.cs`, `PolicyCachePubSubHostedService.cs` (3b) | none | unit 85 | the tenant round-trip and the generation re-read are unit-tested. |
| `Security.ABAC/Persistence/TenantScopedPolicyStore.cs` (new, 7a) | new | unit 98, guard 90 | pure delegation with tenant resolution. |
| `Security.ABAC/Diagnostics/TenantScopedPolicyStoreLog.cs` (new, 7a) | new | unit 90 | generated logging methods. |
| `Security.ABAC/ServiceCollectionExtensions.cs` (7a) | unit 75, guard 60 | unit 75, guard 60 | `ResolvePolicyStore` wrapping is proven by the DI tests; targets kept. |
| `Security.ABAC/Health/ABACHealthCheck.cs` (7b) | unit 94, guard 5 | unit 94, guard 5 | targets kept; the changed count calls are driven by the existing health check tests. |
| `Security.ABAC/Administration/PersistentPolicyAdministrationPoint.cs` (7b) | unit 90, guard 5, integration 45 | unit 90, guard 5, integration 50 | integration rises with the two-tenant tests. |
| the 8 provider stores `PolicyStoreADO/Dapper/EF/Mongo` (4, 5, 6) | none | integration 80 | the integration flag is the only flag that reaches SQL and MongoDB; the stage-refusal and the key are asserted per provider. |
| `EntityFrameworkCore/ABAC/PolicyEntityConfiguration.cs`, `PolicySetEntityConfiguration.cs` (6), `MongoDB/ABAC/PolicyDocument.cs`, `PolicySetDocument.cs` (6) | none | unit 85 | the composite key and the collation are asserted on the model and the document mapping. |
| the 12 SQL scripts (4, 5) | not measured | not measured | executed by the integration tests; scripts carry no coverage flag. |

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
4. ADR in `docs/architecture/adr/`: "Tenant gate as a decorator re-applied by the registration helper" once Design Choice 2 is answered (ADR number assigned when the ADR pull request opens).
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
| `Encina.Security.Audit` | 5013-5015 | inside the registered `SecurityAudit = (5000, 5099)`; 5000-5012 are in use; three messages, 5016 onward unallocated (any later plan takes the next ids after 5015) |
| `Encina.Security.ABAC` | 7400-7402 inside the new `SecurityABACTenancy = (7400, 7419)` | assigned in `eventid-blocks.md`; `SecurityABAC = (9000, 9099)` has 74 of 100 ids in use; register the new range in `EventIdRanges.cs` first and map it to the ABAC assembly in `EncinaEventIdAllocationTests`; ids packed from 7400 |

### Estimated File Count

| Category | Files | Notes |
| :--- | :--- | :--- |
| Production, Security.Audit (gates, errors, meter, log class, registration hook, AddEncinaAudit) | 8 | modified 5, new 3 |
| Production, audit registrations of the 8 provider packages and Marten | 9 | one line each in ServiceCollectionExtensions |
| Production, ABAC core (models, port, PAP, in-memory PAP, caching decorator, message, hosted service, gate, log class, errors, registration) | 14 | modified 11, new 3 |
| Production, ABAC providers (ADO x3, Dapper x3, EF, Mongo, EF configurations x2, Mongo documents x2) | 12 | modified |
| SQL scripts (021 and 022, ADO x3 and Dapper x3) | 12 | modified in place (pre-1.0, no migration) |
| Tests (unit, guard, contract, property, integration, justification .md) | 42 | new and modified, each in the pull request of its code |
| Documentation, manifests, PublicAPI, changelog fragments, ADR | 11 | docs-writer and mechanical-fixer |
| Total | about 108 | twelve pull requests |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Combined prompt — all phases</strong></summary>

```text
PROJECT CONTEXT:
- Encina, .NET 10 / C# 14, pre-1.0, Railway Oriented Programming (Either<EncinaError, T>). Issue #798: tenant isolation for Encina.Security.Audit (IOperationAuditStore, IReadAuditStore) and Encina.Security.ABAC (IPolicyStore, Policy, PolicySet, PAP, cache).
- ITenantContext does not exist. The tenant is IRequestContext.TenantId via IRequestContextAccessor; multi-tenancy is on when MultiTenancyMarker is in the container. Missing tenant context fails closed.
- Prerequisite: ITenantScopeResolver / TenantScope from #1257 (PRs 1257-A1 and A2).

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
- Ambient tenant wins; multi-tenant (marker or ambient tenant) with no tenant is denied; single-tenant passes with the single-tenant scope of #1257 (never hard-code null or default); null tenantId on the policy port means "global rows", never "all"; the policy table key is (TenantId, Id) with the empty string for global.
- No AllTenants switch (2972-D22). No EnableTenantIsolation switch.
- Decorators never swallow a Left; logs carry codes, never tenant ids or EncinaError.Message.
- Edit SQL scripts in place; no migration helpers; no [Obsolete].
- Shared [Collection] fixtures for integration tests; Shouldly via Encina.Testing.Shouldly.

REFERENCE FILES:
- src/Encina.Security.ABAC/DecisionAudit/ABACDecisionAuditReader.cs
- src/Encina.Security.Audit/OperationAuditStoreRegistration.cs
- src/Encina.Compliance.Consent/Services/DefaultConsentService.cs
- src/Encina.Security.ABAC/ServiceCollectionExtensions.cs (ResolvePolicyStore)
- src/Encina.Security.ABAC/DecisionAudit/ABACDecisionAuditEntryMapper.cs
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ✅ | Policy cache partitioned by tenant segment with pattern invalidation for global changes (Phase 7). |
| 2 | OpenTelemetry | ✅ | Denial counters now; tenant tag through the #2676 helper when it lands (ADR-054 D4). |
| 3 | Structured Logging | ✅ | `[LoggerMessage]` ids 5013-5015 and 7400-7402 of the new ABAC range 7400-7419; codes only, never tenant values. |
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

- **Hard**: the `ITenantScopeResolver` primitive (PRs 1257-A1 and A2 of #1257). The issue's "Blocks on #357" is obsolete: #357 was closed as not planned and `ITenantContext` is not coming.
- **Soft**: #2676 (shared tenant telemetry helper), #3271 and #2972 (platform-operator cross-tenant access), #1164 (background tenant restore; the gates read the ambient context that #1164 rebuilds), #1778 (entity-change audit store, out of scope).
- **Conflicts to sequence**: #1636 (`ModuleId`) edits the same audit entry, query and store files; land the pull requests of one plan at a time on those files. #752 and #1257 use the same resolver signature; #752 takes Security.Audit ids from 5016 (the next after this plan's 5015).

---

## Next Steps

1. Maintainer answers Design Choices 2, 3, 6 and 7 when work on the issue starts (recorded in `## Maintainer Decisions`).
2. Split into twelve worker pull requests. Counts include tests, manifest entries, PublicAPI lines, ProjectReference edits and fragments; each is at most 15 files and 1500 lines, builds alone, carries the tests of its own files and leaves `main` buildable (recount by grep when the worker starts; the numbers are estimates). Before PR 1a there is no gate anywhere, so the interval between the audit pull requests is "not yet gated", not a new leak:
   - **PR 1a** (audit gate core): `TenantScopedOperationAuditStore`, `TenantScopedReadAuditStore`, `OperationAuditErrors`, `ReadAuditErrors`, `OperationAuditStoreRegistration`, `ServiceCollectionExtensions`, `OperationAuditMeter`, `TenantScopedAuditLog` (8); unit tests of both gates, DI order test, guard test (4); `Encina.Security.Audit.json` manifest, `PublicAPI.Unshipped.txt` (2): 14 files.
   - **PR 1b** (audit registrations): the 9 provider registration files (8 packages plus Marten) and the existing per-provider registration test files extended for both orders (about 3 test files updated in groups): about 12 files.
   - **PR 2** (ABAC models, no port change): `Policy`, `PolicySet`, `PolicyEntity`, `PolicySetEntity`, `PolicyEntityMapper`, `ABACErrors`, `InMemoryPolicyAdministrationPoint` (7); unit tests for the mapper, errors and in-memory PAP (3); `Encina.Security.ABAC.json`, `PublicAPI.Unshipped.txt` (2): 12 files. The column is not persisted until PRs 4 to 6; a model with a tenant is refused by the stores until then (PR 3a).
   - **PR 3a** (policy port and the staged refusal, builds alone): `IPolicyStore` with the optional trailing `string? tenantId = null`, the 8 store classes, `CachingPolicyStoreDecorator` (bypassing the cache for a non-null tenant) (10); refusal unit tests in 3 files; `PublicAPI.Unshipped.txt` and the manifest (2): 15 files. The callers (`PersistentPolicyAdministrationPoint`, `ABACHealthCheck`) and the ~30 existing test files compile unchanged because the parameter is optional and last; their changes are in PR 7b.
   - **PR 3b** (cache): `CachingPolicyStoreDecorator` tenant keys and generation counter, `PolicyCacheInvalidationMessage`, `PolicyCachePubSubHostedService` (3); decorator unit, decorator contract and hosted service unit tests (3); `PublicAPI.Unshipped.txt`, manifest (2): 8 files.
   - **PR 4** (ADO.NET family): six scripts (`021`, `022` x3), three `PolicyStoreADO`, the three existing store integration test classes extended (two-tenant, ACME against acme, id conflict, duplicate id), three package manifests: 15 files.
   - **PR 5** (Dapper family): the same shape: 15 files.
   - **PR 6** (EF Core and MongoDB family, removing the staged refusal): `PolicyStoreEF`, two EF entity configurations, `PolicyStoreMongo`, `PolicyDocument`, `PolicySetDocument` (6); three EF Core and one MongoDB integration classes (4); two manifests: 12 files.
   - **PR 7a** (ABAC gate): `TenantScopedPolicyStore`, `TenantScopedPolicyStoreLog`, `ServiceCollectionExtensions` (`ResolvePolicyStore`), `EventIdRanges.cs`, the ABAC meter (5); `EncinaEventIdAllocationTests`, gate unit and guard tests (two files), DI test with a warm-cache case (4); `PolicyStoreTenantContractTests` (1); manifest, `PublicAPI.Unshipped.txt` (2): 12 files.
   - **PR 7b** (ABAC administration): `PersistentPolicyAdministrationPoint`, `ABACHealthCheck` (2); tenant cases added to the existing PAP unit test class, `PersistentPolicyAdministrationPointScopeTests`, `ABACHealthCheckTests` and `PersistentPAPContractTests` (4); the FsCheck property test (1); manifest (1): 8 files.
   - **PR 8** (audit two-tenant suite): the eight existing `OperationAuditStore*IntegrationTests` classes and the Marten one extended (9); the `OperationAuditStoreContractTests` and `OperationAuditStorePropertyTests` extensions (2): 11 files.
   - **PR 9** (documentation): ADR, `docs/features/audit-tracking.md`, the ABAC guide, the READMEs of `Encina.Security.Audit` and `Encina.Security.ABAC`, two changelog fragments, the load and benchmark justification `.md` files: 10 files.
3. Follow-up issue file: `artifacts/issues/audit-per-tenant-retention.md` (per-tenant purge of audit entries, Design Choice 6).
4. Link this plan from #798.
