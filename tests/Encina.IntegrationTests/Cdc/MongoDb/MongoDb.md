# IntegrationTests - MongoDB CDC

## Status: Not Implemented

## Justification

The MongoDB CDC connector (`MongoCdcConnector`) uses MongoDB Change Streams, which require a replica set topology. Change Streams do not function on standalone MongoDB instances, making the standard single-node Docker container insufficient for integration testing.

### 1. Replica Set Required

Integration tests for the MongoDB CDC connector require:

- A MongoDB replica set (minimum 1 node with `--replSet` flag)
- Replica set initialization (`rs.initiate()`)
- An active oplog for change stream support

The standard MongoDB Docker image runs as a standalone instance. A custom Docker Compose setup or entrypoint script that initializes a single-node replica set is required.

### 2. Connector Is Change Stream-Coupled

`MongoCdcConnector` uses the MongoDB driver's `Watch()` API to open a change stream cursor. The meaningful integration behavior (receiving real-time change documents with resume tokens) depends entirely on the MongoDB oplog, which is only available in replica set mode.

### 3. Adequate Coverage from Other Test Types

- **Unit Tests**: Position serialization (`MongoCdcPositionTests`), options validation (`MongoCdcOptionsTests`), DI registration (`ServiceCollectionExtensionsTests`)
- **Guard Tests**: Core CDC guard tests cover dispatcher, configuration, and position store parameter validation
- **Contract Tests**: `CdcPositionContractTests` verifies all CDC position implementations follow the `ICdcPosition` contract
- **Property Tests**: `CdcPositionPropertyTests` verifies serialization roundtrip invariants with randomized inputs

### 4. Recommended Alternative

When Docker infrastructure supports MongoDB replica sets:

1. Create a `MongoCdcFixture` that starts MongoDB with `--replSet rs0` and runs `rs.initiate()`
2. Test the full pipeline: open change stream, insert/update/delete documents, verify change events received with correct resume tokens
3. Add as `[Trait("Category", "Integration")][Trait("Database", "MongoDB")]`

## Related Files

- `src/Encina.Cdc.MongoDb/` - Source package
- `tests/Encina.UnitTests/Cdc/MongoDb/` - Unit tests
- `tests/Encina.ContractTests/Cdc/` - CDC contract tests
- `tests/Encina.PropertyTests/Cdc/` - CDC property tests

## Date: 2026-03-25
## Issue: #899
