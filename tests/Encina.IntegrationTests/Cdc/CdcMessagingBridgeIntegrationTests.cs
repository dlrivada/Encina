using System.Diagnostics.CodeAnalysis;
using Encina.Cdc;
using Encina.Cdc.Abstractions;
using Encina.Cdc.Messaging;
using Encina.IntegrationTests.Cdc.Helpers;
using Encina.Testing.Fakes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace Encina.IntegrationTests.Cdc;

/// <summary>
/// Integration tests for <see cref="CdcMessagingBridge"/> verifying that CDC events
/// are correctly published through Encina's notification pipeline using <see cref="FakeEncina"/>.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Feature", "CDC")]
[SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "Integration test assertions on ValueTask results")]
public sealed class CdcMessagingBridgeIntegrationTests
{
    #region Test Helpers

    private sealed record MessagingFixture(
        ServiceProvider ServiceProvider,
        ICdcDispatcher Dispatcher,
        FakeEncina FakeEncina,
        TrackingChangeHandler Handler) : IDisposable
    {
        public void Dispose() => ServiceProvider.Dispose();
    }

    private static MessagingFixture CreateMessagingFixture(
        Action<CdcMessagingOptions>? configureMessaging = null)
    {
        var fakeEncina = new FakeEncina();
        var handler = new TrackingChangeHandler();

        var services = new ServiceCollection();
        services.AddLogging(b => b.AddProvider(NullLoggerProvider.Instance));
        services.AddSingleton<IEncina>(fakeEncina);
        services.AddEncinaCdc(config =>
        {
            config.AddHandler<TestEntity, TrackingChangeHandler>()
                  .WithTableMapping<TestEntity>("TestEntities")
                  .WithMessagingBridge(opts =>
                  {
                      configureMessaging?.Invoke(opts);
                  });
        });
        // Register singleton AFTER AddEncinaCdc to override the scoped registration
        services.AddSingleton<IChangeEventHandler<TestEntity>>(handler);

        var sp = services.BuildServiceProvider();
        var dispatcher = sp.GetRequiredService<ICdcDispatcher>();

        return new MessagingFixture(sp, dispatcher, fakeEncina, handler);
    }

    #endregion

    #region Notification Publishing

    [Fact]
    public async Task Dispatch_WithMessagingBridge_PublishesCdcChangeNotification()
    {
        // Arrange
        using var fixture = CreateMessagingFixture();
        var evt = CdcTestFixtures.CreateInsertEvent(id: 1, name: "Test");

        // Act
        var result = await fixture.Dispatcher.DispatchAsync(evt);

        // Assert
        result.IsRight.ShouldBeTrue();
        fixture.FakeEncina.PublishedNotifications.Count.ShouldBe(1);
        var notification = fixture.FakeEncina.PublishedNotifications[0].ShouldBeOfType<CdcChangeNotification>();
        notification.TableName.ShouldBe("TestEntities");
        notification.Operation.ShouldBe(ChangeOperation.Insert);
    }

    [Fact]
    public async Task Dispatch_WithMessagingBridge_UsesDefaultTopicPattern()
    {
        // Arrange
        using var fixture = CreateMessagingFixture();
        var evt = CdcTestFixtures.CreateInsertEvent(tableName: "TestEntities");

        // Act
        await fixture.Dispatcher.DispatchAsync(evt);

        // Assert
        var notification = fixture.FakeEncina.PublishedNotifications[0].ShouldBeOfType<CdcChangeNotification>();
        notification.TopicName.ShouldBe("TestEntities.insert");
    }

    [Fact]
    public async Task Dispatch_WithCustomTopicPattern_AppliesPattern()
    {
        // Arrange
        using var fixture = CreateMessagingFixture(opts =>
        {
            opts.TopicPattern = "cdc.{tableName}.{operation}";
        });
        var evt = CdcTestFixtures.CreateUpdateEvent(tableName: "TestEntities");

        // Act
        await fixture.Dispatcher.DispatchAsync(evt);

        // Assert
        var notification = fixture.FakeEncina.PublishedNotifications[0].ShouldBeOfType<CdcChangeNotification>();
        notification.TopicName.ShouldBe("cdc.TestEntities.update");
    }

    #endregion

    #region Filtering

