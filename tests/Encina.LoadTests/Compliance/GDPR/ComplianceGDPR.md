# LoadTests - Compliance GDPR

## Status: Not Implemented

## Justification

The `Encina.Compliance.GDPR` module does not warrant dedicated load tests for the following technical reasons:

### 1. No Concurrent or Shared Mutable State

The GDPR compliance pipeline operates within the request pipeline, which is inherently single-request-scoped:

| Component | Concurrency Model |
|-----------|-------------------|
| `InMemoryProcessingActivityRegistry` | `ConcurrentDictionary` (already thread-safe) |
| `GDPRCompliancePipelineBehavior` | Scoped per request, no shared state |
| `DefaultGDPRComplianceValidator` | Stateless |
| `ProcessingActivityAttribute` cache | `ConcurrentDictionary` (read-heavy, thread-safe) |

There are no locks, semaphores, or contention points that would benefit from load testing.

### 2. Pipeline Behavior is CPU-Bound and O(1)

The compliance behavior performs the following per request:

| Operation | Time Complexity | Estimated Cost |
|-----------|-----------------|----------------|
| Attribute cache lookup | O(1) | ~50-100 nanoseconds |
| Registry dictionary lookup | O(1) | ~50-200 nanoseconds |
| Compliance validation | O(1) | ~10-50 nanoseconds (default) |
| Activity Tracing tag set | O(1) | ~100-300 nanoseconds |

**Total overhead**: ~200-650 nanoseconds per request.

The actual request handler (database queries, business logic) will always dominate total execution time by orders of magnitude.

### 3. RoPA Export is Batch, Not Concurrent

The `IRoPAExporter` implementations (JSON/CSV) are designed for administrative batch operations (compliance audits), not high-frequency concurrent usage. A compliance officer runs an export occasionally, not under sustained load.

### 4. Auto-Registration is One-Time at Startup

`GDPRAutoRegistrationHostedService` scans assemblies once during application startup. This is not a recurring operation and does not benefit from load testing.

### 5. Adequate Coverage from Other Test Types

- **Unit Tests**: 60+ tests covering all code paths, including attribute caching, pipeline modes, export edge cases
- **Guard Tests**: Null validation for all public methods
- **Property Tests**: FsCheck invariants for registry operations, compliance results, exporter consistency
- **Contract Tests**: Interface contract verification for all public interfaces

### 6. When Load Tests Would Be Appropriate

Load tests for GDPR compliance would be appropriate if:

1. **Custom IProcessingActivityRegistry**: A database-backed registry with network I/O
2. **Custom IGDPRComplianceValidator**: A validator that calls external compliance services
3. **High-frequency RoPA exports**: Automated export pipelines under concurrency

**Recommended future load test scenario**:

```csharp
// Only implement if database-backed registry is added
[LoadTest]
public async Task RegistryLookup_UnderHighConcurrency_MaintainsThroughput()
{
    var registry = new DatabaseProcessingActivityRegistry(connectionString);
    var tasks = Enumerable.Range(0, 1000)
        .Select(_ => registry.GetActivityByRequestTypeAsync(typeof(MyCommand)));

    var results = await Task.WhenAll(tasks);
    // Assert all succeeded and latency is within bounds
}
```

## Related Files

- `src/Encina.Compliance.GDPR/` - Source implementation
- `tests/Encina.UnitTests/Compliance/GDPR/` - Unit test coverage
- `tests/Encina.GuardTests/Compliance/GDPR/` - Guard clause tests
- `tests/Encina.PropertyTests/Compliance/GDPR/` - Property-based tests
- `tests/Encina.ContractTests/Compliance/GDPR/` - Contract tests

## Date: 2026-02-17
## Issue: #402
