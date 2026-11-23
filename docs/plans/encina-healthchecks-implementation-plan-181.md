# Implementation Plan: `Encina.HealthChecks` Package Question and the Typed `DbContext` Check

> **Issue**: [#181](https://github.com/dlrivada/Encina/issues/181)
> **Type**: Feature (observability, health checks)
> **Related**: #622 (umbrella of the remaining health checks, owns health telemetry), #454 (probe endpoints and tag constants), #754 (the six remaining cache checks), #2396 (processor liveness), #907 (health-check documentation), #906 (observability guide), ADR-018 (cross-cutting integration), ADR-054 (settled point 1 and D4), ADR-062
> **Complexity**: Low (most of the issue exists; the real work is one missing check and an honest closing map)
> **Estimated Scope**: ~110 lines of production code + ~380 lines of tests + 2 documentation files, in one code pull request of 11 files and one documentation pull request of 3 files
> **Milestone**: v0.14.0 Hardening

---

## Summary

### What exists today (read on 2026-11-23, main `237a83364`)

The issue (2026-era text, `status:ready`) asks for a new `Encina.HealthChecks` package. Almost everything it lists already lives in other packages. Verified against `main`:

| Ask of the issue | Finding on `main` | Owner |
|------------------|-------------------|-------|
| `OutboxHealthCheck`, pending monitoring | Exists: `src/Encina.Messaging/Health/OutboxHealthCheck.cs` with `OutboxHealthCheckOptions` (pending and exhausted warning and critical thresholds, `OutboxOptions.MaxRetries` separates them) and a data dictionary in the result | Done |
| `SagaHealthCheck`, stuck sagas | Exists: `SagaHealthCheck.cs`, `SagaHealthCheckOptions.StuckSagaThreshold` (30 minutes), `SagaWarningThreshold`, `SagaCriticalThreshold` | Done |
| `ScheduledMessageHealthCheck`, overdue messages | Exists as `SchedulingHealthCheck.cs`, `SchedulingHealthCheckOptions.OverdueWarningThreshold` and `OverdueCriticalThreshold` | Done |
| `InboxHealthCheck`, stuck message detection | The check exists but only proves connectivity: it calls `IInboxStore.GetExpiredMessagesAsync(batchSize: 1)` and reports `expired_sample`. `IInboxStore` has no count, backlog or failed-message query, so stuck detection needs a new store method on 10 providers | Design Choice 4 and the follow-up issue file `artifacts/issues/inbox-store-backlog-query.md` |
| `CacheHealthCheck`, connectivity | `QueryCacheHealthCheck` (`src/Encina.Caching/Health`) and `RedisHealthCheck` exist; the six other providers are #754 | #754 |
| `DatabaseHealthCheck<TContext>`, EF Core context health | **Gap.** `EntityFrameworkCoreHealthCheck` resolves the one `DbContext` alias (`GetRequiredService<DbContext>()`), and `AddEncinaEntityFrameworkCore<TDbContext>` registers that alias with `TryAddScoped<DbContext>` (`src/Encina.EntityFrameworkCore/ServiceCollectionExtensions.cs:165`), so only the first context of a host with several is ever checked | This plan, Phase 1 |
| Kubernetes probe helpers | Absent today | #454 |
| Health check UI integration | The Encina checks reach the host as framework `IHealthCheck` through `EncinaHealthCheckAdapter` (`src/Encina.AspNetCore/Health`), so any response writer applies; the Encina JSON writer is #454's | #454; documented here (Design Choice 1) |
| Health results as OpenTelemetry metrics | No per-check metric exists; one publisher with a per-check gauge is designed in the plan of #622 (its Design Choice 4) | #622 |
| Configurable thresholds, health data in the response | Exist in the options and result dictionaries above | Done |
| Documentation | `docs/guides/health-checks.md` exists; the page rewrite is #907 | #907 |

### What the issue proposes and what changed since

The issue's `AddEncinaHealthChecks(config => { config.AddOutboxHealthCheck(); ... })` DSL duplicates `AddEncinaHealthChecks`, `AddEncinaOutbox`, `AddEncinaInbox`, `AddEncinaSaga`, `AddEncinaScheduling` and `AddEncinaQueryCache` in `src/Encina.AspNetCore/Health/HealthCheckBuilderExtensions.cs`, which already take the options and tags of each check; its threshold names (`MaxPendingThreshold`, `StuckThreshold`) are replaced by the real option names above. A new package would have to reference every package whose check it registers, or hold only the base types, which already sit in `Encina.Messaging/Health` (`IEncinaHealthCheck`, `EncinaHealthCheck`, `ProviderHealthCheckOptions`). The same package question is held as a Dilemma by the plans of #622 and #454; this plan answers it for #181 and says what follows from each answer.

### What this plan delivers

- `DbContextHealthCheck<TDbContext>` and `AddEncinaDbContextHealthCheck<TDbContext>(Action<ProviderHealthCheckOptions>?)` in `Encina.EntityFrameworkCore`: one check per registered context, each with its own name, tags and timeout (Phase 1).
- The closing map of #181: every checkbox of the issue is closed by code, by a named sibling or by a follow-up issue file (Phase 2, documentation, and the table above).
- Shared pieces and owner: `ProviderHealthCheckOptions` and the tag convention are #754's; probe tags and the endpoint mapping are #454's; the health publisher is #622's. This plan creates no shared type.
- Affected packages: `Encina.EntityFrameworkCore`. No new package under the recommended Design Choice 1.
- Provider category: database. The check is EF Core level and runs on the three EF providers (SqlServer, PostgreSQL, MySQL); the other seven database providers (ADO.NET x3, Dapper x3, MongoDB) already have their provider check (`SqlServerHealthCheck`, `PostgreSqlHealthCheck`, `MySqlHealthCheck` in both ADO and Dapper packages, and `MongoDbHealthCheck`).
- Not delivered, with the reason: a new package (Design Choice 1); inbox stuck detection (Design Choice 4); tenant-scoped database health (Design Choice 3); health metrics (#622); probes (#454).

---

## Design Choices

<details>
<summary><strong>1. Is there a new <code>Encina.HealthChecks</code> package</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) No new package: each check stays in the package of its dependency; the host registers them through the existing `HealthCheckBuilderExtensions`; #181 closes after Phase 1 and Phase 2** | No third location for health code; no reference cycle; no move of about 40 checks; the base types stay where the checks already find them | Discoverability depends on the tags, the guide (#907) and the README |
| **B) A new `Encina.HealthChecks` package that registers every check** | One entry point and one place in NuGet search | It must reference every package that owns a check (database providers, caches, transports), so installing it pulls them all in, against pay-for-what-you-use (`AGENTS.md` section 1); or it holds only the base types, which already live in `Encina.Messaging` |
| **C) A new package holding only the base types and the ASP.NET Core wiring (a move out of `Encina.Messaging` and `Encina.AspNetCore`)** | A clean name for the health vocabulary | A rewrite of dozens of `using` lines and registrations with no behavior gain; ADR-054 settled point 6 asks for complete changes, so it cannot be a half-move |

