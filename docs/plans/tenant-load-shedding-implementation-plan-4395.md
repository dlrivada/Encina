# Implementation Plan: Tenant-scoped priority for load shedding

> **Issue**: [#4395](https://github.com/dlrivada/Encina/issues/4395)
> **Type**: Feature (follow-up of #138)
> **Complexity**: Medium (7 phases, no database providers, 3 pull requests)
> **Estimated Scope**: ~300 lines of production code + ~600 lines of tests (about 1,000 changed lines in total, three pull requests)

---

## Summary

Let load shedding use a per-tenant priority, so a premium tenant keeps service while a free tier is shed first. The plan of #138 keeps shedding process-wide, with the priority taken from the request instance, the per-type option or the default (`Degraded`), and defers the tenant dimension. This plan adds `ITenantPriorityResolver`, consulted by the shedding strategy of #138 when multi-tenancy is on, an options-based default with named tiers, and a documented rule that combines the request's own priority with the tenant's priority. The thresholds, the utilization signals and the manager of #138 stay process-wide.

What exists today (read in `origin/main` at `0a7ce2ae1`):

- No shedding code: `git grep -E "LoadShed|RequestPriority|IPrioritizedRequest" -- src` returns no match. `Encina.Resilience` does not exist in `src/` (its plan is #4380); the shedding plan of #138 is on `main` and not implemented.
- The tenant of a call is `IRequestContext.TenantId` (`src/Encina/Abstractions/IRequestContext.cs:125`); "multi-tenancy is on" is the registration of `MultiTenancyMarker` (`src/Encina/Abstractions/MultiTenancyMarker.cs:25`).
- The #138 plan resolves a priority in the strategy factory (`LoadSheddingStrategyFactory`, its Phase 2) from the instance, then the per-type option, then `DefaultPriority` (its choice 5), and asks `ILoadSheddingManager.ShouldShed(RequestPriority)`. Its `RequestPriority` runs `Critical = 0, Degraded = 1, BestEffort = 2, Bulk = 3`: a lower number is more important, and the plan exposes the order through a `Compare` helper, never the raw number (its choice 4). Its choice 5 also records a trust problem: an instance priority copied from client input can claim `Critical` and avoid shedding.
- There is no tier concept in code: the plan of #740 states that `TenantInfo` has no tier and keeps tiers as its own pending Dilemma 3.
- The rejection `Left` (code, category, details) is decided by choice 10 of the #138 plan; this plan adds no error code and no ADR-062 row: a tenant priority changes who is shed, not how the rejection looks.

What this plan delivers, all in `Encina.Resilience`: `ITenantPriorityResolver`, `TenantPriorityOptions` (named tiers and a tenant to tier map) with its validator, the default `OptionsTenantPriorityResolver`, the internal combination rule, the change to the #138 strategy factory, the opt-in registration `AddEncinaTenantLoadShedding`, three `[LoggerMessage]` definitions, a tier tag on the #138 instruments (the tenant tag itself is #138's), and the tests.

- **Affected packages**: `Encina.Resilience` (planned). The issue ticks only that package.
- **Provider category**: Resilience (AGENTS.md section 5, "Other categories"); no database, caching, transport, lock or validation matrix applies.
- **Estimated size**: 3 pull requests of at most 15 files and 1,500 lines (split in "Implementation Phases").
- **Prerequisites**: #4380 (foundation) and the #138 implementation (its PR-2 and PR-4), described in "Prerequisites & Dependencies".

### Shared Pieces and Ownership (family resilience follow-ups: #4391, #4392, #4393, #4394, #4395)

| Shared piece | Owner | Used by |
|--------------|-------|---------|
| `Encina.Resilience` project, compiler, registry, `ResilienceContext` property keys (request context and request) | #4380 (plan, by number) | this plan |
| `RequestPriority`, `IPrioritizedRequest`, `ILoadSheddingManager`, `LoadSheddingStrategyFactory`, the instruments, EventIds 912-916 | #138 (plan on `main`, by number) | this plan modifies the factory and the telemetry class |
| `ITenantPriorityResolver`, `TenantPriorityOptions`, the combination rule | **This plan (#4395)** | none |
| Tier vocabulary shared with timeouts and rate limiting | #4391 (plan, by number) and #740 (plan on `main`) | choice 4: asked once for the family |

---

## Design Choices

<details>
<summary><strong>1. How a tenant influences shedding: a priority resolver, per-tenant thresholds, or not planned</strong></summary>

The #138 plan has one threshold per priority and one process-wide utilization. The tenant dimension can enter as the priority of a request or as the thresholds themselves.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ITenantPriorityResolver` returning a `RequestPriority` per tenant, read from tenant configuration; thresholds stay per priority and process-wide** | One lookup per request; the manager and the calculator of #138 do not change; a tier maps to one of the four existing levels | Tenants share the four thresholds: a tier cannot start shedding at its own utilization |
| **B) Per-tenant threshold tables in the options (a threshold for each priority for each tenant)** | A tenant can have its own shedding points; static and simple | `ShouldShed` needs the thresholds as an argument (a change to the #138 manager contract); tables grow with tenants; no replaceable source |
| **C) Not planned: the request's own priority is enough** | No code | A free tier and a premium tier with the same request type are shed alike |

### Chosen Option: **A - Priority resolver** (recommended, pending the maintainer)

### Rationale

- A wins with many tenants and a handful of tiers; B wins with a handful of tenants that need different shedding points; C wins when tiers map to different request types (each type carries its own priority through the #138 per-type option).
- Under B the thresholds in the issue title ("priority and thresholds") are built and choice 2 below does not apply.

- **Class:** Dilemma

</details>

<details>
<summary><strong>2. Combining the request's priority with the tenant's priority</strong></summary>

A premium tenant also sends background work, and a request type can declare `Bulk` on its instance. The effective priority needs a rule. The issue says the resolver returns "a `RequestPriority` floor per tenant" without defining which direction a floor has on the scale of #138 (lower number is more important). This plan states the deviation: option A below is a ceiling of importance (a tenant cannot be treated as more important than its tier), and the reading of the issue as a floor (a tenant is treated as at least as important as its tier) is option C.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The effective priority is the less important of the two: a request never rises above its tenant's priority, and a premium tenant's `Bulk` request is still shed first** | A tier is a ceiling of importance; it closes the trust problem of #138 choice 5 for tenant-bound traffic (a request cannot claim `Critical` above its tenant's level) | A premium request type declared `Degraded` stays `Degraded`: a tier never raises a request |
| **B) The tenant priority replaces the request's priority** | One level per tenant; simple to explain | A premium tenant's batch jobs are as protected as its interactive calls; a per-type `Bulk` is ignored |
| **C) A floor: the effective priority is the more important of the two (a premium tenant's `Bulk` request is treated as at least the tenant's tier)** | Reads the issue literally; a premium tenant is guaranteed at least its tier even for background work | Per-type `Bulk` is ignored for premium tenants, and it widens the trust problem of #138 choice 5: a request that claims `Critical` keeps it |
| **D) The tenant priority only applies to requests that declare none** | Instance and type priorities stay authoritative | A free tenant's client-controlled instance priority can still claim `Critical` |

### Chosen Option: **A - Less important of the two** (recommended, pending the maintainer)

### Rationale

- A wins when a tier means "the most important treatment this tenant may receive"; B wins when the tier is the only classification the product has; C wins when a premium contract guarantees a minimum level of service even for the tenant's background work; D wins when request classification is trusted (computed by the application from the request type and the authenticated identity, the rule of #138 choice 5).
- The comparison uses `RequestPriority.Compare` of #138 (never the raw number); a unit test pins the orientation.

- **Class:** Dilemma

</details>

<details>
<summary><strong>3. A call with no tenant, a tenant the resolver does not know, or a resolver that fails</strong></summary>

A shedding control is an availability control, not an access gate: it decides which requests run first when the process is overloaded and reads no data. The issue proposes the default priority for a missing tenant.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The request's own priority applies as if no tenant dimension existed (that is the `DefaultPriority` of #138 for an unclassified request), logged by code and counted; when multi-tenancy is off the resolver is never called** | No call is refused for a configuration gap; the process keeps serving | Tenant-less traffic is treated like ordinary traffic, not less important |
| **B) Treat the call as the least important priority (`Bulk`): tenant-less and unknown-tenant traffic is shed first** | Fails toward shedding under overload; anonymous traffic never competes with identified tenants | A misconfigured tenant is shed first and can look like an outage |

