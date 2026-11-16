# Implementation Plan: Multi-Tenancy Support for Event Sourcing (`Encina.Marten`)

> **Issue**: [#326](https://github.com/dlrivada/Encina/issues/326)
> **Type**: Feature
> **Complexity**: High (5 phases, 8 PRs, one provider, security-sensitive)
> **Estimated Scope**: 106 files and about 6,500 changed lines over 8 PRs (the table in "Estimated File Count" lists every file per PR; the figures assume the recommended answers to the Dilemmas)

---

## Summary

`Encina.Marten` has no tenant isolation. `EventMetadataOptions.CaptureTenantId` (default true) only copies `IRequestContext.TenantId` into the event headers (`EventMetadataEnrichmentService.cs`, `SetHeaderWhen(session, "TenantId", ...)`); `EncinaMartenOptions` has no multi-tenancy member, and the project does not reference `Encina.Tenancy`. Every `IDocumentSession` consumer (`MartenAggregateRepository`, `SnapshotAwareAggregateRepository`, `MartenReadModelRepository`, `Encina.Audit.Marten`, `Encina.Marten.GDPR`) therefore sees all tenants. This plan adds first-class tenancy: **conjoined** (shared tables, partitioned by tenant id), **schema-per-tenant** and **dedicated** (database per tenant), selected with the existing `TenantIsolationStrategy` of `Encina.Tenancy` (`SharedSchema`, `SchemaPerTenant`, `DatabasePerTenant`), driven by `IRequestContext.TenantId`.

**What it implements**

- `EncinaMartenOptions.MultiTenancy` (`MartenMultiTenancyOptions`): `Isolation`, `ConnectionStringResolver`, `SchemaResolver`, `AllowCrossTenantQueries` (default false), `CrossTenantRole`.
- A tenant-aware session accessor (decision 6): the four Marten repositories obtain their session lazily, per operation, from an accessor that returns the session bound to the current tenant and fails closed with a `Left` when the context has no tenant; a tenant-aware Marten session factory opens the sessions.
- Per-tenant `IDocumentStore` routing for the dedicated and schema modes (cached, disposed with the host).
- Tenant provisioning and deprovisioning (Dilemma D2) and an explicit, logged cross-tenant read.
- Per-tenant health check, diagnostics and an isolation test: a tenant never reads another tenant's events.

**Standards**: AGENTS.md section 3 (fail closed, ROP, `TimeProvider`), SPEC-002 DEC-006 and DEC-009 (multi-tenancy is required in 1.0), ADR-018, ADR-021, ADR-027.

**Affected packages**: `Encina.Marten` (code, coverage manifest, new project reference to `Encina.Tenancy`, which references only `Encina` and `Encina.Messaging`; `Encina.Marten` has no `PublicAPI.Unshipped.txt` on main, so no PublicAPI lines for it), `Encina.Tenancy` only if D2 chooses option B (it has `PublicAPI.Unshipped.txt` and a manifest, both edited then).

**Provider category**: event sourcing (Marten primary, AGENTS.md section 5); the 10-database rule does not apply, tenancy of the database families is `Encina.Tenancy`'s concern. Integration tests need Marten on PostgreSQL through Testcontainers, with two tenants.

**Boundary with #1257**: #1257 owns the SPEC-002 capabilities' tenant keys and the two-tenant suite across the 10 providers and Marten. This plan delivers the Marten mechanism that suite relies on; this plan's own isolation tests cover the mechanism, #1257 covers the capabilities.

**Size and split (AGENTS.md section 11)**: 106 files exceed one worker PR, so the work ships as eight PRs of at most 15 files and 1,500 lines each, counted with every edited file (registry, allocation test, project file, DI, error codes, repository constructors and their existing test callers), test, manifest entry and changelog fragment. Each PR adds the manifest entries (per file, per flag, one-sentence justification) of the files it creates, with the tests that meet them; no PR uses a type or log class of a later PR, so the log class and EventIds are created in PR 2, the first PR that logs, and the telemetry in PR 7, the first PR that uses it. The file lists per PR are in "Estimated File Count"; the counts assume the recommended answers to the Dilemmas (decision 6 option A).

| PR | Content | Files | Lines (estimate) |
|----|---------|-------|------------------|
| 1 | Phase 1: options, project reference, error codes, spike test, options tests | 8 | about 600 |
| 2 | Phase 2: session accessor seam, tenant session factory, conjoined configuration, DI, log class and EventIds | 14 | about 1,000 |
| 3 | `EventPublishingPipelineBehavior`, `MartenInlineProjectionDispatcher` and `MartenReadModelRepository` take the accessor; their existing test callers change with the constructors | 14 | about 600 |
| 4 | `MartenAggregateRepository` takes the accessor; its 14 existing test callers change with the constructor | 15 | about 700 |
| 5 | `SnapshotAwareAggregateRepository` and `MartenSnapshotStore` take the accessor; their existing test callers change; the two-tenant conjoined integration test | 12 | about 600 |
| 6 | Phase 3: dedicated and schema routing, resolver, isolation property test, load test | 15 | about 1,300 |
| 7 | Provisioning and deprovisioning, telemetry (Phase 4) | 13 | about 1,100 |
| 8 | Cross-tenant scope, health check (Phase 4) and documentation (Phase 5) | 15 | about 1,100 |
PRs 2 to 6 start only after the maintainer has answered D1 and D3 and decision 6 (and the PR 1 spike has been read): they implement whichever mode set and session design the answers settle. If decision 6 is answered with option B, PRs 3 to 5 disappear and PR 2 has the factory and one exception type instead of the accessor seam (about 64 files over 5 PRs); with B every command still fails while DI builds `EventPublishingPipelineBehavior`, which is resolved for every request.

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
<summary><strong>6. How a missing tenant surfaces: where the session is obtained</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| A. A lazy session-accessor seam: `IMartenSessionAccessor.GetAsync(ct)` returns `Either<EncinaError, IDocumentSession>`; the four repositories (`MartenAggregateRepository`, `SnapshotAwareAggregateRepository`, `MartenReadModelRepository`, `MartenSnapshotStore`) and the two other consumers that DI builds for every command, `EventPublishingPipelineBehavior` (registered open generic and scoped by default while `AutoPublishDomainEvents` is true, `ServiceCollectionExtensions.cs` line 67-70, and resolved by `PipelineBuilder.cs` line 103 before the pipeline runs, so with a plain `IDocumentSession` it would throw for every request) and `MartenInlineProjectionDispatcher` (takes `IDocumentSession` and is injected into the repository constructors, so any registered projection would reproduce the failure), take the accessor in their constructor instead of `IDocumentSession` and call it at the start of each operation, returning `Left` `marten.tenant_required` when the context has no tenant; the accessor returns the scope's own session instance (cached per scope), never a new session, because the pipeline behavior attaches its `CommittedEventCollector` to the session before the handler runs (so it gets the session before `nextStep()`) and must see the commits the repository makes in that same session, and a different session would leave domain events unpublished without any error; a default accessor wraps the plain session when multi-tenancy is off; the inline projection relay is created per call from the obtained session, so `InlineProjectionRelay` does not change | Railway style end to end; fails closed with a `Left` inside the repository, where the failure belongs | Constructor signature changes in six types and about 36 existing test files that construct them (counted on this branch by `new <Type><` in `tests`: 14 for `MartenAggregateRepository`, 6 for `SnapshotAwareAggregateRepository`, 6 for `MartenReadModelRepository`, 5 for `MartenSnapshotStore`, 3 for `EventPublishingPipelineBehavior`, 2 for `MartenInlineProjectionDispatcher`, with overlap; 9 unique files for the snapshot repository and store together), delivered in PRs 3 to 5; other consumers of `IDocumentSession` (projection manager, `CommittedEventCollector`, `Encina.Audit.Marten`, `Encina.Marten.GDPR`) keep the plain session, which fails at resolution when the tenant is missing: follow-up issue to be opened (draft: artifacts/decisions/plan-batch/followups/marten-tenancy-session-consumers.md) |
| B. The tenant-bound session factory throws a dedicated `TenantRequiredException` when the context has no tenant, and nothing else changes | No repository change; the plan shrinks by PRs 3 to 5 | The throw happens while DI builds the repository, and `Encina.Send` resolves the handler (`ResolveHandler` in `src/Encina/Core/Encina.cs`) before the pipeline runs, so no repository, behavior or handler code can turn it into a `Left`; it denies but surfaces as an exception, not as the `Left` the rest of Encina returns |
| C. A pipeline behavior that checks the tenant first | A `Left` before the handler | Cannot work alone: the handler and its repositories are resolved before any behavior runs, so the factory still throws first (it would have to accompany option B) |

### Chosen Option: **A** (recommended, pending the maintainer)

### Rationale

Marten's session factory has no way to return an error value, so for a session injected through DI the only fail-closed outcome is an exception (option B); a `Left` is possible only if the repository obtains the session itself (option A), which is the design that follows ADR-001 and ADR-053 and AGENTS.md section 3 (a missing tenant denies, and the denial is a value). A costs the constructor changes counted above; B is the smaller and faster option and is valid when exceptions at resolution are acceptable for a denied request. The spike of PR 1 confirms the exact Marten 9.38 `ISessionFactory` contract and how `Encina.Send` reports a resolution exception, before PR 2.

**Class:** Dilemma

</details>

<details>
<summary><strong>7. Single global isolation mode, and a tenant whose strategy disagrees</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| A. `MultiTenancy.Isolation` is the one mode of the store; a tenant whose `TenantInfo` strategy differs gets `Left` `marten.tenant_strategy_mismatch` (category per decision 14) | One mode to reason about and test | A deployment cannot mix a premium dedicated tenant with shared ones |
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

The default is a starting point with no measurement behind it (the load test of PR 6 measures it); disposing a store with an open session would break a live request, so eviction only touches idle stores. B wins when memory must never exceed the bound.

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
| A. `marten.tenant_required` (`InvalidRequest`) and `marten.cross_tenant_denied` (`Forbidden`), created with the category overload of `EncinaErrors.Create` | ADR 062 (merged in PR #3599) decides that the `*_tenant_required` codes are `InvalidRequest` and that denials are `Forbidden` (rule R6); the `marten.<snake_case>` shape follows `MartenErrorCodes` | Two constants |
| B. The same two codes without a category | One argument fewer | Category inferred from the code text, the path ADR 062 retires |

### Chosen Option: **A**

### Rationale

The categories follow ADR 062; the names follow the existing constants in `src/Encina.Marten/MartenErrorCodes.cs`. The categories of the other two codes are decision 14.

**Class:** Trivial
**Settled by:** ADR 062 (decided table for `*_tenant_required` and rule R6) and `src/Encina.Marten/MartenErrorCodes.cs`

</details>

<details>
<summary><strong>14. Categories of <code>marten.tenant_strategy_mismatch</code> and <code>marten.tenant_provisioning_failed</code></strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| A. `tenant_strategy_mismatch` is `Conflict` and `tenant_provisioning_failed` is `Internal` | A mismatch is a state conflict between the tenant record and the store mode; a provisioning failure is a fault of the store | ADR 062 rule R2 covers version mismatches, operations in progress and invalid transitions, not a configuration mismatch, and does not decide provisioning failures |
| B. `tenant_strategy_mismatch` is `Forbidden` (rule R6 makes tenant conflicts `Forbidden`) and `tenant_provisioning_failed` is `Unavailable` (the target database or schema cannot be reached) | R6 is the nearest decided rule; a provisioning failure is usually the store being unreachable | A mismatch is a misconfiguration, not a denial; `Unavailable` suggests a retry that may not help |
| C. `tenant_strategy_mismatch` is `Internal` (a misconfigured deployment) | The caller cannot fix it | Hides a tenant-record problem as a server fault |
| D. `tenant_strategy_mismatch` is `FailedPrecondition` (the system is not in the state the operation needs) | The closest semantic match to a precondition on the tenant record | ADR 062 rule R2 says "Do not use `FailedPrecondition` before #3420", whose row is provisional (HTTP 400), so it is not allowed until #3420 lands; listed to record why it is excluded today |

### Chosen Option: **A** (recommended, pending the maintainer)

### Rationale

The category sets the HTTP and gRPC status users see (ADR 062), and no decided rule fixes either code, so the maintainer chooses.

**Class:** Dilemma

</details>

---

## Implementation Phases

### Phase 1: Options, Spike and Abstractions (PR 1, 8 files)

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.Marten/Tenancy/MartenMultiTenancyOptions.cs` (new) and `EncinaMartenOptions.MultiTenancy` (edit); validation (a resolver is required for the dedicated and schema modes); a `ToString()` that never prints connection strings, and `[JsonIgnore]` on resolvers that carry them (AGENTS.md section 3, #851).
2. Project reference `Encina.Marten` to `Encina.Tenancy` (edit of `src/Encina.Marten/Encina.Marten.csproj`; subject to D4).
3. Error codes in `MartenErrorCodes` (edit): the four codes of decisions 13 and 14 with the category overload of `EncinaErrors.Create` (ADR 062).
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

### Phase 2: Session Seam and Conjoined Configuration (PR 2, 14 files), Repositories (PRs 3 to 5); blocked until D1 and decision 6 are answered

<details>
<summary><strong>Tasks</strong></summary>

PR 2 (14 files):

1. `IMartenSessionAccessor` (`ValueTask<Either<EncinaError, IDocumentSession>> GetAsync(CancellationToken)`, decision 6 option A) and its single implementation `MartenSessionAccessor`, scoped, which returns the scope's own session instance on every call (the unit test asserts two calls return the same instance, because a new session would leave the events staged by a repository unseen by `EventPublishingPipelineBehavior`): with no `MultiTenancy.Isolation` configured it returns the scope's plain session; with isolation configured it reads `IRequestContext.TenantId` and returns `Left` `marten.tenant_required` when it is absent, never an unscoped session; `RequireTenant = false` does not change that (decision 8).
2. `TenantSessionFactory` (Marten `ISessionFactory`) that opens the tenant-bound session for the accessor.
3. `MartenTenancyConfigurator` (`IConfigureOptions<StoreOptions>`, registered like `ConfigureMartenEventMetadata`) that sets the tenancy style and multi-tenants documents when `Isolation` is `SharedSchema`.
4. `AddEncinaMarten` registers the accessor always and the factory and configurator only when `MultiTenancy.Isolation` is configured (`TryAdd*`, never overriding the application's own `ISessionFactory`); DI test with `ValidateOnBuild` and `ValidateScopes`.
5. Diagnostics, created here because the accessor is the first logger: `MartenTenancyLog` (`[LoggerMessage]`); register `MartenTenancy = (7280, 7289)` in `src/Encina/Diagnostics/EventIdRanges.cs` and add it to the `["Encina.Marten"]` list in `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` (Phase 4 lists the ids).
6. Tests in the same PR: accessor unit, factory unit, configurator unit, DI test (new file), guard (accessor, factory), the allocation test edit; manifest entries for every new file.

PRs 3 to 5 (the constructor changes of six types, each with its existing test callers in the same PR, counted by `new <Repository><` on this branch):

7. PR 3 (14 files): the consumers that DI builds for every command move first. `EventPublishingPipelineBehavior` (open generic, scoped, registered by default, resolved before the pipeline runs) takes the accessor and obtains the scope's session before `nextStep()` runs, because `Handle` attaches its `CommittedEventCollector` listener to the session before the handler executes and drains it afterwards (`EventPublishingPipelineBehavior.cs`, `Handle` and `TryAttachCollector`; getting the session after the handler would attach the collector too late and lose the domain events silently): a `Left` from the accessor is returned there, before the handler runs, never a pass-through; the same session instance is used to attach, to remove the listener in the `finally` and (through the repositories) to stage the events; `MartenInlineProjectionDispatcher` (injected into the repository constructors) takes the accessor and returns the `Left`; `MartenReadModelRepository` takes the accessor. Their existing test callers change with the constructors (3 files for the behavior: 1 contract, 1 guard, 1 unit; 2 for the dispatcher: 1 guard, 1 unit; 6 for the read-model repository: 2 contract, 1 guard, 2 integration, 1 unit; fewer where one file constructs several), and each unit file gains the missing-tenant `Left` test, so the per-flag targets of the edited files are met in this PR. The behavior's unit file also gains a publish test (with the accessor, the events committed by the handler are still published once, and nothing is published when the accessor returns a `Left`), and the dispatcher's unit file a test that a projection still runs through the accessor's session.
8. PR 4 (15 files): `MartenAggregateRepository` takes `IMartenSessionAccessor` instead of `IDocumentSession`, gets the session at the start of each operation (a `Left` is returned as is) and builds its `InlineProjectionRelay` per call from that session, so the relay does not change; the 14 existing test files that construct it change with it (1 contract, 2 guard, 10 integration, 1 unit), and the unit file gains the missing-tenant `Left` test.
9. PR 5 (12 files): `SnapshotAwareAggregateRepository` and `MartenSnapshotStore` change the same way; their existing test callers (9 unique files for the two) change with them, with the missing-tenant `Left` test in the unit files; the integration test with two tenants on PostgreSQL is added (tenant A never reads tenant B's events, aggregates or read models, and the domain events of the command are published). The same file carries the combined DI-graph test: a provider built with `ValidateOnBuild` and `ValidateScopes` in which a command sent through `IEncina` with no tenant returns `Left` `marten.tenant_required` through the whole pipeline (behavior, handler, repository, dispatcher) and never throws during resolution; it lives in this PR because it needs every consumer on the accessor, and it adds no file.
10. These are not signature-compatible: the old constructors are removed (AGENTS.md section 3, no compatibility layer); no `[Obsolete]`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT: Phase 1 options and spike exist. EncinaStoreOptionsConfigurator bridges IConfigureOptions<StoreOptions> into Marten; MartenAggregateRepository receives IDocumentSession and IRequestContext by constructor (src/Encina.Marten/MartenAggregateRepository.cs line 54), and Encina.Send resolves the handler before the pipeline runs.
TASK: PR 2 implements the session accessor, the tenant-bound session factory and the conjoined configuration; PRs 3 to 5 move EventPublishingPipelineBehavior, MartenInlineProjectionDispatcher and the four repositories to the accessor (the scope's own session instance) with their test callers; a missing tenant returns Left, never an unscoped session.
KEY RULES: fail closed; TryAdd* registration that never overrides the application's own; old constructors removed with all callers in the same PR; tests execute real code; the two-tenant test uses the shared Marten PostgreSQL fixture.
REFERENCE FILES: src/Encina.Marten/EncinaStoreOptionsConfigurator.cs, src/Encina.Marten/ConfigureMartenEventMetadata.cs, src/Encina.Marten/ServiceCollectionExtensions.cs
```

</details>

### Phase 3: Dedicated and Schema-per-Tenant Routing (PR 6, 15 files, blocked until D1 and D3 are answered)

<details>
<summary><strong>Tasks</strong></summary>

1. `ITenantStoreResolver` / `TenantDocumentStoreResolver`: builds and caches one `IDocumentStore` per tenant from `ConnectionStringResolver` (dedicated) or `SchemaResolver` (schema, D3), disposes them with the host; the cache bound `MaxCachedTenantStores` (an edit of `MartenMultiTenancyOptions`, with its options test edit) and the eviction follow decision 9; resolver output is validated per decision 10.
2. The session factory routes through the resolver for these modes (edit of `TenantSessionFactory`; `ServiceCollectionExtensions` registers the resolver; new `[LoggerMessage]` entries in `MartenTenancyLog`).
3. Tenant data comes from `ITenantStore.GetTenantAsync` (`TenantInfo` carries the strategy); an unknown tenant is a `Left`; a strategy that differs from `MultiTenancy.Isolation` is `marten.tenant_strategy_mismatch` (decisions 7 and 14).
4. Tests in the same PR: resolver unit (cache, bound, disposal), resolver guard, routing and isolation per mode (integration, two tenants, the conjoined case already shipped in PR 5), the isolation property test (FsCheck: for any two tenants, the events one writes are never returned to the other), the DI test (edit), the options unit test (edit, for `MaxCachedTenantStores`), a load test with many tenants (connection routing), and the benchmark `.md` justification (routing is cached, not a hot path; allowed for benchmarks, never for unit, guard or contract tests). Manifest entries for the new files.

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

### Phase 4: Provisioning and Telemetry (PR 7, 13 files), Cross-Tenant Access and Health (PR 8), Observability

<details>
<summary><strong>Tasks</strong></summary>

1. PR 7: `IMartenTenantProvisioner` (`ProvisionTenantAsync`, `DeprovisionTenantAsync(tenantId, purge, ct)` per decision 11, `GetStatisticsAsync`, all `Either`), idempotent provisioning; DDL through the tenant store with the names validated per decision 10; `TenantStatistics`; `MartenTenancyTelemetry` (one file holding the meter, the counter `encina.marten.tenancy.cross_tenant_reads` used from PR 8, and the activity source with the span `encina.marten.tenancy.provision`; tenant ids, schema names and connection strings are never tags or log arguments), created here because the provisioner is its first user; new log entries and the DI registration (edits); tests: provisioner unit, guard, contract (real provisioner), integration (provision, deprovision, purge), telemetry unit, DI test (edit); manifest entries.
2. PR 8: cross-tenant read per decision 12: `ICrossTenantReader` and its implementation, requiring `AllowCrossTenantQueries`, logged and counted; the caller authorizes with `[RequireRole]` and `CrossTenantRole`.
3. PR 8: `MartenTenantHealthCheck` per tenant, building on `Encina.Tenancy`'s `TenantHealthCheck` (`DefaultName` const, static `Tags`).
4. PR 2 creates the log class; the ids are packed sequentially from 7280 in the order the PRs add them: 7280 tenant required (PR 2), 7281 tenant bound (PR 2), 7282 store created (PR 6), 7283 store evicted (PR 6), 7284 strategy mismatch (PR 6), 7285 tenant provisioned (PR 7), 7286 tenant deprovisioned (PR 7), 7287 provisioning failed (PR 7), 7288 cross-tenant read (PR 8), 7289 cross-tenant denied (PR 8); the whole range 7280-7289 is registered in PR 2 and lies inside the block assigned to this family in `artifacts/decisions/plan-batch/eventid-blocks.md`.

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

### Phase 5: Testing, Documentation and Finalization (tests in PRs 1 to 8, documentation in PR 8)

<details>
<summary><strong>Tasks</strong></summary>

1. Each PR carries its own tests and per-file manifest targets with justifications (`.github/coverage-manifest/Encina.Marten.json`, `mechanical-fixer`), as listed in Phases 1 to 4: the isolation property test ships in PR 6, the provisioner contract and integration tests in PR 7, the cross-tenant denial integration test in PR 8. Phase 5 adds none.
2. PR 8 (4 documentation files), `docs-writer`: multi-tenancy guide with a decision table of the three modes, ADR for D1 to D4 and decisions 6 to 14 (ADR number assigned when the ADR pull request opens), `docs/INVENTORY.md`, changelog fragment `changelog.d/326-marten-multi-tenancy.added.md`. `Encina.Marten` has no package README on main.
3. Verification in every PR: zero warnings, every flag at target, CRAP of changed methods at most 10.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT: All modes, provisioning, diagnostics exist; coverage is per flag against .github/coverage-manifest/Encina.Marten.json.
TASK: PR 8 only: write the guide, the ADR, the INVENTORY entry and the fragment (docs-writer and mechanical-fixer); tests and manifest targets shipped with PRs 1 to 8.
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
| `Encina.Audit.Marten`, `Encina.Marten.GDPR` stores | `src/Encina.Audit.Marten`, `src/Encina.Marten.GDPR` | Take `IDocumentSession` from DI, so they become tenant-scoped through the factory and fail at resolution when the tenant is missing (decision 6; their move to the accessor is the follow-up issue to be opened, draft: artifacts/decisions/plan-batch/followups/marten-tenancy-session-consumers.md) |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Marten` (new range `MartenTenancy`) | 7280-7289 (all ten used, listed in Phase 4) | Inside the block 7250-7299 assigned to the family in the batch registry (`artifacts/decisions/plan-batch/eventid-blocks.md`), which lists the issues sharing the block; each sibling plan names its own range |

### Estimated File Count

Every added or edited file is counted. `Encina.Marten` has no PublicAPI file; `Encina.Tenancy` is untouched unless D2 = B. Under decision 6 option A the constructors of six types (four repositories, `EventPublishingPipelineBehavior`, `MartenInlineProjectionDispatcher`) change and their existing test callers (counted by `new <Repository><` on this branch) change in the same PRs (3 to 5); under option B no existing signature changes.

| PR | Category | Files | Notes |
|----|----------|-------|-------|
| 1 | Source | 4 | `MartenMultiTenancyOptions`, `EncinaMartenOptions` edit, `Encina.Marten.csproj` edit, `MartenErrorCodes` edit |
| 1 | Tests | 3 | Options unit, options guard, spike (integration) |
| 1 | Manifest | 1 | `Encina.Marten.json` |
| 1 | **Subtotal** | **8** | |
| 2 | Source | 7 | `IMartenSessionAccessor`, `MartenSessionAccessor`, `TenantSessionFactory`, `MartenTenancyConfigurator`, `ServiceCollectionExtensions` edit, `MartenTenancyLog`, `EventIdRanges.cs` edit |
| 2 | Tests | 6 | Accessor unit, factory unit, configurator unit, DI (new), guard (accessor, factory), `EncinaEventIdAllocationTests.cs` edit |
| 2 | Manifest | 1 | `Encina.Marten.json` |
| 2 | **Subtotal** | **14** | |
| 3 | Source | 3 | `EventPublishingPipelineBehavior`, `MartenInlineProjectionDispatcher`, `MartenReadModelRepository` constructors |
| 3 | Existing test callers | 11 | Behavior 3 (contract, guard, unit), dispatcher 2 (guard, unit), read-model repository 6 (2 contract, 1 guard, 2 integration, 1 unit); the unit files gain the missing-tenant `Left` tests; fewer files where one file constructs several |
| 3 | **Subtotal** | **14** | At most 14 |
| 4 | Source | 1 | `MartenAggregateRepository` constructor |
| 4 | Existing test callers | 14 | 1 contract, 2 guard, 10 integration, 1 unit (gains the missing-tenant `Left` test) |
| 4 | **Subtotal** | **15** | |
| 5 | Source | 2 | `SnapshotAwareAggregateRepository`, `MartenSnapshotStore` constructors |
| 5 | Existing test callers | 9 | Unique files that construct either (6 and 5, with overlap) |
| 5 | Tests | 1 | Two-tenant conjoined integration test (also asserts the domain events are published) |
| 5 | **Subtotal** | **12** | |
| 6 | Source | 6 | `ITenantStoreResolver`, `TenantDocumentStoreResolver`, `MartenMultiTenancyOptions` edit, `TenantSessionFactory` edit, `ServiceCollectionExtensions` edit, `MartenTenancyLog` edit |
| 6 | Tests | 8 | Resolver unit, resolver guard, routing integration, isolation property, load, DI test edit, options test edit, benchmark `.md` justification |
| 6 | Manifest | 1 | `Encina.Marten.json` |
| 6 | **Subtotal** | **15** | |
| 7 | Source | 6 | `IMartenTenantProvisioner`, `MartenTenantProvisioner`, `TenantStatistics`, `MartenTenancyTelemetry`, `MartenTenancyLog` edit, `ServiceCollectionExtensions` edit |
| 7 | Tests | 6 | Provisioner unit, guard, contract, integration, telemetry unit, DI test edit |
| 7 | Manifest | 1 | `Encina.Marten.json` |
| 7 | **Subtotal** | **13** | |
| 8 | Source | 5 | `ICrossTenantReader`, `CrossTenantReader`, `MartenTenantHealthCheck`, `MartenTenancyLog` edit, `ServiceCollectionExtensions` edit |
| 8 | Tests | 5 | Cross-tenant unit, health unit, guard (reader and health check), integration (cross-tenant denial), DI test edit |
| 8 | Manifest | 1 | `Encina.Marten.json` |
| 8 | Docs | 4 | Guide, ADR, `docs/INVENTORY.md`, changelog fragment |
| 8 | **Subtotal** | **15** | |
| | **Total** | **106** | 8 + 14 + 14 + 15 + 12 + 15 + 13 + 15; each PR at most 15 files and 1,500 lines |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
PROJECT CONTEXT: Encina, pre-1.0, .NET 10 / C# 14. Issue #326 adds multi-tenancy for event sourcing to Encina.Marten using the Encina.Tenancy abstractions. Follow AGENTS.md: fail closed, ROP, secrets never in logs or ToString, TimeProvider, per-flag coverage.

IMPLEMENTATION OVERVIEW: Phase 1 options, reference, error codes and a Marten-API spike (PR 1). Phase 2 session accessor, tenant-bound session factory and conjoined mode through store options, with the EventId range 7280-7289 (PR 2), then the four repositories take the accessor with their test callers (PRs 3 to 5). Phase 3 per-tenant store resolver for dedicated and schema modes (PR 6). Phase 4 provisioning and telemetry (PR 7), explicit logged cross-tenant reads and health check (PR 8). Phase 5 docs (PR 8). Each PR carries its own tests and manifest targets. Dilemmas D1 (enforcement), D2 (provisioning home), D3 (schema mode), D4 (reuse of Tenancy) and decisions 6 to 12 and 14 follow the maintainer's answers; the spike can send D1 and D3 back to the maintainer.

KEY PATTERNS: tenant from IRequestContext.TenantId; Left when missing; TryAdd* registration with a ValidateOnBuild test; sequential EventIds registered first; shared integration fixtures with two tenants.

REFERENCE FILES: src/Encina.Marten/EncinaMartenOptions.cs, src/Encina.Marten/EncinaStoreOptionsConfigurator.cs, src/Encina.Marten/ServiceCollectionExtensions.cs, src/Encina.Tenancy/Abstractions/TenantIsolationStrategy.cs, src/Encina/Diagnostics/EventIdRanges.cs, docs/plans/dsr-implementation-plan-404.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | The per-tenant store cache covers the hot path; an `ICacheProvider` layer over tenant configuration has no measured need |
| 2 | OpenTelemetry | ✅ | `encina.marten.tenancy.provision` span and cross-tenant counter (`MartenTenancyTelemetry`, PR 7; counter used in PR 8) |
| 3 | Structured Logging | ✅ | `MartenTenancyLog`, EventIds 7280-7289; the cross-tenant opt-in is logged (Phases 2 to 4) |
| 4 | Health Checks | ✅ | `MartenTenantHealthCheck` per tenant (Phase 4, PR 8) |
| 5 | Validation | ✅ | Options, tenant identifiers and resolver output validated (decision 10, Phases 1 and 4) |
| 6 | Resilience | ❌ | A failure to reach a tenant database is a `Left` logged by error code and the caller retries; a retry policy on the store resolver would hide a tenant-specific outage |
| 7 | Distributed Locks | ❌ | Provisioning is idempotent (create if missing, ignore if present), so concurrent provisioning converges without a lock |
| 8 | Transactions | ✅ | Each tenant session is its own unit of work; no cross-tenant transaction exists (Phases 2 and 3) |
| 9 | Idempotency | ✅ | Provisioning and deprovisioning are idempotent (Phase 4) |
| 10 | Multi-Tenancy | ✅ | This is the feature |
| 11 | Module Isolation | ❌ | Marten has no module-scoped streams today, so tenant filtering has no module dimension |
| 12 | Audit Trail | ⏭️ | Provisioning, deprovisioning and cross-tenant reads are compliance relevant: follow-up issue to be opened (draft: artifacts/decisions/plan-batch/followups/marten-tenancy-audit.md); cross-tenant reads are already logged and counted in PR 8 |

---

## Prerequisites & Dependencies

- #346 (Multi-Tenancy Support, open) is the umbrella; #357 delivered `Encina.Tenancy`.
- #1257 consumes this mechanism for the SPEC-002 two-tenant suite on Marten; it should be briefed after PR 5 of this plan merges.
- Siblings #323 (per-tenant snapshot policies) and #324 (per-tenant rebuild) wait for this plan.

---

## Next Steps

1. The maintainer answers the Dilemmas (D1 to D4 and decisions 6 to 12 and 14) when work on #326 starts; the answers go into `## Maintainer Decisions`.
2. Run the Phase 1 spike first; if Marten 9.38 supports more or less than assumed, update the choices before PR 2 starts.
3. Write the PR briefs in order and brief #1257 after PR 5 (the repositories are tenant aware from then on); open the audit follow-up and the session-consumers follow-up from their drafts.
