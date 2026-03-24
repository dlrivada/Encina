# Load Tests - MySQL CDC

## Status: Not Implemented

## Justification

CDC is a streaming/polling pattern, not a request/response pattern. Load testing the connector layer in isolation does not provide meaningful performance insights because throughput depends entirely on the database's binlog replication mechanism.

### 1. I/O-Bound Streaming Pattern

The MySQL CDC connector reads from the binary log stream. The performance characteristics are:

- **Throughput**: Determined by MySQL's binlog generation rate and the replication protocol speed, not by the connector code
- **Latency**: Dominated by network round-trips between the connector and MySQL's binlog stream
- **Backpressure**: Controlled by the replication protocol and TCP flow control

Load testing the .NET connector would measure MySQL's binlog replication performance, not Encina's code.

### 2. Position Serialization Is O(1)

`MySqlCdcPosition.ToBytes()` and `FromBytes()` serialize a GTID string and binlog position. This is a simple byte-array operation that completes in nanoseconds and is not a meaningful load test target.

### 3. Adequate Coverage from Other Test Types

- **Unit Tests**: Position serialization, options validation, DI registration
- **Property Tests**: Serialization roundtrip invariants with randomized inputs across all CDC position types
- **Contract Tests**: `ICdcPosition` contract compliance across all providers

### 4. Recommended Alternative

If CDC throughput testing becomes necessary:

1. Use NBomber to measure events/second throughput for the `CdcDispatcher` with pre-generated change events
2. Stress test the bounded channel between connector and processor under high event rates
3. This should be part of a broader CDC load testing initiative, not specific to MySQL

## Related Files

- `src/Encina.Cdc.MySql/` - Source package
- `tests/Encina.UnitTests/Cdc/MySql/` - Unit tests
- `tests/Encina.PropertyTests/Cdc/` - CDC property tests

## Date: 2026-03-25
## Issue: #899
