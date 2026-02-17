using System.Diagnostics;
using Encina.Sharding.Migrations;

namespace Encina.OpenTelemetry.Migrations;

/// <summary>
/// Provides distributed tracing instrumentation for shard migration coordination.
/// </summary>
/// <remarks>
/// <para>
/// Uses a dedicated <see cref="ActivitySource"/> named <c>"Encina.Migration"</c> to create
/// parent spans for migration coordination and child spans for individual shard migrations.
/// All activity creation is guarded by <see cref="ActivitySource.HasListeners()"/> to avoid
/// allocations when no trace collector is configured.
/// </para>
/// <para>
/// This source must be registered with the OpenTelemetry tracer via
/// <c>tracing.AddSource("Encina.Migration")</c>, which is done automatically by
/// <see cref="ServiceCollectionExtensions.WithEncina"/>.
/// </para>
/// </remarks>
public static class MigrationActivitySource
{
    /// <summary>
    /// The name of the activity source used for migration tracing.
    /// </summary>
    public const string SourceName = "Encina.Migration";

    private static readonly ActivitySource Source = new(SourceName, "1.0");

    /// <summary>
    /// Starts a parent activity for migration coordination across all shards.
    /// </summary>
    /// <param name="migrationId">The unique migration execution identifier.</param>
    /// <param name="strategy">The migration strategy being used.</param>
    /// <param name="shardCount">The total number of shards to migrate.</param>
    /// <returns>
    /// The started <see cref="Activity"/>, or <see langword="null"/> if no listeners are registered.
    /// </returns>
    public static Activity? StartMigrationCoordination(
        Guid migrationId,
        MigrationStrategy strategy,
        int shardCount)
    {
        if (!Source.HasListeners())
        {
            return null;
        }

        var activity = Source.StartActivity("Encina.Migration.Coordinate", ActivityKind.Internal);

        if (activity is not null)
        {
            activity.SetTag(ActivityTagNames.Migration.Id, migrationId.ToString());
            activity.SetTag(ActivityTagNames.Migration.Strategy, strategy.ToString());
            activity.SetTag(ActivityTagNames.Migration.ShardCount, shardCount);
        }

        return activity;
    }

    /// <summary>
    /// Starts a child activity for an individual shard's migration.
    /// </summary>
    /// <param name="shardId">The shard identifier being migrated.</param>
    /// <param name="migrationId">The parent migration execution identifier.</param>
    /// <returns>
    /// The started <see cref="Activity"/>, or <see langword="null"/> if no listeners are registered.
    /// </returns>
    public static Activity? StartShardMigration(string shardId, Guid migrationId)
    {
        if (!Source.HasListeners())
        {
            return null;
        }

        var activity = Source.StartActivity("Encina.Migration.Shard", ActivityKind.Internal);

        if (activity is not null)
        {
            activity.SetTag(ActivityTagNames.Migration.Id, migrationId.ToString());
            activity.SetTag(ActivityTagNames.Migration.ShardId, shardId);
        }

        return activity;
    }

    /// <summary>
    /// Completes a migration activity, setting the outcome status and optional error details.
    /// </summary>
    /// <param name="activity">The activity to complete (may be <c>null</c>).</param>
    /// <param name="outcome">The migration outcome for this span.</param>
    /// <param name="durationMs">The duration in milliseconds, if available.</param>
    /// <param name="errorMessage">The error message, if the migration failed.</param>
    public static void Complete(
        Activity? activity,
        MigrationOutcome outcome,
        double? durationMs = null,
        string? errorMessage = null)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetTag(ActivityTagNames.Migration.ShardOutcome, outcome.ToString());

        if (durationMs.HasValue)
        {
            activity.SetTag(ActivityTagNames.Migration.DurationMs, durationMs.Value);
        }

        if (!string.IsNullOrWhiteSpace(errorMessage))
        {
            activity.SetTag(ActivityTagNames.Migration.Error, errorMessage);
        }

        var status = outcome switch
        {
            MigrationOutcome.Succeeded => ActivityStatusCode.Ok,
            MigrationOutcome.Failed => ActivityStatusCode.Error,
            MigrationOutcome.RolledBack => ActivityStatusCode.Error,
            _ => ActivityStatusCode.Unset
        };

        activity.SetStatus(status, errorMessage);
        activity.Dispose();
    }
}
