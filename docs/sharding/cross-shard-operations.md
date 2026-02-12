# Cross-Shard Operations

This guide covers how to work with data that spans multiple shards, including scatter-gather queries, transaction limitations, aggregation strategies, and the Saga pattern for distributed workflows.

## Table of Contents

1. [Transaction Limitations](#transaction-limitations)
2. [Saga Pattern for Cross-Shard Workflows](#saga-pattern-for-cross-shard-workflows)
3. [Scatter-Gather Queries](#scatter-gather-queries)
4. [Specification-Based Scatter-Gather](#specification-based-scatter-gather)
5. [Partial Failure Handling](#partial-failure-handling)
6. [Aggregation Strategies](#aggregation-strategies)
7. [JOIN Alternative Patterns](#join-alternative-patterns)
8. [Observability](#observability)
9. [FAQ](#faq)

---

## Transaction Limitations

### ACID Transactions Are Shard-Local

Each shard operates as an independent database. ACID transactions apply only within a single shard:

```text
┌─────────────────────────────────────────────────────────────────┐
│                    Transaction Boundaries                         │
│                                                                   │
│  ┌──────────────┐    ┌──────────────┐    ┌──────────────┐        │
│  │   Shard 1    │    │   Shard 2    │    │   Shard 3    │        │
│  │              │    │              │    │              │        │
│  │  BEGIN       │    │  BEGIN       │    │  BEGIN       │        │
│  │  INSERT ...  │    │  UPDATE ...  │    │  DELETE ...  │        │
│  │  UPDATE ...  │    │  INSERT ...  │    │  INSERT ...  │        │
│  │  COMMIT ✓    │    │  COMMIT ✓    │    │  ROLLBACK ✗  │        │
│  │              │    │              │    │              │        │
│  │  Atomic ✓    │    │  Atomic ✓    │    │  Atomic ✓    │        │
│  └──────────────┘    └──────────────┘    └──────────────┘        │
│                                                                   │
│  ✗ No cross-shard ACID guarantees                                 │
│  ✗ Shard 3 rolled back while Shard 1 & 2 committed               │
└─────────────────────────────────────────────────────────────────┘
```

**Why no two-phase commit?** Distributed transactions (2PC) add significant latency (2-3x), require lock coordination across shards, and create availability risks when coordinators fail. Encina prioritizes availability and performance over strict cross-shard consistency.

### What This Means in Practice

| Operation | Single-Shard | Cross-Shard |
|-----------|:------------:|:-----------:|
| Insert entity | ACID guaranteed | N/A (entity lives on one shard) |
| Update entity | ACID guaranteed | N/A (entity lives on one shard) |
| Transfer between entities on same shard | ACID guaranteed | N/A |
| Transfer between entities on different shards | N/A | Use Saga pattern |
| Scatter-gather query | N/A | Eventual consistency |

---

## Saga Pattern for Cross-Shard Workflows

For operations that span multiple shards, use the Saga pattern from Encina.Messaging to coordinate with compensation logic.

### Architecture

```text
┌─────────────────────────────────────────────────────────────────┐
│                    Saga Orchestrator                               │
│                                                                   │
│  Step 1: Debit source account (Shard A)                           │
│       │                                                           │
│       ├── Success ──► Step 2: Credit target account (Shard B)     │
│       │                   │                                       │
│       │                   ├── Success ──► Complete ✓               │
│       │                   │                                       │
│       │                   └── Failure ──► Compensate Step 1       │
│       │                                   (Refund source)         │
│       │                                                           │
│       └── Failure ──► Abort (nothing to compensate)               │
└─────────────────────────────────────────────────────────────────┘
```

### Example: Order Fulfillment Across Shards

```csharp
public class OrderFulfillmentSaga
{
    private readonly IFunctionalShardedRepository<Order, string> _orderRepo;
    private readonly IFunctionalShardedRepository<Inventory, string> _inventoryRepo;

    public async Task<Either<EncinaError, Unit>> FulfillOrderAsync(
        Order order, string productId, int quantity)
    {
        // Step 1: Reserve inventory (Shard B — routed by productId)
        var reserveResult = await ReserveInventoryAsync(productId, quantity);

        return await reserveResult.MatchAsync(
            RightAsync: async _ =>
            {
                // Step 2: Confirm order (Shard A — routed by order's shard key)
                var confirmResult = await ConfirmOrderAsync(order);

                return await confirmResult.MatchAsync(
                    RightAsync: _ => Task.FromResult<Either<EncinaError, Unit>>(Unit.Default),
                    LeftAsync: async error =>
                    {
                        // Compensate: release the reserved inventory
                        await ReleaseInventoryAsync(productId, quantity);
                        return error;
                    });
            },
            LeftAsync: error => Task.FromResult<Either<EncinaError, Unit>>(error));
    }

    private async Task<Either<EncinaError, Unit>> ReserveInventoryAsync(
        string productId, int quantity)
    {
        // Execute against the shard that owns this product
        var inventory = await _inventoryRepo.GetByIdAsync(productId, productId);
        return await inventory.BindAsync(async inv =>
        {
            inv.AvailableQuantity -= quantity;
            return await _inventoryRepo.UpdateAsync(inv);
        });
    }

    private async Task<Either<EncinaError, Unit>> ReleaseInventoryAsync(
        string productId, int quantity)
    {
        // Compensation: restore inventory
        var inventory = await _inventoryRepo.GetByIdAsync(productId, productId);
        return await inventory.BindAsync(async inv =>
        {
            inv.AvailableQuantity += quantity;
            return await _inventoryRepo.UpdateAsync(inv);
        });
    }

    private async Task<Either<EncinaError, Unit>> ConfirmOrderAsync(Order order)
    {
        order.Status = OrderStatus.Confirmed;
        return await _orderRepo.UpdateAsync(order);
    }
}
```

### Saga Design Guidelines

1. **Each step should be idempotent** — Retrying a step produces the same result
2. **Each step needs a compensating action** — Undo logic for when later steps fail
3. **Order matters** — Perform the most likely-to-fail step first to minimize compensations
4. **Track saga state** — Use `SagaState` from Encina.Messaging for persistent orchestration
5. **Handle partial compensations** — Compensation actions can also fail; build retry logic

---

## Scatter-Gather Queries

### Using IShardedQueryExecutor

For read operations that span multiple shards, use the scatter-gather pattern:

```csharp
public class OrderReportService
{
    private readonly IFunctionalShardedRepository<Order, string> _repository;

    // Query all shards
    public async Task<Either<EncinaError, ShardedQueryResult<Order>>> GetAllOrdersAsync(
        CancellationToken cancellationToken = default)
    {
        return await _repository.QueryAllShardsAsync(
            async (shardId, ct) =>
            {
                // This factory runs against each shard independently
                // Return your query results for this specific shard
                // Implementation depends on your data access pattern
                return await GetOrdersFromShardAsync(shardId, ct);
            },
            cancellationToken);
    }

    // Query specific shards
    public async Task<Either<EncinaError, ShardedQueryResult<Order>>> GetOrdersForRegionsAsync(
        IReadOnlyList<string> shardIds,
        CancellationToken cancellationToken = default)
    {
        return await _repository.QueryShardsAsync(
            shardIds,
            async (shardId, ct) => await GetOrdersFromShardAsync(shardId, ct),
            cancellationToken);
    }
}
```

### ScatterGatherOptions Configuration

```csharp
services.AddEncinaSharding<Order>(options =>
{
    options.UseHashRouting()
        .AddShard("shard-1", conn1)
        .AddShard("shard-2", conn2)
        .AddShard("shard-3", conn3);

    // Configure scatter-gather behavior
    options.ScatterGatherOptions.MaxParallelism = 4;
    options.ScatterGatherOptions.Timeout = TimeSpan.FromSeconds(15);
    options.ScatterGatherOptions.AllowPartialResults = true;
});
```

### Performance Considerations

| Factor | Recommendation |
|--------|---------------|
| **Shard count** | Query only relevant shards when possible (use `QueryShardsAsync` with specific IDs) |
| **Parallelism** | Default is unlimited; set `MaxParallelism` to limit connection usage |
| **Timeout** | Set to 2x the P95 single-shard query time |
| **Result size** | Apply filtering and pagination within each shard query, not after aggregation |
| **Memory** | Large scatter-gather results are held in memory; stream or paginate if possible |

---

## Specification-Based Scatter-Gather

> **Added in v0.12.0** — [Feature Guide](../features/specification-scatter-gather.md) | [Issue #652](https://github.com/dlrivada/Encina/issues/652)

Instead of writing lambda expressions for each shard query, reuse existing domain specifications:

### Basic Usage

```csharp
// Define a reusable specification
public class ActiveOrdersSpec : QuerySpecification<Order>
{
    public ActiveOrdersSpec()
    {
        AddCriteria(o => o.Status == OrderStatus.Active);
        ApplyOrderByDescending(o => o.CreatedAtUtc);
    }
}

// Query all shards using the specification
var spec = new ActiveOrdersSpec();
var result = await shardedRepo.QueryAllShardsAsync(spec, ct);

result.Match(
    Right: r => Console.WriteLine($"Found {r.Items.Count} items across {r.ShardsQueried} shards"),
    Left: error => Console.WriteLine($"Error: {error.Message}"));
```

### Cross-Shard Pagination

```csharp
var pagination = new ShardedPaginationOptions
{
    Page = 2,
    PageSize = 20,
    Strategy = ShardedPaginationStrategy.OverfetchAndMerge
};

var result = await shardedRepo.QueryAllShardsPagedAsync(spec, pagination, ct);

result.Match(
    Right: r => Console.WriteLine($"Page {r.Page}/{r.TotalPages} ({r.TotalCount} total)"),
    Left: error => Console.WriteLine($"Error: {error.Message}"));
```

### Count Across Shards

```csharp
var countResult = await shardedRepo.CountAllShardsAsync(spec, ct);

countResult.Match(
    Right: r =>
    {
        Console.WriteLine($"Total: {r.TotalCount}");
        foreach (var (shardId, count) in r.CountPerShard)
            Console.WriteLine($"  Shard {shardId}: {count}");
    },
    Left: error => Console.WriteLine($"Error: {error.Message}"));
```

### Targeted Scatter-Gather

```csharp
// Query only specific shards
var europeShards = new[] { "shard-eu-west", "shard-eu-east" };
var result = await shardedRepo.QueryShardsAsync(spec, europeShards, ct);
```

> For detailed documentation including pagination strategies, result types, and provider-specific notes, see the [Specification-Based Scatter-Gather Guide](../features/specification-scatter-gather.md).

---

## Partial Failure Handling

### ShardedQueryResult Structure

Scatter-gather operations return detailed success/failure information:

```csharp
var result = await repository.QueryAllShardsAsync(queryFactory, ct);

result.IfRight(queryResult =>
{
    // Aggregated results from all successful shards
    var allOrders = queryResult.Results;

    // Check completeness
    if (queryResult.IsComplete)
    {
        // All shards responded successfully
        logger.LogInformation("Query complete: {Count} results from {Shards} shards",
            allOrders.Count, queryResult.SuccessfulShards.Count);
    }
    else if (queryResult.IsPartial)
    {
        // Some shards failed
        logger.LogWarning("Partial results: {Success} shards OK, {Failed} failed",
            queryResult.SuccessfulShards.Count, queryResult.FailedShards.Count);

        foreach (var failure in queryResult.FailedShards)
        {
            logger.LogError("Shard {ShardId} failed: {Error}",
                failure.ShardId, failure.Error.Message);
        }
    }
});
```

### AllowPartialResults Behavior

| Setting | Shard Failure Behavior |
|---------|----------------------|
| `true` (default) | Return results from healthy shards; `FailedShards` list contains errors |
| `false` | Return `Left` with error code `encina.sharding.scatter_gather_partial_failure` |

### Retry Strategies

```csharp
// Strategy 1: Retry failed shards only
var result = await repository.QueryAllShardsAsync(queryFactory, ct);

result.IfRight(async queryResult =>
{
    if (!queryResult.IsComplete)
    {
        var failedShardIds = queryResult.FailedShards.Select(f => f.ShardId).ToList();

        // Retry only the failed shards
        var retryResult = await repository.QueryShardsAsync(
            failedShardIds, queryFactory, ct);

        // Merge results...
    }
});

// Strategy 2: Full retry with exponential backoff (use Polly)
// See Encina.Polly for resilience integration
```

---

## Aggregation Strategies

### Client-Side Aggregation

Perform aggregation in the application after collecting results from all shards:

```csharp
var result = await repository.QueryAllShardsAsync(
    async (shardId, ct) => await GetOrderTotalsFromShard(shardId, ct),
    ct);

result.IfRight(queryResult =>
{
    // SUM across shards
    var totalRevenue = queryResult.Results.Sum(o => o.Total);

    // COUNT across shards
    var totalOrders = queryResult.Results.Count;

    // AVG across shards (weighted by count)
    var avgOrderValue = totalRevenue / totalOrders;
});
```

### Shard-Local Pre-Aggregation

Reduce data transfer by aggregating within each shard query:

```csharp
// Instead of fetching all rows, aggregate per shard
var result = await repository.QueryAllShardsAsync(
    async (shardId, ct) =>
    {
        // Each shard returns a single summary row
        var summary = await connection.QuerySingleAsync<ShardSummary>(
            "SELECT COUNT(*) AS OrderCount, SUM(total) AS Revenue FROM orders WHERE ...",
            cancellationToken: ct);

        return new Either<EncinaError, IReadOnlyList<ShardSummary>>(
            new List<ShardSummary> { summary });
    },
    ct);

// Merge shard summaries (much less data than all rows)
result.IfRight(queryResult =>
{
    var totalOrders = queryResult.Results.Sum(s => s.OrderCount);
    var totalRevenue = queryResult.Results.Sum(s => s.Revenue);
});
```

### Aggregation Reference

| Operation | Strategy | Notes |
|-----------|----------|-------|
| **COUNT** | Sum of per-shard counts | Exact |
| **SUM** | Sum of per-shard sums | Exact |
| **AVG** | Sum of sums / sum of counts | Must track both values |
| **MIN/MAX** | Min/Max of per-shard results | Exact |
| **DISTINCT** | Union of per-shard distinct sets | May have duplicates near shard boundaries; deduplicate in memory |
| **TOP N** | Merge per-shard Top N results, re-sort, take N | Each shard must return at least N results |
| **GROUP BY** | Merge per-shard groups by key | Sum/count values for matching keys |

---

## JOIN Alternative Patterns

Cross-shard JOINs are not supported. Use these alternative patterns:

### 1. Denormalization

Store frequently joined data directly on the entity:

```csharp
// Instead of: SELECT o.*, c.Name FROM orders o JOIN customers c ON ...
// Store customer name on the order
public class Order : IShardable
{
    public string OrderId { get; set; }
    public string CustomerId { get; set; }
    public string CustomerName { get; set; } // Denormalized from Customer
    public string GetShardKey() => CustomerId;
}
```

**Trade-off**: Increases storage; requires updating denormalized fields when source changes.

### 2. Shard Key Co-Location

Ensure entities that are frequently queried together share the same shard key:

```csharp
// Orders and OrderItems share CustomerId as shard key
// Both live on the same shard → local JOIN possible
public class Order : IShardable
{
    public string CustomerId { get; set; }
    public string GetShardKey() => CustomerId;
}

public class OrderItem : IShardable
{
    public string CustomerId { get; set; } // Same shard key as Order
    public string GetShardKey() => CustomerId;
}
```

### 3. Application-Side JOIN

Fetch from multiple shards and join in memory:

```csharp
// Fetch orders from shard A, customers from shard B, join in application
var orders = await orderRepo.QueryAllShardsAsync(getOrders, ct);
var customers = await customerRepo.QueryAllShardsAsync(getCustomers, ct);

// Join in memory
var enrichedOrders = from o in orders.Results
                     join c in customers.Results on o.CustomerId equals c.Id
                     select new { Order = o, Customer = c };
```

**Trade-off**: Higher memory usage; latency of two scatter-gather operations.

### 4. Reference Tables

Replicate small, rarely-changing tables to all shards:

```text
┌──────────┐  ┌──────────┐  ┌──────────┐
│ Shard 1  │  │ Shard 2  │  │ Shard 3  │
│          │  │          │  │          │
│ orders   │  │ orders   │  │ orders   │
│ countries│  │ countries│  │ countries│  ◄── Same data on every shard
│ currencies│ │ currencies│ │ currencies│
└──────────┘  └──────────┘  └──────────┘
```

Reference tables enable local JOINs with lookup data. Keep them small and update infrequently.

---

## Observability

### Distributed Tracing

Cross-shard operations create a trace hierarchy:

```text
ScatterGather [span]
  ├── ShardQuery: shard-1 [span, ActivityKind.Client]
  ├── ShardQuery: shard-2 [span, ActivityKind.Client]
  └── ShardQuery: shard-3 [span, ActivityKind.Client]
```

Each span includes:

- `shard.id` — The target shard identifier
- `shard.count` — Total shards in the scatter-gather
- `scatter.strategy` — "all" or "targeted"

### Key Metrics for Cross-Shard Operations

| Metric | What It Tells You |
|--------|------------------|
| `encina.sharding.scatter.duration_ms` | Total scatter-gather latency; the slowest shard dominates |
| `encina.sharding.scatter.shard.duration_ms` | Per-shard query time; identify slow shards |
| `encina.sharding.scatter.partial_failures` | Shard reliability; recurring failures need investigation |
| `encina.sharding.scatter.queries.active` | Concurrency level; high values may indicate connection pool pressure |

### Identifying Slow Shards

Use the per-shard duration histogram to find outliers:

```promql
# P99 per-shard query time, grouped by shard
histogram_quantile(0.99,
  sum by (le, shard_id) (
    rate(encina_sharding_scatter_shard_duration_ms_bucket[5m])
  )
)
```

If one shard consistently shows higher latency, investigate:

- Database load and query plans on that shard
- Network latency to that shard
- Data skew (more data or more queries to that shard)

---

## FAQ

### Can I use distributed transactions across shards?

Not with Encina's sharding (by design). Use the Saga pattern for workflows that span shards. MongoDB 4.2+ supports cross-shard transactions natively when using mongos, but this is a MongoDB-specific feature and not part of Encina's abstraction.

### What if a Saga compensation fails?

Compensation actions should be idempotent and retryable. If a compensation persistently fails, the saga enters a "failed" state that requires manual intervention. Use Encina.Messaging's `SagaState` to track and recover from such failures.

### How do I paginate scatter-gather results?

Apply LIMIT/OFFSET within each shard query. For consistent global pagination:

1. Each shard returns `pageSize` results sorted by the same criteria
2. Merge and re-sort the combined results
3. Take the first `pageSize` items from the merged set

> **Caveat**: This over-fetches by `(shardCount - 1) * pageSize` rows. For deep pagination, consider shard-local cursors or seek-based pagination.

### Can scatter-gather queries use indexes?

Yes. Each shard query runs independently against that shard's database. Ensure all shards have the same indexes. The scatter-gather framework only handles orchestration; the actual query execution uses your normal data access patterns.

### How do I handle global unique constraints?

Uniqueness is guaranteed within a shard but not across shards. Options:

1. **Include shard key in the unique constraint** — Most natural approach; unique within a tenant/region
2. **Use UUIDs** — Globally unique by construction; no cross-shard coordination needed
3. **Central sequence service** — A dedicated service generates unique IDs (adds latency and a single point of failure)
4. **Snowflake-style IDs** — Combine timestamp + shard ID + sequence for globally unique, sortable IDs
