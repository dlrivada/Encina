using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using LanguageExt;
using Microsoft.Extensions.Logging;

namespace Encina.Messaging.Outbox;

/// <summary>
/// Post-processor that intercepts notifications and stores them in the outbox instead of publishing immediately.
/// This ensures reliable event delivery by persisting events in the same transaction as domain changes.
/// </summary>
/// <typeparam name="TRequest">The type of the request.</typeparam>
/// <typeparam name="TResponse">The type of the response.</typeparam>
public sealed class OutboxPostProcessor<TRequest, TResponse> : IRequestPostProcessor<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IOutboxStore _outboxStore;
    private readonly IOutboxMessageFactory _messageFactory;
    private readonly ILogger<OutboxPostProcessor<TRequest, TResponse>> _logger;
    private readonly TimeProvider _timeProvider;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxPostProcessor{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="outboxStore">The outbox store for persisting notifications.</param>
    /// <param name="messageFactory">The factory for creating outbox messages.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="timeProvider">Optional time provider for testability.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    public OutboxPostProcessor(
        IOutboxStore outboxStore,
        IOutboxMessageFactory messageFactory,
        ILogger<OutboxPostProcessor<TRequest, TResponse>> logger,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(outboxStore);
        ArgumentNullException.ThrowIfNull(messageFactory);
        ArgumentNullException.ThrowIfNull(logger);

        _outboxStore = outboxStore;
        _messageFactory = messageFactory;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public async Task Process(
        TRequest request,
        IRequestContext context,
        Either<EncinaError, TResponse> response,
        CancellationToken cancellationToken)
    {
        // Only process if request has notifications
        if (request is not IHasNotifications hasNotifications)
            return;

        var notifications = hasNotifications.GetNotifications().ToList();
        if (notifications.Count == 0)
            return;

        // Only store notifications from successful requests
        await response.Match(
            Right: async _ =>
            {
                Log.StoringNotificationsInOutbox(_logger, notifications.Count, typeof(TRequest).Name, context.CorrelationId);

                foreach (var notification in notifications)
                {
                    var notificationType = notification.GetType().AssemblyQualifiedName
                        ?? notification.GetType().FullName
                        ?? notification.GetType().Name;

                    var content = JsonSerializer.Serialize(notification, notification.GetType(), JsonOptions);

                    var outboxMessage = _messageFactory.Create(
                        Guid.NewGuid(),
                        notificationType,
                        content,
                        _timeProvider.GetUtcNow().UtcDateTime);

                    await _outboxStore.AddAsync(outboxMessage, cancellationToken).ConfigureAwait(false);
                }

                await _outboxStore.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                Log.StoredNotificationsInOutbox(_logger, notifications.Count, context.CorrelationId);
            },
            Left: error =>
            {
                Log.SkippingOutboxStorageDueToError(_logger, notifications.Count, error.Message, context.CorrelationId);

                return Task.CompletedTask;
            });
    }
}

/// <summary>
/// Marker interface for requests that can emit notifications.
/// </summary>
public interface IHasNotifications
{
    /// <summary>
    /// Gets the notifications to be published.
    /// </summary>
    IEnumerable<INotification> GetNotifications();
}

/// <summary>
/// LoggerMessage definitions for OutboxPostProcessor.
/// </summary>
internal static partial class Log
{
    [LoggerMessage(
        EventId = 110,
        Level = LogLevel.Debug,
        Message = "Storing {Count} notifications in outbox for request {RequestType} (correlation: {CorrelationId})")]
    public static partial void StoringNotificationsInOutbox(ILogger logger, int count, string requestType, string correlationId);

    [LoggerMessage(
        EventId = 111,
        Level = LogLevel.Debug,
        Message = "Stored {Count} notifications in outbox (correlation: {CorrelationId})")]
    public static partial void StoredNotificationsInOutbox(ILogger logger, int count, string correlationId);

    [LoggerMessage(
        EventId = 112,
        Level = LogLevel.Debug,
        Message = "Skipping outbox storage for {Count} notifications due to error: {ErrorMessage} (correlation: {CorrelationId})")]
    public static partial void SkippingOutboxStorageDueToError(ILogger logger, int count, string errorMessage, string correlationId);
}
