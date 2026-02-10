using System.Data;

using Encina.Sharding;

using Npgsql;

using Testcontainers.PostgreSql;

using Xunit;

namespace Encina.TestInfrastructure.Fixtures.Sharding;

/// <summary>
/// Sharding fixture that creates 3 separate schemas within a single PostgreSQL container.
/// Each schema represents a shard for integration testing of sharding features.
/// </summary>
public sealed class ShardedPostgreSqlFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;

    private const string Shard1Schema = "shard1";
    private const string Shard2Schema = "shard2";
    private const string Shard3Schema = "shard3";

    private static string GetShardedTableSql(string schema) => $"""
        CREATE TABLE IF NOT EXISTS {schema}.sharded_entities (
            id VARCHAR(100) NOT NULL PRIMARY KEY,
            shard_key VARCHAR(200) NOT NULL,
            name VARCHAR(500) NOT NULL,
            value TEXT NULL,
            created_at_utc TIMESTAMPTZ NOT NULL DEFAULT NOW()
        );
        """;

    /// <summary>
    /// Gets the connection string for shard 1 (with search_path set to shard1 schema).
    /// </summary>
    public string Shard1ConnectionString => BuildConnectionString(Shard1Schema);

    /// <summary>
    /// Gets the connection string for shard 2 (with search_path set to shard2 schema).
    /// </summary>
    public string Shard2ConnectionString => BuildConnectionString(Shard2Schema);

    /// <summary>
    /// Gets the connection string for shard 3 (with search_path set to shard3 schema).
    /// </summary>
    public string Shard3ConnectionString => BuildConnectionString(Shard3Schema);

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
    /// Creates a connection to the specified shard schema.
    /// </summary>
    /// <param name="shardId">The shard identifier (shard-1, shard-2, or shard-3).</param>
    /// <returns>An open connection to the shard schema.</returns>
    public IDbConnection CreateConnection(string shardId)
    {
        var connectionString = shardId switch
        {
            "shard-1" => Shard1ConnectionString,
            "shard-2" => Shard2ConnectionString,
            "shard-3" => Shard3ConnectionString,
            _ => throw new ArgumentException($"Unknown shard ID: {shardId}", nameof(shardId))
        };

        var connection = new NpgsqlConnection(connectionString);
        connection.Open();
        return connection;
    }

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:17-alpine")
            .WithDatabase("encina_shard_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithCleanUp(true)
            .Build();

        await _container.StartAsync();

        // Create 3 separate schemas for sharding
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();

        await CreateSchemaAsync(connection, Shard1Schema);
        await CreateSchemaAsync(connection, Shard2Schema);
        await CreateSchemaAsync(connection, Shard3Schema);

        // Create tables in each schema
        await CreateShardTableAsync(connection, Shard1Schema);
        await CreateShardTableAsync(connection, Shard2Schema);
        await CreateShardTableAsync(connection, Shard3Schema);
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
    /// Clears all data from all 3 shard schemas.
    /// </summary>
    public async Task ClearAllDataAsync()
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();

        await ClearSchemaDataAsync(connection, Shard1Schema);
        await ClearSchemaDataAsync(connection, Shard2Schema);
        await ClearSchemaDataAsync(connection, Shard3Schema);
    }

    private string BuildConnectionString(string schema)
    {
        if (_container is null)
        {
            return string.Empty;
        }

        var builder = new NpgsqlConnectionStringBuilder(ConnectionString)
        {
            SearchPath = schema
        };
        return builder.ConnectionString;
    }

    private static async Task CreateSchemaAsync(NpgsqlConnection connection, string schema)
    {
        var sql = $"CREATE SCHEMA IF NOT EXISTS {schema};";
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task CreateShardTableAsync(NpgsqlConnection connection, string schema)
    {
        var sql = GetShardedTableSql(schema);
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task ClearSchemaDataAsync(NpgsqlConnection connection, string schema)
    {
        var sql = $"DELETE FROM {schema}.sharded_entities;";
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }
}
