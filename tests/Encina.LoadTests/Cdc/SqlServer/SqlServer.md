# Load Tests - SQL Server CDC

## Status: Not Implemented

## Justification

CDC is a streaming/polling pattern, not a request/response pattern. Load testing the connector layer in isolation does not provide meaningful performance insights because throughput depends entirely on the database's Change Tracking mechanism.

### 1. I/O-Bound Polling Pattern

The SQL Server CDC connector polls `CHANGETABLE` and `CHANGE_TRACKING_CURRENT_VERSION()` at configured intervals. The performance characteristics are:

- **Throughput**: Determined by SQL Server's change tracking query performance and the volume of tracked changes, not by the connector code
- **Latency**: Dominated by SQL query execution time against the change tracking tables
- **Polling interval**: Configurable in `SqlServerCdcOptions`, but the bottleneck is always the database query, not the polling logic

Load testing the .NET connector would measure SQL Server's change tracking query performance, not Encina's code.

### 2. Position Serialization Is O(1)

`SqlServerCdcPosition.ToBytes()` and `FromBytes()` serialize a `long` version number using `BinaryPrimitives`. This is a fixed-size, zero-allocation operation that completes in nanoseconds and is not a meaningful load test target.

### 3. Adequate Coverage from Other Test Types

- **Unit Tests**: Position serialization, options validation, DI registration
- **Property Tests**: Serialization roundtrip invariants with randomized inputs across all CDC position types
- **Contract Tests**: `ICdcPosition` contract compliance across all providers

### 4. Recommended Alternative

If CDC throughput testing becomes necessary:

1. Use NBomber to measure events/second throughput for the `CdcDispatcher` with pre-generated change events
2. Stress test the bounded channel between connector and processor under high event rates
3. This should be part of a broader CDC load testing initiative, not specific to SQL Server

## Related Files

- `src/Encina.Cdc.SqlServer/` - Source package
- `tests/Encina.UnitTests/Cdc/SqlServer/` - Unit tests
- `tests/Encina.PropertyTests/Cdc/` - CDC property tests

## Date: 2026-03-25
## Issue: #899
