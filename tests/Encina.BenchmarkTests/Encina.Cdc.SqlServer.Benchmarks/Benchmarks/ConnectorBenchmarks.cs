using BenchmarkDotNet.Attributes;
using Encina.Cdc.Abstractions;
using Encina.Cdc.SqlServer.Benchmarks.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.Cdc.SqlServer.Benchmarks.Benchmarks;

/// <summary>
/// Measures the end-to-end latency of <see cref="ICdcConnector.GetCurrentPositionAsync"/>
/// against a live SQL Server 2022 container with Change Tracking enabled on a dedicated
/// database.
/// </summary>
/// <remarks>
/// The connector is resolved through <c>AddEncinaCdcSqlServer</c> so the benchmark exercises
/// the full DI-wired code path (options + position store + logger injection) rather than
/// reaching into the <c>internal</c> class directly.
/// </remarks>
[MemoryDiagnoser]
public class ConnectorBenchmarks
{
    private SqlServerCdcBenchmarkContainer _container = null!;
    private ServiceProvider _services = null!;
    private ICdcConnector _connector = null!;

    /// <summary>
    /// Boots the container, creates the Change-Tracking-enabled database, wires the
    /// connector via <c>AddEncinaCdcSqlServer</c>, and resolves <see cref="ICdcConnector"/>
    /// from the resulting service provider.
    /// </summary>
    [GlobalSetup]
    public void GlobalSetup()
    {
        _container = new SqlServerCdcBenchmarkContainer();
        _container.Start();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ICdcPositionStore, InMemoryCdcPositionStore>();
        services.AddEncinaCdcSqlServer(options =>
        {
            options.ConnectionString = _container.ConnectionString;
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
    /// Measures one <see cref="ICdcConnector.GetCurrentPositionAsync"/> call — issues
    /// <c>SELECT CHANGE_TRACKING_CURRENT_VERSION()</c> against the live SQL Server instance.
    /// </summary>
    /// <returns>The Either result carrying the current position.</returns>
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("DocRef:bench:cdc-sqlserver/get-current-position")]
    public object GetCurrentPositionAsync()
    {
        return _connector.GetCurrentPositionAsync().GetAwaiter().GetResult();
    }
}
