# Implementation Plan: Per-tenant replica sets for replication lag detection

> **Issue**: [#3991](https://github.com/dlrivada/Encina/issues/3991) (`area-database`, `area-multi-tenancy`, `p1-recommended`, `needs-decision`)
> **Type**: Feature
> **Complexity**: Medium-High (a tenant-aware routing seam in `Encina.Messaging`, a sweep over the seven provider registrations, an extension of the lag probe service)
> **Estimated Scope**: ~800-1,000 lines of production code + ~1,700-2,300 lines of tests (about 55 files and up to about 4,400 line-changes in total with manifests, PublicAPI and documentation), delivered in seven pull requests of at most 15 files and 1,500 lines each
> **Related plans of the same family**: builds on the plan of #633 (replication lag detection: `IReplicationLagProbe`, `ReplicationLagProbeService`, `LagAwareReplicaSelector`, the tracker scope `readwrite`), which owns every type this plan extends. #3989 and #3995 are unrelated plans written in the same run. Sibling plans are referred to by issue number only.

---

## Summary

#633 tracks one replica set under the tracker scope id `readwrite`. A multi-tenant deployment can give each tenant its own primary and replicas; those replicas lag independently, so a read of tenant A must be judged by the lag of tenant A's replicas and must never be served from another tenant's database. This plan resolves the replica set, the lag reading and the primary fallback from the tenant of the request.

### What the code does today (2026-11-24, `main` at `09c85f62b`)

1. **#633 is not implemented.** `LagAwareReplicaSelector` and the monitor do not exist in `src/Encina.Messaging/ReadWriteSeparation`; the plan of #633 is on `main`. The types named here are the ones that plan specifies, and every pull request below lands after the pull request of #633 that creates the type it edits.
2. **Replica configuration is global.** `ReadWriteSeparationOptions` has one `WriteConnectionString`, one `ReadConnectionStrings` list and one `ReplicaStrategy` (`src/Encina.Messaging/ReadWriteSeparation/ReadWriteSeparationOptions.cs`); `ReadWriteConnectionSelector.GetReadConnectionString()` is synchronous and parameterless (`ReadWriteConnectionSelector.cs:134-139`).
3. **The selector is a singleton built in seven places.** Each of `Encina.ADO.SqlServer|PostgreSQL|MySQL`, `Encina.Dapper.SqlServer|PostgreSQL|MySQL` and `Encina.EntityFrameworkCore` registers `IReadWriteConnectionSelector` with `TryAddSingleton` and two `new ReadWriteConnectionSelector(options, selector)` calls (for example `Encina.ADO.SqlServer/ServiceCollectionExtensions.cs:193,201`); the connection factories consume only the interface. `Encina.EntityFrameworkCore` serves all three EF Core databases.
4. **The tracker compares scope ids case-insensitively.** `ReplicaHealthTracker` keeps `_shardStates` in a dictionary with `StringComparer.OrdinalIgnoreCase` (`src/Encina/Sharding/ReplicaSelection/ReplicaHealthTracker.cs:22`), so the tenant ids `Acme` and `acme` would share one scope if the id were used as is.
5. **The tenant of the request is ambient.** `IRequestContext.TenantId` is `string?` (`src/Encina/Abstractions/IRequestContext.cs:125`), read through `IRequestContextAccessor` (a singleton registered by `TryAddSingleton` in `RequestIdentityServiceCollectionExtensions.cs:49`); `MultiTenancyMarker` (core) signals that multi-tenancy is on. `ITenantProvider` is scoped (`Encina.Tenancy/ServiceCollectionExtensions.cs:113`) and `Encina.Tenancy` references `Encina.Messaging`, so `Encina.Messaging` cannot reference it.
6. **Tenant records hold one connection string.** `TenantInfo.ConnectionString` and `ITenantConnectionFactory` (`src/Encina.Tenancy`) give a tenant a single database string, with no replica list.
7. **MongoDB tenants share a cluster.** `TenantAwareMongoCollectionFactory` selects a database inside one client (`src/Encina.MongoDB/Tenancy`), and `ReadWriteMongoCollectionFactory` builds one read preference from `MongoReadWriteSeparationOptions.MaxStaleness` for that client; the driver tracks staleness per cluster, so there is no per-tenant replica set to scope.

### Scope

- **Implements**: per-tenant replica sets (a primary and replicas per tenant) for the nine relational providers: a tenant replica set source, an injective per-tenant tracker scope, a tenant router used by `ReadWriteConnectionSelector`, the extension of the #633 probe service to probe every tenant's replicas, and tenant tags on the lag telemetry.
- **Affected packages**: `Encina.Messaging` (contracts, router, selector, probe service, registration, diagnostics) and the seven provider packages above (one identical registration edit each). `Encina` (tracker) is reused unchanged. `Encina.MongoDB` is not changed (fact 7).
- **Provider category**: Database, all 10 providers (AGENTS.md section 5): nine through the selector; MongoDB recorded as not applicable with the reason (provider matrix in Phase 5).
- **Corrections to the issue text**: "the primary" of an unknown tenant is not the global primary under the recommended Design Choice 4 (the global connection may hold other tenants' data); the replica list and the tracker scope id come from the tenant of the request through one router, not from a probe service per tenant.
- **Split into pull requests** (counts include tests, manifest edits and PublicAPI lines; the numbering is the merge order; every pull request builds alone and changes no behavior until an application calls `AddEncinaTenantReplicaSets`, which PR 2 adds together with the services it resolves):

  | PR | Content (phase) | Files | Lines (est.) | Depends on |
  |----|-----------------|-------|--------------|------------|
  | 1 | `ITenantReplicaSetSource`, `TenantReplicaSet`, `OptionsTenantReplicaSetSource`, `TenantReplicaScopeId`, `TenantReplicaSetsValidator`, error codes, `ReadWriteSeparationOptions.Tenants`, PublicAPI, manifest, unit, property and guard tests (Phase 1) | 14 | ~750 | #633 PR 1 |
  | 2 | `ITenantReplicaRouter`, `TenantReplicaRouter`, `ReadWriteConnectionSelector` optional router parameter, `AddEncinaTenantReplicaSets` (registers the source, the router and what they resolve), PublicAPI, manifest, unit, property, guard and DI proof tests (Phase 2) | 11 | ~950 | 1, #633 PR 2 |
  | 3 | **Sweep**: the identical registration edit in seven provider packages (pass `sp.GetService<ITenantReplicaRouter>()` to both `new ReadWriteConnectionSelector(...)` calls), one DI test that builds each provider (Phase 3) | 8 | ~250 | 2 |
  | 4 | Probe service extension, `ReplicationLagLog` ids 7110-7113, `MaxConcurrentProbes`, PublicAPI, manifest, unit and guard tests (Phase 4) | 7 | ~650 | 2, #633 PR 3 |
  | 5 | Tenant tag and span attribute on the #633 metrics and activity source, health check per tenant scope, unit tests, manifest (Phase 5) | 6 | ~400 | 4, #633 PR 7 |
  | 6 | Two-tenant integration tests on PostgreSQL and MySQL replicas and the SQL Server unknown path, benchmark case, load justification edit, manifest (Phase 6) | 6 | ~1,000 | 4, 5, #633 PR 8 |
  | 7 | Changelog fragment, `Encina.Messaging` README, read/write separation page (Phase 7) | 3 | ~250 | 6 |

  Total: 7 PRs, 55 files, about 4,550 line-changes (sum of the per-PR upper estimates); no PR exceeds 15 files or 1,500 lines. PR 3 is a declared sweep (AGENTS.md section 11: the same mechanical change in every listed file, no new logic): `src/Encina.ADO.SqlServer`, `Encina.ADO.PostgreSQL`, `Encina.ADO.MySQL`, `Encina.Dapper.SqlServer`, `Encina.Dapper.PostgreSQL`, `Encina.Dapper.MySQL` and `Encina.EntityFrameworkCore`, each `ServiceCollectionExtensions.cs`. If the maintainer chooses the decorator of Design Choice 5, PR 3 disappears and PR 2 and PR 4 change.

---

## Design Choices

<details>
<summary><strong>1. How lag is tracked per tenant</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One tracker scope per tenant** in the existing `IReplicaHealthTracker`; the #633 probe service probes the replicas of every tenant (distinct connection strings once per cycle, bounded concurrency) and reports each reading to the scope of each tenant that uses it | Reuses the tracker, the probes and the sharded health check; one background loop; tenants that share a replica are probed once | Many scopes to hold and probe; one slow tenant database can delay a cycle (bounded by `ProbeTimeout` and the concurrency limit) |
| **B) One probe service per tenant** | Isolation of loop, interval and failure domain per tenant | N hosted services and N timers; start and stop with the tenant set; registration grows with tenants |
| **C) Defer to the tenancy work** | No code now | Tenant-blind lag stays: a lagging tenant database is read through another tenant's reading, or not judged at all |

