using Encina.OpenTelemetry.Resharding;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.OpenTelemetry.Resharding;

/// <summary>
/// Unit tests for <see cref="ReshardingHealthCheckOptions"/>.
/// </summary>
public sealed class ReshardingHealthCheckOptionsTests
{
    [Fact]
    public void Constructor_MaxReshardingDuration_DefaultsToTwoHours()
    {
        var options = new ReshardingHealthCheckOptions();
        options.MaxReshardingDuration.ShouldBe(TimeSpan.FromHours(2));
    }

    [Fact]
    public void Constructor_Timeout_DefaultsToThirtySeconds()
    {
        var options = new ReshardingHealthCheckOptions();
        options.Timeout.ShouldBe(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void MaxReshardingDuration_ShouldBeSettable()
    {
        var options = new ReshardingHealthCheckOptions
        {
            MaxReshardingDuration = TimeSpan.FromHours(4)
        };
        options.MaxReshardingDuration.ShouldBe(TimeSpan.FromHours(4));
    }

    [Fact]
    public void Timeout_ShouldBeSettable()
    {
        var options = new ReshardingHealthCheckOptions
        {
            Timeout = TimeSpan.FromSeconds(15)
        };
        options.Timeout.ShouldBe(TimeSpan.FromSeconds(15));
    }
}
