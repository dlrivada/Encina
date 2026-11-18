# Implementation Plan: Tenant query filter for any EF Core context and the tenant isolation security guide

> **Issue**: [#385](https://github.com/dlrivada/Encina/issues/385)
> **Type**: Feature (child of the EPIC #876; the resolvers, the isolation levels and the provider filtering of its text are already implemented)
> **Complexity**: Low to Medium (2 phases of code, 1 testing and documentation phase, 3 worker pull requests)
> **Estimated Scope**: ~3 production files, ~7 test files, ~6 metadata and documentation files
> **Related plans** (cited by issue number): #346 (the tenant gate), #338 (existence and status), #304 (ownership validation), #1257 (shared tenant-scope primitive, tenant-id rules, tenant-scoped keys, the two-tenant isolation suite), #125 (messaging dispatch guard), #711 (tenant connection cache), #1250 (column encryption), #3748 (the audited platform-operator path)

---

## Summary

#385 proposes `ITenantContext`, a `TenantId` value type, resolvers, a filtering pipeline behavior, EF Core and Dapper filtering packages, tenant propagation in messaging, three isolation levels and a security section. Almost all of it exists under other names or is owned by another plan. This plan records the mapping item by item and plans only what is left: **the tenant query filter for an EF Core context that cannot inherit `TenantDbContext`**, and **the security guide** the issue lists as an acceptance criterion.

**State of the code on `ec129cae5` (current `origin/main`; the cited files were read on it).**

| Item of #385 | State | Evidence or owner |
|--------------|-------|-------------------|
| `ITenantContext`, `TenantId` value type, `TenantIsolationLevel` | Not built; the tenant is the string `IRequestContext.TenantId`, read through `ITenantProvider`; the isolation levels are `TenantIsolationStrategy` (`SharedSchema`, `SchemaPerTenant`, `DatabasePerTenant`) | Design Choice 1 |
| Resolvers: header, claims, subdomain, route, chain | Done | `src/Encina.Tenancy.AspNetCore/Resolution/` |
| `TenantFilteringPipelineBehavior : ITenantAware` | Replaced by the gate (#346), the validation (#338) and the ownership check (#304); the data filter is in the repositories | |
| EF Core `ApplyTenantFiltering` for any context | **Missing: this plan.** The filter is private to `TenantDbContext`, an abstract base class | `src/Encina.EntityFrameworkCore/Tenancy/TenantDbContext.cs` (`ApplyTenantQueryFilters`) |
| `Encina.MultiTenancy.Dapper` package | Not needed: tenant filtering is in each Dapper provider package (`TenantAwareFunctionalRepositoryDapper`, `TenantAwareSpecificationSqlBuilder`); ADO.NET and MongoDB likewise | `src/Encina.Dapper.*/Tenancy/`, `src/Encina.ADO.*/Tenancy/`, `src/Encina.MongoDB/Tenancy/` |
| Tenant context propagation in outbox, inbox, saga, scheduled messages | #737, #738, #739, #760 (tenant columns), #1164 (persisted context), #125 (dispatch guard) | open issues and plans |
| Tenant-aware cache and connection pooling | Cache keys already carry the tenant (`DefaultCacheKeyGenerator`); connection strings #711 | |
| Tenant-specific encryption options | #1257 (tenant-scoped keys) and #1250 (column encryption) | |
| Row-Level Security integration | Deferred by the EPIC (#876 "Out of Scope": row-level security database policies) | Design Choice 4; follow-up issue file |
| "Security documentation" | **Missing: this plan** (`docs/features/multi-tenancy.md` has an FAQ, no threat model) | Phase 3 |

**What is missing, by reading the code.** An application that already derives its context from another base class (an identity context, a shared base of its own) cannot use `TenantDbContext`, and the doc says so ("a plain `DbContext` with a hand-written `HasQueryFilter` does not get any of this behaviour", `docs/features/multi-tenancy.md` line 346). It hand-writes a filter and gets none of the named-filter key, the fail-closed tenant comparison or the tests. And no page tells an architect where a tenant leak can happen and which Encina control stops it.

**Scope.** `ITenantFilterContext`, `ModelBuilder.ApplyTenantQueryFilters(...)`, `TenantDbContext` delegating to it (and dropping its non-public reflection), the integration tests that prove it on the three EF Core providers, the security guide. **Out of scope**: the gate, validation and ownership behaviors (#346, #338, #304), the tenant columns of messages (#737 and siblings), Row-Level Security (follow-up issue), tenant-scoped keys (#1257).

**Affected packages**: `Encina.EntityFrameworkCore` (code), documentation under `docs/features/`. **Provider category**: Database, EF Core x3 (SqlServer, PostgreSQL, MySQL) share one provider-agnostic assembly; the other seven providers already filter in their repositories (table above), so no ADO.NET, Dapper or MongoDB code is written.

---

## Design Choices

<details>
<summary><strong>1. The abstractions of the issue: a tenant value type and a tenant context interface</strong></summary>

**Class:** Dilemma (recommended, pending the maintainer)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Neither: the tenant stays a `string` read through `ITenantProvider` and `IRequestContext.TenantId`; the tenant-id rules (length, blank, padded, reserved id) are enforced by the resolver and the options validator of #1257; the isolation level is `TenantInfo.Strategy`** | No change to the roughly 415 declarations of a string tenant id in `src/` (counted by grep on `origin/main`); one tenant type for the 10 providers, the caches and the messages | The type system does not tell a tenant id from any other string |
| **B) `readonly record struct TenantId`, `ITenantContext` and `TenantIsolationLevel` as in the issue** | A typed id | A second tenant concept next to `ITenantProvider`, `TenantInfo` and the `ITenantScopeResolver` of #1257; an id type through `IRequestContext`, the persisted identity, 10 providers, MongoDB documents and every cache key at once |
| **C) The value type only, validating the rules in its constructor** | One place for the rules; typed | The same churn as B for the id, with the rules duplicated next to #1257's resolver |

### Chosen Option: **A — Keep the string and the existing abstractions** (recommended, pending the maintainer)

### Rationale

- A wins while `IRequestContext.TenantId` is a string persisted with messages and carried through every store (#1164), because the type would have to cross all of them in one step; B or C win if the maintainer wants the type-system guarantee badly enough to pay that cost before 1.0, when the change is cheap and after it is not. The existing name for the isolation level is kept (`TenantIsolationStrategy`); a second enum with different names is not created.

</details>

<details>
<summary><strong>2. How the EF Core tenant filter reaches a context that does not inherit `TenantDbContext`</strong></summary>

**Class:** Dilemma (recommended, pending the maintainer)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A public interface `ITenantFilterContext { string? CurrentTenantId { get; } }` and an extension `ModelBuilder.ApplyTenantQueryFilters<TContext>(this ModelBuilder, TContext context) where TContext : DbContext, ITenantFilterContext`, called from `OnModelCreating(...)` with `this`; `TenantDbContext` implements the interface and calls the same extension** | Explicit; one implementation of the filter for every context; the same named filter key and the same fail-closed comparison; removes the non-public reflection (`BindingFlags.NonPublic`, `S3011`) from `TenantDbContext` | A context that forgets the call has no filter |
| **B) Keep the filter inside `TenantDbContext` only** | No new API | A context with another base class hand-writes a filter and gets neither the key nor the tests |
| **C) An `IModelCustomizer` or convention registered by `AddEncinaTenancy...`, applied to every context of the application with no change to the context classes** | No call to forget | Magic: it applies to contexts that must not be filtered (a platform context); the context instance has to be reached through the model customizer, a less common EF Core extension point; harder to test and to read |

### Chosen Option: **A — An interface and an extension** (recommended, pending the maintainer)

### Rationale

- A wins when each application knows which contexts hold tenant data; C wins if an application has many contexts that all hold tenant data and the maintainer accepts the registration-level magic; B wins if every consumer is willing to inherit the base class. The filter expression is the one `TenantDbContext` builds today (the named filter `Encina.Tenancy`, so it coexists with the soft-delete filter `Encina.SoftDelete`); the integration tests of Phase 2 decide whether EF Core parameterizes the tenant read per context instance when it goes through the interface, and the extension uses the context-typed property access if it does not.

</details>

<details>
<summary><strong>3. What the filter does when the context has no tenant</strong></summary>

**Class:** Settled
**Settled by:** `AGENTS.md` section 3 (compliance and security gates fail closed; the only opt-out is explicit and logged) and the EF filter as shipped: `e.TenantId == CurrentTenantId` over a non-null `TenantId` column (`ITenantEntity.TenantId` is `string`), which matches no row when the tenant is null.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The filter is always `e.TenantId == tenant`; with no tenant it returns no rows; a query that must see other rows names the bypass explicitly (`IgnoreQueryFilters`, Design Choice 5)** | Fail closed; no hidden "no tenant means everything" mode | A tenantless background job reads nothing until it restores a tenant (#1164) |
| **B) The filter is skipped when no tenant is resolved** | Admin and startup code just works | Fail open: a request whose tenant failed to resolve sees every tenant's rows |

### Chosen Option: **A — Always filter**

### Rationale

- Only A complies. The switch `UseQueryFilters` that exists today (`EfCoreTenancyOptions`) is removed by #3197, so the extension never reads an option that could turn the filter off.

</details>

<details>
<summary><strong>4. Row-Level Security as defense in depth</strong></summary>

**Class:** Settled
**Settled by:** the EPIC #876 body, section "Out of Scope": "Row-level security database policies (provider-specific advanced feature, deferred)".

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Defer: not built here; recorded as a follow-up issue so the deferral is tracked** | Complies with the EPIC; RLS exists on PostgreSQL and SQL Server only (MySQL has none), so it would not satisfy the 10-provider rule | Shared-schema isolation relies on application filters until the follow-up |
| **B) Build RLS policies for PostgreSQL and SQL Server now** | A second layer under the filters | Contradicts the EPIC's out-of-scope list; provider-specific and incomplete across the matrix |

### Chosen Option: **A — Deferred, with a follow-up issue**

### Rationale

- Only A complies with the EPIC. The follow-up issue file (`artifacts/issues/tenancy-row-level-security.md`) states the 10-provider consequence (MySQL and MongoDB have no equivalent) for the maintainer to weigh.

</details>

<details>
<summary><strong>5. Detecting a bypass of the tenant filter</strong></summary>

**Class:** Dilemma (recommended, pending the maintainer)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Document the bypasses (the unnamed `IgnoreQueryFilters()`, the keyed `IgnoreQueryFilters(["Encina.Tenancy"])`, raw SQL, `FromSqlRaw`) in the security guide as controls the application owns, and name the keyed form in no new API** | No new machinery; honest about what EF Core allows | The guide cannot stop a bypass |
| **B) An architecture rule in `Encina.Testing.Architecture` (`EncinaArchitectureRules`) that fails when application code calls `IgnoreQueryFilters` on a tenant entity outside an allow-list** | A test that stops regressions in an application | A new public rule to maintain; Roslyn-level detection of an EF call is approximate in a type-based architecture test |

