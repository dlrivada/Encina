# BenchmarkTests - Consent Store and Pipeline

## Status: Not Implemented (Pending Event-Sourced Infrastructure)

## Justification

### 1. Architecture Migration to Event Sourcing

The consent module migrated from `InMemoryConsentStore` to an event-sourced model
using Marten (PostgreSQL). The previous benchmarks measured:
- `InMemoryConsentStore` operations (ConcurrentDictionary lookups) — no longer exists
- `ConsentRecord` creation and serialization — replaced by `ConsentReadModel` projections
- `BulkRecordConsentAsync` — removed from the API (not applicable to event sourcing)

These benchmarks tested in-memory data structure performance, not the actual consent system.

### 2. Event-Sourced Performance Is I/O-Bound

The event-sourced consent system's performance is dominated by:
- PostgreSQL event stream appends (write path)
- Marten projection materialization (read path)
- Cache hit/miss ratios (ICacheProvider)

Benchmarking these with BenchmarkDotNet requires a real PostgreSQL instance,
making them integration-level benchmarks rather than micro-benchmarks.

### 3. Pipeline Benchmarks Require Full DI Stack

The `ConsentPipelineBenchmarks` previously used `InMemoryConsentStore` seeded
with test data. The new pipeline uses `DefaultConsentService` which depends on
`IAggregateRepository<ConsentAggregate>` and `IReadModelRepository<ConsentReadModel>`,
both requiring Marten session infrastructure.

### 4. Adequate Coverage from Other Test Types

- **Unit Tests**: Consent aggregate logic, validator behavior, pipeline behavior
- **Integration Tests**: DI registration, options validation, full pipeline wiring
- **Load Tests**: When implemented, will cover throughput under concurrent access

### 5. Recommended Alternative

When Marten benchmarking infrastructure is available:

1. **ConsentServiceBenchmarks**: Benchmark `IConsentService.GrantConsentAsync` and
   `HasValidConsentAsync` against a real PostgreSQL instance
2. **ConsentValidatorBenchmarks**: Benchmark `IConsentValidator.ValidateAsync` with
   varying numbers of purposes and cache states
3. **ConsentProjectionBenchmarks**: Benchmark projection materialization latency

Use `[GlobalSetup]` to start a Testcontainers PostgreSQL instance and configure Marten.

## Related Files

- `src/Encina.Compliance.Consent/Services/DefaultConsentService.cs` — Event-sourced service
- `src/Encina.Compliance.Consent/DefaultConsentValidator.cs` — Validator
- `src/Encina.Compliance.Consent/ConsentRequiredPipelineBehavior.cs` — Pipeline behavior

## Date: 2026-03-15
