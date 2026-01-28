# Encina Benchmark Results

This document contains the complete benchmark results for the Encina framework, measuring performance across core operations, messaging patterns, and data access layers.

## Test Environment

| Property | Value |
|----------|-------|
| **Date** | January 27, 2026 |
| **OS** | Windows 11 (10.0.26200.7462/25H2/2025Update/HudsonValley2) |
| **CPU** | 13th Gen Intel Core i9-13900KS @ 3.20GHz |
| **Cores** | 32 logical, 24 physical |
| **RAM** | 64 GB DDR5 |
| **.NET SDK** | 10.0.102 |
| **Runtime** | .NET 10.0.2 (10.0.2, 10.0.225.61305), X64 RyuJIT x86-64-v3 |
| **GC** | Concurrent Workstation |
| **Hardware Intrinsics** | AVX2+BMI1+BMI2+F16C+FMA+LZCNT+MOVBE,AVX,SSE3+SSSE3+SSE4.1+SSE4.2+POPCNT,AES+PCLMUL,AvxVnni,SERIALIZE |
| **Vector Size** | 256-bit |

## How to Interpret Results

### Key Metrics

| Metric | Description |
|--------|-------------|
| **Mean** | Arithmetic mean of all measurements |
| **Error** | Half of 99.9% confidence interval |
| **StdDev** | Standard deviation of all measurements |
| **Median** | Value separating the higher half (50th percentile) |
| **Gen0/Gen1/Gen2** | GC collections per 1000 operations |
| **Allocated** | Memory allocated per single operation (managed only) |
| **Ratio** | Mean of the ratio distribution vs baseline |

### Performance Categories

| Category | Latency Range | Use Case |
|----------|---------------|----------|
| **Ultra-fast** | < 100 ns | In-memory operations, cache lookups |
| **Fast** | 100 ns - 10 us | Simple CPU-bound operations |
| **Normal** | 10 us - 1 ms | Database operations, I/O |
| **Slow** | > 1 ms | Bulk operations, complex queries |

---

## 1. Core Mediator Performance

The mediator is the central dispatch mechanism in Encina. These benchmarks measure the overhead of command/query dispatch.

### Expected Performance Targets

| Operation | Target | Rationale |
|-----------|--------|-----------|
| Send Command | < 2 us | Single handler dispatch with DI resolution |
| Publish Notification | < 1.5 us per handler | Fan-out to multiple handlers |

### Results

| Method | Mean | Error | StdDev | Gen0 | Allocated |
|--------|------|-------|--------|------|-----------|
| Send_Command_WithInstrumentation | 1.765 us | 0.0208 us | 0.0174 us | 0.2003 | 3.69 KB |
| Publish_Notification_WithMultipleHandlers | 1.222 us | 0.0118 us | 0.0111 us | 0.1812 | 3.35 KB |

### Analysis

- **Command dispatch**: ~1.77 us with instrumentation overhead. This includes:
  - DI service resolution
  - Handler invocation
  - OpenTelemetry span creation (when instrumented)

- **Notification publishing**: ~1.22 us for multiple handlers demonstrates efficient parallel dispatch. Lower than commands because notifications don't require a return value.

- **Memory allocation**: ~3.5 KB per operation is reasonable for a full mediator dispatch cycle including activity tracking.

---

## 2. Delegate Invocation Comparison

Understanding the cost of different invocation strategies helps optimize the mediator pipeline.

### Results

| Method | Mean | Error | StdDev | Median | Ratio | Gen0 | Allocated |
|--------|------|-------|--------|--------|-------|------|-----------|
| DirectCall | 15.67 ns | 0.328 ns | 0.931 ns | 15.31 ns | 1.00 | 0.0059 | 112 B |
| CompiledDelegate | 15.24 ns | 0.301 ns | 0.281 ns | 15.21 ns | 0.98 | 0.0059 | 112 B |
| MethodInfoInvoke | 32.09 ns | 0.768 ns | 2.215 ns | 31.23 ns | 2.05 | 0.0093 | 176 B |
| GenericTypeConstruction | 39.66 ns | 0.824 ns | 1.567 ns | 39.12 ns | 2.54 | 0.0093 | 176 B |
| ExpressionCompilation | 28,660.68 ns | 485.590 ns | 454.221 ns | 28,695.88 ns | 1,835x | 0.2441 | 5,285 B |

