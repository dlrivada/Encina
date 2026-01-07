# Performance Results: Aspire.Hosting.Testing vs Testcontainers

## Overview

This document captures the performance comparison between two approaches for
PostgreSQL integration testing:

1. **Testcontainers**: Direct container management using `Testcontainers.PostgreSql`
2. **Aspire.Hosting.Testing**: Distributed application orchestration using `.NET Aspire`

## Test Environment

| Component | Version |
|-----------|---------|
| .NET | 10.0 |
| Aspire.Hosting.Testing | 13.1.0 |
| Testcontainers.PostgreSql | 4.10.0 |
| PostgreSQL Image | postgres:17-alpine |
| Docker | (system version) |

## Benchmark Results

> **Note**: Run benchmarks to populate actual results:
>
> ```bash
> dotnet run -c Release --project tests/Encina.Aspire.POC.Tests -- --filter "*ContainerStartup*"
> ```

### Container Startup Time

| Approach | Mean | StdDev | Allocated |
|----------|------|--------|-----------|
| Testcontainers: Startup | TBD | TBD | TBD |
| Aspire: Startup | TBD | TBD | TBD |

### Startup + CRUD Operations

| Approach | Mean | StdDev | Allocated |
|----------|------|--------|-----------|
| Testcontainers: Startup + CRUD | TBD | TBD | TBD |
| Aspire: Startup + CRUD | TBD | TBD | TBD |

## Analysis

### Expected Performance Characteristics

#### Testcontainers Advantages

- **Lower overhead**: Direct Docker API interaction without orchestration layer
- **Faster startup**: Single container initialization without AppHost scaffolding
- **Less memory**: No distributed application infrastructure
- **Simpler**: Direct programmatic control

#### Aspire Advantages

- **Resource management**: Automatic dependency tracking and lifecycle
- **Multi-container orchestration**: Better for distributed system testing
- **Consistent configuration**: Same model as production Aspire apps
- **Observability**: Built-in resource monitoring and notifications

### Performance Expectations

Based on architectural differences, we expect:

1. **Testcontainers will be faster** for single-container scenarios (~30-50% less overhead)
2. **Aspire adds orchestration overhead** but provides better resource management
3. **Memory usage** will be higher with Aspire due to DI container and resource tracking
4. **The gap narrows** as test complexity increases (more containers, dependencies)

## Recommendations

### When to Use Testcontainers

- Component-level integration tests
- Single database/service testing
- Performance-critical test suites
- Oracle database testing (not supported by Aspire)
- Fine-grained container configuration needs

### When to Use Aspire.Hosting.Testing

- End-to-end distributed application testing
- Multi-service orchestration scenarios
- Testing Aspire AppHost configurations
- Cross-service communication testing
- Production parity validation

## Code Comparison

### Testcontainers Pattern

```csharp
public class PostgreSqlTestcontainersTests : IAsyncLifetime
{
    private PostgreSqlContainer? _container;

    public async Task InitializeAsync()
    {
        // Note: Use constructor with image parameter (parameterless is deprecated)
        _container = new PostgreSqlBuilder("postgres:17-alpine")
            .WithDatabase("test_db")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithCleanUp(true)
            .Build();

        await _container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.StopAsync();
        await _container.DisposeAsync();
    }

    [Fact]
    public async Task MyTest()
    {
        var connectionString = _container.GetConnectionString();
        // Use connection string directly
    }
}
```

### Aspire Pattern

```csharp
// First, define an AppHost entry point class:
public sealed class TestAppHost
{
    public const string PostgresDatabaseName = "test_db";

    public static void Main(string[] args)
    {
        var builder = DistributedApplication.CreateBuilder(args);
        var postgres = builder.AddPostgres("postgres")
            .WithImage("postgres")
            .WithImageTag("17-alpine");
        postgres.AddDatabase(PostgresDatabaseName);
        builder.Build().Run();
    }
}

// Then use it in tests:
public class PostgreSqlAspireTests : IAsyncLifetime
{
    private DistributedApplication? _app;
    private string? _connectionString;

    public async Task InitializeAsync()
    {
        // Create builder with the AppHost entry point
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<TestAppHost>();

        _app = await builder.BuildAsync();
        await _app.StartAsync();

        // Wait for resource using ResourceNotifications property
        await _app.ResourceNotifications.WaitForResourceAsync(
            TestAppHost.PostgresDatabaseName,
            KnownResourceStates.Running);

        // Get connection string using the app's helper method
        _connectionString = await _app.GetConnectionStringAsync(
            TestAppHost.PostgresDatabaseName);
    }

    public async Task DisposeAsync()
    {
        await _app.StopAsync();
        await _app.DisposeAsync();
    }

    [Fact]
    public async Task MyTest()
    {
        // Use _connectionString directly
    }
}
```

## Conclusion

Both approaches are valid for different use cases:

- **Testcontainers**: Recommended for **component-level integration tests** where
  performance and simplicity are priorities.

- **Aspire.Hosting.Testing**: Recommended for **distributed application scenarios**
  where multi-service orchestration and production parity matter more than raw
  startup performance.

The Encina project's existing Testcontainers fixture abstraction is well-suited for
its component-level testing needs. Aspire should be considered for future scenarios
involving distributed application testing.

## References

- [Aspire Testing Documentation](https://aspire.dev/testing/write-your-first-test/)
- [Testcontainers .NET](https://dotnet.testcontainers.org/)
- [Aspire vs Testcontainers Analysis](https://endjin.com/blog/2025/06/dotnet-aspire-db-testing-integration-tests)
