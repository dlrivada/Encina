using Encina.Diagnostics;
using Encina.Messaging.DeadLetter;
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
/// With the dead letter queue registered and <c>DeadLetterOptions.IntegrateWithOutbox</c> on, the exhausted
/// message is captured first (stored type name and content, keyed by the message id). When that capture
/// fails, the exhausted state is not recorded: the message is logged with EventId 2961 (operation
/// <c>DeadLetterCapture</c>), counted as a store error and delivered again in a later cycle.
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

    // The operation named by EventId 2961 when the dead letter capture of an exhausted message fails.
    private const string DeadLetterCaptureOperation = "DeadLetterCapture";

    private readonly IOutboxStore _store;
    private readonly OutboxOptions _options;
    private readonly ILogger _logger;
    private readonly IMessageSerializer _serializer;
    private readonly TimeProvider _timeProvider;
    private readonly Func<double> _jitterSource;
    private readonly DeadLetterSourceCapture? _deadLetterCapture;

    internal OutboxBatchProcessor(
        IOutboxStore store,
        OutboxOptions options,
        ILogger logger,
        IMessageSerializer? serializer,
        TimeProvider timeProvider,
        Func<double>? jitterSource = null,
        DeadLetterSourceCapture? deadLetterCapture = null)
    {
        _store = store;
        _options = options;
        _logger = logger;
        _serializer = serializer ?? DefaultSerializer;
        _timeProvider = timeProvider;
        _jitterSource = jitterSource ?? Random.Shared.NextDouble;
        _deadLetterCapture = deadLetterCapture;
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
                    new Failure($"Unknown notification type: {message.NotificationType}", OutboxErrorCodes.UnknownNotificationType),
                    cancellationToken).ConfigureAwait(false);
            }

            var notification = _serializer.Deserialize(message.Content, notificationType);
            if (notification is null)
            {
                return await FailAsync(
                    message,
                    new Failure("Failed to deserialize notification", OutboxErrorCodes.DeserializationFailed),
                    cancellationToken).ConfigureAwait(false);
            }

            var publishResult = await publish(message, notificationType, notification).ConfigureAwait(false);
            if (publishResult.IsLeft)
            {
                return await PublishFailedAsync(message, publishResult.LeftToArray()[0], cancellationToken).ConfigureAwait(false);
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
            // The exception message may carry personal data; store the exception type only.
            return await FailAsync(
                message,
                new Failure(ex.GetType().FullName ?? ex.GetType().Name, OutboxErrorCodes.PublishFailed, ex),
                cancellationToken).ConfigureAwait(false);
        }
    }

    // A Left from the publish callback: a cancellation stops the batch; anything else is a failed delivery.
    private async Task<MessageOutcome> PublishFailedAsync(IOutboxMessage message, EncinaError error, CancellationToken cancellationToken)
    {
        if (IsCancellation(error, cancellationToken))
        {
            return MessageOutcome.Cancelled;
        }

        // Only the error code is stored and logged: EncinaError.Message can carry personal data such as a
        // data-subject id (#1259 review). No exception is attached for a Left: the error's exception carries
        // EncinaError.Message (EncinaErrors.FromException copies the cause's message).
        var errorCode = error.GetCode().IfNone("encina.unknown");
        return await FailAsync(message, new Failure(errorCode, errorCode), cancellationToken).ConfigureAwait(false);
    }

    // Reason is what is stored in ErrorMessage and logged: the EncinaError code, the exception type, or a
    // fixed description, never EncinaError.Message. ErrorCode is what a dead letter records.
    private readonly record struct Failure(string Reason, string ErrorCode, Exception? Exception = null);

    private async Task<MessageOutcome> FailAsync(
        IOutboxMessage message,
        Failure failure,
        CancellationToken cancellationToken)
    {
        var retryCount = message.RetryCount + 1;
        var failureReason = failure.Reason;
        var exception = failure.Exception;

        if (retryCount >= _options.MaxRetries)
        {
            return await ExhaustAsync(message, failure, retryCount, cancellationToken).ConfigureAwait(false);
        }

        var delay = OutboxRetryBackoff.ComputeDelay(
            message.RetryCount,
            _options.BaseRetryDelay,
            _options.MaxRetryDelay,
            _options.RetryJitterRatio,
            _jitterSource());
        var nextRetryAtUtc = AddSaturating(_timeProvider.GetUtcNow().UtcDateTime, delay);

        var failedMark = await _store.MarkAsFailedAsync(message.Id, failureReason, nextRetryAtUtc, cancellationToken)
            .ConfigureAwait(false);
        if (failedMark.IsLeft)
        {
            return OutcomeNotRecorded(nameof(IOutboxStore.MarkAsFailedAsync), message, failedMark);
        }

        MessagingLog.FailedToProcessOutboxMessage(
            _logger,
            exception?.ForLogging(),
            message.Id,
            failureReason,
            retryCount,
            _options.MaxRetries,
            nextRetryAtUtc);

        return MessageOutcome.Failure;
    }

    // The failure that uses up MaxRetries. The dead letter is captured before the exhausted state is recorded:
    // when the capture fails the message keeps its state and is delivered (and captured) again, instead of
    // ending exhausted without its dead letter. The capture is idempotent on the message id.
    private async Task<MessageOutcome> ExhaustAsync(
        IOutboxMessage message,
        Failure failure,
        int retryCount,
        CancellationToken cancellationToken)
    {
        var captured = await CaptureDeadLetterAsync(message, failure, retryCount, cancellationToken).ConfigureAwait(false);
        if (captured.IsLeft)
        {
            return OutcomeNotRecorded(DeadLetterCaptureOperation, message, captured);
        }

        var exhaustedMark = await _store.MarkAsFailedAsync(message.Id, failure.Reason, nextRetryAtUtc: null, cancellationToken)
            .ConfigureAwait(false);
        if (exhaustedMark.IsLeft)
        {
            return OutcomeNotRecorded(nameof(IOutboxStore.MarkAsFailedAsync), message, exhaustedMark);
        }

        MessagingLog.OutboxMessageRetriesExhausted(
            _logger,
            failure.Exception?.ForLogging(),
            message.Id,
            message.NotificationType,
            retryCount,
            OutboxErrorCodes.MaxRetriesExceeded,
            failure.Reason);

        return MessageOutcome.Exhausted;
    }

    private Task<Either<EncinaError, Unit>> CaptureDeadLetterAsync(
        IOutboxMessage message,
        Failure failure,
        int retryCount,
        CancellationToken cancellationToken)
    {
        if (_deadLetterCapture is null)
            return Task.FromResult<Either<EncinaError, Unit>>(Unit.Default);

        // The stored content is copied as is (IMessageSerializer wrote it, so the replay reads it back).
        return _deadLetterCapture.CaptureSerializedAsync(
            message.NotificationType,
            message.Content,
            new DeadLetterContext(
                EncinaErrors.Create(failure.ErrorCode, "Outbox delivery failed"),
                failure.Exception,
                DeadLetterSourcePatterns.Outbox,
                retryCount,
                DeadLetterInputs.AsUtc(message.CreatedAtUtc),
                SourceMessageId: message.Id.ToString("D")),
            cancellationToken);
    }

    private MessageOutcome OutcomeNotRecorded(string operation, IOutboxMessage message, Either<EncinaError, Unit> result)
    {
        var error = result.LeftToArray()[0];
        MessagingLog.OutboxMessageOutcomeNotRecorded(_logger, operation, message.Id, error.GetCode().IfNone("encina.unknown"));
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
