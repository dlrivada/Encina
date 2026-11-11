# Implementation Plan: Cache for ASP.NET Core resource authorization results

> **Issue**: [#710](https://github.com/dlrivada/Encina/issues/710)
> **Type**: Feature
> **Complexity**: Medium (7 phases, 1 package touched, 3 pull requests)
> **Estimated Scope**: ~600-800 lines of production code + ~1,500 lines of tests

---

## Summary

Add an opt-in decorator, `CachedResourceAuthorizer`, over `IResourceAuthorizer` (`src/Encina.AspNetCore/Authorization/`). It caches the allow result of a resource authorization for a configurable TTL so the same principal, policy and scope do not pay the policy evaluation twice. The facade it decorates is `internal sealed ResourceAuthorizer`, registered as `TryAddScoped<IResourceAuthorizer, ResourceAuthorizer>()` in `AddEncinaAuthorization`; it takes the caller from `IRequestContextAccessor` (never `HttpContext.User`, #1705) and returns `Task<Either<EncinaError, bool>>`: `Right(true)` when allowed and `Left(AuthorizationResourceDenied)` or `Left(AuthorizationUnauthenticated)` otherwise (a `Right(false)` never occurs).

Three facts of today's code reshape the issue's sketch:

1. **The method signature differs from the issue**: `AuthorizeAsync<TResource>(TResource resource, string policy, CancellationToken)` and `AuthorizeAsync(object resource, string policy, CancellationToken)`; the user is not a parameter. The decorator reads the principal from the same `IRequestContextAccessor`.
2. **Resource-based policies depend on the resource instance by design.** The interface's own example calls `AuthorizeAsync(order, "CanEditOrder", ct)` after loading the order. A key of resource type, policy and claims would return one order's decision for another (Dilemma D2).
3. **Only allows can be cached safely**: the inner result is `Right(true)` or a `Left`; the decorator caches `Right(true)` only and passes every `Left` through.

The `[Authorize]` pipeline gate (`AuthorizationPipelineBehavior`) is out of scope: the issue targets `IResourceAuthorizer`.

Provider category: **Caching (8)**, through `ICacheProvider` only. Affected packages: `Encina.AspNetCore` (already references `Encina.Caching`); the tenant and module key grammar follows #746. No database provider code.

---

## Design Choices

<details>
<summary><strong>1. Which cache backs the decisions (Dilemma D1)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ICacheProvider` (the application's registered provider)** | One caching abstraction; a revocation can be invalidated on every instance when the provider is distributed; local behavior is available by registering the Memory provider; works with all eight providers | One `ICacheProvider` per application: it cannot be Redis for queries and local for authorization at the same time; a network call per hit when distributed; decisions stored outside the process |
| **B) `IMemoryCache` (always local)** | No network hop, nothing leaves the process | A second caching path outside Encina's abstraction; instances disagree until the TTL expires |
| **C) Selectable by option (`Store`: `CacheProvider` or `Local`), default `CacheProvider`** | Both deployment shapes are possible in one application | One more setting and one more code path to test |

### Chosen Option: **A — `ICacheProvider`** (recommended, pending the maintainer)

### Rationale

A wins in multi-instance deployments where a revoked permission must take effect everywhere quickly, and it keeps the feature inside the eight-provider model (`AGENTS.md` §5). B wins for single-instance setups where the policy evaluation itself is slow and no decision may leave the process; C wins when the application needs a distributed query cache and a local authorization cache together. The decorator requires an `ICacheProvider` and fails at startup when none is registered, so it never becomes a silent no-op.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. What identifies a cacheable decision (Dilemma D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Resource type + policy + principal hash** (the issue's proposal) | Highest hit rate; correct for policies that depend only on the user and the resource type | Wrong for instance-based policies (ownership, state checks): an allow for one order is replayed for another |
| **B) A + a resource identity** (an id supplied through an `ICacheableResource` interface or a key selector) | Correct for instance-based policies | Much lower hit rate; requires every cached resource to expose a stable id and any state the policy reads; a policy that reads mutable state of the resource can still go stale |
| **C) Cache only policies the application explicitly lists as resource-independent** (`CacheablePolicies`) | Safe by construction; security review has one list to read; no change to resource types | Configuration to write; policies not listed are never cached |

### Chosen Option: **C — explicit list of resource-independent policies** (recommended, pending the maintainer)

### Rationale

An allow replayed for the wrong instance is a privilege escalation, and the facade is documented for resource-based checks, so the safe default is to cache nothing unless the application says a policy ignores the resource. A wins when security review accepts that all policies are user and type scoped; B wins when most policies are instance-based and a stable resource id exists. With C, the key is `{prefix}:auth:...:{resourceType}:{policy}:{principalHash}` and the resource instance never enters it.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Fail-closed behavior and what is cached</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Cache `Right(true)` only; every `Left`, a cache read or write failure, a missing identity or an unhashable principal evaluates through the inner authorizer** | A denial is never replayed from the cache and never turned into an allow; a broken cache costs performance, not security | Denials are always re-evaluated |
| **B) Also cache denials** | Fewer evaluations for repeated denials | Replays the failure reasons; delays a newly granted permission; the `Left` payload carries policy details |
| **C) Fail the request when the cache fails** | Surfaces cache problems | Turns a cache outage into an authorization outage |

### Chosen Option: **A — cache allows only, fall through on every doubt**

### Rationale

The decorator preserves `Either<EncinaError, bool>` and never swallows an inner `Left`. A cache failure is logged by error code or exception type only (never `EncinaError.Message`) and the inner authorizer decides.

**Class:** Settled

**Settled by:** `AGENTS.md` §3 (compliance and security gates fail closed; errors never swallowed; ROP; `EncinaError.Message` never reaches logs).

</details>

<details>
<summary><strong>4. Decorator shape and registration order</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `AddEncinaAuthorizationCaching` registers the concrete `ResourceAuthorizer` (`TryAddScoped`) and replaces `IResourceAuthorizer` with a factory that builds `CachedResourceAuthorizer` over it** | Works in either call order with `AddEncinaAuthorization`; no assembly scanning; the application's own `IResourceAuthorizer` registration is not required | The facade must be resolvable as a concrete type (internal today, same assembly) |
| **B) A decoration library such as Scrutor** | Familiar | New dependency for one decoration |
| **C) Wrap inside `ResourceAuthorizer` with an optional cache dependency** | No second type | Always-on branching in a security class; breaks opt-in clarity |

### Chosen Option: **A — concrete registration plus `Replace`**

### Rationale

`AGENTS.md` §3 requires the registration to be complete and order-independent, proven with `ValidateOnBuild` and `ValidateScopes`; the decorator is scoped like the facade because it reads the scoped request context. If the application registers its own `IResourceAuthorizer` after the call, it wins, and that is documented.

**Class:** Trivial

**Settled by:** `AGENTS.md` §3 (registration completeness: option types and dependencies registered, DI test with `ValidateOnBuild` and `ValidateScopes`; the application's own registration wins) and `src/Encina.AspNetCore/ServiceCollectionExtensions.cs` (`AddEncinaAuthorization`).

</details>

<details>
<summary><strong>5. Key grammar, principal hash and invalidation</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `{prefix}:auth:[t:{tenant}:][m:{module}:]u:{userId}:{resourceType}:{policy}:{principalHash}`; the hash is SHA-256 over the sorted `(issuer, type, value)` of every claim plus the identity kind and authentication type; `IAuthorizationCacheInvalidator` removes `...u:{userId}:*` or the whole `auth:` prefix** | Targeted invalidation by user; tenant and module follow the #746 grammar; any claim change yields a new key | Personal data (user id) in keys, as `DefaultCacheKeyGenerator` already does (redacted in logs by `CacheKeyRedactor`); per-token claims such as `jti` lower the hit rate |
| **B) Hash only roles and permissions** | Higher hit rate | Policy handlers may read other claims; two principals the handlers distinguish would share a key |
| **C) No user segment, hash only** | No personal data in keys | Cannot invalidate one user's decisions |

### Chosen Option: **A — full-claims hash with a user segment**

### Rationale

Correctness first: anything less than all claims can conflate principals that a policy tells apart. `RequestIdentity` (`src/Encina/Identity/RequestIdentity.cs`) exposes `UserId`, `Kind`, `Principal`; an anonymous or principal-less identity is never cached. Invalidation by user needs pattern removal, which the Memcached provider serves with a coarse epoch (#277 D2): documented, correct, and heavier there.

**Class:** Trivial

**Settled by:** `src/Encina.Caching/KeyGeneration/DefaultCacheKeyGenerator.cs` (segment grammar, escaping), #746 (module segment) and `AGENTS.md` §3 (fail closed: no weaker identity in the key).

</details>

<details>
<summary><strong>6. Metrics and logging</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One counter on the shared `Encina` meter, `encina.authorization.cache.requests_total`, tag `outcome` = `hit`/`miss`/`bypass`/`error`; `[LoggerMessage]` EventIds 212-217 in the existing AspNetCore range** | Same shape as `TenancyMetrics`; low cardinality; no policy name or claim in tags | Per-policy breakdown unavailable |
| **B) Separate `hit` and `miss` counters (the issue's names)** | Matches the issue text | Two instruments for one dimension; cannot express bypass and error |
| **C) A `policy` tag** | Per-policy hit rate | Policy names are application data and unbounded; the issue forbids them in tags |

### Chosen Option: **A — one counter with an `outcome` tag**

### Rationale

`TenancyMetrics` uses `new Meter("Encina", "1.0")` and tagged counters. EventIds 200-207 are used in `Encina.AspNetCore` (207 in `AuthorizationElevatedLog`); the range 200-249 is registered, so 212-217 need no new registration (208-211 are taken by the #3443 plan).

**Class:** Trivial

**Settled by:** `AGENTS.md` §7 (ADR-021, ids packed inside the registered range) and `src/Encina.Tenancy/Diagnostics/TenancyMetrics.cs` (meter and tag convention).

</details>

---

## Implementation Phases

### Phase 1: Core — options, claims hash, invalidation contract

<details>
<summary>Tasks</summary>

1. `AuthorizationCacheOptions` (public sealed) in `src/Encina.AspNetCore/Authorization/`: `Ttl` (default 5 minutes), `KeyPrefix` (default follows `CachingOptions.KeyPrefix`), `CacheablePolicies` (set of policy names, D2 option C).
2. `AuthorizationCacheOptionsValidator : IValidateOptions<AuthorizationCacheOptions>`: `Ttl` greater than zero; with D2 option C an empty list is allowed (nothing cached, logged once at startup).
3. Internal static `PrincipalHasher`: canonical string of sorted `(issuer, type, value)` claims + `IdentityKind` + authentication type, SHA-256, hex.
4. `IAuthorizationCacheInvalidator` (public): `ValueTask<Either<EncinaError, Unit>> InvalidateUserAsync(string userId, CancellationToken)` and `InvalidateAllAsync(CancellationToken)`; implemented by the decorator's companion class and registered with the decorator.
5. PublicAPI lines in `src/Encina.AspNetCore/PublicAPI.Unshipped.txt`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT: IResourceAuthorizer (src/Encina.AspNetCore/Authorization/IResourceAuthorizer.cs) returns Task<Either<EncinaError, bool>>; only Right(true) is ever an allow. The caller is the RequestIdentity of IRequestContextAccessor.
TASK: Create AuthorizationCacheOptions, its validator, the internal PrincipalHasher and the IAuthorizationCacheInvalidator contract with XML docs and PublicAPI lines.
KEY RULES: Options without secrets need no JsonIgnore, but nothing here may print a claim value in ToString or logs; the hash is deterministic and order-independent; no EncinaError.Message in logs; ROP return types.
REFERENCE FILES: src/Encina/Identity/RequestIdentity.cs, src/Encina.AspNetCore/Authorization/ResourceAuthorizer.cs, src/Encina/Identity/RequestIdentityOptionsValidator.cs, src/Encina.Caching/Configuration/CachingOptions.cs.
```

</details>

### Phase 2: The decorator

<details>
<summary>Tasks</summary>

1. `CachedResourceAuthorizer : IResourceAuthorizer` (internal sealed, scoped): constructor takes the inner `ResourceAuthorizer`, `ICacheProvider`, `IRequestContextAccessor`, `IOptions<AuthorizationCacheOptions>`, `ILogger`, `AuthorizationCacheMetrics`, `TimeProvider?` only if a clock read is needed (TTL is the provider's expiration, so none is expected).
2. Both `AuthorizeAsync` overloads: not-cacheable (policy not listed, identity anonymous or without principal, no request context) goes straight to the inner authorizer; otherwise read the cache; hit returns `Right(true)`; miss or cache error evaluates through the inner authorizer; a `Right(true)` result is stored with `Ttl`; every `Left` is returned unchanged and not stored. A cache failure is caught per call, counted as `error` and logged by exception type through `ForLogging()`.
3. The invalidator implementation uses `ICacheProvider.RemoveByPatternAsync` with the user pattern or the `auth:` prefix pattern and returns a `Left` when the provider throws (never swallowed).
4. Concurrent misses may evaluate twice; no lock (harmless, matrix function 7).

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT: Phase 1 produced the options, hasher and invalidator contract. The decorator is opt-in and must be indistinguishable from the facade for every Left.
TASK: Implement CachedResourceAuthorizer and the invalidator exactly as in the Tasks block; keep each method at cyclomatic complexity of 5 or less (CRAP at or below 10).
KEY RULES: Cache Right(true) only; never turn a Left or a cache failure into an allow; no policy name, claim or user id in metrics tags or log text; a cache failure is logged by exception type via ForLogging(), never its message.
REFERENCE FILES: src/Encina.AspNetCore/Authorization/ResourceAuthorizer.cs, src/Encina.Caching/Abstractions/ICacheProvider.cs, src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs (fail-closed precedent), src/Encina.AspNetCore/AuthorizationElevatedLog.cs.
```

</details>

### Phase 3: Configuration, DI & registration

<details>
<summary>Tasks</summary>

1. `AddEncinaAuthorizationCaching(this IServiceCollection, Action<AuthorizationCacheOptions>? configure = null)` in `src/Encina.AspNetCore/ServiceCollectionExtensions.cs` (Design Choice 4): options plus validator with `ValidateOnStart`, `TryAddScoped<ResourceAuthorizer>()`, `Replace` of `IResourceAuthorizer`, the invalidator, the metrics class (singleton).
2. Startup check: when no `ICacheProvider` is registered the options validation (or a hosted check) fails with a message naming `AddEncinaMemoryCache`/`AddEncinaRedisCache`.
3. DI tests with `ValidateOnBuild` and `ValidateScopes`, in both call orders with `AddEncinaAuthorization`, and with an application-supplied `IResourceAuthorizer`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT: AddEncinaAuthorization registers IResourceAuthorizer with TryAddScoped. The decorator must work whichever of the two calls comes first.
TASK: Add AddEncinaAuthorizationCaching, the startup dependency check and the DI tests listed.
KEY RULES: TryAdd for the concrete facade, Replace only for IResourceAuthorizer; the application's own registration wins when it comes later; options validated at startup; no static state.
REFERENCE FILES: src/Encina.AspNetCore/ServiceCollectionExtensions.cs, src/Encina.Caching.Redis/ServiceCollectionExtensions.cs, tests/Encina.UnitTests/AspNetCore/Authorization/ResourceAuthorizerTests.cs.
```

</details>

### Phase 4: Cross-Cutting Integration

<details>
<summary>Tasks</summary>

1. Caching (the feature), multi-tenancy (`t:` segment from `IRequestContext.TenantId` when present) and module isolation (`m:` segment, the source #746 settles; until then omitted).
2. Validation: options validator only. Resilience and locks: not applicable (a cache failure falls through; concurrent misses evaluate twice).
3. Audit trail: `ResourceAuthorizer` emits no audit event today, so a cache hit skips nothing. The decorator adds none. If the facade gains audit events later, a cache hit must emit the same event; this is a note in the XML docs, not a task.
4. Health checks: not applicable; the backing cache has its own.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT: AGENTS.md §6 matrix at the end of this plan lists the included functions.
TASK: Wire the tenant segment, leave the module segment to the #746 helper when it exists, and add the audit note to the XML docs.
KEY RULES: A request without tenant omits the segment; tenant and module ids are escaped with the shared rule; no new audit event.
REFERENCE FILES: src/Encina.Caching/KeyGeneration/DefaultCacheKeyGenerator.cs, docs/plans/module-id-cache-keys-implementation-plan-746.md (if merged; otherwise issue #746), AGENTS.md §6.
```

</details>

### Phase 5: Observability

<details>
<summary>Tasks</summary>

1. `AuthorizationCacheMetrics` (singleton) with the counter and `outcome` tag of Design Choice 6 on the shared `Encina` meter; no `ActivitySource` (cache hits are too cheap to span).
2. `AuthorizationCacheLog` (internal static partial) with `[LoggerMessage]` ids 212 hit, 213 miss, 214 stored, 215 bypass (policy not cacheable), 216 cache failure (exception type only), 217 invalidation; the XML docs name the range (`EventIdRanges.AspNetCore`, 200-249). Keys and policy names are not logged.
3. Run the architecture tests (EventId allocation).

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT: EventIds 200-207 are used in Encina.AspNetCore; the range 200-249 is registered.
TASK: Add the metrics class and the log class with ids 212 to 217; run the EventId architecture tests.
KEY RULES: [LoggerMessage] generator; ids packed; no policy name, user id, claim or error message in tags or text.
REFERENCE FILES: src/Encina.Tenancy/Diagnostics/TenancyMetrics.cs, src/Encina.AspNetCore/AuthorizationElevatedLog.cs, src/Encina/Diagnostics/EventIdRanges.cs.
```

</details>

### Phase 6: Testing — 7 test types

<details>
<summary>Tasks</summary>

Proposed per-flag targets for the new files (the manifest `.github/coverage-manifest/Encina.AspNetCore.json` gets entries with one-sentence justifications; measured values replace these):

| File | unit | guard | contract | property | integration |
|------|:---:|:---:|:---:|:---:|:---:|
| `CachedResourceAuthorizer.cs` | 95 | 40 | 60 | 0 | 70 |
| `PrincipalHasher.cs` | 100 | 20 | 0 | 90 | 0 |
| `AuthorizationCacheOptions.cs` and validator | 100 | 40 | 0 | 0 | 0 |
| `AuthorizationCacheMetrics.cs`, `AuthorizationCacheLog.cs` | 90 | 20 | 0 | 0 | 0 |
| `ServiceCollectionExtensions.cs` (addition) | 95 | 45 | 0 | 0 | 0 |

1. **Unit** (`tests/Encina.UnitTests/AspNetCore/Authorization/`): hit, miss, store, `Left` not cached, cache read and write failure fall through, anonymous and principal-less identity not cached, policy not listed not cached, tenant segment, invalidation, metrics outcomes; fake cache provider.
2. **Guard**: constructor and public members.
3. **Contract**: the decorator honours `IResourceAuthorizer` exactly like `ResourceAuthorizer` for every inner result (same `Either` for allow, denial, unauthenticated).
4. **Property**: the principal hash is order-independent and distinct claim sets give distinct hashes; a decision cached for a principal is never served to a principal with any different claim.
5. **Integration**: a real `ICacheProvider` (Memory in process; one Redis-protocol container through the shared Redis collection) with the real ASP.NET Core `IAuthorizationService` and a policy; revocation by `InvalidateUserAsync`.
6. **Load**: `.md` justification (no concurrency logic beyond the cache).
7. **Benchmark**: cached versus uncached evaluation (`tests/Encina.BenchmarkTests/Encina.AspNetCore.Benchmarks`); it also checks whether the cache pays off at all for in-memory policies.

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT: Per-flag coverage targets live in .github/coverage-manifest/Encina.AspNetCore.json; targets never go down; tests execute real code.
TASK: Write the unit, guard, contract, property, integration and benchmark tests and the load justification; measure every flag and the CRAP table for the changed methods.
KEY RULES: Shouldly; FakeTimeProvider, never Thread.Sleep; integration tests use the shared [Collection] fixtures; the fail-closed cases (cache failure, Left) are mandatory acceptance tests.
REFERENCE FILES: tests/Encina.UnitTests/AspNetCore/Authorization/ResourceAuthorizerTests.cs, tests/Encina.BenchmarkTests/Encina.AspNetCore.Benchmarks, tests/Encina.IntegrationTests/Infrastructure/Caching, .github/coverage-manifest/Encina.AspNetCore.json.
```

</details>

### Phase 7: Documentation & Finalization

<details>
<summary>Tasks</summary>

1. XML docs with an example of `AddEncinaAuthorizationCaching` and the staleness contract (a revoked permission stays allowed until the TTL or an explicit invalidation).
2. `src/Encina.AspNetCore/README.md` section; configuration guide and usage guidance page (via `docs-writer`), including how to mark a policy resource-independent.
3. `changelog.d/710-authorization-cache.added.md`.
4. ADR only if the maintainer's D2 answer changes what a decision is keyed on (ADR number assigned when the ADR pull request opens).
5. PublicAPI files; zero warnings; all tests green; every flag at its target.

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```text
CONTEXT: Feature and tests are done; docs are English, junior-readable, with no hand-typed coverage figures.
TASK: Write the XML docs, README section, guide page and changelog fragment through the specialists that own them.
KEY RULES: Never edit CHANGELOG.md; state the staleness window plainly; show the security reasoning for the explicit policy list.
REFERENCE FILES: src/Encina.AspNetCore/README.md, changelog.d/README.md, .claude/skills/encina-docs/SKILL.md.
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard / Spec | Relevance | Notes |
|-----------------|-----------|-------|
| `AGENTS.md` §3 | Fail closed, ROP, no error message in logs, registration completeness | Governs Design Choices 3 and 4 |
| ASP.NET Core resource-based authorization | `IAuthorizationService.AuthorizeAsync(user, resource, policy)` | Policies may read the resource instance (D2) |
| ADR-021 / `AGENTS.md` §7 | EventIds | 212-217 inside 200-249 (208-211 reserved by #3443) |
| #746 | Cache key grammar | Tenant and module segments |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `IResourceAuthorizer`, `ResourceAuthorizer` | `src/Encina.AspNetCore/Authorization/` | The decorated facade |
| `AddEncinaAuthorization` | `src/Encina.AspNetCore/ServiceCollectionExtensions.cs` | Registration order and `TryAdd` rules |
| `RequestIdentity`, `IRequestContextAccessor` | `src/Encina/Identity/RequestIdentity.cs` | Principal, kind, user id |
| `ICacheProvider` | `src/Encina.Caching/Abstractions/ICacheProvider.cs` | The store (D1) |
| `CacheKeyRedactor` | `src/Encina.Caching/KeyGeneration/CacheKeyRedactor.cs` | Escaping and redaction of key segments |
| `TenancyMetrics` | `src/Encina.Tenancy/Diagnostics/TenancyMetrics.cs` | Meter and tag convention |
| `AuthorizationElevatedLog` | `src/Encina.AspNetCore/AuthorizationElevatedLog.cs` | Log class style and the id 207 |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.AspNetCore` | 212-217 inside the registered 200-249 | 200-207 used, 208-211 reserved by the #3443 plan; 218-249 stay free |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Production code | 7 | options, validator, hasher, decorator and invalidator, invalidator contract, metrics, log class |
| Registration and API | 3 | `ServiceCollectionExtensions.cs`, `PublicAPI.Unshipped.txt`, coverage manifest |
| Tests | 10 | unit x4, guard x1, contract x1, property x1, integration x1, benchmark x2 |
| Justification | 1 | load |
| Documentation | 4 | README section, guide page, changelog, XML docs (in code) |
| **Total** | **~25** | Three pull requests: (1) core and unit/guard tests, (2) contract, property, integration, benchmark, (3) docs |

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT: Encina is a pre-1.0 .NET 10 / C# 14 library. IResourceAuthorizer is the Railway-Oriented facade over ASP.NET Core resource-based authorization; this feature adds an opt-in caching decorator in Encina.AspNetCore that uses ICacheProvider. Zero warnings, no backward compatibility, security gates fail closed.

IMPLEMENTATION OVERVIEW: Phase 1 options, principal hash, invalidator contract; Phase 2 CachedResourceAuthorizer (allows only, every doubt falls through); Phase 3 AddEncinaAuthorizationCaching with order-independent registration and a startup check for ICacheProvider; Phase 4 tenant and module segments, audit note; Phase 5 one counter with an outcome tag and log ids 212-217; Phase 6 tests by flag; Phase 7 docs. Dilemmas D1 (backing cache) and D2 (decision identity) are implemented as the maintainer answers, the recommended options until then.

KEY PATTERNS: Right(true) is the only cached value; Left and cache failures never become allows; keys follow the t:/m:/u: grammar; no policy, claim, user id or error message in tags or logs; TryAdd plus Replace for the decorator; Shouldly; CRAP at or below 10.

REFERENCE FILES: src/Encina.AspNetCore/Authorization/ResourceAuthorizer.cs, src/Encina.AspNetCore/ServiceCollectionExtensions.cs, src/Encina/Identity/RequestIdentity.cs, src/Encina.Caching/Abstractions/ICacheProvider.cs, src/Encina.Tenancy/Diagnostics/TenancyMetrics.cs, tests/Encina.UnitTests/AspNetCore/Authorization/ResourceAuthorizerTests.cs.
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ✅ | The feature: an `ICacheProvider` decorator over `IResourceAuthorizer`. |
| 2 | OpenTelemetry | ✅ | One counter with an `outcome` tag on the shared meter; no policy names or claims in tags. |
| 3 | Structured Logging | ✅ | `[LoggerMessage]` ids 212-217 in the registered AspNetCore range; failures by exception type only. |
| 4 | Health Checks | ❌ | The backing cache has its own health check. |
| 5 | Validation | ✅ | Options validated at startup (TTL greater than zero, an `ICacheProvider` is present). |
| 6 | Resilience | ❌ | A cache failure falls through to the inner authorizer; no retry is wanted on an authorization path. |
| 7 | Distributed Locks | ❌ | Concurrent misses may evaluate twice, which is harmless. |
| 8 | Transactions | ❌ | No atomic multi-operation guarantee is needed. |
| 9 | Idempotency | ❌ | An authorization check is a read; duplicates are harmless. |
| 10 | Multi-Tenancy | ✅ | Tenant segment in the key when a tenant exists; a claims hash alone can repeat across tenants. |
| 11 | Module Isolation | ⏭️ | The module segment follows the source #746 settles (D1 there); omitted until then. |
| 12 | Audit Trail | ❌ | `ResourceAuthorizer` emits no audit event today, so a cache hit skips none; if it gains one, the decorator must emit it on hits too. |

---

## Prerequisites & Dependencies

- **#746** (key grammar, module source): not blocking; the module segment is added when its helper exists. The tenant segment needs nothing.
- **#277**: the Memcached provider serves `InvalidateUserAsync` with a coarse epoch removal; documented in both plans.
- **Out of scope**: the `[Authorize]` pipeline gate (`AuthorizationPipelineBehavior`) evaluates policies per request; caching it is a separate feature the maintainer has not requested.

---

## Next Steps

1. Ask the maintainer D1 and D2 when work on #710 starts; record the answers in `## Maintainer Decisions`.
2. Pull request 1: Phases 1 to 5 with unit and guard tests (at most 15 files).
3. Pull request 2: contract, property, integration and benchmark tests, load justification, manifest entries.
4. Pull request 3: README, guide page, changelog fragment.
