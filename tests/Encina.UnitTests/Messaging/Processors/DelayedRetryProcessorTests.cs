using Encina.Messaging.Recoverability;
using Encina.Messaging.Serialization;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using Shouldly;

using static LanguageExt.Prelude;

#pragma warning disable CA2012 // NSubstitute setup of ValueTask-returning members

namespace Encina.UnitTests.Messaging.Processors;

/// <summary>
/// Unit tests for <see cref="DelayedRetryProcessor"/>.
/// </summary>
public sealed class DelayedRetryProcessorTests
{
    #region Constructor

    [Fact]
    public void Constructor_WithNullScopeFactory_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new RecoverabilityOptions();
        var logger = NullLogger<DelayedRetryProcessor>.Instance;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new DelayedRetryProcessor(null!, options, logger));
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        var logger = NullLogger<DelayedRetryProcessor>.Instance;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new DelayedRetryProcessor(scopeFactory, null!, logger));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        var options = new RecoverabilityOptions();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new DelayedRetryProcessor(scopeFactory, options, null!));
    }

    [Fact]
    public void Constructor_WithValidParameters_Succeeds()
    {
        // Arrange
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        var options = new RecoverabilityOptions();
        var logger = NullLogger<DelayedRetryProcessor>.Instance;

        // Act
        var processor = new DelayedRetryProcessor(scopeFactory, options, logger);

        // Assert
        processor.ShouldNotBeNull();
    }

    #endregion

    #region ExecuteAsync - Store Not Configured

    [Fact]
    public async Task ExecuteAsync_WhenStoreNotRegistered_FailsTheCycleWithAnErrorLogAndKeepsRunning()
    {
        // Arrange - the startup check guarantees the store when delayed retries are on, so a missing store
        // is an error of the cycle (logged, EventId 2847), never a silent "healthy" cycle.
        var services = new ServiceCollection();
        using var provider = services.BuildServiceProvider();
        var logger = new Microsoft.Extensions.Logging.Testing.FakeLogger<DelayedRetryProcessor>();
        var processor = new DelayedRetryProcessor(provider.GetRequiredService<IServiceScopeFactory>(), new RecoverabilityOptions(), logger)
        {
            ProcessingInterval = TimeSpan.FromMilliseconds(10)
        };

        using var cts = new CancellationTokenSource();

        // Act - wait for two failed cycles to prove the loop survives the failure.
        await processor.StartAsync(cts.Token);
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            while (logger.Collector.GetSnapshot().Count(r => r.Id.Id == 2847) < 2)
            {
                await Task.Delay(5, timeout.Token);
            }
        }
        finally
        {
            await cts.CancelAsync();
            await processor.StopAsync(default);
        }

        // Assert
        var failures = logger.Collector.GetSnapshot().Where(r => r.Id.Id == 2847).ToList();
        failures.Count.ShouldBeGreaterThanOrEqualTo(2);
        failures.ShouldAllBe(r => r.Level == Microsoft.Extensions.Logging.LogLevel.Error);
        logger.Collector.GetSnapshot().ShouldNotContain(r => r.Id.Id == 2848);
    }

    [Fact]
    public async Task ExecuteAsync_WhenEncinaNotRegistered_FailsTheCycleWithAnErrorLogAndKeepsRunning()
    {
        // Arrange - a store but no IEncina (no host, so the startup check never ran): the cycle fails
        // (logged, EventId 2847) instead of skipping silently with a warning.
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<IDelayedRetryStore>());
        services.AddSingleton<IMessageSerializer>(new JsonMessageSerializer());
        using var provider = services.BuildServiceProvider();
        var logger = new Microsoft.Extensions.Logging.Testing.FakeLogger<DelayedRetryProcessor>();
        var processor = new DelayedRetryProcessor(provider.GetRequiredService<IServiceScopeFactory>(), new RecoverabilityOptions(), logger)
        {
            ProcessingInterval = TimeSpan.FromMilliseconds(10)
        };

        using var cts = new CancellationTokenSource();

        // Act - wait for two failed cycles to prove the loop survives the failure.
        await processor.StartAsync(cts.Token);
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            while (logger.Collector.GetSnapshot().Count(r => r.Id.Id == 2847) < 2)
            {
                await Task.Delay(5, timeout.Token);
            }
        }
        finally
        {
            await cts.CancelAsync();
            await processor.StopAsync(default);
        }

        // Assert
        var failures = logger.Collector.GetSnapshot().Where(r => r.Id.Id == 2847).ToList();
        failures.Count.ShouldBeGreaterThanOrEqualTo(2);
        failures.ShouldAllBe(r => r.Level == Microsoft.Extensions.Logging.LogLevel.Error);
        failures.ShouldAllBe(r => r.Exception != null);
    }

    #endregion

    #region ExecuteAsync - Cancellation

    [Fact]
    public async Task ExecuteAsync_WhenCancelled_ExitsGracefully()
    {
        // Arrange
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        var options = new RecoverabilityOptions();
        var logger = NullLogger<DelayedRetryProcessor>.Instance;

        var processor = new DelayedRetryProcessor(scopeFactory, options, logger)
        {
            ProcessingInterval = TimeSpan.FromSeconds(1)
        };

        using var cts = new CancellationTokenSource();

        // Act
        await processor.StartAsync(cts.Token);
        cts.Cancel();

        // Assert - should complete without throwing
        await processor.StopAsync(default);
        Assert.True(true, "Processor exited gracefully when cancelled");
    }

    #endregion

    #region ExecuteAsync - Processing Messages

    [Fact]
    public async Task ExecuteAsync_WhenMessagesExist_ProcessesThem()
    {
        // Arrange
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var message = CreateMockMessage();
        var store = Substitute.For<IDelayedRetryStore>();
        store.GetPendingMessagesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                tcs.TrySetResult();
                return Task.FromResult<IEnumerable<IDelayedRetryMessage>>([message]);
            });

        var encina = Substitute.For<IEncina>();

        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IDelayedRetryStore)).Returns(store);
        serviceProvider.GetService(typeof(IEncina)).Returns(encina);
        serviceProvider.GetService(typeof(IMessageSerializer)).Returns(new JsonMessageSerializer());
        serviceProvider.GetService(typeof(IDelayedRetryScheduler)).Returns(Substitute.For<IDelayedRetryScheduler>());

        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(serviceProvider);

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);

        var options = new RecoverabilityOptions();
        var logger = NullLogger<DelayedRetryProcessor>.Instance;

        var processor = new DelayedRetryProcessor(scopeFactory, options, logger)
        {
            ProcessingInterval = TimeSpan.FromMilliseconds(10)
        };

        using var cts = new CancellationTokenSource();

        // Act
        await processor.StartAsync(cts.Token);
        try
        {
            await tcs.Task.WaitAsync(TimeSpan.FromSeconds(10));
        }
        finally
        {
            await cts.CancelAsync();
            await processor.StopAsync(default);
        }

        // Assert - verify store was called to get pending messages
        await store.Received().GetPendingMessagesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenNoMessages_ContinuesPolling()
    {
        // Arrange
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var store = Substitute.For<IDelayedRetryStore>();
        store.GetPendingMessagesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                tcs.TrySetResult();
                return Task.FromResult<IEnumerable<IDelayedRetryMessage>>(System.Array.Empty<IDelayedRetryMessage>());
            });

        var encina = Substitute.For<IEncina>();

        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IDelayedRetryStore)).Returns(store);
        serviceProvider.GetService(typeof(IEncina)).Returns(encina);
        serviceProvider.GetService(typeof(IMessageSerializer)).Returns(new JsonMessageSerializer());
        serviceProvider.GetService(typeof(IDelayedRetryScheduler)).Returns(Substitute.For<IDelayedRetryScheduler>());

        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(serviceProvider);

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);

        var options = new RecoverabilityOptions();
        var logger = NullLogger<DelayedRetryProcessor>.Instance;

        var processor = new DelayedRetryProcessor(scopeFactory, options, logger)
        {
            ProcessingInterval = TimeSpan.FromMilliseconds(10)
        };

        using var cts = new CancellationTokenSource();

        // Act
        await processor.StartAsync(cts.Token);
        try
        {
            await tcs.Task.WaitAsync(TimeSpan.FromSeconds(10));
        }
        finally
        {
            await cts.CancelAsync();
            await processor.StopAsync(default);
        }

        // Assert - should have polled at least once
        await store.Received().GetPendingMessagesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region ExecuteAsync - Dispatch Outcome (#1147 review)

    /// <summary>A request the processor can resolve by name and deserialize.</summary>
    public sealed record RetriedCommand(int Value) : IRequest<int>;

    [Fact]
    public async Task ExecuteAsync_WhenTheRetriedRequestSucceeds_DispatchesIt_AndMarksTheRetryProcessed()
    {
        // Arrange
        var message = CreateRetriedCommandMessage(value: 42);
        var store = Substitute.For<IDelayedRetryStore>();
        store.GetPendingMessagesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(new[] { message }, System.Array.Empty<IDelayedRetryMessage>());
        var processed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        store.MarkAsProcessedAsync(message.Id, Arg.Any<CancellationToken>())
            .Returns(_ => { processed.TrySetResult(); return Task.CompletedTask; });

        var encina = Substitute.For<IEncina>();
        encina.Send(Arg.Any<IRequest<int>>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, int>>(Right<EncinaError, int>(7)));

        var processor = CreateProcessor(store, encina, new RecoverabilityOptions());
        using var cts = new CancellationTokenSource();

        // Act
        await processor.StartAsync(cts.Token);
        try
        {
            await processed.Task.WaitAsync(TimeSpan.FromSeconds(10));
        }
        finally
        {
            await cts.CancelAsync();
            await processor.StopAsync(default);
        }

        // Assert
        await encina.Received(1).Send(Arg.Is<IRequest<int>>(r => r is RetriedCommand && ((RetriedCommand)r).Value == 42), Arg.Any<CancellationToken>());
        await store.DidNotReceive().MarkAsFailedAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenTheRetriedRequestReturnsLeft_TreatsItAsAFailure_WithTheErrorCode()
    {
        // Arrange - no further delayed retries configured, so a failure is permanent.
        // Only the error code is stored: EncinaError.Message can carry personal data (#1259 review).
        var message = CreateRetriedCommandMessage(value: 1);
        var store = Substitute.For<IDelayedRetryStore>();
        store.GetPendingMessagesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(new[] { message }, System.Array.Empty<IDelayedRetryMessage>());
        var failed = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        store.MarkAsFailedAsync(message.Id, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => { failed.TrySetResult(call.ArgAt<string>(1)); return Task.CompletedTask; });

        var encina = Substitute.For<IEncina>();
        encina.Send(Arg.Any<IRequest<int>>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, int>>(Left<EncinaError, int>(
                EncinaErrors.Create("payment.gateway.down", "payment gateway down for customer 12345"))));

        FailedMessage? permanentFailure = null;
        var options = new RecoverabilityOptions
        {
            DelayedRetries = [],
            OnPermanentFailure = (failedMessage, _) => { permanentFailure = failedMessage; return Task.CompletedTask; }
        };
        var processor = CreateProcessor(store, encina, options);
        using var cts = new CancellationTokenSource();

        // Act
        await processor.StartAsync(cts.Token);
        string errorMessage;
        try
        {
            errorMessage = await failed.Task.WaitAsync(TimeSpan.FromSeconds(10));
        }
        finally
        {
            await cts.CancelAsync();
            await processor.StopAsync(default);
        }

        // Assert
        errorMessage.ShouldBe("payment.gateway.down");
        await store.DidNotReceive().MarkAsProcessedAsync(message.Id, Arg.Any<CancellationToken>());
        permanentFailure.ShouldNotBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_WhenProcessingThrows_StoresOnlyTheExceptionType()
    {
        // Arrange - the dispatch succeeds, then the store throws; the message must never be persisted.
        var message = CreateRetriedCommandMessage(value: 5);
        var store = Substitute.For<IDelayedRetryStore>();
        store.GetPendingMessagesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(new[] { message }, System.Array.Empty<IDelayedRetryMessage>());
        store.MarkAsProcessedAsync(message.Id, Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("patient 12345 not found"));
        var failed = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        store.MarkAsFailedAsync(message.Id, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => { failed.TrySetResult(call.ArgAt<string>(1)); return Task.CompletedTask; });

        var encina = Substitute.For<IEncina>();
        encina.Send(Arg.Any<IRequest<int>>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, int>>(Right<EncinaError, int>(7)));

        var processor = CreateProcessor(store, encina, new RecoverabilityOptions());
        using var cts = new CancellationTokenSource();

        // Act
        await processor.StartAsync(cts.Token);
        string stored;
        try
        {
            stored = await failed.Task.WaitAsync(TimeSpan.FromSeconds(10));
        }
        finally
        {
            await cts.CancelAsync();
            await processor.StopAsync(default);
        }

        // Assert
        stored.ShouldBe(typeof(InvalidOperationException).FullName);
        stored.ShouldNotContain("12345");
    }

    private static DelayedRetryProcessor CreateProcessor(IDelayedRetryStore store, IEncina encina, RecoverabilityOptions options)
    {
        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IDelayedRetryStore)).Returns(store);
        serviceProvider.GetService(typeof(IEncina)).Returns(encina);
        serviceProvider.GetService(typeof(IMessageSerializer)).Returns(new JsonMessageSerializer());
        serviceProvider.GetService(typeof(IDelayedRetryScheduler)).Returns(Substitute.For<IDelayedRetryScheduler>());

        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(serviceProvider);

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);

        return new DelayedRetryProcessor(scopeFactory, options, NullLogger<DelayedRetryProcessor>.Instance)
        {
            ProcessingInterval = TimeSpan.FromMilliseconds(10)
        };
    }

    private static IDelayedRetryMessage CreateRetriedCommandMessage(int value)
    {
        var message = CreateMockMessage();
        message.RequestType.Returns(typeof(RetriedCommand).AssemblyQualifiedName!);
        message.RequestContent.Returns($"{{\"value\":{value}}}");
        return message;
    }

    #endregion

    #region Properties

    [Fact]
    public void ProcessingInterval_CanBeSet()
    {
        // Arrange
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        var options = new RecoverabilityOptions();
        var logger = NullLogger<DelayedRetryProcessor>.Instance;
        var processor = new DelayedRetryProcessor(scopeFactory, options, logger);

        // Act
        processor.ProcessingInterval = TimeSpan.FromMinutes(5);

        // Assert
        processor.ProcessingInterval.ShouldBe(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void BatchSize_CanBeSet()
    {
        // Arrange
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        var options = new RecoverabilityOptions();
        var logger = NullLogger<DelayedRetryProcessor>.Instance;
        var processor = new DelayedRetryProcessor(scopeFactory, options, logger);

        // Act
        processor.BatchSize = 50;

        // Assert
        processor.BatchSize.ShouldBe(50);
    }

    #endregion

    #region Helper Methods

    private static IDelayedRetryMessage CreateMockMessage()
    {
        var message = Substitute.For<IDelayedRetryMessage>();
        message.Id.Returns(Guid.NewGuid());
        message.RequestType.Returns("TestNamespace.TestRequest, TestAssembly");
        message.RequestContent.Returns("{\"value\": 42}");
        message.ContextContent.Returns("{\"id\":\"00000000-0000-0000-0000-000000000000\",\"immediateRetryCount\":0,\"delayedRetryCount\":0}");
        message.CorrelationId.Returns("test-correlation");
        message.DelayedRetryAttempt.Returns(0);
        var scheduledAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        message.ScheduledAtUtc.Returns(scheduledAtUtc);
        message.ExecuteAtUtc.Returns(scheduledAtUtc);
        return message;
    }

    #endregion
}
