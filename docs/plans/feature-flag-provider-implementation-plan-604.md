# Implementation Plan: `IFeatureFlagProvider` — Abstraction and Implementations (Database, Redis, Unleash)

> **Issue**: [#604](https://github.com/dlrivada/Encina/issues/604)
> **Type**: Feature
> **Complexity**: High (provider abstraction, 10 database providers, 2 new provider packages)
> **Estimated Scope**: about 11,600 changed lines in 15 pull requests, tests, manifests and PublicAPI files included (table "Pull request split" under Research; each pull request is at most 15 files and 1,500 lines)

---

## Summary

Define the provider abstraction of the feature-flag family, `IFeatureFlagProvider`, in a new `Encina.FeatureFlags` package, and implement it for database-backed storage (ADO.NET x3, Dapper x3, EF Core x3, MongoDB), Redis and Unleash. Nothing of it exists in `src/` today: no `Encina.FeatureFlags` project, no `Microsoft.FeatureManagement`, `Unleash` or `OpenFeature` reference (`Directory.Packages.props` lists `StackExchange.Redis` 2.12.14 and `MongoDB.Driver` 3.12.0 only).

This plan **owns the foundation of the family** (#300, #345, #451, #604, with #2694 for the SaaS providers, outside 1.0 per SPEC-000 section 6, and #607 for the service-discovery packages that only share the EventId block): the project, the contract, the error codes, the shared diagnostics, the rule evaluator, the cache decorator and the core EventId range. The other plans build on its Phase 1.

| Shared piece | Owner | Used by |
|---|---|---|
| Project `Encina.FeatureFlags`, `IFeatureFlagProvider`, `FeatureFlagContext`, `FeatureFlag`, `FeatureFlagRule`, `FeatureFlagErrors` | #604 Phase 1 | #300, #345, #451 |
| `FeatureFlagsOptions`, `AddEncinaFeatureFlags` skeleton, `FeatureFlagBucketing`, `FeatureFlagRuleEvaluator` | #604 Phase 2 | #345, #300 |
| EventId range `FeatureFlags` 7300-7319 (registered in Phase 3, the first pull request that logs), `FeatureFlagsDiagnostics`, `FeatureFlagsLogMessages`, `CachedFeatureFlagProvider` | #604 Phase 3 | #300 (7306-7309), #345 (7310-7315) |
| EventId ranges `FeatureFlagsUnleash` 7320-7324 and `FeatureFlagsRedis` 7325-7329 | #604 Phases 7b and 6b (each registers its own range) | the Unleash and Redis packages |
| `FeatureFlagPipelineBehavior<,>`, `[FeatureFlag]`, `Microsoft.FeatureManagement` provider | #300 (gate slice) and #345 | — |
| `Encina.FeatureFlags.OpenFeature`, `.ConfigCat`, EventIds 7330-7339 | #451 (registers its own ranges) | — |
| `Encina.ServiceDiscovery*`, EventIds 7340-7349 | #607 | — |

**Affected packages**: new `Encina.FeatureFlags`, `Encina.FeatureFlags.Redis`, `Encina.FeatureFlags.Unleash`; the `FeatureFlags/` folder of `Encina.ADO.SqlServer|PostgreSQL|MySQL`, `Encina.Dapper.SqlServer|PostgreSQL|MySQL`, `Encina.EntityFrameworkCore`, `Encina.MongoDB`; `src/Encina/Diagnostics/EventIdRanges.cs`.

**Provider category**: Database (10) for the store, plus two non-database providers (Redis, Unleash).

**Error contract**: ADR 062 (error category contract, merged in PR #3599) is on `main`. Every new error code of this plan has an `ErrorCategory` and is created with the `EncinaErrors.Create(code, category, message, ...)` overload (`src/Encina/Results/EncinaErrors.cs`); `ErrorCategory` is in `src/Encina/Errors/ErrorCategory.cs`.

| Code | `ErrorCategory` | Meaning |
|---|---|---|
| `encina.featureflags.not_found` | `NotFound` | The flag does not exist (returned only where Design Choice 12 says so) |
| `encina.featureflags.provider_failed` | `UpstreamFailure` | The backing store or service failed (after the resilience layer of Design Choice 16) |
| `encina.featureflags.circuit_open` | `Unavailable` | The circuit breaker of the Redis provider is open (Design Choice 16) |
| `encina.featureflags.invalid_rule` | `FailedPrecondition` | A stored rule cannot be evaluated (unknown operator, malformed value) |
| `encina.featureflags.invalid_flag_name` | `Validation` | The flag name breaks the naming rule of Design Choice 13 |
| `encina.featureflags.disabled` | `FailedPrecondition` | Used by the gate of #300 and #345 |

---

## Design Choices

<details>
<summary><strong>1. Package placement and ownership — one new <code>Encina.FeatureFlags</code> package created here</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) #604 creates `Encina.FeatureFlags` (contract and shared pieces); #345/#300 add the pipeline behavior to it; Redis and Unleash are separate packages** | One owner for the csproj and the contract; core stays free of Redis and Unleash dependencies (pay-for-what-you-use) | The other plans wait for #604 Phase 1 |
| **B) #345 creates the package, #604 adds the abstraction later** | The user-visible gate ships first | The gate would depend on a provider interface that does not exist yet; #345, #451 and #604 all say the interface is "defined by #604" |
| **C) Everything in one package including Redis and Unleash** | One NuGet | Every application that uses the gate pulls StackExchange.Redis and Unleash.Client |

### Chosen Option: **A — #604 Phase 1 creates the package and the contract**

### Rationale