### Chosen Option: **A — a tracker scope per tenant, one probe service** (recommended, pending the maintainer)

### Rationale

B wins when tenants need different intervals or must not share a failure domain (a regulated tenant on its own infrastructure); A wins by default because the probe is a short read-only query and a bounded-concurrency loop gives the isolation that matters (a timeout per probe). C wins only if the maintainer wants the tenant primitive of #1257 designed first. The scope id of a tenant is `readwrite:t:` followed by the lowercase hexadecimal of the tenant id's UTF-8 bytes: injective (distinct tenant ids give distinct hex), safe under the tracker's case-insensitive comparison (fact 4), and distinct from the global scope `readwrite`; a shard literally named like that scope is documented as unsupported, as in #633.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Where a tenant's replica set is configured</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A static `ReadWriteSeparationOptions.Tenants` dictionary of tenant id to `TenantReplicaSet` (write string, read strings)** | Simple; validated at startup; no new abstraction | The tenant set changes only with configuration and a restart; secrets sit in options |
| **B) A small `ITenantReplicaSetSource` (synchronous lookup and enumeration) with an options-backed default; an application can register a source over its own tenant registry** | Same simplicity by default; a dynamic registry plugs in without a package change; keeps the `Encina.Messaging` to `Encina.Tenancy` dependency pointing the allowed way | One more public interface; a dynamic source must be fast because the selector is synchronous |
| **C) Add replica strings to `TenantInfo` and read them from `ITenantStore`** | One tenant registry | `ITenantStore` is asynchronous and `Encina.Messaging` cannot reference `Encina.Tenancy` (fact 5): a cycle or a cache and an adapter project |

