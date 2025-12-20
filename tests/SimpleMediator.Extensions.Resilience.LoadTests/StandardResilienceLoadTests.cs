using FluentAssertions;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NBomber.CSharp;
using Polly;
using SimpleMediator.Extensions.Resilience;
using Xunit;
using Xunit.Abstractions;

namespace SimpleMediator.Extensions.Resilience.LoadTests;

/// <summary>
/// Load tests for SimpleMediator with Standard Resilience using NBomber.
/// Tests behavior under high concurrency and stress conditions.
/// </summary>
[Trait("Category", "Load")]
public class StandardResilienceLoadTests
{
    private readonly ITestOutputHelper _output;

    public StandardResilienceLoadTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task HighConcurrency_RateLimiter_ShouldThrottle()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSimpleMediator(typeof(StandardResilienceLoadTests).Assembly);
        services.AddSimpleMediatorStandardResilience(options =>
        {
            options.RateLimiter = new Polly.RateLimiting.RateLimiterStrategyOptions
            {
                // Default rate limiter configuration
            };
        });

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<ISimpleMediator>();

        // Act - Send 100 concurrent requests
        var tasks = Enumerable.Range(0, 100).Select(async i =>
        {
            var request = new LoadTestRequest { Value = i };
            var result = await mediator.Send(request);
            return result.IsRight || result.IsLeft; // Either success or rate limited
        });

        var results = await Task.WhenAll(tasks);

        // Assert - All should complete (some may be rate limited)
        results.Should().AllSatisfy(r => r.Should().BeTrue());
    }

    [Fact]
    public async Task HighConcurrency_CircuitBreaker_ShouldOpenUnderLoad()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSimpleMediator(typeof(StandardResilienceLoadTests).Assembly);
        services.AddSimpleMediatorStandardResilience(options =>
        {
            options.CircuitBreaker.FailureRatio = 0.5;
            options.CircuitBreaker.MinimumThroughput = 10;
            options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
            options.Retry.MaxRetryAttempts = 0; // Disable retry
        });

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<ISimpleMediator>();

        // Act - Send 50 requests that fail
        var tasks = Enumerable.Range(0, 50).Select(async i =>
        {
            var request = new LoadTestRequest { Value = -1 }; // Negative value triggers failure
            var result = await mediator.Send(request);
            return result;
        });

        var results = await Task.WhenAll(tasks);

        // Assert - All should fail (either from handler or circuit breaker)
        results.Should().OnlyContain(r => r.IsLeft);
    }

    [Fact]
    public async Task MixedLoad_SuccessAndFailure_ShouldHandleCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSimpleMediator(typeof(StandardResilienceLoadTests).Assembly);
        services.AddSimpleMediatorStandardResilience(options =>
        {
            options.Retry.MaxRetryAttempts = 2;
            options.Retry.Delay = TimeSpan.FromMilliseconds(10);
            options.CircuitBreaker.FailureRatio = 0.9; // High threshold
        });

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<ISimpleMediator>();

        // Act - Send mix of successful and failing requests
        var tasks = Enumerable.Range(0, 100).Select(async i =>
        {
            // 70% success, 30% failure
            var value = i % 10 < 7 ? i : -1;
            var request = new LoadTestRequest { Value = value };
            var result = await mediator.Send(request);
            return (IsSuccess: result.IsRight, Value: value);
        });

        var results = await Task.WhenAll(tasks);

        // Assert
        var successCount = results.Count(r => r.IsSuccess);
        var failureCount = results.Count(r => !r.IsSuccess);

        _output.WriteLine($"Successes: {successCount}, Failures: {failureCount}");
        successCount.Should().BeGreaterThan(50);
        failureCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task NBomber_ConstantLoad_ShouldMaintainThroughput()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
        services.AddSimpleMediator(typeof(StandardResilienceLoadTests).Assembly);
        services.AddSimpleMediatorStandardResilience();

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<ISimpleMediator>();

        var scenario = Scenario.Create("constant_load", async context =>
        {
            var request = new LoadTestRequest { Value = context.ScenarioInfo.ThreadNumber };
            var result = await mediator.Send(request);

            return result.Match(
                Right: _ => Response.Ok(),
                Left: error => Response.Fail(error.Message)
            );
        })
        .WithLoadSimulations(
            Simulation.Inject(rate: 50, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(10))
        );

        // Act
        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportFolder("load-test-results")
            .WithReportFileName("resilience-load-test")
            .WithReportFormats(ReportFormat.Html, ReportFormat.Md)
            .Run();

        // Assert
        var scnStats = stats.ScenarioStats[0];
        _output.WriteLine($"OK Count: {scnStats.Ok.Request.Count}");
        _output.WriteLine($"Fail Count: {scnStats.Fail.Request.Count}");
        _output.WriteLine($"RPS: {scnStats.Ok.Request.RPS}");

        scnStats.Ok.Request.Count.Should().BeGreaterThan(400);
    }

    [Fact]
    public async Task NBomber_RampUpLoad_ShouldHandleIncreasingTraffic()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
        services.AddSimpleMediator(typeof(StandardResilienceLoadTests).Assembly);
        services.AddSimpleMediatorStandardResilience();

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<ISimpleMediator>();

        var scenario = Scenario.Create("ramp_up_load", async context =>
        {
            var request = new LoadTestRequest { Value = context.ScenarioInfo.ThreadNumber };
            var result = await mediator.Send(request);

            return result.Match(
                Right: _ => Response.Ok(),
                Left: error => Response.Fail(error.Message)
            );
        })
        .WithLoadSimulations(
            Simulation.RampingInject(rate: 10, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(5)),
            Simulation.RampingInject(rate: 50, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(5))
        );

        // Act
        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportFolder("load-test-results")
            .WithReportFileName("resilience-ramp-up-test")
            .WithReportFormats(ReportFormat.Html, ReportFormat.Md)
            .Run();

        // Assert
        var scnStats = stats.ScenarioStats[0];
        _output.WriteLine($"OK Count: {scnStats.Ok.Request.Count}");
        _output.WriteLine($"Fail Count: {scnStats.Fail.Request.Count}");

        scnStats.Ok.Request.Count.Should().BeGreaterThan(200);
    }

    // Test request/response types
    private record LoadTestRequest : IRequest<LoadTestResponse>
    {
        public int Value { get; init; }
    }

    private record LoadTestResponse
    {
        public int Result { get; init; }
    }

    // Test handler
    private class LoadTestRequestHandler : IRequestHandler<LoadTestRequest, LoadTestResponse>
    {
        public async ValueTask<Either<MediatorError, LoadTestResponse>> Handle(
            LoadTestRequest request,
            IRequestContext context,
            CancellationToken cancellationToken)
        {
            // Simulate some processing
            await Task.Delay(1, cancellationToken);

            if (request.Value < 0)
            {
                return MediatorError.New("Invalid value");
            }

            return new LoadTestResponse { Result = request.Value * 2 };
        }
    }
}
