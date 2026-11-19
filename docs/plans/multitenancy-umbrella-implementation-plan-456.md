# Implementation Plan: Multi-Tenancy Support (umbrella): the item-by-item map and the tenant log scope

> **Issue**: [#456](https://github.com/dlrivada/Encina/issues/456)
> **Type**: Feature (an umbrella whose text duplicates #346 and #385; most of it is delivered under other names, and what is open is owned by the issues named below)
> **Complexity**: Low (3 phases of code and documentation, 2 pull requests, about 12 file touches)
> **Estimated Scope**: ~120 lines of production code + ~600 lines of tests and documentation
> **Related plans** (cited by issue number, not by link): #346 (tenant gate and the item map of the multi-tenancy core), #385 (EF filter for any context and the isolation security guide), #1257 (shared tenant-scope primitive, tenant cache key, default-tenant rule, two-tenant suite), #737, #738, #760 (tenant columns of the messaging rows), #125 (messaging dispatch guard), #338 and #304 (tenant existence and ownership), #798, #845, #752 (tenancy of audit, AI Act and configuration audit), #711 (tenant connection cache), #2676 (tenant telemetry helper)

---

## Summary

#456 asked for a new package `Encina.MultiTenancy` with resolution strategies, data isolation strategies, tenant-aware outbox, saga and inbox stores, a tenant-aware cache key, cross-tenant admin operations and tenant telemetry. Almost all of it exists under other names or is owned by another issue with its own plan. This plan does two things: it records, item by item, **what is done, who owns what is not, and what is deliberately not built**, so that the umbrella can be closed without losing an item; and it plans the **one item no other issue owns**: the tenant in the **log scope** ("TenantId in all log scopes", function 3 of the issue's own cross-cutting table).

**State of `origin/main` (`f484cdf4f`), verified by reading and grep.** There is no `Encina.MultiTenancy` package: the packages are `Encina.Tenancy` and `Encina.Tenancy.AspNetCore` plus the `Tenancy/` folders of the EF Core, Dapper, ADO.NET and MongoDB packages; #357 (the same request) was closed as not planned. A grep for `BeginScope` in `src` finds no log scope that carries a tenant (the only ones are the idempotency outcome holder of the #334 plan and the slice scope of #365, neither a tenant scope).

| Item of #456 | State | Evidence or owner |
|--------------|-------|-------------------|
| `Encina.MultiTenancy` NuGet package | Not built, by design | `Encina.Tenancy` exists (`src/Encina.Tenancy`); #357 closed as not planned; Design Choice 1 |
| `AddEncinaMultiTenancy` with `TenantResolution`, `TenantHeader`, `IsolationLevel`, `IsolateOutbox`, `IsolateSagas` | `AddEncinaTenancy(TenancyOptions)` and `AddEncinaTenancyAspNetCore()`; the per-pattern isolation switches are not built | Design Choice 2 |
| Header, Subdomain, Route, Claim resolvers | Done | `src/Encina.Tenancy.AspNetCore/Resolution/Resolvers/` (`HeaderTenantResolver`, `SubdomainTenantResolver`, `RouteTenantResolver`, `ClaimTenantResolver`), `TenantResolverChain` |
| `ITenantResolver` for custom resolution | Done | `src/Encina.Tenancy.AspNetCore/Resolution/ITenantResolver.cs` |
| Row, Schema, Database isolation | Done | `TenantIsolationStrategy` (`SharedSchema`, `SchemaPerTenant`, `DatabasePerTenant`), `TenantConnectionFactoryBase`, `ITenantSchemaConfigurator`, `TenantDbContext` |
| EF Core query filter on the entities | Done for `TenantDbContext` descendants; any other context is #385 | `src/Encina.EntityFrameworkCore/Tenancy/TenantDbContext.cs`; #385 plan |
| `TenantAwareOutboxStore`, `TenantAwareInboxStore`, `TenantAwareSagaStore` wrappers | Replaced by typed tenant columns and strict tenant queries on the stores themselves | #737 (outbox), #760 (inbox), #738 (saga), #739 (scheduled message); the dispatch guard is #125 |
| Tenant-aware cache key | Done: `DefaultCacheKeyGenerator` prefixes `t:{escaped tenant}` when the context has a tenant; the injective `TenantCacheKey` and the fail-closed rule are #1257 | `src/Encina.Caching/KeyGeneration/DefaultCacheKeyGenerator.cs`; #1257 plan |
| Cross-tenant admin operations (`encina.AsTenant(null)`) | Not built, by design | Design Choice 4; the audited platform-operator path is #3748 |
| Tenant dimension on traces | #2676 (ADR-054 D4) | open |
| **TenantId in all log scopes** | **Missing: this plan** | Phases 1 to 3 |
| Tenant existence and ownership validation | #338, #304 | open, plans on `main` |
| Tenant-specific configuration, audit trail, validation of the tenant configuration | #1257 (fail-closed configuration lookup), #798 and #752 (audit rows), #1257 Phase 1 (options validator) | plans on `main` |
| Unit tests with more than 85% coverage | Replaced by the per-flag model of `AGENTS.md` section 9 | each owner's manifest targets |

**Not in scope**: every row above that names another issue; a persistent tenant store (#596, #598); the tenant connection cache (#711); cache quotas and policies per tenant (#275); a `Encina.MultiTenancy` package.

**Affected packages**: `Encina.Tenancy` (the log scope behavior, one option, registration).

**Provider category**: none (provider-independent; the tenant providers of the data layer are owned by the issues above).

---

## Design Choices

<details>
<summary><strong>1. A new package `Encina.MultiTenancy` (Settled)</strong></summary>

**Class:** Settled
**Settled by:** #357, the same request (`[FEATURE] Multi-Tenancy Support (Encina.MultiTenancy)`), closed as not planned; the item map of #346 and #385 (the package that exists is `Encina.Tenancy`); `AGENTS.md` section 1 (clean architecture, no duplicate or legacy code).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) No new package: `Encina.Tenancy` and `Encina.Tenancy.AspNetCore` stay the multi-tenancy packages and this plan adds to them** | One package, already referenced by the data-layer packages and documented in `docs/features/multi-tenancy.md` | The issue's package name is not created |
| **B) Create `Encina.MultiTenancy` and move or duplicate the existing code** | Matches the issue text | A second package for the same concern, renamed types and every reference changed, with no new capability |

### Chosen Option: **A — extend `Encina.Tenancy`**

### Rationale

- Only A leaves one multi-tenancy package; B adds a parallel mechanism and breaks the plans of #346, #385 and #1257, which name `Encina.Tenancy`.

</details>

<details>
<summary><strong>2. Per-pattern isolation switches such as `IsolateOutbox` and `IsolateSagas` (Settled)</strong></summary>

**Class:** Settled
**Settled by:** `AGENTS.md` section 3 (compliance and security gates fail closed; the only opt-out is explicit and logged), SPEC-002 DEC-006 and AC-043, and the maintainer principle of 2026-10-30 recorded in #3111 (no control is disabled by configuration or tier).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) No switch: when tenancy is registered, every tenant-aware store isolates, and the only opt-out is the explicit, logged `[TenantAgnostic]` of #346** | Isolation cannot be left off by a forgotten flag | An application that wants an unscoped outbox in a multi-tenant host must mark its notification types |
| **B) Booleans `IsolateOutbox` and `IsolateSagas` (the issue text)** | Pay-for-what-you-use per pattern | A deployment can disable isolation of one store silently: fail open |

### Chosen Option: **A — no per-pattern switches**

### Rationale

- Only A complies with the fail-closed rule. Pay-for-what-you-use is met one level up: tenancy itself is opt-in (`AddEncinaTenancy`); once on, isolation is not negotiable.

</details>

<details>
<summary><strong>3. The tenant in the log scope (Dilemma D1)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A pipeline behavior in `Encina.Tenancy` opens an `ILogger.BeginScope` with the key `encina.tenant_id` (the name of the telemetry tag of ADR-054 D4) for the whole dispatch, only when multi-tenancy is on and the request context has a non-blank tenant; `TenancyOptions.IncludeTenantInLogScope` (default true) switches it off explicitly** | Every log line of a request, from any package, carries the tenant without each package adding it (the issue's text); consistent with the span attribute and metric tag the maintainer accepted in ADR-054 D4 | The tenant id may identify a customer that is a natural person (the reference application's tenants are professional practices), so it reaches every log sink |
| **B) Off by default (`IncludeTenantInLogScope = false`)** | Data minimisation by default (GDPR Art. 5(1)(c) and 25) | Operators must turn it on; the default log stream cannot be filtered per tenant |
| **C) No tenant in any log; operators correlate through the trace id** | No personal-data risk in logs | Contradicts the issue's text; log-only deployments cannot isolate a tenant's incident |

