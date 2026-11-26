# Implementation Plan: Cached-value fallback strategy over the fail-safe cache (#268)

> **Issue**: [#4394](https://github.com/dlrivada/Encina/issues/4394)
> **Type**: Feature (follow-up of #137)
> **Complexity**: High (7 phases, 8 caches, 6 pull requests)
> **Estimated Scope**: ~280 lines of production code + ~900 lines of tests (about 1,500 changed lines in total, six pull requests)

---

## Summary

Offer `FallbackStrategy.CachedValue`: when a query fails, serve the last known good value through the fail-safe capability of the caches (#268), not through a copy owned by the resilience package. The plan of #137 leaves the member out and defers it to #268 (its choice 5, option A); ADR-055 settles that fail-safe is a caching feature. This plan adds the missing link: a narrow read seam that `Encina.Caching` implements with the same key, tenant and user rules as `QueryCachingPipelineBehavior`, and a fourth source of the `[Fallback]` attribute that calls it.

What exists today (read in `origin/main` at `4047d9a8a`), and what differs from the issue text:

- The issue speaks of "the abstraction #268 defines", but #268 defines none that a resilience strategy can call. Its plan (on `main`, not implemented) adds the stale path inside `QueryCachingPipelineBehavior` (`src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs:45`), an internal `FailSafePolicy`, and a public in-process `FailSafeTracker`; the stored envelope `CacheEntry<T>` is `internal` (`QueryCachingPipelineBehavior.cs:353`) and holds only `Value` and `CachedAtUtc` today. Nothing outside the behavior can read a stale value, so the seam of this plan is new work, not an adapter over existing code.
- `Encina.Caching` references `Encina`, `Encina.DistributedLock`, `Encina.Messaging` and `Microsoft.EntityFrameworkCore` (`src/Encina.Caching/Encina.Caching.csproj`). A reference from `Encina.Resilience` to it would pull those into every resilience user; choice 2 settles the direction.
- Pipeline position today: `QueryCachingPipelineBehavior` declares no `[PipelineStage]` (`git grep PipelineStage -- src/Encina.Caching` has no match), so it runs in the default stage `Application` (`src/Encina/Pipeline/Ordering/PipelineStageAttribute.cs:15`, `PipelineOrderResolver.cs:304`), which is inside `Resilience` (`PipelineStage.cs:95,119`: 1100 against 1500). The open issue #2184 places it in `Caching` (1000), outside `Resilience`. This matters to choice 1: with the cache outside the resilience pipeline, #268's own stale path already answers every failure the resilience strategies produce (open circuit, timeout, exhausted retry, shed request), and a `CachedValue` fallback adds nothing for them.
- The behavior already fails closed for the cache: with multi-tenancy on and no tenant, a tenant-scoped entry bypasses the cache with no read (`QueryCachingPipelineBehavior.cs:141-150`), and a per-user entry without a user does the same (`:152-163`).
- #137 (plan on `main`, not implemented) has `FallbackStrategy { DefaultValue, FactoryMethod, AlternativeHandler }`, `FallbackAttribute` with three exclusive sources, and triggers `Unavailable`, `Timeout`, `UpstreamFailure` and `Internal` widenable with `FallbackOn` (its choice 7). #268 serves stale only for `Unavailable`, `Timeout`, `UpstreamFailure` and `RateLimited` and fails closed on `Internal` (its choice 1): choice 3 reconciles them.

What this plan delivers: `IStaleResultSource<TRequest, TResponse>` and `StaleResult<TResponse>` (the seam), an internal `CacheEligibility` extracted from the behavior so the reader and the behavior apply one set of bypass and key rules, `CacheStaleResultSource<,>` in `Encina.Caching` and its registration, the `CachedValue` member, `UseCachedValue` attribute property, validator rule and strategy source in `Encina.Resilience`, the counter tag value `cached`, and the contract, integration and DI tests over the caches.

- **Affected packages**: `Encina` (core, the seam interface), `Encina.Caching` (the reader), `Encina.Resilience` (planned; the strategy). The 7 cache provider packages on `main` (Memory, Hybrid, Redis, Valkey, Dragonfly, Garnet, KeyDB) change nothing: the reader depends only on `ICacheProvider`. Memcached (#277, planned) is covered by one derived test class when its package lands.
- **Provider category**: Caching (8), behavior-level (AGENTS.md section 5); all providers are served through `ICacheProvider`.
- **Estimated size**: 6 pull requests of at most 15 files and 1,500 lines (split in "Implementation Phases").
- **Prerequisites**: #268 PR 1 (the stale path and `FailSafeTracker`), the #137 implementation and #4380, described in "Prerequisites & Dependencies".

### Shared Pieces and Ownership (family resilience follow-ups: #4391, #4392, #4393, #4394, #4395)

| Shared piece | Owner | Used by |
|--------------|-------|---------|
| `Encina.Resilience` project, call-contributor hook, `ResilienceContext` keys | #4380 (plan, by number) | the strategy source of this plan |
| `FallbackStrategy`, `FallbackAttribute`, `FallbackStrategyFactory`, `FallbackCallContributor`, the fallback counter and EventIds 906-911 | #137 (plan on `main`, by number) | this plan modifies them |
| `FailSafePolicy`, `FailSafeTracker`, the envelope fields `ExpiresAtUtc` and `FailSafeExpiresAtUtc`, EventIds 462-468 | #268 (plan on `main`, by number; `ExpiresAtUtc` and the pattern settings come from #266) | the reader reads them |
| `IStaleResultSource<,>`, `StaleResult<T>`, `CacheEligibility`, `CacheStaleResultSource<,>` | **This plan (#4394)** | none |
| Stage placement of `QueryCachingPipelineBehavior` | #2184 (open issue, by number) | choice 1 |
| The Memcached provider | #277 (plan on `main`) | the 8th cache of the contract suite |

---

## Design Choices

<details>
<summary><strong>1. How a cached value reaches a failed query: a read seam and strategy, composition by pipeline order, or a direct read from Resilience</strong></summary>

The issue wants the last known good value served on failure through #268's fail-safe, not through a copy owned by resilience.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A read seam in core, implemented by `Encina.Caching` with the behavior's own rules; `[Fallback(UseCachedValue = true)]` calls it** | Works wherever the cache behavior sits in the pipeline; honors the fallback slot, triggers and telemetry of #137; one stale mechanism (#268) | New public API (an interface and a record struct in core) and a refactor of the behavior to share its rules |
| **B) No new API: `QueryCachingPipelineBehavior` moves to the `Caching` stage as #2184 plans, so #268's stale path wraps the whole resilience pipeline and serves stale on every transient `Left` it produces** | No code here; no second path to keep consistent | Depends on #2184 landing; an admission rejection (stage 200, outside the cache) is still not served from the cache, and #137's per-type `[Fallback]` ordering and `FallbackOn` do not apply to the cache path |
| **C) `Encina.Resilience` reads the live `[Cache]` entry itself through `ICacheProvider` and `ICacheKeyGenerator`** | Works today with no change in core | A reference from `Encina.Resilience` to `Encina.Caching` (EF Core, Messaging, DistributedLock); the envelope is internal, so it cannot read stale entries; the tenant and user rules are repeated |

### Chosen Option: **A - Read seam and strategy** (recommended, pending the maintainer)

### Rationale

- A wins while #2184 has not landed or when a type wants the fallback slot and trigger rules of #137; B wins once #2184 lands as written and the failures of interest are those of the `Resilience` stage, because it is less code and has no second path; C wins only if the maintainer accepts `Encina.Resilience` referencing `Encina.Caching` and live entries are enough.
- Under B this plan is a documentation page and one end-to-end test proving the composition; its phases 1 to 3 are not built.

- **Class:** Dilemma

</details>

<details>
<summary><strong>2. Where the seam lives and which way the package reference points (applies under choice 1 option A)</strong></summary>

`Encina.Resilience` must call something `Encina.Caching` implements. ADR-055 R8 states the principle that `Encina.Resilience` "depends only on the abstraction, never on a concrete cache".

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The interface and its result type live in core `Encina`; `Encina.Caching` implements; `Encina.Resilience` consumes; no new package edge** | Both packages already reference core; pay-for-what-you-use holds | Two new public types in core |
| **B) The interface lives in `Encina.Resilience`; `Encina.Caching` references `Encina.Resilience` and implements it** | Core stays unchanged | Every cache user takes the resilience package and Polly |
| **C) `Encina.Resilience` references `Encina.Caching` and reads through its types** | No interface | The heavy reference of choice 1 option C |

### Chosen Option: **A - Interface in core** (recommended, pending the maintainer)

### Rationale

- A wins on dependency direction and opt-in; B wins if the maintainer prefers that capability interfaces live with their consumer and accepts the edge; C wins if `Encina.Resilience` may reference caching.
- The generic form `IStaleResultSource<TRequest, TResponse>` (constraint `TRequest : IRequest<TResponse>`) is resolved from the request's service provider per call by the #137 call contributor (the pattern of #137 choice 9), so no scoped service is captured by the singleton pipeline.

- **Class:** Dilemma

</details>

<details>
<summary><strong>3. Which failures may be answered with a cached value</strong></summary>

#137 triggers on `Unavailable`, `Timeout`, `UpstreamFailure` and `Internal`, widenable by `FallbackOn`; #268 serves stale on `Unavailable`, `Timeout`, `UpstreamFailure` and `RateLimited` only, because an `Internal` error such as `dsr.restriction_active` or `abac.indeterminate` can be a definitive denial that old data must not mask.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The reader applies #268's `FailSafePolicy` itself: it serves a value only when the failure is one #268 serves stale for and the key is not marked "answered definitively", whatever `FallbackOn` lists (the intersection of the two rules)** | A denial is never masked by old data; one eligibility rule for both paths | A user who lists `Internal` in `FallbackOn` together with `UseCachedValue` still gets no cached value for it |
| **B) The `FallbackOn` set of #137 decides** | One rule on the resilience side | An `Internal` denial can be answered with a stale value, the case #268 closes |