- The bodies of #345 and #451 state that `IFeatureFlagProvider` "is defined by #604", and #604's own text puts it "in a new `Encina.FeatureFlags` package".
- Providers with an external dependency live in their own packages: `Encina.FeatureFlags.Redis`, `Encina.FeatureFlags.Unleash` (and `.OpenFeature`, `.ConfigCat` in #451), as `Encina.Caching.Redis` does for caching.
- Database stores live in the existing provider packages, in a `FeatureFlags/` folder named after the feature (AGENTS.md section 4, #413).

**Class:** Settled
**Settled by:** Issue bodies #345, #451, #604 (ownership statements); AGENTS.md section 1 (pay-for-what-you-use) and section 4 (feature folder naming)

</details>

<details>
<summary><strong>2. The contract — return type of the four methods</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Task<Either<EncinaError, T>>` on all four methods (`IsEnabledAsync`, `GetVariationAsync<T>`, `GetAllFlagsAsync`, `GetFlagAsync`); `GetFlagAsync` returns `Either<EncinaError, Option<FeatureFlag>>`; `FeatureFlagContext(UserId, TenantId, Attributes)` as in the issue sketch** | Provider failure is explicit; matches every Encina store and the issue sketch; a missing flag is not an error | Callers unwrap an `Either`; every call allocates a `Task` |
| **B) The original bare `Task<bool>` / `Task<T?>`** | Simplest call site | Hides provider failure; contradicts AGENTS.md section 3 |
| **C) `ValueTask<Either<EncinaError, T>>`** | Fewer allocations on the cached path; wins when the decorator answers most calls from memory | Differs from the issue sketch; consumers that store the result or await twice must call `AsTask()`; AGENTS.md section 3 requires `Either` but not `Task` or `ValueTask`, so it is an open choice |

### Chosen Option: **A — `Task<Either<EncinaError, T>>` (recommended, pending the maintainer)**

### Rationale

- A wins when the contract should match the issue and the existing stores; C wins when profiling the gate (#345) shows allocation on the cached path matters. B is out: AGENTS.md section 3 requires `Either<EncinaError, T>` for operations (ADR-001, ADR-006).
- `GetFlagAsync` returns `Option<FeatureFlag>` so that a missing flag is a value, not an error; Design Choice 12 decides what `IsEnabledAsync` and `GetVariationAsync<T>` do for a missing flag.
- `FeatureFlagContext` carries `UserId`, `TenantId` and free `Attributes`. The #451 sketch had `Environment` as a field: it becomes the attribute `environment`, so there is one context type for the family (#604 owns it; `FeatureFlagContext.From(IRequestContext)` is built by #300).

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Provider failure semantics — what <code>IsEnabledAsync</code> does when the provider fails</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Return a `Left` with `encina.featureflags.provider_failed`; the caller (the gate) decides** | Honest: a wrong flag value is never silently assumed; the gate maps it to its own rule (disabled and reported) | Every direct caller handles it |
| **B) Return the configured default (`FeatureFlagsOptions.DefaultWhenUnavailable`) and log the error code** | A flag outage does not take the application down | Hides the outage from the caller; a kill switch could stay "on" by accident |

### Chosen Option: **A — Return a `Left` (recommended, pending the maintainer)**

### Rationale

- A wins when a wrong value is worse than an error (a kill switch, a compliance feature). B wins when flags are advisory and availability matters more.
- A keeps the choice with the caller: the gate in #345 can apply `DefaultWhenUnavailable` itself without hiding the failure from other callers, and background infrastructure never swallows a `Left` (AGENTS.md section 3).
- The Left carries the error code only; `EncinaError.Message` never reaches logs or tags (AGENTS.md section 3).

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Database provider scope — all 10 providers, delivered in phased pull requests</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) All 10 providers, grouped in pull requests of at most 15 files (Phases 5a-5e)** | Follows AGENTS.md section 5; each pull request stays under the limits | The feature is complete only when the last pull request merges |
| **B) A subset first (for example Dapper) and the rest later** | Smaller first release | Breaks provider coherence while the feature is released |
| **C) One pull request for all 10** | One review | About 60 files with tests; the `pr-size` gate fails it (limit 15 files, 1500 lines) |

### Chosen Option: **A — All 10, five pull requests**

### Rationale

- The issue's D2 asks "all 10 or phased"; both answers deliver all 10, and the size rule forces the phasing, so only A complies with section 5 and section 11 together. What remains is the order of the pull requests, which is not a decision.
- The EF Core store is one class (`FeatureFlagStoreEF`) in `Encina.EntityFrameworkCore`; the three EF Core database providers are covered by running its integration tests on SQL Server, PostgreSQL and MySQL (as `OperationAuditStoreEF` is).

**Class:** Settled
**Settled by:** AGENTS.md section 5 (database rule: every provider-dependent feature for all 10 providers) and section 11 (`pr-size`: at most 15 files and 1500 lines per pull request)

</details>

<details>
<summary><strong>5. Redis and Unleash are part of this issue</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Both packages in this issue** | Matches the issue and its acceptance criteria; Redis and Unleash run locally (Docker), so they are testable in CI | Two more packages to maintain |
| **B) Redis waits for demand** | Smaller first release; the database store already covers simple flags, which is the issue's own D3 Option B | Redis is the usual backend for simple flags; the acceptance criteria list it |

### Chosen Option: **A — Both in this issue (recommended, pending the maintainer)**

### Rationale

- A wins when the family must offer a fast, shared simple-flag backend at 1.0; B wins when the first release should ship with fewer packages and only the demand-driven ones. The acceptance criteria of the issue list Redis, but they are the issue text, not a rule that excludes B.
- SPEC-000 section 6 moves only the cloud halves of #604 to the cloud milestone (#2694); everything else in the issue stays in 1.0 ("every other provider, transport and integration is in the 1.0 contract", DEC-009).
- Unleash is open source and runs in Docker; Redis has `RedisFixture` in `tests/Encina.TestInfrastructure/Fixtures/RedisFixture.cs`.

**Class:** Dilemma

</details>

<details>
<summary><strong>6. Cache backing — in-process cache or the Encina <code>ICacheProvider</code></strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `CachedFeatureFlagProvider` decorator over an in-process `MemoryCache`, TTL from `TimeProvider`, invalidated by a change token the provider raises** | No dependency on `Encina.Caching`; no network round trip on a hot path; testable with `FakeTimeProvider` | Each instance caches separately: a change reaches other instances only when their provider reports it or the TTL expires |
| **B) Decorator over `ICacheProvider` (`src/Encina.Caching/Abstractions/ICacheProvider.cs`)** | Shared cache and pub/sub backplane across instances (Redis and siblings) | Adds `Encina.Caching` and a serialization step to every flag check; `ICacheProvider` has no `TimeProvider`-driven expiry |

### Chosen Option: **A — In-process cache (recommended, pending the maintainer)**

### Rationale

- A wins when evaluation is on the request hot path and a few seconds of staleness is acceptable (the usual case). B wins when several instances must flip together within milliseconds.
- The cache key is `(flag name, tenant, context hash)`; the tenant is part of the key (AGENTS.md section 6, function 10); Design Choice 14 defines the hash.
- B stays possible later: a different decorator over `ICacheProvider` can be registered without a contract change.

**Class:** Dilemma

</details>

<details>
<summary><strong>7. Persistence model — one flags table with rules as one JSON column, or a rules table</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Table `FeatureFlags` (`Name`, `TenantId`, `IsEnabled`, `Description`, `RulesJson`, `UpdatedAtUtc`)** | One read per flag; the schema is identical in all 10 providers; wins when flags are read by name and never searched by rule | Rules are not queryable in SQL |
| **B) A second table `FeatureFlagRules`** | Rules are queryable ("which flags use attribute X"); wins when an admin UI must search rules | A join on every evaluation and 10 sets of two scripts; MongoDB differs (embedded array) |

### Chosen Option: **A — One table, JSON rules (recommended, pending the maintainer)**

### Rationale

- Flags are read by name, never searched by rule, in everything this plan builds; a JSON column keeps the schema identical across providers.
- `UpdatedAtUtc` uses the `AtUtc` suffix and `TimeProvider` (AGENTS.md sections 3 and 4); how the global flag is keyed is Design Choice 8.
- Scripts follow `src/Encina.ADO.PostgreSQL/Scripts/` numbering: the next free number at implementation time.

**Class:** Dilemma

</details>

<details>
<summary><strong>8. Global flags and uniqueness — nullable tenant column or a sentinel</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `TenantId` is `NOT NULL`; the global flag stores the empty string `''`; unique index on `(TenantId, Name)`; the store maps `null` to `''` and rejects an empty or whitespace tenant id from callers** | One plain unique index behaves the same on SQL Server, PostgreSQL, MySQL and MongoDB; no duplicate global rows are possible | A reserved value in a tenant column; every query goes through the mapper |
| **B) `TenantId` is nullable (null means global); a filtered or partial unique index per provider** | `NULL` reads naturally as "no tenant" | A unique index on a nullable column does not stop duplicates: PostgreSQL and MySQL treat NULLs as distinct (SQL Server allows one NULL); it needs a filtered index (SQL Server), a partial index or `NULLS NOT DISTINCT` (PostgreSQL), and a generated column (MySQL has no partial index): three different scripts and a nondeterministic flag value if one is missed |
| **C) A separate table for global flags** | No sentinel, no nullable column | Two tables and a union on every read |

### Chosen Option: **A — Sentinel `''` for the global flag (recommended, pending the maintainer)**

### Rationale

- A wins when one script shape for all providers matters more than the readability of NULL; B wins when the schema must show the absence of a tenant literally and the team accepts three index definitions.
- Whatever the option, the rule "at most one global row per name" is enforced by the database and covered by an integration test per provider (inserting the same global flag twice yields a `Left`, not a second row).
- The Redis key layout (Design Choice 15) and the MongoDB document use the same global marker semantics.

**Class:** Dilemma

</details>

<details>
<summary><strong>9. Percentage bucketing algorithm</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One `FeatureFlagBucketing` in core: a truncated SHA256 of `flag name + bucketing key`, result 0-99** | Deterministic across processes and instances; every provider that has no engine of its own (database, Redis) behaves the same | An evaluator to maintain |
| **B) `string.GetHashCode()`** | One line | Not deterministic across processes in .NET |

### Chosen Option: **A — SHA256 bucketing**

### Rationale

- The maintainer fixed the bucketing rule in the comment of 2026-03-18 on #345 (SHA256, time from `TimeProvider`); B is not deterministic across processes, so it cannot satisfy that decision.
- Unleash and (in #451) OpenFeature providers evaluate remotely or in their SDK and do not use the evaluator.

**Class:** Settled
**Settled by:** Maintainer comment of 2026-03-18 on #345 (SHA256 bucketing, `TimeProvider`); AGENTS.md section 3 (time)

</details>

<details>
<summary><strong>10. Evaluator semantics — no match, order and operators</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `IsEnabled = false` always returns false and rules are not read; `IsEnabled = true` without rules returns true; with rules the first matching rule returns its `Result` (first match wins, in list order); no match returns false; operators `equals`, `not_equals`, `in` (comma-separated, trimmed), `percentage` (rule `Value` is an integer 0-100; the bucket must be below it; bucketing key is `UserId`, otherwise the attribute `bucketing_key`, otherwise the rule does not match), `after_utc`, `before_utc` (ISO 8601 UTC)** | Fails closed: an unmatched context never gets an enabled flag by accident; an unknown operator is `invalid_rule`, never ignored | A flag meant "on for everyone except X" needs an explicit catch-all rule |
| **B) No match returns the flag's `IsEnabled`** | "On except X" is one rule | An enabled flag with a targeting rule is on for everyone the rule does not name, which surprises a rollout of "10 % of users" |

### Chosen Option: **A — No match is false (recommended, pending the maintainer)**

### Rationale

- A wins for rollouts, canaries and kill switches (the unmatched user stays off); B wins for exclusion lists. A compliance-style gate fails closed (AGENTS.md section 3).
- A rule with a missing attribute does not match (it is not an error); a malformed rule value or an unknown operator returns `Left` `invalid_rule`.
- A percentage of 0 matches nobody and 100 matches everybody; the bucket comes from Design Choice 9.

**Class:** Dilemma

</details>

<details>
<summary><strong>11. Tenant resolution and the context-free reads</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `IsEnabledAsync` and `GetVariationAsync<T>` read the tenant row of `context.TenantId` first, then the global row; when `TenantId` is null (or whitespace) only the global row is read, never another tenant's; `GetAllFlagsAsync` and `GetFlagAsync` have no context in the issue sketch and return the global flags only** | Fails closed on a missing tenant; a tenant can override a global flag; the sketch is unchanged | A caller cannot list one tenant's overrides through the provider (the store has `ListAsync(tenantId)`) |
| **B) No fallback: a tenant row or nothing** | A tenant that has no row is "flag missing", simple to reason about | Every tenant needs its own copy of every flag |
| **C) A has the fallback and the two reads take an optional `FeatureFlagContext`** | A tenant-aware listing through the provider | Changes the issue sketch; every implementation of the two methods handles the context |

### Chosen Option: **A — Tenant row, then global row; null tenant reads global only (recommended, pending the maintainer)**

### Rationale

- A wins when most flags are global and a few tenants override them; B wins when flags are strictly per tenant; C wins when an admin screen must list a tenant's effective flags.
- A missing tenant never widens access: the provider never reads a tenant row without a tenant id (compliance and security gates fail closed, AGENTS.md section 3; function 10 of section 6).

**Class:** Dilemma

</details>

<details>
<summary><strong>12. Missing flag and variation type mismatch</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `IsEnabledAsync` for a missing flag returns `Right(false)`; `GetVariationAsync<T>` for a missing flag returns `Right(defaultValue)`; a flag whose stored value cannot be read as `T` returns `Left` `invalid_rule`; `GetFlagAsync` returns `Right(None)`** | The caller-supplied `defaultValue` has its purpose; an unknown flag behaves like an off flag; a typo is visible through a log of the missing flag, not an outage | A typo in a flag name is silently "off" at the call site |
| **B) `IsEnabledAsync` for a missing flag returns `Left` `not_found`** | A typo fails loudly | The gate must map `not_found` (a code that is not an outage) before applying `DefaultWhenUnavailable` |

### Chosen Option: **A — A missing flag is off or the default (recommended, pending the maintainer)**

### Rationale

- A wins when a flag may be created after the code that checks it ships (the usual rollout order); B wins when every checked flag must exist, as in a strict kill-switch inventory.
- `encina.featureflags.not_found` stays in `FeatureFlagErrors` for the providers and for callers that choose B (and for #451's OpenFeature mapping, which reports it for a missing flag).

**Class:** Dilemma

</details>

<details>
<summary><strong>13. Options defaults, limits and name validation</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `DefaultWhenUnavailable = false`; `CacheEnabled = false` (opt in); `CacheDuration = 30 seconds`; flag name 1-128 characters of `[A-Za-z0-9._-]` (no `:`, no spaces), checked at the store and provider boundary (`Left` `invalid_flag_name`); options validated on start** | Fail closed by default (AGENTS.md section 3); pay-for-what-you-use (section 1): nothing is cached until enabled; a name without `:` keeps the Redis key unambiguous | 30 seconds is a guess to tune with the first real usage |
| **B) `CacheEnabled = true` by default** | Faster by default | A cache the application did not ask for; stale flags surprise a kill switch |
| **C) A longer default `CacheDuration` (5 minutes)** | Fewer provider calls | Slow propagation of a kill switch |

### Chosen Option: **A — Defaults above (recommended, pending the maintainer)**

### Rationale

- The three defaults of `FeatureFlagsOptions` and the name rule are not fixed by any cited rule beyond "fail closed" and "opt-in"; the duration and the character set are the open part.
- A wins when the safe, opt-in default matters most; C wins for applications that rarely change flags.

**Class:** Dilemma

</details>

<details>
<summary><strong>14. Cache key composition and negative entries</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Key `(flag name, tenant id, context hash)`, where the context hash is a SHA256 over `UserId` and the sorted attribute names and values; the hash is the only form of that data in the key; the key never reaches a log, an activity tag or a metric; a missing flag (`None`) is cached like a flag for the same TTL; a `Left` is never cached** | User ids and attributes (personal data) are not stored in clear in the key; a burst of checks for a flag that does not exist yet does not hit the store each time | A flag created after a negative entry appears after at most `CacheDuration` (or at once when the provider raises a change) |
| **B) Key `(flag name, tenant id)` only, evaluating rules after the cache** | A smaller key space; the cache holds flag definitions, not results | Rules run on every call; the cache returns shared `FeatureFlag` objects |
| **C) A without negative entries** | A new flag is seen at once | Repeated store reads for a missing flag |

### Chosen Option: **A — Hashed context key; negative entries cached (recommended, pending the maintainer)**

### Rationale

- A wins when evaluation results per context repeat (the common case with few distinct users per window); B wins when contexts are almost all distinct (high-cardinality user ids make A's key space large) and the rule evaluation is cheap.
- Personal data never leaves the process: the hash input is in memory only, and no tenant id, user id or context attribute is used as a tag, a metric dimension or a log argument (Phase 3).

**Class:** Dilemma

</details>

<details>
<summary><strong>15. Redis key layout, value format and dependencies</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Keys `encina:featureflags:global:{name}` and `encina:featureflags:tenant:{tenantId}:{name}` (a configurable prefix like `RedisCacheProvider`'s); the value is the `System.Text.Json` form of `FeatureFlag`; packages use `StackExchange.Redis` (version in `Directory.Packages.props`) and `Unleash.Client` (version pinned in `Directory.Packages.props` at implementation time)** | No collision between a tenant id and a name (the name has no `:`, Design Choice 13); one JSON shape shared with the database provider | A tenant id containing `:` is read right to left (the name is after the last colon) |
| **B) A Redis hash per tenant** | One round trip for a tenant's flags | Tenant-null encoding is a special hash name; a different shape from the cache keys |

### Chosen Option: **A — Prefix layout with JSON values**

### Rationale

- The layout is internal storage with no effect on the contract; it follows the prefixed-key convention of `src/Encina.Caching.Redis/RedisCacheProvider.cs` (`GetPrefixedKey`).
- Dependency versions are pinned in one place (`Directory.Packages.props`), the repository convention for third-party packages.

**Class:** Trivial
**Settled by:** `src/Encina.Caching.Redis/RedisCacheProvider.cs` (prefixed keys); `Directory.Packages.props` (central package versions)

</details>

<details>
<summary><strong>16. Resilience layer of the Redis and Unleash providers</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Redis: a Polly `ResiliencePipeline` (timeout and circuit breaker, no retry on the hot path) built inside `Encina.FeatureFlags.Redis` from its options (`Polly` and `Microsoft.Extensions.Resilience` are already in `Directory.Packages.props`); Unleash: no pipeline, because `Unleash.Client` polls the server in the background and evaluates from its local toggle copy, so an evaluation makes no network call (its own client timeouts come from the options); a Redis failure after the pipeline is `Left` `provider_failed`, an open circuit is `Left` `circuit_open`; no shared policy across packages** | One layer per provider, documented; the circuit protects the hot path when Redis is down; each package is usable alone | Two different mechanisms; the options of the pipeline are another surface |
| **B) Rely on `StackExchange.Redis` timeouts and reconnect only** | No new dependency | No circuit breaker: every call waits for its timeout while Redis is down |
| **C) A shared pipeline in `Encina.FeatureFlags` for every provider** | One mechanism | Core would depend on Polly; Unleash has no per-call call to protect |

### Chosen Option: **A — Per-package resilience (recommended, pending the maintainer)**

### Rationale

- A wins when a Redis outage must not slow every request; B wins when the lowest dependency count matters and flags are read through the cache (Design Choice 6), so Redis is called rarely.
- Failures end as `Left` `provider_failed` or `circuit_open`, never as a default value (Design Choice 3), and the error code (never the message) is logged.

**Class:** Dilemma

</details>

---

## Implementation Phases

Each phase is one pull request: it carries its own unit, guard and (where it applies) contract, property and integration tests, its coverage-manifest entries, its `PublicAPI.Unshipped.txt` lines and the project references of the test projects it needs (AGENTS.md section 9: a target enters in the same pull request as the tests that meet it). Every new file under `src/` gets per-flag targets in the package manifest, demanding and realistic, with a one-sentence justification; a target of 0 needs a reason (for example the unit flag of a database store, which can only run against a real database and is reached by the integration flag; DI extension classes get unit and guard targets from the registration tests). The table "Pull request split" under Research lists the files and lines of each phase.

### Phase 1: Project and Contract

> **Goal**: Create `Encina.FeatureFlags` with the contract and the error codes every other plan of the family builds on.

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.FeatureFlags/Encina.FeatureFlags.csproj` (net10.0; references `Encina`, `Microsoft.Extensions.Options`, `Microsoft.Extensions.Logging.Abstractions`; #300 adds `Microsoft.FeatureManagement` if its Dilemma is decided for the core package), `PublicAPI.Shipped.txt`, `PublicAPI.Unshipped.txt`, project in `Encina.slnx`.
2. `Abstractions/IFeatureFlagProvider.cs` (the four methods of Design Choice 2), `Model/FeatureFlagContext.cs`, `Model/FeatureFlag.cs`, `Model/FeatureFlagRule.cs`.
3. `FeatureFlagErrors.cs` with the codes and `ErrorCategory` values of the table in the Summary (created with `EncinaErrors.Create(code, category, message)`, ADR 062); `not_supported` is added by #451, the first consumer.
4. `.github/coverage-manifest/Encina.FeatureFlags.json` with per-file targets and one-sentence justifications for the six source files of this phase.
5. Tests: project references in `tests/Encina.UnitTests/Encina.UnitTests.csproj` and `tests/Encina.GuardTests/Encina.GuardTests.csproj`; unit tests of the errors (code, category, no message in `ToString` of logs) and of the records; guard tests of the public constructors and the model validation (null and empty name).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
- Issue #604, Phase 1 of the feature-flag family (#300, #345, #451, #604). You create the package the others use.
- Encina is .NET 10 / C# 14, Railway Oriented Programming, pre-1.0 (no compatibility code). ADR 062 is merged: every error code has an ErrorCategory.
TASK:
- Create the project, the contract, the model, the errors, the manifest, the PublicAPI files, the solution entry and the unit and guard tests listed in the Phase 1 Tasks.
KEY RULES:
- Either<EncinaError, T> on every method; no exceptions for business logic.
- EncinaErrors.Create(code, category, message) with the category of the Summary table; never the two-argument overload.
- No EventIds in this phase (nothing logs yet).
- XML docs on every public member; PublicAPI.Unshipped.txt lines.
- At most 15 files and 1500 lines; tests and the manifest ship in this pull request.
REFERENCE FILES:
- src/Encina/Results/EncinaErrors.cs
- src/Encina/Errors/ErrorCategory.cs
- src/Encina.Caching.Redis/Encina.Caching.Redis.csproj (package layout)
- .github/coverage-manifest/Encina.Caching.Redis.json
```

</details>

---

### Phase 2: Options, Registration, Rule Evaluator and Bucketing

> **Goal**: `FeatureFlagsOptions`, the `AddEncinaFeatureFlags` skeleton and one deterministic evaluation engine for providers without their own.

<details>
<summary><strong>Tasks</strong></summary>

1. `FeatureFlagsOptions.cs` (`DefaultWhenUnavailable`, `CacheEnabled`, `CacheDuration`, defaults of Design Choice 13, validated on start) and `ServiceCollectionExtensions.AddEncinaFeatureFlags` (registers the options, `TimeProvider` with `TryAdd`, and the evaluator; the provider comes from a provider package).
2. `Evaluation/FeatureFlagBucketing.cs`: `int Bucket(string flagName, string key)` returns 0-99 from a truncated SHA256 (Design Choice 9).
3. `Evaluation/FeatureFlagRuleEvaluator.cs`: semantics of Design Choice 10 (time from `TimeProvider`; an unknown operator returns `invalid_rule`).
4. Tests: `FeatureFlagBucketingTests`, `FeatureFlagRuleEvaluatorTests`, `FeatureFlagsRegistrationTests` (builds the provider with `ValidateOnBuild` and `ValidateScopes`), guard tests of the evaluator and the registration, property tests of the bucketing (deterministic, roughly uniform) in `tests/Encina.PropertyTests`, the `PropertyTests` project reference, manifest entries for the four files.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
- Issue #604, Phase 2. Phase 1 (package and contract) is merged.
TASK:
- Implement the options, AddEncinaFeatureFlags, FeatureFlagBucketing and FeatureFlagRuleEvaluator with their tests and manifest entries.
KEY RULES:
- SHA256 truncated to an integer; never string.GetHashCode().
- Time only through TimeProvider; no DateTime.UtcNow.
- A rule failure returns Left invalid_rule, never a default value; an enabled flag with rules and no match is false (Design Choice 10).
- The DI test builds the provider with ValidateOnBuild and ValidateScopes.
REFERENCE FILES:
- src/Encina.FeatureFlags/Model/FeatureFlagRule.cs
- docs/plans/feature-flag-provider-implementation-plan-604.md (Design Choices 9, 10, 13)
- src/Encina.Compliance.Consent/ServiceCollectionExtensions.cs
```

</details>

---

### Phase 3: Diagnostics, EventIds and the Cache Decorator

> **Goal**: The shared diagnostics (created here, the first pull request that logs) and `CachedFeatureFlagProvider` over any `IFeatureFlagProvider` (Design Choices 6 and 14).

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina/Diagnostics/EventIdRanges.cs`: `FeatureFlags = (7300, 7319)` (7300-7305 #604, 7306-7309 #300, 7310-7315 #345; 7316-7319 stay unallocated); `AssemblyRanges` entry in `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs`; the new field in `src/Encina/PublicAPI.Unshipped.txt`.
2. `Diagnostics/FeatureFlagsDiagnostics.cs`: `ActivitySource` and `Meter` named `Encina.FeatureFlags`; counter `encina.featureflags.evaluations` (tags `provider`, `outcome`), span `encina.featureflags.evaluate` (tags `feature.name`, `provider`; no tenant id, so metric cardinality stays bounded).
3. `Diagnostics/FeatureFlagsLogMessages.cs` with `[LoggerMessage]` EventIds: 7300 cache entry evicted on change, 7301 evaluation failed (error code of the inner `Left`), 7302 cache hit rate sampled; 7303-7305 are added by Phase 4b (store read failed, stored flag invalid, tenant lookup fell back to the global flag), so the ids stay packed without gaps. No user id, no context attribute, no cache key, no `EncinaError.Message` in tags or logs.
4. `Caching/CachedFeatureFlagProvider.cs` (decorator; `IMemoryCache`; key and negative entries of Design Choice 14; TTL from `FeatureFlagsOptions.CacheDuration`), `Abstractions/IFeatureFlagChangeSource.cs` (a provider that can report changes exposes a change token; the decorator evicts on it).
5. Registration in `AddEncinaFeatureFlags` when `CacheEnabled`: the decorator wraps the provider the application or a provider package registered, in any registration order, and never replaces an application's own provider registration.
6. Tests with `FakeTimeProvider`: hit, miss, expiry, negative entry, eviction on change, a `Left` is not cached, two tenants never share an entry, no user id in a log; the DI test (`ValidateOnBuild`, `ValidateScopes`) for cache on and off; guard tests; the manifest entries for the new files and, if `EventIdRanges.cs` has none, a target in `.github/coverage-manifest/Encina.json`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT:
- Issue #604, Phase 3. Phases 1-2 are merged. The cache is in-process (Design Choice 6, pending the maintainer: if B is chosen, replace IMemoryCache by ICacheProvider here only).
TASK:
- Register the FeatureFlags EventId range, add the diagnostics and log messages, implement CachedFeatureFlagProvider, IFeatureFlagChangeSource and the registration, with their tests.
KEY RULES:
- A Left from the inner provider is never cached; the tenant is part of the cache key; the key and the context are never logged or tagged.
- Register the range in EventIdRanges.cs before using an EventId (ADR-021); EventIds 7300-7302 sequential; [LoggerMessage] source generator; XML docs name the range.
- Registration builds with ValidateOnBuild and ValidateScopes (test it).
- At most 15 files and 1500 lines.
REFERENCE FILES:
- src/Encina.FeatureFlags/ServiceCollectionExtensions.cs
- src/Encina.Caching/Abstractions/ICacheProvider.cs
- src/Encina.Compliance.Consent/Diagnostics/ConsentDiagnostics.cs
- src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

---

### Phase 4a: Store Contract, Record Mapper and In-Memory Store

> **Goal**: The contract the 10 database stores implement, the shared mapper and a store for tests.

<details>
<summary><strong>Tasks</strong></summary>

1. `Abstractions/IFeatureFlagStore.cs`: `GetAsync(name, tenantId)`, `ListAsync(tenantId)`, `UpsertAsync(FeatureFlag, tenantId)`, `DeleteAsync(name, tenantId)`, all `Task<Either<EncinaError, ...>>`; a `null` tenant means the global flag (mapped as Design Choice 8 decides); names validated (Design Choice 13).
2. `Model/FeatureFlagRecord.cs` and `Model/FeatureFlagRecordMapper.cs` (entity/JSON mapping and the tenant mapping shared by the stores).
3. `Stores/InMemoryFeatureFlagStore.cs` (for tests and local use; the provider packages never register it as a store the application did not ask for: Phase 4b has the registration rule).
4. Tests: `InMemoryFeatureFlagStoreTests`, `FeatureFlagRecordMapperTests`, guard tests of the store, the contract suite `FeatureFlagStoreContractTests` (abstract, run against the in-memory store here and against every real store by Phases 5a-5e), the `ContractTests` project reference, manifest entries.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4a</strong></summary>

```text
CONTEXT:
- Issue #604, Phase 4a. Phases 1-3 are merged.
TASK:
- Add IFeatureFlagStore, the record and mapper, the in-memory store and their unit, guard and contract tests with manifest entries.
KEY RULES:
- Store naming {Pattern}Store{Provider}; the in-memory store is InMemoryFeatureFlagStore.
- A null tenant is the global flag; at most one global row per name (Design Choice 8); a second global insert is a Left.
- Timestamps end in AtUtc and come from TimeProvider.
- The contract suite instantiates real implementations; no reflection-only tests.
REFERENCE FILES:
- src/Encina.Security.Audit/ServiceCollectionExtensions.cs
- src/Encina.Dapper.PostgreSQL/Auditing/OperationAuditStoreDapper.cs
```

</details>

---

### Phase 4b: Database-Backed Provider

> **Goal**: `DatabaseFeatureFlagProvider` over any `IFeatureFlagStore`, and the registration rule for stores.

<details>
<summary><strong>Tasks</strong></summary>

1. `Providers/DatabaseFeatureFlagProvider.cs` (reads the store, tenant row then global row per Design Choice 11, evaluates with `FeatureFlagRuleEvaluator`, missing-flag behavior of Design Choice 12).
2. `Diagnostics/FeatureFlagsLogMessages.cs`: EventIds 7303 store read failed, 7304 stored flag invalid, 7305 tenant lookup fell back to the global flag.
3. Registration: `AddEncinaFeatureFlags` registers `DatabaseFeatureFlagProvider` and an `InMemoryFeatureFlagStore` default with `TryAdd`; a database store registered by a provider package replaces the in-memory default in any registration order, and a store or provider the application registered itself is never overridden (AGENTS.md section 3, registration completeness).
4. Tests: `DatabaseFeatureFlagProviderTests`, a registration precedence test in `FeatureFlagsRegistrationTests` (database store before and after `AddEncinaFeatureFlags`, application registration kept, `ValidateOnBuild`, `ValidateScopes`), guard tests, `FeatureFlagProviderContractTests` (abstract, run against this provider and later against Redis and Unleash), manifest entries and `PublicAPI.Unshipped.txt`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4b</strong></summary>

```text
CONTEXT:
- Issue #604, Phase 4b. Phase 4a (store contract) is merged.
TASK:
- Add DatabaseFeatureFlagProvider, its log messages 7303-7305, the store registration rule and the tests.
KEY RULES:
- A tenant row wins over the global row; a null tenant reads the global row only; a missing tenant never reads another tenant's row.
- A database store wins over the in-memory default in any order; the application's own registration is never overridden.
- Never log the context, the cache key or EncinaError.Message; log the error code.
REFERENCE FILES:
- src/Encina.FeatureFlags/Abstractions/IFeatureFlagStore.cs
- src/Encina.FeatureFlags/Diagnostics/FeatureFlagsLogMessages.cs
- src/Encina.Security.Audit/ServiceCollectionExtensions.cs
```

</details>

---

### Phase 5a: ADO.NET Stores — SQL Server and PostgreSQL

> **Goal**: `FeatureFlagStoreADO` in `Encina.ADO.SqlServer` and `Encina.ADO.PostgreSQL`.

<details>
<summary><strong>Tasks</strong></summary>

Inventory of every provider store of Phases 5a-5e:

| Provider | Package | Files | Registration | Integration collection |
|----------|---------|-------|--------------|------------------------|
| ADO-SqlServer | `Encina.ADO.SqlServer` | `FeatureFlags/FeatureFlagStoreADO.cs`, `Scripts/<nnn>_CreateFeatureFlagsTable.sql` | `AddEncinaFeatureFlagsADOSqlServer` | `ADO-SqlServer` |
| ADO-PostgreSQL | `Encina.ADO.PostgreSQL` | same | `AddEncinaFeatureFlagsADOPostgreSQL` | `ADO-PostgreSQL` |
| ADO-MySQL | `Encina.ADO.MySQL` | same | `AddEncinaFeatureFlagsADOMySQL` | `ADO-MySQL` |
| Dapper-SqlServer | `Encina.Dapper.SqlServer` | `FeatureFlags/FeatureFlagStoreDapper.cs`, script | `AddEncinaFeatureFlagsDapperSqlServer` | `Dapper-SqlServer` |
| Dapper-PostgreSQL | `Encina.Dapper.PostgreSQL` | same | `AddEncinaFeatureFlagsDapperPostgreSQL` | `Dapper-PostgreSQL` |
| Dapper-MySQL | `Encina.Dapper.MySQL` | same | `AddEncinaFeatureFlagsDapperMySQL` | `Dapper-MySQL` |
| EFCore-SqlServer / -PostgreSQL / -MySQL | `Encina.EntityFrameworkCore` | `FeatureFlags/FeatureFlagStoreEF.cs`, `FeatureFlagEntityConfiguration.cs` | `AddEncinaFeatureFlagsEFCore` | `EFCore-SqlServer`, `EFCore-PostgreSQL`, `EFCore-MySQL` |
| MongoDB | `Encina.MongoDB` | `FeatureFlags/FeatureFlagStoreMongoDB.cs`, `FeatureFlagDocument.cs` | `AddEncinaFeatureFlagsMongoDB` | `MongoDB` |

Per provider of this phase (7 files, 14 for the two):

1. `FeatureFlags/FeatureFlagStoreADO.cs` (async API with `CancellationToken`, `OpenAsync`, `ExecuteNonQueryAsync`; SQL differences per AGENTS.md section 5: `TOP (@n)` vs `LIMIT @n`, `bit` vs `true/false` vs `0/1`, backtick identifiers in MySQL; the global row and its unique index per Design Choice 8).
2. `Scripts/<nnn>_CreateFeatureFlagsTable.sql` and the registration method in `ServiceCollectionExtensions.cs` (`TryAdd*`; the store wins over the in-memory default in any order; public API line in `PublicAPI.Unshipped.txt`).
3. Manifest entry of the package (per-file targets with justification: guard flag from the registration and constructor tests; unit flag 0 with the reason that the store runs only against a live database; integration flag at its target).
4. `FeatureFlagStoreADO{Provider}IntegrationTests` (`[Collection("ADO-<Database>")]`, `ClearAllDataAsync` in `InitializeAsync`, runs the contract suite of Phase 4a and the duplicate-global-row test) and `FeatureFlagStoreADO{Provider}GuardTests`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5a</strong></summary>

```text
CONTEXT:
- Issue #604, Phase 5a. IFeatureFlagStore and its contract suite exist (Phase 4a). Pattern: src/Encina.ADO.PostgreSQL/ProcessingActivity/.
TASK:
- Implement FeatureFlagStoreADO for SqlServer and PostgreSQL with scripts, registration, manifest entries and integration and guard tests.
KEY RULES:
- No synchronous Open(); CancellationToken everywhere; parameters, never string concatenation.
- Store naming FeatureFlagStoreADO; folder FeatureFlags/.
- Provider-specific SQL stays inside the provider package.
- Integration tests use the shared [Collection] fixture; never a per-class fixture; never dispose the fixture.
- At most 15 files and 1500 lines.
REFERENCE FILES:
- src/Encina.ADO.PostgreSQL/Auditing/OperationAuditStoreADO.cs
- src/Encina.ADO.PostgreSQL/Scripts/011_CreateProcessingActivitiesTable.sql
```

</details>

---

### Phase 5b: ADO.NET MySQL and Dapper SQL Server

> **Goal**: `FeatureFlagStoreADO` in `Encina.ADO.MySQL` and `FeatureFlagStoreDapper` in `Encina.Dapper.SqlServer`.

<details>
<summary><strong>Tasks</strong></summary>

1. ADO MySQL: the 7 files of Phase 5a (MySQL uses `LIMIT @n`, `0/1` and backtick identifiers).
2. Dapper SQL Server: `FeatureFlags/FeatureFlagStoreDapper.cs` with a row type private to the store (as in `OperationAuditStoreDapper`), the script, `AddEncinaFeatureFlagsDapperSqlServer`, the manifest entry, `PublicAPI.Unshipped.txt`, the integration test (`[Collection("Dapper-SqlServer")]`) and the guard test.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5b</strong></summary>

```text
CONTEXT:
- Issue #604, Phase 5b. Same contract, table and test suite as Phase 5a.
TASK:
- Implement FeatureFlagStoreADO for MySQL and FeatureFlagStoreDapper for SQL Server with scripts, registration, manifests and tests.
KEY RULES:
- Async calls with CancellationToken; same column names and semantics as the other stores.
- Store naming FeatureFlagStoreADO / FeatureFlagStoreDapper; folder FeatureFlags/.
- At most 15 files and 1500 lines.
REFERENCE FILES:
- src/Encina.Dapper.PostgreSQL/Auditing/OperationAuditStoreDapper.cs
- src/Encina.Dapper.PostgreSQL/ServiceCollectionExtensions.cs
```

</details>

---

### Phase 5c: Dapper PostgreSQL and MySQL

> **Goal**: `FeatureFlagStoreDapper` in `Encina.Dapper.PostgreSQL` and `Encina.Dapper.MySQL`.

<details>
<summary><strong>Tasks</strong></summary>

1. Per Dapper package: the 7 files of Phase 5a with `FeatureFlagStoreDapper` (inventory table in Phase 5a), integration collections `Dapper-PostgreSQL` and `Dapper-MySQL`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5c</strong></summary>

```text
CONTEXT:
- Issue #604, Phase 5c. Same contract and table as Phases 5a and 5b.
TASK:
- Implement FeatureFlagStoreDapper for PostgreSQL and MySQL with scripts, registration, manifests and tests.
KEY RULES:
- Async Dapper calls with CancellationToken; same column names and semantics as the ADO stores.
- Store naming FeatureFlagStoreDapper.
- At most 15 files and 1500 lines.
REFERENCE FILES:
- src/Encina.Dapper.PostgreSQL/Auditing/OperationAuditStoreDapper.cs
- src/Encina.Dapper.MySQL/ServiceCollectionExtensions.cs
```

</details>

---

### Phase 5d: EF Core Store

> **Goal**: `FeatureFlagStoreEF`, one class for the three EF Core databases.

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.EntityFrameworkCore/FeatureFlags/FeatureFlagStoreEF.cs`, `FeatureFlagEntityConfiguration.cs` (the unique index of Design Choice 8 expressed once for SQL Server, PostgreSQL and MySQL), `AddEncinaFeatureFlagsEFCore`, `PublicAPI.Unshipped.txt`, the manifest entry.
2. Tests: integration classes on `EFCore-SqlServer`, `EFCore-PostgreSQL` and `EFCore-MySQL` (each runs the contract suite and the duplicate-global-row test) and a guard test class.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5d</strong></summary>

```text
CONTEXT:
- Issue #604, Phase 5d. Pattern: src/Encina.EntityFrameworkCore/Auditing/.
TASK:
- Implement the EF Core store, its entity configuration and registration, with integration tests on the three EF Core collections.
KEY RULES:
- EF store is provider-agnostic; entity configuration must work on SqlServer, PostgreSQL and MySQL.
- Shared [Collection] fixtures; never a per-class fixture.
REFERENCE FILES:
- src/Encina.EntityFrameworkCore/Auditing/OperationAuditStoreEF.cs
```

</details>

---

### Phase 5e: MongoDB Store

> **Goal**: `FeatureFlagStoreMongoDB`.

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.MongoDB/FeatureFlags/FeatureFlagStoreMongoDB.cs`, `FeatureFlagDocument.cs`, `AddEncinaFeatureFlagsMongoDB` (unique index on `(TenantId, Name)` with the same global marker semantics as the SQL table), `PublicAPI.Unshipped.txt`, the manifest entry.
2. Tests: the integration class (`[Collection("MongoDB")]`, contract suite and duplicate-global-row test) and a guard test class.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5e</strong></summary>

```text
CONTEXT:
- Issue #604, Phase 5e. Pattern: src/Encina.MongoDB/ProcessingActivity/.
TASK:
- Implement the MongoDB store, document and registration with an integration test.
KEY RULES:
- MongoDB document uses the same field semantics as the SQL table.
- A second global document for a name is a Left, enforced by the unique index.
REFERENCE FILES:
- src/Encina.MongoDB/ProcessingActivity/ProcessingActivityRegistryMongoDB.cs
```

</details>

---

### Phase 6a: Redis Package Skeleton and Options

> **Goal**: The `Encina.FeatureFlags.Redis` project and its options.

<details>
<summary><strong>Tasks</strong></summary>

1. New project `src/Encina.FeatureFlags.Redis/` referencing `Encina.FeatureFlags` and `StackExchange.Redis`, with `PublicAPI.Shipped.txt`, `PublicAPI.Unshipped.txt` and the entry in `Encina.slnx`.
2. `RedisFeatureFlagsOptions.cs` (connection string is `[JsonIgnore]` with a `ToString()` override, AGENTS.md section 3; key prefix, circuit-breaker and timeout settings of Design Choice 16, validated on start).
3. Tests: project references in the unit, guard, contract and integration test projects; `RedisFeatureFlagsOptionsTests` (validation and the redacted `ToString()`); a guard test; the manifest `.github/coverage-manifest/Encina.FeatureFlags.Redis.json`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6a</strong></summary>

```text
CONTEXT:
- Issue #604, Phase 6a. Redis is not a database provider; it is a separate package.
TASK:
- Create the Encina.FeatureFlags.Redis project, its options, the manifest and the test project references.
KEY RULES:
- Options with a connection string: [JsonIgnore] and ToString() override.
- No EventIds in this phase; no type of a later phase.
REFERENCE FILES:
- src/Encina.Caching.Redis/ (registration and options pattern)
- .github/coverage-manifest/Encina.Caching.Redis.json
```

</details>

---

### Phase 6b: Redis Provider

> **Goal**: Simple flags in Redis.

<details>
<summary><strong>Tasks</strong></summary>

1. `RedisFeatureFlagProvider.cs` (key layout and JSON value of Design Choice 15; tenant, then global, per Design Choice 11; evaluation by `FeatureFlagRuleEvaluator`; the Polly pipeline of Design Choice 16: failures return `provider_failed`, an open circuit `circuit_open`), `ServiceCollectionExtensions.AddEncinaFeatureFlagsRedis` (registers the options, the pipeline and the provider with `TryAdd*`).
2. `EventIdRanges.cs`: `FeatureFlagsRedis = (7325, 7329)`, the `AssemblyRanges` entry, the `Encina` `PublicAPI.Unshipped.txt` line; `Diagnostics/RedisFeatureFlagsLogMessages.cs` (7325-7329, packed from 7325).
3. Tests: provider unit tests, a DI test (`ValidateOnBuild`, `ValidateScopes`), guard tests, a contract test that runs `FeatureFlagProviderContractTests` against the provider, an integration test on the existing `RedisFixture` with a `[Collection]`, manifest entries.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6b</strong></summary>

```text
CONTEXT:
- Issue #604, Phase 6b. Phase 6a (package and options) is merged.
TASK:
- Create the Redis provider, its registration, log messages and tests.
KEY RULES:
- Register FeatureFlagsRedis in EventIdRanges.cs first; EventIds only inside 7325-7329.
- Failures return Left provider_failed or circuit_open after the pipeline; never a default value, never EncinaError.Message in logs.
- Integration tests use the existing RedisFixture and a [Collection].
REFERENCE FILES:
- src/Encina.Caching.Redis/ (registration and options pattern)
- tests/Encina.TestInfrastructure/Fixtures/RedisFixture.cs
```

</details>

---

### Phase 7a: Unleash Package Skeleton and Options

> **Goal**: The `Encina.FeatureFlags.Unleash` project, its options and the pinned client version.

<details>
<summary><strong>Tasks</strong></summary>

1. New project `src/Encina.FeatureFlags.Unleash/` referencing `Encina.FeatureFlags` and `Unleash.Client` (version pinned in `Directory.Packages.props`, the one source), `PublicAPI.Shipped.txt`, `PublicAPI.Unshipped.txt`, the entry in `Encina.slnx`.
2. `UnleashFeatureFlagsOptions.cs` (API token `[JsonIgnore]`, `ToString()` override, server URL, app name, client timeouts; validated on start).
3. Tests: project references in the unit, guard, contract and integration test projects; options tests and a guard test; the manifest `.github/coverage-manifest/Encina.FeatureFlags.Unleash.json`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7a</strong></summary>

```text
CONTEXT:
- Issue #604, Phase 7a. Unleash evaluates remotely; it does not use FeatureFlagRuleEvaluator.
TASK:
- Create the Encina.FeatureFlags.Unleash project, its options, the pinned package version, the manifest and the test project references.
KEY RULES:
- API token: [JsonIgnore] and ToString() override.
- One version source: Directory.Packages.props.
REFERENCE FILES:
- src/Encina.Caching.Redis/ (options pattern)
- Directory.Packages.props
```

</details>

---

### Phase 7b: Unleash Provider

> **Goal**: Evaluate flags through an Unleash server.

<details>
<summary><strong>Tasks</strong></summary>

1. `UnleashFeatureFlagProvider.cs` (maps `FeatureFlagContext` to the Unleash context: `UserId`, properties from `Attributes`, the tenant as a property; evaluates from the SDK's local toggle copy, Design Choice 16), `ServiceCollectionExtensions.AddEncinaFeatureFlagsUnleash`.
2. `EventIdRanges.cs`: `FeatureFlagsUnleash = (7320, 7324)`, the `AssemblyRanges` entry and the `Encina` `PublicAPI.Unshipped.txt` line; `Diagnostics/UnleashFeatureFlagsLogMessages.cs` (7320-7324, packed from 7320).
3. `UnleashFixture` (Testcontainers, vendor LTS image tag, one source per provider) in `tests/Encina.TestInfrastructure/Fixtures/` and its collection definition.
4. Tests: provider unit tests, a DI test (`ValidateOnBuild`, `ValidateScopes`), guard tests, the provider contract suite, an integration test, manifest entries.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7b</strong></summary>

```text
CONTEXT:
- Issue #604, Phase 7b. Phase 7a (package and options) is merged.
TASK:
- Create the Unleash provider, registration, log messages, the integration fixture and the tests.
KEY RULES:
- Default container image is the vendor LTS tag; the image is defined in one place.
- An Unleash failure becomes Left provider_failed; never a default value.
- EventIds only inside 7320-7324, registered first.
REFERENCE FILES:
- src/Encina.FeatureFlags/Abstractions/IFeatureFlagProvider.cs
- tests/Encina.TestInfrastructure/Fixtures/RabbitMqFixture.cs
```

</details>

---

### Phase 8: Documentation and Finalization

> **Goal**: Document the family foundation.

<details>
<summary><strong>Tasks</strong></summary>

1. Package READMEs for `Encina.FeatureFlags`, `.Redis`, `.Unleash`; a configuration guide under `docs/features/`; `docs/INVENTORY.md`.
2. `changelog.d/604-feature-flag-provider.added.md`.
3. Justification files (no test code is skipped without a reason): `tests/Encina.LoadTests/FeatureFlags/FeatureFlags.md` (the stores are single-row reads and upserts, not a concurrent hot path; the cached path is the gate's and is measured by #345) and `tests/Encina.BenchmarkTests/FeatureFlags/FeatureFlags.md` (the benchmark of the cached evaluation path belongs to the gate, #345).
4. The issue's acceptance criterion "ADR for D1, D2, D3" is delivered by the family ADR of #345 Phase 7 (ADR number assigned when the ADR pull request opens); the plan link comment on #604 records that deferral.
5. `dotnet build --configuration Release` with 0 warnings; `dotnet test` all pass; per-flag coverage of every new file at its target, shown by the per-file targets table of `coverage-report.cs`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```text
CONTEXT:
- Issue #604, Phase 8; docs go through docs-writer, changelog and PublicAPI through mechanical-fixer.
TASK:
- Finish documentation and the justification files and verify the Release build and tests.
KEY RULES:
- Never edit CHANGELOG.md; use a fragment in changelog.d/.
- Coverage figures are cited with covref markers, never typed by hand.
- This pull request carries Fixes #604.
REFERENCE FILES:
- changelog.d/README.md
- .claude/skills/encina-docs/SKILL.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | What it asks of this feature |
|--------|-----------|------------------------------|
| SPEC-000 section 6, DEC-009 | The cloud halves of #604 move to #2694; Redis, Unleash and the 10 database stores stay | Scope of this plan |
| AGENTS.md section 5 | Database rule: all 10 providers | Stores for ADO x3, Dapper x3, EF Core x3 and MongoDB |
| AGENTS.md section 3 | ROP, `TimeProvider`, no secret in `ToString()`, fail closed, registration completeness | Either results, SHA256 bucketing, option types, registration tests |
| ADR 062 | Error category contract (merged in PR #3599) | Every error code has an `ErrorCategory` |
| ADR-018 | 12 cross-cutting functions | Matrix below |
| Unleash and Redis documentation | Client SDK behavior | Provider mapping |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|----------------------|
| `EncinaErrors.Create` (with `ErrorCategory`) | `src/Encina/Results/EncinaErrors.cs` | Error factory |
| `IRequestContext` (`TenantId`, `UserId`) | `src/Encina/Abstractions/IRequestContext.cs` | `FeatureFlagContext.From` (built by #300) |
| `EventIdRanges` | `src/Encina/Diagnostics/EventIdRanges.cs` (`OpenTelemetry` 7000-7099 is the last range below the block) | New ranges 7300-7329 (core 7300-7319, Unleash 7320-7324, Redis 7325-7329) |
| `ICacheProvider` | `src/Encina.Caching/Abstractions/ICacheProvider.cs` | Option B of Design Choice 6 only |
| `RedisCacheProvider` key prefix | `src/Encina.Caching.Redis/RedisCacheProvider.cs` | Redis key layout |
| Audit store pattern (ADO, Dapper, EF, MongoDB) | `src/Encina.*/Auditing/` | Store layout |
| `RedisFixture`, provider fixtures | `tests/Encina.TestInfrastructure/Fixtures/` | Integration tests |
| `IEncinaHealthCheck` | `src/Encina.Messaging/Health/IEncinaHealthCheck.cs` | Deferred health check |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| **`Encina.FeatureFlags`** | **7300-7319** | New, registered in Phase 3; #604 uses 7300-7305, #300 7306-7309, #345 7310-7315; 7316-7319 are unallocated |
| **`Encina.FeatureFlags.Unleash`** | **7320-7324** | New, registered in Phase 7b; claimed by this plan |
| **`Encina.FeatureFlags.Redis`** | **7325-7329** | New, registered in Phase 6b; claimed by this plan |
| `Encina.FeatureFlags.OpenFeature` / `.ConfigCat` | 7330-7339 | Owned and registered by #451 (not registered here) |
| `Encina.ServiceDiscovery*` | 7340-7349 | Owned by #607 |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Core, Phases 1-4b | 63 | Contract, errors, evaluator, diagnostics, cache, store contract, database provider, with their tests |
| Database stores, Phases 5a-5e | 58 | 10 providers (EF Core is one class for 3 databases), scripts, registrations, integration and guard tests |
| Redis, Phases 6a-6b | 25 | Package, options, provider, tests |
| Unleash, Phases 7a-7b | 28 | Package, options, provider, fixture, tests |
| Documentation, Phase 8 | 8 | README x3, guide, INVENTORY, fragment, 2 justification files |
| **Total** | **182** | 15 pull requests of at most 15 files and 1500 lines (next table) |

#### Pull Request Split

Files include tests, manifests, `PublicAPI` files, solution and project references, and the changelog fragment; line counts are estimates re-measured when each pull request opens. Every pull request is at most 15 files and 1500 lines; no pull request uses a type, project or log class that a later one creates.

| Phase | Content | Files | Lines |
|-------|---------|------:|------:|
| 1 | Project, contract, model, errors, manifest, solution entry, 2 test project references, unit and guard tests | 15 | 650 |
| 2 | Options, `AddEncinaFeatureFlags`, bucketing, evaluator, manifest, PublicAPI, property project reference, unit, guard, DI and property tests | 13 | 700 |
| 3 | EventId range, diagnostics, log messages (7300-7302), cache decorator, change source, registration, manifest and PublicAPI (2), allocation test, unit, DI and guard tests | 15 | 900 |
| 4a | Store contract, record, mapper, in-memory store, contract project reference, manifest, PublicAPI, unit, guard and contract tests | 11 | 700 |
| 4b | `DatabaseFeatureFlagProvider`, log messages 7303-7305, registration, manifest, PublicAPI, unit, DI, guard and provider contract tests | 9 | 650 |
| 5a | ADO SqlServer and PostgreSQL: store, script, registration, PublicAPI, manifest, integration and guard tests (7 files each) | 14 | 1,200 |
| 5b | ADO MySQL and Dapper SqlServer (7 files each) | 14 | 1,200 |
| 5c | Dapper PostgreSQL and MySQL (7 files each) | 14 | 1,200 |
| 5d | EF Core: store, entity configuration, registration, PublicAPI, manifest, 3 integration classes, guard tests | 9 | 800 |
| 5e | MongoDB: store, document, registration, PublicAPI, manifest, integration and guard tests | 7 | 600 |
| 6a | Redis package skeleton, options, solution entry, manifest, PublicAPI (2), 4 test project references, options and guard tests | 12 | 450 |
| 6b | Redis provider, registration, log messages, EventId range and allocation test, PublicAPI (2), manifest, unit, DI, guard, contract and integration tests | 13 | 800 |
| 7a | Unleash package skeleton, options, `Directory.Packages.props`, solution entry, manifest, PublicAPI (2), 4 test project references, options and guard tests | 13 | 450 |
| 7b | Unleash provider, registration, log messages, EventId range and allocation test, PublicAPI (2), manifest, `UnleashFixture` and collection, unit, DI, guard, contract and integration tests | 15 | 900 |
| 8 | 3 READMEs, guide, `INVENTORY.md`, changelog fragment, load and benchmark justification files | 8 | 450 |
| **Total** | 15 pull requests | **182** | **11,650** |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
You are implementing issue #604: IFeatureFlagProvider and its implementations (database, Redis, Unleash).

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library with Railway Oriented Programming; pre-1.0, no compatibility code. ADR 062 (error categories) is merged.
- This issue owns the foundation of the feature-flag family (#300, #345, #451): package, contract, errors, core EventIds.
- Provider category: Database (10) for the store; Redis and Unleash are separate packages.

IMPLEMENTATION OVERVIEW:
Phase 1: package, contract, errors. Phase 2: options, registration, bucketing and rule evaluator.
Phase 3: EventId range 7300-7319, diagnostics, in-process cache decorator.
Phase 4a-4b: IFeatureFlagStore, mapper, in-memory store, DatabaseFeatureFlagProvider.
Phase 5a-5e: ADO x3, Dapper x3, EF Core, MongoDB stores.
Phase 6a-6b: Redis package and provider. Phase 7a-7b: Unleash package and provider. Phase 8: documentation.

KEY PATTERNS:
- Either<EncinaError, T> everywhere; every error code has an ErrorCategory; Left never swallowed; TimeProvider for time.
- Store names {Pattern}Store{Provider}; folder FeatureFlags/; satellite DI AddEncinaFeatureFlags{Provider} with TryAdd*.
- Options with secrets: [JsonIgnore] and ToString() override.
- Each phase is one pull request of at most 15 files and 1500 lines, with its own tests, manifest entries and PublicAPI lines.

REFERENCE FILES:
- docs/plans/feature-flag-provider-implementation-plan-604.md (this plan)
- src/Encina.Compliance.Consent/ServiceCollectionExtensions.cs
- src/Encina.ADO.PostgreSQL/Auditing/OperationAuditStoreADO.cs
- AGENTS.md sections 3, 5, 6, 7, 9
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ✅ Phase 3 | `CachedFeatureFlagProvider` (in-process, Design Choice 6) |
| 2 | OpenTelemetry | ✅ Phase 3 | `ActivitySource` and `Meter` `Encina.FeatureFlags`; evaluation counter and span |
| 3 | Structured Logging | ✅ Phases 3, 4b, 6b, 7b | `[LoggerMessage]` in 7300-7305 (core) and the provider ranges |
| 4 | Health Checks | ⏭️ Deferred | Provider health check: follow-up issue to be opened (draft: artifacts/decisions/plan-batch/followups/feature-flag-provider-health-checks.md) |
| 5 | Validation | ✅ Phases 2, 4a | Flag name (Design Choice 13) and tenant validated at the store boundary; options validated on start |
| 6 | Resilience | ✅ Phases 6b, 7b | Redis: Polly timeout and circuit breaker inside its package; Unleash: SDK-local evaluation (Design Choice 16); failure returns `provider_failed` or `circuit_open` |
| 7 | Distributed Locks | ❌ N/A | Evaluation is a read; flag writes are single-row upserts |
| 8 | Transactions | ❌ N/A | One row per operation; no multi-operation atomicity needed |
| 9 | Idempotency | ❌ N/A | Evaluation is deterministic; an upsert of the same flag is idempotent |
| 10 | Multi-Tenancy | ✅ Phases 3, 4a, 4b | `TenantId` column with the global marker, tenant-over-global precedence, tenant in the cache key |
| 11 | Module Isolation | ❌ N/A | Flag names are chosen by the application and shared across modules; a module name can be a context attribute for rules that need it (the issue's table marked it Defer; the family settled on not applicable) |
| 12 | Audit Trail | ⏭️ Deferred | Audit of flag changes through `IOperationAuditStore`: follow-up issue to be opened (draft: artifacts/decisions/plan-batch/followups/feature-flag-change-audit.md) |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | Status | Notes |
|-------------|--------|-------|
| `EncinaErrors` (with `ErrorCategory`, ADR 062), `IRequestContext`, `TimeProvider` abstractions | Done | In `src/Encina`; ADR 062 merged in PR #3599 |
| Provider fixtures for the 10 databases and Redis | Done | `tests/Encina.TestInfrastructure/Fixtures/` |

### Recommended (Not Blocking)

| Dependency | Issue | Notes |
|-----------|-------|-------|
| Pipeline gate and `Microsoft.FeatureManagement` provider | #300, #345 | Consumers of Phases 1-3 |
| OpenFeature and ConfigCat providers | #451 | Consumers of Phase 1 |
| SaaS providers (Azure App Configuration, LaunchDarkly, Split.io) | #2694 | Cloud milestone, outside 1.0 |
| Composite provider (several backends at once) | follow-up issue to be opened (draft: artifacts/decisions/plan-batch/followups/feature-flag-composite-provider.md) | The motivation mentions "use several at once"; not part of this plan |

---

## Next Steps

1. Review this plan; the Dilemmas (2, 3, 5, 6, 7, 8, 10, 11, 12, 13, 14 and 16) are asked when work on the issue starts.
2. Post the plan link on #604, with the note that the family ADR (the issue's D1, D2, D3) is delivered by #345 Phase 7.
3. Phase 1 is the first pull request of the whole family; #300, #345 and #451 start after Phases 1-3 merge.
4. Each phase is one pull request of at most 15 files and 1500 lines; the last one (Phase 8) carries `Fixes #604`.
