using Testcontainers.PostgreSql;

namespace Encina.ADO.PostgreSQL.Benchmarks.Infrastructure;

/// <summary>
/// Spins up a throwaway PostgreSQL container for benchmark execution.
/// </summary>
/// <remarks>
/// BenchmarkDotNet's <c>[GlobalSetup]</c> and <c>[GlobalCleanup]</c> hooks are synchronous,
/// so this helper exposes blocking <see cref="Start"/> / <see cref="Stop"/> wrappers around
/// Testcontainers' async API. The image mirrors
/// <c>tests/Encina.TestInfrastructure/Fixtures/PostgreSqlFixture.cs</c> for parity with
/// integration tests (<c>postgres:17-alpine</c>).
/// </remarks>
public sealed class PostgreSqlBenchmarkContainer : IAsyncDisposable
{
    private readonly PostgreSqlContainer _container;

    /// <summary>
    /// Initializes a new container definition. The underlying Docker container is not started
    /// until <see cref="Start"/> is called.
    /// </summary>
    public PostgreSqlBenchmarkContainer()
    {
        _container = new PostgreSqlBuilder("postgres:17-alpine")
            .WithDatabase("encina_bench")
            .WithUsername("postgres")
            .WithPassword("postgres")
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