### Chosen Option: **A - Intersection with #268's rule** (recommended, pending the maintainer)

### Rationale

- A wins when correctness of access decisions matters (health or personal data); B wins for a catalog page where any failure should show the last page and no denial exists. A cancellation, an `OperationCanceledException` and a `Left` of category `Cancelled` are never answered (ADR-062 rule R5; #137 choice 7).

- **Class:** Dilemma

</details>

<details>
<summary><strong>4. Test breadth over the 8 caches</strong></summary>

The reader uses only `ICacheProvider.GetAsync`, so behavior should be identical on every cache; the issue asks for one contract suite over the 8 caches and integration tests on cache containers. The repository has an integration fixture for Redis only (`tests/Encina.IntegrationTests/Infrastructure/Caching/Fixtures/RedisFixture.cs`); Valkey, Dragonfly, Garnet and KeyDB have none, and the Memcached package is not on `main`.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One abstract contract suite, run in-process on Memory and Hybrid and, through four new collection fixtures, on Redis, Valkey, Dragonfly, Garnet and KeyDB; a Memcached class when #277 lands** | Proves the consistency rule of AGENTS.md section 5 for every cache on `main`; catches serialization and TTL differences | Four new fixtures (eight files) in a separate pull request |
| **B) Contract on Memory and Hybrid, integration on Redis only, with a written justification that the other four speak the Redis protocol through the same client** | Small | The four are untested for this feature; the justification is the kind of claim AGENTS.md section 9 asks to prove |

