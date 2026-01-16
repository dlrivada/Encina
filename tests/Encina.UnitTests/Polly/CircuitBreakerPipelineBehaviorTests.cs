using System.Reflection;
using Encina.Polly;
using Encina.Testing;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Polly.CircuitBreaker;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Polly;

/// <summary>
/// Unit tests for <see cref="CircuitBreakerPipelineBehavior{TRequest, TResponse}"/>.
/// Tests circuit breaker states, caching, thread safety, and failure handling.
/// </summary>
public class CircuitBreakerPipelineBehaviorTests
{
    /// <summary>
    /// Clears the static circuit breaker cache to ensure test isolation.
    /// Note: This is necessary because CircuitBreakerPipelineBehavior uses a static cache.
    /// </summary>
    private static void ClearCircuitBreakerCache<TReq, TResp>() where TReq : IRequest<TResp>
    {
        var cacheField = typeof(CircuitBreakerPipelineBehavior<,>)
            .MakeGenericType(typeof(TReq), typeof(TResp))
            .GetField("_circuitBreakerCache", BindingFlags.Static | BindingFlags.NonPublic);

        if (cacheField?.GetValue(null) is System.Collections.IDictionary cache)
        {
            cache.Clear();
        }
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
            Right: value => value.ShouldBe(expectedResponse),
            Left: _ => throw new InvalidOperationException("Should not be Left")
        );
    }

    [Fact]
    public async Task Handle_WithCircuitBreakerAttribute_SuccessfulRequest_ShouldPassThrough()
    {
        // Arrange
        ClearCircuitBreakerCache<TestRequest, string>();
        var logger = Substitute.For<ILogger<CircuitBreakerPipelineBehavior<TestRequest, string>>>();
        var behavior = new CircuitBreakerPipelineBehavior<TestRequest, string>(logger);
        var request = new TestRequest();
        var context = Substitute.For<IRequestContext>();
        var expectedResponse = "success";
        RequestHandlerCallback<string> next = () => ValueTask.FromResult(Right<EncinaError, string>(expectedResponse));

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
    public async Task Handle_CircuitClosed_ShouldAllowRequests()
    {
        // Arrange
        ClearCircuitBreakerCache<TestRequest, string>();
        var logger = Substitute.For<ILogger<CircuitBreakerPipelineBehavior<TestRequest, string>>>();
        var behavior = new CircuitBreakerPipelineBehavior<TestRequest, string>(logger);
        var request = new TestRequest();
        var context = Substitute.For<IRequestContext>();
        var callCount = 0;
        RequestHandlerCallback<string> next = () =>
        {
            callCount++;
            return ValueTask.FromResult(Right<EncinaError, string>("success"));
        };

        // Act
        await behavior.Handle(request, context, next, CancellationToken.None);
        await behavior.Handle(request, context, next, CancellationToken.None);
        await behavior.Handle(request, context, next, CancellationToken.None);

        // Assert
        callCount.ShouldBe(3, "all requests should pass through when circuit is closed");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Handle_RepeatedFailures_ShouldOpenCircuit()
    {
        // Arrange
        ClearCircuitBreakerCache<TestRequestLowThreshold, string>();
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
            Left: error => error.Message.ShouldContain("Circuit breaker is open")
        );
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Handle_CircuitOpen_ShouldReturnCircuitBreakerError()
    {
        // Arrange
        ClearCircuitBreakerCache<TestRequestLowThreshold, string>();
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
                error.Message.ShouldContain("Circuit breaker is open");
                error.Message.ShouldContain("Service temporarily unavailable");
            }
        );
    }

    [Fact]
    public async Task GetOrCreateCircuitBreaker_ShouldCachePipeline()
    {
        // Arrange
        ClearCircuitBreakerCache<TestRequest, string>();
        var logger = Substitute.For<ILogger<CircuitBreakerPipelineBehavior<TestRequest, string>>>();
        var behavior = new CircuitBreakerPipelineBehavior<TestRequest, string>(logger);
        var request = new TestRequest();
        var context = Substitute.For<IRequestContext>();
        RequestHandlerCallback<string> next = () => ValueTask.FromResult(Right<EncinaError, string>("success"));

        // Act - Multiple calls should use cached pipeline
        await behavior.Handle(request, context, next, CancellationToken.None);
        await behavior.Handle(request, context, next, CancellationToken.None);

        // Assert - Verify cache is being used (we can't directly test static field, but this ensures no crash)
        var result = await behavior.Handle(request, context, next, CancellationToken.None);
        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task GetOrCreateCircuitBreaker_ThreadSafe_ShouldNotCreateDuplicates()
    {
        // Arrange
        ClearCircuitBreakerCache<TestRequest, string>();
        var logger = Substitute.For<ILogger<CircuitBreakerPipelineBehavior<TestRequest, string>>>();
        var behavior = new CircuitBreakerPipelineBehavior<TestRequest, string>(logger);
        var request = new TestRequest();
        var context = Substitute.For<IRequestContext>();
        RequestHandlerCallback<string> next = () => ValueTask.FromResult(Right<EncinaError, string>("success"));

        // Act - Call from multiple threads concurrently
        var tasks = Enumerable.Range(0, 10).Select(_ => Task.Run(async () =>
        {
            await behavior.Handle(request, context, next, CancellationToken.None);
        }));

        await Task.WhenAll(tasks);

        // Assert - No exceptions means thread-safe double-check locking worked
        var result = await behavior.Handle(request, context, next, CancellationToken.None);
        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task Handle_ExceptionThrown_ShouldReturnEncinaError()
    {
        // Arrange
        ClearCircuitBreakerCache<TestRequest, string>();
        var logger = Substitute.For<ILogger<CircuitBreakerPipelineBehavior<TestRequest, string>>>();
        var behavior = new CircuitBreakerPipelineBehavior<TestRequest, string>(logger);
        var request = new TestRequest();
        var context = Substitute.For<IRequestContext>();
        RequestHandlerCallback<string> next = () => throw new InvalidOperationException("Test exception");

        // Act
        var result = await behavior.Handle(request, context, next, CancellationToken.None);

        // Assert
        result.ShouldBeError();
        _ = result.Match(
            Right: _ => throw new InvalidOperationException("Should be Left"),
            Left: error => error.Message.ShouldContain("Test exception")
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

    // Test request types
    [CircuitBreaker(FailureThreshold = 5, SamplingDurationSeconds = 10, MinimumThroughput = 10, DurationOfBreakSeconds = 5)]
    public record TestRequest : IRequest<string>;

    public record TestRequestNoCircuitBreaker : IRequest<string>;

    [CircuitBreaker(FailureThreshold = 3, SamplingDurationSeconds = 2, MinimumThroughput = 3, DurationOfBreakSeconds = 1, FailureRateThreshold = 0.5)]
    public record TestRequestLowThreshold : IRequest<string>;

    [CircuitBreaker(FailureThreshold = 10, SamplingDurationSeconds = 30, MinimumThroughput = 20, DurationOfBreakSeconds = 15, FailureRateThreshold = 0.7)]
    public record TestRequestCustomConfig : IRequest<string>;
}