### Chosen Option: **A — on when multi-tenancy is on, with an explicit off switch** (recommended, pending the maintainer)

### Rationale

- A wins when the same data is already on spans and metrics by decision (ADR-054 D4: the tenant is an attribute and a tag when multi-tenancy is on) and operations need per-tenant log filtering; B wins when the deployment's tenants are natural persons and the data protection officer wants logs free of identifiers until enabled; C wins for a log-averse deployment that has traces. The scope is a structured key, not a message fragment, so a sink can drop it. The sibling plans (#798, #845) log error codes only and never a tenant value in their own messages; this scope is the single, switchable place where a tenant reaches logs, and it carries only the id, never the tenant name or any other `TenantInfo` field (the connection string is never in scope).

</details>

<details>
<summary><strong>4. Cross-tenant administration and `AsTenant(null)` (Settled)</strong></summary>

**Class:** Settled
**Settled by:** `AGENTS.md` section 3 (gates fail closed when context is missing; the only opt-out is explicit and logged), SPEC-002 DEC-006, and #3271 (closed: `AllowCrossTenantQueries` removed so the ABAC decision audit reader fails closed; the audited, permission-gated platform-operator read is #3748).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Not built here: a cross-tenant read exists only as an audited platform-operator permission with a mandatory reason (#3748), never as an ambient `AsTenant(null)` switch** | Fail closed; every cross-tenant access is attributable | The issue's `using (encina.AsTenant(null))` example is not provided |
| **B) `AsTenant(null)` as in the issue** | One line for an admin | A `null` tenant becomes "all tenants" for any caller: fail open |

