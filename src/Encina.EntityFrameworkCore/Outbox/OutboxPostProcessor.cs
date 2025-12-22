using System.Text.Json;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Encina.EntityFrameworkCore.Outbox;

/// <summary>
/// Post-processor that stores notifications in the outbox instead of publishing them immediately.
/// </summary>
/// <typeparam name="TRequest">The type of request.</typeparam>
/// <typeparam name="TResponse">The type of response.</typeparam>
/// <remarks>
/// <para>
/// This post-processor implements the Outbox Pattern for reliable event publishing.
/// Instead of publishing notifications immediately (which could fail if the system crashes
/// before the transaction commits), notifications are stored in the database as part of
/// the same transaction.
/// </para>
/// <para>
/// A background processor (<see cref="OutboxProcessor"/>) then reads pending messages
/// from the outbox and publishes them, with retry logic for transient failures.
/// </para>
/// <para>
/// <b>Guarantees</b>:
/// <list type="bullet">
/// <item><description>At-least-once delivery (events may be published multiple times)</description></item>
/// <item><description>Events are never lost (stored durably in database)</description></item>
/// <item><description>Events published in order of creation</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class OutboxPostProcessor<TRequest, TResponse> : IRequestPostProcessor<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly DbContext _dbContext;
    private readonly ILogger<OutboxPostProcessor<TRequest, TResponse>> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxPostProcessor{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    /// <param name="logger">The logger.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="dbContext"/> or <paramref name="logger"/> is null.</exception>
    public OutboxPostProcessor(
        DbContext dbContext,
        ILogger<OutboxPostProcessor<TRequest, TResponse>> logger)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(logger);

        _dbContext = dbContext;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task Process(
        TRequest request,
        IRequestContext context,
        Either<EncinaError, TResponse> response,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        // Only process if request has notifications and result is successful
        if (request is not IHasNotifications hasNotifications)
            return;

        var notifications = hasNotifications.GetNotifications().ToList();
        if (notifications.Count == 0)
            return;

        // Only store in outbox if the request succeeded
        await response.Match(
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

                    _dbContext.Set<OutboxMessage>().Add(outboxMessage);
                }

                // SaveChanges will be called as part of the transaction commit
                await _dbContext.SaveChangesAsync(cancellationToken);

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
/// <remarks>
/// Implement this interface on commands/queries that need to publish domain events.
/// </remarks>
public interface IHasNotifications
{
    /// <summary>
    /// Gets the notifications to be published.
    /// </summary>
    /// <returns>A collection of notifications.</returns>
    IEnumerable<INotification> GetNotifications();
}
