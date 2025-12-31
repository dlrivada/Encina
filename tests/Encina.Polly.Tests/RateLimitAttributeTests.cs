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
        attribute.MaxRequestsPerWindow.ShouldBe(100);
        attribute.WindowSizeSeconds.ShouldBe(60);
        attribute.ErrorThresholdPercent.ShouldBe(50.0);
        attribute.CooldownSeconds.ShouldBe(60);
        attribute.RampUpFactor.ShouldBe(2.0);
        attribute.EnableAdaptiveThrottling.ShouldBeTrue();
        attribute.MinimumThroughputForThrottling.ShouldBe(10);
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
        attribute.MaxRequestsPerWindow.ShouldBe(50);
        attribute.WindowSizeSeconds.ShouldBe(30);
        attribute.ErrorThresholdPercent.ShouldBe(25.0);
        attribute.CooldownSeconds.ShouldBe(120);
        attribute.RampUpFactor.ShouldBe(1.5);
        attribute.EnableAdaptiveThrottling.ShouldBeFalse();
        attribute.MinimumThroughputForThrottling.ShouldBe(20);
    }

    [Fact]
    public void Attribute_CanBeAppliedToClass()
    {
        // Arrange
        var requestType = typeof(TestRateLimitRequest);

        // Act
        var attribute = requestType.GetCustomAttributes(typeof(RateLimitAttribute), false).FirstOrDefault() as RateLimitAttribute;

        // Assert
        attribute.ShouldNotBeNull("RateLimitAttribute should be applicable to classes");
        attribute!.MaxRequestsPerWindow.ShouldBe(50);
        attribute.WindowSizeSeconds.ShouldBe(30);
    }

    [Fact]
    public void Attribute_AllowMultipleFalse_ShouldPreventDuplicates()
    {
        // Arrange
        var attributeUsage = typeof(RateLimitAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .FirstOrDefault() as AttributeUsageAttribute;

        // Assert
        attributeUsage.ShouldNotBeNull();
        attributeUsage!.AllowMultiple.ShouldBeFalse("only one RateLimitAttribute should be allowed per class");
    }

    [Fact]
    public void Attribute_InheritedTrue_ShouldInheritFromBaseClass()
    {
        // Arrange
        var attributeUsage = typeof(RateLimitAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .FirstOrDefault() as AttributeUsageAttribute;

        // Assert
        attributeUsage.ShouldNotBeNull();
        attributeUsage!.Inherited.ShouldBeTrue("RateLimitAttribute should be inherited by derived classes");
    }

    [RateLimitAttribute(MaxRequestsPerWindow = 50, WindowSizeSeconds = 30)]
    private sealed record TestRateLimitRequest : IRequest<string>;
}
