using Encina.Messaging.Diagnostics;
using Encina.Messaging.Serialization;
using LanguageExt;
using Microsoft.Extensions.Logging;

namespace Encina.Messaging.Outbox;

/// <summary>
/// The single implementation of one outbox processing cycle, shared by
/// <see cref="OutboxProcessorBase"/> (every provider's background processor) and
/// <see cref="OutboxOrchestrator.ProcessPendingMessagesAsync"/>.
/// </summary>
/// <remarks>
/// <para>
/// For each pending message it resolves the notification type, deserializes the payload and
/// invokes the publish callback. A message is marked processed only when the callback returns
/// <c>Right</c>. A <c>Left</c>, a thrown exception, an unknown type or an undeserializable
/// payload all follow the same failure path: <see cref="IOutboxStore.MarkAsFailedAsync"/> with
/// the next retry time computed by <see cref="OutboxRetryBackoff"/> from the message's current
/// <see cref="IOutboxMessage.RetryCount"/>.
/// </para>
/// <para>
/// When the failure brings the retry count to <see cref="OutboxOptions.MaxRetries"/>, the message
/// is recorded with no next retry, logged with <see cref="OutboxErrorCodes.MaxRetriesExceeded"/>
/// and counted with the <c>exhausted</c> outcome; the stores never fetch it again.
/// </para>
/// </remarks>
internal sealed class OutboxBatchProcessor
{
    private static readonly IMessageSerializer DefaultSerializer = new JsonMessageSerializer();

    private readonly IOutboxStore _store;
    private readonly OutboxOptions _options;
    private readonly ILogger _logger;
    private readonly IMessageSerializer _serializer;
    private readonly TimeProvider _timeProvider;
    private readonly Func<double> _jitterSource;

    internal OutboxBatchProcessor(
        IOutboxStore store,
        OutboxOptions options,
        ILogger logger,
        IMessageSerializer? serializer,
        TimeProvider timeProvider,
        Func<double>? jitterSource = null)
    {
        _store = store;
        _options = options;
        _logger = logger;
        _serializer = serializer ?? DefaultSerializer;
        _timeProvider = timeProvider;
        _jitterSource = jitterSource ?? Random.Shared.NextDouble;
    }

    /// <summary>
    /// Fetches one batch of pending messages and delivers each of them.
    /// </summary>
    /// <param name="publish">Publishes a deserialized notification; <c>Left</c> means the delivery failed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The outcome counts of the batch, or the store error that prevented fetching it.</returns>
    internal async Task<Either<EncinaError, OutboxBatchResult>> ProcessAsync(
        Func<IOutboxMessage, Type, object, ValueTask<Either<EncinaError, Unit>>> publish,
        CancellationToken cancellationToken)
    {
        var messagesResult = await _store.GetPendingMessagesAsync(
            _options.BatchSize,
            _options.MaxRetries,
            cancellationToken).ConfigureAwait(false);

        if (messagesResult.IsLeft)
        {
            return messagesResult.LeftToArray()[0];
        }

        var messages = messagesResult.Match(
            Right: m => m.ToList(),
            Left: _ => []);

        if (messages.Count == 0)
        {
            return default(OutboxBatchResult);
        }

        MessagingLog.ProcessingPendingOutboxMessages(_logger, messages.Count);

        var succeeded = 0;
        var failed = 0;
        var exhausted = 0;

        foreach (var message in messages)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            var outcome = await ProcessMessageAsync(message, publish, cancellationToken).ConfigureAwait(false);
            OutboxProcessorMetrics.Instance.RecordOutcome(outcome);

            switch (outcome)
            {
                case OutboxProcessorMetrics.OutcomeSuccess:
                    succeeded++;
                    break;
                case OutboxProcessorMetrics.OutcomeExhausted:
                    exhausted++;
                    break;
                default:
                    failed++;
                    break;
            }
        }

        return new OutboxBatchResult(succeeded, failed, exhausted);
    }

    private async Task<string> ProcessMessageAsync(
        IOutboxMessage message,
        Func<IOutboxMessage, Type, object, ValueTask<Either<EncinaError, Unit>>> publish,
        CancellationToken cancellationToken)
    {
        try
        {
            var notificationType = Type.GetType(message.NotificationType);
            if (notificationType is null)
            {
                return await FailAsync(
                    message,
                    $"Unknown notification type: {message.NotificationType}",
                    exception: null,
                    cancellationToken).ConfigureAwait(false);
            }

            var notification = _serializer.Deserialize(message.Content, notificationType);
            if (notification is null)
            {
                return await FailAsync(
                    message,
                    "Failed to deserialize notification",
                    exception: null,
                    cancellationToken).ConfigureAwait(false);
            }

            var publishResult = await publish(message, notificationType, notification).ConfigureAwait(false);
            if (publishResult.IsLeft)
            {
                var error = publishResult.LeftToArray()[0];
                return await FailAsync(
                    message,
                    error.Message,
                    error.Exception.MatchUnsafe(ex => ex, () => null),
                    cancellationToken).ConfigureAwait(false);
            }

            await _store.MarkAsProcessedAsync(message.Id, cancellationToken).ConfigureAwait(false);
            MessagingLog.ProcessedOutboxMessage(_logger, message.Id, message.NotificationType);
            return OutboxProcessorMetrics.OutcomeSuccess;
        }
        catch (Exception ex)
        {
            return await FailAsync(message, ex.Message, ex, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<string> FailAsync(
        IOutboxMessage message,
        string errorMessage,
        Exception? exception,
        CancellationToken cancellationToken)
    {
        var retryCount = message.RetryCount + 1;

        if (retryCount >= _options.MaxRetries)
        {
            await _store.MarkAsFailedAsync(message.Id, errorMessage, nextRetryAtUtc: null, cancellationToken)
                .ConfigureAwait(false);

            MessagingLog.OutboxMessageRetriesExhausted(
                _logger,
                exception,
                message.Id,
                message.NotificationType,
                retryCount,
                OutboxErrorCodes.MaxRetriesExceeded,
                errorMessage);

            return OutboxProcessorMetrics.OutcomeExhausted;
        }

        var delay = OutboxRetryBackoff.ComputeDelay(
            message.RetryCount,
            _options.BaseRetryDelay,
            _options.MaxRetryDelay,
            _options.RetryJitterRatio,
            _jitterSource());
        var nextRetryAtUtc = _timeProvider.GetUtcNow().UtcDateTime.Add(delay);

        await _store.MarkAsFailedAsync(message.Id, errorMessage, nextRetryAtUtc, cancellationToken)
            .ConfigureAwait(false);

        MessagingLog.FailedToProcessOutboxMessage(
            _logger,
            exception,
            message.Id,
            errorMessage,
            retryCount,
            _options.MaxRetries,
            nextRetryAtUtc);

        return OutboxProcessorMetrics.OutcomeFailure;
    }
}

/// <summary>
/// Outcome counts of one outbox processing cycle.
/// </summary>
/// <param name="Succeeded">Messages delivered and marked processed.</param>
/// <param name="Failed">Messages that failed and were scheduled for a retry.</param>
/// <param name="Exhausted">Messages whose failure used up their retries.</param>
internal readonly record struct OutboxBatchResult(int Succeeded, int Failed, int Exhausted)
{
    /// <summary>Gets the number of messages handled in the cycle.</summary>
    public int Total => Succeeded + Failed + Exhausted;
}
