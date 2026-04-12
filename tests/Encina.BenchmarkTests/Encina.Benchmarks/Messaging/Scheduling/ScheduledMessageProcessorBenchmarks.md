# Benchmark Tests - Scheduling Processor

## Status: Not Implemented

## Justification

The `ScheduledMessageProcessor`'s per-cycle cost is dominated by I/O operations (`IScheduledMessageStore.GetDueMessagesAsync`, `IEncina.Send`/`Publish`, `MarkAsProcessedAsync`) rather than CPU-bound computation.

### 1. Dispatcher's expression-tree cost is amortized

The `CompiledExpressionScheduledMessageDispatcher` incurs a one-time `Expression.Lambda.Compile()` cost per request type, then subsequent calls are a direct compiled delegate invocation. The amortized hot-path cost is near-zero and not meaningful to benchmark (it's a single virtual call + IEncina call).

### 2. Retry policy computation is trivial

`ExponentialBackoffRetryPolicy.Compute` performs a single `Math.Pow` + `DateTime.AddMilliseconds` — sub-microsecond, not worth benchmarking in isolation.

### 3. Processor loop cost is I/O-dominated

The processor's `ProcessOnceAsync` spends >99% of wall time in store queries and `IEncina.Send`/`Publish`. Benchmarking the loop overhead without the I/O is measuring nanoseconds against seconds — the signal-to-noise ratio is negligible.

### Adequate Coverage from Other Test Types

- **Unit Tests**: Verify dispatch delegation works correctly for all shapes
- **Contract Tests**: Verify compiled delegates are cached (CacheCount property)
- **Integration Tests**: Verify end-to-end execution against real store

### Recommended Alternative

When the compiled expression dispatcher is compared against a future source-generator-based dispatcher (AOT), benchmarks comparing the two implementations' dispatch latency per call would be meaningful. At that point, create a `DispatcherBenchmarks.cs` comparing `CompiledExpression` vs `SourceGen` per-call overhead.

## Related Files

- `src/Encina.Messaging/Scheduling/CompiledExpressionScheduledMessageDispatcher.cs`
- `src/Encina.Messaging/Scheduling/ExponentialBackoffRetryPolicy.cs`
- `src/Encina.Messaging/Scheduling/ScheduledMessageProcessor.cs`

## Date: 2026-04-12

## Issue: #765
