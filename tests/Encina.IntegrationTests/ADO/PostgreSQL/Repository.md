# Integration Tests - ADO.PostgreSQL Repository

## Status: Not Implemented

## Justification

Integration tests for ADO.PostgreSQL Repository are not implemented for the following reasons:

### 1. Docker Dependency

PostgreSQL integration tests require a running PostgreSQL instance via Docker. The infrastructure exists but adds CI/CD complexity.

### 2. Adequate Coverage from Other Test Types

- **Unit Tests**: Full coverage of FunctionalRepositoryADO, EntityMappingBuilder, and SpecificationSqlBuilder
- **Guard Tests**: Parameter validation for all public methods
- **Property Tests**: Invariant verification for entity mappings
- **Contract Tests**: API consistency verification across all ADO providers

### 3. PostgreSQL-Specific Syntax Tested in Unit Tests

The PostgreSQL-specific syntax (e.g., `"identifier"` quoting, `LIMIT/OFFSET` pagination, native UUID support) is validated in unit tests.

### 4. Recommended Alternative

For CI/CD integration testing:
```bash
docker compose --profile databases up -d
dotnet test --filter "Category=Integration&Database=PostgreSQL"
```

## Related Files

- `src/Encina.ADO.PostgreSQL/Repository/FunctionalRepositoryADO.cs`
- `src/Encina.ADO.PostgreSQL/Repository/EntityMappingBuilder.cs`
- `tests/Encina.UnitTests/ADO/PostgreSQL/Repository/`

## Date: 2026-01-24

## Issue: #279
