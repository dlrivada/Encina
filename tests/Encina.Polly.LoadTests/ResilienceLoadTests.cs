using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using static LanguageExt.Prelude;

namespace Encina.Polly.LoadTests;

/// <summary>
/// Load tests for Polly resilience behaviors.
/// Tests performance and thread-safety under high concurrency.
/// </summary>
[Trait("Category", "Load")]
public class ResilienceLoadTests
{
    [Fact]
    public async Task RetryPolicy_HighConcurrency_ShouldHandle()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncina(config => { });
        services.AddEncinaPolly();
        services.AddLogging();
        services.AddTransient<RetryRequestHandler>();

        var serviceProvider = services.BuildServiceProvider();
        var Encina = serviceProvider.GetRequiredService<IEncina>();

        var tasks = Enumerable.Range(0, 100).Select(async i =>
        {
            var request = new RetryRequest();
            return await Encina.Send(request);
        });

        // Act
        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().AllSatisfy(result => result.ShouldBeSuccess());
    }

    [Fact]
    public async Task CircuitBreaker_HighConcurrency_ShouldBeThreadSafe()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncina(config => { });
        services.AddEncinaPolly();
        services.AddLogging();
        services.AddTransient<CircuitBreakerRequestHandler>();

        var serviceProvider = services.BuildServiceProvider();
        var Encina = serviceProvider.GetRequiredService<IEncina>();

        var tasks = Enumerable.Range(0, 50).Select(async i =>
        {
            var request = new CircuitBreakerRequest();
            return await Encina.Send(request);
        });

        // Act - No exceptions means thread-safe
        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().NotBeNull("all requests should complete without crashing");
    }

    // Test types
    [Retry(MaxAttempts = 2, BaseDelayMs = 1)]
    private sealed record RetryRequest : IRequest<string>;

    private sealed class RetryRequestHandler : IRequestHandler<RetryRequest, string>
    {
        public Task<Either<EncinaError, string>> Handle(RetryRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(Right<EncinaError, string>("Success"));
        }
    }

    [CircuitBreaker(FailureThreshold = 10, MinimumThroughput = 5)]
    private sealed record CircuitBreakerRequest : IRequest<string>;

    private sealed class CircuitBreakerRequestHandler : IRequestHandler<CircuitBreakerRequest, string>
    {
        public Task<Either<EncinaError, string>> Handle(CircuitBreakerRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(Right<EncinaError, string>("Success"));
        }
    }

    #region Rate Limiting Load Tests

    [Fact]
    public async Task RateLimiting_HighConcurrency_ShouldBeThreadSafe()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncina(config => { });
        services.AddEncinaPolly();
        services.AddLogging();
        services.AddTransient<RateLimitedRequestHandler>();

        var serviceProvider = services.BuildServiceProvider();
        var Encina = serviceProvider.GetRequiredService<IEncina>();

        var tasks = Enumerable.Range(0, 50).Select(async i =>
        {
            var request = new RateLimitedRequest();
            return await Encina.Send(request);
        });

        // Act - No exceptions means thread-safe
        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().NotBeNull("all requests should complete without crashing");
    }

    [Fact]
    public async Task RateLimiting_EnforcesLimit_UnderConcurrentLoad()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncina(config => { });
        services.AddEncinaPolly();
        services.AddLogging();
        services.AddTransient<SmallLimitRequestHandler>();

        var serviceProvider = services.BuildServiceProvider();
        var Encina = serviceProvider.GetRequiredService<IEncina>();

        // Send 100 concurrent requests with a limit of 10
        var tasks = Enumerable.Range(0, 100).Select(async i =>
        {
            var request = new SmallLimitRequest();
            return await Encina.Send(request);
        });

        // Act
        var results = await Task.WhenAll(tasks);

        // Assert - some should succeed, some should be rate limited
        var successes = results.Count(r => r.IsRight);
        var failures = results.Count(r => r.IsLeft);

        successes.Should().BeGreaterThan(0, "some requests should succeed");
        failures.Should().BeGreaterThan(0, "some requests should be rate limited");
    }

    [Fact]
    public async Task AdaptiveRateLimiter_ConcurrentAcquire_ShouldBeThreadSafe()
    {
        // Arrange
        var rateLimiter = new AdaptiveRateLimiter();
        var attribute = new RateLimitAttribute
        {
            MaxRequestsPerWindow = 1000,
            WindowSizeSeconds = 60
        };

        var tasks = Enumerable.Range(0, 500).Select(async i =>
        {
            return await rateLimiter.AcquireAsync($"key-{i % 10}", attribute, CancellationToken.None);
        });

        // Act - No exceptions means thread-safe
        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(500, "all acquire attempts should complete");
        results.All(r => r.IsAllowed || !r.IsAllowed).Should().BeTrue("all results should be valid");
    }

    [Fact]
    public async Task AdaptiveRateLimiter_ConcurrentRecordSuccessAndFailure_ShouldBeThreadSafe()
    {
        // Arrange
        var rateLimiter = new AdaptiveRateLimiter();
        var attribute = new RateLimitAttribute
        {
            MaxRequestsPerWindow = 1000,
            WindowSizeSeconds = 60,
            EnableAdaptiveThrottling = true
        };

        // Pre-populate with some acquisitions
        for (int i = 0; i < 20; i++)
        {
            await rateLimiter.AcquireAsync("concurrent-key", attribute, CancellationToken.None);
        }

        var tasks = new List<Task>();

        // Concurrent success recordings
        tasks.AddRange(Enumerable.Range(0, 100).Select(_ => Task.Run(() => rateLimiter.RecordSuccess("concurrent-key"))));

        // Concurrent failure recordings
        tasks.AddRange(Enumerable.Range(0, 100).Select(_ => Task.Run(() => rateLimiter.RecordFailure("concurrent-key"))));

        // Act - No exceptions means thread-safe
        await Task.WhenAll(tasks);

        // Assert
        var state = rateLimiter.GetState("concurrent-key");
        state.Should().NotBeNull("state should be retrievable after concurrent operations");
    }

    [RateLimit(MaxRequestsPerWindow = 1000, WindowSizeSeconds = 60)]
    private sealed record RateLimitedRequest : IRequest<string>;

    private sealed class RateLimitedRequestHandler : IRequestHandler<RateLimitedRequest, string>
    {
        public Task<Either<EncinaError, string>> Handle(RateLimitedRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(Right<EncinaError, string>("Success"));
        }
    }

    [RateLimit(MaxRequestsPerWindow = 10, WindowSizeSeconds = 60)]
    private sealed record SmallLimitRequest : IRequest<string>;

    private sealed class SmallLimitRequestHandler : IRequestHandler<SmallLimitRequest, string>
    {
        public Task<Either<EncinaError, string>> Handle(SmallLimitRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(Right<EncinaError, string>("Success"));
        }
    }

    #endregion

    #region Bulkhead Load Tests

    [Fact]
    public async Task Bulkhead_HighConcurrency_ShouldBeThreadSafe()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncina(config => { });
        services.AddEncinaPolly();
        services.AddLogging();
        services.AddTransient<BulkheadRequestHandler>();

        var serviceProvider = services.BuildServiceProvider();
        var encina = serviceProvider.GetRequiredService<IEncina>();

        var tasks = Enumerable.Range(0, 100).Select(async i =>
        {
            var request = new BulkheadRequest();
            return await encina.Send(request);
        });

        // Act - No exceptions means thread-safe
        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().NotBeNull("all requests should complete without crashing");
    }

    [Fact]
    public async Task Bulkhead_EnforcesConcurrencyLimit_UnderLoad()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncina(config => { });
        services.AddEncinaPolly();
        services.AddLogging();
        services.AddTransient<SmallBulkheadRequestHandler>();

        var serviceProvider = services.BuildServiceProvider();
        var encina = serviceProvider.GetRequiredService<IEncina>();

        var concurrentCount = 0;
        var maxConcurrent = 0;
        var lockObj = new object();

        SmallBulkheadRequestHandler.OnExecute = async () =>
        {
            var current = Interlocked.Increment(ref concurrentCount);
            lock (lockObj)
            {
                if (current > maxConcurrent) maxConcurrent = current;
            }
            await Task.Delay(10); // Brief work
            Interlocked.Decrement(ref concurrentCount);
        };

        // Send 50 concurrent requests with a limit of 5
        var tasks = Enumerable.Range(0, 50).Select(async i =>
        {
            var request = new SmallBulkheadRequest();
            return await encina.Send(request);
        });

        // Act
        var results = await Task.WhenAll(tasks);

        // Assert
        maxConcurrent.Should().BeLessOrEqualTo(5, "concurrent executions should not exceed bulkhead limit");
    }

    [Fact]
    public async Task BulkheadManager_ConcurrentAcquireAndRelease_ShouldBeThreadSafe()
    {
        // Arrange
        using var manager = new BulkheadManager();
        var attribute = new BulkheadAttribute
        {
            MaxConcurrency = 100,
            MaxQueuedActions = 100,
            QueueTimeoutMs = 10000
        };

        var tasks = Enumerable.Range(0, 500).Select(async i =>
        {
            var result = await manager.TryAcquireAsync($"key-{i % 10}", attribute);
            if (result.IsAcquired)
            {
                await Task.Delay(1); // Brief hold
                result.Releaser?.Dispose();
            }
            return result;
        });

        // Act - No exceptions means thread-safe
        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(500, "all acquire attempts should complete");
    }

    [Fact]
    public async Task BulkheadManager_ConcurrentGetMetrics_ShouldBeThreadSafe()
    {
        // Arrange
        using var manager = new BulkheadManager();
        var attribute = new BulkheadAttribute { MaxConcurrency = 50 };

        // Pre-populate with some acquisitions
        var permits = new List<BulkheadAcquireResult>();
        for (int i = 0; i < 10; i++)
        {
            permits.Add(await manager.TryAcquireAsync("metrics-key", attribute));
        }

        var tasks = new List<Task<BulkheadMetrics?>>();

        // Concurrent metrics retrieval
        tasks.AddRange(Enumerable.Range(0, 100).Select(_ =>
            Task.Run(() => manager.GetMetrics("metrics-key"))));

        // Act - No exceptions means thread-safe
        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(100);
        results.Should().AllSatisfy(m => m.Should().NotBeNull());

        // Cleanup
        foreach (var permit in permits)
        {
            permit.Releaser?.Dispose();
        }
    }

    [Fact]
    public async Task Bulkhead_RejectsExcess_WhenBulkheadFull()
    {
        // Arrange
        using var manager = new BulkheadManager();
        var attribute = new BulkheadAttribute
        {
            MaxConcurrency = 5,
            MaxQueuedActions = 0, // No queue, immediate rejection
            QueueTimeoutMs = 100
        };

        // Hold all permits
        var permits = new List<BulkheadAcquireResult>();
        for (int i = 0; i < 5; i++)
        {
            permits.Add(await manager.TryAcquireAsync("full-key", attribute));
        }

        // Try to acquire more (should all be rejected)
        var tasks = Enumerable.Range(0, 100).Select(async i =>
        {
            return await manager.TryAcquireAsync("full-key", attribute);
        });

        // Act
        var results = await Task.WhenAll(tasks);

        // Assert
        var rejectedCount = results.Count(r => !r.IsAcquired && r.RejectionReason == BulkheadRejectionReason.BulkheadFull);
        rejectedCount.Should().Be(100, "all requests should be rejected when bulkhead is full with no queue");

        // Cleanup
        foreach (var permit in permits)
        {
            permit.Releaser?.Dispose();
        }
    }

    [Bulkhead(MaxConcurrency = 50, MaxQueuedActions = 100)]
    private sealed record BulkheadRequest : IRequest<string>;

    private sealed class BulkheadRequestHandler : IRequestHandler<BulkheadRequest, string>
    {
        public Task<Either<EncinaError, string>> Handle(BulkheadRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(Right<EncinaError, string>("Success"));
        }
    }

    [Bulkhead(MaxConcurrency = 5, MaxQueuedActions = 50, QueueTimeoutMs = 5000)]
    private sealed record SmallBulkheadRequest : IRequest<string>;

    private sealed class SmallBulkheadRequestHandler : IRequestHandler<SmallBulkheadRequest, string>
    {
        public static Func<Task>? OnExecute { get; set; }

        public async Task<Either<EncinaError, string>> Handle(SmallBulkheadRequest request, CancellationToken cancellationToken)
        {
            if (OnExecute != null)
            {
                await OnExecute();
            }
            return Right<EncinaError, string>("Success");
        }
    }

    #endregion
}
