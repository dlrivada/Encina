using Encina.Messaging.Health;
using Shouldly;

namespace Encina.Messaging.Tests.Health;

/// <summary>
/// Unit tests for <see cref="ProviderHealthCheckOptions"/>.
/// </summary>
public sealed class ProviderHealthCheckOptionsTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var options = new ProviderHealthCheckOptions();

        // Assert
        options.Enabled.ShouldBeTrue();
        options.Timeout.ShouldBe(TimeSpan.FromSeconds(5));
        options.Name.ShouldBeNull();
        options.Tags.ShouldContain("encina");
        options.Tags.ShouldContain("database");
        options.Tags.ShouldContain("ready");
        options.Tags.Count.ShouldBe(3);
        options.FailureStatus.ShouldBe(HealthStatus.Unhealthy);
    }

    [Fact]
    public void CanSetEnabled()
    {
        // Arrange & Act
        var options = new ProviderHealthCheckOptions { Enabled = false };

        // Assert
        options.Enabled.ShouldBeFalse();
    }

    [Fact]
    public void CanSetTimeout()
    {
        // Arrange & Act
        var options = new ProviderHealthCheckOptions { Timeout = TimeSpan.FromSeconds(10) };

        // Assert
        options.Timeout.ShouldBe(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void CanSetName()
    {
        // Arrange & Act
        var options = new ProviderHealthCheckOptions { Name = "custom-health-check" };

        // Assert
        options.Name.ShouldBe("custom-health-check");
    }

    [Fact]
    public void CanSetTags()
    {
        // Arrange & Act
        var options = new ProviderHealthCheckOptions { Tags = ["critical", "messaging"] };

        // Assert
        options.Tags.ShouldContain("critical");
        options.Tags.ShouldContain("messaging");
        options.Tags.Count.ShouldBe(2);
    }

    [Fact]
    public void CanSetFailureStatus_ToDegraded()
    {
        // Arrange & Act
        var options = new ProviderHealthCheckOptions { FailureStatus = HealthStatus.Degraded };

        // Assert
        options.FailureStatus.ShouldBe(HealthStatus.Degraded);
    }
}
