using Encina.TestInfrastructure.PropertyTests;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using LanguageExt;
using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.Polly.PropertyTests;

/// <summary>
/// Property-based tests for circuit breaker policy invariants.
/// Verifies circuit breaker behavior guarantees hold for all valid inputs.
/// </summary>
public class CircuitBreakerInvariantPropertyTests
{
    private const int MaxTestsForExpensive = 20;

    #region CircuitBreakerAttribute Invariants

    [Property]
    public bool CircuitBreakerAttribute_FailureThreshold_PreservesPositiveValues(PositiveInt threshold)
    {
        // Arrange
        var value = threshold.Get;

        // Act
        var attribute = new CircuitBreakerAttribute { FailureThreshold = value };

        // Assert
        return attribute.FailureThreshold == value;
    }

    [Property]
    public bool CircuitBreakerAttribute_FailureThreshold_ThrowsOnNonPositive(NonNegativeInt nonNegative)
    {
        // Arrange - convert to non-positive (0 or negative)
        var invalidValue = -nonNegative.Get;

        // Act & Assert
        try
        {
            _ = new CircuitBreakerAttribute { FailureThreshold = invalidValue };
            return false; // Should have thrown
        }
        catch (ArgumentOutOfRangeException)
        {
            return true;
        }
    }

    [Property]
    public bool CircuitBreakerAttribute_SamplingDurationSeconds_PreservesPositiveValues(PositiveInt seconds)
    {
        // Arrange
        var value = seconds.Get;

        // Act
        var attribute = new CircuitBreakerAttribute { SamplingDurationSeconds = value };

        // Assert
        return attribute.SamplingDurationSeconds == value;
    }

    [Property]
    public bool CircuitBreakerAttribute_SamplingDurationSeconds_ThrowsOnNonPositive(NonNegativeInt nonNegative)
    {
        // Arrange - convert to non-positive (0 or negative)
        var invalidValue = -nonNegative.Get;

        // Act & Assert
        try
        {
            _ = new CircuitBreakerAttribute { SamplingDurationSeconds = invalidValue };
            return false; // Should have thrown
        }
        catch (ArgumentOutOfRangeException)
        {
            return true;
        }
    }

    [Property]
    public bool CircuitBreakerAttribute_MinimumThroughput_PreservesPositiveValues(PositiveInt throughput)
    {
        // Arrange
        var value = throughput.Get;

        // Act
        var attribute = new CircuitBreakerAttribute { MinimumThroughput = value };

        // Assert
        return attribute.MinimumThroughput == value;
    }

    [Property]
    public bool CircuitBreakerAttribute_MinimumThroughput_ThrowsOnNonPositive(NonNegativeInt nonNegative)
    {
        // Arrange - convert to non-positive (0 or negative)
        var invalidValue = -nonNegative.Get;

        // Act & Assert
        try
        {
            _ = new CircuitBreakerAttribute { MinimumThroughput = invalidValue };
            return false; // Should have thrown
        }
        catch (ArgumentOutOfRangeException)
        {
            return true;
        }
    }

    [Property]
    public bool CircuitBreakerAttribute_DurationOfBreakSeconds_PreservesPositiveValues(PositiveInt seconds)
    {
        // Arrange
        var value = seconds.Get;

        // Act
        var attribute = new CircuitBreakerAttribute { DurationOfBreakSeconds = value };

        // Assert
        return attribute.DurationOfBreakSeconds == value;
    }

    [Property]
    public bool CircuitBreakerAttribute_DurationOfBreakSeconds_ThrowsOnNonPositive(NonNegativeInt nonNegative)
    {
        // Arrange - convert to non-positive (0 or negative)
        var invalidValue = -nonNegative.Get;

        // Act & Assert
        try
        {
            _ = new CircuitBreakerAttribute { DurationOfBreakSeconds = invalidValue };
            return false; // Should have thrown
        }
        catch (ArgumentOutOfRangeException)
        {
            return true;
        }
    }

