using Encina.Sharding.Diagnostics;

namespace Encina.UnitTests.Core.Sharding.Diagnostics;

/// <summary>
/// Unit tests for <see cref="ShardingMetricsOptions"/>.
/// </summary>
public sealed class ShardingMetricsOptionsTests
{
    [Fact]
    public void HealthCheckInterval_DefaultIs30Seconds()
    {
        var options = new ShardingMetricsOptions();
        options.HealthCheckInterval.ShouldBe(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void EnableRoutingMetrics_DefaultIsTrue()
    {
        var options = new ShardingMetricsOptions();
        options.EnableRoutingMetrics.ShouldBeTrue();
    }

    [Fact]
    public void EnableScatterGatherMetrics_DefaultIsTrue()
    {
        var options = new ShardingMetricsOptions();
        options.EnableScatterGatherMetrics.ShouldBeTrue();
    }

    [Fact]
    public void EnableHealthMetrics_DefaultIsTrue()
    {
        var options = new ShardingMetricsOptions();
        options.EnableHealthMetrics.ShouldBeTrue();
    }

    [Fact]
    public void EnableTracing_DefaultIsTrue()
    {
        var options = new ShardingMetricsOptions();
        options.EnableTracing.ShouldBeTrue();
    }

    [Fact]
    public void EnableAggregationMetrics_DefaultIsTrue()
    {
        var options = new ShardingMetricsOptions();
        options.EnableAggregationMetrics.ShouldBeTrue();
    }

    [Fact]
    public void Properties_CanBeModified()
    {
        var options = new ShardingMetricsOptions
        {
            HealthCheckInterval = TimeSpan.FromMinutes(1),
            EnableRoutingMetrics = false,
            EnableScatterGatherMetrics = false,
            EnableHealthMetrics = false,
            EnableAggregationMetrics = false,
            EnableTracing = false
        };

        options.HealthCheckInterval.ShouldBe(TimeSpan.FromMinutes(1));
        options.EnableRoutingMetrics.ShouldBeFalse();
        options.EnableScatterGatherMetrics.ShouldBeFalse();
        options.EnableHealthMetrics.ShouldBeFalse();
        options.EnableAggregationMetrics.ShouldBeFalse();
        options.EnableTracing.ShouldBeFalse();
    }
}