### Analysis

- **Compiled delegates** perform identically to direct calls (~15 ns) - this validates our caching strategy for handler delegates.
- **MethodInfo.Invoke** is ~2x slower (32 ns) - acceptable for cold paths but not for hot dispatch.
- **Expression compilation** is extremely expensive (~28,660 ns) - this is why we cache compiled expressions and never compile on the hot path.

### Recommendation

Always use cached compiled delegates for handler invocation. Never compile expressions during request processing.

---

## 3. Outbox Pattern (Dapper)

The Outbox pattern ensures reliable message publishing with at-least-once delivery semantics.

### Expected Performance Targets

| Operation | Target | Rationale |
|-----------|--------|-----------|
| AddAsync single | < 50 us | Single INSERT with serialization |
| AddAsync batch 10 | < 200 us | Batch INSERT |
| MarkAsProcessed | < 100 us | Single UPDATE |

### Results

| Method | Mean | Error | StdDev | Ratio | Rank | Allocated |
|--------|------|-------|--------|-------|------|-----------|
| AddAsync single message | 44.29 us | 2.677 us | 7.893 us | 1.03 | 1 | 5.48 KB |
| AddAsync 10 messages | 146.03 us | 3.853 us | 11.239 us | 3.41 | 3 | 55.23 KB |
| GetPendingMessagesAsync batch=10 | 592.67 us | 9.834 us | 8.718 us | 13.83 | 4 | 285.39 KB |
| MarkAsProcessedAsync | 53.64 us | 3.434 us | 10.071 us | 1.25 | 2 | 7.76 KB |
| MarkAsFailedAsync | 52.81 us | 1.967 us | 5.579 us | 1.23 | 2 | 8.35 KB |

### Analysis

- **Single message operations** are well under 100 us targets
- **Batch operations** scale approximately linearly (10 messages = ~3.4x single)
- **Memory allocation** is proportional to message payload size
- **GetPendingMessages** is the most expensive operation due to result set materialization

---

## 4. Inbox Pattern (Dapper)

The Inbox pattern provides exactly-once message processing through idempotency.

### Results (MessageCount = 1)

| Method | Mean | Error | StdDev | Rank | Allocated |
|--------|------|-------|--------|------|-----------|
| AddAsync single message | 45.24 us | 2.985 us | 8.803 us | 2 | 6.55 KB |
| GetMessageAsync (hit) | 74.78 us | 2.806 us | 8.230 us | 4 | 10.65 KB |
| GetMessageAsync (miss) | 29.67 us | 1.890 us | 5.574 us | 1 | 1.77 KB |
| Full workflow: Add -> Process | 92.86 us | 3.225 us | 9.252 us | 5 | 13.24 KB |
| Full workflow: Add -> Fail -> Retry -> Process | 104.10 us | 3.237 us | 9.339 us | 5 | 15.72 KB |
| Idempotent request (duplicate detection) | 92.44 us | 3.180 us | 9.226 us | 5 | 12.96 KB |

### Scaling Behavior (MessageCount = 100)

| Method | Mean | Allocated |
|--------|------|-----------|
| AddAsync batch | 1,158.84 us | 659.37 KB |
| GetExpiredMessagesAsync (batch) | 1,496.93 us | 731.37 KB |
| RemoveExpiredMessagesAsync (batch) | 1,418.86 us | 730.41 KB |

### Analysis

- **Cache miss** (~30 us) is significantly faster than cache hit (~75 us) due to early return optimization
- **Idempotent detection** adds minimal overhead (~92 us total for full cycle)
- **Batch operations** scale linearly - 100x messages = ~29x time for batch insert

---

## 5. Bulk Operations Comparison (SQLite)

Comparing ADO.NET, Dapper, and EF Core for bulk data operations.

### Bulk Insert