### Chosen Option: **A - no new package** (recommended, pending the maintainer)

### Rationale

- A matches the recommendation of the plans of #622 (Design Choice 1) and #454 (Design Choice 1); the three answers must be given together.
- B wins when one package must be the only dependency of an application that wants every check, for example a platform team shipping a base image; C wins when the maintainer wants the health vocabulary independent of messaging. If the maintainer picks B or C, the ADR number is assigned when the ADR pull request opens, the package needs its own coverage manifest, `PublicAPI` files and README, and Phase 1 moves to it.
- Health UI integration (an issue task) is closed by documentation: the Encina checks are framework `IHealthCheck` instances, so `AspNetCore.HealthChecks.UI.Client` response writers work unchanged, and the Encina JSON writer is #454's. No UI package or adapter is built here.
- Class: Dilemma

</details>

<details>
<summary><strong>2. Shape of the typed context check</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A new sealed `DbContextHealthCheck<TDbContext> : EncinaHealthCheck` plus `AddEncinaDbContextHealthCheck<TDbContext>` registering it as an `IEncinaHealthCheck`** | One check per context with a compile-time context type; the existing `EntityFrameworkCoreHealthCheck` is untouched; the options instance belongs to its own registration | One more public type |
| **B) Give the existing `EntityFrameworkCoreHealthCheck` a `Type` or a context factory and register it once per context** | No new type | The constructor of a public class changes and loses the compile-time type; the `DbContext` alias path and the typed path share one class |
| **C) Keep the alias only and document that the first registered context is the one checked** | No code | A host with an audit context and a main context cannot see the second one fail; the issue's `DatabaseHealthCheck<TContext>` stays unbuilt |

