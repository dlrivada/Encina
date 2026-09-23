using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Encina.Messaging.Diagnostics;
using Encina.Messaging.Serialization;
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
    private readonly IMessageSerializer _messageSerializer;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxOrchestrator"/> class.
    /// </summary>
    /// <param name="store">The outbox store for persistence.</param>
    /// <param name="options">The outbox options.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="messageFactory">Factory to create outbox messages.</param>
    /// <param name="messageSerializer">The message serializer for payload serialization/deserialization.</param>
    /// <param name="timeProvider">Optional time provider for testability.</param>
    public OutboxOrchestrator(
        IOutboxStore store,
        OutboxOptions options,
        ILogger<OutboxOrchestrator> logger,
        IOutboxMessageFactory messageFactory,
        IMessageSerializer messageSerializer,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(messageFactory);
        ArgumentNullException.ThrowIfNull(messageSerializer);

        _store = store;
        _options = options;
        _logger = logger;
        _messageFactory = messageFactory;
        _messageSerializer = messageSerializer;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// Adds a notification to the outbox for reliable publishing.
    /// </summary>
    /// <typeparam name="TNotification">The notification type.</typeparam>
    /// <param name="notification">The notification to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Unit on success, or an error if the message could not be added.</returns>
    public async Task<Either<EncinaError, Unit>> AddAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : class
    {
        ArgumentNullException.ThrowIfNull(notification);

        var notificationType = typeof(TNotification).AssemblyQualifiedName
            ?? typeof(TNotification).FullName
            ?? typeof(TNotification).Name;

        var content = _messageSerializer.Serialize(notification);

        var message = _messageFactory.Create(
            Guid.NewGuid(),
            notificationType,
            content,
            _timeProvider.GetUtcNow().UtcDateTime);

        var addResult = await _store.AddAsync(message, cancellationToken).ConfigureAwait(false);
        if (addResult.IsLeft)
            return addResult.LeftToArray()[0];

        Log.MessageAddedToOutbox(_logger, message.Id, notificationType);

        return Unit.Default;
    }

    /// <summary>
    /// Processes one batch of pending messages from the outbox.
    /// </summary>
    /// <param name="publishCallback">
    /// Publishes each deserialized notification. It returns <c>Right</c> when the delivery succeeded
    /// and <c>Left</c> when it failed; a <c>Left</c> is handled exactly like a thrown exception.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of messages processed successfully, or an error.</returns>
    /// <remarks>
    /// <para>
    /// A message is marked processed only when the callback returns <c>Right</c>. Otherwise it is marked
    /// failed, with the next retry computed by <see cref="OutboxRetryBackoff"/> from its current
    /// <see cref="IOutboxMessage.RetryCount"/>; when the failure uses up <see cref="OutboxOptions.MaxRetries"/>
    /// it is recorded with no next retry and logged with <see cref="OutboxErrorCodes.MaxRetriesExceeded"/>.
    /// </para>
    /// <para>
    /// This method does not call <see cref="IOutboxStore.SaveChangesAsync"/>; the caller commits the
    /// batch, as <see cref="OutboxProcessorBase"/> does.
    /// </para>
    /// </remarks>
    public async Task<Either<EncinaError, int>> ProcessPendingMessagesAsync(
        Func<IOutboxMessage, Type, object, ValueTask<Either<EncinaError, Unit>>> publishCallback,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(publishCallback);

        var batchProcessor = new OutboxBatchProcessor(_store, _options, _logger, _messageSerializer, _timeProvider);
        var result = await batchProcessor.ProcessAsync(publishCallback, cancellationToken).ConfigureAwait(false);

        return result.Map(r => r.Succeeded);
    }

    /// <summary>
    /// Gets the number of messages waiting to be delivered: not processed and with retries left under
    /// <see cref="OutboxOptions.MaxRetries"/>, whether due now or scheduled for a later retry.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The count of pending messages, or an error.</returns>
    public Task<Either<EncinaError, int>> GetPendingCountAsync(CancellationToken cancellationToken = default)
        => _store.GetPendingCountAsync(_options.MaxRetries, cancellationToken);

    /// <summary>
    /// Gets the number of messages whose retries are exhausted under <see cref="OutboxOptions.MaxRetries"/>.
    /// </summary>
    /// <remarks>
    /// Exhausted messages stay in the outbox, unprocessed, and are not delivered again until they are
    /// requeued with <see cref="RequeueExhaustedAsync(CancellationToken)"/>. The count is also published by
    /// the <c>encina.outbox.messages_exhausted</c> gauge.
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The count of exhausted messages, or an error.</returns>
    public async Task<Either<EncinaError, int>> GetExhaustedCountAsync(CancellationToken cancellationToken = default)
    {
        var result = await _store.GetExhaustedCountAsync(_options.MaxRetries, cancellationToken).ConfigureAwait(false);
        result.IfRight(count => OutboxProcessorMetrics.Instance.SetExhaustedCount(count));
        return result;
    }

    /// <summary>
    /// Returns every exhausted message to the pending state, so that the processor delivers it again
    /// with a fresh retry budget.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of messages requeued, or an error.</returns>
    /// <remarks>
    /// The change is saved with <see cref="IOutboxStore.SaveChangesAsync"/>, logged with EventId 2959 and
    /// counted by <c>encina.outbox.messages_requeued_total</c>.
    /// </remarks>
    public Task<Either<EncinaError, int>> RequeueExhaustedAsync(CancellationToken cancellationToken = default)
        => RequeueExhaustedCoreAsync(null, cancellationToken);

    /// <summary>
    /// Returns the given exhausted messages to the pending state, so that the processor delivers them again
    /// with a fresh retry budget.
    /// </summary>
    /// <param name="messageIds">
    /// The identifiers of the messages to requeue. Identifiers of messages that are not exhausted
    /// (pending, processed or unknown) are ignored.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of messages requeued, or an error.</returns>
    /// <remarks>
    /// The change is saved with <see cref="IOutboxStore.SaveChangesAsync"/>, logged with EventId 2959 and
    /// counted by <c>encina.outbox.messages_requeued_total</c>.
    /// </remarks>
    public Task<Either<EncinaError, int>> RequeueExhaustedAsync(
        IEnumerable<Guid> messageIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messageIds);

        var ids = messageIds.Where(id => id != Guid.Empty).Distinct().ToArray();
        if (ids.Length == 0)
        {
            return Task.FromResult<Either<EncinaError, int>>(0);
        }

        return RequeueExhaustedCoreAsync(ids, cancellationToken);
    }

    private async Task<Either<EncinaError, int>> RequeueExhaustedCoreAsync(
        Guid[]? messageIds,
        CancellationToken cancellationToken)
    {
        var requeueResult = await _store.RequeueExhaustedAsync(_options.MaxRetries, messageIds, cancellationToken)
            .ConfigureAwait(false);
        if (requeueResult.IsLeft)
        {
            return requeueResult;
        }

        var requeued = requeueResult.RightToArray()[0];

        var saveResult = await _store.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        if (saveResult.IsLeft)
        {
            return saveResult.LeftToArray()[0];
        }

        MessagingLog.OutboxExhaustedMessagesRequeued(
            _logger,
            requeued,
            messageIds is null ? "all" : string.Create(CultureInfo.InvariantCulture, $"ids:{messageIds.Length}"));
        OutboxProcessorMetrics.Instance.RecordRequeued(requeued);

        return requeued;
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

    /// <summary>
    /// Failed to add message to outbox.
    /// </summary>
    public const string AddFailed = "outbox.add_failed";
}

/// <summary>
/// LoggerMessage definitions for high-performance logging.
/// </summary>
[ExcludeFromCodeCoverage]
internal static partial class Log
{
    [LoggerMessage(
        EventId = 2837,
        Level = LogLevel.Debug,
        Message = "Message {MessageId} added to outbox (type: {NotificationType})")]
    public static partial void MessageAddedToOutbox(ILogger logger, Guid messageId, string notificationType);

    // EventIds 2838-2841 (per-message processing) were retired when message processing moved to
    // OutboxBatchProcessor, which logs through MessagingLog (2830-2832, 2958).
}
