# Aspire vs Testcontainers Migration Guide

> **Related Documentation:**
>
> - [ADR-008: Aspire vs Testcontainers Testing Strategy](../architecture/adr/008-aspire-vs-testcontainers-testing-strategy.md)
> - [Integration Testing with Docker](./integration-tests.md)
> - [POC Results](../../tests/Encina.Aspire.POC.Tests/PERFORMANCE-RESULTS.md)

---

## Overview

This guide helps developers choose the right integration testing approach and provides side-by-side comparisons for migrating between Testcontainers and Aspire.Hosting.Testing when appropriate.

### Decision Flowchart

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    Which Testing Approach Should I Use?                  │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
                    ┌───────────────────────────────┐
                    │ Do you need AppHost           │
                    │ orchestration (multiple       │
                    │ services, dependencies)?      │
                    └───────────────────────────────┘
                           │               │
                          YES              NO
                           │               │
                           ▼               ▼
        ┌──────────────────────┐   ┌──────────────────────┐
        │ Is this testing a    │   │ Do you need Oracle,  │
        │ full Aspire AppHost  │   │ NATS, or MQTT?       │
        │ configuration?       │   └──────────────────────┘
        └──────────────────────┘          │        │
              │           │              YES       NO
             YES          NO              │        │
              │           │               ▼        ▼
              ▼           │    ┌────────────┐ ┌────────────────────┐
    ┌─────────────────┐   │    │TESTCONTAIN-│ │ Is it a component- │
    │ USE ASPIRE      │   │    │ERS (only   │ │ level DB/service   │
    │ HOSTING.TESTING │   │    │ option)    │ │ test?              │
    └─────────────────┘   │    └────────────┘ └────────────────────┘
                          │                          │        │
                          │                         YES       NO
                          │                          │        │
                          │                          ▼        ▼
                          │              ┌────────────┐ ┌────────────┐
                          │              │TESTCONTAIN-│ │ USE ASPIRE │
                          │              │ERS         │ │ HOSTING    │
                          │              │(DatabaseFix│ │ .TESTING   │
                          │              │ture<T>)   │ └────────────┘
                          │              └────────────┘
                          │
                          ▼
        ┌──────────────────────────────────────────────┐
        │ Consider Testcontainers for fine-grained     │
        │ control, or Aspire if testing multi-service  │
        │ communication (evaluate case by case)        │
        └──────────────────────────────────────────────┘
```

### Quick Reference Table

| Scenario | Recommended Approach | Reason |
|----------|---------------------|--------|
| Single database test | Testcontainers | Simpler, lower overhead |
| Oracle database | Testcontainers | Not supported in Aspire |
| NATS/MQTT messaging | Testcontainers | Limited Aspire support |
| Full AppHost validation | Aspire | Production parity |
| Multi-service orchestration | Aspire | Built-in dependency management |
| HTTP endpoint testing | WebApplicationFactory | Not related to container orchestration |
| Component-level store tests | Testcontainers | Existing fixtures, proven patterns |

---

## Provider-Specific Comparisons

### SQL Server

#### Testcontainers Pattern (Current)

```csharp
using Testcontainers.MsSql;

public sealed class SqlServerFixture : DatabaseFixture<MsSqlContainer>
{
    private MsSqlContainer? _container;

    public override string ConnectionString => _container?.GetConnectionString() ?? string.Empty;

    protected override async Task<MsSqlContainer> CreateContainerAsync()
    {
        _container = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPassword("YourStrong!Passw0rd")
            .WithCleanUp(true)
            .Build();

        return _container;
    }

    public override IDbConnection CreateConnection()
    {
        var connection = new SqlConnection(ConnectionString);
        connection.Open();
        return connection;
    }
}
```

#### Aspire Pattern (Alternative)

```csharp
// AppHost definition
public sealed class SqlServerAppHost
{
    public const string DatabaseName = "encina_test";

    public static void Main(string[] args)
    {
        var builder = DistributedApplication.CreateBuilder(args);
        var sqlServer = builder.AddSqlServer("sqlserver")
            .WithImage("mcr.microsoft.com/mssql/server")
            .WithImageTag("2022-latest");
        sqlServer.AddDatabase(DatabaseName);
        builder.Build().Run();
    }
}

