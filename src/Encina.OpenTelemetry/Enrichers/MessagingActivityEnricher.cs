using System.Diagnostics;
using Encina.Messaging.Inbox;
using Encina.Messaging.Outbox;
using Encina.Messaging.Sagas;
using Encina.Messaging.Scheduling;

namespace Encina.OpenTelemetry.Enrichers;

/// <summary>
/// Enriches OpenTelemetry activities with messaging pattern information (Outbox, Inbox, Sagas, Scheduling).
/// </summary>
public static class MessagingActivityEnricher
{
    /// <summary>
    /// Enriches an activity with Outbox message information.
    /// </summary>
    /// <param name="activity">The activity to enrich.</param>
    /// <param name="message">The outbox message.</param>
    public static void EnrichWithOutboxMessage(Activity? activity, IOutboxMessage message)
    {
        if (activity is null || message is null)
        {
            return;
        }

        activity.SetTag("messaging.system", "simplemediator.outbox");
        activity.SetTag("messaging.operation.name", "publish");
        activity.SetTag("messaging.message.id", message.Id.ToString());
        activity.SetTag("messaging.message.type", message.NotificationType);
        activity.SetTag("messaging.message.processed", message.IsProcessed);

        if (message.ProcessedAtUtc.HasValue)
        {
            activity.SetTag("messaging.message.processed_at", message.ProcessedAtUtc.Value.ToString("O"));
        }

        if (message.RetryCount > 0)
        {
            activity.SetTag("messaging.message.retry_count", message.RetryCount);
        }

        if (!string.IsNullOrWhiteSpace(message.ErrorMessage))
        {
            activity.SetTag("messaging.message.error", message.ErrorMessage);
        }
    }

    /// <summary>
    /// Enriches an activity with Inbox message information.
    /// </summary>
    /// <param name="activity">The activity to enrich.</param>
    /// <param name="message">The inbox message.</param>
    public static void EnrichWithInboxMessage(Activity? activity, IInboxMessage message)
    {
        if (activity is null || message is null)
        {
            return;
        }

        activity.SetTag("messaging.system", "simplemediator.inbox");
        activity.SetTag("messaging.operation.name", "receive");
        activity.SetTag("messaging.message.id", message.MessageId);
        activity.SetTag("messaging.message.type", message.RequestType);
        activity.SetTag("messaging.message.processed", message.IsProcessed);

        if (message.ProcessedAtUtc.HasValue)
        {
            activity.SetTag("messaging.message.processed_at", message.ProcessedAtUtc.Value.ToString("O"));
        }

        if (message.RetryCount > 0)
        {
            activity.SetTag("messaging.message.retry_count", message.RetryCount);
        }

        if (!string.IsNullOrWhiteSpace(message.ErrorMessage))
        {
            activity.SetTag("messaging.message.error", message.ErrorMessage);
        }
    }

    /// <summary>
    /// Enriches an activity with Saga state information.
    /// </summary>
    /// <param name="activity">The activity to enrich.</param>
    /// <param name="sagaState">The saga state.</param>
    public static void EnrichWithSagaState(Activity? activity, ISagaState sagaState)
    {
        if (activity is null || sagaState is null)
        {
            return;
        }

        activity.SetTag("saga.id", sagaState.SagaId.ToString());
        activity.SetTag("saga.type", sagaState.SagaType);
        activity.SetTag("saga.status", sagaState.Status);
        activity.SetTag("saga.current_step", sagaState.CurrentStep);

        if (sagaState.CompletedAtUtc.HasValue)
        {
            activity.SetTag("saga.completed_at", sagaState.CompletedAtUtc.Value.ToString("O"));
        }

        if (!string.IsNullOrWhiteSpace(sagaState.ErrorMessage))
        {
            activity.SetTag("saga.error", sagaState.ErrorMessage);
        }
    }

    /// <summary>
    /// Enriches an activity with Scheduled message information.
    /// </summary>
    /// <param name="activity">The activity to enrich.</param>
    /// <param name="message">The scheduled message.</param>
    public static void EnrichWithScheduledMessage(Activity? activity, IScheduledMessage message)
    {
        if (activity is null || message is null)
        {
            return;
        }

        activity.SetTag("messaging.system", "simplemediator.scheduling");
        activity.SetTag("messaging.operation.name", "schedule");
        activity.SetTag("messaging.message.id", message.Id.ToString());
        activity.SetTag("messaging.message.type", message.RequestType);
        activity.SetTag("messaging.message.scheduled_at", message.ScheduledAtUtc.ToString("O"));
        activity.SetTag("messaging.message.processed", message.IsProcessed);
        activity.SetTag("messaging.message.is_recurring", message.IsRecurring);

        if (message.ProcessedAtUtc.HasValue)
        {
            activity.SetTag("messaging.message.processed_at", message.ProcessedAtUtc.Value.ToString("O"));
        }

        if (message.IsRecurring && !string.IsNullOrWhiteSpace(message.CronExpression))
        {
            activity.SetTag("messaging.message.cron_expression", message.CronExpression);
        }

        if (message.RetryCount > 0)
        {
            activity.SetTag("messaging.message.retry_count", message.RetryCount);
        }

        if (!string.IsNullOrWhiteSpace(message.ErrorMessage))
        {
            activity.SetTag("messaging.message.error", message.ErrorMessage);
        }
    }
}