    [Fact]
    public async Task Dispatch_WithExcludedTable_DoesNotPublishNotification()
    {
        // Arrange
        using var fixture = CreateMessagingFixture(opts =>
        {
            opts.ExcludeTables = ["TestEntities"];
        });
        var evt = CdcTestFixtures.CreateInsertEvent(tableName: "TestEntities");

        // Act
        var result = await fixture.Dispatcher.DispatchAsync(evt);

        // Assert
        result.IsRight.ShouldBeTrue();
        // Handler should still be invoked (filtering is in the interceptor, not the dispatcher)
        fixture.Handler.Invocations.ShouldHaveSingleItem();
        // But notification should NOT be published
        fixture.FakeEncina.PublishedNotifications.ShouldBeEmpty();
    }

    [Fact]
    public async Task Dispatch_WithIncludedTables_OnlyPublishesMatchingTables()
    {
        // Arrange
        using var fixture = CreateMessagingFixture(opts =>
        {
            opts.IncludeTables = ["AllowedTable"];
        });
        // TestEntities is NOT in IncludeTables
        var evt = CdcTestFixtures.CreateInsertEvent(tableName: "TestEntities");

        // Act
        var result = await fixture.Dispatcher.DispatchAsync(evt);

        // Assert
        result.IsRight.ShouldBeTrue();
        fixture.Handler.Invocations.ShouldHaveSingleItem();
        fixture.FakeEncina.PublishedNotifications.ShouldBeEmpty();
    }

    #endregion

    #region Multiple Events

    [Fact]
    public async Task Dispatch_MultipleEvents_PublishesNotificationForEach()
    {
        // Arrange
        using var fixture = CreateMessagingFixture();
        var events = new[]
        {
            CdcTestFixtures.CreateInsertEvent(id: 1, positionValue: 1),
            CdcTestFixtures.CreateUpdateEvent(id: 2, positionValue: 2),
            CdcTestFixtures.CreateDeleteEvent(id: 3, positionValue: 3)
        };

        // Act
        foreach (var evt in events)
        {
            var result = await fixture.Dispatcher.DispatchAsync(evt);
            result.IsRight.ShouldBeTrue();
        }

        // Assert
        fixture.FakeEncina.PublishedNotifications.Count.ShouldBe(3);
        var notifications = fixture.FakeEncina.PublishedNotifications
            .Cast<CdcChangeNotification>()
            .ToList();

        notifications[0].Operation.ShouldBe(ChangeOperation.Insert);
        notifications[1].Operation.ShouldBe(ChangeOperation.Update);
        notifications[2].Operation.ShouldBe(ChangeOperation.Delete);
    }

    #endregion

    #region Full Pipeline with Processor

    [Fact]
    public async Task FullPipeline_ProcessorWithMessagingBridge_PublishesAllEvents()
    {
        // Arrange
        var connector = new TestCdcConnector("messaging-test");
        connector
            .AddEvent(CdcTestFixtures.CreateInsertEvent(id: 1, positionValue: 1))
            .AddEvent(CdcTestFixtures.CreateInsertEvent(id: 2, positionValue: 2));

        var fakeEncina = new FakeEncina();
        var handler = new TrackingChangeHandler();

        var services = new ServiceCollection();
        services.AddLogging(b => b.AddProvider(NullLoggerProvider.Instance));
        services.AddSingleton<ICdcConnector>(connector);
        services.AddSingleton<IEncina>(fakeEncina);
        services.AddEncinaCdc(config =>
        {
            config.UseCdc()
                  .AddHandler<TestEntity, TrackingChangeHandler>()
                  .WithTableMapping<TestEntity>("TestEntities")
                  .WithOptions(opts => opts.PollingInterval = TimeSpan.FromMilliseconds(50))
                  .WithMessagingBridge(opts =>
                  {
                      opts.TopicPattern = "cdc.{tableName}.{operation}";
                  });
        });
        // Register singleton AFTER AddEncinaCdc to override the scoped registration
        services.AddSingleton<IChangeEventHandler<TestEntity>>(handler);

        using var sp = services.BuildServiceProvider();
        var hostedServices = sp.GetServices<Microsoft.Extensions.Hosting.IHostedService>();
        var processor = hostedServices.First(s => s.GetType().Name == "CdcProcessor");

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));

        // Act
        try
        {
            await processor.StartAsync(cts.Token);
            await Task.Delay(TimeSpan.FromMilliseconds(400), cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Expected
        }
        finally
        {
            await processor.StopAsync(CancellationToken.None);
        }

        // Assert
        handler.Invocations.Count.ShouldBe(2);
        fakeEncina.PublishedNotifications.Count.ShouldBe(2);
        var notifications = fakeEncina.PublishedNotifications
            .Cast<CdcChangeNotification>()
            .ToList();
        notifications.ShouldAllBe(n => n.TopicName == "cdc.TestEntities.insert");
    }

    #endregion
}
