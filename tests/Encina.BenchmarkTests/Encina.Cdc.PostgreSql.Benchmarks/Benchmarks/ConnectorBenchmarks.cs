using BenchmarkDotNet.Attributes;
using Encina.Cdc.Abstractions;
using Encina.Cdc.PostgreSql.Benchmarks.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.Cdc.PostgreSql.Benchmarks.Benchmarks;

/// <summary>
/// Measures the end-to-end latency of <see cref="ICdcConnector.GetCurrentPositionAsync"/>
/// against a live PostgreSQL 17 container configured with <c>wal_level = logical</c>.
/// </summary>
/// <remarks>
/// The connector is resolved through <c>AddEncinaCdcPostgreSql</c> so the benchmark exercises
/// the full DI-wired code path (options + position store + logger injection) rather than
/// reaching into the <c>internal</c> class directly.
/// </remarks>
[MemoryDiagnoser]
public class ConnectorBenchmarks
{
    private PostgreSqlCdcBenchmarkContainer _container = null!;
    private ServiceProvider _services = null!;
    private ICdcConnector _connector = null!;

    /// <summary>
    /// Boots the container, wires the connector via <c>AddEncinaCdcPostgreSql</c>, and
    /// resolves <see cref="ICdcConnector"/> from the resulting service provider.
    /// </summary>
    [GlobalSetup]
    public void GlobalSetup()
    {
        _container = new PostgreSqlCdcBenchmarkContainer();
        _container.Start();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ICdcPositionStore, InMemoryCdcPositionStore>();
        services.AddEncinaCdcPostgreSql(options =>
        {
            options.ConnectionString = _container.ConnectionString;
            options.PublicationName = "encina_cdc_publication";
            options.ReplicationSlotName = "encina_cdc_slot";
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
    /// <c>SELECT pg_current_wal_lsn()</c> against the live WAL-logical PostgreSQL server.
    /// </summary>
    /// <returns>The Either result carrying the current position.</returns>
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("DocRef:bench:cdc-postgresql/get-current-position")]
    public object GetCurrentPositionAsync()
    {
        return _connector.GetCurrentPositionAsync().GetAwaiter().GetResult();
    }
}
