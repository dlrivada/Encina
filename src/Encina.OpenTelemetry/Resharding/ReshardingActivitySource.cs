using System.Diagnostics;
using Encina.Sharding.Resharding;

namespace Encina.OpenTelemetry.Resharding;

/// <summary>
/// Provides distributed tracing instrumentation for online resharding operations.
/// </summary>
/// <remarks>
/// <para>
/// Uses a dedicated <see cref="ActivitySource"/> named <c>"Encina.Resharding"</c> to create
/// parent spans for resharding execution and child spans for individual phase execution.
/// All activity creation is guarded by <see cref="ActivitySource.HasListeners()"/> to avoid
/// allocations when no trace collector is configured.
/// </para>
/// <para>
/// This source must be registered with the OpenTelemetry tracer via
/// <c>tracing.AddSource("Encina.Resharding")</c>, which is done automatically by
/// <see cref="ServiceCollectionExtensions.WithEncina"/>.
/// </para>
/// </remarks>
public static class ReshardingActivitySource
{
    /// <summary>
    /// The name of the activity source used for resharding tracing.
    /// </summary>
    public const string SourceName = "Encina.Resharding";

    private static readonly ActivitySource Source = new(SourceName, "1.0");

    /// <summary>
    /// Starts a parent activity for a resharding execution.
    /// </summary>
    /// <param name="reshardingId">The unique resharding operation identifier.</param>
    /// <param name="stepCount">The number of migration steps in the plan.</param>
    /// <param name="estimatedRows">The estimated total rows to migrate.</param>
    /// <returns>
    /// The started <see cref="Activity"/>, or <see langword="null"/> if no listeners are registered.
    /// </returns>
    public static Activity? StartReshardingExecution(
        Guid reshardingId,
        int stepCount,
        long estimatedRows)
    {
        if (!Source.HasListeners())
        {
            return null;
        }

        var activity = Source.StartActivity("Encina.Resharding.Execute", ActivityKind.Internal);

        if (activity is not null)
        {
            activity.SetTag(ActivityTagNames.Resharding.Id, reshardingId.ToString());
            activity.SetTag("resharding.step_count", stepCount);
            activity.SetTag("resharding.estimated_rows", estimatedRows);
        }

        return activity;
    }

    /// <summary>
    /// Starts a child activity for an individual resharding phase.
    /// </summary>
    /// <param name="reshardingId">The parent resharding operation identifier.</param>
    /// <param name="phase">The phase being executed.</param>
    /// <returns>
    /// The started <see cref="Activity"/>, or <see langword="null"/> if no listeners are registered.
    /// </returns>
    public static Activity? StartPhaseExecution(
        Guid reshardingId,
        ReshardingPhase phase)
    {
        if (!Source.HasListeners())
        {
            return null;
        }

        var activity = Source.StartActivity("Encina.Resharding.Phase", ActivityKind.Internal);

        if (activity is not null)
        {
            activity.SetTag(ActivityTagNames.Resharding.Id, reshardingId.ToString());
            activity.SetTag(ActivityTagNames.Resharding.Phase, phase.ToString());
        }

        return activity;
    }

    /// <summary>
    /// Completes a resharding activity, setting the outcome status and optional error details.
    /// </summary>
    /// <param name="activity">The activity to complete (may be <c>null</c>).</param>
    /// <param name="succeeded">Whether the operation succeeded.</param>
    /// <param name="durationMs">The duration in milliseconds, if available.</param>
    /// <param name="errorMessage">The error message, if the operation failed.</param>
    public static void Complete(
        Activity? activity,
        bool succeeded,
        double? durationMs = null,
        string? errorMessage = null)
    {
        if (activity is null)
        {
            return;
        }

        if (durationMs.HasValue)
        {
            activity.SetTag("resharding.duration_ms", durationMs.Value);
        }

        if (!string.IsNullOrWhiteSpace(errorMessage))
        {
            activity.SetTag("resharding.error", errorMessage);
        }

        activity.SetStatus(
            succeeded ? ActivityStatusCode.Ok : ActivityStatusCode.Error,
            errorMessage);

        activity.Dispose();
    }
}
