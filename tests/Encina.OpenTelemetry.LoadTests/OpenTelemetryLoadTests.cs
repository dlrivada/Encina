using FluentAssertions;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using NBomber.CSharp;
using OpenTelemetry.Trace;
using Xunit;
using Xunit.Abstractions;

namespace Encina.OpenTelemetry.LoadTests;

/// <summary>
/// Load tests for Encina.OpenTelemetry using NBomber.
/// Verifies performance and concurrency under stress.
/// </summary>
[Trait("Category", "Load")]
public sealed class OpenTelemetryLoadTests
{
    private readonly ITestOutputHelper _output;

    public OpenTelemetryLoadTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact(Skip = "Load test - requires proper DI scoping setup")]
    public void HighConcurrency_Send_WithOpenTelemetry_ShouldHandleLoad()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncina(config => { });
        services.AddSingleton<IRequestHandler<LoadTestRequest, string>, LoadTestHandler>();

        services.AddOpenTelemetry()
            .WithTracing(builder => builder
                .AddEncinaInstrumentation()
                .AddConsoleExporter());

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act - Load test with NBomber
        var scenario = Scenario.Create("send_requests_with_telemetry", async context =>
        {
            var instanceId = int.Parse(context.ScenarioInfo.InstanceId.Split('-').Last(), System.Globalization.CultureInfo.InvariantCulture);
            var result = await mediator.Send(new LoadTestRequest { Value = instanceId }, CancellationToken.None);

            return result.IsRight ? Response.Ok() : Response.Fail<string>(statusCode: result.Match(Right: _ => "", Left: e => e.Message));
        })
        .WithLoadSimulations(
            Simulation.Inject(rate: 100, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(10))
        );

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .WithoutReports()
            .Run();

        // Assert
        var scen = stats.ScenarioStats[0];
        _output.WriteLine($"Total requests: {scen.Ok.Request.Count + scen.Fail.Request.Count}");
        _output.WriteLine($"OK: {scen.Ok.Request.Count}, Fail: {scen.Fail.Request.Count}");
        _output.WriteLine($"RPS: {scen.Ok.Request.RPS}");

        scen.Ok.Request.Count.Should().BeGreaterThan(900); // At least 90% success rate
        scen.Fail.Request.Count.Should().BeLessThan(100); // Less than 10% failures
    }

    [Fact(Skip = "Load test - requires proper DI scoping setup")]
    public void Stress_Publish_WithOpenTelemetry_ShouldHandleLoad()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncina(config => { });
        services.AddSingleton<INotificationHandler<LoadTestNotification>, LoadTestNotificationHandler>();

        services.AddOpenTelemetry()
            .WithTracing(builder => builder
                .AddEncinaInstrumentation()
                .AddConsoleExporter());

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act - Load test with NBomber
        var scenario = Scenario.Create("publish_notifications_with_telemetry", async context =>
        {
            var result = await mediator.Publish(new LoadTestNotification { Message = $"msg-{context.ScenarioInfo.InstanceId}" }, CancellationToken.None);

            return result.IsRight ? Response.Ok() : Response.Fail<Unit>(statusCode: result.Match(Right: _ => "", Left: e => e.Message));
        })
        .WithLoadSimulations(
            Simulation.Inject(rate: 50, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(10))
        );

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .WithoutReports()
            .Run();

        // Assert
        var scen = stats.ScenarioStats[0];
        _output.WriteLine($"Total notifications: {scen.Ok.Request.Count + scen.Fail.Request.Count}");
        _output.WriteLine($"OK: {scen.Ok.Request.Count}, Fail: {scen.Fail.Request.Count}");

        scen.Ok.Request.Count.Should().BeGreaterThan(450); // At least 90% success rate
    }

    #region Test Helpers

    private sealed record LoadTestRequest : IRequest<string>
    {
        public int Value { get; init; }
    }

    private sealed class LoadTestHandler : IRequestHandler<LoadTestRequest, string>
    {
        public async Task<Either<MediatorError, string>> Handle(LoadTestRequest request, CancellationToken cancellationToken)
        {
            await Task.Delay(1, cancellationToken); // Simulate minimal work
            return $"processed-{request.Value}";
        }
    }

    private sealed record LoadTestNotification : INotification
    {
        public string Message { get; init; } = string.Empty;
    }

    private sealed class LoadTestNotificationHandler : INotificationHandler<LoadTestNotification>
    {
        public async Task<Either<MediatorError, Unit>> Handle(LoadTestNotification notification, CancellationToken cancellationToken)
        {
            await Task.Delay(1, cancellationToken); // Simulate minimal work
            return Unit.Default;
        }
    }

    #endregion
}
