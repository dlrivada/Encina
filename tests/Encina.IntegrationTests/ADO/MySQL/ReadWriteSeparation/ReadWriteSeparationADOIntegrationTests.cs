using System.Data;
using Encina.ADO.MySQL.ReadWriteSeparation;
using Encina.Messaging.ReadWriteSeparation;
using Encina.TestInfrastructure;
using Encina.TestInfrastructure.Entities;
using Encina.TestInfrastructure.Fixtures;
using Encina.TestInfrastructure.Schemas;
using MySqlConnector;
using Shouldly;
using Xunit;

namespace Encina.IntegrationTests.ADO.MySQL.ReadWriteSeparation;

/// <summary>
/// Integration tests for read/write separation in ADO.NET MySQL provider.
/// Tests connection routing logic using the same database (simulated separation).
/// </summary>
/// <remarks>
/// <para>
/// Since setting up actual MySQL master-slave replication requires complex infrastructure,
/// these tests verify routing logic using the same database with different
/// "simulated" connection strings. This validates that:
/// </para>
/// <list type="bullet">
/// <item><description>ReadWriteConnectionFactory correctly routes read operations</description></item>
/// <item><description>ReadWriteConnectionFactory correctly routes write operations</description></item>
/// <item><description>ForceWriteDatabase scenarios work correctly</description></item>
/// <item><description>Connection factory creates usable connections</description></item>
/// </list>
/// </remarks>
[Trait("Category", "Integration")]
[Trait("Database", "MySQL")]
[Collection("ADO-MySQL")]
public class ReadWriteSeparationADOIntegrationTests : ReadWriteSeparationTestsBase<MySqlFixture>
{
    private readonly MySqlFixture _fixture;
    private ReadWriteConnectionFactory _connectionFactory = null!;
    private ReadWriteSeparationOptions _options = null!;

    public ReadWriteSeparationADOIntegrationTests(MySqlFixture fixture)
    {
        _fixture = fixture;
    }

    /// <inheritdoc />
    protected override MySqlFixture Fixture => _fixture;

    /// <inheritdoc />
    protected override string ProviderName => "MySQL";

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
        if (!_fixture.IsAvailable)
            return;

        // Create read/write test entities table
        await using var connection = (MySqlConnection)_fixture.CreateConnection();
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

        await base.InitializeAsync();
    }

    /// <inheritdoc />
    public override async Task DisposeAsync()
    {
        if (_fixture.IsAvailable)
        {
            try
            {
                await using var connection = (MySqlConnection)_fixture.CreateConnection();
                await TenancySchema.ClearTenancyDataAsync(connection);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        await _fixture.ClearAllDataAsync();
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
            await ((MySqlConnection)connection).OpenAsync();

        const string sql = """
            INSERT INTO ReadWriteTestEntities (Id, Name, Value, Timestamp, WriteCounter)
            VALUES (@Id, @Name, @Value, @Timestamp, @WriteCounter)
            """;

        await using var command = new MySqlCommand(sql, (MySqlConnection)connection);
        command.Parameters.AddWithValue("@Id", entity.Id.ToString());
        command.Parameters.AddWithValue("@Name", entity.Name);
        command.Parameters.AddWithValue("@Value", entity.Value);
        command.Parameters.AddWithValue("@Timestamp", entity.Timestamp);
        command.Parameters.AddWithValue("@WriteCounter", entity.WriteCounter);
        await command.ExecuteNonQueryAsync();
    }

    /// <inheritdoc />
    protected override async Task<List<ReadWriteTestEntity>> QueryEntitiesAsync(IDbConnection connection)
    {
        if (connection.State != ConnectionState.Open)
            await ((MySqlConnection)connection).OpenAsync();

        const string sql = "SELECT Id, Name, Value, Timestamp, WriteCounter FROM ReadWriteTestEntities";

        await using var command = new MySqlCommand(sql, (MySqlConnection)connection);
        await using var reader = await command.ExecuteReaderAsync();

        var entities = new List<ReadWriteTestEntity>();
        while (await reader.ReadAsync())
        {
            // MySQL may return CHAR(36) as string or as Guid depending on connector settings
            var idValue = reader.GetValue(0);
            var id = idValue is Guid guid ? guid : Guid.Parse(idValue.ToString()!);

            entities.Add(new ReadWriteTestEntity
            {
                Id = id,
                Name = reader.GetString(1),
                Value = reader.GetInt32(2),
                Timestamp = reader.GetDateTime(3),
                WriteCounter = reader.GetInt32(4)
            });
        }

        return entities;
    }

    /// <inheritdoc />
    protected override async Task<ReadWriteTestEntity?> QueryEntityByIdAsync(IDbConnection connection, Guid id)
    {
        if (connection.State != ConnectionState.Open)
            await ((MySqlConnection)connection).OpenAsync();

        const string sql = "SELECT Id, Name, Value, Timestamp, WriteCounter FROM ReadWriteTestEntities WHERE Id = @Id";

        await using var command = new MySqlCommand(sql, (MySqlConnection)connection);
        command.Parameters.AddWithValue("@Id", id.ToString());
        await using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            // MySQL may return CHAR(36) as string or as Guid depending on connector settings
            var idValue = reader.GetValue(0);
            var entityId = idValue is Guid guid ? guid : Guid.Parse(idValue.ToString()!);

            return new ReadWriteTestEntity
            {
                Id = entityId,
                Name = reader.GetString(1),
                Value = reader.GetInt32(2),
                Timestamp = reader.GetDateTime(3),
                WriteCounter = reader.GetInt32(4)
            };
        }

        return null;
    }

    /// <inheritdoc />
    protected override async Task UpdateEntityAsync(IDbConnection connection, ReadWriteTestEntity entity)
    {
        if (connection.State != ConnectionState.Open)
            await ((MySqlConnection)connection).OpenAsync();

        const string sql = """
            UPDATE ReadWriteTestEntities
            SET Name = @Name, Value = @Value, Timestamp = @Timestamp, WriteCounter = @WriteCounter
            WHERE Id = @Id
            """;

        await using var command = new MySqlCommand(sql, (MySqlConnection)connection);
        command.Parameters.AddWithValue("@Id", entity.Id.ToString());
        command.Parameters.AddWithValue("@Name", entity.Name);
        command.Parameters.AddWithValue("@Value", entity.Value);
        command.Parameters.AddWithValue("@Timestamp", entity.Timestamp);
        command.Parameters.AddWithValue("@WriteCounter", entity.WriteCounter);
        await command.ExecuteNonQueryAsync();
    }

    /// <inheritdoc />
    protected override string GetConnectionString(IDbConnection connection)
    {
        return ((MySqlConnection)connection).ConnectionString;
    }

    /// <inheritdoc />
    protected override async Task ClearTestDataAsync()
    {
        if (!_fixture.IsAvailable)
            return;

        try
        {
            await using var connection = (MySqlConnection)_fixture.CreateConnection();
            await TenancySchema.ClearTenancyDataAsync(connection);
        }
        catch
        {
            // Ignore if table doesn't exist
        }
    }

    #region Additional MySQL-Specific Tests

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
            connection.ShouldBeOfType<MySqlConnection>();
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
            connection.ShouldBeOfType<MySqlConnection>();
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

    #endregion
}
