# ADO.NET Benchmarks

Comprehensive BenchmarkDotNet benchmarks for Encina.ADO provider stores and repository operations.

## Overview

These benchmarks measure the performance of pure ADO.NET data access implementations, focusing on:

- **Messaging Store Operations**: Outbox, Inbox, Saga, and Scheduled Message stores
- **Repository Pattern**: CRUD operations via `FunctionalRepositoryADO<TEntity, TId>`
- **Specification SQL Building**: Expression-to-SQL translation performance

ADO.NET benchmarks provide the baseline for comparison against Dapper implementations, measuring the overhead of micro-ORM abstractions.

## Benchmark Classes

### Messaging Stores

| Class | Benchmarks | Focus |
|-------|------------|-------|
| `OutboxStoreBenchmarks` | 10 | Outbox message CRUD and batch operations |
| `InboxStoreBenchmarks` | 10 | Inbox idempotency checks and message retrieval |
| `SagaStoreBenchmarks` | 10 | Saga state persistence and status transitions |
| `ScheduledMessageStoreBenchmarks` | 10 | Scheduled message due date queries and updates |

### Repository Operations

| Class | Benchmarks | Focus |
|-------|------------|-------|
| `RepositoryBenchmarks` | 20 | Full CRUD via repository vs raw ADO.NET |
| `SpecificationSqlBuilderBenchmarks` | 12 | SQL generation from specifications |

## Provider Matrix

All store benchmarks support the following database providers via `[Params]`:

| Provider | Connection Type | Parameter Syntax | Boolean |
|----------|-----------------|------------------|---------|
| **SQLite** | `SqliteConnection` | `@param` | `0/1` |
| **SQL Server** | `SqlConnection` | `@param` | `bit` |
| **PostgreSQL** | `NpgsqlConnection` | `@param` | `true/false` |
| **MySQL** | `MySqlConnection` | `@param` | `0/1` |

## Benchmark Details

### OutboxStoreBenchmarks

Measures `OutboxStoreADO` operations:

| Benchmark | Description | Parameters |
|-----------|-------------|------------|
| `AddAsync_SingleMessage` | Single outbox message insert | - |
| `GetPendingMessagesAsync` | Batch retrieval of pending messages | `BatchSize: 10, 100` |
| `MarkAsProcessedAsync` | Status update for single message | - |
| `MarkAsFailedAsync` | Failure recording with retry scheduling | - |
| `GetMessageAsync` | Single message retrieval by ID | - |

### InboxStoreBenchmarks

Measures `InboxStoreADO` operations:

| Benchmark | Description | Parameters |
|-----------|-------------|------------|
| `AddAsync_SingleMessage` | Single inbox message insert | - |
| `ExistsAsync` | Idempotency check by message ID | - |
| `GetMessageAsync` | Single message retrieval | - |
| `MarkAsProcessedAsync` | Processing completion | - |

### SagaStoreBenchmarks

Measures `SagaStoreADO` operations:

| Benchmark | Description | Parameters |
|-----------|-------------|------------|
| `CreateAsync` | New saga state creation | - |
| `GetByIdAsync` | Saga retrieval by ID | - |
| `UpdateAsync` | Saga state update | - |
| `CompleteAsync` | Saga completion status | - |
| `GetPendingSagasAsync` | Batch retrieval of running sagas | `BatchSize: 10, 100` |

### ScheduledMessageStoreBenchmarks

Measures `ScheduledMessageStoreADO` operations:

| Benchmark | Description | Parameters |
|-----------|-------------|------------|
| `AddAsync_SingleMessage` | Single scheduled message insert | - |
| `GetDueMessagesAsync` | Batch retrieval of due messages | `BatchSize: 10, 100` |
| `MarkAsProcessedAsync` | Processing completion | - |
| `GetRecurringMessagesAsync` | Recurring message retrieval | - |

### RepositoryBenchmarks

Measures `FunctionalRepositoryADO<TEntity, TId>` operations:

