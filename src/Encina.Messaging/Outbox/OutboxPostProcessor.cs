using System.Diagnostics.CodeAnalysis;
using Encina.Messaging.Serialization;
using LanguageExt;
using Microsoft.Extensions.Logging;

namespace Encina.Messaging.Outbox;

/// <summary>
/// Post-processor that intercepts notifications and stores them in the outbox instead of publishing immediately.
/// </summary>
/// <remarks>
/// Post-processors run after the whole pipeline, so the business transaction has already ended when this one
/// writes: the outbox row is NOT atomic with the business change. It is the interim write path of the providers
/// that have not moved to <see cref="TransactionalOutboxPipelineBehavior{TRequest, TResponse}"/> yet
/// (Dapper, EF Core and MongoDB, #718); it is removed when the last one does. A failed store write now fails the
/// request instead of being dropped.
/// </remarks>
/// <typeparam name="TRequest">The type of the request.</typeparam>
/// <typeparam name="TResponse">The type of the response.</typeparam>
public sealed class OutboxPostProcessor<TRequest, TResponse> : IRequestPostProcessor<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly OutboxWriter _writer;
    private readonly ILogger<OutboxPostProcessor<TRequest, TResponse>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxPostProcessor{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="outboxStore">The outbox store for persisting notifications.</param>
    /// <param name="messageFactory">The factory for creating outbox messages.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="messageSerializer">
    /// The message serializer used to convert notifications to their persisted representation.
    /// Serializing through this abstraction (rather than calling <c>JsonSerializer</c> directly)
    /// ensures that decorators such as <c>EncryptingMessageSerializer</c> from
    /// <c>Encina.Messaging.Encryption</c> apply to outbox payloads too.
    /// </param>
    /// <param name="timeProvider">Optional time provider for testability.</param>
    /// <exception cref="ArgumentNullException">Thrown when any required parameter is null.</exception>
    public OutboxPostProcessor(
        IOutboxStore outboxStore,
        IOutboxMessageFactory messageFactory,
        ILogger<OutboxPostProcessor<TRequest, TResponse>> logger,
        IMessageSerializer messageSerializer,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(outboxStore);
        ArgumentNullException.ThrowIfNull(messageFactory);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(messageSerializer);

        _logger = logger;
        _writer = new OutboxWriter(outboxStore, messageFactory, messageSerializer, timeProvider ?? TimeProvider.System);
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> Process(
        TRequest request,
        IRequestContext context,
        TResponse response,
        CancellationToken cancellationToken)
    {
        // Only process if request has notifications
        if (request is not IHasNotifications hasNotifications)
            return Unit.Default;

        var notifications = hasNotifications.GetNotifications().ToList();
        if (notifications.Count == 0)
            return Unit.Default;

        // Post-processors run only for successful requests, so these notifications are always stored.
        Log.StoringNotificationsInOutbox(_logger, notifications.Count, typeof(TRequest).Name, context.CorrelationId);

        var written = await _writer.WriteAsync(notifications, cancellationToken).ConfigureAwait(false);

        // A failed store write fails the request; it is never reported as success.
        return written.Match<Either<EncinaError, Unit>>(
            Right: _ =>
            {
                Log.StoredNotificationsInOutbox(_logger, notifications.Count, context.CorrelationId);
                return Unit.Default;
            },
            Left: error =>
            {
                MessagingLog.OutboxWriteFailed(_logger, typeof(TRequest).Name, error.GetCode().IfNone("unknown"));
                return error;
            });
    }
}

/// <summary>
/// LoggerMessage definitions for OutboxPostProcessor.
/// </summary>
internal static partial class Log
{
    [LoggerMessage(
        EventId = 2842,
        Level = LogLevel.Debug,
        Message = "Storing {Count} notifications in outbox for request {RequestType} (correlation: {CorrelationId})")]
    public static partial void StoringNotificationsInOutbox(ILogger logger, int count, string requestType, string correlationId);

    [LoggerMessage(
        EventId = 2843,
        Level = LogLevel.Debug,
        Message = "Stored {Count} notifications in outbox (correlation: {CorrelationId})")]
    public static partial void StoredNotificationsInOutbox(ILogger logger, int count, string correlationId);
}
