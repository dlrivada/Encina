using System.Diagnostics.CodeAnalysis;
using Encina.Cdc;
using Encina.Cdc.Abstractions;
using Encina.Cdc.Messaging;
using Encina.Cdc.Processing;
using Encina.IntegrationTests.Cdc.Helpers;
using Encina.Testing.Fakes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace Encina.IntegrationTests.Cdc;

/// <summary>
/// End-to-end integration tests for the complete CDC pipeline:
/// Connector → Processor → Dispatcher → Handler → Interceptor → PositionStore.
/// Verifies the full flow with real DI, real service resolution, and real async execution.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Feature", "CDC")]
[SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "Integration test assertions on ValueTask results")]
public sealed class CdcPipelineEndToEndIntegrationTests
{
    #region Full Pipeline

    [Fact]
    public async Task FullPipeline_InsertUpdateDelete_AllHandledAndPositionTracked()
    {
        // Arrange
        var connector = new TestCdcConnector("e2e-test");
        connector
            .AddEvent(CdcTestFixtures.CreateInsertEvent(id: 1, name: "Created", positionValue: 10))
            .AddEvent(CdcTestFixtures.CreateUpdateEvent(id: 1, oldName: "Created", newName: "Updated", positionValue: 20))
            .AddEvent(CdcTestFixtures.CreateDeleteEvent(id: 1, name: "Updated", positionValue: 30));

        var handler = new TrackingChangeHandler();

        var services = new ServiceCollection();
        services.AddLogging(b => b.AddProvider(NullLoggerProvider.Instance));
        services.AddSingleton<ICdcConnector>(connector);
        services.AddEncinaCdc(config =>
        {
            config.UseCdc()
                  .AddHandler<TestEntity, TrackingChangeHandler>()
                  .WithTableMapping<TestEntity>("TestEntities")
                  .WithOptions(opts =>
                  {
                      opts.PollingInterval = TimeSpan.FromMilliseconds(50);
                  });
        });
        // Register singleton AFTER AddEncinaCdc to override the scoped registration
        services.AddSingleton<IChangeEventHandler<TestEntity>>(handler);

        using var sp = services.BuildServiceProvider();

        // Act
        await RunProcessorOnce(sp, TimeSpan.FromMilliseconds(500));

        // Assert — All three operations processed
        handler.Invocations.Count.ShouldBe(3);
        handler.Invocations[0].Operation.ShouldBe("Insert");
        handler.Invocations[0].After!.Name.ShouldBe("Created");
        handler.Invocations[1].Operation.ShouldBe("Update");
        handler.Invocations[1].Before!.Name.ShouldBe("Created");
        handler.Invocations[1].After!.Name.ShouldBe("Updated");
        handler.Invocations[2].Operation.ShouldBe("Delete");
        handler.Invocations[2].Before!.Name.ShouldBe("Updated");

        // Assert — Position saved at last event
        var positionStore = sp.GetRequiredService<ICdcPositionStore>();
        var posResult = await positionStore.GetPositionAsync("e2e-test");
        posResult.IsRight.ShouldBeTrue();
        var option = posResult.Match(Right: o => o, Left: _ => default);
        option.IsSome.ShouldBeTrue();
        option.IfSome(p =>
        {
            p.ShouldBeOfType<TestCdcPosition>().Value.ShouldBe(30);
        });
    }

    #endregion

    #region Pipeline with Messaging Bridge