    [Property]
    public bool CircuitBreakerAttribute_FailureRateThreshold_ShouldPreserveValue(NormalFloat rate)
    {
        // Constrain to valid range [0.0, 1.0]
        var validRate = Math.Clamp(Math.Abs(rate.Get), 0.0, 1.0);
        var attribute = new CircuitBreakerAttribute { FailureRateThreshold = validRate };
        return Math.Abs(attribute.FailureRateThreshold - validRate) < 0.0001;
    }

    [Fact]
    public void CircuitBreakerAttribute_DefaultValues_ShouldBeReasonable()
    {
        // Arrange & Act
        var attribute = new CircuitBreakerAttribute();

        // Assert
        Assert.Equal(5, attribute.FailureThreshold);
        Assert.Equal(60, attribute.SamplingDurationSeconds);
        Assert.Equal(10, attribute.MinimumThroughput);
        Assert.Equal(30, attribute.DurationOfBreakSeconds);
        Assert.True(Math.Abs(attribute.FailureRateThreshold - 0.5) < 0.0001);
    }

    #endregion

    #region Circuit Breaker State Transition Invariants

    [Property(MaxTest = MaxTestsForExpensive)]
    public bool CircuitBreaker_NoAttribute_ShouldPassThrough(NonEmptyString expectedResultRaw)
    {
        // Arrange - request without CircuitBreaker attribute
        var expectedResult = expectedResultRaw.Get;
        var behavior = new CircuitBreakerPipelineBehavior<TestNoCircuitBreakerRequest, string>(
            NullLogger<CircuitBreakerPipelineBehavior<TestNoCircuitBreakerRequest, string>>.Instance);

        RequestHandlerCallback<string> handler = () =>
            ValueTask.FromResult(Prelude.Right<EncinaError, string>(expectedResult));

        // Act
        var result = ExecuteSyncWithAttribute<TestNoCircuitBreakerRequest>(behavior, handler);

        // Assert - should pass through without circuit breaker interference
        return result.Match(
            Right: value => value == expectedResult,
            Left: _ => false);
    }

    [Property(MaxTest = MaxTestsForExpensive)]
    public bool CircuitBreaker_SuccessfulRequest_ShouldReturnSuccess(NonEmptyString expectedResultRaw)
    {
        // Arrange
        var expectedResult = expectedResultRaw.Get;
        var behavior = new CircuitBreakerPipelineBehavior<TestCircuitBreakerSuccessRequest, string>(
            NullLogger<CircuitBreakerPipelineBehavior<TestCircuitBreakerSuccessRequest, string>>.Instance);

        RequestHandlerCallback<string> handler = () =>
            ValueTask.FromResult(Prelude.Right<EncinaError, string>(expectedResult));

        // Act
        var result = ExecuteSyncWithAttribute<TestCircuitBreakerSuccessRequest>(behavior, handler);

        // Assert
        return result.Match(
            Right: value => value == expectedResult,
            Left: _ => false);
    }

    [Property(MaxTest = MaxTestsForExpensive)]
    public bool CircuitBreaker_FailedRequest_ShouldReturnError(NonEmptyString errorMessageRaw)
    {
        // Arrange
        var errorMessage = errorMessageRaw.Get;
        var behavior = new CircuitBreakerPipelineBehavior<TestCircuitBreakerFailureRequest, string>(
            NullLogger<CircuitBreakerPipelineBehavior<TestCircuitBreakerFailureRequest, string>>.Instance);

        var expectedError = EncinaError.New(errorMessage);
        RequestHandlerCallback<string> handler = () =>
            ValueTask.FromResult(Prelude.Left<EncinaError, string>(expectedError));

        // Act
        var result = ExecuteSyncWithAttribute<TestCircuitBreakerFailureRequest>(behavior, handler);

        // Assert
        return result.IsLeft;
    }

