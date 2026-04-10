using BenchmarkDotNet.Attributes;
using Encina.Cdc.Abstractions;
using Encina.Cdc.MySql.Benchmarks.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.Cdc.MySql.Benchmarks.Benchmarks;

/// <summary>
/// Measures the end-to-end latency of <see cref="ICdcConnector.GetCurrentPositionAsync"/>
/// against a live MySQL 8 container with binary logging enabled. This is the only operation
/// that is cheap enough to benchmark meaningfully for a CDC connector — streaming the change
/// feed is infinite by design and would require injecting a write workload.
/// </summary>
/// <remarks>
/// The connector is resolved through <c>AddEncinaCdcMySql</c> so the benchmark exercises the
/// full DI-wired code path (options + position store + logger injection) rather than reaching
/// into the <c>internal</c> class directly.
/// </remarks>
[MemoryDiagnoser]
public class ConnectorBenchmarks
{
    private MySqlCdcBenchmarkContainer _container = null!;
    private ServiceProvider _services = null!;
    private ICdcConnector _connector = null!;

    /// <summary>
    /// Boots the container, wires the connector via <c>AddEncinaCdcMySql</c>, and resolves
    /// <see cref="ICdcConnector"/> from the resulting service provider.
    /// </summary>
    [GlobalSetup]
    public void GlobalSetup()
    {
        _container = new MySqlCdcBenchmarkContainer();
        _container.Start();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ICdcPositionStore, InMemoryCdcPositionStore>();
        services.AddEncinaCdcMySql(options =>
        {
            options.ConnectionString = _container.ConnectionString;
            options.Hostname = "localhost";
            options.Port = 3306;
            options.Username = "root";
            options.Password = "mysql";
            options.ServerId = 1;
        });

        _services = services.BuildServiceProvider();
        _connector = _services.GetRequiredService<ICdcConnector>();
    }

    /// <summary>
    /// Disposes the service provider and stops the container.
    /// </summary>
    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _services?.Dispose();
        _container?.Stop();
    }

    /// <summary>
    /// Measures one <see cref="ICdcConnector.GetCurrentPositionAsync"/> call — issues a
    /// <c>SHOW MASTER STATUS</c> query against the live binlog-enabled MySQL server.
    /// </summary>
    /// <returns>The Either result carrying the current position.</returns>
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("DocRef:bench:cdc-mysql/get-current-position")]
    public object GetCurrentPositionAsync()
    {
        return _connector.GetCurrentPositionAsync().GetAwaiter().GetResult();
    }
}
