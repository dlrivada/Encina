namespace Encina.Sharding.Migrations;

/// <summary>
/// Error codes emitted by the Encina sharded migration coordination infrastructure.
/// </summary>
/// <remarks>
/// <para>
/// All error codes follow the <c>encina.sharding.migration.*</c> namespace convention and are
/// returned inside <c>Either&lt;EncinaError, T&gt;</c> results from
/// <see cref="IShardedMigrationCoordinator"/> methods. These codes are also emitted as
/// OpenTelemetry tags (<c>encina.sharding.migration.error.code</c>) on migration activity
/// spans, enabling correlation between ROP error paths and distributed traces.
/// </para>
/// <para>
/// Error codes are stable string constants suitable for alerting rules, log filters, and
/// dashboard queries. They never change between releases.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var result = await coordinator.ApplyToAllShardsAsync(script, options, ct);
///
/// result.Match(
///     Right: r => logger.LogInformation("Migration succeeded"),
///     Left: error =>
///     {
///         if (error.GetCode() == MigrationErrorCodes.MigrationTimeout)
///             logger.LogWarning("Migration timed out — consider increasing PerShardTimeout");
///         else if (error.GetCode() == MigrationErrorCodes.DriftDetected)
///             logger.LogWarning("Schema drift detected — run DetectDriftAsync for details");
///     });
/// </code>
/// </example>
public static class MigrationErrorCodes
{
    /// <summary>A migration script failed to apply on one or more shards.</summary>
    public const string MigrationFailed = "encina.sharding.migration.migration_failed";

    /// <summary>A per-shard migration exceeded the configured <see cref="MigrationOptions.PerShardTimeout"/>.</summary>
    public const string MigrationTimeout = "encina.sharding.migration.migration_timeout";

    /// <summary>A rollback operation failed on one or more shards.</summary>
    public const string RollbackFailed = "encina.sharding.migration.rollback_failed";

    /// <summary>Schema drift was detected across shards during validation or drift detection.</summary>
    public const string DriftDetected = "encina.sharding.migration.drift_detected";

    /// <summary>The migration script is invalid (e.g., checksum mismatch, empty SQL, malformed ID).</summary>
    public const string InvalidScript = "encina.sharding.migration.invalid_script";

    /// <summary>A schema comparison operation failed (e.g., unable to read <c>INFORMATION_SCHEMA</c>).</summary>
    public const string SchemaComparisonFailed = "encina.sharding.migration.schema_comparison_failed";

    /// <summary>The requested migration was not found (e.g., unknown migration ID for progress or rollback).</summary>
    public const string MigrationNotFound = "encina.sharding.migration.migration_not_found";
}
