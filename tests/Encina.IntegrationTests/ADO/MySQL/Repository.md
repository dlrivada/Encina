# Integration Tests - ADO.MySQL Repository

## Status: Not Implemented

## Justification

Integration tests for ADO.MySQL Repository are not implemented for the following reasons:

### 1. Docker Dependency

MySQL integration tests require a running MySQL instance via Docker. The infrastructure exists but adds CI/CD complexity.

### 2. Adequate Coverage from Other Test Types

- **Unit Tests**: Full coverage of FunctionalRepositoryADO, EntityMappingBuilder, and SpecificationSqlBuilder
- **Guard Tests**: Parameter validation for all public methods
- **Property Tests**: Invariant verification for entity mappings
- **Contract Tests**: API consistency verification across all ADO providers

### 3. MySQL-Specific Syntax Tested in Unit Tests

The MySQL-specific syntax (e.g., backtick quoting, `LIMIT/OFFSET` pagination, `CHAR(36)` for GUIDs) is validated in unit tests.

### 4. Recommended Alternative

For CI/CD integration testing:
```bash
docker compose --profile databases up -d
dotnet test --filter "Category=Integration&Database=MySQL"
```

## Related Files

- `src/Encina.ADO.MySQL/Repository/FunctionalRepositoryADO.cs`
- `src/Encina.ADO.MySQL/Repository/EntityMappingBuilder.cs`
- `tests/Encina.UnitTests/ADO/MySQL/Repository/`

## Date: 2026-01-24

## Issue: #279