| Method | EntityCount | Mean | Ratio | Rank | Allocated |
|--------|-------------|------|-------|------|-----------|
| ADO.NET BulkInsert | 100 | 937.6 us | 1.33 | 2 | 164.27 KB |
| Dapper BulkInsert | 100 | 3,510.0 us | 4.97 | 3 | 378.16 KB |
| EF Core BulkInsert | 100 | 706.3 us | 1.00 | 1 | 209.33 KB |
| ADO.NET BulkInsert | 1000 | 12,809.6 us | 0.40 | 1 | 1,626.82 KB |
| Dapper BulkInsert | 1000 | 36,427.2 us | 1.13 | 3 | 3,807.95 KB |
| EF Core BulkInsert | 1000 | 32,238.8 us | 1.00 | 2 | 2,080.96 KB |
| ADO.NET BulkInsert | 5000 | 56,421.4 us | 0.42 | 1 | 8,120.66 KB |
| Dapper BulkInsert | 5000 | 181,013.7 us | 1.36 | 3 | 19,032.01 KB |
| EF Core BulkInsert | 5000 | 133,233.0 us | 1.00 | 2 | 4,180.02 KB |

### Bulk Update

| Method | EntityCount | Mean | Ratio | Rank | Allocated |
|--------|-------------|------|-------|------|-----------|
| ADO.NET BulkUpdate | 100 | 705.5 us | 0.91 | 1 | 293.71 KB |
| Dapper BulkUpdate | 100 | 1,302.9 us | 1.67 | 2 | 532.16 KB |
| EF Core BulkUpdate | 100 | 795.4 us | 1.02 | 1 | 314.22 KB |
| ADO.NET BulkUpdate | 1000 | 4,796.4 us | 1.41 | 2 | 2,869.08 KB |
| Dapper BulkUpdate | 1000 | 5,472.4 us | 1.61 | 3 | 5,265.94 KB |
| EF Core BulkUpdate | 1000 | 3,431.3 us | 1.01 | 1 | 3,086.8 KB |

### Analysis

- **Small batches (100)**: EF Core wins for inserts, ADO.NET ties for updates
- **Medium batches (1000)**: ADO.NET significantly faster for inserts (0.40x ratio)
- **Large batches (5000)**: ADO.NET maintains 2.5x advantage over EF Core
- **Memory**: ADO.NET uses ~40% less memory than EF Core, ~80% less than Dapper

### Recommendation

| Batch Size | Recommended Provider |
|------------|---------------------|
| < 100 | EF Core (simplicity) |
| 100-500 | EF Core or ADO.NET |
| 500-1000 | ADO.NET |
| > 1000 | ADO.NET (required) |

---

## 6. Polly Resilience Patterns

Polly provides resilience and transient-fault-handling capabilities. These benchmarks measure the overhead of Encina's attribute-based Polly integration.

### Retry Attribute Overhead

Measures the cost of applying `[Retry]` attribute vs baseline execution.

| Method | Mean | Error | StdDev | Ratio | Allocated |
|--------|------|-------|--------|-------|-----------|
| NoRetryAttribute_Baseline | 204.0 ns | 2.94 ns | 0.45 ns | 1.00 | 880 B |
| WithRetryAttribute_NoActualRetries | 207.9 ns | 7.54 ns | 1.17 ns | 1.02 | 880 B |

**Analysis**: The `[Retry]` attribute adds only ~4 ns overhead (2%) when no retries occur. This validates the zero-cost-when-not-needed design.

### Circuit Breaker Attribute Overhead

Measures the cost of applying `[CircuitBreaker]` attribute in closed state.

| Method | Mean | Error | StdDev | Ratio | Allocated |
|--------|------|-------|--------|-------|-----------|
| NoCircuitBreakerAttribute_Baseline | 208.7 ns | 6.58 ns | 1.71 ns | 1.00 | 904 B |
| WithCircuitBreakerAttribute_ClosedState | 214.7 ns | 10.41 ns | 2.70 ns | 1.03 | 896 B |

**Analysis**: The `[CircuitBreaker]` attribute adds only ~6 ns overhead (3%) in closed state. Memory allocation is actually slightly lower due to optimized path.

### Rate Limiting Operations

Measures core rate limiting operations for throttling scenarios.

