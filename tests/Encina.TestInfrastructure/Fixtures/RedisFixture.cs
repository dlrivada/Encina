using StackExchange.Redis;
using Testcontainers.Redis;
using Xunit;

namespace Encina.TestInfrastructure.Fixtures;

/// <summary>
/// Redis fixture using Testcontainers.
/// Provides a throwaway Redis instance for integration tests.
/// </summary>
public sealed class RedisFixture : IAsyncLifetime
{
    private RedisContainer? _container;

    /// <summary>
    /// Gets the Redis connection multiplexer.
    /// </summary>
    public IConnectionMultiplexer? Connection { get; private set; }

    /// <summary>
    /// Gets the connection string for the Redis container.
    /// </summary>
    public string ConnectionString => _container?.GetConnectionString() ?? string.Empty;

    /// <summary>
    /// Gets a value indicating whether the Redis container is available.
    /// </summary>
    public bool IsAvailable => _container is not null && Connection is not null;

    /// <summary>
    /// Gets the database instance for direct Redis operations.
    /// </summary>
    public IDatabase? Database => Connection?.GetDatabase();

    /// <summary>
    /// Flushes all keys from the current database.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// This is useful for test isolation - call in InitializeAsync or at the start of each test
    /// to ensure a clean state.
    /// </remarks>
    public async Task FlushDatabaseAsync()
    {
        if (Connection is null)
        {
            return;
        }

        var server = Connection.GetServer(Connection.GetEndPoints()[0]);
        await server.FlushDatabaseAsync();
    }

    /// <summary>
    /// Clears all keys matching a pattern from the cache.
    /// </summary>
    /// <param name="pattern">The glob pattern to match (e.g., "user:*", "cache:*:data").</param>
    /// <returns>The number of keys that were deleted.</returns>
    /// <remarks>
    /// Supported pattern syntax:
    /// <list type="bullet">
    /// <item><description><c>*</c> matches any sequence of characters</description></item>
    /// <item><description><c>?</c> matches any single character</description></item>
    /// <item><description><c>[abc]</c> matches any character in the set</description></item>
    /// </list>
    /// </remarks>
    public async Task<long> ClearCacheAsync(string pattern)
    {
        if (Connection is null || Database is null)
        {
            return 0;
        }

        var server = Connection.GetServer(Connection.GetEndPoints()[0]);
        var keys = server.Keys(pattern: pattern).ToArray();

        if (keys.Length == 0)
        {
            return 0;
        }

        return await Database.KeyDeleteAsync(keys);
    }

    /// <summary>
    /// Gets all keys matching a pattern.
    /// </summary>
    /// <param name="pattern">The glob pattern to match (default: "*" for all keys).</param>
    /// <returns>An array of matching key names.</returns>
    public string[] GetKeys(string pattern = "*")
    {
        if (Connection is null)
        {
            return [];
        }

        var server = Connection.GetServer(Connection.GetEndPoints()[0]);
        return server.Keys(pattern: pattern).Select(k => k.ToString()).ToArray();
    }

    /// <summary>
    /// Gets all keys matching a pattern asynchronously.
    /// </summary>
    /// <param name="pattern">The glob pattern to match (default: "*" for all keys).</param>
    /// <returns>An array of matching key names.</returns>
    public async Task<string[]> GetKeysAsync(string pattern = "*")
    {
        if (Connection is null)
        {
            return [];
        }

        var server = Connection.GetServer(Connection.GetEndPoints()[0]);
        var keys = new List<string>();

        await foreach (var key in server.KeysAsync(pattern: pattern))
        {
            keys.Add(key.ToString());
        }

        return [.. keys];
    }

    /// <summary>
    /// Gets the count of keys in the database.
    /// </summary>
    /// <returns>The number of keys.</returns>
    public long GetKeyCount()
    {
        if (Connection is null)
        {
            return 0;
        }

        var server = Connection.GetServer(Connection.GetEndPoints()[0]);
        return server.DatabaseSize();
    }

    /// <inheritdoc/>
    public async ValueTask InitializeAsync()
    {
        _container = new RedisBuilder()
            .WithImage("redis:7-alpine")
            .WithCleanUp(true)
            .Build();

        await _container.StartAsync();

        Connection = await ConnectionMultiplexer.ConnectAsync(ConnectionString);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (Connection is not null)
        {
            await Connection.CloseAsync();
            Connection.Dispose();
        }

        if (_container is not null)
        {
            await _container.StopAsync();
            await _container.DisposeAsync();
        }
    }
}

/// <summary>
/// Collection definition for Redis integration tests.
/// </summary>
[CollectionDefinition(Name)]
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
public class RedisCollection : ICollectionFixture<RedisFixture>
#pragma warning restore CA1711
{
    /// <summary>
    /// The name of the collection.
    /// </summary>
    public const string Name = "Redis";
}
