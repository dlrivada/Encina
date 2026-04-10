using Testcontainers.PostgreSql;

namespace Encina.Dapper.PostgreSQL.Benchmarks.Infrastructure;

/// <summary>
/// Spins up a throwaway PostgreSQL container for Dapper benchmark execution.
/// </summary>
/// <remarks>
/// Mirrors <c>tests/Encina.BenchmarkTests/Encina.ADO.PostgreSQL.Benchmarks/Infrastructure/PostgreSqlBenchmarkContainer.cs</c>
/// and <c>tests/Encina.TestInfrastructure/Fixtures/PostgreSqlFixture.cs</c> for parity with
/// the ADO PostgreSQL benchmarks and integration tests (<c>postgres:17-alpine</c>).
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
