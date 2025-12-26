namespace Encina.Polly.Tests;

/// <summary>
/// Unit tests for <see cref="RateLimitAttribute"/>.
/// Verifies default values and property initialization.
/// </summary>
public class RateLimitAttributeTests
{
    [Fact]
    public void Constructor_ShouldInitializeDefaults()
    {
        // Act
        var attribute = new RateLimitAttribute();

        // Assert
        attribute.MaxRequestsPerWindow.Should().Be(100);
        attribute.WindowSizeSeconds.Should().Be(60);
        attribute.ErrorThresholdPercent.Should().Be(50.0);
        attribute.CooldownSeconds.Should().Be(60);
        attribute.RampUpFactor.Should().Be(2.0);
        attribute.EnableAdaptiveThrottling.Should().BeTrue();
        attribute.MinimumThroughputForThrottling.Should().Be(10);
    }

    [Fact]
    public void MaxRequestsPerWindow_ShouldDefault100()
    {
        // Act
        var attribute = new RateLimitAttribute();

        // Assert
        attribute.MaxRequestsPerWindow.Should().Be(100, "default should be 100 requests per window");
    }

    [Fact]
    public void WindowSizeSeconds_ShouldDefault60()
    {
        // Act
        var attribute = new RateLimitAttribute();

        // Assert
        attribute.WindowSizeSeconds.Should().Be(60, "default should be 60 seconds (1 minute)");
    }

    [Fact]
    public void ErrorThresholdPercent_ShouldDefault50()
    {
        // Act
        var attribute = new RateLimitAttribute();

        // Assert
        attribute.ErrorThresholdPercent.Should().Be(50.0, "default should be 50% error rate to trigger throttling");
    }

    [Fact]
    public void CooldownSeconds_ShouldDefault60()
    {
        // Act
        var attribute = new RateLimitAttribute();

        // Assert
        attribute.CooldownSeconds.Should().Be(60, "default should be 60 seconds cooldown");
    }

    [Fact]
    public void RampUpFactor_ShouldDefault2()
    {
        // Act
        var attribute = new RateLimitAttribute();

        // Assert
        attribute.RampUpFactor.Should().Be(2.0, "default should be 2.0x capacity increase on recovery");
    }

    [Fact]
    public void EnableAdaptiveThrottling_ShouldDefaultTrue()
    {
        // Act
        var attribute = new RateLimitAttribute();

        // Assert
        attribute.EnableAdaptiveThrottling.Should().BeTrue("default should enable adaptive throttling");
    }

    [Fact]
    public void MinimumThroughputForThrottling_ShouldDefault10()
    {
        // Act
        var attribute = new RateLimitAttribute();

        // Assert
        attribute.MinimumThroughputForThrottling.Should().Be(10, "default should require 10 requests before calculating error rate");
    }

    [Fact]
    public void Constructor_WithCustomValues_ShouldSetProperties()
    {
        // Act
        var attribute = new RateLimitAttribute
        {
            MaxRequestsPerWindow = 50,
            WindowSizeSeconds = 30,
            ErrorThresholdPercent = 25.0,
            CooldownSeconds = 120,
            RampUpFactor = 1.5,
            EnableAdaptiveThrottling = false,
            MinimumThroughputForThrottling = 20
        };

        // Assert
        attribute.MaxRequestsPerWindow.Should().Be(50);
        attribute.WindowSizeSeconds.Should().Be(30);
        attribute.ErrorThresholdPercent.Should().Be(25.0);
        attribute.CooldownSeconds.Should().Be(120);
        attribute.RampUpFactor.Should().Be(1.5);
        attribute.EnableAdaptiveThrottling.Should().BeFalse();
        attribute.MinimumThroughputForThrottling.Should().Be(20);
    }

    [Fact]
    public void Attribute_CanBeAppliedToClass()
    {
        // Arrange
        var requestType = typeof(TestRateLimitRequest);

        // Act
        var attribute = requestType.GetCustomAttributes(typeof(RateLimitAttribute), false).FirstOrDefault() as RateLimitAttribute;

        // Assert
        attribute.Should().NotBeNull("RateLimitAttribute should be applicable to classes");
        attribute!.MaxRequestsPerWindow.Should().Be(50);
        attribute.WindowSizeSeconds.Should().Be(30);
    }

    [Fact]
    public void Attribute_AllowMultipleFalse_ShouldPreventDuplicates()
    {
        // Arrange
        var attributeUsage = typeof(RateLimitAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .FirstOrDefault() as AttributeUsageAttribute;

        // Assert
        attributeUsage.Should().NotBeNull();
        attributeUsage!.AllowMultiple.Should().BeFalse("only one RateLimitAttribute should be allowed per class");
    }

    [Fact]
    public void Attribute_InheritedTrue_ShouldInheritFromBaseClass()
    {
        // Arrange
        var attributeUsage = typeof(RateLimitAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .FirstOrDefault() as AttributeUsageAttribute;

        // Assert
        attributeUsage.Should().NotBeNull();
        attributeUsage!.Inherited.Should().BeTrue("RateLimitAttribute should be inherited by derived classes");
    }

    [RateLimitAttribute(MaxRequestsPerWindow = 50, WindowSizeSeconds = 30)]
    private sealed record TestRateLimitRequest : IRequest<string>;
}