### Chosen Option: **A — Document, no new rule** (recommended, pending the maintainer)

### Rationale

- A wins now; B wins if the maintainer wants a guard that application teams run in their own CI, a decision that belongs with the platform-operator path of #3748, which replaces the bypass with an audited permission. The guide at `docs/features/multi-tenancy.md` lines 346 and 840 currently teaches the keyed bypass as normal practice; that contradicts the maintainer decision of #3111 (gate G3) and is the follow-up issue file `artifacts/issues/tenancy-guide-filter-bypass.md`, not a change in this plan.

</details>

<details>
<summary><strong>6. Where the security guide lives</strong></summary>

**Class:** Trivial
**Settled by:** the documentation skill `.claude/skills/encina-docs/SKILL.md` (one Diataxis quadrant per page): the existing `docs/features/multi-tenancy.md` mixes reference and how-to; a threat model is explanation.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A new page `docs/features/multi-tenancy-security.md` (explanation), linked from the existing guide** | One quadrant per page; the guide stays a how-to | A second page to keep in step |
| **B) A new section in `docs/features/multi-tenancy.md`** | One page | A third quadrant in a page that already mixes two |

### Chosen Option: **A — A new explanation page**

### Rationale

- The page maps each leak vector to the Encina control and the plan or issue that owns it, so it is checked against the code by the docs reviewer.

