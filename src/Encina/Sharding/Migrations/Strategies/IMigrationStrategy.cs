using LanguageExt;

namespace Encina.Sharding.Migrations.Strategies;

/// <summary>
/// Defines the execution strategy for applying a migration action across a set of shards.
/// </summary>
/// <remarks>
/// <para>
/// Implementations control the ordering, parallelism, and failure semantics of the migration
/// rollout. The <see cref="ShardedMigrationCoordinator"/> selects the appropriate strategy
/// based on <see cref="MigrationOptions.Strategy"/>.
/// </para>
/// </remarks>
internal interface IMigrationStrategy
{
    /// <summary>
    /// Executes a migration action across the given shards according to this strategy's rules.
    /// </summary>
    /// <param name="shards">The shards to migrate, in topology order.</param>
    /// <param name="migrationAction">
    /// A delegate that applies the migration to a single shard and returns
    /// <c>Right(Unit)</c> on success or <c>Left(EncinaError)</c> on failure.
    /// </param>
    /// <param name="options">Options controlling parallelism, timeouts, and failure behavior.</param>
    /// <param name="progressTracker">Callback to report per-shard progress updates.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A dictionary mapping each shard ID to its migration status.</returns>
    Task<IReadOnlyDictionary<string, ShardMigrationStatus>> ExecuteAsync(
        IReadOnlyList<ShardInfo> shards,
        Func<ShardInfo, CancellationToken, Task<Either<EncinaError, Unit>>> migrationAction,
        MigrationOptions options,
        Action<string, ShardMigrationStatus> progressTracker,
        CancellationToken cancellationToken);
}
