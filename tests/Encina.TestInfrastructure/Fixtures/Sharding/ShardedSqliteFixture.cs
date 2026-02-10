using System.Data;

using Encina.Sharding;

using Microsoft.Data.Sqlite;

using Xunit;

namespace Encina.TestInfrastructure.Fixtures.Sharding;

/// <summary>
/// Sharding fixture that creates 3 separate file-based SQLite databases.
/// Uses file-based databases instead of in-memory shared to avoid the disposal trap
/// documented in MEMORY.md.
/// </summary>
public sealed class ShardedSqliteFixture : IAsyncLifetime
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), $"encina_shard_{Guid.NewGuid():N}");

    private const string ShardedTableSql = """
        CREATE TABLE IF NOT EXISTS ShardedEntities (
            Id TEXT NOT NULL PRIMARY KEY,
            ShardKey TEXT NOT NULL,
            Name TEXT NOT NULL,
            Value TEXT NULL,
            CreatedAtUtc TEXT NOT NULL DEFAULT (datetime('now'))
        );
        """;

    /// <summary>
    /// Gets the connection string for shard 1.
    /// </summary>
    public string Shard1ConnectionString => $"Data Source={Path.Combine(_tempDir, "shard1.db")}";

    /// <summary>
    /// Gets the connection string for shard 2.
    /// </summary>
    public string Shard2ConnectionString => $"Data Source={Path.Combine(_tempDir, "shard2.db")}";

    /// <summary>
    /// Gets the connection string for shard 3.
    /// </summary>
    public string Shard3ConnectionString => $"Data Source={Path.Combine(_tempDir, "shard3.db")}";

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

        var connection = new SqliteConnection(connectionString);
        connection.Open();
        return connection;
    }

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(_tempDir);

        // Create schema in each shard database
        await CreateShardSchemaAsync(Shard1ConnectionString);
        await CreateShardSchemaAsync(Shard2ConnectionString);
        await CreateShardSchemaAsync(Shard3ConnectionString);
    }

    /// <inheritdoc />
    public Task DisposeAsync()
    {
        // Clean up temp directory with shard databases
        try
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, recursive: true);
            }
        }
        catch
        {
            // Best effort cleanup
        }

        return Task.CompletedTask;
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

    private static async Task CreateShardSchemaAsync(string connectionString)
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = ShardedTableSql;
        await command.ExecuteNonQueryAsync();
    }

    private static async Task ClearShardDataAsync(string connectionString)
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM ShardedEntities;";
        await command.ExecuteNonQueryAsync();
    }
}
