using Encina.ADO.SqlServer.Outbox;
using Encina.Messaging.Outbox;
using LanguageExt;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

#pragma warning disable CA2012 // Use ValueTasks correctly - Required for NSubstitute mocking pattern

namespace Encina.UnitTests.ADO.SqlServer.Outbox;

/// <summary>
/// Regression tests for #1151: <see cref="OutboxProcessor"/> must treat a <c>Left</c> returned by
/// <c>IEncina.Publish</c> as a failed delivery. <c>NotificationDispatcher</c> turns both a handler's
/// <c>Left</c> and a handler exception into a <c>Left</c> return value and never throws, so a
/// processor that only reacted to exceptions marked every failed delivery as processed.
/// </summary>
public sealed class OutboxProcessorLeftResultTests
{
    [Fact]
    public async Task ExecuteAsync_WhenPublishReturnsLeft_ShouldNotMarkAsProcessed_AndShouldScheduleRetry()
    {
        // Arrange
        var now = new DateTimeOffset(2026, 9, 23, 10, 0, 0, TimeSpan.Zero);
        var timeProvider = new FakeTimeProvider(now);
        var messageId = Guid.NewGuid();
        var message = new OutboxMessage
        {
            Id = messageId,
            NotificationType = typeof(TestOutboxNotification).AssemblyQualifiedName!,
            Content = "{\"Value\":\"test\"}",
            CreatedAtUtc = now.UtcDateTime,
            RetryCount = 0
        };

        var markedAsFailed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var messagesReturned = false;
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
        store.MarkAsFailedAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Either<EncinaError, Unit>>(Unit.Default))
            .AndDoes(_ => markedAsFailed.TrySetResult());
        store.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Either<EncinaError, Unit>>(Unit.Default));

        // This is exactly what NotificationDispatcher returns when a handler returns Left or throws.
        var handlerError = EncinaErrors.Create("handler.rejected", "Handler rejected the notification");
        var encina = Substitute.For<IEncina>();
        encina.Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<Either<EncinaError, Unit>>(Either<EncinaError, Unit>.Left(handlerError)));

        var scope = Substitute.For<IServiceScope>();
        var scopeServiceProvider = Substitute.For<IServiceProvider>();
        scopeServiceProvider.GetService(typeof(IOutboxStore)).Returns(store);
        scopeServiceProvider.GetService(typeof(IEncina)).Returns(encina);
        scope.ServiceProvider.Returns(scopeServiceProvider);

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);

        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IServiceScopeFactory)).Returns(scopeFactory);

        var options = new OutboxOptions
        {
            EnableProcessor = true,
            ProcessingInterval = TimeSpan.FromMilliseconds(20),
            MaxRetries = 3,
            BaseRetryDelay = TimeSpan.FromSeconds(5),
            RetryJitterRatio = 0
        };
        var processor = new OutboxProcessor(serviceProvider, NullLogger<OutboxProcessor>.Instance, options, timeProvider);

        using var cts = new CancellationTokenSource();

        // Act
        await processor.StartAsync(cts.Token);
        await markedAsFailed.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await cts.CancelAsync();
        await processor.StopAsync(CancellationToken.None);

        // Assert
        await encina.Received().Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>());
        await store.Received(1).MarkAsFailedAsync(
            messageId,
            "Handler rejected the notification",
            now.UtcDateTime.AddSeconds(5),
            Arg.Any<CancellationToken>());
        await store.DidNotReceive().MarkAsProcessedAsync(messageId, Arg.Any<CancellationToken>());
    }
}
