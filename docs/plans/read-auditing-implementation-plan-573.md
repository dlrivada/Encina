# Implementation Plan: Read Auditing — Track Data Access for Sensitive Entities

> **Issue**: [#573](https://github.com/dlrivada/Encina/issues/573)
> **Type**: Feature
> **Complexity**: Medium (8 phases, 13 database providers, ~80 files)
> **Estimated Scope**: ~3,000-4,000 lines of production code + ~2,500-3,500 lines of tests

---

## Summary

Implement read auditing to track when sensitive data is accessed at the data access level. While the existing `Encina.Security.Audit` package (Issue #286) handles CUD auditing via `AuditPipelineBehavior` at the CQRS pipeline level, and Issue #395 covers command/query-level auditing, this feature targets **read operations at the repository layer** — capturing who accessed which entity, when, and why.

The implementation uses a **repository decorator pattern** (`AuditedRepository<TEntity, TId>`) that wraps `IReadOnlyRepository<TEntity, TId>` and `IRepository<TEntity, TId>`, recording `ReadAuditEntry` records via a dedicated `IReadAuditStore` interface. Entities opt in via the `IReadAuditable` marker interface. The feature extends the existing `Encina.Security.Audit` package rather than creating a new package, since read auditing is a natural extension of the audit domain.

**Provider category**: Database (13 providers) — `IReadAuditStore` requires persistence across ADO.NET (×4), Dapper (×4), EF Core (×4), and MongoDB (×1).

**Affected packages**:

- `Encina.Security.Audit` — Core abstractions (`IReadAuditable`, `ReadAuditEntry`, `IReadAuditStore`, `ReadAuditOptions`)
- `Encina.DomainModeling` — `IReadAuditable` marker interface (alongside existing `IEntity<TId>`)
- `Encina.EntityFrameworkCore` — `ReadAuditStoreEF`, `AuditedRepositoryEF`
- `Encina.Dapper.{Sqlite,SqlServer,PostgreSQL,MySQL}` — `ReadAuditStoreDapper`
- `Encina.ADO.{Sqlite,SqlServer,PostgreSQL,MySQL}` — `ReadAuditStoreADO`
- `Encina.MongoDB` — `ReadAuditStoreMongoDB`

---

## Design Choices

<details>
<summary><strong>1. Package Placement — Extend <code>Encina.Security.Audit</code> (not a new package)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Extend `Encina.Security.Audit`** | Natural extension of audit domain, reuses `IRequestContext` integration, shared `AuditOptions` configuration, fewer packages to maintain | Slightly increases package size |
| **B) New `Encina.Security.ReadAudit` package** | Clean separation, independent versioning | Unnecessary package proliferation — read audit is a subset of auditing, not a separate domain |
| **C) Extend `Encina.Compliance.GDPR`** | Compliance context | Mixes security auditing with compliance; read auditing applies beyond GDPR (HIPAA, SOX, PCI-DSS) |

### Chosen Option: **A — Extend `Encina.Security.Audit`**

### Rationale

- Read auditing is fundamentally an **auditing concern**, not a compliance concern — it tracks "who accessed what" alongside the existing "who changed what"
- The existing `AuditEntry` model, `IAuditStore`, `AuditOptions`, and DI registration infrastructure can be reused
- Follows the principle of cohesion: all audit-related abstractions live in the same package
- The `IReadAuditable` marker interface goes in `Encina.DomainModeling` alongside `IEntity<TId>`, `IAuditableEntity`, and other domain markers
- Provider implementations go in their existing `Auditing/` subfolders within each satellite package

</details>

<details>
<summary><strong>2. Decorator Pattern — Repository decorator wrapping <code>IReadOnlyRepository</code> and <code>IRepository</code></strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Repository decorator** | Transparent to consumers, intercepts all read operations, follows DDD patterns, existing precedent in `AuditedSecretReaderDecorator` | Requires repository pattern usage; doesn't capture direct `DbContext` access |
| **B) EF Core query interceptor** | Captures ALL queries including direct `DbContext` | Cannot determine which entities were returned, high volume, cannot distinguish "read for display" vs "read for processing" |
| **C) CQRS pipeline behavior** | Works at query handler level | Doesn't capture direct repository access; already covered by Issue #395 |

### Chosen Option: **A — Repository decorator**

### Rationale

- The issue explicitly recommends this approach, and it's the correct architectural choice
- Existing precedent: `AuditedSecretReaderDecorator` in `Encina.Security.Secrets` proves the pattern works well
- The decorator only activates for entities implementing `IReadAuditable` — zero overhead for non-auditable entities
- Records audit entries fire-and-forget (audit failures never block reads), same resilience pattern as `AuditedSecretReaderDecorator`
- Integrates naturally with the optional repository pattern described in CLAUDE.md
- Applications not using repository pattern can still use `IReadAuditStore` directly for manual audit logging

</details>

<details>
<summary><strong>3. Read Audit Entry Model — Dedicated <code>ReadAuditEntry</code> record (not reusing <code>AuditEntry</code>)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Dedicated `ReadAuditEntry` record** | Purpose-built for read access, includes `Purpose` field, no unnecessary CUD fields (payload hash, request/response payload), cleaner queries | Separate table/collection, separate store interface |
| **B) Reuse existing `AuditEntry`** | Single table, single store, unified query surface | Bloated with irrelevant CUD fields, no `Purpose` field, conflates access auditing with operation auditing |
| **C) Extend `AuditEntry` with optional read fields** | Single schema | Nullable sprawl, violates single responsibility |

### Chosen Option: **A — Dedicated `ReadAuditEntry` record**

### Rationale

- Read access auditing has different data requirements than CUD auditing:
  - No `RequestPayload`/`ResponsePayload` (reads don't modify data)
  - No `RequestPayloadHash` (tamper detection irrelevant for reads)
  - Adds `Purpose` field (required by GDPR Art. 15 — why was data accessed?)
  - Adds `AccessMethod` (repository, direct query, API, etc.)
- Separate `ReadAuditEntries` table allows independent retention policies (read logs may have shorter retention than CUD audit)
- `IReadAuditStore` provides read-specific queries: access history by entity, user access history with date ranges, access frequency reports
- Follows the existing pattern: `BreachAuditEntry` is separate from `AuditEntry` in the breach notification domain

</details>

<details>
<summary><strong>4. Configuration Model — Fluent <code>ReadAuditOptions</code> with entity-level opt-in</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Fluent `ReadAuditOptions` with type registration** | Explicit opt-in per entity type, configurable sampling, purpose tracking | Requires registration per type |
| **B) Attribute-based only (`IReadAuditable` marker)** | Simple, no configuration needed | No runtime configurability, no sampling, no exclusion |
| **C) Convention-based (all entities audited)** | Zero configuration | Massive audit volume, performance disaster |

### Chosen Option: **A — Fluent `ReadAuditOptions` with type registration**

### Rationale

- Combines marker interface (`IReadAuditable`) with runtime configuration
- `IReadAuditable` marks which entity types are eligible; `ReadAuditOptions` controls runtime behavior
- Sampling support for high-traffic entities: `options.AuditReadsFor<Patient>(samplingRate: 0.1)` — audit 10% of reads
- System access exclusion: `options.ExcludeSystemAccess = true` — skip audit for background jobs
- Batching support: `options.BatchSize = 100` — batch audit writes for high-throughput scenarios
- Purpose tracking: `options.RequirePurpose = true` — enforce purpose declaration for compliance

```csharp
services.AddEncinaReadAuditing(options =>
{
    options.AuditReadsFor<Patient>();
    options.AuditReadsFor<FinancialRecord>(samplingRate: 0.5);
    options.ExcludeSystemAccess = true;
    options.BatchSize = 50;
});
```

