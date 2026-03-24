# IntegrationTests - MySQL CDC

## Status: Not Implemented

## Justification

The MySQL CDC connector (`MySqlCdcConnector`) requires a MySQL instance with binary logging enabled and GTID mode active. The connector reads the binlog stream directly, which demands specific server-side configuration that is not part of the standard Docker setup.

### 1. Specialized Database Configuration Required

Integration tests for the MySQL CDC connector require:

- A MySQL instance with `log_bin = ON` and `binlog_format = ROW`
- GTID mode enabled (`gtid_mode = ON`, `enforce_gtid_consistency = ON`)
- A user with `REPLICATION SLAVE` and `REPLICATION CLIENT` privileges
- Active DML operations to generate binlog entries

The standard MySQL Docker image does not enable binary logging or GTID mode by default. A custom `my.cnf` or Docker entrypoint is required, adding infrastructure complexity beyond the current CI/CD pipeline.

### 2. Connector Is Binlog-Coupled

`MySqlCdcConnector` communicates directly with the MySQL binary log replication protocol. The meaningful integration behavior (receiving binlog events, tracking GTID positions) is entirely server-side. Testing this without a real MySQL server with binlog enabled provides no value.

### 3. Adequate Coverage from Other Test Types

- **Unit Tests**: Position serialization (`MySqlCdcPositionTests`), options validation (`MySqlCdcOptionsTests`), DI registration (`ServiceCollectionExtensionsTests`)
- **Guard Tests**: Core CDC guard tests cover dispatcher, configuration, and position store parameter validation
- **Contract Tests**: `CdcPositionContractTests` verifies all CDC position implementations follow the `ICdcPosition` contract
- **Property Tests**: `CdcPositionPropertyTests` verifies serialization roundtrip invariants with randomized inputs

### 4. Recommended Alternative

When Docker infrastructure supports custom MySQL configurations:

1. Create a `MySqlCdcFixture` with binlog and GTID enabled via custom `my.cnf`
2. Test the full pipeline: start connector, execute DML, verify change events with correct GTID positions
3. Add as `[Trait("Category", "Integration")][Trait("Database", "MySQL")]`

## Related Files

- `src/Encina.Cdc.MySql/` - Source package
- `tests/Encina.UnitTests/Cdc/MySql/` - Unit tests
- `tests/Encina.ContractTests/Cdc/` - CDC contract tests
- `tests/Encina.PropertyTests/Cdc/` - CDC property tests

## Date: 2026-03-25
## Issue: #899
