using System.Diagnostics.CodeAnalysis;
using Encina.Cdc;
using Encina.Cdc.Abstractions;
using Encina.Cdc.Processing;
using Encina.IntegrationTests.Cdc.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace Encina.IntegrationTests.Cdc;

/// <summary>
/// Integration tests for <see cref="CdcProcessor"/> verifying the full
/// BackgroundService lifecycle: connector → dispatcher → handler → position store.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Feature", "CDC")]
[SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "Integration test assertions on ValueTask results")]
public sealed class CdcProcessorIntegrationTests
{
    #region Test Helpers

    private sealed record ProcessorFixture(
        ServiceProvider ServiceProvider,
        TestCdcConnector Connector,
        TrackingChangeHandler Handler,
        ICdcPositionStore PositionStore) : IDisposable
    {
        public void Dispose() => ServiceProvider.Dispose();
    }

    private static ProcessorFixture CreateProcessorFixture(Action<CdcOptions>? configureOptions = null)
    {
        var connector = new TestCdcConnector("integration-test");
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
                      opts.BatchSize = 100;
                      configureOptions?.Invoke(opts);
                  });
        });

        // Register singleton AFTER AddEncinaCdc to override the scoped registration,
        // so the processor resolves the same tracking handler instance from scopes.
        services.AddSingleton<IChangeEventHandler<TestEntity>>(handler);

        var sp = services.BuildServiceProvider();
        var positionStore = sp.GetRequiredService<ICdcPositionStore>();

        return new ProcessorFixture(sp, connector, handler, positionStore);
    }

    private static async Task RunProcessorForDuration(
        ServiceProvider sp,
        TimeSpan duration)
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

    #region Basic Processing

    [Fact]
    public async Task Processor_WithEvents_DispatchesAllEvents()
    {
        // Arrange
        using var fixture = CreateProcessorFixture();
        fixture.Connector
            .AddEvent(CdcTestFixtures.CreateInsertEvent(id: 1, positionValue: 1))
            .AddEvent(CdcTestFixtures.CreateInsertEvent(id: 2, positionValue: 2))
            .AddEvent(CdcTestFixtures.CreateInsertEvent(id: 3, positionValue: 3));

        // Act
        await RunProcessorForDuration(fixture.ServiceProvider, TimeSpan.FromMilliseconds(500));

        // Assert
        fixture.Handler.Invocations.Count.ShouldBe(3);
        fixture.Handler.Invocations.ShouldAllBe(i => i.Operation == "Insert");
    }

    [Fact]
    public async Task Processor_WithEvents_SavesPositionAfterEachEvent()
    {
        // Arrange
        using var fixture = CreateProcessorFixture();
        fixture.Connector
            .AddEvent(CdcTestFixtures.CreateInsertEvent(id: 1, positionValue: 10))
            .AddEvent(CdcTestFixtures.CreateInsertEvent(id: 2, positionValue: 20));

        // Act
        await RunProcessorForDuration(fixture.ServiceProvider, TimeSpan.FromMilliseconds(500));

        // Assert
        var positionResult = await fixture.PositionStore.GetPositionAsync("integration-test");
        positionResult.IsRight.ShouldBeTrue();
        var option = positionResult.Match(Right: o => o, Left: _ => default);
        option.IsSome.ShouldBeTrue();
        option.IfSome(p =>
        {
            var testPosition = p.ShouldBeOfType<TestCdcPosition>();
            testPosition.Value.ShouldBe(20);
        });
    }

    #endregion

    #region Position Tracking

    [Fact]
    public async Task Processor_PositionTrackingDisabled_DoesNotSavePosition()
    {
        // Arrange
        using var fixture = CreateProcessorFixture(opts =>
        {
            opts.EnablePositionTracking = false;
        });
        fixture.Connector.AddEvent(CdcTestFixtures.CreateInsertEvent(id: 1, positionValue: 100));

        // Act
        await RunProcessorForDuration(fixture.ServiceProvider, TimeSpan.FromMilliseconds(500));

        // Assert
        fixture.Handler.Invocations.ShouldHaveSingleItem();
        var positionResult = await fixture.PositionStore.GetPositionAsync("integration-test");
        positionResult.IsRight.ShouldBeTrue();
        var option = positionResult.Match(Right: o => o, Left: _ => default);
        option.IsNone.ShouldBeTrue();
    }

    #endregion

    #region Mixed Operations

    [Fact]
    public async Task Processor_MixedOperations_DispatchesAllTypes()
    {
        // Arrange
        using var fixture = CreateProcessorFixture();
        fixture.Connector
            .AddEvent(CdcTestFixtures.CreateInsertEvent(id: 1, positionValue: 1))
            .AddEvent(CdcTestFixtures.CreateUpdateEvent(id: 1, oldName: "A", newName: "B", positionValue: 2))
            .AddEvent(CdcTestFixtures.CreateDeleteEvent(id: 1, positionValue: 3));

        // Act
        await RunProcessorForDuration(fixture.ServiceProvider, TimeSpan.FromMilliseconds(500));

        // Assert
        fixture.Handler.Invocations.Count.ShouldBe(3);
        fixture.Handler.Invocations[0].Operation.ShouldBe("Insert");
        fixture.Handler.Invocations[1].Operation.ShouldBe("Update");
        fixture.Handler.Invocations[2].Operation.ShouldBe("Delete");
    }

    #endregion

    #region Empty Stream

    [Fact]
    public async Task Processor_NoEvents_HandlerNotInvoked()
    {
        // Arrange
        using var fixture = CreateProcessorFixture();
        // No events added to connector

        // Act
        await RunProcessorForDuration(fixture.ServiceProvider, TimeSpan.FromMilliseconds(300));

        // Assert
        fixture.Handler.Invocations.ShouldBeEmpty();
    }

    #endregion

    #region Batch Size

    [Fact]
    public async Task Processor_ExceedsBatchSize_ProcessesOnlyBatchSizeEvents()
    {
        // Arrange
        using var fixture = CreateProcessorFixture(opts =>
        {
            opts.BatchSize = 3;
        });
        for (var i = 1; i <= 10; i++)
        {
            fixture.Connector.AddEvent(
                CdcTestFixtures.CreateInsertEvent(id: i, positionValue: i));
        }

        // Act — Run briefly (one poll cycle)
        await RunProcessorForDuration(fixture.ServiceProvider, TimeSpan.FromMilliseconds(200));

        // Assert — First batch should have exactly 3 events
        fixture.Handler.Invocations.Count.ShouldBe(3);
    }

    #endregion

    #region Disabled Processor

    [Fact]
    public async Task Processor_WhenDisabled_DoesNotProcessEvents()
    {
        // Arrange
        var connector = new TestCdcConnector();
        connector.AddEvent(CdcTestFixtures.CreateInsertEvent());
        var handler = new TrackingChangeHandler();

        var services = new ServiceCollection();
        services.AddLogging(b => b.AddProvider(NullLoggerProvider.Instance));
        services.AddSingleton<ICdcConnector>(connector);
        services.AddSingleton<IChangeEventHandler<TestEntity>>(handler);
        services.AddEncinaCdc(config =>
        {
            // Do NOT call UseCdc() — processor stays disabled
            config.AddHandler<TestEntity, TrackingChangeHandler>()
                  .WithTableMapping<TestEntity>("TestEntities");
        });

        using var sp = services.BuildServiceProvider();
        var hostedServices = sp.GetServices<IHostedService>().ToList();

        // Assert — No processor registered when Enabled = false
        hostedServices.ShouldNotContain(s => s.GetType().Name == "CdcProcessor");
    }

    #endregion
}