</details>

<details>
<summary><strong>5. Store Persistence — Separate <code>ReadAuditEntries</code> table with async writes</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Separate table with fire-and-forget async writes** | Independent of main transaction, no read performance impact, separate retention | Extra table per database |
| **B) Same table as `AuditEntries` with action type filter** | Single table | Conflates read and write auditing, complicates queries and retention |
| **C) In-memory buffer with periodic flush** | Zero latency impact | Risk of data loss on crash, memory pressure |

### Chosen Option: **A — Separate table with fire-and-forget async writes**

### Rationale

- Read audit entries are fire-and-forget: audit failures must never block the original read operation (same resilience pattern as `AuditedSecretReaderDecorator`)
- Separate `ReadAuditEntries` table enables:
  - Independent indexes optimized for read access patterns (by entity, by user, by time range)
  - Independent retention policies (read logs may have shorter TTL)
  - Independent partitioning/archival strategies
- Async writes using `Task.Run` + `try/catch` with logging — identical pattern to existing audit infrastructure
- `IReadAuditStore` implementations across all 13 database providers follow the same SQL/query patterns as `IAuditStore`

</details>

<details>
<summary><strong>6. Decorator Registration — Automatic via DI with conditional activation</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Automatic DI decoration via `Decorate<IRepository, AuditedRepository>`** | Transparent, zero code changes for consumers | Requires DI decoration support (Scrutor or manual) |
| **B) Manual wrapping by consumer** | Full control | Boilerplate, easy to forget |
| **C) Factory-based creation** | Flexible | More complex DI setup |

### Chosen Option: **A — Automatic DI decoration**

### Rationale

- When `AddEncinaReadAuditing()` is called, it decorates all registered `IRepository<TEntity, TId>` and `IReadOnlyRepository<TEntity, TId>` implementations where `TEntity : IReadAuditable`
- Uses `services.Decorate<IRepository<TEntity, TId>, AuditedRepository<TEntity, TId>>()` pattern
- Conditional: only entities implementing `IReadAuditable` get decorated — others pass through unchanged
- Implementation uses a generic open decorator with runtime type checking: `if (typeof(IReadAuditable).IsAssignableFrom(typeof(TEntity)))`
- For non-repository users: `IReadAuditStore` is available for direct manual logging

</details>

---

## Implementation Phases

### Phase 1: Core Models & Marker Interface

> **Goal**: Establish the foundational types for read auditing.

<details>
<summary><strong>Tasks</strong></summary>

1. **`IReadAuditable` marker interface** in `src/Encina.DomainModeling/IReadAuditable.cs`:
   - `public interface IReadAuditable { }` — marker interface, no members
   - XML docs referencing GDPR Art. 15, HIPAA, SOX, PCI-DSS
   - Placed alongside `IEntity<TId>`, `IAuditableEntity`, and other domain markers

2. **`ReadAuditEntry` sealed record** in `src/Encina.Security.Audit/ReadAuditEntry.cs`:
   - `Id` (Guid) — unique identifier
   - `EntityType` (string) — fully qualified or short entity type name
   - `EntityId` (string) — the specific entity accessed
   - `UserId` (string?) — from `IRequestContext`
   - `TenantId` (string?) — from `IRequestContext`
   - `AccessedAtUtc` (DateTimeOffset) — when the access occurred
   - `CorrelationId` (string?) — from `IRequestContext`
   - `Purpose` (string?) — why data was accessed (GDPR Art. 15 compliance)
   - `AccessMethod` (ReadAccessMethod) — how the data was accessed
   - `EntityCount` (int) — number of entities returned (for bulk reads)
   - `Metadata` (IReadOnlyDictionary<string, object?>?) — extensibility

3. **`ReadAccessMethod` enum** in `src/Encina.Security.Audit/ReadAccessMethod.cs`:
   - `Repository` — accessed via `IRepository<T, TId>`
   - `DirectQuery` — accessed via direct database query
   - `Api` — accessed via external API call
   - `Export` — accessed for data export/portability
   - `Custom` — user-defined access method

4. **`ReadAuditQuery` record** in `src/Encina.Security.Audit/ReadAuditQuery.cs`:
   - Builder pattern matching existing `AuditQuery`
   - Filters: `EntityType`, `EntityId`, `UserId`, `TenantId`, `FromUtc`, `ToUtc`, `AccessMethod`, `Purpose`
   - Pagination: `PageNumber`, `PageSize`

