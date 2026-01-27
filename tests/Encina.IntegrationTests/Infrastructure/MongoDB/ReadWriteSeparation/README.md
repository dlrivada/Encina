# MongoDB Read/Write Separation Integration Tests

This folder contains integration tests for MongoDB read/write separation functionality in Encina.

## Test Coverage

### Infrastructure Tests (`MongoDbReplicaSetFixtureTests.cs`)
Verifies the test infrastructure (single-node replica set) works correctly:
- Fixture availability when Docker is running
- Replica set PRIMARY member existence
- Read preference support (Primary, SecondaryPreferred)
- Transaction support (required for replica sets)

### Collection Factory Tests (`ReadWriteSeparationMongoDBIntegrationTests.cs`)
Tests `ReadWriteMongoCollectionFactory` and `IReadWriteMongoCollectionFactory`:
- **Write Collection**: Always uses `ReadPreference.Primary`
- **Read Collection**: Uses configured read preference (default: `SecondaryPreferred`)
- **Context-Based Routing**: `GetCollectionAsync<T>()` routes based on `DatabaseRoutingContext.CurrentIntent`
  - `DatabaseIntent.Read` -> configured read preference
  - `DatabaseIntent.Write` / `DatabaseIntent.ForceWrite` -> Primary
  - No context -> Primary (safe default)
- **Read Concern Configuration**: Validates read concern is applied correctly
- **MaxStaleness Configuration**: Validates max staleness settings

### Configuration Validation Tests
Tests `MongoReadWriteSeparationOptions` configuration:
- Default values (SecondaryPreferred, Majority read concern)
- All `MongoReadPreference` enum values (Primary, PrimaryPreferred, Secondary, SecondaryPreferred, Nearest)
- All `MongoReadConcern` enum values (Default, Local, Majority, Linearizable, Available, Snapshot)
- MaxStaleness configuration
- FallbackToPrimaryOnNoSecondaries option
- ValidateOnStartup option

### Health Check Tests
Tests `ReadWriteMongoHealthCheck`:
- Healthy status: Primary + secondaries available
- Degraded status: Primary only (no secondaries in single-node RS)
- Unhealthy status: No primary available / not a replica set
- Health check data dictionary contents
- Standalone server detection

## Test Infrastructure

### `MongoDbReplicaSetFixture`
Located in `Encina.TestInfrastructure.Fixtures`, provides:
- MongoDB 7.x container with single-node replica set (`rs0`)
- Connection string with replica set configuration
- Pre-configured `IMongoClient` and `IMongoDatabase`
- Replica set status verification methods

### `MongoDbReplicaSetCollection`
xUnit collection definition for sharing the fixture across test classes:
```csharp
[Collection(MongoDbReplicaSetCollection.Name)]
public class MyTests
{
    public MyTests(MongoDbReplicaSetFixture fixture) { }
}
```

## Limitations

### Single-Node Replica Set
Tests run against a **single-node replica set** where the same node is both PRIMARY and the only member. This means:

1. **No actual secondary reads**: All reads go to the same node regardless of read preference
2. **SecondaryPreferred behaves like Primary**: Falls back to PRIMARY since no secondaries exist
3. **Cannot test replication lag**: No secondary to have lag
4. **Cannot test failover**: No other nodes to fail over to

### What We CAN Test
- Read preference **configuration** is applied correctly to collections
- Read concern **configuration** is applied correctly
- `DatabaseRoutingContext` integration with collection factory
- Health check **logic** for different cluster states
- Configuration options are properly converted to MongoDB driver settings

### What We CANNOT Test (would require multi-node RS)
- Actual read distribution across replicas
- Replication lag scenarios
- Failover behavior
- Network partition handling
- MaxStaleness filtering of lagging secondaries

## Running Tests

```bash
# Run only MongoDB R/W separation tests
dotnet test --filter "FullyQualifiedName~MongoDB.ReadWriteSeparation"

# Run with Docker (required)
# Ensure Docker is running before executing tests
```

## Related Source Files

- `src/Encina.MongoDB/ReadWriteSeparation/ReadWriteMongoCollectionFactory.cs`
- `src/Encina.MongoDB/ReadWriteSeparation/IReadWriteMongoCollectionFactory.cs`
- `src/Encina.MongoDB/ReadWriteSeparation/MongoReadWriteSeparationOptions.cs`
- `src/Encina.MongoDB/ReadWriteSeparation/ReadWriteMongoHealthCheck.cs`
- `src/Encina.MongoDB/ReadWriteSeparation/MongoReadPreference.cs`
- `src/Encina.MongoDB/ReadWriteSeparation/MongoReadConcern.cs`
- `src/Encina.Messaging/ReadWriteSeparation/DatabaseRoutingContext.cs`
- `src/Encina.Messaging/ReadWriteSeparation/DatabaseIntent.cs`

## Issue Reference

GitHub Issue: [#547 - [TEST] Implement MongoDB Read/Write Separation Integration Tests](https://github.com/dlrivada/Encina/issues/547)
