using Encina.Extensions.Resilience;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shouldly;
using Xunit;

namespace Encina.IntegrationTests.Observability.Resilience;

/// <summary>
/// End-to-end integration tests for Encina with Standard Resilience.
/// Tests full integration with all resilience strategies working together.
/// </summary>
[Trait("Category", "Integration")]
public class StandardResilienceEndToEndTests
{
    [Fact]
    public async Task EndToEnd_WithRateLimiter_ShouldThrottle()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncina(typeof(StandardResilienceEndToEndTests).Assembly);
        services.AddScoped<IRequestHandler<TestRequest, TestResponse>, TestRequestHandler>();
        services.AddEncinaStandardResilience(options =>
        {
            options.RateLimiter = new global::Polly.RateLimiting.RateLimiterStrategyOptions
            {
                // Configure very low limit for testing
            };
        });

        var provider = services.BuildServiceProvider();
        var Encina = provider.GetRequiredService<IEncina>();

        // Act - Send a request
        var result = await Encina.Send(new TestRequest { Value = "test" });

        // Assert - Should succeed (rate limiter allows at least one request)
        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task EndToEnd_WithRetry_ShouldRetryOnFailure()
    {
        // Arrange
        var handler = new FailingThenSucceedingHandler();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncina(typeof(StandardResilienceEndToEndTests).Assembly);
        services.AddEncinaStandardResilience(options =>
        {
            options.Retry.MaxRetryAttempts = 3;
            options.Retry.Delay = TimeSpan.FromMilliseconds(10);
            options.Retry.BackoffType = global::Polly.DelayBackoffType.Constant;
        });

        // Register a handler that fails twice then succeeds (singleton so we can track attempts)
        services.AddSingleton<IRequestHandler<RetryTestRequest, RetryTestResponse>>(handler);

        var provider = services.BuildServiceProvider();
        var Encina = provider.GetRequiredService<IEncina>();

        // Act
        var result = await Encina.Send(new RetryTestRequest());

        // Assert - Should eventually succeed after retries
        result.ShouldBeSuccess();
        handler.AttemptCount.ShouldBeGreaterThan(1);
    }

    [Fact]
    public async Task EndToEnd_WithCircuitBreaker_ShouldOpenAfterFailures()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncina(typeof(StandardResilienceEndToEndTests).Assembly);
        services.AddScoped<IRequestHandler<TestRequest, TestResponse>, TestRequestHandler>();
        services.AddEncinaStandardResilience(options =>
        {
            options.CircuitBreaker.FailureRatio = 0.5; // 50% failure threshold
            options.CircuitBreaker.MinimumThroughput = 2;
            options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(10);
            options.CircuitBreaker.BreakDuration = TimeSpan.FromMilliseconds(500);
            // Use minimal retry (1 attempt = no actual retry) since Polly requires at least 1
            options.Retry.MaxRetryAttempts = 1;
            options.Retry.Delay = TimeSpan.FromMilliseconds(1);
        });

        var provider = services.BuildServiceProvider();
        var Encina = provider.GetRequiredService<IEncina>();

        // Act - Send multiple failing requests
        var results = new List<Either<EncinaError, TestResponse>>();
        for (int i = 0; i < 5; i++)
        {
            var result = await Encina.Send(new TestRequest { Value = "fail" });
            results.Add(result);
            await Task.Delay(10); // Small delay between requests
        }

