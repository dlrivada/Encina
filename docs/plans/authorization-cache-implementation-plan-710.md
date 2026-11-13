# Implementation Plan: Cache for ASP.NET Core resource authorization results

> **Issue**: [#710](https://github.com/dlrivada/Encina/issues/710)
> **Type**: Feature
> **Complexity**: Medium (7 phases, 2 packages touched, 4 pull requests)
> **Estimated Scope**: ~600-800 lines of production code + ~1,500 lines of tests, in four pull requests of 6 to 13 files (see Estimated File Count)

---

## Summary

Add an opt-in decorator, `CachedResourceAuthorizer`, over `IResourceAuthorizer` (`src/Encina.AspNetCore/Authorization/`). It caches the allow result of a resource authorization for a configurable TTL so the same principal, policy and scope do not pay the policy evaluation twice. The facade it decorates is `internal sealed ResourceAuthorizer`, registered as `TryAddScoped<IResourceAuthorizer, ResourceAuthorizer>()` in `AddEncinaAuthorization`; it takes the caller from `IRequestContextAccessor` (never `HttpContext.User`, #1705) and returns `Task<Either<EncinaError, bool>>`: `Right(true)` when allowed and `Left(AuthorizationResourceDenied)` or `Left(AuthorizationUnauthenticated)` otherwise (a `Right(false)` never occurs).

Three facts of today's code reshape the issue's sketch:

1. **The method signature differs from the issue**: `AuthorizeAsync<TResource>(TResource resource, string policy, CancellationToken)` and `AuthorizeAsync(object resource, string policy, CancellationToken)`; the user is not a parameter. The decorator reads the principal from the same `IRequestContextAccessor`.
2. **Resource-based policies depend on the resource instance by design.** The interface's own example calls `AuthorizeAsync(order, "CanEditOrder", ct)` after loading the order. A key of resource type, policy and claims would return one order's decision for another (Dilemma D2).
3. **Only allows can be cached safely**: the inner result is `Right(true)` or a `Left`; the decorator caches `Right(true)` only and passes every `Left` through. `ICacheProvider.GetAsync<bool>` returns `default` (`false`) on a miss, so only a stored `true` counts as a hit and a returned `false` is a miss, never a denial.

The `[Authorize]` pipeline gate (`AuthorizationPipelineBehavior`) is out of scope: the issue targets `IResourceAuthorizer`.

Provider category: **Caching (8)**, through `ICacheProvider` only. Affected packages: `Encina.AspNetCore` (already references `Encina.Caching`) and `Encina` (one new constant in `EncinaErrorCodes.cs`); the tenant and module key grammar follows #746. No database provider code.

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
| **C) Cache only policies the application explicitly lists as depending only on the principal** (`CacheablePolicies`; not only "resource-independent": a policy that reads request data such as route, IP address or time, or mutable store state, is also unsafe to list) | Safe by construction; security review has one list to read; no change to resource types | Configuration to write; policies not listed are never cached |

### Chosen Option: **C — explicit list of principal-only policies** (recommended, pending the maintainer)

### Rationale

An allow replayed for the wrong instance is a privilege escalation, and the facade is documented for resource-based checks, so the safe default is to cache nothing unless the application says a policy ignores the resource. A wins when security review accepts that all policies are user and type scoped; B wins when most policies are instance-based and a stable resource id exists. With C, the key is `{prefix}:auth:...:{resourceType}:{policy}:{principalHash}` and the resource instance never enters it.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. What is cached: allows only, never a `Left`</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Cache `Right(true)` only; every `Left`, a missing identity or an unhashable principal evaluates through the inner authorizer** | A denial is never replayed from the cache and never turned into an allow | Denials are always re-evaluated |
| **B) Also cache denials** | Fewer evaluations for repeated denials | A `Left` carries `EncinaError.Message` and would sit in plaintext distributed storage; replays the failure reasons; delays a newly granted permission |

### Chosen Option: **A — cache allows only**

### Rationale

The decorator preserves `Either<EncinaError, bool>` and never swallows an inner `Left`. Option B is excluded by the rules cited below, so this narrow choice is settled; what the decorator does when the cache itself fails is a separate Dilemma (D3b).

**Class:** Settled

**Settled by:** `AGENTS.md` §3 (`EncinaError.Message` never reaches plaintext storage; gates fail closed, so a denial is never replayed or turned into an allow; ROP).