### Chosen Option: **A - All caches on `main`** (recommended, pending the maintainer)

### Rationale

- A wins when providers can differ in TTL or serialization behavior; B wins if the maintainer judges the protocol-compatible stores equivalent and wants fewer container fixtures. Integration classes use `[Collection("...")]` fixtures only, never `IClassFixture` (AGENTS.md section 9).

- **Class:** Dilemma

</details>

<details>
<summary><strong>5. Resilience owns no copy of responses</strong></summary>

The issue's third alternative is an "own last-known-good copy" written by the resilience package on every success. ADR-055 settled points say stampede protection, fail-safe and eager refresh are caching-provider features on every cache, not resilience strategies; AGENTS.md section 5 lists them in the caching row.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) No copy in `Encina.Resilience`: the value always comes from the cache through the seam (choice 1 options A to C all comply)** | One stale mechanism; responses (possibly health data) are stored only under the caching rules | Without `[Cache]` and fail-safe configured there is no value to serve |
| **B) The fallback writes its own copy on every success** | Always available | Duplicates fail-safe; stores responses outside the caching rules (tenant, user and retention rules); a write on every call |

### Chosen Option: **A - No copy**

### Rationale

- B contradicts the ADR-055 settled point; A is the only option that complies, and it holds under every option of choices 1 to 4.
- **Class:** Settled
- **Settled by:** ADR-055 settled points ("Stampede protection, fail-safe and eager refresh are caching-provider features on every cache, not resilience strategies"); AGENTS.md section 5 (caching row)

</details>

<details>
<summary><strong>6. Never another tenant's or user's value: the reader fails closed with the behavior's rules</strong></summary>

