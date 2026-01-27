# Read/Write Separation Benchmarks

This directory contains BenchmarkDotNet benchmarks for the Read/Write Separation pattern,
measuring the performance overhead of replica selection strategies and routing context operations.

## Benchmark Classes

### ReplicaSelectionBenchmarks

Compares all three replica selection strategies to establish performance baselines.

| Benchmark | Description | Expected |
|-----------|-------------|----------|
| `RoundRobin.SelectReplica` | Interlocked.Increment + modulo | <50ns |
| `Random.SelectReplica` | Random.Shared.Next | <100ns |
| `LeastConnections.SelectReplica` | Lock + min search | <500ns |
| `LeastConnections.AcquireReplica` | Full lease acquire/release cycle | <1000ns |

### DatabaseRoutingBenchmarks

Measures routing context operations (AsyncLocal read/write performance).

| Benchmark | Description | Expected |
|-----------|-------------|----------|
| `Read CurrentIntent` | AsyncLocal read | <10ns |
| `Read EffectiveIntent` | Null-coalescing behavior | <10ns |
| `Read HasIntent` | Null check | <10ns |
| `Read IsReadIntent` | Intent comparison | <10ns |
| `Read IsWriteIntent` | Intent comparison | <10ns |
| `DatabaseRoutingScope.ForRead()` | Scope create/dispose | <100ns |
| `DatabaseRoutingScope.ForWrite()` | Scope create/dispose | <100ns |
| `DatabaseRoutingScope.ForForceWrite()` | Scope create/dispose | <100ns |
| `DatabaseRoutingContext.Clear()` | AsyncLocal clear | <50ns |
| `Nested scopes` | Restore overhead | <200ns |

### ConcurrentReplicaSelectionBenchmarks

Thread-safety benchmarks with ThreadingDiagnoser to measure contention under varying parallelism levels.

| Benchmark | Thread Counts | Key Metric |
|-----------|---------------|------------|
| `Concurrent RoundRobin` | 1, 4, 8, 16 | Linear scaling (Interlocked) |
| `Concurrent Random` | 1, 4, 8, 16 | Linear scaling (Random.Shared) |
| `Concurrent LeastConnections` | 1, 4, 8, 16 | May show contention (lock) |
| `Concurrent LeastConnections (lease)` | 1, 4, 8, 16 | Full lease pattern under load |

## Running the Benchmarks

```bash
# Run all Read/Write Separation benchmarks
dotnet run -c Release --project tests/Encina.BenchmarkTests/Encina.Benchmarks -- --filter "*ReadWriteSeparation*"

# Run specific benchmark class
dotnet run -c Release --project tests/Encina.BenchmarkTests/Encina.Benchmarks -- --filter "*ReplicaSelectionBenchmarks*"
dotnet run -c Release --project tests/Encina.BenchmarkTests/Encina.Benchmarks -- --filter "*DatabaseRoutingBenchmarks*"
dotnet run -c Release --project tests/Encina.BenchmarkTests/Encina.Benchmarks -- --filter "*ConcurrentReplicaSelectionBenchmarks*"
```

## Performance Targets Summary

| Category | Target | Rationale |
|----------|--------|-----------|
| **Routing context read** | <10ns | AsyncLocal read is a simple property access |
| **Routing scope lifecycle** | <100ns | AsyncLocal write + dispose pattern |
| **RoundRobin selection** | <50ns | Single Interlocked.Increment |
| **Random selection** | <100ns | Random.Shared.Next is thread-safe |
| **LeastConnections selection** | <500ns | Lock + iteration through replicas |

## Related Files

- `src/Encina.Messaging/ReadWriteSeparation/` - Core abstractions
- `tests/Encina.UnitTests/Messaging/ReadWriteSeparation/` - Unit tests
- `tests/Encina.ContractTests/Database/ReadWriteSeparation/` - Contract tests

## Issue

Issue #540 - [TEST] Implement BenchmarkTests for Read/Write Separation replica selection algorithms
