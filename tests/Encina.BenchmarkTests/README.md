# Encina Benchmark Tests

This directory contains comprehensive BenchmarkDotNet micro-benchmarks for measuring performance across all Encina data access providers and abstractions.

## Overview

The benchmark suite validates that Encina's abstractions introduce minimal overhead while providing clean APIs. Each benchmark project targets a specific provider or component.

## Benchmark Projects

### Data Access Providers

| Project | Provider | Components |
|---------|----------|------------|
| **[Encina.Dapper.Benchmarks](Encina.Dapper.Benchmarks/README.md)** | Dapper | Messaging stores, Repository, SQL Builder, Dapper vs ADO comparison |
| **[Encina.ADO.Benchmarks](Encina.ADO.Benchmarks/README.md)** | ADO.NET | Messaging stores, Repository, SQL Builder |
| **[Encina.Benchmarks](Encina.Benchmarks/)** | Multiple | EF Core, Read/Write Separation, Provider Comparison |
| **Encina.EntityFrameworkCore.Benchmarks** | EF Core | Transaction behavior, Specification evaluator, Repository, UoW, Bulk ops |

### Component-Specific

| Project | Focus |
|---------|-------|
| **Encina.AspNetCore.Benchmarks** | Middleware and endpoint benchmarks |
| **Encina.Caching.Benchmarks** | Cache abstraction overhead |
| **Encina.DistributedLock.Benchmarks** | Lock acquisition and release |
| **Encina.Polly.Benchmarks** | Resilience policy execution |
| **Encina.Refit.Benchmarks** | HTTP client abstraction |
| **Encina.Extensions.Resilience.Benchmarks** | Resilience extensions |
| **Encina.AwsLambda.Benchmarks** | AWS Lambda handler |
| **Encina.AzureFunctions.Benchmarks** | Azure Functions handler |

## Benchmark Categories

### Messaging Store Operations

Benchmarks for Outbox, Inbox, Saga, and Scheduled Message stores across all providers:

| Store | Operations | Focus |
|-------|------------|-------|
| **Outbox** | Add, GetPending, MarkProcessed, MarkFailed | Event publishing reliability |
| **Inbox** | Add, Exists, Get, MarkProcessed | Idempotent processing |
| **Saga** | Create, Get, Update, Complete | Distributed transaction state |
| **Scheduled** | Add, GetDue, MarkProcessed | Delayed message execution |

### Repository Pattern

Benchmarks comparing repository abstraction overhead vs raw data access:

| Operation | Repository vs Raw | Focus |
|-----------|-------------------|-------|
| GetByIdAsync | ~10-20% overhead | Single entity retrieval |
| ListAsync | ~15-25% overhead | Collection queries |
| AddAsync | ~5-15% overhead | Single insert |
| AddRangeAsync | ~10-20% overhead | Batch inserts |
| Specification queries | Variable | Expression-to-SQL translation |

### SQL Generation

Benchmarks for `SpecificationSqlBuilder<TEntity>`:

| Operation | Complexity | Target |
|-----------|------------|--------|
| Simple WHERE | Single equality | <10 μs |
| Complex WHERE | Multiple AND/OR | <20 μs |
| String operations | Contains/StartsWith/EndsWith | <15 μs |
| Full SELECT | WHERE + ORDER + LIMIT | <50 μs |

### Provider Comparison

Direct comparison across providers:

| Comparison | Location | Insight |
|------------|----------|---------|
| Dapper vs ADO.NET | `Encina.Dapper.Benchmarks/ProviderComparison/` | Micro-ORM overhead |
| EF Core vs Dapper | `Encina.Benchmarks/ProviderComparison/` | ORM vs micro-ORM |
| SQLite vs SqlServer vs PostgreSQL vs MySQL | Various | Provider characteristics |

## Running Benchmarks

### Quick Start

```bash
# Run all benchmarks in a specific project
cd tests/Encina.BenchmarkTests/Encina.Dapper.Benchmarks
dotnet run -c Release

# Run with filtering
dotnet run -c Release -- --filter "*OutboxStore*"

# List available benchmarks
dotnet run -c Release -- --list flat
```

### Common Commands