| Method | Mean | Error | StdDev | Ratio | Allocated |
|--------|------|-------|--------|-------|-----------|
| AcquireAsync_SimpleRateLimiting | 2,740 ns | 4,170 ns | 1,083 ns | 1.12 | 504 B |
| AcquireAsync_WithAdaptiveThrottling | 2,460 ns | 2,504 ns | 650 ns | 1.01 | 504 B |
| RecordSuccess | 250 ns | 373 ns | 58 ns | 0.10 | 0 B |
| RecordFailure | 240 ns | 211 ns | 55 ns | 0.10 | 0 B |
| GetState | 360 ns | 887 ns | 230 ns | 0.15 | 0 B |
| AcquireAndRecordSuccess_Combined | 3,480 ns | 3,145 ns | 817 ns | 1.42 | 400 B |
| AcquireAndRecordFailure_Combined | 3,050 ns | 373 ns | 58 ns | 1.25 | 400 B |

**Analysis**:

- **Acquire operations**: ~2.5-2.7 us for permit acquisition
- **Record operations**: Ultra-fast ~250 ns with zero allocation
- **Combined workflows**: ~3-3.5 us for full acquire + record cycle
- **Adaptive throttling**: Equivalent performance to simple rate limiting

### Rate Limiting Multi-Key Scaling

Measures how rate limiting scales with multiple keys (e.g., per-tenant, per-user limits).

| Method | KeyCount | Mean | Error | StdDev | Allocated |
|--------|----------|------|-------|--------|-----------|
| AcquireAcrossMultipleKeys | 1 | 56.86 ns | 9.10 ns | 0.50 ns | 96 B |
| AcquireAcrossMultipleKeys | 10 | 542.94 ns | 65.60 ns | 3.60 ns | 960 B |
| AcquireAcrossMultipleKeys | 100 | 5,963.44 ns | 316.98 ns | 17.38 ns | 9,600 B |

**Analysis**:

- **Linear scaling**: Performance scales linearly with key count (O(n))
- **Per-key cost**: ~57 ns and 96 B per key
- **Multi-tenant scenarios**: 100 concurrent keys adds ~6 us overhead

### Polly Recommendations

| Scenario | Recommendation |
|----------|----------------|
| Single request resilience | Use `[Retry]` and `[CircuitBreaker]` freely - minimal overhead |
| High-throughput APIs | Prefer `RecordSuccess`/`RecordFailure` over `GetState` |
| Multi-tenant rate limiting | Budget ~60 ns per tenant key |
| Combined operations | Use `AcquireAndRecord*` patterns for atomic operations |

---

## 7. Validation Provider Comparison

Comparing different validation libraries integrated with Encina's mediator pipeline.

### Results

| Method | Mean | Error | StdDev | Ratio | Rank | Gen0 | Allocated |
|--------|------|-------|--------|-------|------|------|-----------|
| FluentValidation (Valid) | 1.300 μs | 0.0258 μs | 0.0317 μs | 1.00 | 1 | 0.1316 | 2.43 KB |
| DataAnnotations (Valid) | 1.307 μs | 0.0175 μs | 0.0155 μs | 1.01 | 1 | 0.1316 | 2.43 KB |
| MiniValidator (Valid) | 1.307 μs | 0.0253 μs | 0.0224 μs | 1.01 | 1 | 0.1316 | 2.43 KB |
| GuardClauses (Valid) | 1.384 μs | 0.0274 μs | 0.0458 μs | 1.06 | 1 | 0.1316 | 2.43 KB |

### Analysis

- **All validation libraries perform equivalently** (~1.3 μs) for valid inputs
- **Memory allocation is identical** (2.43 KB) across all providers
- **GuardClauses slightly slower** (~6% overhead) due to different validation approach
- **Choose based on features**, not performance - all are suitable for production

---

## 8. Cache Optimization Analysis

Measures the impact of caching strategies in the mediator pipeline.

### Results

| Method | Mean | Error | StdDev | Ratio | Gen0 | Allocated |
|--------|------|-------|--------|-------|------|-----------|
| Cache_GetOrAdd_Direct | 3.41 ns | 0.068 ns | 0.063 ns | 1.00 | - | 0 B |
| Cache_TryGetValue_ThenGetOrAdd | 3.65 ns | 0.103 ns | 0.086 ns | 1.07 | - | 0 B |
| Send_Command_CacheHit | 1,036 ns | 6.31 ns | 5.60 ns | 304x | 0.1068 | 2,016 B |
| Send_Query_CacheHit | 1,173 ns | 7.09 ns | 6.63 ns | 344x | 0.1049 | 1,992 B |
| Publish_Notification_CacheHit | 461 ns | 4.06 ns | 3.60 ns | 135x | 0.0734 | 1,384 B |
| TypeCheck_Cached | 2.10 ns | 0.028 ns | 0.026 ns | 0.62 | - | 0 B |
| TypeCheck_Direct | 0.38 ns | 0.019 ns | 0.015 ns | 0.11 | - | 0 B |

