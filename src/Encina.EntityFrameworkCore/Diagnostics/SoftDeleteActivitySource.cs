using System.Diagnostics;

namespace Encina.EntityFrameworkCore.Diagnostics;

/// <summary>
/// Provides the activity source for soft delete distributed tracing.
/// </summary>
/// <remarks>
/// All methods guard with <see cref="ActivitySource.HasListeners()"/> for zero-cost
/// when no OpenTelemetry listener is attached.
/// </remarks>
internal static class SoftDeleteActivitySource
{
    internal static readonly ActivitySource ActivitySource = new("Encina.SoftDelete", "1.0");

    /// <summary>
    /// The activity source name for external registration.
    /// </summary>
    internal const string SourceName = "Encina.SoftDelete";

    /// <summary>
    /// Starts a soft delete activity.
    /// </summary>
    /// <param name="entityType">The entity type name.</param>
    /// <returns>The started activity, or <c>null</c> if no listener is active.</returns>
    internal static Activity? StartSoftDelete(string entityType)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("encina.softdelete.delete", ActivityKind.Internal);
        activity?.SetTag("softdelete.entity_type", entityType);
        activity?.SetTag("softdelete.operation", "delete");
        return activity;
    }

    /// <summary>
    /// Starts a restore activity.
    /// </summary>
    /// <param name="entityType">The entity type name.</param>
    /// <returns>The started activity, or <c>null</c> if no listener is active.</returns>
    internal static Activity? StartRestore(string entityType)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("encina.softdelete.restore", ActivityKind.Internal);
        activity?.SetTag("softdelete.entity_type", entityType);
        activity?.SetTag("softdelete.operation", "restore");
        return activity;
    }

    /// <summary>
    /// Starts a hard delete activity.
    /// </summary>
    /// <param name="entityType">The entity type name.</param>
    /// <returns>The started activity, or <c>null</c> if no listener is active.</returns>
    internal static Activity? StartHardDelete(string entityType)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("encina.softdelete.hard_delete", ActivityKind.Internal);
        activity?.SetTag("softdelete.entity_type", entityType);
        activity?.SetTag("softdelete.operation", "hard_delete");
        return activity;
    }

    /// <summary>
    /// Completes a soft delete activity successfully.
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
    /// Marks a soft delete activity as failed.
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
