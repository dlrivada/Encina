# Benchmark Tests - MongoDB CDC

## Status: Not Implemented

## Justification

The MongoDB CDC connector does not contain hot paths that would benefit from micro-benchmarking. The performance-critical operations are either simple byte-array copies or delegated to the MongoDB driver.

### 1. Position Serialization Is Lightweight

`MongoCdcPosition.ToBytes()` and `FromBytes()` serialize a BSON resume token as a byte array. This is a simple array copy operation with predictable, constant-time performance. Benchmarking this would measure basic `Array.Copy` / `MemoryStream` performance, which is a .NET runtime primitive.

### 2. No Computationally Intensive Code Paths

The connector's code paths are:

- **Options validation**: Runs once at startup, not a hot path
- **Health check**: Network I/O bound, not CPU bound
- **DI registration**: Runs once at startup
- **Change stream reading**: Delegated entirely to the MongoDB C# driver's `Watch()` API

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

- `src/Encina.Cdc.MongoDb/` - Source package
- `tests/Encina.UnitTests/Cdc/MongoDb/` - Unit tests
- `tests/Encina.PropertyTests/Cdc/` - CDC property tests

## Date: 2026-03-25
## Issue: #899
