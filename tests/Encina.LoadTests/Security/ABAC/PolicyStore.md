# LoadTests - ABAC Policy Store (Persistent PAP)

## Status: Not Implemented

## Justification

The ABAC Persistent Policy Administration Point (`PersistentPolicyAdministrationPoint` + `IPolicyStore`) does not have concurrency-sensitive coordination logic that would benefit from load testing. Policy administration is an infrequent administrative operation, not a high-throughput hot path.

### 1. Administrative Operation, Not a Hot Path

Policy CRUD operations (save, update, delete) are performed by administrators, not by every request:

- **Write frequency**: Policies change infrequently (hours/days/weeks between updates)
- **Read frequency**: The `CachingPolicyStoreDecorator` caches policies in memory, so the database store is only hit on cache misses or cache invalidation
- **Concurrency**: It is unlikely that multiple administrators modify the same policy simultaneously; if they do, the upsert semantics handle it correctly via `FindAsync` → `SaveChangesAsync`

The actual authorization hot path is the `XACMLPolicyDecisionPoint`, which evaluates cached policies in-memory — no database calls occur per request.

### 2. Store Operations Are Thin Wrappers

The `IPolicyStore` implementations are thin persistence wrappers:

- **EF Core (`PolicyStoreEF`)**: Single `FindAsync` → `AddAsync`/`SetValues` → `SaveChangesAsync` per operation
- **Dapper (`PolicyStoreDapper`)**: Single parameterized SQL query per operation
- **ADO.NET (`PolicyStoreADO`)**: Single `DbCommand.ExecuteNonQueryAsync` per operation

Load characteristics are entirely determined by the underlying database, not by the store logic. Database-level load testing belongs in infrastructure-level tests, not application-level store tests.

### 3. Serialization Is the Only CPU-Bound Component

The `DefaultPolicySerializer` (System.Text.Json) is the only CPU-intensive component. Its performance is fully covered by the `PolicySerializerBenchmarks` (12 benchmarks across small/medium/large policy graphs, measuring serialization, deserialization, and round-trip throughput with memory allocation tracking).

### 4. Adequate Coverage from Other Test Types

- **Unit Tests**: Cover all serializer logic, entity mapping, error handling, and ROP patterns
- **Guard Tests**: Verify null/empty parameter validation for all public methods
- **Property Tests**: Verify serialization round-trip invariants with randomly generated policy graphs
- **Contract Tests**: Verify all 13 provider implementations conform to the `IPolicyStore` contract
- **Integration Tests** (182 tests across 11 providers): Verify actual database persistence with real Docker containers
- **Benchmark Tests** (12 benchmarks): Measure serialization throughput and memory allocation at varying graph sizes

### 5. Recommended Alternative

If load testing becomes necessary (e.g., bulk policy migration scenario):

- Add an NBomber scenario simulating concurrent policy saves across multiple tenants
- Focus on database connection pool saturation under burst writes
- Test the `CachingPolicyStoreDecorator` cache invalidation pub/sub under concurrent writes
- Measure cache hit ratio and invalidation latency under sustained read load

## Related Files

- `src/Encina.Security.ABAC/Persistence/` - Core abstractions and serializer
- `src/Encina.EntityFrameworkCore/ABAC/` - EF Core store implementation
- `src/Encina.Dapper.*/ABAC/` - Dapper store implementations (4 providers)
- `src/Encina.ADO.*/ABAC/` - ADO.NET store implementations (4 providers)
- `tests/Encina.IntegrationTests/Security/ABAC/` - Integration tests (182 tests)
- `tests/Encina.BenchmarkTests/Encina.Benchmarks/Security/ABAC/` - Benchmark tests (12 benchmarks)

## Date: 2026-03-09
## Issue: #691