### Chosen Option: **A - Own priority, logged** (recommended, pending the maintainer)

### Rationale

- A wins while a gap in configuration should not degrade a tenant; B wins on public endpoints where tenant-less traffic is untrusted and capacity must go to identified tenants first. Fail-closed gates (AGENTS.md section 3) apply to compliance and security decisions; for a request type that has a rate limit, the limiter of #2414 D3 (an `Admission` gate that denies a tenant-less request unless the type opts out) is the control that denies, so neither option here denies.
- A cancellation of the caller's token during a lookup is never a `Left`: `OperationCanceledException` propagates (ADR-062 rule R5). A resolver `Left` is logged by its error code only, never by `EncinaError.Message` (AGENTS.md section 3).

- **Class:** Dilemma

</details>

<details>
<summary><strong>4. The tier vocabulary: its own options, or one tier map shared with timeouts and rate limiting</strong></summary>

This is the same question as choice 2 of the #4391 plan, asked once for the family. #740 keeps tiers as its pending Dilemma 3 (`TenantRateLimitOptions`).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `TenantPriorityOptions` with its own named tiers and tenant to tier map** | Not blocked by a pending decision; a tenant may be premium for shedding and standard for timeouts | The same tenant is assigned to tiers in two or three places |
| **B) One tier map in core consumed by rate limiting, timeouts and shedding** | One place to assign a tenant to a plan | Needs the #740 Dilemma 3 answered first and a new core abstraction; couples three features |

