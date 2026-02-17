using System.Data;

using Encina.Sharding;

using Microsoft.Data.SqlClient;

using Testcontainers.MsSql;

using Xunit;

namespace Encina.TestInfrastructure.Fixtures.Sharding;

/// <summary>
/// Sharding fixture that creates 3 separate databases within a single SQL Server container.
/// Each database represents a shard for integration testing of sharding features.
/// </summary>
public sealed class ShardedSqlServerFixture : IAsyncLifetime
{
    private MsSqlContainer? _container;

    private const string Shard1DbName = "EncinaShard1";
    private const string Shard2DbName = "EncinaShard2";
    private const string Shard3DbName = "EncinaShard3";

    private const string ShardedTableSql = """
        CREATE TABLE ShardedEntities (
            Id NVARCHAR(100) NOT NULL PRIMARY KEY,
            ShardKey NVARCHAR(200) NOT NULL,
            Name NVARCHAR(500) NOT NULL,
            Value NVARCHAR(MAX) NULL,
            CreatedAtUtc DATETIME2 NOT NULL DEFAULT GETUTCDATE()
        );
        """;

    private const string ReferenceTableSql = """
        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Country')
        CREATE TABLE Country (
            Id NVARCHAR(100) NOT NULL PRIMARY KEY,
            Code NVARCHAR(10) NOT NULL,
            Name NVARCHAR(200) NOT NULL
        );
        """;

    /// <summary>
    /// Gets the connection string for shard 1.
    /// </summary>
    public string Shard1ConnectionString => BuildConnectionString(Shard1DbName);

    /// <summary>
    /// Gets the connection string for shard 2.
    /// </summary>
    public string Shard2ConnectionString => BuildConnectionString(Shard2DbName);

    /// <summary>
    /// Gets the connection string for shard 3.
    /// </summary>
    public string Shard3ConnectionString => BuildConnectionString(Shard3DbName);

    /// <summary>
    /// Gets the base connection string (master database).
    /// </summary>
    public string ConnectionString => _container?.GetConnectionString() ?? string.Empty;

    /// <summary>
    /// Creates a <see cref="ShardTopology"/> with all 3 shards.
    /// </summary>
    public ShardTopology CreateTopology() => new([
        new ShardInfo("shard-1", Shard1ConnectionString),
        new ShardInfo("shard-2", Shard2ConnectionString),
        new ShardInfo("shard-3", Shard3ConnectionString)
    ]);

    /// <summary>
    /// Creates a connection to the specified shard database.
    /// </summary>
    /// <param name="shardId">The shard identifier (shard-1, shard-2, or shard-3).</param>
    /// <returns>An open connection to the shard database.</returns>
    public IDbConnection CreateConnection(string shardId)
    {
        var connectionString = shardId switch
        {
            "shard-1" => Shard1ConnectionString,
            "shard-2" => Shard2ConnectionString,
            "shard-3" => Shard3ConnectionString,
            _ => throw new ArgumentException($"Unknown shard ID: {shardId}", nameof(shardId))
        };

        var connection = new SqlConnection(connectionString);
        connection.Open();
        return connection;
    }

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        _container = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPassword("StrongP@ssw0rd!")
            .WithCleanUp(true)
            .Build();

        await _container.StartAsync();

        // Create 3 separate databases for sharding
        await using var masterConnection = new SqlConnection(ConnectionString);
        await masterConnection.OpenAsync();

        await CreateDatabaseAsync(masterConnection, Shard1DbName);
        await CreateDatabaseAsync(masterConnection, Shard2DbName);
        await CreateDatabaseAsync(masterConnection, Shard3DbName);

        // Create schema in each shard
        await CreateShardSchemaAsync(Shard1ConnectionString);
        await CreateShardSchemaAsync(Shard2ConnectionString);
        await CreateShardSchemaAsync(Shard3ConnectionString);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    /// <summary>
    /// Clears all data from all 3 shard databases.
    /// </summary>
    public async Task ClearAllDataAsync()
    {
        await ClearShardDataAsync(Shard1ConnectionString);
        await ClearShardDataAsync(Shard2ConnectionString);
        await ClearShardDataAsync(Shard3ConnectionString);
    }

    private string BuildConnectionString(string databaseName)
    {
        if (_container is null)
        {
            return string.Empty;
        }

        var builder = new SqlConnectionStringBuilder(_container.GetConnectionString())
        {
            InitialCatalog = databaseName
        };
        return builder.ConnectionString;
    }

    private static async Task CreateDatabaseAsync(SqlConnection masterConnection, string databaseName)
    {
        var sql = $"""
            IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = '{databaseName}')
            BEGIN
                CREATE DATABASE [{databaseName}];
            END
            """;

        await using var command = new SqlCommand(sql, masterConnection);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task CreateShardSchemaAsync(string connectionString)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand(ShardedTableSql, connection);
        await command.ExecuteNonQueryAsync();

        command.CommandText = ReferenceTableSql;
        await command.ExecuteNonQueryAsync();
    }

    private static async Task ClearShardDataAsync(string connectionString)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand("DELETE FROM ShardedEntities; DELETE FROM Country;", connection);
        await command.ExecuteNonQueryAsync();
    }
}
