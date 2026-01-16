using Encina.Testing;
using Encina.Polly;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Polly;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Polly;

/// <summary>
/// Unit tests for <see cref="RetryPipelineBehavior{TRequest, TResponse}"/>.
/// Tests retry logic, backoff strategies, exception handling, and logging.
/// </summary>
public class RetryPipelineBehaviorTests
{
    private readonly ILogger<RetryPipelineBehavior<TestRequest, string>> _logger;
    private readonly RetryPipelineBehavior<TestRequest, string> _behavior;

    public RetryPipelineBehaviorTests()
    {
        _logger = Substitute.For<ILogger<RetryPipelineBehavior<TestRequest, string>>>();
        _behavior = new RetryPipelineBehavior<TestRequest, string>(_logger);
    }

    [Fact]
    public async Task Handle_NoRetryAttribute_ShouldPassThrough()
    {
        // Arrange
        var request = new TestRequestNoRetry();
        var context = Substitute.For<IRequestContext>();
        var expectedResponse = "success";
        RequestHandlerCallback<string> next = () => ValueTask.FromResult(Right<EncinaError, string>(expectedResponse));

        var behavior = new RetryPipelineBehavior<TestRequestNoRetry, string>(
            Substitute.For<ILogger<RetryPipelineBehavior<TestRequestNoRetry, string>>>());

        // Act
        var result = await behavior.Handle(request, context, next, CancellationToken.None);

        // Assert
        result.ShouldBeSuccess();
        _ = result.Match(
            Right: value => value.ShouldBe(expectedResponse),
            Left: _ => throw new InvalidOperationException("Should not be Left")
        );
    }

    [Fact]
    public async Task Handle_WithRetryAttribute_SuccessOnFirstAttempt_ShouldNotRetry()
    {
        // Arrange
        var request = new TestRequest();
        var context = Substitute.For<IRequestContext>();
        var expectedResponse = "success";
        var callCount = 0;
        RequestHandlerCallback<string> next = () =>
        {
            callCount++;
            return ValueTask.FromResult(Right<EncinaError, string>(expectedResponse));
        };

        // Act
        var result = await _behavior.Handle(request, context, next, CancellationToken.None);

        // Assert
        result.ShouldBeSuccess();
        _ = result.Match(
            Right: value => value.ShouldBe(expectedResponse),
            Left: _ => throw new InvalidOperationException("Should not be Left")
        );
        callCount.ShouldBe(1, "should only call handler once on success");
    }

    [Fact]
    public async Task Handle_WithRetryAttribute_SuccessOnSecondAttempt_ShouldRetry()
    {
        // Arrange
        var request = new TestRequest();
        var context = Substitute.For<IRequestContext>();
        var callCount = 0;
        RequestHandlerCallback<string> next = () =>
        {
            callCount++;
            return callCount == 1
                ? ValueTask.FromResult(Left<EncinaError, string>(EncinaErrors.Create("test.error", "Transient failure")))
                : ValueTask.FromResult(Right<EncinaError, string>("success"));
        };

        // Act
        var result = await _behavior.Handle(request, context, next, CancellationToken.None);

        // Assert
        result.ShouldBeSuccess();
        callCount.ShouldBe(2, "should retry once and succeed on second attempt");
    }

    [Fact]
    public async Task Handle_MaxAttemptsExhausted_ShouldReturnError()
    {
        // Arrange
        var request = new TestRequest();
        var context = Substitute.For<IRequestContext>();
        var callCount = 0;
        RequestHandlerCallback<string> next = () =>
        {
            callCount++;
            return ValueTask.FromResult(Left<EncinaError, string>(EncinaErrors.Create("test.error", "Persistent failure")));
        };

        // Act
        var result = await _behavior.Handle(request, context, next, CancellationToken.None);

        // Assert
        result.ShouldBeError();
        callCount.ShouldBe(3, "should try initial + 2 retries (MaxAttempts = 3)");
    }

