# Integration Tests - Module Isolation

## Status: Not Implemented

## Justification

Module Isolation integration tests are intentionally not implemented at this time for the following reasons:

### 1. Development-Only Validation Strategy

Module Isolation uses `ModuleIsolationStrategy.DevelopmentValidationOnly` as the default strategy. This strategy:
- Validates SQL schema access at runtime in development
- Does NOT create real database users or permissions
- Throws `ModuleIsolationViolationException` when violations are detected

Real database permission testing would require:
- Creating actual database users per module
- Setting up schema permissions
- Managing database credentials in CI/CD
- Different SQL syntax per database vendor

### 2. Schema Parsing Complexity

The SQL schema validation relies on regex-based parsing which:
- Is tested via unit tests with mock dependencies
- Does not require actual database connections
- Works identically across all providers

### 3. Adequate Coverage from Other Test Types

- **Unit Tests**: Cover `ModuleSchemaRegistry` validation logic
- **Guard Tests**: Verify null parameter handling in factories
- **Property Tests**: Test schema name parsing with randomized inputs
- **Contract Tests**: Verify API consistency across all 10 ADO/Dapper providers

### 4. Infrastructure Complexity

Integration tests would require:
- 5 different database engines running simultaneously
- Complex permission setup scripts per database
- Teardown logic to restore database state
- Significant CI/CD infrastructure investment

### 5. Recommended Alternative

For applications that need to verify module isolation at the database level:

1. Use `ModuleIsolationStrategy.SchemaWithPermissions` in staging/production
2. Generate permission scripts using `IModulePermissionScriptGenerator`
3. Apply scripts as part of database migration
4. Test actual database permissions via manual or automated security audits

## Related Files

- `src/Encina/Modules/Isolation/ModuleIsolationOptions.cs`
- `src/Encina/Modules/Isolation/ModuleSchemaRegistry.cs`
- `src/Encina.*/Modules/ModuleAwareConnectionFactory.cs`
- `tests/Encina.ContractTests/Database/ModuleIsolation/`

## Date: 2026-01-24
## Issue: #534
