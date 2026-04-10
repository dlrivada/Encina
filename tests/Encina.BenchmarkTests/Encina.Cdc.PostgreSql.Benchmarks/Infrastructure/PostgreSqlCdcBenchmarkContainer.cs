using Testcontainers.PostgreSql;

namespace Encina.Cdc.PostgreSql.Benchmarks.Infrastructure;

/// <summary>
/// Spins up a PostgreSQL container with <c>wal_level = logical</c> so the CDC
/// <c>GetCurrentPositionAsync</c> benchmark can actually read the current WAL LSN via
/// <c>pg_current_wal_lsn()</c>.
/// </summary>
public sealed class PostgreSqlCdcBenchmarkContainer : IAsyncDisposable
{
    private readonly PostgreSqlContainer _container;

    /// <summary>
    /// Initializes a new container definition. The underlying Docker container is not started
    /// until <see cref="Start"/> is called.
    /// </summary>
    public PostgreSqlCdcBenchmarkContainer()
    {
        _container = new PostgreSqlBuilder("postgres:17-alpine")
            .WithDatabase("encina_cdc_bench")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithCommand(
                "-c", "wal_level=logical",
                "-c", "max_wal_senders=4",
                "-c", "max_replication_slots=4")
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