### Chosen Option: **B — `ITenantReplicaSetSource` with an options-backed default** (recommended, pending the maintainer)

### Rationale

A wins when the tenant set is fixed and the maintainer wants no new interface (it is B without the seam); C wins if a single registry for all tenant data matters more than the dependency direction. B costs one interface and keeps A's configuration as the default. Secret-bearing members (`TenantReplicaSet` connection strings) are `[JsonIgnore]` with a `ToString()` override that prints no string (AGENTS.md section 3, #851). A tenant removed from a dynamic source stops being probed; its old tracker scope holds a few dictionary entries until restart, and readings older than the reading-age limit of #633 are ineligible, so correctness does not depend on removal.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. How the selector learns the tenant of the request</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The router reads `IRequestContextAccessor.RequestContext?.TenantId` and the `MultiTenancyMarker` registration** | No change to the eight routing pipeline behaviors; works in the singleton selector; the same signals #1257 plans to use | A second reader of the ambient tenant next to the shared resolver of #1257; no ambient context in a background job means the tenant is missing there |
| **B) The routing behaviors put the tenant in `DatabaseRoutingScope`** | The tenant is captured once, where the intent is | The eight identical `ReadWriteRoutingPipelineBehavior` files change, and MongoDB's too; far beyond one pull request with their tests |
| **C) `ITenantScopeResolver.ResolveRead` of #1257 (fail-closed, shared)** | One definition of "tenant required" for every capability | The resolver does not exist yet; this plan would wait for #1257 |

### Chosen Option: **A — ambient accessor in the router** (recommended, pending the maintainer)

### Rationale

C wins once #1257 has landed and the maintainer wants a single fail-closed rule; the change from A to C is one line inside the router (the router is the only reader). B wins if the routing scope must carry the tenant across threads that lose the ambient context. A is recommended because it is buildable today and touches no behavior file. Tenant presence follows the activation rule of #1257: tenant routing is active when tenant replica sets are configured; a request is tenant-scoped when the ambient tenant is non-blank.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. A request with no tenant, or a tenant with no replica set</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Read from the global primary (`WriteConnectionString`), as the issue text says** | The request is served; no new failure | When tenant data lives in per-tenant databases, the global connection is a different tenant's or shared database: the request reads data of no tenant or of all |
| **B) Fail closed: `Left` with `tenancy.tenant_required` (`InvalidRequest`, ADR-062 rule R3) when multi-tenancy is on and the tenant is missing, and `encina.readwrite.tenant_replicas_missing` (`Internal`, rule R1: a missing topology entry) when the tenant has no replica set** | Complies with the fail-closed gate; an offboarded or mistyped tenant is denied | The request fails instead of degrading; operators must keep the tenant source complete |
| **C) A tenant with no replica set uses the global replica set; a missing tenant is denied** | Tenants that do not need their own set keep working | A tenant silently shares another database; the "default" reading hides a configuration mistake |

### Chosen Option: **B — fail closed** (recommended, pending the maintainer)

### Rationale

Compliance and security gates fail closed when context is missing or a lookup fails (AGENTS.md section 3), which contradicts the issue text ("unknown tenant reads from the primary") for the global primary; the maintainer decides whether the issue's wording stands. A wins where tenants are a column inside one shared database and the global primary is correct for everyone (then no per-tenant replica set exists and this feature is not used); C wins in a mixed estate where only large tenants have their own sets. The case that stays as in #633 is a known tenant whose replicas are stale or unknown: that tenant's own primary is used (its `WriteConnectionString`, or a `Left` when `FallbackToPrimary` is off), never another tenant's.

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Where the tenant router plugs into the connection selector</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) An optional `ITenantReplicaRouter` constructor parameter on `ReadWriteConnectionSelector`; the seven provider registrations pass `sp.GetService<ITenantReplicaRouter>()` (a declared sweep, PR 3)** | Independent of registration order: the router is looked up when the selector is built; no wrapper type | Seven identical edits in seven packages and their DI test |
| **B) A decorator over `IReadWriteConnectionSelector` installed by `AddEncinaTenantReplicaSets`** | No change in the provider packages | The extension must run after the provider registration (it wraps an existing descriptor) or fail at registration; an order rule the other extensions do not have |

### Chosen Option: **A — optional router parameter and a sweep** (recommended, pending the maintainer)

### Rationale

B wins if the maintainer wants zero edits in the provider packages and accepts an explicit registration-order error; A wins by default because registration must work in any order (AGENTS.md section 3, registration completeness) and the sweep is mechanical. The optional parameter keeps the 19 constructions in `ReadWriteConnectionSelectorTests.cs` and the eight integration tests compiling.

**Class:** Dilemma

