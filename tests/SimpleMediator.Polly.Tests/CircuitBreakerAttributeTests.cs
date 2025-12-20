namespace SimpleMediator.Polly.Tests;

/// <summary>
/// Unit tests for <see cref="CircuitBreakerAttribute"/>.
/// Verifies default values and property initialization.
/// </summary>
public class CircuitBreakerAttributeTests
{
    [Fact]
    public void Constructor_ShouldInitializeDefaults()
    {
        // Act
        var attribute = new CircuitBreakerAttribute();

        // Assert
        attribute.FailureThreshold.Should().Be(5);
        attribute.SamplingDurationSeconds.Should().Be(60);
        attribute.MinimumThroughput.Should().Be(10);
        attribute.DurationOfBreakSeconds.Should().Be(30);
        attribute.FailureRateThreshold.Should().Be(0.5);
    }

    [Fact]
    public void FailureThreshold_ShouldDefault5()
    {
        // Act
        var attribute = new CircuitBreakerAttribute();

        // Assert
        attribute.FailureThreshold.Should().Be(5, "default should be 5 consecutive failures");
    }

    [Fact]
    public void SamplingDurationSeconds_ShouldDefault60()
    {
        // Act
        var attribute = new CircuitBreakerAttribute();

        // Assert
        attribute.SamplingDurationSeconds.Should().Be(60, "default should be 60 seconds sampling window");
    }

    [Fact]
    public void MinimumThroughput_ShouldDefault10()
    {
        // Act
        var attribute = new CircuitBreakerAttribute();

        // Assert
        attribute.MinimumThroughput.Should().Be(10, "default should be 10 requests minimum");
    }

    [Fact]
    public void DurationOfBreakSeconds_ShouldDefault30()
    {
        // Act
        var attribute = new CircuitBreakerAttribute();

        // Assert
        attribute.DurationOfBreakSeconds.Should().Be(30, "default should be 30 seconds break duration");
    }

    [Fact]
    public void FailureRateThreshold_ShouldDefault0Point5()
    {
        // Act
        var attribute = new CircuitBreakerAttribute();

        // Assert
        attribute.FailureRateThreshold.Should().Be(0.5, "default should be 50% failure rate (0.5)");
    }

    [Fact]
    public void Constructor_WithCustomValues_ShouldSetProperties()
    {
        // Act
        var attribute = new CircuitBreakerAttribute
        {
            FailureThreshold = 10,
            SamplingDurationSeconds = 120,
            MinimumThroughput = 20,
            DurationOfBreakSeconds = 60,
            FailureRateThreshold = 0.75
        };

        // Assert
        attribute.FailureThreshold.Should().Be(10);
        attribute.SamplingDurationSeconds.Should().Be(120);
        attribute.MinimumThroughput.Should().Be(20);
        attribute.DurationOfBreakSeconds.Should().Be(60);
        attribute.FailureRateThreshold.Should().Be(0.75);
    }

    [Fact]
    public void Attribute_CanBeAppliedToClass()
    {
        // Arrange
        var requestType = typeof(TestCircuitBreakerRequest);

        // Act
        var attribute = requestType.GetCustomAttributes(typeof(CircuitBreakerAttribute), false)
            .FirstOrDefault() as CircuitBreakerAttribute;

        // Assert
        attribute.Should().NotBeNull("CircuitBreakerAttribute should be applicable to classes");
        attribute!.FailureThreshold.Should().Be(3);
        attribute.DurationOfBreakSeconds.Should().Be(15);
    }

    [Fact]
    public void Attribute_AllowMultipleFalse_ShouldPreventDuplicates()
    {
        // Arrange
        var attributeUsage = typeof(CircuitBreakerAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .FirstOrDefault() as AttributeUsageAttribute;

        // Assert
        attributeUsage.Should().NotBeNull();
        attributeUsage!.AllowMultiple.Should().BeFalse("only one CircuitBreakerAttribute should be allowed per class");
    }

    [Fact]
    public void Attribute_InheritedTrue_ShouldInheritFromBaseClass()
    {
        // Arrange
        var attributeUsage = typeof(CircuitBreakerAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .FirstOrDefault() as AttributeUsageAttribute;

        // Assert
        attributeUsage.Should().NotBeNull();
        attributeUsage!.Inherited.Should().BeTrue("CircuitBreakerAttribute should be inherited by derived classes");
    }

    [Fact]
    public void Attribute_TargetsShouldBeClass()
    {
        // Arrange
        var attributeUsage = typeof(CircuitBreakerAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .FirstOrDefault() as AttributeUsageAttribute;

        // Assert
        attributeUsage.Should().NotBeNull();
        attributeUsage!.ValidOn.Should().Be(AttributeTargets.Class, "should only be applicable to classes");
    }

    [CircuitBreakerAttribute(FailureThreshold = 3, DurationOfBreakSeconds = 15)]
    private sealed record TestCircuitBreakerRequest : IRequest<string>;
}
