# Online Resharding Workflow

Online data resharding across shards with a 6-phase workflow, crash recovery, automatic rollback, and full observability.

## Table of Contents

1. [Overview](#overview)
2. [Quick Start](#quick-start)
3. [6-Phase Workflow](#6-phase-workflow)
4. [State Management & Crash Recovery](#state-management--crash-recovery)
5. [Rollback](#rollback)
6. [Configuration](#configuration)
7. [Builder API](#builder-api)
8. [DI Registration](#di-registration)
9. [Provider Abstractions](#provider-abstractions)
10. [Progress Tracking](#progress-tracking)
11. [Observability](#observability)
12. [Health Checks](#health-checks)
13. [Error Codes](#error-codes)
14. [Best Practices](#best-practices)
15. [FAQ](#faq)

---

## Overview

When a shard topology changes (adding, removing, or rebalancing shards), the data distributed across shards must be migrated to match the new topology. Doing this manually is error-prone, slow, and risks downtime. Encina provides `IReshardingOrchestrator` to automate the full data migration workflow with minimal downtime, crash recovery, automatic rollback, and real-time progress tracking.

```text
+---------------------------------------------------------------------+
|              Online Resharding Workflow (6 Phases)                  |
|                                                                     |
|  orchestrator.PlanAsync(request, ct)                                |
|              |                                                      |
|              v                                                      |
|  Phase 1: Planning ─── Generate migration steps from topology diff  |
|              |                                                      |
|  orchestrator.ExecuteAsync(plan, options, ct)                       |
|              |                                                      |
|              v                                                      |
|  Phase 2: Copying ──── Bulk copy rows (batched, resumable)          |
|              |                                                      |
|              v                                                      |
|  Phase 3: Replicating ─ CDC catch-up for changes during copy        |
|              |                                                      |
|              v                                                      |
|  Phase 4: Verifying ── Row count + checksum validation              |
|              |                                                      |
|              v                                                      |
|  Phase 5: CuttingOver ─ Atomic topology switch (brief read-only)    |
|              |                                                      |
|              v                                                      |
|  Phase 6: CleaningUp ─ Remove migrated rows from source shards      |
|              |                                                      |
|              v                                                      |
|          Completed                                                  |
+---------------------------------------------------------------------+
```

All operations return `Either<EncinaError, T>` following Encina's Railway Oriented Programming pattern.

---

## Quick Start

```csharp
using Encina.Sharding.Resharding;

// 1. Enable resharding in sharding configuration
services.AddEncinaSharding<Order>(options =>
{
    options.UseHashRouting()
        .AddShard("shard-0", "Server=shard0;Database=Orders;...")
        .AddShard("shard-1", "Server=shard1;Database=Orders;...")
        .WithResharding(resharding =>
        {
            resharding.CopyBatchSize = 10_000;
            resharding.CdcLagThreshold = TimeSpan.FromSeconds(5);
            resharding.VerificationMode = VerificationMode.CountAndChecksum;
            resharding.CutoverTimeout = TimeSpan.FromSeconds(30);
            resharding.CleanupRetentionPeriod = TimeSpan.FromHours(24);
        });
});

// 2. Register your IReshardingStateStore implementation (required)
services.AddSingleton<IReshardingStateStore, SqlReshardingStateStore>();

// 3. Plan a resharding operation
var request = new ReshardingRequest(oldTopology, newTopology);
var planResult = await orchestrator.PlanAsync(request, ct);

// 4. Execute the plan
var result = await planResult.BindAsync(plan =>
    orchestrator.ExecuteAsync(plan, options, ct));

// 5. Handle the result
result.Match(
    Right: r =>
    {
        if (r.IsSuccess)
            logger.LogInformation("Resharding completed in {Count} phases", r.PhaseHistory.Count);
        else
            logger.LogWarning("Resharding ended in phase: {Phase}", r.FinalPhase);
    },
    Left: error => logger.LogError("Resharding failed: {Error}", error.Message));
```

---

## 6-Phase Workflow

The resharding workflow progresses through six sequential phases. Each phase persists its state via `IReshardingStateStore` so the operation can resume after a crash.

### Phase 1: Planning

Generates the resharding plan by analyzing the difference between the old and new topologies.

```text
Old Topology: [shard-0, shard-1]        New Topology: [shard-0, shard-1, shard-2]
    Hash Ring: 0────────50%────────100%      Hash Ring: 0────33%────66%────100%
               ├── shard-0 ──┤── shard-1 ─┤             ├─ s0 ─┤─ s1 ─┤─ s2 ─┤
```

The planner produces a `ReshardingPlan` containing `ShardMigrationStep` records, each describing a source shard, target shard, key range, and estimated row count.

```csharp
var request = new ReshardingRequest(oldTopology, newTopology);
var planResult = await orchestrator.PlanAsync(request, ct);

planResult.IfRight(plan =>
{
    Console.WriteLine($"Plan {plan.Id}: {plan.Steps.Count} steps");
    Console.WriteLine($"Estimated: {plan.Estimate.TotalRows} rows, {plan.Estimate.EstimatedDuration}");

    foreach (var step in plan.Steps)
        Console.WriteLine($"  {step.SourceShardId} → {step.TargetShardId}: ~{step.EstimatedRows} rows");
});
```

**Errors**: `TopologiesIdentical`, `EmptyPlan`, `PlanGenerationFailed`

### Phase 2: Copying

Bulk-copies existing rows from source shards to target shards in configurable batches. Each batch records a position checkpoint for crash recovery.

- Batch size is controlled by `ReshardingOptions.CopyBatchSize` (default: 10,000)
- Progress is tracked per migration step via `ShardMigrationProgress.RowsCopied`
- On crash, the copy resumes from `ReshardingCheckpoint.LastCopiedBatchPosition`

**Errors**: `CopyFailed`

### Phase 3: Replicating

Uses Change Data Capture (CDC) to replicate incremental changes that occurred on source shards during the copy phase. The replication loop runs until the CDC lag drops below `ReshardingOptions.CdcLagThreshold` (default: 5 seconds).

- Replication position is persisted in `ReshardingCheckpoint.CdcPosition`
- Current lag is monitored via `IReshardingServices.GetReplicationLagAsync`
- On crash, replication resumes from the stored CDC position

**Errors**: `ReplicationFailed`

### Phase 4: Verifying

Validates data consistency between source and target shards for each migration step. Three verification modes are available:

| Mode | What It Checks | Speed |
|------|---------------|-------|
| `Count` | Row counts only | Fastest |
| `Checksum` | Checksum comparison only | Medium |
| `CountAndChecksum` | Row counts + checksums | Thorough (default) |

If verification fails, the operation transitions to `Failed` and provides `RollbackMetadata` for manual rollback.

**Errors**: `VerificationFailed`

### Phase 5: CuttingOver

Atomically switches the active shard topology to the new topology. This is the only phase with a brief read-only window.

- **Timeout**: Controlled by `ReshardingOptions.CutoverTimeout` (default: 30 seconds)
- **Gate predicate**: `ReshardingOptions.OnCutoverStarting` is invoked before the switch begins. Return `false` to abort.
- On timeout or failure, the original topology is restored

**Errors**: `CutoverTimeout`, `CutoverAborted`, `CutoverFailed`

### Phase 6: CleaningUp

Removes migrated rows from source shards after the configurable retention period (`ReshardingOptions.CleanupRetentionPeriod`, default: 24 hours). This retention window allows safe rollback after cutover.

Cleanup failures are non-fatal: the operation still transitions to `Completed` because the topology switch has already succeeded. Cleanup errors are logged and reported in the result.

**Errors**: `CleanupFailed`

---

## State Management & Crash Recovery

Resharding state is persisted via `IReshardingStateStore` after every phase transition. This enables the orchestrator to resume interrupted operations from the last completed phase after a process restart.

### ReshardingState

The persistent state record:

```csharp
public sealed record ReshardingState(
    Guid Id,                              // Unique operation identifier
    ReshardingPhase CurrentPhase,         // Current phase
    ReshardingPlan Plan,                  // The resharding plan
    ReshardingProgress Progress,          // Current progress
    ReshardingPhase? LastCompletedPhase,  // Last successfully completed phase
    DateTime StartedAtUtc,                // When the operation started
    ReshardingCheckpoint? Checkpoint);    // Phase-specific resume point
```

### ReshardingCheckpoint

Phase-specific data for resuming after a crash:

```csharp
public sealed record ReshardingCheckpoint(
    long? LastCopiedBatchPosition,  // Resume point for copy phase
    string? CdcPosition);          // Resume point for replication phase (LSN, binlog offset, WAL)
```

### IReshardingStateStore

Consumers **must** register their own `IReshardingStateStore` implementation. No default in-memory implementation is provided because resharding state must survive process restarts.

| Method | Purpose |
|--------|---------|
| `SaveStateAsync(ReshardingState, CancellationToken)` | Persist current state |
| `GetStateAsync(Guid, CancellationToken)` | Retrieve state by ID |
| `GetActiveReshardingsAsync(CancellationToken)` | List non-terminal operations for recovery |
| `DeleteStateAsync(Guid, CancellationToken)` | Clean up completed/rolled-back state |

All methods return `Either<EncinaError, T>`.

### Recovery Flow

```text
Process restarts
       |
       v
stateStore.GetActiveReshardingsAsync()
       |
       v
For each active state:
  - Read LastCompletedPhase
  - Read Checkpoint (batch position, CDC position)
  - Resume from next phase after LastCompletedPhase
```

---

## Rollback

Roll back a failed or partial resharding operation using the `RollbackMetadata` attached to the result:

```csharp
var result = await orchestrator.ExecuteAsync(plan, options, ct);

result.IfRight(async r =>
{
    if (!r.IsSuccess && r.RollbackMetadata is not null)
    {
        var rollback = await orchestrator.RollbackAsync(r, ct);
        rollback.Match(
            Right: _ => logger.LogInformation("Rollback completed, original topology restored"),
            Left: error => logger.LogError("Rollback failed: {Error}", error.Message));
    }
});
```

### RollbackMetadata

```csharp
public sealed record RollbackMetadata(
    ReshardingPlan OriginalPlan,          // The plan that was being executed
    ShardTopology OldTopology,            // The original topology to restore
    ReshardingPhase LastCompletedPhase);  // Where the operation failed
```

### Rollback Behavior by Phase

| Failed At Phase | Rollback Action |
|-----------------|-----------------|
| Planning | No-op (no data was touched) |
| Copying | Delete partially copied rows from target shards |
| Replicating | Stop CDC streams, delete copied + replicated rows |
| Verifying | Delete copied + replicated rows |
| CuttingOver | Restore original topology, delete target data |
| CleaningUp | Topology already switched; rollback not available |

**Errors**: `RollbackFailed`, `RollbackNotAvailable`

---

## Configuration

### ReshardingOptions

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `CopyBatchSize` | `int` | `10,000` | Rows per batch during the copy phase |
| `CdcLagThreshold` | `TimeSpan` | `5s` | Max CDC lag before transitioning to verification |
| `VerificationMode` | `VerificationMode` | `CountAndChecksum` | How to verify data consistency |
| `CutoverTimeout` | `TimeSpan` | `30s` | Max duration for the cutover phase |
| `CleanupRetentionPeriod` | `TimeSpan` | `24h` | How long to retain source data before cleanup |
| `OnPhaseCompleted` | `Func<ReshardingPhase, ReshardingProgress, Task>?` | `null` | Callback after each phase completes |
| `OnCutoverStarting` | `Func<ReshardingPlan, CancellationToken, Task<bool>>?` | `null` | Predicate gate before cutover begins |

### VerificationMode Enum

| Value | Description |
|-------|-------------|
| `Count` | Verify row counts only (fastest) |
| `Checksum` | Verify checksums only (medium) |
| `CountAndChecksum` | Verify both row counts and checksums (default, most thorough) |

---

## Builder API

The fluent builder provides a discoverable API for configuring resharding within the sharding setup pipeline:

```csharp
services.AddEncinaSharding<Order>(options =>
{
    options.UseHashRouting()
        .AddShard("shard-0", "Server=shard0;...")
        .AddShard("shard-1", "Server=shard1;...")
        .WithResharding(resharding =>
        {
            resharding.CopyBatchSize = 10_000;
            resharding.CdcLagThreshold = TimeSpan.FromSeconds(5);
            resharding.VerificationMode = VerificationMode.CountAndChecksum;
            resharding.CutoverTimeout = TimeSpan.FromSeconds(30);
            resharding.CleanupRetentionPeriod = TimeSpan.FromHours(24);
            resharding.OnPhaseCompleted(async (phase, progress) =>
            {
                logger.LogInformation("Phase {Phase} completed: {Percent}%",
                    phase, progress.OverallPercentComplete);
            });
            resharding.OnCutoverStarting(async (plan, ct) =>
            {
                // External validation before cutover
                return await healthChecker.IsSystemHealthyAsync(ct);
            });
        });
});
```

### ReshardingBuilder Members

| Member | Kind | Description |
|--------|------|-------------|
| `CopyBatchSize` | Property | Rows per batch during copy (default: 10,000) |
| `CdcLagThreshold` | Property | Max CDC lag threshold (default: 5s) |
| `VerificationMode` | Property | Verification strategy (default: CountAndChecksum) |
| `CutoverTimeout` | Property | Max cutover duration (default: 30s) |
| `CleanupRetentionPeriod` | Property | Source data retention (default: 24h) |
| `OnPhaseCompleted(Func<ReshardingPhase, ReshardingProgress, Task>)` | Method | Set per-phase completion callback |
| `OnCutoverStarting(Func<ReshardingPlan, CancellationToken, Task<bool>>)` | Method | Set cutover gate predicate |

---

## DI Registration

Resharding is enabled via `ShardingOptions<TEntity>.WithResharding()`:

```csharp
services.AddEncinaSharding<Order>(options =>
{
    options.UseHashRouting()
        .AddShard("shard-0", "Server=shard0;...")
        .WithResharding();
});
```

When `UseResharding` is `true`, the internal `ReshardingServiceCollectionExtensions.AddResharding` method registers:

- `ReshardingOptions` as **Singleton**
- `IReshardingOrchestrator` → `ReshardingOrchestrator` as **Singleton** (via `TryAddSingleton`)

**Consumer responsibility**: `IReshardingStateStore` must be registered by the consumer. No default in-memory implementation is provided because resharding state must survive process restarts for crash recovery.

```csharp
// Consumer registers their own state store
services.AddSingleton<IReshardingStateStore, SqlReshardingStateStore>();
```

---

## Provider Abstractions

### IReshardingOrchestrator

Coordinates the full 6-phase resharding workflow:

| Method | Return Type | Purpose |
|--------|------------|---------|
| `PlanAsync(ReshardingRequest, CancellationToken)` | `Either<EncinaError, ReshardingPlan>` | Generate migration plan from topology diff |
| `ExecuteAsync(ReshardingPlan, ReshardingOptions, CancellationToken)` | `Either<EncinaError, ReshardingResult>` | Execute the 6-phase workflow |
| `RollbackAsync(ReshardingResult, CancellationToken)` | `Either<EncinaError, Unit>` | Roll back a failed operation |
| `GetProgressAsync(Guid, CancellationToken)` | `Either<EncinaError, ReshardingProgress>` | Get real-time progress |

### IReshardingServices

Aggregates all external dependencies needed by the workflow phases:

| Method | Return Type | Purpose |
|--------|------------|---------|
| `CopyBatchAsync(sourceShardId, targetShardId, keyRange, batchSize, lastPosition, ct)` | `Either<EncinaError, CopyBatchResult>` | Copy a batch of rows |
| `ReplicateChangesAsync(sourceShardId, targetShardId, keyRange, cdcPosition, ct)` | `Either<EncinaError, ReplicationResult>` | Replicate CDC changes |
| `GetReplicationLagAsync(sourceShardId, ct)` | `Either<EncinaError, TimeSpan>` | Check current CDC lag |
| `VerifyDataConsistencyAsync(sourceShardId, targetShardId, keyRange, mode, ct)` | `Either<EncinaError, VerificationResult>` | Verify data consistency |
| `SwapTopologyAsync(newTopology, ct)` | `Either<EncinaError, Unit>` | Atomically switch topology |
| `CleanupSourceDataAsync(sourceShardId, keyRange, batchSize, ct)` | `Either<EncinaError, long>` | Delete migrated rows |
| `EstimateRowCountAsync(shardId, keyRange, ct)` | `Either<EncinaError, long>` | Estimate row count |

### Result Records

```csharp
// Copy phase result
public sealed record CopyBatchResult(
    long RowsCopied,           // Rows copied in this batch
    long NewBatchPosition,     // Resume point for next batch
    bool HasMoreRows);         // Whether more rows remain

// Replication phase result
public sealed record ReplicationResult(
    long RowsReplicated,       // Rows replicated (inserts + updates + deletes)
    string? FinalCdcPosition,  // CDC position for persistence/recovery
    TimeSpan CurrentLag);      // CDC lag at end of pass

// Verification phase result
public sealed record VerificationResult(
    bool IsConsistent,         // Whether data matches
    long SourceRowCount,       // Source shard row count
    long TargetRowCount,       // Target shard row count
    string? MismatchDetails);  // Description of mismatches, if any
```

---

## Progress Tracking

Monitor active resharding operations in real-time:

```csharp
var progress = await orchestrator.GetProgressAsync(reshardingId, ct);

progress.IfRight(p =>
{
    Console.WriteLine($"Phase: {p.CurrentPhase}");
    Console.WriteLine($"Overall: {p.OverallPercentComplete:F1}%");

    foreach (var (stepKey, step) in p.PerStepProgress)
    {
        Console.WriteLine($"  {stepKey}: copied={step.RowsCopied}, " +
            $"replicated={step.RowsReplicated}, verified={step.IsVerified}");
    }
});
```

### ReshardingProgress Properties

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `Guid` | Resharding operation identifier |
| `CurrentPhase` | `ReshardingPhase` | Current workflow phase |
| `OverallPercentComplete` | `double` | Overall progress (0.0 to 100.0) |
| `PerStepProgress` | `IReadOnlyDictionary<string, ShardMigrationProgress>` | Per-step progress, keyed by `"sourceShardId->targetShardId"` |

### ShardMigrationProgress Properties

| Property | Type | Description |
|----------|------|-------------|
| `RowsCopied` | `long` | Rows copied during the copy phase |
| `RowsReplicated` | `long` | Rows replicated via CDC during the replication phase |
| `IsVerified` | `bool` | Whether verification has passed for this step |

### ReshardingResult Properties

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `Guid` | Resharding operation identifier |
| `FinalPhase` | `ReshardingPhase` | Terminal phase (Completed, RolledBack, or Failed) |
| `PhaseHistory` | `IReadOnlyList<PhaseHistoryEntry>` | Ordered history with timing |
| `RollbackMetadata` | `RollbackMetadata?` | Rollback info (null if succeeded or already rolled back) |
| `IsSuccess` | `bool` | Computed: `FinalPhase == ReshardingPhase.Completed` |

### PhaseHistoryEntry Properties

| Property | Type | Description |
|----------|------|-------------|
| `Phase` | `ReshardingPhase` | The phase that completed |
| `StartedAtUtc` | `DateTime` | When the phase started |
| `CompletedAtUtc` | `DateTime` | When the phase completed |
| `Duration` | `TimeSpan` | Computed: `CompletedAtUtc - StartedAtUtc` |

---

## Observability

### Metrics

Seven metric instruments under the `"Encina"` meter:

| Metric | Type | Description | Tags |
|--------|------|-------------|------|
| `encina.resharding.phase_duration_ms` | Histogram | Duration of each resharding phase (ms) | `resharding.id`, `resharding.phase` |
| `encina.resharding.rows_copied_total` | Counter | Total rows copied during resharding | `resharding.source_shard`, `resharding.target_shard` |
| `encina.resharding.rows_per_second` | ObservableGauge | Current copy throughput (rows/s) | -- |
| `encina.resharding.cdc_lag_ms` | ObservableGauge | Current CDC replication lag (ms) | -- |
| `encina.resharding.verification_mismatches_total` | Counter | Total verification mismatches detected | `resharding.id` |
| `encina.resharding.cutover_duration_ms` | Histogram | Duration of the cutover phase (ms) | `resharding.id` |
| `encina.resharding.active_resharding_count` | ObservableGauge | Number of active resharding operations | -- |

### Traces

Three activities via `ReshardingActivitySource` (`"Encina.Resharding"`):

- **StartReshardingExecution**: Parent span for the entire resharding operation, tagged with `resharding.id`, `resharding.step_count`, and `resharding.estimated_rows`.
- **StartPhaseExecution**: Child span for each individual phase, tagged with `resharding.id` and `resharding.phase`.
- **Complete**: Enriches the activity with final status (`resharding.duration_ms`, `resharding.error`) and sets `ActivityStatusCode.Ok` or `ActivityStatusCode.Error`.

The activity source is automatically registered via `AddEncinaOpenTelemetry()` with `tracing.AddSource("Encina.Resharding")`.

### Structured Logging

`ReshardingLogMessages` provides high-performance `LoggerMessage`-based structured logging for all workflow events, including phase transitions, batch progress, CDC lag updates, verification results, and error details.

---

## Health Checks

`ReshardingHealthCheck` monitors active resharding operations and reports health status:

```csharp
services.AddHealthChecks()
    .Add(new HealthCheckRegistration(
        "resharding",
        sp => new ReshardingHealthCheck(
            sp.GetRequiredService<IReshardingStateStore>(),
            new ReshardingHealthCheckOptions()),
        failureStatus: HealthStatus.Degraded,
        tags: ["resharding", "sharding"]));
```

### Health Status Classification

| Condition | Status | Description |
|-----------|--------|-------------|
| No active resharding operations | **Healthy** | `"No active resharding operations."` |
| Active operations progressing within time limit | **Degraded** | `"N resharding operation(s) in progress."` |
| Failed states without rollback | **Unhealthy** | `"Resharding issues: N failed without rollback."` |
| Operations exceeding MaxReshardingDuration | **Unhealthy** | `"Resharding issues: N exceeded max duration of Xh."` |
| State store query failure | **Unhealthy** | `"Failed to query resharding state: {error}"` |
| Health check timeout | **Unhealthy** | `"Resharding health check timed out after Xs."` |

### ReshardingHealthCheckOptions

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `MaxReshardingDuration` | `TimeSpan` | `2h` | Max expected duration before Unhealthy |
| `Timeout` | `TimeSpan` | `30s` | Health check query timeout |

### Health Check Data Dictionary

The health check result includes structured data for programmatic consumption:

| Key | Type | When Present |
|-----|------|-------------|
| `activeCount` | `int` | Always |
| `inProgressCount` | `int` | When active operations exist |
| `failedCount` | `int` | When active operations exist |
| `overdueCount` | `int` | When active operations exist |
| `failedIds` | `string` | When failed operations exist (comma-separated) |
| `overdueIds` | `string` | When overdue operations exist (comma-separated) |
| `activeOperations` | `string` | When in-progress operations exist (`"id=Phase"` format) |
| `error` | `string` | When state store query fails |

---

## Error Codes

All error codes follow the `encina.sharding.resharding.*` convention and are stable across releases:

| Constant | Code | Description |
|----------|------|-------------|
| `TopologiesIdentical` | `encina.sharding.resharding.topologies_identical` | Old and new topologies are identical |
| `EmptyPlan` | `encina.sharding.resharding.empty_plan` | Plan has no migration steps |
| `PlanGenerationFailed` | `encina.sharding.resharding.plan_generation_failed` | Unreachable shards or estimation errors |
| `CopyFailed` | `encina.sharding.resharding.copy_failed` | Bulk copy phase failed |
| `ReplicationFailed` | `encina.sharding.resharding.replication_failed` | CDC replication failed or lag too high |
| `VerificationFailed` | `encina.sharding.resharding.verification_failed` | Data mismatch between shards |
| `CutoverTimeout` | `encina.sharding.resharding.cutover_timeout` | Cutover exceeded configured timeout |
| `CutoverAborted` | `encina.sharding.resharding.cutover_aborted` | Cutover predicate returned false |
| `CutoverFailed` | `encina.sharding.resharding.cutover_failed` | Topology switch failed |
| `CleanupFailed` | `encina.sharding.resharding.cleanup_failed` | Source data cleanup failed |
| `RollbackFailed` | `encina.sharding.resharding.rollback_failed` | Rollback operation failed |
| `RollbackNotAvailable` | `encina.sharding.resharding.rollback_not_available` | No rollback metadata available |
| `ReshardingNotFound` | `encina.sharding.resharding.resharding_not_found` | Resharding operation not found |
| `InvalidPhaseTransition` | `encina.sharding.resharding.invalid_phase_transition` | Invalid phase transition attempted |
| `StateStoreFailed` | `encina.sharding.resharding.state_store_failed` | State store operation failed |
| `ConcurrentReshardingNotAllowed` | `encina.sharding.resharding.concurrent_resharding_not_allowed` | Another resharding is already active |

Error codes are emitted as OpenTelemetry tags on resharding activity spans, enabling correlation between ROP error paths and distributed traces. They are suitable for alerting rules, log filters, and dashboard queries.

---

## Best Practices

1. **Start with a dry run**: Use `PlanAsync` to generate and inspect the plan before calling `ExecuteAsync`. Review estimated rows, bytes, and duration to set appropriate expectations.

2. **Use shadow sharding first**: Validate the new topology under real traffic using [Shadow Sharding](shadow-sharding.md) before committing to a resharding operation.

3. **Set the cutover gate predicate**: Always configure `OnCutoverStarting` to validate system health before the topology switch. This is your last safe exit before the brief read-only window.

4. **Register a durable state store**: The `IReshardingStateStore` must be backed by a durable store (database table, not in-memory) to survive process restarts. Without this, crash recovery is impossible.

5. **Monitor the health check**: Register `ReshardingHealthCheck` and configure `MaxReshardingDuration` based on your data volume. Alert on `Unhealthy` status.

6. **Tune batch size for your workload**: The default `CopyBatchSize` of 10,000 works well for most schemas. Increase for simple rows (few columns, small values) and decrease for wide rows (LOBs, JSON columns).

7. **Keep the retention period**: The default 24-hour `CleanupRetentionPeriod` provides a safety window for rollback after cutover. Do not set it to zero unless you are certain the migration succeeded.

8. **Handle concurrent resharding**: Only one resharding operation is allowed at a time. The orchestrator returns `ConcurrentReshardingNotAllowed` if another operation is active. Design your automation to check for active operations before starting a new one.

---

## FAQ

**Q: What happens if the process crashes during resharding?**

The orchestrator persists state after every phase transition via `IReshardingStateStore`. On restart, call `GetActiveReshardingsAsync()` to discover interrupted operations and resume from the last completed phase using the stored `ReshardingCheckpoint`.

**Q: How long does the read-only window last during cutover?**

The cutover phase performs an atomic topology switch, which is typically sub-second. The `CutoverTimeout` (default: 30 seconds) is a safety limit, not the expected duration. Monitor `encina.resharding.cutover_duration_ms` for actual timing.

**Q: Can I reshard while the application is serving traffic?**

Yes. Phases 1-4 (Planning, Copying, Replicating, Verifying) run in the background without affecting production traffic. Only the CuttingOver phase (Phase 5) introduces a brief read-only window while the topology is switched.

**Q: Why do I need to provide my own IReshardingStateStore?**

Resharding state must survive process restarts for crash recovery. An in-memory implementation would lose all state on crash, making recovery impossible. By requiring consumers to provide a durable implementation (backed by a database table, for example), Encina ensures correct crash recovery behavior.

**Q: Can I constrain resharding to specific entity types?**

Yes. The `ReshardingRequest` record accepts an optional `EntityTypeConstraints` parameter. When provided, only the specified entity types are included in the migration plan. When `null` or empty, all entity types are included.

**Q: What if verification fails?**

The operation transitions to the `Failed` phase and includes `RollbackMetadata` in the result. You can call `orchestrator.RollbackAsync(result, ct)` to restore the original topology and clean up partially migrated data. Investigate the `VerificationResult.MismatchDetails` to understand the root cause before retrying.

---

## Related Documentation

- [Shadow Sharding](shadow-sharding.md) -- Test new topologies risk-free under production traffic
- [Migration Coordination](migration-coordination.md) -- Coordinated schema migrations across shards
- [Time-Based Sharding](time-based-sharding.md) -- Temporal data partitioning with tier lifecycle
- [Database Sharding](../sharding/configuration.md) -- Topology configuration reference
- [Scaling Guidance](../sharding/scaling-guidance.md) -- Shard key selection and capacity planning
