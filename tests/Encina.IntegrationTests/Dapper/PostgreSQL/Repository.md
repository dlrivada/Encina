# Integration Tests - Dapper.PostgreSQL Repository

## Status: Not Implemented

## Justification

Integration tests for Dapper.PostgreSQL Repository are not implemented for the following reasons:

### 1. Docker Dependency

PostgreSQL integration tests require a running PostgreSQL instance via Docker.

### 2. Adequate Coverage from Other Test Types

- **Unit Tests**: Full coverage of FunctionalRepositoryDapper, EntityMappingBuilder, and SpecificationSqlBuilder
- **Guard Tests**: Parameter validation for all public methods
- **Property Tests**: Invariant verification for entity mappings
- **Contract Tests**: API consistency verification across all Dapper providers

### 3. PostgreSQL-Specific Features Tested in Unit Tests

The PostgreSQL-specific syntax (double-quote quoting, native UUID, JSONB support) is validated in unit tests.

### 4. Recommended Alternative

For CI/CD integration testing:
```bash
docker compose --profile databases up -d
dotnet test --filter "Category=Integration&Database=PostgreSQL&Provider=Dapper"
```

## Related Files

- `src/Encina.Dapper.PostgreSQL/Repository/FunctionalRepositoryDapper.cs`
- `src/Encina.Dapper.PostgreSQL/Repository/EntityMappingBuilder.cs`
- `tests/Encina.UnitTests/Dapper/PostgreSQL/Repository/`

## Date: 2026-01-24

## Issue: #279
