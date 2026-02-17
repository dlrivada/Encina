using LanguageExt;

namespace Encina.Sharding.Migrations.Strategies;

/// <summary>
/// Applies the migration to a single canary shard first. If the canary succeeds,
/// the remaining shards are migrated using the <see cref="ParallelMigrationStrategy"/>.
/// </summary>
internal sealed class CanaryFirstStrategy : IMigrationStrategy
{
    private readonly ParallelMigrationStrategy _parallelStrategy = new();

    public async Task<IReadOnlyDictionary<string, ShardMigrationStatus>> ExecuteAsync(
        IReadOnlyList<ShardInfo> shards,
        Func<ShardInfo, CancellationToken, Task<Either<EncinaError, Unit>>> migrationAction,
        MigrationOptions options,
        Action<string, ShardMigrationStatus> progressTracker,
        CancellationToken cancellationToken)
    {
        if (shards.Count == 0)
        {
            return new Dictionary<string, ShardMigrationStatus>();
        }

        var allResults = new Dictionary<string, ShardMigrationStatus>(StringComparer.OrdinalIgnoreCase);

        // Phase 1: Canary — migrate the first shard only
        var canary = shards[0];
        var canaryStatus = await SequentialMigrationStrategy.ExecuteOnShardAsync(
            canary, migrationAction, options.PerShardTimeout, cancellationToken);

        allResults[canary.ShardId] = canaryStatus;
        progressTracker(canary.ShardId, canaryStatus);

        if (canaryStatus.Outcome != MigrationOutcome.Succeeded)
        {
            // Canary failed — mark remaining shards as pending
            foreach (var remaining in shards.Skip(1))
            {
                var pending = new ShardMigrationStatus(remaining.ShardId, MigrationOutcome.Pending, TimeSpan.Zero);
                allResults[remaining.ShardId] = pending;
                progressTracker(remaining.ShardId, pending);
            }

            return allResults;
        }

        // Phase 2: Rollout — migrate remaining shards in parallel
        if (shards.Count > 1)
        {
            var remaining = shards.Skip(1).ToList();
            var rolloutResults = await _parallelStrategy.ExecuteAsync(
                remaining, migrationAction, options, progressTracker, cancellationToken);

            foreach (var (shardId, status) in rolloutResults)
            {
                allResults[shardId] = status;
            }
        }

        return allResults;
    }
}
