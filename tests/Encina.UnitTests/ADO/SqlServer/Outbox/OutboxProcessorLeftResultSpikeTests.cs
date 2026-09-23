using Encina.ADO.SqlServer.Outbox;
using Encina.Messaging;
using Encina.Messaging.Outbox;
using LanguageExt;
using Microsoft.Extensions.Logging.Abstractions;

#pragma warning disable CA2012 // Use ValueTasks correctly - Required for NSubstitute mocking pattern

namespace Encina.UnitTests.ADO.SqlServer.Outbox;

/// <summary>
/// Spike reproduction for finding C2 (verification spike, branch spike/verify-request-context):
/// <see cref="OutboxProcessor"/> (src/Encina.ADO.SqlServer/Outbox/OutboxProcessor.cs, ~line 162)
/// calls <c>await encina.Publish(notification, cancellationToken)</c> and discards the returned
/// <see cref="Either{EncinaError, Unit}"/>. <c>Encina.NotificationDispatcher.ExecuteAsync</c>
/// (src/Encina/Dispatchers/Encina.NotificationDispatcher.cs, ~lines 72-96) converts a handler's
/// <c>Left</c> result (or a handler exception, via <c>ExecuteHandlerAsync</c>, ~lines 157-190) into
/// a <c>Left</c> return value from <c>Publish</c> - it does not throw. Because
/// <see cref="OutboxProcessor"/> only reacts to a thrown <see cref="Exception"/> (the surrounding
/// <c>try</c>/<c>catch</c>), a handler that returns <c>Left</c> falls straight through to
/// <c>store.MarkAsProcessedAsync</c>, so a failed delivery is recorded as delivered and the retry
/// path (<c>MarkAsFailedAsync</c> with an incremented <c>RetryCount</c>/<c>NextRetryAtUtc</c>) never
/// runs. The same <c>encina.Publish(...)</c>-without-checking-<c>IsLeft</c> pattern is present
/// verbatim in all six ADO.NET/Dapper processors (SqlServer, PostgreSQL, MySQL) and in
/// <c>Encina.EntityFrameworkCore.Outbox.OutboxProcessor</c> (see the sibling EF Core spike test),
/// so this reproduction is representative of all seven. <c>Encina.MongoDB.Outbox</c> has an
/// <c>OutboxMessage</c>/<c>OutboxMessageFactory</c>/<c>OutboxStoreMongoDB</c> but no
/// <c>OutboxProcessor.cs</c> at all - MongoDB currently has no outbox background processor to carry
/// this bug (or to process the outbox at all).
/// </summary>
public sealed class OutboxProcessorLeftResultSpikeTests
{
    /// <summary>
    /// CONFIRMED (C2): a notification handler that returns <c>Left</c> (surfaced through
    /// <c>IEncina.Publish</c> returning <c>Left</c>, exactly as <c>NotificationDispatcher</c> does for
    /// both a handler-returned <c>Left</c> and a handler exception) should cause the outbox processor
    /// to treat the message as failed and schedule a retry - not mark it processed.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WhenPublishReturnsLeft_ShouldNotMarkAsProcessed_AndShouldScheduleRetry()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var message = new OutboxMessage
        {
            Id = messageId,
            NotificationType = typeof(TestOutboxNotification).AssemblyQualifiedName!,
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
                    return Task.FromResult<Either<EncinaError, IEnumerable<IOutboxMessage>>>(Either<EncinaError, IEnumerable<IOutboxMessage>>.Right(new List<IOutboxMessage> { message }));
                }
                return Task.FromResult<Either<EncinaError, IEnumerable<IOutboxMessage>>>(Either<EncinaError, IEnumerable<IOutboxMessage>>.Right(Enumerable.Empty<IOutboxMessage>()));
            });

        store.MarkAsProcessedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Either<EncinaError, Unit>>(Unit.Default));
        store.MarkAsFailedAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Either<EncinaError, Unit>>(Unit.Default));
        store.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Either<EncinaError, Unit>>(Unit.Default));

        // This is exactly what NotificationDispatcher.ExecuteAsync returns when a handler returns
        // Left, or when a handler throws - both cases surface as a Left from Publish, never as a
        // thrown exception out of Publish itself.
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
        await Task.Delay(500);
        cts.Cancel();
        await processor.StopAsync(CancellationToken.None);

        // Assert
        // Sanity check: the handler failure did reach Publish.
        await encina.Received().Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>());

        // Expected (correct) behavior: a Left from Publish should be treated as a failed delivery.
        // Actual (current, buggy) behavior: the Either is discarded, so the processor falls through
        // to MarkAsProcessedAsync and never calls MarkAsFailedAsync.
        await store.Received(1).MarkAsFailedAsync(
            messageId,
            Arg.Any<string>(),
            Arg.Any<DateTime?>(),
            Arg.Any<CancellationToken>());
        await store.DidNotReceive().MarkAsProcessedAsync(messageId, Arg.Any<CancellationToken>());
    }
}
