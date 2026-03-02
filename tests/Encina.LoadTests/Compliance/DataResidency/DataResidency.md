# LoadTests - Data Residency

## Status: Not Implemented

## Justification

The Data Residency module (Encina.Compliance.DataResidency) does not require dedicated load tests for the following reasons:

### 1. Non-Concurrent Core Operations

Data residency policy evaluation, region resolution, and cross-border transfer validation are predominantly single-request operations. They do not involve connection pool management, transaction coordination, or resource contention patterns that benefit from load testing.

### 2. In-Memory Default Stores

The default store implementations (`InMemoryResidencyPolicyStore`, `InMemoryDataLocationStore`, `InMemoryResidencyAuditStore`) use `ConcurrentDictionary` which is inherently thread-safe. Concurrent access correctness is already validated in integration tests.

### 3. Pipeline Behavior is Stateless

The `DataResidencyPipelineBehavior<TRequest, TResponse>` is registered as Transient and uses static per-type caching for attribute resolution. Each request gets its own instance, eliminating shared-state contention.

### 4. Adequate Coverage from Other Test Types

- **Unit Tests**: 23 files covering all services, mappers, stores, and models in isolation
- **Guard Tests**: 6 files verifying null-check guards on all public methods
- **Property Tests**: 3 files verifying invariants via FsCheck random generation
- **Contract Tests**: 6 files (3 abstract bases + 3 concrete) verifying store contracts
- **Integration Tests**: Full lifecycle, DI registration, options, and concurrent access (50 parallel operations)

### 5. Recommended Alternative

If load testing becomes necessary (e.g., after adding database-backed stores), consider:
- NBomber scenarios targeting cross-border transfer validation under sustained load
- Testcontainers-based load tests against real database providers
- Focus on store write throughput and policy lookup latency

## Related Files

- `src/Encina.Compliance.DataResidency/` — Source implementation
- `tests/Encina.UnitTests/Compliance/DataResidency/` — Unit tests
- `tests/Encina.IntegrationTests/Compliance/DataResidency/` — Integration tests
- `tests/Encina.ContractTests/Compliance/DataResidency/` — Contract tests
- `tests/Encina.PropertyTests/Compliance/DataResidency/` — Property tests
- `tests/Encina.GuardTests/Compliance/DataResidency/` — Guard tests

## Date: 2026-03-02
## Issue: #405
