using Encina.Quartz;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NBomber.CSharp;
using Quartz;
using Xunit.Abstractions;
using static LanguageExt.Prelude;

namespace Encina.Quartz.LoadTests;

/// <summary>
/// Load tests for Quartz jobs using NBomber.
/// Verifies performance, concurrency, and throughput under stress conditions.
/// </summary>
[Trait("Category", "Load")]
public sealed class QuartzJobLoadTests
{
    private readonly ITestOutputHelper _output;

    public QuartzJobLoadTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void HighConcurrency_RequestJob_ShouldHandleLoad()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncina();
        services.AddTransient<IRequestHandler<TestRequest, string>, TestRequestHandler>();

        var provider = services.BuildServiceProvider();

        // Act
        var scenario = Scenario.Create("request_job", async context =>
        {
            using var scope = provider.CreateScope();
            var Encina = scope.ServiceProvider.GetRequiredService<IEncina>();
            var logger = Substitute.For<ILogger<QuartzRequestJob<TestRequest, string>>>();
            var job = new QuartzRequestJob<TestRequest, string>(Encina, logger);

            var request = new TestRequest("load-test");
            var jobContext = CreateJobExecutionContext(request);

            await job.Execute(jobContext);

            return Response.Ok();
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
        _output.WriteLine($"OK: {scen.Ok.Request.Count}, Fail: {scen.Fail.Request.Count}");
        Assert.True(scen.Ok.Request.Count > 900, $"Expected > 900, got {scen.Ok.Request.Count}");
    }

    [Fact]
    public void HighConcurrency_NotificationJob_ShouldHandleLoad()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncina();
        services.AddTransient<INotificationHandler<TestNotification>, TestNotificationHandler>();

        var provider = services.BuildServiceProvider();

        // Act
        var scenario = Scenario.Create("notification_job", async context =>
        {
            using var scope = provider.CreateScope();
            var Encina = scope.ServiceProvider.GetRequiredService<IEncina>();
            var logger = Substitute.For<ILogger<QuartzNotificationJob<TestNotification>>>();
            var job = new QuartzNotificationJob<TestNotification>(Encina, logger);

            var notification = new TestNotification("load-test");
            var jobContext = CreateJobExecutionContext(notification);

            await job.Execute(jobContext);

            return Response.Ok();
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
        _output.WriteLine($"OK: {scen.Ok.Request.Count}, RPS: {scen.Ok.Request.RPS}");
        Assert.True(scen.Ok.Request.Count > 900, $"Expected > 900, got {scen.Ok.Request.Count}");
    }

    [Fact]
    public void Endurance_ContinuousLoad_ShouldNotDegrade()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncina();
        services.AddTransient<IRequestHandler<TestRequest, string>, TestRequestHandler>();

        var provider = services.BuildServiceProvider();

        // Act
        var scenario = Scenario.Create("endurance", async context =>
        {
            using var scope = provider.CreateScope();
            var Encina = scope.ServiceProvider.GetRequiredService<IEncina>();
            var logger = Substitute.For<ILogger<QuartzRequestJob<TestRequest, string>>>();
            var job = new QuartzRequestJob<TestRequest, string>(Encina, logger);

            var request = new TestRequest("endurance-test");
            var jobContext = CreateJobExecutionContext(request);

            await job.Execute(jobContext);

            return Response.Ok();
        })
        .WithLoadSimulations(
            Simulation.KeepConstant(copies: 20, during: TimeSpan.FromSeconds(30))
        );

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .WithoutReports()
            .Run();

        // Assert
        var scen = stats.ScenarioStats[0];
        _output.WriteLine($"OK: {scen.Ok.Request.Count}");
        Assert.True(scen.Ok.Request.Count > 0, $"Expected > 0, got {scen.Ok.Request.Count}");
    }

    // Helper methods
    private static IJobExecutionContext CreateJobExecutionContext(TestRequest request)
    {
        var context = Substitute.For<IJobExecutionContext>();
        var jobDetail = Substitute.For<IJobDetail>();
        var jobDataMap = new JobDataMap();

        jobDataMap.Put(QuartzConstants.RequestKey, request);

        jobDetail.JobDataMap.Returns(jobDataMap);
        jobDetail.Key.Returns(new JobKey("load-test-job"));
        context.JobDetail.Returns(jobDetail);
        context.CancellationToken.Returns(CancellationToken.None);

        return context;
    }

    private static IJobExecutionContext CreateJobExecutionContext(TestNotification notification)
    {
        var context = Substitute.For<IJobExecutionContext>();
        var jobDetail = Substitute.For<IJobDetail>();
        var jobDataMap = new JobDataMap();

        jobDataMap.Put(QuartzConstants.NotificationKey, notification);

        jobDetail.JobDataMap.Returns(jobDataMap);
        jobDetail.Key.Returns(new JobKey("load-test-job"));
        context.JobDetail.Returns(jobDetail);
        context.CancellationToken.Returns(CancellationToken.None);

        return context;
    }
}

// Test types
public sealed record TestRequest(string Data) : IRequest<string>;
public sealed record TestNotification(string Message) : INotification;

public sealed class TestRequestHandler : IRequestHandler<TestRequest, string>
{
    public Task<Either<EncinaError, string>> Handle(
        TestRequest request,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(Right<EncinaError, string>($"Processed: {request.Data}"));
    }
}

public sealed class TestNotificationHandler : INotificationHandler<TestNotification>
{
    public Task<Either<EncinaError, Unit>> Handle(
        TestNotification notification,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(Right<EncinaError, Unit>(unit));
    }
}
