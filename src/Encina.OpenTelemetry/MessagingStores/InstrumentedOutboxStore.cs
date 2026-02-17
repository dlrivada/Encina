using System.Diagnostics;
using Encina.Messaging.Outbox;

namespace Encina.OpenTelemetry.MessagingStores;

/// <summary>
/// Decorator that adds OpenTelemetry distributed tracing to any <see cref="IOutboxStore"/> implementation.
/// </summary>
/// <remarks>
/// <para>
/// Wraps the inner store and creates <see cref="Activity"/> spans for state-changing operations
/// (add, get pending, mark processed, mark failed). All activity creation is guarded by
/// <see cref="ActivitySource.HasListeners()"/> for zero-cost when no trace collector is configured.
/// </para>
/// <para>
/// The activity source name <c>"Encina.Messaging.Outbox"</c> must be registered with the
/// OpenTelemetry tracer, which is done automatically by
/// <see cref="ServiceCollectionExtensions.WithEncina"/>.
/// </para>
/// </remarks>
internal sealed class InstrumentedOutboxStore : IOutboxStore
{
    private static readonly ActivitySource Source = new("Encina.Messaging.Outbox", "1.0");

    private readonly IOutboxStore _inner;

    /// <summary>
    /// Initializes a new instance of the <see cref="InstrumentedOutboxStore"/> class.
    /// </summary>
    /// <param name="inner">The inner outbox store to decorate.</param>
    public InstrumentedOutboxStore(IOutboxStore inner)
    {
        ArgumentNullException.ThrowIfNull(inner);
        _inner = inner;
    }

    /// <inheritdoc />
    public async Task AddAsync(IOutboxMessage message, CancellationToken cancellationToken = default)
    {
        using var activity = StartAdd(message.NotificationType, message.Id.ToString());

        try
        {
            await _inner.AddAsync(message, cancellationToken).ConfigureAwait(false);
            Complete(activity);
        }
        catch (Exception ex)
        {
            Failed(activity, ex.Message);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<IOutboxMessage>> GetPendingMessagesAsync(
        int batchSize,
        int maxRetries,
        CancellationToken cancellationToken = default)
    {
        using var activity = StartProcessBatch(batchSize);

        try
        {
            var messages = await _inner.GetPendingMessagesAsync(batchSize, maxRetries, cancellationToken)
                .ConfigureAwait(false);

            var messageList = messages as IList<IOutboxMessage> ?? messages.ToList();
            CompleteBatch(activity, messageList.Count);
            return messageList;
        }
        catch (Exception ex)
        {
            Failed(activity, ex.Message);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        using var activity = StartMarkProcessed(messageId.ToString());

        try
        {
            await _inner.MarkAsProcessedAsync(messageId, cancellationToken).ConfigureAwait(false);
            Complete(activity);
        }
        catch (Exception ex)
        {
            Failed(activity, ex.Message);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task MarkAsFailedAsync(
        Guid messageId,
        string errorMessage,
        DateTime? nextRetryAtUtc,
        CancellationToken cancellationToken = default)
    {
        using var activity = StartMarkFailed(messageId.ToString());

        try
        {
            await _inner.MarkAsFailedAsync(messageId, errorMessage, nextRetryAtUtc, cancellationToken)
                .ConfigureAwait(false);
            Complete(activity);
        }
        catch (Exception ex)
        {
            Failed(activity, ex.Message);
            throw;
        }
    }

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _inner.SaveChangesAsync(cancellationToken);

    #region Activity Helpers

    private static Activity? StartAdd(string messageType, string messageId)
    {
        if (!Source.HasListeners())
        {
            return null;
        }

        var activity = Source.StartActivity("encina.outbox.add", ActivityKind.Internal);
        activity?.SetTag("outbox.message_type", messageType);
        activity?.SetTag("outbox.message_id", messageId);
        return activity;
    }

    private static Activity? StartProcessBatch(int batchSize)
    {
        if (!Source.HasListeners())
        {
            return null;
        }

        var activity = Source.StartActivity("encina.outbox.process_batch", ActivityKind.Internal);
        activity?.SetTag("outbox.batch_size", batchSize);
        return activity;
    }

    private static Activity? StartMarkProcessed(string messageId)
    {
        if (!Source.HasListeners())
        {
            return null;
        }

        var activity = Source.StartActivity("encina.outbox.mark_processed", ActivityKind.Internal);
        activity?.SetTag("outbox.message_id", messageId);
        return activity;
    }

    private static Activity? StartMarkFailed(string messageId)
    {
        if (!Source.HasListeners())
        {
            return null;
        }

        var activity = Source.StartActivity("encina.outbox.mark_failed", ActivityKind.Internal);
        activity?.SetTag("outbox.message_id", messageId);
        return activity;
    }

    private static void Complete(Activity? activity)
    {
        activity?.SetStatus(ActivityStatusCode.Ok);
    }

    private static void CompleteBatch(Activity? activity, int processedCount)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetTag("outbox.processed_count", processedCount);
        activity.SetStatus(ActivityStatusCode.Ok);
    }

    private static void Failed(Activity? activity, string? errorMessage)
    {
        activity?.SetStatus(ActivityStatusCode.Error, errorMessage);
    }

    #endregion
}
