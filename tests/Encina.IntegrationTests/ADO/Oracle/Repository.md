# Integration Tests - ADO.Oracle Repository

## Status: Not Implemented

## Justification

Integration tests for ADO.Oracle Repository are not implemented for the following reasons:

### 1. Docker Dependency and Licensing

Oracle integration tests require a running Oracle instance via Docker. Oracle's licensing and image size make this more complex than other databases.

### 2. Adequate Coverage from Other Test Types

- **Unit Tests**: Full coverage of FunctionalRepositoryADO, EntityMappingBuilder, and SpecificationSqlBuilder
- **Guard Tests**: Parameter validation for all public methods
- **Property Tests**: Invariant verification for entity mappings
- **Contract Tests**: API consistency verification across all ADO providers

### 3. Oracle-Specific Syntax Tested in Unit Tests

The Oracle-specific syntax (e.g., `:param` bind variables, `FETCH FIRST N ROWS ONLY` pagination, uppercase identifiers) is validated in unit tests.

### 4. Recommended Alternative

For CI/CD integration testing:
```bash
docker compose --profile databases up -d
dotnet test --filter "Category=Integration&Database=Oracle"
```

## Related Files

- `src/Encina.ADO.Oracle/Repository/FunctionalRepositoryADO.cs`
- `src/Encina.ADO.Oracle/Repository/EntityMappingBuilder.cs`
- `tests/Encina.UnitTests/ADO/Oracle/Repository/`

## Date: 2026-01-24

## Issue: #279
