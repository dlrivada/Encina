# LoadTests - Compliance GDPR

## Status: Not Implemented

## Justification

The `Encina.Compliance.GDPR` module does not warrant dedicated load tests for the following technical reasons:

### 1. No Concurrent or Shared Mutable State

The GDPR compliance pipeline operates within the request pipeline, which is inherently single-request-scoped:

| Component | Concurrency Model |
|-----------|-------------------|
| `InMemoryProcessingActivityRegistry` | `ConcurrentDictionary` (already thread-safe) |
| Database-backed registries (13 providers) | Connection pool managed by DB driver (thread-safe) |
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

1. **Custom IGDPRComplianceValidator**: A validator that calls external compliance services
2. **High-frequency RoPA exports**: Automated export pipelines under concurrency

**Note on database-backed registries (Issue #681):**
As of Issue #681, database-backed `IProcessingActivityRegistry` implementations exist for all 13 providers
(ADO.NET, Dapper, EF Core, MongoDB). These are thin CRUD wrappers where actual load/concurrency handling
is managed by the underlying database connection pool. Load testing these would measure database driver
performance, not application code. Concurrent access behavior is validated in IntegrationTests instead.

## Note: Lawful Basis Load Tests ARE Implemented

This justification applies only to the **GDPR Core (RoPA)** module (#402). The **Lawful Basis Validation** feature (#413) has dedicated load tests:

- `tests/Encina.LoadTests/Compliance/GDPR/LawfulBasisValidationLoadTests.cs` — 8 high-concurrency scenarios (50 workers × 10K operations each)

See [Load Test Baselines — Compliance](../../../docs/testing/load-test-baselines.md#compliance-load-tests) for details.

## Related Files

- `src/Encina.Compliance.GDPR/` - Source implementation
- `tests/Encina.UnitTests/Compliance/GDPR/` - Unit test coverage
- `tests/Encina.GuardTests/Compliance/GDPR/` - Guard clause tests
- `tests/Encina.PropertyTests/Compliance/GDPR/` - Property-based tests
- `tests/Encina.ContractTests/Compliance/GDPR/` - Contract tests

## Date: 2026-02-17 (updated 2026-02-24)
## Issues: #402, #681
