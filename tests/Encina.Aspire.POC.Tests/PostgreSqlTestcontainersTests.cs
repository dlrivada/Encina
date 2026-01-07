using System.Diagnostics;
using Testcontainers.PostgreSql;

namespace Encina.Aspire.POC.Tests;

/// <summary>
/// Integration tests using Testcontainers for PostgreSQL.
/// </summary>
/// <remarks>
/// <para>
/// These tests demonstrate the Testcontainers approach to integration testing:
/// <list type="bullet">
/// <item><description>Uses <see cref="PostgreSqlBuilder"/> to configure the container</description></item>
/// <item><description>Direct programmatic control over container lifecycle</description></item>
/// <item><description>Gets connection string via <c>container.GetConnectionString()</c></description></item>
/// </list>
/// </para>
/// <para>
/// This class implements <see cref="IAsyncLifetime"/> to manage the container
/// lifecycle efficiently across all tests in the class.
/// </para>
/// </remarks>
[Trait("Category", "Integration")]
[Trait("Provider", "Testcontainers")]
public sealed class PostgreSqlTestcontainersTests : IAsyncLifetime
{
    private PostgreSqlContainer? _container;

    /// <summary>
    /// Gets the time taken to initialize the container infrastructure.
    /// </summary>
    public TimeSpan InitializationTime { get; private set; }

    /// <summary>
    /// Gets the PostgreSQL connection string from the container.
    /// </summary>
    public string ConnectionString => _container?.GetConnectionString()
        ?? throw new InvalidOperationException("Not initialized");

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        var stopwatch = Stopwatch.StartNew();

        // Create and start the PostgreSQL container
        // Note: Use constructor with image parameter as parameterless constructor is deprecated
        _container = new PostgreSqlBuilder("postgres:17-alpine")
            .WithDatabase("encina_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithCleanUp(true)
            .Build();

        await _container.StartAsync();

        stopwatch.Stop();
        InitializationTime = stopwatch.Elapsed;
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.StopAsync();
            await _container.DisposeAsync();
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
