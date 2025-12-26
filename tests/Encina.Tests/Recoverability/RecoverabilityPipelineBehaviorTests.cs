using Encina.Messaging.Recoverability;
using LanguageExt;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace Encina.Tests.Recoverability;

public sealed class RecoverabilityPipelineBehaviorTests
{
    private readonly RecoverabilityOptions _options;
    private readonly ILogger<RecoverabilityPipelineBehavior<TestCommand, TestResponse>> _logger;
    private readonly IDelayedRetryScheduler _delayedRetryScheduler;
    private readonly IRequestContext _requestContext;

    public RecoverabilityPipelineBehaviorTests()
    {
        _options = new RecoverabilityOptions
        {
            ImmediateRetries = 3,
            ImmediateRetryDelay = TimeSpan.FromMilliseconds(1), // Fast for tests
            UseExponentialBackoffForImmediateRetries = false,
            UseJitter = false
        };
        _logger = Substitute.For<ILogger<RecoverabilityPipelineBehavior<TestCommand, TestResponse>>>();
        _delayedRetryScheduler = Substitute.For<IDelayedRetryScheduler>();
        _requestContext = Substitute.For<IRequestContext>();
        _requestContext.CorrelationId.Returns("test-correlation");
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new RecoverabilityPipelineBehavior<TestCommand, TestResponse>(null!, _logger));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new RecoverabilityPipelineBehavior<TestCommand, TestResponse>(_options, null!));
    }

    [Fact]
    public async Task Handle_SuccessOnFirstAttempt_ReturnsSuccess()
    {
        // Arrange
        var behavior = new RecoverabilityPipelineBehavior<TestCommand, TestResponse>(_options, _logger);
        var request = new TestCommand();
        var expectedResponse = new TestResponse { Value = "Success" };

        RequestHandlerCallback<TestResponse> nextStep = () =>
            new ValueTask<Either<EncinaError, TestResponse>>(Either<EncinaError, TestResponse>.Right(expectedResponse));

        // Act
        var result = await behavior.Handle(request, _requestContext, nextStep, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: r => r.Value.ShouldBe("Success"),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task Handle_SuccessAfterRetries_ReturnsSuccess()
    {
        // Arrange
        var behavior = new RecoverabilityPipelineBehavior<TestCommand, TestResponse>(_options, _logger);
        var request = new TestCommand();
        var expectedResponse = new TestResponse { Value = "Success after retry" };
        var attemptCount = 0;

        RequestHandlerCallback<TestResponse> nextStep = () =>
        {
            attemptCount++;
            if (attemptCount < 3)
            {
                return new ValueTask<Either<EncinaError, TestResponse>>(
                    Either<EncinaError, TestResponse>.Left(EncinaError.New("Transient error")));
            }
            return new ValueTask<Either<EncinaError, TestResponse>>(
                Either<EncinaError, TestResponse>.Right(expectedResponse));
        };

        // Act
        var result = await behavior.Handle(request, _requestContext, nextStep, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        attemptCount.ShouldBe(3);
    }

    [Fact]
    public async Task Handle_PermanentError_DoesNotRetry()
    {
        // Arrange
        var options = new RecoverabilityOptions
        {
            ImmediateRetries = 3,
            ImmediateRetryDelay = TimeSpan.FromMilliseconds(1),
            EnableDelayedRetries = false
        };
        var behavior = new RecoverabilityPipelineBehavior<TestCommand, TestResponse>(options, _logger);
        var request = new TestCommand();
        var attemptCount = 0;
        var permanentException = new ArgumentException("Validation failed");

        RequestHandlerCallback<TestResponse> nextStep = () =>
        {
            attemptCount++;
            return new ValueTask<Either<EncinaError, TestResponse>>(
                Either<EncinaError, TestResponse>.Left(EncinaError.New(permanentException)));
        };

        // Act
        var result = await behavior.Handle(request, _requestContext, nextStep, CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
        attemptCount.ShouldBe(1); // No retries for permanent errors
    }

    [Fact]
    public async Task Handle_AllImmediateRetriesExhausted_SchedulesDelayedRetry()
    {
        // Arrange
        var options = new RecoverabilityOptions
        {
            ImmediateRetries = 2,
            ImmediateRetryDelay = TimeSpan.FromMilliseconds(1),
            EnableDelayedRetries = true,
            DelayedRetries = [TimeSpan.FromMinutes(1)]
        };
        var behavior = new RecoverabilityPipelineBehavior<TestCommand, TestResponse>(
            options, _logger, _delayedRetryScheduler);
        var request = new TestCommand();

        RequestHandlerCallback<TestResponse> nextStep = () =>
            new ValueTask<Either<EncinaError, TestResponse>>(
                Either<EncinaError, TestResponse>.Left(EncinaError.New("Transient error")));

        // Act
        var result = await behavior.Handle(request, _requestContext, nextStep, CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
        await _delayedRetryScheduler.Received(1).ScheduleRetryAsync(
            request,
            Arg.Any<RecoverabilityContext>(),
            Arg.Any<TimeSpan>(),
            0,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AllImmediateRetriesExhausted_WithoutScheduler_DoesNotThrow()
    {
        // Arrange
        var options = new RecoverabilityOptions
        {
            ImmediateRetries = 2,
            ImmediateRetryDelay = TimeSpan.FromMilliseconds(1),
            EnableDelayedRetries = true,
            DelayedRetries = [TimeSpan.FromMinutes(1)]
        };
        // No scheduler provided
        var behavior = new RecoverabilityPipelineBehavior<TestCommand, TestResponse>(options, _logger);
        var request = new TestCommand();

        RequestHandlerCallback<TestResponse> nextStep = () =>
            new ValueTask<Either<EncinaError, TestResponse>>(
                Either<EncinaError, TestResponse>.Left(EncinaError.New("Transient error")));

        // Act
        var result = await behavior.Handle(request, _requestContext, nextStep, CancellationToken.None);

        // Assert - Should not throw, just return the error
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_DelayedRetriesDisabled_DoesNotSchedule()
    {
        // Arrange
        var options = new RecoverabilityOptions
        {
            ImmediateRetries = 2,
            ImmediateRetryDelay = TimeSpan.FromMilliseconds(1),
            EnableDelayedRetries = false
        };
        var behavior = new RecoverabilityPipelineBehavior<TestCommand, TestResponse>(
            options, _logger, _delayedRetryScheduler);
        var request = new TestCommand();

        RequestHandlerCallback<TestResponse> nextStep = () =>
            new ValueTask<Either<EncinaError, TestResponse>>(
                Either<EncinaError, TestResponse>.Left(EncinaError.New("Transient error")));

        // Act
        var result = await behavior.Handle(request, _requestContext, nextStep, CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
        await _delayedRetryScheduler.DidNotReceive().ScheduleRetryAsync(
            Arg.Any<TestCommand>(),
            Arg.Any<RecoverabilityContext>(),
            Arg.Any<TimeSpan>(),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExceptionThrown_IsClassifiedAndRetried()
    {
        // Arrange
        var options = new RecoverabilityOptions
        {
            ImmediateRetries = 3,
            ImmediateRetryDelay = TimeSpan.FromMilliseconds(1),
            EnableDelayedRetries = false
        };
        var behavior = new RecoverabilityPipelineBehavior<TestCommand, TestResponse>(options, _logger);
        var request = new TestCommand();
        var attemptCount = 0;

        RequestHandlerCallback<TestResponse> nextStep = () =>
        {
            attemptCount++;
            if (attemptCount < 3)
            {
                throw new TimeoutException("Transient timeout");
            }
            return new ValueTask<Either<EncinaError, TestResponse>>(
                Either<EncinaError, TestResponse>.Right(new TestResponse { Value = "Success" }));
        };

        // Act
        var result = await behavior.Handle(request, _requestContext, nextStep, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        attemptCount.ShouldBe(3);
    }

    [Fact]
    public async Task Handle_PermanentExceptionThrown_DoesNotRetry()
    {
        // Arrange
        var options = new RecoverabilityOptions
        {
            ImmediateRetries = 3,
            ImmediateRetryDelay = TimeSpan.FromMilliseconds(1),
            EnableDelayedRetries = false
        };
        var behavior = new RecoverabilityPipelineBehavior<TestCommand, TestResponse>(options, _logger);
        var request = new TestCommand();
        var attemptCount = 0;

        RequestHandlerCallback<TestResponse> nextStep = () =>
        {
            attemptCount++;
            throw new ArgumentException("Permanent validation error");
        };

        // Act
        var result = await behavior.Handle(request, _requestContext, nextStep, CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
        attemptCount.ShouldBe(1);
    }

    [Fact]
    public async Task Handle_CancellationRequested_ReturnsCancelled()
    {
        // Arrange
        var behavior = new RecoverabilityPipelineBehavior<TestCommand, TestResponse>(_options, _logger);
        var request = new TestCommand();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        RequestHandlerCallback<TestResponse> nextStep = () =>
            new ValueTask<Either<EncinaError, TestResponse>>(
                Either<EncinaError, TestResponse>.Right(new TestResponse { Value = "Success" }));

        // Act
        var result = await behavior.Handle(request, _requestContext, nextStep, cts.Token);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: e => e.Message.ShouldContain("cancelled"));
    }

    [Fact]
    public async Task Handle_OnPermanentFailure_IsInvoked()
    {
        // Arrange
        FailedMessage? capturedMessage = null;
        var options = new RecoverabilityOptions
        {
            ImmediateRetries = 1,
            ImmediateRetryDelay = TimeSpan.FromMilliseconds(1),
            EnableDelayedRetries = false,
            OnPermanentFailure = (msg, _) =>
            {
                capturedMessage = msg;
                return Task.CompletedTask;
            }
        };
        var behavior = new RecoverabilityPipelineBehavior<TestCommand, TestResponse>(options, _logger);
        var request = new TestCommand();

        RequestHandlerCallback<TestResponse> nextStep = () =>
            new ValueTask<Either<EncinaError, TestResponse>>(
                Either<EncinaError, TestResponse>.Left(EncinaError.New("Transient but exhausted")));

        // Act
        var result = await behavior.Handle(request, _requestContext, nextStep, CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
        capturedMessage.ShouldNotBeNull();
        capturedMessage!.Request.ShouldBe(request);
    }

    [Fact]
    public async Task Handle_OnPermanentFailureThrows_DoesNotPropagateException()
    {
        // Arrange
        var options = new RecoverabilityOptions
        {
            ImmediateRetries = 1,
            ImmediateRetryDelay = TimeSpan.FromMilliseconds(1),
            EnableDelayedRetries = false,
            OnPermanentFailure = (_, _) => throw new InvalidOperationException("Callback failed")
        };
        var behavior = new RecoverabilityPipelineBehavior<TestCommand, TestResponse>(options, _logger);
        var request = new TestCommand();

        RequestHandlerCallback<TestResponse> nextStep = () =>
            new ValueTask<Either<EncinaError, TestResponse>>(
                Either<EncinaError, TestResponse>.Left(EncinaError.New("Error")));

        // Act & Assert - Should not throw
        var result = await behavior.Handle(request, _requestContext, nextStep, CancellationToken.None);
        result.IsLeft.ShouldBeTrue();
    }

    public sealed class TestCommand : IRequest<TestResponse>
    {
    }

    public sealed class TestResponse
    {
        public required string Value { get; init; }
    }
}
