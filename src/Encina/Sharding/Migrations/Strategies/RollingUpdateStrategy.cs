using System.Collections.Concurrent;
using LanguageExt;

namespace Encina.Sharding.Migrations.Strategies;

/// <summary>
/// Divides shards into batches of <see cref="MigrationOptions.MaxParallelism"/> and executes
/// each batch in parallel. Each batch must succeed before the next batch starts.
/// </summary>
internal sealed class RollingUpdateStrategy : IMigrationStrategy
{
    public async Task<IReadOnlyDictionary<string, ShardMigrationStatus>> ExecuteAsync(
        IReadOnlyList<ShardInfo> shards,
        Func<ShardInfo, CancellationToken, Task<Either<EncinaError, Unit>>> migrationAction,
        MigrationOptions options,
        Action<string, ShardMigrationStatus> progressTracker,
        CancellationToken cancellationToken)
    {
        var allResults = new Dictionary<string, ShardMigrationStatus>(StringComparer.OrdinalIgnoreCase);
        var batchSize = options.MaxParallelism <= 0 ? shards.Count : options.MaxParallelism;

        var batches = shards
            .Select((shard, index) => (shard, index))
            .GroupBy(x => x.index / batchSize)
            .Select(g => g.Select(x => x.shard).ToList())
            .ToList();

        for (var batchIndex = 0; batchIndex < batches.Count; batchIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var batch = batches[batchIndex];
            var batchResults = new ConcurrentDictionary<string, ShardMigrationStatus>(StringComparer.OrdinalIgnoreCase);

            var tasks = batch.Select(async shard =>
            {
                var status = await SequentialMigrationStrategy.ExecuteOnShardAsync(
                    shard, migrationAction, options.PerShardTimeout, cancellationToken);
                batchResults[shard.ShardId] = status;
                progressTracker(shard.ShardId, status);
            });

            await Task.WhenAll(tasks);

            // Merge batch results
            foreach (var (shardId, status) in batchResults)
            {
                allResults[shardId] = status;
            }

            // Check for failures in this batch
            var batchFailed = batchResults.Values.Any(s => s.Outcome == MigrationOutcome.Failed);

            if (batchFailed && options.StopOnFirstFailure)
            {
                // Mark remaining batches as pending
                foreach (var remainingBatch in batches.Skip(batchIndex + 1))
                {
                    foreach (var shard in remainingBatch)
                    {
                        var pending = new ShardMigrationStatus(shard.ShardId, MigrationOutcome.Pending, TimeSpan.Zero);
                        allResults[shard.ShardId] = pending;
                        progressTracker(shard.ShardId, pending);
                    }
                }

                break;
            }
        }

        return allResults;
    }
}
