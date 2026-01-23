# Integration Tests - Dapper.SqlServer Repository

## Status: Not Implemented

## Justification

Integration tests for Dapper.SqlServer Repository are not implemented for the following reasons:

### 1. Docker Dependency

SQL Server integration tests require a running SQL Server instance via Docker.

### 2. Adequate Coverage from Other Test Types

- **Unit Tests**: Full coverage of FunctionalRepositoryDapper, EntityMappingBuilder, and SpecificationSqlBuilder
- **Guard Tests**: Parameter validation for all public methods
- **Property Tests**: Invariant verification for entity mappings
- **Contract Tests**: API consistency verification across all Dapper providers

### 3. Dapper SQL Server Integration is Mature

Dapper's integration with SQL Server is one of the most common and well-tested combinations. The repository simply orchestrates Dapper calls.

### 4. Recommended Alternative

For CI/CD integration testing:
```bash
docker compose --profile databases up -d
dotnet test --filter "Category=Integration&Database=SqlServer&Provider=Dapper"
```

## Related Files

- `src/Encina.Dapper.SqlServer/Repository/FunctionalRepositoryDapper.cs`
- `src/Encina.Dapper.SqlServer/Repository/EntityMappingBuilder.cs`
- `tests/Encina.UnitTests/Dapper/SqlServer/Repository/`

## Date: 2026-01-24

## Issue: #279