</details>

---

## Implementation Phases

### Phase 1: Core Abstractions & Models (PR 385-A)

> **Goal**: the interface and the extension.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create `src/Encina.EntityFrameworkCore/Tenancy/ITenantFilterContext.cs`**: `public interface ITenantFilterContext { string? CurrentTenantId { get; } }` with XML docs that state the contract: the property returns the tenant of the current operation, `null` when none (the filter then matches no row).
2. **Create `src/Encina.EntityFrameworkCore/Tenancy/TenantModelBuilderExtensions.cs`**: `public static ModelBuilder ApplyTenantQueryFilters<TContext>(this ModelBuilder modelBuilder, TContext context) where TContext : DbContext, ITenantFilterContext`; guards null arguments; for every entity type assignable to `ITenantEntity` adds the named query filter `TenantDbContext.TenantQueryFilterKey` (`"Encina.Tenancy"`, made `public const` in a new `TenantQueryFilterKeys` holder only if the keyed bypass of Design Choice 5 needs it; otherwise it stays internal) built as `e => e.TenantId == context.CurrentTenantId`; returns the builder for chaining. A type that already has a filter under that key is left as is (the call is idempotent).
3. **Modify `src/Encina.EntityFrameworkCore/Tenancy/TenantDbContext.cs`**: implement `ITenantFilterContext` (`CurrentTenantId` already exists as a protected property; the interface member is an explicit implementation), replace the private `ApplyTenantQueryFilters` body and its `BindingFlags.NonPublic` reflection (`S3011`) by `modelBuilder.ApplyTenantQueryFilters(this)`. The call stays unconditional in this plan; #3197 removes the `UseQueryFilters` switch that surrounds it today, so the two changes touch the same lines and land in sequence (Prerequisites).
4. PublicAPI lines for the interface and the extension (mechanical-fixer).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
- Issue #385, plan tenant-ef-filter-security-implementation-plan-385.md. Design Choices 1, 2, 3 and 5 define the API; read them first. TenantDbContext builds the filter with Expression and a non-public reflection on its protected CurrentTenantId.

