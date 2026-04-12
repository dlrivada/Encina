# Load Tests - Scheduling Processor

## Status: Not Implemented

## Justification

The `ScheduledMessageProcessor` is a sequential polling background service. Its throughput is bounded by `SchedulingOptions.BatchSize * ProcessingInterval` — there is no concurrent message dispatch within a single cycle (messages are processed sequentially in `SchedulerOrchestrator.ProcessDueMessagesAsync`).

### 1. No concurrent dispatch within cycles

The processor creates one scope per cycle and processes messages sequentially. Load testing a sequential loop yields no insight beyond what unit tests already cover.

### 2. Store-level load is the real bottleneck

The processor's hot path is `IScheduledMessageStore.GetDueMessagesAsync` + `MarkAsProcessedAsync` / `MarkAsFailedAsync`. These store operations are already covered by existing integration tests with real databases.

### 3. Multi-replica load requires distributed locks (deferred)

The most meaningful load scenario — multiple processor instances competing for the same messages — cannot be meaningfully tested until Issue #716 (distributed locks) is implemented. Load testing a single-instance processor only measures the store's throughput.

### Adequate Coverage from Other Test Types

- **Unit Tests**: Cycle-level behavior (empty batches, failure isolation, cancellation)
- **Integration Tests**: End-to-end dispatch with real SQLite store
- **Contract Tests**: Lifecycle and exception isolation

### Recommended Alternative

Revisit when distributed lock support (#716) lands. At that point, a load test with N concurrent processors competing for M scheduled messages would validate the lock acquisition strategy and measure throughput under contention.

## Related Files

- `src/Encina.Messaging/Scheduling/ScheduledMessageProcessor.cs`
- `src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs`

## Date: 2026-04-12

## Issue: #765