// Test class
public sealed class SqlServerAspireTests : IAsyncLifetime
{
    private DistributedApplication? _app;
    private string? _connectionString;

    public async Task InitializeAsync()
    {
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<SqlServerAppHost>();

        _app = await builder.BuildAsync();
        await _app.StartAsync();

        await _app.ResourceNotifications.WaitForResourceAsync(
            SqlServerAppHost.DatabaseName,
            KnownResourceStates.Running);

        _connectionString = await _app.GetConnectionStringAsync(
            SqlServerAppHost.DatabaseName);
    }

    public async Task DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }
}
```

#### Key Differences

| Aspect | Testcontainers | Aspire |
|--------|---------------|--------|
| Connection String | `container.GetConnectionString()` | `app.GetConnectionStringAsync(name)` |
| Container Config | `MsSqlBuilder().WithPassword()` | `AddSqlServer().WithImage()` |
| Lifecycle | Direct `StartAsync()/StopAsync()` | AppHost orchestration |
| Wait Strategy | Built into `MsSqlBuilder` | `WaitForResourceAsync()` |

---

### PostgreSQL

#### Testcontainers Pattern (Current)

```csharp
using Testcontainers.PostgreSql;

public sealed class PostgreSqlFixture : DatabaseFixture<PostgreSqlContainer>
{
    private PostgreSqlContainer? _container;

    public override string ConnectionString => _container?.GetConnectionString() ?? string.Empty;

    protected override async Task<PostgreSqlContainer> CreateContainerAsync()
    {
        // Note: Use constructor with image parameter (parameterless is deprecated)
        _container = new PostgreSqlBuilder("postgres:17-alpine")
            .WithDatabase("encina_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithCleanUp(true)
            .Build();

        return _container;
    }

    public override IDbConnection CreateConnection()
    {
        var connection = new NpgsqlConnection(ConnectionString);
        connection.Open();
        return connection;
    }
}
```

#### Aspire Pattern (Alternative)

```csharp
// AppHost definition
public sealed class PostgresAppHost
{
    public const string DatabaseName = "encina_test";

    public static void Main(string[] args)
    {
        var builder = DistributedApplication.CreateBuilder(args);
        var postgres = builder.AddPostgres("postgres")
            .WithImage("postgres")
            .WithImageTag("17-alpine");
        postgres.AddDatabase(DatabaseName);
        builder.Build().Run();
    }
}

// Test class
public sealed class PostgresAspireTests : IAsyncLifetime
{
    private DistributedApplication? _app;
    private string? _connectionString;

    public async Task InitializeAsync()
    {
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<PostgresAppHost>();

        _app = await builder.BuildAsync();
        await _app.StartAsync();

        await _app.ResourceNotifications.WaitForResourceAsync(
            PostgresAppHost.DatabaseName,
            KnownResourceStates.Running);

        _connectionString = await _app.GetConnectionStringAsync(
            PostgresAppHost.DatabaseName);
    }

    public async Task DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }
}
```

---

### MySQL

#### Testcontainers Pattern (Current)

```csharp
using Testcontainers.MySql;

public sealed class MySqlFixture : DatabaseFixture<MySqlContainer>
{
    private MySqlContainer? _container;

    public override string ConnectionString => _container?.GetConnectionString() ?? string.Empty;

    protected override async Task<MySqlContainer> CreateContainerAsync()
    {
        _container = new MySqlBuilder()
            .WithImage("mysql:8.0")
            .WithDatabase("encina_test")
            .WithUsername("encina")
            .WithPassword("encina123")
            .WithCleanUp(true)
            .Build();

        return _container;
    }

    public override IDbConnection CreateConnection()
    {
        var connection = new MySqlConnection(ConnectionString);
        connection.Open();
        return connection;
    }
}
```

#### Aspire Pattern (Alternative)

```csharp
// AppHost definition
public sealed class MySqlAppHost
{
    public const string DatabaseName = "encina_test";

    public static void Main(string[] args)
    {
        var builder = DistributedApplication.CreateBuilder(args);
        var mysql = builder.AddMySql("mysql")
            .WithImage("mysql")
            .WithImageTag("8.0");
        mysql.AddDatabase(DatabaseName);
        builder.Build().Run();
    }
}
```

---

### Redis

#### Testcontainers Pattern (Current)

```csharp
using Testcontainers.Redis;

