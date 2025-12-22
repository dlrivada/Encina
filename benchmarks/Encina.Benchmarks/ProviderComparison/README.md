# Provider Comparison Benchmarks

This directory contains BenchmarkDotNet benchmarks comparing the performance of different data access providers (ADO.NET, Dapper, and EF Core) for Encina's messaging patterns.

## Available Benchmarks

### 1. OutboxProviderComparisonBenchmarks

Compares Outbox pattern performance across three providers:

- **ADO.NET**: Raw ADO.NET implementation with SqliteCommand/SqliteDataReader
- **Dapper**: Micro-ORM with parameterized queries
- **EF Core**: Full ORM with change tracking

**Operations Benchmarked:**

- `AddAsync_Single` - Single message insert
- `AddAsync_Batch10` - Batch insert of 10 messages
- `AddAsync_Batch100` - Batch insert of 100 messages
- `GetPendingMessages_Batch10` - Query 10 pending messages from 50 total
- `GetPendingMessages_Batch100` - Query 100 pending messages from 500 total
- `MarkAsProcessedAsync` - Update message as processed
- `MarkAsFailedAsync` - Update message as failed with error

### 2. InboxProviderComparisonBenchmarks

Compares Inbox pattern performance across three providers for idempotent message processing:

**Operations Benchmarked:**

- `AddAsync_Single` - Single message insert for deduplication
- `AddAsync_Batch10` - Batch insert of 10 messages
- `AddAsync_Batch100` - Batch insert of 100 messages
- `GetMessage_DuplicateCheck` - Check if message exists (idempotency check)
- `MarkAsProcessedAsync` - Mark message as processed with response
- `MarkAsFailedAsync` - Mark message as failed with error
- `GetExpiredMessages_Batch10` - Query 10 expired messages for cleanup
- `RemoveExpiredMessages_Batch10` - Delete 10 expired messages in batch

### 3. SagaProviderComparisonBenchmarks

Compares Saga pattern performance across two providers (Dapper and EF Core):

**Important Note:** ADO.NET providers do NOT support Sagas. Only Dapper and EF Core implement the Saga pattern.

**Operations Benchmarked:**

- `AddAsync_Single` - Create new saga instance
- `GetAsync_ById` - Retrieve saga by ID
- `UpdateAsync_StateTransition` - Update saga state (single step transition)
- `UpdateAsync_FiveSteps` - Progress saga through 5 steps sequentially
- `GetStuckSagas_Batch10` - Query 10 stuck sagas needing intervention from 50 total

**Use Cases:**

Sagas are used for distributed transaction orchestration. Examples include:

- Multi-step order processing (reserve inventory → charge customer → ship order)
- Cross-service workflows with compensation on failure
- Long-running business processes requiring state persistence

### 4. SchedulingProviderComparisonBenchmarks

Compares Scheduling pattern performance across two providers (Dapper and EF Core):

**Important Note:** ADO.NET providers do NOT support Scheduling. Only Dapper and EF Core implement the Scheduling pattern.

**Operations Benchmarked:**

- `AddAsync_Single` - Schedule single message for future execution
- `AddAsync_Batch10` - Schedule 10 messages in batch
- `AddAsync_Batch100` - Schedule 100 messages in batch
- `GetDueMessages_Batch10` - Query 10 due messages from 50 total
- `GetDueMessages_Batch100` - Query 100 due messages from 500 total
- `MarkAsProcessed` - Mark message as executed successfully
- `MarkAsFailed` - Mark message as failed with retry scheduling
- `RescheduleRecurringMessage` - Reschedule recurring message for next execution
- `CancelScheduledMessage` - Cancel a scheduled message

**Use Cases:**

Scheduling is used for delayed/recurring domain command execution. Examples include:

- Send reminder email 24 hours before appointment
- Cancel unpaid order after 30 minutes
- Archive old records every 90 days
- Retry failed operations with exponential backoff

## Running the Benchmarks

### Run All Benchmarks

```bash
cd benchmarks/Encina.Benchmarks
dotnet run -c Release
```

### Run Specific Benchmark Class

```bash
# Outbox comparison only
dotnet run -c Release --filter *OutboxProviderComparisonBenchmarks*

# Inbox comparison only
dotnet run -c Release --filter *InboxProviderComparisonBenchmarks*

# Saga comparison only (Dapper and EF Core only)
dotnet run -c Release --filter *SagaProviderComparisonBenchmarks*

# Scheduling comparison only (Dapper and EF Core only)
dotnet run -c Release --filter *SchedulingProviderComparisonBenchmarks*
```

### Run Specific Provider

```bash
# ADO.NET only
dotnet run -c Release --filter *OutboxProviderComparisonBenchmarks* --job short --runtimes net10.0 --allCategories ADO

# Dapper only
dotnet run -c Release --filter *OutboxProviderComparisonBenchmarks* --job short --runtimes net10.0 --allCategories Dapper

# EF Core only
dotnet run -c Release --filter *OutboxProviderComparisonBenchmarks* --job short --runtimes net10.0 --allCategories EFCore
```

## Expected Results Format

```
| Method                        | Provider | Mean      | Error    | Ratio | Rank | Allocated |
|-------------------------------|----------|-----------|----------|-------|------|-----------|
| AddAsync_Single               | ADO      |  63.2 μs  | 1.2 μs   | 1.00  | 1    | 2.5 KB    |
| AddAsync_Single               | Dapper   | 100.5 μs  | 2.1 μs   | 1.59  | 2    | 3.8 KB    |
| AddAsync_Single               | EFCore   | 180.3 μs  | 3.5 μs   | 2.85  | 3    | 8.2 KB    |
| GetPendingMessages_Batch10    | ADO      | 120.1 μs  | 2.3 μs   | 1.00  | 1    | 5.1 KB    |
| GetPendingMessages_Batch10    | Dapper   | 195.8 μs  | 4.2 μs   | 1.63  | 2    | 7.3 KB    |
| GetPendingMessages_Batch10    | EFCore   | 340.2 μs  | 6.8 μs   | 2.83  | 3    | 15.8 KB   |
```

