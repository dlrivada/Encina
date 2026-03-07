using System.Diagnostics;
using Encina.Messaging.Inbox;
using LanguageExt;

namespace Encina.OpenTelemetry.MessagingStores;

/// <summary>
/// Decorator that adds OpenTelemetry distributed tracing to any <see cref="IInboxStore"/> implementation.
/// </summary>
/// <remarks>
/// <para>
/// Wraps the inner store and creates <see cref="Activity"/> spans for state-changing operations
/// (add, get message, mark processed, mark failed, increment retry, remove expired).
/// All activity creation is guarded by <see cref="ActivitySource.HasListeners()"/> for zero-cost
/// when no trace collector is configured.
/// </para>
/// <para>
/// The activity source name <c>"Encina.Messaging.Inbox"</c> must be registered with the
/// OpenTelemetry tracer, which is done automatically by
/// <see cref="ServiceCollectionExtensions.WithEncina"/>.
/// </para>
/// </remarks>
internal sealed class InstrumentedInboxStore : IInboxStore
{
    private static readonly ActivitySource Source = new("Encina.Messaging.Inbox", "1.0");

    private readonly IInboxStore _inner;

    /// <summary>
    /// Initializes a new instance of the <see cref="InstrumentedInboxStore"/> class.
    /// </summary>
    /// <param name="inner">The inner inbox store to decorate.</param>
    public InstrumentedInboxStore(IInboxStore inner)
    {
        ArgumentNullException.ThrowIfNull(inner);
        _inner = inner;
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Option<IInboxMessage>>> GetMessageAsync(string messageId, CancellationToken cancellationToken = default)
    {
        using var activity = StartDuplicateCheck(messageId);
        var result = await _inner.GetMessageAsync(messageId, cancellationToken).ConfigureAwait(false);
        result.IfRight(opt => opt.Match(
            Some: _ => CompleteDuplicateFound(activity),
            None: () => Complete(activity)));
        result.IfLeft(err => Failed(activity, err.Message));
        return result;
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> AddAsync(IInboxMessage message, CancellationToken cancellationToken = default)
    {
        using var activity = StartReceive(message.RequestType, message.MessageId);
        var result = await _inner.AddAsync(message, cancellationToken).ConfigureAwait(false);
        result.IfRight(_ => Complete(activity));
        result.IfLeft(err => Failed(activity, err.Message));
        return result;
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> MarkAsProcessedAsync(
        string messageId,
        string response,
        CancellationToken cancellationToken = default)
    {
        using var activity = StartMarkProcessed(messageId);
        var result = await _inner.MarkAsProcessedAsync(messageId, response, cancellationToken).ConfigureAwait(false);
        result.IfRight(_ => Complete(activity));
        result.IfLeft(err => Failed(activity, err.Message));
        return result;
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> MarkAsFailedAsync(
        string messageId,
        string errorMessage,
        DateTime? nextRetryAtUtc,
        CancellationToken cancellationToken = default)
    {
        using var activity = StartMarkFailed(messageId);
        var result = await _inner.MarkAsFailedAsync(messageId, errorMessage, nextRetryAtUtc, cancellationToken)
            .ConfigureAwait(false);
        result.IfRight(_ => Complete(activity));
        result.IfLeft(err => Failed(activity, err.Message));
        return result;
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> IncrementRetryCountAsync(string messageId, CancellationToken cancellationToken = default)
    {
        using var activity = StartIncrementRetry(messageId);
        var result = await _inner.IncrementRetryCountAsync(messageId, cancellationToken).ConfigureAwait(false);
        result.IfRight(_ => Complete(activity));
        result.IfLeft(err => Failed(activity, err.Message));
        return result;
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, IEnumerable<IInboxMessage>>> GetExpiredMessagesAsync(
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        using var activity = StartGetExpired(batchSize);
        var result = await _inner.GetExpiredMessagesAsync(batchSize, cancellationToken)
            .ConfigureAwait(false);
        result.IfRight(messages =>
        {
            var count = messages is ICollection<IInboxMessage> col ? col.Count : messages.Count();
            CompleteBatch(activity, count);
        });
        result.IfLeft(err => Failed(activity, err.Message));
        return result;
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> RemoveExpiredMessagesAsync(
        IEnumerable<string> messageIds,
        CancellationToken cancellationToken = default)
    {
        using var activity = StartRemoveExpired();
        var result = await _inner.RemoveExpiredMessagesAsync(messageIds, cancellationToken).ConfigureAwait(false);
        result.IfRight(_ => Complete(activity));
        result.IfLeft(err => Failed(activity, err.Message));
        return result;
    }

    /// <inheritdoc />
    public Task<Either<EncinaError, Unit>> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _inner.SaveChangesAsync(cancellationToken);

    private static Activity? StartReceive(string messageType, string messageId)
    {
        if (!Source.HasListeners())
        {
            return null;
        }

        var activity = Source.StartActivity("encina.inbox.receive", ActivityKind.Internal);
        activity?.SetTag("inbox.message_type", messageType);
        activity?.SetTag("inbox.message_id", messageId);
        return activity;
    }

    private static Activity? StartDuplicateCheck(string messageId)
    {
        if (!Source.HasListeners())
        {
            return null;
        }

        var activity = Source.StartActivity("encina.inbox.duplicate_check", ActivityKind.Internal);
        activity?.SetTag("inbox.message_id", messageId);
        return activity;
    }

    private static Activity? StartMarkProcessed(string messageId)
    {
        if (!Source.HasListeners())
        {
            return null;
        }

        var activity = Source.StartActivity("encina.inbox.mark_processed", ActivityKind.Internal);
        activity?.SetTag("inbox.message_id", messageId);
        return activity;
    }

    private static Activity? StartMarkFailed(string messageId)
    {
        if (!Source.HasListeners())
        {
            return null;
        }

        var activity = Source.StartActivity("encina.inbox.mark_failed", ActivityKind.Internal);
        activity?.SetTag("inbox.message_id", messageId);
        return activity;
    }

    private static Activity? StartIncrementRetry(string messageId)
    {
        if (!Source.HasListeners())
        {
            return null;
        }

        var activity = Source.StartActivity("encina.inbox.increment_retry", ActivityKind.Internal);
        activity?.SetTag("inbox.message_id", messageId);
        return activity;
    }

    private static Activity? StartGetExpired(int batchSize)
    {
        if (!Source.HasListeners())
        {
            return null;
        }

        var activity = Source.StartActivity("encina.inbox.get_expired", ActivityKind.Internal);
        activity?.SetTag("inbox.batch_size", batchSize);
        return activity;
    }

    private static Activity? StartRemoveExpired()
    {
        if (!Source.HasListeners())
        {
            return null;
        }

        return Source.StartActivity("encina.inbox.remove_expired", ActivityKind.Internal);
    }

    private static void Complete(Activity? activity)
    {
        activity?.SetStatus(ActivityStatusCode.Ok);
    }

    private static void CompleteDuplicateFound(Activity? activity)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetTag("inbox.duplicate", true);
        activity.SetStatus(ActivityStatusCode.Ok);
    }

    private static void CompleteBatch(Activity? activity, int count)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetTag("inbox.expired_count", count);
        activity.SetStatus(ActivityStatusCode.Ok);
    }

    private static void Failed(Activity? activity, string? errorMessage)
    {
        activity?.SetStatus(ActivityStatusCode.Error, errorMessage);
    }
}