public sealed class RedisFixture : IAsyncLifetime
{
    private RedisContainer? _container;

    public string ConnectionString => _container?.GetConnectionString() ?? string.Empty;

    public async Task InitializeAsync()
    {
        _container = new RedisBuilder()
            .WithImage("redis:7-alpine")
            .WithCleanUp(true)
            .Build();

        await _container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.StopAsync();
            await _container.DisposeAsync();
        }
    }
}
```

#### Aspire Pattern (Alternative)

```csharp
// AppHost definition
public sealed class RedisAppHost
{
    public const string RedisName = "redis";

    public static void Main(string[] args)
    {
        var builder = DistributedApplication.CreateBuilder(args);
        builder.AddRedis(RedisName)
            .WithImage("redis")
            .WithImageTag("7-alpine");
        builder.Build().Run();
    }
}

// Test class
public sealed class RedisAspireTests : IAsyncLifetime
{
    private DistributedApplication? _app;
    private string? _connectionString;

    public async Task InitializeAsync()
    {
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<RedisAppHost>();

        _app = await builder.BuildAsync();
        await _app.StartAsync();

        await _app.ResourceNotifications.WaitForResourceAsync(
            RedisAppHost.RedisName,
            KnownResourceStates.Running);

        _connectionString = await _app.GetConnectionStringAsync(
            RedisAppHost.RedisName);
    }

    public async Task DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }
}
```

---

### RabbitMQ

#### Testcontainers Pattern (Current)

```csharp
using Testcontainers.RabbitMq;

public sealed class RabbitMqFixture : IAsyncLifetime
{
    private RabbitMqContainer? _container;

    public string ConnectionString => _container?.GetConnectionString() ?? string.Empty;

    public async Task InitializeAsync()
    {
        _container = new RabbitMqBuilder()
            .WithImage("rabbitmq:3-management-alpine")
            .WithUsername("guest")
            .WithPassword("guest")
            .WithCleanUp(true)
            .Build();

        await _container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.StopAsync();
            await _container.DisposeAsync();
        }
    }
}
```

#### Aspire Pattern (Alternative)

```csharp
// AppHost definition
public sealed class RabbitMqAppHost
{
    public const string RabbitMqName = "rabbitmq";

    public static void Main(string[] args)
    {
        var builder = DistributedApplication.CreateBuilder(args);
        builder.AddRabbitMQ(RabbitMqName)
            .WithImage("rabbitmq")
            .WithImageTag("3-management-alpine")
            .WithManagementPlugin();
        builder.Build().Run();
    }
}
```

---

### Kafka

#### Testcontainers Pattern (Current)

```csharp
using Testcontainers.Kafka;

public sealed class KafkaFixture : IAsyncLifetime
{
    private KafkaContainer? _container;

    public string BootstrapServers => _container?.GetBootstrapAddress() ?? string.Empty;

    public async Task InitializeAsync()
    {
        _container = new KafkaBuilder()
            .WithImage("confluentinc/cp-kafka:7.5.0")
            .WithCleanUp(true)
            .Build();

        await _container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.StopAsync();
            await _container.DisposeAsync();
        }
    }
}
```

#### Aspire Pattern (Alternative)

```csharp
// AppHost definition
public sealed class KafkaAppHost
{
    public const string KafkaName = "kafka";

    public static void Main(string[] args)
    {
        var builder = DistributedApplication.CreateBuilder(args);
        builder.AddKafka(KafkaName)
            .WithImage("confluentinc/cp-kafka")
            .WithImageTag("7.5.0");
        builder.Build().Run();
    }
}
```

---

## Testcontainers-Only Providers

The following providers are **NOT supported by Aspire.Hosting** and must use Testcontainers:

### Oracle Database

```csharp
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

public sealed class OracleFixture : DatabaseFixture<IContainer>
{
    private IContainer? _container;
    private string _connectionString = string.Empty;

    public override string ConnectionString => _connectionString;

