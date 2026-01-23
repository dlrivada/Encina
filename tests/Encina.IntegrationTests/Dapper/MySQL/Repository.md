# Integration Tests - Dapper.MySQL Repository

## Status: Not Implemented

## Justification

Integration tests for Dapper.MySQL Repository are not implemented for the following reasons:

### 1. Docker Dependency

MySQL integration tests require a running MySQL instance via Docker.

### 2. Adequate Coverage from Other Test Types

- **Unit Tests**: Full coverage of FunctionalRepositoryDapper, EntityMappingBuilder, and SpecificationSqlBuilder
- **Guard Tests**: Parameter validation for all public methods
- **Property Tests**: Invariant verification for entity mappings
- **Contract Tests**: API consistency verification across all Dapper providers

### 3. MySQL-Specific Features Tested in Unit Tests

The MySQL-specific syntax (backtick quoting, `?` or `@` parameters) is validated in unit tests.

### 4. Recommended Alternative

For CI/CD integration testing:
```bash
docker compose --profile databases up -d
dotnet test --filter "Category=Integration&Database=MySQL&Provider=Dapper"
```

## Related Files

- `src/Encina.Dapper.MySQL/Repository/FunctionalRepositoryDapper.cs`
- `src/Encina.Dapper.MySQL/Repository/EntityMappingBuilder.cs`
- `tests/Encina.UnitTests/Dapper/MySQL/Repository/`

## Date: 2026-01-24

## Issue: #279
