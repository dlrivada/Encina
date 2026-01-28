# EntityFrameworkCore Benchmarks

Comprehensive BenchmarkDotNet benchmarks for Encina.EntityFrameworkCore data access components.

## Overview

These benchmarks measure the performance overhead of Encina's Entity Framework Core abstractions, focusing on hot path operations that execute on every request.

## Benchmark Classes

### TransactionBehaviorBenchmarks

Measures `TransactionPipelineBehavior<TRequest, TResponse>` overhead including reflection-based transaction detection and pipeline cost.

| Operation | Target | Description |
|-----------|--------|-------------|
| Non-transactional passthrough | <1μs | Request without transaction requirements |
| Interface detection | <100ns | ITransactionalCommand interface check |
| Attribute detection | <1μs | [Transaction] attribute reflection |
| Transaction lifecycle | - | BeginTransaction + Commit/Rollback |

**Key benchmarks:**
- `DirectHandler_Baseline` - Direct handler invocation without pipeline
- `NonTransactional_Passthrough` - Tests RequiresTransaction check overhead
- `InterfaceDetection_WithTransaction` - Interface-based detection + transaction
- `AttributeDetection_WithTransaction` - Attribute-based detection + transaction
- `RequiresTransaction_*` - Pure reflection overhead measurements

### SpecificationEvaluatorBenchmarks

Measures `SpecificationEvaluator` query expression building for specification-based queries.

| Operation | Target | Description |
|-----------|--------|-------------|
| Simple predicate | <100ns | Single Where criterion |
| Complex predicates | - | Multiple AND-combined criteria |
| Keyset pagination | 1-5μs | Expression tree construction |
| Full specification | <10μs | Predicates + ordering + pagination + includes |

**Key benchmarks:**
- `SimplePredicate_SingleWhere` - Baseline single criterion
- `ComplexPredicates_Parameterized` - [Params(2, 5, 10)] criteria counts
- `KeysetPagination_ExpressionBuilding` - Cursor-based pagination
- `FullSpecification_AllFeatures` - End-to-end specification evaluation

### FunctionalRepositoryBenchmarks

Measures `FunctionalRepositoryEF<TEntity, TId>` CRUD operations and exception mapping.

**Key benchmarks:**
- `GetByIdAsync_*` - FindAsync with identity map behavior
- `ListAsync_*` - AsNoTracking vs tracked queries
- `AddAsync_*` - Single entity persistence
- `AddRangeAsync_Batch` - Batch operations [Params(10, 100, 1000)]
- `DeleteRangeAsync_BulkDelete` - ExecuteDeleteAsync efficiency
- `AddAsync_DuplicateKey` - IsDuplicateKeyException string matching

### UnitOfWorkBenchmarks

Measures `UnitOfWorkEF` repository caching, transaction lifecycle, and SaveChanges overhead.

**Key benchmarks:**
- `Repository_CacheMiss/CacheHit` - ConcurrentDictionary.GetOrAdd performance
- `BeginTransactionAsync/CommitAsync` - Transaction coordination
- `SaveChangesAsync_Parameterized` - [Params(1, 10, 100)] tracked entities
- `ChangeTrackerClear_Parameterized` - Cleanup cost
- `ConcurrentRepositoryAccess` - Thread safety stress test

### UnitOfWorkRepositoryBenchmarks

Measures deferred write operations (tracking without immediate SaveChanges).

**Key benchmarks:**
- `UoW_AddAsync_TrackingOnly` - Pure tracking overhead
- `UoW_AddRangeAsync_TrackingBatch` - [Params(1, 10, 100, 1000)]
- `DeferredPersistence_TrackManyThenSave` - Unit of Work pattern
- `ImmediatePersistence_SaveEach` - Traditional repository pattern

### BulkOperationsBenchmarks

Measures `BulkOperationsEF<TEntity>` factory method and provider detection.

**Architecture note:** BulkOperationsEF uses a factory pattern that detects the database provider at instantiation:

| Provider | Implementation | Optimization |
|----------|---------------|--------------|
| **SQL Server** | SqlBulkCopy | Native bulk copy (fastest) |
| **PostgreSQL** | COPY command | Batched parameterized SQL |
| **MySQL** | Extended inserts | Batched parameterized SQL |
| **SQLite** | INSERT OR REPLACE | Batched parameterized SQL |
| **Oracle** | INSERT ALL/MERGE | Batched statements |

**Key benchmarks:**
- `CreateBulkOperations_Factory` - Factory method overhead
- `GetDbConnection_Cost` - Connection retrieval
- `ConnectionType_PatternMatching` - Provider detection
- `CachedBulkOperations_Usage` - Cached vs uncached comparison
- `BulkInsertAsync_Sqlite` - Actual bulk insert [Params(100, 1000)]

## Database Provider Usage

