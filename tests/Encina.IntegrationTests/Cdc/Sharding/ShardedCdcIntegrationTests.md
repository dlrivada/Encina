# IntegrationTests - CDC Sharding

## Status: Not Implemented

## Justification

Sharded CDC is an **abstraction layer** that orchestrates multiple `ICdcConnector` instances across shards. It does not interact directly with databases — it delegates all database work to the per-shard `ICdcConnector` implementations, which already have their own integration tests.

### 1. No Direct Database Interaction

`ShardedCdcConnector`, `ShardedCdcProcessor`, and `InMemoryShardedCdcPositionStore` are purely in-memory components:

- `ShardedCdcConnector` aggregates `IAsyncEnumerable<ChangeEvent>` streams from child connectors via `Channel<T>`
- `ShardedCdcProcessor` is a `BackgroundService` that consumes aggregated streams and dispatches events
- `InMemoryShardedCdcPositionStore` uses `ConcurrentDictionary` with no external storage

None of these classes execute SQL, open database connections, or interact with external infrastructure.

### 2. Child Connectors Are Already Integration-Tested

The per-shard `ICdcConnector` implementations (Debezium, polling, etc.) have dedicated integration tests that verify:

- Real database CDC stream consumption
- Position tracking against real databases
- Connection lifecycle management
- Provider-specific SQL behavior

See: `tests/Encina.IntegrationTests/Cdc/` for existing integration tests.

### 3. Adequate Coverage from Other Test Types

- **Unit Tests** (86 tests): Full coverage of ShardedCdcConnector (streaming, positions, topology changes, disposal), ShardedCdcProcessor (processing loop, position saving, retry, batch size), InMemoryShardedCdcPositionStore (CRUD, concurrency, case-insensitivity), ShardedCdcHealthCheck (all health states), ShardedCdcMetrics (all recording methods)
- **Guard Tests** (23 tests): All public method parameter validation for all sharded CDC types
- **Contract Tests** (14 tests): IShardedCdcPositionStore composite key contract verification
- **Property Tests** (9 tests): FsCheck-based invariant verification for position store

### 4. Recommended Alternative

If database-backed position stores are implemented in the future (e.g., `SqlServerShardedCdcPositionStore`), those **would** require integration tests against real databases. At that point, create integration tests following the existing collection fixture pattern (`[Collection("ADO-SqlServer")]`).

## Related Files

- `src/Encina.Cdc/Sharding/ShardedCdcConnector.cs` - Aggregates per-shard connectors
- `src/Encina.Cdc/Sharding/ShardedCdcProcessor.cs` - Background processing loop
- `src/Encina.Cdc/Processing/InMemoryShardedCdcPositionStore.cs` - In-memory position store
- `src/Encina.Cdc/Health/ShardedCdcHealthCheck.cs` - Health check
- `src/Encina.OpenTelemetry/Cdc/ShardedCdcMetrics.cs` - Metrics
- `tests/Encina.UnitTests/Cdc/Sharding/` - Unit tests (86 tests)
- `tests/Encina.GuardTests/Cdc/Sharding/` - Guard tests (23 tests)
- `tests/Encina.ContractTests/Cdc/Sharding/` - Contract tests (14 tests)
- `tests/Encina.PropertyTests/Cdc/Sharding/` - Property tests (9 tests)

## Date: 2026-02-14
## Issue: #646