    protected override async Task<IContainer> CreateContainerAsync()
    {
        // Uses GenericContainer since there's no official Oracle module
        _container = new ContainerBuilder()
            .WithImage("gvenzl/oracle-free:23-slim-faststart")
            .WithPortBinding(1521, true)
            .WithEnvironment("ORACLE_PASSWORD", "OraclePwd123")
            .WithEnvironment("APP_USER", "encina")
            .WithEnvironment("APP_USER_PASSWORD", "SimplePwd123")
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilMessageIsLogged("DATABASE IS READY TO USE!"))
            .WithCleanUp(true)
            .Build();

        await _container.StartAsync();

        var port = _container.GetMappedPublicPort(1521);
        _connectionString = $"Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)" +
            $"(HOST=localhost)(PORT={port}))(CONNECT_DATA=(SERVICE_NAME=FREEPDB1)));" +
            $"User Id=encina;Password=SimplePwd123;";

        return _container;
    }
}
```

### NATS

```csharp
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

public sealed class NatsFixture : IAsyncLifetime
{
    private IContainer? _container;

    public string ConnectionUrl { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        _container = new ContainerBuilder()
            .WithImage("nats:2-alpine")
            .WithPortBinding(4222, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(4222))
            .WithCleanUp(true)
            .Build();

        await _container.StartAsync();

        var port = _container.GetMappedPublicPort(4222);
        ConnectionUrl = $"nats://localhost:{port}";
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.StopAsync();
            await _container.DisposeAsync();
        }
    }
}
```

### MQTT

```csharp
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

public sealed class MqttFixture : IAsyncLifetime
{
    private IContainer? _container;

    public string BrokerAddress { get; private set; } = string.Empty;
    public int Port { get; private set; }

    public async Task InitializeAsync()
    {
        _container = new ContainerBuilder()
            .WithImage("eclipse-mosquitto:2")
            .WithPortBinding(1883, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(1883))
            .WithCleanUp(true)
            .Build();

        await _container.StartAsync();

        Port = _container.GetMappedPublicPort(1883);
        BrokerAddress = "localhost";
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.StopAsync();
            await _container.DisposeAsync();
        }
    }
}
```

---

## WebApplicationFactory Integration

### Understanding the Tools

| Tool | Purpose | Use Case |
|------|---------|----------|
| **WebApplicationFactory** | In-memory HTTP server | Testing single ASP.NET Core API |
| **Aspire.Hosting.Testing** | Distributed app orchestration | Testing multi-service architectures |
| **Testcontainers** | Container lifecycle management | External dependencies (DB, cache, MQ) |

### When to Use Each

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    Testing ASP.NET Core Applications                    │
└─────────────────────────────────────────────────────────────────────────┘

┌───────────────────────┐  ┌───────────────────────┐  ┌───────────────────┐
│  Single API Endpoint  │  │  Multi-Service        │  │  Component with   │
│  Testing              │  │  Orchestration        │  │  External Deps    │
├───────────────────────┤  ├───────────────────────┤  ├───────────────────┤
│                       │  │                       │  │                   │
│  WebApplicationFactory│  │  Aspire.Hosting       │  │  Testcontainers   │
│                       │  │  .Testing             │  │                   │
│  - HttpClient access  │  │  - AppHost validation │  │  - DatabaseFixture│
│  - DI overrides       │  │  - Service discovery  │  │  - Schema setup   │
│  - In-memory server   │  │  - Cross-service calls│  │  - Connection mgmt│
│                       │  │                       │  │                   │
└───────────────────────┘  └───────────────────────┘  └───────────────────┘
```

### Combining Approaches

**WebApplicationFactory + Testcontainers (Common Pattern):**

```csharp
public class ApiIntegrationTests : IClassFixture<PostgreSqlFixture>
{
    private readonly PostgreSqlFixture _dbFixture;
    private readonly WebApplicationFactory<Program> _factory;

    public ApiIntegrationTests(PostgreSqlFixture dbFixture)
    {
        _dbFixture = dbFixture;
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Override connection string with Testcontainers
                    services.Configure<DatabaseOptions>(options =>
                    {
                        options.ConnectionString = _dbFixture.ConnectionString;
                    });
                });
            });
    }

    [Fact]
    public async Task GetUsers_ReturnsOk()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/users");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
```

**Aspire + WebApplicationFactory (Distributed Scenario):**

