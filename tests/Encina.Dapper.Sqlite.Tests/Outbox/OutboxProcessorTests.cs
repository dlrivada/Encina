using Encina.Dapper.Sqlite.Outbox;
using Encina.Messaging;
using Encina.Messaging.Outbox;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

#pragma warning disable CA2012 // Use ValueTasks correctly - Required for NSubstitute mocking pattern

namespace Encina.Dapper.Sqlite.Tests.Outbox;

/// <summary>
/// Unit tests for <see cref="OutboxProcessor"/>.
/// </summary>
public sealed class OutboxProcessorTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_NullServiceProvider_ThrowsArgumentNullException()
    {
        // Arrange
        var logger = NullLogger<OutboxProcessor>.Instance;
        var options = new OutboxOptions();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new OutboxProcessor(null!, logger, options));
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        var options = new OutboxOptions();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new OutboxProcessor(serviceProvider, null!, options));
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        var logger = NullLogger<OutboxProcessor>.Instance;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new OutboxProcessor(serviceProvider, logger, null!));
    }

    [Fact]
    public void Constructor_ValidParameters_CreatesInstance()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        var logger = NullLogger<OutboxProcessor>.Instance;
        var options = new OutboxOptions();

        // Act
        var processor = new OutboxProcessor(serviceProvider, logger, options);

        // Assert
        processor.ShouldNotBeNull();
    }

    #endregion

    #region ExecuteAsync Tests

    [Fact]
    public async Task ExecuteAsync_WhenProcessorDisabled_ReturnsImmediately()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        var logger = NullLogger<OutboxProcessor>.Instance;
        var options = new OutboxOptions { EnableProcessor = false };
        var processor = new OutboxProcessor(serviceProvider, logger, options);

        using var cts = new CancellationTokenSource();

        // Act - Start and give it some time to process
        await processor.StartAsync(cts.Token);

        // Wait a bit and stop
        await Task.Delay(50);
        await processor.StopAsync(cts.Token);

        // Assert - Should complete without processing anything
        // The processor should have returned immediately due to disabled flag
    }

    [Fact]
    public async Task ExecuteAsync_WhenCancellationRequested_StopsProcessing()
    {
        // Arrange
        var store = Substitute.For<IOutboxStore>();
        store.GetPendingMessagesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IEnumerable<IOutboxMessage>>([]))
            .AndDoes(_ => Thread.Sleep(10)); // Small delay to simulate work

        var encina = Substitute.For<IEncina>();
        var scope = Substitute.For<IServiceScope>();
        var scopeServiceProvider = Substitute.For<IServiceProvider>();
        scopeServiceProvider.GetService(typeof(IOutboxStore)).Returns(store);
        scopeServiceProvider.GetService(typeof(IEncina)).Returns(encina);
        scope.ServiceProvider.Returns(scopeServiceProvider);

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);

        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IServiceScopeFactory)).Returns(scopeFactory);

        var logger = NullLogger<OutboxProcessor>.Instance;
        var options = new OutboxOptions
        {
            EnableProcessor = true,
            ProcessingInterval = TimeSpan.FromMilliseconds(10)
        };
        var processor = new OutboxProcessor(serviceProvider, logger, options);

        using var cts = new CancellationTokenSource();

        // Act
        await processor.StartAsync(cts.Token);
        await Task.Delay(50); // Let it run a bit
        cts.Cancel();
        await processor.StopAsync(CancellationToken.None);

        // Assert - Should have called GetPendingMessagesAsync at least once
        await store.Received().GetPendingMessagesAsync(
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithNoPendingMessages_ContinuesProcessingLoop()
    {
        // Arrange
        var callCount = 0;
        var store = Substitute.For<IOutboxStore>();
        store.GetPendingMessagesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                callCount++;
                return Task.FromResult<IEnumerable<IOutboxMessage>>([]);
            });

        var encina = Substitute.For<IEncina>();
        var scope = Substitute.For<IServiceScope>();
        var scopeServiceProvider = Substitute.For<IServiceProvider>();
        scopeServiceProvider.GetService(typeof(IOutboxStore)).Returns(store);
        scopeServiceProvider.GetService(typeof(IEncina)).Returns(encina);
        scope.ServiceProvider.Returns(scopeServiceProvider);

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);

        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IServiceScopeFactory)).Returns(scopeFactory);

        var logger = NullLogger<OutboxProcessor>.Instance;
        var options = new OutboxOptions
        {
            EnableProcessor = true,
            ProcessingInterval = TimeSpan.FromMilliseconds(20)
        };
        var processor = new OutboxProcessor(serviceProvider, logger, options);

        using var cts = new CancellationTokenSource();

        // Act
        await processor.StartAsync(cts.Token);
        await Task.Delay(100); // Let it run multiple iterations
        cts.Cancel();
        await processor.StopAsync(CancellationToken.None);

        // Assert - Should have called GetPendingMessagesAsync multiple times
        callCount.ShouldBeGreaterThan(1);
    }

    [Fact]
    public async Task ExecuteAsync_WithPendingMessage_ProcessesAndMarksAsProcessed()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var message = new OutboxMessage
        {
            Id = messageId,
            NotificationType = typeof(TestNotification).AssemblyQualifiedName!,
            Content = "{\"Value\":\"test\"}",
            CreatedAtUtc = DateTime.UtcNow,
            RetryCount = 0
        };

        var messagesReturned = false;
        var store = Substitute.For<IOutboxStore>();
        store.GetPendingMessagesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                if (!messagesReturned)
                {
                    messagesReturned = true;
                    return Task.FromResult<IEnumerable<IOutboxMessage>>([message]);
                }
                return Task.FromResult<IEnumerable<IOutboxMessage>>([]);
            });

        var encina = Substitute.For<IEncina>();
        encina.Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, Unit>>(Either<EncinaError, Unit>.Right(Unit.Default)));

        var scope = Substitute.For<IServiceScope>();
        var scopeServiceProvider = Substitute.For<IServiceProvider>();
        scopeServiceProvider.GetService(typeof(IOutboxStore)).Returns(store);
        scopeServiceProvider.GetService(typeof(IEncina)).Returns(encina);
        scope.ServiceProvider.Returns(scopeServiceProvider);

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);

        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IServiceScopeFactory)).Returns(scopeFactory);

        var logger = NullLogger<OutboxProcessor>.Instance;
        var options = new OutboxOptions
        {
            EnableProcessor = true,
            ProcessingInterval = TimeSpan.FromMilliseconds(20)
        };
        var processor = new OutboxProcessor(serviceProvider, logger, options);

        using var cts = new CancellationTokenSource();

        // Act
        await processor.StartAsync(cts.Token);
        await Task.Delay(100);
        cts.Cancel();
        await processor.StopAsync(CancellationToken.None);

        // Assert
        await encina.Received().Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>());
        await store.Received().MarkAsProcessedAsync(messageId, Arg.Any<CancellationToken>());
        await store.Received().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithUnknownNotificationType_MarksAsFailed()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var message = new OutboxMessage
        {
            Id = messageId,
            NotificationType = "NonExistent.Type, NonExistent",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow,
            RetryCount = 0
        };

        var messagesReturned = false;
        var store = Substitute.For<IOutboxStore>();
        store.GetPendingMessagesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                if (!messagesReturned)
                {
                    messagesReturned = true;
                    return Task.FromResult<IEnumerable<IOutboxMessage>>([message]);
                }
                return Task.FromResult<IEnumerable<IOutboxMessage>>([]);
            });

        var encina = Substitute.For<IEncina>();
        var scope = Substitute.For<IServiceScope>();
        var scopeServiceProvider = Substitute.For<IServiceProvider>();
        scopeServiceProvider.GetService(typeof(IOutboxStore)).Returns(store);
        scopeServiceProvider.GetService(typeof(IEncina)).Returns(encina);
        scope.ServiceProvider.Returns(scopeServiceProvider);

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);

        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IServiceScopeFactory)).Returns(scopeFactory);

        var logger = NullLogger<OutboxProcessor>.Instance;
        var options = new OutboxOptions
        {
            EnableProcessor = true,
            ProcessingInterval = TimeSpan.FromMilliseconds(20),
            MaxRetries = 3
        };
        var processor = new OutboxProcessor(serviceProvider, logger, options);

        using var cts = new CancellationTokenSource();

        // Act
        await processor.StartAsync(cts.Token);
        await Task.Delay(100);
        cts.Cancel();
        await processor.StopAsync(CancellationToken.None);

        // Assert
        await store.Received().MarkAsFailedAsync(
            messageId,
            Arg.Is<string>(s => s.Contains("Type not found")),
            Arg.Any<DateTime?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenPublishThrowsException_MarksAsFailed()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var message = new OutboxMessage
        {
            Id = messageId,
            NotificationType = typeof(TestNotification).AssemblyQualifiedName!,
            Content = "{\"Value\":\"test\"}",
            CreatedAtUtc = DateTime.UtcNow,
            RetryCount = 0
        };

        var messagesReturned = false;
        var store = Substitute.For<IOutboxStore>();
        store.GetPendingMessagesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                if (!messagesReturned)
                {
                    messagesReturned = true;
                    return Task.FromResult<IEnumerable<IOutboxMessage>>([message]);
                }
                return Task.FromResult<IEnumerable<IOutboxMessage>>([]);
            });

        var encina = Substitute.For<IEncina>();
        encina.Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, Unit>>(Task.FromException<Either<EncinaError, Unit>>(new InvalidOperationException("Test exception"))));

        var scope = Substitute.For<IServiceScope>();
        var scopeServiceProvider = Substitute.For<IServiceProvider>();
        scopeServiceProvider.GetService(typeof(IOutboxStore)).Returns(store);
        scopeServiceProvider.GetService(typeof(IEncina)).Returns(encina);
        scope.ServiceProvider.Returns(scopeServiceProvider);

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);

        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IServiceScopeFactory)).Returns(scopeFactory);

        var logger = NullLogger<OutboxProcessor>.Instance;
        var options = new OutboxOptions
        {
            EnableProcessor = true,
            ProcessingInterval = TimeSpan.FromMilliseconds(20),
            MaxRetries = 3
        };
        var processor = new OutboxProcessor(serviceProvider, logger, options);

        using var cts = new CancellationTokenSource();

        // Act
        await processor.StartAsync(cts.Token);
        await Task.Delay(100);
        cts.Cancel();
        await processor.StopAsync(CancellationToken.None);

        // Assert
        await store.Received().MarkAsFailedAsync(
            messageId,
            Arg.Is<string>(s => s.Contains("Test exception")),
            Arg.Any<DateTime?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithExhaustedRetries_DoesNotScheduleNextRetry()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var message = new OutboxMessage
        {
            Id = messageId,
            NotificationType = "NonExistent.Type, NonExistent",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow,
            RetryCount = 2  // Already at max-1, so next failure will exhaust retries
        };

        var messagesReturned = false;
        var store = Substitute.For<IOutboxStore>();
        store.GetPendingMessagesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                if (!messagesReturned)
                {
                    messagesReturned = true;
                    return Task.FromResult<IEnumerable<IOutboxMessage>>([message]);
                }
                return Task.FromResult<IEnumerable<IOutboxMessage>>([]);
            });

        var encina = Substitute.For<IEncina>();
        var scope = Substitute.For<IServiceScope>();
        var scopeServiceProvider = Substitute.For<IServiceProvider>();
        scopeServiceProvider.GetService(typeof(IOutboxStore)).Returns(store);
        scopeServiceProvider.GetService(typeof(IEncina)).Returns(encina);
        scope.ServiceProvider.Returns(scopeServiceProvider);

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);

        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IServiceScopeFactory)).Returns(scopeFactory);

        var logger = NullLogger<OutboxProcessor>.Instance;
        var options = new OutboxOptions
        {
            EnableProcessor = true,
            ProcessingInterval = TimeSpan.FromMilliseconds(20),
            MaxRetries = 3  // retryCount + 1 >= maxRetries, so no next retry
        };
        var processor = new OutboxProcessor(serviceProvider, logger, options);

        using var cts = new CancellationTokenSource();

        // Act
        await processor.StartAsync(cts.Token);
        await Task.Delay(100);
        cts.Cancel();
        await processor.StopAsync(CancellationToken.None);

        // Assert - nextRetryAtUtc should be null (exhausted retries)
        await store.Received().MarkAsFailedAsync(
            messageId,
            Arg.Any<string>(),
            Arg.Is<DateTime?>(d => d == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenExceptionThrownDuringProcessing_ContinuesLoop()
    {
        // Arrange
        var callCount = 0;
        var store = Substitute.For<IOutboxStore>();
        store.GetPendingMessagesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                callCount++;
                if (callCount == 1)
                {
                    throw new InvalidOperationException("Simulated error");
                }
                return Task.FromResult<IEnumerable<IOutboxMessage>>([]);
            });

        var encina = Substitute.For<IEncina>();
        var scope = Substitute.For<IServiceScope>();
        var scopeServiceProvider = Substitute.For<IServiceProvider>();
        scopeServiceProvider.GetService(typeof(IOutboxStore)).Returns(store);
        scopeServiceProvider.GetService(typeof(IEncina)).Returns(encina);
        scope.ServiceProvider.Returns(scopeServiceProvider);

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);

        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IServiceScopeFactory)).Returns(scopeFactory);

        var logger = NullLogger<OutboxProcessor>.Instance;
        var options = new OutboxOptions
        {
            EnableProcessor = true,
            ProcessingInterval = TimeSpan.FromMilliseconds(20)
        };
        var processor = new OutboxProcessor(serviceProvider, logger, options);

        using var cts = new CancellationTokenSource();

        // Act
        await processor.StartAsync(cts.Token);
        await Task.Delay(100); // Let it recover from error and continue
        cts.Cancel();
        await processor.StopAsync(CancellationToken.None);

        // Assert - Should have called multiple times despite the first error
        callCount.ShouldBeGreaterThan(1);
    }

    #endregion
}

/// <summary>
/// Test notification for OutboxProcessor tests.
/// </summary>
public record TestNotification(string Value) : INotification;