### Chosen Option: **A - a generic check and a registration extension** (recommended, pending the maintainer)

### Rationale

- A resolves `TDbContext` from a scope created per probe (never injected into the singleton check, so no scoped service lives in a singleton) and calls `Database.CanConnectAsync(linkedToken)`, with the timeout of `ProviderHealthCheckOptions.Timeout` (5 seconds by default) linked to the caller's token, the pattern of `EntityFrameworkCoreHealthCheck.CheckHealthCoreAsync`.
- C wins for the common application with one context, which is why the existing alias check stays as it is; B wins when the maintainer prefers fewer public types over a typed check.
- Names are injective: the default name is `encina-efcore-` plus the kebab-case `typeof(TDbContext).Name`, a caller-supplied `Name` overrides it, and registering two checks with the same name throws `InvalidOperationException` at registration, so two contexts never collapse into one report entry.
- Class: Dilemma

</details>

<details>
<summary><strong>3. Tenant-scoped database health</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Not here: a follow-up issue file `artifacts/issues/tenant-scoped-database-health.md` (a check per tenant for tenants with their own database)** | Keeps this plan to one PR; tenant resolution and per-tenant connection strings belong to the tenancy design, and a check that opens one connection per tenant needs a cap and a cardinality rule | The issue's deferred multi-tenancy row stays deferred, with an issue |
| **B) Build it here, enumerating tenants from `ITenantProvider` on every probe** | Closes the row | A probe whose cost grows with the number of tenants; ADR-054 D4 warns about unbounded tenant ids in telemetry and the health data dictionary would carry them |

### Chosen Option: **A - a follow-up issue** (recommended, pending the maintainer)

### Rationale

- A single probe that opens N tenant connections can itself take a deployment down; the follow-up designs the cap and the aggregation (one Degraded entry with a count, never one entry per tenant id).
- B wins for a small fixed set of tenant databases where per-tenant status in the probe output is the requirement.
- Class: Dilemma

</details>

<details>
<summary><strong>4. Inbox stuck-message detection</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Not here: a follow-up issue file `artifacts/issues/inbox-store-backlog-query.md` adding a backlog or failed-message count to `IInboxStore` on all 10 providers, then thresholds in `InboxHealthCheck`** | Honest size: the store method, 10 implementations, the in-memory store and the test doubles are one pull request of their own | The issue's "stuck message detection" is delivered later |
| **B) Add the store method and the 10 implementations in this plan** | Closes the task now | More than 15 files; the provider rule (`AGENTS.md` section 5) makes the 10 implementations a requirement, so a partial version is not allowed |

### Chosen Option: **A - a follow-up issue**

### Rationale

- `AGENTS.md` section 11 limits a worker's issue to one pull request of at most 15 files and 1500 lines, and section 5 requires all 10 providers for a store method; B cannot satisfy both, so A is the only option that complies.
- Class: Settled
- Settled by: `AGENTS.md` section 11 (pull request size) and section 5 (database provider rule)