### Chosen Option: **A - Own options** (recommended, pending the maintainer)

### Rationale

- A wins when the features ship in different releases or tiers differ per concern; B wins when one commercial plan drives every quota. Tenant ids are matched with `StringComparer.Ordinal` and are stored as dictionary keys, never concatenated into a string key, so no two (tenant, tier) pairs can collide; `TenantPriorityOptions` holds no secret and needs no `ToString` override.

- **Class:** Dilemma

</details>

<details>
<summary><strong>5. Resolver lifetime: a singleton read in the strategy, or a per-call service placed by a contributor</strong></summary>

Shedding runs at `PipelineStage.Admission` on the hot path of every request. The #4380 hook for per-call (possibly scoped) services is the call contributor, which the foundation plan attaches to the Resilience-stage behavior; whether the Admission-stage behavior also calls contributors is not stated there.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ITenantPriorityResolver` is a singleton; the strategy factory receives it by constructor and reads the tenant from the `ResilienceContext` request key; a resolver that needs scoped data creates its own scope through `IServiceScopeFactory`** | No dependency on a contributor at Admission; no per-call resolution of a service; allocation-free with the options default | A scoped resolver is more work for the application |
| **B) A call contributor resolves a scoped resolver per call and places it in the `ResilienceContext`** | Scoped resolvers (a `DbContext`) work directly | Requires #4380's Admission behavior to call contributors (a modification of that file in this plan if absent); one service resolution per request |

### Chosen Option: **A - Singleton** (recommended, pending the maintainer)

### Rationale

- A wins when tiers come from configuration or an in-memory snapshot; B wins when the tier lookup naturally needs a scoped service. A never injects a scoped service into a singleton (AGENTS.md rule on lifetimes); the registration test builds with `ValidateOnBuild` and `ValidateScopes`.

- **Class:** Dilemma

</details>

<details>
<summary><strong>6. Telemetry: a tier tag on the #138 instruments</strong></summary>

