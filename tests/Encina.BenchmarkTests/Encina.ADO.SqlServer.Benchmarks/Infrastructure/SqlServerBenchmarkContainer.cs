using Testcontainers.MsSql;

namespace Encina.ADO.SqlServer.Benchmarks.Infrastructure;

/// <summary>
/// Spins up a throwaway SQL Server container for benchmark execution.
/// </summary>
/// <remarks>
/// BenchmarkDotNet's <c>[GlobalSetup]</c> and <c>[GlobalCleanup]</c> hooks are synchronous,
/// so this helper exposes blocking <see cref="Start"/> / <see cref="Stop"/> wrappers around
/// Testcontainers' async API. The image mirrors
/// <c>tests/Encina.TestInfrastructure/Fixtures/SqlServerFixture.cs</c> for parity with
/// integration tests (<c>mcr.microsoft.com/mssql/server:2022-latest</c>).
/// </remarks>
public sealed class SqlServerBenchmarkContainer : IAsyncDisposable
{
    private readonly MsSqlContainer _container;

    /// <summary>
    /// Initializes a new container definition. The underlying Docker container is not started
    /// until <see cref="Start"/> is called.
    /// </summary>
    public SqlServerBenchmarkContainer()
    {
        _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPassword("StrongP@ssw0rd!")
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