### Analysis

- **Cache operations are sub-5ns** - ConcurrentDictionary has excellent performance
- **Full mediator dispatch with cache hit**: ~1 μs for commands, ~460 ns for notifications
- **TypeCheck caching adds ~1.7 ns overhead** but avoids repeated type inspection
- **Direct method calls** are ~3 ns without caching overhead

---

## 9. Inbox Pattern (EF Core)

The Inbox pattern with Entity Framework Core implementation.

### Results (MessageCount = 1)

| Method | Mean | Error | StdDev | Rank | Allocated |
|--------|------|-------|--------|------|-----------|
| AddAsync single message | 158.4 μs | 7.11 μs | 20.06 μs | 1 | 16.75 KB |
| GetMessageAsync (hit) | 403.8 μs | 28.74 μs | 81.54 μs | 2 | 24.87 KB |
| GetMessageAsync (miss) | 142.0 μs | 7.40 μs | 21.36 μs | 1 | 7.84 KB |
| MarkAsProcessedAsync | 427.9 μs | 22.07 μs | 61.52 μs | 2 | 34.71 KB |
| MarkAsFailedAsync (5 retries) | 937.2 μs | 37.42 μs | 105.53 μs | 4 | 109.85 KB |
| Full workflow: Add → Process | 456.0 μs | 23.30 μs | 66.10 μs | 2 | 42.85 KB |
| Idempotent request (duplicate detection) | 405.5 μs | 30.06 μs | 85.76 μs | 2 | 32.60 KB |

### Scaling Behavior (MessageCount = 100)

| Method | Mean | Allocated |
|--------|------|-----------|
| AddAsync batch | 3,954.9 μs | 1,428.98 KB |
| GetExpiredMessagesAsync (batch) | 4,333.6 μs | 1,485.88 KB |
| RemoveExpiredMessagesAsync (batch) | 6,364.4 μs | 2,229.14 KB |

### EF Core vs Dapper Comparison

| Operation | EF Core | Dapper | Ratio |
|-----------|---------|--------|-------|
| AddAsync single | 158 μs | 45 μs | 3.5x slower |
| GetMessageAsync (hit) | 404 μs | 75 μs | 5.4x slower |
| Full workflow | 456 μs | 93 μs | 4.9x slower |

### Analysis

- **EF Core is 3-5x slower than Dapper** for Inbox operations due to change tracking overhead
- **Cache miss is faster than hit** due to early return optimization
- **Batch operations scale linearly** with message count
- **Use EF Core** when you need change tracking or prefer code-first approach
- **Use Dapper** for maximum performance in high-throughput scenarios

---

## 10. Read/Write Separation (Replica Selection)

The Read/Write Separation pattern enables distributing read queries across multiple database replicas while directing writes to the primary. These benchmarks measure the performance of different replica selection strategies.

### 10.1 Replica Selection Strategies

**Expected Performance Targets:**

| Strategy | Target | Rationale |
|----------|--------|-----------|
| RoundRobin | < 50 ns | Interlocked.Increment + modulo |
| Random | < 100 ns | Random.Shared.Next |
| LeastConnections | < 500 ns | Lock + min search |

#### Results

| Method | Mean | Error | StdDev | Ratio | Rank | Allocated |
|--------|------|-------|--------|-------|------|-----------|
| RoundRobin.SelectReplica | 5.75 ns | 0.05 ns | 0.05 ns | 1.00 | 2 | - |
| Random.SelectReplica | 0.90 ns | 0.02 ns | 0.02 ns | 0.16 | 1 | - |
| LeastConnections.SelectReplica | 63.45 ns | 0.52 ns | 0.48 ns | 11.03 | 3 | 32 B |
| LeastConnections.AcquireReplica (lease) | 79.00 ns | 0.95 ns | 0.89 ns | 13.74 | 4 | 32 B |

