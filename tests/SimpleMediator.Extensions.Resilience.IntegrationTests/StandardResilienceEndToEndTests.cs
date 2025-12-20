using FluentAssertions;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using SimpleMediator.Extensions.Resilience;
using Xunit;

namespace SimpleMediator.Extensions.Resilience.IntegrationTests;

/// <summary>
/// End-to-end integration tests for SimpleMediator with Standard Resilience.
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
        services.AddSimpleMediator(typeof(StandardResilienceEndToEndTests).Assembly);
        services.AddSimpleMediatorStandardResilience(options =>
        {
            options.RateLimiter = new Polly.RateLimiting.RateLimiterStrategyOptions
            {
                // Configure very low limit for testing
            };
        });

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<ISimpleMediator>();

        // Act - Send a request
        var result = await mediator.Send(new TestRequest { Value = "test" });

        // Assert - Should succeed (rate limiter allows at least one request)
        result.IsRight.Should().BeTrue();
    }

    [Fact]
    public async Task EndToEnd_WithRetry_ShouldRetryOnFailure()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSimpleMediator(typeof(StandardResilienceEndToEndTests).Assembly);
        services.AddSimpleMediatorStandardResilience(options =>
        {
            options.Retry.MaxRetryAttempts = 3;
            options.Retry.Delay = TimeSpan.FromMilliseconds(10);
            options.Retry.BackoffType = DelayBackoffType.Constant;
        });

        // Register a handler that fails twice then succeeds
        services.AddScoped<FailingThenSucceedingHandler>();
        services.AddScoped<IRequestHandler<RetryTestRequest, RetryTestResponse>>(sp =>
            sp.GetRequiredService<FailingThenSucceedingHandler>());

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<ISimpleMediator>();

        // Act
        var result = await mediator.Send(new RetryTestRequest());

        // Assert - Should eventually succeed after retries
        result.IsRight.Should().BeTrue();
        var handler = provider.GetRequiredService<FailingThenSucceedingHandler>();
        handler.AttemptCount.Should().BeGreaterThan(1);
    }

    [Fact]
    public async Task EndToEnd_WithCircuitBreaker_ShouldOpenAfterFailures()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSimpleMediator(typeof(StandardResilienceEndToEndTests).Assembly);
        services.AddSimpleMediatorStandardResilience(options =>
        {
            options.CircuitBreaker.FailureRatio = 0.5; // 50% failure threshold
            options.CircuitBreaker.MinimumThroughput = 2;
            options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(10);
            options.CircuitBreaker.BreakDuration = TimeSpan.FromMilliseconds(100);
            options.Retry.MaxRetryAttempts = 0; // Disable retry to test circuit breaker
        });

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<ISimpleMediator>();

        // Act - Send multiple failing requests
        var results = new List<Either<MediatorError, TestResponse>>();
        for (int i = 0; i < 5; i++)
        {
            var result = await mediator.Send(new TestRequest { Value = "fail" });
            results.Add(result);
            await Task.Delay(10); // Small delay between requests
        }

        // Assert - All should fail
        results.Should().OnlyContain(r => r.IsLeft);
    }

    [Fact]
    public async Task EndToEnd_WithTimeout_ShouldTimeoutLongRunningRequest()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSimpleMediator(typeof(StandardResilienceEndToEndTests).Assembly);
        services.AddSimpleMediatorStandardResilience(options =>
        {
            options.TotalRequestTimeout.Timeout = TimeSpan.FromMilliseconds(50);
            options.AttemptTimeout.Timeout = TimeSpan.FromMilliseconds(30);
        });

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<ISimpleMediator>();

        // Act
        var result = await mediator.Send(new LongRunningRequest());

        // Assert - Should timeout
        result.IsLeft.Should().BeTrue();
        result.Match(
            Right: _ => throw new InvalidOperationException("Should not succeed"),
            Left: error => error.Message.Should().Contain("timed out")
        );
    }

    [Fact]
    public async Task EndToEnd_CombinedStrategies_ShouldWork()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSimpleMediator(typeof(StandardResilienceEndToEndTests).Assembly);
        services.AddSimpleMediatorStandardResilience(options =>
        {
            options.Retry.MaxRetryAttempts = 2;
            options.Retry.Delay = TimeSpan.FromMilliseconds(10);
            options.CircuitBreaker.FailureRatio = 0.8;
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(5);
        });

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<ISimpleMediator>();

        // Act - Send successful request
        var result = await mediator.Send(new TestRequest { Value = "success" });

        // Assert
        result.IsRight.Should().BeTrue();
        result.Match(
            Right: response => response.Message.Should().Be("Processed: success"),
            Left: _ => throw new InvalidOperationException("Should not fail")
        );
    }

    [Fact]
    public async Task EndToEnd_RequestSpecificResilience_ShouldUseCustomConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSimpleMediator(typeof(StandardResilienceEndToEndTests).Assembly);

        // Configure specific resilience for TestRequest
        services.AddSimpleMediatorStandardResilienceFor<TestRequest, TestResponse>(options =>
        {
            options.Retry.MaxRetryAttempts = 5;
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(10);
        });

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<ISimpleMediator>();

        // Act
        var result = await mediator.Send(new TestRequest { Value = "test" });

        // Assert - Should use custom configuration
        result.IsRight.Should().BeTrue();
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
        public ValueTask<Either<MediatorError, TestResponse>> Handle(
            TestRequest request,
            IRequestContext context,
            CancellationToken cancellationToken)
        {
            if (request.Value == "fail")
            {
                return ValueTask.FromResult<Either<MediatorError, TestResponse>>(
                    MediatorError.New("Intentional failure"));
            }

            return ValueTask.FromResult<Either<MediatorError, TestResponse>>(
                new TestResponse { Message = $"Processed: {request.Value}" });
        }
    }

    private class FailingThenSucceedingHandler : IRequestHandler<RetryTestRequest, RetryTestResponse>
    {
        public int AttemptCount { get; private set; }

        public ValueTask<Either<MediatorError, RetryTestResponse>> Handle(
            RetryTestRequest request,
            IRequestContext context,
            CancellationToken cancellationToken)
        {
            AttemptCount++;

            if (AttemptCount < 3)
            {
                return ValueTask.FromResult<Either<MediatorError, RetryTestResponse>>(
                    MediatorError.New($"Attempt {AttemptCount} failed"));
            }

            return ValueTask.FromResult<Either<MediatorError, RetryTestResponse>>(
                new RetryTestResponse { Success = true });
        }
    }

    private class LongRunningRequestHandler : IRequestHandler<LongRunningRequest, LongRunningResponse>
    {
        public async ValueTask<Either<MediatorError, LongRunningResponse>> Handle(
            LongRunningRequest request,
            IRequestContext context,
            CancellationToken cancellationToken)
        {
            // Simulate long-running operation
            await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
            return new LongRunningResponse();
        }
    }
}
