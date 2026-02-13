using System.Diagnostics;

namespace Encina.IdGeneration.Diagnostics;

/// <summary>
/// Provides the activity source for ID generation distributed tracing.
/// </summary>
/// <remarks>
/// <para>
/// Uses a separate <see cref="ActivitySource"/> (<c>"Encina.IdGeneration"</c>) from the
/// core <c>"Encina"</c> source to allow independent subscription filtering for
/// ID generation traces.
/// </para>
/// <para>
/// All methods guard with <see cref="ActivitySource.HasListeners()"/> for zero-cost
/// when no OpenTelemetry listener is attached.
/// </para>
/// </remarks>
internal static class IdGenerationActivitySource
{
    internal static readonly ActivitySource ActivitySource = new("Encina.IdGeneration", "1.0");

    /// <summary>
    /// The activity source name for external registration (e.g., in OpenTelemetry builder).
    /// </summary>
    internal const string SourceName = "Encina.IdGeneration";

    /// <summary>
    /// Starts an ID generation activity.
    /// </summary>
    /// <param name="strategy">The ID generation strategy name (e.g., "Snowflake", "Ulid").</param>
    /// <param name="shardId">The optional shard ID used for generation.</param>
    /// <returns>The started activity, or <c>null</c> if no listener is active.</returns>
    internal static Activity? StartIdGeneration(string strategy, string? shardId = null)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity(
            "encina.id_generation.generate", ActivityKind.Internal);
        activity?.SetTag("id.strategy", strategy);

        if (shardId is not null)
        {
            activity?.SetTag("id.shard_id", shardId);
        }

        return activity;
    }

    /// <summary>
    /// Starts a shard extraction activity.
    /// </summary>
    /// <param name="idValue">The string representation of the ID being inspected.</param>
    /// <returns>The started activity, or <c>null</c> if no listener is active.</returns>
    internal static Activity? StartShardExtraction(string idValue)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity(
            "encina.id_generation.extract_shard", ActivityKind.Internal);
        activity?.SetTag("id.value", idValue);
        return activity;
    }

    /// <summary>
    /// Completes an ID generation or shard extraction activity successfully.
    /// </summary>
    /// <param name="activity">The activity to complete.</param>
    /// <param name="resultValue">The optional result value to record as a tag.</param>
    internal static void Complete(Activity? activity, string? resultValue = null)
    {
        if (activity is null)
        {
            return;
        }

        if (resultValue is not null)
        {
            activity.SetTag("id.result", resultValue);
        }

        activity.SetStatus(ActivityStatusCode.Ok);
        activity.Dispose();
    }

    /// <summary>
    /// Completes a shard extraction activity with the extracted shard ID.
    /// </summary>
    /// <param name="activity">The activity to complete.</param>
    /// <param name="shardId">The extracted shard ID.</param>
    internal static void CompleteShardExtraction(Activity? activity, string shardId)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetTag("id.shard_id", shardId);
        activity.SetStatus(ActivityStatusCode.Ok);
        activity.Dispose();
    }

    /// <summary>
    /// Marks an activity as failed.
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
