# Implementation Plan: Tenant-scoped timeout values for the request timeout strategy

> **Issue**: [#4391](https://github.com/dlrivada/Encina/issues/4391)
> **Type**: Feature (follow-up of #335)
> **Complexity**: Medium (7 phases, no database providers, 3 pull requests)
> **Estimated Scope**: ~330 lines of production code + ~650 lines of tests (about 1,100 changed lines in total, three pull requests)

---

## Summary

Let the total and the optional per-attempt timeout of a request type take a per-tenant value, so a premium tenant can have a longer budget than a free tier. The plan of #335 applies one timeout per request type to every tenant and defers this item; its only tenant element is the tenant tag on telemetry ([ADR-054](../architecture/adr/054-observability-conventions-and-trace-propagation.md) D4). This plan adds a tenant-aware resolver, `ITenantTimeoutResolver`, consulted once per call before the timeout strategy and the deadline contributor of #335 compute their value, an options-based default implementation with named tiers, and the rule that a resolved value can never extend a deadline that is already set (earliest deadline wins, [ADR-055](../architecture/adr/055-resilience-family.md) R6).

What exists today (read in `origin/main` at `0a7ce2ae1`):

- No timeout code at all: `git grep -i "TimeoutAttribute\|IHasTimeout\|TenantTimeout" -- src` returns no match. `Encina.Resilience` does not exist in `src/` (`ls src` lists only `Encina.Polly` and `Encina.Extensions.Resilience` for this family) and its plan is #4380; the token and deadline of R6 are the plan of #4382; the timeout strategy this plan extends is the plan of #335 (on `main`, not implemented).
- The tenant of a call is `IRequestContext.TenantId` (`src/Encina/Abstractions/IRequestContext.cs:125`). "Multi-tenancy is on" is the registration of `MultiTenancyMarker` (`src/Encina/Abstractions/MultiTenancyMarker.cs:25`), the signal `QueryCachingPipelineBehavior` already uses (`src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs:141-150`).
- There is no tier concept: the plan of #740 (on `main`) states that `TenantInfo` carries no tier and leaves tiers to its own pending Dilemma 3; the issue's phrase "tier concept exists" therefore means a design intent, not code.
- The error side needs nothing new: `encina.timeout` (`src/Encina/Errors/EncinaErrorCodes.cs:63`) and `ErrorCategory.Timeout = 11` (`src/Encina/Errors/ErrorCategory.cs`) exist and #335 owns them. This plan adds no error code and no ADR-062 row: the resolver never fails a request.

What this plan delivers, all in `Encina.Resilience`: `ITenantTimeoutResolver`, `TenantTimeout`, `TenantTimeoutOptions` (named tiers and a tenant to tier map) with its validator, the default `OptionsTenantTimeoutResolver`, an internal get-or-resolve helper used by both #335 components that need the effective timeout, the call contributor and the opt-in registration `AddEncinaTenantTimeouts`, three `[LoggerMessage]` definitions, the tenant tag on the #335 span event, and the tests.

- **Affected packages**: `Encina.Resilience` (planned). The issue ticks only that package; no change in core, hosts or caching.
- **Provider category**: Resilience (AGENTS.md section 5, "Other categories"); no database, caching, transport, lock or validation matrix applies.
- **Estimated size**: 3 pull requests of at most 15 files and 1,500 lines (split in "Implementation Phases").
- **Prerequisites**: #4380 (foundation) for PR-1, the #335 implementation (its PR-3) for PR-2, described in "Prerequisites & Dependencies".

### Shared Pieces and Ownership (family resilience follow-ups: #4391, #4392, #4393, #4394, #4395)

| Shared piece | Owner | Used by |
|--------------|-------|---------|
| `Encina.Resilience` project, per-type compiler and registry, the call-contributor hook (`IResilienceCallContributor`), `ResilienceContext` property keys | #4380 (plan, by number) | this plan, #4394, #4395, #4392, #4393 |
| `RequestHandlerCallback<TResponse>(CancellationToken)` and `IRequestContext.Deadline` (R6) | #4382 (plan, by number) | the #335 deadline contributor this plan modifies |
| `TimeoutAttribute`, `IHasTimeout`, `TimeoutStrategyFactory`, `TimeoutDeadlineContributor`, EventIds 950-954 | #335 (plan on `main`, by number) | this plan modifies the factory and the deadline contributor |
| `ITenantTimeoutResolver`, `TenantTimeout`, `TenantTimeoutOptions`, the get-or-resolve helper | **This plan (#4391)** | none |
| Tier vocabulary shared with rate limiting | #740 (plan on `main`; its Dilemma 3 pending) | choice 2 of this plan |
| Tenant-scoped priority for load shedding (a sibling resolver with the same shape) | #4395 (plan, by number) | independent; no code is shared |

---

## Design Choices

<details>
<summary><strong>1. How a per-tenant value is supplied: a resolver, tables in the options, or not planned</strong></summary>

The #335 plan resolves one timeout per request type from options, `IHasTimeout`, the attribute and the global default (its choices 2 and 4). A tenant dimension needs a source of values that is not compiled into request types.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ITenantTimeoutResolver` (resolver over tenant configuration), consulted once per call; a default implementation reads `TenantTimeoutOptions`; an application replaces it with its own (a database of plans)** | One lookup per call; the source can be configuration or a store; the strategy stays compiled once (R2) | One abstraction and one options class to document |
| **B) Per-tenant tables inside `For<T>()` options of each request type** | No new abstraction; static and visible next to the type | One table per request type multiplies configuration; a new tenant edits many types; no replaceable source |
| **C) Not planned: one timeout per request type is enough** | No code | Tenant tiers cannot differ in budget; the issue is closed without a feature |

### Chosen Option: **A - Resolver over tenant configuration** (recommended, pending the maintainer)

### Rationale

- A wins with many tiers or tenants whose plan lives outside code; B wins with a handful of tenants and one or two request types; C wins when tiers map to different request types (a premium endpoint is its own type with its own `[Timeout]`).
- A does not rebuild a pipeline per tenant: the value is passed per call (as #335 does for `IHasTimeout`), so R2's rule "no pipeline is rebuilt per call" holds.

- **Class:** Dilemma

</details>

<details>
<summary><strong>2. The tier vocabulary: its own options, or one tier map shared with rate limiting (#740)</strong></summary>

The issue mentions tiers "as in the sibling work on tenant-scoped rate limiting". #740 has no tier code on `main` and records the same question as its pending Dilemma 3 (`ITenantRateLimitPolicy` with `TenantRateLimitOptions`: named tiers and a tenant to tier map).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `TenantTimeoutOptions` with its own named tiers and tenant to tier map; independent of #740** | Not blocked by a pending decision; tiers can differ per concern (a tenant may be premium for timeouts only) | The same tenant is assigned to tiers in two or three places |
| **B) One tier map in core consumed by rate limiting, timeouts and shedding** | One place to assign a tenant to a plan | Needs #740's Dilemma 3 answered first and a new core abstraction; couples three features |

### Chosen Option: **A - Own options** (recommended, pending the maintainer)

### Rationale

- A wins when the features ship in different releases or tiers differ per concern; B wins when one commercial plan drives every quota and a single assignment point matters more than independence.
- `TenantTimeoutOptions` holds no secret; it needs no `ToString` override (AGENTS.md section 3 applies to passwords, tokens and keys only). Tenant ids are matched with `StringComparer.Ordinal` and are stored as dictionary keys, never concatenated into a string key, so no two (tenant, tier) pairs can collide.

- **Class:** Dilemma

</details>

<details>
<summary><strong>3. A call with no tenant, a tenant the resolver does not know, or a resolver that fails</strong></summary>

A timeout is a protective control, not an access gate: it grants or limits a time budget and reads no data. The issue proposes the type's default timeout in those cases.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Use the request type's own value (the #335 result without a tenant value), log by code and count the case; when multi-tenancy is off the resolver is never called** | The call keeps a bounded timeout; no tenant gets a longer budget than the default by omission (least privilege); one failing lookup does not stop traffic | A misconfigured tenant silently gets the default budget (visible in logs and in the span event) |
| **B) Fail closed: answer a coded `Left` (`InvalidRequest` for a missing tenant, ADR-062 R3; `Internal` for a lookup failure, R4)** | A tenant-less or unknown call never runs with an unintended budget | A transient lookup failure of a protective setting takes the endpoint down; an unauthenticated health probe without tenant fails |

### Chosen Option: **A - Type default, logged** (recommended, pending the maintainer)

### Rationale

- A wins while the timeout only limits time and a default tier is safe for everyone; B wins when a tier is a contractual entitlement and an unidentified call must not consume resources, in which case, for a request type that has a rate limit, the limiter of #2414 D3 (an `Admission` gate that runs before the timeout and denies a tenant-less request unless the type opts out) is already the control and B would duplicate it.
- A cancellation of the caller's token during the lookup is never a `Left`: `OperationCanceledException` propagates (ADR-062 rule R5). A `Left` from the resolver is logged by its error code only, never by `EncinaError.Message` (AGENTS.md section 3).

- **Class:** Dilemma

</details>

<details>
<summary><strong>4. Precedence of the tenant value against the per-instance and per-type values</strong></summary>

#335 fixes the precedence options-for-type, then `IHasTimeout`, then the attribute, then the global default. A tenant value is a fifth source.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The tenant value replaces the type's configured total (and attempt) value, and caps an `IHasTimeout` instance value: effective = min(instance value, tenant value) when both exist** | A tier is an entitlement: a free tenant cannot buy a longer budget with a large payload; a premium tenant gets its budget for every type that has a timeout | A request type with no timeout at all (no attribute, options or default) stays without one: the tenant value never creates a timeout where none was configured (opt-in, AGENTS.md section 1) |
| **B) Precedence list: options for the type, `IHasTimeout`, tenant value, attribute, default** | No capping logic; the instance value always wins | A free tenant's large report overrides its tier |
| **C) The tenant value is a ceiling only (never raises the type's value)** | Cannot extend any budget | Contradicts the issue: a premium tenant could not have a longer budget than the type default |

### Chosen Option: **A - Replace and cap** (recommended, pending the maintainer)

### Rationale

- A wins when tiers are entitlements; B wins when instance data is authoritative and tiers are only defaults; C wins when the product only ever shortens budgets for lower tiers.
- The per-attempt value follows the same rule and must stay below the effective total (the validator rule of #335, applied again to the resolved pair; an invalid pair is ignored and logged, choice 3 option A).

- **Class:** Dilemma

</details>

<details>
<summary><strong>5. A resolved value never extends an existing deadline</strong></summary>

ADR-055 R6 states that the timeout behavior shortens the context deadline and that outbound clients propagate it, "the earliest deadline winning, as gRPC's call-context propagation does". #335's deadline contributor sets `min(existing, now + total)` and never extends. A premium tenant's longer budget must not override a shorter deadline that an inbound call or a parent request already carries.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The effective deadline is `min(existing deadline, now + effective total)`; a tenant value only matters when no earlier deadline exists** | Meets R6; the inbound caller's deadline stays authoritative | A premium tenant behind a short upstream deadline gets no benefit |
| **B) The tenant value overrides the existing deadline** | The tier is always honored | Violates "the earliest deadline winning" and lets a request outlive its caller |

### Chosen Option: **A - Earliest deadline wins**

### Rationale

- B does not comply with ADR-055 R6; A is the only option that does. It holds under every option of choices 1 to 4 that builds the feature (A and B of choice 1; under C nothing is built) because it concerns the deadline arithmetic, not the source of the value. Time comes from `TimeProvider` (AGENTS.md section 3).
- **Class:** Settled
- **Settled by:** ADR-055 R6 ("the earliest deadline winning")

</details>

<details>
<summary><strong>6. Telemetry: a tier tag on the #335 span event</strong></summary>

The tenant tag, only when multi-tenancy is on, follows ADR-055 R5 and ADR-054 D4 and is the telemetry element of #335 itself; this plan adds nothing to it. What is new is the tier name, which neither ADR-055 R5 nor ADR-054 D4 names (they name the tenant).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A `tenant.tier` tag (bounded by the number of tiers) on the existing #335 `timeout` span event, only when `MultiTenancyMarker` is registered; no Encina instrument** | Operators see which tier's budget fired without the cardinality of the tenant id; no double counting of Polly's instruments (ADR-054 D6.2) | A tag beyond what the ADRs name; the name waits for #728 |
| **B) No tier tag; operators read the tenant tag of #335** | Nothing beyond the ADRs | Per-tier views need a join outside the telemetry |