    [Property(MaxTest = MaxTestsForExpensive)]
    public bool CircuitBreaker_ExceptionInHandler_ShouldReturnError(NonEmptyString exceptionMessageRaw)
    {
        // Arrange
        var exceptionMessage = exceptionMessageRaw.Get;
        var behavior = new CircuitBreakerPipelineBehavior<TestCircuitBreakerExceptionRequest, string>(
            NullLogger<CircuitBreakerPipelineBehavior<TestCircuitBreakerExceptionRequest, string>>.Instance);

        RequestHandlerCallback<string> handler = () =>
            throw new InvalidOperationException(exceptionMessage);

        // Act
        var result = ExecuteSyncWithAttribute<TestCircuitBreakerExceptionRequest>(behavior, handler);

        // Assert - should convert exception to EncinaError
        return result.IsLeft;
    }

    #endregion

    #region Throughput Invariants

    /// <summary>
    /// This test verifies the circuit breaker configuration is correctly applied.
    /// Due to the static circuit breaker cache, property-based iteration tests cannot
    /// reliably test stateful circuit breaker behavior across multiple runs.
    /// Instead, we verify the configuration invariant directly.
    /// </summary>
    [Fact]
    public void MinimumThroughput_Configuration_ShouldBePreserved()
    {
        // Arrange & Act
        var attribute = new CircuitBreakerAttribute { MinimumThroughput = 5 };

        // Assert - Configuration should be preserved
        Assert.Equal(5, attribute.MinimumThroughput);
    }

    #endregion

    #region Break Duration Invariants

    [Property]
    public bool BreakDuration_AttributeShouldPreserveValue(PositiveInt durationSecondsRaw)
    {
        // Constrain to reasonable range
        var durationSeconds = (durationSecondsRaw.Get % 3600) + 1; // 1-3600 seconds

        var attribute = new CircuitBreakerAttribute { DurationOfBreakSeconds = durationSeconds };

        // Invariant: Attribute preserves the configured break duration
        return attribute.DurationOfBreakSeconds == durationSeconds;
    }

    #endregion

    #region Circuit Breaker Configuration Invariants

    [Property(Arbitrary = [typeof(ValidRatioArbitrary)])]
    public bool Configuration_FailureRatioMustBeValid(double ratio)
    {
        var attribute = new CircuitBreakerAttribute { FailureRateThreshold = ratio };
        return attribute.FailureRateThreshold >= 0.0 && attribute.FailureRateThreshold <= 1.0;
    }

    [Property(Arbitrary = [typeof(InvalidRatioArbitrary)])]
    public bool Configuration_FailureRatioInvalidValuesMustThrow(double ratio)
    {
        // Invariant: Invalid ratios outside [0.0, 1.0] should throw ArgumentOutOfRangeException
        try
        {
            _ = new CircuitBreakerAttribute { FailureRateThreshold = ratio };

            // If we get here, validation is missing - the current implementation accepts invalid values
            // This test documents the expected behavior: invalid ratios should be rejected
            return false;
        }
        catch (ArgumentOutOfRangeException)
        {
            return true;
        }
    }

    [Property]
    public bool Configuration_AllPositiveIntegerProperties_ShouldBeSettable(
        PositiveInt threshold,
        PositiveInt sampling,
        PositiveInt throughput,
        PositiveInt breakDuration)
    {
        var attribute = new CircuitBreakerAttribute
        {
            FailureThreshold = threshold.Get,
            SamplingDurationSeconds = sampling.Get,
            MinimumThroughput = throughput.Get,
            DurationOfBreakSeconds = breakDuration.Get
        };

        return attribute.FailureThreshold == threshold.Get
               && attribute.SamplingDurationSeconds == sampling.Get
               && attribute.MinimumThroughput == throughput.Get
               && attribute.DurationOfBreakSeconds == breakDuration.Get;
    }

    #endregion

    #region Helper Types

