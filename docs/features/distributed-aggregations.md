# Distributed Aggregations in Encina

This guide explains how to perform distributed aggregation operations (Count, Sum, Avg, Min, Max) across sharded repositories. Encina uses mathematically correct two-phase aggregation to avoid common pitfalls like the average-of-averages error.

## Table of Contents

1. [Overview](#overview)
2. [Supported Operations](#supported-operations)
3. [Quick Start](#quick-start)
4. [Two-Phase Aggregation Architecture](#two-phase-aggregation-architecture)
5. [The Average-of-Averages Problem](#the-average-of-averages-problem)
6. [Handling Results](#handling-results)
7. [Partial Failures](#partial-failures)
8. [Provider Support](#provider-support)
9. [Observability](#observability)
10. [Performance Considerations](#performance-considerations)
11. [Edge Cases](#edge-cases)
12. [FAQ](#faq)

---

## Overview

When data is distributed across multiple shards, computing aggregates like counts, sums, and averages requires querying every shard and combining the results correctly. Encina provides extension methods on `IFunctionalShardedRepository<TEntity, TId>` that handle this automatically:

```text
┌─────────────────────────────────────────────────────────────────────┐
│                   Distributed Aggregation Flow                      │
│                                                                     │
│  Client: repo.SumAcrossShardsAsync(o => o.Amount, o => o.IsActive)  │
│                           │                                         │
│              ┌────────────┼────────────┐                            │
│              ▼            ▼            ▼                            │
│          Shard-1      Shard-2      Shard-3                          │
│         SUM(Amount)  SUM(Amount)  SUM(Amount)                       │
│         WHERE Active WHERE Active WHERE Active                      │
│           = 500        = 300        = 200                           │
│              │            │            │                            │
│              └────────────┼────────────┘                            │
│                           ▼                                         │
│                 AggregationCombiner                                 │
│                 CombineSum → 1000                                   │
│                           │                                         │
│                           ▼                                         │
│              AggregationResult<decimal>                             │
│              Value = 1000, ShardsQueried = 3                        │
└─────────────────────────────────────────────────────────────────────┘
```

---

## Supported Operations

| Operation | Extension Method | Return Type | Description |
|-----------|-----------------|-------------|-------------|
| **Count** | `CountAcrossShardsAsync` | `long` | Total count of matching entities |
| **Sum** | `SumAcrossShardsAsync` | `TValue` | Sum of a numeric field |
| **Avg** | `AvgAcrossShardsAsync` | `TValue` | Correct global average (two-phase) |
| **Min** | `MinAcrossShardsAsync` | `TValue?` | Global minimum (null if no data) |
| **Max** | `MaxAcrossShardsAsync` | `TValue?` | Global maximum (null if no data) |

All operations return `Either<EncinaError, AggregationResult<T>>` following Encina's Railway Oriented Programming pattern.

---

## Quick Start

```csharp
using Encina.Sharding.Extensions;

IFunctionalShardedRepository<Order, OrderId> repo = ...;

// Count active orders across all shards
var countResult = await repo.CountAcrossShardsAsync(
    o => o.Status == OrderStatus.Active,
    cancellationToken);

// Sum order totals with an optional filter
var sumResult = await repo.SumAcrossShardsAsync(
    o => o.TotalAmount,
    o => o.Status == OrderStatus.Completed,
    cancellationToken);

// Correct global average (two-phase aggregation)
var avgResult = await repo.AvgAcrossShardsAsync(
    o => o.TotalAmount,
    cancellationToken: cancellationToken);

// Min/Max across shards
var minResult = await repo.MinAcrossShardsAsync(
    o => o.TotalAmount,
    o => o.CreatedAtUtc > cutoff,
    cancellationToken);

var maxResult = await repo.MaxAcrossShardsAsync(
    o => o.TotalAmount,
    cancellationToken: cancellationToken);
```

---

## Two-Phase Aggregation Architecture

Encina uses a two-phase aggregation approach for correctness:

### Phase 1: Scatter (Per-Shard Execution)

Each shard executes its own SQL/query and returns a `ShardAggregatePartial<TValue>` containing:

| Field | Purpose |
|-------|---------|
| `ShardId` | Identifies which shard produced this partial |
| `Sum` | Local sum of the field (used by Sum and Avg) |
| `Count` | Local count of matching rows (used by Count and Avg) |
| `Min` | Local minimum value (used by Min) |
| `Max` | Local maximum value (used by Max) |

### Phase 2: Combine (Global Aggregation)

The `AggregationCombiner` static class merges all partials:

| Method | Algorithm |
|--------|-----------|
| `CombineCount` | Sum of all per-shard counts |
| `CombineSum` | Sum of all per-shard sums |
| `CombineAvg` | `totalSum / totalCount` (NOT average of averages) |
| `CombineMin` | Minimum of all per-shard minimums |
| `CombineMax` | Maximum of all per-shard maximums |

### Why Two Phases?

The key insight is that intermediate aggregation results carry different semantic weight. A shard with 1 million rows and a shard with 10 rows both contribute equally in a naive average, but should not. Two-phase aggregation preserves the raw sum and count from each shard, allowing mathematically correct global computation.

---

## The Average-of-Averages Problem

This is the most critical correctness issue in distributed aggregation.

### The Wrong Way

```text
Shard A:  1 order,  Amount = 100  →  AVG = 100
Shard B: 99 orders, Amount =   1 each → AVG = 1

Naive average-of-averages: (100 + 1) / 2 = 50.5  ❌ WRONG
```

### The Correct Way (Encina)

```text
Shard A:  Sum = 100, Count =  1
Shard B:  Sum =  99, Count = 99

Global: totalSum / totalCount = (100 + 99) / (1 + 99) = 199 / 100 = 1.99  ✅ CORRECT
```

Encina always uses the two-phase approach for averages, collecting both SUM and COUNT from each shard and computing the correct weighted result.

### SQL Generated for Avg (Two-Phase)

For SQL providers, the average operation generates a single query that retrieves both values:

```sql
-- SQL Server / PostgreSQL / SQLite
SELECT SUM([Amount]) AS SumValue, COUNT(*) AS CountValue
FROM [Orders]
WHERE [Status] = @p0

-- MySQL
SELECT SUM(`Amount`) AS SumValue, COUNT(*) AS CountValue
FROM `Orders`
WHERE `Status` = @p0
```

---

## Handling Results

All aggregation methods return `Either<EncinaError, AggregationResult<T>>`:

```csharp
var result = await repo.CountAcrossShardsAsync(o => o.IsActive, ct);

result.Match(
    Right: agg =>
    {
        Console.WriteLine($"Count: {agg.Value}");
        Console.WriteLine($"Shards queried: {agg.ShardsQueried}");
        Console.WriteLine($"Duration: {agg.Duration.TotalMilliseconds}ms");

        if (agg.IsPartial)
        {
            Console.WriteLine($"WARNING: {agg.FailedShards.Count} shards failed");
            foreach (var failure in agg.FailedShards)
            {
                Console.WriteLine($"  - {failure.ShardId}: {failure.ErrorMessage}");
            }
        }
    },
    Left: error => Console.WriteLine($"Error: {error.Message}"));
```

### `AggregationResult<T>` Properties

| Property | Type | Description |
|----------|------|-------------|
| `Value` | `T` | The aggregated result |
| `ShardsQueried` | `int` | Total number of shards queried |
| `FailedShards` | `IReadOnlyList<ShardFailure>` | Shards that failed |
| `Duration` | `TimeSpan` | Total operation duration |
| `IsPartial` | `bool` | `true` if any shards failed |

---

## Partial Failures

When some shards fail but others succeed, Encina returns a **partial result** rather than failing the entire operation:

```csharp
var result = await repo.SumAcrossShardsAsync(o => o.Amount, ct);

result.Match(
    Right: agg =>
    {
        if (agg.IsPartial)
        {
            // Value is computed from successful shards only
            logger.LogWarning(
                "Partial result: {Value} from {Success}/{Total} shards",
                agg.Value,
                agg.ShardsQueried - agg.FailedShards.Count,
                agg.ShardsQueried);
        }
    },
    Left: error =>
    {
        // Complete failure - no shards succeeded
        logger.LogError("Aggregation failed: {Error}", error.Message);
    });
```

### Failure Behavior

| Scenario | Result |
|----------|--------|
| All shards succeed | `Right` with `IsPartial = false` |
| Some shards fail | `Right` with `IsPartial = true`, value from successful shards |
| All shards fail | `Left` with `EncinaError` |
| No matching entities | `Right` with `Value = 0` (Count/Sum/Avg) or `null` (Min/Max) |

---

## Provider Support

Distributed aggregation is supported across all 13 database providers:

| Provider Category | Providers | Status |
|-------------------|-----------|--------|
| **ADO.NET** | SqlServer, PostgreSQL, MySQL, SQLite | ✅ Supported |
| **Dapper** | SqlServer, PostgreSQL, MySQL, SQLite | ✅ Supported |
| **EF Core** | SqlServer, PostgreSQL, MySQL, SQLite | ✅ Supported |
| **MongoDB** | MongoDB | ✅ Supported |

### Provider-Specific SQL

Each SQL provider generates correctly quoted SQL for its database:

| Provider | Column Quoting | Example |
|----------|---------------|---------|
| SQL Server | `[column]` | `SELECT SUM([Amount]) FROM [Orders]` |
| PostgreSQL | `"column"` | `SELECT SUM("Amount") FROM "Orders"` |
| MySQL | `` `column` `` | ``SELECT SUM(`Amount`) FROM `Orders` `` |
| SQLite | `"column"` | `SELECT SUM("Amount") FROM "Orders"` |

### MongoDB

MongoDB uses its aggregation pipeline framework instead of SQL:

```javascript
// Count
db.orders.aggregate([
    { $match: { status: "Active" } },
    { $count: "count" }
])

// Sum
db.orders.aggregate([
    { $match: { status: "Completed" } },
    { $group: { _id: null, result: { $sum: "$totalAmount" } } }
])

// Avg (two-phase: collects sum + count)
db.orders.aggregate([
    { $group: { _id: null, sumValue: { $sum: "$totalAmount" }, countValue: { $sum: 1 } } }
])
```

### EF Core

EF Core providers leverage LINQ-to-SQL translation for maximum compatibility:

```csharp
// Internally translates to:
await dbContext.Set<Order>()
    .Where(predicate)
    .SumAsync(selector, cancellationToken);
```

### Unsupported Repositories

If a repository does not implement `IShardedAggregationSupport`, the extension methods throw `NotSupportedException` with a clear message:

```
The repository of type 'MyCustomRepository' does not implement
IShardedAggregationSupport<Order, OrderId>. Use a provider-specific
sharded repository that supports distributed aggregation.
```

---

## Observability

### OpenTelemetry Tracing

Aggregation operations emit `Activity` spans via `ShardingActivitySource`:

| Span | Operation Name | Tags |
|------|---------------|------|
| Parent span | `Encina.Sharding.Aggregation` | `aggregation.operation_type`, `aggregation.shards_queried` |
| Per-shard span | `Encina.Sharding.ShardAggregation` | `shard.id`, `aggregation.operation_type` |

Completion tags include:

- `aggregation.shards_succeeded`: Number of successful shard queries
- `aggregation.shards_failed`: Number of failed shard queries
- `aggregation.is_partial`: Whether the result is partial
- `aggregation.result_value`: The computed result (string representation)

### Metrics

The `ShardRoutingMetrics` class emits the following metrics when `EnableAggregationMetrics` is `true`:

| Metric | Type | Description |
|--------|------|-------------|
| `encina.sharding.aggregation.duration` | Histogram | Duration in milliseconds per operation type |
| `encina.sharding.aggregation.partial_results` | Counter | Number of partial results per operation type |

Tags: `operation_type` (Count, Sum, Avg, Min, Max), `shards_queried`, `failed_count`, `total_count`.

### Configuration

```csharp
services.Configure<ShardingMetricsOptions>(options =>
{
    options.EnableAggregationMetrics = true;  // default: true
    options.EnableTracing = true;             // default: true
});
```

---

## Performance Considerations

### Parallelism

All aggregation operations use Encina's scatter-gather execution model, querying all shards **in parallel** via `IShardedQueryExecutor.ExecuteAllAsync`. This means the total latency is approximately equal to the slowest shard, not the sum of all shard latencies.

### Query Optimization

- **Use predicates**: Always filter with a predicate when possible to reduce data scanned per shard
- **Avoid unnecessary aggregations**: If you only need a count, use `CountAcrossShardsAsync` rather than fetching all entities and counting in memory
- **Consider caching**: For aggregations that don't need real-time accuracy, cache results with appropriate TTL

### Numeric Type Selection

The `TValue` type parameter for Sum/Avg operations must implement `INumber<TValue>`. Common choices:

| Type | Precision | Use Case |
|------|-----------|----------|
| `decimal` | 28-29 digits | Financial calculations |
| `double` | 15-17 digits | Scientific/statistical |
| `int` / `long` | Integer | Counts, quantities |

### Memory Efficiency

Aggregation operations have constant memory overhead regardless of dataset size. Only the partial results (one per shard) are held in memory, not the individual entities.

---

## Edge Cases

### Empty Shards

If a shard has no matching entities:

- **Count**: Contributes 0 to the total
- **Sum**: Contributes 0 (zero value of `TValue`)
- **Avg**: Contributes sum=0, count=0 (weighted correctly)
- **Min/Max**: Contributes `null` (ignored in global comparison)

### All Shards Empty

- **Count**: Returns 0
- **Sum**: Returns `TValue.Zero`
- **Avg**: Returns `TValue.Zero` (division by zero is handled)
- **Min/Max**: Returns `null`

### Single Shard

Works correctly — the combiner simply returns the single shard's result.

### Nullable Fields

Min and Max return `TValue?` to handle the case where no entities match the predicate. Always check for `null`:

```csharp
var minResult = await repo.MinAcrossShardsAsync(o => o.Amount, ct);

minResult.Match(
    Right: agg =>
    {
        if (agg.Value.HasValue)
            Console.WriteLine($"Minimum: {agg.Value.Value}");
        else
            Console.WriteLine("No matching entities found");
    },
    Left: error => Console.WriteLine($"Error: {error.Message}"));
```

---

## FAQ

### Can I use aggregation without the extension methods?

Yes. If your repository implements `IShardedAggregationSupport<TEntity, TId>`, you can cast and call the methods directly. The extension methods are convenience wrappers that perform the cast and provide better error messages.

### What happens if I call an aggregation on a non-sharded repository?

The extension methods check at runtime whether the repository implements `IShardedAggregationSupport`. If not, a `NotSupportedException` is thrown immediately (no database calls are made).

### Is the predicate parameter required?

- **Count**: The predicate is **required** (you must specify what to count)
- **Sum, Avg, Min, Max**: The predicate is **optional** — passing `null` aggregates all entities

### What numeric types are supported for Sum/Avg?

Any type implementing `System.Numerics.INumber<T>`, including `int`, `long`, `float`, `double`, `decimal`, and `Half`.

### What types are supported for Min/Max?

Any type implementing `IComparable<T>`, which covers all numeric types plus `DateTime`, `DateTimeOffset`, `string`, and custom comparable types.

### How do I aggregate across a subset of shards?

Currently, aggregation always queries all shards in the topology. To query a subset, use the provider-specific `IShardedAggregationSupport` interface directly with a custom query executor.

### Can I combine multiple aggregations in one call?

Not currently. Each operation is independent. If you need Count + Sum + Avg, make three separate calls. The parallel scatter-gather model minimizes the overhead of multiple calls.
