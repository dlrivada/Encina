using System.Diagnostics;
using Encina.Messaging.Scheduling;

namespace Encina.OpenTelemetry.MessagingStores;

/// <summary>
/// Decorator that adds OpenTelemetry distributed tracing to any <see cref="IScheduledMessageStore"/> implementation.
/// </summary>
/// <remarks>
/// <para>
/// Wraps the inner store and creates <see cref="Activity"/> spans for state-changing operations
/// (add, get due, mark processed, mark failed, reschedule, cancel). All activity creation is
/// guarded by <see cref="ActivitySource.HasListeners()"/> for zero-cost when no trace collector
/// is configured.
/// </para>
/// <para>
/// The activity source name <c>"Encina.Messaging.Scheduling"</c> must be registered with the
/// OpenTelemetry tracer, which is done automatically by
/// <see cref="ServiceCollectionExtensions.WithEncina"/>.
/// </para>
/// </remarks>
internal sealed class InstrumentedScheduledMessageStore : IScheduledMessageStore
{
    private static readonly ActivitySource Source = new("Encina.Messaging.Scheduling", "1.0");

    private readonly IScheduledMessageStore _inner;

    /// <summary>
    /// Initializes a new instance of the <see cref="InstrumentedScheduledMessageStore"/> class.
    /// </summary>
    /// <param name="inner">The inner scheduled message store to decorate.</param>
    public InstrumentedScheduledMessageStore(IScheduledMessageStore inner)
    {
        ArgumentNullException.ThrowIfNull(inner);
        _inner = inner;
    }

    /// <inheritdoc />
    public async Task AddAsync(IScheduledMessage message, CancellationToken cancellationToken = default)
    {
        using var activity = StartSchedule(message.RequestType, message.Id, message.ScheduledAtUtc);

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
    public async Task<IEnumerable<IScheduledMessage>> GetDueMessagesAsync(
        int batchSize,
        int maxRetries,
        CancellationToken cancellationToken = default)
    {
        using var activity = StartProcessDue(batchSize);

        try
        {
            var messages = await _inner.GetDueMessagesAsync(batchSize, maxRetries, cancellationToken)
                .ConfigureAwait(false);

            var messageList = messages as IList<IScheduledMessage> ?? messages.ToList();
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
        using var activity = StartExecute(messageId);

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
        using var activity = StartMarkFailed(messageId);

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
    public async Task RescheduleRecurringMessageAsync(
        Guid messageId,
        DateTime nextScheduledAtUtc,
        CancellationToken cancellationToken = default)
    {
        using var activity = StartReschedule(messageId, nextScheduledAtUtc);

        try
        {
            await _inner.RescheduleRecurringMessageAsync(messageId, nextScheduledAtUtc, cancellationToken)
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
    public async Task CancelAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        using var activity = StartCancel(messageId);

        try
        {
            await _inner.CancelAsync(messageId, cancellationToken).ConfigureAwait(false);
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

    private static Activity? StartSchedule(string messageType, Guid messageId, DateTime scheduledAtUtc)
    {
        if (!Source.HasListeners())
        {
            return null;
        }

        var activity = Source.StartActivity("encina.scheduling.schedule", ActivityKind.Internal);
        activity?.SetTag("scheduling.message_type", messageType);
        activity?.SetTag("scheduling.message_id", messageId.ToString());
        activity?.SetTag("scheduling.scheduled_at", scheduledAtUtc.ToString("O"));
        return activity;
    }

    private static Activity? StartProcessDue(int batchSize)
    {
        if (!Source.HasListeners())
        {
            return null;
        }

        var activity = Source.StartActivity("encina.scheduling.process_due", ActivityKind.Internal);
        activity?.SetTag("scheduling.batch_size", batchSize);
        return activity;
    }

    private static Activity? StartExecute(Guid messageId)
    {
        if (!Source.HasListeners())
        {
            return null;
        }

        var activity = Source.StartActivity("encina.scheduling.execute", ActivityKind.Internal);
        activity?.SetTag("scheduling.message_id", messageId.ToString());
        return activity;
    }

    private static Activity? StartMarkFailed(Guid messageId)
    {
        if (!Source.HasListeners())
        {
            return null;
        }

        var activity = Source.StartActivity("encina.scheduling.mark_failed", ActivityKind.Internal);
        activity?.SetTag("scheduling.message_id", messageId.ToString());
        return activity;
    }

    private static Activity? StartReschedule(Guid messageId, DateTime nextScheduledAtUtc)
    {
        if (!Source.HasListeners())
        {
            return null;
        }

        var activity = Source.StartActivity("encina.scheduling.reschedule", ActivityKind.Internal);
        activity?.SetTag("scheduling.message_id", messageId.ToString());
        activity?.SetTag("scheduling.next_scheduled_at", nextScheduledAtUtc.ToString("O"));
        return activity;
    }

    private static Activity? StartCancel(Guid messageId)
    {
        if (!Source.HasListeners())
        {
            return null;
        }

        var activity = Source.StartActivity("encina.scheduling.cancel", ActivityKind.Internal);
        activity?.SetTag("scheduling.message_id", messageId.ToString());
        return activity;
    }

    private static void Complete(Activity? activity)
    {
        activity?.SetStatus(ActivityStatusCode.Ok);
    }

    private static void CompleteBatch(Activity? activity, int count)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetTag("scheduling.due_count", count);
        activity.SetStatus(ActivityStatusCode.Ok);
    }

    private static void Failed(Activity? activity, string? errorMessage)
    {
        activity?.SetStatus(ActivityStatusCode.Error, errorMessage);
    }
}
