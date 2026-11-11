# Implementation Plan: Multi-Tenancy Support for Event Sourcing (`Encina.Marten`)

> **Issue**: [#326](https://github.com/dlrivada/Encina/issues/326)
> **Type**: Feature
> **Complexity**: High (5 phases, 3 PRs, one provider, security-sensitive)
> **Estimated Scope**: about 36 files, about 3,000 changed lines over 3 PRs

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

**Affected packages**: `Encina.Marten` (code, new project reference to `Encina.Tenancy`, which references only `Encina` and `Encina.Messaging`), `Encina.Tenancy` only if D2 chooses option B; `PublicAPI.Unshipped.txt` and the coverage manifests of both.

**Provider category**: event sourcing (Marten primary, AGENTS.md section 5); the 10-database rule does not apply, tenancy of the database families is `Encina.Tenancy`'s concern. Integration tests need Marten on PostgreSQL through Testcontainers, with two tenants.

**Boundary with #1257**: #1257 owns the SPEC-002 capabilities' tenant keys and the two-tenant suite across the 10 providers and Marten. This plan delivers the Marten mechanism that suite relies on; this plan's own isolation tests cover the mechanism, #1257 covers the capabilities.

**Size and split (AGENTS.md section 11)**: about 36 files exceed one worker PR. PR 1: Phases 1 and 2 (options, guard, conjoined mode, about 14 files). PR 2: Phase 3 (dedicated and schema routing, about 12 files). PR 3: Phases 4 and 5 (provisioning, cross-tenant, health check, diagnostics, docs, about 12 files).

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

### Chosen Option: **A — reuse**

### Rationale

`Encina.Tenancy` already depends only on `Encina` and `Encina.Messaging`, so there is no cycle; a second enum would drift from the first.

**Class:** Settled
**Settled by:** AGENTS.md section 3 (clean architecture, no duplicate concepts), `src/Encina.Tenancy/Abstractions/TenantIsolationStrategy.cs`

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

Background daemons and projections run without a request context; they must bind a tenant explicitly (tenant-aware background cycles are tracked by #1257). Whether `TenancyOptions.RequireTenant` (default true in `Encina.Tenancy`) is honored here follows that option.

**Class:** Settled
**Settled by:** AGENTS.md section 3 (compliance and security gates fail closed), SPEC-002 DEC-006

</details>

---

## Implementation Phases

### Phase 1: Options, Spike and Abstractions

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.Marten/Tenancy/MartenMultiTenancyOptions.cs` and `EncinaMartenOptions.MultiTenancy`; validation (a resolver is required for the dedicated and schema modes); a `ToString()` that never prints connection strings, and `[JsonIgnore]` on resolvers that carry them.
2. Project reference `Encina.Marten` to `Encina.Tenancy`.
3. Error codes in `MartenErrorCodes`: `marten.tenant_required`, `marten.cross_tenant_denied`, `marten.tenant_provisioning_failed`.
4. Spike integration test (decides D1 and D3): conjoined events with two tenant ids on a real store, and the available Marten 9.38 API for tenant-bound sessions and per-tenant databases and schemas.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT: Encina.Marten has no tenancy; Encina.Tenancy has TenantIsolationStrategy, ITenantStore, TenantInfo. Issue #326.
TASK: Add MartenMultiTenancyOptions, the project reference, the error codes and a spike test that proves which isolation modes Marten 9.38 supports natively.
KEY RULES: options with connection strings override ToString() and use [JsonIgnore]; no secrets in logs; validation at registration; XML docs with examples; PublicAPI.Unshipped.txt.
REFERENCE FILES: src/Encina.Marten/EncinaMartenOptions.cs, src/Encina.Tenancy/Configuration/TenancyOptions.cs, src/Encina.Tenancy/Abstractions/TenantIsolationStrategy.cs, src/Encina.Marten/MartenErrorCodes.cs
```

</details>

### Phase 2: Conjoined Mode and the Tenant Guard

<details>
<summary><strong>Tasks</strong></summary>

1. `IConfigureOptions<StoreOptions>` (registered like `ConfigureMartenEventMetadata`) that sets the tenancy style and multi-tenants documents when `Isolation` is `SharedSchema`.
2. `TenantSessionFactory` (Marten `ISessionFactory`) reading `IRequestContext.TenantId`; when absent it fails closed (`marten.tenant_required`) instead of opening an unscoped session.
3. `AddEncinaMarten` registers the factory only when `MultiTenancy.Isolation` is configured (`TryAdd*`); DI test with `ValidateOnBuild` and `ValidateScopes`.
4. Unit and guard tests; integration test with two tenants on PostgreSQL: tenant A never reads tenant B's events, aggregates or read models.

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

### Phase 3: Dedicated and Schema-per-Tenant Routing

<details>
<summary><strong>Tasks</strong></summary>

1. `ITenantStoreResolver` / `TenantDocumentStoreResolver`: builds and caches one `IDocumentStore` per tenant from `ConnectionStringResolver` (dedicated) or `SchemaResolver` (schema, D3), disposes them with the host, bounded by an options limit.
2. The session factory routes through the resolver for these modes.
3. Tenant data comes from `ITenantStore.GetTenantAsync` (`TenantInfo` carries the strategy); an unknown tenant is a `Left`.
4. Tests: resolver cache and disposal (unit), routing and isolation per mode (integration, two tenants), load test with many tenants (connection routing).

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

### Phase 4: Provisioning, Cross-Tenant Access, Health and Observability

<details>
<summary><strong>Tasks</strong></summary>

1. `IMartenTenantProvisioner` (`ProvisionTenantAsync`, `DeprovisionTenantAsync`, `GetStatisticsAsync`, all `Either`), idempotent provisioning; DDL through the tenant store.
2. Cross-tenant read: an explicit `ICrossTenantReader`-style scope that requires `AllowCrossTenantQueries`, is logged and counted; the caller authorizes with `[RequireRole]` and `CrossTenantRole`.
3. `MartenTenantHealthCheck` per tenant, building on `Encina.Tenancy`'s `TenantHealthCheck`.
4. Register `MartenTenancy = (7280, 7289)` in `EventIdRanges.cs`, add it to the `["Encina.Marten"]` list in `EncinaEventIdAllocationTests.cs`; `MartenTenancyLog`, counter `encina.marten.tenancy.cross_tenant_reads`, span `encina.marten.tenancy.resolve` (`tenant.isolation`).

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

### Phase 5: Testing, Documentation and Finalization

<details>
<summary><strong>Tasks</strong></summary>

1. Property test: for any two tenants, the events one writes are never returned to the other (FsCheck over tenant ids and event counts); contract test for the provisioner interface; guard tests.
2. Integration tests for all three modes, provisioning, deprovisioning and the cross-tenant denial. Benchmark: justification `.md` (routing is cached, not a hot path).
3. Per-file targets with justifications in `.github/coverage-manifest/Encina.Marten.json` (`mechanical-fixer`).
4. Documentation (`docs-writer`): multi-tenancy guide with a decision table of the three modes, README, ADR for D1 to D3 (ADR number assigned when the ADR pull request opens), changelog fragment `326-marten-multi-tenancy.added.md`.
5. Verification: zero warnings, every flag at target, CRAP of changed methods at most 10.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT: All modes, provisioning, diagnostics exist; coverage is per flag against .github/coverage-manifest/Encina.Marten.json.
TASK: Write the remaining tests, manifest targets, docs and fragment.
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
| `Encina.Marten` (new range `MartenTenancy`) | 7280-7289 | From the family block 7250-7299: #320 owns 7250-7259, #323 7260-7269, #324 7270-7279, this plan 7280-7289, #535 7290-7299 |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Options, guard, factory | 6 | Options, store-options configurator, session factory, error codes, DI edit, project file |
| Resolver and routing | 3 | Resolver interface, implementation, cache options |
| Provisioning and cross-tenant | 4 | Provisioner, statistics, cross-tenant scope, health check |
| Diagnostics | 2 | Log class, metrics/activity |
| Unit and guard tests | 8 | Options, factory, resolver, provisioner, guards |
| Contract and property tests | 2 | Provisioner contract, isolation property |
| Integration and load tests | 4 | Three modes, provisioning, many-tenant load |
| Docs, changelog, PublicAPI, manifest | 7 | Guide, ADR, fragment, PublicAPI x2, manifest x2 |
| **Total** | **36** | Three PRs |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
PROJECT CONTEXT: Encina, pre-1.0, .NET 10 / C# 14. Issue #326 adds multi-tenancy for event sourcing to Encina.Marten using the Encina.Tenancy abstractions. Follow AGENTS.md: fail closed, ROP, secrets never in logs or ToString, TimeProvider, per-flag coverage.

IMPLEMENTATION OVERVIEW: Phase 1 options, reference, error codes and a Marten-API spike. Phase 2 conjoined mode through store options and a tenant-bound session factory. Phase 3 per-tenant store resolver for dedicated and schema modes. Phase 4 provisioning, explicit logged cross-tenant reads, health check, EventId range 7280-7289, diagnostics. Phase 5 property, integration, docs. Three PRs (Phases 1-2, Phase 3, Phases 4-5). Dilemmas D1 (enforcement), D2 (provisioning home) and D3 (schema mode) follow the maintainer's answers; the spike can send D1 and D3 back to the maintainer.

KEY PATTERNS: tenant from IRequestContext.TenantId; Left when missing; TryAdd* registration with a ValidateOnBuild test; sequential EventIds registered first; shared integration fixtures with two tenants.

REFERENCE FILES: src/Encina.Marten/EncinaMartenOptions.cs, src/Encina.Marten/EncinaStoreOptionsConfigurator.cs, src/Encina.Marten/ServiceCollectionExtensions.cs, src/Encina.Tenancy/Abstractions/TenantIsolationStrategy.cs, src/Encina/Diagnostics/EventIdRanges.cs, docs/plans/dsr-implementation-plan-404.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ⏭️ | Caching tenant configuration and connection strings; the per-tenant store cache covers the hot path, an `ICacheProvider` layer waits for a measured need |
| 2 | OpenTelemetry | ✅ | `encina.marten.tenancy.resolve` span and cross-tenant counter (Phase 4) |
| 3 | Structured Logging | ✅ | `MartenTenancyLog`, EventIds 7280-7289; the cross-tenant opt-in is logged (Phase 4) |
| 4 | Health Checks | ✅ | `MartenTenantHealthCheck` per tenant (Phase 4) |
| 5 | Validation | ✅ | Options and provisioning input validated (Phases 1 and 4) |
| 6 | Resilience | ⏭️ | Retry on tenant database connection failures; a Polly policy is a separate issue |
| 7 | Distributed Locks | ⏭️ | Lock during provisioning; provisioning is idempotent first, a lock is added if concurrent provisioning is observed |
| 8 | Transactions | ✅ | Each tenant session is its own unit of work; no cross-tenant transaction exists (Phases 2 and 3) |
| 9 | Idempotency | ✅ | Provisioning and deprovisioning are idempotent (Phase 4) |
| 10 | Multi-Tenancy | ✅ | This is the feature |
| 11 | Module Isolation | ⏭️ | Module-aware tenant filtering is not designed in Marten today; open an issue when a module needs it |
| 12 | Audit Trail | ⏭️ | Audit of provisioning and deprovisioning needs the operator identity of an admin surface; open an issue with the provisioning API consumers |

---

## Prerequisites & Dependencies

- #346 (Multi-Tenancy Support, open) is the umbrella; #357 delivered `Encina.Tenancy`.
- #1257 consumes this mechanism for the SPEC-002 two-tenant suite on Marten; it should be briefed after PR 1 of this plan merges.
- Siblings #323 (per-tenant snapshot policies) and #324 (per-tenant rebuild) wait for this plan.

---

## Next Steps

1. The maintainer answers D1, D2 and D3 when work on #326 starts; the answers go into `## Maintainer Decisions`.
2. Run the Phase 1 spike first; if Marten 9.38 supports more or less than assumed, update the choices before PR 1 continues.
3. Write the PR briefs in order and brief #1257 after PR 1.
