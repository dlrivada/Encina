using StackExchange.Redis;
using Testcontainers.Redis;

namespace Encina.IntegrationTests.Infrastructure.Caching.Fixtures;

/// <summary>
/// Test fixture that provides a Redis container for integration tests.
/// </summary>
public class RedisFixture : IAsyncLifetime
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

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        try
        {
            _container = new RedisBuilder("redis:7-alpine")
                .WithPortBinding(6379, true)
                .Build();

            await _container.StartAsync();

            Connection = await ConnectionMultiplexer.ConnectAsync(ConnectionString);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to start Redis container: {ex.Message}");
            // Container might not be available in CI without Docker
        }
    }

    /// <inheritdoc/>
    public async Task DisposeAsync()
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