</details>

<details>
<summary><strong>3b. When the cache itself fails (Dilemma D3b)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A cache read or write failure falls through to the inner authorizer, which decides** | A broken cache costs performance, not availability; the inner authorizer still denies what it denies | A broken cache is visible only through the `error` counter and the failure log (exception type only) |
| **B) A cache failure fails the request (a `Left` with a dedicated error code)** | A broken cache is impossible to miss; also fail-closed (it denies on doubt) | Turns a cache outage into an authorization outage |

### Chosen Option: **A — fall through on a cache failure** (recommended, pending the maintainer)

### Rationale

Both options deny on doubt and satisfy `AGENTS.md` §3; A wins when a cache outage must not become an authorization outage, B wins when a broken cache must be visible to the caller at once. The failure is logged by exception type through `ForLogging()` (never `EncinaError.Message`).

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Decorator shape and registration order (Dilemma D4)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `AddEncinaAuthorizationCaching` decorates the `IResourceAuthorizer` descriptor that is registered when it runs (implementation type, factory or instance), and when none is registered yet it registers the concrete `ResourceAuthorizer` (`TryAddScoped`) as the inner and registers the decorator as `IResourceAuthorizer`** | The application's own `IResourceAuthorizer` registered **before** the call becomes the inner one and is never discarded; works whichever of the two calls comes first (`AddEncinaAuthorization` uses `TryAddScoped`, so it skips when the decorator is already there); no new dependency | An application registration made **after** the call replaces the decorator (documented: the application's own registration wins); descriptor handling for the three registration kinds must be tested |
| **B) A decoration library such as Scrutor** | Familiar | New dependency for one decoration |
| **C) Register the concrete `ResourceAuthorizer` and `Replace` `IResourceAuthorizer` with a factory over it** | Smallest code | An application's `IResourceAuthorizer` registered before the call is silently discarded and the default facade is wrapped instead, which changes the application's authorization logic and breaks `AGENTS.md` §3 (a registration never overrides the application's own); excluded |
| **D) Wrap inside `ResourceAuthorizer` with an optional cache dependency** | No second type | Always-on branching in a security class; breaks opt-in clarity |

### Chosen Option: **A — decorate the descriptor that is registered** (recommended, pending the maintainer)

### Rationale

`AGENTS.md` §3 requires the registration to be complete and never to override the application's own, proven with `ValidateOnBuild` and `ValidateScopes`; the decorator is scoped like the facade because it reads the scoped request context. A and B both comply, so the choice is a Dilemma: A needs no dependency, B wins if the maintainer prefers a general decoration helper. The DI tests (Phase 3) cover: both call orders with `AddEncinaAuthorization`; an application implementation registered first is the inner one; an application implementation registered after the call wins.

**Class:** Dilemma

</details>

<details>
<summary><strong>5. The user segment of the key (Dilemma D5)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A plaintext `u:{userId}` segment: `{prefix}:auth:[t:{tenant}:][m:{module}:]u:{userId}:{resourceType}:{policy}:{principalHash}`** | Targeted invalidation of one user by pattern; simplest | The user id is personal data written in plaintext to distributed storage (`CacheKeyRedactor` redacts logs only, not storage); the first target application handles health data; `DefaultCacheKeyGenerator` writes `u:` only under the opt-in `VaryByUser`, so it does not settle this |
| **A') A pseudonymised segment `u:{HMAC-SHA-256(userId)}`, keyed with a key supplied in `AuthorizationCacheOptions` (a credential: `[JsonIgnore]`, `ToString()` override, required at startup)** | Per-user invalidation without plaintext ids in storage | One more secret to manage; a lost key loses per-user invalidation of existing entries until their TTL |
| **C) No user segment; the claims hash only and `InvalidateAllAsync` as the only invalidation** | No personal data in keys | Cannot invalidate one user's decisions; the issue's criterion (invalidation on a role or permission change "when detectable") is met only coarsely |

### Chosen Option: **A' — a keyed pseudonymised user segment** (recommended, pending the maintainer)

### Rationale

A wins on simplicity if the maintainer accepts user ids in cache storage; C wins when no secret should be managed and coarse invalidation is acceptable. `IAuthorizationCacheInvalidator.InvalidateUserAsync(userId)` derives the same pseudonym to build its pattern; the tenant and module segments follow the #746 grammar and are escaped with the shared rule.

**Class:** Dilemma

