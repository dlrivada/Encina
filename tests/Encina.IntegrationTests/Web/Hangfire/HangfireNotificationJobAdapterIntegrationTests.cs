using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.IntegrationTests.Web.Hangfire;

/// <summary>
/// Integration tests for HangfireNotificationJobAdapter.
/// Tests end-to-end scenarios with DI container and real Encina.
/// </summary>
[Trait("Category", "Integration")]
public sealed class HangfireNotificationJobAdapterIntegrationTests
{
    [Fact]
    public async Task Integration_ValidNotification_ShouldPublishSuccessfully()
    {
        // Arrange
        var handlerInvoked = false;
        var services = new ServiceCollection();
        services.AddEncina();
        services.AddTransient<INotificationHandler<TestNotification>>(sp =>
            new TestNotificationHandler(() => handlerInvoked = true));

        var provider = services.BuildServiceProvider();
        var Encina = provider.GetRequiredService<IEncina>();
        var logger = Substitute.For<ILogger<HangfireNotificationJobAdapter<TestNotification>>>();

        var adapter = new HangfireNotificationJobAdapter<TestNotification>(Encina, logger);
        var notification = new TestNotification("integration-test");

        // Act
        await adapter.PublishAsync(notification);

        // Assert
        handlerInvoked.ShouldBeTrue();
    }

    [Fact]
    public async Task Integration_MultipleHandlers_ShouldInvokeAll()
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
        var logger = Substitute.For<ILogger<HangfireNotificationJobAdapter<TestNotification>>>();

        var adapter = new HangfireNotificationJobAdapter<TestNotification>(Encina, logger);
        var notification = new TestNotification("multi-handler-test");

        // Act
        await adapter.PublishAsync(notification);

        // Assert
        handler1Invoked.ShouldBeTrue();
        handler2Invoked.ShouldBeTrue();
    }
}

// Test types
public sealed record TestNotification(string Message) : INotification;

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
