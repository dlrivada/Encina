using System.Diagnostics;

namespace Encina.Messaging.Diagnostics;

/// <summary>
/// Provides the activity source for Scheduling distributed tracing.
/// </summary>
/// <remarks>
/// <para>
/// Uses a separate <see cref="ActivitySource"/> (<c>"Encina.Messaging.Scheduling"</c>) from the
/// core <c>"Encina"</c> source to allow independent subscription filtering for
/// scheduling traces.
/// </para>
/// <para>
/// All methods guard with <see cref="ActivitySource.HasListeners()"/> for zero-cost
/// when no OpenTelemetry listener is attached.
/// </para>
/// </remarks>
internal static class SchedulingActivitySource
{
    internal static readonly ActivitySource ActivitySource = new("Encina.Messaging.Scheduling", "1.0");

    /// <summary>
    /// The activity source name for external registration (e.g., in OpenTelemetry builder).
    /// </summary>
    internal const string SourceName = "Encina.Messaging.Scheduling";

    /// <summary>
    /// Starts a message scheduling activity.
    /// </summary>
    /// <param name="messageType">The type of the message being scheduled.</param>
    /// <param name="messageId">The unique identifier of the scheduled message.</param>
    /// <param name="scheduledAtUtc">The UTC time at which the message is scheduled to execute.</param>
    /// <returns>The started activity, or <c>null</c> if no listener is active.</returns>
    internal static Activity? StartSchedule(string messageType, Guid messageId, DateTime scheduledAtUtc)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity(
            "encina.scheduling.schedule", ActivityKind.Internal);
        activity?.SetTag("scheduling.message_type", messageType);
        activity?.SetTag("scheduling.message_id", messageId.ToString());
        activity?.SetTag("scheduling.scheduled_at", scheduledAtUtc.ToString("O"));
        return activity;
    }

    /// <summary>
    /// Starts an activity for processing a batch of due scheduled messages.
    /// </summary>
    /// <param name="batchSize">The number of due messages in the batch.</param>
    /// <returns>The started activity, or <c>null</c> if no listener is active.</returns>
    internal static Activity? StartProcessDue(int batchSize)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity(
            "encina.scheduling.process_due", ActivityKind.Internal);
        activity?.SetTag("scheduling.batch_size", batchSize);
        return activity;
    }

    /// <summary>
    /// Starts an activity for executing a single scheduled message.
    /// </summary>
    /// <param name="messageId">The unique identifier of the message being executed.</param>
    /// <returns>The started activity, or <c>null</c> if no listener is active.</returns>
    internal static Activity? StartExecute(Guid messageId)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity(
            "encina.scheduling.execute", ActivityKind.Internal);
        activity?.SetTag("scheduling.message_id", messageId.ToString());
        return activity;
    }

    /// <summary>
    /// Starts an activity for cancelling a scheduled message.
    /// </summary>
    /// <param name="messageId">The unique identifier of the message being cancelled.</param>
    /// <returns>The started activity, or <c>null</c> if no listener is active.</returns>
    internal static Activity? StartCancel(Guid messageId)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity(
            "encina.scheduling.cancel", ActivityKind.Internal);
        activity?.SetTag("scheduling.message_id", messageId.ToString());
        return activity;
    }

    /// <summary>
    /// Completes a scheduling activity successfully.
    /// </summary>
    /// <param name="activity">The activity to complete.</param>
    internal static void Complete(Activity? activity)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetStatus(ActivityStatusCode.Ok);
        activity.Dispose();
    }

    /// <summary>
    /// Marks a scheduling activity as failed.
    /// </summary>
    /// <param name="activity">The activity to mark as failed.</param>
    /// <param name="errorCode">The error code.</param>
    /// <param name="errorMessage">The error message.</param>
    internal static void Failed(Activity? activity, string? errorCode, string? errorMessage)
    {
        if (activity is null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(errorCode))
        {
            activity.SetTag("error.code", errorCode);
        }

        activity.SetStatus(ActivityStatusCode.Error, errorMessage);
        activity.Dispose();
    }
}
