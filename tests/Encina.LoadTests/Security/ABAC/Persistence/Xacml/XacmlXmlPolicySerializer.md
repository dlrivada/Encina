# LoadTests - XACML XML Policy Serializer

## Status: Not Implemented

## Justification

The `XacmlXmlPolicySerializer` is a stateless, thread-safe XML serializer with no shared mutable state, no connection pools, and no resource contention. Load tests are not applicable for the following reasons:

### 1. No Shared State or Concurrency Concerns

The serializer is a sealed class with only a readonly `ILogger` dependency. Each serialization/deserialization operation:

- Creates new `XDocument`/`XElement` instances (no shared buffers)
- Uses no locks, semaphores, or synchronization primitives
- Has no connection pooling or resource management
- Cannot exhibit contention, deadlocks, or race conditions

Load tests are designed to validate behavior under high concurrency with shared resources. Since this component has no shared resources, load testing would only measure `System.Xml.Linq` throughput — which is Microsoft's responsibility.

### 2. No External Dependencies Under Load

Unlike database stores (OutboxStore, InboxStore) where load tests validate connection pool behavior, transaction isolation, and query performance under concurrent access, the serializer has no external systems to stress-test.

### 3. Adequate Coverage from Other Test Types

- **Unit Tests**: Full round-trip coverage for all ABAC model types
- **Property Tests**: FsCheck generates thousands of random inputs per run, providing implicit throughput validation
- **Contract Tests**: Verify API shape stability under any usage pattern

### 4. Recommended Alternative

If serialization performance becomes a concern:

- Create **BenchmarkDotNet** benchmarks in `tests/Encina.BenchmarkTests/` to measure per-operation throughput, memory allocation, and GC pressure
- Focus on: large policy graphs (deep nesting), complex expression trees, many obligations/advice expressions
- This provides actionable performance data without the overhead of load test infrastructure

## Related Files

- `src/Encina.Security.ABAC/Persistence/Xacml/XacmlXmlPolicySerializer.cs`
- `tests/Encina.UnitTests/Security/ABAC/Persistence/Xacml/XacmlXmlPolicySerializerTests.cs`
- `tests/Encina.PropertyTests/Security/ABAC/Persistence/Xacml/XacmlXmlRoundTripPropertyTests.cs`

## Date: 2026-03-09
## Issue: #692
