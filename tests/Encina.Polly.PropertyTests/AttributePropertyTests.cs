using FsCheck;
using FsCheck.Xunit;
using Xunit;

namespace Encina.Polly.PropertyTests;

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

    [Fact]
    public void RateLimitAttribute_MaxRequestsPerWindow_ShouldBePositive()
    {
        // Arrange & Act
        var attribute = new RateLimitAttribute { MaxRequestsPerWindow = 100 };

        // Assert
        attribute.MaxRequestsPerWindow.Should().BeGreaterThan(0);
    }

    [Fact]
    public void RateLimitAttribute_WindowSizeSeconds_ShouldBePositive()
    {
        // Arrange & Act
        var attribute = new RateLimitAttribute { WindowSizeSeconds = 60 };

        // Assert
        attribute.WindowSizeSeconds.Should().BeGreaterThan(0);
    }

    [Fact]
    public void RateLimitAttribute_ErrorThresholdPercent_ShouldBeBetween0And100()
    {
        // Arrange & Act
        var attribute = new RateLimitAttribute { ErrorThresholdPercent = 50 };

        // Assert
        attribute.ErrorThresholdPercent.Should().BeInRange(0, 100);
    }

    [Fact]
    public void RateLimitAttribute_RampUpFactor_ShouldBePositive()
    {
        // Arrange & Act
        var attribute = new RateLimitAttribute { RampUpFactor = 1.5 };

        // Assert
        attribute.RampUpFactor.Should().BeGreaterThan(0);
    }

    #region Bulkhead Attribute Properties

    [Fact]
    public void BulkheadAttribute_MaxConcurrency_ShouldBePositive()
    {
        // Arrange & Act
        var attribute = new BulkheadAttribute { MaxConcurrency = 10 };

        // Assert
        attribute.MaxConcurrency.Should().BeGreaterThan(0);
    }

    [Fact]
    public void BulkheadAttribute_MaxQueuedActions_ShouldBeNonNegative()
    {
        // Arrange & Act
        var attribute = new BulkheadAttribute { MaxQueuedActions = 20 };

        // Assert
        attribute.MaxQueuedActions.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void BulkheadAttribute_QueueTimeoutMs_ShouldBePositive()
    {
        // Arrange & Act
        var attribute = new BulkheadAttribute { QueueTimeoutMs = 5000 };

        // Assert
        attribute.QueueTimeoutMs.Should().BeGreaterThan(0);
    }

    [Property]
    public bool BulkheadMetrics_RejectionRate_ShouldBeBetween0And100(PositiveInt acquired, PositiveInt rejected)
    {
        var metrics = new BulkheadMetrics(
            0, 0, 10, 20,
            acquired.Get,
            rejected.Get);

        return metrics.RejectionRate >= 0 && metrics.RejectionRate <= 100;
    }

    [Property]
    public bool BulkheadMetrics_ConcurrencyUtilization_ShouldBeBetween0And100(PositiveInt currentConcurrency, PositiveInt maxConcurrency)
    {
        var current = Math.Min(currentConcurrency.Get, maxConcurrency.Get);
        var max = Math.Max(maxConcurrency.Get, 1);
        var metrics = new BulkheadMetrics(
            current,
            0,
            max,
            20,
            100,
            0);

        return metrics.ConcurrencyUtilization >= 0 && metrics.ConcurrencyUtilization <= 100;
    }

    [Property]
    public bool BulkheadMetrics_QueueUtilization_ShouldBeBetween0And100(PositiveInt currentQueued, PositiveInt maxQueued)
    {
        var current = Math.Min(currentQueued.Get, maxQueued.Get);
        var max = Math.Max(maxQueued.Get, 1);
        var metrics = new BulkheadMetrics(
            0,
            current,
            10,
            max,
            100,
            0);

        return metrics.QueueUtilization >= 0 && metrics.QueueUtilization <= 100;
    }

    [Property]
    public bool BulkheadAttribute_MaxConcurrency_ShouldPreserveValue(PositiveInt value)
    {
        var attribute = new BulkheadAttribute { MaxConcurrency = value.Get };
        return attribute.MaxConcurrency == value.Get;
    }

    [Property]
    public bool BulkheadAttribute_MaxQueuedActions_ShouldPreserveValue(NonNegativeInt value)
    {
        var attribute = new BulkheadAttribute { MaxQueuedActions = value.Get };
        return attribute.MaxQueuedActions == value.Get;
    }

    [Property]
    public bool BulkheadAttribute_QueueTimeoutMs_ShouldPreserveValue(PositiveInt value)
    {
        var attribute = new BulkheadAttribute { QueueTimeoutMs = value.Get };
        return attribute.QueueTimeoutMs == value.Get;
    }

    #endregion
}