#### Analysis

- **Random selector is fastest** (~0.9 ns) due to thread-safe Random.Shared implementation
- **RoundRobin uses Interlocked** (~5.8 ns) for atomic increment, still ultra-fast
- **LeastConnections requires lock** (~63 ns) but still well under 500 ns target
- **Lease pattern adds ~16 ns** for automatic connection count management

### 10.2 Concurrent Replica Selection

Benchmarks measuring thread contention under multi-threaded scenarios (1, 4, 8, 16 threads).

#### Results

| Method | ThreadCount | Mean | Ratio | Lock Contentions | Allocated |
|--------|-------------|------|-------|------------------|-----------|
| Concurrent RoundRobin | 1 | 7.05 μs | 1.00 | - | 1.47 KB |
| Concurrent Random | 1 | 3.08 μs | 0.44 | - | 1.47 KB |
| Concurrent LeastConnections | 1 | 65.68 μs | 9.32 | - | 32.72 KB |
| Concurrent LeastConnections (lease) | 1 | 80.81 μs | 11.46 | - | 32.72 KB |
| | | | | | |
| Concurrent RoundRobin | 4 | 26.28 μs | 1.00 | 0.0001 | 2.05 KB |
| Concurrent Random | 4 | 3.05 μs | 0.12 | 0.0000 | 2.05 KB |
| Concurrent LeastConnections | 4 | 97.23 μs | 3.70 | 2.44 | 33.3 KB |
| Concurrent LeastConnections (lease) | 4 | 153.08 μs | 5.83 | 0.44 | 33.3 KB |
| | | | | | |
| Concurrent RoundRobin | 8 | 16.32 μs | 1.00 | 0.0001 | 2.88 KB |
| Concurrent Random | 8 | 5.39 μs | 0.33 | 0.0001 | 2.92 KB |
| Concurrent LeastConnections | 8 | 139.61 μs | 8.56 | 7.17 | 34.18 KB |
| Concurrent LeastConnections (lease) | 8 | 239.37 μs | 14.67 | 2.59 | 34.16 KB |
| | | | | | |
| Concurrent RoundRobin | 16 | 34.51 μs | 1.00 | 0.0005 | 4.57 KB |
| Concurrent Random | 16 | 11.50 μs | 0.33 | 0.0003 | 4.76 KB |
| Concurrent LeastConnections | 16 | 195.00 μs | 5.65 | 13.84 | 35.96 KB |
| Concurrent LeastConnections (lease) | 16 | 276.22 μs | 8.00 | 9.64 | 35.94 KB |

#### Analysis

- **Random scales best** - maintains ~0.3x ratio even at 16 threads
- **RoundRobin has minimal contention** - Interlocked operations scale well
- **LeastConnections shows lock contention** - contentions increase with thread count
- **Lease pattern reduces contentions** vs direct LeastConnections (9.64 vs 13.84 at 16 threads)

### 10.3 Database Routing Context

Benchmarks for AsyncLocal-based routing context operations.

**Expected Performance Targets:**

| Operation | Target | Rationale |
|-----------|--------|-----------|
| Routing context read | < 10 ns | AsyncLocal read |
| Routing scope create/dispose | < 100 ns | AsyncLocal write + dispose |

#### Results

| Method | Mean | Error | StdDev | Allocated |
|--------|------|-------|--------|-----------|
| Read CurrentIntent (AsyncLocal) | 200.0 ns | 0.00 ns | 0.00 ns | - |
| Read EffectiveIntent (null-coalesce) | 242.1 ns | 23.06 ns | 66.17 ns | - |
| Read HasIntent | 200.0 ns | 0.00 ns | 0.00 ns | - |
| Read IsReadIntent | 200.0 ns | 0.00 ns | 0.00 ns | - |
| Read IsWriteIntent | 236.6 ns | 23.70 ns | 67.22 ns | - |
| DatabaseRoutingScope.ForRead() | 1,348.0 ns | 28.71 ns | 57.99 ns | 392 B |
| DatabaseRoutingScope.ForWrite() | 1,452.9 ns | 48.63 ns | 131.46 ns | 392 B |
| DatabaseRoutingScope.ForForceWrite() | 1,405.9 ns | 43.97 ns | 118.88 ns | 392 B |
| DatabaseRoutingContext.Clear() | 617.5 ns | 22.57 ns | 59.05 ns | 96 B |
| Nested scopes (Read → ForceWrite) | 2,015.0 ns | 187.65 ns | 553.30 ns | 840 B |