### Chosen Option: **A — the audited operator path only**

### Rationale

- Only A complies; the item stays with #3748 (open).

</details>

<details>
<summary><strong>5. Where the scope is opened (Dilemma D2)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A transient pipeline behavior `TenantLogScopeBehavior<TRequest, TResponse>` in `Encina.Tenancy`, `[PipelineStage(PipelineStage.Observability, Order = 5)]` (the outermost observability position, so every inner behavior and handler logs inside the scope), registered by `AddEncinaTenancy` through `TryAddEnumerable`** | Covers HTTP, background dispatch (the outbox, inbox and scheduler dispatch through the pipeline, with the tenant restored by #1164) and gRPC bridges; one place | Streams (`IStreamPipelineBehavior`) are not covered: a scope held across `yield` is fragile (Design Choice 6) |
| **B) In `TenantResolutionMiddleware` (`Encina.Tenancy.AspNetCore`)** | Wraps the whole HTTP request | HTTP only: background and bridged work logs no tenant |
| **C) In `ITenantProvider`** | Close to the tenant | A provider is a read abstraction; it has no scope lifetime |

### Chosen Option: **A — a pipeline behavior at the outermost Observability position** (recommended, pending the maintainer)

### Rationale

- A wins because the pipeline is the one place every dispatch passes, background included; B wins for an application that only needs HTTP logs and wants no pipeline cost. The scope keys are `encina.tenant_id` only; the behavior never throws (a blank tenant opens no scope), never logs, and disposes the scope in a `finally`.

</details>

<details>
<summary><strong>6. Stream requests (Dilemma D3)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Not covered now; the guide says so and a follow-up issue file records it** | No fragile scope across `yield` | Logs of a stream handler carry no tenant scope |
| **B) A `TenantLogScopeStreamBehavior` that opens the scope around the enumeration** | Complete coverage | The scope would stay open while the consumer iterates, including code outside Encina; ordering with disposal on early exit needs its own design |

### Chosen Option: **A — not covered now** (recommended, pending the maintainer)

### Rationale

- A wins until a stream handler needs tenant-filtered logs; B wins if streams are a main entry point of the reference application. The follow-up issue file is `artifacts/issues/tenant-log-scope-streams.md`.

</details>

---

## Implementation Phases

### Phase 1: The Log Scope Behavior

> **Goal**: The behavior, its option and its registration (PR 1).

<details>
<summary>Tasks</summary>

