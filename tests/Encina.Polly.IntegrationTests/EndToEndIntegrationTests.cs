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
        services.AddTransient<TestRetryRequestHandler>();

        var serviceProvider = services.BuildServiceProvider();
        var Encina = serviceProvider.GetRequiredService<IEncina>();

        var request = new TestRetryRequest(failCount: 1); // Fail once, then succeed

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
        services.AddTransient<TestRetryRequestHandler>();

        var serviceProvider = services.BuildServiceProvider();
        var Encina = serviceProvider.GetRequiredService<IEncina>();

        var request = new TestRetryRequest(failCount: 10); // Fail more than max attempts

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
        services.AddTransient<TestCircuitBreakerRequestHandler>();

        var serviceProvider = services.BuildServiceProvider();
        var Encina = serviceProvider.GetRequiredService<IEncina>();

        var request = new TestCircuitBreakerRequest(shouldFail: true);

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
        services.AddTransient<TestCombinedRequestHandler>();

        var serviceProvider = services.BuildServiceProvider();
        var Encina = serviceProvider.GetRequiredService<IEncina>();

        var request = new TestCombinedRequest(FailCount: 1);

        // Act
        var result = await Encina.Send(request);

        // Assert
        result.ShouldBeSuccess("retry should succeed before circuit breaks");
    }

    // Test request types and handlers
    [Retry(MaxAttempts = 3, BaseDelayMs = 10)]
    private sealed record TestRetryRequest(int FailCount) : IRequest<string>;

    private sealed class TestRetryRequestHandler : IRequestHandler<TestRetryRequest, string>
    {
        private static int _attemptCount;

        public Task<Either<EncinaError, string>> Handle(TestRetryRequest request, CancellationToken cancellationToken)
        {
            _attemptCount++;

            if (_attemptCount <= request.FailCount)
            {
                _attemptCount = 0; // Reset for next test
                return Task.FromResult(Left<EncinaError, string>(EncinaErrors.Create("test.error", "Simulated failure")));
            }

            _attemptCount = 0;
            return Task.FromResult(Right<EncinaError, string>("Success after retry"));
        }
    }

    [CircuitBreaker(FailureThreshold = 3, MinimumThroughput = 3, DurationOfBreakSeconds = 1, SamplingDurationSeconds = 2)]
    private sealed record TestCircuitBreakerRequest(bool ShouldFail) : IRequest<string>;

    private sealed class TestCircuitBreakerRequestHandler : IRequestHandler<TestCircuitBreakerRequest, string>
    {
        public Task<Either<EncinaError, string>> Handle(TestCircuitBreakerRequest request, CancellationToken cancellationToken)
        {
            if (request.ShouldFail)
            {
                return Task.FromResult(Left<EncinaError, string>(EncinaErrors.Create("test.error", "Simulated failure")));
            }

            return Task.FromResult(Right<EncinaError, string>("Success"));
        }
    }

    [Retry(MaxAttempts = 3, BaseDelayMs = 10)]
    [CircuitBreaker(FailureThreshold = 5, MinimumThroughput = 5, DurationOfBreakSeconds = 1)]
    private sealed record TestCombinedRequest(int FailCount) : IRequest<string>;

    private sealed class TestCombinedRequestHandler : IRequestHandler<TestCombinedRequest, string>
    {
        private static int _attemptCount;

        public Task<Either<EncinaError, string>> Handle(TestCombinedRequest request, CancellationToken cancellationToken)
        {
            _attemptCount++;

            if (_attemptCount <= request.FailCount)
            {
                return Task.FromResult(Left<EncinaError, string>(EncinaErrors.Create("test.error", "Simulated failure")));
            }

            _attemptCount = 0;
            return Task.FromResult(Right<EncinaError, string>("Success"));
        }
    }
}
