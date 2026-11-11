# Implementation Plan: `Encina.DistributedLock` — Tenant-Scoped Distributed Locks

> **Issue**: [#741](https://github.com/dlrivada/Encina/issues/741)
> **Type**: Feature
> **Complexity**: Medium (one decorator, one options class, one registration, 1 pull request)
> **Estimated Scope**: ~350-450 lines of production code + ~700-900 lines of tests
> **Specification**: [SPEC-000](../specifications/SPEC-000-encina-1.0-baseline-and-release-scope.md) REQ-027, AC-027 (lock providers); [SPEC-002](../specifications/SPEC-002-eu-regulatory-readiness.md) REQ-061, REQ-062 (tenant awareness and instrumentation)
> **Decision**: [ADR-052](../architecture/adr/052-distributed-lock-failure-contract.md) (lock failure contract), [ADR-050](../architecture/adr/050-modules-may-require-modules.md) (registration errors)
> **Depends on**: [#2667](https://github.com/dlrivada/Encina/issues/2667) (implements ADR-052; open, not a pending maintainer decision)
> **Related**: [#226](https://github.com/dlrivada/Encina/issues/226) (RedLock, same family, [plan](redlock-implementation-plan-226.md)), [#717](https://github.com/dlrivada/Encina/issues/717) (leader election, global locks), [#763](https://github.com/dlrivada/Encina/issues/763) (DPIA reminder lock, global), [#2653](https://github.com/dlrivada/Encina/issues/2653)

---

## Summary

Lock keys are global today: `process-payments` is one lock for the whole deployment, so in a SaaS application every tenant's payment job waits for the others. This plan adds **one decorator over `IDistributedLockProvider`** that derives the lock key from the current tenant for the lock names that are configured as tenant-scoped, so all lock providers are covered without provider code:

1. **Scope decision.** Each requested lock name is either *tenant-scoped* or *global* according to `TenantLockScopingOptions` (a list of names; the default scope for names not listed is Dilemma 1).
2. **Key derivation.** A tenant-scoped name becomes `{escaped-tenant-id}:{resource}`; the provider's own `KeyPrefix` is still applied by the provider on top. The tenant id is escaped so an id containing the separator cannot collide with another tenant's key.
3. **Fail closed.** A tenant-scoped lock with no tenant (no request context, empty id) returns `Left` with a lock error code; it never falls back to a global key (`AGENTS.md` section 3).
4. **Opt-in.** `AddEncinaDistributedLockTenantScoping(...)` decorates the registered provider; nothing changes unless it is called.
5. **ADR-052 shapes.** The decorator forwards `TryAcquireAsync`, `AcquireAsync`, `IsLockedAsync` and `ExtendAsync` with the scoped resource and returns the inner `Either` unchanged, so contention (`Right(None)`) is never counted as a failure.

**Standards**: ADR-052; SPEC-002 REQ-061 (tenant id on every record and fail-closed tenant resolution) and REQ-062 (tenant as span attribute); `AGENTS.md` sections 3 and 6.

**Estimated scope**: 4 new production files, 3 modified, about 8 test files, 3 documentation files.

**Affected packages**: `Encina.DistributedLock` (decorator, options, registration, a new project reference to `Encina.Tenancy`); `Encina.Tenancy` is read only. No change in `Encina.DistributedLock.InMemory`, `.Redis` or `.SqlServer`.

**Provider category**: Distributed locks (`AGENTS.md` section 5; SPEC-000 DEC-009: InMemory, Redis, SqlServer, PostgreSQL, MySQL, Azure Blob, DynamoDB). The decorator is provider agnostic: it is written once and proved through each provider that exists; the four planned providers (#207-#210) are covered when they land.

**What the code does today** (`origin/main` at `7a0049c07`):

- `IDistributedLockProvider` (`src/Encina.DistributedLock/IDistributedLockProvider.cs`) has `TryAcquireAsync`, `AcquireAsync`, `IsLockedAsync` and `ExtendAsync` on a plain `string resource`; ADR-052 and #2667 change the return types to `Either`. `DistributedLockOptions` has `KeyPrefix`; each provider builds its key itself (for example `InMemoryDistributedLockProvider.GetLockKey`).
- The three existing providers register with `TryAddSingleton<IDistributedLockProvider, ...>` (`Encina.DistributedLock.InMemory/ServiceCollectionExtensions.cs:70`, `.SqlServer/ServiceCollectionExtensions.cs:88`, `.Redis/ServiceCollectionExtensions.cs`). `AddEncinaDistributedLock(...)` in the core package registers only `Configure(configure)` on `DistributedLockOptions` and is a documentation marker otherwise.
- The tenant abstraction is `ITenantProvider` (`src/Encina.Tenancy/Abstractions/ITenantProvider.cs`): `string? GetCurrentTenantId()`, null without a request context. `Encina.Tenancy` registers it as **scoped** (`DefaultTenantProvider`, `TryAddScoped<ITenantProvider, DefaultTenantProvider>`), reading the singleton `IRequestContextAccessor`. This matters: the lock providers are **singletons**, so a decorator cannot simply take `ITenantProvider` in its constructor (see Dilemma 2).
- `Encina.DistributedLock` references `Encina` and `Encina.Messaging`; `Encina.Tenancy` references the same two, so a project reference from `Encina.DistributedLock` to `Encina.Tenancy` creates no cycle.
- The package has no EventId range of its own. `3850-3899` is taken by `TenancyAspNetCore`; the block `6250-6299` is assigned to the `DistributedLock` assembly in the family registry (`src/Encina/Diagnostics/EventIdRanges.cs` has no entry yet).
- The manifest `.github/coverage-manifest/Encina.DistributedLock.json` has package targets guard 20 and unit 60; `Encina.DistributedLock` has `InternalsVisibleTo` for the unit, integration, contract and property test projects.
- The original issue's names (`ITenantContext`, lock keys in `Encina.Messaging`) are stale; this plan uses `ITenantProvider` and the real package layout.

---

## Design Choices

<details>
<summary><strong>1. Default scope of a lock name when tenant scoping is enabled</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Tenant-scoped by default, a named list of global locks** | Safe by default for SaaS (the original wording: auto-prefix when a tenant is available); a forgotten tenant lock cannot cause cross-tenant contention | A forgotten global lock multiplies across tenants (N holders instead of one); infrastructure code without a tenant (outbox processors, #717 election, #763 reminder) fails closed unless every infrastructure lock is listed as global |
| **B) Global by default, a named list of tenant-scoped locks** | Infrastructure locks keep working without a tenant and without listing; no surprise for existing code | A forgotten tenant lock keeps the cross-tenant contention this issue describes; "enabled" does little until locks are listed |

### Chosen Option: **B — global by default, explicit tenant-scoped list** (recommended, pending the maintainer)

### Rationale

- A wins when nearly every lock in the application protects tenant data (a pure SaaS back end); B wins when many locks are infrastructure locks, which is the case in this repository: the background processors (#717, #763, the outbox) are global and must not stop because they have no tenant.
- With B, the fail-closed rule still applies to every lock the application *declares* tenant-scoped.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Where the tenant id comes from — a singleton decorator over a scoped <code>ITenantProvider</code></strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Per-call scope:** the singleton decorator creates an `IServiceScope` through `IServiceScopeFactory` for each tenant-scoped call and resolves `ITenantProvider` from it | Honours any `ITenantProvider` implementation (custom resolvers); no captive dependency; passes `ValidateScopes` | One scope creation per tenant-scoped acquisition (microseconds, only for tenant-scoped names) |
| **B) Read `IRequestContextAccessor` directly** (the singleton `DefaultTenantProvider` itself reads) | Cheapest; singleton safe | Bypasses a custom `ITenantProvider`; duplicates the default provider's rule, which can drift |
| **C) Make the decorator scoped** | Takes `ITenantProvider` directly | A scoped `IDistributedLockProvider` cannot be injected into the singleton `BackgroundService`s that use locks (#717, #763) |

### Chosen Option: **A — per-call scope over `ITenantProvider`** (recommended, pending the maintainer)

### Rationale

- The issue named `ITenantProvider` as the source; taking it as a constructor dependency of a singleton would fail the DI proof the registration rule demands (`ValidateOnBuild`, `ValidateScopes`), so how to reach it is a real choice.
- B wins if profiling shows the per-call scope matters on a hot path, or if the maintainer wants the lock layer independent of the tenancy provider abstraction; the cost of choosing B later is one class.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Package placement — decorator inside <code>Encina.DistributedLock</code>, or a separate package</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Inside `Encina.DistributedLock`** with a project reference to `Encina.Tenancy` | One package for the lock feature; the issue's affected-package list; `Encina.Tenancy` adds no new transitive package (it references `Encina` and `Encina.Messaging`, which the lock package already references) | Every lock user ships the tenancy assembly even without tenancy |
| **B) New package `Encina.DistributedLock.Tenancy`** | Pay-for-what-you-use at assembly level; no dependency for other users | One more package, README, manifest and release item for about 4 files |

### Chosen Option: **A — inside `Encina.DistributedLock`** (recommended, pending the maintainer)

### Rationale

- B wins if keeping the base lock package free of any tenancy reference matters more than one extra package; the maintainer can pick it when work starts (the EventId range and file paths move to the new assembly, nothing else changes).
- The feature is opt-in either way: nothing runs unless the registration is called.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. One decorator over the interface, not code in each provider</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One decorator** over `IDistributedLockProvider` | Written and tested once; covers the three providers now, the four planned ones and the duplicate providers of #2653 without edits | The scoped resource is what the handle reports as `Resource` |
| **B) Tenant logic in each provider** | Handle can report the original name | Seven copies (and Caching duplicates) of the same rule; providers would reference tenancy |

### Chosen Option: **A — one decorator**

### Rationale

- Providers differ only in implementation and abstractions stay free of provider code.

**Class:** Settled
**Settled by:** `AGENTS.md` section 5 (provider coherence: providers share the same interfaces and differ only in implementation), SPEC-000 REQ-027

</details>

<details>
<summary><strong>5. A tenant-scoped lock without a tenant fails closed</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Left(EncinaError)` with a lock error code** | The gate denies when context is missing; visible to the caller; follows ADR-052 | A job that forgot the tenant stops instead of locking globally |
| **B) Fall back to the global key** | Never blocks | Silently re-creates the cross-tenant contention, or worse lets two tenants' jobs exclude each other |

### Chosen Option: **A — `Left`, never a fallback**

### Rationale

- The error code is added to `LockErrorCodes` (created by #2667) as `lock.tenant_required`; an error carries no tenant id or exception message.
- The only opt-out is explicit: do not list the name as tenant-scoped.

**Class:** Settled
**Settled by:** `AGENTS.md` section 3 (compliance and security gates fail closed; the only opt-out is explicit and logged), ADR-052 (`Left` for a fault), SPEC-002 REQ-061

</details>

<details>
<summary><strong>6. Key format, escaping and name matching</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `{escaped-tenant}:{resource}`; escape `%` and `:` in the tenant id (percent-encoding); exact, ordinal name match; an empty or whitespace tenant id counts as missing** | Injective (two tenants never share a key); same `:` separator as `KeyPrefix`; no pattern engine | Names cannot be matched by prefix or wildcard |
| **B) Hash the tenant id** | Fixed length, no escaping | Keys unreadable in Redis or SQL Server tooling |
| **C) Prefix or wildcard name rules** | Fewer list entries | A rule engine nobody asked for |

### Chosen Option: **A — escaped prefix, exact names**

### Rationale

- A property test shows injectivity for any pair of distinct tenant ids and resources, which is the safety property of the feature.

**Class:** Trivial
**Settled by:** `AGENTS.md` section 1 (every line serves a current purpose), the `KeyPrefix` convention of `src/Encina.DistributedLock/DistributedLockOptions.cs`

</details>

<details>
<summary><strong>7. Opt-in registration that decorates the registered provider</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `AddEncinaDistributedLockTenantScoping(Action<TenantLockScopingOptions>)` captures the existing `IDistributedLockProvider` descriptor and replaces it with the decorator; with no provider registered it throws `InvalidOperationException` naming the missing provider** | Opt-in; works with every provider and with the #2653 duplicates; the failure is at registration, not at first lock | Must be called after the provider registration |
| **B) Order independent through a keyed inner registration** | Any order | Providers register unkeyed with `TryAdd`; keying them is a change to all of them |

### Chosen Option: **A — decorate at registration, registration error when no provider exists**

### Rationale

- The decorator also needs `ITenantProvider`: the registration validates that `ITenantProvider` is registered (a registration error naming `AddEncinaTenancy`), proven by a DI test with `ValidateOnBuild` and `ValidateScopes`.

**Class:** Settled
**Settled by:** `AGENTS.md` section 1 (every feature opt-in), `AGENTS.md` section 3 (registration completeness), ADR-050 (rule 3: registration error, never a silent default)

</details>

<details>
<summary><strong>8. Observability — tenant as a span attribute, never a metric tag</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Counter `encina.distributed_lock.scoped_acquisitions` with tags `scope` (tenant, global) and `outcome` (acquired, contended, denied, failed); span `encina.distributed_lock.scope_resolution` with `lock.scope` and `encina.tenant_id`** | Metric cardinality stays bounded; the tenant is traceable | Per-tenant counts need trace queries |
| **B) Tenant id as a metric tag** | Per-tenant dashboards | Unbounded cardinality in a SaaS |

### Chosen Option: **A — tenant on spans only**

### Rationale

- Metric cardinality is bounded by two small enums; the tenant stays traceable through the span attribute.

**Class:** Settled
**Settled by:** SPEC-002 REQ-062 (tenant as an activity attribute, no direct identifiers of a data subject), `AGENTS.md` section 6 (function 2, OpenTelemetry)

</details>

---

## Implementation Phases

One pull request (about 14 files, within 15 files and 1500 lines). If Dilemma 3 picks a separate package, the same content becomes a second PR for the package scaffold.

### Phase 1: Options, Key Derivation and the Decorator

> **Goal**: The scoping rule and the decorator, written against the ADR-052 contract.

<details>
<summary><strong>Tasks</strong></summary>

#### `src/Encina.DistributedLock/` (after #2667)

1. **`Encina.DistributedLock.csproj`** (modify) — project reference to `..\Encina.Tenancy\Encina.Tenancy.csproj`
2. **`TenantLockScopingOptions.cs`** (new, public sealed) — `ISet<string> TenantScopedLocks` and `ISet<string> GlobalLocks` (ordinal), `TenantLockScope DefaultScope` (enum `Global`, `Tenant`; default per Dilemma 1); `ToString()` not needed (no secrets)
3. **`TenantLockKey.cs`** (new, internal static) — `string Compose(string tenantId, string resource)` (percent-escape `%` and `:`)
4. **`TenantScopedLockProvider.cs`** (new, internal sealed) — `IDistributedLockProvider`; constructor `(IDistributedLockProvider inner, IServiceScopeFactory scopeFactory, IOptions<TenantLockScopingOptions> options, ILogger<TenantScopedLockProvider> logger)`; each method: decide scope; global forwards unchanged; tenant-scoped resolves the tenant (per Dilemma 2), returns `Left` (`lock.tenant_required`) when null or whitespace, otherwise forwards `Compose(tenantId, resource)` and returns the inner `Either` as is; cancellation propagates
5. **`LockErrorCodes`** (modify, created by #2667) — add `TenantRequired = "lock.tenant_required"`
6. **`PublicAPI.Unshipped.txt`** — options and enum (the decorator is internal)

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of issue #741: the tenant-scoping decorator for distributed locks.

CONTEXT:
- Issue #2667 has already changed IDistributedLockProvider to the ADR-052 shapes (Either, Option for contention)
  and created LockErrorCodes; read their current signatures before editing.
- ITenantProvider (Encina.Tenancy) is scoped; lock providers are singletons, so the decorator must not take
  ITenantProvider in its constructor (design choice 2).
- Contention is Right(None), not a failure; the decorator returns the inner Either unchanged.

TASK:
Create TenantLockScopingOptions, TenantLockKey and TenantScopedLockProvider as described in the Phase 1 tasks and add
the project reference to Encina.Tenancy. Add the unit tests: scoped name with a tenant, global name, scoped name
without a tenant (Left, no inner call), tenant id containing ':' and '%', IsLockedAsync and ExtendAsync scoping,
cancellation, inner Left and Right(None) passed through.

KEY RULES:
- Fail closed: no tenant, no lock; never a global fallback.
- Never log or tag the tenant id as a metric label; EncinaError carries no tenant id or exception message.
- ArgumentNullException guards on every public parameter; XML documentation on public members.

REFERENCE FILES:
- src/Encina.DistributedLock/IDistributedLockProvider.cs
- src/Encina.DistributedLock/DistributedLockOptions.cs
- src/Encina.Tenancy/Abstractions/ITenantProvider.cs
- docs/architecture/adr/052-distributed-lock-failure-contract.md
```

</details>

### Phase 2: Configuration, DI and Registration Completeness

> **Goal**: Opt-in registration that decorates whatever provider is registered.

<details>
<summary><strong>Tasks</strong></summary>

1. **`ServiceCollectionExtensions.cs`** (modify, `src/Encina.DistributedLock/`) — `AddEncinaDistributedLockTenantScoping(this IServiceCollection, Action<TenantLockScopingOptions>? configure = null)`: throws `InvalidOperationException` when no `IDistributedLockProvider` descriptor exists or `ITenantProvider` is not registered (message names the missing `AddEncinaDistributedLock*` or `AddEncinaTenancy` call and ADR-050); captures the provider descriptor, removes it and registers the decorator with the same lifetime, building the inner instance from the captured descriptor (implementation type, factory or instance); registers `TenantLockScopingOptions` through `Configure`
2. **`TenantLockScopingOptionsValidator.cs`** (new, internal) — `IValidateOptions`: a name is not in both lists; no empty names
3. **Docs of `AddEncinaDistributedLock`** (modify) — remove the claim that the method registers core services only if untrue after this change; document the order requirement
4. **DI test** — builds the provider with `ValidateOnBuild` and `ValidateScopes` for InMemory, then for a fake second provider registered through a factory

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of issue #741: registration of the tenant-scoping decorator.

CONTEXT:
- Providers register with TryAddSingleton<IDistributedLockProvider, ...>; the decorator replaces the captured
  descriptor and keeps its lifetime. ITenantProvider is scoped (Encina.Tenancy).
- AGENTS.md section 3 (registration completeness): every option type and dependency the decorator resolves must be
  registered, proven by a DI test with ValidateOnBuild and ValidateScopes.

TASK:
Add AddEncinaDistributedLockTenantScoping, the options validator and the DI tests (with and without a provider,
with and without tenancy, calling twice, instance/factory/type descriptors).

KEY RULES:
- A registration error (InvalidOperationException) when a dependency is missing; never a silent default.
- Do not override an application's own registrations other than the captured provider descriptor.

REFERENCE FILES:
- src/Encina.DistributedLock/ServiceCollectionExtensions.cs
- src/Encina.DistributedLock.InMemory/ServiceCollectionExtensions.cs
- src/Encina.Tenancy/ServiceCollectionExtensions.cs
- tests/Encina.UnitTests/DistributedLock/
```

</details>

### Phase 3: Cross-Cutting Integration

> **Goal**: Each function of the matrix is integrated or recorded.

<details>
<summary><strong>Tasks</strong></summary>

1. **Validation (5)** — options validator and registration errors (Phase 2)
2. **Multi-tenancy (10)** — this is the feature; with tenancy off the decorator is not registered, so no fixed default tenant is needed
3. **Module isolation (11)** — not applicable: modules are separated with `KeyPrefix`, and a module-scoped lock name is the same mechanism with a list entry
4. **Redis quorum (#226)** — no change: the decorator hands the Redis provider a plain resource string

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are implementing Phase 3 of issue #741: cross-cutting verification.

CONTEXT:
- The matrix of docs/plans/tenant-scoped-lock-implementation-plan-741.md marks validation and multi-tenancy as
  included; everything else is not applicable or covered in other phases.

TASK:
Verify the validator and the fail-closed path are covered by tests, and add the two-tenant test: tenant A holds
"payments"; tenant B acquires "payments" (Right(Some)); an unscoped name stays exclusive across tenants.

KEY RULES:
- Two tenants never read, act on or contend for each other's lock.
- No tenant configuration is required when tenancy is off.

REFERENCE FILES:
- docs/plans/tenant-scoped-lock-implementation-plan-741.md
- tests/Encina.UnitTests/DistributedLock/
```

</details>

### Phase 4: Observability — Diagnostics, Metrics and Logging

> **Goal**: The scope decision is visible without exposing tenant ids as labels.

<details>
<summary><strong>Tasks</strong></summary>

1. **Range registration** — add `DistributedLock = (6250, 6299)` to `src/Encina/Diagnostics/EventIdRanges.cs` (3850-3899 is taken; 6250-6299 is the block assigned to this assembly); map `Encina.DistributedLock` to it in `EncinaEventIdAllocationTests`; this plan uses 6250-6269, the election of #717 uses 6270-6299 in the same assembly
2. **`Diagnostics/TenantLockLogMessages.cs`** (new, internal static partial) — `[LoggerMessage]` 6250 scope resolved (Debug), 6251 tenant required, lock denied (Warning), 6252 scoping enabled at startup (Information)
3. **`Diagnostics/TenantLockDiagnostics.cs`** (new, internal static) — `ActivitySource` `Encina.DistributedLock` (version `1.0`), `Meter`, counter `encina.distributed_lock.scoped_acquisitions` (tags `scope`, `outcome`), span `encina.distributed_lock.scope_resolution` with `lock.scope` and `encina.tenant_id`; reuse the shared instruments of #724 if it has merged

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
You are implementing Phase 4 of issue #741: logging and telemetry of the scope decision.

CONTEXT:
- AGENTS.md section 7: register the EventId range first in src/Encina/Diagnostics/EventIdRanges.cs, then add the
  assembly to the AssemblyRanges map of tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs.
- This plan owns 6250-6269 of the DistributedLock range; #717 owns 6270-6299.

TASK:
Register the range, write the log messages and diagnostics as listed, wire them into the decorator, run the EventId
architecture tests.

KEY RULES:
- EventIds packed from 6250, no gaps; XML docs name the range.
- The tenant id is a span attribute only; never a metric tag or a log argument.
- Contention (Right(None)) counts as outcome=contended, not as a failure.

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
- src/Encina.Tenancy/Diagnostics/TenancyActivitySource.cs
```

</details>

### Phase 5: Testing

> **Goal**: Every flag reaches its own target.

<details>
<summary><strong>Tasks</strong></summary>

1. **Unit** (`tests/Encina.UnitTests/DistributedLock/`) — decorator, key composition, options validator, registration (Phases 1 to 4)
2. **Guard** (`tests/Encina.GuardTests/Infrastructure/DistributedLock/`) — options, registration extension, decorator constructor
3. **Contract** (`tests/Encina.ContractTests/`) — run the lock provider contract suite of #2667 through the decorator with a tenant-scoped name, for InMemory, Redis and SqlServer (SPEC-000 AC-027)
4. **Property** (`tests/Encina.PropertyTests/`) — FsCheck: for distinct (tenant, resource) pairs the composed keys are distinct, and `Compose` never yields a key equal to another pair's, for tenant ids containing `:` and `%`
5. **Integration** (`tests/Encina.IntegrationTests/Infrastructure/DistributedLock/`) — Redis and SqlServer through the existing Testcontainers fixtures: two tenants hold the same name at once, the same tenant is excluded
6. **Load and benchmark** — justification files (the decorator is one string operation and one scope creation); a benchmark only if the maintainer picks Dilemma 2 option A and wants the per-call scope cost measured
7. **Coverage manifest** — per-file targets with one-sentence justifications in `Encina.DistributedLock.json` for the new files (written by `mechanical-fixer`)

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
You are implementing Phase 5 of issue #741: tests of tenant-scoped locks.

CONTEXT:
- Tests execute real package code (instantiate the decorator over the InMemory provider); assertions use Shouldly
  through Encina.Testing.Shouldly; time from FakeTimeProvider.
- Integration tests use the shared [Collection] fixtures, never IClassFixture.
- Coverage is per flag against .github/coverage-manifest/Encina.DistributedLock.json.

TASK:
Write the test files listed in the Phase 5 tasks, measure each flag with
`dotnet test tests\Encina.<Flag>Tests --collect "XPlat Code Coverage"` and report the per-file table.

KEY RULES:
- No Thread.Sleep; builders for test data; no swallowed exceptions.
- The contract tests must call the real decorator, not a mock of it.

REFERENCE FILES:
- tests/Encina.UnitTests/DistributedLock/
- tests/Encina.IntegrationTests/Infrastructure/DistributedLock/
- .github/coverage-manifest/Encina.DistributedLock.json
```

</details>

### Phase 6: Documentation and Finalization

> **Goal**: Users can configure tenant scoping and know its limits.

<details>
<summary><strong>Tasks</strong></summary>

1. XML documentation on every new public member (`<example>` on the registration extension)
2. `src/Encina.DistributedLock/README.md` and the lock guide page through `docs-writer`: configuration, default scope, fail-closed behavior, the registration order
3. `changelog.d/741-tenant-scoped-locks.added.md` (written by `mechanical-fixer`)
4. `PublicAPI.Unshipped.txt`; `docs/INVENTORY.md` if the package list changes
5. ADR only if the maintainer's answers to Dilemmas 1 to 3 warrant one (decision 3 changes the package layout)
6. `dotnet build --configuration Release` with 0 warnings; `dotnet test`; every flag at its target; CRAP <= 10 on changed methods

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
You are finishing issue #741: documentation and verification.

CONTEXT:
- Documentation goes through the docs-writer agent and the encina-docs skill; the changelog fragment and PublicAPI
  lines go through mechanical-fixer.

TASK:
Complete the Phase 6 tasks and report the per-flag figures of the touched files against their targets.

KEY RULES:
- English only; no private client application named; state the default scope and that a tenant-scoped lock without a
  tenant fails.
- Zero warnings; no [Obsolete]; no compatibility layer.

REFERENCE FILES:
- .claude/skills/encina-docs/SKILL.md
- changelog.d/README.md
- src/Encina.DistributedLock/README.md
```

</details>

---

## Research

### Relevant Standards and Specifications

| Source | Provision | Relevance |
|--------|-----------|-----------|
| ADR-052 | Lock providers return `Either`; contention is `Right(None)` | The decorator forwards the inner result and adds one `Left` |
| ADR-050 | Registration error, never a silent default | Missing provider or tenancy fails at registration |
| SPEC-002 REQ-061, AC-043 | Tenant-aware; missing tenant configuration fails closed | Fail-closed lock without a tenant |
| SPEC-002 REQ-062, AC-044 | Tenant as span attribute; no direct identifiers in tags | Observability choice |
| SPEC-000 REQ-027, DEC-009, AC-027 | Seven lock providers with contract tests | Contract tests through the decorator |
| `AGENTS.md` sections 3, 5, 6, 7 | Fail closed, registration completeness, provider coherence, EventIds | Constraints used throughout |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|----------------------|
| `IDistributedLockProvider`, `ILockHandle` | `src/Encina.DistributedLock/` | The decorated contract |
| `DistributedLockOptions` | `src/Encina.DistributedLock/DistributedLockOptions.cs` | `KeyPrefix` convention; the provider still applies it |
| `AddEncinaDistributedLock` | `src/Encina.DistributedLock/ServiceCollectionExtensions.cs` | Home of the new registration extension |
| `ITenantProvider`, `DefaultTenantProvider` | `src/Encina.Tenancy/Abstractions/ITenantProvider.cs`, `Core/DefaultTenantProvider.cs` | Tenant source; scoped lifetime drives Dilemma 2 |
| `AddEncinaTenancy` registrations | `src/Encina.Tenancy/ServiceCollectionExtensions.cs` | Required dependency, validated at registration |
| `TenancyActivitySource` | `src/Encina.Tenancy/Diagnostics/TenancyActivitySource.cs` | Convention for `ActivitySource` naming and the `encina.tenant_id` attribute |
| Provider registrations | `src/Encina.DistributedLock.InMemory`, `.Redis`, `.SqlServer` `ServiceCollectionExtensions.cs` | `TryAddSingleton<IDistributedLockProvider, ...>` descriptors the decorator captures |
| Lock tests and fixtures | `tests/Encina.UnitTests/DistributedLock/`, `tests/Encina.IntegrationTests/Infrastructure/DistributedLock/` | Extended; no lock contract suite exists on `main` until #2667 |

### Provider Matrix (distributed locks, SPEC-000 DEC-009)

| Provider | Tenant scoping | Notes |
|----------|:-:|-------|
| InMemory | Included | Through the decorator; unit and contract tests |
| Redis | Included | Through the decorator; contract and Testcontainers tests |
| SqlServer | Included | Through the decorator; contract and Testcontainers tests |
| PostgreSQL (#207), MySQL (#208), Azure Blob (#209), DynamoDB (#210) | Deferred | Covered by the decorator when each provider lands, with a contract test entry; tracked by those issues |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.DistributedLock` (new `DistributedLock` entry) | 6250-6299 | Registered by this plan; #741 uses 6250-6269 (6250-6252 allocated), the election of #717 uses 6270-6299 |
| `Encina.DistributedLock.Redis` | 3750-3799 | Not used by this plan (the Redis assembly keeps its range; see the #226 plan) |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Production (new) | 5 | `TenantLockScopingOptions.cs`, `TenantLockKey.cs`, `TenantScopedLockProvider.cs`, `TenantLockScopingOptionsValidator.cs`, `Diagnostics/*` (2) |
| Production (modified) | 4 | `Encina.DistributedLock.csproj`, `ServiceCollectionExtensions.cs`, `EventIdRanges.cs`, `LockErrorCodes` (from #2667) |
| PublicAPI and manifest | 2 | `PublicAPI.Unshipped.txt`, `Encina.DistributedLock.json` |
| Unit and guard tests | 4 | Includes the DI and EventId map change |
| Contract, property, integration tests | 3 | |
| Documentation and changelog | 3 | README, guide page, fragment |
| **Total** | **~21** | Production and docs fit one PR only if tests are trimmed; split tests into a second PR if the 15-file limit binds |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
You are implementing issue #741: tenant-scoped distributed locks as one decorator over IDistributedLockProvider.

PROJECT CONTEXT:
- .NET 10 / C# 14, nullable enabled, pre-1.0: best solution first, no compatibility layers, no [Obsolete].
- Lock contract: ADR-052, implemented by #2667 before this work (Either, Option for contention, LockErrorCodes).
- ITenantProvider is scoped; lock providers are singletons; the decorator reaches the tenant through a per-call scope
  (or the option the maintainer picks for Dilemma 2).

IMPLEMENTATION OVERVIEW:
1. TenantLockScopingOptions (tenant-scoped and global name sets, default scope per Dilemma 1), TenantLockKey
   (percent-escape '%' and ':'), TenantScopedLockProvider (internal sealed decorator), lock.tenant_required code.
2. AddEncinaDistributedLockTenantScoping: capture and replace the provider descriptor; registration errors when no
   provider or no ITenantProvider; options validator; DI test with ValidateOnBuild and ValidateScopes.
3. Observability: register DistributedLock = 6250-6299 in EventIdRanges.cs, map the assembly in the EventId test,
   log messages 6250-6252, counter with tags scope and outcome, span with encina.tenant_id.
4. Tests: unit, guard, contract (through the decorator), property (key injectivity), integration (Redis, SqlServer),
   justification files for load and benchmark; docs through docs-writer; changelog, PublicAPI and manifest through
   mechanical-fixer.

KEY PATTERNS:
- Fail closed on a missing tenant; never a global fallback.
- Tenant id as a span attribute only.
- Methods small enough for CRAP <= 10.

REFERENCE FILES:
- docs/plans/tenant-scoped-lock-implementation-plan-741.md
- docs/architecture/adr/052-distributed-lock-failure-contract.md
- src/Encina.DistributedLock/ServiceCollectionExtensions.cs
- src/Encina.Tenancy/Abstractions/ITenantProvider.cs
- .github/coverage-manifest/Encina.DistributedLock.json
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Lock state is ephemeral coordination; nothing to cache |
| 2 | OpenTelemetry | ✅ | Phase 4: counter with `scope` and `outcome` tags, span with `encina.tenant_id`; moves onto the shared instruments of #724 if it lands first |
| 3 | Structured Logging | ✅ | Phase 4: `[LoggerMessage]` 6250-6252 in the new `DistributedLock` range registered first |
| 4 | Health Checks | ❌ | No new dependency; the lock providers own their health checks (#755) |
| 5 | Validation | ✅ | Phase 2: options validator and registration errors for a missing provider or tenancy |
| 6 | Resilience | ❌ | The inner provider owns resilience (the RedLock plan #226 for Redis) |
| 7 | Distributed Locks | ❌ | This decorates the lock feature itself |
| 8 | Transactions | ❌ | No atomic multi-operation need |
| 9 | Idempotency | ❌ | Acquisition receives no duplicate deliveries |
| 10 | Multi-Tenancy | ✅ | This is the feature (Phases 1 and 3) |
| 11 | Module Isolation | ❌ | `KeyPrefix` and a list entry already separate modules; no module data is held |
| 12 | Audit Trail | ❌ | A lock key derivation is not a data access or state change; logs and traces cover operations |

---

## Prerequisites & Dependencies

| Item | State | Effect on this plan |
|------|-------|---------------------|
| [#2667](https://github.com/dlrivada/Encina/issues/2667) — lock providers return `Either` (ADR-052) | Open, implementation issue | Phases 1 and 5 start after it merges; `LockErrorCodes` and the contract suite come from it |
| [#2653](https://github.com/dlrivada/Encina/issues/2653) — duplicate lock providers in the caching packages | Open | The decorator wraps whichever `IDistributedLockProvider` is registered; no coupling |
| [#717](https://github.com/dlrivada/Encina/issues/717) and [#763](https://github.com/dlrivada/Encina/issues/763) | Open (same family) | Global lock users; share the `DistributedLock` range (6270-6299 for #717) |
| [#226](https://github.com/dlrivada/Encina/issues/226) | Open (same family) | Redis quorum; unaffected, the decorator passes a plain resource |
| [#207](https://github.com/dlrivada/Encina/issues/207)-[#210](https://github.com/dlrivada/Encina/issues/210) | Open | Covered when each provider lands |

---

## Next Steps

1. Link this plan from issue #741 and start the brief after [#2667](https://github.com/dlrivada/Encina/issues/2667) merges.
2. When work on the issue starts, ask the maintainer to answer Dilemmas 1 to 3 (default scope, tenant source, package placement), record the answers in a `## Maintainer Decisions` section placed last, and have the adversarial-reviewer verify the Settled and Trivial choices.
3. One commit per phase; the last commit references `Fixes #741`.
