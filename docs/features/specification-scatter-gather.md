# Specification-Based Scatter-Gather

Reuse domain specifications for cross-shard queries with per-shard metadata, ordering, and pagination.

## Table of Contents

1. [Overview](#overview)
2. [Quick Start](#quick-start)
3. [Architecture](#architecture)
4. [Query Operations](#query-operations)
5. [Pagination Strategies](#pagination-strategies)
6. [Result Types](#result-types)
7. [Targeted Scatter-Gather](#targeted-scatter-gather)
8. [Observability](#observability)
9. [Provider-Specific Notes](#provider-specific-notes)
10. [Performance Considerations](#performance-considerations)
11. [Migration from Lambda-Based Queries](#migration-from-lambda-based-queries)

---

## Overview

When querying across shards, the traditional approach uses lambda expressions to define per-shard query logic. This works, but it means writing ad-hoc lambdas that duplicate filtering and ordering logic already captured in domain specifications.

Specification-based scatter-gather solves this by allowing `Specification<T>` objects to be evaluated across shards directly. Each provider translates the specification to its native query mechanism: `IQueryable<T>` for EF Core, parameterized SQL for Dapper and ADO.NET, and `FilterDefinition<T>` for MongoDB.

**Benefits**:

- **Reuse**: Write a specification once, use it for both single-shard and cross-shard queries
- **Consistency**: Filtering and ordering logic is defined in one place
- **Testability**: Specifications can be tested in isolation without shard infrastructure
- **Per-shard metadata**: Results include item counts, query durations, and failure information per shard
- **Cross-shard pagination**: Built-in support with configurable merge strategies
- **Partial failure handling**: Successful shard results are returned even when some shards fail

All operations return `Either<EncinaError, T>` following Encina's Railway Oriented Programming pattern.

---

## Quick Start

```csharp
using Encina.DomainModeling.Sharding;
using Encina.Sharding;

IFunctionalShardedRepository<Order, OrderId> repo = ...;

// 1. Define a reusable specification
public class ActiveOrdersSpec : QuerySpecification<Order>
{
    public ActiveOrdersSpec()
    {
        AddCriteria(o => o.Status == OrderStatus.Active);
        ApplyOrderByDescending(o => o.CreatedAtUtc);
    }
}

// 2. Query all shards with the specification
var spec = new ActiveOrdersSpec();
var result = await repo.QueryAllShardsAsync(spec, cancellationToken);

// 3. Handle the result
result.Match(
    Right: r =>
    {
        Console.WriteLine($"Found {r.Items.Count} items across {r.ShardsQueried} shards");
        Console.WriteLine($"Total duration: {r.TotalDuration.TotalMilliseconds}ms");

        foreach (var (shardId, count) in r.ItemsPerShard)
        {
            var duration = r.DurationPerShard[shardId];
            Console.WriteLine($"  Shard {shardId}: {count} items in {duration.TotalMilliseconds}ms");
        }

        if (r.IsPartial)
            Console.WriteLine($"WARNING: {r.FailedShards.Count} shard(s) failed");
    },
    Left: error => Console.WriteLine($"Error: {error.Message}"));
```

---

## Architecture

```text
┌──────────────────────────────────────────────────────────────────────────────────┐
│                   Specification-Based Scatter-Gather Flow                        │
│                                                                                  │
│  Application: repo.QueryAllShardsAsync(spec, ct)                                 │
│                           │                                                      │
│              ┌────────────┼────────────┐                                         │
│              │            │            │                                         │
│              ▼            ▼            ▼                                         │
│          Shard-1      Shard-2      Shard-3                                       │
│       ┌──────────┐ ┌──────────┐ ┌──────────┐                                     │
│       │ Provider  │ │ Provider  │ │ Provider  │                                  │
│       │ translates│ │ translates│ │ translates│                                  │
│       │ spec to   │ │ spec to   │ │ spec to   │                                  │
│       │ native    │ │ native    │ │ native    │                                  │
│       │ query     │ │ query     │ │ query     │                                  │
│       └──────────┘ └──────────┘ └──────────┘                                     │
│           │ 5 items    │ 8 items    │ 3 items                                    │
│              └────────────┼────────────┘                                         │
│                           ▼                                                      │
│              ScatterGatherResultMerger                                           │
│              MergeAndOrder(perShardItems, spec)                                  │
│              → 16 items, ordered by spec                                         │
│                           │                                                      │
│                           ▼                                                      │
│              ShardedSpecificationResult<Order>                                   │
│              Items = 16, ShardsQueried = 3                                       │
│              ItemsPerShard = { S1:5, S2:8, S3:3 }                                │
└──────────────────────────────────────────────────────────────────────────────────┘
```

### Key Components

| Component | Location | Responsibility |
|-----------|----------|----------------|
| `IShardedSpecificationSupport<TEntity, TId>` | `Encina.DomainModeling` | Interface that providers implement to support specification queries |
| `ShardedSpecificationExtensions` | `Encina.DomainModeling` | Extension methods on `IFunctionalShardedRepository` for convenient API |
| `ScatterGatherResultMerger` | `Encina.DomainModeling` | Merges per-shard results, applies ordering from the specification, and optionally paginates |
| Provider implementations | `Encina.EntityFrameworkCore`, `Encina.Dapper.*`, `Encina.ADO.*`, `Encina.MongoDB` | Translate specifications to native queries and execute against individual shards |

### Runtime Dispatch

The extension methods perform a runtime check to see if the repository implements `IShardedSpecificationSupport<TEntity, TId>`. If it does, the call is delegated to the provider-specific implementation. If not, a `NotSupportedException` is thrown:

```
The repository of type 'MyCustomRepository' does not implement
IShardedSpecificationSupport<Order, OrderId>. Use a provider-specific
sharded repository that supports specification-based scatter-gather.
```

All 13 database provider implementations (`FunctionalShardedRepositoryEF`, `FunctionalShardedRepositoryDapper`, `FunctionalShardedRepositoryADO`, `FunctionalShardedRepositoryMongoDB`) implement this interface.

---

## Query Operations

### QueryAllShardsAsync

Evaluates a specification against all active shards in parallel and returns the merged results with per-shard metadata.

```csharp
var spec = new ActiveOrdersSpec();
var result = await repo.QueryAllShardsAsync(spec, ct);
// Returns Either<EncinaError, ShardedSpecificationResult<Order>>
```

The specification's ordering expressions are applied after merging to ensure correct cross-shard ordering. If the specification defines `OrderByDescending(o => o.CreatedAtUtc)`, the merged result list respects that order across all shards.

### QueryAllShardsPagedAsync

Paginated query across all shards using a `ShardedPaginationOptions` object that specifies the page number, page size, and merge strategy.

```csharp
var spec = new ActiveOrdersSpec();
var pagination = new ShardedPaginationOptions
{
    Page = 2,
    PageSize = 20,
    Strategy = ShardedPaginationStrategy.OverfetchAndMerge
};

var result = await repo.QueryAllShardsPagedAsync(spec, pagination, ct);
// Returns Either<EncinaError, ShardedPagedResult<Order>>
```

### CountAllShardsAsync

A lightweight count-only operation that avoids fetching entity data. Returns the total count with a per-shard breakdown.

```csharp
var spec = new ActiveOrdersSpec();
var result = await repo.CountAllShardsAsync(spec, ct);
// Returns Either<EncinaError, ShardedCountResult>

result.Match(
    Right: r =>
    {
        Console.WriteLine($"Total: {r.TotalCount} across {r.ShardsQueried} shards");
        foreach (var (shardId, count) in r.CountPerShard)
            Console.WriteLine($"  {shardId}: {count}");
    },
    Left: error => Console.WriteLine($"Error: {error.Message}"));
```

### QueryShardsAsync

Queries specific shards by ID rather than all active shards. Useful when you know which shards contain relevant data based on routing logic, compound keys, or domain knowledge.

```csharp
var spec = new ActiveOrdersSpec();
var result = await repo.QueryShardsAsync(
    spec,
    ["shard-eu-west", "shard-eu-east"],
    ct);
// Returns Either<EncinaError, ShardedSpecificationResult<Order>>
```

At least one shard ID must be specified; passing an empty list throws `ArgumentException`.

---

## Pagination Strategies

Cross-shard pagination is fundamentally different from single-database pagination. No single shard has a global view of the data, so the coordinator must choose how to distribute page requests and merge results. Encina provides two strategies via the `ShardedPaginationStrategy` enum.

### OverfetchAndMerge (Default)

Fetches `PageSize` items from **each** shard, merges all results, applies the specification's ordering, and trims to the requested page.

```text
Page 2, PageSize 20, 3 shards:
  Shard-1: fetch 20 items → returns 20
  Shard-2: fetch 20 items → returns 15
  Shard-3: fetch 20 items → returns 20
  Merge: 55 items → order by spec → skip 20 → take 20 → page 2
```

| Aspect | Details |
|--------|---------|
| **Correctness** | Guaranteed correct ordering across shards |
| **Data transfer** | Up to `PageSize * ShardCount` items per request |
| **Round-trips** | One per shard (parallel) |
| **Best for** | Small to medium page sizes, few shards, strict ordering requirements |

### EstimateAndDistribute

First queries each shard for its count, then distributes the page request proportionally based on each shard's share of the total data.

```text
Page 2, PageSize 20, 3 shards:
  Phase 1 (counts): Shard-1: 500, Shard-2: 300, Shard-3: 200
  Phase 2 (proportional fetch):
    Shard-1: fetch 10 items (50% of 20)
    Shard-2: fetch 6 items (30% of 20)
    Shard-3: fetch 4 items (20% of 20)
  Merge: 20 items → order by spec → page 2
```

| Aspect | Details |
|--------|---------|
| **Correctness** | Slightly imprecise when data distribution changes between count and fetch |
| **Data transfer** | Approximately `PageSize` items total (minimal overfetch) |
| **Round-trips** | Two per shard (count + fetch, both parallel) |
| **Best for** | Large page sizes, many shards, minimizing data transfer |

### Configuration

```csharp
var pagination = new ShardedPaginationOptions
{
    Page = 2,       // 1-based, default: 1
    PageSize = 20,  // Items per page, default: 20
    Strategy = ShardedPaginationStrategy.OverfetchAndMerge  // default
};

var result = await repo.QueryAllShardsPagedAsync(spec, pagination, ct);
```

Both `Page` and `PageSize` validate that values are >= 1, throwing `ArgumentOutOfRangeException` for invalid values.

---

## Result Types

### ShardedSpecificationResult\<T\>

Returned by `QueryAllShardsAsync` and `QueryShardsAsync`.

| Property | Type | Description |
|----------|------|-------------|
| `Items` | `IReadOnlyList<T>` | Merged and ordered result items from all successful shards |
| `ItemsPerShard` | `IReadOnlyDictionary<string, int>` | Number of items returned by each successful shard |
| `TotalDuration` | `TimeSpan` | Wall-clock duration of the entire scatter-gather operation |
| `DurationPerShard` | `IReadOnlyDictionary<string, TimeSpan>` | Query duration for each individual shard |
| `FailedShards` | `IReadOnlyList<ShardFailure>` | Shards that failed, with error information |
| `IsComplete` | `bool` | `true` if all shards responded successfully |
| `IsPartial` | `bool` | `true` if some shards failed but results are available from others |
| `ShardsQueried` | `int` | Total number of shards queried (successful + failed) |

### ShardedPagedResult\<T\>

Returned by `QueryAllShardsPagedAsync`.

| Property | Type | Description |
|----------|------|-------------|
| `Items` | `IReadOnlyList<T>` | Merged and paginated result items |
| `TotalCount` | `long` | Total matching items across all successful shards |
| `Page` | `int` | The 1-based page number that was requested |
| `PageSize` | `int` | The page size that was requested |
| `TotalPages` | `int` | Total pages based on `TotalCount / PageSize` |
| `HasNextPage` | `bool` | `true` if `Page < TotalPages` |
| `HasPreviousPage` | `bool` | `true` if `Page > 1` |
| `CountPerShard` | `IReadOnlyDictionary<string, long>` | Total matching count per successful shard |
| `FailedShards` | `IReadOnlyList<ShardFailure>` | Shards that failed, with error information |
| `IsComplete` | `bool` | `true` if all shards responded successfully |
| `IsPartial` | `bool` | `true` if some shards failed but results are available from others |
| `ShardsQueried` | `int` | Total number of shards queried (successful + failed) |

### ShardedCountResult

Returned by `CountAllShardsAsync`.

| Property | Type | Description |
|----------|------|-------------|
| `TotalCount` | `long` | Total count across all successful shards |
| `CountPerShard` | `IReadOnlyDictionary<string, long>` | Count per successful shard |
| `FailedShards` | `IReadOnlyList<ShardFailure>` | Shards that failed, with error information |
| `IsComplete` | `bool` | `true` if all shards responded successfully |
| `IsPartial` | `bool` | `true` if some shards failed but results are available from others |
| `ShardsQueried` | `int` | Total number of shards queried (successful + failed) |

### Partial Failure Behavior

| Scenario | Result |
|----------|--------|
| All shards succeed | `Right` with `IsComplete = true`, `IsPartial = false` |
| Some shards fail | `Right` with `IsComplete = false`, `IsPartial = true` |
| All shards fail | `Left` with error code `encina.sharding.specification_scatter_gather_failed` |

When `IsPartial` is true, the `Items`, `TotalCount`, and per-shard dictionaries only reflect successful shards. The actual totals may be higher. Always check `IsPartial` before presenting aggregated numbers to users.

---

## Targeted Scatter-Gather

Use `QueryShardsAsync` to query a known subset of shards when you already know which shards contain relevant data. This avoids unnecessary queries to shards that cannot have matching results.

```csharp
// Only query European shards
var europeShards = new[] { "shard-eu-west", "shard-eu-east" };
var result = await repo.QueryShardsAsync(spec, europeShards, ct);

result.Match(
    Right: r =>
    {
        Console.WriteLine($"Found {r.Items.Count} items from {r.ShardsQueried} European shards");
        foreach (var (shardId, count) in r.ItemsPerShard)
            Console.WriteLine($"  {shardId}: {count} items");
    },
    Left: error => Console.WriteLine($"Error: {error.Message}"));
```

**When to use targeted scatter-gather**:

| Scenario | Approach |
|----------|----------|
| You know the geographic shard from user context | `QueryShardsAsync` with region-specific shard IDs |
| Compound key partial routing resolved to a shard subset | Pass the resolved shard IDs directly |
| Hot-path query where only one shard is relevant | `QueryShardsAsync` with a single shard ID |
| No shard routing information available | Use `QueryAllShardsAsync` instead |

---

## Observability

### Metrics

The `ShardRoutingMetrics` class emits the following metrics when `EnableSpecificationMetrics` is `true`:

| Metric | Type | Unit | Description |
|--------|------|------|-------------|
| `encina.sharding.specification.queries_total` | Counter | count | Total number of specification-based scatter-gather queries executed |
| `encina.sharding.specification.merge.duration_ms` | Histogram | ms | Duration of the merge/pagination step after per-shard results are collected |
| `encina.sharding.specification.items_per_shard` | Histogram | items | Distribution of items returned per shard |
| `encina.sharding.specification.shard_fan_out` | Histogram | shards | Number of shards queried per specification scatter-gather operation |

Tags applied to specification metrics:

| Tag | Values | Description |
|-----|--------|-------------|
| `encina.sharding.specification.type` | Specification class name | The concrete specification type |
| `encina.sharding.specification.operation` | `query`, `paged_query`, `count` | The operation kind |
| `encina.sharding.shard.count` | integer | Number of shards queried |
| `db.shard.id` | shard identifier | Per-shard item count metric |

### Tracing

The `ShardingActivitySource` (`"Encina.Sharding"`) emits the following activities:

| Activity Name | Kind | Tags | Description |
|---------------|------|------|-------------|
| `Encina.Sharding.SpecificationScatterGather` | Internal | `specification.type`, `specification.operation`, `shard.count` | Parent span for the entire scatter-gather operation |
| `Encina.Sharding.ShardQuery` | Client | `db.shard.id` | Child span for each individual shard query |

Paginated queries add additional tags to the parent span:

| Tag | Description |
|-----|-------------|
| `encina.sharding.specification.pagination.strategy` | `OverfetchAndMerge` or `EstimateAndDistribute` |
| `encina.sharding.specification.pagination.page` | Requested page number |
| `encina.sharding.specification.pagination.page_size` | Requested page size |

Completion tags on the parent span:

| Tag | Description |
|-----|-------------|
| `encina.sharding.scatter.success_count` | Number of successful shard queries |
| `encina.sharding.scatter.failed_count` | Number of failed shard queries |
| `encina.sharding.specification.total_items` | Total items in the result |
| `encina.sharding.specification.merge.duration_ms` | Duration of the merge step |

### Structured Logging

All provider implementations emit structured log messages including:

- Specification type name
- Number of shards queried
- Per-shard item counts
- Merge duration
- Failed shard details (when partial)

### Configuration

```csharp
services.Configure<ShardingMetricsOptions>(options =>
{
    options.EnableSpecificationMetrics = true;  // default: true
    options.EnableTracing = true;               // default: true
});
```

---

## Provider-Specific Notes

Specification-based scatter-gather is supported across all 13 database providers:

| Provider Category | Providers | Status |
|-------------------|-----------|--------|
| **EF Core** | SqlServer, PostgreSQL, MySQL, SQLite | Supported |
| **Dapper** | SqlServer, PostgreSQL, MySQL, SQLite | Supported |
| **ADO.NET** | SqlServer, PostgreSQL, MySQL, SQLite | Supported |
| **MongoDB** | MongoDB | Supported |

### EF Core

Specifications are translated to `IQueryable<T>` via the specification's `ToExpression()` method. This provides full LINQ support including navigation property access and complex expressions:

```csharp
// The provider internally does:
var query = dbContext.Set<Order>()
    .Where(specification.ToExpression());

if (specification is IQuerySpecification<Order> querySpec)
{
    if (querySpec.OrderBy is not null)
        query = query.OrderBy(querySpec.OrderBy);
    // ... additional ordering, includes, etc.
}
```

### Dapper and ADO.NET

Specifications are translated to parameterized SQL via `SpecificationSqlBuilder`. The SQL builder handles provider-specific differences in quoting and syntax:

| Provider | Column Quoting | Parameter Prefix |
|----------|---------------|-----------------|
| SQL Server | `[Column]` | `@param` |
| PostgreSQL | `"Column"` | `@param` |
| MySQL | `` `Column` `` | `@param` |
| SQLite | `"Column"` | `@param` |

### MongoDB

Specifications are translated to `FilterDefinition<T>` and `SortDefinition<T>` via `SpecificationFilterBuilder`:

```csharp
// The provider internally does:
var filter = SpecificationFilterBuilder.Build(specification);
var sort = SpecificationSortBuilder.Build(specification);

var results = await collection
    .Find(filter)
    .Sort(sort)
    .ToListAsync(ct);
```

### Unsupported Repositories

If a repository does not implement `IShardedSpecificationSupport`, the extension methods throw `NotSupportedException` immediately (no database calls are made).

---

## Performance Considerations

| Factor | Recommendation |
|--------|---------------|
| **Specification complexity** | Simple criteria add negligible overhead; complex expressions with navigation properties may increase per-shard query time |
| **Ordering** | Cross-shard ordering requires an in-memory sort of merged results via `ScatterGatherResultMerger`. Ensure the total result set fits in memory |
| **Page size** | With `OverfetchAndMerge`, the data transferred is up to `PageSize * ShardCount`. Use `EstimateAndDistribute` for large pages with many shards |
| **Shard count** | More shards means more parallel queries. Use targeted scatter-gather (`QueryShardsAsync`) when you know the relevant shards |
| **Result caching** | Consider caching specification results for read-heavy workloads. Specifications are value objects and work well as cache keys |
| **Count operations** | Use `CountAllShardsAsync` instead of fetching all entities and counting in memory |
| **Merge step** | The merge step runs in O(n log n) where n is the total items across all shards. Monitor `specification.merge.duration_ms` for large result sets |

### Memory Profile

- **Query operations**: All per-shard items are held in memory during the merge step. For large result sets, consider paginated queries
- **Count operations**: Constant memory overhead (one `long` per shard), regardless of entity count
- **Paged operations**: With `OverfetchAndMerge`, up to `PageSize * ShardCount` items are held during merge, then trimmed to `PageSize`

---

## Migration from Lambda-Based Queries

### Before (Lambda-Based)

```csharp
// Ad-hoc lambda for each scatter-gather query
var result = await repo.QueryAllShardsAsync(
    async (shardId, ct) =>
    {
        // Manual per-shard query logic
        return await GetActiveOrdersFromShard(shardId, ct);
    },
    cancellationToken);
```

Problems with the lambda approach:

- Query logic is inline and not reusable
- Ordering must be applied manually after merging
- No per-shard metadata (item counts, durations)
- No built-in pagination support
- Testing requires mocking the entire shard infrastructure

### After (Specification-Based)

```csharp
// Reusable specification
public class ActiveOrdersSpec : QuerySpecification<Order>
{
    public ActiveOrdersSpec()
    {
        AddCriteria(o => o.Status == OrderStatus.Active);
        ApplyOrderByDescending(o => o.CreatedAtUtc);
    }
}

// Clean scatter-gather call
var spec = new ActiveOrdersSpec();
var result = await repo.QueryAllShardsAsync(spec, ct);
```

### Benefits of the Specification Approach

| Aspect | Lambda-Based | Specification-Based |
|--------|-------------|-------------------|
| **Reusability** | Inline, one-off | Same spec works for single-shard and cross-shard |
| **Testability** | Requires shard infrastructure | Spec testable in isolation |
| **Ordering** | Manual post-merge | Automatic from spec via `ScatterGatherResultMerger` |
| **Per-shard metadata** | Not included | Item counts, durations, failures per shard |
| **Pagination** | Not built-in | Built-in with configurable strategies |
| **Provider translation** | Manual per provider | Automatic (EF Core, Dapper, ADO.NET, MongoDB) |
| **Observability** | Manual instrumentation | Automatic metrics and tracing |

### Coexistence

The lambda-based `QueryAllShardsAsync` (from `IFunctionalShardedRepository`) and the specification-based `QueryAllShardsAsync` (from `ShardedSpecificationExtensions`) coexist. The specification-based methods are additional extension methods, not replacements. Existing lambda-based code continues to work without changes.

---

## Error Codes

Specification scatter-gather operations may produce the following error codes:

| Error Code | Constant | Cause |
|------------|----------|-------|
| `encina.sharding.specification_scatter_gather_failed` | `ShardingErrorCodes.SpecificationScatterGatherFailed` | All shards failed during the scatter-gather operation |
| `encina.sharding.specification_scatter_gather_partial_failure` | `ShardingErrorCodes.SpecificationScatterGatherPartialFailure` | Some shards failed (result still returned with `IsPartial = true`) |
| `encina.sharding.pagination_merge_failed` | `ShardingErrorCodes.PaginationMergeFailed` | The pagination merge phase failed |
| `encina.sharding.no_active_shards` | `ShardingErrorCodes.NoActiveShards` | The topology has no active shards to query |

---

## Related Documentation

- [Distributed Aggregations](distributed-aggregations.md) -- Cross-shard Count, Sum, Avg, Min, Max operations
- [Compound Shard Keys](compound-shard-keys.md) -- Multi-field routing and partial key scatter-gather
