using System.Diagnostics;
using Encina.Sharding.Shadow;

namespace Encina.OpenTelemetry.Sharding;

/// <summary>
/// Enriches <see cref="Activity"/> spans with shadow sharding comparison data
/// for distributed tracing of topology migration tests.
/// </summary>
/// <remarks>
/// <para>
/// All methods are null-safe: passing a <c>null</c> activity is a no-op.
/// Tags are applied using constants from <see cref="ActivityTagNames.Shadow"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// using var activity = source.StartActivity("encina.sharding.shadow.route");
/// ShadowShardingActivityEnricher.EnrichWithShadowRouting(activity, comparisonResult);
/// </code>
/// </example>
public static class ShadowShardingActivityEnricher
{
    /// <summary>
    /// Enriches the current activity with shadow routing comparison data.
    /// </summary>
    /// <param name="activity">The activity to enrich, or <c>null</c> for a no-op.</param>
    /// <param name="result">The shadow routing comparison result.</param>
    public static void EnrichWithShadowRouting(Activity? activity, ShadowComparisonResult result)
    {
        if (activity is null)
        {
            return;
        }

        ArgumentNullException.ThrowIfNull(result);

        activity.SetTag(ActivityTagNames.Shadow.ProductionShard, result.ProductionShardId);
        activity.SetTag(ActivityTagNames.Shadow.ShadowShard, result.ShadowShardId);
        activity.SetTag(ActivityTagNames.Shadow.RoutingMatch, result.RoutingMatch);

        if (result.ResultsMatch.HasValue)
        {
            activity.SetTag(ActivityTagNames.Shadow.ReadResultsMatch, result.ResultsMatch.Value);
        }
    }

    /// <summary>
    /// Enriches the current activity with shadow write operation data.
    /// </summary>
    /// <param name="activity">The activity to enrich, or <c>null</c> for a no-op.</param>
    /// <param name="shardId">The target shard identifier for the shadow write.</param>
    /// <param name="success"><c>true</c> if the shadow write succeeded; <c>false</c> otherwise.</param>
    public static void EnrichWithShadowWrite(Activity? activity, string shardId, bool success)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetTag(ActivityTagNames.Shadow.ShadowShard, shardId);
        activity.SetTag(ActivityTagNames.Shadow.WriteOutcome, success ? "success" : "failure");
    }
}