| Benchmark | Description | Parameters |
|-----------|-------------|------------|
| `Repository_GetByIdAsync` | Entity retrieval by ID (baseline) | - |
| `RawAdo_GetById` | Raw ADO.NET comparison | - |
| `Repository_ListAsync` | Full table retrieval | - |
| `Repository_ListWithSpecification` | Filtered query via specification | - |
| `RawAdo_FilteredQuery` | Raw ADO.NET filtered query | - |
| `Repository_AddAsync` | Single entity insert | - |
| `RawAdo_Insert` | Raw ADO.NET insert | - |
| `Repository_UpdateAsync` | Single entity update | - |
| `RawAdo_Update` | Raw ADO.NET update | - |
| `Repository_DeleteAsync` | Single entity delete | - |
| `RawAdo_Delete` | Raw ADO.NET delete | - |
| `Repository_AddRangeAsync` | Batch insert | `BatchSize: 10, 100, 1000` |
| `RawAdo_BulkInsert` | Raw ADO.NET batch insert | Same |
| `Repository_UpdateRangeAsync` | Batch update | `BatchSize: 10, 100, 1000` |
| `RawAdo_BulkUpdate` | Raw ADO.NET batch update | Same |
| `Repository_DeleteRangeAsync` | Batch delete via specification | `BatchSize: 10, 100, 1000` |
| `RawAdo_BulkDelete` | Raw ADO.NET batch delete | Same |
| `Repository_CountAsync` | Aggregate count with specification | - |
| `RawAdo_Count` | Raw ADO.NET count | - |
| `Repository_AnyAsync` | Existence check with specification | - |
| `Repository_FirstOrDefaultAsync` | Single entity with specification | - |

### SpecificationSqlBuilderBenchmarks

Measures `SpecificationSqlBuilder<TEntity>` SQL generation:

| Benchmark | Description | Notes |
|-----------|-------------|-------|
| `BuildWhereClause_SingleEquality` | `WHERE Id = @p0` (baseline) | Simplest case |
| `BuildWhereClause_MultipleAnd` | `WHERE A AND B AND C` | 3 conditions |
| `BuildWhereClause_OrCombination` | `WHERE A OR B` | Disjunction |
| `BuildWhereClause_StringContains` | `WHERE Name LIKE '%value%'` | String operation |
| `BuildWhereClause_StringStartsWith` | `WHERE Name LIKE 'value%'` | String operation |
| `BuildWhereClause_StringEndsWith` | `WHERE Name LIKE '%value'` | String operation |
| `BuildOrderByClause_SingleColumn` | `ORDER BY Name` | Simple ordering |
| `BuildOrderByClause_MultipleColumns` | `ORDER BY A DESC, B, C DESC` | Complex ordering |
| `BuildPaginationClause` | `LIMIT @n OFFSET @m` | SQLite syntax |
| `BuildSelectStatement_Complete` | Full SELECT with WHERE, ORDER, LIMIT | End-to-end |
| `SpecificationReuse_PreBuilt` | Pre-instantiated specification | Reuse pattern |
| `SpecificationReuse_DynamicCreation` | New specification each time | Creation overhead |

## Running Benchmarks

### Run all ADO.NET benchmarks

```bash
cd tests/Encina.BenchmarkTests/Encina.ADO.Benchmarks
dotnet run -c Release
```

### Run specific benchmark class

```bash
# Store benchmarks
dotnet run -c Release -- --filter "*OutboxStoreBenchmarks*"
dotnet run -c Release -- --filter "*InboxStoreBenchmarks*"
dotnet run -c Release -- --filter "*SagaStoreBenchmarks*"
dotnet run -c Release -- --filter "*ScheduledMessageStoreBenchmarks*"

# Repository benchmarks
dotnet run -c Release -- --filter "*RepositoryBenchmarks*"
dotnet run -c Release -- --filter "*SpecificationSqlBuilderBenchmarks*"
```

### Run specific benchmark method

```bash
# Single benchmark
dotnet run -c Release -- --filter "*GetByIdAsync*"

# Multiple benchmarks with pattern
dotnet run -c Release -- --filter "*AddAsync*"
```

### Common options

```bash
# List benchmarks without running
dotnet run -c Release -- --list flat

# Short run for quick validation
dotnet run -c Release -- --filter "*ADO*" --job short

# Dry run (validate setup only)
dotnet run -c Release -- --filter "*ADO*" --job dry

# Export results to specific format
dotnet run -c Release -- --exporters json markdown

# Memory diagnostics only
dotnet run -c Release -- --memory
```

