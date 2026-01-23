# Integration Tests - Dapper.Oracle Repository

## Status: Not Implemented

## Justification

Integration tests for Dapper.Oracle Repository are not implemented for the following reasons:

### 1. Docker Dependency and Licensing

Oracle integration tests require a running Oracle instance via Docker. Oracle's licensing and image size make this more complex.

### 2. Adequate Coverage from Other Test Types

- **Unit Tests**: Full coverage of FunctionalRepositoryDapper, EntityMappingBuilder, and SpecificationSqlBuilder
- **Guard Tests**: Parameter validation for all public methods
- **Property Tests**: Invariant verification for entity mappings
- **Contract Tests**: API consistency verification across all Dapper providers

### 3. Oracle-Specific Features Tested in Unit Tests

The Oracle-specific syntax (`:param` bind variables, `FETCH FIRST N ROWS ONLY`) is validated in unit tests.

### 4. Recommended Alternative

For CI/CD integration testing:
```bash
docker compose --profile databases up -d
dotnet test --filter "Category=Integration&Database=Oracle&Provider=Dapper"
```

## Related Files

- `src/Encina.Dapper.Oracle/Repository/FunctionalRepositoryDapper.cs`
- `src/Encina.Dapper.Oracle/Repository/EntityMappingBuilder.cs`
- `tests/Encina.UnitTests/Dapper/Oracle/Repository/`

## Date: 2026-01-24

## Issue: #279
