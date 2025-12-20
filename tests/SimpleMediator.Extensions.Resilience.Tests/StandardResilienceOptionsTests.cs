using FluentAssertions;
using Polly;
using SimpleMediator.Extensions.Resilience;
using Xunit;

namespace SimpleMediator.Extensions.Resilience.Tests;

/// <summary>
/// Unit tests for <see cref="StandardResilienceOptions"/>.
/// Tests the default configuration values for all resilience strategies.
/// </summary>
public class StandardResilienceOptionsTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        // Act
        var options = new StandardResilienceOptions();

        // Assert
        options.Should().NotBeNull();
        options.RateLimiter.Should().NotBeNull();
        options.TotalRequestTimeout.Should().NotBeNull();
        options.Retry.Should().NotBeNull();
        options.CircuitBreaker.Should().NotBeNull();
        options.AttemptTimeout.Should().NotBeNull();
    }

    [Fact]
    public void RateLimiter_ShouldHaveDefaultValue()
    {
        // Act
        var options = new StandardResilienceOptions();

        // Assert
        options.RateLimiter.Should().NotBeNull();
        // RateLimiterStrategyOptions has internal properties, just verify it exists
    }

    [Fact]
    public void TotalRequestTimeout_ShouldBe30Seconds()
    {
        // Act
        var options = new StandardResilienceOptions();

        // Assert
        options.TotalRequestTimeout.Timeout.Should().Be(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void Retry_ShouldHave3AttemptsExponentialBackoff()
    {
        // Act
        var options = new StandardResilienceOptions();

        // Assert
        options.Retry.MaxRetryAttempts.Should().Be(3);
        options.Retry.Delay.Should().Be(TimeSpan.FromSeconds(1));
        options.Retry.BackoffType.Should().Be(DelayBackoffType.Exponential);
        options.Retry.UseJitter.Should().BeTrue();
    }

    [Fact]
    public void CircuitBreaker_ShouldHave10PercentFailureThreshold()
    {
        // Act
        var options = new StandardResilienceOptions();

        // Assert
        options.CircuitBreaker.FailureRatio.Should().Be(0.1);
        options.CircuitBreaker.MinimumThroughput.Should().Be(10);
        options.CircuitBreaker.SamplingDuration.Should().Be(TimeSpan.FromSeconds(30));
        options.CircuitBreaker.BreakDuration.Should().Be(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void AttemptTimeout_ShouldBe10Seconds()
    {
        // Act
        var options = new StandardResilienceOptions();

        // Assert
        options.AttemptTimeout.Timeout.Should().Be(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void RateLimiter_ShouldBeSettable()
    {
        // Arrange
        var options = new StandardResilienceOptions();
        var newRateLimiter = new Polly.RateLimiting.RateLimiterStrategyOptions();

        // Act
        options.RateLimiter = newRateLimiter;

        // Assert
        options.RateLimiter.Should().BeSameAs(newRateLimiter);
    }

    [Fact]
    public void TotalRequestTimeout_ShouldBeSettable()
    {
        // Arrange
        var options = new StandardResilienceOptions();
        var newTimeout = new Polly.Timeout.TimeoutStrategyOptions { Timeout = TimeSpan.FromSeconds(60) };

        // Act
        options.TotalRequestTimeout = newTimeout;

        // Assert
        options.TotalRequestTimeout.Should().BeSameAs(newTimeout);
        options.TotalRequestTimeout.Timeout.Should().Be(TimeSpan.FromSeconds(60));
    }

    [Fact]
    public void Retry_ShouldBeSettable()
    {
        // Arrange
        var options = new StandardResilienceOptions();
        var newRetry = new Polly.Retry.RetryStrategyOptions
        {
            MaxRetryAttempts = 5,
            Delay = TimeSpan.FromSeconds(2)
        };

        // Act
        options.Retry = newRetry;

        // Assert
        options.Retry.Should().BeSameAs(newRetry);
        options.Retry.MaxRetryAttempts.Should().Be(5);
    }

    [Fact]
    public void CircuitBreaker_ShouldBeSettable()
    {
        // Arrange
        var options = new StandardResilienceOptions();
        var newCircuitBreaker = new Polly.CircuitBreaker.CircuitBreakerStrategyOptions
        {
            FailureRatio = 0.2
        };

        // Act
        options.CircuitBreaker = newCircuitBreaker;

        // Assert
        options.CircuitBreaker.Should().BeSameAs(newCircuitBreaker);
        options.CircuitBreaker.FailureRatio.Should().Be(0.2);
    }

    [Fact]
    public void AttemptTimeout_ShouldBeSettable()
    {
        // Arrange
        var options = new StandardResilienceOptions();
        var newTimeout = new Polly.Timeout.TimeoutStrategyOptions { Timeout = TimeSpan.FromSeconds(15) };

        // Act
        options.AttemptTimeout = newTimeout;

        // Assert
        options.AttemptTimeout.Should().BeSameAs(newTimeout);
        options.AttemptTimeout.Timeout.Should().Be(TimeSpan.FromSeconds(15));
    }
}
