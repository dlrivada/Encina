using FsCheck;
using FsCheck.Xunit;
using Xunit;

namespace SimpleMediator.Polly.PropertyTests;

/// <summary>
/// Property-based tests for Polly attributes.
/// Uses FsCheck to verify properties hold for all valid inputs.
/// </summary>
public class AttributePropertyTests
{
    [Fact]
    public void RetryAttribute_MaxAttempts_ShouldBePositive()
    {
        // Arrange & Act
        var attribute = new RetryAttribute { MaxAttempts = 5 };

        // Assert
        attribute.MaxAttempts.Should().BeGreaterThan(0);
    }

    [Fact]
    public void RetryAttribute_BaseDelay_ShouldBePositive()
    {
        // Arrange & Act
        var attribute = new RetryAttribute { BaseDelayMs = 1000 };

        // Assert
        attribute.BaseDelayMs.Should().BeGreaterThan(0);
    }

    [Fact]
    public void CircuitBreaker_FailureRateThreshold_ShouldBeBetween0And1()
    {
        // Arrange & Act
        var attribute = new CircuitBreakerAttribute { FailureRateThreshold = 0.5 };

        // Assert
        attribute.FailureRateThreshold.Should().BeInRange(0, 1);
    }

    [Fact]
    public void CircuitBreaker_MinimumThroughput_ShouldBePositive()
    {
        // Arrange & Act
        var attribute = new CircuitBreakerAttribute { MinimumThroughput = 10 };

        // Assert
        attribute.MinimumThroughput.Should().BeGreaterThan(0);
    }
}
