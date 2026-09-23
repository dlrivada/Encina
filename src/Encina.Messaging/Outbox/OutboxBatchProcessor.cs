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
/// <para>
/// Every store call is checked. When <see cref="IOutboxStore.MarkAsProcessedAsync"/> or
/// <see cref="IOutboxStore.MarkAsFailedAsync"/> returns <c>Left</c>, the message is logged with EventId
/// 2961 and counted as a store error, never as delivered or exhausted.
/// </para>
/// <para>
/// Cancellation is not a delivery failure. When the cycle's token is cancelled, or the dispatcher
/// returns <see cref="EncinaErrorCodes.NotificationCancelled"/>, the batch stops (EventId 2962) without
/// marking the interrupted message failed, so no retry is consumed.
/// </para>
/// <para>
/// Metrics are not recorded here: the caller records the returned <see cref="OutboxBatchResult"/>
/// once it knows whether the batch was saved.
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
        var storeErrors = 0;

        foreach (var message in messages)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            var outcome = await ProcessMessageAsync(message, publish, cancellationToken).ConfigureAwait(false);
            if (outcome == MessageOutcome.Cancelled)
            {
                MessagingLog.OutboxBatchCancelled(_logger, message.Id);
                break;
            }

            switch (outcome)
            {
                case MessageOutcome.Success:
                    succeeded++;
                    break;
                case MessageOutcome.Exhausted:
                    exhausted++;
                    break;
                case MessageOutcome.StoreError:
                    storeErrors++;
                    break;
                default:
                    failed++;
                    break;
            }
        }

        return new OutboxBatchResult(succeeded, failed, exhausted, storeErrors);
    }

    /// <summary>
    /// Returns <see langword="true"/> when a failed delivery is a cancellation rather than a delivery
    /// failure: the cycle's token was cancelled, or the dispatcher reported
    /// <see cref="EncinaErrorCodes.NotificationCancelled"/>.
    /// </summary>
    private static bool IsCancellation(EncinaError error, CancellationToken cancellationToken)
        => cancellationToken.IsCancellationRequested
           || error.GetCode().Match(
               Some: code => string.Equals(code, EncinaErrorCodes.NotificationCancelled, StringComparison.Ordinal),
               None: () => false);

    private async Task<MessageOutcome> ProcessMessageAsync(
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
                if (IsCancellation(error, cancellationToken))
                {
                    return MessageOutcome.Cancelled;
                }

                return await FailAsync(
                    message,
                    error.Message,
                    error.Exception.MatchUnsafe(ex => ex, () => null),
                    cancellationToken).ConfigureAwait(false);
            }

            var marked = await _store.MarkAsProcessedAsync(message.Id, cancellationToken).ConfigureAwait(false);
            if (marked.IsLeft)
            {
                return OutcomeNotRecorded(nameof(IOutboxStore.MarkAsProcessedAsync), message, marked);
            }

            MessagingLog.ProcessedOutboxMessage(_logger, message.Id, message.NotificationType);
            return MessageOutcome.Success;
        }
        catch (Exception) when (cancellationToken.IsCancellationRequested)
        {
            // Stopping the host is not a delivery failure: the message keeps its retry budget.
            return MessageOutcome.Cancelled;
        }
        catch (Exception ex)
        {
            return await FailAsync(message, ex.Message, ex, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<MessageOutcome> FailAsync(
        IOutboxMessage message,
        string errorMessage,
        Exception? exception,
        CancellationToken cancellationToken)
    {
        var retryCount = message.RetryCount + 1;

        if (retryCount >= _options.MaxRetries)
        {
            var exhaustedMark = await _store.MarkAsFailedAsync(message.Id, errorMessage, nextRetryAtUtc: null, cancellationToken)
                .ConfigureAwait(false);
            if (exhaustedMark.IsLeft)
            {
                return OutcomeNotRecorded(nameof(IOutboxStore.MarkAsFailedAsync), message, exhaustedMark);
            }

            MessagingLog.OutboxMessageRetriesExhausted(
                _logger,
                exception,
                message.Id,
                message.NotificationType,
                retryCount,
                OutboxErrorCodes.MaxRetriesExceeded,
                errorMessage);

            return MessageOutcome.Exhausted;
        }

        var delay = OutboxRetryBackoff.ComputeDelay(
            message.RetryCount,
            _options.BaseRetryDelay,
            _options.MaxRetryDelay,
            _options.RetryJitterRatio,
            _jitterSource());
        var nextRetryAtUtc = AddSaturating(_timeProvider.GetUtcNow().UtcDateTime, delay);

        var failedMark = await _store.MarkAsFailedAsync(message.Id, errorMessage, nextRetryAtUtc, cancellationToken)
            .ConfigureAwait(false);
        if (failedMark.IsLeft)
        {
            return OutcomeNotRecorded(nameof(IOutboxStore.MarkAsFailedAsync), message, failedMark);
        }

        MessagingLog.FailedToProcessOutboxMessage(
            _logger,
            exception,
            message.Id,
            errorMessage,
            retryCount,
            _options.MaxRetries,
            nextRetryAtUtc);

        return MessageOutcome.Failure;
    }

    private MessageOutcome OutcomeNotRecorded(string operation, IOutboxMessage message, Either<EncinaError, Unit> result)
    {
        var error = result.LeftToArray()[0];
        MessagingLog.OutboxMessageOutcomeNotRecorded(_logger, operation, message.Id, error.Message);
        return MessageOutcome.StoreError;
    }

    /// <summary>
    /// Adds <paramref name="delay"/> to <paramref name="nowUtc"/>, saturating at <see cref="DateTime.MaxValue"/>
    /// instead of throwing when a very large <see cref="OutboxOptions.MaxRetryDelay"/> would overflow.
    /// </summary>
    internal static DateTime AddSaturating(DateTime nowUtc, TimeSpan delay)
    {
        var maxUtc = DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc);
        return delay >= maxUtc - nowUtc ? maxUtc : nowUtc.Add(delay);
    }

    private enum MessageOutcome
    {
        Success,
        Failure,
        Exhausted,
        StoreError,
        Cancelled
    }
}

/// <summary>
/// Outcome counts of one outbox processing cycle.
/// </summary>
/// <param name="Succeeded">Messages delivered and marked processed.</param>
/// <param name="Failed">Messages that failed and were scheduled for a retry.</param>
/// <param name="Exhausted">Messages whose failure used up their retries and whose exhausted state was recorded.</param>
/// <param name="StoreErrors">
/// Messages whose outcome the store failed to record (<see cref="IOutboxStore.MarkAsProcessedAsync"/> or
/// <see cref="IOutboxStore.MarkAsFailedAsync"/> returned <c>Left</c>); they keep their previous state.
/// </param>
/// <remarks>A message interrupted by cancellation is not counted in any outcome.</remarks>
internal readonly record struct OutboxBatchResult(int Succeeded, int Failed, int Exhausted, int StoreErrors)
{
    /// <summary>Gets the number of messages handled in the cycle.</summary>
    public int Total => Succeeded + Failed + Exhausted + StoreErrors;

    /// <summary>Gets the number of messages that were not delivered or whose outcome was not recorded.</summary>
    public int NotDelivered => Failed + Exhausted + StoreErrors;
}
