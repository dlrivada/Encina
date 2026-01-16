using Encina.Polly;
using Encina.Testing;
using LanguageExt;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Polly;

/// <summary>
/// Unit tests for <see cref="RateLimitingPipelineBehavior{TRequest, TResponse}"/>.
/// Tests rate limiting integration, adaptive throttling, and error handling.
/// </summary>
public class RateLimitingPipelineBehaviorTests
{
    private readonly ILogger<RateLimitingPipelineBehavior<TestRateLimitedRequest, string>> _logger;
    private readonly IRateLimiter _rateLimiter;
    private readonly RateLimitingPipelineBehavior<TestRateLimitedRequest, string> _behavior;

    public RateLimitingPipelineBehaviorTests()
    {
        _logger = Substitute.For<ILogger<RateLimitingPipelineBehavior<TestRateLimitedRequest, string>>>();
        _rateLimiter = Substitute.For<IRateLimiter>();
        _behavior = new RateLimitingPipelineBehavior<TestRateLimitedRequest, string>(_logger, _rateLimiter);
    }

    #region Pass-Through Tests

    [Fact]
    public async Task Handle_NoRateLimitAttribute_ShouldPassThrough()
    {
        // Arrange
        var request = new TestRequestNoRateLimit();
        var context = Substitute.For<IRequestContext>();
        var expectedResponse = "success";
        RequestHandlerCallback<string> next = () => ValueTask.FromResult(Right<EncinaError, string>(expectedResponse));

        var behavior = new RateLimitingPipelineBehavior<TestRequestNoRateLimit, string>(
            Substitute.For<ILogger<RateLimitingPipelineBehavior<TestRequestNoRateLimit, string>>>(),
            _rateLimiter);

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
    public async Task Handle_NoRateLimitAttribute_ShouldNotCallRateLimiter()
    {
        // Arrange
        var request = new TestRequestNoRateLimit();
        var context = Substitute.For<IRequestContext>();
        RequestHandlerCallback<string> next = () => ValueTask.FromResult(Right<EncinaError, string>("success"));

        var behavior = new RateLimitingPipelineBehavior<TestRequestNoRateLimit, string>(
            Substitute.For<ILogger<RateLimitingPipelineBehavior<TestRequestNoRateLimit, string>>>(),
            _rateLimiter);

        // Act
        await behavior.Handle(request, context, next, CancellationToken.None);

        // Assert
        _ = await _rateLimiter.DidNotReceive().AcquireAsync(Arg.Any<string>(), Arg.Any<RateLimitAttribute>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region Rate Limiting Tests

    [Fact]
    public async Task Handle_WithinRateLimit_ShouldAllow()
    {
        // Arrange
        var request = new TestRateLimitedRequest();
        var context = Substitute.For<IRequestContext>();
        var expectedResponse = "success";
        RequestHandlerCallback<string> next = () => ValueTask.FromResult(Right<EncinaError, string>(expectedResponse));

        SetupAcquireReturns(RateLimitResult.Allowed(RateLimitState.Normal, 1, 100, 0.0));

        // Act
        var result = await _behavior.Handle(request, context, next, CancellationToken.None);

        // Assert
        result.ShouldBeSuccess();
        _ = result.Match(
            Right: value => value.ShouldBe(expectedResponse),
            Left: _ => throw new InvalidOperationException("Should not be Left")
        );
    }

    [Fact]
    public async Task Handle_RateLimitExceeded_ShouldReturnError()
    {
        // Arrange
        var request = new TestRateLimitedRequest();
        var context = Substitute.For<IRequestContext>();
        RequestHandlerCallback<string> next = () => ValueTask.FromResult(Right<EncinaError, string>("success"));

        SetupAcquireReturns(RateLimitResult.Denied(
            RateLimitState.Normal,
            TimeSpan.FromSeconds(30),
            100,
            100,
            0.0));

        // Act
        var result = await _behavior.Handle(request, context, next, CancellationToken.None);

        // Assert
        result.ShouldBeError();
        _ = result.Match(
            Right: _ => throw new InvalidOperationException("Should be Left"),
            Left: error => error.Message.ShouldContain("Rate limit exceeded")
        );
    }

    [Fact]
    public async Task Handle_RateLimitExceeded_ShouldNotCallNextStep()
    {
        // Arrange
        var request = new TestRateLimitedRequest();
        var context = Substitute.For<IRequestContext>();
        var nextCalled = false;
        RequestHandlerCallback<string> next = () =>
        {
            nextCalled = true;
            return ValueTask.FromResult(Right<EncinaError, string>("success"));
        };

        SetupAcquireReturns(RateLimitResult.Denied(
            RateLimitState.Throttled,
            TimeSpan.FromSeconds(30),
            10,
            10,
            50.0));

        // Act
        await _behavior.Handle(request, context, next, CancellationToken.None);

        // Assert
        nextCalled.ShouldBeFalse("next step should not be called when rate limit is exceeded");
    }

    #endregion

    #region Success/Failure Recording Tests

    [Fact]
    public async Task Handle_SuccessfulExecution_ShouldRecordSuccess()
    {
        // Arrange
        var request = new TestRateLimitedRequest();
        var context = Substitute.For<IRequestContext>();
        RequestHandlerCallback<string> next = () => ValueTask.FromResult(Right<EncinaError, string>("success"));

        SetupAcquireReturns(RateLimitResult.Allowed(RateLimitState.Normal, 1, 100, 0.0));

        // Act
        await _behavior.Handle(request, context, next, CancellationToken.None);

        // Assert
        _rateLimiter.Received(1).RecordSuccess(Arg.Is<string>(s => s == nameof(TestRateLimitedRequest)));
    }

    [Fact]
    public async Task Handle_FailedExecution_ShouldRecordFailure()
    {
        // Arrange
        var request = new TestRateLimitedRequest();
        var context = Substitute.For<IRequestContext>();
        RequestHandlerCallback<string> next = () => ValueTask.FromResult(Left<EncinaError, string>(EncinaErrors.Create("test.error", "Test failure")));

        SetupAcquireReturns(RateLimitResult.Allowed(RateLimitState.Normal, 1, 100, 0.0));

        // Act
        await _behavior.Handle(request, context, next, CancellationToken.None);

        // Assert
        _rateLimiter.Received(1).RecordFailure(Arg.Is<string>(s => s == nameof(TestRateLimitedRequest)));
    }

    [Fact]
    public async Task Handle_ExceptionInNextStep_ShouldRecordFailureAndRethrow()
    {
        // Arrange
        var request = new TestRateLimitedRequest();
        var context = Substitute.For<IRequestContext>();
        RequestHandlerCallback<string> next = () => throw new InvalidOperationException("Handler exception");

        SetupAcquireReturns(RateLimitResult.Allowed(RateLimitState.Normal, 1, 100, 0.0));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _behavior.Handle(request, context, next, CancellationToken.None).AsTask());

        _rateLimiter.Received(1).RecordFailure(Arg.Is<string>(s => s == nameof(TestRateLimitedRequest)));
    }

    #endregion

    #region State-Specific Tests

    [Fact]
    public async Task Handle_ThrottledState_ShouldIncludeStateInError()
    {
        // Arrange
        var request = new TestRateLimitedRequest();
        var context = Substitute.For<IRequestContext>();
        RequestHandlerCallback<string> next = () => ValueTask.FromResult(Right<EncinaError, string>("success"));

        SetupAcquireReturns(RateLimitResult.Denied(
            RateLimitState.Throttled,
            TimeSpan.FromSeconds(60),
            10,
            10,
            75.0));

        // Act
        var result = await _behavior.Handle(request, context, next, CancellationToken.None);

        // Assert
        result.ShouldBeError();
        _ = result.Match(
            Right: _ => throw new InvalidOperationException("Should be Left"),
            Left: error =>
            {
                error.Message.ShouldContain("Throttled");
                error.Message.ShouldContain("10/10");
                return Unit.Default;
            }
        );
    }

    [Fact]
    public async Task Handle_RecoveringState_ShouldAllowReducedCapacity()
    {
        // Arrange
        var request = new TestRateLimitedRequest();
        var context = Substitute.For<IRequestContext>();
        RequestHandlerCallback<string> next = () => ValueTask.FromResult(Right<EncinaError, string>("success"));

        SetupAcquireReturns(RateLimitResult.Allowed(RateLimitState.Recovering, 5, 20, 25.0));

        // Act
        var result = await _behavior.Handle(request, context, next, CancellationToken.None);

        // Assert
        result.ShouldBeSuccess();
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task Handle_WithRealRateLimiter_ShouldEnforceLimit()
    {
        // Arrange
        var realRateLimiter = new AdaptiveRateLimiter();
        var behavior = new RateLimitingPipelineBehavior<TestRateLimitedSmall, string>(
            Substitute.For<ILogger<RateLimitingPipelineBehavior<TestRateLimitedSmall, string>>>(),
            realRateLimiter);

        var context = Substitute.For<IRequestContext>();
        RequestHandlerCallback<string> next = () => ValueTask.FromResult(Right<EncinaError, string>("success"));

        // Act - Make 3 requests (limit is 2)
        var result1 = await behavior.Handle(new TestRateLimitedSmall(), context, next, CancellationToken.None);
        var result2 = await behavior.Handle(new TestRateLimitedSmall(), context, next, CancellationToken.None);
        var result3 = await behavior.Handle(new TestRateLimitedSmall(), context, next, CancellationToken.None);

        // Assert
        result1.ShouldBeSuccess();
        result2.ShouldBeSuccess();
        result3.ShouldBeError();
    }

    #endregion

    #region Helper Methods

    private void SetupAcquireReturns(RateLimitResult result)
    {
#pragma warning disable CA2012 // Use ValueTasks correctly - Required for NSubstitute mocking
        _ = _rateLimiter.AcquireAsync(Arg.Any<string>(), Arg.Any<RateLimitAttribute>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<RateLimitResult>(result));
#pragma warning restore CA2012
    }

    #endregion

    // Test request types
    [RateLimit(MaxRequestsPerWindow = 100, WindowSizeSeconds = 60)]
    public record TestRateLimitedRequest : IRequest<string>;

    [RateLimit(MaxRequestsPerWindow = 2, WindowSizeSeconds = 60)]
    public record TestRateLimitedSmall : IRequest<string>;

    public record TestRequestNoRateLimit : IRequest<string>;
}
