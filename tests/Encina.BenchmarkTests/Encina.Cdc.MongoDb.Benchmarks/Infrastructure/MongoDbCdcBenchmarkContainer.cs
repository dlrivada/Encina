using Testcontainers.MongoDb;

namespace Encina.Cdc.MongoDb.Benchmarks.Infrastructure;

/// <summary>
/// Spins up a MongoDB container for CDC benchmarks. Uses a standalone deployment because
/// the connector's <c>GetCurrentPositionAsync</c> path only needs to ping the server and
/// read the last saved position from the store (replica set / change streams are only
/// required for the full <c>StreamChangesAsync</c> path, which is out of scope here).
/// </summary>
public sealed class MongoDbCdcBenchmarkContainer : IAsyncDisposable
{
    private readonly MongoDbContainer _container;

    /// <summary>
    /// Initializes a new container definition. The underlying Docker container is not started
    /// until <see cref="Start"/> is called.
    /// </summary>
    public MongoDbCdcBenchmarkContainer()
    {
        _container = new MongoDbBuilder("mongo:7")
            .WithCleanUp(true)
            .Build();
    }

    /// <summary>
    /// Gets the connection string for the running container. Only valid after <see cref="Start"/>.
    /// </summary>
    public string ConnectionString => _container.GetConnectionString();

    /// <summary>
    /// Starts the container synchronously. Safe to call from <c>[GlobalSetup]</c>.
    /// </summary>
    public void Start()
    {
        _container.StartAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Stops and removes the container synchronously. Safe to call from <c>[GlobalCleanup]</c>.
    /// </summary>
    public void Stop()
    {
        _container.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _container.DisposeAsync().ConfigureAwait(false);
    }
}
