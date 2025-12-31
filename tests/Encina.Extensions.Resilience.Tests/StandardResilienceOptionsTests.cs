using Shouldly;
using Polly;
using Encina.Extensions.Resilience;
using Xunit;

namespace Encina.Extensions.Resilience.Tests;

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
        options.ShouldNotBeNull();
        options.RateLimiter.ShouldNotBeNull();
        options.TotalRequestTimeout.ShouldNotBeNull();
        options.Retry.ShouldNotBeNull();
        options.CircuitBreaker.ShouldNotBeNull();
        options.AttemptTimeout.ShouldNotBeNull();
    }

    [Fact]
    public void RateLimiter_ShouldHaveDefaultValue()
    {
        // Act
        var options = new StandardResilienceOptions();

        // Assert
        options.RateLimiter.ShouldNotBeNull();
        // RateLimiterStrategyOptions has internal properties, just verify it exists
    }

    [Fact]
    public void TotalRequestTimeout_ShouldBe30Seconds()
    {
        // Act
        var options = new StandardResilienceOptions();

        // Assert
        options.TotalRequestTimeout.Timeout.ShouldBe(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void Retry_ShouldHave3AttemptsExponentialBackoff()
    {
        // Act
        var options = new StandardResilienceOptions();

        // Assert
        options.Retry.MaxRetryAttempts.ShouldBe(3);
        options.Retry.Delay.ShouldBe(TimeSpan.FromSeconds(1));
        options.Retry.BackoffType.ShouldBe(DelayBackoffType.Exponential);
        options.Retry.UseJitter.ShouldBeTrue();
    }

    [Fact]
    public void CircuitBreaker_ShouldHave10PercentFailureThreshold()
    {
        // Act
        var options = new StandardResilienceOptions();

        // Assert
        options.CircuitBreaker.FailureRatio.ShouldBe(0.1);
        options.CircuitBreaker.MinimumThroughput.ShouldBe(10);
        options.CircuitBreaker.SamplingDuration.ShouldBe(TimeSpan.FromSeconds(30));
        options.CircuitBreaker.BreakDuration.ShouldBe(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void AttemptTimeout_ShouldBe10Seconds()
    {
        // Act
        var options = new StandardResilienceOptions();

        // Assert
        options.AttemptTimeout.Timeout.ShouldBe(TimeSpan.FromSeconds(10));
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
        options.RateLimiter.ShouldBeSameAs(newRateLimiter);
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
        options.TotalRequestTimeout.ShouldBeSameAs(newTimeout);
        options.TotalRequestTimeout.Timeout.ShouldBe(TimeSpan.FromSeconds(60));
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
        options.Retry.ShouldBeSameAs(newRetry);
        options.Retry.MaxRetryAttempts.ShouldBe(5);
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
        options.CircuitBreaker.ShouldBeSameAs(newCircuitBreaker);
        options.CircuitBreaker.FailureRatio.ShouldBe(0.2);
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
        options.AttemptTimeout.ShouldBeSameAs(newTimeout);
        options.AttemptTimeout.Timeout.ShouldBe(TimeSpan.FromSeconds(15));
    }

    #region Null Safety Tests

    [Fact]
    public void RateLimiter_SetToNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = new StandardResilienceOptions();

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() => options.RateLimiter = null!);
        ex.ParamName.ShouldBe("value");
    }

    [Fact]
    public void TotalRequestTimeout_SetToNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = new StandardResilienceOptions();

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() => options.TotalRequestTimeout = null!);
        ex.ParamName.ShouldBe("value");
    }

    [Fact]
    public void Retry_SetToNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = new StandardResilienceOptions();

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() => options.Retry = null!);
        ex.ParamName.ShouldBe("value");
    }

    [Fact]
    public void CircuitBreaker_SetToNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = new StandardResilienceOptions();

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() => options.CircuitBreaker = null!);
        ex.ParamName.ShouldBe("value");
    }

    [Fact]
    public void AttemptTimeout_SetToNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = new StandardResilienceOptions();

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() => options.AttemptTimeout = null!);
        ex.ParamName.ShouldBe("value");
    }

    #endregion
}
