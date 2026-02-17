# Shadow Sharding in Encina

Test new shard topologies under real production traffic without risk. Shadow sharding routes every operation through both the production and a shadow topology, comparing results while keeping the production path completely isolated.

## Table of Contents

1. [Overview](#overview)
2. [When to Use Shadow Sharding](#when-to-use-shadow-sharding)
3. [Configuration](#configuration)
4. [Usage Patterns](#usage-patterns)
5. [Observability](#observability)
6. [Safety Guarantees](#safety-guarantees)
7. [Migration Workflow](#migration-workflow)
8. [Error Codes](#error-codes)
9. [Troubleshooting](#troubleshooting)
10. [FAQ](#faq)

---

## Overview

Shadow sharding decorates the existing `IShardRouter` with a `ShadowShardRouterDecorator` that transparently routes operations to both the production and a shadow topology. The production path is never modified—shadow operations run as fire-and-forget side-effects:

```text
                      Shadow Sharding Decorator
  ┌──────────────────────────────────────────────────────────┐
  │                                                          │
  │  Production Router (primary path)                        │
  │  ┌──────────────┐         ┌──────────────┐               │
  │  │ GetShardId() │────────►│  Production  │──► Response   │
  │  └──────────────┘         │  Shard       │               │
  │                           └──────────────┘               │
  │                                                          │
  │  Shadow Router (fire-and-forget)                         │
  │  ┌──────────────┐         ┌──────────────┐               │
  │  │ GetShardId() │- - - - ►│  Shadow      │──► Compare    │
  │  └──────────────┘         │  Shard       │   & Log       │
  │                           └──────────────┘               │
  │                                                          │
  └──────────────────────────────────────────────────────────┘
```

The decorator implements `IShadowShardRouter` which extends `IShardRouter`, so any code that depends on `IShardRouter` continues to work unchanged. Shadow-specific operations (`RouteShadowAsync`, `CompareAsync`) are available via `IShadowShardRouter`.

---

## When to Use Shadow Sharding

| Scenario | Approach |
|----------|----------|
| Resharding from 4 to 8 shards | Enable shadow to verify key distribution before cutover |
| Switching from hash to range routing | Compare routing decisions at current traffic volume |
| Validating a new topology before migration | Dual-write to confirm shadow shards can handle the load |
| Measuring latency impact of a topology change | Shadow reads capture latency differences |
| Auditing routing consistency across topologies | Use `CompareAsync` for periodic spot-checks |

**Do not use shadow sharding for**:

- Permanent dual-write architectures (use CDC or event sourcing instead)
- Load testing unrelated to topology changes (use dedicated load tests)
- A/B testing of application logic (shadow sharding is infrastructure-level)

---

## Configuration

Shadow sharding is configured per entity type via the fluent `WithShadowSharding` method:

```csharp
// Define the new topology you want to test
var shadowTopology = new ShardTopology(new[]
{
    new ShardInfo("shadow-1", "Server=shadow1;Database=Orders"),
    new ShardInfo("shadow-2", "Server=shadow2;Database=Orders"),
    new ShardInfo("shadow-3", "Server=shadow3;Database=Orders"),
    new ShardInfo("shadow-4", "Server=shadow4;Database=Orders"),
});

// Enable shadow sharding alongside the production topology
services.AddEncinaSharding<Order>(options =>
{
    options.UseHashRouting()
        .AddShard("shard-0", "Server=prod0;Database=Orders")
        .AddShard("shard-1", "Server=prod1;Database=Orders")
        .WithShadowSharding(shadow =>
        {
            shadow.ShadowTopology = shadowTopology;
            shadow.DualWriteEnabled = true;
            shadow.ShadowReadPercentage = 10;
            shadow.CompareResults = true;
            shadow.ShadowWriteTimeout = TimeSpan.FromSeconds(3);
        });
});
```

### Configuration Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ShadowTopology` | `ShardTopology` | `null` (required) | The shadow topology to test |
| `DualWriteEnabled` | `bool` | `true` | Fire-and-forget shadow writes after production writes |
| `ShadowReadPercentage` | `int` | `0` | Percentage of reads also executed against the shadow (0–100) |
| `CompareResults` | `bool` | `true` | Compare production and shadow query results |
| `ShadowWriteTimeout` | `TimeSpan` | `5s` | Maximum time for a shadow write before cancellation |
| `ShadowRouterFactory` | `Func<ShardTopology, IShardRouter>?` | `null` | Custom router factory (defaults to `HashShardRouter`) |
| `DiscrepancyHandler` | `Func<...>?` | `null` | Custom callback for routing/result mismatches |

### Custom Shadow Router

By default, the shadow topology uses `HashShardRouter`. To test a different routing strategy:

```csharp
shadow.ShadowRouterFactory = topology =>
    new RangeShardRouter(topology, ranges);
```

### Custom Discrepancy Handler

React to routing or result mismatches with custom logic:

```csharp
shadow.DiscrepancyHandler = async (result, context, ct) =>
{
    if (!result.RoutingMatch)
    {
        await alertService.SendAsync(
            $"Routing mismatch for {result.ShardKey}: " +
            $"prod={result.ProductionShardId}, shadow={result.ShadowShardId}",
            ct);
    }
};
```

---

## Usage Patterns

### Dual-Write for Migration Validation

When `DualWriteEnabled` is `true`, the `ShadowWritePipelineBehavior<TCommand, TResponse>` intercepts all command pipeline executions. After a successful production write (`Right` result), it fires a shadow routing comparison as a fire-and-forget task:

```text
Command ──► Production Write ──► Success? ──► Return result
                                    │
                                    └──► Fire-and-forget shadow comparison
                                         (bounded by ShadowWriteTimeout)
```

The shadow write verifies that the shadow router can process the same key. Failures are logged but never propagate.

### Percentage-Based Shadow Reads

When `ShadowReadPercentage > 0`, the `ShadowReadPipelineBehavior<TQuery, TResponse>` samples a percentage of queries for shadow comparison:

```text
Query ──► ShouldShadowRead? ──► No  ──► Production only
              │
              └──► Yes ──► Production Read ──► Success? ──► Return result
                                                   │
                                                   └──► Fire-and-forget shadow comparison
                                                        + result hash comparison
```

The sampling uses `Random.Shared.Next(100) < ShadowReadPercentage` for thread-safe, low-overhead selection.

### Direct Shadow Routing

For diagnostics outside the pipeline, resolve `IShadowShardRouter` directly:

```csharp
public class DiagnosticsController(IShadowShardRouter shadowRouter)
{
    public async Task<ShadowComparisonResult> CompareRouting(string key, CancellationToken ct)
    {
        // Compare both topologies for a single key
        return await shadowRouter.CompareAsync(key, ct);
    }

    public async Task<string?> GetShadowShard(string key, CancellationToken ct)
    {
        // Route using only the shadow topology
        var result = await shadowRouter.RouteShadowAsync(key, ct);
        return result.Match(Right: id => id, Left: _ => null);
    }
}
```

---

## Observability

Shadow sharding integrates with Encina's OpenTelemetry infrastructure across all three pillars.

### Metrics

All metrics use the `Encina` meter. Available via `ShadowShardingMetrics`:

| Metric | Type | Tags | Description |
|--------|------|------|-------------|
| `encina.sharding.shadow.routing_total` | Counter | `routing_match` | Total shadow routing comparisons |
| `encina.sharding.shadow.routing_mismatches_total` | Counter | `shard_key_prefix` | Routing mismatches by key prefix |
| `encina.sharding.shadow.write_total` | Counter | `outcome` | Shadow writes by outcome (success/failure) |
| `encina.sharding.shadow.write_latency_diff_ms` | Histogram | `db.shard.id` | Write latency difference (shadow − production) |
| `encina.sharding.shadow.read_comparison_total` | Counter | `results_match` | Shadow read comparisons by result match |
| `encina.sharding.shadow.read_latency_diff_ms` | Histogram | — | Read latency difference (shadow − production) |

### Traces

`ShadowShardingActivityEnricher` enriches `Activity` spans with shadow-specific tags:

| Tag | Value | When |
|-----|-------|------|
| `encina.sharding.shadow.production_shard` | Shard ID | All shadow comparisons |
| `encina.sharding.shadow.shadow_shard` | Shard ID | All shadow comparisons |
| `encina.sharding.shadow.routing_match` | `true`/`false` | All shadow comparisons |
| `encina.sharding.shadow.write_outcome` | `success`/`failure` | Shadow writes |
| `encina.sharding.shadow.read_results_match` | `true`/`false` | Shadow reads with `CompareResults` |

### Logs

High-performance structured logging via `LoggerMessage` source generators (zero-allocation). EventId range: **700–749**.

| EventId | Level | Message |
|---------|-------|---------|
| 700 | Warning | Shadow routing failed for shard key `{ShardKey}` |
| 701 | Warning | Shadow routing mismatch: production=`{ProductionShardId}`, shadow=`{ShadowShardId}` |
| 710 | Warning | Shadow write failed for command `{CommandType}` |
| 711 | Warning | Shadow write timed out after `{TimeoutMs}`ms |
| 720 | Warning | Shadow read discrepancy for query `{QueryType}` |
| 721 | Warning | Shadow read failed for query `{QueryType}` |
| 722 | Warning | Shadow discrepancy handler failed for query `{QueryType}` |
| 730 | Information | Shadow sharding enabled with configuration summary |
| 731 | Information | Shadow comparison summary with mismatch rate |

---

## Safety Guarantees

Shadow sharding is designed with multiple layers of production isolation:

1. **Production path isolation**: All `IShardRouter` methods (`GetShardId`, `GetAllShardIds`, `GetShardConnectionString`, `GetColocationGroup`) delegate directly to the production router. No shadow code executes in the production path.

2. **Fire-and-forget semantics**: Shadow writes and reads are dispatched as detached `Task` instances (`_ = ExecuteAsync(...)`). The production result is returned immediately.

3. **Timeout protection**: Shadow writes are bounded by `ShadowWriteTimeout` (default: 5 seconds). After timeout, the `CancellationTokenSource` cancels the shadow operation.

4. **Exception swallowing**: All shadow operations are wrapped in try/catch. Exceptions—including `OperationCanceledException`—are logged but never propagated to the caller.

5. **Discrepancy handler isolation**: Even the custom `DiscrepancyHandler` is wrapped in try/catch. A failing handler cannot affect the production path.

6. **No shared state**: The shadow router is a separate `IShardRouter` instance with its own topology. There is no shared mutable state between production and shadow paths.

---

## Migration Workflow

A typical topology migration follows these phases:

### Phase 1: Shadow-Only Routing (No Reads)

```csharp
shadow.ShadowTopology = newTopology;
shadow.DualWriteEnabled = true;
shadow.ShadowReadPercentage = 0;  // No shadow reads yet
```

**Goal**: Verify that the shadow router can handle all shard keys without errors. Monitor `encina.sharding.shadow.write_total{outcome=failure}` for zero failures.

### Phase 2: Low-Percentage Shadow Reads (5%)

```csharp
shadow.ShadowReadPercentage = 5;
shadow.CompareResults = true;
```

**Goal**: Start comparing routing decisions and results at low volume. Monitor `encina.sharding.shadow.routing_mismatches_total` to understand the mismatch pattern.

### Phase 3: Increase Shadow Reads (25%)

```csharp
shadow.ShadowReadPercentage = 25;
```

**Goal**: Validate at higher volume. Check latency histograms (`write_latency_diff_ms`, `read_latency_diff_ms`) to ensure the shadow topology performs within acceptable bounds.

### Phase 4: Full Shadow Reads (100%)

```csharp
shadow.ShadowReadPercentage = 100;
```

**Goal**: Full coverage. Every read is compared. This is the final validation before cutover.

### Phase 5: Cutover

Replace the production topology with the shadow topology and remove the shadow configuration:

```csharp
// New configuration - shadow topology becomes production
services.AddEncinaSharding<Order>(options =>
{
    options.UseHashRouting()
        .AddShard("shadow-1", "Server=shadow1;Database=Orders")
        .AddShard("shadow-2", "Server=shadow2;Database=Orders")
        .AddShard("shadow-3", "Server=shadow3;Database=Orders")
        .AddShard("shadow-4", "Server=shadow4;Database=Orders");
    // No WithShadowSharding — shadow is now production
});
```

### Rollback

If issues are detected at any phase, simply remove `WithShadowSharding()` from the configuration and redeploy. The production path was never modified.

---

## Error Codes

| Code | Constant | Description |
|------|----------|-------------|
| `encina.sharding.shadow_routing_failed` | `ShardingErrorCodes.ShadowRoutingFailed` | The shadow router failed to route a shard key |

Shadow errors are returned as `Either<EncinaError, string>.Left` from `RouteShadowAsync` but are never returned from production-path methods (`GetShardId`, etc.).

---

## Troubleshooting

### High Mismatch Rate

**Symptom**: `encina.sharding.shadow.routing_mismatches_total` is high relative to `routing_total`.

**Cause**: Expected when switching routing strategies or changing shard count. Hash-based routing with different shard counts will produce different key distributions.

**Resolution**: This is informational. Mismatches indicate which keys will move during migration. Use `DiscrepancyHandler` to log affected keys for data migration planning.

### Shadow Write Timeouts

**Symptom**: EventId 711 warnings in logs (`Shadow write timed out`).

**Cause**: The shadow topology is slower than `ShadowWriteTimeout` allows.

**Resolution**:

- Increase `ShadowWriteTimeout` if the shadow infrastructure is genuinely slower
- Check shadow database connectivity and indexing
- Verify the shadow topology shard count is appropriate for the load

### Shadow Read Failures

**Symptom**: EventId 721 warnings (`Shadow read failed`).

**Cause**: The shadow router encountered an error for a shard key that succeeds on production.

**Resolution**:

- Check if the shadow topology covers all shard IDs returned by the routing strategy
- Verify `ShadowTopology` shard definitions include valid connection strings
- If using a custom `ShadowRouterFactory`, verify it handles all key formats

### Discrepancy Handler Errors

**Symptom**: EventId 722 warnings (`Shadow discrepancy handler failed`).

**Cause**: The custom `DiscrepancyHandler` delegate threw an exception.

**Resolution**: Add error handling inside your `DiscrepancyHandler`. The exception is logged and swallowed, but your handler logic did not complete.

---

## FAQ

**Q: Does shadow sharding add latency to production operations?**

No. Shadow operations are fire-and-forget. The production result is returned before the shadow operation begins. The only overhead is the cost of scheduling the shadow task (negligible).

**Q: Can I use different routing strategies for production and shadow?**

Yes. Set `ShadowRouterFactory` to create any `IShardRouter` implementation. For example, production can use `HashShardRouter` while shadow uses `RangeShardRouter`.

**Q: What happens if the shadow topology is unavailable?**

Nothing visible to users. Shadow failures are caught, logged (EventId 700/710/721), and discarded. Production routing continues normally.

**Q: Can I shadow-test with compound shard keys?**

Yes. Both `RouteShadowAsync(string, CancellationToken)` and `RouteShadowAsync(CompoundShardKey, CancellationToken)` are supported. The decorator implements the full `IShardRouter` contract including `GetShardId(CompoundShardKey)` and `GetShardIds(CompoundShardKey)`.

**Q: How do I know when it's safe to cut over?**

Monitor these signals over a sustained period (recommended: 48–72 hours at 100% shadow reads):

- `routing_mismatches_total` is stable and understood
- `write_total{outcome=failure}` is zero
- `write_latency_diff_ms` and `read_latency_diff_ms` are within acceptable bounds
- No EventId 700/710/721 warnings in logs

---

## Related Documentation

- [Compound Shard Keys](compound-shard-keys.md)
- [Sharding Co-location](sharding-colocation.md)
- [Time-Based Sharding](time-based-sharding.md)
- [Distributed Aggregations](distributed-aggregations.md)
