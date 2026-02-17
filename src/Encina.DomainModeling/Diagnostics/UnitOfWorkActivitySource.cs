using System.Diagnostics;

namespace Encina.DomainModeling.Diagnostics;

/// <summary>
/// Provides the activity source for Unit of Work distributed tracing.
/// </summary>
/// <remarks>
/// All methods guard with <see cref="ActivitySource.HasListeners()"/> for zero-cost
/// when no OpenTelemetry listener is attached.
/// </remarks>
internal static class UnitOfWorkActivitySource
{
    internal static readonly ActivitySource ActivitySource = new("Encina.UnitOfWork", "1.0");

    /// <summary>
    /// The activity source name for external registration.
    /// </summary>
    internal const string SourceName = "Encina.UnitOfWork";

    /// <summary>
    /// Starts a SaveChanges activity.
    /// </summary>
    /// <returns>The started activity, or <c>null</c> if no listener is active.</returns>
    internal static Activity? StartSaveChanges()
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        return ActivitySource.StartActivity("encina.uow.save_changes", ActivityKind.Internal);
    }

    /// <summary>
    /// Starts a transaction activity.
    /// </summary>
    /// <returns>The started activity, or <c>null</c> if no listener is active.</returns>
    internal static Activity? StartTransaction()
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        return ActivitySource.StartActivity("encina.uow.transaction", ActivityKind.Internal);
    }

    /// <summary>
    /// Completes a SaveChanges activity successfully.
    /// </summary>
    /// <param name="activity">The activity to complete.</param>
    /// <param name="affectedRows">The number of rows affected.</param>
    internal static void CompleteSaveChanges(Activity? activity, int affectedRows)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetTag("uow.affected_rows", affectedRows);
        activity.SetStatus(ActivityStatusCode.Ok);
        activity.Dispose();
    }

    /// <summary>
    /// Completes a transaction activity successfully.
    /// </summary>
    /// <param name="activity">The activity to complete.</param>
    /// <param name="outcome">The transaction outcome (committed, rolledback).</param>
    internal static void CompleteTransaction(Activity? activity, string outcome)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetTag("uow.outcome", outcome);
        activity.SetStatus(ActivityStatusCode.Ok);
        activity.Dispose();
    }

    /// <summary>
    /// Marks a Unit of Work activity as failed.
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