1. **Create `src/Encina.Tenancy/Logging/TenantLogScopeBehavior.cs`**: `[PipelineStage(PipelineStage.Observability, Order = 5)] public sealed class TenantLogScopeBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>`; constructor `(ILogger<TenantLogScopeBehavior<TRequest, TResponse>> logger, IOptions<TenancyOptions> options)`; `Handle` reads `context.TenantId`; when the option is on and the id is not null, empty or white space it opens `logger.BeginScope(new KeyValuePair<string, object?>[] { new("encina.tenant_id", tenantId) })` in a `using` around `nextStep()`; otherwise it calls `nextStep()` directly. No logging, no exception, no allocation when there is no tenant.
2. **Modify `src/Encina.Tenancy/Configuration/TenancyOptions.cs`**: `public bool IncludeTenantInLogScope { get; set; } = true;` (and the `ToString()` of the options, which exists, lists it); `AddEncinaTenancy` copies it like the other properties.
3. **Modify `src/Encina.Tenancy/ServiceCollectionExtensions.cs`**: `services.TryAddEnumerable(ServiceDescriptor.Transient(typeof(IPipelineBehavior<,>), typeof(TenantLogScopeBehavior<,>)))`; a behavior registered by the application is never overridden.
4. `PublicAPI.Unshipped.txt` of `Encina.Tenancy` (the type, its constructor, `Handle`, the option getter and setter).

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```
CONTEXT:
- Issue #456; plan docs/plans/multitenancy-umbrella-implementation-plan-456.md. Dilemmas D1-D3 are recommended, pending the maintainer: follow the Maintainer Decisions section if it exists.
- Encina is pre-1.0: no [Obsolete], no compatibility layers. The tenant of a request is IRequestContext.TenantId.

TASK:
Create TenantLogScopeBehavior, add TenancyOptions.IncludeTenantInLogScope and register the behavior in AddEncinaTenancy.

KEY RULES:
- The scope key is encina.tenant_id and carries the id only (never the tenant name or connection string).
- The behavior never throws and never logs; a null, empty or white-space tenant opens no scope.
- The behavior is transient and holds no scoped service; TryAddEnumerable keeps an application's own registration.
- Zero warnings; PublicAPI.Unshipped.txt updated.

REFERENCE FILES:
- src/Encina.Tenancy/ServiceCollectionExtensions.cs
- src/Encina.Tenancy/Configuration/TenancyOptions.cs
- src/Encina/Pipeline/Ordering/PipelineStage.cs
- src/Encina.Compliance.ProcessorAgreements/ProcessorValidationPipelineBehavior.cs (behavior shape)
</details>
```

</details>

---

### Phase 2: Testing

> **Goal**: Per-flag coverage at the manifest targets (PR 1).

<details>
<summary>Tasks</summary>

1. **Unit** (`tests/Encina.UnitTests/Tenancy/TenantLogScopeBehaviorTests.cs`, new): with a recording logger provider, a request with tenant `T` logs inside the scope with `encina.tenant_id = T`; no tenant, empty and white-space tenants open no scope; the option off opens no scope; `nextStep` runs exactly once in every row; an exception from `nextStep` still disposes the scope; two concurrent requests with two tenants never see each other's scope (`AsyncLocal` isolation).
2. **Guard** (`tests/Encina.GuardTests/Tenancy/`): the constructor (null logger, null options) and `Handle` argument checks; a null `IRequestContext` is rejected the way the sibling behaviors reject it.
3. **DI** (edit `tests/Encina.UnitTests/Tenancy/` or the existing `TenancyServiceCollectionExtensions` test): the provider built with `ValidateOnBuild` and `ValidateScopes` resolves the behavior; an application registration of the same type is kept.
4. **Options** (edit of the `TenancyOptions` test): the default is true and the property round trips.
5. **Contract, Property, Integration**: contract has no provider family; property has no complex logic; integration needs no external service (the behavior is exercised against a real `ILogger` provider in the unit project): justified, none created.
6. **Benchmark**: justification `tests/Encina.BenchmarkTests/Tenancy/TenantLogScope.md` (one scope allocation per tenant request on a thin wrapper; measured by the existing request pipeline benchmarks, not worth a benchmark of its own).
7. **Per-file targets** (in `.github/coverage-manifest/Encina.Tenancy.json`, one-sentence justifications written there; measured with `dotnet test tests\Encina.<Flag>Tests --collect "XPlat Code Coverage"` and `.github/scripts/coverage-report.cs`; the existing targets of `TenancyOptions.cs` (unit 100, guard 85) and `ServiceCollectionExtensions.cs` (unit 95, guard 70) are held, never lowered):

   | File | Unit | Guard | Contract | Property | Integration | Justification |
   |---|---|---|---|---|---|---|
   | `Logging/TenantLogScopeBehavior.cs` (new) | 100 | 90 | 0 | 0 | 0 | Four branches (tenant, blank, option off, exception) are all unit cases; guard reaches the constructor and `Handle` checks; no other flag has reachable lines |
   | `Configuration/TenancyOptions.cs` | 100 | 85 | 0 | 0 | 0 | Held at the current targets; the new property is read and set in tests |
   | `ServiceCollectionExtensions.cs` | 95 | 70 | 0 | 0 | 0 | Held at the current targets; the new registration line is asserted by the DI test |

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```
CONTEXT:
- Issue #456. Phase 1 is implemented. AGENTS.md section 9 per-flag coverage model; guard tests for every new public constructor and method (#3798).

TASK:
Write the tests of Phase 2 Tasks 1-6 and the manifest targets of Task 7.

KEY RULES:
- Tests execute real code with a real logger provider; Shouldly via Encina.Testing.Shouldly; no Thread.Sleep.
- A nullable parameter that accepts null gets a test of the valid result, not an exception test.

REFERENCE FILES:
- tests/Encina.GuardTests/Tenancy/TenancyServiceCollectionExtensionsGuardTests.cs
- .github/coverage-manifest/Encina.Tenancy.json
- tests/Encina.BenchmarkTests/Tenancy.md
```

