# Implementation Plan: `IFeatureFlagProvider` — Abstraction and Implementations (Database, Redis, Unleash)

> **Issue**: [#604](https://github.com/dlrivada/Encina/issues/604)
> **Type**: Feature
> **Complexity**: High (provider abstraction, 10 database providers, 2 new provider packages)
> **Estimated Scope**: ~2,300 lines of production code + ~3,000 lines of tests, delivered in about 12 pull requests

---

## Summary

Define the provider abstraction of the feature-flag family, `IFeatureFlagProvider`, in a new `Encina.FeatureFlags` package, and implement it for database-backed storage (ADO.NET x3, Dapper x3, EF Core x3, MongoDB), Redis and Unleash. Nothing of it exists in `src/` today: no `Encina.FeatureFlags` project, no `Microsoft.FeatureManagement`, `Unleash` or `OpenFeature` reference (`Directory.Packages.props` lists `StackExchange.Redis` 2.12.14 and `MongoDB.Driver` 3.12.0 only).

This plan **owns the foundation of the family** (#300, #345, #451, #604, with #2694 for the SaaS providers, outside 1.0 per SPEC-000 section 6): the project, the contract, the error codes, the shared diagnostics, the rule evaluator, the cache decorator and the EventId range. The other three plans build on its Phase 1.

| Shared piece | Owner | Used by |
|---|---|---|
| Project `Encina.FeatureFlags`, `IFeatureFlagProvider`, `FeatureFlagContext`, `FeatureFlag`, `FeatureFlagRule`, `FeatureFlagErrors`, `FeatureFlagsOptions`, `AddEncinaFeatureFlags` skeleton | #604 Phase 1 | #300, #345, #451 |
| EventId range `FeatureFlags` 7300-7319 and the four provider ranges 7320-7339 in `EventIdRanges.cs` | #604 Phase 1 (registers all five) | each plan allocates inside its own sub-block |
| `FeatureFlagBucketing` (SHA256 bucketing), `FeatureFlagRuleEvaluator`, `CachedFeatureFlagProvider` | #604 Phases 2-3 | #345, #300 |
| `FeatureFlagPipelineBehavior<,>`, `[FeatureFlag]`, `Microsoft.FeatureManagement` provider | #300 (gate slice) and #345 | — |
| `Encina.FeatureFlags.OpenFeature`, `.ConfigCat` | #451 | — |

**Affected packages**: new `Encina.FeatureFlags`, `Encina.FeatureFlags.Redis`, `Encina.FeatureFlags.Unleash`; the `FeatureFlags/` folder of `Encina.ADO.SqlServer|PostgreSQL|MySQL`, `Encina.Dapper.SqlServer|PostgreSQL|MySQL`, `Encina.EntityFrameworkCore`, `Encina.MongoDB`; `src/Encina/Diagnostics/EventIdRanges.cs`.

**Provider category**: Database (10) for the store, plus two non-database providers (Redis, Unleash).

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
<summary><strong>2. The contract — Either results, a context record, <code>Option</code> for a missing flag</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Task<Either<EncinaError, T>>` on all four methods; `GetFlagAsync` returns `Either<EncinaError, Option<FeatureFlag>>`; `FeatureFlagContext(UserId, TenantId, Attributes)`** | Provider failure is explicit; matches every Encina store; a missing flag is not an error | Callers unwrap an `Either` |
| **B) The original bare `Task<bool>` / `Task<T?>`** | Simplest call site | Hides provider failure; contradicts AGENTS.md section 3 |
| **C) `ValueTask` returns** | Fewer allocations on the cached path | Differs from the issue sketch; stores in this repository return `ValueTask` for hot paths and `Task` elsewhere, so no rule forces it |

### Chosen Option: **A — `Task<Either<EncinaError, T>>`**

### Rationale

- AGENTS.md section 3 requires `Either<EncinaError, T>` for operations; the issue already rewrote the sketch this way.
- `FeatureFlagContext` carries `UserId`, `TenantId` and free `Attributes`. The #451 sketch had `Environment` as a field: it becomes the attribute `environment`, so there is one context type for the family (#604 owns it; `FeatureFlagContext.From(IRequestContext)` is built in #345 Phase 1).
- `ValueTask` for the cached hot path is an implementation detail of `CachedFeatureFlagProvider` benchmarks (Phase 9), not a contract change.

**Class:** Settled
**Settled by:** AGENTS.md section 3 (Railway Oriented Programming), ADR-001, ADR-006; issue #604 body (interface definition)

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
| **A) All 10 providers, grouped in one pull request per family (ADO, Dapper, EF Core, MongoDB)** | Follows AGENTS.md section 5; each pull request stays under 15 files | The feature is complete only when the last pull request merges |
| **B) A subset first (for example Dapper) and the rest later** | Smaller first release | Breaks provider coherence while the feature is released |
| **C) One pull request for all 10** | One review | About 40 files; the `pr-size` gate fails it (limit 15 files, 1500 lines) |

### Chosen Option: **A — All 10, one pull request per family**

### Rationale

- The issue's D2 asks "all 10 or phased"; both answers deliver all 10, and the size rule forces the phasing, so only A complies with section 5 and section 11 together.
- The EF Core store is one class (`FeatureFlagStoreEF`) in `Encina.EntityFrameworkCore`; the three EF Core database providers are covered by running its integration tests on SQL Server, PostgreSQL and MySQL (as `OperationAuditStoreEF` is).

**Class:** Settled
**Settled by:** AGENTS.md section 5 (database rule: every provider-dependent feature for all 10 providers) and section 11 (`pr-size`: at most 15 files and 1500 lines per pull request)

</details>

<details>
<summary><strong>5. Redis and Unleash are part of this issue</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Both packages in this issue** | Matches the issue and the acceptance criteria; Redis and Unleash run locally (Docker), so they are testable in CI | Two more packages to maintain |
| **B) Redis waits for demand** | Smaller first release | Contradicts the acceptance criteria; Redis is the usual backend for simple flags |

### Chosen Option: **A — Both in this issue**

### Rationale

- SPEC-000 section 6 moves only the cloud halves of #604 to the cloud milestone (#2694); everything else in the issue stays in 1.0 ("every other provider, transport and integration is in the 1.0 contract", DEC-009).
- Unleash is open source and runs in Docker; Redis has `RedisFixture` in `tests/Encina.TestInfrastructure/Fixtures/RedisFixture.cs`.

**Class:** Settled
**Settled by:** SPEC-000 section 6 (cloud halves of #604 split to #2694) and DEC-009; issue #604 acceptance criteria

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
- The cache key is `(flag name, tenant, context hash)`; the tenant is part of the key (AGENTS.md section 6, function 10).
- B stays possible later: a different decorator over `ICacheProvider` can be registered without a contract change.

**Class:** Dilemma

</details>

<details>
<summary><strong>7. Persistence model — one flags table, tenant-scoped, rules as one JSON column</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Table `FeatureFlags` (`Name`, `TenantId` nullable, `IsEnabled`, `Description`, `RulesJson`, `UpdatedAtUtc`), unique on `(TenantId, Name)`** | One read per flag; works in all 10 providers; null tenant means a global flag | Rules are not queryable in SQL |
| **B) A second table `FeatureFlagRules`** | Queryable rules | A join on every evaluation and 10 sets of two scripts; MongoDB differs |

### Chosen Option: **A — One table, JSON rules**

### Rationale

- Flags are read by name, never searched by rule; a JSON column keeps the schema identical across providers.
- `TenantId` follows the multi-tenancy convention (function 10); `UpdatedAtUtc` uses the `AtUtc` suffix and `TimeProvider` (AGENTS.md sections 3 and 4).
- Scripts follow `src/Encina.ADO.PostgreSQL/Scripts/` numbering: the next free number at implementation time.

**Class:** Trivial
**Settled by:** AGENTS.md sections 4 and 6; convention of `src/Encina.*/Scripts/*_Create*Table.sql` and the `ProcessingActivity` stores

</details>

<details>
<summary><strong>8. Rule evaluation and percentage bucketing</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One `FeatureFlagRuleEvaluator` in core; bucketing by a truncated SHA256 of `flag name + bucketing key`** | Deterministic across processes and instances; every provider that has no engine of its own (database, Redis) behaves the same | An evaluator to maintain |
| **B) `string.GetHashCode()`** | One line | Not deterministic across processes in .NET |

### Chosen Option: **A — Shared evaluator with SHA256 bucketing**

### Rationale

- The maintainer fixed the bucketing rule in the comment of 2026-03-18 on #345. Time-based rules read `TimeProvider`.
- Unleash and (in #451) OpenFeature providers evaluate remotely or in their SDK and do not use the evaluator.

**Class:** Settled
**Settled by:** Maintainer comment of 2026-03-18 on #345 (SHA256 bucketing, `TimeProvider`); AGENTS.md section 3 (time)

</details>

---

## Implementation Phases

### Phase 1: Project, Contract, Errors and EventId Range

> **Goal**: Create `Encina.FeatureFlags` and the contract every other plan of the family builds on.

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.FeatureFlags/Encina.FeatureFlags.csproj` (net10.0; references `Encina`, `Microsoft.Extensions.Options`, `Microsoft.Extensions.Logging.Abstractions`; #300 Phase 2 adds `Microsoft.FeatureManagement` if its Dilemma 3 is decided for the core package).
2. `Abstractions/IFeatureFlagProvider.cs` (four methods of Design Choice 2), `Model/FeatureFlagContext.cs`, `Model/FeatureFlag.cs`, `Model/FeatureFlagRule.cs`.
3. `FeatureFlagErrors.cs` with codes `encina.featureflags.not_found`, `provider_failed`, `invalid_rule`, `disabled` (the last one is used by #300/#345).
4. `FeatureFlagsOptions.cs` (`DefaultWhenUnavailable`, `CacheEnabled`, `CacheDuration`) and `ServiceCollectionExtensions.AddEncinaFeatureFlags` (registers options and `TimeProvider`; the provider comes from a provider package).
5. `src/Encina/Diagnostics/EventIdRanges.cs`: `FeatureFlags = (7300, 7319)`, `FeatureFlagsUnleash = (7320, 7324)`, `FeatureFlagsRedis = (7325, 7329)`, `FeatureFlagsOpenFeature = (7330, 7334)`, `FeatureFlagsConfigCat = (7335, 7339)`; `AssemblyRanges` entries in `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs`.
6. `PublicAPI.Unshipped.txt`, `.github/coverage-manifest/Encina.FeatureFlags.json`, project in `Encina.slnx`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
- Issue #604, Phase 1 of the feature-flag family (#300, #345, #451, #604). You create the package the others use.
- Encina is .NET 10 / C# 14, Railway Oriented Programming, pre-1.0 (no compatibility code).
TASK:
- Create the project, the contract, the errors, the options, AddEncinaFeatureFlags and the EventId ranges listed in the Phase 1 Tasks.
KEY RULES:
- Either<EncinaError, T> on every method; no exceptions for business logic.
- Register all five EventId ranges first (ADR-021) and update the AssemblyRanges map; run the architecture tests.
- Options validated on start; TryAdd* registration; XML docs on every public member; PublicAPI.Unshipped.txt.
- Add guard and unit tests in the same pull request and the coverage-manifest targets with justifications.
REFERENCE FILES:
- src/Encina.Compliance.Consent/ServiceCollectionExtensions.cs (registration pattern)
- src/Encina/Diagnostics/EventIdRanges.cs
- src/Encina/Results/EncinaErrors.cs
```

</details>

---

### Phase 2: Rule Evaluator and Bucketing

> **Goal**: One deterministic evaluation engine for providers without their own.

<details>
<summary><strong>Tasks</strong></summary>

1. `Evaluation/FeatureFlagBucketing.cs`: `int Bucket(string flagName, string key)` returns 0-99 from a truncated SHA256.
2. `Evaluation/FeatureFlagRuleEvaluator.cs`: operators `equals`, `not_equals`, `in`, `percentage`, `after_utc`, `before_utc` (time from `TimeProvider`); first matching rule wins; an unknown operator returns `invalid_rule`.
3. Property tests for the bucketing (deterministic, roughly uniform) in `Encina.PropertyTests`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
- Issue #604, Phase 2. Phase 1 (contract and package) is merged.
TASK:
- Implement FeatureFlagBucketing and FeatureFlagRuleEvaluator from the Phase 2 Tasks.
KEY RULES:
- SHA256 truncated to an integer; never string.GetHashCode().
- Time only through TimeProvider; no DateTime.UtcNow.
- A rule failure returns Left, never a default value.
REFERENCE FILES:
- src/Encina.FeatureFlags/Model/FeatureFlagRule.cs
- docs/plans/feature-flag-provider-implementation-plan-604.md (Design Choice 8)
```

</details>

---

### Phase 3: Cache Decorator and Change Notification

> **Goal**: `CachedFeatureFlagProvider` over any `IFeatureFlagProvider` (Design Choice 6).

<details>
<summary><strong>Tasks</strong></summary>

1. `Caching/CachedFeatureFlagProvider.cs` (decorator; `IMemoryCache`; key `(name, tenant, context hash)`; TTL from `FeatureFlagsOptions.CacheDuration`).
2. `Abstractions/IFeatureFlagChangeSource.cs` (a provider that can report changes exposes a change token; the decorator evicts on it).
3. Registration in `AddEncinaFeatureFlags` when `CacheEnabled` (decorates the registered provider with `Decorate`-style `TryAdd` ordering; no override of the application's registration).
4. Unit tests with `FakeTimeProvider`: hit, miss, expiry, eviction on change, two tenants never share an entry.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT:
- Issue #604, Phase 3. The cache is in-process (Design Choice 6, pending the maintainer: if B is chosen, replace IMemoryCache by ICacheProvider here only).
TASK:
- Implement CachedFeatureFlagProvider, IFeatureFlagChangeSource and the registration.
KEY RULES:
- A Left from the inner provider is never cached.
- The tenant is part of the cache key.
- Registration builds with ValidateOnBuild and ValidateScopes (test it).
REFERENCE FILES:
- src/Encina.FeatureFlags/ServiceCollectionExtensions.cs
- src/Encina.Caching/Abstractions/ICacheProvider.cs
```

</details>

---

### Phase 4: Store Contract, In-Memory Store and Database-Backed Provider

> **Goal**: The contract the 10 database stores implement, and the provider on top of it.

<details>
<summary><strong>Tasks</strong></summary>

1. `Abstractions/IFeatureFlagStore.cs`: `GetAsync(name, tenantId)`, `ListAsync(tenantId)`, `UpsertAsync(FeatureFlag, tenantId)`, `DeleteAsync(name, tenantId)`, all `Task<Either<EncinaError, ...>>`.
2. `Stores/InMemoryFeatureFlagStore.cs` (testing and default for tests only).
3. `Providers/DatabaseFeatureFlagProvider.cs` (reads the store, evaluates with `FeatureFlagRuleEvaluator`, tenant flag wins over the global flag of the same name).
4. `Model/FeatureFlagRecord.cs` and `Model/FeatureFlagRecordMapper.cs` (entity/JSON mapping shared by the stores).
5. Registration precedence test: a database store registered in any order wins over the in-memory default and over the application's own registration is not overridden (AGENTS.md section 3).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT:
- Issue #604, Phase 4. Phases 1-3 are merged.
TASK:
- Add IFeatureFlagStore, the in-memory store, DatabaseFeatureFlagProvider and the shared record/mapper.
KEY RULES:
- Store naming {Pattern}Store{Provider}; the in-memory store is InMemoryFeatureFlagStore.
- TenantId null means a global flag; a tenant row overrides it.
- Timestamps end in AtUtc and come from TimeProvider.
REFERENCE FILES:
- src/Encina.Security.Audit/ServiceCollectionExtensions.cs
- src/Encina.Dapper.PostgreSQL/Auditing/OperationAuditStoreDapper.cs
```

</details>

---

### Phase 5a: ADO.NET Stores (SqlServer, PostgreSQL, MySQL)

> **Goal**: `FeatureFlagStoreADO` in the three ADO.NET packages.

<details>
<summary><strong>Tasks</strong></summary>

Draft of the per-provider inventory (local model draft, reviewed; script number left to the implementer):

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

1. Per ADO package: `FeatureFlags/FeatureFlagStoreADO.cs` (async API with `CancellationToken`, `OpenAsync`, `ExecuteNonQueryAsync`; SQL differences per AGENTS.md section 5: `TOP (@n)` vs `LIMIT @n`, `bit` vs `true/false` vs `0/1`, backtick identifiers in MySQL).
2. Per ADO package: `Scripts/<nnn>_CreateFeatureFlagsTable.sql` and the registration method in `ServiceCollectionExtensions.cs` (`TryAdd*`, called before `AddEncinaFeatureFlags`).
3. Manifest entries for the three packages.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5a</strong></summary>

```text
CONTEXT:
- Issue #604, Phase 5a. IFeatureFlagStore exists (Phase 4). Pattern: src/Encina.ADO.PostgreSQL/ProcessingActivity/.
TASK:
- Implement FeatureFlagStoreADO for SqlServer, PostgreSQL and MySQL with scripts and registration.
KEY RULES:
- No synchronous Open(); CancellationToken everywhere; parameters, never string concatenation.
- Store naming FeatureFlagStoreADO; folder FeatureFlags/.
- Provider-specific SQL stays inside the provider package.
REFERENCE FILES:
- src/Encina.ADO.PostgreSQL/Auditing/OperationAuditStoreADO.cs
- src/Encina.ADO.PostgreSQL/Scripts/011_CreateProcessingActivitiesTable.sql
```

</details>

---

### Phase 5b: Dapper Stores (SqlServer, PostgreSQL, MySQL)

> **Goal**: `FeatureFlagStoreDapper` in the three Dapper packages.

<details>
<summary><strong>Tasks</strong></summary>

1. Per Dapper package: `FeatureFlags/FeatureFlagStoreDapper.cs`, the SQL script and `AddEncinaFeatureFlagsDapper{Provider}` (inventory table in Phase 5a).
2. Row type private to the store, as in `OperationAuditStoreDapper`.
3. Manifest entries for the three packages.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5b</strong></summary>

```text
CONTEXT:
- Issue #604, Phase 5b. Same contract and table as Phase 5a.
TASK:
- Implement FeatureFlagStoreDapper for SqlServer, PostgreSQL and MySQL.
KEY RULES:
- Async Dapper calls with CancellationToken; same column names and semantics as the ADO stores.
- Store naming FeatureFlagStoreDapper.
REFERENCE FILES:
- src/Encina.Dapper.PostgreSQL/Auditing/OperationAuditStoreDapper.cs
- src/Encina.Dapper.PostgreSQL/ServiceCollectionExtensions.cs
```

</details>

---

### Phase 5c: EF Core and MongoDB Stores

> **Goal**: `FeatureFlagStoreEF` (one class for the 3 EF Core databases) and `FeatureFlagStoreMongoDB`.

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.EntityFrameworkCore/FeatureFlags/FeatureFlagStoreEF.cs`, `FeatureFlagEntityConfiguration.cs`, `AddEncinaFeatureFlagsEFCore`.
2. `src/Encina.MongoDB/FeatureFlags/FeatureFlagStoreMongoDB.cs`, `FeatureFlagDocument.cs`, `AddEncinaFeatureFlagsMongoDB` (unique index on `(TenantId, Name)`).
3. Manifest entries for both packages.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5c</strong></summary>

```text
CONTEXT:
- Issue #604, Phase 5c. Pattern: src/Encina.EntityFrameworkCore/ProcessingActivity/ and src/Encina.MongoDB/ProcessingActivity/.
TASK:
- Implement the EF Core store and the MongoDB store with registration.
KEY RULES:
- EF store is provider-agnostic; entity configuration must work on SqlServer, PostgreSQL and MySQL.
- MongoDB document uses the same field semantics as the SQL table.
REFERENCE FILES:
- src/Encina.EntityFrameworkCore/Auditing/OperationAuditStoreEF.cs
- src/Encina.MongoDB/ProcessingActivity/ProcessingActivityRegistryMongoDB.cs
```

</details>

---

### Phase 6: Redis Provider (`Encina.FeatureFlags.Redis`)

> **Goal**: Simple flags in Redis.

<details>
<summary><strong>Tasks</strong></summary>

1. New project `src/Encina.FeatureFlags.Redis/` referencing `Encina.FeatureFlags` and `StackExchange.Redis`.
2. `RedisFeatureFlagProvider.cs` (key `encina:featureflags:{tenant}:{name}`; the value is the JSON of `FeatureFlag`; evaluation by `FeatureFlagRuleEvaluator`), `RedisFeatureFlagsOptions.cs` (connection string is `[JsonIgnore]` with a `ToString()` override, AGENTS.md section 3), `ServiceCollectionExtensions.AddEncinaFeatureFlagsRedis`, `Diagnostics/RedisFeatureFlagsLogMessages.cs` (7325-7329).
3. Resilience: calls go through the shared Polly policy of the family (Phase 9); failures return `provider_failed`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```text
CONTEXT:
- Issue #604, Phase 6. Redis is not a database provider; it is a separate package.
TASK:
- Create Encina.FeatureFlags.Redis with the provider, options and registration.
KEY RULES:
- Options with a connection string: [JsonIgnore] and ToString() override.
- EventIds only inside 7325-7329.
- Integration tests use the existing RedisFixture and a [Collection].
REFERENCE FILES:
- src/Encina.Caching.Redis/ (registration and options pattern)
- tests/Encina.TestInfrastructure/Fixtures/RedisFixture.cs
```

</details>

---

### Phase 7: Unleash Provider (`Encina.FeatureFlags.Unleash`)

> **Goal**: Evaluate flags through an Unleash server.

<details>
<summary><strong>Tasks</strong></summary>

1. New project `src/Encina.FeatureFlags.Unleash/` referencing `Encina.FeatureFlags` and `Unleash.Client` (version pinned in `Directory.Packages.props`).
2. `UnleashFeatureFlagProvider.cs` (maps `FeatureFlagContext` to the Unleash context: `UserId`, properties from `Attributes`, the tenant as a property), `UnleashFeatureFlagsOptions.cs` (API token `[JsonIgnore]`, `ToString()` override), registration, log messages 7320-7324.
3. Test fixture `UnleashFixture` (Testcontainers, vendor LTS image tag, one source per provider) in `tests/Encina.TestInfrastructure/Fixtures/`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```text
CONTEXT:
- Issue #604, Phase 7. Unleash evaluates remotely; it does not use FeatureFlagRuleEvaluator.
TASK:
- Create Encina.FeatureFlags.Unleash with the provider, options, registration and the integration fixture.
KEY RULES:
- Default container image is the vendor LTS tag; the image is defined in one place.
- A Unleash failure becomes Left provider_failed; never a default value.
REFERENCE FILES:
- src/Encina.FeatureFlags/Abstractions/IFeatureFlagProvider.cs
- tests/Encina.TestInfrastructure/Fixtures/RabbitMqFixture.cs
```

</details>

---

### Phase 8: Observability

> **Goal**: Shared diagnostics for the family, reported by the cache and the providers.

<details>
<summary><strong>Tasks</strong></summary>

1. `Diagnostics/FeatureFlagsDiagnostics.cs`: `ActivitySource` and `Meter` named `Encina.FeatureFlags`; counter `encina.featureflags.evaluations` (tags `provider`, `outcome`), span `encina.featureflags.evaluate` (tags `feature.name`, `provider`).
2. `Diagnostics/FeatureFlagsLogMessages.cs` with `[LoggerMessage]` EventIds 7300-7305 (this plan's sub-block: evaluation failed, cache evicted, store read failed, invalid rule, provider unavailable, cache hit rate sampled).
3. Tenant id is a tag; no user id, no flag context attributes, no `EncinaError.Message` in tags or logs.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```text
CONTEXT:
- Issue #604, Phase 8. The ranges are registered in Phase 1.
TASK:
- Add the diagnostics and log messages and call them from the providers and the cache decorator.
KEY RULES:
- EventIds sequential inside 7300-7305; [LoggerMessage] source generator; XML docs name the range.
- Never log EncinaError.Message; log the error code.
REFERENCE FILES:
- src/Encina.Compliance.Consent/ConsentDiagnostics.cs
- src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

---

### Phase 9: Testing

> **Goal**: Every applicable test type for the foundation, the 10 stores, Redis and Unleash.

<details>
<summary><strong>Tasks</strong></summary>

1. Unit: evaluator, bucketing, cache (with `FakeTimeProvider`), providers, registration (DI test with `ValidateOnBuild` and `ValidateScopes`).
2. Guard: every public method with parameters.
3. Contract: one suite over `IFeatureFlagProvider` and one over `IFeatureFlagStore`, run against every real implementation.
4. Property: bucketing determinism and uniformity; rule order.
5. Integration (`[Collection]` fixtures, `ClearAllDataAsync` in `InitializeAsync`): 10 database stores, Redis, Unleash.
6. Load and benchmark: justification `.md` files for the load test; benchmark of the cached path is covered by #345 (the gate's hot path).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 9</strong></summary>

```text
CONTEXT:
- Issue #604, Phase 9. Per-flag coverage targets come from .github/coverage-manifest/*.json.
TASK:
- Write the tests of the Phase 9 Tasks and reach every flag's per-file target.
KEY RULES:
- Tests execute real package code; no reflection-only tests.
- Shared [Collection("<Family>-<Database>")] fixtures; never a per-class database fixture.
- CRAP <= 10 on changed methods.
REFERENCE FILES:
- docs/testing/integration-tests.md
- tests/Encina.IntegrationTests (existing audit store tests)
```

</details>

---

### Phase 10: Documentation and Finalization

> **Goal**: Document the family foundation.

<details>
<summary><strong>Tasks</strong></summary>

1. XML docs on all public APIs; package READMEs for `Encina.FeatureFlags`, `.Redis`, `.Unleash`.
2. `changelog.d/604-feature-flag-provider.added.md`; `docs/INVENTORY.md`; the family ADR is owned by #345 (ADR number assigned when the ADR pull request opens).
3. Configuration guide under `docs/features/`.
4. `dotnet build --configuration Release` with 0 warnings; `dotnet test` all pass; per-flag coverage at target.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 10</strong></summary>

```text
CONTEXT:
- Issue #604, Phase 10; docs go through docs-writer, changelog and PublicAPI through mechanical-fixer.
TASK:
- Finish documentation and verify the Release build and tests.
KEY RULES:
- Never edit CHANGELOG.md; use a fragment in changelog.d/.
- Coverage figures are cited with covref markers, never typed by hand.
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
| AGENTS.md section 3 | ROP, `TimeProvider`, no secret in `ToString()`, fail closed | Either results, SHA256 bucketing, option types |
| ADR-018 | 12 cross-cutting functions | Matrix below |
| Unleash and Redis documentation | Client SDK behavior | Provider mapping |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|----------------------|
| `EncinaErrors.Create` | `src/Encina/Results/EncinaErrors.cs:25` | Error factory |
| `IRequestContext` (`TenantId`, `UserId`) | `src/Encina/Abstractions/IRequestContext.cs` | `FeatureFlagContext.From` (built in #345) |
| `EventIdRanges` | `src/Encina/Diagnostics/EventIdRanges.cs` (`OpenTelemetry` 7000-7099 is the last range below the block) | New range 7300-7339 |
| `ICacheProvider` | `src/Encina.Caching/Abstractions/ICacheProvider.cs` | Option B of Design Choice 6 only |
| Audit store pattern (ADO, Dapper, EF, MongoDB) | `src/Encina.*/Auditing/` | Store layout |
| `RedisFixture`, provider fixtures | `tests/Encina.TestInfrastructure/Fixtures/` | Integration tests |
| `IEncinaHealthCheck` | `src/Encina.Messaging/Health/IEncinaHealthCheck.cs` | Deferred health check |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| **`Encina.FeatureFlags`** | **7300-7319** | New; #604 uses 7300-7305, #300 7306-7309, #345 7310-7315, #451 7316-7319 |
| **`Encina.FeatureFlags.Unleash`** | **7320-7324** | New |
| **`Encina.FeatureFlags.Redis`** | **7325-7329** | New |
| **`Encina.FeatureFlags.OpenFeature`** | **7330-7334** | New (#451) |
| **`Encina.FeatureFlags.ConfigCat`** | **7335-7339** | New (#451, only if its Dilemma is decided for inclusion) |
| `Encina.ServiceDiscovery*` | 7340-7349 | Allocated by #607 from the same assigned block 7300-7349 |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Phase 1-4 core | ~30 | Contract, errors, evaluator, cache, store contract (4 pull requests) |
| Phase 5a-5c stores | ~26 | 6 ADO/Dapper stores + scripts + registrations, EF, MongoDB |
| Phase 6-7 providers | ~18 | Redis, Unleash, fixture |
| Phase 8 observability | ~4 | |
| Phase 9 tests | ~45 | Unit, guard, contract, property, 12 integration classes |
| Phase 10 docs | ~6 | README x3, fragment, guide, INVENTORY |
| **Total** | **~130** | In about 12 pull requests of at most 15 files and 1500 lines |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
You are implementing issue #604: IFeatureFlagProvider and its implementations (database, Redis, Unleash).

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library with Railway Oriented Programming; pre-1.0, no compatibility code.
- This issue owns the foundation of the feature-flag family (#300, #345, #451): package, contract, errors, EventIds.
- Provider category: Database (10) for the store; Redis and Unleash are separate packages.

IMPLEMENTATION OVERVIEW:
Phase 1: package, contract, errors, options, EventId ranges 7300-7339.
Phase 2: SHA256 bucketing and rule evaluator.
Phase 3: in-process cache decorator and change source.
Phase 4: IFeatureFlagStore, in-memory store, DatabaseFeatureFlagProvider.
Phase 5a-5c: ADO x3, Dapper x3, EF Core, MongoDB stores.
Phase 6-7: Redis and Unleash packages.
Phase 8: diagnostics and log messages. Phase 9: tests. Phase 10: documentation.

KEY PATTERNS:
- Either<EncinaError, T> everywhere; Left never swallowed; TimeProvider for time.
- Store names {Pattern}Store{Provider}; folder FeatureFlags/; satellite DI AddEncinaFeatureFlags{Provider} with TryAdd*.
- Options with secrets: [JsonIgnore] and ToString() override.
- Each phase is one pull request of at most 15 files and 1500 lines.

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
| 2 | OpenTelemetry | ✅ Phase 8 | `ActivitySource` and `Meter` `Encina.FeatureFlags`; evaluation counter and span |
| 3 | Structured Logging | ✅ Phase 8 | `[LoggerMessage]` in 7300-7305 (core) and the provider ranges |
| 4 | Health Checks | ⏭️ Deferred | Provider health check: issue file `feature-flag-provider-health-checks` in `artifacts/issues/`, to be opened by the orchestrator |
| 5 | Validation | ✅ Phases 1, 4 | Flag name and tenant validated at the store boundary; options validated on start |
| 6 | Resilience | ✅ Phases 6, 7 | Redis and Unleash calls go through Polly; failure returns `provider_failed` (Design Choice 3) |
| 7 | Distributed Locks | ❌ N/A | Evaluation is a read; flag writes are single-row upserts |
| 8 | Transactions | ❌ N/A | One row per operation; no multi-operation atomicity needed |
| 9 | Idempotency | ❌ N/A | Evaluation is deterministic; an upsert of the same flag is idempotent |
| 10 | Multi-Tenancy | ✅ Phases 3, 4 | `TenantId` column, tenant-over-global precedence, tenant in the cache key |
| 11 | Module Isolation | ❌ N/A | Flag names are chosen by the application and shared across modules; a module name can be a context attribute for rules that need it |
| 12 | Audit Trail | ⏭️ Deferred | Audit of flag changes through `IOperationAuditStore`: issue file `feature-flag-change-audit` in `artifacts/issues/` |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | Status | Notes |
|-------------|--------|-------|
| `EncinaErrors`, `IRequestContext`, `TimeProvider` abstractions | Done | In `src/Encina` |
| Provider fixtures for the 10 databases and Redis | Done | `tests/Encina.TestInfrastructure/Fixtures/` |

### Recommended (Not Blocking)

| Dependency | Issue | Notes |
|-----------|-------|-------|
| Pipeline gate and `Microsoft.FeatureManagement` provider | #300, #345 | Consumers of Phase 1 |
| OpenFeature and ConfigCat providers | #451 | Consumers of Phase 1 |
| SaaS providers (Azure App Configuration, LaunchDarkly, Split.io) | #2694 | Cloud milestone, outside 1.0 |
| Composite provider (several backends at once) | issue file `feature-flag-composite-provider` | The motivation mentions "use several at once"; not part of this plan |

---

## Next Steps

1. Review this plan; the Dilemmas (3 and 6) are asked when work on the issue starts.
2. Post the plan link on #604.
3. Phase 1 is the first pull request of the whole family; #300, #345 and #451 start after it merges.
4. Each phase is one pull request of at most 15 files and 1500 lines; the last one carries `Fixes #604`.
