using Encina.Cdc;
using Encina.Cdc.Abstractions;
using Encina.Cdc.DeadLetter;
using Encina.Cdc.Processing;
using Encina.Testing.Fakes.Stores;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Cdc;

/// <summary>
/// Unit tests for <see cref="CdcProcessor"/> dead letter queue integration.
/// Verifies that failed events are persisted to the dead letter store after
/// retry exhaustion and that the processor behaves correctly when the DLQ
/// is disabled or when the DLQ store itself fails.
/// </summary>
public sealed class CdcProcessorDeadLetterTests
{
    #region Test Helpers

    private static readonly DateTime FixedUtcNow = new(2026, 2, 15, 12, 0, 0, DateTimeKind.Utc);

    private static ChangeEvent CreateTestEvent(string tableName = "Orders", long positionValue = 1)
    {
        return new ChangeEvent(
            TableName: tableName,
            Operation: ChangeOperation.Insert,
            Before: null,
            After: new { Id = 1 },
            Metadata: new ChangeMetadata(
                Position: new TestCdcPosition(positionValue),
                CapturedAtUtc: FixedUtcNow,
                TransactionId: null,
                SourceDatabase: null,
                SourceSchema: null));
    }

    private static ICdcConnector CreateFailingConnector(string connectorId = "test-connector")
    {
        var connector = Substitute.For<ICdcConnector>();
        connector.ConnectorId.Returns(connectorId);

        // StreamChangesAsync yields a single event that causes dispatcher to fail
        var changeEvent = CreateTestEvent();
        var items = new List<Either<EncinaError, ChangeEvent>>
        {
            Right(changeEvent)
        };
        connector.StreamChangesAsync(Arg.Any<CancellationToken>())
            .Returns(items.ToAsyncEnumerable());

        return connector;
    }