</details>

### Register of defaults and semantics

| Id | Choice | Class | Basis |
|----|--------|-------|-------|
| C1 | Default tags of the typed check: `encina`, `database`, `efcore`, `ready`; never `live`. The same set as `EntityFrameworkCoreHealthCheck.DefaultTags` (`src/Encina.EntityFrameworkCore/Health/EntityFrameworkCoreHealthCheck.cs:20`), merged with the tags of the caller through `MergeTags` | Trivial | The existing check of the same package; `IEncinaHealthCheck.Tags` documentation |
| C2 | The check name is lowercase kebab-case, as the `IEncinaHealthCheck.Name` documentation asks; the default is `encina-efcore-<context>`; the failure result carries the exception type through the `EncinaHealthCheck` base class and never an exception message | Trivial | `src/Encina.Messaging/Health/IEncinaHealthCheck.cs`; `AGENTS.md` section 3 (messages never reach logs or health results) |
| C3 | Timeout and failure status come from `ProviderHealthCheckOptions` (`Timeout` 5 seconds, `FailureStatus` Unhealthy); each registration receives its own options instance built by a factory and the shared type is never registered as a service | Trivial | Family answer of the plans of #754, #755 and #622 (options per registration) |
| C4 | No new `EncinaError` code and therefore no ADR-062 `ErrorCategory`: the check returns `HealthCheckResult`, never an `Either` | Settled | ADR-062 (a category is chosen where an `EncinaError` is created; none is created) |
| C5 | No telemetry of its own: no span, no metric, no tenant tag and no tenant id in the result data; no tenant-qualified id is built, so the reserved `default` tenant segment does not apply | Settled | ADR-054 D4 (a tag only on telemetry that exists); the health publisher of #622 owns the per-check gauge |
| C6 | Fail closed: a context that cannot be resolved from the scope is a failed check (`FailureStatus`), never a silent Healthy; a host that registers a check for a context that is not registered learns it at the first probe through the exception type | Settled | `AGENTS.md` section 3 (gates fail closed; errors are never swallowed) |
| C7 | New public names: `DbContextHealthCheck<TDbContext>`, `AddEncinaDbContextHealthCheck<TDbContext>` | Dilemma (recommended, pending the maintainer) | Public surface; the alternative is a `Type` parameter on the existing class (Design Choice 2, option B) |

---

## Implementation Phases

### Phase 1: The Typed `DbContext` Check (pull request H1)

