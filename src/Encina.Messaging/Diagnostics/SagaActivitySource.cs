using System.Diagnostics;

namespace Encina.Messaging.Diagnostics;

/// <summary>
/// Provides the activity source for Saga distributed tracing.
/// </summary>
/// <remarks>
/// <para>
/// Uses a separate <see cref="ActivitySource"/> (<c>"Encina.Messaging.Saga"</c>) from the
/// core <c>"Encina"</c> source to allow independent subscription filtering for
/// saga traces.
/// </para>
/// <para>
/// All methods guard with <see cref="ActivitySource.HasListeners()"/> for zero-cost
/// when no OpenTelemetry listener is attached.
/// </para>
/// </remarks>
internal static class SagaActivitySource
{
    internal static readonly ActivitySource ActivitySource = new("Encina.Messaging.Saga", "1.0");

    /// <summary>
    /// The activity source name for external registration (e.g., in OpenTelemetry builder).
    /// </summary>
    internal const string SourceName = "Encina.Messaging.Saga";

    /// <summary>
    /// Starts a saga transition activity.
    /// </summary>
    /// <param name="sagaType">The type name of the saga.</param>
    /// <param name="sagaId">The unique identifier of the saga instance.</param>
    /// <param name="fromStep">The step the saga is transitioning from.</param>
    /// <param name="toStep">The step the saga is transitioning to.</param>
    /// <returns>The started activity, or <c>null</c> if no listener is active.</returns>
    internal static Activity? StartSagaTransition(string sagaType, Guid sagaId, string fromStep, string toStep)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity(
            "encina.saga.transition", ActivityKind.Internal);
        activity?.SetTag("saga.type", sagaType);
        activity?.SetTag("saga.id", sagaId.ToString());
        activity?.SetTag("saga.from_step", fromStep);
        activity?.SetTag("saga.to_step", toStep);
        return activity;
    }

    /// <summary>
    /// Starts a saga creation activity.
    /// </summary>
    /// <param name="sagaType">The type name of the saga.</param>
    /// <param name="sagaId">The unique identifier of the new saga instance.</param>
    /// <returns>The started activity, or <c>null</c> if no listener is active.</returns>
    internal static Activity? StartSagaCreate(string sagaType, Guid sagaId)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity(
            "encina.saga.create", ActivityKind.Internal);
        activity?.SetTag("saga.type", sagaType);
        activity?.SetTag("saga.id", sagaId.ToString());
        return activity;
    }

    /// <summary>
    /// Starts a saga completion activity.
    /// </summary>
    /// <param name="sagaType">The type name of the saga.</param>
    /// <param name="sagaId">The unique identifier of the saga instance being completed.</param>
    /// <returns>The started activity, or <c>null</c> if no listener is active.</returns>
    internal static Activity? StartSagaComplete(string sagaType, Guid sagaId)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity(
            "encina.saga.complete", ActivityKind.Internal);
        activity?.SetTag("saga.type", sagaType);
        activity?.SetTag("saga.id", sagaId.ToString());
        return activity;
    }

    /// <summary>
    /// Starts a saga compensation activity.
    /// </summary>
    /// <param name="sagaType">The type name of the saga.</param>
    /// <param name="sagaId">The unique identifier of the saga instance being compensated.</param>
    /// <returns>The started activity, or <c>null</c> if no listener is active.</returns>
    internal static Activity? StartSagaCompensate(string sagaType, Guid sagaId)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity(
            "encina.saga.compensate", ActivityKind.Internal);
        activity?.SetTag("saga.type", sagaType);
        activity?.SetTag("saga.id", sagaId.ToString());
        return activity;
    }

    /// <summary>
    /// Completes a saga activity successfully.
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
    /// Marks a saga activity as failed.
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
