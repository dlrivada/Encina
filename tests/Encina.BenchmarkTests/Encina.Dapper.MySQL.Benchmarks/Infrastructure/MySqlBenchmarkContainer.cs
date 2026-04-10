using Testcontainers.MySql;

namespace Encina.Dapper.MySQL.Benchmarks.Infrastructure;

/// <summary>
/// Spins up a throwaway MySQL container for Dapper benchmark execution.
/// </summary>
/// <remarks>
/// Mirrors <c>tests/Encina.BenchmarkTests/Encina.ADO.MySQL.Benchmarks/Infrastructure/MySqlBenchmarkContainer.cs</c>
/// and <c>tests/Encina.TestInfrastructure/Fixtures/MySqlFixture.cs</c> for parity with
/// the ADO MySQL benchmarks and integration tests:
/// <list type="bullet">
///   <item><description><c>mysql:9.1</c> image</description></item>
///   <item><description><c>--local-infile=1</c> to enable <c>MySqlBulkLoader</c> / <c>MySqlBulkCopy</c></description></item>
///   <item><description><c>Allow User Variables=true</c> appended to the connection string</description></item>
/// </list>
/// </remarks>
public sealed class MySqlBenchmarkContainer : IAsyncDisposable
{
    private readonly MySqlContainer _container;

    /// <summary>
    /// Initializes a new container definition. The underlying Docker container is not started
    /// until <see cref="Start"/> is called.
    /// </summary>
    public MySqlBenchmarkContainer()
    {
        _container = new MySqlBuilder("mysql:9.1")
            .WithDatabase("encina_bench")
            .WithUsername("root")
            .WithPassword("mysql")
            .WithCommand("--local-infile=1")
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