The acceptance criterion is "never serves another tenant's or user's value". The behavior bypasses the cache with no read when a tenant-scoped entry has no tenant under multi-tenancy, and when a per-user entry has no user (`QueryCachingPipelineBehavior.cs:141-163`), and it reads an entry only while the caller is still the identity the key was built from (`:174-180`).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The reader applies the same eligibility, key generation and caller check, through one shared internal `CacheEligibility` that the behavior also uses, and returns no value (the primary failure stands) when a rule bypasses** | One set of rules cannot drift; a gate denies by default when context is missing | A refactor of the behavior (its existing targets are kept) |
| **B) The reader builds a key from whatever context it has (a tenant-less key when no tenant)** | Simpler reader | The tenant-less key is shared by every tenant's callers: another tenant's value can be served |

### Chosen Option: **A - Shared rules, fail closed**

### Rationale

- B serves a key shared across tenants and violates the fail-closed rule; A is the only option that satisfies both the rule and the acceptance criterion. It holds under every option of choices 1 to 4 that builds the reader (A of choice 1; under B and C of choice 1 the reader does not exist or is replaced by a direct read that must apply the same rules).
- **Class:** Settled
- **Settled by:** AGENTS.md section 3 ("Compliance and security gates fail closed"); `src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs:141-163`

</details>

---

## Implementation Phases

