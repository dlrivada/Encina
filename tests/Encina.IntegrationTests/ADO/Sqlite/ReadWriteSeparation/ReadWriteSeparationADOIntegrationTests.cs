using System.Data;
using System.Globalization;
using Encina.ADO.Sqlite.ReadWriteSeparation;
using Encina.Messaging.ReadWriteSeparation;
using Encina.TestInfrastructure;
using Encina.TestInfrastructure.Entities;
using Encina.TestInfrastructure.Fixtures;
using Encina.TestInfrastructure.Schemas;
using Microsoft.Data.Sqlite;
using Shouldly;
using Xunit;

namespace Encina.IntegrationTests.ADO.Sqlite.ReadWriteSeparation;

/// <summary>
/// Integration tests for read/write separation in ADO.NET SQLite provider.
/// Tests connection routing logic using the same database (simulated separation).
/// </summary>
/// <remarks>
/// <para>
/// SQLite doesn't natively support replication. These tests verify routing logic
/// using the same database with the same connection string. This validates that:
/// </para>
/// <list type="bullet">
/// <item><description>ReadWriteConnectionFactory correctly routes read operations</description></item>
/// <item><description>ReadWriteConnectionFactory correctly routes write operations</description></item>
/// <item><description>ForceWriteDatabase scenarios work correctly</description></item>
/// <item><description>Connection factory creates usable connections</description></item>
/// </list>
/// <para>
/// In production SQLite scenarios, "replicas" would be file copies that are periodically
/// synchronized from the primary database file.
/// </para>
/// </remarks>
[Trait("Category", "Integration")]
[Trait("Database", "Sqlite")]
public class ReadWriteSeparationADOIntegrationTests : ReadWriteSeparationTestsBase<SqliteFixture>, IClassFixture<SqliteFixture>
{
    private readonly SqliteFixture _fixture;
    private ReadWriteConnectionFactory _connectionFactory = null!;
    private ReadWriteSeparationOptions _options = null!;
    private bool _initialized;

    public ReadWriteSeparationADOIntegrationTests(SqliteFixture fixture)
    {
        _fixture = fixture;
    }

    /// <inheritdoc />
    protected override SqliteFixture Fixture => _fixture;

    /// <inheritdoc />
    protected override string ProviderName => "Sqlite";

    /// <inheritdoc />
    protected override bool HasSeparateReadWriteEndpoints => false; // Simulated, same DB

    /// <inheritdoc />
    protected override string PrimaryConnectionString => _options?.WriteConnectionString ?? string.Empty;

    /// <inheritdoc />
    protected override string ReadReplicaConnectionString =>
        _options?.ReadConnectionStrings.FirstOrDefault() ?? PrimaryConnectionString;

    /// <inheritdoc />
    public override async Task InitializeAsync()
    {
        if (!_initialized)
        {
            // Create read/write test entities table
            using var connection = (SqliteConnection)_fixture.CreateConnection();
            await TenancySchema.CreateReadWriteTestEntitiesSchemaAsync(connection);

            // Configure read/write separation options
            _options = new ReadWriteSeparationOptions
            {
                WriteConnectionString = _fixture.ConnectionString,
                ReadConnectionStrings = { _fixture.ConnectionString } // Same DB for routing logic test
            };

            var replicas = _options.ReadConnectionStrings.ToList().AsReadOnly();
            var selector = new ReadWriteConnectionSelector(_options, new RoundRobinReplicaSelector(replicas));
            _connectionFactory = new ReadWriteConnectionFactory(selector);

            _initialized = true;
        }

        await base.InitializeAsync();
    }

    /// <inheritdoc />
    public override async Task DisposeAsync()
    {
        try
        {
            using var connection = (SqliteConnection)_fixture.CreateConnection();
            await TenancySchema.ClearTenancyDataAsync(connection);
        }
        catch
        {
            // Ignore cleanup errors
        }

        await base.DisposeAsync();
    }

    /// <inheritdoc />
    protected override IDbConnection CreateReadConnection()
    {
        return _connectionFactory.CreateReadConnection();
    }

    /// <inheritdoc />
    protected override IDbConnection CreateWriteConnection()
    {
        return _connectionFactory.CreateWriteConnection();
    }

