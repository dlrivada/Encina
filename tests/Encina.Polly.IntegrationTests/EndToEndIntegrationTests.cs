using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace Encina.Polly.IntegrationTests;

/// <summary>
/// End-to-end integration tests for Polly resilience patterns with Encina.
/// </summary>
[Trait("Category", "Integration")]
public class EndToEndIntegrationTests
{
    [Fact]
    public async Task RetryPolicy_EndToEnd_SuccessOnSecondAttempt()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncina(config => { });
        services.AddEncinaPolly();
        services.AddLogging();
        services.AddTransient<IRequestHandler<TestRetryRequest, string>, TestRetryRequestHandler>();

        var serviceProvider = services.BuildServiceProvider();
        var Encina = serviceProvider.GetRequiredService<IEncina>();

        var request = new TestRetryRequest(1); // Fail once, then succeed

        // Act
        var result = await Encina.Send(request);

        // Assert
        result.ShouldBeSuccess().Should().Be("Success after retry");
    }

    [Fact]
    public async Task RetryPolicy_EndToEnd_ExhaustsRetries()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncina(config => { });
        services.AddEncinaPolly();
        services.AddLogging();
        services.AddTransient<IRequestHandler<TestRetryRequest, string>, TestRetryRequestHandler>();

        var serviceProvider = services.BuildServiceProvider();
        var Encina = serviceProvider.GetRequiredService<IEncina>();

        var request = new TestRetryRequest(10); // Fail more than max attempts

        // Act
        var result = await Encina.Send(request);

        // Assert
        result.ShouldBeError("should fail after exhausting retries");
    }

    [Fact]
    public async Task CircuitBreaker_EndToEnd_OpensAfterFailures()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncina(config => { });
        services.AddEncinaPolly();
        services.AddLogging();
        services.AddTransient<IRequestHandler<TestCircuitBreakerRequest, string>, TestCircuitBreakerRequestHandler>();

        var serviceProvider = services.BuildServiceProvider();
        var Encina = serviceProvider.GetRequiredService<IEncina>();

        var request = new TestCircuitBreakerRequest(true);

        // Act - Cause multiple failures to open circuit
        for (int i = 0; i < 5; i++)
        {
            await Encina.Send(request);
        }

        await Task.Delay(100); // Give circuit breaker time to evaluate

        var result = await Encina.Send(request);

        // Assert
        result.ShouldBeError(error =>
            error.Message.Should().Contain("Circuit breaker is open", "circuit should be open after repeated failures"));
    }

    [Fact]
    public async Task CombinedPolicies_EndToEnd_RetryAndCircuitBreaker()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncina(config => { });
        services.AddEncinaPolly();
        services.AddLogging();
        services.AddTransient<IRequestHandler<TestCombinedRequest, string>, TestCombinedRequestHandler>();

        var serviceProvider = services.BuildServiceProvider();
        var Encina = serviceProvider.GetRequiredService<IEncina>();

        var request = new TestCombinedRequest(1);

        // Act
        var result = await Encina.Send(request);

        // Assert
        result.ShouldBeSuccess("retry should succeed before circuit breaks");
    }

    // Test request types and handlers
    [Retry(MaxAttempts = 3, BaseDelayMs = 10)]
    private sealed record TestRetryRequest(int failCount) : IRequest<string>;

    private sealed class TestRetryRequestHandler : IRequestHandler<TestRetryRequest, string>
    {
        private static int _attemptCount;

        public Task<Either<EncinaError, string>> Handle(TestRetryRequest request, CancellationToken cancellationToken)
        {
            _attemptCount++;

            if (_attemptCount <= request.failCount)
            {
                // Don't reset - let retry behavior retry
                return Task.FromResult(Left<EncinaError, string>(EncinaErrors.Create("test.error", "Simulated failure")));
            }

            _attemptCount = 0; // Reset only on success for next test
            return Task.FromResult(Right<EncinaError, string>("Success after retry"));
        }
    }

    [CircuitBreaker(FailureThreshold = 3, MinimumThroughput = 3, DurationOfBreakSeconds = 1, SamplingDurationSeconds = 2)]
    private sealed record TestCircuitBreakerRequest(bool shouldFail) : IRequest<string>;

    private sealed class TestCircuitBreakerRequestHandler : IRequestHandler<TestCircuitBreakerRequest, string>
    {
        public Task<Either<EncinaError, string>> Handle(TestCircuitBreakerRequest request, CancellationToken cancellationToken)
        {
            if (request.shouldFail)
            {
                return Task.FromResult(Left<EncinaError, string>(EncinaErrors.Create("test.error", "Simulated failure")));
            }

            return Task.FromResult(Right<EncinaError, string>("Success"));
        }
    }

    [Retry(MaxAttempts = 3, BaseDelayMs = 10)]
    [CircuitBreaker(FailureThreshold = 5, MinimumThroughput = 5, DurationOfBreakSeconds = 1)]
    private sealed record TestCombinedRequest(int failCount) : IRequest<string>;

    private sealed class TestCombinedRequestHandler : IRequestHandler<TestCombinedRequest, string>
    {
        private static int _attemptCount;

        public Task<Either<EncinaError, string>> Handle(TestCombinedRequest request, CancellationToken cancellationToken)
        {
            _attemptCount++;

            if (_attemptCount <= request.failCount)
            {
                return Task.FromResult(Left<EncinaError, string>(EncinaErrors.Create("test.error", "Simulated failure")));
            }

            _attemptCount = 0;
            return Task.FromResult(Right<EncinaError, string>("Success"));
        }
    }

    #region Bulkhead Tests

    [Fact]
    public async Task Bulkhead_EndToEnd_LimitsConcurrentExecutions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncina(config => { });
        services.AddEncinaPolly();
        services.AddLogging();
        services.AddTransient<IRequestHandler<TestBulkheadRequest, string>, TestBulkheadRequestHandler>();

        var serviceProvider = services.BuildServiceProvider();
        var encina = serviceProvider.GetRequiredService<IEncina>();

        var concurrentCount = 0;
        var maxConcurrent = 0;
        var lockObj = new object();

        TestBulkheadRequestHandler.OnExecute = async () =>
        {
            var current = Interlocked.Increment(ref concurrentCount);
            lock (lockObj)
            {
                if (current > maxConcurrent) maxConcurrent = current;
            }
            await Task.Delay(50); // Simulate work
            Interlocked.Decrement(ref concurrentCount);
        };

        // Act - Start many concurrent requests
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => encina.Send(new TestBulkheadRequest()).AsTask())
            .ToList();

        await Task.WhenAll(tasks);

        // Assert - Max concurrent should not exceed bulkhead limit (2)
        maxConcurrent.Should().BeLessThanOrEqualTo(2, "bulkhead should limit concurrent executions");
    }

    [Fact]
    public async Task Bulkhead_EndToEnd_RejectsWhenFull()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncina(config => { });
        services.AddEncinaPolly();
        services.AddLogging();
        services.AddTransient<IRequestHandler<TestBulkheadNoQueueRequest, string>, TestBulkheadNoQueueRequestHandler>();

        var serviceProvider = services.BuildServiceProvider();
        var encina = serviceProvider.GetRequiredService<IEncina>();

        var tasksStarted = new TaskCompletionSource();
        var releaseSignal = new TaskCompletionSource();

        TestBulkheadNoQueueRequestHandler.OnExecute = async () =>
        {
            tasksStarted.TrySetResult();
            await releaseSignal.Task; // Wait until signaled to complete
        };

        // Start 2 requests that will hold the bulkhead (limit is 2, queue is 0)
        var holdingTask1 = encina.Send(new TestBulkheadNoQueueRequest());
        var holdingTask2 = encina.Send(new TestBulkheadNoQueueRequest());

        // Wait a bit for requests to acquire permits
        await Task.Delay(50);

        // Act - Try to send one more request (should be rejected immediately)
        var result = await encina.Send(new TestBulkheadNoQueueRequest());

        // Release the holding tasks
        releaseSignal.SetResult();
        await Task.WhenAll(holdingTask1.AsTask(), holdingTask2.AsTask());

        // Assert
        result.ShouldBeError(error =>
            error.Message.Should().Contain("Bulkhead full", "request should be rejected when bulkhead is full"));
    }

    [Fact]
    public async Task Bulkhead_CombinedWithRetry_ShouldWork()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncina(config => { });
        services.AddEncinaPolly();
        services.AddLogging();
        services.AddTransient<IRequestHandler<TestBulkheadWithRetryRequest, string>, TestBulkheadWithRetryRequestHandler>();

        var serviceProvider = services.BuildServiceProvider();
        var encina = serviceProvider.GetRequiredService<IEncina>();

        TestBulkheadWithRetryRequestHandler.AttemptCount = 0;

        // Act
        var result = await encina.Send(new TestBulkheadWithRetryRequest(FailCount: 1));

        // Assert
        result.ShouldBeSuccess("should succeed after retry within bulkhead");
    }

    // Bulkhead test request types and handlers
    [Bulkhead(MaxConcurrency = 2, MaxQueuedActions = 10, QueueTimeoutMs = 5000)]
    private sealed record TestBulkheadRequest : IRequest<string>;

    private sealed class TestBulkheadRequestHandler : IRequestHandler<TestBulkheadRequest, string>
    {
        public static Func<Task>? OnExecute { get; set; }

        public async Task<Either<EncinaError, string>> Handle(TestBulkheadRequest request, CancellationToken cancellationToken)
        {
            if (OnExecute != null)
            {
                await OnExecute();
            }
            return Right<EncinaError, string>("Success");
        }
    }

    [Bulkhead(MaxConcurrency = 2, MaxQueuedActions = 0, QueueTimeoutMs = 100)]
    private sealed record TestBulkheadNoQueueRequest : IRequest<string>;

    private sealed class TestBulkheadNoQueueRequestHandler : IRequestHandler<TestBulkheadNoQueueRequest, string>
    {
        public static Func<Task>? OnExecute { get; set; }

        public async Task<Either<EncinaError, string>> Handle(TestBulkheadNoQueueRequest request, CancellationToken cancellationToken)
        {
            if (OnExecute != null)
            {
                await OnExecute();
            }
            return Right<EncinaError, string>("Success");
        }
    }

    [Bulkhead(MaxConcurrency = 5, MaxQueuedActions = 10)]
    [Retry(MaxAttempts = 3, BaseDelayMs = 10)]
    private sealed record TestBulkheadWithRetryRequest(int FailCount) : IRequest<string>;

    private sealed class TestBulkheadWithRetryRequestHandler : IRequestHandler<TestBulkheadWithRetryRequest, string>
    {
        public static int AttemptCount;

        public Task<Either<EncinaError, string>> Handle(TestBulkheadWithRetryRequest request, CancellationToken cancellationToken)
        {
            var attempt = Interlocked.Increment(ref AttemptCount);

            if (attempt <= request.FailCount)
            {
                return Task.FromResult(Left<EncinaError, string>(EncinaErrors.Create("test.error", "Simulated failure")));
            }

            return Task.FromResult(Right<EncinaError, string>("Success"));
        }
    }

    #endregion
}
