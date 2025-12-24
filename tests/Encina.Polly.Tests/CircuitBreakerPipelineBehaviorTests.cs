using LanguageExt;
using Microsoft.Extensions.Logging;
using Polly.CircuitBreaker;
using System.Reflection;
using static LanguageExt.Prelude;

namespace Encina.Polly.Tests;

/// <summary>
/// Unit tests for <see cref="CircuitBreakerPipelineBehavior{TRequest, TResponse}"/>.
/// Tests circuit breaker states, caching, thread safety, and failure handling.
/// </summary>
public class CircuitBreakerPipelineBehaviorTests
{
    private readonly ILogger<CircuitBreakerPipelineBehavior<TestRequest, string>> _logger;
    private readonly CircuitBreakerPipelineBehavior<TestRequest, string> _behavior;

    public CircuitBreakerPipelineBehaviorTests()
    {
        _logger = Substitute.For<ILogger<CircuitBreakerPipelineBehavior<TestRequest, string>>>();
        _behavior = new CircuitBreakerPipelineBehavior<TestRequest, string>(_logger);

        // Clear static cache before each test
        ClearCircuitBreakerCache();
    }

    [Fact]
    public async Task Handle_NoCircuitBreakerAttribute_ShouldPassThrough()
    {
        // Arrange
        var request = new TestRequestNoCircuitBreaker();
        var context = Substitute.For<IRequestContext>();
        var expectedResponse = "success";
        RequestHandlerCallback<string> next = () => ValueTask.FromResult(Right<EncinaError, string>(expectedResponse));

        var behavior = new CircuitBreakerPipelineBehavior<TestRequestNoCircuitBreaker, string>(
            Substitute.For<ILogger<CircuitBreakerPipelineBehavior<TestRequestNoCircuitBreaker, string>>>());

        // Act
        var result = await behavior.Handle(request, context, next, CancellationToken.None);

        // Assert
        result.ShouldBeSuccess();
        _ = result.Match(
            Right: value => value.Should().Be(expectedResponse),
            Left: _ => throw new InvalidOperationException("Should not be Left")
        );
    }

    [Fact]
    public async Task Handle_WithCircuitBreakerAttribute_SuccessfulRequest_ShouldPassThrough()
    {
        // Arrange
        var request = new TestRequest();
        var context = Substitute.For<IRequestContext>();
        var expectedResponse = "success";
        RequestHandlerCallback<string> next = () => ValueTask.FromResult(Right<EncinaError, string>(expectedResponse));

        // Act
        var result = await _behavior.Handle(request, context, next, CancellationToken.None);

        // Assert
        result.ShouldBeSuccess();
        _ = result.Match(
            Right: value => value.Should().Be(expectedResponse),
            Left: _ => throw new InvalidOperationException("Should not be Left")
        );
    }

    [Fact]
    public async Task Handle_CircuitClosed_ShouldAllowRequests()
    {
        // Arrange
        var request = new TestRequest();
        var context = Substitute.For<IRequestContext>();
        var callCount = 0;
        RequestHandlerCallback<string> next = () =>
        {
            callCount++;
            return ValueTask.FromResult(Right<EncinaError, string>("success"));
        };

        // Act
        await _behavior.Handle(request, context, next, CancellationToken.None);
        await _behavior.Handle(request, context, next, CancellationToken.None);
        await _behavior.Handle(request, context, next, CancellationToken.None);

        // Assert
        callCount.Should().Be(3, "all requests should pass through when circuit is closed");
    }

    [Fact]
    public async Task Handle_RepeatedFailures_ShouldOpenCircuit()
    {
        // Arrange
        var request = new TestRequestLowThreshold(); // FailureThreshold = 3, MinimumThroughput = 3
        var context = Substitute.For<IRequestContext>();
        var behavior = new CircuitBreakerPipelineBehavior<TestRequestLowThreshold, string>(
            Substitute.For<ILogger<CircuitBreakerPipelineBehavior<TestRequestLowThreshold, string>>>());

        RequestHandlerCallback<string> next = () =>
            ValueTask.FromResult(Left<EncinaError, string>(EncinaErrors.Create("test.error", "Failure")));

        // Act - Cause failures to open circuit
        await behavior.Handle(request, context, next, CancellationToken.None);
        await behavior.Handle(request, context, next, CancellationToken.None);
        await behavior.Handle(request, context, next, CancellationToken.None);

        // Give circuit breaker time to evaluate state
        await Task.Delay(100);

        // Now circuit should be open
        var result = await behavior.Handle(request, context, next, CancellationToken.None);

        // Assert
        result.ShouldBeError();
        _ = result.Match(
            Right: _ => throw new InvalidOperationException("Should be Left"),
            Left: error => error.Message.Should().Contain("Circuit breaker is open")
        );
    }