</details>

<details>
<summary><strong>5b. What the principal hash covers and how it is encoded</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) SHA-256 over an injective, length-prefixed encoding of everything a policy can read from the principal, hashed per identity: for each `ClaimsIdentity` of the principal its authentication type, `IsAuthenticated`, `NameClaimType`, `RoleClaimType`, then its claims sorted by `(issuer, type, value)`; each field is written as a 4-byte length followed by its UTF-8 bytes, identities are separated by an explicit identity boundary and count, and the identity kind of `RequestIdentity` is part of the input** | No two different principals can share an encoding (`("a","b|c")` and `("a|b","c")` differ); a different role claim type, identity split or `IsAuthenticated` changes the hash | Per-token claims such as `jti` lower the hit rate; hashing cost per call |
| **B) Hash only roles and permissions, or a delimiter-joined canonical string** | Higher hit rate, simpler | Policy handlers may read other claims; two principals the handlers distinguish would share a key; a delimiter-joined string can collide |

### Chosen Option: **A — injective encoding of everything the policy can read**

### Rationale

Correctness first: anything less can conflate principals that a policy tells apart, and a stale or foreign allow is a privilege escalation. `RequestIdentity` (`src/Encina/Identity/RequestIdentity.cs`) exposes `UserId`, `Kind` and `Principal`; an anonymous, unauthenticated or principal-less identity is never cached. Invalidation by user needs pattern removal, which the Memcached provider serves with a coarse epoch (#277 D2): documented, correct, and heavier there. The property test (Phase 6) generates separator-bearing claim values and multi-identity principals and asserts that distinct principals never share a hash.

**Class:** Settled

**Settled by:** `AGENTS.md` §3 (compliance and security gates fail closed: no weaker identity in the key, so every input the policies read is in the hash and the encoding is injective).

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

<details>
<summary><strong>7. Option defaults, error code and startup check</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Ttl` default 5 minutes (the issue's criterion); `KeyPrefix` default follows `CachingOptions.KeyPrefix` (`"sm"` on main); the failure of `InvalidateUserAsync`/`InvalidateAllAsync` is a `Left` created with `EncinaErrors.Create` (ADR 062, merged in PR #3599) under the new public code `EncinaErrorCodes.AuthorizationCacheInvalidationFailed` (`encina.authorization.cache_invalidation_failed`, next to `AuthorizationResourceDenied`) in category `UpstreamFailure`; the check that an `ICacheProvider` is registered is an `IValidateOptions<AuthorizationCacheOptions>` that takes `IServiceProviderIsService` and runs with `ValidateOnStart`** | Follows the issue, the existing option conventions, the error-category contract and one mechanism with no hosted service | The new code is a public name in core `Encina` (`EncinaErrorCodes.cs`, PublicAPI) |
| **B) Other defaults, an ad hoc error code outside `EncinaErrorCodes`, or a hosted check** | None | Diverges from the issue and from the conventions of the existing codes; a hosted service for one startup check |

### Chosen Option: **A — the defaults, code and mechanism above**

### Rationale

Each value is fixed by the issue (the TTL) or by an existing convention (key prefix, error code with a category per ADR 062, options validation at startup) and none changes a security property.

**Class:** Trivial

</details>

<details>
<summary><strong>8. An empty `CacheablePolicies` (Dilemma D8)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Allowed: nothing is cached and one message is logged at startup** | A valid "installed but not configured yet" state | A misconfiguration becomes a silent no-op, which D1's rationale says the decorator must never be; needs one more log id |
| **B) Options validation fails at startup when the list is empty** | The decorator can never be a silent no-op; no extra log id | An application cannot install the decorator before choosing its policies |

### Chosen Option: **B — fail validation** (recommended, pending the maintainer)

### Rationale

B keeps the promise of D1 that the decorator is never a silent no-op; A wins if the maintainer wants installation and configuration to be separable steps.

**Class:** Dilemma

</details>

---

## Implementation Phases

### Phase 1: Core — options, claims hash, invalidation contract (pull request 1)

<details>
<summary>Tasks</summary>

1. `AuthorizationCacheOptions` (public sealed) in `src/Encina.AspNetCore/Authorization/`: `Ttl` (default 5 minutes), `KeyPrefix` (default follows `CachingOptions.KeyPrefix`, `"sm"`), `CacheablePolicies` (set of policy names, D2 option C), and the user-id pseudonymisation key of D5 option A' (a credential: `[JsonIgnore]`, `ToString()` overridden so no value prints).
2. `AuthorizationCacheOptionsValidator : IValidateOptions<AuthorizationCacheOptions>` (D7): `Ttl` greater than zero; `CacheablePolicies` not empty (D8 option B); the pseudonymisation key present (D5 option A'); an `ICacheProvider` registered, through `IServiceProviderIsService`; used with `ValidateOnStart` in pull request 3.
3. Internal static `PrincipalHasher` (D5b): `string? Compute(ClaimsPrincipal? principal, IdentityKind kind)` returns `null` for a null principal (the caller then bypasses the cache); otherwise SHA-256 hex over the length-prefixed, per-identity encoding of authentication type, `IsAuthenticated`, `NameClaimType`, `RoleClaimType` and the claims sorted by `(issuer, type, value)`, with an explicit identity count and boundary.
4. `IAuthorizationCacheInvalidator` (public): `ValueTask<Either<EncinaError, Unit>> InvalidateUserAsync(string userId, CancellationToken)` and `InvalidateAllAsync(CancellationToken)`; the implementation is its own file in pull request 2.
5. `EncinaErrorCodes.AuthorizationCacheInvalidationFailed` in `src/Encina/Errors/EncinaErrorCodes.cs` and its line in `src/Encina/PublicAPI.Unshipped.txt` (D7), with the `Encina.json` manifest entry of `EncinaErrorCodes.cs` kept as is (a constants file with no executable lines).
6. PublicAPI lines in `src/Encina.AspNetCore/PublicAPI.Unshipped.txt`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT: IResourceAuthorizer (src/Encina.AspNetCore/Authorization/IResourceAuthorizer.cs) returns Task<Either<EncinaError, bool>>; only Right(true) is ever an allow. The caller is the RequestIdentity of IRequestContextAccessor.
TASK: Create AuthorizationCacheOptions, its validator, the internal PrincipalHasher (injective, length-prefixed, per-identity encoding), the IAuthorizationCacheInvalidator contract and the new error code with XML docs and PublicAPI lines.
KEY RULES: The pseudonymisation key is a credential: [JsonIgnore] and a ToString() override; nothing here may print a claim value in ToString or logs; the hash is deterministic, order-independent over claims and injective (no delimiter-joined strings); no EncinaError.Message in logs; ROP return types.
REFERENCE FILES: src/Encina/Identity/RequestIdentity.cs, src/Encina.AspNetCore/Authorization/ResourceAuthorizer.cs, src/Encina/Identity/RequestIdentityOptionsValidator.cs, src/Encina.Caching/Configuration/CachingOptions.cs.
```

</details>

### Phase 2: The decorator (pull request 2)

<details>
<summary>Tasks</summary>

1. `CachedResourceAuthorizer : IResourceAuthorizer` (internal sealed, scoped): constructor takes the inner `IResourceAuthorizer` (D4), `ICacheProvider`, `IRequestContextAccessor`, `IOptions<AuthorizationCacheOptions>`, `ILogger`, `AuthorizationCacheMetrics`, `TimeProvider?` only if a clock read is needed (TTL is the provider's expiration, so none is expected).
2. Both `AuthorizeAsync` overloads: not-cacheable (policy not listed, identity anonymous, unauthenticated or without principal, no request context, `PrincipalHasher` returning `null`) goes straight to the inner authorizer; otherwise read the cache with `GetAsync<bool>`: only a stored `true` is a hit and returns `Right(true)`; a `false` (the provider's miss value) or a cache error (D3b option A) evaluates through the inner authorizer; a `Right(true)` result is stored with `Ttl`; every `Left` is returned unchanged and not stored (D3). A cache failure is caught per call, counted as `error` and logged by exception type through `ForLogging()`.
3. The invalidator implementation (`AuthorizationCacheInvalidator.cs`, its own file) uses `ICacheProvider.RemoveByPatternAsync` with the user pattern (built from the same pseudonym as the key, D5) or the `auth:` prefix pattern and returns a `Left` with `EncinaErrorCodes.AuthorizationCacheInvalidationFailed` (D7) when the provider throws (never swallowed).
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

### Phase 3: Configuration, DI & registration (pull request 3)

<details>
<summary>Tasks</summary>

1. `AddEncinaAuthorizationCaching(this IServiceCollection, Action<AuthorizationCacheOptions>? configure = null)` in `src/Encina.AspNetCore/ServiceCollectionExtensions.cs` (Design Choice 4, option A): options plus validator with `ValidateOnStart`, decoration of the registered `IResourceAuthorizer` descriptor (fallback `TryAddScoped<ResourceAuthorizer>()` as the inner when none is registered), the invalidator and the metrics class (singleton).
2. Startup check (D7): the validator of Phase 1 fails with a message naming `AddEncinaMemoryCache`/`AddEncinaRedisCache` when no `ICacheProvider` is registered.
3. DI tests with `ValidateOnBuild` and `ValidateScopes`: both call orders with `AddEncinaAuthorization`; an application `IResourceAuthorizer` registered **first** is the inner one and is not discarded; one registered **after** wins (documented); a database or other `ICacheProvider` registered first is not overridden.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT: AddEncinaAuthorization registers IResourceAuthorizer with TryAddScoped. The decorator must work whichever of the two calls comes first.
TASK: Add AddEncinaAuthorizationCaching (D4 as the maintainer answered, option A until then), the startup dependency check and the DI tests listed.
KEY RULES: Decorate the descriptor that is registered, never discard an application's own IResourceAuthorizer registered earlier; the application's own registration wins when it comes later; options validated at startup; no static state.
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

### Phase 5: Observability (pull request 2)

<details>
<summary>Tasks</summary>

1. `AuthorizationCacheMetrics` (singleton) with the counter and `outcome` tag of Design Choice 6 on the shared `Encina` meter; no `ActivitySource` (cache hits are too cheap to span).
2. `AuthorizationCacheLog` (internal static partial) with `[LoggerMessage]` ids packed from 212 inside the block 212-217 assigned to #710: 212 hit, 213 miss, 214 bypass (policy not cacheable or identity not cacheable), 215 cache failure (exception type only), 216 invalidation, 217 the startup note for an empty `CacheablePolicies`, **used only if D8 option A is chosen** (under the recommended option B it stays unassigned; there is no "stored" message, the store is not an outcome of the counter and would only add noise). The XML docs name the range (`EventIdRanges.AspNetCore`, 200-249; 218-225 belong to #2229). Keys and policy names are not logged.
3. Run the architecture tests (EventId allocation).

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT: EventIds 200-207 are used in Encina.AspNetCore; the range 200-249 is registered.
TASK: Add the metrics class and the log class with ids 212 to 216 (217 only if D8 option A is chosen); run the EventId architecture tests.
KEY RULES: [LoggerMessage] generator; ids packed; no policy name, user id, claim or error message in tags or text.
REFERENCE FILES: src/Encina.Tenancy/Diagnostics/TenancyMetrics.cs, src/Encina.AspNetCore/AuthorizationElevatedLog.cs, src/Encina/Diagnostics/EventIdRanges.cs.
```

</details>

### Phase 6: Testing — 7 test types

<details>
<summary>Tasks</summary>

Per-flag targets for the new files. Each row is entered in `.github/coverage-manifest/Encina.AspNetCore.json` **in the same pull request as the tests that meet it** (the "PR" column), with a one-sentence justification in the manifest's `justifications`; measured values replace the proposals, and targets never go down.

| PR | File | unit | guard | contract | property | integration |
|:--:|------|:---:|:---:|:---:|:---:|:---:|
| 1 | `AuthorizationCacheOptions.cs`, `AuthorizationCacheOptionsValidator.cs` | 100 | 40 | 0 | 0 | 0 |
| 1 | `PrincipalHasher.cs` | 100 | 30 | 0 | 90 | 0 |
| 2 | `CachedResourceAuthorizer.cs` | 95 | 40 | 60 | 0 | 0 |
| 2 | `AuthorizationCacheInvalidator.cs` | 95 | 40 | 0 | 0 | 0 |
| 2 | `AuthorizationCacheMetrics.cs` | 90 | 0 | 0 | 0 | 0 |
| 2 | `AuthorizationCacheLog.cs` | 0 | 0 | 0 | 0 | 0 |
| 3 | `ServiceCollectionExtensions.cs` (addition) | 95 | 45 | 0 | 0 | 0 |
| 3 | `CachedResourceAuthorizer.cs`, `AuthorizationCacheInvalidator.cs` (integration added) | unchanged | unchanged | unchanged | unchanged | 70 |

| Row | Justification |
|-----|---------------|
| Options and validator | Plain properties and rules: every rule (TTL, empty list, key, cache provider presence) is reachable from unit tests and the null arguments from guard tests. |
| `PrincipalHasher.cs` unit 100, guard 30, property 90 | The hasher is deterministic and small, so unit tests reach every line; guard tests reach only the null-principal early return (a valid null result, not an exception); the property test reaches the encoding through generated claims, including separator-bearing values and multi-identity principals. |
| `CachedResourceAuthorizer.cs` | Unit tests with a fake cache provider reach every branch; guard tests reach the constructor and argument checks; the contract tests execute the real decorator against the real inner authorizer for allow, denial and unauthenticated results. |
| `AuthorizationCacheInvalidator.cs` | Both operations and the failure branch are unit-testable with a fake provider; guard tests reach the user-id argument check. |
| `AuthorizationCacheMetrics.cs` | One counter and one record method that unit tests drive for each outcome; guard 0 because `Record` takes an enum and no reference-type parameter. |
| `AuthorizationCacheLog.cs` all 0 | A `[LoggerMessage]` partial has no hand-written coverable lines; the same reason as the manifest entry of `AuthorizationElevatedLog.cs`; the messages are asserted through the decorator tests. |
| `ServiceCollectionExtensions.cs` | The new method is fully reachable from DI tests; guard covers the null arguments. |
| Integration 70 | A real provider with the real `IAuthorizationService` reaches the main hit, miss, store and invalidation paths; failure branches stay in unit tests. |

Contract is 0 for every file except the decorator because the contract is the decorator's equivalence to `ResourceAuthorizer` and no interface is added besides `IAuthorizationCacheInvalidator`, whose implementation the unit tests cover; a `.md` justification is never written for contract tests (`AGENTS.md` §9).

1. **Unit** (`tests/Encina.UnitTests/AspNetCore/Authorization/`): hit (only a stored `true`), miss (the provider's `false`), store, `Left` not cached, cache read and write failure fall through, anonymous and principal-less identity not cached, policy not listed not cached, tenant segment, user pseudonym, invalidation and its failure code, metrics outcomes; fake cache provider; DI (pull request 3).
2. **Guard** (`tests/Encina.GuardTests/AspNetCore/`): constructors, public members and the validator; a nullable parameter that accepts null gets a valid-result test, not an exception test (#3798).
3. **Contract**: the decorator honours `IResourceAuthorizer` exactly like `ResourceAuthorizer` for every inner result (same `Either` for allow, denial, unauthenticated).
4. **Property**: the principal hash is order-independent over claims and distinct principals give distinct hashes, including separator-bearing claim values (`("a","b|c")` against `("a|b","c")`), different `RoleClaimType`/`NameClaimType`, different identity splits and different `IsAuthenticated`; a decision cached for a principal is never served to a principal with any different input.
5. **Integration**: a real `ICacheProvider` (Memory in process; one Redis-protocol container through the shared Redis collection) with the real ASP.NET Core `IAuthorizationService` and a policy; revocation by `InvalidateUserAsync` (pull request 3).
6. **Load**: `.md` justification (no concurrency logic beyond the cache) (pull request 4).
7. **Benchmark**: cached versus uncached evaluation (`tests/Encina.BenchmarkTests/Encina.AspNetCore.Benchmarks`, the project exists with its `.github/perf-manifest/Encina.AspNetCore.Benchmarks.json`); it also checks whether the cache pays off at all for in-memory policies (pull request 4).

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT: Per-flag coverage targets live in .github/coverage-manifest/Encina.AspNetCore.json; targets never go down; tests execute real code.
TASK: Write the unit, guard, contract, property, integration and benchmark tests, the load justification and the manifest rows with their one-sentence justifications in the pull request of each file; measure every flag and the CRAP table for the changed methods.
KEY RULES: Shouldly; FakeTimeProvider, never Thread.Sleep; integration tests use the shared [Collection] fixtures; the fail-closed cases (cache failure, Left) are mandatory acceptance tests.
REFERENCE FILES: tests/Encina.UnitTests/AspNetCore/Authorization/ResourceAuthorizerTests.cs, tests/Encina.BenchmarkTests/Encina.AspNetCore.Benchmarks, tests/Encina.IntegrationTests/Infrastructure/Caching, .github/coverage-manifest/Encina.AspNetCore.json.
```

</details>

### Phase 7: Documentation & Finalization (pull request 4)

<details>
<summary>Tasks</summary>

1. XML docs with an example of `AddEncinaAuthorizationCaching` and the staleness contract (a revoked permission stays allowed until the TTL or an explicit invalidation).
2. `src/Encina.AspNetCore/README.md` section; configuration guide and usage guidance page (via `docs-writer`), including when a policy may be listed (it depends only on the principal: not on the resource instance, request data such as route, IP address or time, or mutable store state).
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
| `Encina.AspNetCore` | 212-217 inside the registered 200-249 (the block assigned to #710 in `eventid-blocks.md`) | 200-207 used, 208-211 reserved by the #3443 plan, 218-225 by #2229; ids 212-216 used, 217 only if D8 option A is chosen; 226-229 belong to #454; packed sequentially |

### Estimated File Count

Each pull request carries its tests, manifest rows (in the same pull request as the tests that meet them), PublicAPI lines and any caller it changes. No existing test calls a changed signature (the only edit to an existing production file besides registration is the new constant in `EncinaErrorCodes.cs`). No pull request uses a type created by a later one.

| PR | Contents | Files | Lines |
|:--:|----------|------:|------:|
| 1 | Options, validator, `PrincipalHasher`, `IAuthorizationCacheInvalidator` (4); `EncinaErrorCodes.cs` (1); PublicAPI of `Encina.AspNetCore` and of `Encina` (2); manifest rows (1); unit tests of options/validator and of the hasher (2); guard test (1); hasher property test (1) | **12** | about 1,000 |
| 2 | `CachedResourceAuthorizer`, `AuthorizationCacheInvalidator` (its own file), `AuthorizationCacheMetrics`, `AuthorizationCacheLog` (4); manifest rows (1); unit tests of decorator, invalidator and metrics (3); guard test (1); contract test (1) | **10** | about 1,300 |
| 3 | `ServiceCollectionExtensions.cs` (1); PublicAPI (1); manifest rows (1); DI unit test (1); guard test (1); integration test (1) | **6** | about 600 |
| 4 | Benchmark class and perf manifest (2); load `.md` justification (1); README section, guide page, changelog fragment (3) | **6** | about 450 |
| **Total** | Four pull requests | **34 file touches, 31 distinct files** (the `Encina.AspNetCore` manifest is touched 3 times and its PublicAPI file twice) | about 3,350 |

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT: Encina is a pre-1.0 .NET 10 / C# 14 library. IResourceAuthorizer is the Railway-Oriented facade over ASP.NET Core resource-based authorization; this feature adds an opt-in caching decorator in Encina.AspNetCore that uses ICacheProvider. Zero warnings, no backward compatibility, security gates fail closed.

IMPLEMENTATION OVERVIEW: Four pull requests. 1: options, injective principal hash, invalidator contract, error code; 2: CachedResourceAuthorizer (allows only, every doubt falls through), invalidator, metrics and log ids 212-216; 3: AddEncinaAuthorizationCaching that decorates the registered IResourceAuthorizer with order-independent registration and a startup check for ICacheProvider; 4: benchmark, load justification, docs. Phase 4 tenant and module segments, audit note; Phase 6 tests by flag with manifest rows in the pull request of their tests. Dilemmas D1 (backing cache), D2 (decision identity), D3b (cache failure), D4 (decoration), D5 (user segment) and D8 (empty list) are implemented as the maintainer answers, the recommended options until then.

KEY PATTERNS: Right(true) is the only cached value (a returned false from GetAsync<bool> is a miss); Left and cache failures never become allows; keys follow the t:/m:/u: grammar with a pseudonymised user segment; the principal hash is injective and length-prefixed; no policy, claim, user id or error message in tags or logs; decorate the registered descriptor, never discard the application's own registration; Shouldly; CRAP at or below 10.

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

1. Ask the maintainer D1, D2, D3b, D4, D5 and D8 when work on #710 starts; record the answers in `## Maintainer Decisions`.
2. Pull request 1 (12 files): Phase 1 with its unit, guard and property tests and manifest rows.
3. Pull request 2 (10 files): Phases 2 and 5 with unit, guard and contract tests and manifest rows.
4. Pull request 3 (6 files): Phase 3 registration with DI, guard and integration tests and manifest rows.
5. Pull request 4 (6 files): benchmark, load justification, README section, guide page, changelog fragment.