TASK:
Create ITenantFilterContext and the ApplyTenantQueryFilters extension; make TenantDbContext implement the interface and call the extension instead of its private method.

KEY RULES:
- Always filter: e.TenantId == context.CurrentTenantId; no option can turn the filter off; no tenant means no rows.
- Same named filter key as today so the soft-delete filter (Encina.SoftDelete) still coexists; guard null arguments; XML docs with an example of a context with its own base class.
- No change in behavior for existing TenantDbContext descendants: the existing unit and integration tests pass unchanged.

REFERENCE FILES:
- src/Encina.EntityFrameworkCore/Tenancy/TenantDbContext.cs
- src/Encina.EntityFrameworkCore/Configuration/EntityConfigurationExtensions.cs (SoftDeleteQueryFilterKey and the named-filter extension style)
- tests/Encina.UnitTests/EntityFrameworkCore/Tenancy/TenantDbContextTests.cs
```

</details>

### Phase 2: Provider Integrations (PR 385-B)

> **Goal**: prove the extension on the three EF Core providers.

<details>
<summary><strong>Tasks</strong></summary>

1. A shared test context `tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/Tenancy/PlainTenantFilterDbContext.cs` that derives from `DbContext` (not `TenantDbContext`), implements `ITenantFilterContext` over an `ITenantProvider`, and calls the extension from `OnModelCreating`.
2. Integration tests on the three EF Core providers, each in the existing collection (`[Collection("EFCore-SqlServer")]`, `[Collection("EFCore-PostgreSQL")]`, `[Collection("EFCore-MySQL")]`) next to the existing `TenancyEF*Tests`: `tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/{SqlServer,PostgreSQL,MySQL}/Tenancy/PlainContextTenantFilter*Tests.cs`. Cases: tenant A sees only A's rows; two instances of the same context type with different tenants in one process each see only their own rows (the model is cached per context type, so this is the case that proves the tenant is read per instance); no tenant returns no rows; the soft-delete filter and the tenant filter coexist on one entity; the keyed bypass returns all rows (documented behavior, asserted so a change is deliberate); a filtered query never returns another tenant's row after a tenant switch on a new instance.
3. `[Trait("Category", "Integration")]` and `[Trait("Database", "<Db>")]` on each class; `ClearAllDataAsync()` in `InitializeAsync()`; never `IClassFixture<T>`, never a per-class fixture.
4. EF Core x3 only: ADO.NET, Dapper and MongoDB have no EF context; their tenant filter is already covered by the per-provider tenancy tests listed in Research.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
- PR 385-A merged: ITenantFilterContext and the extension exist. The existing TenancyEF*Tests use TenantTestDbContext, which inherits TenantDbContext.

TASK:
Write PlainTenantFilterDbContext and the three provider integration test classes of the task list.

KEY RULES:
- Shared [Collection] fixtures only (EFCore-SqlServer, EFCore-PostgreSQL, EFCore-MySQL); ClearAllDataAsync in InitializeAsync; never dispose the fixture from a test.
- Shouldly through Encina.Testing.Shouldly; no Thread.Sleep.
- If EF Core does not read the tenant per context instance through the interface, change the extension to the context-typed property access and keep the test: the test decides.

REFERENCE FILES:
- tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/SqlServer/Tenancy/TenancyEFSqlServerTests.cs
- tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/Tenancy/TenantTestDbContext.cs
- docs/testing/integration-tests.md
```