> **Goal**: Every registered context can be probed by its own check (Design Choice 2, option A; if the maintainer picks B or C this phase changes as that choice says).

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina.EntityFrameworkCore/Health/DbContextHealthCheck.cs`: `public sealed class DbContextHealthCheck<TDbContext> : EncinaHealthCheck where TDbContext : DbContext`; constructor `(IServiceProvider serviceProvider, ProviderHealthCheckOptions? options)` with `ArgumentNullException.ThrowIfNull(serviceProvider)`; the base call passes `options?.Name ?? DefaultNameFor<TDbContext>()` and `MergeTags(options?.Tags, DefaultTags)`. `CheckHealthCoreAsync` follows `EntityFrameworkCoreHealthCheck`: a `CancellationTokenSource` with `_options.Timeout` linked to the caller's token, `using var scope = _serviceProvider.CreateScope()`, `scope.ServiceProvider.GetRequiredService<TDbContext>()`, `await dbContext.Database.CanConnectAsync(linkedCts.Token)`; `true` returns Healthy; `false` and a timeout return Degraded when `_options.FailureStatus` is `Degraded` and Unhealthy otherwise, the rule `Encina.Messaging/Health/DatabaseHealthCheck.cs:143` applies (the existing EF check ignores `FailureStatus`; the typed check honors it); any other exception is reported by the base class (exception type only). The generic constraint is `where TDbContext : DbContext`; `DbContext` is a reference type, so no CS0452 arises.
2. **Create** `src/Encina.EntityFrameworkCore/Health/DbContextHealthCheckExtensions.cs`: `public static IServiceCollection AddEncinaDbContextHealthCheck<TDbContext>(this IServiceCollection services, Action<ProviderHealthCheckOptions>? configure = null) where TDbContext : DbContext`. It builds the options instance, applies `configure`, computes the final name, throws `InvalidOperationException` when an `IEncinaHealthCheck` descriptor with the same name was already added by this method (the extension keeps the names in a marker singleton, `DbContextHealthCheckNames`), and adds `services.AddSingleton<IEncinaHealthCheck>(sp => new DbContextHealthCheck<TDbContext>(sp, options))`. It does not register `DbContext` or the context; the host's own `AddDbContext<TDbContext>` does.
3. **Edit** `src/Encina.EntityFrameworkCore/PublicAPI.Unshipped.txt` through `mechanical-fixer` (the file is at the package root, not in a `PublicAPI` folder): the class, its constructor and the extension method (the default-name helper is `private`).
4. **Edit** `.github/coverage-manifest/Encina.EntityFrameworkCore.json` through `mechanical-fixer`: the two new entries with the per-file targets of the table in Research.
5. **Create** `tests/Encina.UnitTests/EntityFrameworkCore/Health/DbContextHealthCheckTests.cs`: the real check class driven with the dependency substitutes of the sibling `EntityFrameworkCoreHealthCheckTests` (NSubstitute for `DbContext`, `DatabaseFacade` and the scope factory; follow that file): a reachable context returns Healthy; an unreachable one returns Unhealthy, or Degraded when `FailureStatus` is `Degraded`; the timeout path (`CancellationTokenSource(TimeSpan)` does not use a `TimeProvider`, so the test passes a 1 millisecond timeout and a substitute whose `CanConnectAsync` waits on the token); a second context type has its own result; the failure data never contains an exception message.
6. **Create** `tests/Encina.UnitTests/EntityFrameworkCore/Health/DbContextHealthCheckRegistrationTests.cs`: a provider built with `ValidateOnBuild` and `ValidateScopes` that registers two contexts and two checks resolves both `IEncinaHealthCheck` instances; default names are `encina-efcore-<kebab>` and distinct; a duplicate name throws `InvalidOperationException`; the check is a singleton and resolves the context from a scope per probe (a scoped context never captured by the singleton).
7. **Create** `tests/Encina.GuardTests/EntityFrameworkCore/HealthChecks/DbContextHealthCheckGuardTests.cs`: constructor `serviceProvider` null throws `ArgumentNullException`; `options` null is valid and builds a check with the default name; `AddEncinaDbContextHealthCheck` with a null `services` throws; a null `configure` is valid (it registers the check with defaults).
8. **Edit** the three existing provider integration classes, one test each: `tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/SqlServer/Health/EntityFrameworkCoreHealthCheckSqlServerTests.cs` (collection `EFCore-SqlServer`), the PostgreSQL file (`EFCore-PostgreSQL`) and the MySQL file (`EFCore-MySQL`) under the sibling folders: with the shared fixture, a typed check reports Healthy against the real container and the check of a second context pointing at a closed port reports Unhealthy. Use the existing shared collection fixtures; no new fixture.
9. Add `changelog.d/181-typed-dbcontext-health-check.added.md` through `mechanical-fixer`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
Encina (.NET 10, C# 14, pre-1.0, no compatibility layers). Issue #181, plan docs/plans/encina-healthchecks-implementation-plan-181.md,
phase 1. EntityFrameworkCoreHealthCheck (src/Encina.EntityFrameworkCore/Health) checks only the DbContext alias that
AddEncinaEntityFrameworkCore registers with TryAddScoped (first context wins). Add a typed check for every other context.

TASK:
1. Create DbContextHealthCheck<TDbContext> (sealed, derives EncinaHealthCheck, where TDbContext : DbContext) and
   AddEncinaDbContextHealthCheck<TDbContext>(this IServiceCollection, Action<ProviderHealthCheckOptions>? configure = null).
2. Copy the timeout, scope-per-probe and CanConnectAsync pattern of EntityFrameworkCoreHealthCheck.CheckHealthCoreAsync.
3. Default name encina-efcore-<kebab context name>; a duplicate name throws InvalidOperationException at registration.
4. Add unit, registration (ValidateOnBuild and ValidateScopes), guard and three EF provider integration tests as the plan lists.
5. PublicAPI, the coverage manifest and the changelog fragment go through mechanical-fixer.

KEY RULES:
- Never inject a scoped service into the singleton check: create a scope per probe.
- Failure results carry the exception type or the error code only, never an exception message (AGENTS.md section 3).
- Edit source only with Edit/Write; no [Obsolete], no alias; tests use Shouldly through Encina.Testing.Shouldly.
- Integration tests use the shared [Collection("EFCore-...")] fixtures and ClearAllDataAsync in InitializeAsync.

REFERENCE FILES:
- src/Encina.EntityFrameworkCore/Health/EntityFrameworkCoreHealthCheck.cs
- src/Encina.Messaging/Health/EncinaHealthCheck.cs, ProviderHealthCheckOptions.cs
- tests/Encina.UnitTests/EntityFrameworkCore/Health/EntityFrameworkCoreHealthCheckTests.cs
- tests/Encina.GuardTests/EntityFrameworkCore/HealthChecks/EntityFrameworkCoreHealthCheckGuardTests.cs
```

