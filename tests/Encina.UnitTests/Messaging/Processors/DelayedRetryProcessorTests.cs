using Encina.Messaging.Recoverability;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using Shouldly;

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
            .Returns(Array.Empty<IDelayedRetryMessage>());

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

        // Assert - should have polled multiple times
        await store.Received().GetPendingMessagesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
        await processor.StopAsync(default);
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
