# Implementation Plan: Multi-Tenancy Support for Event Sourcing (`Encina.Marten`)

> **Issue**: [#326](https://github.com/dlrivada/Encina/issues/326)
> **Type**: Feature
> **Complexity**: High (5 phases, 5 PRs, one provider, security-sensitive)
> **Estimated Scope**: 62 files and about 5,000 changed lines over 5 PRs (the table in "Estimated File Count" lists every file per PR; the figures assume the recommended answers to the Dilemmas)

---

## Summary

`Encina.Marten` has no tenant isolation. `EventMetadataOptions.CaptureTenantId` (default true) only copies `IRequestContext.TenantId` into the event headers (`EventMetadataEnrichmentService.cs`, `SetHeaderWhen(session, "TenantId", ...)`); `EncinaMartenOptions` has no multi-tenancy member, and the project does not reference `Encina.Tenancy`. Every `IDocumentSession` consumer (`MartenAggregateRepository`, `SnapshotAwareAggregateRepository`, `MartenReadModelRepository`, `Encina.Audit.Marten`, `Encina.Marten.GDPR`) therefore sees all tenants. This plan adds first-class tenancy: **conjoined** (shared tables, partitioned by tenant id), **schema-per-tenant** and **dedicated** (database per tenant), selected with the existing `TenantIsolationStrategy` of `Encina.Tenancy` (`SharedSchema`, `SchemaPerTenant`, `DatabasePerTenant`), driven by `IRequestContext.TenantId`.

**What it implements**

- `EncinaMartenOptions.MultiTenancy` (`MartenMultiTenancyOptions`): `Isolation`, `ConnectionStringResolver`, `SchemaResolver`, `AllowCrossTenantQueries` (default false), `CrossTenantRole`.
- A tenant-aware session factory so that every repository in the DI container gets a session bound to the current tenant, and fails closed with a `Left` when the context has no tenant.
- Per-tenant `IDocumentStore` routing for the dedicated and schema modes (cached, disposed with the host).
- Tenant provisioning and deprovisioning (Dilemma D2) and an explicit, logged cross-tenant read.
- Per-tenant health check, diagnostics and an isolation test: a tenant never reads another tenant's events.

**Standards**: AGENTS.md section 3 (fail closed, ROP, `TimeProvider`), SPEC-002 DEC-006 and DEC-009 (multi-tenancy is required in 1.0), ADR-018, ADR-021, ADR-027.

**Affected packages**: `Encina.Marten` (code, coverage manifest, new project reference to `Encina.Tenancy`, which references only `Encina` and `Encina.Messaging`; `Encina.Marten` has no `PublicAPI.Unshipped.txt` on main, so no PublicAPI lines for it), `Encina.Tenancy` only if D2 chooses option B (it has `PublicAPI.Unshipped.txt` and a manifest, both edited then).

**Provider category**: event sourcing (Marten primary, AGENTS.md section 5); the 10-database rule does not apply, tenancy of the database families is `Encina.Tenancy`'s concern. Integration tests need Marten on PostgreSQL through Testcontainers, with two tenants.

**Boundary with #1257**: #1257 owns the SPEC-002 capabilities' tenant keys and the two-tenant suite across the 10 providers and Marten. This plan delivers the Marten mechanism that suite relies on; this plan's own isolation tests cover the mechanism, #1257 covers the capabilities.

**Size and split (AGENTS.md section 11)**: 62 files exceed one worker PR, so the work ships as five PRs of at most 15 files and 1,500 lines each, counted with every edited file (registry, allocation test, project file, DI, error codes), test, manifest entry and changelog fragment. Each PR adds the manifest entries (per file, per flag, one-sentence justification) of the files it creates, with the tests that meet them; no PR uses a type or log class of a later PR, so the log class, metrics and EventIds are created in PR 2, the first PR that logs. The file lists per PR are in "Estimated File Count".

| PR | Content | Files | Lines (estimate) |
|----|---------|-------|------------------|
| 1 | Phase 1: options, project reference, error codes, spike test, options tests | 8 | about 600 |
| 2 | Phase 2 and the diagnostics of Phase 4: conjoined mode, tenant session factory, DI, log, metrics, EventIds, tests | 15 | about 1,300 |
| 3 | Phase 3: dedicated and schema routing, resolver, isolation property test, load test | 14 | about 1,300 |
| 4 | Provisioning and deprovisioning (Phase 4) | 11 | about 1,000 |
| 5 | Cross-tenant scope, health check (Phase 4) and documentation (Phase 5) | 14 | about 1,100 |

PR 2 and PR 3 start only after the maintainer has answered D1 and D3 (and the PR 1 spike has been read): they implement whichever mode set the answers settle.

---

## Design Choices

<details>
<summary><strong>1. Who enforces row isolation in conjoined mode (issue Dilemma D1)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| A. Marten's native conjoined tenancy (`Events.TenancyStyle = Conjoined`, documents multi-tenanted by policy, sessions opened for a tenant) | Enforced below Encina, in the event store's own SQL | Ties behavior to Marten's feature set |
| B. An Encina-side tenant filter in the repositories | Independent of the store | A missed filter leaks data; reimplements what Marten does |

### Chosen Option: **A — Marten native conjoined tenancy** (recommended, pending the maintainer)

### Rationale

Marten is the only event store (ADR-027) and native tenancy cannot be bypassed by a forgotten `WHERE`. B wins when a second event store with no native tenancy is planned. Phase 1 confirms the exact Marten 9.38 API (tenancy style, tenant-bound sessions) in a spike test before code depends on it.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Where tenant provisioning lives (issue Dilemma D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| A. A provisioner inside `Encina.Marten` (the issue's `ITenantManager`; named `IMartenTenantProvisioner` here to say what it does) | Self-contained; `ITenantStore` (`GetTenantAsync`, `GetAllTenantsAsync`, `ExistsAsync`) stays read-only | A second place that knows about tenant lifecycle |
| B. Extend `ITenantStore` of `Encina.Tenancy` with provisioning | One lifecycle across packages | Tenancy gains an event-store concern and every `ITenantStore` implementation (`InMemoryTenantStore`, user stores) changes |

### Chosen Option: **A — provisioner in `Encina.Marten`** (recommended, pending the maintainer)

### Rationale

Provisioning an event-store tenant means creating a schema or a database, which only Marten code can do. B wins when the same tenants must be provisioned in several stores by one call.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Schema-per-tenant has no native Marten mode (new Dilemma D3)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| A. One cached `IDocumentStore` per tenant with `DatabaseSchemaName` set to `SchemaResolver(tenantId)` | The mode in the issue works | More code; many tenants mean many stores (memory, startup cost); needs DDL rights |
| B. Ship conjoined and dedicated first; schema-per-tenant as a later issue | Smaller, uses only native Marten modes | The issue's acceptance "Schema-per-tenant mode works" is not met |

### Chosen Option: **A — store per tenant schema** (recommended, pending the maintainer)

### Rationale

To my reading Marten offers conjoined tenancy and database-per-tenant but not schema-per-tenant; Phase 1's spike confirms this against 9.38 before anything is built. A wins because the issue and `TenantIsolationStrategy.SchemaPerTenant` promise the mode. B wins when the maintainer prefers to avoid a large number of stores in one process.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Reuse the Tenancy abstractions, no second isolation enum</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| A. Reuse `TenantIsolationStrategy`, `ITenantStore`, `TenantInfo` from `Encina.Tenancy` | One concept; `TenantInfo.HasDedicatedDatabase`/`HasDedicatedSchema` already map to the modes | `Encina.Marten` gets a project reference |
| B. A `MultiTenancyMode` enum in `Encina.Marten` (the original proposal) | No reference | Duplicates the three values |

### Chosen Option: **A — reuse** (recommended, pending the maintainer)

### Rationale

`Encina.Tenancy` already depends only on `Encina` and `Encina.Messaging` (`src/Encina.Tenancy/Encina.Tenancy.csproj`), so there is no cycle, and a second enum would drift from the first. Option B satisfies the same rules and avoids forcing the Tenancy reference on every Marten user, which weighs against pay-for-what-you-use (AGENTS.md section 1); A wins because the tenant lookup (`ITenantStore`, `TenantInfo`) is needed anyway for the dedicated and schema modes.

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Missing tenant and cross-tenant access fail closed</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| A. No tenant in `IRequestContext` yields `Left` (`marten.tenant_required`); cross-tenant reads need `AllowCrossTenantQueries` and an explicit call, logged | Fail closed; the only opt-out is explicit and logged | Background jobs without a tenant must set one |
| B. Missing tenant falls back to a default tenant or all tenants | Convenient | A silent leak path |

### Chosen Option: **A — fail closed**

### Rationale

Background daemons and projections run without a request context; they must bind a tenant explicitly (tenant-aware background cycles are tracked by #1257, open). Option B is a silent leak path and fails the rule.

**Class:** Settled
**Settled by:** AGENTS.md section 3 (compliance and security gates fail closed), SPEC-002 DEC-006

</details>

<details>
<summary><strong>6. How a missing tenant surfaces from a session factory</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| A. The tenant-bound session factory throws a dedicated exception when the context has no tenant, and Encina's repositories (which call `IDocumentSession` through DI) map it to `Left` `marten.tenant_required` at their boundary | Works with Marten's `ISessionFactory`, whose `OpenSession` returns a session, not an `Either` | An exception crosses DI resolution when a session is injected directly into user code |
| B. A separate `Either`-returning `ITenantSessionProvider.OpenAsync` used by Encina repositories, with `IDocumentSession` registered scoped over it | Railway style end to end | A second way to get a session; user code that injects `IDocumentSession` gets the exception anyway |

### Chosen Option: **A** (recommended, pending the maintainer)

### Rationale

Marten's session factory has no way to return an error value, so one of the two layers must throw; ADR-001 and ADR-053 apply to business results, and a missing tenant on an injected session is a gate that must deny (AGENTS.md section 3). The spike of PR 1 confirms the exact Marten 9.38 `ISessionFactory` contract before PR 2.

**Class:** Dilemma

</details>

<details>
<summary><strong>7. Single global isolation mode, and a tenant whose strategy disagrees</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| A. `MultiTenancy.Isolation` is the one mode of the store; a tenant whose `TenantInfo` strategy differs gets `Left` `marten.tenant_strategy_mismatch` (category `Conflict`, ADR 062 rule R2) | One mode to reason about and test | A deployment cannot mix a premium dedicated tenant with shared ones |
| B. The mode is per tenant, read from `TenantInfo` (`HasDedicatedDatabase`, `HasDedicatedSchema`) | Mixed deployments | Every repository path must handle all three modes at once; the isolation test matrix multiplies |

### Chosen Option: **A** (recommended, pending the maintainer)

### Rationale

The issue has one `Isolation` option; mixed modes are a larger feature that #1257 (the two-tenant suite on Marten) would have to cover too, which makes the choice one for the maintainer.

**Class:** Dilemma

</details>

<details>
<summary><strong>8. Is <code>TenancyOptions.RequireTenant = false</code> honored?</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| A. No: a missing tenant is always a `Left`; the only way to see several tenants is the explicit cross-tenant scope (decision 11) | Fails closed with no global switch | An application that wants an optional tenant must bind a default tenant itself |
| B. Yes: with `RequireTenant = false` a missing tenant opens an unscoped session | Same semantics as `Encina.Tenancy` | A one-line configuration disables isolation for every event stream |

### Chosen Option: **A** (recommended, pending the maintainer)

### Rationale

`RequireTenant` defaults to true in `src/Encina.Tenancy/Configuration/TenancyOptions.cs`; honoring `false` would be an opt-out that is neither explicit per call nor logged per use, which AGENTS.md section 3 allows only as an explicit logged opt-out, so the maintainer decides whether a global flag counts.

**Class:** Dilemma

</details>

<details>
<summary><strong>9. Bound and eviction of the per-tenant store cache</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| A. `MaxCachedTenantStores` (default 100); when full, the least recently used idle store is disposed; stores with open sessions are never evicted | Bounded memory; no live store is disposed | A burst of more than the limit of busy tenants exceeds it until sessions close |
| B. When full, return `Left` `marten.tenant_store_limit_reached` for a new tenant | Hard bound | A busy deployment rejects tenants |

### Chosen Option: **A** (recommended, pending the maintainer)

### Rationale

The default is a starting point with no measurement behind it (the load test of PR 3 measures it); disposing a store with an open session would break a live request, so eviction only touches idle stores. B wins when memory must never exceed the bound.

**Class:** Dilemma

</details>

<details>
<summary><strong>10. Validation of tenant identifiers and resolver output (DDL and connection strings)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| A. A schema name produced by `SchemaResolver` must match `^[a-z][a-z0-9_]{0,62}$` (PostgreSQL identifier limit), is rejected with `Left` `marten.tenant_provisioning_failed` otherwise, and is always quoted as an identifier in DDL; a connection string comes only from `ConnectionStringResolver`, is never built from the tenant id by the library, and is never logged | Fails visibly; no injection through resolver output or tenant ids | A tenant id with unusual characters needs a resolver that maps it |
| B. Hash the tenant id into the schema name | Any tenant id works | Unreadable schema names for operators |

### Chosen Option: **A** (recommended, pending the maintainer)

### Rationale

DDL (create schema, create database) is built from this value in provisioning (decision 2) and in the schema-per-tenant store (D3); no unvalidated value reaches a statement under either option. In conjoined mode the tenant id is a Marten parameter, not DDL.

**Class:** Dilemma

</details>

<details>
<summary><strong>11. Deprovisioning semantics</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| A. `DeprovisionTenantAsync(tenantId, purge, ct)`: by default the tenant is marked deprovisioned (new sessions refused) and the data is kept; `purge: true` drops the schema or database or deletes the tenant's conjoined rows | Reversible by default; erasure on request is explicit | Storage until purged |
| B. Deprovisioning always drops the data | Simple | Irreversible by one call |

### Chosen Option: **A** (recommended, pending the maintainer)

### Rationale

Retention and erasure duties differ per tenant and per data (SPEC-002), so the library does not choose a retention period; the call is idempotent either way.

**Class:** Dilemma

</details>

<details>
<summary><strong>12. Shape of the cross-tenant scope, and background work without a tenant</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| A. A separate read-only `ICrossTenantReader` that opens a session over all tenants only when `AllowCrossTenantQueries` is true and the caller holds `CrossTenantRole`; every call is logged and counted; it never changes tenant-bound repositories | Tenant-bound repositories cannot be widened by a flag; the grant is visible in code | A second read API |
| B. A flag on the repositories that widens the session | One API | A widened repository can write across tenants |

### Chosen Option: **A** (recommended, pending the maintainer)

### Rationale

A background daemon or projection without a request context must bind a tenant explicitly (decision 5, #1257) or use the reader; the authorization check and the logging make the opt-out explicit, as AGENTS.md section 3 requires.

**Class:** Dilemma

</details>

<details>
<summary><strong>13. New error codes and their categories</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| A. `marten.tenant_required` (`InvalidRequest`), `marten.cross_tenant_denied` (`Forbidden`), `marten.tenant_provisioning_failed` (`Internal`), `marten.tenant_strategy_mismatch` (`Conflict`), all created with the category overload of `EncinaErrors.Create` | ADR 062 (merged in PR #3599) already decides that the `*_tenant_required` codes are `InvalidRequest` and that denials are `Forbidden` (rule R6); the `marten.<snake_case>` shape follows `MartenErrorCodes` | Four constants |
| B. The same codes without a category | One argument fewer | Category inferred from the code text, the path ADR 062 retires |

### Chosen Option: **A**

### Rationale

The categories follow ADR 062; the names follow the existing constants in `src/Encina.Marten/MartenErrorCodes.cs`.

**Class:** Trivial
**Settled by:** ADR 062 (decided table for `*_tenant_required` and rule R6) and `src/Encina.Marten/MartenErrorCodes.cs`

</details>

---

## Implementation Phases

### Phase 1: Options, Spike and Abstractions (PR 1, 8 files)

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.Marten/Tenancy/MartenMultiTenancyOptions.cs` (new) and `EncinaMartenOptions.MultiTenancy` (edit); validation (a resolver is required for the dedicated and schema modes); a `ToString()` that never prints connection strings, and `[JsonIgnore]` on resolvers that carry them (AGENTS.md section 3, #851).
2. Project reference `Encina.Marten` to `Encina.Tenancy` (edit of `src/Encina.Marten/Encina.Marten.csproj`; subject to D4).
3. Error codes in `MartenErrorCodes` (edit): the four codes of decision 13 with the category overload of `EncinaErrors.Create` (ADR 062).
4. Spike integration test (decides D1, D3 and the session-factory contract of decision 6): conjoined events with two tenant ids on a real store, and the available Marten 9.38 API for tenant-bound sessions and per-tenant databases and schemas.
5. Tests in the same PR: options unit (validation, `ToString`), options guard (`tests/Encina.GuardTests`: null resolvers where checked), the spike. Manifest entries for the new and edited files in `.github/coverage-manifest/Encina.Marten.json` with the tests that meet them (`mechanical-fixer`). No PublicAPI lines (no file for `Encina.Marten`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT: Encina.Marten has no tenancy; Encina.Tenancy has TenantIsolationStrategy, ITenantStore, TenantInfo. Issue #326.
TASK: Add MartenMultiTenancyOptions, the project reference, the error codes and a spike test that proves which isolation modes Marten 9.38 supports natively.
KEY RULES: options with connection strings override ToString() and use [JsonIgnore]; no secrets in logs; validation at registration; new error codes use the ErrorCategory overload (ADR 062); XML docs with examples; no PublicAPI file exists for Encina.Marten; guard tests for validated parameters.
REFERENCE FILES: src/Encina.Marten/EncinaMartenOptions.cs, src/Encina.Tenancy/Configuration/TenancyOptions.cs, src/Encina.Tenancy/Abstractions/TenantIsolationStrategy.cs, src/Encina.Marten/MartenErrorCodes.cs
```

</details>

### Phase 2: Conjoined Mode, the Tenant Guard and Diagnostics (PR 2, 15 files, blocked until D1 is answered)

<details>
<summary><strong>Tasks</strong></summary>

1. `MartenTenancyConfigurator` (`IConfigureOptions<StoreOptions>`, registered like `ConfigureMartenEventMetadata`) that sets the tenancy style and multi-tenants documents when `Isolation` is `SharedSchema`.
2. `TenantSessionFactory` (Marten `ISessionFactory`) reading `IRequestContext.TenantId`; when absent it fails closed (`marten.tenant_required`, surfaced as decision 6 says) instead of opening an unscoped session; `RequireTenant = false` does not change that (decision 8).
3. `AddEncinaMarten` registers the factory only when `MultiTenancy.Isolation` is configured (`TryAdd*`, never overriding the application's own `ISessionFactory`); DI test with `ValidateOnBuild` and `ValidateScopes`.
4. Diagnostics, created here because the factory is the first logger: `MartenTenancyLog`, `MartenTenancyMetrics`, `MartenTenancyActivitySource`; register `MartenTenancy = (7280, 7289)` in `src/Encina/Diagnostics/EventIdRanges.cs` and add it to the `["Encina.Marten"]` list in `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` (Phase 4 lists the ids).
5. Tests in the same PR: configurator unit, factory unit, DI test (new file), factory guard, diagnostics unit, and the integration test with two tenants on PostgreSQL (tenant A never reads tenant B's events, aggregates or read models); manifest entries for every new file.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT: Phase 1 options and spike exist. EncinaStoreOptionsConfigurator bridges IConfigureOptions<StoreOptions> into Marten; MartenAggregateRepository receives IDocumentSession and IRequestContext by constructor.
TASK: Implement conjoined tenancy through the store options and the tenant-bound session factory; a missing tenant returns Left, never an unscoped session.
KEY RULES: fail closed; TryAdd* registration that never overrides the application's own; tests execute real code; the two-tenant test uses the shared Marten PostgreSQL fixture.
REFERENCE FILES: src/Encina.Marten/EncinaStoreOptionsConfigurator.cs, src/Encina.Marten/ConfigureMartenEventMetadata.cs, src/Encina.Marten/ServiceCollectionExtensions.cs
```

</details>

### Phase 3: Dedicated and Schema-per-Tenant Routing (PR 3, 15 files, blocked until D1 and D3 are answered)

<details>
<summary><strong>Tasks</strong></summary>

1. `ITenantStoreResolver` / `TenantDocumentStoreResolver`: builds and caches one `IDocumentStore` per tenant from `ConnectionStringResolver` (dedicated) or `SchemaResolver` (schema, D3), disposes them with the host; the cache bound `MaxCachedTenantStores` (an edit of `MartenMultiTenancyOptions`) and the eviction follow decision 9; resolver output is validated per decision 10.
2. The session factory routes through the resolver for these modes (edit of `TenantSessionFactory`; `ServiceCollectionExtensions` registers the resolver; new `[LoggerMessage]` entries in `MartenTenancyLog`).
3. Tenant data comes from `ITenantStore.GetTenantAsync` (`TenantInfo` carries the strategy); an unknown tenant is a `Left`; a strategy that differs from `MultiTenancy.Isolation` is `marten.tenant_strategy_mismatch` (decision 7).
4. Tests in the same PR: resolver unit (cache, bound, disposal), resolver guard, routing and isolation per mode (integration, two tenants, the conjoined case already shipped in PR 2), the isolation property test (FsCheck: for any two tenants, the events one writes are never returned to the other), the DI test (edit), a load test with many tenants (connection routing; the load-test justification, if one is needed, lives in this PR), and the benchmark `.md` justification (routing is cached, not a hot path; allowed for benchmarks, never for unit, guard or contract tests). Manifest entries for the new files.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT: The conjoined path and the guard are merged. Marten has no schema-per-tenant mode; the plan builds one store per tenant schema (Dilemma D3, per the maintainer's answer).
TASK: Implement the per-tenant store resolver and route the session factory through it for DatabasePerTenant and SchemaPerTenant.
KEY RULES: async with CancellationToken; no connection string in any log or exception; dispose stores; unknown tenant is a Left; bounded cache.
REFERENCE FILES: src/Encina.Tenancy/Data/ITenantConnectionFactory.cs, src/Encina.Tenancy/Abstractions/TenantInfo.cs, src/Encina.Marten/Tenancy
```

</details>

### Phase 4: Provisioning (PR 4, 11 files), Cross-Tenant Access and Health (PR 5), Observability (created in PR 2)

<details>
<summary><strong>Tasks</strong></summary>

1. PR 4: `IMartenTenantProvisioner` (`ProvisionTenantAsync`, `DeprovisionTenantAsync(tenantId, purge, ct)` per decision 11, `GetStatisticsAsync`, all `Either`), idempotent provisioning; DDL through the tenant store with the names validated per decision 10; `TenantStatistics`; new log entries and the DI registration (edits); tests: provisioner unit, guard, contract (real provisioner), integration (provision, deprovision, purge), DI test (edit); manifest entries.
2. PR 5: cross-tenant read per decision 12: `ICrossTenantReader` and its implementation, requiring `AllowCrossTenantQueries`, logged and counted; the caller authorizes with `[RequireRole]` and `CrossTenantRole`.
3. PR 5: `MartenTenantHealthCheck` per tenant, building on `Encina.Tenancy`'s `TenantHealthCheck` (`DefaultName` const, static `Tags`).
4. PR 2 creates the diagnostics; the ids are packed sequentially from 7280 in the order the PRs add them: 7280 tenant required (PR 2), 7281 tenant bound (PR 2), 7282 store created (PR 3), 7283 store evicted (PR 3), 7284 strategy mismatch (PR 3), 7285 tenant provisioned (PR 4), 7286 tenant deprovisioned (PR 4), 7287 provisioning failed (PR 4), 7288 cross-tenant read (PR 5), 7289 cross-tenant denied (PR 5); the whole range 7280-7289 is registered in PR 2 and lies inside the block assigned to this family in `artifacts/decisions/plan-batch/eventid-blocks.md`. Counter `encina.marten.tenancy.cross_tenant_reads`, span `encina.marten.tenancy.resolve` (`tenant.isolation`); tenant ids, schema names and connection strings are never tags or log arguments.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT: Routing works for all three modes. TenancyActivitySource, TenancyMetrics and TenantHealthCheck in Encina.Tenancy are the patterns. The range 7280-7289 is assigned to #326 from the family block 7250-7299.
TASK: Add the provisioner, the explicit cross-tenant scope, the health check and the diagnostics; register the EventId range first.
KEY RULES: the cross-tenant opt-in is explicit and logged; never log tenant connection strings or EncinaError.Message; sequential EventIds; health check has DefaultName const and static Tags.
REFERENCE FILES: src/Encina.Tenancy/Health/TenantHealthCheck.cs, src/Encina.Tenancy/Diagnostics/TenancyMetrics.cs, src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

### Phase 5: Testing, Documentation and Finalization (tests in PRs 1 to 5, documentation in PR 5)

<details>
<summary><strong>Tasks</strong></summary>

1. Each PR carries its own tests and per-file manifest targets with justifications (`.github/coverage-manifest/Encina.Marten.json`, `mechanical-fixer`), as listed in Phases 1 to 4: the isolation property test ships in PR 3, the provisioner contract and integration tests in PR 4, the cross-tenant denial integration test in PR 5. Phase 5 adds none.
2. PR 5 (4 documentation files), `docs-writer`: multi-tenancy guide with a decision table of the three modes, ADR for D1 to D4 and decisions 6 to 12 (ADR number assigned when the ADR pull request opens), `docs/INVENTORY.md`, changelog fragment `changelog.d/326-marten-multi-tenancy.added.md`. `Encina.Marten` has no package README on main.
3. Verification in every PR: zero warnings, every flag at target, CRAP of changed methods at most 10.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT: All modes, provisioning, diagnostics exist; coverage is per flag against .github/coverage-manifest/Encina.Marten.json.
TASK: PR 5 only: write the guide, the ADR, the INVENTORY entry and the fragment (docs-writer and mechanical-fixer); tests and manifest targets shipped with PRs 1 to 5.
KEY RULES: shared fixtures only; FsCheck through Encina.Testing.FsCheck; Shouldly through Encina.Testing.Shouldly; docs cite coverage with covref markers.
REFERENCE FILES: tests/Encina.IntegrationTests/Infrastructure/Marten, tests/Encina.PropertyTests, docs/testing/coverage-measurement-methodology.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Relevance |
|--------|-----------|
| SPEC-002 DEC-006, DEC-009 | Fail closed; multi-tenancy is required in 1.0 |
| Marten multi-tenancy documentation | Conjoined tenancy and database-per-tenant; confirmed for 9.38 in the Phase 1 spike |
| ADR-027 | Marten as the event-sourcing provider |
| AGENTS.md sections 3 and 6 | Fail closed, secrets in options, function 10 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|----------------------|
| `TenantIsolationStrategy`, `TenantInfo`, `ITenantStore` | `src/Encina.Tenancy/Abstractions` | Isolation modes and tenant lookup |
| `IRequestContext.TenantId` | `src/Encina/Abstractions/IRequestContext.cs` | The tenant source for every session |
| `EventMetadataOptions.CaptureTenantId` | `src/Encina.Marten/EventMetadataOptions.cs` | Stays as metadata; isolation is separate |
| `EncinaStoreOptionsConfigurator` | `src/Encina.Marten/EncinaStoreOptionsConfigurator.cs` | Carries the store-options configuration |
| `TenantHealthCheck`, `TenancyActivitySource`, `TenancyMetrics` | `src/Encina.Tenancy` | Patterns for the per-tenant health and diagnostics |
| `Encina.Audit.Marten`, `Encina.Marten.GDPR` stores | `src/Encina.Audit.Marten`, `src/Encina.Marten.GDPR` | Take `IDocumentSession` from DI, so they become tenant-scoped through the factory |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Marten` (new range `MartenTenancy`) | 7280-7289 (all ten used, listed in Phase 4) | Inside the block 7250-7299 assigned to the family in the batch registry (`artifacts/decisions/plan-batch/eventid-blocks.md`), which records the sub-ranges of the sibling plans |

### Estimated File Count

Every added or edited file is counted. `Encina.Marten` has no PublicAPI file; `Encina.Tenancy` is untouched unless D2 = B. No existing signature changes, so there are no existing callers to update.

| PR | Category | Files | Notes |
|----|----------|-------|-------|
| 1 | Source | 4 | `MartenMultiTenancyOptions`, `EncinaMartenOptions` edit, `Encina.Marten.csproj` edit, `MartenErrorCodes` edit |
| 1 | Tests | 3 | Options unit, options guard, spike (integration) |
| 1 | Manifest | 1 | `Encina.Marten.json` |
| 1 | **Subtotal** | **8** | |
| 2 | Source | 7 | `TenantSessionFactory`, `MartenTenancyConfigurator`, `ServiceCollectionExtensions` edit, `MartenTenancyLog`, `MartenTenancyMetrics`, `MartenTenancyActivitySource`, `EventIdRanges.cs` edit |
| 2 | Tests | 7 | Configurator unit, factory unit, DI (new), factory guard, diagnostics unit, two-tenant integration, `EncinaEventIdAllocationTests.cs` edit |
| 2 | Manifest | 1 | `Encina.Marten.json` |
| 2 | **Subtotal** | **15** | |
| 3 | Source | 6 | `ITenantStoreResolver`, `TenantDocumentStoreResolver`, `MartenMultiTenancyOptions` edit, `TenantSessionFactory` edit, `ServiceCollectionExtensions` edit, `MartenTenancyLog` edit |
| 3 | Tests | 7 | Resolver unit, resolver guard, routing integration, isolation property, load, DI test edit, benchmark `.md` justification |
| 3 | Manifest | 1 | `Encina.Marten.json` |
| 3 | **Subtotal** | **14** | |
| 4 | Source | 5 | `IMartenTenantProvisioner`, `MartenTenantProvisioner`, `TenantStatistics`, `MartenTenancyLog` edit, `ServiceCollectionExtensions` edit |
| 4 | Tests | 5 | Provisioner unit, guard, contract, integration, DI test edit |
| 4 | Manifest | 1 | `Encina.Marten.json` |
| 4 | **Subtotal** | **11** | |
| 5 | Source | 5 | `ICrossTenantReader`, `CrossTenantReader`, `MartenTenantHealthCheck`, `MartenTenancyLog` edit, `ServiceCollectionExtensions` edit |
| 5 | Tests | 4 | Cross-tenant unit, health unit, guard (reader and health check), integration (cross-tenant denial) |
| 5 | Manifest | 1 | `Encina.Marten.json` |
| 5 | Docs | 4 | Guide, ADR, `docs/INVENTORY.md`, changelog fragment |
| 5 | **Subtotal** | **14** | |
| | **Total** | **62** | 8 + 15 + 14 + 11 + 14; each PR at most 15 files and 1,500 lines |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
PROJECT CONTEXT: Encina, pre-1.0, .NET 10 / C# 14. Issue #326 adds multi-tenancy for event sourcing to Encina.Marten using the Encina.Tenancy abstractions. Follow AGENTS.md: fail closed, ROP, secrets never in logs or ToString, TimeProvider, per-flag coverage.

IMPLEMENTATION OVERVIEW: Phase 1 options, reference, error codes and a Marten-API spike (PR 1). Phase 2 conjoined mode through store options and a tenant-bound session factory, with the EventId range 7280-7289 and diagnostics (PR 2). Phase 3 per-tenant store resolver for dedicated and schema modes (PR 3). Phase 4 provisioning (PR 4), explicit logged cross-tenant reads and health check (PR 5). Phase 5 docs (PR 5). Each PR carries its own tests and manifest targets. Dilemmas D1 (enforcement), D2 (provisioning home), D3 (schema mode), D4 (reuse of Tenancy) and decisions 6 to 12 follow the maintainer's answers; the spike can send D1 and D3 back to the maintainer.

KEY PATTERNS: tenant from IRequestContext.TenantId; Left when missing; TryAdd* registration with a ValidateOnBuild test; sequential EventIds registered first; shared integration fixtures with two tenants.

REFERENCE FILES: src/Encina.Marten/EncinaMartenOptions.cs, src/Encina.Marten/EncinaStoreOptionsConfigurator.cs, src/Encina.Marten/ServiceCollectionExtensions.cs, src/Encina.Tenancy/Abstractions/TenantIsolationStrategy.cs, src/Encina/Diagnostics/EventIdRanges.cs, docs/plans/dsr-implementation-plan-404.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | The per-tenant store cache covers the hot path; an `ICacheProvider` layer over tenant configuration has no measured need |
| 2 | OpenTelemetry | ✅ | `encina.marten.tenancy.resolve` span and cross-tenant counter (Phases 2 and 4) |
| 3 | Structured Logging | ✅ | `MartenTenancyLog`, EventIds 7280-7289; the cross-tenant opt-in is logged (Phases 2 to 4) |
| 4 | Health Checks | ✅ | `MartenTenantHealthCheck` per tenant (Phase 4, PR 5) |
| 5 | Validation | ✅ | Options, tenant identifiers and resolver output validated (decision 10, Phases 1 and 4) |
| 6 | Resilience | ❌ | A failure to reach a tenant database is a `Left` logged by error code and the caller retries; a retry policy on the store resolver would hide a tenant-specific outage |
| 7 | Distributed Locks | ❌ | Provisioning is idempotent (create if missing, ignore if present), so concurrent provisioning converges without a lock |
| 8 | Transactions | ✅ | Each tenant session is its own unit of work; no cross-tenant transaction exists (Phases 2 and 3) |
| 9 | Idempotency | ✅ | Provisioning and deprovisioning are idempotent (Phase 4) |
| 10 | Multi-Tenancy | ✅ | This is the feature |
| 11 | Module Isolation | ❌ | Marten has no module-scoped streams today, so tenant filtering has no module dimension |
| 12 | Audit Trail | ⏭️ | Provisioning, deprovisioning and cross-tenant reads are compliance relevant: follow-up issue to be opened (draft: artifacts/decisions/plan-batch/followups/marten-tenancy-audit.md); cross-tenant reads are already logged and counted in Phase 4 |

---

## Prerequisites & Dependencies

- #346 (Multi-Tenancy Support, open) is the umbrella; #357 delivered `Encina.Tenancy`.
- #1257 consumes this mechanism for the SPEC-002 two-tenant suite on Marten; it should be briefed after PR 1 of this plan merges.
- Siblings #323 (per-tenant snapshot policies) and #324 (per-tenant rebuild) wait for this plan.

---

## Next Steps

1. The maintainer answers the Dilemmas (D1 to D4 and decisions 6 to 12) when work on #326 starts; the answers go into `## Maintainer Decisions`.
2. Run the Phase 1 spike first; if Marten 9.38 supports more or less than assumed, update the choices before PR 2 starts.
3. Write the PR briefs in order and brief #1257 after PR 2; open the audit follow-up from its draft.