The tenant tag on the #138 counters and span event, only when multi-tenancy is on, is already specified by #138 (its choice 13 option A and Phase 4 task 2) following ADR-055 R5 and ADR-054 D4; this plan adds nothing to it. What is new is the tier name, which neither ADR-055 R5 nor ADR-054 D4 names (they name the tenant).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A `tenant.tier` tag (bounded by the number of tiers) on the same counters and span event, only when `MultiTenancyMarker` is registered; name from the convention #728 settles; no new instrument** | Shed counts per tier without the cardinality of the tenant id | A tag beyond what the ADRs name; the name waits for #728 |
| **B) No tier tag; operators read the tenant tag of #138** | Nothing beyond the ADRs | Per-tier views need a join outside the telemetry |

### Chosen Option: **A - Tier tag only with multi-tenancy** (recommended, pending the maintainer)

### Rationale

- A wins when tiers are few and tenants many; B wins for a deployment with a handful of tenants where the tenant tag already answers the question. Tags never carry `EncinaError.Message` or request data (ADR-054 settled point 2).

- **Class:** Dilemma

</details>

---

## Implementation Phases

Three pull requests, in order: **PR-1** Phases 1 and 2 and the registration of Phase 3 (resolver contract, options, validator, default implementation, combination rule and `AddEncinaTenantLoadShedding`, which registers the options, validator and resolver so the validator runs; it needs the foundation project of #4380 PR-1 and `RequestPriority` of the #138 PR-1); **PR-2** the factory change of Phase 3 and Phases 4 and 5 (the change to the strategy factory, logs and the tier tag; it needs the #138 PR-2 and PR-4 merged); **PR-3** Phase 7 (documentation and changelog). Phase 6 tests are inside PR-1 and PR-2, each PR carrying the tests of the files it adds.

### Phase 1: Core Models and Contracts

<details>
<summary>Tasks</summary>

1. `src/Encina.Resilience/LoadShedding/Tenants/ITenantPriorityResolver.cs`: `public interface ITenantPriorityResolver { ValueTask<Either<EncinaError, RequestPriority?>> ResolveAsync(string tenantId, CancellationToken cancellationToken); }`; a `null` result means "no tenant priority" (choice 3).
2. `TenantPriorityOptions.cs`: `Dictionary<string, RequestPriority> Tiers` (tier name to priority) and `Dictionary<string, string> Tenants` (tenant id to tier name), both `StringComparer.Ordinal`; `TenantPriorityOptionsValidator.cs` (internal `IValidateOptions<TenantPriorityOptions>`): every mapped tenant names an existing tier, tier values are defined enum members, tenant id not blank and at most 128 characters, tenant count bounded (documented default of 10,000).
3. `EffectivePriority.cs` (internal static): `RequestPriority Combine(RequestPriority requestPriority, RequestPriority? tenantPriority)` applying choice 2 through `RequestPriority.Compare`; a null tenant priority returns the request priority.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT: Encina issue #4395, plan docs/plans/tenant-load-shedding-implementation-plan-4395.md. Encina.Resilience exists (foundation #4380) with RequestPriority and the load shedding strategy of #138 merged.
TASK: Create ITenantPriorityResolver, TenantPriorityOptions, its validator and EffectivePriority.
KEY RULES: Either<EncinaError, T> on the resolver; a null result means no tenant priority; the order of priorities is read through RequestPriority.Compare, never the raw number; invalid options fail at startup; XML docs with <example>; PublicAPI lines through mechanical-fixer.
REFERENCE FILES: docs/plans/load-shedding-implementation-plan-138.md (choices 4 and 5), src/Encina/Abstractions/MultiTenancyMarker.cs
```

</details>

### Phase 2: Default Implementation

<details>
<summary>Tasks</summary>

1. `OptionsTenantPriorityResolver.cs` (internal sealed; constructor `(IOptionsMonitor<TenantPriorityOptions> options)`, null-checked): tenant id to tier to priority from a frozen dictionary rebuilt on options change; unknown tenant returns `Right(null)`; no I/O and no allocation on the hot path.
2. Tests in the same PR (PR-1): resolver, validator, `EffectivePriority` (all 16 combinations of two priorities and null) and a property test (below).

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT: Phase 1 done.
TASK: Implement OptionsTenantPriorityResolver and the PR-1 tests.
KEY RULES: no I/O; unknown tenant is Right(null), never a Left; frozen dictionary rebuilt on options change; Shouldly through Encina.Testing.Shouldly.
REFERENCE FILES: docs/en/guides/TESTING.md
```

</details>

### Phase 3: Strategy Integration, Configuration and DI

<details>
<summary>Tasks</summary>

1. `LoadShedding/LoadSheddingStrategyFactory.cs` (modified, file of #138 PR-2; PR-2 of this plan): the factory takes `ITenantPriorityResolver?` and `MultiTenancyMarker?` through optional trailing constructor parameters (null when the opt-in registration is absent or multi-tenancy is off, so nothing changes without them); when the marker is present it reads the tenant from the `ResilienceContext` request key of #4380, calls the resolver once per request, applies choices 2 and 3 and passes `EffectivePriority.Combine(...)` to `ILoadSheddingManager.ShouldShed`. A cancelled caller token propagates.
2. `LoadShedding/Tenants/TenantPriorityServiceCollectionExtensions.cs` (public static; PR-1): `AddEncinaTenantLoadShedding(this IServiceCollection services, Action<TenantPriorityOptions>? configure = null)`; `ArgumentNullException.ThrowIfNull(services)`; registers the validator and `TryAddSingleton<ITenantPriorityResolver, OptionsTenantPriorityResolver>` so an application's own resolver wins in any order; the DI test builds the provider with `ValidateOnBuild` and `ValidateScopes`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT: Phases 1 and 2 merged; the #138 strategy factory exists.
TASK: Modify the strategy factory and add the registration.
KEY RULES: opt-in (nothing changes without AddEncinaTenantLoadShedding); the resolver is a singleton and no scoped service is captured; a missing tenant, unknown tenant or failing resolver uses the request's own priority and is logged by code; cancellation propagates and is never a Left.
REFERENCE FILES: src/Encina/Abstractions/IRequestContext.cs, docs/plans/load-shedding-implementation-plan-138.md
```

</details>

### Phase 4: Cross-Cutting Integration

<details>
<summary>Tasks</summary>

1. Multi-tenancy (function 10): the resolver is consulted only when `MultiTenancyMarker` is registered; no tenant-qualified string key is built (dictionary lookups by tenant id and tier name), so the reserved `default` tenant segment of the sibling plans does not apply here; a custom resolver that caches by tenant follows the injective tenant-qualified key convention of the #740 and #1257 plans (a convention of those plans, not enforced here).
2. Health check of #138 (`LoadSheddingHealthCheck`, state `Degraded` while shedding): unchanged; it reads process-wide state and reports no tenant.
3. Idempotency: a shed request has not run, so a retry is safe; unchanged from #138.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT: Phases 1 to 3 done.
TASK: Verify the cross-cutting rows of the plan and add only what they need.
KEY RULES: no tenant-qualified key; no change to the health check; tenant tag only with multi-tenancy.
REFERENCE FILES: docs/architecture/adr/054-observability-conventions-and-trace-propagation.md
```

</details>

### Phase 5: Observability

<details>
<summary>Tasks</summary>

1. `Diagnostics/TenantPriorityLogMessages.cs`: three `[LoggerMessage]` definitions: 917 `TenantPriorityTenantMissing` (Debug), 918 `TenantPriorityResolverFailed` (Warning; error code only), 919 `TenantPriorityApplied` (Debug; request priority, tenant priority, effective priority; no tenant id). The ids are inside the `ResilienceLoadShedding` range (912-923) that the #138 plan registers in its PR-4 (912-916 used, 917-923 spare); a search of the block registry rows 900-949 and the #139 and #141 plans found no other claim on 917-919. They are valid only once #138 PR-4 has merged and the range exists in `src/Encina/Diagnostics/EventIdRanges.cs`; this plan creates no range.
2. `LoadShedding/LoadSheddingTelemetry.cs` (modified, file of #138 PR-4): the counters and the span event gain the `tenant.tier` tag only when `MultiTenancyMarker` is registered (choice 6); the tenant tag is #138's.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT: Phases 1 to 4 done.
TASK: Add the three log messages and the conditional tags.
KEY RULES: [LoggerMessage] source generator; ids packed without gaps inside the #138 range (confirm it is registered first); never EncinaError.Message, tenant ids or request data in logs; exceptions through ForLogging().
REFERENCE FILES: src/Encina/Diagnostics/EventIdRanges.cs, docs/engineering/eventid-block-registry.md
```

</details>

### Phase 6: Testing

<details>
<summary>Tasks</summary>

1. Unit (`tests/Encina.UnitTests/Resilience/LoadShedding/Tenants/`): the 20 combinations of choice 2 (four request priorities by four tenant priorities and by a null tenant priority), missing tenant, unknown tenant, failing resolver (logged by code) and multi-tenancy off (resolver never called); the factory with a fake manager shows the effective priority reaches `ShouldShed`; cancellation propagates.
2. Guard (`tests/Encina.GuardTests/Resilience/LoadShedding/Tenants/TenantPriorityGuardTests.cs`, in PR-1): `AddEncinaTenantLoadShedding` with a null `services`; the validator and the resolver constructors with a null; the nullable `configure` accepted as null (valid result, not an exception test). The factory's new optional constructor parameters accept null and are covered by a valid-result test in PR-2.
3. Property (`tests/Encina.PropertyTests/Resilience/LoadShedding/TenantPriorityProperties.cs`): for any request priority and tenant priority the effective priority is never more important than either input (monotonicity per tenant), and for a fixed tenant the shed probability is monotone in utilization.
4. DI (`TenantPriorityRegistrationTests.cs`): `ValidateOnBuild`, `ValidateScopes`, an application resolver registered before and after the call.
5. Contract, integration, load, benchmark: not applicable (arithmetic over a dictionary, no new public abstraction beyond the resolver interface); justification files for load and benchmark.

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT: Phases 1 to 5 done.
TASK: Write the Phase 6 tests.
KEY RULES: tests execute real package code; FakeTimeProvider, never Thread.Sleep; Shouldly through Encina.Testing.Shouldly; a test never swallows an exception thrown by the code it drives.
REFERENCE FILES: docs/en/guides/TESTING.md
```

</details>

### Phase 7: Documentation and Finalization

<details>
<summary>Tasks</summary>

1. `docs/features/resilience-load-shedding.md` (page created by #138, modified) and the `Encina.Resilience` README section (`docs-writer`): the resolver, the tiers, the combination rule, the missing-tenant behavior, the trust rule for instance priorities.
2. `changelog.d/4395-tenant-load-shedding.added.md`, `PublicAPI.Unshipped.txt`, the manifest rows (`mechanical-fixer`).
3. Verification: `dotnet build` 0 warnings; tests filtered to `LoadShedding.Tenants`; `--check-justifications`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```text
CONTEXT: Phases 1 to 6 green.
TASK: Documentation and verification.
KEY RULES: never edit CHANGELOG.md; documentation by docs-writer, metadata by mechanical-fixer; no hand-typed coverage figures (cite covref markers).
REFERENCE FILES: changelog.d/README.md, .claude/skills/encina-docs/SKILL.md
```

</details>

---

## Research

### Relevant Standards and Specifications

| Source | Relevance |
|--------|-----------|
| ADR-055 R1, R5 | Shedding lives in `Encina.Resilience`; enricher adds the tenant only with multi-tenancy |
| ADR-054 D4 | The tenant is a tag only when multi-tenancy is on, never baggage |
| ADR-062 R5 | A cancellation is never a fault |
| AGENTS.md section 3 (fail-closed gates, `EncinaError.Message` never logged) | Choice 3 and the log rules |
| Netflix prioritized load shedding, Uber request prioritization | Comparable products; tiers decide who is shed first |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage |
|-----------|----------|-------|
| `IRequestContext.TenantId` | `src/Encina/Abstractions/IRequestContext.cs:125` | The tenant of a call |
| `MultiTenancyMarker` | `src/Encina/Abstractions/MultiTenancyMarker.cs:25` | "Multi-tenancy is on" |
| `RequestPriority`, `ILoadSheddingManager`, `LoadSheddingStrategyFactory`, `LoadSheddingTelemetry` | #138 plan, PR-1, PR-2, PR-4 | Modified or consumed |
| `ResilienceContext` request key | #4380 plan, PR-1 | The tenant read inside the strategy |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Resilience` (`ResilienceLoadShedding`, registered by #138 PR-4) | 917-919 | Three ids from the spare 917-923 of the #138 range; no other plan or registry row claims them; no new range is created here |

### Estimated File Count

| PR | Files | Content |
|----|-------|---------|
| PR-1 | 14 | `ITenantPriorityResolver.cs`, `TenantPriorityOptions.cs`, `TenantPriorityOptionsValidator.cs`, `OptionsTenantPriorityResolver.cs`, `EffectivePriority.cs`, `TenantPriorityServiceCollectionExtensions.cs`, `OptionsTenantPriorityResolverTests.cs`, `TenantPriorityOptionsValidatorTests.cs`, `EffectivePriorityTests.cs`, `TenantPriorityProperties.cs`, `TenantPriorityRegistrationTests.cs`, `TenantPriorityGuardTests.cs`, `PublicAPI.Unshipped.txt`, manifest rows |
| PR-2 | 8 | `LoadSheddingStrategyFactory.cs` and `LoadSheddingTelemetry.cs` (modified), `Diagnostics/TenantPriorityLogMessages.cs`, `LoadSheddingStrategyFactoryTests.cs` and `LoadSheddingTelemetryTests.cs` (modified), `TenantPriorityLogMessagesTests.cs`, `PublicAPI.Unshipped.txt`, manifest rows |
| PR-3 | 5 | feature page (modified), README section, changelog fragment, two justification `.md` (load, benchmark) |

Each PR is within 15 files and 1,500 lines. The `ProjectReference` of the test projects to `Encina.Resilience` are acceptance items of #4380; the `Encina.PropertyTests` reference is added by this plan's PR-1 as one `csproj` line if the foundation has not added it (PR-1 then has 15 files, the limit). No test double is affected (`ITenantPriorityResolver` is new; the factory takes optional trailing constructor parameters, and its `new` sites in the #138 tests are unchanged).

Per-file targets (new files; baseline: none exists on `origin/main`, and `.github/coverage-manifest/Encina.Resilience.json` is created by #4380 PR-1):

| File | Unit | Guard | Contract | Property | Integration | Justification |
|------|------|-------|----------|----------|-------------|---------------|
| `ITenantPriorityResolver.cs` | 0 | 0 | 0 | 0 | 0 | Interface, no executable code |
| `TenantPriorityOptions.cs` | 90 | 0 | 0 | 0 | 0 | Property bag read by the validator and resolver tests |
| `TenantPriorityOptionsValidator.cs` | 95 | 15 | 0 | 0 | 0 | One test per rule; the guard flag reaches the single null check |
| `OptionsTenantPriorityResolver.cs` | 95 | 15 | 0 | 0 | 0 | Known tenant, unknown tenant, reload; the guard flag reaches the constructor check |
| `EffectivePriority.cs` | 100 | 0 | 0 | 90 | 0 | Pure function; all combinations by unit test and the monotonicity property |
| `TenantPriorityServiceCollectionExtensions.cs` | 85 | 40 | 0 | 0 | 0 | The null check is a large share of a short method; the DI test reaches the registrations |
| `TenantPriorityLogMessages.cs` | 85 | 0 | 0 | 0 | 0 | Log assertions with a fake logger (`TenantPriorityLogMessagesTests.cs`, PR-2); generated code with no parameters to guard |
| `LoadSheddingStrategyFactory.cs` (modified, file of #138) | 90 | 0 | 0 | 0 | 0 | The #138 plan's table row (unit 90, guard 0, contract 0, property 0, integration 0) is kept and never lowered; the new branches are covered by the modified factory tests of PR-2 |
| `LoadSheddingTelemetry.cs` (modified, file of #138) | 85 | 0 | 0 | 0 | 0 | The #138 plan's per-file table has no row for it; this PR sets one so the touched file has its own targets; the tier tag is asserted in the modified telemetry tests |

CRAP of each added method is at most 10 (the factory moves the tenant steps into one small method).

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT: Encina (pre-1.0, .NET 10, C# 14). Issue #4395: tenant-scoped priority for the load shedding of #138 in Encina.Resilience (ADR-055 R1, R5). AGENTS.md rules apply. The foundation (#4380) and the #138 implementation are merged first; if missing, stop and report.

IMPLEMENTATION OVERVIEW: (1) ITenantPriorityResolver, TenantPriorityOptions, validator and EffectivePriority; (2) the options-based default resolver; (3) the change to the #138 strategy factory and AddEncinaTenantLoadShedding; (4) three log messages and conditional tenant tags; (5) unit, guard, property and DI tests; (6) documentation and fragments.

KEY PATTERNS: the effective priority is the less important of the request's and the tenant's (RequestPriority.Compare, never the raw number); a missing, unknown or failing tenant lookup uses the request's own priority and is logged by code; the resolver is a singleton; opt-in registration; no new error code; specialists for PublicAPI, changelog, manifest and docs.

REFERENCE FILES: docs/plans/load-shedding-implementation-plan-138.md, docs/architecture/adr/055-resilience-family.md, src/Encina/Abstractions/MultiTenancyMarker.cs
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | The default resolver reads in-memory options with no I/O; an application's remote resolver caches its own lookups |
| 2 | OpenTelemetry | ✅ | The tenant tag is #138's; this plan adds a `tenant.tier` tag on the same counters and span event, only with multi-tenancy (choice 6) |
| 3 | Structured Logging | ✅ | Three definitions, EventIds 917-919 of the #138 range |
| 4 | Health Checks | ❌ | The #138 health check reports process-wide state; no tenant dimension |
| 5 | Validation | ✅ | Options validated on start |
| 6 | Resilience | ✅ | Extends the #138 load shedding strategy |
| 7 | Distributed Locks | ❌ | No shared mutable state |
| 8 | Transactions | ❌ | No transactional work |
| 9 | Idempotency | ❌ | A shed request has not run, so a retry is safe (#138); this plan adds no state |
| 10 | Multi-Tenancy | ✅ | The feature; the request's own priority applies for a missing tenant (choice 3); no tenant-qualified key is built |
| 11 | Module Isolation | ❌ | Module-independent |
| 12 | Audit Trail | ❌ | Shedding is an operational control, not an audited access decision |

---

## Prerequisites & Dependencies

| Prerequisite | State | Effect |
|--------------|-------|--------|
| #4380 (`Encina.Resilience` foundation) | Plan on `main`; not implemented | Blocks PR-1 (project, `ResilienceContext` keys) |
| #138 implementation (`RequestPriority`, manager, factory, telemetry) | Plan on `main`; not implemented | Blocks PR-1 (`RequestPriority`) and PR-2 (the two files it modifies) |
| #4382 (token and deadline, R6) | Plan on `main`; not implemented | Indirect: the #138 strategy needs it |
| #4391 (tenant-scoped timeouts) | Plan, by number | Choice 4 is asked once for both |
| #740 (tenant rate limiting) | Plan on `main`; Dilemma 3 (tiers) pending | Choice 4 option B only |
| #728 (telemetry names) | Plan on `main`; choices pending | Tag and event names of choice 6 |

---

## Next Steps

1. Commit this plan, open its PR and link it from #4395.
2. When #4395 is picked up, re-check the choices against today's code and ask the maintainer about the Dilemmas (choices 1 to 6).
3. Brief workers for PR-1 to PR-3 in order, after #4380 and the #138 implementation are merged.