    [Fact]
    public async Task FullPipeline_WithMessagingBridge_HandlerAndNotificationBothFire()
    {
        // Arrange
        var connector = new TestCdcConnector("e2e-messaging");
        connector
            .AddEvent(CdcTestFixtures.CreateInsertEvent(id: 1, name: "NotifyMe", positionValue: 1))
            .AddEvent(CdcTestFixtures.CreateInsertEvent(id: 2, name: "AlsoNotify", positionValue: 2));

        var handler = new TrackingChangeHandler();
        var fakeEncina = new FakeEncina();

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
                      opts.TopicPattern = "events.{tableName}.{operation}";
                  });
        });
        // Register singleton AFTER AddEncinaCdc to override the scoped registration
        services.AddSingleton<IChangeEventHandler<TestEntity>>(handler);

        using var sp = services.BuildServiceProvider();

        // Act
        await RunProcessorOnce(sp, TimeSpan.FromMilliseconds(500));

        // Assert — Handler invoked for both events
        handler.Invocations.Count.ShouldBe(2);

        // Assert — Notifications published for both events
        fakeEncina.PublishedNotifications.Count.ShouldBe(2);
        var notifications = fakeEncina.PublishedNotifications
            .Cast<CdcChangeNotification>()
            .ToList();
        notifications.ShouldAllBe(n => n.TopicName == "events.TestEntities.insert");
        notifications[0].After.ShouldNotBeNull();
        notifications[1].After.ShouldNotBeNull();
    }

    [Fact]
    public async Task FullPipeline_WithMessagingFilter_OnlyMatchingTablesPublished()
    {
        // Arrange
        var connector = new TestCdcConnector("e2e-filter");
        connector
            .AddEvent(CdcTestFixtures.CreateInsertEvent(tableName: "TestEntities", id: 1, positionValue: 1))
            .AddEvent(CdcTestFixtures.CreateInsertEvent(tableName: "TestEntities", id: 2, positionValue: 2));

        var handler = new TrackingChangeHandler();
        var fakeEncina = new FakeEncina();

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
                      opts.ExcludeTables = ["TestEntities"];
                  });
        });
        // Register singleton AFTER AddEncinaCdc to override the scoped registration
        services.AddSingleton<IChangeEventHandler<TestEntity>>(handler);

        using var sp = services.BuildServiceProvider();

        // Act
        await RunProcessorOnce(sp, TimeSpan.FromMilliseconds(500));

        // Assert — Handler invoked (dispatch is independent of messaging bridge filtering)
        handler.Invocations.Count.ShouldBe(2);

        // Assert — No notifications published (table is excluded)
        fakeEncina.PublishedNotifications.ShouldBeEmpty();
    }

    #endregion

    #region Pipeline with Custom Interceptor

    [Fact]
    public async Task FullPipeline_WithCustomInterceptor_InterceptorInvokedAfterHandler()
    {
        // Arrange
        var connector = new TestCdcConnector("e2e-interceptor");
        connector.AddEvent(CdcTestFixtures.CreateInsertEvent(id: 1, positionValue: 1));

        var handler = new TrackingChangeHandler();
        var interceptor = new TrackingInterceptor();

        var services = new ServiceCollection();
        services.AddLogging(b => b.AddProvider(NullLoggerProvider.Instance));
        services.AddSingleton<ICdcConnector>(connector);
        services.AddSingleton<ICdcEventInterceptor>(interceptor);
        services.AddEncinaCdc(config =>
        {
            config.UseCdc()
                  .AddHandler<TestEntity, TrackingChangeHandler>()
                  .WithTableMapping<TestEntity>("TestEntities")
                  .WithOptions(opts => opts.PollingInterval = TimeSpan.FromMilliseconds(50));
        });
        // Register singleton AFTER AddEncinaCdc to override the scoped registration
        services.AddSingleton<IChangeEventHandler<TestEntity>>(handler);

        using var sp = services.BuildServiceProvider();

        // Act
        await RunProcessorOnce(sp, TimeSpan.FromMilliseconds(500));

        // Assert — Both handler and interceptor invoked
        handler.Invocations.ShouldHaveSingleItem();
        interceptor.InterceptedEvents.ShouldHaveSingleItem();
        interceptor.InterceptedEvents[0].TableName.ShouldBe("TestEntities");
    }

    #endregion

    #region Pipeline Error Recovery

    [Fact]
    public async Task FullPipeline_MixOfSuccessAndUnmappedTables_SuccessEventsProcessed()
    {
        // Arrange
        var connector = new TestCdcConnector("e2e-mixed");
        connector
            .AddEvent(CdcTestFixtures.CreateInsertEvent(tableName: "TestEntities", id: 1, positionValue: 1))
            .AddEvent(CdcTestFixtures.CreateInsertEvent(tableName: "UnknownTable", id: 2, positionValue: 2))
            .AddEvent(CdcTestFixtures.CreateInsertEvent(tableName: "TestEntities", id: 3, positionValue: 3));

        var handler = new TrackingChangeHandler();

        var services = new ServiceCollection();
        services.AddLogging(b => b.AddProvider(NullLoggerProvider.Instance));
        services.AddSingleton<ICdcConnector>(connector);
        services.AddEncinaCdc(config =>
        {
            config.UseCdc()
                  .AddHandler<TestEntity, TrackingChangeHandler>()
                  .WithTableMapping<TestEntity>("TestEntities")
                  .WithOptions(opts => opts.PollingInterval = TimeSpan.FromMilliseconds(50));
        });
        // Register singleton AFTER AddEncinaCdc to override the scoped registration
        services.AddSingleton<IChangeEventHandler<TestEntity>>(handler);

        using var sp = services.BuildServiceProvider();

        // Act
        await RunProcessorOnce(sp, TimeSpan.FromMilliseconds(500));

        // Assert — Only mapped events processed by handler; unmapped events skipped gracefully
        handler.Invocations.Count.ShouldBe(2);
        handler.Invocations[0].After!.Id.ShouldBe(1);
        handler.Invocations[1].After!.Id.ShouldBe(3);

        // Assert — Position saved for all events (including unmapped)
        var positionStore = sp.GetRequiredService<ICdcPositionStore>();
        var posResult = await positionStore.GetPositionAsync("e2e-mixed");
        posResult.IsRight.ShouldBeTrue();
        var option = posResult.Match(Right: o => o, Left: _ => default);
        option.IsSome.ShouldBeTrue();
        option.IfSome(p =>
        {
            p.ShouldBeOfType<TestCdcPosition>().Value.ShouldBe(3);
        });
    }

    #endregion

    #region Pipeline with Multiple Handlers

    [Fact]
    public async Task FullPipeline_MultipleTableMappings_EachRoutedCorrectly()
    {
        // This tests that the pipeline correctly routes events to different handlers
        // based on table name mapping when using a single entity type

        // Arrange
        var connector = new TestCdcConnector("e2e-multitable");
        connector
            .AddEvent(CdcTestFixtures.CreateInsertEvent(tableName: "TestEntities", id: 1, name: "FromTable1", positionValue: 1))
            .AddEvent(CdcTestFixtures.CreateInsertEvent(tableName: "TestEntities", id: 2, name: "FromTable1Again", positionValue: 2));

        var handler = new TrackingChangeHandler();

        var services = new ServiceCollection();
        services.AddLogging(b => b.AddProvider(NullLoggerProvider.Instance));
        services.AddSingleton<ICdcConnector>(connector);
        services.AddEncinaCdc(config =>
        {
            config.UseCdc()
                  .AddHandler<TestEntity, TrackingChangeHandler>()
                  .WithTableMapping<TestEntity>("TestEntities")
                  .WithOptions(opts => opts.PollingInterval = TimeSpan.FromMilliseconds(50));
        });
        // Register singleton AFTER AddEncinaCdc to override the scoped registration
        services.AddSingleton<IChangeEventHandler<TestEntity>>(handler);

        using var sp = services.BuildServiceProvider();

        // Act
        await RunProcessorOnce(sp, TimeSpan.FromMilliseconds(500));

        // Assert
        handler.Invocations.Count.ShouldBe(2);
        handler.Invocations[0].After!.Name.ShouldBe("FromTable1");
        handler.Invocations[1].After!.Name.ShouldBe("FromTable1Again");

        // Both events carry correct context with table name
        handler.Invocations[0].Context.TableName.ShouldBe("TestEntities");
        handler.Invocations[1].Context.TableName.ShouldBe("TestEntities");
    }

    #endregion

    #region Helpers

    private static async Task RunProcessorOnce(ServiceProvider sp, TimeSpan duration)
    {
        var hostedServices = sp.GetServices<IHostedService>();
        var processor = hostedServices.First(s => s.GetType().Name == "CdcProcessor");

        using var cts = new CancellationTokenSource(duration);
        try
        {
            await processor.StartAsync(cts.Token);
            await Task.Delay(duration, cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Expected
        }
        finally
        {
            await processor.StopAsync(CancellationToken.None);
        }
    }

    #endregion
}