</details>

---

### Phase 3: Documentation & Finalization

> **Goal**: The item map is published, so the umbrella can be closed (PR 2).

<details>
<summary>Tasks</summary>

1. `docs/features/multi-tenancy.md` (through `docs-writer`): a section "Tenant in the log scope" (the key, the option, the privacy consequence, the streams limit) and the item map of this plan as a table "What replaced the #456 proposal".
2. `changelog.d/456-tenant-log-scope.added.md` (through `mechanical-fixer`): one bullet naming the log scope and the option.
3. XML docs on every new public member; `dotnet build --configuration Release` with 0 warnings; `dotnet test` with every flag at its target.
4. After PR 1 merges, the orchestrator posts the item map on #456 and the maintainer closes it; the open items stay with their owners.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```
CONTEXT:
- Issue #456; the behavior and its tests are merged.

TASK:
Write the guide section and the changelog fragment through the specialists.

KEY RULES:
- Pages under docs/ go through docs-writer; fragments through mechanical-fixer; coverage figures are cited with covref markers, never typed.
- Public text never names a private client application.

REFERENCE FILES:
- docs/features/multi-tenancy.md
- changelog.d/README.md
```

</details>

---

### Proposed split into pull requests (limit 15 files and 1500 lines each)

| PR | Content | Approx. files | Approx. lines |
|----|---------|---------------|---------------|
| 1 | `Encina.Tenancy`: `TenantLogScopeBehavior`, `TenancyOptions`, `ServiceCollectionExtensions`, PublicAPI, manifest, unit tests, guard test, DI test edit, options test edit, benchmark justification | 10 | 550 |
| 2 | Documentation: guide section with the item map, changelog fragment | 2 | 150 |

A pull request that measures over 15 files or 1,500 lines when it is briefed is split again.

---

## Research

### Relevant Standards & Specifications

| Reference | What it defines | Use in this feature |
|-----------|-----------------|---------------------|
| ADR-054 D4 | Tenant as span attribute and metric tag only when multi-tenancy is on; no baggage | The key name and the default of the log scope |
| GDPR Art. 5(1)(c) and 25 | Data minimisation and data protection by design | Why the log scope has an explicit off switch (Design Choice 3) |
| SPEC-002 DEC-006, AC-043 | Gates fail closed | Design Choices 2 and 4 |
| `Microsoft.Extensions.Logging` scopes | Structured scopes with key-value pairs | The scope shape |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `AddEncinaTenancy`, `TenancyOptions`, `MultiTenancyMarker` | `src/Encina.Tenancy/` | Registration and option |
| `IRequestContext.TenantId` | `src/Encina/Abstractions/IRequestContext.cs` | The tenant of the dispatch |
| `PipelineStage.Observability` | `src/Encina/Pipeline/Ordering/PipelineStage.cs` | The outermost stage |
| `TenancyActivityEnricher`, `ActivityTagNames.Tenancy` | `src/Encina.OpenTelemetry/` | The span side of the same data (the helper is #2676) |
| `docs/features/multi-tenancy.md` | `docs/features/` | The guide that gets the section |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Tenancy` | none | A log scope is not a log message and the behavior logs nothing; no `[LoggerMessage]` is added, so no id is allocated (the existing `Tenancy` range 1800-1899 is untouched) |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Production | 3 | Behavior, options, registration |
| Public API and manifest | 2 | `PublicAPI.Unshipped.txt`, `Encina.Tenancy.json` |
| Tests | 5 | Unit, guard, DI edit, options edit, benchmark justification |
| Documentation | 2 | Guide section, changelog fragment |
| **Total** | **12 across 2 pull requests** | The sum of the split rows is the authority |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
PROJECT CONTEXT:
- Encina is a pre-1.0 .NET 10 / C# 14 library; Either<EncinaError, T>; no [Obsolete], no compatibility layers.
- Issue #456 is an umbrella: everything but the tenant log scope is owned by other issues (the map is in the plan). Plan: docs/plans/multitenancy-umbrella-implementation-plan-456.md; Dilemmas D1-D3 are recommended, pending the maintainer.