```bash
# Short run for quick validation
dotnet run -c Release -- --filter "*" --job short

# Dry run (validate setup only)
dotnet run -c Release -- --filter "*" --job dry

# Export results
dotnet run -c Release -- --exporters json markdown html

# Memory diagnostics only
dotnet run -c Release -- --memory

# Specific benchmark method
dotnet run -c Release -- --filter "*GetByIdAsync*"
```

### Filter Patterns

```bash
# By provider
--filter "*Dapper*"
--filter "*ADO*"
--filter "*EntityFrameworkCore*"

# By component
--filter "*OutboxStore*"
--filter "*Repository*"
--filter "*SpecificationSqlBuilder*"

# By operation
--filter "*AddAsync*"
--filter "*GetPending*"
```

## Performance Targets

### Store Operations

| Provider | Single Write | Batch Read (100) |
|----------|-------------|------------------|
| ADO.NET | 40-150 μs | 150-400 μs |
| Dapper | 50-200 μs | 200-500 μs |
| EF Core | 100-500 μs | 500 μs - 2 ms |

### Repository Operations

| Provider | GetById | List (1000) | AddRange (100) |
|----------|---------|-------------|----------------|
| ADO.NET | 15-80 μs | 800 μs - 4 ms | 400 μs - 1.5 ms |
| Dapper | 20-100 μs | 1-5 ms | 500 μs - 2 ms |
| EF Core | 50-200 μs | 2-10 ms | 1-5 ms |

### SQL Generation

| Operation | Target |
|-----------|--------|
| Simple WHERE | <10 μs |
| Complex WHERE | <20 μs |
| Full SELECT | <50 μs |

## Output Location

All benchmark results are written to:

```
artifacts/performance/results/
```

## Justification Documents

Some benchmark categories have justification documents explaining why benchmarks are or aren't implemented:

| File | Status | Description |
|------|--------|-------------|
| `Repository.md` | ✅ Implemented | Repository benchmarks now exist in Dapper/ADO projects |
| `Specification.md` | ✅ Implemented | SQL Builder benchmarks now exist |
| `Tenancy.md` | Not Implemented | Single WHERE clause, O(1) operation |
| `ModuleIsolation.md` | Not Implemented | Development-only feature |
| `UnitOfWork.md` | Not Implemented | Transaction coordination, not hot path |

## Infrastructure

### Test Entities

All benchmark projects use consistent test entities:

- `BenchmarkOutboxMessage` - Implements `IOutboxMessage`
- `BenchmarkInboxMessage` - Implements `IInboxMessage`
- `BenchmarkSagaState` - Implements `ISagaState`
- `BenchmarkScheduledMessage` - Implements `IScheduledMessage`
- `BenchmarkRepositoryEntity` - Generic CRUD entity

### Connection Factories

Each provider project has a connection factory for in-memory databases:

- `DapperConnectionFactory.CreateSharedMemorySqliteConnection(name)`
- `AdoConnectionFactory.CreateSharedMemorySqliteConnection(name)`

### Schema Builders

Each provider project has schema builders supporting all 4 database providers:

- `DapperSchemaBuilder.CreateAllTables(connection, provider)`
- `AdoSchemaBuilder.CreateAllTables(connection, provider)`

## Related Documentation

- [BenchmarkDotNet Documentation](https://benchmarkdotnet.org/)
- [Benchmark Result Templates](../docs/benchmarks/) - For publishing results
- [Performance Testing Guide](../docs/testing/performance-testing.md)

## Contributing

When adding new benchmarks:

1. Follow the existing class structure with `[MemoryDiagnoser]` and `[RankColumn]`
2. Mark baseline benchmarks with `[Benchmark(Baseline = true)]`
3. Use `[Params]` for parameterized tests (batch sizes, providers)
4. Add benchmarks to the project README
5. Update this README's benchmark matrix

## Notes

### BenchmarkSwitcher

All benchmark projects use `BenchmarkSwitcher.FromAssembly().Run(args, config)` to support command-line filtering. Do not use `BenchmarkRunner.Run<T>()` which ignores filters.

### SQLite DateTime Format

SQLite stores DateTime as ISO 8601 text. Always use `DateTime.UtcNow` from C# with `CultureInfo.InvariantCulture` for parsing.

### CA1001 Suppression

BenchmarkDotNet classes with IDisposable fields use `[GlobalCleanup]` for disposal, suppressing CA1001.