## Performance Expectations

### Store Operations

ADO.NET provides the baseline performance without micro-ORM overhead:

| Category | Operation | Expected Range |
|----------|-----------|----------------|
| **Outbox** | AddAsync (single) | 40-150 μs |
| **Outbox** | GetPendingMessagesAsync (100) | 150-400 μs |
| **Inbox** | ExistsAsync | 15-80 μs |
| **Saga** | CreateAsync | 40-150 μs |
| **Scheduled** | GetDueMessagesAsync (100) | 150-400 μs |

### Repository Operations

| Category | Operation | Expected Range |
|----------|-----------|----------------|
| **Repository** | GetByIdAsync | 15-80 μs |
| **Repository** | ListAsync (1000 entities) | 800 μs - 4 ms |
| **Repository** | AddRangeAsync (100) | 400 μs - 1.5 ms |
| **SQL Builder** | BuildWhereClause (simple) | <10 μs |
| **SQL Builder** | BuildSelectStatement | <50 μs |

### ADO.NET vs Repository Abstraction

| Operation | Repository Overhead | Notes |
|-----------|---------------------|-------|
| Single read | ~10-20% | Specification evaluation |
| Batch read | ~15-25% | Per-row mapping + list |
| Single write | ~5-15% | Parameter creation |
| Bulk operations | ~10-20% | Batch coordination |

## Memory Allocation Expectations

| Operation | Expected Allocation | Notes |
|-----------|---------------------|-------|
| Single message add | ~400-800 B | Message + command + parameters |
| Batch read (100 messages) | ~40-80 KB | Message list + reader iteration |
| Specification SQL build | ~200-500 B | String allocations |
| Repository GetByIdAsync | ~250-500 B | Entity + Either wrapper |
| Raw ADO.NET GetById | ~200-400 B | Entity only |

## Infrastructure

### BenchmarkEntityFactory

Factory methods for creating test entities:

- `CreateOutboxMessage()` - Single outbox message
- `CreateInboxMessage()` - Single inbox message
- `CreateSagaState()` - Single saga state
- `CreateScheduledMessage()` - Single scheduled message
- `CreateRepositoryEntity()` - Single repository entity
- `Create*Messages(count)` - Batch generation

### AdoConnectionFactory

Connection management for in-memory SQLite:

- `CreateSharedMemorySqliteConnection(name)` - Named shared memory database
- Uses `Mode=Memory;Cache=Shared` for test isolation

### AdoSchemaBuilder

Schema creation for all supported providers:

- `CreateAllTables(connection, provider)` - All messaging tables
- `CreateBenchmarkEntityTable(connection, provider)` - Repository entity table
- Provider-specific SQL syntax handled internally

### AdoCommandHelper

Common ADO.NET operations:

- `AddParameter(command, name, value)` - Type-safe parameter creation
- Handles DBNull conversion automatically

## Related Projects

- **Encina.Dapper.Benchmarks** - Dapper provider benchmarks (parallel structure)
- **Encina.Benchmarks** - EF Core and general benchmarks
- **Encina.EntityFrameworkCore.Benchmarks** - EF Core-specific benchmarks

## Comparison with Dapper

For direct Dapper vs ADO.NET comparison, see:

- `Encina.Dapper.Benchmarks/ProviderComparison/DapperVsAdoComparisonBenchmarks.cs`

This benchmark class runs both implementations side-by-side using `[Params("Dapper", "ADO")]` for fair comparison.

## Notes

### SQLite DateTime Format

SQLite stores DateTime as ISO 8601 text. All benchmarks use `DateTime.UtcNow` from C# with `CultureInfo.InvariantCulture` for parsing to ensure format compatibility.

### Manual Object Mapping

ADO.NET requires manual mapping from `IDataReader` to entities. The `MapFromReader` helper methods handle:

- Nullable column handling with `IsDBNull()`
- String-to-GUID parsing for SQLite
- DateTime parsing with invariant culture

### BenchmarkDotNet Configuration

All benchmark classes use:

- `[MemoryDiagnoser]` - Memory allocation tracking
- `[RankColumn]` - Comparative ranking
- `[Baseline = true]` - Baseline marking for ratios