| Benchmark Class | Provider | Rationale |
|-----------------|----------|-----------|
| TransactionBehavior | SQLite in-memory | Requires real transaction support |
| SpecificationEvaluator | EF Core InMemory | Pure CPU measurement |
| FunctionalRepository | SQLite in-memory | Requires actual persistence |
| UnitOfWork | SQLite in-memory | Requires real database behavior |
| BulkOperations | SQLite in-memory | Provider-specific SQL generation |

## Performance Targets Summary

| Category | Operation | Target |
|----------|-----------|--------|
| **Transaction** | Non-transactional passthrough | <1μs |
| **Transaction** | Interface detection | <100ns |
| **Specification** | Simple predicate | <100ns |
| **Specification** | Keyset pagination | 1-5μs |
| **Specification** | Full specification | <10μs |
| **Repository** | GetByIdAsync (identity map hit) | <1μs |
| **UnitOfWork** | Repository cache hit | <100ns |

## Related Benchmarks

**Messaging Store Benchmarks** (not duplicated here to avoid maintenance burden):

- `Encina.Benchmarks/Inbox/InboxEfCoreBenchmarks.cs` - EF Core Inbox store operations
- `Encina.Benchmarks/Outbox/OutboxEfCoreBenchmarks.cs` - EF Core Outbox store operations

These existing benchmarks provide comprehensive coverage of EF Core messaging patterns including batch operations and full workflows.

## Running Benchmarks

### Run all EntityFrameworkCore benchmarks

```bash
cd tests/Encina.BenchmarkTests/Encina.Benchmarks
dotnet run -c Release -- --filter "*EntityFrameworkCore*"
```

### Run specific benchmark class

```bash
# TransactionBehavior benchmarks
dotnet run -c Release -- --filter "*TransactionBehaviorBenchmarks*"

# SpecificationEvaluator benchmarks
dotnet run -c Release -- --filter "*SpecificationEvaluatorBenchmarks*"

# FunctionalRepository benchmarks
dotnet run -c Release -- --filter "*FunctionalRepositoryBenchmarks*"

# UnitOfWork benchmarks
dotnet run -c Release -- --filter "*UnitOfWorkBenchmarks*"

# BulkOperations benchmarks
dotnet run -c Release -- --filter "*BulkOperationsBenchmarks*"
```

### Run specific benchmark method

```bash
# Single benchmark
dotnet run -c Release -- --filter "*SimplePredicate_SingleWhere*"

# Multiple benchmarks with pattern
dotnet run -c Release -- --filter "*GetByIdAsync*"
```

### Common options

```bash
# Short run for quick validation
dotnet run -c Release -- --filter "*EntityFrameworkCore*" --job short

# Export results to specific format
dotnet run -c Release -- --filter "*EntityFrameworkCore*" --exporters json

# Memory diagnostics only
dotnet run -c Release -- --filter "*EntityFrameworkCore*" --memory
```

## Memory Allocation Expectations

| Operation | Expected Allocation | Notes |
|-----------|---------------------|-------|
| Repository cache hit | 0 B | No allocation for cached lookup |
| Simple specification | 96-192 B | Expression tree nodes |
| Complex specification (10 criteria) | ~1 KB | Combined expression trees |
| AddAsync (single entity) | ~500 B | Entity + tracking entry |
| AddRangeAsync (100 entities) | ~50 KB | Entities + tracking entries |
| BulkOperationsEF factory | ~200 B | Provider implementation allocation |

### Interpreting Memory Diagnostics

- **Gen 0/1/2**: Indicates garbage collection pressure
- **Allocated**: Total bytes allocated during benchmark
- **0 B allocation**: Ideal for hot path operations
- **Large allocations**: May indicate unnecessary object creation

## Infrastructure

### BenchmarkEntity

Simple test entity implementing `IEntity<Guid>` with properties:
- `Id`, `Name`, `Value`, `CreatedAtUtc`, `Category`, `IsActive`

### EntityFrameworkBenchmarkDbContext

Configured with realistic indexes for query benchmarks. Supports:
- `CreateInMemory()` - Pure CPU measurement
- `CreateSqlite(connection)` - Real SQL behavior

### TestData

Factory methods for generating test entities:
- `CreateEntity(index)` - Single entity
- `CreateEntities(count)` - Batch generation
- Standard batch sizes: 1, 10, 100, 1000

## CA1001 Suppression Pattern

BenchmarkDotNet classes with IDisposable fields (DbContext, SqliteConnection) use:

```csharp
#pragma warning disable CA1001 // BenchmarkDotNet handles disposal via GlobalCleanup
public class MyBenchmarks
#pragma warning restore CA1001
{
    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _dbContext?.Dispose();
        _connection?.Dispose();
    }
}
```

This is the standard pattern throughout BenchmarkDotNet suites - disposal is managed by lifecycle methods, not IDisposable implementation.
