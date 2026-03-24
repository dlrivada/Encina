# Benchmark Tests - MySQL CDC

## Status: Not Implemented

## Justification

The MySQL CDC connector does not contain hot paths that would benefit from micro-benchmarking. The performance-critical operations are either simple serialization or delegated to external libraries.

### 1. Position Serialization Is Lightweight

`MySqlCdcPosition.ToBytes()` and `FromBytes()` serialize a GTID string and binlog position. This is a simple byte-array operation with predictable, constant-time performance. Benchmarking this would measure basic string-to-bytes encoding, which is a .NET runtime primitive.

### 2. No Computationally Intensive Code Paths

The connector's code paths are:

- **Options validation**: Runs once at startup, not a hot path
- **Health check**: Network I/O bound, not CPU bound
- **DI registration**: Runs once at startup
- **Binlog reading**: Delegated entirely to the MySQL client library

There are no algorithms, data structures, or transformation pipelines in the connector code that would produce variable benchmark results.

### 3. Adequate Coverage from Other Test Types

- **Unit Tests**: Position serialization correctness, options validation, DI registration
- **Property Tests**: Serialization roundtrip invariants with randomized inputs verify correctness under diverse conditions
- **Contract Tests**: `ICdcPosition` contract compliance ensures consistent behavior

### 4. Recommended Alternative

If benchmark data is needed:

1. Add a `CdcPositionSerializationBenchmarks` class that benchmarks all CDC position types together (PostgreSQL, MySQL, SQL Server, MongoDB) for comparison
2. Expected result: sub-microsecond for all providers, within measurement noise of each other

## Related Files

- `src/Encina.Cdc.MySql/` - Source package
- `tests/Encina.UnitTests/Cdc/MySql/` - Unit tests
- `tests/Encina.PropertyTests/Cdc/` - CDC property tests

## Date: 2026-03-25
## Issue: #899
