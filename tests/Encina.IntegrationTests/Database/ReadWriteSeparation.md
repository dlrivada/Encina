# Integration Tests - Read/Write Separation Pattern

## Status: Not Implemented (Placeholder for Future)

## Justification

Integration tests for the Read/Write Separation pattern are documented as a future consideration. Currently:

### 1. Unit Tests Provide Adequate Coverage with Mocks

The existing Unit Tests in `Encina.UnitTests/*/ReadWriteSeparation/` use mocked connection selectors to verify:

- Factory construction and validation
- Connection creation (read/write/routing-based)
- Connection string retrieval
- Cancellation token handling
- Routing scope management

### 2. Integration Tests Would Require Complex Database Infrastructure

Full integration tests would need:

- Multiple database instances per provider (primary + replicas)
- Docker containers with replication configured for each database provider:
  - SQLite: File-based copies (simulated replicas)
  - SQL Server: AlwaysOn replicas
  - PostgreSQL: Streaming replication
  - MySQL: Master-slave replication
  - Oracle: Data Guard
- Network configuration for replica connections
- Replication lag simulation for testing eventual consistency

### 3. Contract Tests Verify API Consistency

`Encina.ContractTests/Database/ReadWriteSeparation/ReadWriteSeparationContractTests.cs` verifies:

- All 10 ADO/Dapper providers implement the same `IReadWriteConnectionFactory` interface
- All providers have consistent method signatures
- All providers have health check implementations
- All providers have pipeline behavior implementations

### 4. Core Abstractions Are Thoroughly Tested

The core read/write separation abstractions in `Encina.Messaging` have comprehensive unit tests:

- `DatabaseRoutingContext` and `DatabaseRoutingScope`
- `IReadWriteConnectionSelector` implementations
- Replica selection strategies (RoundRobin, Random, LeastConnections)
- `ForceWriteDatabaseAttribute` behavior

### 5. Recommended Integration Tests (if implemented)

```csharp
[Trait("Category", "Integration")]
[Trait("Database", "SqlServer")]
public class ReadWriteSeparationSqlServerIntegrationTests : IAsyncLifetime
{
    private readonly SqlConnection _primaryConnection;
    private readonly SqlConnection _replicaConnection;

    [Fact]
    public async Task CreateReadConnection_RoutesToReplica()
    {
        // Arrange
        var options = new ReadWriteSeparationOptions
        {
            WriteConnectionString = "Server=primary;...",
            ReadConnectionStrings = { "Server=replica;..." }
        };
        var selector = new ReadWriteConnectionSelector(options);
        var factory = new ReadWriteConnectionFactory(selector);

        // Act
        await using var connection = await factory.CreateReadConnectionAsync();

        // Assert - verify connection is to replica (would need server identification)
    }

    [Fact]
    public async Task RoutingScope_WithReadIntent_RoutesToReplica()
    {
        // Arrange
        using var scope = new DatabaseRoutingScope(DatabaseIntent.Read);

        // Act
        await using var connection = await factory.CreateConnectionAsync();

        // Assert - verify routing based on scope
    }
}
```

## Related Files

- `src/Encina.Messaging/ReadWriteSeparation/` - Core abstractions
- `src/Encina.*/ReadWriteSeparation/` - Provider implementations (12 providers)
- `tests/Encina.UnitTests/*/ReadWriteSeparation/` - Unit tests
- `tests/Encina.ContractTests/Database/ReadWriteSeparation/` - Contract tests

## Date: 2026-01-24

## Issue: #283