5. **Update `PublicAPI.Unshipped.txt`** in `Encina.DomainModeling` and `Encina.Security.Audit`

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of Read Auditing (Issue #573).

CONTEXT:
- Encina is a .NET 10 / C# 14 library using Railway Oriented Programming (Either<EncinaError, T>)
- IReadAuditable is a MARKER INTERFACE (no members) placed in Encina.DomainModeling
- ReadAuditEntry is a SEALED RECORD in Encina.Security.Audit
- Follow existing patterns from AuditEntry (src/Encina.Security.Audit/AuditEntry.cs)
- Timestamps use DateTimeOffset with AtUtc suffix convention
- All public types need XML documentation with <summary>, <remarks>, regulatory references

TASK:
1. Create IReadAuditable marker interface in src/Encina.DomainModeling/IReadAuditable.cs
2. Create ReadAuditEntry sealed record in src/Encina.Security.Audit/ReadAuditEntry.cs
3. Create ReadAccessMethod enum in src/Encina.Security.Audit/ReadAccessMethod.cs
4. Create ReadAuditQuery with builder pattern in src/Encina.Security.Audit/ReadAuditQuery.cs
5. Update PublicAPI.Unshipped.txt in both projects

KEY RULES:
- IReadAuditable goes in Encina.DomainModeling namespace (alongside IEntity<TId>)
- ReadAuditEntry is independent from AuditEntry — different fields, different table
- Include Purpose field (GDPR Art. 15 — right of access requires recording purpose)
- Include EntityCount field (for bulk read operations like GetAll, Find)
- ReadAuditQuery follows same builder pattern as AuditQuery

REFERENCE FILES:
- src/Encina.Security.Audit/AuditEntry.cs (record pattern)
- src/Encina.Security.Audit/AuditQuery.cs (query builder pattern)
- src/Encina.Security.Audit/AuditOutcome.cs (enum pattern)
- src/Encina.DomainModeling/IEntity.cs (marker interface placement)
```

</details>

---

### Phase 2: Core Interface & Error Codes

> **Goal**: Define the public API surface for read audit storage and configuration.

<details>
<summary><strong>Tasks</strong></summary>

1. **`IReadAuditStore` interface** in `src/Encina.Security.Audit/Abstractions/IReadAuditStore.cs`:
   - `LogReadAsync(ReadAuditEntry entry, CancellationToken)` → `ValueTask<Either<EncinaError, Unit>>`
   - `GetAccessHistoryAsync(string entityType, string entityId, CancellationToken)` → `ValueTask<Either<EncinaError, IReadOnlyList<ReadAuditEntry>>>`
   - `GetUserAccessHistoryAsync(string userId, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken)` → `ValueTask<Either<EncinaError, IReadOnlyList<ReadAuditEntry>>>`
   - `QueryAsync(ReadAuditQuery query, CancellationToken)` → `ValueTask<Either<EncinaError, PagedResult<ReadAuditEntry>>>`
   - `PurgeEntriesAsync(DateTimeOffset olderThanUtc, CancellationToken)` → `ValueTask<Either<EncinaError, int>>`

2. **`InMemoryReadAuditStore`** in `src/Encina.Security.Audit/InMemoryReadAuditStore.cs`:
   - Thread-safe `ConcurrentBag<ReadAuditEntry>` implementation
   - Default registration via `TryAddSingleton`
   - Follows `InMemoryAuditStore` pattern

3. **`ReadAuditOptions`** in `src/Encina.Security.Audit/ReadAuditOptions.cs`:
   - `Enabled` (bool, default: true)
   - `AuditReadsFor<TEntity>()` — register entity type for auditing
   - `AuditReadsFor<TEntity>(double samplingRate)` — register with sampling (0.0-1.0)
   - `ExcludeSystemAccess` (bool, default: false) — skip audit for system/background access
   - `RequirePurpose` (bool, default: false) — enforce purpose declaration
   - `BatchSize` (int, default: 1) — number of entries to batch before writing
   - `RetentionDays` (int, default: 365) — read audit retention period
   - `EnableAutoPurge` (bool, default: false) — background purge service
   - `PurgeIntervalHours` (int, default: 24) — purge check interval
   - Internal: `IsAuditable(Type entityType)` → bool
   - Internal: `GetSamplingRate(Type entityType)` → double

4. **`ReadAuditErrors`** in `src/Encina.Security.Audit/ReadAuditErrors.cs`:
   - Error code prefix: `read_audit.`
   - Codes: `read_audit.store_error`, `read_audit.not_found`, `read_audit.invalid_query`, `read_audit.purge_failed`, `read_audit.purpose_required`
   - Follow `BreachNotificationErrors.cs` factory pattern

5. **Notification records** in `src/Encina.Security.Audit/Notifications/`:
   - `SensitiveDataAccessedNotification` — implements `INotification`: `EntityType`, `EntityId`, `UserId`, `AccessedAtUtc`

6. **Update `PublicAPI.Unshipped.txt`**

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of Read Auditing (Issue #573).

CONTEXT:
- Phase 1 models are already implemented (IReadAuditable, ReadAuditEntry, ReadAccessMethod, ReadAuditQuery)
- Encina uses ROP: all store methods return ValueTask<Either<EncinaError, T>>
- Follow IAuditStore pattern for IReadAuditStore
- Follow InMemoryAuditStore pattern for InMemoryReadAuditStore
- Follow AuditOptions pattern for ReadAuditOptions

TASK:
1. Create IReadAuditStore in src/Encina.Security.Audit/Abstractions/IReadAuditStore.cs
2. Create InMemoryReadAuditStore in src/Encina.Security.Audit/InMemoryReadAuditStore.cs
3. Create ReadAuditOptions in src/Encina.Security.Audit/ReadAuditOptions.cs
4. Create ReadAuditErrors in src/Encina.Security.Audit/ReadAuditErrors.cs
5. Create SensitiveDataAccessedNotification in src/Encina.Security.Audit/Notifications/
6. Update PublicAPI.Unshipped.txt

KEY RULES:
- IReadAuditStore is separate from IAuditStore — different entry types, different queries
- InMemoryReadAuditStore is the default (registered via TryAddSingleton)
- ReadAuditOptions supports per-entity sampling rates for high-traffic scenarios
- Error factory methods follow the static class pattern with string error codes
- Notification implements INotification from Encina core
- All public types need full XML documentation

REFERENCE FILES:
- src/Encina.Security.Audit/Abstractions/IAuditStore.cs (store interface pattern)
- src/Encina.Security.Audit/InMemoryAuditStore.cs (in-memory implementation)
- src/Encina.Security.Audit/AuditOptions.cs (options pattern with fluent API)
- src/Encina.Compliance.BreachNotification/BreachNotificationErrors.cs (error factory)
```

</details>

---

### Phase 3: Repository Decorator

> **Goal**: Implement the `AuditedRepository` decorator that intercepts read operations.

<details>
<summary><strong>Tasks</strong></summary>

1. **`AuditedRepository<TEntity, TId>`** in `src/Encina.Security.Audit/AuditedRepository.cs`:
   - Implements both `IReadOnlyRepository<TEntity, TId>` and `IRepository<TEntity, TId>`
   - Constructor: `(IRepository<TEntity, TId> inner, IReadAuditStore auditStore, IRequestContext requestContext, ReadAuditOptions options, TimeProvider timeProvider, ILogger<AuditedRepository<TEntity, TId>> logger)`
   - Constraint: `where TEntity : class, IEntity<TId>, IReadAuditable`
   - Read methods intercepted:
     - `GetByIdAsync(TId id)` — logs single entity access, `EntityCount = 1`
     - `GetAllAsync()` — logs bulk access, `EntityCount = result.Count`
     - `FindAsync(Specification<TEntity>)` — logs specification query, `EntityCount = result.Count`
     - `FindOneAsync(Specification<TEntity>)` — logs single entity query, `EntityCount = result.IsSome ? 1 : 0`
     - `FindAsync(Expression<Func<TEntity, bool>>)` — logs predicate query, `EntityCount = result.Count`
     - `GetPagedAsync(int, int)` — logs paged access, `EntityCount = result.Items.Count`
   - Write methods: delegate directly to `_inner` without auditing (CUD handled by existing audit)
   - Sampling: check `options.GetSamplingRate(typeof(TEntity))`, use `Random.Shared.NextDouble() < rate`
   - Fire-and-forget: `_ = LogReadAccessAsync(...)` pattern with `try/catch` and logging

2. **`AuditedReadOnlyRepository<TEntity, TId>`** in `src/Encina.Security.Audit/AuditedReadOnlyRepository.cs`:
   - Same pattern but implements only `IReadOnlyRepository<TEntity, TId>`
   - For scenarios where only read-only repository is registered

3. **`IReadAuditContext`** in `src/Encina.Security.Audit/Abstractions/IReadAuditContext.cs`:
   - `string? Purpose { get; }` — current access purpose
   - `IReadAuditContext WithPurpose(string purpose)` — fluent setter
   - Allows callers to declare purpose before reading: `readAuditContext.WithPurpose("Patient care review")`

4. **`ReadAuditContext`** implementation in `src/Encina.Security.Audit/ReadAuditContext.cs`:
   - Scoped service, stores purpose per request
   - Used by decorator to populate `ReadAuditEntry.Purpose`

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are implementing Phase 3 of Read Auditing (Issue #573).

CONTEXT:
- Phase 1-2 models and interfaces are implemented (IReadAuditable, ReadAuditEntry, IReadAuditStore, ReadAuditOptions)
- Follow AuditedSecretReaderDecorator pattern from src/Encina.Security.Secrets/Auditing/
- Repository interfaces defined in src/Encina.DomainModeling/Repository.cs
- IRepository<TEntity, TId> extends IReadOnlyRepository<TEntity, TId>
- IRequestContext provides UserId, TenantId, CorrelationId

TASK:
1. Create AuditedRepository<TEntity, TId> in src/Encina.Security.Audit/AuditedRepository.cs
2. Create AuditedReadOnlyRepository<TEntity, TId> in src/Encina.Security.Audit/AuditedReadOnlyRepository.cs
3. Create IReadAuditContext in src/Encina.Security.Audit/Abstractions/IReadAuditContext.cs
4. Create ReadAuditContext in src/Encina.Security.Audit/ReadAuditContext.cs

KEY RULES:
- RESILIENCE: Audit failures NEVER block read operations — use try/catch with logging
- FIRE-AND-FORGET: Use _ = LogReadAccessAsync(...) pattern (do NOT await audit writes)
- SAMPLING: Check options.GetSamplingRate(typeof(TEntity)) before logging
- SYSTEM ACCESS: Skip audit when options.ExcludeSystemAccess && requestContext.UserId == null
- PURPOSE: Populate from IReadAuditContext if available
- CONSTRAINT: TEntity must implement both IEntity<TId> and IReadAuditable
- Write methods (Add, Update, Remove) delegate directly to inner without auditing
- TimeProvider for consistent timestamps (injected, not static)

REFERENCE FILES:
- src/Encina.Security.Secrets/Auditing/AuditedSecretReaderDecorator.cs (decorator pattern)
- src/Encina.DomainModeling/Repository.cs (IRepository, IReadOnlyRepository interfaces)
- src/Encina.Security.Audit/AuditEntry.cs (entry creation pattern)
```

</details>

---

### Phase 4: Configuration, DI & Auto-Registration

> **Goal**: Wire up read auditing via `AddEncinaReadAuditing()` extension method.

<details>
<summary><strong>Tasks</strong></summary>

1. **`AddEncinaReadAuditing()` extension method** in `src/Encina.Security.Audit/ServiceCollectionExtensions.cs`:
   - Add new method alongside existing `AddEncinaAudit()`:

   ```csharp
   public static IServiceCollection AddEncinaReadAuditing(
       this IServiceCollection services,
       Action<ReadAuditOptions>? configure = null)
   ```

   - Register `ReadAuditOptions` via `services.Configure<ReadAuditOptions>()`
   - Register `IReadAuditStore` → `InMemoryReadAuditStore` via `TryAddSingleton`
   - Register `IReadAuditContext` → `ReadAuditContext` via `AddScoped`
   - Register `ReadAuditRetentionService` as `IHostedService` when `options.EnableAutoPurge`
   - **Repository decoration**: Scan registered services for `IRepository<TEntity, TId>` where `TEntity : IReadAuditable`, decorate with `AuditedRepository<TEntity, TId>`

2. **`ReadAuditRetentionService`** in `src/Encina.Security.Audit/ReadAuditRetentionService.cs`:
   - `IHostedService` for periodic purge of old read audit entries
   - Follow `AuditRetentionService` pattern exactly
   - Uses `IReadAuditStore.PurgeEntriesAsync()` with `RetentionDays` from `ReadAuditOptions`
   - Configurable interval via `PurgeIntervalHours`

3. **Health check** `ReadAuditStoreHealthCheck` in `src/Encina.Security.Audit/Health/ReadAuditStoreHealthCheck.cs`:
   - Follow `AuditStoreHealthCheck` pattern
   - `DefaultName` const, `Tags` static array
   - Scoped resolution via `IServiceProvider.CreateScope()`
   - Register via `AddReadAuditStoreHealthCheck()` extension

4. **Update `PublicAPI.Unshipped.txt`**

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
You are implementing Phase 4 of Read Auditing (Issue #573).

CONTEXT:
- Phases 1-3 are implemented (models, interfaces, decorators)
- Follow ServiceCollectionExtensions.cs pattern from Encina.Security.Audit
- Follow AuditRetentionService pattern for background purge
- Follow AuditStoreHealthCheck pattern for health check

TASK:
1. Add AddEncinaReadAuditing() to src/Encina.Security.Audit/ServiceCollectionExtensions.cs
2. Create ReadAuditRetentionService in src/Encina.Security.Audit/ReadAuditRetentionService.cs
3. Create ReadAuditStoreHealthCheck in src/Encina.Security.Audit/Health/ReadAuditStoreHealthCheck.cs
4. Update PublicAPI.Unshipped.txt

KEY RULES:
- Use TryAddSingleton for IReadAuditStore → InMemoryReadAuditStore (overrideable)
- Use AddScoped for IReadAuditContext → ReadAuditContext
- Repository decoration: only decorate IRepository<TEntity, TId> where TEntity : IReadAuditable
- Health check: DefaultName const = "read_audit_store", Tags = ["encina", "audit", "read-audit"]
- Health check uses scoped resolution
- Retention service follows AuditRetentionService exactly (IHostedService, periodic timer, TimeProvider)
- ArgumentNullException.ThrowIfNull on all parameters

REFERENCE FILES:
- src/Encina.Security.Audit/ServiceCollectionExtensions.cs (DI registration pattern)
- src/Encina.Security.Audit/AuditRetentionService.cs (background service pattern)
- src/Encina.Security.Audit/Health/AuditStoreHealthCheck.cs (health check pattern)
```

</details>

---

### Phase 5: Persistence Entity, Mapper & SQL Scripts

> **Goal**: Create the persistence layer and database schemas for all 13 providers.

<details>
<summary><strong>Tasks</strong></summary>

1. **`ReadAuditEntryEntity`** in `src/Encina.Security.Audit/ReadAuditEntryEntity.cs`:
   - Persistence POCO for database storage
   - Properties match database columns: `Id`, `EntityType`, `EntityId`, `UserId`, `TenantId`, `AccessedAtUtc`, `CorrelationId`, `Purpose`, `AccessMethod` (int), `EntityCount`, `MetadataJson` (string?)

2. **`ReadAuditEntryMapper`** in `src/Encina.Security.Audit/ReadAuditEntryMapper.cs`:
   - `static ReadAuditEntryEntity ToEntity(ReadAuditEntry entry)`
   - `static ReadAuditEntry ToDomain(ReadAuditEntryEntity entity)`
   - Handles enum conversion (`ReadAccessMethod` ↔ int)
   - Handles JSON serialization for Metadata dictionary

3. **SQL Scripts** — Create `XXX_CreateReadAuditEntriesTable.sql` for each provider:

   **ADO.NET providers** (in `Scripts/` folder of each):
   - `src/Encina.ADO.Sqlite/Scripts/020_CreateReadAuditEntriesTable.sql`
   - `src/Encina.ADO.SqlServer/Scripts/020_CreateReadAuditEntriesTable.sql`
   - `src/Encina.ADO.PostgreSQL/Scripts/020_CreateReadAuditEntriesTable.sql`
   - `src/Encina.ADO.MySQL/Scripts/020_CreateReadAuditEntriesTable.sql`

   **Dapper providers** (same scripts, mirrored):
   - `src/Encina.Dapper.Sqlite/Scripts/020_CreateReadAuditEntriesTable.sql`
   - `src/Encina.Dapper.SqlServer/Scripts/020_CreateReadAuditEntriesTable.sql`
   - `src/Encina.Dapper.PostgreSQL/Scripts/020_CreateReadAuditEntriesTable.sql`
   - `src/Encina.Dapper.MySQL/Scripts/020_CreateReadAuditEntriesTable.sql`

   **Table schema**:

   ```
   ReadAuditEntries (
     Id              GUID/TEXT PRIMARY KEY,
     EntityType      VARCHAR/TEXT NOT NULL,
     EntityId        VARCHAR/TEXT NOT NULL,
     UserId          VARCHAR/TEXT NULL,
     TenantId        VARCHAR/TEXT NULL,
     AccessedAtUtc   DATETIME/TEXT NOT NULL,
     CorrelationId   VARCHAR/TEXT NULL,
     Purpose         VARCHAR/TEXT NULL,
     AccessMethod    INT NOT NULL DEFAULT 0,
     EntityCount     INT NOT NULL DEFAULT 1,
     MetadataJson    TEXT NULL
   )
   ```

   **Indexes**:
   - `IX_ReadAuditEntries_EntityType_EntityId` — entity access history
   - `IX_ReadAuditEntries_UserId_AccessedAtUtc` — user access history
   - `IX_ReadAuditEntries_AccessedAtUtc` — retention purge

   **Provider-specific differences**:

   | Provider | Id Type | DateTime Type | Index Syntax |
   |----------|---------|---------------|--------------|
   | SQLite | TEXT | TEXT (ISO 8601) | CREATE INDEX IF NOT EXISTS |
   | SQL Server | UNIQUEIDENTIFIER | DATETIME2(7) | CREATE INDEX with INCLUDE |
   | PostgreSQL | UUID | TIMESTAMPTZ | CREATE INDEX |
   | MySQL | CHAR(36) | DATETIME(6) | CREATE INDEX |

4. **EF Core configuration** in `src/Encina.EntityFrameworkCore/Auditing/ReadAuditEntryEntityConfiguration.cs`:
   - `IEntityTypeConfiguration<ReadAuditEntryEntity>`
   - Table name: `ReadAuditEntries`
   - Index configuration matching SQL scripts

5. **Update `000_CreateAllTables.sql`** in all 8 ADO/Dapper providers to include the new table

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
You are implementing Phase 5 of Read Auditing (Issue #573).

CONTEXT:
- Phases 1-4 are implemented (models, interfaces, decorators, DI)
- SQL scripts follow numbered convention (check existing highest number, use next available)
- Provider differences: SQLite stores GUIDs as TEXT, dates as ISO 8601 TEXT; SQL Server uses UNIQUEIDENTIFIER/DATETIME2; PostgreSQL uses UUID/TIMESTAMPTZ; MySQL uses CHAR(36)/DATETIME(6)
- Follow existing AuditEntryEntity and AuditStoreEF patterns

TASK:
1. Create ReadAuditEntryEntity in src/Encina.Security.Audit/ReadAuditEntryEntity.cs
2. Create ReadAuditEntryMapper in src/Encina.Security.Audit/ReadAuditEntryMapper.cs
3. Create SQL scripts (020_CreateReadAuditEntriesTable.sql) in all 8 ADO/Dapper provider Scripts/ folders
4. Create EF Core entity configuration in src/Encina.EntityFrameworkCore/Auditing/
5. Update 000_CreateAllTables.sql in all 8 providers

KEY RULES:
- SQLITE: TEXT for all types, ISO 8601 for dates, CREATE INDEX IF NOT EXISTS
- SQL SERVER: UNIQUEIDENTIFIER for Id, NVARCHAR for strings, DATETIME2(7) for dates
- POSTGRESQL: UUID for Id, VARCHAR for strings, TIMESTAMPTZ for dates
- MYSQL: CHAR(36) for Id, VARCHAR for strings, DATETIME(6) for dates, backtick identifiers
- Mapper handles enum → int conversion, JSON serialization for Metadata
- Entity is a POCO with get/set properties (not a sealed record)
- Script number must not collide — check existing scripts in each provider

REFERENCE FILES:
- src/Encina.ADO.Sqlite/Scripts/005_CreateAuditLogsTable.sql (SQLite script pattern)
- src/Encina.ADO.SqlServer/Scripts/005_CreateAuditLogsTable.sql (SQL Server script pattern)
- src/Encina.ADO.PostgreSQL/Scripts/005_CreateAuditLogsTable.sql (PostgreSQL script pattern)
- src/Encina.ADO.MySQL/Scripts/005_CreateAuditLogsTable.sql (MySQL script pattern)
- src/Encina.EntityFrameworkCore/Auditing/AuditStoreEF.cs (EF Core entity config reference)
```

</details>

---

### Phase 6: Multi-Provider Store Implementations

> **Goal**: Implement `IReadAuditStore` across all 13 database providers.

<details>
<summary><strong>Tasks</strong></summary>

#### EF Core (4 implementations)

1. **`ReadAuditStoreEF`** in `src/Encina.EntityFrameworkCore/Auditing/ReadAuditStoreEF.cs`:
   - Constructor: `(DbContext dbContext, TimeProvider? timeProvider = null)`
   - Uses `DbContext.Set<ReadAuditEntryEntity>()` for LINQ queries
   - `LogReadAsync` → `AddAsync` + `SaveChangesAsync`
   - `GetAccessHistoryAsync` → Where + OrderByDescending(AccessedAtUtc)
   - `GetUserAccessHistoryAsync` → Where(UserId, date range) + OrderByDescending
   - `QueryAsync` → dynamic Where + pagination
   - `PurgeEntriesAsync` → `ExecuteDeleteAsync` for bulk delete
   - Register in `Encina.EntityFrameworkCore/ServiceCollectionExtensions.cs`

2. **Register for SqlServer, PostgreSQL, MySQL, Sqlite** — same `ReadAuditStoreEF` class, registered per provider

#### Dapper (4 implementations)

1. **`ReadAuditStoreDapper`** in each Dapper provider's `Auditing/` folder:
   - `src/Encina.Dapper.Sqlite/Auditing/ReadAuditStoreDapper.cs`
   - `src/Encina.Dapper.SqlServer/Auditing/ReadAuditStoreDapper.cs`
   - `src/Encina.Dapper.PostgreSQL/Auditing/ReadAuditStoreDapper.cs`
   - `src/Encina.Dapper.MySQL/Auditing/ReadAuditStoreDapper.cs`
   - Constructor: `(IDbConnection connection, string tableName = "ReadAuditEntries", TimeProvider? timeProvider = null)`
   - SQL queries use provider-specific syntax (LIMIT vs TOP, parameter style)
   - SQLite: dates stored as ISO 8601 text, use parameterized `@NowUtc`

#### ADO.NET (4 implementations)

1. **`ReadAuditStoreADO`** in each ADO provider's `Auditing/` folder:
   - `src/Encina.ADO.Sqlite/Auditing/ReadAuditStoreADO.cs`
   - `src/Encina.ADO.SqlServer/Auditing/ReadAuditStoreADO.cs`
   - `src/Encina.ADO.PostgreSQL/Auditing/ReadAuditStoreADO.cs`
   - `src/Encina.ADO.MySQL/Auditing/ReadAuditStoreADO.cs`
   - Constructor: `(IDbConnection connection, string tableName = "ReadAuditEntries", TimeProvider? timeProvider = null)`
   - Manual `CreateCommand`, `AddParameter`, `ExecuteNonQueryAsync`, `ExecuteReaderAsync`
   - Manual mapping from `IDataReader`

#### MongoDB (1 implementation)

1. **`ReadAuditStoreMongoDB`** in `src/Encina.MongoDB/Auditing/ReadAuditStoreMongoDB.cs`:
   - Constructor: `(IMongoClient mongoClient, IOptions<EncinaMongoDbOptions> options, ILogger<ReadAuditStoreMongoDB> logger, TimeProvider? timeProvider = null)`
   - Collection: `ReadAuditEntries`
   - Uses `Builders<ReadAuditEntryEntity>.Filter` for queries
   - `InsertOneAsync`, `Find().ToListAsync()`, `DeleteManyAsync`

#### DI Registration

1. **Update `ServiceCollectionExtensions.cs`** in each satellite provider:
   - Register `IReadAuditStore` → provider-specific implementation
   - Conditional on `config.UseReadAudit` flag (add flag to `MessagingConfiguration`)

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
You are implementing Phase 6 of Read Auditing (Issue #573).

CONTEXT:
- Phases 1-5 are implemented (models, interfaces, decorators, DI, persistence entity, SQL scripts)
- IReadAuditStore interface defined in src/Encina.Security.Audit/Abstractions/IReadAuditStore.cs
- ReadAuditEntryEntity defined in src/Encina.Security.Audit/ReadAuditEntryEntity.cs
- ReadAuditEntryMapper defined in src/Encina.Security.Audit/ReadAuditEntryMapper.cs
- Must implement across ALL 13 database providers

TASK:
Create IReadAuditStore implementations for:
1. EF Core: src/Encina.EntityFrameworkCore/Auditing/ReadAuditStoreEF.cs
2. Dapper (×4): src/Encina.Dapper.{Sqlite,SqlServer,PostgreSQL,MySQL}/Auditing/ReadAuditStoreDapper.cs
3. ADO.NET (×4): src/Encina.ADO.{Sqlite,SqlServer,PostgreSQL,MySQL}/Auditing/ReadAuditStoreADO.cs
4. MongoDB: src/Encina.MongoDB/Auditing/ReadAuditStoreMongoDB.cs
5. Update ServiceCollectionExtensions.cs in each satellite provider

KEY RULES:
- All methods return ValueTask<Either<EncinaError, T>> (ROP pattern)
- Use ReadAuditEntryMapper for domain ↔ entity conversion
- SQLITE: Never use datetime('now') — always parameterized @NowUtc with DateTime.UtcNow
- SQLITE: Dates stored as ISO 8601 TEXT via .ToString("O")
- SQL SERVER: Use DATETIME2(7), TOP(@n) for limiting, [dbo].[ReadAuditEntries]
- POSTGRESQL: Use TIMESTAMPTZ, LIMIT @n
- MYSQL: Use DATETIME(6), LIMIT @n, backtick identifiers
- MongoDB: Use Builders<T>.Filter/Update pattern
- PurgeEntriesAsync uses bulk delete (ExecuteDeleteAsync for EF, DELETE WHERE for ADO/Dapper)
- TryAdd/TryAddScoped for store registrations in satellite DI

REFERENCE FILES:
- src/Encina.EntityFrameworkCore/Auditing/AuditStoreEF.cs (EF Core store pattern)
- src/Encina.Dapper.Sqlite/Auditing/AuditStoreDapper.cs (Dapper SQLite pattern)
- src/Encina.ADO.Sqlite/Auditing/AuditStoreADO.cs (ADO SQLite pattern)
- src/Encina.MongoDB/Auditing/AuditStoreMongoDB.cs (MongoDB store pattern)
- src/Encina.Dapper.SqlServer/Auditing/AuditStoreDapper.cs (Dapper SQL Server pattern)
```

</details>

---

### Phase 7: Observability

> **Goal**: Add ActivitySource, Meter, and LoggerMessage diagnostics for read auditing.

<details>
<summary><strong>Tasks</strong></summary>

1. **`ReadAuditActivitySource`** in `src/Encina.Security.Audit/Diagnostics/ReadAuditActivitySource.cs`:
   - `ActivitySource` named `"Encina.ReadAudit"` with version `"1.0"`
   - Activities:
     - `StartLogRead(string entityType, string entityId)` — span for audit write
     - `StartQueryAccessHistory(string entityType)` — span for access history queries
   - Guard with `HasListeners()` for zero-cost when disabled
   - Tags: `read_audit.entity_type`, `read_audit.entity_id`, `read_audit.user_id`, `read_audit.access_method`

2. **`ReadAuditMeter`** in `src/Encina.Security.Audit/Diagnostics/ReadAuditMeter.cs`:
   - `Meter` named `"Encina.ReadAudit"` with version `"1.0"`
   - Counters:
     - `encina.read_audit.entries_logged_total` — Counter<long>, tagged by `entity_type`, `access_method`
     - `encina.read_audit.entries_sampled_out_total` — Counter<long>, tagged by `entity_type` (reads skipped due to sampling)
     - `encina.read_audit.entries_purged_total` — Counter<long>
   - Histograms:
     - `encina.read_audit.query_duration_seconds` — Histogram<double>, tagged by `query_type`

3. **`ReadAuditLog`** in `src/Encina.Security.Audit/Diagnostics/ReadAuditLog.cs`:
   - **EventId range: 1700-1799** (next available after Audit's 1600-1699)
   - Source-generated `[LoggerMessage]` methods:
     - 1700: `ReadAccessLogged` (Debug) — "{EntityType}/{EntityId} accessed by {UserId}"
     - 1701: `ReadAccessSampledOut` (Trace) — "{EntityType} read access skipped (sampling)"
     - 1702: `ReadAuditStoreFailed` (Warning) — "Failed to log read access for {EntityType}: {ErrorMessage}"
     - 1703: `ReadAuditPurgeStarted` (Debug) — "Starting read audit purge for entries older than {CutoffDate}"
     - 1704: `ReadAuditPurgeCompleted` (Information) — "Purged {Count} read audit entries"
     - 1705: `ReadAuditPurgeFailed` (Warning) — "Read audit purge failed: {ErrorMessage}"
     - 1706: `ReadAuditDecoratorApplied` (Debug) — "Read audit decorator applied for {EntityType}"
     - 1707: `ReadAuditPurposeRequired` (Warning) — "Purpose required but not provided for {EntityType} access"
     - 1708: `ReadAuditRetentionServiceStarted` (Information) — "Read audit retention service started"
     - 1709: `ReadAuditRetentionServiceStopped` (Information) — "Read audit retention service stopped"

4. **Update `PublicAPI.Unshipped.txt`**

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
You are implementing Phase 7 of Read Auditing (Issue #573).

CONTEXT:
- Phases 1-6 are implemented (full functionality)
- EventId range 1600-1699 is used by Audit; use 1700-1799 for Read Audit
- Follow existing diagnostic patterns from Encina.Security.Audit and Encina.DomainModeling

TASK:
1. Create ReadAuditActivitySource in src/Encina.Security.Audit/Diagnostics/ReadAuditActivitySource.cs
2. Create ReadAuditMeter in src/Encina.Security.Audit/Diagnostics/ReadAuditMeter.cs
3. Create ReadAuditLog in src/Encina.Security.Audit/Diagnostics/ReadAuditLog.cs
4. Wire diagnostics into AuditedRepository and ReadAuditRetentionService
5. Update PublicAPI.Unshipped.txt

KEY RULES:
- ActivitySource named "Encina.ReadAudit" with version "1.0"
- Guard with HasListeners() for zero-cost when disabled
- Meter named "Encina.ReadAudit" with version "1.0"
- Counter/Histogram with tags (entity_type, access_method, query_type)
- [LoggerMessage] source generator with EventId range 1700-1799
- [ExcludeFromCodeCoverage] on partial Log class
- All log messages use structured logging ({Property} format)

REFERENCE FILES:
- src/Encina.DomainModeling/Diagnostics/AuditLog.cs (LoggerMessage pattern)
- src/Encina.DomainModeling/Diagnostics/AuditActivitySource.cs (ActivitySource pattern)
- src/Encina.Compliance.BreachNotification/Diagnostics/ (Meter pattern)
```

</details>

---

### Phase 8: Testing & Documentation

> **Goal**: Comprehensive test coverage and complete documentation.

<details>
<summary><strong>Tasks</strong></summary>

#### Unit Tests (`tests/Encina.UnitTests/Security/Audit/ReadAudit/`)

1. **`ReadAuditEntryTests.cs`** — record creation, property values, metadata
2. **`ReadAuditOptionsTests.cs`** — entity registration, sampling rates, system access exclusion
3. **`ReadAuditQueryTests.cs`** — builder pattern, filter combinations
4. **`InMemoryReadAuditStoreTests.cs`** — all IReadAuditStore methods, thread safety, purge
5. **`AuditedRepositoryTests.cs`** — decorator behavior:
   - Logs read for GetByIdAsync when entity implements IReadAuditable
   - Logs read with correct EntityCount for FindAsync
   - Delegates write operations without auditing
   - Respects sampling rate
   - Skips audit when ExcludeSystemAccess and no UserId
   - Continues on audit store failure (fire-and-forget resilience)
   - Populates Purpose from IReadAuditContext
6. **`AuditedReadOnlyRepositoryTests.cs`** — same but for read-only variant
7. **`ReadAuditRetentionServiceTests.cs`** — background service lifecycle, purge behavior
8. **`ReadAuditStoreHealthCheckTests.cs`** — health check behavior
9. **`ReadAuditEntryMapperTests.cs`** — entity ↔ domain round-trip
10. **`ReadAuditErrorsTests.cs`** — error factory methods
11. **`SensitiveDataAccessedNotificationTests.cs`** — notification record

#### Guard Tests (`tests/Encina.GuardTests/Security/Audit/ReadAudit/`)

1. **Guard tests** for all public constructors and methods — `ArgumentNullException` validation

#### Contract Tests (`tests/Encina.ContractTests/Security/Audit/ReadAudit/`)

1. **`IReadAuditStoreContractTests.cs`** — verify all 13 implementations follow the same contract

#### Property Tests (`tests/Encina.PropertyTests/Security/Audit/ReadAudit/`)

1. **`ReadAuditEntryPropertyTests.cs`** — FsCheck: log-then-query round-trip, purge-never-returns-deleted

#### Integration Tests (`tests/Encina.IntegrationTests/Security/Audit/ReadAudit/`)

1. **Integration tests** per provider using `[Collection]` fixtures:
    - `ReadAuditStoreEFIntegrationTests.cs` — [Collection("EFCore-SqlServer")]
    - `ReadAuditStoreDapperSqliteIntegrationTests.cs` — [Collection("Dapper-Sqlite")]
    - `ReadAuditStoreDapperSqlServerIntegrationTests.cs` — [Collection("Dapper-SqlServer")]
    - `ReadAuditStoreADOSqliteIntegrationTests.cs` — [Collection("ADO-Sqlite")]
    - `ReadAuditStoreADOSqlServerIntegrationTests.cs` — [Collection("ADO-SqlServer")]
    - (Additional per-provider as needed)

#### Load Tests

1. **Justification `.md`** in `tests/Encina.LoadTests/Security/Audit/ReadAudit/ReadAudit.md`:
    - Read auditing is fire-and-forget; the decorator delegates to async audit write
    - Load characteristics defined by the underlying repository, not the decorator
    - Performance overhead is minimal (one async fire-and-forget per read)

#### Benchmark Tests

1. **Justification `.md`** in `tests/Encina.BenchmarkTests/Encina.Benchmarks/Security/Audit/ReadAudit/ReadAudit.md`:
    - Decorator overhead is a single method call + async fire-and-forget
    - Not a hot path — audit write is I/O bound, not CPU bound
    - Benchmarking the decorator would measure I/O latency, not algorithmic performance

#### Documentation

1. **`docs/features/read-auditing.md`** — feature guide:
    - Overview and motivation (HIPAA, GDPR, SOX, PCI-DSS)
    - Getting started with code examples
    - Configuration options (sampling, system access, purpose tracking)
    - Integration with existing audit infrastructure
    - Provider-specific setup
    - Performance considerations

2. **Update `CHANGELOG.md`** — add entry under `### Added` in Unreleased section

3. **Update `ROADMAP.md`** — mark read auditing as implemented if listed

4. **Update `docs/INVENTORY.md`** — add new files/modules

5. **Update package README** — `src/Encina.Security.Audit/README.md` if exists

6. **XML doc comments** — verify all public APIs have `<summary>`, `<remarks>`, `<param>`, `<returns>`, `<example>`

7. **Build verification**: `dotnet build --configuration Release` → 0 errors, 0 warnings

8. **Test verification**: `dotnet test` → all pass

9. **Update `PublicAPI.Unshipped.txt`** — ensure all public symbols tracked

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```
You are implementing Phase 8 of Read Auditing (Issue #573).

CONTEXT:
- Phases 1-7 are fully implemented (models, interfaces, decorators, DI, persistence, stores, diagnostics)
- Testing standards: ≥85% line coverage, AAA pattern, descriptive names
- Integration tests use [Collection("Provider-DB")] shared fixtures
- Guard tests verify ArgumentNullException for all public parameters
- Contract tests verify all 13 implementations follow same interface contract

TASK:
1. Create unit tests in tests/Encina.UnitTests/Security/Audit/ReadAudit/
2. Create guard tests in tests/Encina.GuardTests/Security/Audit/ReadAudit/
3. Create contract tests in tests/Encina.ContractTests/Security/Audit/ReadAudit/
4. Create property tests in tests/Encina.PropertyTests/Security/Audit/ReadAudit/
5. Create integration tests in tests/Encina.IntegrationTests/Security/Audit/ReadAudit/
6. Create load test justification .md
7. Create benchmark test justification .md
8. Create docs/features/read-auditing.md
9. Update CHANGELOG.md, ROADMAP.md, docs/INVENTORY.md
10. Verify build and tests pass

KEY RULES:
- SQLite tests: [Collection("ADO-Sqlite")] / [Collection("Dapper-Sqlite")] — DisableParallelization = true
- SQLite: NEVER dispose shared connection from SqliteFixture
- Integration tests: ClearAllDataAsync() in InitializeAsync(), never DisposeAsync() on fixture
- Unit tests: mock IReadAuditStore, IRequestContext, IReadAuditContext with NSubstitute or Moq
- Fire-and-forget resilience: test that decorator continues when audit store throws
- Sampling: test that decorator respects sampling rate
- All test outputs go to artifacts/ directory
- Contract tests: verify all providers return same results for same inputs

REFERENCE FILES:
- tests/Encina.UnitTests/Security/Audit/ (existing audit test patterns)
- tests/Encina.IntegrationTests/ (integration test patterns with [Collection])
- tests/Encina.GuardTests/ (guard test patterns)
- tests/Encina.ContractTests/ (contract test patterns)
- tests/Encina.PropertyTests/ (FsCheck property test patterns)
```

</details>

---

## Research

### Regulatory Standards Covered

| Standard | Article/Section | Requirement | Coverage |
|----------|----------------|-------------|----------|
| **GDPR** | Art. 15 | Right of access — data subject can request log of who accessed their data | `ReadAuditEntry.Purpose`, `GetUserAccessHistoryAsync` |
| **GDPR** | Art. 5(2) | Accountability — demonstrate compliance with data protection principles | Complete audit trail of read access |
| **HIPAA** | §164.312(b) | Audit controls — record and examine activity in systems with ePHI | `IReadAuditable` on health records |
| **SOX** | §302, §404 | Internal controls — track access to financial data | `ReadAuditEntry` with `UserId`, `AccessedAtUtc` |
| **PCI-DSS** | Req. 10.2 | Logging — track access to cardholder data | `EntityType`, `EntityId`, `CorrelationId` |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in Read Auditing |
|-----------|----------|----------------------|
| `IRequestContext` | `src/Encina/Abstractions/IRequestContext.cs` | UserId, TenantId, CorrelationId for entries |
| `IAuditStore` | `src/Encina.Security.Audit/Abstractions/IAuditStore.cs` | Pattern reference for `IReadAuditStore` |
| `AuditedSecretReaderDecorator` | `src/Encina.Security.Secrets/Auditing/` | Decorator pattern reference |
| `IRepository<TEntity, TId>` | `src/Encina.DomainModeling/Repository.cs` | Decorated interface |
| `IReadOnlyRepository<TEntity, TId>` | `src/Encina.DomainModeling/Repository.cs` | Decorated interface |
| `InMemoryAuditStore` | `src/Encina.Security.Audit/InMemoryAuditStore.cs` | Pattern for `InMemoryReadAuditStore` |
| `AuditRetentionService` | `src/Encina.Security.Audit/AuditRetentionService.cs` | Pattern for `ReadAuditRetentionService` |
| `AuditStoreHealthCheck` | `src/Encina.Security.Audit/Health/` | Pattern for `ReadAuditStoreHealthCheck` |
| `PagedResult<T>` | `src/Encina.Security.Audit/PagedResult.cs` | Reused for query pagination |
| `AuditQuery` | `src/Encina.Security.Audit/AuditQuery.cs` | Pattern for `ReadAuditQuery` |
| `INotification` | `src/Encina/Abstractions/INotification.cs` | For `SensitiveDataAccessedNotification` |
| `TimeProvider` | .NET 10 built-in | Testable timestamp generation |

### EventId Allocation

| Package | Range | Notes |
|---------|-------|-------|
| Audit (existing) | 1600-1699 | Retention service, CUD auditing |
| **Read Audit (new)** | **1700-1799** | Read access logging, purge, decorator |
| Tenancy | 1800-1899 | Already allocated |

### Estimated File Count

| Category | Count | Details |
|----------|-------|---------|
| Core models & enums | 5 | IReadAuditable, ReadAuditEntry, ReadAccessMethod, ReadAuditQuery, ReadAuditEntryEntity |
| Core interfaces | 3 | IReadAuditStore, IReadAuditContext, ReadAuditErrors |
| Core implementations | 6 | InMemoryReadAuditStore, ReadAuditOptions, AuditedRepository, AuditedReadOnlyRepository, ReadAuditContext, ReadAuditEntryMapper |
| DI & Services | 3 | ServiceCollectionExtensions update, ReadAuditRetentionService, ReadAuditStoreHealthCheck |
| Notifications | 1 | SensitiveDataAccessedNotification |
| Diagnostics | 3 | ReadAuditActivitySource, ReadAuditMeter, ReadAuditLog |
| SQL Scripts | 16 | 8 ADO/Dapper × 2 (create + update AllTables) |
| EF Core | 2 | ReadAuditStoreEF, ReadAuditEntryEntityConfiguration |
| Provider stores (ADO ×4) | 4 | ReadAuditStoreADO per provider |
| Provider stores (Dapper ×4) | 4 | ReadAuditStoreDapper per provider |
| Provider stores (MongoDB) | 1 | ReadAuditStoreMongoDB |
| Provider DI updates | 9 | ServiceCollectionExtensions.cs updates |
| Unit tests | ~11 | Core functionality tests |
| Guard tests | ~5 | Parameter validation tests |
| Contract tests | 1 | All-providers contract test |
| Property tests | 1 | FsCheck round-trip tests |
| Integration tests | ~6 | Per-provider integration tests |
| Documentation | 4 | Feature guide, CHANGELOG, ROADMAP, INVENTORY |
| Justification docs | 2 | Load + Benchmark justification |
| **Total** | **~80** | |

---

## Combined AI Agent Prompt

<details>
<summary><strong>Complete Implementation Prompt (All Phases)</strong></summary>

```
You are implementing Read Auditing — Track Data Access for Sensitive Entities (Issue #573) for the Encina .NET 10 library.

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library for CQRS, messaging, and domain modeling
- Uses Railway Oriented Programming (Either<EncinaError, T>) for all store/handler methods
- Pre-1.0: no backward compatibility, always choose the best solution
- All features opt-in via configuration
- Database features must support ALL 13 providers: ADO.NET (Sqlite, SqlServer, PostgreSQL, MySQL), Dapper (same 4), EF Core (same 4), MongoDB
- Repository pattern is optional — IRepository<TEntity, TId> from Encina.DomainModeling

IMPLEMENTATION OVERVIEW:
This feature adds read access auditing at the repository layer via the decorator pattern.

Core types (in Encina.Security.Audit):
- IReadAuditable (marker interface in Encina.DomainModeling)
- ReadAuditEntry (sealed record with EntityType, EntityId, UserId, Purpose, AccessMethod, etc.)
- ReadAccessMethod (enum: Repository, DirectQuery, Api, Export, Custom)
- IReadAuditStore (store interface: LogReadAsync, GetAccessHistoryAsync, GetUserAccessHistoryAsync, QueryAsync, PurgeEntriesAsync)
- ReadAuditOptions (fluent config: AuditReadsFor<T>(), sampling, system access exclusion)
- AuditedRepository<TEntity, TId> (decorator wrapping IRepository, intercepting read methods)
- AuditedReadOnlyRepository<TEntity, TId> (decorator wrapping IReadOnlyRepository)
- IReadAuditContext / ReadAuditContext (scoped purpose tracking)

Persistence:
- ReadAuditEntryEntity (POCO for database)
- ReadAuditEntryMapper (domain ↔ entity conversion)
- ReadAuditEntries table with indexes (entity, user, timestamp)
- 13 store implementations: ReadAuditStoreEF, ReadAuditStoreDapper (×4), ReadAuditStoreADO (×4), ReadAuditStoreMongoDB

DI Registration:
- AddEncinaReadAuditing(options => { ... }) extension method
- Automatic repository decoration for IReadAuditable entities
- TryAddSingleton for IReadAuditStore → InMemoryReadAuditStore (default)

Observability:
- ActivitySource "Encina.ReadAudit" (v1.0)
- Meter "Encina.ReadAudit" with counters and histograms
- [LoggerMessage] EventId range: 1700-1799

KEY PATTERNS:
1. DECORATOR: Follow AuditedSecretReaderDecorator pattern — fire-and-forget audit, never block reads
2. STORE: Follow IAuditStore/AuditStoreEF pattern — Either<EncinaError, T> returns, ROP
3. DI: Follow AddEncinaAudit() — TryAdd for defaults, Configure<T> for options
4. SQL: Provider-specific scripts — SQLite TEXT dates, SQL Server DATETIME2, etc.
5. TESTING: Unit + Guard + Contract + Property + Integration, ≥85% coverage
6. SQLITE: Never datetime('now'), never dispose shared connection, ISO 8601 dates

REFERENCE FILES:
- src/Encina.Security.Audit/ — all existing audit infrastructure
- src/Encina.Security.Secrets/Auditing/AuditedSecretReaderDecorator.cs — decorator pattern
- src/Encina.DomainModeling/Repository.cs — IRepository interfaces
- src/Encina/Abstractions/IRequestContext.cs — request context
- src/Encina.EntityFrameworkCore/Auditing/ — EF Core store pattern
- src/Encina.Dapper.Sqlite/Auditing/ — Dapper SQLite store pattern
- src/Encina.ADO.Sqlite/Auditing/ — ADO SQLite store pattern
- src/Encina.MongoDB/Auditing/ — MongoDB store pattern
- src/Encina.Compliance.BreachNotification/ — complete satellite package reference

PHASES:
1. Core models (IReadAuditable, ReadAuditEntry, ReadAccessMethod, ReadAuditQuery)
2. Core interfaces (IReadAuditStore, InMemoryReadAuditStore, ReadAuditOptions, ReadAuditErrors)
3. Repository decorators (AuditedRepository, AuditedReadOnlyRepository, IReadAuditContext)
4. DI & services (AddEncinaReadAuditing, ReadAuditRetentionService, HealthCheck)
5. Persistence (ReadAuditEntryEntity, Mapper, SQL scripts for all 8 ADO/Dapper providers, EF Core config)
6. Multi-provider stores (13 implementations: EF ×4, Dapper ×4, ADO ×4, MongoDB ×1)
7. Observability (ActivitySource, Meter, LoggerMessage 1700-1799)
8. Testing & documentation (Unit, Guard, Contract, Property, Integration, docs)
```

</details>

---

## Next Steps

1. **Review** this plan for design decisions and completeness
2. **Publish** as comment on Issue #573
3. **Implement** phase by phase (phases 1-4 are core, 5-6 are provider implementations, 7-8 are observability and testing)
4. **Final commit** with `Fixes #573` in the message
