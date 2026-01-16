using Encina.Polly;
using Encina.Testing;
namespace Encina.UnitTests.Polly;

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
        attribute.FailureThreshold.ShouldBe(5);
        attribute.SamplingDurationSeconds.ShouldBe(60);
        attribute.MinimumThroughput.ShouldBe(10);
        attribute.DurationOfBreakSeconds.ShouldBe(30);
        attribute.FailureRateThreshold.ShouldBe(0.5);
    }

    [Fact]
    public void FailureThreshold_ShouldDefault5()
    {
        // Act
        var attribute = new CircuitBreakerAttribute();

        // Assert
        attribute.FailureThreshold.ShouldBe(5, "default should be 5 consecutive failures");
    }

    [Fact]
    public void SamplingDurationSeconds_ShouldDefault60()
    {
        // Act
        var attribute = new CircuitBreakerAttribute();

        // Assert
        attribute.SamplingDurationSeconds.ShouldBe(60, "default should be 60 seconds sampling window");
    }

    [Fact]
    public void MinimumThroughput_ShouldDefault10()
    {
        // Act
        var attribute = new CircuitBreakerAttribute();

        // Assert
        attribute.MinimumThroughput.ShouldBe(10, "default should be 10 requests minimum");
    }

    [Fact]
    public void DurationOfBreakSeconds_ShouldDefault30()
    {
        // Act
        var attribute = new CircuitBreakerAttribute();

        // Assert
        attribute.DurationOfBreakSeconds.ShouldBe(30, "default should be 30 seconds break duration");
    }

    [Fact]
    public void FailureRateThreshold_ShouldDefault0Point5()
    {
        // Act
        var attribute = new CircuitBreakerAttribute();

        // Assert
        attribute.FailureRateThreshold.ShouldBe(0.5, "default should be 50% failure rate (0.5)");
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
        attribute.FailureThreshold.ShouldBe(10);
        attribute.SamplingDurationSeconds.ShouldBe(120);
        attribute.MinimumThroughput.ShouldBe(20);
        attribute.DurationOfBreakSeconds.ShouldBe(60);
        attribute.FailureRateThreshold.ShouldBe(0.75);
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
        attribute.ShouldNotBeNull("CircuitBreakerAttribute should be applicable to classes");
        attribute!.FailureThreshold.ShouldBe(3);
        attribute.DurationOfBreakSeconds.ShouldBe(15);
    }

    [Fact]
    public void Attribute_AllowMultipleFalse_ShouldPreventDuplicates()
    {
        // Arrange
        var attributeUsage = typeof(CircuitBreakerAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .FirstOrDefault() as AttributeUsageAttribute;

        // Assert
        attributeUsage.ShouldNotBeNull();
        attributeUsage!.AllowMultiple.ShouldBeFalse("only one CircuitBreakerAttribute should be allowed per class");
    }

    [Fact]
    public void Attribute_InheritedTrue_ShouldInheritFromBaseClass()
    {
        // Arrange
        var attributeUsage = typeof(CircuitBreakerAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .FirstOrDefault() as AttributeUsageAttribute;

        // Assert
        attributeUsage.ShouldNotBeNull();
        attributeUsage!.Inherited.ShouldBeTrue("CircuitBreakerAttribute should be inherited by derived classes");
    }

    [Fact]
    public void Attribute_TargetsShouldBeClass()
    {
        // Arrange
        var attributeUsage = typeof(CircuitBreakerAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .FirstOrDefault() as AttributeUsageAttribute;

        // Assert
        attributeUsage.ShouldNotBeNull();
        attributeUsage!.ValidOn.ShouldBe(AttributeTargets.Class, "should only be applicable to classes");
    }

    #region Validation Tests

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void FailureThreshold_WithInvalidValue_ShouldThrowArgumentOutOfRangeException(int invalidValue)
    {
        // Act & Assert
        var exception = Should.Throw<ArgumentOutOfRangeException>(
            () => new CircuitBreakerAttribute { FailureThreshold = invalidValue });

        exception.Message.ShouldContain("FailureThreshold");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void SamplingDurationSeconds_WithInvalidValue_ShouldThrowArgumentOutOfRangeException(int invalidValue)
    {
        // Act & Assert
        var exception = Should.Throw<ArgumentOutOfRangeException>(
            () => new CircuitBreakerAttribute { SamplingDurationSeconds = invalidValue });

        exception.Message.ShouldContain("SamplingDurationSeconds");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void MinimumThroughput_WithInvalidValue_ShouldThrowArgumentOutOfRangeException(int invalidValue)
    {
        // Act & Assert
        var exception = Should.Throw<ArgumentOutOfRangeException>(
            () => new CircuitBreakerAttribute { MinimumThroughput = invalidValue });

        exception.Message.ShouldContain("MinimumThroughput");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void DurationOfBreakSeconds_WithInvalidValue_ShouldThrowArgumentOutOfRangeException(int invalidValue)
    {
        // Act & Assert
        var exception = Should.Throw<ArgumentOutOfRangeException>(
            () => new CircuitBreakerAttribute { DurationOfBreakSeconds = invalidValue });

        exception.Message.ShouldContain("DurationOfBreakSeconds");
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(-1.0)]
    [InlineData(1.1)]
    [InlineData(2.0)]
    public void FailureRateThreshold_WithInvalidValue_ShouldThrowArgumentOutOfRangeException(double invalidValue)
    {
        // Act & Assert
        var exception = Should.Throw<ArgumentOutOfRangeException>(
            () => new CircuitBreakerAttribute { FailureRateThreshold = invalidValue });

        exception.Message.ShouldContain("FailureRateThreshold");
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    public void FailureRateThreshold_WithValidBoundaryValues_ShouldSucceed(double validValue)
    {
        // Act
        var attribute = new CircuitBreakerAttribute { FailureRateThreshold = validValue };

        // Assert
        attribute.FailureRateThreshold.ShouldBe(validValue);
    }

    #endregion

    [CircuitBreakerAttribute(FailureThreshold = 3, DurationOfBreakSeconds = 15)]
    private sealed record TestCircuitBreakerRequest : IRequest<string>;
}