Six pull requests, in order: **PR-1** Phases 1 and 2 (the seam, the shared eligibility, the reader and its registration, in core and `Encina.Caching`; it needs #268 PR 1 merged because it edits the same behavior); **PR-2** Phase 3 part 1 (`FallbackStrategy` member, attribute property, options and validator in `Encina.Resilience`); **PR-3** Phase 3 part 2 (strategy source, call contributor, property keys, telemetry value); **PR-4** the contract suite (Phase 6, in-process caches); **PR-5** the four container fixtures and the integration classes (Phase 6); **PR-6** Phase 7 (documentation). PR-2 and PR-3 need the #137 implementation; PR-3 needs PR-1.

### Phase 1: Core Models and Contracts

<details>
<summary>Tasks</summary>

1. `src/Encina/Abstractions/IStaleResultSource.cs`: `public interface IStaleResultSource<TRequest, TResponse> where TRequest : IRequest<TResponse> { ValueTask<StaleResult<TResponse>> TryGetAsync(TRequest request, IRequestContext context, EncinaError failure, CancellationToken cancellationToken); }`; XML docs state that a result is returned only for the caller whose key matches.
2. `src/Encina/Abstractions/StaleResult.cs`: `public readonly record struct StaleResult<TResponse>(bool Found, TResponse? Value)` with `static StaleResult<TResponse> None` and `static StaleResult<TResponse> From(TResponse value)`.
3. Core `PublicAPI.Unshipped.txt` lines and the `Encina.json` manifest rows (`mechanical-fixer`).

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT: Encina issue #4394, plan docs/plans/cached-value-fallback-implementation-plan-4394.md. #268 PR 1 is merged (FailSafePolicy, FailSafeTracker, the envelope with FailSafeExpiresAtUtc).
TASK: Add IStaleResultSource<TRequest, TResponse> and StaleResult<TResponse> to core.
KEY RULES: no reference from core to a caching package; the result carries no error; XML docs with <example>; PublicAPI lines through mechanical-fixer.
REFERENCE FILES: src/Encina/Abstractions/CqrsContracts.cs, src/Encina/Abstractions/IRequestContext.cs
```

</details>

### Phase 2: Default Implementation in `Encina.Caching`

<details>
<summary>Tasks</summary>

1. `src/Encina.Caching/Patterns/CacheEligibility.cs` (internal sealed): the bypass decisions of `ShouldUseCache`, `TenantAllowsCache`, `UserAllowsCache`, the key context and the caller check, extracted from `QueryCachingPipelineBehavior` (`:131-163`, `:196-201`) unchanged in behavior; the log call keeps the existing EventId 3513.
2. `src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs` (modified): delegates to `CacheEligibility`; its existing targets (unit 98, guard 20, contract 0 in `.github/coverage-manifest/Encina.Caching.json`) are kept and the existing tests pass unchanged.
3. `src/Encina.Caching/Patterns/CacheStaleResultSource.cs` (internal sealed, implements `IStaleResultSource<TRequest, TResponse>`; constructor takes `ICacheProvider`, `ICacheKeyGenerator`, `CachingOptions`, `FailSafeTracker`, `CacheEligibility`, `TimeProvider`, `ILogger`): applies choice 6, then `FailSafePolicy.IsTransientFailure(failure)` (choice 3), then reads `GetAsync<CacheEntry<TResponse>>` and returns the value when `CanServeStale(now, entry.FailSafeExpiresAtUtc)` and the key is not marked definitive; a cache read failure returns `StaleResult.None` and logs the exception type through `ForLogging()` (it never throws unless `ThrowOnCacheErrors` is set, as the behavior does); a cancelled token propagates; it logs the existing #268 events 462 (stale served) and 466 (fail-safe exhausted); no new EventId.
4. `src/Encina.Caching/ServiceCollectionExtensions.cs` (modified): `TryAddTransient(typeof(IStaleResultSource<,>), typeof(CacheStaleResultSource<,>))` and `TryAddSingleton<CacheEligibility>()`, registered with the existing caching registration so nothing is added without it; the DI test builds with `ValidateOnBuild` and `ValidateScopes`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT: Phase 1 merged.
TASK: Extract CacheEligibility, make the behavior use it, implement CacheStaleResultSource and register it.
KEY RULES: no change in behavior of QueryCachingPipelineBehavior (all its tests pass unchanged); the reader fails closed (no tenant under multi-tenancy, no user for a per-user entry, caller no longer the identity: no value); a stale value is never served for a failure outside #268's transient set or past FailSafeExpiresAtUtc or for a key marked answered-definitively; never log EncinaError.Message or the cache key (CacheKeyRedactor); time from TimeProvider; CRAP of each method at most 10.
REFERENCE FILES: src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs, src/Encina.Caching/KeyGeneration/CacheKeyRedactor.cs, docs/plans/cache-fail-safe-implementation-plan-268.md
```

</details>

### Phase 3: Strategy Integration in `Encina.Resilience`

<details>
<summary>Tasks</summary>

1. PR-2: `Fallback/FallbackStrategy.cs` (modified: `CachedValue`), `FallbackAttribute.cs` (modified: `bool UseCachedValue { get; init; }`, the fourth exclusive source), `FallbackOptions.cs` (modified), `FallbackOptionsValidator.cs` (modified: exactly one of four sources; `UseCachedValue` on a type that is a command is rejected like the other sources, per #137 choice 4).
2. PR-3: `FallbackStrategyFactory.cs` (modified: the `CachedValue` source calls `IStaleResultSource<,>.TryGetAsync` with the primary `Left`; a `None` result returns the primary result unchanged; a value is returned as `Right`), `FallbackCallContributor.cs` (modified: resolves `IStaleResultSource<TRequest, TResponse>` per call from the request's service provider, as it does for `IFallbackHandler<,>`; a missing registration, which means `Encina.Caching` is not registered, is an `InvalidOperationException` at startup naming the request type), `FallbackPropertyKeys.cs` (modified), `FallbackTelemetry.cs` (modified: the counter tag `fallback.strategy` takes the value `cached`).
3. A request type with `UseCachedValue` but no `[Cache]`, or `[Cache]` without fail-safe, gets `StaleResult.None` (a live entry may still exist; otherwise the primary failure stands); `Encina.Resilience` cannot read `CacheAttribute` without referencing `Encina.Caching` (choice 2), so there is no startup check for it and the documentation says so.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT: Phases 1 and 2 merged; the #137 implementation and #4380 are merged.
TASK: Add the CachedValue source to the fallback attribute, options, validator, factory, contributor and telemetry.
KEY RULES: exactly one source per attribute; the cached value is read only through IStaleResultSource; a missing registration fails at startup; cancellation propagates; the fallback is never applied to a command; no new error code.
REFERENCE FILES: docs/plans/fallback-implementation-plan-137.md, src/Encina/Abstractions/StaleResult.cs
```

</details>

### Phase 4: Cross-Cutting Integration

<details>
<summary>Tasks</summary>

1. Multi-tenancy (function 10): choice 6; the reader builds its key with the behavior's `ICacheKeyGenerator`, whose tenant and user segments are escaped there, so no new tenant-qualified key format exists and no `default` tenant segment is introduced by this plan (the sibling plans' convention is not exercised).
2. Telemetry tags: tenant tag only with multi-tenancy (ADR-054 D4), through #137's counter.
3. Pipeline placement: nothing moves here; the `Caching` stage assignment is #2184.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT: Phases 1 to 3 done.
TASK: Verify the cross-cutting rows and add only what they need.
KEY RULES: no new key format; no stage change; tenant tag only with multi-tenancy.
REFERENCE FILES: docs/architecture/adr/054-observability-conventions-and-trace-propagation.md
```

</details>

### Phase 5: Observability

<details>
<summary>Tasks</summary>

1. No new EventId and no new range: the reader emits the #268 events 462 and 466 (range `CachingPatterns` 450-499, registered by #266) and the existing 3513; the strategy emits the fallback-applied event of #137 (range `ResilienceFallback` 906-911). Nothing is assigned by the orchestrator for this plan.
2. The counter value `cached` of the #137 fallback counter is the only telemetry change (Phase 3, PR-3).

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT: Phases 1 to 4 done.
TASK: Confirm that no new log message is needed and that the counter value is covered by a test.
KEY RULES: never EncinaError.Message or cache keys in logs; exceptions through ForLogging().
REFERENCE FILES: src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

### Phase 6: Testing

<details>
<summary>Tasks</summary>

1. Unit (`tests/Encina.UnitTests/Caching/`): `CacheStaleResultSourceTests` (fresh and stale entry served, past `FailSafeExpiresAtUtc` none, a failure outside the transient set none for every non-transient category and for `Internal`, a key marked definitive none, cancellation propagates, a cache read failure is `None`, bypass cases none, two tenants never see each other's value, two users likewise), `CacheEligibilityTests`, the DI test (`ValidateOnBuild`, `ValidateScopes`); Resilience unit tests for the validator, the factory (value, `None`, cancellation) and the missing-registration startup failure.
2. Guard (`tests/Encina.GuardTests/Infrastructure/Caching/CacheStaleResultSourceGuardTests.cs`): the constructor nulls; `TryGetAsync` with null request, context and failure.
3. Contract (`tests/Encina.ContractTests/Caching/`): an abstract `StaleResultSourceContractTests` run against real `MemoryCacheProvider` and the Hybrid provider (PR-4); each cache derives one class.
4. Integration (PR-5, `tests/Encina.IntegrationTests/Infrastructure/Caching/`): the same suite as derived classes with `[Collection]` fixtures: the existing Redis collection and four new fixtures (Valkey, Dragonfly, Garnet, KeyDB) with their collection definitions in `Collections.cs`; `[Trait("Category", "Integration")]`. A Memcached class is added by the #277 implementation when its package lands.
5. Property, load, benchmark: not applicable; justification files for load and benchmark.

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT: Phases 1 to 5 done.
TASK: Write the Phase 6 tests.
KEY RULES: tests execute real package code; FakeTimeProvider, never Thread.Sleep; collection fixtures only (never IClassFixture, never new fixtures per class); Shouldly through Encina.Testing.Shouldly; a test never swallows an exception thrown by the code it drives.
REFERENCE FILES: tests/Encina.IntegrationTests/Infrastructure/Caching/Fixtures/RedisFixture.cs, tests/Encina.UnitTests/Caching/
```

</details>

### Phase 7: Documentation and Finalization

<details>
<summary>Tasks</summary>

1. `docs/features/resilience-fallback.md` (page of #137, modified), `docs/features/handler-caching.md` (modified) and the `Encina.Resilience` and `Encina.Caching` README sections (`docs-writer`): when to use `UseCachedValue`, the failures it answers, the need for `[Cache]` with fail-safe, the stage placement.
2. `changelog.d/4394-cached-value-fallback.added.md`, the `PublicAPI.Unshipped.txt` lines and the manifest rows (`mechanical-fixer`).
3. Verification: `dotnet build` 0 warnings; the filtered tests; `--check-justifications`.

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
| ADR-055 settled points, R1, R5 | Fail-safe belongs to the caches; strategies in `Encina.Resilience`; counter and tags |
| ADR-062 R5 | A cancellation is never a fault and never answered with a value |
| ADR-054 D4 | Tenant tag only with multi-tenancy |
| AGENTS.md section 3 (fail-closed gates, `EncinaError.Message` never logged) | Choices 3 and 6 |
| AGENTS.md section 5 (caching row, consistency across providers) | Choice 4 |
| FusionCache fail-safe, Polly fallback over a cache read | Comparable products named by the issue |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage |
|-----------|----------|-------|
| `QueryCachingPipelineBehavior`, `CacheEntry<T>` | `src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs:45,353` | Rules shared with the reader |
| `ICacheKeyGenerator`, `CacheKeyRedactor` | `src/Encina.Caching/Abstractions/`, `src/Encina.Caching/KeyGeneration/` | Key and redacted logging |
| `FailSafePolicy`, `FailSafeTracker` | #268 plan, PR 1 | Eligibility and definitive-answer marks |
| `FallbackStrategyFactory`, `FallbackCallContributor` | #137 plan | Modified |
| `PipelineStage.Caching`, `PipelineStage.Resilience` | `src/Encina/Pipeline/Ordering/PipelineStage.cs:90,95` | Placement (choice 1) |
| `RedisFixture` | `tests/Encina.IntegrationTests/Infrastructure/Caching/Fixtures/RedisFixture.cs` | Redis integration class |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Caching`, `Encina.Resilience` | none new | Reuses 462 and 466 (#268, `CachingPatterns` 450-499), 3513 (existing) and the #137 fallback-applied event |

### Estimated File Count

| PR | Files | Content |
|----|-------|---------|
| PR-1 | 13 | `IStaleResultSource.cs`, `StaleResult.cs`, `CacheEligibility.cs`, `CacheStaleResultSource.cs`, `QueryCachingPipelineBehavior.cs` and `ServiceCollectionExtensions.cs` (modified), `CacheStaleResultSourceTests.cs`, `CacheEligibilityTests.cs`, `ServiceCollectionExtensionsTests.cs` (modified), `CacheStaleResultSourceGuardTests.cs`, core `PublicAPI.Unshipped.txt`, `Encina.json` and `Encina.Caching.json` manifest rows |
| PR-2 | 9 | `FallbackStrategy.cs`, `FallbackAttribute.cs`, `FallbackOptions.cs`, `FallbackOptionsValidator.cs` (modified), `FallbackOptionsValidatorTests.cs` (modified), `FallbackAttributeTests.cs` (modified), `FallbackGuardTests.cs` (modified), `PublicAPI.Unshipped.txt`, manifest rows |
| PR-3 | 10 | `FallbackStrategyFactory.cs`, `FallbackCallContributor.cs`, `FallbackPropertyKeys.cs`, `FallbackTelemetry.cs` (modified), their four tests (modified), `CachedValueFallbackTests.cs`, manifest rows |
| PR-4 | 5 | `StaleResultSourceContractTests.cs` (abstract), `MemoryStaleResultSourceContractTests.cs`, `HybridStaleResultSourceContractTests.cs`, `Encina.ContractTests.csproj` (`ProjectReference` lines if absent), `Encina.Caching.json` manifest rows |
| PR-5 | 12 | four fixtures (`ValkeyFixture`, `DragonflyFixture`, `GarnetFixture`, `KeyDBFixture`), `Collections.cs` (modified, four definitions), abstract integration suite, five derived classes (Redis, Valkey, Dragonfly, Garnet, KeyDB), `Encina.IntegrationTests.csproj` (references if absent) |
| PR-6 | 6 | two feature pages (modified), two README sections, changelog fragment, two justification `.md` (load, benchmark) |

Each PR is within 15 files and 1,500 lines. Implementers and doubles: `IStaleResultSource<,>` is new with one implementer; no existing interface changes, and the `QueryCachingPipelineBehavior` constructor keeps its public signature (the shared `CacheEligibility` is injected through an internal path or created inside; a change to its constructor would be an optional trailing parameter like the #268 tracker), so no `new QueryCachingPipelineBehavior(` site changes.

Per-file targets (existing manifest values quoted from `.github/coverage-manifest/Encina.Caching.json`; new files have no row on `origin/main`):

| File | Unit | Guard | Contract | Property | Integration | Justification |
|------|------|-------|----------|----------|-------------|---------------|
| `IStaleResultSource.cs` (core) | 0 | 0 | 0 | 0 | 0 | Interface, no executable code; the `Encina.json` package targets (unit 70, guard 20, contract 15, property 80, integration 85) are unchanged |
| `StaleResult.cs` (core) | 90 | 0 | 0 | 0 | 0 | Two factories and one property; no parameters to validate |
| `CacheEligibility.cs` | 100 | 0 | 0 | 0 | 0 | Pure rules, every branch of choice 6 by unit test; internal, no guarded public parameters |
| `CacheStaleResultSource.cs` | 95 | 15 | 70 | 0 | 60 | Every branch of choices 3 and 6 with a fake provider; guard reaches the constructor and method checks; contract and integration run the real caches |
| `QueryCachingPipelineBehavior.cs` (modified) | 98 | 20 | 0 | 0 | 0 | Existing manifest targets kept (unit 98, guard 20, contract 0); the refactor must not lower them |
| `ServiceCollectionExtensions.cs` of `Encina.Caching` (modified) | 85 | 30 | 0 | 0 | 0 | The file has no per-file row today (only the package targets: unit 60, guard 30, contract 10); this PR adds a row so the touched file has its own targets; the DI test reaches the new registrations and the existing null checks are the guard share |
| `FallbackStrategyFactory.cs` and the other #137 files (modified) | the #137 targets are kept and never lowered | as #137 | as #137 | 0 | 0 | New branches covered by the modified tests of PR-2 and PR-3 |

CRAP of each added method is at most 10 (the reader splits its checks into small methods; `Handle` of the behavior keeps its CRAP by delegating).

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT: Encina (pre-1.0, .NET 10, C# 14). Issue #4394: FallbackStrategy.CachedValue over the fail-safe cache of #268, in Encina (seam), Encina.Caching (reader) and Encina.Resilience (strategy). AGENTS.md rules apply. #268 PR 1, the #137 implementation and #4380 are merged first; if missing, stop and report.

IMPLEMENTATION OVERVIEW: (1) IStaleResultSource and StaleResult in core; (2) CacheEligibility extracted from the behavior, CacheStaleResultSource and its registration; (3) CachedValue member, UseCachedValue, validator, factory source, contributor resolution and counter value; (4) unit, guard, DI tests, a contract suite over the in-process caches and integration classes over the container caches; (5) documentation and fragments.

KEY PATTERNS: no copy of responses in Encina.Resilience; the reader fails closed with the behavior's rules and never serves another tenant's or user's value; a value only for #268's transient failures and never for a cancellation; no new EventId; nothing registered without the caching registration; specialists for PublicAPI, changelog, manifest and docs.

REFERENCE FILES: src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs, docs/plans/cache-fail-safe-implementation-plan-268.md, docs/plans/fallback-implementation-plan-137.md, docs/architecture/adr/055-resilience-family.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ✅ | The feature: the value comes from the cache through the seam |
| 2 | OpenTelemetry | ✅ | Counter tag `fallback.strategy = cached` on the #137 counter; tenant tag only with multi-tenancy |
| 3 | Structured Logging | ✅ | Reuses events 462, 466 (#268), 3513 and the #137 event; no new id |
| 4 | Health Checks | ❌ | No dependency or state of its own |
| 5 | Validation | ✅ | Exactly one fallback source; a missing registration fails at startup |
| 6 | Resilience | ✅ | A fallback source of #137 |
| 7 | Distributed Locks | ❌ | Read-only; no shared mutable state |
| 8 | Transactions | ❌ | Queries only |
| 9 | Idempotency | ❌ | A read; changes no state |
| 10 | Multi-Tenancy | ✅ | Choice 6: fail closed with the behavior's tenant and user rules |
| 11 | Module Isolation | ❌ | Module-independent |
| 12 | Audit Trail | ❌ | Serving a value the caller could already read is not a new access path; the cache rules of #268 apply |

---

## Prerequisites & Dependencies

| Prerequisite | State | Effect |
|--------------|-------|--------|
| #268 PR 1 (stale path, `FailSafePolicy`, `FailSafeTracker`, envelope fields) | Plan on `main`; not implemented | Blocks PR-1 (same behavior file) |
| #266 PR 1 (`ExpiresAtUtc`, pattern settings) | Plan on `main`; not implemented | Prerequisite of #268 |
| #137 implementation (`FallbackStrategy`, attribute, factory) | Plan on `main`; not implemented; its choice 5 option A is this plan's premise (if the maintainer picks B or C there, this plan is re-planned) | Blocks PR-2 and PR-3 |
| #4380 (`Encina.Resilience` foundation) | Plan open (PR #4388); not implemented | Blocks PR-3 |
| #2184 (stage placement of built-in behaviors) | Open issue | Decides whether choice 1 option B becomes sufficient |
| #277 (Memcached provider) | Plan on `main`; not implemented | The 8th cache; one derived test class when it lands |

---

## Next Steps

1. Commit this plan, open its PR and link it from #4394.
2. When #4394 is picked up, re-check the choices against today's code and ask the maintainer about the Dilemmas (choices 1 to 4).
3. Brief workers for PR-1 to PR-6 in order, after #268 PR 1, the #137 implementation and #4380 are merged.
