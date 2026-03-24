# IntegrationTests - SQL Server CDC

## Status: Not Implemented

## Justification

The SQL Server CDC connector (`SqlServerCdcConnector`) requires a SQL Server instance with Change Tracking or Change Data Capture enabled on specific tables. These features require `ALTER DATABASE` and `ALTER TABLE` permissions that go beyond standard integration test setup.

### 1. Specialized Database Configuration Required

Integration tests for the SQL Server CDC connector require:

- A SQL Server instance with Change Tracking enabled at the database level (`ALTER DATABASE ... SET CHANGE_TRACKING = ON`)
- Change Tracking enabled on each tracked table (`ALTER TABLE ... ENABLE CHANGE_TRACKING`)
- Appropriate retention period configuration
- Active DML operations to populate the change tracking tables

The standard SQL Server Docker image does not enable Change Tracking by default. An initialization script with `ALTER DATABASE` commands is required during container startup.

### 2. Connector Is Change Tracking-Coupled

`SqlServerCdcConnector` queries SQL Server's change tracking functions (`CHANGETABLE`, `CHANGE_TRACKING_CURRENT_VERSION`) to detect changes. The meaningful integration behavior is the interaction between DML operations and the change tracking metadata, which is entirely server-side.

### 3. Adequate Coverage from Other Test Types

- **Unit Tests**: Position serialization (`SqlServerCdcPositionTests`), options validation (`SqlServerCdcOptionsTests`), DI registration (`ServiceCollectionExtensionsTests`)
- **Guard Tests**: Core CDC guard tests cover dispatcher, configuration, and position store parameter validation
- **Contract Tests**: `CdcPositionContractTests` verifies all CDC position implementations follow the `ICdcPosition` contract
- **Property Tests**: `CdcPositionPropertyTests` verifies serialization roundtrip invariants with randomized inputs

### 4. Recommended Alternative

When Docker infrastructure supports SQL Server with Change Tracking:

1. Create a `SqlServerCdcFixture` that enables Change Tracking via initialization script
2. Test the full pipeline: enable tracking on test table, insert/update/delete rows, verify change events received with correct versions
3. Add as `[Trait("Category", "Integration")][Trait("Database", "SqlServer")]`

## Related Files

- `src/Encina.Cdc.SqlServer/` - Source package
- `tests/Encina.UnitTests/Cdc/SqlServer/` - Unit tests
- `tests/Encina.ContractTests/Cdc/` - CDC contract tests
- `tests/Encina.PropertyTests/Cdc/` - CDC property tests

## Date: 2026-03-25
## Issue: #899
