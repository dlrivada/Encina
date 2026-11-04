using Testcontainers.MySql;

namespace Encina.Cdc.MySql.Benchmarks.Infrastructure;

/// <summary>
/// Spins up a MySQL container with binary logging enabled so the CDC
/// <c>GetCurrentPositionAsync</c> benchmark can actually read binlog state via
/// <c>SHOW BINARY LOG STATUS</c>.
/// </summary>
/// <remarks>
/// Uses the repository's 9.x MySQL image. The Encina MySQL CDC connector queries
/// <c>SHOW BINARY LOG STATUS</c>, the statement that replaced <c>SHOW MASTER STATUS</c>,
/// which was removed in MySQL 8.4 (MySQL 8.4 Reference Manual). Enables <c>--log-bin</c>
/// explicitly so we do not depend on the image's default binlog configuration.
/// </remarks>
public sealed class MySqlCdcBenchmarkContainer : IAsyncDisposable
{
    // Same 9.x tag as MySqlFixture (tests/Encina.TestInfrastructure/Fixtures/MySqlFixture.cs).
    // This project does not reference Encina.TestInfrastructure; keep both in step.
    private const string MySqlImage = "mysql:9.1";

    private readonly MySqlContainer _container;

    /// <summary>
    /// Initializes a new container definition. The underlying Docker container is not started
    /// until <see cref="Start"/> is called.
    /// </summary>
    public MySqlCdcBenchmarkContainer()
    {
        _container = new MySqlBuilder(MySqlImage)
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
