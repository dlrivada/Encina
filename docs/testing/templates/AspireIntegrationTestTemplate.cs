// =============================================================================
// ASPIRE INTEGRATION TEST TEMPLATE
// =============================================================================
// Use this template when creating new distributed application integration tests
// with Aspire.Hosting.Testing.
//
// Prerequisites:
//   - Package: Aspire.Hosting.Testing
//   - Package: Aspire.Hosting.{Provider} (e.g., Aspire.Hosting.PostgreSQL)
//   - Docker running
//
// When to use this template:
//   - Testing full AppHost configurations
//   - Multi-service orchestration scenarios
//   - Cross-service communication tests
//   - Production parity validation
//
// When NOT to use (use Testcontainers instead):
//   - Single component/database tests
//   - Oracle, NATS, or MQTT providers
//   - Fine-grained container control needed
//
// See: docs/testing/aspire-migration-guide.md
// See: docs/architecture/adr/008-aspire-vs-testcontainers-testing-strategy.md
// =============================================================================

using System.Diagnostics;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Xunit;

namespace YourNamespace.IntegrationTests;

// =============================================================================
// STEP 1: Define your AppHost entry point
// =============================================================================
// This class configures the distributed application resources.
// It mirrors what you would have in a production AppHost project.

/// <summary>
/// Test AppHost that configures resources for integration testing.
/// </summary>
/// <remarks>
/// <para>
/// This class serves as the entry point for Aspire.Hosting.Testing.
/// Configure all resources your tests need here.
/// </para>
/// <para>
/// Resource names defined here are used to retrieve connection strings
/// and wait for resource readiness in tests.
/// </para>
/// </remarks>
public sealed class TestAppHost
{
    // Define resource names as constants for type safety
    public const string PostgresServerName = "postgres";
    public const string PostgresDatabaseName = "test_db";
    public const string RedisName = "redis";

    /// <summary>
    /// Main entry point that configures the distributed application.
    /// </summary>
    /// <param name="args">Command line arguments (passed by Aspire).</param>
    public static void Main(string[] args)
    {
        var builder = DistributedApplication.CreateBuilder(args);

        // Add PostgreSQL with a named database
        var postgres = builder.AddPostgres(PostgresServerName)
            .WithImage("postgres")
            .WithImageTag("17-alpine");
        postgres.AddDatabase(PostgresDatabaseName);

        // Add Redis (optional - remove if not needed)
        builder.AddRedis(RedisName)
            .WithImage("redis")
            .WithImageTag("7-alpine");

        // Add more resources as needed:
        // builder.AddSqlServer("sqlserver");
        // builder.AddMySql("mysql");
        // builder.AddRabbitMQ("rabbitmq");
        // builder.AddKafka("kafka");

        builder.Build().Run();
    }
}

// =============================================================================
// STEP 2: Create your test class with IAsyncLifetime
// =============================================================================

/// <summary>
/// Integration tests using Aspire.Hosting.Testing for distributed application testing.
/// </summary>
/// <remarks>
/// <para>
/// This class demonstrates the Aspire testing pattern:
/// <list type="bullet">
/// <item><description>Creates the AppHost via DistributedApplicationTestingBuilder</description></item>
/// <item><description>Manages application lifecycle with IAsyncLifetime</description></item>
/// <item><description>Retrieves connection strings via app.GetConnectionStringAsync()</description></item>
/// <item><description>Waits for resources with app.ResourceNotifications</description></item>
/// </list>
/// </para>
/// </remarks>
[Trait("Category", "Integration")]
[Trait("Provider", "Aspire")]
public sealed class AspireIntegrationTests : IAsyncLifetime
{
    // =============================================================================
    // Configuration
    // =============================================================================

    /// <summary>
    /// Maximum time to wait for resources to start.
    /// Increase for slow-starting resources like Oracle.
    /// </summary>
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromSeconds(60);

    // =============================================================================
    // State
    // =============================================================================

    private DistributedApplication? _app;
    private string? _postgresConnectionString;
    private string? _redisConnectionString;

    /// <summary>
    /// Gets the time taken to initialize all resources.
    /// Useful for performance monitoring.
    /// </summary>
    public TimeSpan InitializationTime { get; private set; }

    // =============================================================================
    // Connection String Properties
    // =============================================================================

    /// <summary>
    /// Gets the PostgreSQL connection string.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if accessed before initialization.</exception>
    public string PostgresConnectionString =>
        _postgresConnectionString ?? throw new InvalidOperationException("Not initialized");

    /// <summary>
    /// Gets the Redis connection string.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if accessed before initialization.</exception>
    public string RedisConnectionString =>
        _redisConnectionString ?? throw new InvalidOperationException("Not initialized");

