using System.Data;

using Encina.Sharding;

using MySqlConnector;

using Testcontainers.MySql;

using Xunit;

namespace Encina.TestInfrastructure.Fixtures.Sharding;

/// <summary>
/// Sharding fixture that creates 3 separate databases within a single MySQL container.
/// Each database represents a shard for integration testing of sharding features.
/// </summary>
public sealed class ShardedMySqlFixture : IAsyncLifetime
{
    private MySqlContainer? _container;

    private const string Shard1DbName = "encina_shard1";
    private const string Shard2DbName = "encina_shard2";
    private const string Shard3DbName = "encina_shard3";

    private const string ShardedTableSql = """
        CREATE TABLE IF NOT EXISTS `ShardedEntities` (
            `Id` VARCHAR(100) NOT NULL PRIMARY KEY,
            `ShardKey` VARCHAR(200) NOT NULL,
            `Name` VARCHAR(500) NOT NULL,
            `Value` TEXT NULL,
            `CreatedAtUtc` DATETIME NOT NULL DEFAULT UTC_TIMESTAMP()
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
    /// Gets the base connection string.
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

        var connection = new MySqlConnection(connectionString);
        connection.Open();
        return connection;
    }

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        _container = new MySqlBuilder()
            .WithImage("mysql:9.1")
            .WithDatabase("encina_shard_test")
            .WithUsername("root")
            .WithPassword("mysql")
            .WithCleanUp(true)
            .Build();

        await _container.StartAsync();

        // Create 3 separate databases for sharding
        await using var masterConnection = new MySqlConnection(ConnectionString);
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
    public async Task DisposeAsync()
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

        var builder = new MySqlConnectionStringBuilder(ConnectionString)
        {
            Database = databaseName
        };
        return builder.ConnectionString;
    }

    private static async Task CreateDatabaseAsync(MySqlConnection connection, string databaseName)
    {
        var sql = $"CREATE DATABASE IF NOT EXISTS `{databaseName}`;";
        await using var command = new MySqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task CreateShardSchemaAsync(string connectionString)
    {
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = new MySqlCommand(ShardedTableSql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task ClearShardDataAsync(string connectionString)
    {
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = new MySqlCommand("DELETE FROM `ShardedEntities`;", connection);
        await command.ExecuteNonQueryAsync();
    }
}
