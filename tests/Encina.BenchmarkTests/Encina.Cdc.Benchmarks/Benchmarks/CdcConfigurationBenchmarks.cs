using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.Cdc.Benchmarks.Benchmarks;

/// <summary>
/// Measures the cost of wiring the Encina CDC pipeline via the fluent
/// <see cref="CdcConfiguration"/> builder. This covers both the fluent chain itself and the
/// <c>AddEncinaCdc</c> service-collection extension: both run exactly once per host startup,
/// so the numbers double as a "cold start" floor for CDC-enabled apps.
/// </summary>
[MemoryDiagnoser]
public class CdcConfigurationBenchmarks
{
    /// <summary>
    /// Baseline: construct a <see cref="CdcConfiguration"/> and drive a short fluent chain
    /// (UseCdc + table mapping + messaging bridge + cache invalidation).
    /// </summary>
    /// <returns>The configured builder.</returns>
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("DocRef:bench:cdc/config-fluent-chain")]
    public CdcConfiguration BuildConfigurationFluentChain()
    {
        var config = new CdcConfiguration();
        config.UseCdc()
              .WithTableMapping<BenchmarkOrder>("dbo.Orders")
              .WithMessagingBridge(opts => opts.TopicPattern = "cdc.{tableName}.{operation}")
              .WithCacheInvalidation();
        return config;
    }

    /// <summary>
    /// Measures <c>AddEncinaCdc</c> end-to-end: building the options + registering every
    /// service in a fresh <see cref="ServiceCollection"/>. Reflects what a host pays on startup.
    /// </summary>
    /// <returns>The service collection the CDC services were registered into.</returns>
    [Benchmark]
    [BenchmarkCategory("DocRef:bench:cdc/add-encina-cdc")]
    public IServiceCollection AddEncinaCdc_Registration()
    {
        var services = new ServiceCollection();
        services.AddEncinaCdc(config =>
        {
            config.UseCdc()
                  .WithTableMapping<BenchmarkOrder>("dbo.Orders")
                  .WithMessagingBridge(opts => opts.TopicPattern = "cdc.{tableName}.{operation}");
        });
        return services;
    }

    private sealed class BenchmarkOrder
    {
    }
}