</details>

<details>
<summary><strong>6. The reserved <code>default</code> tenant id as a replica set key</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A key equal to `default` is rejected at startup; untenanted requests use the global configuration of #633** | Consistent with the recommended reserved-id rule of #1257; no overlap between "no tenant" and a tenant | An application that already names a tenant `default` must rename it |
| **B) `default` is an ordinary tenant id** | No special case | Two meanings of one id in one map; contradicts the reserved id if #1257 keeps it |

### Chosen Option: **A — reject `default`** (recommended, pending the maintainer)

### Rationale

B wins if the maintainer decides in #1257 that `default` is an ordinary id; A wins while the reserved id is the recommendation there, because a single-tenant request and a tenant must never share a scope or a database. The comparison uses the exact tenant id, never a case-folded or trimmed form; ids with leading or trailing whitespace are rejected by the validator.

**Class:** Dilemma

</details>

### Smaller decisions (register)

Every entry below holds under every option of Design Choices 1 to 6 (under option C of Design Choice 1 or 4 nothing is built and the entry does not apply).

| Id | Decision | Class | Basis |
|----|----------|-------|-------|
| R1 | A tenant's entry holds its own primary (`WriteConnectionString`) and replicas; reads and writes of that tenant both use the entry, so a tenant is never read from one database and written to another | Trivial | The issue's "replica set per tenant"; a replica set is a primary with its replicas |
| R2 | Per-tenant routing is off unless tenant replica sets are configured; the `Enabled` flag of #633 still gates lag detection | Trivial | AGENTS.md section 1 (pay-for-what-you-use) |
| R3 | Connection strings of a tenant entry never reach logs, tags, metrics or `ToString()`; everything observable uses the tracker scope id and the replica index | Settled | AGENTS.md section 3 (secrets in options, #851) |
| R4 | Cancellation propagates as `OperationCanceledException`; a probe timeout is not a cancellation of the caller and is an unknown reading | Settled | AGENTS.md section 3 (cancellation is not an error and propagates, ADR-052); ADR-062 rule R5 |
| R5 | New log ids 7110-7113 sit inside the `ReplicationLag` block 7100-7149 reserved for the family (7100-7109 are #633's), packed; they log scope ids and error codes only | Trivial | AGENTS.md section 7; `docs/engineering/eventid-block-registry.md` (7100-7149 reserved) |
| R6 | The tenant tag on the lag telemetry is emitted only when multi-tenancy is on and only for tenants present in the replica set source; rejected requests are counted with no tenant tag | Settled | ADR-054 D4 (tenant tag when multi-tenancy is on; cardinality documented, #906); a tag fed by arbitrary request values is unbounded |
| R7 | Database calls of the probes stay asynchronous with a `CancellationToken` (`OpenAsync`, `ExecuteReaderAsync`) | Settled | AGENTS.md section 3 (database calls are asynchronous with a token) |

---

## Implementation Phases

### Phase 1: Replica set source, scope id and validation (`Encina.Messaging`)

> **Goal**: describe a tenant's replica set and its tracker scope; no routing.

<details>
<summary><strong>Tasks</strong></summary>

1. `ReadWriteSeparation/TenantReplicaSet.cs` (public sealed): `WriteConnectionString`, `ReadConnectionStrings`, optional `ReplicaStrategy`; secret-bearing members `[JsonIgnore]`, `ToString()` prints nothing sensitive.
2. `ReadWriteSeparation/ITenantReplicaSetSource.cs` (public): `bool TryGet(string tenantId, out TenantReplicaSet replicaSet)` and `IReadOnlyCollection<string> TenantIds { get; }` (Design Choice 2); `OptionsTenantReplicaSetSource` reads `ReadWriteSeparationOptions.Tenants` (new `IDictionary<string, TenantReplicaSet>`, default empty).
3. `TenantReplicaScopeId` (`internal static`): `For(string tenantId)` returns `readwrite:t:` plus the lowercase hex of the UTF-8 bytes (Design Choice 1).
4. `TenantReplicaSetsValidator` (`IValidateOptions`): tenant ids non-blank, trimmed, not `default` (Design Choice 6), unique by ordinal comparison; every entry has a write string and at least one read string; no two tenants share the same write string (two tenants on one primary is a configuration error or a shared database that needs no per-tenant set).
5. `ReplicationLagErrorCodes` (#633): `encina.readwrite.tenant_replicas_missing` (`Internal`, ADR-062 R1, constant message, used under Design Choice 4 option B); `tenancy.tenant_required` is reused, not redefined.
6. Tests: scope id (injectivity property over random strings including case pairs, hex only, prefix constant), source lookup, validator rejections, guard tests; PublicAPI and manifest through `mechanical-fixer`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT: ReadWriteSeparationOptions (src/Encina.Messaging/ReadWriteSeparation) has one global replica list; the tracker
compares scope ids with OrdinalIgnoreCase (ReplicaHealthTracker.cs:22).
TASK: Add TenantReplicaSet, ITenantReplicaSetSource with the options-backed default, TenantReplicaScopeId, the validator and
the error code.
KEY RULES: injective scope ids safe under a case-insensitive comparer (hex of the UTF-8 bytes); secrets [JsonIgnore] with a
ToString() override; error code with its ADR-062 category and a constant message; Either returns; no [Obsolete]; nothing is
registered yet.
REFERENCE FILES: docs/plans/replication-lag-detection-implementation-plan-633.md, src/Encina.Messaging/ReadWriteSeparation/ReadWriteSeparationOptions.cs.
```

</details>

### Phase 2: The tenant router and the selector (`Encina.Messaging`)

> **Goal**: the selector reads from the tenant's replica set and judges its lag.

<details>
<summary><strong>Tasks</strong></summary>

1. `ITenantReplicaRouter` (public): `Option<Either<EncinaError, string>> GetReadConnectionString()` and `GetWriteConnectionString()`; `None` means the request is not tenant-routed (multi-tenancy off and no ambient tenant).
2. `TenantReplicaRouter` (`internal sealed`, singleton-safe): reads the tenant through `IRequestContextAccessor` and `MultiTenancyMarker` (Design Choice 3); a missing tenant while multi-tenant, or an unknown tenant, returns `Left` (Design Choice 4); a known tenant gets its own `IReplicaSelector` (built once per tenant by `ReplicaSelectorFactory`, wrapped by the `LagAwareReplicaSelector` of #633 with the tenant's scope id); stale or unknown lag selects the tenant's own primary, or a `Left` when `FallbackToPrimary` is off.
3. `ReadWriteConnectionSelector`: optional `ITenantReplicaRouter? tenantRouter = null` on both constructors; when the router returns a value it is used, otherwise today's path.
4. `AddEncinaTenantReplicaSets(Action<ReadWriteSeparationOptions>?)` (new file in `Encina.Messaging`): `TryAddSingleton` for `ITenantReplicaSetSource`, `ITenantReplicaRouter` and `IRequestContextAccessor`, the validator through `TryAddEnumerable`; it registers every dependency the router resolves, in any order relative to the provider registration and to `AddEncinaReplicationLagDetection`; lag gating applies only when `ReplicationLagOptions.Enabled` of #633 is true, otherwise the tenant's strategy selector is used alone, as the global path does today.
5. Tests: router (known tenant, unknown, missing, multi-tenant off, two tenants never see each other's strings, stale and unknown lag, transitions logged once), selector, guard (optional parameter accepts null: valid-result test), property (random tenants and readings: the returned string always belongs to the request's tenant set), DI proof with `ValidateOnBuild` and `ValidateScopes` in both registration orders.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT: ReadWriteConnectionSelector is a singleton and GetReadConnectionString is synchronous; the tenant of the request is
ambient (IRequestContextAccessor). #633 adds LagAwareReplicaSelector over a tracker scope id.
TASK: Add ITenantReplicaRouter, TenantReplicaRouter and the optional router parameter of ReadWriteConnectionSelector.
KEY RULES: fail closed (missing or unknown tenant is a Left, never the global primary or another tenant's set); exact tenant id
lookup; no I/O on the query path; never inject a scoped service; Either returns; no connection string in any log or tag.
REFERENCE FILES: src/Encina.Messaging/ReadWriteSeparation/ReadWriteConnectionSelector.cs, src/Encina/Abstractions/IRequestContextAccessor.cs,
docs/plans/replication-lag-detection-implementation-plan-633.md (Phase 2).
```

</details>

### Phase 3: The provider sweep

> **Goal**: every provider's selector receives the router; one identical edit per package.

<details>
<summary><strong>Tasks</strong></summary>

1. In each `ServiceCollectionExtensions.cs` of the seven packages listed in the Summary, pass `sp.GetService<ITenantReplicaRouter>()` as the third argument of the two `new ReadWriteConnectionSelector(...)` calls (the second call becomes a lambda factory); no other change.
2. One DI test builds each provider with read/write separation, `AddEncinaTenantReplicaSets` (PR 2) and `ValidateOnBuild` and `ValidateScopes`, and asserts that a tenant request is routed by the tenant's set and that a provider built without the router behaves as before.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT: Seven packages construct ReadWriteConnectionSelector; the router is optional and registered by AddEncinaTenantReplicaSets (Phase 2).
TASK: Apply the identical registration edit to the seven files and add one DI test over them.
KEY RULES: declared sweep: same change, no new logic; GetService (optional), never GetRequiredService; list the seven files in the PR.
REFERENCE FILES: src/Encina.ADO.SqlServer/ServiceCollectionExtensions.cs (RegisterReadWriteSeparation).
```

</details>

### Phase 4: Probe service and logging

> **Goal**: every tenant's replicas are probed.

<details>
<summary><strong>Tasks</strong></summary>

1. `ReplicationLagProbeService` (#633): each iteration builds the work list from the global replicas and every tenant of `ITenantReplicaSetSource.TenantIds` (a snapshot), probes each distinct replica connection string once with `ProbeTimeout`, at most `MaxConcurrentProbes` at a time (new option, default 4, validated positive), and reports the reading to the scope of every tenant that lists it; the iteration body stays inside `try/catch` (cancellation ends the loop, other exceptions are logged by error code); no probe runs when no tenant set exists beyond #633's behavior.
2. `ReplicationLagLog` ids 7110 request without tenant rejected, 7111 tenant has no replica set, 7112 tenant replica set became ineligible, 7113 tenant replica set recovered (scope id and code only).
3. The probe service resolves `ITenantReplicaSetSource` with `GetService` (optional): when the source is registered (PR 2) it probes tenants, otherwise it behaves exactly as #633; `MaxConcurrentProbes` joins the options validator of #633. The DI proof of PR 2 gains a case with `AddEncinaReplicationLagDetection` registered before and after `AddEncinaTenantReplicaSets` (a test edit, no extra file).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT: The #633 probe service reports readings to the tracker under the scope id readwrite. Tenants share replicas rarely
but may; the tracker keys readings by (scope id, replica connection string).
TASK: Extend the probe service to tenants and add the log ids and MaxConcurrentProbes.
KEY RULES: a timer or loop never lets an exception escape; TimeProvider for every clock read; the tenant source is optional
(GetService); ids 7110-7113 only; bounded concurrency; one probe per distinct connection string.
REFERENCE FILES: src/Encina.Messaging/Outbox/OutboxProcessorBase.cs, src/Encina/Diagnostics/EventIdRanges.cs,
docs/plans/replication-lag-detection-implementation-plan-633.md (Phase 3).
```

</details>

### Phase 5: Observability and health

> **Goal**: lag and fallbacks are visible per tenant without unbounded tags.

<details>
<summary><strong>Tasks</strong></summary>

1. The metrics of #633 (`encina.readwrite.replica.lag_seconds`, `.fallbacks`, `.healthy`) gain a `tenant` tag for tenants in the source only (R6); a new counter `encina.readwrite.tenant.rejected` (tag `reason`: `tenant_missing`, `replicas_missing`) with no tenant tag; the probe span gains the tenant attribute under the same rule, through the helper of #2676 once it exists.
2. The replica health check of #633 reports each tenant scope as a named entry, keyed by the scope id, never a connection string.
3. Unit tests with meter and activity listeners; manifest rows through `mechanical-fixer`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
CONTEXT: ADR-054 D4: tenant as a tag when multi-tenancy is on; tenant values from requests must not create unbounded tags.
TASK: Add the tenant tag, the rejection counter and per-tenant health entries.
KEY RULES: tag only configured tenants; no connection string anywhere; names lowercase dotted; tests assert every tag.
REFERENCE FILES: docs/plans/replication-lag-detection-implementation-plan-633.md (Phase 7), docs/architecture/adr/054-observability-conventions-and-trace-propagation.md.
```

</details>

### Phase 6: Provider integration and the provider matrix

> **Goal**: two tenants on real databases, each judged by its own replicas.

<details>
<summary><strong>Tasks</strong></summary>

1. Integration tests through the shared `[Collection]` fixtures of #633 PR 8 for PostgreSQL and MySQL replicas: two tenants with separate replica sets; tenant A's lagging replica does not remove tenant B's; a request without a tenant or with an unknown tenant is denied; SQL Server (no availability group) reports unknown lag for both tenants and each falls back to its own primary.
2. A benchmark case (router resolution per query) added to the #633 benchmark class; the load justification of #633 gets one paragraph.

#### Provider matrix

| # | Family | Database | Per-tenant lag scope | Notes |
|---|--------|----------|----------------------|-------|
| 1 | ADO.NET | SQL Server | yes | probe: `sys.dm_hadr_database_replica_states` for the tenant's database; standalone instances report unknown lag |
| 2 | ADO.NET | PostgreSQL | yes | probe: `pg_last_xact_replay_timestamp()`; one server may host several tenants and then the probe is deduplicated by connection string |
| 3 | ADO.NET | MySQL | yes | probe: `SHOW REPLICA STATUS` |
| 4 | Dapper | SQL Server | yes | as row 1 |
| 5 | Dapper | PostgreSQL | yes | as row 2 |
| 6 | Dapper | MySQL | yes | as row 3 |
| 7 | EF Core | SQL Server | yes | `ReadWriteDbContextFactory` consumes the same selector |
| 8 | EF Core | PostgreSQL | yes | as row 7 |
| 9 | EF Core | MySQL | yes | as row 7 |
| 10 | MongoDB | MongoDB | not applicable | tenants share one cluster and one client (fact 7); the driver's `MaxStaleness` applies per cluster, no per-tenant scope exists |

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
CONTEXT: Replica containers are expensive; reuse the fixtures of the #633 integration pull request.
TASK: Add the two-tenant isolation integration tests and the benchmark case.
KEY RULES: shared [Collection] fixtures; tests run real code; FakeTimeProvider where time matters; no Thread.Sleep;
BenchmarkSwitcher.FromAssembly; MongoDB gets a justification line, not a test.
REFERENCE FILES: docs/plans/replication-lag-detection-implementation-plan-633.md (Phase 8), docs/testing/integration-tests.md.
```

</details>

### Phase 7: Documentation and finalization

> **Goal**: operators can configure per-tenant replica sets safely.

<details>
<summary><strong>Tasks</strong></summary>

1. XML docs with examples on every new public member; the router remark states that a missing or unknown tenant is denied.
2. `changelog.d/3991-replication-lag-per-tenant.added.md`; `src/Encina.Messaging/README.md` and the read/write separation page: the tenant set configuration, the scope id rule, the denial behavior, the privileges the probes need, the cardinality note of #906.
3. No ADR: the choices are recorded here and extend the measurement decision of #633.
4. Verification: zero warnings, tests, every flag at its target below.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
CONTEXT: The feature is code-complete after Phase 6.
TASK: Documentation, changelog fragment, verification.
KEY RULES: never type coverage figures by hand; English only; follow the encina-docs skill; never edit CHANGELOG.md; never name a
private application.
REFERENCE FILES: .claude/skills/encina-docs/SKILL.md, changelog.d/README.md.
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Relevance |
|--------|-----------|
| SPEC-002 DEC-006, DEC-009 | Fail-closed gates; every capability SPEC-002 touches is tenant-aware |
| ADR-054 D4 | Tenant id as span attribute and metric tag when multi-tenancy is on |
| ADR-062 rules R1, R3, R5 | Categories of the error codes |
| ADR-052 | Cancellation propagates |
| Database vendor documentation (PostgreSQL replication monitoring, MySQL `SHOW REPLICA STATUS`, SQL Server availability group DMVs) | Per-database lag sources used by the probes of #633 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|----------------------|
| `ReadWriteConnectionSelector`, `ReadWriteSeparationOptions`, `ReplicaSelectorFactory` | `src/Encina.Messaging/ReadWriteSeparation` | Extended, not replaced |
| `IReplicaHealthTracker`, `ReplicaHealthTracker` | `src/Encina/Sharding/ReplicaSelection` | One scope per tenant |
| Lag types of #633 | `src/Encina.Messaging/ReadWriteSeparation` (after #633) | Probe, probe service, lag-aware selector |
| `IRequestContextAccessor`, `MultiTenancyMarker` | `src/Encina/Abstractions` | Tenant of the request |
| Seven `ServiceCollectionExtensions.cs` | provider packages | Sweep |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Messaging` (`ReplicationLag` block 7100-7149, reserved-pre-zone in the block registry; #633 owns 7100-7109) | 7110-7113 used, packed | `ReplicationLagLog` of #633; 7114-7149 spare; no map change (the range is registered by #633 PR 3) |

### Per-file coverage targets (AGENTS.md section 9)

Per flag: U unit, G guard, C contract, P property, I integration; entered in `.github/coverage-manifest/Encina.Messaging.json` (and each provider manifest for the sweep) by the PR in the column, with that PR's tests, never lowered. For an existing file the row is the whole-file target (the manifest value when the PR opens, or the value shown if higher). The integration targets of PR 2 and PR 4 files are set in PR 6, with its tests.

| File | PR | U | G | C | P | I | Justification |
|------|----|---|---|---|---|---|---------------|
| `TenantReplicaSet.cs` | 1 | 90 | 90 | 0 | 0 | 0 | Options-like type; `ToString()` and defaults reachable |
| `ITenantReplicaSetSource.cs` | 1 | 0 | 0 | 0 | 0 | 0 | Interface, no executable lines |
| `OptionsTenantReplicaSetSource.cs` | 1 | 95 | 90 | 0 | 0 | 0 | Dictionary lookup, every branch reachable |
| `TenantReplicaScopeId.cs` | 1 | 95 | 0 | 0 | 90 | 0 | Pure function; the injectivity property test runs it |
| `TenantReplicaSetsValidator.cs` | 1 | 95 | 90 | 0 | 0 | 0 | Pure validation, every rejection reachable |
| `ReadWriteSeparationOptions.cs` (whole file) | 1 | 90 | 90 | 0 | 0 | 0 | Plain options |
| `ReplicationLagErrorCodes.cs` (whole file) | 1 | 90 | 0 | 0 | 0 | 0 | Factories plus the `GetCategory` test |
| `ITenantReplicaRouter.cs` | 2 | 0 | 0 | 0 | 0 | 0 | Interface, no executable lines |
| `TenantReplicaRouter.cs` | 2 | 95 | 90 | 0 | 90 | 70 | Branches reachable with fake accessor, source and tracker; integration in PR 6 |
| `ReadWriteConnectionSelector.cs` (whole file) | 2 | 95 | 90 | 0 | 0 | 70 | Existing 19 unit tests plus the router path; provider integration in PR 6 |
| Seven provider `ServiceCollectionExtensions.cs` (whole file) | 3 | 80 | 0 | 0 | 0 | 0 | The DI test builds each provider; no new branches |
| `ReplicationLagProbeService.cs` (whole file) | 4 | 90 | 90 | 0 | 0 | 70 | Loop body with fake timer and probe; integration in PR 6 |
| `ReplicationLagLog.cs` (whole file) | 4 | 70 | 0 | 0 | 0 | 0 | Generated methods reached by router and probe tests |
| `ReplicationLagOptions.cs` (whole file) | 4 | 90 | 90 | 0 | 0 | 0 | Options and validation |
| `AddEncinaTenantReplicaSets` file | 2 | 90 | 90 | 0 | 0 | 0 | DI proof builds the provider with `ValidateOnBuild` and `ValidateScopes` |
| Metrics, activity source and health check files of #633 PR 7 (whole files) | 5 | 90 | 0 | 0 | 0 | 0 | Listeners assert every tag |

CRAP stays at or below 10 on every changed method: the router has one private method per decision (tenant resolution, set lookup, lag gate).

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Production, new or edited under `src/` | 24 | PR 1: 7; PR 2: 4 (interface, router, selector, registration extension); PR 3: 7; PR 4: 3 (probe service, log, options); PR 5: 3 (metrics, activity source, health check) |
| PublicAPI and manifests | 8 | PRs 1, 2, 4: two each; PR 5: one manifest; PR 6: one manifest (PR 3 edits existing rows of no manifest) |
| Tests | 15 | PR 1: 5; PR 2: 5; PR 3: 1; PR 4: 2; PR 5: 2 |
| Integration, benchmark, load justification | 5 | PR 6: three integration classes, one benchmark edit, one load justification edit |
| Docs, changelog | 3 | PR 7 |

PR rows add to 55 distinct touches: 14 + 11 + 8 + 7 + 6 + 6 + 3.

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
PROJECT CONTEXT: Encina is a pre-1.0 .NET 10 / C# 14 library with nullable reference types. You are implementing issue #3991:
per-tenant replica sets for the replication lag detection of #633. Read AGENTS.md first.

IMPLEMENTATION OVERVIEW: Phase 1 adds TenantReplicaSet, ITenantReplicaSetSource, the injective tracker scope id and the
validator. Phase 2 adds ITenantReplicaRouter and the optional router parameter of ReadWriteConnectionSelector. Phase 3 is the
sweep over the seven provider registrations. Phase 4 extends the probe service, adds log ids 7110-7113 and
AddEncinaTenantReplicaSets. Phase 5 adds tenant tags and per-tenant health. Phase 6 adds two-tenant integration tests. Phase 7
documents. Deliver as the seven pull requests of the Summary, after the pull requests of #633 each one names.

KEY PATTERNS: fail closed (missing or unknown tenant is a Left); exact, injective tenant-qualified scope ids safe under the
tracker's case-insensitive comparer; a tenant's own primary is the only fallback; never log connection strings; no I/O on the
query path; TimeProvider; TryAdd registrations that work in any order; DI proof with ValidateOnBuild and ValidateScopes.

REFERENCE FILES: docs/plans/replication-lag-detection-implementation-plan-633.md, src/Encina.Messaging/ReadWriteSeparation,
src/Encina/Sharding/ReplicaSelection/ReplicaHealthTracker.cs, .github/coverage-manifest/Encina.Messaging.json.
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Not applicable: connection routing, no cacheable read path |
| 2 | OpenTelemetry | ✅ | Phase 5: tenant tag only for configured tenants (ADR-054 D4), rejection counter, span attribute |
| 3 | Structured Logging | ✅ | `ReplicationLagLog` ids 7110-7113; scope ids and codes only |
| 4 | Health Checks | ✅ | Phase 5: one entry per tenant scope in the replica health check of #633 |
| 5 | Validation | ✅ | Phase 1: tenant ids, entries and `MaxConcurrentProbes` validated at startup |
| 6 | Resilience | ✅ | Per-probe timeout and bounded concurrency; a failed probe marks the replica unhealthy as in #633; no circuit breaker (read-only probe) |
| 7 | Distributed Locks | ❌ | Not applicable: each instance probes for itself |
| 8 | Transactions | ❌ | Not applicable: no persistence |
| 9 | Idempotency | ❌ | Not applicable: no message handling |
| 10 | Multi-Tenancy | ✅ | The feature: per-tenant replica sets, scopes, fail-closed denial |
| 11 | Module Isolation | ❌ | Not applicable: infrastructure-level |
| 12 | Audit Trail | ❌ | Not applicable: routing decisions are not audit events; the denial is counted and logged by code |

---

## Prerequisites & Dependencies

| Item | State | Effect |
|------|-------|--------|
| #633 (replication lag detection) | open, plan on `main` | Owns every type extended here; this plan starts after the #633 pull requests named in the split |
| #1257 (SPEC-002 multi-tenancy) | open, plan on `main` | The reserved `default` id (Design Choice 6) and the shared tenant resolver (Design Choice 3 option C); no code dependency |
| #2676 (tenant tag helper) | open | The span attribute helper of Phase 5; a local constant meanwhile |
| #906 (OpenTelemetry guide) | open | Documents the tenant tag cardinality |

---

## Next Steps

1. Review and approve this plan; the maintainer answers the Dilemmas (choices 1 to 6) when work on the issue starts; none is decided here.
2. Publish the plan link as a comment on #3991.
3. Begin PR 1 after PR 1 of #633 lands.
4. Each phase is a self-contained commit; the final commit references `Fixes #3991`.
