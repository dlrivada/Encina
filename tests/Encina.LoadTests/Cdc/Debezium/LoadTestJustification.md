# Load Tests - Debezium CDC

## Status: Not Implemented

## Justification

The Debezium CDC connectors are I/O-bound components where load testing the connector layer in isolation does not provide meaningful performance insights.

### 1. I/O-Bound Nature of Debezium Connectors

- **HTTP connector**: The performance bottleneck is the network latency between Debezium Server and the .NET HTTP listener, not the listener itself. The bounded channel already provides backpressure via HTTP 503 responses.
- **Kafka connector**: The performance bottleneck is Kafka broker throughput, consumer group rebalancing, and partition assignment — all external to the .NET consumer wrapper.
- **DebeziumEventMapper**: JSON parsing is CPU-bound but extremely fast. Property tests with 200+ randomized inputs already validate correctness under diverse conditions.

### 2. External System Dependency

Load tests for Debezium would require:
- A running Debezium Server or Debezium Connect cluster
- A source database generating change events at scale
- A Kafka cluster (for Kafka mode)

This infrastructure complexity makes load tests impractical for CI/CD pipelines. Real performance testing is better done in staging environments with actual Debezium deployments.

### 3. Adequate Coverage from Other Test Types

- **Unit Tests**: ~84 tests covering all parsing paths, options, service registration, and position handling
- **Guard Tests**: ~16 tests verifying parameter validation
- **Contract Tests**: ~22 tests verifying CdcPosition contract compliance for both position types
- **Property Tests**: ~28 tests with randomized inputs (200 iterations each) verifying invariants
- **Integration Tests**: ~13 tests with realistic Debezium JSON payloads from SQL Server, PostgreSQL, MySQL

### 4. Recommended Alternative

When testing the full CDC pipeline (connector → processor → handler) under concurrent load, consider adding NBomber scenarios that:
1. Use the `DebeziumEventMapper` directly with pre-generated JSON payloads
2. Measure events/second throughput for the bounded channel
3. Test concurrent access patterns with multiple consumer instances

This should be done as part of a broader CDC load testing initiative (Issue TBD), not specific to Debezium.

## Related Files

- `src/Encina.Cdc.Debezium/` — Source files
- `tests/Encina.UnitTests/Cdc/Debezium/` — Unit tests
- `tests/Encina.ContractTests/Cdc/Debezium/` — Contract tests
- `tests/Encina.PropertyTests/Cdc/Debezium/` — Property tests
- `tests/Encina.IntegrationTests/Cdc/Debezium/` — Integration tests

## Date: 2026-02-09
## Issue: #288