</details>

### Phase 3: Documentation & Finalization (PR 385-C; tests and metadata in PR 385-A)

> **Goal**: every flag at its target; the security guide exists.

<details>
<summary><strong>Tasks</strong></summary>

1. **Unit tests** `tests/Encina.UnitTests/EntityFrameworkCore/Tenancy/TenantModelBuilderExtensionsTests.cs`: the filter is added to every `ITenantEntity` type and to no other; the key is `Encina.Tenancy`; a second call is idempotent; the returned builder is the same instance; `TenantDbContextTests` still pass with the delegation.
2. **Guard tests** `tests/Encina.GuardTests/EntityFrameworkCore/Tenancy/TenantModelBuilderExtensionsGuardTests.cs`: null `modelBuilder` and null `context` throw `ArgumentNullException`.
3. **Contract**: justification in the unit test class comment: `ITenantFilterContext` has one member and one implementer in `src/`; the contract is asserted by the integration tests.
4. **Property**: justification `.md` (`tests/Encina.PropertyTests/EntityFrameworkCore/Tenancy.md`): the filter is a single equality over a non-null column; the invariant (a tenant sees only its rows) is asserted per provider by the integration tests with several tenants and by #1257's two-tenant suite.
5. **Load and benchmark**: justification `.md` files (`tests/Encina.LoadTests/Tenancy/TenantFilter.md`, `tests/Encina.BenchmarkTests/Tenancy/TenantFilter.md`): the extension runs once per model build and holds no state.
6. **Documentation (PR 385-C, docs-writer)**: the new page `docs/features/multi-tenancy-security.md` (threat model: tenant spoofing through a header and the trusted-proxy rule, a client value that disagrees with the verified claim answers 403, cross-tenant leakage vectors and the control that stops each, with the owner: the gate #346, validation #338, ownership #304, repository and EF filters, cache keys and #1257's injective segment, background jobs and #1164, message columns #737 and siblings, tenant keys #1257 and #1250, connection strings #711, raw SQL and `IgnoreQueryFilters` as application-owned risks, Row-Level Security deferred); a section "A context with its own base class" in `docs/features/multi-tenancy.md` replaces the sentence at line 346; `src/Encina.EntityFrameworkCore/README.md` mentions the extension.
7. **Changelog fragment** `changelog.d/385-tenant-filter-any-context.added.md` (mechanical-fixer); manifest `Encina.EntityFrameworkCore.json` and PublicAPI through mechanical-fixer.

**Per-file targets** (proposed; measured values replace them and never go down; the package targets of `Encina.EntityFrameworkCore.json` are unit 40, guard 10, integration 25, property 17, contract 5; the existing Tenancy files have no per-file entry):

| File | Baseline | Flags | Justification |
|------|----------|-------|---------------|
| `Tenancy/TenantModelBuilderExtensions.cs` (new) | new | unit 100, guard 90, integration 90 | the extension is a few lines: unit builds a model and inspects the filters, guard rejects nulls, integration runs it on three databases. |
| `Tenancy/ITenantFilterContext.cs` (new) | new | unit 0 | an interface with no executable line; exercised through the extension. |
| `Tenancy/TenantDbContext.cs` | none | unit 80, integration 85 | existing context tests and the three provider tests run the delegated filter; the save-time branches keep their own cases. |

CRAP gate: the extension has no branch beyond the entity-type loop and the idempotence check.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT:
- Code phases written. Per-flag coverage is measured separately (AGENTS.md section 9); tests execute real code.

TASK:
Write the unit and guard tests and the justification files; delegate manifests, PublicAPI and the fragment to mechanical-fixer and the pages to docs-writer; measure per-flag coverage.

KEY RULES:
- Justification .md only where the test type is legitimately skipped (property, load, benchmark); never for unit, guard or the database integration tests.
- Never edit CHANGELOG.md; documentation never types coverage figures; the security page cites the code and the owning plan or issue for every control.

REFERENCE FILES:
- tests/Encina.UnitTests/EntityFrameworkCore/Tenancy/TenantDbContextTests.cs
- .github/coverage-manifest/Encina.EntityFrameworkCore.json
- .claude/skills/encina-docs/SKILL.md
- changelog.d/README.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Requirement | Use in this feature |
|--------|-------------|---------------------|
| `AGENTS.md` section 3 and section 5 | gates fail closed; a database feature is implemented for all 10 providers | Design Choice 3; the provider table below |
| SPEC-002 REQ-061, INV-008 | no cross-tenant read, action or export | the filter and the guide |
| ADR-054 D4 | tenant tag only when multi-tenancy is on | no telemetry is added here |
| ADR-018 | 12-function evaluation | matrix |
| OWASP ASVS V4 and the Azure multitenant guidance (isolation models) | access control on the server; isolation models and their trade-offs | the security guide |
| EF Core named query filters (EF Core 10) | several filters per entity with keys; `IgnoreQueryFilters(keys)` | Design Choices 2, 3 and 5 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
| :--- | :--- | :--- |
| `TenantDbContext`, `TenantDbContextFactory`, `EfCoreTenancyOptions` | `src/Encina.EntityFrameworkCore/Tenancy/` | the filter to extract; the switch #3197 removes |
| Soft-delete named filter and `SoftDeleteQueryFilterKey` | `src/Encina.EntityFrameworkCore/Configuration/EntityConfigurationExtensions.cs` | pattern for a named-filter extension; coexistence test |
| `ITenantEntity` | `src/Encina.Tenancy/Abstractions/ITenantEntity.cs` | the entity contract the filter targets |
| `TenantAwareFunctionalRepository*`, `TenantAwareSpecificationSqlBuilder` | `src/Encina.ADO.*/Tenancy/`, `src/Encina.Dapper.*/Tenancy/`, `src/Encina.MongoDB/Tenancy/` | the filter on the other seven providers; no code here |
| Existing EF Core tenancy integration tests | `tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/{SqlServer,PostgreSQL,MySQL}/Tenancy/` | template and collections of the new tests |
| `DefaultCacheKeyGenerator` | `src/Encina.Caching/KeyGeneration/DefaultCacheKeyGenerator.cs` | tenant segment of cache keys (cited in the guide) |
| Tenancy guide | `docs/features/multi-tenancy.md` | gains the section and links the new page |
| `TenantResolutionMiddleware` | `src/Encina.Tenancy.AspNetCore/Middleware/TenantResolutionMiddleware.cs` | 403 on a header that disagrees with the claim (cited in the guide) |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.EntityFrameworkCore` | none | the extension runs once per model build and logs nothing; no `[LoggerMessage]` is added, so no range is registered or used |

### Estimated File Count

| Category | Files | Notes |
| :--- | :--- | :--- |
| Production (interface, extension, `TenantDbContext` edit) | 3 | PR 385-A |
| Tests, unit and guard | 2 | PR 385-A |
| Metadata (manifest, `PublicAPI.Unshipped.txt`, changelog fragment) | 3 | PR 385-A |
| Tests, integration (shared context, three provider classes) | 4 | PR 385-B |
| Justification files (property, load, benchmark) | 3 | PR 385-B |
| Documentation (`multi-tenancy-security.md`, `multi-tenancy.md`, EF README) | 3 | PR 385-C (plus `docs/INVENTORY.md` if the docs-writer finds a listing: 1) |
| Total | 18 to 19 | three worker pull requests |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Combined prompt — all phases</strong></summary>

```text
PROJECT CONTEXT:
- Encina, .NET 10 / C# 14, pre-1.0, ROP (Either<EncinaError, T>). Issue #385: most of the multi-tenancy core exists in Encina.Tenancy and the provider packages; the missing pieces are a tenant query filter for an EF Core context that cannot inherit TenantDbContext, and a security guide.
- The tenant is the string IRequestContext.TenantId; TenantDbContext applies the named filter "Encina.Tenancy" over ITenantEntity.

IMPLEMENTATION OVERVIEW:
1. ITenantFilterContext and ModelBuilder.ApplyTenantQueryFilters<TContext>(context): the filter extracted from TenantDbContext, which delegates to it.
2. Integration tests on the three EF Core providers with a plain DbContext, including two instances of one context type with different tenants.
3. Unit and guard tests; justification files for property, load and benchmark; manifest, PublicAPI and fragment by mechanical-fixer.
4. docs-writer: the security explanation page, the section in the guide, the EF README.
5. Follow-up issue files: Row-Level Security, the guide teaching the filter bypass, the error-code prefix reconciliation is #346's.

KEY PATTERNS:
- Always filter; no tenant means no rows; no option turns the filter off (#3197 removes the existing switch).
- Same named filter key so the soft-delete filter coexists; no non-public reflection.
- Shared [Collection] fixtures for the integration tests; EF Core x3 only, the other seven providers already filter in their repositories.

REFERENCE FILES:
- src/Encina.EntityFrameworkCore/Tenancy/TenantDbContext.cs
- src/Encina.EntityFrameworkCore/Configuration/EntityConfigurationExtensions.cs
- tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/SqlServer/Tenancy/TenancyEFSqlServerTests.cs
- docs/features/multi-tenancy.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | The extension runs at model build; cache keys already carry the tenant (`DefaultCacheKeyGenerator`) and the injective segment is #1257's. |
| 2 | OpenTelemetry | ❌ | Nothing runs per request; the tenant tag helper is #2676. |
| 3 | Structured Logging | ❌ | The extension runs once per model build; no `[LoggerMessage]` and no EventId range. |
| 4 | Health Checks | ❌ | No dependency to check. |
| 5 | Validation | ✅ | The filter compares every query with the tenant of the context; the tenant-id rules are #1257's. |
| 6 | Resilience | ❌ | No external call. |
| 7 | Distributed Locks | ❌ | No shared state is updated. |
| 8 | Transactions | ❌ | A query filter takes part in the transaction of the context unchanged. |
| 9 | Idempotency | ❌ | A pure model-build function; calling it twice is a no-op. |
| 10 | Multi-Tenancy | ✅ | This feature. |
| 11 | Module Isolation | ❌ | `ModuleId` filtering is #1636 and the modules family. |
| 12 | Audit Trail | ⏭️ | Access to other tenants' rows goes through the audited platform-operator path of #3748; the audit event for denied requests is the follow-up issue file `tenant-boundary-audit` (written with #304). |

---

## Prerequisites & Dependencies

| Prerequisite | State | Effect on this plan |
|--------------|-------|---------------------|
| #3197 (remove `UseQueryFilters`, `AutoAssignTenantId`, `ValidateTenantOnSave` from the EF Core tenancy options) | open | edits the lines of `TenantDbContext` that PR 385-A also edits; land in either order, the second rebases; the extension never reads an option |
| #1257 (tenant-id rules, injective cache segment, the two-tenant suite) | plan merged | the rules the guide cites; the suite adds the cross-store isolation check |
| #346, #338, #304 (gate, validation, ownership) | plans | the controls the guide maps |
| #3748 (platform-operator path) | open, `needs-decision` | the guide describes the bypass as application-owned until it lands |

**Sequencing**: 385-A, then 385-B and 385-C in any order.

---

## Next Steps

1. Maintainer answers Design Choices 1, 2 and 5 when work on the issue starts (recorded in `## Maintainer Decisions`).
2. Worker pull requests (counts include tests, manifests, PublicAPI lines and fragments; recount by grep when the worker starts):
   - **PR 385-A** (interface, extension, delegation, unit and guard tests): `ITenantFilterContext.cs`, `TenantModelBuilderExtensions.cs`, `TenantDbContext.cs` (3); `TenantModelBuilderExtensionsTests.cs`, `TenantModelBuilderExtensionsGuardTests.cs` (2); `Encina.EntityFrameworkCore.json`, `PublicAPI.Unshipped.txt`, `385-tenant-filter-any-context.added.md` (3): 8 files.
   - **PR 385-B** (integration tests and justifications): `PlainTenantFilterDbContext.cs`, three provider test classes (4); `Tenancy.md` (property), `TenantFilter.md` (load, benchmark) (3): 7 files.
   - **PR 385-C** (documentation): `docs/features/multi-tenancy-security.md`, `docs/features/multi-tenancy.md`, `src/Encina.EntityFrameworkCore/README.md`, and `docs/INVENTORY.md` if it lists pages: 3 to 4 files.
3. Link this plan from #385; the note on the closed-out items of the issue goes in the same comment.
