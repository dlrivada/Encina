using System.Diagnostics;

namespace Encina.Messaging.Diagnostics;

/// <summary>
/// Provides the activity source for Inbox distributed tracing.
/// </summary>
/// <remarks>
/// <para>
/// Uses a separate <see cref="ActivitySource"/> (<c>"Encina.Messaging.Inbox"</c>) from the
/// core <c>"Encina"</c> source to allow independent subscription filtering for
/// inbox traces.
/// </para>
/// <para>
/// All methods guard with <see cref="ActivitySource.HasListeners()"/> for zero-cost
/// when no OpenTelemetry listener is attached.
/// </para>
/// </remarks>
internal static class InboxActivitySource
{
    internal static readonly ActivitySource ActivitySource = new("Encina.Messaging.Inbox", "1.0");

    /// <summary>
    /// The activity source name for external registration (e.g., in OpenTelemetry builder).
    /// </summary>
    internal const string SourceName = "Encina.Messaging.Inbox";

    /// <summary>
    /// Starts an inbox receive activity.
    /// </summary>
    /// <param name="messageType">The type of the incoming message.</param>
    /// <param name="messageId">The unique identifier of the message.</param>
    /// <returns>The started activity, or <c>null</c> if no listener is active.</returns>
    internal static Activity? StartReceive(string messageType, string messageId)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity(
            "encina.inbox.receive", ActivityKind.Internal);
        activity?.SetTag("inbox.message_type", messageType);
        activity?.SetTag("inbox.message_id", messageId);
        return activity;
    }

    /// <summary>
    /// Starts a duplicate check activity for an inbox message.
    /// </summary>
    /// <param name="messageId">The unique identifier of the message being checked.</param>
    /// <returns>The started activity, or <c>null</c> if no listener is active.</returns>
    internal static Activity? StartDuplicateCheck(string messageId)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity(
            "encina.inbox.duplicate_check", ActivityKind.Internal);
        activity?.SetTag("inbox.message_id", messageId);
        return activity;
    }

    /// <summary>
    /// Completes a duplicate check activity when a duplicate message is found.
    /// </summary>
    /// <param name="activity">The duplicate check activity to complete.</param>
    internal static void CompleteDuplicateFound(Activity? activity)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetTag("inbox.duplicate", true);
        activity.SetStatus(ActivityStatusCode.Ok);
        activity.Dispose();
    }

    /// <summary>
    /// Starts an activity for marking an inbox message as processed.
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
            "encina.inbox.mark_processed", ActivityKind.Internal);
        activity?.SetTag("inbox.message_id", messageId);
        return activity;
    }

    /// <summary>
    /// Completes an inbox activity successfully.
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
    /// Marks an inbox activity as failed.
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