    /// <inheritdoc />
    protected override IDbConnection CreateForcedWriteConnection()
    {
        return _connectionFactory.CreateWriteConnection();
    }

    /// <inheritdoc />
    protected override async Task InsertEntityAsync(IDbConnection connection, ReadWriteTestEntity entity)
    {
        if (connection.State != ConnectionState.Open)
            await ((SqliteConnection)connection).OpenAsync();

        const string sql = """
            INSERT INTO ReadWriteTestEntities (Id, Name, Value, Timestamp, WriteCounter)
            VALUES (@Id, @Name, @Value, @Timestamp, @WriteCounter)
            """;

        await using var command = new SqliteCommand(sql, (SqliteConnection)connection);
        command.Parameters.AddWithValue("@Id", entity.Id.ToString());
        command.Parameters.AddWithValue("@Name", entity.Name);
        command.Parameters.AddWithValue("@Value", entity.Value);
        command.Parameters.AddWithValue("@Timestamp", entity.Timestamp.ToString("O"));
        command.Parameters.AddWithValue("@WriteCounter", entity.WriteCounter);
        await command.ExecuteNonQueryAsync();
    }

    /// <inheritdoc />
    protected override async Task<List<ReadWriteTestEntity>> QueryEntitiesAsync(IDbConnection connection)
    {
        if (connection.State != ConnectionState.Open)
            await ((SqliteConnection)connection).OpenAsync();

        const string sql = "SELECT Id, Name, Value, Timestamp, WriteCounter FROM ReadWriteTestEntities";

        await using var command = new SqliteCommand(sql, (SqliteConnection)connection);
        await using var reader = await command.ExecuteReaderAsync();

        var entities = new List<ReadWriteTestEntity>();
        while (await reader.ReadAsync())
        {
            entities.Add(new ReadWriteTestEntity
            {
                Id = Guid.Parse(reader.GetString(0)),
                Name = reader.GetString(1),
                Value = reader.GetInt32(2),
                Timestamp = DateTime.Parse(reader.GetString(3), CultureInfo.InvariantCulture),
                WriteCounter = reader.GetInt32(4)
            });
        }

        return entities;
    }

    /// <inheritdoc />
    protected override async Task<ReadWriteTestEntity?> QueryEntityByIdAsync(IDbConnection connection, Guid id)
    {
        if (connection.State != ConnectionState.Open)
            await ((SqliteConnection)connection).OpenAsync();

        const string sql = "SELECT Id, Name, Value, Timestamp, WriteCounter FROM ReadWriteTestEntities WHERE Id = @Id";

        await using var command = new SqliteCommand(sql, (SqliteConnection)connection);
        command.Parameters.AddWithValue("@Id", id.ToString());
        await using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return new ReadWriteTestEntity
            {
                Id = Guid.Parse(reader.GetString(0)),
                Name = reader.GetString(1),
                Value = reader.GetInt32(2),
                Timestamp = DateTime.Parse(reader.GetString(3), CultureInfo.InvariantCulture),
                WriteCounter = reader.GetInt32(4)
            };
        }