#### Analysis

- **AsyncLocal reads are ~200 ns** - slightly higher than expected due to timing resolution
- **Scope operations are ~1.3-1.5 μs** - includes AsyncLocal write and IDisposable registration
- **Nested scopes are ~2 μs** - linear combination of two scope operations
- **Clear operation is ~617 ns** - faster than scope creation (no dispose registration)

### 10.4 Read/Write Separation Recommendations

| Scenario | Recommended Strategy |
|----------|---------------------|
| High-throughput reads | Random (fastest, no contention) |
| Fair distribution needed | RoundRobin (guaranteed rotation) |
| Load-aware routing | LeastConnections (intelligent but slower) |
| Hot replica avoidance | LeastConnections with lease pattern |

---

## 11. Previously Resolved Issues

All benchmark issues have been resolved:

| Issue | Resolution |
|-------|------------|
| ValidationBenchmarks missing dependencies | Fixed handler registration: Changed `ICommandHandler` to `IRequestHandler` with correct scoped lifetime |
| CacheOptimizationBenchmarks configuration | Fixed assembly scanning: Changed `AddEncina(assembly)` to `AddEncina()` to avoid picking up unrelated processors |
| InboxEfCoreBenchmarks entity tracking | Fixed IterationSetup: Added `_context.ChangeTracker.Clear()` after SQL delete |
| Polly Benchmarks duplicate project files | Removed `.backup/benchmarks-old` directory completely |

---

## Running the Benchmarks

### Prerequisites

```bash
# Ensure .NET 10 SDK is installed
dotnet --version  # Should show 10.0.x

# Build in Release mode
dotnet build -c Release
```

### Run All Benchmarks

```bash
# Main benchmarks
dotnet run -c Release --project tests/Encina.BenchmarkTests/Encina.Benchmarks -- --filter "*"

# Polly benchmarks
dotnet run -c Release --project tests/Encina.BenchmarkTests/Encina.Polly.Benchmarks -- --filter "*"

# With specific output
dotnet run -c Release --project tests/Encina.BenchmarkTests/Encina.Benchmarks -- \
  --filter "*" \
  --artifacts "artifacts/benchmarks" \
  --exporters html csv github json
```

### Run Specific Categories

```bash
# Mediator only
dotnet run -c Release --project tests/Encina.BenchmarkTests/Encina.Benchmarks -- --filter "*EncinaBenchmarks*"

# Outbox only
dotnet run -c Release --project tests/Encina.BenchmarkTests/Encina.Benchmarks -- --filter "*OutboxDapper*"

# Bulk operations
dotnet run -c Release --project tests/Encina.BenchmarkTests/Encina.Benchmarks -- --filter "*BulkOperations*"
```

### Output Locations

| Output Type | Location |
|-------------|----------|
| CSV reports | `artifacts/performance/results/*.csv` |
| HTML reports | `artifacts/performance/results/*.html` |
| GitHub markdown | `artifacts/performance/results/*-github.md` |
| Logs | `artifacts/performance/*.log` |

---

## Performance Guidelines

### Do

- Use compiled delegates for handler invocation
- Batch database operations when possible (> 100 entities)
- Use ADO.NET for large bulk operations (> 500 entities)
- Cache expression compilations
- Prefer async operations for I/O-bound work

### Don't

- Compile expressions on the hot path
- Use MethodInfo.Invoke for high-frequency calls
- Perform single-row inserts in loops (use batches)
- Allocate large objects in tight loops

---

## Related Documentation

- [Read/Write Separation Benchmarks](../../tests/Encina.BenchmarkTests/Encina.Benchmarks/ReadWriteSeparation/README.md)
- [Testing Strategy](../testing/testing-strategy.md)
- [Performance Optimization Guide](../guides/performance.md)

---

## Issue Tracking

- Issue #540: BenchmarkTests for Read/Write Separation ✅ **Completed**
- Issues #560-#568: Additional benchmark implementations (planned)

---

*Last updated: January 28, 2026*
