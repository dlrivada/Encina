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
}
