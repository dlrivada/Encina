namespace Encina.Sharding.Migrations;

/// <summary>
/// The result of a schema drift detection operation across all shards.
/// </summary>
/// <remarks>
/// <para>
/// Returned by <see cref="IShardedMigrationCoordinator.DetectDriftAsync"/> inside an
/// <c>Either&lt;EncinaError, SchemaDriftReport&gt;</c>. The <see cref="HasDrift"/>
/// property provides a quick boolean check, while <see cref="Diffs"/> contains the
/// detailed per-shard differences for remediation.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var report = await coordinator.DetectDriftAsync(options: null, ct);
///
/// report.Match(
///     Right: r =>
///     {
///         if (r.HasDrift)
///             foreach (var diff in r.Diffs)
///                 logger.LogWarning("Shard {ShardId} drifted from {Baseline}: {Count} table diffs",
///                     diff.ShardId, diff.BaselineShardId, diff.TableDiffs.Count);
///         else
///             logger.LogInformation("No schema drift detected at {Time}", r.DetectedAtUtc);
///     },
///     Left: error => logger.LogError("Drift detection failed: {Error}", error.Message));
/// </code>
/// </example>
/// <param name="Diffs">Per-shard schema differences relative to the baseline.</param>
/// <param name="DetectedAtUtc">UTC timestamp when the drift detection was performed.</param>
public sealed record SchemaDriftReport(
    IReadOnlyList<ShardSchemaDiff> Diffs,
    DateTimeOffset DetectedAtUtc)
{
    /// <summary>
    /// Gets whether any shard has schema differences compared to the baseline.
    /// </summary>
    public bool HasDrift => Diffs.Count > 0 && Diffs.Any(d => d.TableDiffs.Count > 0);
}