    // Note: The three test request types below use identical CircuitBreaker configuration
    // (FailureThreshold=100, SamplingDurationSeconds=1, MinimumThroughput=100,
    // DurationOfBreakSeconds=1, FailureRateThreshold=0.99) intentionally.
    // C# attribute arguments must be compile-time constants and cannot reference
    // const fields, so we cannot extract these values into shared constants.
    // The high thresholds prevent the circuit from opening during individual test runs.

    /// <summary>
    /// Request with circuit breaker configuration for success tests.
    /// Uses high thresholds to avoid circuit opening during success tests.
    /// </summary>
    [CircuitBreaker(
        FailureThreshold = 100,
        SamplingDurationSeconds = 1,
        MinimumThroughput = 100,
        DurationOfBreakSeconds = 1,
        FailureRateThreshold = 0.99)]
    private sealed record TestCircuitBreakerSuccessRequest : IRequest<string>;

    /// <summary>
    /// Request with circuit breaker configuration for failure tests.
    /// </summary>
    [CircuitBreaker(
        FailureThreshold = 100,
        SamplingDurationSeconds = 1,
        MinimumThroughput = 100,
        DurationOfBreakSeconds = 1,
        FailureRateThreshold = 0.99)]
    private sealed record TestCircuitBreakerFailureRequest : IRequest<string>;

    /// <summary>
    /// Request with circuit breaker configuration for exception tests.
    /// </summary>
    [CircuitBreaker(
        FailureThreshold = 100,
        SamplingDurationSeconds = 1,
        MinimumThroughput = 100,
        DurationOfBreakSeconds = 1,
        FailureRateThreshold = 0.99)]
    private sealed record TestCircuitBreakerExceptionRequest : IRequest<string>;

    /// <summary>
    /// Request with circuit breaker configuration for minimum throughput tests.
    /// Uses MinimumThroughput = 5 so we can test behavior when requests are below threshold.
    /// </summary>
    [CircuitBreaker(
        FailureThreshold = 1,
        SamplingDurationSeconds = 60,
        MinimumThroughput = 5,
        DurationOfBreakSeconds = 30,
        FailureRateThreshold = 0.5)]
    private sealed record TestMinimumThroughputRequest : IRequest<string>;

    /// <summary>
    /// Request without circuit breaker attribute for pass-through testing.
    /// </summary>
    private sealed record TestNoCircuitBreakerRequest : IRequest<string>;

    #endregion

    #region Custom Arbitraries

    /// <summary>
    /// Custom FsCheck Arbitrary that generates valid failure rate ratios in the range [0.0, 1.0].
    /// </summary>
    private static class ValidRatioArbitrary
    {
        public static Arbitrary<double> Ratio() =>
            Arb.From(Gen.Choose(0, 100).Select(x => x / 100.0));
    }

    /// <summary>
    /// Custom FsCheck Arbitrary that generates invalid failure rate ratios outside [0.0, 1.0].
    /// </summary>
    private static class InvalidRatioArbitrary
    {
        public static Arbitrary<double> Ratio()
        {
            var negative = Gen.Choose(-1000, -1).Select(x => x / 100.0);
            var greaterThanOne = Gen.Choose(101, 1000).Select(x => x / 100.0);
            return Arb.From(Gen.OneOf(negative, greaterThanOne));
        }
    }

    #endregion

    #region Helper Methods

#pragma warning disable CA2012 // Use ValueTasks correctly - Required for synchronous FsCheck property tests
    private static Either<EncinaError, string> ExecuteSyncWithAttribute<TRequest>(
        CircuitBreakerPipelineBehavior<TRequest, string> behavior,
        RequestHandlerCallback<string> handler)
        where TRequest : IRequest<string>, new()
    {
        var request = new TRequest();
        var context = new TestRequestContext();

        return behavior.Handle(request, context, handler, CancellationToken.None)
            .GetAwaiter()
            .GetResult();
    }
#pragma warning restore CA2012

    #endregion
}
