using Testcontainers.MySql;

namespace Encina.Cdc.MySql.Benchmarks.Infrastructure;

/// <summary>
/// Spins up a MySQL container with binary logging enabled so the CDC
/// <c>GetCurrentPositionAsync</c> benchmark can actually read binlog state via
/// <c>SHOW MASTER STATUS</c>.
/// </summary>
/// <remarks>
/// Uses <c>mysql:8.0</c> rather than 9.x because the Encina MySQL CDC connector queries
/// <c>SHOW MASTER STATUS</c>, which was removed in MySQL 9.0. Enables <c>--log-bin</c>
/// explicitly so we do not depend on the image's default binlog configuration.
/// </remarks>
public sealed class MySqlCdcBenchmarkContainer : IAsyncDisposable
{
    private readonly MySqlContainer _container;

    /// <summary>
    /// Initializes a new container definition. The underlying Docker container is not started
    /// until <see cref="Start"/> is called.
    /// </summary>
    public MySqlCdcBenchmarkContainer()
    {
        _container = new MySqlBuilder("mysql:8.0")
            .WithDatabase("encina_cdc_bench")
            .WithUsername("root")
            .WithPassword("mysql")
            .WithCommand(
                "--log-bin=mysql-bin",
                "--binlog-format=ROW",
                "--server-id=1")
            .WithCleanUp(true)
            .Build();
    }

    /// <summary>
    /// Gets the connection string for the running container. Only valid after <see cref="Start"/>.
    /// </summary>
    public string ConnectionString =>
        _container.GetConnectionString() + ";Allow User Variables=true";

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
