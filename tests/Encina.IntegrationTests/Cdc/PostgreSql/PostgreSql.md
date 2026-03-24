# IntegrationTests - PostgreSQL CDC

## Status: Not Implemented

## Justification

The PostgreSQL CDC connector (`PostgresCdcConnector`) requires a PostgreSQL instance with logical replication enabled, including publication creation and replication slot management. The connector is `internal sealed` and interacts directly with the Write-Ahead Log (WAL) via Npgsql's `LogicalReplicationConnection`, making it impractical to test without a specially configured database server.

### 1. Specialized Database Configuration Required

Integration tests for the PostgreSQL CDC connector require:

- A PostgreSQL instance with `wal_level = logical` in `postgresql.conf`
- Permissions to create publications (`CREATE PUBLICATION`)
- Permissions to create replication slots (`pg_create_logical_replication_slot`)
- A table with active changes to generate WAL entries

The standard PostgreSQL Docker image does not enable logical replication by default. A custom Docker configuration or entrypoint script is needed, which adds significant infrastructure complexity beyond what the current CI/CD pipeline supports.

### 2. Connector Is Internal and WAL-Coupled

`PostgresCdcConnector` is `internal sealed` and communicates directly with PostgreSQL's logical replication protocol. The meaningful integration behavior (receiving WAL changes, advancing replication slots) is entirely server-side. Mocking this at the integration level would defeat the purpose of integration testing.

### 3. Adequate Coverage from Other Test Types

- **Unit Tests**: Position serialization (`PostgresCdcPositionTests`), options validation (`PostgresCdcOptionsTests`), DI registration (`ServiceCollectionExtensionsTests`)
- **Guard Tests**: Core CDC guard tests cover dispatcher, configuration, and position store parameter validation
- **Contract Tests**: `CdcPositionContractTests` verifies all CDC position implementations follow the `ICdcPosition` contract
- **Property Tests**: `CdcPositionPropertyTests` verifies serialization roundtrip invariants with randomized inputs

### 4. Recommended Alternative

When Docker infrastructure supports custom PostgreSQL configurations:

1. Create a `PostgreSqlCdcFixture` with `wal_level=logical` enabled via Docker entrypoint
2. Test the full pipeline: create publication, start connector, insert rows, verify change events received
3. Add as `[Trait("Category", "Integration")][Trait("Database", "PostgreSQL")]`
4. Consider sharing the fixture across CDC and non-CDC PostgreSQL integration tests

## Related Files

- `src/Encina.Cdc.PostgreSql/` - Source package
- `tests/Encina.UnitTests/Cdc/PostgreSql/` - Unit tests
- `tests/Encina.ContractTests/Cdc/` - CDC contract tests
- `tests/Encina.PropertyTests/Cdc/` - CDC property tests

## Date: 2026-03-25
## Issue: #899
