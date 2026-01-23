# Integration Tests - ADO.Sqlite Repository

## Status: Not Implemented

## Justification

Integration tests for ADO.Sqlite Repository are not implemented for the following reasons:

### 1. In-Memory Database Nature

SQLite is primarily used as an in-memory or file-based database for testing purposes. While the repository implementation is functional, it's typically used for:
- Unit testing other components
- Local development scenarios
- Embedded applications

### 2. Adequate Coverage from Other Test Types

- **Unit Tests**: Full coverage of FunctionalRepositoryADO, EntityMappingBuilder, and SpecificationSqlBuilder
- **Guard Tests**: Parameter validation for all public methods (null checks, empty strings)
- **Property Tests**: Invariant verification for entity mappings across all 12 providers
- **Contract Tests**: API consistency verification ensuring all providers implement IFunctionalRepository correctly

### 3. SQL Generation is Tested in Unit Tests

The SQL generation logic (INSERT, UPDATE, DELETE, SELECT with specifications) is fully tested in unit tests without needing a real database connection. The parameterized queries prevent SQL injection.

### 4. Recommended Alternative

If database-level integration testing is needed for SQLite:
1. Use `Microsoft.Data.Sqlite` with an in-memory database
2. Create the schema using `EntityMappingBuilder` metadata
3. Test CRUD operations end-to-end

## Related Files

- `src/Encina.ADO.Sqlite/Repository/FunctionalRepositoryADO.cs`
- `src/Encina.ADO.Sqlite/Repository/EntityMappingBuilder.cs`
- `src/Encina.ADO.Sqlite/Repository/SpecificationSqlBuilder.cs`
- `tests/Encina.UnitTests/ADO/Sqlite/Repository/`

## Date: 2026-01-24

## Issue: #279