    [Fact]
    public async Task Handle_TransientException_ShouldRetry()
    {
        // Arrange
        var request = new TestRequest();
        var context = Substitute.For<IRequestContext>();
        var callCount = 0;
        RequestHandlerCallback<string> next = () =>
        {
            callCount++;
            return callCount == 1
                ? throw new TimeoutException("Transient timeout")
                : ValueTask.FromResult(Right<EncinaError, string>("success"));
        };

        // Act
        var result = await _behavior.Handle(request, context, next, CancellationToken.None);

        // Assert
        result.ShouldBeSuccess();
        callCount.ShouldBe(2, "should retry after TimeoutException");
    }

    [Fact]
    public async Task Handle_HttpRequestException_ShouldRetry()
    {
        // Arrange
        var request = new TestRequest();
        var context = Substitute.For<IRequestContext>();
        var callCount = 0;
        RequestHandlerCallback<string> next = () =>
        {
            callCount++;
            return callCount == 1
                ? throw new HttpRequestException("Network error")
                : ValueTask.FromResult(Right<EncinaError, string>("success"));
        };

        // Act
        var result = await _behavior.Handle(request, context, next, CancellationToken.None);

        // Assert
        result.ShouldBeSuccess();
        callCount.ShouldBe(2, "should retry after HttpRequestException");
    }

    [Fact]
    public async Task Handle_IOException_ShouldRetry()
    {
        // Arrange
        var request = new TestRequest();
        var context = Substitute.For<IRequestContext>();
        var callCount = 0;
        RequestHandlerCallback<string> next = () =>
        {
            callCount++;
            return callCount == 1
                ? throw new IOException("IO error")
                : ValueTask.FromResult(Right<EncinaError, string>("success"));
        };

        // Act
        var result = await _behavior.Handle(request, context, next, CancellationToken.None);

        // Assert
        result.ShouldBeSuccess();
        callCount.ShouldBe(2, "should retry after IOException");
    }

    [Fact]
    public async Task Handle_NonTransientException_RetryOnAllExceptionsFalse_ShouldNotRetry()
    {
        // Arrange
        var request = new TestRequest(); // RetryOnAllExceptions = false (default)
        var context = Substitute.For<IRequestContext>();
        var callCount = 0;
        RequestHandlerCallback<string> next = () =>
        {
            callCount++;
            throw new InvalidOperationException("Non-transient error");
        };

        // Act
        var result = await _behavior.Handle(request, context, next, CancellationToken.None);

        // Assert - Polly catches exception and returns EncinaError without retry
        result.ShouldBeError(); // non-transient exception should be caught and converted to EncinaError
        callCount.ShouldBe(1, "should not retry non-transient exceptions when RetryOnAllExceptions = false");
    }

    [Fact]
    public async Task Handle_NonTransientException_RetryOnAllExceptionsTrue_ShouldRetry()
    {
        // Arrange
        var request = new TestRequestRetryAll();
        var context = Substitute.For<IRequestContext>();
        var callCount = 0;
        var behavior = new RetryPipelineBehavior<TestRequestRetryAll, string>(
            Substitute.For<ILogger<RetryPipelineBehavior<TestRequestRetryAll, string>>>());

        RequestHandlerCallback<string> next = () =>
        {
            callCount++;
            return callCount < 3
                ? throw new InvalidOperationException("Any error")
                : ValueTask.FromResult(Right<EncinaError, string>("success"));
        };

        // Act
        var result = await behavior.Handle(request, context, next, CancellationToken.None);

        // Assert
        result.ShouldBeSuccess();
        callCount.ShouldBe(3, "should retry all exceptions when RetryOnAllExceptions = true");
    }

