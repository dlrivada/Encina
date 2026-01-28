# Dapper Benchmarks

Comprehensive BenchmarkDotNet benchmarks for Encina.Dapper provider stores and repository operations.

## Overview

These benchmarks measure the performance of Dapper-based data access implementations, focusing on:

- **Messaging Store Operations**: Outbox, Inbox, Saga, and Scheduled Message stores
- **Repository Pattern**: CRUD operations via `FunctionalRepositoryDapper<TEntity, TId>`
- **Specification SQL Building**: Expression-to-SQL translation performance
- **Provider Comparison**: Direct Dapper vs ADO.NET comparison

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
| `RepositoryBenchmarks` | 20 | Full CRUD via repository vs raw Dapper |
| `SpecificationSqlBuilderBenchmarks` | 12 | SQL generation from specifications |

### Provider Comparison

| Class | Benchmarks | Focus |
|-------|------------|-------|
| `DapperVsAdoComparisonBenchmarks` | 10 | Direct Dapper vs ADO.NET overhead |

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

Measures `OutboxStoreDapper` operations:

| Benchmark | Description | Parameters |
|-----------|-------------|------------|
| `AddAsync_SingleMessage` | Single outbox message insert | - |
| `GetPendingMessagesAsync` | Batch retrieval of pending messages | `BatchSize: 10, 100` |
| `MarkAsProcessedAsync` | Status update for single message | - |
| `MarkAsFailedAsync` | Failure recording with retry scheduling | - |
| `GetMessageAsync` | Single message retrieval by ID | - |

### InboxStoreBenchmarks

Measures `InboxStoreDapper` operations:

| Benchmark | Description | Parameters |
|-----------|-------------|------------|
| `AddAsync_SingleMessage` | Single inbox message insert | - |
| `ExistsAsync` | Idempotency check by message ID | - |
| `GetMessageAsync` | Single message retrieval | - |
| `MarkAsProcessedAsync` | Processing completion | - |

### SagaStoreBenchmarks

Measures `SagaStoreDapper` operations:

| Benchmark | Description | Parameters |
|-----------|-------------|------------|
| `CreateAsync` | New saga state creation | - |
| `GetByIdAsync` | Saga retrieval by ID | - |
| `UpdateAsync` | Saga state update | - |
| `CompleteAsync` | Saga completion status | - |
| `GetPendingSagasAsync` | Batch retrieval of running sagas | `BatchSize: 10, 100` |

### ScheduledMessageStoreBenchmarks

Measures `ScheduledMessageStoreDapper` operations:

| Benchmark | Description | Parameters |
|-----------|-------------|------------|
| `AddAsync_SingleMessage` | Single scheduled message insert | - |
| `GetDueMessagesAsync` | Batch retrieval of due messages | `BatchSize: 10, 100` |
| `MarkAsProcessedAsync` | Processing completion | - |
| `GetRecurringMessagesAsync` | Recurring message retrieval | - |

### RepositoryBenchmarks

Measures `FunctionalRepositoryDapper<TEntity, TId>` operations:

| Benchmark | Description | Parameters |
|-----------|-------------|------------|
| `Repository_GetByIdAsync` | Entity retrieval by ID (baseline) | - |
| `RawDapper_GetById` | Raw Dapper comparison | - |
| `Repository_ListAsync` | Full table retrieval | - |
| `Repository_ListWithSpecification` | Filtered query via specification | - |
| `Repository_AddAsync` | Single entity insert | - |
| `Repository_UpdateAsync` | Single entity update | - |
| `Repository_DeleteAsync` | Single entity delete | - |
| `Repository_AddRangeAsync` | Batch insert | `BatchSize: 10, 100, 1000` |
| `Repository_UpdateRangeAsync` | Batch update | `BatchSize: 10, 100, 1000` |
| `Repository_DeleteRangeAsync` | Batch delete via specification | `BatchSize: 10, 100, 1000` |
| `Repository_CountAsync` | Aggregate count with specification | - |
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

### DapperVsAdoComparisonBenchmarks

Direct comparison between Dapper and ADO.NET implementations:

| Benchmark | Description | Parameters |
|-----------|-------------|------------|
| `BatchRead_OutboxGetPendingMessages` | Batch read comparison (baseline) | `Provider: Dapper, ADO` `BatchSize: 10, 100` |
| `BatchRead_ScheduledGetDueMessages` | Due message retrieval | Same |
| `SingleWrite_InboxAdd` | Single insert comparison | `Provider: Dapper, ADO` |
| `SingleWrite_OutboxAdd` | Single insert comparison | Same |
| `ParameterizedQuery_InboxGetMessage` | Parameterized SELECT by ID | Same |
| `StatusUpdate_OutboxMarkAsProcessed` | Simple UPDATE | Same |
| `StatusUpdate_OutboxMarkAsFailed` | UPDATE with multiple parameters | Same |
| `RawQuery_Dapper` | Pure Dapper query (no abstraction) | Same |
| `RawQuery_ADO` | Pure ADO.NET query (no abstraction) | Same |

## Running Benchmarks

### Run all Dapper benchmarks

```bash
cd tests/Encina.BenchmarkTests/Encina.Dapper.Benchmarks
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

# Provider comparison
dotnet run -c Release -- --filter "*DapperVsAdoComparisonBenchmarks*"
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
dotnet run -c Release -- --filter "*Dapper*" --job short

# Dry run (validate setup only)
dotnet run -c Release -- --filter "*Dapper*" --job dry

# Export results to specific format
dotnet run -c Release -- --exporters json markdown

# Memory diagnostics only
dotnet run -c Release -- --memory
```

## Performance Expectations

### Store Operations

| Category | Operation | Expected Range |
|----------|-----------|----------------|
| **Outbox** | AddAsync (single) | 50-200 μs |
| **Outbox** | GetPendingMessagesAsync (100) | 200-500 μs |
| **Inbox** | ExistsAsync | 20-100 μs |
| **Saga** | CreateAsync | 50-200 μs |
| **Scheduled** | GetDueMessagesAsync (100) | 200-500 μs |

### Repository Operations

| Category | Operation | Expected Range |
|----------|-----------|----------------|
| **Repository** | GetByIdAsync | 20-100 μs |
| **Repository** | ListAsync (1000 entities) | 1-5 ms |
| **Repository** | AddRangeAsync (100) | 500 μs - 2 ms |
| **SQL Builder** | BuildWhereClause (simple) | <10 μs |
| **SQL Builder** | BuildSelectStatement | <50 μs |

### Dapper vs ADO.NET

| Operation | Dapper Overhead | Notes |
|-----------|-----------------|-------|
| Single read | ~5-15% | Object mapping cost |
| Batch read (100) | ~10-20% | Per-row mapping |
| Single write | ~5-10% | Parameter handling |
| Raw query | Baseline | No abstraction |

## Memory Allocation Expectations

| Operation | Expected Allocation | Notes |
|-----------|---------------------|-------|
| Single message add | ~500-1000 B | Message + command |
| Batch read (100 messages) | ~50-100 KB | Message list + mapping |
| Specification SQL build | ~200-500 B | String allocations |
| Repository GetByIdAsync | ~300-600 B | Entity + Either wrapper |

## Infrastructure

### BenchmarkEntityFactory

Factory methods for creating test entities:

- `CreateOutboxMessage()` - Single outbox message
- `CreateInboxMessage()` - Single inbox message
- `CreateSagaState()` - Single saga state
- `CreateScheduledMessage()` - Single scheduled message
- `CreateRepositoryEntity()` - Single repository entity
- `Create*Messages(count)` - Batch generation

### DapperConnectionFactory

Connection management for in-memory SQLite:

- `CreateSharedMemorySqliteConnection(name)` - Named shared memory database
- Uses `Mode=Memory;Cache=Shared` for test isolation

### DapperSchemaBuilder

Schema creation for all supported providers:

- `CreateAllTables(connection, provider)` - All messaging tables
- `CreateBenchmarkEntityTable(connection, provider)` - Repository entity table
- Provider-specific SQL syntax handled internally

## Related Projects

- **Encina.ADO.Benchmarks** - ADO.NET provider benchmarks (parallel structure)
- **Encina.Benchmarks** - EF Core and general benchmarks
- **Encina.EntityFrameworkCore.Benchmarks** - EF Core-specific benchmarks

## Notes

### SQLite DateTime Format

SQLite stores DateTime as ISO 8601 text. All benchmarks use `DateTime.UtcNow` from C# (not SQLite's `datetime('now')`) to ensure format compatibility.

### Type Handlers

Dapper type handlers are registered in `GlobalSetup`:

- `GuidTypeHandler` - String-based GUID storage for SQLite
- Other providers use native types

### BenchmarkDotNet Configuration

All benchmark classes use:

- `[MemoryDiagnoser]` - Memory allocation tracking
- `[RankColumn]` - Comparative ranking
- `[Baseline = true]` - Baseline marking for ratios
