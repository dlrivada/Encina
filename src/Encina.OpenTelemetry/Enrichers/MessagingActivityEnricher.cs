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

        activity.SetTag(ActivityTagNames.Messaging.System, ActivityTagNames.Systems.Outbox);
        activity.SetTag(ActivityTagNames.Messaging.OperationName, ActivityTagNames.Operations.Publish);
        activity.SetTag(ActivityTagNames.Messaging.MessageId, message.Id.ToString());
        activity.SetTag(ActivityTagNames.Messaging.MessageType, message.NotificationType);
        activity.SetTag(ActivityTagNames.Messaging.Processed, message.IsProcessed);

        if (message.ProcessedAtUtc.HasValue)
        {
            activity.SetTag(ActivityTagNames.Messaging.ProcessedAt, message.ProcessedAtUtc.Value.ToString("O"));
        }

        if (message.RetryCount > 0)
        {
            activity.SetTag(ActivityTagNames.Messaging.RetryCount, message.RetryCount);
        }

        if (!string.IsNullOrWhiteSpace(message.ErrorMessage))
        {
            activity.SetTag(ActivityTagNames.Messaging.Error, message.ErrorMessage);
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

        activity.SetTag(ActivityTagNames.Messaging.System, ActivityTagNames.Systems.Inbox);
        activity.SetTag(ActivityTagNames.Messaging.OperationName, ActivityTagNames.Operations.Receive);
        activity.SetTag(ActivityTagNames.Messaging.MessageId, message.MessageId);
        activity.SetTag(ActivityTagNames.Messaging.MessageType, message.RequestType);
        activity.SetTag(ActivityTagNames.Messaging.Processed, message.IsProcessed);

        if (message.ProcessedAtUtc.HasValue)
        {
            activity.SetTag(ActivityTagNames.Messaging.ProcessedAt, message.ProcessedAtUtc.Value.ToString("O"));
        }

        if (message.RetryCount > 0)
        {
            activity.SetTag(ActivityTagNames.Messaging.RetryCount, message.RetryCount);
        }

        if (!string.IsNullOrWhiteSpace(message.ErrorMessage))
        {
            activity.SetTag(ActivityTagNames.Messaging.Error, message.ErrorMessage);
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

        activity.SetTag(ActivityTagNames.Saga.Id, sagaState.SagaId.ToString());
        activity.SetTag(ActivityTagNames.Saga.Type, sagaState.SagaType);
        activity.SetTag(ActivityTagNames.Saga.Status, sagaState.Status);
        activity.SetTag(ActivityTagNames.Saga.CurrentStep, sagaState.CurrentStep);

        if (sagaState.CompletedAtUtc.HasValue)
        {
            activity.SetTag(ActivityTagNames.Saga.CompletedAt, sagaState.CompletedAtUtc.Value.ToString("O"));
        }

        if (!string.IsNullOrWhiteSpace(sagaState.ErrorMessage))
        {
            activity.SetTag(ActivityTagNames.Saga.Error, sagaState.ErrorMessage);
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

        activity.SetTag(ActivityTagNames.Messaging.System, ActivityTagNames.Systems.Scheduling);
        activity.SetTag(ActivityTagNames.Messaging.OperationName, ActivityTagNames.Operations.Schedule);
        activity.SetTag(ActivityTagNames.Messaging.MessageId, message.Id.ToString());
        activity.SetTag(ActivityTagNames.Messaging.MessageType, message.RequestType);
        activity.SetTag(ActivityTagNames.Messaging.ScheduledAt, message.ScheduledAtUtc.ToString("O"));
        activity.SetTag(ActivityTagNames.Messaging.Processed, message.IsProcessed);
        activity.SetTag(ActivityTagNames.Messaging.IsRecurring, message.IsRecurring);

        if (message.ProcessedAtUtc.HasValue)
        {
            activity.SetTag(ActivityTagNames.Messaging.ProcessedAt, message.ProcessedAtUtc.Value.ToString("O"));
        }

        if (message.IsRecurring && !string.IsNullOrWhiteSpace(message.CronExpression))
        {
            activity.SetTag(ActivityTagNames.Messaging.CronExpression, message.CronExpression);
        }

        if (message.RetryCount > 0)
        {
            activity.SetTag(ActivityTagNames.Messaging.RetryCount, message.RetryCount);
        }

        if (!string.IsNullOrWhiteSpace(message.ErrorMessage))
        {
            activity.SetTag(ActivityTagNames.Messaging.Error, message.ErrorMessage);
        }
    }
}
