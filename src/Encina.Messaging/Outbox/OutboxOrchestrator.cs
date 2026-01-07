using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using LanguageExt;
using Microsoft.Extensions.Logging;

namespace Encina.Messaging.Outbox;

/// <summary>
/// Orchestrates the Outbox Pattern for reliable event publishing.
/// </summary>
/// <remarks>
/// <para>
/// This orchestrator contains all domain logic for the Outbox Pattern, delegating
/// persistence operations to <see cref="IOutboxStore"/>. It ensures at-least-once
/// delivery of domain events.
/// </para>
/// <para>
/// <b>Processing Flow</b>:
/// <list type="number">
/// <item><description>Add message to outbox (in same transaction as domain changes)</description></item>
/// <item><description>Background processor retrieves pending messages</description></item>
/// <item><description>Publish each message via the configured publisher</description></item>
/// <item><description>Mark as processed or schedule retry on failure</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class OutboxOrchestrator
{
    private readonly IOutboxStore _store;
    private readonly OutboxOptions _options;
    private readonly ILogger<OutboxOrchestrator> _logger;
    private readonly IOutboxMessageFactory _messageFactory;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxOrchestrator"/> class.
    /// </summary>
    /// <param name="store">The outbox store for persistence.</param>
    /// <param name="options">The outbox options.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="messageFactory">Factory to create outbox messages.</param>
    public OutboxOrchestrator(
        IOutboxStore store,
        OutboxOptions options,
        ILogger<OutboxOrchestrator> logger,
        IOutboxMessageFactory messageFactory)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(messageFactory);

        _store = store;
        _options = options;
        _logger = logger;
        _messageFactory = messageFactory;
    }

    /// <summary>
    /// Adds a notification to the outbox for reliable publishing.
    /// </summary>
    /// <typeparam name="TNotification">The notification type.</typeparam>
    /// <param name="notification">The notification to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the operation.</returns>
    public async Task AddAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : class
    {
        ArgumentNullException.ThrowIfNull(notification);

        var notificationType = typeof(TNotification).AssemblyQualifiedName
            ?? typeof(TNotification).FullName
            ?? typeof(TNotification).Name;

        var content = JsonSerializer.Serialize(notification, JsonOptions);

        var message = _messageFactory.Create(
            Guid.NewGuid(),
            notificationType,
            content,
            DateTime.UtcNow);

        await _store.AddAsync(message, cancellationToken).ConfigureAwait(false);

        Log.MessageAddedToOutbox(_logger, message.Id, notificationType);
    }

    /// <summary>
    /// Processes pending messages from the outbox.
    /// </summary>
    /// <param name="publishCallback">The callback to publish each message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of messages processed successfully.</returns>
    public async Task<int> ProcessPendingMessagesAsync(
        Func<IOutboxMessage, Type, object, Task> publishCallback,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(publishCallback);

        var messages = await _store.GetPendingMessagesAsync(
            _options.BatchSize,
            _options.MaxRetries,
            cancellationToken).ConfigureAwait(false);

        var processedCount = 0;

        foreach (var message in messages)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                var notificationType = Type.GetType(message.NotificationType);
                if (notificationType == null)
                {
                    Log.UnknownNotificationType(_logger, message.Id, message.NotificationType);
                    await MarkAsFailedAsync(message.Id, $"Unknown notification type: {message.NotificationType}", cancellationToken).ConfigureAwait(false);
                    continue;
                }

                var notification = JsonSerializer.Deserialize(message.Content, notificationType, JsonOptions);
                if (notification == null)
                {
                    Log.DeserializationFailed(_logger, message.Id, message.NotificationType);
                    await MarkAsFailedAsync(message.Id, "Failed to deserialize notification", cancellationToken).ConfigureAwait(false);
                    continue;
                }

                await publishCallback(message, notificationType, notification).ConfigureAwait(false);

                await _store.MarkAsProcessedAsync(message.Id, cancellationToken).ConfigureAwait(false);
                processedCount++;

                Log.MessageProcessed(_logger, message.Id);
            }
            catch (Exception ex)
            {
                Log.ProcessingFailed(_logger, ex, message.Id);
                await MarkAsFailedAsync(message.Id, ex.Message, cancellationToken).ConfigureAwait(false);
            }
        }

        return processedCount;
    }

    /// <summary>
    /// Gets the count of pending messages.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The count of pending messages.</returns>
    public async Task<int> GetPendingCountAsync(CancellationToken cancellationToken = default)
    {
        var messages = await _store.GetPendingMessagesAsync(
            int.MaxValue,
            _options.MaxRetries,
            cancellationToken).ConfigureAwait(false);

        return messages.Count();
    }

    private async Task MarkAsFailedAsync(Guid messageId, string errorMessage, CancellationToken cancellationToken)
    {
        var nextRetryAt = CalculateNextRetryTime();
        await _store.MarkAsFailedAsync(messageId, errorMessage, nextRetryAt, cancellationToken).ConfigureAwait(false);
    }

    private DateTime CalculateNextRetryTime()
    {
        // Exponential backoff: BaseRetryDelay * 2^retryCount
        // Since we don't have the current retry count here, use base delay
        return DateTime.UtcNow.Add(_options.BaseRetryDelay);
    }
}