## Key Questions Answered

1. **Which provider is fastest for writes?**
   - Expected: ADO.NET (lowest overhead)
   - Dapper adds SQL generation overhead
   - EF Core adds change tracking and SQL generation

2. **Which provider is fastest for reads?**
   - Expected: ADO.NET (manual mapping)
   - Dapper adds object mapping overhead
   - EF Core adds tracking and materialization

3. **Which provider allocates least memory?**
   - Expected: ADO.NET (minimal allocations)
   - Dapper adds some allocations for SQL generation
   - EF Core allocates for change tracking, proxies, etc.

4. **Which provider should I use?**
   - **ADO.NET**: Maximum performance, manual SQL, no abstraction
   - **Dapper**: Good balance of performance and productivity
   - **EF Core**: Best for complex queries, migrations, change tracking

## Architecture Notes

### Why SQLite Only?

We benchmark only SQLite because:

1. **In-Memory Performance**: `:memory:` databases eliminate network latency
2. **Consistent Results**: No external DB server variables
3. **CI/CD Friendly**: No Docker/infrastructure dependencies
4. **Provider Comparison**: Isolates ORM overhead from DB server differences

For real-world database performance (SQL Server, PostgreSQL, MySQL, Oracle):

- Network latency dominates (100-1000x slower than in-memory)
- Provider differences become less significant
- Use load testing instead of micro-benchmarks

### Type Ambiguity Resolution

Each provider (ADO.NET, Dapper, EF Core) defines its own messaging entity classes.
We use namespace aliases to resolve ambiguity:

```csharp
// Outbox/Inbox (ADO, Dapper, EF Core)
using ADOOutbox = Encina.ADO.Sqlite.Outbox;
using DapperOutbox = Encina.Dapper.Sqlite.Outbox;
using EFOutbox = Encina.EntityFrameworkCore.Outbox;

// Saga (Dapper, EF Core only - NO ADO support)
using DapperSagas = Encina.Dapper.Sqlite.Sagas;
using EFSagas = Encina.EntityFrameworkCore.Sagas;

// Scheduling (Dapper, EF Core only - NO ADO support)
using DapperScheduling = Encina.Dapper.Sqlite.Scheduling;
using EFScheduling = Encina.EntityFrameworkCore.Scheduling;

// Usage
var message = new DapperOutbox.OutboxMessage { ... };
var saga = new EFSagas.SagaState { ... };
var scheduled = new DapperScheduling.ScheduledMessage { ... };
```

## Interpreting Results

### Ratio Column

Shows relative performance compared to baseline (ADO.NET):

- `1.00` = Baseline (ADO.NET)
- `1.59` = 59% slower than baseline
- `2.85` = 185% slower than baseline

### Rank Column

Ranks providers from fastest (1) to slowest (3) for each operation.

### Allocated Column

Shows heap allocations per operation:

- Lower is better
- Important for high-throughput scenarios
- Gen 0/1/2 collections impact latency

## Performance Optimization Tips

### ADO.NET

- Reuse `SqliteCommand` objects
- Use parameterized queries
- Avoid boxing/unboxing
- Use `Span<T>` for string operations

### Dapper

- Cache SQL strings (already done)
- Use `buffered: false` for large result sets
- Consider `QueryMultiple` for batches
- Use custom type handlers for complex types

### EF Core

- Disable change tracking for read-only queries: `.AsNoTracking()`
- Use compiled queries for repeated operations
- Batch inserts with `AddRange` + single `SaveChanges`
- Consider raw SQL for performance-critical paths

## Provider Coverage by Pattern

| Pattern    | ADO.NET | Dapper | EF Core |
|------------|---------|--------|---------|
| Outbox     | ✓       | ✓      | ✓       |
| Inbox      | ✓       | ✓      | ✓       |
| Saga       | ✗       | ✓      | ✓       |
| Scheduling | ✗       | ✓      | ✓       |

**Note:** ADO.NET providers only implement Outbox and Inbox patterns. Saga and Scheduling require more complex features (state transitions, cron expressions, etc.) which are only implemented in Dapper and EF Core providers.

## Related Benchmarks

- `benchmarks/Encina.Benchmarks/Outbox/OutboxDapperBenchmarks.cs` - Dapper-specific
- `benchmarks/Encina.Benchmarks/Outbox/OutboxEfCoreBenchmarks.cs` - EF Core-specific
- `benchmarks/Encina.Benchmarks/Inbox/InboxDapperBenchmarks.cs` - Dapper-specific
- `benchmarks/Encina.Benchmarks/Inbox/InboxEfCoreBenchmarks.cs` - EF Core-specific

## Contributing

When adding new benchmarks:

1. Follow existing naming conventions
2. Use `[MemoryDiagnoser]` and `[RankColumn]` attributes
3. Add XML documentation explaining what's being tested
4. Clean up data in `[IterationSetup]` for consistency
5. Use realistic data sizes (not trivial, not massive)
6. Test all applicable providers:
   - **Outbox/Inbox**: ADO, Dapper, EF Core
   - **Saga/Scheduling**: Dapper, EF Core only (ADO.NET does NOT support these patterns)