IMPLEMENTATION OVERVIEW (2 pull requests):
1. TenantLogScopeBehavior, TenancyOptions.IncludeTenantInLogScope and the registration in Encina.Tenancy, with unit, guard, DI and options tests (Phases 1 and 2).
2. The guide section with the item map and the changelog fragment (Phase 3).

KEY PATTERNS:
- Scope key encina.tenant_id, id only; only when a tenant exists and the option is on; never throws, never logs.
- Transient behavior, TryAddEnumerable, PipelineStage.Observability Order 5.
- Tests: real logger provider, Shouldly, per-flag coverage from the manifest, guard tests for new public API.

REFERENCE FILES:
- src/Encina.Tenancy/ServiceCollectionExtensions.cs
- src/Encina.Tenancy/Configuration/TenancyOptions.cs
- docs/features/multi-tenancy.md
- docs/architecture/adr/054-observability-conventions-and-trace-propagation.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ⏭️ | Deferred: tenant-scoped cache policies and quotas are #275 (open); tenant cache keys already exist and the injective rule is #1257 |
| 2 | OpenTelemetry | ⏭️ | Deferred: the tenant attribute and metric tag on every package is #2676 (open) under ADR-054 D4; the log scope of this plan uses the same key |
| 3 | Structured Logging | ✅ | Phases 1 and 2: the `encina.tenant_id` scope; no `[LoggerMessage]` and no EventId is added |
| 4 | Health Checks | ⏭️ | Deferred: per-tenant data source health has no issue yet; `TenantHealthCheck` exists for the tenant store and an issue is opened if the maintainer wants it (not planned here) |
| 5 | Validation | ⏭️ | Deferred: the tenant configuration and resolution validation is #1257 Phase 1 and #338 |
| 6 | Resilience | ⏭️ | Deferred: per-tenant circuit breakers have no issue; per-tenant rate limiting is #740 |
| 7 | Distributed Locks | ⏭️ | Deferred: tenant-scoped lock names are #741 (plan on `main`) |
| 8 | Transactions | ❌ | Not applicable: tenant connections and transactions are delivered by `ITenantConnectionFactory` and the provider packages; nothing is added |
| 9 | Idempotency | ⏭️ | Deferred: tenant-scoped idempotency keys are #760 (inbox) and #3437 |
| 10 | Multi-Tenancy | ✅ | The feature itself; this plan maps every item to its owner |
| 11 | Module Isolation | ⏭️ | Deferred: module and tenant together are #747 and #761 |
| 12 | Audit Trail | ⏭️ | Deferred: tenant on audit entries is #798 and #752 (plans on `main`) |

---

## Prerequisites & Dependencies

- None blocking: the behavior needs only `IRequestContext.TenantId` and `IPipelineBehavior`.
- The owners named in the item map keep their own prerequisites; this plan neither waits for them nor changes them.

---

## Next Steps

1. The maintainer answers D1 to D3 when work on #456 starts (D1 is the privacy default of the log scope); the answers go into `## Maintainer Decisions` and the markers are removed.
2. Brief pull request 1; after it merges, post the item map on #456 so the umbrella can be closed.
