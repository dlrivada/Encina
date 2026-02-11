# v0.12.0 - Database & Repository

> **Release Date**: In Progress
> **Milestone**: [v0.12.0 - Database & Repository](https://github.com/dlrivada/Encina/milestone/9)
> **Status**: In Progress (22 issues)

This document captures the detailed implementation history for v0.12.0 (February 2026).

## Milestone Overview

v0.12.0 focuses on database and repository patterns, completing the data access layer for production-ready applications.

### Issues in Milestone

| Issue | Feature | Status |
|-------|---------|--------|
| #279 | Generic Repository | Completado |
| #280 | Specification Pattern | Completado |
| #281 | Unit of Work | Completado |
| #282 | Multi-Tenancy | Completado |
| #283 | Read/Write Separation | Pendiente |
| #284 | Bulk Operations | Completado (v0.11.0) |
| #285 | Soft Delete | Pendiente |
| #286 | Audit Trail | Pendiente |
| #287 | Optimistic Concurrency | Completado |
| #288 | CDC Integration | **Completado** |
| #289 | Sharding | **Completado** |
| #290 | Connection Pool Resilience | **Completado** |
| #291 | Query Cache | **Completado** |
| #292 | Domain Entity Base | Completado |
| #293 | Pagination Abstractions | **Completado** |
| #294 | Cursor Pagination | Pendiente |
| #534 | Module Isolation | Completado |

---

## Week of February 10, 2026

### February 10 - Database Sharding (#289)

**Issue**: [#289 - Database Sharding](https://github.com/dlrivada/Encina/issues/289)

Implemented comprehensive database sharding with four routing strategies, scatter-gather query execution, and MongoDB dual-mode support across all 13 database providers.

#### Phases 1-9: Core Implementation & Testing

**Files Created**:

**Encina** (core abstractions):

- `src/Encina/Sharding/IShardable.cs` - Entity shard key interface
- `src/Encina/Sharding/ShardKeyAttribute.cs` - Attribute-based shard key extraction
- `src/Encina/Sharding/ShardKeyExtractor.cs` - Cached reflection extraction
- `src/Encina/Sharding/EntityShardRouter.cs` - Combined extraction + routing
- `src/Encina/Sharding/ShardTopology.cs` - Immutable shard configuration
- `src/Encina/Sharding/ShardInfo.cs` - Shard connection metadata
- `src/Encina/Sharding/Routing/IShardRouter.cs` - Core routing abstraction
- `src/Encina/Sharding/Routing/HashShardRouter.cs` - xxHash64 + consistent hashing (150 virtual nodes)
- `src/Encina/Sharding/Routing/RangeShardRouter.cs` - Sorted boundary binary search
- `src/Encina/Sharding/Routing/DirectoryShardRouter.cs` - Key-to-shard lookup
- `src/Encina/Sharding/Routing/GeoShardRouter.cs` - Region-based with fallback chains
- `src/Encina/Sharding/Routing/IShardRebalancer.cs` - Topology change planning
- `src/Encina/Sharding/Routing/IShardDirectoryStore.cs` - Pluggable directory backend
- `src/Encina/Sharding/Query/IShardedQueryExecutor.cs` - Scatter-gather engine
- `src/Encina/Sharding/Query/ScatterGatherOptions.cs` - Query configuration
- `src/Encina/Sharding/Query/ShardedQueryResult.cs` - Results with partial failure tracking
- `src/Encina/Sharding/Health/ShardHealthResult.cs` - Three-state health model
- `src/Encina/Sharding/Health/ShardedHealthSummary.cs` - Aggregate health
- `src/Encina/Sharding/Diagnostics/ShardingMetrics.cs` - 7 OpenTelemetry instruments
- `src/Encina/Sharding/Diagnostics/ShardingTracing.cs` - 3 trace activities
- `src/Encina/Sharding/ShardingErrorCodes.cs` - 13 stable error codes

**Provider Factories** (13 providers):

- `src/Encina.ADO.Sqlite/Sharding/` - SQLite sharded connection factory
- `src/Encina.ADO.SqlServer/Sharding/` - SQL Server sharded connection factory
- `src/Encina.ADO.PostgreSQL/Sharding/` - PostgreSQL sharded connection factory
- `src/Encina.ADO.MySQL/Sharding/` - MySQL sharded connection factory
- `src/Encina.Dapper.Sqlite/Sharding/` - Reuses ADO factory
- `src/Encina.Dapper.SqlServer/Sharding/` - Reuses ADO factory
- `src/Encina.Dapper.PostgreSQL/Sharding/` - Reuses ADO factory
- `src/Encina.Dapper.MySQL/Sharding/` - Reuses ADO factory
- `src/Encina.EntityFrameworkCore.Sqlite/Sharding/` - Sharded DbContext factory
- `src/Encina.EntityFrameworkCore.SqlServer/Sharding/` - Sharded DbContext factory
- `src/Encina.EntityFrameworkCore.PostgreSQL/Sharding/` - Sharded DbContext factory
- `src/Encina.EntityFrameworkCore.MySQL/Sharding/` - Sharded DbContext factory
- `src/Encina.MongoDB/Sharding/` - Dual-mode (native mongos + app-level)

#### Phase 9: Testing (~680+ tests)

| Test Type | Count | Location |
|-----------|-------|----------|
| Unit Tests | ~300 | `tests/Encina.UnitTests/Sharding/` |
| Guard Tests | ~120 | `tests/Encina.GuardTests/Sharding/` |
| Contract Tests | ~80 | `tests/Encina.ContractTests/Sharding/` |
| Property Tests | ~100 | `tests/Encina.PropertyTests/Sharding/` |
| Integration Tests | ~80 | `tests/Encina.IntegrationTests/Sharding/` |
| Benchmarks | 13 | `tests/Encina.BenchmarkTests/Sharding/` |
| **Total** | **~680+** | |

#### Phase 10: Documentation (5 guides + 1 ADR)

- `docs/architecture/adr/010-database-sharding.md` — Architecture Decision Record
- `docs/sharding/configuration.md` — Complete configuration reference (~25KB)
- `docs/sharding/scaling-guidance.md` — Shard key selection, capacity planning, rebalancing (~16KB)
- `docs/sharding/mongodb.md` — MongoDB dual-mode (native vs app-level) (~15KB)
- `docs/sharding/cross-shard-operations.md` — Scatter-gather, Saga pattern, partial failures (~20KB)
- Enhanced XML documentation in 8 source files with `<remarks>` and `<example>` tags

#### Key Design Decisions

- **Provider-agnostic**: Same `IShardRouter` + `ShardTopology` abstraction across all 13 providers
- **Dapper reuses ADO**: Zero extra factory classes — Dapper sharding requires ADO registration first
- **MongoDB dual-mode**: `UseNativeSharding` flag for native mongos (production) vs app-level routing (dev/test)
- **No cross-shard 2PC**: Use Saga pattern from Encina.Messaging for distributed workflows
- **Sub-microsecond routing**: Pre-computed ring (Hash), sorted arrays (Range), O(1) dictionary (Directory)
- **Observable**: Full OpenTelemetry integration with `HasListeners()` guards for zero-cost when disabled

---

## Week of February 9, 2026

### February 9 - Change Data Capture (#288/#308)

**Issues**: [#288 - CDC Integration](https://github.com/dlrivada/Encina/issues/288), [#308 - CDC Pattern](https://github.com/dlrivada/Encina/issues/308)

Implemented provider-agnostic Change Data Capture (CDC) with support for 5 database platforms plus Debezium. Includes messaging integration (CdcMessagingBridge, OutboxCdcHandler) and comprehensive documentation.

#### Phases 1-5: Core Implementation

**Files Created**:

**Encina.Cdc** (core abstractions + processing):

- `src/Encina.Cdc/Abstractions/ICdcConnector.cs` - Provider abstraction
- `src/Encina.Cdc/Abstractions/IChangeEventHandler.cs` - Typed entity handler
- `src/Encina.Cdc/Abstractions/ICdcDispatcher.cs` - Event routing
- `src/Encina.Cdc/Abstractions/ICdcPositionStore.cs` - Position persistence
- `src/Encina.Cdc/Abstractions/CdcPosition.cs` - Abstract position
- `src/Encina.Cdc/ChangeEvent.cs`, `ChangeContext.cs`, `ChangeMetadata.cs`, `ChangeOperation.cs`
- `src/Encina.Cdc/CdcOptions.cs`, `CdcConfiguration.cs` - Fluent builder API
- `src/Encina.Cdc/Processing/CdcProcessor.cs` - BackgroundService
- `src/Encina.Cdc/Processing/CdcDispatcher.cs` - Handler routing
- `src/Encina.Cdc/Messaging/CdcMessagingBridge.cs` - Publishes CdcChangeNotification
- `src/Encina.Cdc/Messaging/OutboxCdcHandler.cs` - CDC-driven outbox processing
- `src/Encina.Cdc/Messaging/CdcChangeNotification.cs` - INotification record
- `src/Encina.Cdc/Health/CdcHealthCheck.cs` - Health check

**Provider Packages** (5 connectors):

- `src/Encina.Cdc.SqlServer/` - SQL Server Change Tracking
- `src/Encina.Cdc.PostgreSql/` - PostgreSQL Logical Replication (WAL)
- `src/Encina.Cdc.MySql/` - MySQL Binary Log (GTID + binlog position)
- `src/Encina.Cdc.MongoDb/` - MongoDB Change Streams (resume token)
- `src/Encina.Cdc.Debezium/` - Debezium HTTP Consumer + Kafka Consumer (CloudEvents/Flat)

**Debezium Kafka Consumer Integration** (Phase 2):

- `src/Encina.Cdc.Debezium/Kafka/DebeziumKafkaConnector.cs` - Kafka consumer connector
- `src/Encina.Cdc.Debezium/Kafka/DebeziumKafkaOptions.cs` - Kafka configuration (12 properties)
- `src/Encina.Cdc.Debezium/Kafka/DebeziumKafkaPosition.cs` - Topic/partition/offset position
- `src/Encina.Cdc.Debezium/Kafka/DebeziumKafkaHealthCheck.cs` - Kafka health check

#### Phase 5: Testing (498+ tests)

- ~232 unit tests, ~60 integration tests, ~69 guard tests, ~71 contract tests, ~66 property tests
- Debezium-specific: ~143 tests (76 unit + 19 guard + 24 contract + 19 property + 5 integration)

#### Phase 6: Documentation (10 docs)

- `docs/features/cdc.md` - Main feature guide
- `docs/features/cdc-sqlserver.md`, `cdc-postgresql.md`, `cdc-mysql.md`, `cdc-mongodb.md`, `cdc-debezium.md` - Provider docs
- `docs/examples/cdc-basic-setup.md`, `cdc-outbox-integration.md`, `cdc-messaging-bridge.md`, `cdc-position-tracking.md`

#### Key Design Decisions

- **Provider-agnostic**: `ICdcConnector` abstraction allows swapping database CDC mechanisms
- **Position tracking**: `ICdcPositionStore` enables resume from last position on restart
- **Messaging bridge**: `CdcMessagingBridge` publishes changes as `INotification` via `IEncina.Publish()`
- **Outbox replacement**: `OutboxCdcHandler` provides near-real-time alternative to polling
- **Fluent configuration**: `AddEncinaCdc()` with builder pattern for handlers, mappings, and options

---

## Week of February 8, 2026

### February 8 - Query Cache Interceptor (#291)

**Issue**: [#291 - Query Cache Interceptor](https://github.com/dlrivada/Encina/issues/291)

Implemented EF Core second-level query caching via `DbCommandInterceptor` + `ISaveChangesInterceptor`. The interceptor transparently caches query results and invalidates them on `SaveChanges`.

#### Phases 1-5: Core Implementation

**Files Created**:

**Encina.Caching** (abstractions):

- `src/Encina.Caching/Abstractions/IQueryCacheKeyGenerator.cs` - Interface for SQL command key generation
- `QueryCacheKey` record with `Key` and `EntityTypes` properties

**Encina.EntityFrameworkCore** (implementation):

- `src/Encina.EntityFrameworkCore/Caching/QueryCacheInterceptor.cs` - EF Core interceptor (658 lines)
- `src/Encina.EntityFrameworkCore/Caching/DefaultQueryCacheKeyGenerator.cs` - SHA256-based key generator
- `src/Encina.EntityFrameworkCore/Caching/CachedDataReader.cs` - DbDataReader for cached results (522 lines)
- `src/Encina.EntityFrameworkCore/Caching/CachedQueryResult.cs` - Serializable cached result model
- `src/Encina.EntityFrameworkCore/Caching/SqlTableExtractor.cs` - Compiled regex SQL table extraction
- `src/Encina.EntityFrameworkCore/Caching/QueryCacheOptions.cs` - Configuration options
- `src/Encina.EntityFrameworkCore/Extensions/QueryCachingExtensions.cs` - DI registration

**Encina.Messaging** (configuration bridge):

- `src/Encina.Messaging/Configuration/QueryCacheMessagingOptions.cs` - Messaging-level options

#### Phase 6: Testing (256 tests)

- 184 unit tests (`Encina.UnitTests/EntityFrameworkCore/Caching/` - 7 files)
- 19 guard tests (`Encina.GuardTests/EntityFrameworkCore/Caching/`)
- 29 contract tests (`Encina.ContractTests/Database/Caching/`)
- 16 property tests (`Encina.PropertyTests/Infrastructure/EntityFrameworkCore/Caching/`)
- 8 integration tests (`Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/Caching/`)
- 7 BenchmarkDotNet benchmarks (`Encina.Benchmarks/EntityFrameworkCore/`)
- Load test justification document (`Encina.LoadTests/EntityFrameworkCore/Caching/`)

#### Key Design Decisions

- **Two-step DI pattern**: `AddQueryCaching()` + `UseQueryCaching()` for explicit cache provider validation
- **Cache key format**: `{prefix}:{primaryEntity}:{hash}` or `{prefix}:{tenant}:{primaryEntity}:{hash}`
- **Entity-aware invalidation**: Cache entries track which entity types they involve for targeted invalidation
- **Error resilience**: `ThrowOnCacheErrors = false` by default — cache failures fall through to database
- **Works with all 8 cache providers**: Memory, Hybrid, Redis, Valkey, KeyDB, Dragonfly, Garnet, Memcached

---

### February 8 - Connection Pool Resilience (#290)

**Issue**: [#290 - Connection Pool Resilience](https://github.com/dlrivada/Encina/issues/290)

Implemented connection pool monitoring, database-aware circuit breakers, transient error detection, and connection warm-up across all 13 database providers.

#### Phase 1-5: Core Implementation

**Files Created**:

**Encina** (core abstractions):

- `src/Encina/Database/IDatabaseHealthMonitor.cs` - Core interface
- `src/Encina/Database/ConnectionPoolStats.cs` - Pool statistics record
- `src/Encina/Database/DatabaseHealthResult.cs` - Health check result + DatabaseHealthStatus enum
- `src/Encina/Database/DatabaseResilienceOptions.cs` - Resilience configuration
- `src/Encina/Database/DatabaseCircuitBreakerOptions.cs` - Circuit breaker settings

**Encina.Messaging** (base class):

- `src/Encina.Messaging/Health/DatabaseHealthMonitorBase.cs` - Abstract base for relational providers

**Encina.Polly** (circuit breaker):

- `src/Encina.Polly/Behaviors/DatabaseCircuitBreakerPipelineBehavior.cs` - Pipeline behavior
- `src/Encina.Polly/Predicates/DatabaseTransientErrorPredicate.cs` - Transient error detection

**ADO.NET Providers** (4 health monitors):

- `src/Encina.ADO.Sqlite/Health/SqliteDatabaseHealthMonitor.cs`
- `src/Encina.ADO.SqlServer/Health/SqlServerDatabaseHealthMonitor.cs`
- `src/Encina.ADO.PostgreSQL/Health/PostgreSqlDatabaseHealthMonitor.cs`
- `src/Encina.ADO.MySQL/Health/MySqlDatabaseHealthMonitor.cs`

**Dapper Providers** (4 health monitors):

- `src/Encina.Dapper.Sqlite/Health/DapperSqliteDatabaseHealthMonitor.cs`
- `src/Encina.Dapper.SqlServer/Health/DapperSqlServerDatabaseHealthMonitor.cs`
- `src/Encina.Dapper.PostgreSQL/Health/DapperPostgreSqlDatabaseHealthMonitor.cs`
- `src/Encina.Dapper.MySQL/Health/DapperMySqlDatabaseHealthMonitor.cs`

**EF Core**:

- `src/Encina.EntityFrameworkCore/Resilience/EfCoreDatabaseHealthMonitor.cs`
- `src/Encina.EntityFrameworkCore/Resilience/ConnectionPoolMonitoringInterceptor.cs`

**MongoDB**:

- `src/Encina.MongoDB/Health/MongoDbDatabaseHealthMonitor.cs`

**Files Modified** (service registration):

- `src/Encina.ADO.Sqlite/ServiceCollectionExtensions.cs` - Register `SqliteDatabaseHealthMonitor`
- `src/Encina.ADO.SqlServer/ServiceCollectionExtensions.cs` - Register `SqlServerDatabaseHealthMonitor`
- `src/Encina.ADO.PostgreSQL/ServiceCollectionExtensions.cs` - Register `PostgreSqlDatabaseHealthMonitor`
- `src/Encina.ADO.MySQL/ServiceCollectionExtensions.cs` - Register `MySqlDatabaseHealthMonitor`
- `src/Encina.Dapper.Sqlite/ServiceCollectionExtensions.cs` - Register `DapperSqliteDatabaseHealthMonitor`
- `src/Encina.Dapper.SqlServer/ServiceCollectionExtensions.cs` - Register `DapperSqlServerDatabaseHealthMonitor`
- `src/Encina.Dapper.PostgreSQL/ServiceCollectionExtensions.cs` - Register `DapperPostgreSqlDatabaseHealthMonitor`
- `src/Encina.Dapper.MySQL/ServiceCollectionExtensions.cs` - Register `DapperMySqlDatabaseHealthMonitor`
- `src/Encina.EntityFrameworkCore/ServiceCollectionExtensions.cs` - Register `EfCoreDatabaseHealthMonitor`
- `src/Encina.MongoDB/ServiceCollectionExtensions.cs` - Register `MongoDbDatabaseHealthMonitor`
- `src/Encina.Polly/ServiceCollectionExtensions.cs` - Register `DatabaseCircuitBreakerPipelineBehavior`

#### Phase 6: Testing

| Test Type | Count | Location |
|-----------|-------|----------|
| Unit Tests | 113 | `tests/Encina.UnitTests/Database/` |
| Guard Tests | 22 | `tests/Encina.GuardTests/Database/` |
| Contract Tests | 20 | `tests/Encina.ContractTests/Database/Resilience/` |
| Property Tests | 19 | `tests/Encina.PropertyTests/Database/Resilience/` |
| Integration Tests | 15 | `tests/Encina.IntegrationTests/ADO/Sqlite/Resilience/` + `Dapper/Sqlite/Resilience/` |
| Load Tests | Justified | `tests/Encina.LoadTests/Database/Resilience/Resilience.md` |
| Benchmark Tests | Justified | `tests/Encina.BenchmarkTests/Resilience.md` |
| **Total** | **189** | |

#### Phase 7: Documentation

- Updated `README.md` with database resilience feature
- Updated `ROADMAP.md` to mark #290 as completed
- Updated `CHANGELOG.md` with detailed entry
- Created `docs/features/database-resilience.md` comprehensive guide
- Updated `docs/INVENTORY.md` with applicability matrix entry
- Updated `PublicAPI.Unshipped.txt` for all affected packages

---

## Week of February 5, 2026

### February 5 - Pagination Abstractions (#293)

**Issue**: [#293 - Pagination Abstractions](https://github.com/dlrivada/Encina/issues/293)

Implemented comprehensive pagination abstractions for data access, including pagination options, paged results, and specification-based pagination.

#### Phase 1-3: Core Implementation

**Files Created/Modified**:

**Encina.DomainModeling**:

- `src/Encina.DomainModeling/PaginationOptions.cs` - Core pagination records
  - `PaginationOptions(PageNumber, PageSize)` with computed `Skip` property
  - `SortedPaginationOptions` extending with `SortBy` and `SortDescending`
  - Fluent builder methods: `WithPage()`, `WithSize()`, `WithSort()`
  - Validation: page/size must be >= 1, sortBy cannot be null/whitespace

- `src/Encina.DomainModeling/PagedResult.cs` - Paged result record
  - `PagedResult<T>` with `Items`, `PageNumber`, `PageSize`, `TotalCount`
  - Computed properties: `TotalPages`, `HasPreviousPage`, `HasNextPage`
  - Navigation helpers: `FirstItemIndex`, `LastItemIndex`, `IsFirstPage`, `IsLastPage`
  - `Map<TDestination>()` for functional projections
  - `Empty()` factory for empty results

- `src/Encina.DomainModeling/IPagedSpecification.cs` - Specification interface
  - `IPagedSpecification<T>` with `Pagination` property
  - `IPagedSpecification<T, TResult>` with `Selector` for projections

- `src/Encina.DomainModeling/PagedQuerySpecification.cs` - Base class
  - `PagedQuerySpecification<T>` implementing both interfaces
  - Constructor with `PaginationOptions` (null-check validation)
  - Access to all `QuerySpecification<T>` methods

**Encina.EntityFrameworkCore**:

- `src/Encina.EntityFrameworkCore/Extensions/QueryablePagedExtensions.cs`
  - `ToPagedResultAsync<T>()` - Basic pagination
  - `ToPagedResultAsync<T, TResult>()` - With projection expression
  - Efficient: single count query + paginated data query

- `src/Encina.EntityFrameworkCore/Repository/FunctionalRepositoryEF.cs` (Modified)
  - `GetPagedAsync(PaginationOptions)` - Basic pagination
  - `GetPagedAsync(Specification, PaginationOptions)` - With filter
  - `GetPagedAsync(IPagedSpecification)` - Full specification-based
  - All return `Either<EncinaError, PagedResult<T>>`
  - Fixed double-pagination bug when using IPagedSpecification

#### Phase 4: Comprehensive Testing

**Unit Tests** (221 tests):

- `tests/Encina.UnitTests/DomainModeling/Pagination/PaginationOptionsTests.cs` (43 tests)
  - Constructor and record behavior
  - Skip calculation
  - WithPage/WithSize builder methods
  - Default singleton

- `tests/Encina.UnitTests/DomainModeling/Pagination/SortedPaginationOptionsTests.cs` (38 tests)
  - Inheritance from PaginationOptions
  - Sorting properties
  - WithSort builder method
  - Type preservation in builders

- `tests/Encina.UnitTests/DomainModeling/Pagination/PagedResultTests.cs` (59 tests)
  - Computed properties (TotalPages, HasPrevious/NextPage)
  - Navigation indices (FirstItemIndex, LastItemIndex)
  - Edge cases (empty results, single page, last page)
  - Map() projection functionality
  - Empty() factory method

- `tests/Encina.UnitTests/DomainModeling/Pagination/PagedQuerySpecificationTests.cs` (16 tests)
  - Constructor validation
  - Pagination property access
  - Integration with QuerySpecification base class

- `tests/Encina.UnitTests/EntityFrameworkCore/Extensions/QueryablePagedExtensionsTests.cs` (39 tests)
  - ToPagedResultAsync without projection
  - ToPagedResultAsync with projection
  - Page navigation
  - Empty dataset handling
  - Large dataset pagination

- `tests/Encina.UnitTests/EntityFrameworkCore/Repository/FunctionalRepositoryEFPaginationTests.cs` (20 tests)
  - GetPagedAsync(PaginationOptions) overload
  - GetPagedAsync with predicate overload
  - GetPagedAsync with IPagedSpecification overload
  - ROP integration (Either return types)

**Guard Tests** (25 tests):

- `tests/Encina.GuardTests/DomainModeling/PaginationGuardTests.cs` (14 tests)
  - PaginationOptions.WithPage() throws for values < 1
  - PaginationOptions.WithSize() throws for values < 1
  - SortedPaginationOptions.WithSort() throws for null/whitespace
  - PagedResult.Map() throws for null selector
  - PagedQuerySpecification constructor throws for null pagination

- `tests/Encina.GuardTests/Infrastructure/EntityFrameworkCore/QueryablePagedExtensionsGuardTests.cs` (5 tests)
  - ToPagedResultAsync() null query validation
  - ToPagedResultAsync() null pagination validation
  - ToPagedResultAsync() with projection null validations

**Bug Fix**: Fixed double-pagination bug in `FunctionalRepositoryEF.GetPagedAsync(IPagedSpecification)`:

- SpecificationEvaluator was applying Skip/Take, then ToPagedResultAsync applied them again
- Solution: Build base query with filtering only, let ToPagedResultAsync handle pagination
- Added `ApplyOrderingForPaging()` helper method for correct ordering

#### Code Examples

**Basic Pagination**:

```csharp
// Using PaginationOptions
var options = PaginationOptions.Default
    .WithPage(2)
    .WithSize(25);

var result = await repository.GetPagedAsync(options);
// result: Either<EncinaError, PagedResult<Entity>>

result.Match(
    Right: pagedResult =>
    {
        Console.WriteLine($"Page {pagedResult.PageNumber} of {pagedResult.TotalPages}");
        Console.WriteLine($"Showing items {pagedResult.FirstItemIndex}-{pagedResult.LastItemIndex}");
        foreach (var item in pagedResult.Items) { /* ... */ }
    },
    Left: error => Console.WriteLine(error.Message)
);
```

**With Sorting**:

```csharp
var options = SortedPaginationOptions.Default
    .WithSort("CreatedAtUtc", descending: true)
    .WithPage(1)
    .WithSize(50);
```

**Using Specifications**:

```csharp
public class ActiveOrdersSpec : PagedQuerySpecification<Order>
{
    public ActiveOrdersSpec(PaginationOptions pagination) : base(pagination)
    {
        AddCriteria(o => o.Status == OrderStatus.Active);
        ApplyOrderByDescending(o => o.CreatedAtUtc);
    }
}

var spec = new ActiveOrdersSpec(PaginationOptions.Default.WithSize(20));
var result = await repository.GetPagedAsync(spec);
```

**EF Core Extensions**:

```csharp
// Direct IQueryable usage
var pagedResult = await dbContext.Orders
    .Where(o => o.IsActive)
    .OrderByDescending(o => o.CreatedAtUtc)
    .ToPagedResultAsync(new PaginationOptions(1, 25));

// With projection
var pagedDtos = await dbContext.Orders
    .Where(o => o.IsActive)
    .ToPagedResultAsync(
        o => new OrderDto(o.Id, o.Total),
        new PaginationOptions(1, 25));
```

---

## Quality Metrics

| Metric | Current | Target |
|--------|---------|--------|
| Pagination Unit Tests | 221 | - |
| Pagination Guard Tests | 25 | - |
| Build Warnings | 0 | 0 ✅ |
| Code Coverage | TBD | ≥85% |

---

## February 11 - TimeProvider Injection (#543)

**Issue**: [#543 - Replace DateTime.UtcNow with TimeProvider injection](https://github.com/dlrivada/Encina/issues/543)

Replaced all ~205 occurrences of `DateTime.UtcNow` across ~112 source files with `TimeProvider` injection for deterministic time control in tests.

### Pattern Applied

```csharp
// Constructor injection (optional parameter, defaults to system clock)
public SomeClass(..., TimeProvider? timeProvider = null)
{
    _timeProvider = timeProvider ?? TimeProvider.System;
}

// Usage
var now = _timeProvider.GetUtcNow().UtcDateTime;
```

### Scope

| Category | Files Modified | Occurrences |
|----------|---------------|-------------|
| Encina.Messaging | 13 | 34 |
| Encina.EntityFrameworkCore | 9 | 15 |
| Encina.MongoDB | 6 | 11 |
| ADO.NET (4 providers) | 12 | 12 |
| Dapper (4 providers) | 18 | 20+ |
| Caching + Distributed Lock | 7 | 18 |
| CDC Connectors | 5 | 9 |
| DomainModeling | 8 | 7 |
| Marten | 2 | 6 |
| Testing.* | 15 | 25 |
| Other (Redis PubSub, Security, Aspire) | ~10 | ~15 |
| **Total** | **~112** | **~205** |

### DI Registration

Added `services.TryAddSingleton(TimeProvider.System)` to all `ServiceCollectionExtensions`:

- `Encina.Messaging` (MessagingServiceCollectionExtensions)
- `Encina.EntityFrameworkCore` (ServiceCollectionExtensions)
- `Encina.MongoDB` (ServiceCollectionExtensions)
- `Encina.Caching.Redis` (ServiceCollectionExtensions)
- `Encina.DistributedLock.Redis` (ServiceCollectionExtensions)
- `Encina.DistributedLock.SqlServer` (ServiceCollectionExtensions)
- All 5 CDC provider `ServiceCollectionExtensions`

### Model Classes (Interface Contract)

ADO, Dapper, EF Core, and MongoDB `InboxMessage.IsExpired()` and `ScheduledMessage.IsDue()` use `TimeProvider.System.GetUtcNow().UtcDateTime` directly (parameterless interface contract from `IInboxMessage`/`IScheduledMessage`).

### PublicAPI Updates

Updated `PublicAPI.Unshipped.txt` for packages with changed public constructor signatures:

- `Encina.Caching` (2 constructors)
- `Encina.Caching.Redis` (2 constructors)
- `Encina.DistributedLock.Redis` (1 constructor)
- `Encina.DistributedLock.SqlServer` (1 constructor)

### Test Impact

- Contract test `QueryCachingContractTests` updated: expected 6 constructor parameters (was 5)
- All tests pass: Unit (10,373), Guard (1,191), Contract (422), Property (530)

---

## Next Steps

1. **Integration Tests**: Add real database tests for pagination (Docker/Testcontainers)
2. **Cursor Pagination** (#294): Research and implement keyset pagination
3. **API Documentation**: Add pagination examples to API docs
4. **ASP.NET Core Integration**: Add controller helpers for pagination
