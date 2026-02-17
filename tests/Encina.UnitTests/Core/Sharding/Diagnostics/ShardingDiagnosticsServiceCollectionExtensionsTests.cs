using Encina.Sharding;
using Encina.Sharding.Configuration;
using Encina.Sharding.Diagnostics;
using Encina.Sharding.Health;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Encina.UnitTests.Core.Sharding.Diagnostics;

/// <summary>
/// Unit tests for <see cref="ShardingDiagnosticsServiceCollectionExtensions"/>.
/// </summary>
public sealed class ShardingDiagnosticsServiceCollectionExtensionsTests
{
    private static ServiceCollection CreateServicesWithSharding()
    {
        var services = new ServiceCollection();
        services.AddEncinaSharding<TestEntity>(opts =>
        {
            opts.AddShard("shard-1", "conn1")
                .AddShard("shard-2", "conn2")
                .UseHashRouting();
        });
        return services;
    }

    // ────────────────────────────────────────────────────────────
    //  AddEncinaShardingMetrics — null guard
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void AddEncinaShardingMetrics_NullServices_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddEncinaShardingMetrics());
    }

    // ────────────────────────────────────────────────────────────
    //  AddEncinaShardingMetrics — registration
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void AddEncinaShardingMetrics_RegistersShardRoutingMetrics()
    {
        // Arrange
        var services = CreateServicesWithSharding();

        // Act
        services.AddEncinaShardingMetrics();

        // Assert
        var provider = services.BuildServiceProvider();
        var metrics = provider.GetService<ShardRoutingMetrics>();
        metrics.ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaShardingMetrics_DecoratesIShardRouterWithInstrumented()
    {
        // Arrange
        var services = CreateServicesWithSharding();

        // Act
        services.AddEncinaShardingMetrics();

        // Assert
        var provider = services.BuildServiceProvider();
        var router = provider.GetRequiredService<IShardRouter>();
        router.ShouldBeOfType<InstrumentedShardRouter>();
    }

    [Fact]
    public void AddEncinaShardingMetrics_RegistersShardingMetricsOptions()
    {
        // Arrange
        var services = CreateServicesWithSharding();

        // Act
        services.AddEncinaShardingMetrics(opts =>
        {
            opts.HealthCheckInterval = TimeSpan.FromMinutes(2);
            opts.EnableTracing = false;
        });

        // Assert
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<ShardingMetricsOptions>>().Value;
        options.HealthCheckInterval.ShouldBe(TimeSpan.FromMinutes(2));
        options.EnableTracing.ShouldBeFalse();
    }

    [Fact]
    public void AddEncinaShardingMetrics_DisableRoutingMetrics_DoesNotDecorateRouter()
    {
        // Arrange
        var services = CreateServicesWithSharding();

        // Act
        services.AddEncinaShardingMetrics(opts =>
        {
            opts.EnableRoutingMetrics = false;
            opts.EnableScatterGatherMetrics = false;
        });

        // Assert
        var provider = services.BuildServiceProvider();
        var router = provider.GetRequiredService<IShardRouter>();
        router.ShouldNotBeOfType<InstrumentedShardRouter>();
    }

    [Fact]
    public void AddEncinaShardingMetrics_ReturnsSameServiceCollection()
    {
        // Arrange
        var services = CreateServicesWithSharding();

        // Act
        var result = services.AddEncinaShardingMetrics();

        // Assert
        result.ShouldBeSameAs(services);
    }

    // ────────────────────────────────────────────────────────────
    //  AddEncinaShardingHealthMetrics
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void AddEncinaShardingHealthMetrics_NullServices_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddEncinaShardingHealthMetrics());
    }

    [Fact]
    public void AddEncinaShardingHealthMetrics_ReturnsSameServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddEncinaShardingHealthMetrics();

        // Assert
        result.ShouldBeSameAs(services);
    }

    // ────────────────────────────────────────────────────────────
    //  Test entity
    // ────────────────────────────────────────────────────────────

    private sealed class TestEntity : IShardable
    {
        public string Key { get; set; } = default!;
        public string GetShardKey() => Key;
    }
}
