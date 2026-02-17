using System.Diagnostics;

namespace Encina.Messaging.Diagnostics;

/// <summary>
/// Provides the activity source for Outbox distributed tracing.
/// </summary>
/// <remarks>
/// <para>
/// Uses a separate <see cref="ActivitySource"/> (<c>"Encina.Messaging.Outbox"</c>) from the
/// core <c>"Encina"</c> source to allow independent subscription filtering for
/// outbox traces.
/// </para>
/// <para>
/// All methods guard with <see cref="ActivitySource.HasListeners()"/> for zero-cost
/// when no OpenTelemetry listener is attached.
/// </para>
/// </remarks>
internal static class OutboxActivitySource
{
    internal static readonly ActivitySource ActivitySource = new("Encina.Messaging.Outbox", "1.0");

    /// <summary>
    /// The activity source name for external registration (e.g., in OpenTelemetry builder).
    /// </summary>
    internal const string SourceName = "Encina.Messaging.Outbox";

    /// <summary>
    /// Starts an outbox add message activity.
    /// </summary>
    /// <param name="messageType">The type of the outbox message.</param>
    /// <param name="messageId">The unique identifier of the message.</param>
    /// <returns>The started activity, or <c>null</c> if no listener is active.</returns>
    internal static Activity? StartAdd(string messageType, string messageId)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity(
            "encina.outbox.add", ActivityKind.Internal);
        activity?.SetTag("outbox.message_type", messageType);
        activity?.SetTag("outbox.message_id", messageId);
        return activity;
    }

    /// <summary>
    /// Starts an outbox processing batch activity.
    /// </summary>
    /// <param name="batchSize">The number of messages in the batch.</param>
    /// <returns>The started activity, or <c>null</c> if no listener is active.</returns>
    internal static Activity? StartProcessBatch(int batchSize)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity(
            "encina.outbox.process_batch", ActivityKind.Internal);
        activity?.SetTag("outbox.batch_size", batchSize);
        return activity;
    }

    /// <summary>
    /// Starts an activity for marking an outbox message as processed.
    /// </summary>
    /// <param name="messageId">The unique identifier of the message being marked.</param>
    /// <returns>The started activity, or <c>null</c> if no listener is active.</returns>
    internal static Activity? StartMarkProcessed(string messageId)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity(
            "encina.outbox.mark_processed", ActivityKind.Internal);
        activity?.SetTag("outbox.message_id", messageId);
        return activity;
    }

    /// <summary>
    /// Completes an outbox activity successfully.
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
    /// Completes a batch processing activity with the count of processed messages.
    /// </summary>
    /// <param name="activity">The activity to complete.</param>
    /// <param name="processedCount">The number of messages successfully processed.</param>
    internal static void CompleteBatch(Activity? activity, int processedCount)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetTag("outbox.processed_count", processedCount);
        activity.SetStatus(ActivityStatusCode.Ok);
        activity.Dispose();
    }

    /// <summary>
    /// Marks an outbox activity as failed.
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
