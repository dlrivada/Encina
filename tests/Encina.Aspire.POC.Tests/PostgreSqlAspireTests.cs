using System.Diagnostics;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.Aspire.POC.Tests;

/// <summary>
/// Integration tests using Aspire.Hosting.Testing for PostgreSQL.
/// </summary>
/// <remarks>
/// <para>
/// These tests demonstrate the Aspire approach to integration testing:
/// <list type="bullet">
/// <item><description>Uses <see cref="DistributedApplicationTestingBuilder"/> to create test host</description></item>
/// <item><description>Configures resources programmatically via the builder</description></item>
/// <item><description>Gets connection string via <c>app.GetConnectionString()</c></description></item>
/// </list>
/// </para>
/// <para>
/// This class implements <see cref="IAsyncLifetime"/> to manage the Aspire
/// application lifecycle efficiently across all tests in the class.
/// </para>
/// </remarks>
[Trait("Category", "Integration")]
[Trait("Provider", "Aspire")]
public sealed class PostgreSqlAspireTests : IAsyncLifetime
{
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromSeconds(60);

    private DistributedApplication? _app;
    private string? _connectionString;

    /// <summary>
    /// Gets the time taken to initialize the container infrastructure.
    /// </summary>
    public TimeSpan InitializationTime { get; private set; }

    /// <summary>
    /// Gets the PostgreSQL connection string from the Aspire resource.
    /// </summary>
    public string ConnectionString => _connectionString ?? throw new InvalidOperationException("Not initialized");

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        var stopwatch = Stopwatch.StartNew();

        // Create the distributed application testing builder using our TestAppHost entry point
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<TestAppHost>();

        // Build and start the application
        _app = await builder.BuildAsync();
        await _app.StartAsync().WaitAsync(StartupTimeout);

        // Wait for PostgreSQL to be ready using ResourceNotifications
        await _app.ResourceNotifications
            .WaitForResourceAsync(TestAppHost.PostgresDatabaseName, KnownResourceStates.Running)
            .WaitAsync(StartupTimeout);

        // Get connection string using the recommended method
        _connectionString = await _app.GetConnectionStringAsync(TestAppHost.PostgresDatabaseName);

        stopwatch.Stop();
        InitializationTime = stopwatch.Elapsed;
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }

    #region CRUD Operations Tests

    [Fact]
    public async Task CreateTable_ShouldSucceed()
    {
        // Arrange
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();

        // Act
        const string sql = """
            CREATE TABLE IF NOT EXISTS test_entities (
                id SERIAL PRIMARY KEY,
                name VARCHAR(100) NOT NULL,
                created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
            )
            """;
        await connection.ExecuteAsync(sql);

        // Assert - verify table exists
        var tableExists = await connection.ExecuteScalarAsync<bool>("""
            SELECT EXISTS (
                SELECT FROM information_schema.tables
                WHERE table_name = 'test_entities'
            )
            """);
        tableExists.ShouldBeTrue();
    }

    [Fact]
    public async Task Insert_ShouldReturnId()
    {
        // Arrange
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await EnsureTableExistsAsync(connection);

        // Act
        var id = await connection.ExecuteScalarAsync<int>("""
            INSERT INTO test_entities (name) VALUES ('Test Entity') RETURNING id
            """);

        // Assert
        id.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task Select_ShouldReturnInsertedData()
    {
        // Arrange
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await EnsureTableExistsAsync(connection);

        var expectedName = $"Entity_{Guid.NewGuid():N}";
        await connection.ExecuteAsync(
            "INSERT INTO test_entities (name) VALUES (@Name)",
            new { Name = expectedName });

        // Act
        var result = await connection.QuerySingleAsync<TestEntity>(
            "SELECT id, name, created_at FROM test_entities WHERE name = @Name",
            new { Name = expectedName });

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe(expectedName);
        result.Id.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task Update_ShouldModifyData()
    {
        // Arrange
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await EnsureTableExistsAsync(connection);

        var id = await connection.ExecuteScalarAsync<int>("""
            INSERT INTO test_entities (name) VALUES ('Original') RETURNING id
            """);

        // Act
        var rowsAffected = await connection.ExecuteAsync(
            "UPDATE test_entities SET name = @Name WHERE id = @Id",
            new { Id = id, Name = "Updated" });

        // Assert
        rowsAffected.ShouldBe(1);
        var updated = await connection.QuerySingleAsync<string>(
            "SELECT name FROM test_entities WHERE id = @Id",
            new { Id = id });
        updated.ShouldBe("Updated");
    }

    [Fact]
    public async Task Delete_ShouldRemoveData()
    {
        // Arrange
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await EnsureTableExistsAsync(connection);

        var id = await connection.ExecuteScalarAsync<int>("""
            INSERT INTO test_entities (name) VALUES ('ToDelete') RETURNING id
            """);

        // Act
        var rowsAffected = await connection.ExecuteAsync(
            "DELETE FROM test_entities WHERE id = @Id",
            new { Id = id });

        // Assert
        rowsAffected.ShouldBe(1);
        var exists = await connection.ExecuteScalarAsync<bool>(
            "SELECT EXISTS (SELECT 1 FROM test_entities WHERE id = @Id)",
            new { Id = id });
        exists.ShouldBeFalse();
    }

    #endregion

    #region Helper Methods

    private static async Task EnsureTableExistsAsync(NpgsqlConnection connection)
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS test_entities (
                id SERIAL PRIMARY KEY,
                name VARCHAR(100) NOT NULL,
                created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
            )
            """;
        await connection.ExecuteAsync(sql);
    }

    #endregion

    #region Test Entity

    private sealed record TestEntity(int Id, string Name, DateTime CreatedAt);

    #endregion
}