```csharp
public class DistributedApiTests : IAsyncLifetime
{
    private DistributedApplication? _app;

    public async Task InitializeAsync()
    {
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<MyAppHost>();

        _app = await builder.BuildAsync();
        await _app.StartAsync();

        // Wait for all services
        await _app.ResourceNotifications.WaitForResourceAsync(
            "api", KnownResourceStates.Running);
        await _app.ResourceNotifications.WaitForResourceAsync(
            "worker", KnownResourceStates.Running);
    }

    [Fact]
    public async Task CrossServiceCommunication_Works()
    {
        // Aspire provides HttpClient for services
        var apiClient = _app!.CreateHttpClient("api");
        var response = await apiClient.PostAsync("/api/orders",
            JsonContent.Create(new { Item = "Test" }));

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        // Verify worker processed the message
        // (worker consumes from message queue)
    }

    public async Task DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }
}
```

---

## Connection String Retrieval Comparison

| Approach | Pattern | Async | Notes |
|----------|---------|-------|-------|
| **Testcontainers** | `container.GetConnectionString()` | No | Synchronous, direct access |
| **Aspire** | `app.GetConnectionStringAsync(name)` | Yes | Requires resource name |
| **Testcontainers (port)** | `container.GetMappedPublicPort(port)` | No | For building custom strings |
| **Aspire (explicit)** | `resource.ConnectionStringExpression.GetValueAsync()` | Yes | Via resource reference |

---

## Lifecycle Management Comparison

### Testcontainers Lifecycle

```csharp
public class TestcontainersLifecycle : IAsyncLifetime
{
    private PostgreSqlContainer? _container;

    public async Task InitializeAsync()
    {
        // 1. Build container
        _container = new PostgreSqlBuilder("postgres:17-alpine")
            .WithDatabase("test")
            .Build();

        // 2. Start container (pulls image if needed)
        await _container.StartAsync();

        // 3. Container is ready - connection string available
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            // 4. Stop container
            await _container.StopAsync();

            // 5. Dispose resources
            await _container.DisposeAsync();
        }
    }
}
```

### Aspire Lifecycle

```csharp
public class AspireLifecycle : IAsyncLifetime
{
    private DistributedApplication? _app;

    public async Task InitializeAsync()
    {
        // 1. Create builder (reads AppHost configuration)
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<MyAppHost>();

        // 2. Build the application
        _app = await builder.BuildAsync();

        // 3. Start all resources (orchestrated)
        await _app.StartAsync();

        // 4. Wait for specific resources
        await _app.ResourceNotifications.WaitForResourceAsync(
            "database", KnownResourceStates.Running);
    }

    public async Task DisposeAsync()
    {
        if (_app is not null)
        {
            // 5. Stop all resources (orchestrated)
            await _app.StopAsync();

            // 6. Dispose application
            await _app.DisposeAsync();
        }
    }
}
```

---

## Migration Checklist

When considering migration from Testcontainers to Aspire:

### Pre-Migration Assessment

- [ ] Identify all providers used (check for Oracle/NATS/MQTT blockers)
- [ ] Count affected test files and fixtures
- [ ] Evaluate test complexity (single vs multi-service)
- [ ] Review custom container configurations
- [ ] Check for programmatic container control requirements

### Migration Steps (If Applicable)

1. [ ] Create AppHost class with resource definitions
2. [ ] Update test class to use `DistributedApplicationTestingBuilder`
3. [ ] Replace `GetConnectionString()` with `GetConnectionStringAsync()`
4. [ ] Add `WaitForResourceAsync()` calls for resource readiness
5. [ ] Update lifecycle management (`StartAsync`/`StopAsync`)
6. [ ] Run tests to verify behavior parity
7. [ ] Update documentation and patterns

### Post-Migration Validation

- [ ] All tests pass
- [ ] No performance regression (benchmark if critical)
- [ ] Connection strings resolve correctly
- [ ] Container cleanup works properly
- [ ] CI/CD pipelines updated if needed

---

## References

- [ADR-008: Aspire vs Testcontainers Testing Strategy](../architecture/adr/008-aspire-vs-testcontainers-testing-strategy.md)
- [Aspire Testing Documentation](https://aspire.dev/testing/write-your-first-test/)
- [Testcontainers .NET](https://dotnet.testcontainers.org/)
- [WebApplicationFactory](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests)
- [POC Implementation](../../tests/Encina.Aspire.POC.Tests/)
