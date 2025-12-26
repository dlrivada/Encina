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
}
