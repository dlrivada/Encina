using Encina.ADO.PostgreSQL.Outbox;
using Encina.Messaging;
using Encina.Messaging.Outbox;
using LanguageExt;
using Microsoft.Extensions.Logging.Abstractions;

#pragma warning disable CA2012 // Use ValueTasks correctly - Required for NSubstitute mocking pattern

namespace Encina.UnitTests.ADO.PostgreSQL.Outbox;

/// <summary>
/// Unit tests for <see cref="OutboxProcessor"/>.
/// </summary>
public sealed class OutboxProcessorTests
{
    private static readonly DateTime FixedCreatedAtUtc = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

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

        // Act - a disabled processor's ExecuteAsync returns immediately, so there is no
        // loop iteration to wait for.
        await processor.StartAsync(cts.Token);
        await processor.StopAsync(cts.Token);

        // Assert - Should complete without processing anything
    }

    [Fact]
    public async Task ExecuteAsync_WhenCancellationRequested_StopsProcessing()
    {
        // Arrange
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var store = Substitute.For<IOutboxStore>();
        store.GetPendingMessagesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Either<EncinaError, IEnumerable<IOutboxMessage>>>(Either<EncinaError, IEnumerable<IOutboxMessage>>.Right(Enumerable.Empty<IOutboxMessage>())))
            .AndDoes(_ =>
            {
                Thread.Sleep(10);
                tcs.TrySetResult();
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
            ProcessingInterval = TimeSpan.FromMilliseconds(10)
        };
        var processor = new OutboxProcessor(serviceProvider, logger, options);

        using var cts = new CancellationTokenSource();

        // Act
        await processor.StartAsync(cts.Token);
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(10));
        cts.Cancel();
        await processor.StopAsync(CancellationToken.None);

        // Assert
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
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var store = Substitute.For<IOutboxStore>();
        store.GetPendingMessagesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                callCount++;
                if (callCount >= 2)
                {
                    tcs.TrySetResult();
                }
                return Task.FromResult<Either<EncinaError, IEnumerable<IOutboxMessage>>>(Either<EncinaError, IEnumerable<IOutboxMessage>>.Right(Enumerable.Empty<IOutboxMessage>()));
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
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(10));
        cts.Cancel();
        await processor.StopAsync(CancellationToken.None);

        // Assert
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
            NotificationType = typeof(TestOutboxNotification).AssemblyQualifiedName!,
            Content = "{\"Value\":\"test\"}",
            CreatedAtUtc = FixedCreatedAtUtc,
            RetryCount = 0
        };

        var messagesReturned = false;
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var store = Substitute.For<IOutboxStore>();
        store.GetPendingMessagesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                if (!messagesReturned)
                {
                    messagesReturned = true;
                    return Task.FromResult<Either<EncinaError, IEnumerable<IOutboxMessage>>>(Either<EncinaError, IEnumerable<IOutboxMessage>>.Right(new List<IOutboxMessage> { message }));
                }
                return Task.FromResult<Either<EncinaError, IEnumerable<IOutboxMessage>>>(Either<EncinaError, IEnumerable<IOutboxMessage>>.Right(Enumerable.Empty<IOutboxMessage>()));
            });

        store.MarkAsProcessedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Either<EncinaError, Unit>>(Unit.Default));
        store.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                tcs.TrySetResult();
                return Task.FromResult<Either<EncinaError, Unit>>(Unit.Default);
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
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(10));
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
            CreatedAtUtc = FixedCreatedAtUtc,
            RetryCount = 0
        };

        var messagesReturned = false;
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var store = Substitute.For<IOutboxStore>();
        store.GetPendingMessagesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                if (!messagesReturned)
                {
                    messagesReturned = true;
                    return Task.FromResult<Either<EncinaError, IEnumerable<IOutboxMessage>>>(Either<EncinaError, IEnumerable<IOutboxMessage>>.Right(new List<IOutboxMessage> { message }));
                }
                return Task.FromResult<Either<EncinaError, IEnumerable<IOutboxMessage>>>(Either<EncinaError, IEnumerable<IOutboxMessage>>.Right(Enumerable.Empty<IOutboxMessage>()));
            });
        store.MarkAsFailedAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                tcs.TrySetResult();
                return Task.FromResult<Either<EncinaError, Unit>>(Unit.Default);
            });
        store.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Either<EncinaError, Unit>>(Unit.Default));

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
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(10));
        cts.Cancel();
        await processor.StopAsync(CancellationToken.None);

        // Assert
        await store.Received().MarkAsFailedAsync(
            messageId,
            Arg.Is<string>(s => s.Contains("Unknown notification type")),
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
            NotificationType = typeof(TestOutboxNotification).AssemblyQualifiedName!,
            Content = "{\"Value\":\"test\"}",
            CreatedAtUtc = FixedCreatedAtUtc,
            RetryCount = 0
        };

        var messagesReturned = false;
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var store = Substitute.For<IOutboxStore>();
        store.GetPendingMessagesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                if (!messagesReturned)
                {
                    messagesReturned = true;
                    return Task.FromResult<Either<EncinaError, IEnumerable<IOutboxMessage>>>(Either<EncinaError, IEnumerable<IOutboxMessage>>.Right(new List<IOutboxMessage> { message }));
                }
                return Task.FromResult<Either<EncinaError, IEnumerable<IOutboxMessage>>>(Either<EncinaError, IEnumerable<IOutboxMessage>>.Right(Enumerable.Empty<IOutboxMessage>()));
            });
        store.MarkAsFailedAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                tcs.TrySetResult();
                return Task.FromResult<Either<EncinaError, Unit>>(Unit.Default);
            });
        store.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Either<EncinaError, Unit>>(Unit.Default));

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
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(10));
        cts.Cancel();
        await processor.StopAsync(CancellationToken.None);

        // Assert
        await store.Received().MarkAsFailedAsync(
            messageId,
            Arg.Is<string>(s => s == typeof(InvalidOperationException).FullName && !s.Contains("Test exception")),
            Arg.Any<DateTime?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenExceptionThrownDuringProcessing_ContinuesLoop()
    {
        // Arrange
        var callCount = 0;
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var store = Substitute.For<IOutboxStore>();
        store.GetPendingMessagesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                callCount++;
                if (callCount == 1)
                {
                    throw new InvalidOperationException("Simulated error");
                }
                tcs.TrySetResult();
                return Task.FromResult<Either<EncinaError, IEnumerable<IOutboxMessage>>>(Either<EncinaError, IEnumerable<IOutboxMessage>>.Right(Enumerable.Empty<IOutboxMessage>()));
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
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(10));
        cts.Cancel();
        await processor.StopAsync(CancellationToken.None);

        // Assert
        callCount.ShouldBeGreaterThan(1);
    }

    #endregion
}

/// <summary>
/// Test notification for OutboxProcessor tests.
/// </summary>
public record TestOutboxNotification(string Value) : INotification;