        return null;
    }

    /// <inheritdoc />
    protected override async Task UpdateEntityAsync(IDbConnection connection, ReadWriteTestEntity entity)
    {
        if (connection.State != ConnectionState.Open)
            await ((SqliteConnection)connection).OpenAsync();

        const string sql = """
            UPDATE ReadWriteTestEntities
            SET Name = @Name, Value = @Value, Timestamp = @Timestamp, WriteCounter = @WriteCounter
            WHERE Id = @Id
            """;

        await using var command = new SqliteCommand(sql, (SqliteConnection)connection);
        command.Parameters.AddWithValue("@Id", entity.Id.ToString());
        command.Parameters.AddWithValue("@Name", entity.Name);
        command.Parameters.AddWithValue("@Value", entity.Value);
        command.Parameters.AddWithValue("@Timestamp", entity.Timestamp.ToString("O"));
        command.Parameters.AddWithValue("@WriteCounter", entity.WriteCounter);
        await command.ExecuteNonQueryAsync();
    }

    /// <inheritdoc />
    protected override string GetConnectionString(IDbConnection connection)
    {
        return ((SqliteConnection)connection).ConnectionString;
    }

    /// <inheritdoc />
    protected override async Task ClearTestDataAsync()
    {
        try
        {
            using var connection = (SqliteConnection)_fixture.CreateConnection();
            await TenancySchema.ClearTenancyDataAsync(connection);
        }
        catch
        {
            // Ignore if table doesn't exist
        }
    }

    #region Additional SQLite-Specific Tests

    [Fact]
    public void ConnectionFactory_ShouldBeConfiguredCorrectly()
    {
        // Assert
        _connectionFactory.ShouldNotBeNull();
        _connectionFactory.GetWriteConnectionString().ShouldBe(_fixture.ConnectionString);
        _connectionFactory.GetReadConnectionString().ShouldBe(_fixture.ConnectionString);
    }

    [Fact]
    public async Task ReadConnectionAsync_ShouldOpenAndReturnConnection()
    {
        // Act
        var connection = await _connectionFactory.CreateReadConnectionAsync();
        try
        {
            // Assert
            connection.State.ShouldBe(ConnectionState.Open);
            connection.ShouldBeOfType<SqliteConnection>();
        }
        finally
        {
            connection.Dispose();
        }
    }

    [Fact]
    public async Task WriteConnectionAsync_ShouldOpenAndReturnConnection()
    {
        // Act
        var connection = await _connectionFactory.CreateWriteConnectionAsync();
        try
        {
            // Assert
            connection.State.ShouldBe(ConnectionState.Open);
            connection.ShouldBeOfType<SqliteConnection>();
        }
        finally
        {
            connection.Dispose();
        }
    }

    [Fact]
    public async Task ConnectionAsync_WithReadIntent_ShouldRouteToRead()
    {
        // Arrange
        using var scope = new DatabaseRoutingScope(DatabaseIntent.Read);

        // Act
        var connection = await _connectionFactory.CreateConnectionAsync();
        try
        {
            // Assert
            connection.State.ShouldBe(ConnectionState.Open);
            _connectionFactory.GetReadConnectionString().ShouldNotBeEmpty();
        }
        finally
        {
            connection.Dispose();
        }
    }

    [Fact]
    public async Task ConnectionAsync_WithWriteIntent_ShouldRouteToPrimary()
    {
        // Arrange
        using var scope = new DatabaseRoutingScope(DatabaseIntent.Write);

        // Act
        var connection = await _connectionFactory.CreateConnectionAsync();
        try
        {
            // Assert
            connection.State.ShouldBe(ConnectionState.Open);
            _connectionFactory.GetWriteConnectionString().ShouldNotBeEmpty();
        }
        finally
        {
            connection.Dispose();
        }
    }

    [Fact]
    public async Task MultipleReadConnections_ShouldAllBeUsable()
    {
        // Arrange - Insert test data
        var entity = new ReadWriteTestEntity
        {
            Id = Guid.NewGuid(),
            Name = "Multi-Read Test",
            Value = 999,
            Timestamp = DateTime.UtcNow,
            WriteCounter = 0
        };

        using (var writeConn = CreateWriteConnection())
        {
            await InsertEntityAsync(writeConn, entity);
        }

        // Act - Create multiple read connections
        var connections = new List<IDbConnection>();
        try
        {
            for (int i = 0; i < 3; i++)
            {
                var conn = _connectionFactory.CreateReadConnection();
                connections.Add(conn);
            }

            // Assert - All connections should work
            foreach (var conn in connections)
            {
                var entities = await QueryEntitiesAsync(conn);
                entities.ShouldContain(e => e.Id == entity.Id);
            }
        }
        finally
        {
            foreach (var conn in connections)
            {
                conn.Dispose();
            }
        }
    }

    [Fact]
    public void SqliteConnection_ShouldUseInMemoryMode()
    {
        // SQLite in-memory mode means "replicas" would share the same database
        // This test verifies we're operating in in-memory mode
        var connectionString = _fixture.ConnectionString;
        connectionString.ShouldContain("Mode=Memory", Case.Insensitive);
    }

    #endregion
}
