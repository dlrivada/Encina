# LoadTests - Consent Validation

## Status: Not Implemented (Pending Event-Sourced Infrastructure)

## Justification

### 1. Architecture Migration to Event Sourcing

The consent module migrated from `InMemoryConsentStore` (ConcurrentDictionary-based) to a full
event-sourced model using Marten (PostgreSQL). The previous load tests directly instantiated
`InMemoryConsentStore` and tested ConcurrentDictionary throughput, which is no longer representative
of the actual system behavior.

The event-sourced consent system has fundamentally different performance characteristics:
- **Write path**: Appends events to a PostgreSQL stream (I/O-bound)
- **Read path**: Queries projected read models from PostgreSQL (I/O-bound)
- **Concurrency**: Handled by Marten's optimistic concurrency, not ConcurrentDictionary

### 2. Infrastructure Requirements

Meaningful load tests for the event-sourced consent system require:
- A running PostgreSQL instance with Marten configured
- Proper projection setup (inline or async)
- Cache layer configuration (ICacheProvider)
- These are integration-level concerns, not unit-level load tests

### 3. Adequate Coverage from Other Test Types

- **Unit Tests**: `DefaultConsentValidator`, `DefaultConsentService`, `ConsentAggregate` logic
- **Integration Tests**: Full pipeline with DI registration and options validation
- **Marten Integration Tests**: Event-sourced aggregate persistence and projection (requires PostgreSQL)
- **Property Tests**: Consent aggregate invariants under varied inputs

### 4. Recommended Alternative

When Marten integration test infrastructure is mature, create load tests that:
1. Use a real PostgreSQL instance (Docker/Testcontainers)
2. Test `IConsentService.GrantConsentAsync` under concurrent load
3. Measure `IConsentValidator.ValidateAsync` latency with projected read models
4. Benchmark cache hit/miss ratios under realistic workloads

These tests should live in `tests/Encina.LoadTests/Compliance/Consent/` and use the
`[Collection("Marten-PostgreSQL")]` fixture when available.

## Related Files

- `src/Encina.Compliance.Consent/Services/DefaultConsentService.cs` — Event-sourced service
- `src/Encina.Compliance.Consent/Aggregates/ConsentAggregate.cs` — Event-sourced aggregate
- `src/Encina.Compliance.Consent/DefaultConsentValidator.cs` — Validator (uses IConsentService)
- `tests/Encina.IntegrationTests/Compliance/Consent/` — DI and pipeline integration tests

## Date: 2026-03-15
