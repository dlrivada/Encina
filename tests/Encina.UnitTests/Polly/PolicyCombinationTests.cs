using Encina.Testing;
using Encina.Polly;
using System.Reflection;
using Encina.TestInfrastructure.PropertyTests;
using LanguageExt;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Polly;

/// <summary>
/// Tests for combinations of resilience policies to verify they work correctly together.
/// Tests scenarios where multiple policies (Retry, CircuitBreaker, Bulkhead, RateLimit) interact.
/// </summary>
public sealed class PolicyCombinationTests
{

    #region Retry + CircuitBreaker Tests

    [Fact]
    public async Task RetryWithCircuitBreaker_TransientFailure_ShouldRetryAndSucceed()
    {
        // Arrange
        var retryBehavior = new RetryPipelineBehavior<RetryCircuitBreakerRequest, string>(
            NullLogger<RetryPipelineBehavior<RetryCircuitBreakerRequest, string>>.Instance);
        var circuitBreakerBehavior = new CircuitBreakerPipelineBehavior<RetryCircuitBreakerRequest, string>(
            NullLogger<CircuitBreakerPipelineBehavior<RetryCircuitBreakerRequest, string>>.Instance);

        var request = new RetryCircuitBreakerRequest();
        var context = new TestRequestContext();
        var callCount = 0;

        RequestHandlerCallback<string> handler = () =>
        {
            callCount++;
            return callCount < 2
                ? ValueTask.FromResult(Left<EncinaError, string>(EncinaError.New("Transient")))
                : ValueTask.FromResult(Right<EncinaError, string>("Success"));
        };

        // Act - Retry wraps CircuitBreaker
        var result = await retryBehavior.Handle(request, context,
            async () => await circuitBreakerBehavior.Handle(request, context, handler, CancellationToken.None),
            CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue("Should succeed after retry");
        callCount.ShouldBeGreaterThanOrEqualTo(2, "Should have retried at least once");
    }

    #endregion

    #region Retry + RateLimit Tests

    [Fact]
    public async Task RetryWithRateLimit_ExceedsLimit_ShouldRespectRateLimit()
    {
        // Arrange
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var rateLimiter = new AdaptiveRateLimiter(timeProvider);

        var retryBehavior = new RetryPipelineBehavior<RetryRateLimitRequest, string>(
            NullLogger<RetryPipelineBehavior<RetryRateLimitRequest, string>>.Instance);
        var rateLimitBehavior = new RateLimitingPipelineBehavior<RetryRateLimitRequest, string>(
            NullLogger<RateLimitingPipelineBehavior<RetryRateLimitRequest, string>>.Instance,
            rateLimiter);

        var request = new RetryRateLimitRequest();
        var context = new TestRequestContext();

        RequestHandlerCallback<string> handler = () =>
            ValueTask.FromResult(Right<EncinaError, string>("Success"));

        // Act - Make requests up to the rate limit
        var results = new List<Either<EncinaError, string>>();
        for (var i = 0; i < 15; i++) // 10 is the limit
        {
            var result = await retryBehavior.Handle(request, context,
                async () => await rateLimitBehavior.Handle(request, context, handler, CancellationToken.None),
                CancellationToken.None);
            results.Add(result);
        }

        // Assert - Some should succeed, some should fail due to rate limit
        var successes = results.Count(r => r.IsRight);
        var failures = results.Count(r => r.IsLeft);

        successes.ShouldBeLessThanOrEqualTo(10, "Should not exceed rate limit");
        failures.ShouldBeGreaterThanOrEqualTo(5, "Excess requests should be rate limited");
    }

    #endregion

    #region Bulkhead Tests with Failures

    [Fact]
    public async Task Bulkhead_ConcurrentRequests_ShouldIsolate()
    {
        // Arrange
        var bulkheadManager = new BulkheadManager();
        var bulkheadBehavior = new BulkheadPipelineBehavior<BulkheadTestRequest, string>(
            NullLogger<BulkheadPipelineBehavior<BulkheadTestRequest, string>>.Instance,
            bulkheadManager);

        var request = new BulkheadTestRequest();
        var context = new TestRequestContext();

        // Read MaxConcurrency from attribute to keep test in sync with configuration
        var maxConcurrency = typeof(BulkheadTestRequest)
            .GetCustomAttribute<BulkheadAttribute>()!.MaxConcurrency;
        using var startedSemaphore = new SemaphoreSlim(0, maxConcurrency);
        var delayTcs = new TaskCompletionSource<bool>();

        RequestHandlerCallback<string> slowHandler = async () =>
        {
            startedSemaphore.Release();
            await delayTcs.Task;
            return Right<EncinaError, string>("Success");
        };

        RequestHandlerCallback<string> fastHandler = () =>
            ValueTask.FromResult(Right<EncinaError, string>("Fast"));

        // Act - Start slow requests up to concurrency limit
        var slowTasks = new List<Task<Either<EncinaError, string>>>();
        for (var i = 0; i < maxConcurrency; i++)
        {
            slowTasks.Add(bulkheadBehavior.Handle(request, context, slowHandler, CancellationToken.None).AsTask());
        }

        // Wait for all handlers to signal they have started
        for (var i = 0; i < maxConcurrency; i++)
        {
            await startedSemaphore.WaitAsync();
        }

        // Try one more request (should be queued or rejected)
        var queuedTask = bulkheadBehavior.Handle(request, context, fastHandler, CancellationToken.None).AsTask();

        // Complete the blocking tasks
        delayTcs.SetResult(true);

        // Wait for all to complete
        await Task.WhenAll(slowTasks);
        var queuedResult = await queuedTask;

        // Assert - All slow tasks should succeed
        foreach (var task in slowTasks)
        {
            task.Result.IsRight.ShouldBeTrue("Slow tasks should succeed");
        }

        // Queued task should have succeeded after slots freed
        queuedResult.IsRight.ShouldBeTrue("Queued task should eventually succeed");

        // Cleanup
        bulkheadManager.Dispose();
    }

    [Fact]
    public async Task Bulkhead_ExceedsQueueCapacity_ShouldReject()
    {
        // Arrange
        var bulkheadManager = new BulkheadManager();
        var bulkheadBehavior = new BulkheadPipelineBehavior<BulkheadNoQueueRequest, string>(
            NullLogger<BulkheadPipelineBehavior<BulkheadNoQueueRequest, string>>.Instance,
            bulkheadManager);

        var request = new BulkheadNoQueueRequest();
        var context = new TestRequestContext();

        const int maxConcurrency = 3;
        using var startedSemaphore = new SemaphoreSlim(0, maxConcurrency);
        var delayTcs = new TaskCompletionSource<bool>();

        RequestHandlerCallback<string> slowHandler = async () =>
        {
            startedSemaphore.Release();
            await delayTcs.Task;
            return Right<EncinaError, string>("Success");
        };

        // Act - Fill all slots (MaxConcurrency = 3, MaxQueuedActions = 0)
        var slowTasks = new List<Task<Either<EncinaError, string>>>();
        for (var i = 0; i < maxConcurrency; i++)
        {
            slowTasks.Add(bulkheadBehavior.Handle(request, context, slowHandler, CancellationToken.None).AsTask());
        }

        // Wait for all handlers to signal they have started
        for (var i = 0; i < maxConcurrency; i++)
        {
            await startedSemaphore.WaitAsync();
        }

        // Try one more request (should be rejected immediately - no queue)
        var rejectedResult = await bulkheadBehavior.Handle(request, context, slowHandler, CancellationToken.None);

        // Complete the blocking tasks
        delayTcs.SetResult(true);
        await Task.WhenAll(slowTasks);

        // Assert
        rejectedResult.IsLeft.ShouldBeTrue("Should be rejected when bulkhead is full and no queue");

        // Cleanup
        bulkheadManager.Dispose();
    }

    #endregion

    #region Failure Simulation Tests

    [Fact]
    public async Task Retry_WithPersistentFailure_ShouldExhaustRetries()
    {
        // Arrange
        var retryBehavior = new RetryPipelineBehavior<RetryTestRequest, string>(
            NullLogger<RetryPipelineBehavior<RetryTestRequest, string>>.Instance);

        var request = new RetryTestRequest();
        var context = new TestRequestContext();
        var callCount = 0;

        RequestHandlerCallback<string> persistentFailure = () =>
        {
            callCount++;
            return ValueTask.FromResult(Left<EncinaError, string>(EncinaError.New("Persistent failure")));
        };

        // Act
        var result = await retryBehavior.Handle(request, context, persistentFailure, CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue("Should fail after exhausting retries");
        callCount.ShouldBe(3, "Should attempt MaxAttempts times (3)");
    }

    [Fact]
    public async Task Retry_WithIntermittentFailure_ShouldEventuallySucceed()
    {
        // Arrange
        var retryBehavior = new RetryPipelineBehavior<RetryTestRequest, string>(
            NullLogger<RetryPipelineBehavior<RetryTestRequest, string>>.Instance);

        var request = new RetryTestRequest();
        var context = new TestRequestContext();
        var callCount = 0;

        // Fails first 2 times, succeeds on 3rd
        RequestHandlerCallback<string> intermittentFailure = () =>
        {
            callCount++;
            return callCount >= 3
                ? ValueTask.FromResult(Right<EncinaError, string>("Success"))
                : ValueTask.FromResult(Left<EncinaError, string>(EncinaError.New("Intermittent failure")));
        };

        // Act
        var result = await retryBehavior.Handle(request, context, intermittentFailure, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue("Should succeed after retries");
        callCount.ShouldBe(3, "Should succeed on 3rd attempt");
    }

    [Fact]
    public async Task Retry_WithException_ShouldCatchAndRetry()
    {
        // Arrange
        var retryBehavior = new RetryPipelineBehavior<RetryExceptionRequest, string>(
            NullLogger<RetryPipelineBehavior<RetryExceptionRequest, string>>.Instance);

        var request = new RetryExceptionRequest();
        var context = new TestRequestContext();
        var callCount = 0;

        // Throws on first attempt, succeeds on second
        RequestHandlerCallback<string> throwsThenSucceeds = () =>
        {
            callCount++;
            if (callCount == 1)
            {
                throw new TimeoutException("Simulated timeout");
            }

            return ValueTask.FromResult(Right<EncinaError, string>("Success"));
        };

        // Act
        var result = await retryBehavior.Handle(request, context, throwsThenSucceeds, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue("Should succeed after catching exception and retrying");
        callCount.ShouldBe(2);
    }

    #endregion

    #region Test Request Types

    [Retry(MaxAttempts = 3, BackoffType = BackoffType.Constant, BaseDelayMs = 10)]
    [CircuitBreaker(FailureThreshold = 100, MinimumThroughput = 100, DurationOfBreakSeconds = 1)]
    private sealed record RetryCircuitBreakerRequest : IRequest<string>;

    [Retry(MaxAttempts = 3, BackoffType = BackoffType.Constant, BaseDelayMs = 10)]
    [RateLimit(MaxRequestsPerWindow = 10, WindowSizeSeconds = 60, EnableAdaptiveThrottling = false)]
    private sealed record RetryRateLimitRequest : IRequest<string>;

    [Bulkhead(MaxConcurrency = 5, MaxQueuedActions = 10, QueueTimeoutMs = 5000)]
    private sealed record BulkheadTestRequest : IRequest<string>;

    [Bulkhead(MaxConcurrency = 3, MaxQueuedActions = 0, QueueTimeoutMs = 100)]
    private sealed record BulkheadNoQueueRequest : IRequest<string>;

    [Retry(MaxAttempts = 3, BackoffType = BackoffType.Constant, BaseDelayMs = 10)]
    private sealed record RetryTestRequest : IRequest<string>;

    [Retry(MaxAttempts = 3, BackoffType = BackoffType.Constant, BaseDelayMs = 10, RetryOnAllExceptions = true)]
    private sealed record RetryExceptionRequest : IRequest<string>;

    #endregion
}
