namespace Encina.Sharding.Migrations;

/// <summary>
/// Configuration options for schema drift detection across shards.
/// </summary>
/// <remarks>
/// <para>
/// Passed to <see cref="IShardedMigrationCoordinator.DetectDriftAsync"/> to control
/// which shard is used as baseline, the depth of comparison, and which schema elements
/// are included in the drift analysis.
/// When <see langword="null"/> is passed, the coordinator selects defaults automatically.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var options = new DriftDetectionOptions
/// {
///     ComparisonDepth = SchemaComparisonDepth.Full,
///     IncludeIndexes = true,
///     IncludeConstraints = true,
///     CriticalTables = ["orders", "customers", "payments"]
/// };
///
/// var report = await coordinator.DetectDriftAsync(options, ct);
/// </code>
/// </example>
public sealed class DriftDetectionOptions
{
    /// <summary>
    /// Gets or sets the shard ID to use as the baseline for comparison.
    /// When <see langword="null"/>, the coordinator selects the first active shard
    /// in the topology.
    /// </summary>
    public string? BaselineShardId { get; set; }

    /// <summary>
    /// Gets or sets whether to include column-level diff details in addition to
    /// table-level diffs.
    /// </summary>
    /// <value>Defaults to <see langword="true"/>.</value>
    public bool IncludeColumnDiffs { get; set; } = true;

    /// <summary>
    /// Gets or sets the depth of schema comparison performed during drift detection.
    /// </summary>
    /// <value>Defaults to <see cref="SchemaComparisonDepth.TablesAndColumns"/>.</value>
    /// <remarks>
    /// <para>
    /// Higher depth values provide more thorough comparison but require additional
    /// introspection queries. Use <see cref="SchemaComparisonDepth.Full"/> for
    /// comprehensive analysis including indexes and constraints.
    /// </para>
    /// </remarks>
    public SchemaComparisonDepth ComparisonDepth { get; set; } = SchemaComparisonDepth.TablesAndColumns;

    /// <summary>
    /// Gets or sets whether to include index comparison in drift detection.
    /// </summary>
    /// <value>Defaults to <see langword="false"/>.</value>
    /// <remarks>
    /// <para>
    /// When <see langword="true"/>, the introspector retrieves index metadata from each shard
    /// and includes index differences in the drift report. This is automatically enabled
    /// when <see cref="ComparisonDepth"/> is set to <see cref="SchemaComparisonDepth.Full"/>.
    /// </para>
    /// </remarks>
    public bool IncludeIndexes { get; set; }

    /// <summary>
    /// Gets or sets whether to include constraint comparison (primary keys, foreign keys,
    /// unique constraints) in drift detection.
    /// </summary>
    /// <value>Defaults to <see langword="false"/>.</value>
    /// <remarks>
    /// <para>
    /// When <see langword="true"/>, the introspector retrieves constraint metadata from each shard
    /// and includes constraint differences in the drift report. This is automatically enabled
    /// when <see cref="ComparisonDepth"/> is set to <see cref="SchemaComparisonDepth.Full"/>.
    /// </para>
    /// </remarks>
    public bool IncludeConstraints { get; set; }

    /// <summary>
    /// Gets or sets the list of critical table names that must be present and consistent
    /// across all shards. When set, the drift report flags any critical table that is
    /// missing or has structural differences as a critical drift.
    /// </summary>
    /// <value>Defaults to an empty list (no critical tables).</value>
    /// <remarks>
    /// <para>
    /// Table names are compared using case-insensitive matching. This is useful for
    /// ensuring that essential tables (e.g., orders, payments) are always consistent
    /// across all shards, even when other tables may have acceptable drift.
    /// </para>
    /// </remarks>
    public IList<string> CriticalTables { get; set; } = [];
}
