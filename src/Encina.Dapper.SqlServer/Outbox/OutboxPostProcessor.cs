using System.Text.Json;
using Encina.Messaging.Outbox;
using LanguageExt;
using Microsoft.Extensions.Logging;

namespace Encina.Dapper.SqlServer.Outbox;

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
    private readonly ILogger<OutboxPostProcessor<TRequest, TResponse>> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxPostProcessor{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="outboxStore">The outbox store for persisting notifications.</param>
    /// <param name="logger">The logger.</param>
    public OutboxPostProcessor(
        IOutboxStore outboxStore,
        ILogger<OutboxPostProcessor<TRequest, TResponse>> logger)
    {
        _outboxStore = outboxStore;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task Process(
        TRequest request,
        IRequestContext context,
        Either<EncinaError, TResponse> result,
        CancellationToken cancellationToken)
    {
        // Only process if request has notifications
        if (request is not IHasNotifications hasNotifications)
            return;

        var notifications = hasNotifications.GetNotifications().ToList();
        if (notifications.Count == 0)
            return;

        // Only store notifications from successful requests
        await result.Match(
            Right: async _ =>
            {
                Log.StoringNotificationsInOutbox(_logger, notifications.Count, typeof(TRequest).Name, context.CorrelationId);

                foreach (var notification in notifications)
                {
                    var outboxMessage = new OutboxMessage
                    {
                        Id = Guid.NewGuid(),
                        NotificationType = notification.GetType().AssemblyQualifiedName
                            ?? notification.GetType().FullName
                            ?? notification.GetType().Name,
                        Content = JsonSerializer.Serialize(notification, notification.GetType(), JsonOptions),
                        CreatedAtUtc = DateTime.UtcNow,
                        RetryCount = 0
                    };

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
