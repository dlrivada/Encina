using System.Diagnostics;

namespace Encina.DomainModeling.Diagnostics;

/// <summary>
/// Provides the activity source for bulk operations distributed tracing.
/// </summary>
/// <remarks>
/// All methods guard with <see cref="ActivitySource.HasListeners()"/> for zero-cost
/// when no OpenTelemetry listener is attached.
/// </remarks>
internal static class BulkOperationsActivitySource
{
    internal static readonly ActivitySource ActivitySource = new("Encina.BulkOperations", "1.0");

    /// <summary>
    /// The activity source name for external registration.
    /// </summary>
    internal const string SourceName = "Encina.BulkOperations";

    /// <summary>
    /// Starts a bulk operation activity.
    /// </summary>
    /// <param name="operation">The operation type (insert, update, delete, merge, read).</param>
    /// <param name="entityType">The entity type name.</param>
    /// <param name="provider">The data access provider.</param>
    /// <returns>The started activity, or <c>null</c> if no listener is active.</returns>
    internal static Activity? StartBulkOperation(string operation, string entityType, string provider)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity(
            $"encina.bulk.{operation}", ActivityKind.Internal);
        activity?.SetTag("bulk.operation", operation);
        activity?.SetTag("bulk.entity_type", entityType);
        activity?.SetTag("bulk.provider", provider);
        return activity;
    }

    /// <summary>
    /// Completes a bulk operation activity successfully.
    /// </summary>
    /// <param name="activity">The activity to complete.</param>
    /// <param name="rowsAffected">The number of rows affected.</param>
    internal static void Complete(Activity? activity, int rowsAffected)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetTag("bulk.rows_affected", rowsAffected);
        activity.SetStatus(ActivityStatusCode.Ok);
        activity.Dispose();
    }

    /// <summary>
    /// Marks a bulk operation activity as failed.
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