/// <summary>
/// Factory interface for creating outbox messages.
/// </summary>
/// <remarks>
/// Each provider (EF Core, Dapper, ADO.NET) implements this to create their specific message type.
/// </remarks>
public interface IOutboxMessageFactory
{
    /// <summary>
    /// Creates a new outbox message.
    /// </summary>
    /// <param name="id">The message ID.</param>
    /// <param name="notificationType">The notification type.</param>
    /// <param name="content">The serialized content.</param>
    /// <param name="createdAtUtc">The creation timestamp.</param>
    /// <returns>A new outbox message instance.</returns>
    IOutboxMessage Create(
        Guid id,
        string notificationType,
        string content,
        DateTime createdAtUtc);
}

/// <summary>
/// Error codes for outbox operations.
/// </summary>
public static class OutboxErrorCodes
{
    /// <summary>
    /// Unknown notification type during processing.
    /// </summary>
    public const string UnknownNotificationType = "outbox.unknown_notification_type";

    /// <summary>
    /// Failed to deserialize notification.
    /// </summary>
    public const string DeserializationFailed = "outbox.deserialization_failed";

    /// <summary>
    /// Failed to publish notification.
    /// </summary>
    public const string PublishFailed = "outbox.publish_failed";

    /// <summary>
    /// Maximum retries exceeded.
    /// </summary>
    public const string MaxRetriesExceeded = "outbox.max_retries_exceeded";
}

/// <summary>
/// LoggerMessage definitions for high-performance logging.
/// </summary>
[ExcludeFromCodeCoverage]
internal static partial class Log
{
    [LoggerMessage(
        EventId = 101,
        Level = LogLevel.Debug,
        Message = "Message {MessageId} added to outbox (type: {NotificationType})")]
    public static partial void MessageAddedToOutbox(ILogger logger, Guid messageId, string notificationType);

    [LoggerMessage(
        EventId = 102,
        Level = LogLevel.Debug,
        Message = "Message {MessageId} processed successfully")]
    public static partial void MessageProcessed(ILogger logger, Guid messageId);

    [LoggerMessage(
        EventId = 103,
        Level = LogLevel.Warning,
        Message = "Unknown notification type for message {MessageId}: {NotificationType}")]
    public static partial void UnknownNotificationType(ILogger logger, Guid messageId, string notificationType);

    [LoggerMessage(
        EventId = 104,
        Level = LogLevel.Warning,
        Message = "Failed to deserialize message {MessageId} of type {NotificationType}")]
    public static partial void DeserializationFailed(ILogger logger, Guid messageId, string notificationType);

    [LoggerMessage(
        EventId = 105,
        Level = LogLevel.Error,
        Message = "Failed to process message {MessageId}")]
    public static partial void ProcessingFailed(ILogger logger, Exception ex, Guid messageId);
}
