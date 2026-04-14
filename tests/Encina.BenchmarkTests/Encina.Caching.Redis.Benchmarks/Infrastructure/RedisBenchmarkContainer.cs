using StackExchange.Redis;
using Testcontainers.Redis;

namespace Encina.Caching.Redis.Benchmarks.Infrastructure;

/// <summary>
/// Manages a single Redis container shared across all benchmark methods in a class.
/// Uses synchronous Start/Stop because BenchmarkDotNet's [GlobalSetup]/[GlobalCleanup]
/// run synchronously by default.
/// </summary>
public sealed class RedisBenchmarkContainer : IAsyncDisposable
{
    private readonly RedisContainer _container;
    private ConnectionMultiplexer? _connection;

    public RedisBenchmarkContainer()
    {
        _container = new RedisBuilder("redis:7-alpine")
            .WithCleanUp(true)
            .Build();
    }

    public string ConnectionString =>
        _container.GetConnectionString();

    public IConnectionMultiplexer Connection =>
        _connection ?? throw new InvalidOperationException(
            "Container not started. Call Start() first.");

    public void Start()
    {
        _container.StartAsync().GetAwaiter().GetResult();
        _connection = ConnectionMultiplexer.Connect(_container.GetConnectionString());
    }

    public void Stop()
    {
        _connection?.Dispose();
        _container.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    public async ValueTask DisposeAsync()
    {
        _connection?.Dispose();
        await _container.DisposeAsync().ConfigureAwait(false);
    }
}