    private static ICdcDispatcher CreateFailingDispatcher()
    {
        var dispatcher = Substitute.For<ICdcDispatcher>();
#pragma warning disable CA2012 // Use ValueTasks correctly - NSubstitute internally manages the ValueTask
        dispatcher.DispatchAsync(Arg.Any<ChangeEvent>(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, Unit>>(
                Left<EncinaError, Unit>(EncinaError.New("Dispatch failed"))));
#pragma warning restore CA2012
        return dispatcher;
    }

    private static ICdcDispatcher CreateSuccessfulDispatcher()
    {
        var dispatcher = Substitute.For<ICdcDispatcher>();
#pragma warning disable CA2012 // Use ValueTasks correctly - NSubstitute internally manages the ValueTask
        dispatcher.DispatchAsync(Arg.Any<ChangeEvent>(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, Unit>>(
                Right<EncinaError, Unit>(unit)));
#pragma warning restore CA2012
        return dispatcher;
    }

    private static ICdcPositionStore CreatePositionStore()
    {
        var store = Substitute.For<ICdcPositionStore>();
        store.SavePositionAsync(Arg.Any<string>(), Arg.Any<CdcPosition>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, Unit>(unit)));
        return store;
    }

    private static (CdcProcessor Processor, FakeCdcDeadLetterStore DlqStore) CreateProcessorWithDlq(
        ICdcConnector? connector = null,
        ICdcDispatcher? dispatcher = null,
        CdcOptions? options = null)
    {
        connector ??= CreateFailingConnector();
        dispatcher ??= CreateFailingDispatcher();
        options ??= new CdcOptions
        {
            Enabled = true,
            MaxRetries = 2,
            BaseRetryDelay = TimeSpan.FromMilliseconds(1),
            PollingInterval = TimeSpan.FromMilliseconds(1),
            BatchSize = 10
        };

        var dlqStore = new FakeCdcDeadLetterStore();

        var services = new ServiceCollection();
        services.AddSingleton(connector);
        services.AddSingleton(dispatcher);
        services.AddSingleton(CreatePositionStore());
        var serviceProvider = services.BuildServiceProvider();

        var logger = NullLogger<CdcProcessor>.Instance;
        var processor = new CdcProcessor(serviceProvider, logger, options, dlqStore);

        return (processor, dlqStore);
    }

    private static CdcProcessor CreateProcessorWithoutDlq(
        ICdcConnector? connector = null,
        ICdcDispatcher? dispatcher = null,
        CdcOptions? options = null)
    {
        connector ??= CreateFailingConnector();
        dispatcher ??= CreateFailingDispatcher();
        options ??= new CdcOptions
        {
            Enabled = true,
            MaxRetries = 2,
            BaseRetryDelay = TimeSpan.FromMilliseconds(1),
            PollingInterval = TimeSpan.FromMilliseconds(1),
            BatchSize = 10
        };

        var services = new ServiceCollection();
        services.AddSingleton(connector);
        services.AddSingleton(dispatcher);
        services.AddSingleton(CreatePositionStore());
        var serviceProvider = services.BuildServiceProvider();

        var logger = NullLogger<CdcProcessor>.Instance;
        return new CdcProcessor(serviceProvider, logger, options, deadLetterStore: null);
    }

    #endregion

    #region Constructor

    [Fact]
    public void Constructor_NullDeadLetterStore_DoesNotThrow()
    {
        // Arrange & Act & Assert
        Should.NotThrow(() =>
        {
            var services = new ServiceCollection();
            var sp = services.BuildServiceProvider();
            var logger = NullLogger<CdcProcessor>.Instance;
            _ = new CdcProcessor(sp, logger, new CdcOptions(), deadLetterStore: null);
        });
    }

    [Fact]
    public void Constructor_WithDeadLetterStore_DoesNotThrow()
    {
        // Arrange & Act & Assert
        Should.NotThrow(() =>
        {
            var services = new ServiceCollection();
            var sp = services.BuildServiceProvider();
            var logger = NullLogger<CdcProcessor>.Instance;
            _ = new CdcProcessor(sp, logger, new CdcOptions(), new FakeCdcDeadLetterStore());
        });
    }

    #endregion

    #region DLQ Persistence After Retry Exhaustion

    [Fact]
    public async Task ExecuteAsync_RetriesExhausted_PersistsToDeadLetterStore()
    {
        // Arrange
        var connector = Substitute.For<ICdcConnector>();
        connector.ConnectorId.Returns("test-connector");
        // Connector that throws on StreamChangesAsync to trigger outer retry loop
        connector.StreamChangesAsync(Arg.Any<CancellationToken>())
            .Returns<IAsyncEnumerable<Either<EncinaError, ChangeEvent>>>(_ =>
                throw new InvalidOperationException("Stream failed"));

        var options = new CdcOptions
        {
            Enabled = true,
            MaxRetries = 2,
            BaseRetryDelay = TimeSpan.FromMilliseconds(1),
            PollingInterval = TimeSpan.FromMilliseconds(1),
            BatchSize = 10
        };

        var dlqStore = new FakeCdcDeadLetterStore();

        var services = new ServiceCollection();
        services.AddSingleton(connector);
        services.AddSingleton(CreateFailingDispatcher());
        services.AddSingleton(CreatePositionStore());
        var serviceProvider = services.BuildServiceProvider();

        var logger = NullLogger<CdcProcessor>.Instance;
        var processor = new CdcProcessor(serviceProvider, logger, options, dlqStore);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // Act - let the processor run until retries are exhausted
        // The processor will: fail once → retry (1) → fail → retry (2) → fail → exceed maxRetries → persist to DLQ → then we cancel
        try
        {
            await processor.StartAsync(cts.Token);
            // Give time for the retry loop to exhaust and persist
            await Task.Delay(TimeSpan.FromSeconds(2), cts.Token);
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
        var entries = dlqStore.GetEntries();
        entries.Count.ShouldBeGreaterThan(0,
            "At least one entry should be persisted to DLQ after retries are exhausted");

        var entry = entries[0];
        entry.ConnectorId.ShouldBe("test-connector");
        entry.Status.ShouldBe(CdcDeadLetterStatus.Pending);
        entry.RetryCount.ShouldBe(2, "RetryCount should match MaxRetries");
        entry.ErrorMessage.ShouldNotBeNullOrEmpty();
    }

    #endregion

    #region DLQ Not Called When Feature Disabled

    [Fact]
    public async Task ExecuteAsync_NoDlqStore_DoesNotThrow()
    {
        // Arrange
        var connector = Substitute.For<ICdcConnector>();
        connector.ConnectorId.Returns("test-connector");
        connector.StreamChangesAsync(Arg.Any<CancellationToken>())
            .Returns<IAsyncEnumerable<Either<EncinaError, ChangeEvent>>>(_ =>
                throw new InvalidOperationException("Stream failed"));

        var processor = CreateProcessorWithoutDlq(connector: connector);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));

        // Act - should not throw even when retries are exhausted and no DLQ store
        try
        {
            await processor.StartAsync(cts.Token);
            await Task.Delay(TimeSpan.FromSeconds(1), cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Expected
        }
        finally
        {
            await processor.StopAsync(CancellationToken.None);
        }

        // Assert - no exception means the test passes
    }

    #endregion

    #region DLQ Store Failure Resilience

    [Fact]
    public async Task ExecuteAsync_DlqStoreFails_ContinuesProcessing()
    {
        // Arrange - use a DLQ store that always fails
        var failingDlqStore = Substitute.For<ICdcDeadLetterStore>();
        failingDlqStore.AddAsync(Arg.Any<CdcDeadLetterEntry>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Left<EncinaError, Unit>(
                EncinaError.New("DLQ store failed"))));

        var connector = Substitute.For<ICdcConnector>();
        connector.ConnectorId.Returns("test-connector");
        connector.StreamChangesAsync(Arg.Any<CancellationToken>())
            .Returns<IAsyncEnumerable<Either<EncinaError, ChangeEvent>>>(_ =>
                throw new InvalidOperationException("Stream failed"));

        var options = new CdcOptions
        {
            Enabled = true,
            MaxRetries = 1,
            BaseRetryDelay = TimeSpan.FromMilliseconds(1),
            PollingInterval = TimeSpan.FromMilliseconds(1),
            BatchSize = 10
        };

        var services = new ServiceCollection();
        services.AddSingleton(connector);
        services.AddSingleton(CreateFailingDispatcher());
        services.AddSingleton(CreatePositionStore());
        var serviceProvider = services.BuildServiceProvider();

        var logger = NullLogger<CdcProcessor>.Instance;
        var processor = new CdcProcessor(serviceProvider, logger, options, failingDlqStore);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));

        // Act - processor should not crash even when DLQ store fails
        try
        {
            await processor.StartAsync(cts.Token);
            await Task.Delay(TimeSpan.FromSeconds(1), cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Expected
        }
        finally
        {
            await processor.StopAsync(CancellationToken.None);
        }

        // Assert - the processor continued running (didn't crash)
        // No exception means the processor was resilient to DLQ store failures
    }

    #endregion

    #region Processor Disabled

    [Fact]
    public async Task ExecuteAsync_ProcessorDisabled_DoesNotUseDlq()
    {
        // Arrange
        var dlqStore = new FakeCdcDeadLetterStore();
        var options = new CdcOptions
        {
            Enabled = false,
            MaxRetries = 2
        };

        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var logger = NullLogger<CdcProcessor>.Instance;
        var processor = new CdcProcessor(serviceProvider, logger, options, dlqStore);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));

        // Act
        await processor.StartAsync(cts.Token);
        await Task.Delay(TimeSpan.FromMilliseconds(200), cts.Token);
        await processor.StopAsync(CancellationToken.None);

        // Assert
        dlqStore.GetEntries().Count.ShouldBe(0,
            "DLQ store should not be used when processor is disabled");
    }

    #endregion
}
