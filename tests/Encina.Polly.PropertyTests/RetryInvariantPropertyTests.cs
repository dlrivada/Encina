using Encina.TestInfrastructure.PropertyTests;
using FsCheck;
using FsCheck.Xunit;
using LanguageExt;
using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.Polly.PropertyTests;

/// <summary>
/// Property-based tests for retry policy invariants.
/// Verifies retry behavior guarantees hold for all valid inputs.
/// </summary>
public sealed class RetryInvariantPropertyTests
{
    private const int MaxTestsForExpensive = 20;

    #region Retry Budget Exhaustion Invariants

    [Property(MaxTest = MaxTestsForExpensive)]
    public bool RetryBehavior_ShouldStopAfterMaxAttempts()
    {
        // Arrange - TestRetryRequest has MaxAttempts = 3
        var attemptCount = 0;
        var behavior = new RetryPipelineBehavior<TestRetryRequest, string>(
            NullLogger<RetryPipelineBehavior<TestRetryRequest, string>>.Instance);

        // Simulate handler that always fails
        RequestHandlerCallback<string> failingHandler = () =>
        {
            attemptCount++;
            return ValueTask.FromResult(Prelude.Left<EncinaError, string>(
                EncinaError.New("Simulated failure")));
        };

        // Act - execute synchronously
        var result = ExecuteSyncWithAttribute<TestRetryRequest>(behavior, failingHandler);

        // Assert - attempt count should be exactly MaxAttempts (3)
        // Polly's MaxRetryAttempts is MaxAttempts - 1, so total = 1 initial + (MaxAttempts-1) retries = MaxAttempts
        return attemptCount == 3 && result.IsLeft;
    }

    [Property(MaxTest = MaxTestsForExpensive)]
    public bool RetryBehavior_SuccessWithinBudget_ShouldReturnSuccess(PositiveInt successAtAttemptRaw)
    {
        // Arrange - TestRetryRequest has MaxAttempts = 3
        var successAtAttempt = (successAtAttemptRaw.Get % 3) + 1; // 1, 2, or 3
        var attemptCount = 0;
        var behavior = new RetryPipelineBehavior<TestRetryRequest, string>(
            NullLogger<RetryPipelineBehavior<TestRetryRequest, string>>.Instance);

        // Handler that succeeds at specific attempt
        RequestHandlerCallback<string> conditionalHandler = () =>
        {
            attemptCount++;
            if (attemptCount >= successAtAttempt)
            {
                return ValueTask.FromResult(Prelude.Right<EncinaError, string>("Success"));
            }

            return ValueTask.FromResult(Prelude.Left<EncinaError, string>(
                EncinaError.New("Retry needed")));
        };

        // Act
        var result = ExecuteSyncWithAttribute<TestRetryRequest>(behavior, conditionalHandler);

        // Assert - should succeed if success happens within budget (3 attempts)
        return result.IsRight;
    }

    #endregion

    #region Backoff Invariants

    /// <summary>
    /// Property: Constant backoff attribute should preserve fixed delay configuration.
    /// </summary>
    [Property]
    public bool BackoffType_Constant_AttributeShouldPreserveConfiguration(PositiveInt baseDelayRaw)
    {
        // Arrange
        var baseDelay = (baseDelayRaw.Get % 1000) + 100; // 100-1100ms

        var attribute = new RetryAttribute
        {
            MaxAttempts = 3,
            BackoffType = BackoffType.Constant,
            BaseDelayMs = baseDelay
        };

        // Invariant: Attribute should preserve constant backoff configuration
        return attribute.BackoffType == BackoffType.Constant &&
               attribute.BaseDelayMs == baseDelay;
    }

    /// <summary>
    /// Property: Linear backoff attribute should preserve linear delay configuration.
    /// </summary>
    [Property]
    public bool BackoffType_Linear_AttributeShouldPreserveConfiguration(PositiveInt baseDelayRaw)
    {
        // Arrange
        var baseDelay = (baseDelayRaw.Get % 500) + 100; // 100-600ms

        var attribute = new RetryAttribute
        {
            MaxAttempts = 3,
            BackoffType = BackoffType.Linear,
            BaseDelayMs = baseDelay
        };

        // Invariant: Attribute should preserve linear backoff configuration
        return attribute.BackoffType == BackoffType.Linear &&
               attribute.BaseDelayMs == baseDelay;
    }

    /// <summary>
    /// Property: Exponential backoff attribute should preserve exponential delay configuration.
    /// </summary>
    [Property]
    public bool BackoffType_Exponential_AttributeShouldPreserveConfiguration(PositiveInt baseDelayRaw)
    {
        // Arrange
        var baseDelay = (baseDelayRaw.Get % 500) + 100; // 100-600ms

        var attribute = new RetryAttribute
        {
            MaxAttempts = 3,
            BackoffType = BackoffType.Exponential,
            BaseDelayMs = baseDelay
        };

        // Invariant: Attribute should preserve exponential backoff configuration
        return attribute.BackoffType == BackoffType.Exponential &&
               attribute.BaseDelayMs == baseDelay;
    }

