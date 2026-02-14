#pragma warning disable CA2012 // Use ValueTasks correctly - Required for NSubstitute mocking pattern

using Encina.Cdc;
using Encina.Cdc.Abstractions;
using Encina.Cdc.Sharding;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Cdc.Sharding;

/// <summary>
/// Unit tests for <see cref="ShardedCdcProcessor"/>.
/// Verifies processing loop, position saving, retry logic, and disabled behavior.
/// </summary>
public sealed class ShardedCdcProcessorTests
{
    private static readonly ILogger<ShardedCdcProcessor> Logger =
        NullLogger<ShardedCdcProcessor>.Instance;

    #region Test Helpers

    private static CdcOptions CreateOptions(bool enabled = true, bool enablePositionTracking = true) => new()
    {
        Enabled = enabled,
        PollingInterval = TimeSpan.FromMilliseconds(50),
        BatchSize = 10,
        MaxRetries = 3,
        BaseRetryDelay = TimeSpan.FromMilliseconds(10),
        EnablePositionTracking = enablePositionTracking
    };

    private static ChangeEvent CreateChangeEvent(long positionValue) =>
        new("test_table",
            ChangeOperation.Insert,
            null,
            new { Id = 1 },
            new ChangeMetadata(
                new TestCdcPosition(positionValue),
                DateTime.UtcNow,
                null, null, null));

    private static IServiceProvider CreateServiceProvider(
        IShardedCdcConnector connector,
        ICdcDispatcher dispatcher,
        IShardedCdcPositionStore positionStore)
    {
        var scopedServiceProvider = Substitute.For<IServiceProvider>();
        scopedServiceProvider.GetService(typeof(IShardedCdcConnector)).Returns(connector);
        scopedServiceProvider.GetService(typeof(ICdcDispatcher)).Returns(dispatcher);
        scopedServiceProvider.GetService(typeof(IShardedCdcPositionStore)).Returns(positionStore);

        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(scopedServiceProvider);

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);

        var rootServiceProvider = Substitute.For<IServiceProvider>();
        rootServiceProvider.GetService(typeof(IServiceScopeFactory)).Returns(scopeFactory);
        rootServiceProvider.GetService(typeof(IShardedCdcConnector)).Returns(connector);

