using LanguageExt;

namespace Encina.Sharding.Migrations;

/// <summary>
/// Persists and queries migration history for individual shards.
/// </summary>
/// <remarks>
/// <para>
/// Each shard maintains its own migration history in a <c>__EncinaMigrationHistory</c> table.
/// The coordinator uses this store to track which migrations have been applied, to support
/// idempotent re-runs, and to provide history queries via
/// <see cref="IShardedMigrationCoordinator.GetAppliedMigrationsAsync"/>.
/// </para>
/// <para>
/// Provider-specific implementations are responsible for creating the history table
/// if it does not exist, using the appropriate DDL for each database engine.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Query migration history for a shard
/// var applied = await historyStore.GetAppliedAsync("shard-01", ct);
///
/// // Record a successful migration
/// await historyStore.RecordAppliedAsync("shard-01", script, elapsed, ct);
///
/// // Onboard a new shard with all historical migrations
/// await historyStore.ApplyHistoricalMigrationsAsync("shard-new", allScripts, ct);
/// </code>
/// </example>
public interface IMigrationHistoryStore
{
    /// <summary>
    /// Retrieves the IDs of all migrations that have been applied to a shard,
    /// in chronological order.
    /// </summary>
    /// <param name="shardId">The shard to query.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// Right with the list of applied migration IDs;
    /// Left with an <see cref="EncinaError"/> if the query fails.
    /// </returns>
    Task<Either<EncinaError, IReadOnlyList<string>>> GetAppliedAsync(
        string shardId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records that a migration was successfully applied to a shard.
    /// </summary>
    /// <param name="shardId">The shard that was migrated.</param>
    /// <param name="script">The migration script that was applied.</param>
    /// <param name="duration">The execution duration.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// Right with <see cref="Unit"/> on success;
    /// Left with an <see cref="EncinaError"/> if recording fails.
    /// </returns>
    Task<Either<EncinaError, Unit>> RecordAppliedAsync(
        string shardId,
        MigrationScript script,
        TimeSpan duration,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records that a migration was rolled back from a shard.
    /// </summary>
    /// <param name="shardId">The shard that was rolled back.</param>
    /// <param name="migrationId">The migration ID that was rolled back.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// Right with <see cref="Unit"/> on success;
    /// Left with an <see cref="EncinaError"/> if recording fails.
    /// </returns>
    Task<Either<EncinaError, Unit>> RecordRolledBackAsync(
        string shardId,
        string migrationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ensures the migration history table exists on the specified shard,
    /// creating it if necessary.
    /// </summary>
    /// <param name="shardInfo">The shard where the history table should exist.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// Right with <see cref="Unit"/> on success;
    /// Left with an <see cref="EncinaError"/> if table creation fails.
    /// </returns>
    Task<Either<EncinaError, Unit>> EnsureHistoryTableExistsAsync(
        ShardInfo shardInfo,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies a list of historical migrations to a new shard for onboarding.
    /// Records each migration in the history without re-executing the scripts
    /// (assumes the shard was created from a snapshot with the schema already applied).
    /// </summary>
    /// <param name="shardId">The new shard being onboarded.</param>
    /// <param name="scripts">The historical migration scripts to record.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// Right with <see cref="Unit"/> on success;
    /// Left with an <see cref="EncinaError"/> if recording fails.
    /// </returns>
    Task<Either<EncinaError, Unit>> ApplyHistoricalMigrationsAsync(
        string shardId,
        IReadOnlyList<MigrationScript> scripts,
        CancellationToken cancellationToken = default);
}
