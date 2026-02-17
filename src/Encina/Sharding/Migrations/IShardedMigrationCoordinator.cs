using LanguageExt;

namespace Encina.Sharding.Migrations;

/// <summary>
/// Coordinates the application and rollback of schema migrations across all shards
/// in the topology, with progress reporting and drift detection.
/// </summary>
/// <remarks>
/// <para>
/// All operations return <see cref="Either{EncinaError, T}"/> following the Railway Oriented
/// Programming pattern. Error codes from <see cref="MigrationErrorCodes"/> are used throughout.
/// </para>
/// <para>
/// Implementations are provider-specific: ADO.NET and Dapper providers execute raw DDL,
/// EF Core providers leverage <c>Database.Migrate()</c>, and MongoDB providers use
/// index/validator commands.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Apply a migration using the canary-first strategy
/// var script = new MigrationScript(
///     Id: "20260216_add_status_index",
///     UpSql: "CREATE INDEX idx_orders_status ON orders (status);",
///     DownSql: "DROP INDEX idx_orders_status;",
///     Description: "Add index on orders.status",
///     Checksum: "sha256:abc123...");
///
/// var options = new MigrationOptions
/// {
///     Strategy = MigrationStrategy.CanaryFirst,
///     MaxParallelism = 4,
///     StopOnFirstFailure = true
/// };
///
/// var result = await coordinator.ApplyToAllShardsAsync(script, options, ct);
///
/// result.Match(
///     Right: r =>
///     {
///         if (r.AllSucceeded)
///             logger.LogInformation("Migration applied to all shards");
///         else
///             logger.LogWarning("{Failed} shards failed", r.FailedCount);
///     },
///     Left: error => logger.LogError("Migration coordination error: {Error}", error.Message));
/// </code>
/// </example>
public interface IShardedMigrationCoordinator
{
    /// <summary>
    /// Applies a migration script to all shards in the topology using the specified strategy.
    /// </summary>
    /// <param name="script">The migration script containing forward and reverse DDL.</param>
    /// <param name="options">Options controlling strategy, parallelism, timeouts, and failure behavior.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// Right with a <see cref="MigrationResult"/> containing per-shard outcomes;
    /// Left with an <see cref="EncinaError"/> if the coordination itself fails.
    /// </returns>
    Task<Either<EncinaError, MigrationResult>> ApplyToAllShardsAsync(
        MigrationScript script,
        MigrationOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back a previously applied migration using the <see cref="MigrationScript.DownSql"/>
    /// for each shard that was successfully migrated.
    /// </summary>
    /// <param name="result">
    /// The <see cref="MigrationResult"/> from a previous <see cref="ApplyToAllShardsAsync"/> call.
    /// Only shards with <see cref="MigrationOutcome.Succeeded"/> are rolled back.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// Right with <see cref="Unit"/> on success;
    /// Left with an <see cref="EncinaError"/> if the rollback fails.
    /// </returns>
    Task<Either<EncinaError, Unit>> RollbackAsync(
        MigrationResult result,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Compares the schema of all shards against a baseline to detect drift.
    /// </summary>
    /// <param name="options">
    /// Options controlling which shard is used as baseline and comparison depth.
    /// When <see langword="null"/>, the coordinator selects defaults automatically.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// Right with a <see cref="SchemaDriftReport"/> describing any detected differences;
    /// Left with an <see cref="EncinaError"/> if the comparison fails.
    /// </returns>
    Task<Either<EncinaError, SchemaDriftReport>> DetectDriftAsync(
        DriftDetectionOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current progress of an in-flight migration execution.
    /// </summary>
    /// <param name="migrationId">The unique identifier of the migration execution.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// Right with a <see cref="MigrationProgress"/> snapshot;
    /// Left with an <see cref="EncinaError"/> if the migration ID is unknown.
    /// </returns>
    Task<Either<EncinaError, MigrationProgress>> GetProgressAsync(
        Guid migrationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the list of migrations that have been applied to a specific shard.
    /// </summary>
    /// <param name="shardId">The shard to query for migration history.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// Right with the list of applied migration script IDs in chronological order;
    /// Left with an <see cref="EncinaError"/> if the shard is not found or the query fails.
    /// </returns>
    Task<Either<EncinaError, IReadOnlyList<string>>> GetAppliedMigrationsAsync(
        string shardId,
        CancellationToken cancellationToken = default);
}
