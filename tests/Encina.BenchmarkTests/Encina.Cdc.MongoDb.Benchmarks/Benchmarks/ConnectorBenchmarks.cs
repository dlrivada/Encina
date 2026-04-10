using BenchmarkDotNet.Attributes;
using Encina.Cdc.Abstractions;
using Encina.Cdc.MongoDb.Benchmarks.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.Cdc.MongoDb.Benchmarks.Benchmarks;

/// <summary>
/// Measures the end-to-end latency of <see cref="ICdcConnector.GetCurrentPositionAsync"/>
/// against a live MongoDB container. The connector path for this operation is just a
/// <c>ping</c> command plus a lookup against the in-memory position store — it does not
/// require a replica set, unlike the full <c>StreamChangesAsync</c> path.
/// </summary>
/// <remarks>
/// The connector is resolved through <c>AddEncinaCdcMongoDb</c> so the benchmark exercises
/// the full DI-wired code path (options + position store + logger injection).
/// </remarks>
[MemoryDiagnoser]
public class ConnectorBenchmarks
{
    private MongoDbCdcBenchmarkContainer _container = null!;
    private ServiceProvider _services = null!;
    private ICdcConnector _connector = null!;

    /// <summary>
    /// Boots the container, wires the connector via <c>AddEncinaCdcMongoDb</c>, and
    /// resolves <see cref="ICdcConnector"/> from the resulting service provider.
    /// </summary>
    [GlobalSetup]
    public void GlobalSetup()
    {
        _container = new MongoDbCdcBenchmarkContainer();
        _container.Start();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ICdcPositionStore, InMemoryCdcPositionStore>();
        services.AddEncinaCdcMongoDb(options =>
        {
            options.ConnectionString = _container.ConnectionString;
            options.DatabaseName = "encina_cdc_bench";
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
    /// Measures one <see cref="ICdcConnector.GetCurrentPositionAsync"/> call — a ping
    /// round-trip to MongoDB plus a position-store lookup.
    /// </summary>
    /// <returns>The Either result carrying the current position.</returns>
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("DocRef:bench:cdc-mongodb/get-current-position")]
    public object GetCurrentPositionAsync()
    {
        return _connector.GetCurrentPositionAsync().GetAwaiter().GetResult();
    }
}