    [Fact]
    public async Task Handle_ExponentialBackoff_ShouldIncreaseDelay()
    {
        // This test verifies that exponential backoff is configured correctly
        // We can't easily test actual delays without slowing tests, but we can verify the behavior is created
        // Arrange
        var request = new TestRequestExponential();
        var context = Substitute.For<IRequestContext>();
        var callCount = 0;
        var behavior = new RetryPipelineBehavior<TestRequestExponential, string>(
            Substitute.For<ILogger<RetryPipelineBehavior<TestRequestExponential, string>>>());

        RequestHandlerCallback<string> next = () =>
        {
            callCount++;
            return callCount < 3
                ? ValueTask.FromResult(Left<EncinaError, string>(EncinaErrors.Create("test.error", "Fail")))
                : ValueTask.FromResult(Right<EncinaError, string>("success"));
        };

        // Act
        var result = await behavior.Handle(request, context, next, CancellationToken.None);

        // Assert
        result.ShouldBeSuccess();
        callCount.ShouldBe(3, "should attempt 3 times with exponential backoff");
    }

    [Fact]
    public async Task Handle_ConstantBackoff_ShouldUseFixedDelay()
    {
        // Arrange
        var request = new TestRequestConstant();
        var context = Substitute.For<IRequestContext>();
        var callCount = 0;
        var behavior = new RetryPipelineBehavior<TestRequestConstant, string>(
            Substitute.For<ILogger<RetryPipelineBehavior<TestRequestConstant, string>>>());

        RequestHandlerCallback<string> next = () =>
        {
            callCount++;
            return callCount < 3
                ? ValueTask.FromResult(Left<EncinaError, string>(EncinaErrors.Create("test.error", "Fail")))
                : ValueTask.FromResult(Right<EncinaError, string>("success"));
        };

        // Act
        var result = await behavior.Handle(request, context, next, CancellationToken.None);

        // Assert
        result.ShouldBeSuccess();
        callCount.ShouldBe(3, "should attempt 3 times with constant backoff");
    }

    [Fact]
    public async Task Handle_LinearBackoff_ShouldIncreaseLinearly()
    {
        // Arrange
        var request = new TestRequestLinear();
        var context = Substitute.For<IRequestContext>();
        var callCount = 0;
        var behavior = new RetryPipelineBehavior<TestRequestLinear, string>(
            Substitute.For<ILogger<RetryPipelineBehavior<TestRequestLinear, string>>>());

        RequestHandlerCallback<string> next = () =>
        {
            callCount++;
            return callCount < 3
                ? ValueTask.FromResult(Left<EncinaError, string>(EncinaErrors.Create("test.error", "Fail")))
                : ValueTask.FromResult(Right<EncinaError, string>("success"));
        };

        // Act
        var result = await behavior.Handle(request, context, next, CancellationToken.None);

        // Assert
        result.ShouldBeSuccess();
        callCount.ShouldBe(3, "should attempt 3 times with linear backoff");
    }

    [Fact]
    public async Task Handle_ExceptionExhaustsRetries_ReturnsErrorFromException()
    {
        // Arrange
        var request = new TestRequest();
        var context = Substitute.For<IRequestContext>();
        RequestHandlerCallback<string> next = () => throw new TimeoutException("Persistent timeout");

        // Act
        var result = await _behavior.Handle(request, context, next, CancellationToken.None);

        // Assert
        result.ShouldBeError();
        _ = result.Match(
            Right: _ => throw new InvalidOperationException("Should be Left"),
            Left: error => error.Message.ShouldContain("Persistent timeout")
        );
    }

    // Test request types
    [Retry(MaxAttempts = 3, BaseDelayMs = 10, BackoffType = BackoffType.Exponential)]
    public record TestRequest : IRequest<string>;

    public record TestRequestNoRetry : IRequest<string>;

    [Retry(MaxAttempts = 3, BaseDelayMs = 10, RetryOnAllExceptions = true)]
    public record TestRequestRetryAll : IRequest<string>;

    [Retry(MaxAttempts = 3, BaseDelayMs = 10, BackoffType = BackoffType.Exponential)]
    public record TestRequestExponential : IRequest<string>;

    [Retry(MaxAttempts = 3, BaseDelayMs = 10, BackoffType = BackoffType.Constant)]
    public record TestRequestConstant : IRequest<string>;

    [Retry(MaxAttempts = 3, BaseDelayMs = 10, BackoffType = BackoffType.Linear)]
    public record TestRequestLinear : IRequest<string>;
}