### Chosen Option: **A - Tier tag only with multi-tenancy** (recommended, pending the maintainer)

### Rationale

- A wins when tiers are few and tenants many; B wins for a deployment with a handful of tenants. Tags never carry `EncinaError.Message` or request data (ADR-054 settled point 2).

- **Class:** Dilemma

</details>

---

## Implementation Phases

Three pull requests, in order: **PR-1** Phases 1 and 2 and the opt-in registration `AddEncinaTenantTimeouts` (resolver contract, options, validator, default implementation; the extension registers the options, validator and resolver so the validator runs; it needs the foundation project of #4380 PR-1); **PR-2** Phases 3 and 5 (the contributor, the get-or-resolve helper, the changes to the two #335 components, the registration, the logs; it needs the #335 implementation PR-3 merged); **PR-3** Phase 7 (documentation and changelog). Phase 4 and 6 add no separate PR: their items are inside PR-1 and PR-2.

### Phase 1: Core Models and Contracts

<details>
<summary>Tasks</summary>

1. `src/Encina.Resilience/Timeouts/Tenants/ITenantTimeoutResolver.cs`: `public interface ITenantTimeoutResolver { ValueTask<Either<EncinaError, TenantTimeout?>> ResolveAsync(string tenantId, Type requestType, CancellationToken cancellationToken); }`. A `null` result means "no tenant-specific value" (the type value applies). The issue proposes a `TimeSpan?` result; this plan returns `TenantTimeout?` (total and optional attempt value) because the issue also asks for the per-attempt timeout to be tenant-scoped, a deviation recorded here. No `TenantId` of the request context is passed as nullable: the caller checks it first (choice 3).
2. `TenantTimeout.cs`: `public readonly record struct TenantTimeout(TimeSpan Total, TimeSpan? Attempt)`; no executable branch (validation is the validator's).
3. `TenantTimeoutOptions.cs`: `Dictionary<string, TenantTimeoutTier> Tiers` (name to `Total`, `Attempt`), `Dictionary<string, string> Tenants` (tenant id to tier name), both `StringComparer.Ordinal`; `TenantTimeoutOptionsValidator.cs` (internal `IValidateOptions<TenantTimeoutOptions>`): positive values, `Attempt` below `Total`, every mapped tenant names an existing tier, tenant id not blank and at most 128 characters, tier count and tenant count bounded (a documented default of 10,000 tenants).

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT: Encina issue #4391, plan docs/plans/tenant-scoped-timeouts-implementation-plan-4391.md. Encina.Resilience exists (foundation #4380 merged).
TASK: Create ITenantTimeoutResolver, TenantTimeout, TenantTimeoutOptions and its validator.
KEY RULES: Either<EncinaError, T> on the resolver; a null value means no tenant value; invalid options fail at startup (ValidateOnStart); no exception for business flow; XML docs with <example> on every public member; PublicAPI lines through mechanical-fixer.
REFERENCE FILES: docs/architecture/adr/055-resilience-family.md, src/Encina/Abstractions/IRequestContext.cs, src/Encina/Abstractions/MultiTenancyMarker.cs
```

</details>

### Phase 2: Default Implementation

<details>
<summary>Tasks</summary>

1. `OptionsTenantTimeoutResolver.cs` (internal sealed; constructor `(IOptionsMonitor<TenantTimeoutOptions> options)`, null-checked): looks the tenant id up in `Tenants`, then the tier in `Tiers`; unknown tenant returns `Right(null)`; no I/O, so it is synchronous inside a `ValueTask` and allocation-free on the hot path (a precomputed frozen dictionary rebuilt on options change).
2. Tests in the same PR (PR-1): unit tests for the resolver and the validator (every rule, unknown tenant, reload on options change).

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT: Phase 1 done.
TASK: Implement OptionsTenantTimeoutResolver and its tests.
KEY RULES: no I/O; unknown tenant is Right(null), never a Left; a frozen dictionary rebuilt when options change; tenant ids matched with StringComparer.Ordinal; Shouldly through Encina.Testing.Shouldly.
REFERENCE FILES: src/Encina.Security.Secrets (an options-driven decorator), docs/en/guides/TESTING.md
```

</details>

### Phase 3: Strategy Integration, Configuration and DI

<details>
<summary>Tasks</summary>

1. `Timeouts/Tenants/TenantTimeoutLookup.cs` (internal static): `GetOrResolveAsync(IServiceProvider services, ResilienceContext context, IRequestContext request, CancellationToken ct)` resolves the value once per call and stores it under an internal `ResiliencePropertyKey<TenantTimeout?>` (`TenantTimeoutPropertyKeys.cs`, internal, inside the same file to keep the count); returns the stored value on later calls, so the result does not depend on the order of the two #335 components. Steps: multi-tenancy off (no `MultiTenancyMarker` in `services`) returns null; blank `request.TenantId` returns null and logs (choice 3); resolver `Left` returns null and logs the error code; an invalid value is ignored and logged.
2. `Timeouts/TimeoutStrategyFactory.cs` (modified, file of #335 PR-2): the compiled generator applies choice 4 (replace the type value, cap an `IHasTimeout` value) when `TenantTimeoutLookup` returns a value.
3. `Timeouts/TimeoutDeadlineContributor.cs` (modified, file of #335 PR-3): calls `TenantTimeoutLookup.GetOrResolveAsync` before computing `min(existing, now + effective total)` (choice 5).
3b. One shared function computes the effective values for both consumers, so they cannot drift: `Timeouts/Tenants/EffectiveTimeout.cs` (internal static; `EffectiveTimeout.Compute(TenantTimeout? tenantValue, TimeSpan? typeValue, TimeSpan? instanceValue, TimeSpan? existingRemaining)` implements choices 4 and 5); the strategy generator of `TimeoutStrategyFactory` and `TimeoutDeadlineContributor` both call it (this file is counted in PR-2).
4. `Timeouts/Tenants/TenantTimeoutCallContributor.cs` (internal sealed, implements `IResilienceCallContributor`; no constructor parameters, the services come from the `IServiceProvider` the hook passes): performs the get-or-resolve at the start of the call so the span event of #335 can carry the tier.
5. `Timeouts/Tenants/TenantTimeoutServiceCollectionExtensions.cs` (public static; created in PR-1 with the validator and resolver registrations, and modified in PR-2 to add the contributor): `AddEncinaTenantTimeouts(this IServiceCollection services, Action<TenantTimeoutOptions>? configure = null)`; `ArgumentNullException.ThrowIfNull(services)`; registers the validator, `OptionsTenantTimeoutResolver` (`TryAddSingleton<ITenantTimeoutResolver>` so an application's own resolver wins in any order) and `TryAddEnumerable` of the contributor; the DI test builds the provider with `ValidateOnBuild` and `ValidateScopes` (AGENTS.md section 3), including a scoped custom resolver resolved per call from the request's provider (no scoped service is captured by the singleton pipeline).

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT: Phases 1 and 2 merged; the #335 implementation (TimeoutStrategyFactory, TimeoutDeadlineContributor) is merged.
TASK: Add TenantTimeoutLookup, the contributor and the registration, and modify the two #335 components to use the lookup.
KEY RULES: the lookup resolves once per call and is order-independent; a caller's cancellation propagates as OperationCanceledException and is never a Left; a missing tenant, an unknown tenant or a failing resolver uses the type value and is logged by code only; a resolved value never extends a deadline; no scoped service is captured by a singleton; registration is TryAdd and proven with ValidateOnBuild and ValidateScopes.
REFERENCE FILES: src/Encina/Abstractions/IRequestContextAccessor.cs, src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs (multi-tenancy marker use)
```

</details>

### Phase 4: Cross-Cutting Integration

<details>
<summary>Tasks</summary>

1. Multi-tenancy (function 10): the resolver is consulted only when `MultiTenancyMarker` is registered; no tenant-qualified string key is built anywhere (dictionary lookups by tenant id and tier name), so the reserved `default` tenant segment of the sibling plans does not apply here; a custom resolver that caches by tenant follows the injective tenant-qualified key convention of the #740 and #1257 plans (a convention of sibling plans, not enforced by this plan).
2. Validation (function 5): options validated on start (Phase 1); a resolved invalid value is ignored and logged.
3. Hosts: no change; the produced error is the #335 `encina.timeout` with category `Timeout`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT: Phases 1 to 3 done.
TASK: Verify the cross-cutting rows of the plan and add only what they need.
KEY RULES: no tenant-qualified key is built; no host code; the tenant tag only with multi-tenancy.
REFERENCE FILES: docs/architecture/adr/054-observability-conventions-and-trace-propagation.md, docs/architecture/adr/055-resilience-family.md
```

</details>

### Phase 5: Observability

<details>
<summary>Tasks</summary>

1. `Diagnostics/TenantTimeoutLogMessages.cs`: three `[LoggerMessage]` definitions: `TenantTimeoutTenantMissing` (Debug; request type), `TenantTimeoutResolverFailed` (Warning; request type and error code), `TenantTimeoutValueInvalid` (Warning; request type and tier name). EventIds: 955 `TenantTimeoutTenantMissing`, 956 `TenantTimeoutResolverFailed`, 957 `TenantTimeoutValueInvalid`, inside the range `ResilienceTimeout` (950-959) that the #335 plan registers in its PR-3 (950-954 used, 955-959 spare); the registry rows and the #139 and #145 plans claim none of 955-957. They are valid once #335 PR-3 has merged and the range exists in `src/Encina/Diagnostics/EventIdRanges.cs`; no new range is created by this plan.
2. The `timeout` span event of #335 (`TimeoutStrategyFactory.cs`, modified) gains the `tenant.tier` tag only when `MultiTenancyMarker` is registered (choice 6); the tenant tag is #335's; no Encina instrument is added.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT: Phases 1 to 4 done.
TASK: Add the three log messages and the conditional span-event tags.
KEY RULES: [LoggerMessage] source generator; ids packed without gaps inside the range of #335 (confirm it is registered first); never EncinaError.Message or request data in logs; exceptions through ForLogging().
REFERENCE FILES: src/Encina/Diagnostics/EventIdRanges.cs, docs/engineering/eventid-block-registry.md
```

</details>

### Phase 6: Testing

<details>
<summary>Tasks</summary>

1. Unit (`tests/Encina.UnitTests/Resilience/Timeouts/Tenants/`, `FakeTimeProvider`): replace and cap (choice 4: tenant value replaces the type value; `IHasTimeout` value above the tenant value is capped; no timeout configured for the type stays none), missing tenant, unknown tenant, failing resolver and invalid value all use the type value and log, multi-tenancy off never calls the resolver, the two #335 components give the same result in either order and the resolver is called once, a cancelled token during the lookup propagates.
2. Guard (`tests/Encina.GuardTests/Resilience/Timeouts/Tenants/TenantTimeoutGuardTests.cs`, created in PR-1 for `AddEncinaTenantTimeouts` with a null `services` and the validator and resolver constructors with a null, and modified in PR-2 for the contributor and lookup parameters).
3. Property (`tests/Encina.PropertyTests/Resilience/Timeouts/`): for any existing deadline, any `now` and any resolved total, the effective deadline is never later than the existing one (choice 5).
4. DI (`tests/Encina.UnitTests/Resilience/Timeouts/Tenants/TenantTimeoutRegistrationTests.cs`): `ValidateOnBuild`, `ValidateScopes`, an application resolver registered before and after the call.
5. Contract, integration, load, benchmark: not applicable; the feature is arithmetic over a dictionary and a Polly generator; the contract flag has no public abstraction beyond the resolver interface, whose default is exercised by the unit tests; justification files for load and benchmark under `tests/Encina.LoadTests/Resilience/Timeouts/` and `tests/Encina.BenchmarkTests/Resilience/Timeouts/`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT: Phases 1 to 5 done.
TASK: Write the Phase 6 tests.
KEY RULES: tests execute real package code; FakeTimeProvider, never Thread.Sleep; Shouldly through Encina.Testing.Shouldly; a test never swallows an exception thrown by the code it drives.
REFERENCE FILES: docs/en/guides/TESTING.md, .github/coverage-manifest/Encina.Caching.json
```

</details>

### Phase 7: Documentation and Finalization

<details>
<summary>Tasks</summary>

1. `docs/features/resilience-timeouts.md` (page created by #335, modified) and the `Encina.Resilience` README section (`docs-writer`): the resolver, the tiers, the missing-tenant behavior, the cap by the existing deadline.
2. `changelog.d/4391-tenant-scoped-timeouts.added.md`, `PublicAPI.Unshipped.txt`, the manifest rows (`mechanical-fixer`).
3. Verification: `dotnet build` 0 warnings; tests filtered to `Timeouts.Tenants`; `--check-justifications`.

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
| ADR-055 R2, R3, R5, R6 | Per-type compiled pipeline, one options model, enricher with tenant tag, earliest deadline wins |
| ADR-054 D4 | The tenant is a tag only when multi-tenancy is on, never baggage |
| ADR-062 (R3, R4, R5; category `Timeout`) | Categories if choice 3 option B is picked; cancellation is never a fault |
| AGENTS.md section 3 (fail-closed gates, `EncinaError.Message` never logged, `TimeProvider`) | Choice 3 and the log rules |
| gRPC deadlines, Go `context.WithDeadline` | The earliest deadline wins |
| Per-tenant quotas and timeouts in multi-tenant API gateways | Comparable products named by the issue |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage |
|-----------|----------|-------|
| `IRequestContext.TenantId` | `src/Encina/Abstractions/IRequestContext.cs:125` | The tenant of a call |
| `MultiTenancyMarker` | `src/Encina/Abstractions/MultiTenancyMarker.cs:25` | "Multi-tenancy is on" |
| `IRequestContextAccessor` | `src/Encina/Abstractions/IRequestContextAccessor.cs` | Deadline visibility (#335) |
| `EncinaErrorCodes.Timeout`, `ErrorCategory.Timeout` | `src/Encina/Errors/` | The error of #335; unchanged here |
| Timeout strategy and deadline contributor | #335 plan, PR-2 and PR-3 | Modified by this plan |
| Foundation hooks (`IResilienceCallContributor`, `ResilienceContext` keys) | #4380 plan, PR-1 | The contributor |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Resilience` (`ResilienceTimeout`, registered by #335 PR-3) | 955-957 | Three ids from the spare 955-959 of the #335 range; no registry row or sibling plan claims them; no new range is created here |

### Estimated File Count

| PR | Files | Content |
|----|-------|---------|
| PR-1 | 12 | `ITenantTimeoutResolver.cs`, `TenantTimeout.cs`, `TenantTimeoutOptions.cs`, `TenantTimeoutOptionsValidator.cs`, `OptionsTenantTimeoutResolver.cs`, `TenantTimeoutServiceCollectionExtensions.cs`, `OptionsTenantTimeoutResolverTests.cs`, `TenantTimeoutOptionsValidatorTests.cs`, `TenantTimeoutRegistrationTests.cs`, `TenantTimeoutGuardTests.cs`, `PublicAPI.Unshipped.txt`, manifest rows |
| PR-2 | 15 | `EffectiveTimeout.cs`, `TenantTimeoutLookup.cs`, `TenantTimeoutCallContributor.cs`, `Diagnostics/TenantTimeoutLogMessages.cs`, `TenantTimeoutServiceCollectionExtensions.cs`, `TimeoutStrategyFactory.cs` and `TimeoutDeadlineContributor.cs` (modified); `TenantTimeoutLookupTests.cs` (also covers the contributor and `EffectiveTimeout`), `TenantTimeoutRegistrationTests.cs`, `TimeoutStrategyFactoryTests.cs`, `TimeoutDeadlineContributorTests.cs` and `TenantTimeoutGuardTests.cs` (modified), `TenantTimeoutDeadlineProperties.cs`, `PublicAPI.Unshipped.txt`, manifest rows (counted as one) |
| PR-3 | 6 | feature page (modified), README section, changelog fragment, two justification `.md` (load, benchmark), manifest rows |

Each PR is within 15 files and 1,500 lines. The `ProjectReference` of the test projects to `Encina.Resilience` and `InternalsVisibleTo` are acceptance items of #4380 (PR-1 of the foundation adds the `Encina.UnitTests` reference; the `Encina.GuardTests` and `Encina.IntegrationTests` references arrive in its PR-2 and PR-4); the property test needs the `Encina.PropertyTests` reference: if the foundation has not added it, that one `csproj` line moves the property test to PR-3 (PR-2 stays at 15 files, the limit, and the property target of `TenantTimeoutLookup.cs` is then set in PR-3). No test double is affected (no interface of core changes; `ITenantTimeoutResolver` is new and has no implementer other than the default).

Per-file targets (new files; baseline: none exists on `origin/main`, and `.github/coverage-manifest/Encina.Resilience.json` is created by #4380 PR-1):

| File | Unit | Guard | Contract | Property | Integration | Justification |
|------|------|-------|----------|----------|-------------|---------------|
| `ITenantTimeoutResolver.cs` | 0 | 0 | 0 | 0 | 0 | Interface, no executable code |
| `TenantTimeout.cs` | 90 | 0 | 0 | 0 | 0 | Record struct read by the resolver and factory tests; no parameters to validate |
| `TenantTimeoutOptions.cs` | 90 | 0 | 0 | 0 | 0 | Property bag read by the validator and resolver tests |
| `TenantTimeoutOptionsValidator.cs` | 95 | 15 | 0 | 0 | 0 | One test per rule; the guard flag reaches the single null check |
| `OptionsTenantTimeoutResolver.cs` | 95 | 15 | 0 | 0 | 0 | Known tenant, unknown tenant, reload; the guard flag reaches the constructor check |
| `TenantTimeoutLookup.cs` | 92 | 20 | 0 | 55 | 0 | Every branch of choice 3 with a fake resolver; the property test drives the arithmetic of choice 5 |
| `EffectiveTimeout.cs` | 100 | 0 | 0 | 55 | 0 | Pure function of choices 4 and 5; unit tests per rule and the deadline property |
| `TenantTimeoutCallContributor.cs` | 90 | 25 | 0 | 0 | 0 | Covered by `TenantTimeoutLookupTests.cs` (PR-2) and registered through DI; the guard file of PR-2 reaches the argument checks of the contributor method |
| `TenantTimeoutServiceCollectionExtensions.cs` | 85 | 40 | 0 | 0 | 0 | The null check is a large share of a ten-line method; the DI test reaches the registrations |
| `TenantTimeoutLogMessages.cs` | 85 | 0 | 0 | 0 | 0 | Log assertions with a fake logger; generated code with no parameters to guard |
| `TimeoutStrategyFactory.cs`, `TimeoutDeadlineContributor.cs` (modified, files of #335) | the targets written by #335 are kept and never lowered (#335 plan: factory unit 90 and contract 80, contributor unit 90) | 0 | as #335 | 0 | 0 | The new branches are covered by the modified tests of this PR |

CRAP of each added method is at most 10 (the lookup splits into one small method per condition of choice 3).

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT: Encina (pre-1.0, .NET 10, C# 14). Issue #4391: tenant-scoped timeout values on top of the #335 timeout strategy in Encina.Resilience (ADR-055 R2, R3, R5, R6). AGENTS.md rules apply. The foundation (#4380), the token and deadline (#4382) and the #335 implementation are merged first; if missing, stop and report.

IMPLEMENTATION OVERVIEW: (1) ITenantTimeoutResolver, TenantTimeout, TenantTimeoutOptions and validator; (2) the options-based default resolver; (3) an order-independent get-or-resolve helper, a call contributor, changes to the #335 strategy factory and deadline contributor, and AddEncinaTenantTimeouts; (4) three log messages and the conditional tenant tag on the span event; (5) unit, guard, property and DI tests; (6) documentation and fragments.

KEY PATTERNS: a missing, unknown or failing tenant lookup uses the type value and is logged by code; a resolved value never extends a deadline; no pipeline per tenant; no scoped service in a singleton; opt-in registration; time through TimeProvider; specialists for PublicAPI, changelog, manifest and docs.

REFERENCE FILES: docs/architecture/adr/055-resilience-family.md, src/Encina/Abstractions/IRequestContext.cs, src/Encina/Abstractions/MultiTenancyMarker.cs
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | The default resolver reads in-memory options with no I/O; an application's remote resolver caches its own lookups; no caching code is built here |
| 2 | OpenTelemetry | ✅ | The tenant tag is #335's; this plan adds a `tenant.tier` tag on its `timeout` span event, only with multi-tenancy (choice 6) |
| 3 | Structured Logging | ✅ | Three definitions, EventIds 955-957 of the #335 range |
| 4 | Health Checks | ❌ | No dependency or persistent state |
| 5 | Validation | ✅ | Options validated on start; an invalid resolved value is ignored and logged |
| 6 | Resilience | ✅ | Extends the #335 timeout strategy |
| 7 | Distributed Locks | ❌ | No shared mutable state |
| 8 | Transactions | ❌ | No transactional work |
| 9 | Idempotency | ❌ | The value is recomputed per call and changes no state |
| 10 | Multi-Tenancy | ✅ | The feature; fail-safe default for a missing tenant (choice 3); no tenant-qualified key is built |
| 11 | Module Isolation | ❌ | Module-independent |
| 12 | Audit Trail | ❌ | A time budget is an operational setting, not an audited access decision |

---

## Prerequisites & Dependencies

| Prerequisite | State | Effect |
|--------------|-------|--------|
| #4380 (`Encina.Resilience` foundation) | Plan on `main`; not implemented | Blocks PR-1 (project, hooks) |
| #4382 (token and deadline, R6) | Plan on `main`; not implemented | Blocks the deadline part of PR-2 through #335 |
| #335 choice 7 (how the shortened deadline becomes visible: a contributor that sets and restores the accessor, option A; or R6 carrying the context, option B) | Pending Dilemma | PR-2 modifies the contributor that option A creates; if the maintainer picks option B there, PR-2 is re-planned |
| #335 implementation (timeout strategy, deadline contributor, EventIds) | Plan on `main`; not implemented | Blocks PR-2 (the two files it modifies) |
| #740 (tenant rate limiting) | Plan on `main`; Dilemma 3 (tiers) pending | Choice 2 option B only |
| #728 (telemetry names) | Plan on `main`; choices pending | Tag and event names of choice 6 |

---

## Next Steps

1. Commit this plan, open its PR and link it from #4391.
2. When #4391 is picked up, re-check the choices against today's code and ask the maintainer about the Dilemmas (choices 1 to 4 and 6).
3. Brief workers for PR-1 to PR-3 in order, after #4380, #4382 and the #335 implementation are merged.
