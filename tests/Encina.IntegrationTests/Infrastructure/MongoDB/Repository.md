# Integration Tests - MongoDB Repository

## Status: Not Implemented

## Justification

Integration tests for MongoDB Repository are not implemented for the following reasons:

### 1. Docker Dependency

MongoDB integration tests require a running MongoDB instance via Docker.

### 2. Adequate Coverage from Other Test Types

- **Unit Tests**: Full coverage of FunctionalRepositoryMongoDB using mocked IMongoCollection
- **Guard Tests**: Parameter validation for all public methods (collection, idSelector, options)
- **Property Tests**: Invariant verification for MongoDbRepositoryOptions
- **Contract Tests**: API consistency verification with relational repository implementations

### 3. MongoDB Driver Has Excellent Mocking Support

The MongoDB .NET Driver supports mocking through `IMongoCollection<T>`, which is used extensively in unit tests.

### 4. Recommended Alternative

For integration tests with real MongoDB:
```bash
docker compose --profile databases up -d
dotnet test --filter "Category=Integration&Database=MongoDB"
```

Or use MongoDB.Driver.Core testing utilities:
```csharp
var client = new MongoClient("mongodb://localhost:27017");
var database = client.GetDatabase("test");
var collection = database.GetCollection<Order>("orders");
```

## Related Files

- `src/Encina.MongoDB/Repository/FunctionalRepositoryMongoDB.cs`
- `src/Encina.MongoDB/Repository/SpecificationFilterBuilder.cs`
- `src/Encina.MongoDB/Repository/MongoDbRepositoryOptions.cs`
- `tests/Encina.UnitTests/MongoDB/Repository/`

## Date: 2026-01-24

## Issue: #279