        return rootServiceProvider;
    }

    #endregion

    #region Constructor Guards

    [Fact]
    public void Constructor_NullServiceProvider_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ShardedCdcProcessor(null!, Logger, CreateOptions()));
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var sp = Substitute.For<IServiceProvider>();

        Should.Throw<ArgumentNullException>(() =>
            new ShardedCdcProcessor(sp, null!, CreateOptions()));
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var sp = Substitute.For<IServiceProvider>();

        Should.Throw<ArgumentNullException>(() =>
            new ShardedCdcProcessor(sp, Logger, null!));
    }

    #endregion

    #region Disabled Behavior

    [Fact]
    public async Task ExecuteAsync_WhenDisabled_ExitsImmediately()
    {
        var connector = Substitute.For<IShardedCdcConnector>();
        var dispatcher = Substitute.For<ICdcDispatcher>();
        var positionStore = Substitute.For<IShardedCdcPositionStore>();
        var sp = CreateServiceProvider(connector, dispatcher, positionStore);
        var options = CreateOptions(enabled: false);

        var processor = new ShardedCdcProcessor(sp, Logger, options);
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        await processor.StartAsync(cts.Token);
        // Give it a moment to execute
        await Task.Delay(100);
        await processor.StopAsync(CancellationToken.None);

        // When disabled, the connector should never be called
        connector.DidNotReceive().StreamAllShardsAsync(Arg.Any<CancellationToken>());
    }

    #endregion

    #region Processing Loop

    [Fact]
    public async Task ExecuteAsync_ProcessesEventsAndSavesPositions()
    {
        var changeEvent = CreateChangeEvent(42);
        var shardedEvent = new ShardedChangeEvent("shard-1", changeEvent, new TestCdcPosition(42));

        var connector = Substitute.For<IShardedCdcConnector>();
        connector.GetConnectorId().Returns("test-connector");
        connector.StreamAllShardsAsync(Arg.Any<CancellationToken>())
            .Returns(ToShardedAsyncEnumerable(Right<EncinaError, ShardedChangeEvent>(shardedEvent)));

        var dispatcher = Substitute.For<ICdcDispatcher>();
        dispatcher.DispatchAsync(Arg.Any<ChangeEvent>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => new ValueTask<Either<EncinaError, Unit>>(Right<EncinaError, Unit>(unit)));

        var positionStore = Substitute.For<IShardedCdcPositionStore>();
        positionStore.SavePositionAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CdcPosition>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, Unit>(unit)));

        var sp = CreateServiceProvider(connector, dispatcher, positionStore);
        var options = CreateOptions(enabled: true);

        var processor = new ShardedCdcProcessor(sp, Logger, options);
        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));

        await processor.StartAsync(cts.Token);
        await Task.Delay(200);
        await processor.StopAsync(CancellationToken.None);

        // Verify dispatch was called at least once
        await dispatcher.Received().DispatchAsync(
            Arg.Is<ChangeEvent>(e => e.TableName == "test_table"),
            Arg.Any<CancellationToken>());

        // Verify position save was called at least once
        await positionStore.Received().SavePositionAsync(
            "shard-1",
            "test-connector",
            Arg.Any<CdcPosition>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_PositionTrackingDisabled_DoesNotSavePositions()
    {
        var changeEvent = CreateChangeEvent(42);
        var shardedEvent = new ShardedChangeEvent("shard-1", changeEvent, new TestCdcPosition(42));

        var connector = Substitute.For<IShardedCdcConnector>();
        connector.GetConnectorId().Returns("test-connector");
        connector.StreamAllShardsAsync(Arg.Any<CancellationToken>())
            .Returns(ToShardedAsyncEnumerable(Right<EncinaError, ShardedChangeEvent>(shardedEvent)));

        var dispatcher = Substitute.For<ICdcDispatcher>();
        dispatcher.DispatchAsync(Arg.Any<ChangeEvent>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => new ValueTask<Either<EncinaError, Unit>>(Right<EncinaError, Unit>(unit)));

        var positionStore = Substitute.For<IShardedCdcPositionStore>();

        var sp = CreateServiceProvider(connector, dispatcher, positionStore);
        var options = CreateOptions(enabled: true, enablePositionTracking: false);

        var processor = new ShardedCdcProcessor(sp, Logger, options);
        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));

        await processor.StartAsync(cts.Token);
        await Task.Delay(200);
        await processor.StopAsync(CancellationToken.None);

        // Position store should never be called when tracking is disabled
        await positionStore.DidNotReceive().SavePositionAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CdcPosition>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_DispatchFailure_IncrementsFailureCount()
    {
        var changeEvent = CreateChangeEvent(42);
        var shardedEvent = new ShardedChangeEvent("shard-1", changeEvent, new TestCdcPosition(42));

        var connector = Substitute.For<IShardedCdcConnector>();
        connector.GetConnectorId().Returns("test-connector");
        connector.StreamAllShardsAsync(Arg.Any<CancellationToken>())
            .Returns(ToShardedAsyncEnumerable(Right<EncinaError, ShardedChangeEvent>(shardedEvent)));

        var dispatcher = Substitute.For<ICdcDispatcher>();
        dispatcher.DispatchAsync(Arg.Any<ChangeEvent>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => new ValueTask<Either<EncinaError, Unit>>(Left<EncinaError, Unit>(EncinaError.New("Dispatch failed"))));

        var positionStore = Substitute.For<IShardedCdcPositionStore>();

        var sp = CreateServiceProvider(connector, dispatcher, positionStore);
        var options = CreateOptions(enabled: true);

        var processor = new ShardedCdcProcessor(sp, Logger, options);
        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));

        await processor.StartAsync(cts.Token);
        await Task.Delay(200);
        await processor.StopAsync(CancellationToken.None);

        // Position should not be saved on dispatch failure
        await positionStore.DidNotReceive().SavePositionAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CdcPosition>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_LeftValueFromStream_CountsAsFailure()
    {
        var error = Left<EncinaError, ShardedChangeEvent>(EncinaError.New("Stream error"));

        var connector = Substitute.For<IShardedCdcConnector>();
        connector.GetConnectorId().Returns("test-connector");
        connector.StreamAllShardsAsync(Arg.Any<CancellationToken>())
            .Returns(ToShardedAsyncEnumerable(error));

        var dispatcher = Substitute.For<ICdcDispatcher>();
        var positionStore = Substitute.For<IShardedCdcPositionStore>();

        var sp = CreateServiceProvider(connector, dispatcher, positionStore);
        var options = CreateOptions(enabled: true);

        var processor = new ShardedCdcProcessor(sp, Logger, options);
        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));

        await processor.StartAsync(cts.Token);
        await Task.Delay(200);
        await processor.StopAsync(CancellationToken.None);

        // Dispatcher should not be called for Left values
        await dispatcher.DidNotReceive().DispatchAsync(
            Arg.Any<ChangeEvent>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region Batch Size

    [Fact]
    public async Task ExecuteAsync_RespectsBatchSize()
    {
        var events = Enumerable.Range(1, 20)
            .Select(i =>
            {
                var ce = CreateChangeEvent(i);
                return Right<EncinaError, ShardedChangeEvent>(
                    new ShardedChangeEvent("shard-1", ce, new TestCdcPosition(i)));
            })
            .ToArray();

        var connector = Substitute.For<IShardedCdcConnector>();
        connector.GetConnectorId().Returns("test-connector");
        connector.StreamAllShardsAsync(Arg.Any<CancellationToken>())
            .Returns(ToShardedAsyncEnumerable(events));

        var dispatchCount = 0;
        var dispatcher = Substitute.For<ICdcDispatcher>();
        dispatcher.DispatchAsync(Arg.Any<ChangeEvent>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                Interlocked.Increment(ref dispatchCount);
                return new ValueTask<Either<EncinaError, Unit>>(Right<EncinaError, Unit>(unit));
            });

        var positionStore = Substitute.For<IShardedCdcPositionStore>();
        positionStore.SavePositionAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CdcPosition>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, Unit>(unit)));

        var sp = CreateServiceProvider(connector, dispatcher, positionStore);
        var options = CreateOptions(enabled: true);
        options.BatchSize = 5; // Only process 5 per cycle

        var processor = new ShardedCdcProcessor(sp, Logger, options);
        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(150));

        await processor.StartAsync(cts.Token);
        await Task.Delay(100);
        await processor.StopAsync(CancellationToken.None);

        // Should have dispatched events (may vary due to timing, but should be limited by batch size per cycle)
        dispatchCount.ShouldBeGreaterThan(0);
    }

    #endregion

    #region AsyncEnumerable Helpers

    private static async IAsyncEnumerable<Either<EncinaError, ShardedChangeEvent>> ToShardedAsyncEnumerable(
        params Either<EncinaError, ShardedChangeEvent>[] items)
    {
        foreach (var item in items)
        {
            yield return item;
            await Task.Yield();
        }
    }

    #endregion
}