    // =============================================================================
    // IAsyncLifetime Implementation
    // =============================================================================

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        var stopwatch = Stopwatch.StartNew();

        // Step 1: Create the distributed application testing builder
        // This uses the TestAppHost class as the entry point
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<TestAppHost>();

        // Step 2: Build and start the application
        // This orchestrates all resource containers
        _app = await builder.BuildAsync();
        await _app.StartAsync().WaitAsync(StartupTimeout);

        // Step 3: Wait for resources to be ready
        // Use KnownResourceStates.Running for container resources
        await _app.ResourceNotifications
            .WaitForResourceAsync(TestAppHost.PostgresDatabaseName, KnownResourceStates.Running)
            .WaitAsync(StartupTimeout);

        await _app.ResourceNotifications
            .WaitForResourceAsync(TestAppHost.RedisName, KnownResourceStates.Running)
            .WaitAsync(StartupTimeout);

        // Step 4: Retrieve connection strings
        // These are available after resources are running
        _postgresConnectionString = await _app.GetConnectionStringAsync(TestAppHost.PostgresDatabaseName);
        _redisConnectionString = await _app.GetConnectionStringAsync(TestAppHost.RedisName);

        stopwatch.Stop();
        InitializationTime = stopwatch.Elapsed;
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        if (_app is not null)
        {
            // Stop all resources gracefully
            await _app.StopAsync();

            // Dispose the application
            await _app.DisposeAsync();
        }
    }

    // =============================================================================
    // Example Tests
    // =============================================================================

    [Fact]
    public async Task Database_CanConnect()
    {
        // Arrange
        await using var connection = new Npgsql.NpgsqlConnection(PostgresConnectionString);

        // Act
        await connection.OpenAsync();

        // Assert
        connection.State.ShouldBe(System.Data.ConnectionState.Open);
    }

    [Fact]
    public async Task Database_CanExecuteQuery()
    {
        // Arrange
        await using var connection = new Npgsql.NpgsqlConnection(PostgresConnectionString);
        await connection.OpenAsync();

        // Act
        const string sql = "SELECT 1 AS result";
        var result = await connection.ExecuteScalarAsync<int>(sql);

        // Assert
        result.ShouldBe(1);
    }

    [Fact]
    public void InitializationTime_ShouldBeReasonable()
    {
        // Assert that startup completed within timeout
        InitializationTime.ShouldBeLessThan(StartupTimeout);

        // Log for performance tracking
        // Consider: output.WriteLine($"Initialization took {InitializationTime.TotalSeconds:F2}s");
    }

    // =============================================================================
    // Integration with Encina.Testing
    // =============================================================================
    // If using Encina.Testing packages, you can combine patterns:

    /*
    [Fact]
    public async Task WithEncinaTestFixture()
    {
        // Use Encina.Testing.Bogus for test data
        var faker = new EncinaFaker<TestEntity>()
            .RuleFor(x => x.Name, f => f.Name.FullName());

        var entity = faker.Generate();

        // Use Encina.Testing.Shouldly for assertions
        entity.Name.ShouldNotBeNullOrEmpty();

        // Use connection from Aspire
        await using var connection = new NpgsqlConnection(PostgresConnectionString);
        await connection.OpenAsync();

        // Execute with Dapper
        var id = await connection.ExecuteScalarAsync<int>(
            "INSERT INTO entities (name) VALUES (@Name) RETURNING id",
            new { entity.Name });

        id.ShouldBeGreaterThan(0);
    }
    */
}

// =============================================================================
// STEP 3: Optional - Create a shared fixture for test collections
// =============================================================================
// Use this pattern when multiple test classes share the same Aspire application.

/// <summary>
/// Shared fixture for Aspire integration tests.
/// Use with [CollectionDefinition] and [Collection] attributes.
/// </summary>
/// <example>
/// <code>
/// [CollectionDefinition("Aspire")]
/// public class AspireCollection : ICollectionFixture&lt;AspireSharedFixture&gt; { }
///
/// [Collection("Aspire")]
/// public class MyTests
/// {
///     private readonly AspireSharedFixture _fixture;
///     public MyTests(AspireSharedFixture fixture) => _fixture = fixture;
/// }
/// </code>
/// </example>
public sealed class AspireSharedFixture : IAsyncLifetime
{
    private DistributedApplication? _app;

    public string PostgresConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<TestAppHost>();
        _app = await builder.BuildAsync();
        await _app.StartAsync();

        await _app.ResourceNotifications
            .WaitForResourceAsync(TestAppHost.PostgresDatabaseName, KnownResourceStates.Running)
            .WaitAsync(TimeSpan.FromSeconds(60));

        PostgresConnectionString = await _app.GetConnectionStringAsync(TestAppHost.PostgresDatabaseName)
            ?? throw new InvalidOperationException("Connection string is null");
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
