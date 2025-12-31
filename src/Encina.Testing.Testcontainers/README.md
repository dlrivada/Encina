# Encina.Testing.Testcontainers

Pre-configured xUnit fixtures for spinning up real database instances in Docker during integration tests.

## Installation

```bash
dotnet add package Encina.Testing.Testcontainers
```

## Quick Start

Use the fixtures with xUnit's `IClassFixture<T>` pattern:

```csharp
public class OrderRepositoryTests : IClassFixture<SqlServerContainerFixture>
{
    private readonly SqlServerContainerFixture _db;

    public OrderRepositoryTests(SqlServerContainerFixture db) => _db = db;

    [Fact]
    public async Task CreateOrder_ShouldPersist()
    {
        await using var connection = new SqlConnection(_db.ConnectionString);
        await connection.OpenAsync();

        // Test with real SQL Server...
    }
}
```

## Available Fixtures

| Fixture | Database | Default Image |
|---------|----------|---------------|
| `SqlServerContainerFixture` | SQL Server | `mcr.microsoft.com/mssql/server:2022-latest` |
| `PostgreSqlContainerFixture` | PostgreSQL | `postgres:17-alpine` |
| `MySqlContainerFixture` | MySQL | `mysql:9.1` |
| `MongoDbContainerFixture` | MongoDB | `mongo:7` |
| `RedisContainerFixture` | Redis | `redis:7-alpine` |

## Factory Methods

Use `EncinaContainers` factory for programmatic creation:

```csharp
// With default configuration
var sqlServer = EncinaContainers.SqlServer();
await sqlServer.InitializeAsync();

// With custom configuration
var postgres = EncinaContainers.PostgreSql(builder => builder
    .WithImage("postgres:16-alpine")
    .WithDatabase("custom_db"));
await postgres.InitializeAsync();
```

## Sharing Containers Across Tests

### Class-level sharing (IClassFixture)

Container is shared across all tests in a class:

```csharp
public class OrderTests : IClassFixture<PostgreSqlContainerFixture>
{
    private readonly PostgreSqlContainerFixture _db;

    public OrderTests(PostgreSqlContainerFixture db) => _db = db;

    // All tests share the same container
}
```

### Collection-level sharing (ICollectionFixture)

Container is shared across multiple test classes:

```csharp
[CollectionDefinition("Database")]
public class DatabaseCollection : ICollectionFixture<SqlServerContainerFixture>
{
}

[Collection("Database")]
public class OrderTests
{
    private readonly SqlServerContainerFixture _db;

    public OrderTests(SqlServerContainerFixture db) => _db = db;
}

[Collection("Database")]
public class CustomerTests
{
    private readonly SqlServerContainerFixture _db;

    public CustomerTests(SqlServerContainerFixture db) => _db = db;
}
```

## Integration with Encina.Testing.Respawn

This package includes built-in integration with `Encina.Testing.Respawn` for automatic database cleanup between tests.

### Using DatabaseIntegrationTestBase (Recommended)

The simplest way to combine Testcontainers with Respawn is to inherit from one of the provided base classes:

```csharp
public class OrderRepositoryTests : SqlServerIntegrationTestBase
{
    public OrderRepositoryTests(SqlServerContainerFixture fixture) : base(fixture) { }

    [Fact]
    public async Task CreateOrder_ShouldPersist()
    {
        // Database is automatically reset before each test
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();

        // Insert test data
        await connection.ExecuteAsync("INSERT INTO Orders (Id, Name) VALUES (1, 'Test')");

        // Verify
        var count = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Orders");
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task GetOrder_ShouldReturnNull_WhenNotExists()
    {
        // This test also starts with a clean database
        // Previous test's data is automatically cleaned up
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();

        var result = await connection.QuerySingleOrDefaultAsync<dynamic>(
            "SELECT * FROM Orders WHERE Id = 1");
        Assert.Null(result);
    }
}
```

### Available Base Classes

| Base Class | Database |
|------------|----------|
| `SqlServerIntegrationTestBase` | SQL Server |
| `PostgreSqlIntegrationTestBase` | PostgreSQL |
| `MySqlIntegrationTestBase` | MySQL |
| `DatabaseIntegrationTestBase<TFixture>` | Generic (any supported database) |

### Customizing Respawn Options

Override the `RespawnOptions` property to customize cleanup behavior:

```csharp
public class OrderTests : SqlServerIntegrationTestBase
{
    public OrderTests(SqlServerContainerFixture fixture) : base(fixture) { }

    protected override RespawnOptions RespawnOptions => new()
    {
        TablesToIgnore = ["__EFMigrationsHistory", "SeedData"],
        ResetEncinaMessagingTables = true, // Include Encina tables in cleanup
        SchemasToExclude = ["sys", "INFORMATION_SCHEMA"]
    };
}
```

### Manual Respawn Integration

For more control, you can manually integrate Respawn with Testcontainers:

```csharp
public class OrderTests : IClassFixture<SqlServerContainerFixture>, IAsyncLifetime
{
    private readonly SqlServerContainerFixture _db;
    private DatabaseRespawner? _respawner;

    public OrderTests(SqlServerContainerFixture db) => _db = db;

    public async Task InitializeAsync()
    {
        _respawner = RespawnerFactory.Create(
            RespawnAdapter.SqlServer,
            _db.ConnectionString);
        await _respawner.InitializeAsync();
    }

    public async Task DisposeAsync()
    {
        if (_respawner is not null)
        {
            await _respawner.ResetAsync();
        }
    }

    [Fact]
    public async Task Test1()
    {
        // Database is clean before each test
    }
}
```

## Requirements

- Docker Desktop or Docker Engine running
- .NET 10.0 or later

## Container Properties

Each fixture exposes:

| Property | Description |
|----------|-------------|
| `Container` | The underlying Testcontainers container instance |
| `ConnectionString` | Connection string for the database |
| `IsRunning` | Whether the container is currently running |

## Custom Configuration

Use the factory overloads for custom container configuration:

```csharp
var sqlServer = EncinaContainers.SqlServer(builder => builder
    .WithImage("mcr.microsoft.com/mssql/server:2019-latest")
    .WithPassword("CustomP@ssw0rd!")
    .WithEnvironment("ACCEPT_EULA", "Y")
    .WithPortBinding(1433, true));
```

## CI/CD Compatibility

These fixtures are compatible with GitHub Actions and other CI environments that support Docker:

```yaml
# .github/workflows/test.yml
jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - run: dotnet test
```

## See Also

- [Encina.Testing.Respawn](../Encina.Testing.Respawn/README.md) - Database cleanup between tests
- [Testcontainers for .NET](https://testcontainers.com/guides/getting-started-with-testcontainers-for-dotnet/)