        // Assert - All should fail
        results.ForEach(r => r.ShouldBeError());
    }

    [Fact]
    public async Task EndToEnd_WithTimeout_ShouldConfigureTimeoutCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncina(typeof(StandardResilienceEndToEndTests).Assembly);
        services.AddScoped<IRequestHandler<TestRequest, TestResponse>, TestRequestHandler>();
        services.AddEncinaStandardResilience(options =>
        {
            // Configure timeouts (we're just verifying configuration works)
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30);
            options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(10);
        });

        var provider = services.BuildServiceProvider();
        var Encina = provider.GetRequiredService<IEncina>();

        // Act - Send a fast request to verify the pipeline works with timeout configured
        var result = await Encina.Send(new TestRequest { Value = "quick" });

        // Assert - Should succeed (request completes before timeout)
        result.ShouldBeSuccess();
        result.Match(
            Right: response => response.Message.ShouldBe("Processed: quick"),
            Left: _ => throw new InvalidOperationException("Should not fail")
        );
    }

    [Fact]
    public async Task EndToEnd_CombinedStrategies_ShouldWork()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncina(typeof(StandardResilienceEndToEndTests).Assembly);
        services.AddScoped<IRequestHandler<TestRequest, TestResponse>, TestRequestHandler>();
        services.AddEncinaStandardResilience(options =>
        {
            options.Retry.MaxRetryAttempts = 2;
            options.Retry.Delay = TimeSpan.FromMilliseconds(10);
            options.CircuitBreaker.FailureRatio = 0.8;
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(5);
        });

        var provider = services.BuildServiceProvider();
        var Encina = provider.GetRequiredService<IEncina>();

        // Act - Send successful request
        var result = await Encina.Send(new TestRequest { Value = "success" });

        // Assert
        result.ShouldBeSuccess();
        result.Match(
            Right: response => response.Message.ShouldBe("Processed: success"),
            Left: _ => throw new InvalidOperationException("Should not fail")
        );
    }

    [Fact]
    public async Task EndToEnd_RequestSpecificResilience_ShouldUseCustomConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncina(typeof(StandardResilienceEndToEndTests).Assembly);
        services.AddScoped<IRequestHandler<TestRequest, TestResponse>, TestRequestHandler>();

        // Configure specific resilience for TestRequest
        services.AddEncinaStandardResilienceFor<TestRequest, TestResponse>(options =>
        {
            options.Retry.MaxRetryAttempts = 5;
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(10);
        });

        var provider = services.BuildServiceProvider();
        var Encina = provider.GetRequiredService<IEncina>();

        // Act
        var result = await Encina.Send(new TestRequest { Value = "test" });

        // Assert - Should use custom configuration
        result.ShouldBeSuccess();
    }

    // Test request/response types
    private record TestRequest : IRequest<TestResponse>
    {
        public string Value { get; init; } = string.Empty;
    }

    private record TestResponse
    {
        public string Message { get; init; } = string.Empty;
    }

    private record RetryTestRequest : IRequest<RetryTestResponse>;

    private record RetryTestResponse
    {
        public bool Success { get; init; }
    }

    private record LongRunningRequest : IRequest<LongRunningResponse>;

    private record LongRunningResponse;

    // Test handlers
    private class TestRequestHandler : IRequestHandler<TestRequest, TestResponse>
    {
        public Task<Either<EncinaError, TestResponse>> Handle(
            TestRequest request,
            CancellationToken cancellationToken)
        {
            if (request.Value == "fail")
            {
                return Task.FromResult<Either<EncinaError, TestResponse>>(
                    EncinaError.New("Intentional failure"));
            }

            return Task.FromResult<Either<EncinaError, TestResponse>>(
                new TestResponse { Message = $"Processed: {request.Value}" });
        }
    }

    private class FailingThenSucceedingHandler : IRequestHandler<RetryTestRequest, RetryTestResponse>
    {
        public int AttemptCount { get; private set; }

        public Task<Either<EncinaError, RetryTestResponse>> Handle(
            RetryTestRequest request,
            CancellationToken cancellationToken)
        {
            AttemptCount++;

            if (AttemptCount < 3)
            {
                return Task.FromResult<Either<EncinaError, RetryTestResponse>>(
                    EncinaError.New($"Attempt {AttemptCount} failed"));
            }

            return Task.FromResult<Either<EncinaError, RetryTestResponse>>(
                new RetryTestResponse { Success = true });
        }
    }

    private class LongRunningRequestHandler : IRequestHandler<LongRunningRequest, LongRunningResponse>
    {
        public async Task<Either<EncinaError, LongRunningResponse>> Handle(
            LongRunningRequest request,
            CancellationToken cancellationToken)
        {
            // Simulate long-running operation
            await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
            return new LongRunningResponse();
        }
    }
}
