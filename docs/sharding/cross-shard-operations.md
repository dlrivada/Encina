# Cross-Shard Operations

This guide covers how to work with data that spans multiple shards, including scatter-gather queries, transaction limitations, aggregation strategies, and the Saga pattern for distributed workflows.

## Table of Contents

1. [Transaction Limitations](#transaction-limitations)
2. [Saga Pattern for Cross-Shard Workflows](#saga-pattern-for-cross-shard-workflows)
   - [Architecture](#architecture)
   - [Saga Data Design](#saga-data-design)
   - [Example: Entity Transfer Between Shards](#example-entity-transfer-between-shards)
   - [Example: Cross-Shard Funds Transfer](#example-cross-shard-funds-transfer)
   - [Example: Cross-Shard Order Fulfillment](#example-cross-shard-order-fulfillment)
   - [Idempotency Patterns](#idempotency-patterns)
   - [Saga Design Guidelines](#saga-design-guidelines)
   - [When to Use Sagas vs. Other Approaches](#when-to-use-sagas-vs-other-approaches)
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
│                    Transaction Boundaries                       │
│                                                                 │
│  ┌──────────────┐    ┌──────────────┐    ┌──────────────┐       │
│  │   Shard 1    │    │   Shard 2    │    │   Shard 3    │       │
│  │              │    │              │    │              │       │
│  │  BEGIN       │    │  BEGIN       │    │  BEGIN       │       │
│  │  INSERT ...  │    │  UPDATE ...  │    │  DELETE ...  │       │
│  │  UPDATE ...  │    │  INSERT ...  │    │  INSERT ...  │       │
│  │  COMMIT ✓    │    │  COMMIT ✓   │    │  ROLLBACK ✗  │       │
│  │              │    │              │    │              │       │
│  │  Atomic ✓    │    │  Atomic ✓   │    │  Atomic ✓    │       │
│  └──────────────┘    └──────────────┘    └──────────────┘       │
│                                                                 │
│  ✗ No cross-shard ACID guarantees                               │
│  ✗ Shard 3 rolled back while Shard 1 & 2 committed              │
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

For write operations that span multiple shards, use the Saga pattern from `Encina.Messaging.Sagas.LowCeremony` to coordinate steps with automatic compensation on failure.

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

`SagaRunner` from `Encina.Messaging.Sagas.LowCeremony` handles the full lifecycle automatically:

- **Step orchestration** — Executes steps sequentially, advancing state after each
- **State persistence** — Each step is persisted to `ISagaStore` (all 13 database providers)
- **Compensation** — On failure, runs compensation in reverse order for completed steps
- **Timeout management** — Optional per-saga timeout via `WithTimeout`
- **Crash recovery** — `SagaOrchestrator.GetStuckSagasAsync` detects incomplete sagas after a process restart

### Saga Data Design

When coordinating across shards, include shard routing information in the saga data so each step (and its compensation) can target the correct shard:

```csharp
public sealed record CrossShardTransferData
{
    // Identifiers
    public string EntityId { get; init; } = string.Empty;

    // Shard routing — each step uses these to target the right shard
    public string SourceShardKey { get; init; } = string.Empty;
    public string TargetShardKey { get; init; } = string.Empty;

    // Completion flags — compensation only runs when the step actually completed
    public bool InsertedInTarget { get; init; }
    public bool DeletedFromSource { get; init; }
}
```

**Key principles:**

- Store **source and target shard keys** so compensation steps can route correctly
- Track **completion flags** (`InsertedInTarget`, `DeletedFromSource`) for idempotent compensation — only undo what was actually done
- Use `record` types with `init` properties and `with` expressions for immutable state transitions between steps

### Example: Entity Transfer Between Shards

Moving an entity from one shard to another (e.g., rebalancing, user migration):

```csharp
public class CustomerTransferService
{
    private readonly IFunctionalShardedRepository<Customer, string> _repo;
    private readonly ISagaRunner _sagaRunner;

    public async Task<Either<EncinaError, SagaResult<CrossShardTransferData>>>
        TransferCustomerAsync(
            string customerId,
            string sourceShardKey,
            string targetShardKey,
            CancellationToken ct)
    {
        var definition = SagaDefinition.Create<CrossShardTransferData>("CustomerTransfer")
            .Step("Insert into target shard")
                .Execute(async (data, ct) =>
                {
                    // Read from source shard
                    var customerResult = await _repo.GetByIdAsync(
                        customerId, data.SourceShardKey, ct);

                    return await customerResult.BindAsync(async customer =>
                    {
                        // Insert into target shard (AddAsync routes by shard key)
                        var clone = customer.WithShardKey(data.TargetShardKey);
                        var addResult = await _repo.AddAsync(clone, ct);
                        return addResult.Map(_ =>
                            data with { InsertedInTarget = true });
                    });
                })
                .Compensate(async (data, ct) =>
                {
                    if (data.InsertedInTarget)
                    {
                        await _repo.DeleteAsync(
                            data.EntityId, data.TargetShardKey, ct);
                    }
                })
            .Step("Delete from source shard")
                .Execute(async (data, ct) =>
                {
                    var deleteResult = await _repo.DeleteAsync(
                        data.EntityId, data.SourceShardKey, ct);
                    return deleteResult.Map(_ =>
                        data with { DeletedFromSource = true });
                })
                .Compensate(async (data, ct) =>
                {
                    if (data.DeletedFromSource)
                    {
                        // Re-read from target and re-insert in source
                        var customerResult = await _repo.GetByIdAsync(
                            data.EntityId, data.TargetShardKey, ct);
                        await customerResult.IfRightAsync(async customer =>
                        {
                            var original = customer.WithShardKey(data.SourceShardKey);
                            await _repo.AddAsync(original, ct);
                        });
                    }
                })
            .WithTimeout(TimeSpan.FromMinutes(5))
            .Build();

        return await _sagaRunner.RunAsync(definition,
            new CrossShardTransferData
            {
                EntityId = customerId,
                SourceShardKey = sourceShardKey,
                TargetShardKey = targetShardKey,
            }, ct);
    }
}
```

### Example: Cross-Shard Funds Transfer

Transferring funds between accounts that live on different shards. The **debit step comes first** because insufficient funds is the most likely failure — this minimizes the number of compensations:

```csharp
public sealed record FundsTransferData
{
    public string SourceAccountId { get; init; } = string.Empty;
    public string TargetAccountId { get; init; } = string.Empty;
    public string SourceShardKey { get; init; } = string.Empty;
    public string TargetShardKey { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public bool Debited { get; init; }
    public bool Credited { get; init; }
    public string? TransactionRef { get; init; }
}

public class FundsTransferService
{
    private readonly IFunctionalShardedRepository<Account, string> _accountRepo;
    private readonly ISagaRunner _sagaRunner;

    public async Task<Either<EncinaError, SagaResult<FundsTransferData>>>
        TransferAsync(
            string sourceAccountId, string sourceShardKey,
            string targetAccountId, string targetShardKey,
            decimal amount, CancellationToken ct)
    {
        var definition = SagaDefinition.Create<FundsTransferData>("FundsTransfer")
            .Step("Debit source account")
                .Execute(async (data, ct) =>
                {
                    var accountResult = await _accountRepo.GetByIdAsync(
                        data.SourceAccountId, data.SourceShardKey, ct);

                    return await accountResult.BindAsync(async account =>
                    {
                        if (account.Balance < data.Amount)
                            return EncinaErrors.Create(
                                "transfer.insufficient_funds",
                                $"Insufficient balance: {account.Balance} < {data.Amount}");

                        account.Balance -= data.Amount;
                        var updateResult = await _accountRepo.UpdateAsync(account, ct);
                        return updateResult.Map(_ =>
                            data with { Debited = true, TransactionRef = Guid.NewGuid().ToString() });
                    });
                })
                .Compensate(async (data, ct) =>
                {
                    if (data.Debited)
                    {
                        var accountResult = await _accountRepo.GetByIdAsync(
                            data.SourceAccountId, data.SourceShardKey, ct);
                        await accountResult.IfRightAsync(async account =>
                        {
                            account.Balance += data.Amount;
                            await _accountRepo.UpdateAsync(account, ct);
                        });
                    }
                })
            .Step("Credit target account")
                .Execute(async (data, ct) =>
                {
                    var accountResult = await _accountRepo.GetByIdAsync(
                        data.TargetAccountId, data.TargetShardKey, ct);

                    return await accountResult.BindAsync(async account =>
                    {
                        account.Balance += data.Amount;
                        var updateResult = await _accountRepo.UpdateAsync(account, ct);
                        return updateResult.Map(_ =>
                            data with { Credited = true });
                    });
                })
                .Compensate(async (data, ct) =>
                {
                    if (data.Credited)
                    {
                        var accountResult = await _accountRepo.GetByIdAsync(
                            data.TargetAccountId, data.TargetShardKey, ct);
                        await accountResult.IfRightAsync(async account =>
                        {
                            account.Balance -= data.Amount;
                            await _accountRepo.UpdateAsync(account, ct);
                        });
                    }
                })
            .WithTimeout(TimeSpan.FromMinutes(2))
            .Build();

        return await _sagaRunner.RunAsync(definition,
            new FundsTransferData
            {
                SourceAccountId = sourceAccountId,
                SourceShardKey = sourceShardKey,
                TargetAccountId = targetAccountId,
                TargetShardKey = targetShardKey,
                Amount = amount,
            }, ct);
    }
}
```

### Example: Cross-Shard Order Fulfillment

Reserving inventory (routed by product ID on Shard B) and confirming an order (routed by customer ID on Shard A):

```csharp
public sealed record OrderFulfillmentData
{
    public string OrderId { get; init; } = string.Empty;
    public string CustomerShardKey { get; init; } = string.Empty;
    public string ProductId { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public bool InventoryReserved { get; init; }
    public bool OrderConfirmed { get; init; }
}

public class OrderFulfillmentService
{
    private readonly IFunctionalShardedRepository<Order, string> _orderRepo;
    private readonly IFunctionalShardedRepository<Inventory, string> _inventoryRepo;
    private readonly ISagaRunner _sagaRunner;

    public async Task<Either<EncinaError, SagaResult<OrderFulfillmentData>>>
        FulfillOrderAsync(
            string orderId, string customerShardKey,
            string productId, int quantity,
            CancellationToken ct)
    {
        var definition = SagaDefinition.Create<OrderFulfillmentData>("OrderFulfillment")
            .Step("Reserve inventory")
                .Execute(async (data, ct) =>
                {
                    // Inventory lives on the shard determined by productId
                    var invResult = await _inventoryRepo.GetByIdAsync(
                        data.ProductId, data.ProductId, ct);

                    return await invResult.BindAsync(async inv =>
                    {
                        if (inv.AvailableQuantity < data.Quantity)
                            return EncinaErrors.Create(
                                "fulfillment.insufficient_stock",
                                $"Only {inv.AvailableQuantity} available");

                        inv.AvailableQuantity -= data.Quantity;
                        var updateResult = await _inventoryRepo.UpdateAsync(inv, ct);
                        return updateResult.Map(_ =>
                            data with { InventoryReserved = true });
                    });
                })
                .Compensate(async (data, ct) =>
                {
                    if (data.InventoryReserved)
                    {
                        var invResult = await _inventoryRepo.GetByIdAsync(
                            data.ProductId, data.ProductId, ct);
                        await invResult.IfRightAsync(async inv =>
                        {
                            inv.AvailableQuantity += data.Quantity;
                            await _inventoryRepo.UpdateAsync(inv, ct);
                        });
                    }
                })
            .Step("Confirm order")
                .Execute(async (data, ct) =>
                {
                    // Order lives on the shard determined by customerShardKey
                    var orderResult = await _orderRepo.GetByIdAsync(
                        data.OrderId, data.CustomerShardKey, ct);

                    return await orderResult.BindAsync(async order =>
                    {
                        order.Status = OrderStatus.Confirmed;
                        var updateResult = await _orderRepo.UpdateAsync(order, ct);
                        return updateResult.Map(_ =>
                            data with { OrderConfirmed = true });
                    });
                })
            .WithTimeout(TimeSpan.FromMinutes(5))
            .Build();

        return await _sagaRunner.RunAsync(definition,
            new OrderFulfillmentData
            {
                OrderId = orderId,
                CustomerShardKey = customerShardKey,
                ProductId = productId,
                Quantity = quantity,
            }, ct);
    }
}
```

### Idempotency Patterns

Cross-shard sagas are more susceptible to retries and partial failures. Making every step idempotent prevents duplicate side effects.

**1. Completion flags in saga data**

Track whether each step actually completed so compensation only undoes real work:

```csharp
// In the compensation handler:
if (data.InsertedInTarget)  // Only compensate if step actually succeeded
{
    await _repo.DeleteAsync(data.EntityId, data.TargetShardKey, ct);
}
```

**2. Check-before-write**

Read current state before modifying to detect duplicate executions:

```csharp
.Step("Reserve inventory")
    .Execute(async (data, ct) =>
    {
        var inv = await _inventoryRepo.GetByIdAsync(data.ProductId, data.ProductId, ct);
        return await inv.BindAsync(async inventory =>
        {
            // Guard: check if already reserved (idempotent)
            if (inventory.ReservationRef == data.TransactionRef)
                return data with { InventoryReserved = true };

            inventory.AvailableQuantity -= data.Quantity;
            inventory.ReservationRef = data.TransactionRef;
            var result = await _inventoryRepo.UpdateAsync(inventory, ct);
            return result.Map(_ => data with { InventoryReserved = true });
        });
    })
```

**3. Transaction reference**

Pass the saga ID as an idempotency key to downstream operations. If the saga is retried, the same reference prevents duplicate processing:

```csharp
// The SagaRunner generates a unique SagaId on start
// You can store it in saga data for use as an idempotency key
data with { TransactionRef = sagaId.ToString() }
```

**4. Idempotent compensations**

Compensations can also be retried. Use the same guard patterns:

```csharp
.Compensate(async (data, ct) =>
{
    if (!data.Debited) return; // Nothing to undo

    var account = await _accountRepo.GetByIdAsync(data.SourceAccountId, data.SourceShardKey, ct);
    await account.IfRightAsync(async acc =>
    {
        // Check if already refunded to avoid double-refund
        if (acc.LastRefundRef == data.TransactionRef) return;

        acc.Balance += data.Amount;
        acc.LastRefundRef = data.TransactionRef;
        await _accountRepo.UpdateAsync(acc, ct);
    });
})
```

### Saga Design Guidelines

1. **Each step should be idempotent** — Retrying a step must produce the same result. Use completion flags and transaction references.
2. **Each step needs a compensating action** — Define undo logic for when later steps fail. The last step can omit compensation if there is nothing to undo.
3. **Order matters** — Perform the most likely-to-fail step first to minimize the number of compensations. For example, debit before credit (insufficient funds is more likely than a credit failure).
4. **Use `SagaRunner` for automatic state tracking** — Do not manually chain `MatchAsync` calls. `SagaRunner` persists state after each step, runs compensation in reverse order on failure, and handles exceptions and cancellation.
5. **Handle partial compensations** — `SagaRunner` logs compensation failures but continues compensating remaining steps. If a compensation persistently fails, the saga enters `Failed` status for manual intervention.
6. **Include shard identifiers in saga data** — Store source and target shard keys so each step and compensation can route to the correct shard via `IFunctionalShardedRepository`.
7. **Use `WithTimeout` for cross-shard sagas** — Network failures between shards are more likely than within a single database. Set a timeout so stuck sagas are detected by `SagaOrchestrator.GetExpiredSagasAsync`.
8. **Read-then-act pattern** — Always read the current entity state from the shard before modifying it. This ensures you are working with the latest data and enables idempotency checks.

### When to Use Sagas vs. Other Approaches

| Scenario | Approach |
|----------|----------|
| Single entity on one shard | Standard repository operation (ACID guaranteed) |
| Two entities on the same shard | Single transaction via co-location |
| Two entities on different shards | **Saga pattern** |
| Bulk entity migration between shards | Saga per entity or batch saga |
| Read-only query across shards | Scatter-gather (no saga needed) |
| Entities that should always be co-located | Co-location group (prevent the problem) |

> **Future consideration**: If cross-shard transfers become a frequent pattern, a thin `TransferAsync` convenience wrapper on `IFunctionalShardedRepository` could reduce boilerplate. For now, the saga approach provides full flexibility and explicit control over compensation logic.

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

Not with Encina's sharding (by design). Use the [Saga pattern](#saga-pattern-for-cross-shard-workflows) for workflows that span shards. MongoDB 4.2+ supports cross-shard transactions natively when using mongos, but this is a MongoDB-specific feature and not part of Encina's abstraction.

### How do I use the Saga API for cross-shard writes?

Use `SagaDefinition.Create<TData>()` from `Encina.Messaging.Sagas.LowCeremony` to define steps, then execute with `ISagaRunner.RunAsync()`. Each step targets a specific shard via the shard key stored in the saga data. See the [entity transfer](#example-entity-transfer-between-shards), [funds transfer](#example-cross-shard-funds-transfer), and [order fulfillment](#example-cross-shard-order-fulfillment) examples above.

### What happens if the process crashes mid-saga?

Saga state is persisted to `ISagaStore` after each step. On restart, use `SagaOrchestrator.GetStuckSagasAsync()` to detect incomplete sagas older than a configurable threshold (`SagaOptions.StuckSagaThreshold`, default 5 minutes). Expired sagas can be detected with `GetExpiredSagasAsync()` for sagas that used `WithTimeout`.

### What if a Saga compensation fails?

`SagaRunner` logs the compensation failure but continues compensating remaining steps (it does not abort on a single compensation failure). If a compensation persistently fails, the saga enters `Failed` status. Use `SagaOrchestrator.GetStuckSagasAsync` or `GetExpiredSagasAsync` to find these sagas for manual intervention. Compensations should be idempotent and retryable.

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
