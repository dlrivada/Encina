using System.Data;
using Encina.Dapper.SqlServer.ReadWriteSeparation;
using Encina.Messaging.ReadWriteSeparation;
using Encina.TestInfrastructure;
using Encina.TestInfrastructure.Entities;
using Encina.TestInfrastructure.Fixtures;
using Encina.TestInfrastructure.Schemas;
using Microsoft.Data.SqlClient;
using Shouldly;

namespace Encina.IntegrationTests.Dapper.SqlServer.ReadWriteSeparation;

/// <summary>
/// Integration tests for read/write separation in Dapper SQL Server provider.
/// Tests connection routing logic using the same database (simulated separation).
/// </summary>
/// <remarks>
/// <para>
/// Since setting up actual SQL Server replication requires complex infrastructure,
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
[Trait("Database", "SqlServer")]
public class ReadWriteSeparationDapperIntegrationTests : ReadWriteSeparationTestsBase<SqlServerFixture>
{
    private readonly SqlServerFixture _fixture = new();
    private ReadWriteConnectionFactory _connectionFactory = null!;
    private ReadWriteSeparationOptions _options = null!;

    /// <inheritdoc />
    protected override SqlServerFixture Fixture => _fixture;

    /// <inheritdoc />
    protected override string ProviderName => "Dapper.SqlServer";

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
        await _fixture.InitializeAsync();

        if (!_fixture.IsAvailable)
            return;

        // Create read/write test entities table
        using var connection = (SqlConnection)_fixture.CreateConnection();
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
        try
        {
            if (_fixture.IsAvailable)
            {
                using var connection = (SqlConnection)_fixture.CreateConnection();
                await TenancySchema.ClearTenancyDataAsync(connection);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }

        await _fixture.DisposeAsync();
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
            await ((SqlConnection)connection).OpenAsync();

        const string sql = """
            INSERT INTO ReadWriteTestEntities (Id, Name, Value, Timestamp, WriteCounter)
            VALUES (@Id, @Name, @Value, @Timestamp, @WriteCounter)
            """;

        await using var command = new SqlCommand(sql, (SqlConnection)connection);
        command.Parameters.AddWithValue("@Id", entity.Id);
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
            await ((SqlConnection)connection).OpenAsync();

        const string sql = "SELECT Id, Name, Value, Timestamp, WriteCounter FROM ReadWriteTestEntities";

        await using var command = new SqlCommand(sql, (SqlConnection)connection);
        await using var reader = await command.ExecuteReaderAsync();

        var entities = new List<ReadWriteTestEntity>();
        while (await reader.ReadAsync())
        {
            entities.Add(new ReadWriteTestEntity
            {
                Id = reader.GetGuid(0),
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
            await ((SqlConnection)connection).OpenAsync();

        const string sql = "SELECT Id, Name, Value, Timestamp, WriteCounter FROM ReadWriteTestEntities WHERE Id = @Id";

        await using var command = new SqlCommand(sql, (SqlConnection)connection);
        command.Parameters.AddWithValue("@Id", id);
        await using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return new ReadWriteTestEntity
            {
                Id = reader.GetGuid(0),
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
            await ((SqlConnection)connection).OpenAsync();

        const string sql = """
            UPDATE ReadWriteTestEntities
            SET Name = @Name, Value = @Value, Timestamp = @Timestamp, WriteCounter = @WriteCounter
            WHERE Id = @Id
            """;

        await using var command = new SqlCommand(sql, (SqlConnection)connection);
        command.Parameters.AddWithValue("@Id", entity.Id);
        command.Parameters.AddWithValue("@Name", entity.Name);
        command.Parameters.AddWithValue("@Value", entity.Value);
        command.Parameters.AddWithValue("@Timestamp", entity.Timestamp);
        command.Parameters.AddWithValue("@WriteCounter", entity.WriteCounter);
        await command.ExecuteNonQueryAsync();
    }

    /// <inheritdoc />
    protected override string GetConnectionString(IDbConnection connection)
    {
        return ((SqlConnection)connection).ConnectionString;
    }

    /// <inheritdoc />
    protected override async Task ClearTestDataAsync()
    {
        try
        {
            if (_fixture.IsAvailable)
            {
                using var connection = (SqlConnection)_fixture.CreateConnection();
                await TenancySchema.ClearTenancyDataAsync(connection);
            }
        }
        catch
        {
            // Ignore if table doesn't exist
        }
    }

    #region Additional Dapper SqlServer-Specific Tests

    [SkippableFact]
    public void ConnectionFactory_ShouldBeConfiguredCorrectly()
    {
        Skip.IfNot(_fixture.IsAvailable, "SQL Server container not available");

        // Assert
        _connectionFactory.ShouldNotBeNull();
        _connectionFactory.GetWriteConnectionString().ShouldBe(_fixture.ConnectionString);
        _connectionFactory.GetReadConnectionString().ShouldBe(_fixture.ConnectionString);
    }

    [SkippableFact]
    public async Task ReadConnectionAsync_ShouldOpenAndReturnConnection()
    {
        Skip.IfNot(_fixture.IsAvailable, "SQL Server container not available");

        // Act
        var connection = await _connectionFactory.CreateReadConnectionAsync();
        try
        {
            // Assert
            connection.State.ShouldBe(ConnectionState.Open);
            connection.ShouldBeOfType<SqlConnection>();
        }
        finally
        {
            connection.Dispose();
        }
    }

    [SkippableFact]
    public async Task WriteConnectionAsync_ShouldOpenAndReturnConnection()
    {
        Skip.IfNot(_fixture.IsAvailable, "SQL Server container not available");

        // Act
        var connection = await _connectionFactory.CreateWriteConnectionAsync();
        try
        {
            // Assert
            connection.State.ShouldBe(ConnectionState.Open);
            connection.ShouldBeOfType<SqlConnection>();
        }
        finally
        {
            connection.Dispose();
        }
    }

    [SkippableFact]
    public async Task ConnectionAsync_WithReadIntent_ShouldRouteToRead()
    {
        Skip.IfNot(_fixture.IsAvailable, "SQL Server container not available");

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

    [SkippableFact]
    public async Task ConnectionAsync_WithWriteIntent_ShouldRouteToPrimary()
    {
        Skip.IfNot(_fixture.IsAvailable, "SQL Server container not available");

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

    [SkippableFact]
    public async Task MultipleReadConnections_ShouldAllBeUsable()
    {
        Skip.IfNot(_fixture.IsAvailable, "SQL Server container not available");

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
