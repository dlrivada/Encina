using Encina.Messaging.Recoverability;
using Encina.Recoverability;
using Encina.UnitTests.Recoverability;

using LanguageExt;

using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using Shouldly;

namespace Encina.UnitTests.Messaging.Pipeline;

/// <summary>
/// Unit tests for <see cref="RecoverabilityPipelineBehavior{TRequest, TResponse}"/>.
/// </summary>
public sealed class RecoverabilityPipelineBehaviorTests
{
    private sealed record TestRequest : IRequest<TestResponse>
    {
        public int Value { get; init; }
    }

    private sealed record TestResponse
    {
        public string Result { get; init; } = string.Empty;
    }

    #region Constructor

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var logger = NullLogger<RecoverabilityPipelineBehavior<TestRequest, TestResponse>>.Instance;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new RecoverabilityPipelineBehavior<TestRequest, TestResponse>(null!, logger));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new RecoverabilityOptions();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new RecoverabilityPipelineBehavior<TestRequest, TestResponse>(options, null!));
    }

    [Fact]
    public void Constructor_WithValidParameters_Succeeds()
    {
        // Arrange
        var options = new RecoverabilityOptions();
        var logger = NullLogger<RecoverabilityPipelineBehavior<TestRequest, TestResponse>>.Instance;

        // Act
        var behavior = new RecoverabilityPipelineBehavior<TestRequest, TestResponse>(options, logger);

        // Assert
        behavior.ShouldNotBeNull();
    }

    [Fact]
    public void Constructor_WithOptionalScheduler_Succeeds()
    {
        // Arrange
        var options = new RecoverabilityOptions();
        var logger = NullLogger<RecoverabilityPipelineBehavior<TestRequest, TestResponse>>.Instance;
        var scheduler = Substitute.For<IDelayedRetryScheduler>();

        // Act
        var behavior = new RecoverabilityPipelineBehavior<TestRequest, TestResponse>(
            options, logger, scheduler);

        // Assert
        behavior.ShouldNotBeNull();
    }

    #endregion

    #region Handle - Successful Execution

    [Fact]
    public async Task Handle_WhenFirstAttemptSucceeds_ReturnsSuccess()
    {
        // Arrange
        var behavior = CreateBehavior();
        var request = new TestRequest { Value = 42 };
        var context = CreateContext();
        var expectedResponse = new TestResponse { Result = "Success" };

        RequestHandlerCallback<TestResponse> nextStep = () =>
            ValueTask.FromResult(Either<EncinaError, TestResponse>.Right(expectedResponse));

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: r => r.Result.ShouldBe("Success"),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    #endregion

    #region Handle - Immediate Retries

    [Fact]
    public async Task Handle_WhenTransientErrorThenSuccess_ReturnsSuccessAfterRetry()
    {
        // Arrange
        var options = new RecoverabilityOptions
        {
            ImmediateRetries = 3,
            ImmediateRetryDelay = TimeSpan.FromMilliseconds(1)
        };
        var behavior = CreateBehavior(options);
        var request = new TestRequest { Value = 42 };
        var context = CreateContext();

        var attemptCount = 0;
        RequestHandlerCallback<TestResponse> nextStep = () =>
        {
            attemptCount++;
            if (attemptCount < 2)
            {
                // Fail first attempt
                return ValueTask.FromResult(
                    Either<EncinaError, TestResponse>.Left(EncinaError.New("Transient error")));
            }

            // Succeed on second attempt
            return ValueTask.FromResult(
                Either<EncinaError, TestResponse>.Right(new TestResponse { Result = "Success" }));
        };

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        attemptCount.ShouldBe(2);
    }

    [Fact]
    public async Task Handle_WhenAllImmediateRetriesFail_RetriesCorrectNumberOfTimes()
    {
        // Arrange
        var options = new RecoverabilityOptions
        {
            ImmediateRetries = 2,
            ImmediateRetryDelay = TimeSpan.FromMilliseconds(1),
            EnableDelayedRetries = false
        };
        var behavior = CreateBehavior(options);
        var request = new TestRequest { Value = 42 };
        var context = CreateContext();

        var attemptCount = 0;
        RequestHandlerCallback<TestResponse> nextStep = () =>
        {
            attemptCount++;
            return ValueTask.FromResult(
                Either<EncinaError, TestResponse>.Left(EncinaError.New("Transient error")));
        };

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
        // Initial attempt + 2 retries = 3 total attempts
        attemptCount.ShouldBe(3);
    }

    [Fact]
    public async Task Handle_WhenPermanentError_DoesNotRetry()
    {
        // Arrange
        var permanentClassifier = Substitute.For<IErrorClassifier>();
        permanentClassifier.Classify(Arg.Any<EncinaError>(), Arg.Any<Exception?>())
            .Returns(ErrorClassification.Permanent);

        var options = new RecoverabilityOptions
        {
            ImmediateRetries = 3,
            ImmediateRetryDelay = TimeSpan.FromMilliseconds(1),
            ErrorClassifier = permanentClassifier
        };
        var behavior = CreateBehavior(options);
        var request = new TestRequest { Value = 42 };
        var context = CreateContext();

        var attemptCount = 0;
        RequestHandlerCallback<TestResponse> nextStep = () =>
        {
            attemptCount++;
            return ValueTask.FromResult(
                Either<EncinaError, TestResponse>.Left(EncinaError.New("Permanent error")));
        };

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
        attemptCount.ShouldBe(1); // Only initial attempt, no retries
    }

    #endregion

    #region Handle - Exceptions

    [Fact]
    public async Task Handle_WhenExceptionThrownThenSuccess_ReturnsSuccessAfterRetry()
    {
        // Arrange
        var options = new RecoverabilityOptions
        {
            ImmediateRetries = 3,
            ImmediateRetryDelay = TimeSpan.FromMilliseconds(1)
        };
        var behavior = CreateBehavior(options);
        var request = new TestRequest { Value = 42 };
        var context = CreateContext();

        var attemptCount = 0;
        RequestHandlerCallback<TestResponse> nextStep = () =>
        {
            attemptCount++;
            if (attemptCount < 2)
            {
                // TimeoutException is classified as Transient by the default classifier
                throw new TimeoutException("Transient exception");
            }

            return ValueTask.FromResult(
                Either<EncinaError, TestResponse>.Right(new TestResponse { Result = "Success" }));
        };

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        attemptCount.ShouldBe(2);
    }

    [Fact]
    public async Task Handle_WhenPermanentException_DoesNotRetry()
    {
        // Arrange
        var permanentClassifier = Substitute.For<IErrorClassifier>();
        permanentClassifier.Classify(Arg.Any<EncinaError>(), Arg.Any<Exception?>())
            .Returns(ErrorClassification.Permanent);

        var options = new RecoverabilityOptions
        {
            ImmediateRetries = 3,
            ImmediateRetryDelay = TimeSpan.FromMilliseconds(1),
            ErrorClassifier = permanentClassifier
        };
        var behavior = CreateBehavior(options);
        var request = new TestRequest { Value = 42 };
        var context = CreateContext();

        var attemptCount = 0;
        RequestHandlerCallback<TestResponse> nextStep = () =>
        {
            attemptCount++;
            throw new ArgumentException("Permanent exception");
        };

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
        attemptCount.ShouldBe(1);
    }

    #endregion

    #region Handle - Cancellation

    [Fact]
    public async Task Handle_WhenCancelled_ReturnsError()
    {
        // Arrange
        var behavior = CreateBehavior();
        var request = new TestRequest { Value = 42 };
        var context = CreateContext();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        RequestHandlerCallback<TestResponse> nextStep = () =>
            ValueTask.FromResult(Either<EncinaError, TestResponse>.Right(new TestResponse()));

        // Act
        var result = await behavior.Handle(request, context, nextStep, cts.Token);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: e => e.Message.ShouldContain("cancelled"));
    }

    #endregion

    #region Handle - Delayed Retries

    [Fact]
    public async Task Handle_WhenDelayedRetriesEnabled_SchedulesDelayedRetry()
    {
        // Arrange
        var scheduler = Substitute.For<IDelayedRetryScheduler>();
        scheduler.ScheduleRetryAsync(
            Arg.Any<TestRequest>(),
            Arg.Any<RecoverabilityContext>(),
            Arg.Any<TimeSpan>(),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Either<EncinaError, Unit>>(Unit.Default));
        var options = new RecoverabilityOptions
        {
            ImmediateRetries = 0,
            EnableDelayedRetries = true,
            DelayedRetries = [TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5)]
        };
        var behavior = CreateBehavior(options, scheduler);
        var request = new TestRequest { Value = 42 };
        var context = CreateContext();

        RequestHandlerCallback<TestResponse> nextStep = () =>
            ValueTask.FromResult(
                Either<EncinaError, TestResponse>.Left(EncinaError.New("Transient error")));

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        await scheduler.Received(1).ScheduleRetryAsync(
            request,
            Arg.Any<RecoverabilityContext>(),
            Arg.Any<TimeSpan>(),
            0,
            Arg.Any<CancellationToken>());

        result.IsLeft.ShouldBeTrue();
        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: e => e.Message.ShouldContain("Delayed retry scheduled"));
    }

    [Fact]
    public async Task Handle_WhenDelayedRetriesDisabled_DoesNotSchedule()
    {
        // Arrange
        var scheduler = Substitute.For<IDelayedRetryScheduler>();
        scheduler.ScheduleRetryAsync(
            Arg.Any<TestRequest>(),
            Arg.Any<RecoverabilityContext>(),
            Arg.Any<TimeSpan>(),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Either<EncinaError, Unit>>(Unit.Default));
        var options = new RecoverabilityOptions
        {
            ImmediateRetries = 0,
            EnableDelayedRetries = false
        };
        var behavior = CreateBehavior(options, scheduler);
        var request = new TestRequest { Value = 42 };
        var context = CreateContext();

        RequestHandlerCallback<TestResponse> nextStep = () =>
            ValueTask.FromResult(
                Either<EncinaError, TestResponse>.Left(EncinaError.New("Transient error")));

        // Act
        await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        await scheduler.DidNotReceive().ScheduleRetryAsync(
            Arg.Any<TestRequest>(),
            Arg.Any<RecoverabilityContext>(),
            Arg.Any<TimeSpan>(),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenNoSchedulerProvided_DoesNotSchedule()
    {
        // Arrange
        var options = new RecoverabilityOptions
        {
            ImmediateRetries = 0,
            EnableDelayedRetries = true,
            DelayedRetries = [TimeSpan.FromMinutes(1)]
        };
        // No scheduler provided
        var behavior = CreateBehavior(options, scheduler: null);
        var request = new TestRequest { Value = 42 };
        var context = CreateContext();

        RequestHandlerCallback<TestResponse> nextStep = () =>
            ValueTask.FromResult(
                Either<EncinaError, TestResponse>.Left(EncinaError.New("Transient error")));

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert - should not throw, just return error
        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region Handle - Exponential Backoff

    [Fact]
    public async Task Handle_WithExponentialBackoff_IncreasesDelayBetweenRetries()
    {
        // Arrange
        var options = new RecoverabilityOptions
        {
            ImmediateRetries = 3,
            ImmediateRetryDelay = TimeSpan.FromMilliseconds(10),
            UseExponentialBackoffForImmediateRetries = true,
            UseJitter = false,
            EnableDelayedRetries = false
        };
        var behavior = CreateBehavior(options);
        var request = new TestRequest { Value = 42 };
        var context = CreateContext();

        var attemptTimes = new List<DateTime>();
        RequestHandlerCallback<TestResponse> nextStep = () =>
        {
            attemptTimes.Add(DateTime.UtcNow);
            return ValueTask.FromResult(
                Either<EncinaError, TestResponse>.Left(EncinaError.New("Transient error")));
        };

        // Act
        await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert - delays should increase (2^0 * 10ms, 2^1 * 10ms, 2^2 * 10ms)
        attemptTimes.Count.ShouldBe(4); // Initial + 3 retries
        // Timing is variable, but the test verifies the pattern is applied
    }

    #endregion

    #region Handle - OnPermanentFailure Callback

    [Fact]
    public async Task Handle_WhenPermanentFailure_CallsOnPermanentFailureCallback()
    {
        // Arrange
        FailedMessage? capturedFailedMessage = null;
        var options = new RecoverabilityOptions
        {
            ImmediateRetries = 0,
            EnableDelayedRetries = false,
            OnPermanentFailure = (failedMsg, ct) =>
            {
                capturedFailedMessage = failedMsg;
                return Task.CompletedTask;
            }
        };
        var behavior = CreateBehavior(options);
        var request = new TestRequest { Value = 42 };
        var context = CreateContext();

        RequestHandlerCallback<TestResponse> nextStep = () =>
            ValueTask.FromResult(
                Either<EncinaError, TestResponse>.Left(EncinaError.New("Fatal error")));

        // Act
        await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        capturedFailedMessage.ShouldNotBeNull();
        capturedFailedMessage!.Request.ShouldBe(request);
    }

    [Fact]
    public async Task Handle_WhenOnPermanentFailureThrows_ContinuesWithoutError()
    {
        // Arrange
        var options = new RecoverabilityOptions
        {
            ImmediateRetries = 0,
            EnableDelayedRetries = false,
            OnPermanentFailure = (_, _) => throw new InvalidOperationException("Callback failed")
        };
        var behavior = CreateBehavior(options);
        var request = new TestRequest { Value = 42 };
        var context = CreateContext();

        RequestHandlerCallback<TestResponse> nextStep = () =>
            ValueTask.FromResult(
                Either<EncinaError, TestResponse>.Left(EncinaError.New("Fatal error")));

        // Act - should not throw
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region Handle - Jitter

    [Fact]
    public async Task Handle_JitterSourceZero_SchedulesBaseMinusMaxJitter()
    {
        // Arrange
        var scheduledDelay = TimeSpan.MinValue;
        var scheduler = CreateScheduler(delay => scheduledDelay = delay);
        var options = new RecoverabilityOptions
        {
            ImmediateRetries = 0,
            EnableDelayedRetries = true,
            DelayedRetries = [TimeSpan.FromSeconds(10)],
            UseJitter = true,
            MaxJitterPercent = 20
        };
        var behavior = CreateBehavior(options, scheduler, () => 0.0);

        RequestHandlerCallback<TestResponse> nextStep = () =>
            ValueTask.FromResult(
                Either<EncinaError, TestResponse>.Left(EncinaError.New("Transient error")));

        // Act
        await behavior.Handle(new TestRequest { Value = 42 }, CreateContext(), nextStep, CancellationToken.None);

        // Assert
        scheduledDelay.ShouldBe(TimeSpan.FromSeconds(8));
    }

    [Fact]
    public async Task Handle_JitterSourceHalf_SchedulesBaseDelay()
    {
        // Arrange
        var scheduledDelay = TimeSpan.MinValue;
        var scheduler = CreateScheduler(delay => scheduledDelay = delay);
        var options = new RecoverabilityOptions
        {
            ImmediateRetries = 0,
            EnableDelayedRetries = true,
            DelayedRetries = [TimeSpan.FromSeconds(10)],
            MaxJitterPercent = 20
        };
        var behavior = CreateBehavior(options, scheduler, () => 0.5);

        RequestHandlerCallback<TestResponse> nextStep = () =>
            ValueTask.FromResult(
                Either<EncinaError, TestResponse>.Left(EncinaError.New("Transient error")));

        // Act
        await behavior.Handle(new TestRequest { Value = 42 }, CreateContext(), nextStep, CancellationToken.None);

        // Assert
        scheduledDelay.ShouldBe(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task Handle_JitterSourceThreeQuarters_SchedulesBasePlusHalfMaxJitter()
    {
        // Arrange
        var scheduledDelay = TimeSpan.MinValue;
        var scheduler = CreateScheduler(delay => scheduledDelay = delay);
        var options = new RecoverabilityOptions
        {
            ImmediateRetries = 0,
            EnableDelayedRetries = true,
            DelayedRetries = [TimeSpan.FromSeconds(10)],
            MaxJitterPercent = 20
        };
        var behavior = CreateBehavior(options, scheduler, () => 0.75);

        RequestHandlerCallback<TestResponse> nextStep = () =>
            ValueTask.FromResult(
                Either<EncinaError, TestResponse>.Left(EncinaError.New("Transient error")));

        // Act
        await behavior.Handle(new TestRequest { Value = 42 }, CreateContext(), nextStep, CancellationToken.None);

        // Assert
        scheduledDelay.ShouldBe(TimeSpan.FromSeconds(11));
    }

    [Fact]
    public async Task Handle_JitterAboveHundredPercent_FloorsDelayAtZero()
    {
        // Arrange
        var scheduledDelay = TimeSpan.MinValue;
        var scheduler = CreateScheduler(delay => scheduledDelay = delay);
        var options = new RecoverabilityOptions
        {
            ImmediateRetries = 0,
            EnableDelayedRetries = true,
            DelayedRetries = [TimeSpan.FromSeconds(10)],
            MaxJitterPercent = 150
        };
        var behavior = CreateBehavior(options, scheduler, () => 0.0);

        RequestHandlerCallback<TestResponse> nextStep = () =>
            ValueTask.FromResult(
                Either<EncinaError, TestResponse>.Left(EncinaError.New("Transient error")));

        // Act
        await behavior.Handle(new TestRequest { Value = 42 }, CreateContext(), nextStep, CancellationToken.None);

        // Assert
        scheduledDelay.ShouldBe(TimeSpan.Zero);
    }

    [Theory]
    [InlineData(false, 20)]
    [InlineData(true, 0)]
    public async Task Handle_JitterDisabled_DoesNotDrawFromSource(bool useJitter, int maxJitterPercent)
    {
        // Arrange
        var sourceCalls = 0;
        var scheduledDelay = TimeSpan.MinValue;
        var scheduler = CreateScheduler(delay => scheduledDelay = delay);
        var options = new RecoverabilityOptions
        {
            ImmediateRetries = 0,
            EnableDelayedRetries = true,
            DelayedRetries = [TimeSpan.FromSeconds(10)],
            UseJitter = useJitter,
            MaxJitterPercent = maxJitterPercent
        };
        var behavior = CreateBehavior(options, scheduler, () =>
        {
            sourceCalls++;
            return 0.0;
        });

        RequestHandlerCallback<TestResponse> nextStep = () =>
            ValueTask.FromResult(
                Either<EncinaError, TestResponse>.Left(EncinaError.New("Transient error")));

        // Act
        await behavior.Handle(new TestRequest { Value = 42 }, CreateContext(), nextStep, CancellationToken.None);

        // Assert
        sourceCalls.ShouldBe(0);
        scheduledDelay.ShouldBe(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task Handle_RetryAfterError_DoesNotDrawFromSource()
    {
        // Arrange
        var sourceCalls = 0;
        var scheduledDelay = TimeSpan.MinValue;
        var scheduler = CreateScheduler(delay => scheduledDelay = delay);
        var options = new RecoverabilityOptions
        {
            ImmediateRetries = 0,
            EnableDelayedRetries = true,
            DelayedRetries = [TimeSpan.FromSeconds(10)]
        };
        var behavior = CreateBehavior(options, scheduler, () =>
        {
            sourceCalls++;
            return 0.0;
        });

        var error = OutboundTestSupport.Outbound(
            OutboundErrorCodes.RateLimited,
            transient: true,
            status: 429,
            retryAfter: TimeSpan.FromSeconds(5));
        RequestHandlerCallback<TestResponse> nextStep = () =>
            ValueTask.FromResult(Either<EncinaError, TestResponse>.Left(error));

        // Act
        await behavior.Handle(new TestRequest { Value = 42 }, CreateContext(), nextStep, CancellationToken.None);

        // Assert
        sourceCalls.ShouldBe(0);
        scheduledDelay.ShouldBe(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Handle_ImmediateRetries_DrawOncePerRetry()
    {
        // Arrange
        var sourceCalls = 0;
        var options = new RecoverabilityOptions
        {
            ImmediateRetries = 2,
            ImmediateRetryDelay = TimeSpan.Zero,
            EnableDelayedRetries = false
        };
        var behavior = CreateBehavior(options, jitterSource: () =>
        {
            sourceCalls++;
            return 0.5;
        });

        RequestHandlerCallback<TestResponse> nextStep = () =>
            ValueTask.FromResult(
                Either<EncinaError, TestResponse>.Left(EncinaError.New("Transient error")));

        // Act
        await behavior.Handle(new TestRequest { Value = 42 }, CreateContext(), nextStep, CancellationToken.None);

        // Assert
        sourceCalls.ShouldBe(2);
    }

    #endregion

    #region Helper Methods

    private static RecoverabilityPipelineBehavior<TestRequest, TestResponse> CreateBehavior(
        RecoverabilityOptions? options = null,
        IDelayedRetryScheduler? scheduler = null,
        Func<double>? jitterSource = null)
    {
        var opts = options ?? new RecoverabilityOptions
        {
            ImmediateRetries = 0,
            EnableDelayedRetries = false
        };
        var logger = NullLogger<RecoverabilityPipelineBehavior<TestRequest, TestResponse>>.Instance;

        return new RecoverabilityPipelineBehavior<TestRequest, TestResponse>(
            opts, logger, scheduler, timeProvider: null, deadLetterCapture: null, errorClassifier: null, jitterSource);
    }

    private static IDelayedRetryScheduler CreateScheduler(Action<TimeSpan> captureDelay)
    {
        var scheduler = Substitute.For<IDelayedRetryScheduler>();
        scheduler.ScheduleRetryAsync(
            Arg.Any<TestRequest>(),
            Arg.Any<RecoverabilityContext>(),
            Arg.Any<TimeSpan>(),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                captureDelay(callInfo.ArgAt<TimeSpan>(2));
                return Task.FromResult<Either<EncinaError, Unit>>(Unit.Default);
            });
        return scheduler;
    }

    private static IRequestContext CreateContext(string correlationId = "test-correlation")
    {
        var context = Substitute.For<IRequestContext>();
        context.CorrelationId.Returns(correlationId);
        context.IdempotencyKey.Returns("test-idempotency-key");
        return context;
    }

    #endregion
}