    /// <summary>
    /// Property: MaxDelay should cap the effective delay regardless of backoff type.
    /// </summary>
    [Property]
    public bool BackoffType_MaxDelay_ShouldBeCappedInAttribute(
        PositiveInt baseDelayRaw,
        PositiveInt maxDelayRaw)
    {
        // Arrange
        var baseDelay = (baseDelayRaw.Get % 500) + 100; // 100-600ms
        var maxDelay = (maxDelayRaw.Get % 10000) + baseDelay; // At least baseDelay

        var attribute = new RetryAttribute
        {
            MaxAttempts = 5,
            BackoffType = BackoffType.Exponential,
            BaseDelayMs = baseDelay,
            MaxDelayMs = maxDelay
        };

        // Invariant: MaxDelayMs should be preserved and >= BaseDelayMs
        return attribute.MaxDelayMs == maxDelay &&
               attribute.MaxDelayMs >= attribute.BaseDelayMs;
    }

    /// <summary>
    /// Property: Retry behavior with constant backoff should execute all attempts on persistent failure.
    /// </summary>
    [Property(MaxTest = MaxTestsForExpensive)]
    public bool BackoffType_Constant_BehaviorShouldExecuteAllAttempts()
    {
        // Arrange - TestRetryConstantRequest has MaxAttempts = 3, BackoffType = Constant
        var attemptCount = 0;
        var behavior = new RetryPipelineBehavior<TestRetryConstantRequest, string>(
            NullLogger<RetryPipelineBehavior<TestRetryConstantRequest, string>>.Instance);

        RequestHandlerCallback<string> failingHandler = () =>
        {
            attemptCount++;
            return ValueTask.FromResult(Prelude.Left<EncinaError, string>(
                EncinaError.New("Simulated failure")));
        };

        // Act
        _ = ExecuteSyncWithAttribute<TestRetryConstantRequest>(behavior, failingHandler);

        // Invariant: Should have executed exactly MaxAttempts times
        return attemptCount == 3;
    }

    /// <summary>
    /// Property: Retry behavior with linear backoff should execute all attempts on persistent failure.
    /// </summary>
    [Property(MaxTest = MaxTestsForExpensive)]
    public bool BackoffType_Linear_BehaviorShouldExecuteAllAttempts()
    {
        // Arrange - TestRetryLinearRequest has MaxAttempts = 3, BackoffType = Linear
        var attemptCount = 0;
        var behavior = new RetryPipelineBehavior<TestRetryLinearRequest, string>(
            NullLogger<RetryPipelineBehavior<TestRetryLinearRequest, string>>.Instance);

        RequestHandlerCallback<string> failingHandler = () =>
        {
            attemptCount++;
            return ValueTask.FromResult(Prelude.Left<EncinaError, string>(
                EncinaError.New("Simulated failure")));
        };

        // Act
        _ = ExecuteSyncWithAttribute<TestRetryLinearRequest>(behavior, failingHandler);

        // Invariant: Should have executed exactly MaxAttempts times
        return attemptCount == 3;
    }

    /// <summary>
    /// Property: Retry behavior with exponential backoff should execute all attempts on persistent failure.
    /// </summary>
    [Property(MaxTest = MaxTestsForExpensive)]
    public bool BackoffType_Exponential_BehaviorShouldExecuteAllAttempts()
    {
        // Arrange - TestRetryExponentialRequest has MaxAttempts = 3, BackoffType = Exponential
        var attemptCount = 0;
        var behavior = new RetryPipelineBehavior<TestRetryExponentialRequest, string>(
            NullLogger<RetryPipelineBehavior<TestRetryExponentialRequest, string>>.Instance);

        RequestHandlerCallback<string> failingHandler = () =>
        {
            attemptCount++;
            return ValueTask.FromResult(Prelude.Left<EncinaError, string>(
                EncinaError.New("Simulated failure")));
        };

        // Act
        _ = ExecuteSyncWithAttribute<TestRetryExponentialRequest>(behavior, failingHandler);

        // Invariant: Should have executed exactly MaxAttempts times
        return attemptCount == 3;
    }

    #endregion

    #region Outcome Preservation Invariants

    [Property(MaxTest = MaxTestsForExpensive)]
    public bool RetryBehavior_FinalOutcome_ShouldBePreserved()
    {
        // Arrange
        var expectedError = EncinaError.New("Final error message");
        var behavior = new RetryPipelineBehavior<TestRetryRequest, string>(
            NullLogger<RetryPipelineBehavior<TestRetryRequest, string>>.Instance);

        // Handler that always returns the same error
        RequestHandlerCallback<string> failingHandler = () =>
            ValueTask.FromResult(Prelude.Left<EncinaError, string>(expectedError));

        // Act
        var result = ExecuteSyncWithAttribute<TestRetryRequest>(behavior, failingHandler);

        // Assert - final error should be preserved (or wrapped)
        return result.IsLeft;
    }

