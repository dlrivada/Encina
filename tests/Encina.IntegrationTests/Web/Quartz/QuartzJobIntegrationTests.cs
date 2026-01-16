using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.IntegrationTests.Web.Quartz;

/// <summary>
/// Integration tests for Quartz job integration.
/// Tests end-to-end scenarios with DI container and real Encina.
/// </summary>
[Trait("Category", "Integration")]
public sealed class QuartzJobIntegrationTests
{
    [Fact]
    public async Task Integration_RequestJob_ShouldExecuteSuccessfully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncina();
        services.AddTransient<IRequestHandler<TestRequest, string>, TestRequestHandler>();

        var provider = services.BuildServiceProvider();
        var Encina = provider.GetRequiredService<IEncina>();
        var logger = Substitute.For<ILogger<QuartzRequestJob<TestRequest, string>>>();

        var job = new QuartzRequestJob<TestRequest, string>(Encina, logger);
        var request = new TestRequest("integration-test");
        var context = CreateJobExecutionContext(request);

        // Act
        await job.Execute(context);

        // Assert
        context.Result.ShouldNotBeNull();
        context.Result.ShouldBe("Processed: integration-test");
    }

    [Fact]
    public async Task Integration_NotificationJob_ShouldPublishSuccessfully()
    {
        // Arrange
        var handlerInvoked = false;
        var services = new ServiceCollection();
        services.AddEncina();
        services.AddTransient<INotificationHandler<TestNotification>>(sp =>
            new TestNotificationHandler(() => handlerInvoked = true));

        var provider = services.BuildServiceProvider();
        var Encina = provider.GetRequiredService<IEncina>();
        var logger = Substitute.For<ILogger<QuartzNotificationJob<TestNotification>>>();

        var job = new QuartzNotificationJob<TestNotification>(Encina, logger);
        var notification = new TestNotification("integration-test");
        var context = CreateJobExecutionContext(notification);

        // Act
        await job.Execute(context);

        // Assert
        handlerInvoked.ShouldBeTrue();
    }

    [Fact]
    public async Task Integration_MultipleNotificationHandlers_ShouldInvokeAll()
    {
        // Arrange
        var handler1Invoked = false;
        var handler2Invoked = false;

        var services = new ServiceCollection();
        services.AddEncina();
        services.AddTransient<INotificationHandler<TestNotification>>(sp =>
            new TestNotificationHandler(() => handler1Invoked = true));
        services.AddTransient<INotificationHandler<TestNotification>>(sp =>
            new TestNotificationHandler(() => handler2Invoked = true));

        var provider = services.BuildServiceProvider();
        var Encina = provider.GetRequiredService<IEncina>();
        var logger = Substitute.For<ILogger<QuartzNotificationJob<TestNotification>>>();

        var job = new QuartzNotificationJob<TestNotification>(Encina, logger);
        var notification = new TestNotification("multi-handler-test");
        var context = CreateJobExecutionContext(notification);

        // Act
        await job.Execute(context);

        // Assert
        handler1Invoked.ShouldBeTrue();
        handler2Invoked.ShouldBeTrue();
    }

    // Helper methods
    private static IJobExecutionContext CreateJobExecutionContext(TestRequest request)
    {
        var context = Substitute.For<IJobExecutionContext>();
        var jobDetail = Substitute.For<IJobDetail>();
        var jobDataMap = new JobDataMap();

        jobDataMap.Put(QuartzConstants.RequestKey, request);

        jobDetail.JobDataMap.Returns(jobDataMap);
        jobDetail.Key.Returns(new JobKey("test-job"));
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
        jobDetail.Key.Returns(new JobKey("test-job"));
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
    private readonly Action _onHandle;

    public TestNotificationHandler(Action onHandle)
    {
        _onHandle = onHandle;
    }

    public Task<Either<EncinaError, Unit>> Handle(
        TestNotification notification,
        CancellationToken cancellationToken)
    {
        _onHandle();
        return Task.FromResult(Right<EncinaError, Unit>(unit));
    }
}
