using Encina.Messaging.RoutingSlip;
using Shouldly;

namespace Encina.Messaging.Tests.RoutingSlip;

/// <summary>
/// Unit tests for <see cref="RoutingSlipOptions"/>.
/// </summary>
public sealed class RoutingSlipOptionsTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var options = new RoutingSlipOptions();

        // Assert
        options.DefaultTimeout.ShouldBe(TimeSpan.FromMinutes(30));
        options.StuckCheckInterval.ShouldBe(TimeSpan.FromMinutes(5));
        options.StuckThreshold.ShouldBe(TimeSpan.FromMinutes(10));
        options.BatchSize.ShouldBe(100);
        options.ContinueCompensationOnFailure.ShouldBeTrue();
    }

    [Fact]
    public void CanSetDefaultTimeout()
    {
        // Arrange & Act
        var options = new RoutingSlipOptions { DefaultTimeout = TimeSpan.FromHours(1) };

        // Assert
        options.DefaultTimeout.ShouldBe(TimeSpan.FromHours(1));
    }

    [Fact]
    public void CanSetStuckCheckInterval()
    {
        // Arrange & Act
        var options = new RoutingSlipOptions { StuckCheckInterval = TimeSpan.FromMinutes(10) };

        // Assert
        options.StuckCheckInterval.ShouldBe(TimeSpan.FromMinutes(10));
    }

    [Fact]
    public void CanSetStuckThreshold()
    {
        // Arrange & Act
        var options = new RoutingSlipOptions { StuckThreshold = TimeSpan.FromMinutes(15) };

        // Assert
        options.StuckThreshold.ShouldBe(TimeSpan.FromMinutes(15));
    }

    [Fact]
    public void CanSetBatchSize()
    {
        // Arrange & Act
        var options = new RoutingSlipOptions { BatchSize = 50 };

        // Assert
        options.BatchSize.ShouldBe(50);
    }

    [Fact]
    public void CanSetContinueCompensationOnFailure()
    {
        // Arrange & Act
        var options = new RoutingSlipOptions { ContinueCompensationOnFailure = false };

        // Assert
        options.ContinueCompensationOnFailure.ShouldBeFalse();
    }

    [Fact]
    public void CanSetAllProperties()
    {
        // Arrange & Act
        var options = new RoutingSlipOptions
        {
            DefaultTimeout = TimeSpan.FromHours(2),
            StuckCheckInterval = TimeSpan.FromMinutes(1),
            StuckThreshold = TimeSpan.FromMinutes(5),
            BatchSize = 200,
            ContinueCompensationOnFailure = false
        };

        // Assert
        options.DefaultTimeout.ShouldBe(TimeSpan.FromHours(2));
        options.StuckCheckInterval.ShouldBe(TimeSpan.FromMinutes(1));
        options.StuckThreshold.ShouldBe(TimeSpan.FromMinutes(5));
        options.BatchSize.ShouldBe(200);
        options.ContinueCompensationOnFailure.ShouldBeFalse();
    }
}
