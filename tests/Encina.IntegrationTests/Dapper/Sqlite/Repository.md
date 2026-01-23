# Integration Tests - Dapper.Sqlite Repository

## Status: Not Implemented

## Justification

Integration tests for Dapper.Sqlite Repository are not implemented for the following reasons:

### 1. In-Memory Database Nature

SQLite with Dapper is primarily used for testing and local development. The repository implementation follows the same patterns as ADO.Sqlite.

### 2. Adequate Coverage from Other Test Types

- **Unit Tests**: Full coverage of FunctionalRepositoryDapper, EntityMappingBuilder, and SpecificationSqlBuilder
- **Guard Tests**: Parameter validation for all public methods
- **Property Tests**: Invariant verification for entity mappings across all 12 providers
- **Contract Tests**: API consistency verification ensuring all providers implement IFunctionalRepository correctly

### 3. Dapper Integration is Well-Tested

Dapper's integration with SQLite is a well-established pattern. The repository simply orchestrates Dapper calls with generated SQL.

### 4. Recommended Alternative

If database-level integration testing is needed:
```csharp
using var connection = new SqliteConnection("Data Source=:memory:");
await connection.OpenAsync();
// Create schema and test CRUD operations
```

## Related Files

- `src/Encina.Dapper.Sqlite/Repository/FunctionalRepositoryDapper.cs`
- `src/Encina.Dapper.Sqlite/Repository/EntityMappingBuilder.cs`
- `tests/Encina.UnitTests/Dapper/Sqlite/Repository/`

## Date: 2026-01-24

## Issue: #279
