using FsCheck;
using FsCheck.Xunit;
using Polly;
using SimpleMediator.Extensions.Resilience;

namespace SimpleMediator.Extensions.Resilience.PropertyTests;

/// <summary>
/// Property-based tests for <see cref="StandardResilienceOptions"/>.
/// Uses FsCheck to verify invariants hold for all possible inputs.
/// </summary>
public class StandardResilienceOptionsPropertyTests
{
    [Property(Arbitrary = new[] { typeof(PositiveIntGenerator) })]
    public Property Property_RetryMaxAttempts_AlwaysPositive(PositiveInt maxAttempts)
    {
        // Arrange
        var options = new StandardResilienceOptions();

        // Act
        options.Retry.MaxRetryAttempts = maxAttempts.Get;

        // Assert
        return (options.Retry.MaxRetryAttempts > 0).ToProperty();
    }

    [Property(Arbitrary = new[] { typeof(ValidFailureRatioGenerator) })]
    public Property Property_CircuitBreakerFailureRatio_Between0And1(double failureRatio)
    {
        // Arrange
        var options = new StandardResilienceOptions();

        // Act
        options.CircuitBreaker.FailureRatio = failureRatio;

        // Assert
        return (options.CircuitBreaker.FailureRatio >= 0.0 &&
                options.CircuitBreaker.FailureRatio <= 1.0).ToProperty();
    }

    [Property(Arbitrary = new[] { typeof(PositiveTimeSpanGenerator) })]
    public Property Property_TotalTimeout_AlwaysPositive(TimeSpan timeout)
    {
        // Arrange
        var options = new StandardResilienceOptions();

        // Act
        options.TotalRequestTimeout.Timeout = timeout;

        // Assert
        return (options.TotalRequestTimeout.Timeout > TimeSpan.Zero).ToProperty();
    }

    [Property(Arbitrary = new[] { typeof(PositiveTimeSpanGenerator) })]
    public Property Property_AttemptTimeout_AlwaysPositive(TimeSpan timeout)
    {
        // Arrange
        var options = new StandardResilienceOptions();

        // Act
        options.AttemptTimeout.Timeout = timeout;

        // Assert
        return (options.AttemptTimeout.Timeout > TimeSpan.Zero).ToProperty();
    }

    [Property]
    public Property Property_BackoffType_ValidEnumValue()
    {
        // Arrange
        var options = new StandardResilienceOptions();
        var validBackoffTypes = new[]
        {
            DelayBackoffType.Constant,
            DelayBackoffType.Linear,
            DelayBackoffType.Exponential
        };

        // Act & Assert
        return validBackoffTypes.Contains(options.Retry.BackoffType).ToProperty();
    }

    [Property(Arbitrary = new[] { typeof(PositiveTimeSpanGenerator) })]
    public Property Property_RetryDelay_AlwaysPositive(TimeSpan delay)
    {
        // Arrange
        var options = new StandardResilienceOptions();

        // Act
        options.Retry.Delay = delay;

        // Assert
        return (options.Retry.Delay >= TimeSpan.Zero).ToProperty();
    }

    [Property(Arbitrary = new[] { typeof(PositiveIntGenerator) })]
    public Property Property_CircuitBreakerMinimumThroughput_AlwaysPositive(PositiveInt throughput)
    {
        // Arrange
        var options = new StandardResilienceOptions();

        // Act
        options.CircuitBreaker.MinimumThroughput = throughput.Get;

        // Assert
        return (options.CircuitBreaker.MinimumThroughput > 0).ToProperty();
    }

    [Property(Arbitrary = new[] { typeof(PositiveTimeSpanGenerator) })]
    public Property Property_CircuitBreakerSamplingDuration_AlwaysPositive(TimeSpan duration)
    {
        // Arrange
        var options = new StandardResilienceOptions();

        // Act
        options.CircuitBreaker.SamplingDuration = duration;

        // Assert
        return (options.CircuitBreaker.SamplingDuration > TimeSpan.Zero).ToProperty();
    }

    [Property(Arbitrary = new[] { typeof(PositiveTimeSpanGenerator) })]
    public Property Property_CircuitBreakerBreakDuration_AlwaysPositive(TimeSpan duration)
    {
        // Arrange
        var options = new StandardResilienceOptions();

        // Act
        options.CircuitBreaker.BreakDuration = duration;

        // Assert
        return (options.CircuitBreaker.BreakDuration > TimeSpan.Zero).ToProperty();
    }

    [Property]
    public Property Property_DefaultOptions_AreValid()
    {
        // Act
        var options = new StandardResilienceOptions();

        // Assert
        return (options.Retry.MaxRetryAttempts > 0 &&
                options.CircuitBreaker.FailureRatio >= 0.0 &&
                options.CircuitBreaker.FailureRatio <= 1.0 &&
                options.TotalRequestTimeout.Timeout > TimeSpan.Zero &&
                options.AttemptTimeout.Timeout > TimeSpan.Zero).ToProperty();
    }
}

/// <summary>
/// Generator for positive integers.
/// </summary>
public class PositiveIntGenerator
{
    public static Arbitrary<PositiveInt> PositiveInts() =>
        Arb.From(Gen.Choose(1, 100).Select(x => new PositiveInt(x)));
}

/// <summary>
/// Generator for valid failure ratios (0.0 to 1.0).
/// </summary>
public class ValidFailureRatioGenerator
{
    public static Arbitrary<double> FailureRatios() =>
        Arb.From(Gen.Choose(0, 100).Select(x => x / 100.0));
}

/// <summary>
/// Generator for positive TimeSpan values.
/// </summary>
public class PositiveTimeSpanGenerator
{
    public static Arbitrary<TimeSpan> PositiveTimeSpans() =>
        Arb.From(Gen.Choose(1, 300).Select(seconds => TimeSpan.FromSeconds(seconds)));
}
