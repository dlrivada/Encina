# Integration Tests - ADO.SqlServer Repository

## Status: Not Implemented

## Justification

Integration tests for ADO.SqlServer Repository are not implemented for the following reasons:

### 1. Docker Dependency

SQL Server integration tests require a running SQL Server instance via Docker. While the infrastructure exists (`docker-compose.yml`), these tests are resource-intensive and add significant CI/CD pipeline time.

### 2. Adequate Coverage from Other Test Types

- **Unit Tests**: Full coverage of FunctionalRepositoryADO, EntityMappingBuilder, and SpecificationSqlBuilder
- **Guard Tests**: Parameter validation for all public methods
- **Property Tests**: Invariant verification for entity mappings
- **Contract Tests**: API consistency verification across all ADO providers

### 3. SQL Generation is Dialect-Specific but Tested

The SQL Server-specific syntax (e.g., `[identifier]` quoting, `OFFSET/FETCH` pagination, `@param` parameters) is validated in unit tests through SQL string assertions.

### 4. Recommended Alternative

For CI/CD integration testing:
```bash
docker compose --profile databases up -d
dotnet test --filter "Category=Integration&Database=SqlServer"
```

## Related Files

- `src/Encina.ADO.SqlServer/Repository/FunctionalRepositoryADO.cs`
- `src/Encina.ADO.SqlServer/Repository/EntityMappingBuilder.cs`
- `tests/Encina.UnitTests/ADO/SqlServer/Repository/`

## Date: 2026-01-24

## Issue: #279