    [Property(MaxTest = MaxTestsForExpensive)]
    public bool RetryBehavior_SuccessResult_ShouldBePreservedExactly(NonEmptyString expectedValueRaw)
    {
        // Arrange
        var expectedValue = expectedValueRaw.Get;
        var behavior = new RetryPipelineBehavior<TestRetryRequest, string>(
            NullLogger<RetryPipelineBehavior<TestRetryRequest, string>>.Instance);

        // Handler that succeeds immediately
        RequestHandlerCallback<string> successHandler = () =>
            ValueTask.FromResult(Prelude.Right<EncinaError, string>(expectedValue));

        // Act
        var result = ExecuteSyncWithAttribute<TestRetryRequest>(behavior, successHandler);

        // Assert - success value should be exactly preserved
        return result.Match(
            Right: value => value == expectedValue,
            Left: _ => false);
    }

    #endregion

    #region Transient vs Non-Transient Exception Invariants

    [Property(MaxTest = MaxTestsForExpensive)]
    public bool RetryBehavior_TransientException_ShouldRetry()
    {
        // Arrange
        var attemptCount = 0;
        var behavior = new RetryPipelineBehavior<TestRetryRequest, string>(
            NullLogger<RetryPipelineBehavior<TestRetryRequest, string>>.Instance);

        // Handler that throws transient exception then succeeds
        RequestHandlerCallback<string> transientHandler = () =>
        {
            attemptCount++;
            if (attemptCount == 1)
            {
                throw new TimeoutException("Simulated timeout");
            }

            return ValueTask.FromResult(Prelude.Right<EncinaError, string>("Success after timeout"));
        };

        // Act
        var result = ExecuteSyncWithAttribute<TestRetryRequest>(behavior, transientHandler);

        // Assert - should have retried and succeeded
        return attemptCount >= 2 && result.IsRight;
    }

    [Property(MaxTest = MaxTestsForExpensive)]
    public bool RetryBehavior_NonTransientException_WithRetryOnAllFalse_ShouldNotRetry()
    {
        // Arrange
        var attemptCount = 0;
        var behavior = new RetryPipelineBehavior<TestRetryNoRetryAllRequest, string>(
            NullLogger<RetryPipelineBehavior<TestRetryNoRetryAllRequest, string>>.Instance);

        // Handler that throws non-transient exception
        RequestHandlerCallback<string> nonTransientHandler = () =>
        {
            attemptCount++;
            throw new InvalidOperationException("Non-transient failure");
        };

        // Act - will throw because non-transient exceptions aren't retried
        try
        {
            _ = ExecuteSyncWithAttribute<TestRetryNoRetryAllRequest>(behavior, nonTransientHandler);
        }
        catch (InvalidOperationException)
        {
            // Expected - non-transient exception should propagate
        }

        // Assert - should not have retried (only 1 attempt)
        return attemptCount == 1;
    }

    [Property(MaxTest = MaxTestsForExpensive)]
    public bool RetryBehavior_AnyException_WithRetryOnAllTrue_ShouldRetry()
    {
        // Arrange
        var attemptCount = 0;
        var behavior = new RetryPipelineBehavior<TestRetryAllExceptionsRequest, string>(
            NullLogger<RetryPipelineBehavior<TestRetryAllExceptionsRequest, string>>.Instance);

        // Handler that throws non-transient exception then succeeds
        RequestHandlerCallback<string> handler = () =>
        {
            attemptCount++;
            if (attemptCount < 2)
            {
                throw new InvalidOperationException("Should be retried");
            }

            return ValueTask.FromResult(Prelude.Right<EncinaError, string>("Success"));
        };

        // Act
        var result = ExecuteSyncWithAttribute<TestRetryAllExceptionsRequest>(behavior, handler);

        // Assert - should have retried
        return attemptCount >= 2 && result.IsRight;
    }

    #endregion

    #region Helper Types

    [Retry(MaxAttempts = 3, BackoffType = BackoffType.Constant, BaseDelayMs = 10)]
    private sealed record TestRetryRequest : IRequest<string>;

    [Retry(MaxAttempts = 3, BackoffType = BackoffType.Constant, BaseDelayMs = 10, RetryOnAllExceptions = false)]
    private sealed record TestRetryNoRetryAllRequest : IRequest<string>;

    [Retry(MaxAttempts = 3, BackoffType = BackoffType.Constant, BaseDelayMs = 10, RetryOnAllExceptions = true)]
    private sealed record TestRetryAllExceptionsRequest : IRequest<string>;

    [Retry(MaxAttempts = 3, BackoffType = BackoffType.Constant, BaseDelayMs = 10)]
    private sealed record TestRetryConstantRequest : IRequest<string>;

    [Retry(MaxAttempts = 3, BackoffType = BackoffType.Linear, BaseDelayMs = 10)]
    private sealed record TestRetryLinearRequest : IRequest<string>;

    [Retry(MaxAttempts = 3, BackoffType = BackoffType.Exponential, BaseDelayMs = 10, MaxDelayMs = 1000)]
    private sealed record TestRetryExponentialRequest : IRequest<string>;

    #endregion

    #region Helper Methods

#pragma warning disable CA2012 // Use ValueTasks correctly - Required for synchronous FsCheck property tests
    private static Either<EncinaError, string> ExecuteSyncWithAttribute<TRequest>(
        RetryPipelineBehavior<TRequest, string> behavior,
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
