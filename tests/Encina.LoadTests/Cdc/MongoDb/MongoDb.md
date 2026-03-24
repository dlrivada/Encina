# Load Tests - MongoDB CDC

## Status: Not Implemented

## Justification

CDC is a streaming/polling pattern, not a request/response pattern. Load testing the connector layer in isolation does not provide meaningful performance insights because throughput depends entirely on MongoDB's change stream mechanism.

### 1. I/O-Bound Streaming Pattern

The MongoDB CDC connector uses the MongoDB driver's `Watch()` API to open a change stream cursor. The performance characteristics are:

- **Throughput**: Determined by the oplog tailing speed and MongoDB's change stream aggregation pipeline, not by the connector code
- **Latency**: Dominated by the network round-trip between the connector and the MongoDB replica set
- **Backpressure**: Controlled by the cursor iteration speed and MongoDB's oplog retention

Load testing the .NET connector would measure MongoDB's change stream performance, not Encina's code.

### 2. Position Serialization Is Lightweight

`MongoCdcPosition.ToBytes()` and `FromBytes()` serialize a BSON resume token. This is a simple byte-array copy operation that completes in nanoseconds and is not a meaningful load test target.

### 3. Adequate Coverage from Other Test Types

- **Unit Tests**: Position serialization, options validation, DI registration
- **Property Tests**: Serialization roundtrip invariants with randomized inputs across all CDC position types
- **Contract Tests**: `ICdcPosition` contract compliance across all providers

### 4. Recommended Alternative

If CDC throughput testing becomes necessary:

1. Use NBomber to measure events/second throughput for the `CdcDispatcher` with pre-generated change events
2. Stress test the bounded channel between connector and processor under high event rates
3. This should be part of a broader CDC load testing initiative, not specific to MongoDB

## Related Files

- `src/Encina.Cdc.MongoDb/` - Source package
- `tests/Encina.UnitTests/Cdc/MongoDb/` - Unit tests
- `tests/Encina.PropertyTests/Cdc/` - CDC property tests

## Date: 2026-03-25
## Issue: #899