</details>

---

### Phase 2: Documentation and the Closing Map (pull request D1)

> **Goal**: A reader finds where every check of the issue lives and how to register it, and #181 closes with a map instead of a new package.

<details>
<summary><strong>Tasks</strong></summary>

1. `docs/guides/health-checks.md` (through `docs-writer`, coordinated with #907): a section "Which package owns which check" with the table of this plan's Summary, the typed context check and the host-side use of a UI response writer.
2. `src/Encina.EntityFrameworkCore/README.md` (through `docs-writer`): the new registration and the name rule.
3. `docs/INVENTORY.md` (through `docs-writer`): the two new public types.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
Issue #181 plan docs/plans/encina-healthchecks-implementation-plan-181.md, phase 2, documentation only. Phase 1 merged.

TASK:
1. Update docs/guides/health-checks.md, the Encina.EntityFrameworkCore README and docs/INVENTORY.md as the plan lists.

KEY RULES:
- Use the encina-docs skill; no emojis; no hand-typed coverage figures; every identifier must exist in src/.
- Link siblings by issue number only (#622, #454, #754, #2396, #907); do not link a plan file that is not on main.

REFERENCE FILES:
- src/Encina.EntityFrameworkCore/Health/DbContextHealthCheck.cs, DbContextHealthCheckExtensions.cs
- src/Encina.AspNetCore/Health/HealthCheckBuilderExtensions.cs
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | Use in this feature |
|--------|-----------|---------------------|
| ASP.NET Core health checks | `IHealthCheck`, `HealthCheckRegistration` names unique per host, tags select probe sets, `Timeout` per registration | Why names must be injective and why the check carries its own timeout |
| `AGENTS.md` section 3 | Gates fail closed; errors are never swallowed; messages never reach logs | Register rows C2 and C6 |
| `AGENTS.md` section 5 | Database features implemented for all 10 providers | The EF check covers 3; the other 7 have their own check (Summary) |
| `AGENTS.md` section 6 and ADR-018 | The 12-function evaluation | The matrix below |
| ADR-054 D4 | Tenant tag only on telemetry that exists | Register row C5 |
| ADR-062 | Error category where an `EncinaError` is created | Register row C4 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `EntityFrameworkCoreHealthCheck` | `src/Encina.EntityFrameworkCore/Health/EntityFrameworkCoreHealthCheck.cs` | The pattern copied by the typed check |
| `EncinaHealthCheck`, `IEncinaHealthCheck`, `ProviderHealthCheckOptions` | `src/Encina.Messaging/Health/` | Base class, contract, options (`Timeout`, `Name`, `Tags`, `FailureStatus`) |
| `HealthCheckBuilderExtensions.AddEncinaHealthChecks` | `src/Encina.AspNetCore/Health/HealthCheckBuilderExtensions.cs:70` | Collects every registered `IEncinaHealthCheck`, so the typed checks are picked up with no change |
| `EncinaHealthCheckAdapter`, `CompositeEncinaHealthCheck` | `src/Encina.AspNetCore/Health/` | The bridge to framework `IHealthCheck` |
| `AddEncinaEntityFrameworkCore<TDbContext>` | `src/Encina.EntityFrameworkCore/ServiceCollectionExtensions.cs:136` | The alias registration the typed check complements |
| Existing EF health tests | `tests/Encina.UnitTests/EntityFrameworkCore/Health/`, `tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/{SqlServer,PostgreSQL,MySQL}/Health/` | Patterns and shared `[Collection("EFCore-...")]` fixtures |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| none | none | The check adds no log message: failures are reported by the `EncinaHealthCheck` base class (exception type only). No range is claimed and nothing is taken from the `10000+` zone |

### Estimated File Count

Every count includes tests, the coverage manifest, PublicAPI and the changelog fragment; each pull request builds and passes alone.

| PR | Content | New src | Changed src | New tests | Edited tests | Manifest | Other | Files | Lines (est.) |
|----|---------|---------|-------------|-----------|--------------|----------|-------|-------|--------------|
| H1 | Typed check and registration | 2 | 1 (`PublicAPI.Unshipped.txt`) | 3 | 3 (EF provider integration classes) | 1 | 1 (changelog) | 11 | ~520 |
| D1 | Documentation | 0 | 0 | 0 | 0 | 0 | 3 (guide, README, inventory) | 3 | ~120 |
| **Total** | | **2** | **1** | **3** | **3** | **1** | **4** | **14** | **~640** |

The pull request of this plan itself is one file. Under Design Choice 1 option B or C the count changes: a new package adds a project file, two `PublicAPI` files, a README, a manifest, the solution entry and three test-project references.

Per-file coverage targets (per flag, one sentence each), written to `.github/coverage-manifest/Encina.EntityFrameworkCore.json`; the changed methods reach full line coverage (CRAP gate):

| File | Unit | Guard | Contract | Property | Justification |
|------|------|-------|----------|----------|---------------|
| `Health/DbContextHealthCheck.cs` | 95 | 80 | 0 | 0 | Healthy, unreachable, timeout and second-context paths are driven with a real context; guard reaches the `serviceProvider` null check and the nullable options; a health check has no contract or property of its own beyond the `IEncinaHealthCheck` contract |
| `Health/DbContextHealthCheckExtensions.cs` | 95 | 85 | 0 | 0 | The registration test builds the provider with `ValidateOnBuild` and `ValidateScopes`; guard covers the null `services`, the nullable `configure` and the duplicate-name throw |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
You are implementing issue #181 - the Encina health check package question - in two small pull requests (H1 and D1).

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library; pre-1.0, no backward compatibility, best solution always; edit source only with the
  Edit/Write tools; PublicAPI, manifests and changelog fragments through mechanical-fixer; docs through docs-writer.
- The recommended answer (pending the maintainer) is that no new package is created: the health base types live in
  Encina.Messaging/Health, the registration in Encina.AspNetCore/Health, and every check in the package of its dependency.

IMPLEMENTATION OVERVIEW:
1. H1: DbContextHealthCheck<TDbContext> and AddEncinaDbContextHealthCheck<TDbContext> in Encina.EntityFrameworkCore, with unit,
   registration, guard and three EF provider integration tests.
2. D1: the closing map in docs/guides/health-checks.md, the EF README and the inventory.

KEY PATTERNS:
- Scope per probe; the check is a singleton and never holds a scoped service.
- Names are lowercase kebab-case and unique; a duplicate throws at registration.
- Tests execute real code (a real DbContext, real containers through the shared collection fixtures), never reflection only.

REFERENCE FILES:
- src/Encina.EntityFrameworkCore/Health/EntityFrameworkCoreHealthCheck.cs
- src/Encina.Messaging/Health/EncinaHealthCheck.cs
- src/Encina.AspNetCore/Health/HealthCheckBuilderExtensions.cs
- docs/plans/encina-healthchecks-implementation-plan-181.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ N/A | Health results must be real time; the cache checks are #754 and `QueryCacheHealthCheck`. |
| 2 | OpenTelemetry | ⏭️ #622 | One health publisher emitting a per-check gauge is owned by the plan of #622 (its Design Choice 4); this check adds no instrument. |
| 3 | Structured Logging | ❌ N/A | The `EncinaHealthCheck` base class reports a failure by exception type; this plan adds no log message and no EventId. |
| 4 | Health Checks | ✅ Phase 1 | This is the typed `DbContext` check and its registration, which closes the only gap found. |
| 5 | Validation | ❌ N/A | A read-only internal check with no external input; a duplicate check name is rejected at registration (Design Choice 2). |
| 6 | Resilience | ✅ Phase 1 | Per-check timeout and failure status come from `ProviderHealthCheckOptions` (`Timeout`, `FailureStatus`); no Polly. |
| 7 | Distributed Locks | ❌ N/A | Each instance checks its own connection. |
| 8 | Transactions | ❌ N/A | `CanConnectAsync` is read-only and opens no transaction. |
| 9 | Idempotency | ❌ N/A | A probe is idempotent by nature. |
| 10 | Multi-Tenancy | ⏭️ Follow-up issue file | `artifacts/issues/tenant-scoped-database-health.md` (Design Choice 3). |
| 11 | Module Isolation | ❌ N/A | Already integrated through `IModuleHealthCheck` and `AddEncinaModuleHealthChecks`. |
| 12 | Audit Trail | ❌ N/A | An infrastructure probe, not an auditable operation. |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | Status | Notes |
|-------------|--------|-------|
| `EncinaHealthCheck`, `ProviderHealthCheckOptions`, `HealthCheckBuilderExtensions` | On `main` | Base, options and registration the check builds on |

### Recommended (Not Blocking)

| Dependency | Issue | Notes |
|-----------|-------|-------|
| Health telemetry publisher | #622 | Owns the per-check gauge and the answer to the same package question |
| Probe endpoints and tag constants | #454 | The `live`, `ready` and `startup` tag sets and the JSON writer |
| Remaining cache checks | #754 | The six cache providers without a check |
| Processor liveness | #2396 | Background-processor checks the host can tag `live` |
| Health-check documentation | #907 | Rewrites the guide this plan adds a section to |
| Inbox backlog query | `artifacts/issues/inbox-store-backlog-query.md` | The store method behind stuck-message detection |
| Tenant-scoped health | `artifacts/issues/tenant-scoped-database-health.md` | The per-tenant database check |

---

## Next Steps

1. Link this plan from #181 and ask the maintainer to answer Design Choices 1 to 3 and register row C7 when work starts; Design Choice 1 is answered together with #622 and #454.
2. Implement H1, then D1. After D1 merges, close #181 with the map of the Summary table in the closing comment; the open items of the map are tracked by their owners and the two follow-up issue files, which the orchestrator opens.