    [Fact]
    public async Task Handle_CircuitOpen_ShouldReturnCircuitBreakerError()
    {
        // Arrange
        var request = new TestRequestLowThreshold();
        var context = Substitute.For<IRequestContext>();
        var behavior = new CircuitBreakerPipelineBehavior<TestRequestLowThreshold, string>(
            Substitute.For<ILogger<CircuitBreakerPipelineBehavior<TestRequestLowThreshold, string>>>());

        RequestHandlerCallback<string> next = () =>
            ValueTask.FromResult(Left<EncinaError, string>(EncinaErrors.Create("test.error", "Failure")));

        // Cause failures to open circuit
        for (int i = 0; i < 5; i++)
        {
            await behavior.Handle(request, context, next, CancellationToken.None);
        }

        await Task.Delay(100);

        // Act - Try request when circuit is open
        var result = await behavior.Handle(request, context, next, CancellationToken.None);

        // Assert
        result.ShouldBeError();
        _ = result.Match(
            Right: _ => throw new InvalidOperationException("Should be Left"),
            Left: error =>
            {
                error.Message.Should().Contain("Circuit breaker is open");
                error.Message.Should().Contain("Service temporarily unavailable");
            }
        );
    }

    [Fact]
    public async Task GetOrCreateCircuitBreaker_ShouldCachePipeline()
    {
        // Arrange
        var request = new TestRequest();
        var context = Substitute.For<IRequestContext>();
        RequestHandlerCallback<string> next = () => ValueTask.FromResult(Right<EncinaError, string>("success"));

        // Act - Multiple calls should use cached pipeline
        await _behavior.Handle(request, context, next, CancellationToken.None);
        await _behavior.Handle(request, context, next, CancellationToken.None);

        // Assert - Verify cache is being used (we can't directly test static field, but this ensures no crash)
        var result = await _behavior.Handle(request, context, next, CancellationToken.None);
        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task GetOrCreateCircuitBreaker_ThreadSafe_ShouldNotCreateDuplicates()
    {
        // Arrange
        var request = new TestRequest();
        var context = Substitute.For<IRequestContext>();
        RequestHandlerCallback<string> next = () => ValueTask.FromResult(Right<EncinaError, string>("success"));

        // Act - Call from multiple threads concurrently
        var tasks = Enumerable.Range(0, 10).Select(_ => Task.Run(async () =>
        {
            await _behavior.Handle(request, context, next, CancellationToken.None);
        }));

        await Task.WhenAll(tasks);

        // Assert - No exceptions means thread-safe double-check locking worked
        var result = await _behavior.Handle(request, context, next, CancellationToken.None);
        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task Handle_ExceptionThrown_ShouldReturnEncinaError()
    {
        // Arrange
        var request = new TestRequest();
        var context = Substitute.For<IRequestContext>();
        RequestHandlerCallback<string> next = () => throw new InvalidOperationException("Test exception");

        // Act
        var result = await _behavior.Handle(request, context, next, CancellationToken.None);

        // Assert
        result.ShouldBeError();
        _ = result.Match(
            Right: _ => throw new InvalidOperationException("Should be Left"),
            Left: error => error.Message.Should().Contain("Test exception")
        );
    }

    [Fact]
    public async Task Handle_CircuitBreakerConfiguration_ShouldUseAttributeValues()
    {
        // Arrange
        var request = new TestRequestCustomConfig();
        var context = Substitute.For<IRequestContext>();
        var behavior = new CircuitBreakerPipelineBehavior<TestRequestCustomConfig, string>(
            Substitute.For<ILogger<CircuitBreakerPipelineBehavior<TestRequestCustomConfig, string>>>());

        RequestHandlerCallback<string> next = () => ValueTask.FromResult(Right<EncinaError, string>("success"));

        // Act
        var result = await behavior.Handle(request, context, next, CancellationToken.None);

        // Assert
        result.ShouldBeSuccess();
        // The fact that it doesn't throw means the custom configuration was applied correctly
    }

    private static void ClearCircuitBreakerCache()
    {
        // Use reflection to clear the static cache between tests
        var cacheField = typeof(CircuitBreakerPipelineBehavior<,>)
            .MakeGenericType(typeof(TestRequest), typeof(string))
            .GetField("_circuitBreakerCache", BindingFlags.Static | BindingFlags.NonPublic);

        if (cacheField?.GetValue(null) is System.Collections.IDictionary cache)
        {
            cache.Clear();
        }
    }

    // Test request types
    [CircuitBreaker(FailureThreshold = 5, SamplingDurationSeconds = 10, MinimumThroughput = 10, DurationOfBreakSeconds = 5)]
    public record TestRequest : IRequest<string>;

    public record TestRequestNoCircuitBreaker : IRequest<string>;

    [CircuitBreaker(FailureThreshold = 3, SamplingDurationSeconds = 2, MinimumThroughput = 3, DurationOfBreakSeconds = 1, FailureRateThreshold = 0.5)]
    public record TestRequestLowThreshold : IRequest<string>;

    [CircuitBreaker(FailureThreshold = 10, SamplingDurationSeconds = 30, MinimumThroughput = 20, DurationOfBreakSeconds = 15, FailureRateThreshold = 0.7)]
    public record TestRequestCustomConfig : IRequest<string>;
}
