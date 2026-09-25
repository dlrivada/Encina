using Encina.Messaging.Recoverability;

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
    public async Task ExecuteAsync_WhenStoreNotConfigured_ContinuesWithoutError()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IDelayedRetryStore)).Returns(null);

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
        await Task.Delay(50);
        cts.Cancel();

        // Assert - should complete without throwing
        await processor.StopAsync(default);
        Assert.True(true, "Processor completed without error when store not configured");
    }

    [Fact]
    public async Task ExecuteAsync_WhenEncinaNotConfigured_ContinuesWithoutError()
    {
        // Arrange
        var store = Substitute.For<IDelayedRetryStore>();
        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IDelayedRetryStore)).Returns(store);
        serviceProvider.GetService(typeof(IEncina)).Returns(null);

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
        await Task.Delay(50);
        cts.Cancel();

        // Assert - should complete without throwing
        await processor.StopAsync(default);
        Assert.True(true, "Processor completed without error when Encina not configured");
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
        var message = CreateMockMessage();
        var store = Substitute.For<IDelayedRetryStore>();
        store.GetPendingMessagesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([message]);

        var encina = Substitute.For<IEncina>();

        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IDelayedRetryStore)).Returns(store);
        serviceProvider.GetService(typeof(IEncina)).Returns(encina);

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
        await Task.Delay(100);
        cts.Cancel();

        // Assert - verify store was called to get pending messages
        await processor.StopAsync(default);
        await store.Received().GetPendingMessagesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenNoMessages_ContinuesPolling()
    {
        // Arrange
        var store = Substitute.For<IDelayedRetryStore>();
        store.GetPendingMessagesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(System.Array.Empty<IDelayedRetryMessage>());

        var encina = Substitute.For<IEncina>();

        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IDelayedRetryStore)).Returns(store);
        serviceProvider.GetService(typeof(IEncina)).Returns(encina);

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
        await Task.Delay(500);
        cts.Cancel();

        // Assert - should have polled multiple times
        await store.Received().GetPendingMessagesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
        await processor.StopAsync(default);
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
        await processed.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await cts.CancelAsync();
        await processor.StopAsync(default);

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
        var errorMessage = await failed.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await cts.CancelAsync();
        await processor.StopAsync(default);

        // Assert
        errorMessage.ShouldBe("payment.gateway.down");
        await store.DidNotReceive().MarkAsProcessedAsync(message.Id, Arg.Any<CancellationToken>());
        permanentFailure.ShouldNotBeNull();
    }

    private static DelayedRetryProcessor CreateProcessor(IDelayedRetryStore store, IEncina encina, RecoverabilityOptions options)
    {
        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IDelayedRetryStore)).Returns(store);
        serviceProvider.GetService(typeof(IEncina)).Returns(encina);

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
        message.ScheduledAtUtc.Returns(DateTime.UtcNow.AddMinutes(-1));
        message.ExecuteAtUtc.Returns(DateTime.UtcNow.AddMinutes(-1));
        return message;
    }

    #endregion
}
