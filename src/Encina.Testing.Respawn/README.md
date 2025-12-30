# Encina.Testing.Respawn

Intelligent database cleanup for integration tests using the [Respawn](https://github.com/jbogard/Respawn) library. Respawn resets databases to a clean state by intelligently deleting data in the correct foreign key order.

## Installation

```bash
dotnet add package Encina.Testing.Respawn
```

## Why Respawn?

Traditional approaches to database cleanup in integration tests include:
- **Container recreation**: Slow (seconds per test)
- **Transaction rollback**: Doesn't test commits, can mask issues
- **Manual DELETE statements**: Fragile, must maintain order manually

**Respawn advantages**:
- **Fast**: Deletes data in milliseconds (orders of magnitude faster than container recreation)
- **Smart**: Automatically handles foreign key dependencies
- **Reliable**: Tested in production by many teams
- **Flexible**: Selective table/schema exclusion

## Quick Start

### Basic Usage

```csharp
using Encina.Testing.Respawn;

public class OrderTests : IAsyncLifetime
{
    private SqlServerRespawner _respawner = null!;
    private readonly string _connectionString = "Server=localhost;Database=TestDb;...";

    public async Task InitializeAsync()
    {
        _respawner = new SqlServerRespawner(_connectionString);
        await _respawner.InitializeAsync();
    }

    public async Task DisposeAsync()
    {
        await _respawner.DisposeAsync();
    }

    [Fact]
    public async Task CreateOrder_ShouldPersist()
    {
        // Arrange - Reset database to clean state
        await _respawner.ResetAsync();

        // Act - Create order
        // ...

        // Assert
        // ...
    }
}
```

### Using the Factory

```csharp
using Encina.Testing.Respawn;

// Create respawner with automatic provider detection
var respawner = RespawnerFactory.Create(
    RespawnAdapter.PostgreSql,
    "Host=localhost;Database=TestDb;Username=postgres;Password=secret");

await respawner.InitializeAsync();
await respawner.ResetAsync();
```

### Automatic Provider Inference

```csharp
// Infer provider from connection string
var adapter = RespawnerFactory.InferAdapter(connectionString);

if (adapter.HasValue)
{
    var respawner = RespawnerFactory.Create(adapter.Value, connectionString);
    await respawner.InitializeAsync();
}
```

## Supported Databases

| Database | Class | Via |
|----------|-------|-----|
| SQL Server | `SqlServerRespawner` | Respawn library |
| PostgreSQL | `PostgreSqlRespawner` | Respawn library |
| MySQL/MariaDB | `MySqlRespawner` | Respawn library |
| SQLite | `SqliteRespawner` | Custom implementation |
| Oracle | - | Not included (requires Oracle.ManagedDataAccess.Core) |

## Configuration

### RespawnOptions

```csharp
var options = new RespawnOptions
{
    // Tables to exclude from cleanup
    TablesToIgnore = ["AuditLog", "__EFMigrationsHistory"],

    // Only reset these schemas (empty = all schemas)
    SchemasToInclude = ["dbo", "sales"],

    // Exclude these schemas
    SchemasToExclude = ["sys", "INFORMATION_SCHEMA"],

    // Reset Encina messaging tables (OutboxMessages, InboxMessages, etc.)
    ResetEncinaMessagingTables = true, // default: true

    // Reset identity/sequence columns
    WithReseed = true, // default: true

    // Handle SQL Server temporal tables
    CheckTemporalTables = false // default: false
};

var respawner = new SqlServerRespawner(connectionString);
respawner.Options = options;
```

### Encina Messaging Tables

By default, Encina's messaging tables are reset. To preserve them:

```csharp
var options = new RespawnOptions
{
    ResetEncinaMessagingTables = false // Preserve OutboxMessages, InboxMessages, SagaStates, ScheduledMessages
};
```

The messaging tables are:
- `OutboxMessages`
- `InboxMessages`
- `SagaStates`
- `ScheduledMessages`

## xUnit Integration

### Class Fixture (Shared across tests in a class)

```csharp
public class DatabaseFixture : IAsyncLifetime
{
    public SqlServerRespawner Respawner { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        Respawner = new SqlServerRespawner("Server=localhost;Database=TestDb;...");
        await Respawner.InitializeAsync();
    }

    public async Task DisposeAsync()
    {
        await Respawner.DisposeAsync();
    }
}

public class OrderTests : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;

    public OrderTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => _fixture.Respawner.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Test1() { /* ... */ }
}
```

### Collection Fixture (Shared across test classes)

```csharp
[CollectionDefinition("Database")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture> { }

[Collection("Database")]
public class OrderTests : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;

    public OrderTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => _fixture.Respawner.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CreateOrder_ShouldPersist() { /* ... */ }
}

[Collection("Database")]
public class CustomerTests : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;

    public CustomerTests(DatabaseFixture fixture) => _fixture = fixture;

    public Task InitializeAsync() => _fixture.Respawner.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    // Same fixture, different test class
}
```

## SQLite Notes

SQLite doesn't have native Respawn support, so `SqliteRespawner` uses a custom implementation:

```csharp
var respawner = RespawnerFactory.CreateSqlite("Data Source=:memory:");
await respawner.InitializeAsync();
await respawner.ResetAsync();
```

For in-memory SQLite, you typically use a single connection that stays open. The respawner will delete from all user tables respecting foreign key constraints.

## Debugging

Use `GetDeleteCommands()` to see what SQL will be executed:

```csharp
var respawner = new SqlServerRespawner(connectionString);
await respawner.InitializeAsync();

var commands = respawner.GetDeleteCommands();
foreach (var command in commands ?? [])
{
    Console.WriteLine(command);
}
// Output:
// DELETE FROM [dbo].[OrderItems]
// DELETE FROM [dbo].[Orders]
// DELETE FROM [dbo].[Customers]
```

## Builder Pattern

For complex connection strings, use the builder pattern:

```csharp
var respawner = SqlServerRespawner.FromBuilder(builder =>
{
    builder.DataSource = "localhost";
    builder.InitialCatalog = "TestDb";
    builder.IntegratedSecurity = true;
    builder.TrustServerCertificate = true;
});

await respawner.InitializeAsync();
```

## Performance Tips

1. **Initialize once per test class** - Use `IClassFixture<T>` or `ICollectionFixture<T>`
2. **Reset before each test** - Not after (avoids cleanup on test failure)
3. **Exclude audit tables** - Use `TablesToIgnore` for tables that don't affect test isolation
4. **Use in-memory SQLite** - Fastest option for unit-like integration tests

## Related Packages

- **Encina.Testing.Fakes** - In-memory fakes for IEncina and stores
- **Encina.Testing.Shouldly** - Shouldly assertions for Either and Aggregates
- **Encina.TestInfrastructure** - Base fixtures for database integration tests

## License

MIT License - see LICENSE file for details.
