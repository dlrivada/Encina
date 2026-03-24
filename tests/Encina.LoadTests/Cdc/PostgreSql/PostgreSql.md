# Load Tests - PostgreSQL CDC

## Status: Not Implemented

## Justification

CDC is a streaming/polling pattern, not a request/response pattern. Load testing the connector layer in isolation does not provide meaningful performance insights because throughput depends entirely on the database's WAL replication mechanism.

### 1. I/O-Bound Streaming Pattern

The PostgreSQL CDC connector reads from the Write-Ahead Log via logical replication. The performance characteristics are:

- **Throughput**: Determined by PostgreSQL's WAL generation rate and logical decoding speed, not by the connector code
- **Latency**: Dominated by network round-trips between the connector and PostgreSQL's replication slot
- **Backpressure**: Controlled by the replication protocol (the server pauses WAL streaming when the client is slow)

Load testing the .NET connector would measure PostgreSQL's replication performance, not Encina's code.

### 2. Position Serialization Is O(1)

`PostgresCdcPosition.ToBytes()` and `FromBytes()` use `BinaryPrimitives.WriteInt64BigEndian` / `ReadInt64BigEndian` for the LSN value. This is a fixed-size, zero-allocation operation that completes in nanoseconds and is not a meaningful load test target.

### 3. Adequate Coverage from Other Test Types

- **Unit Tests**: Position serialization, options validation, DI registration, health check behavior
- **Property Tests**: Serialization roundtrip invariants with randomized inputs across all CDC position types
- **Contract Tests**: `ICdcPosition` contract compliance across all providers

### 4. Recommended Alternative

If CDC throughput testing becomes necessary:

1. Use NBomber to measure events/second throughput for the `CdcDispatcher` with pre-generated change events
2. Stress test the bounded channel between connector and processor under high event rates
3. This should be part of a broader CDC load testing initiative, not specific to PostgreSQL

## Related Files

- `src/Encina.Cdc.PostgreSql/` - Source package
- `tests/Encina.UnitTests/Cdc/PostgreSql/` - Unit tests
- `tests/Encina.PropertyTests/Cdc/` - CDC property tests

## Date: 2026-03-25
## Issue: #899
