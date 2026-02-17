using System.Collections.Concurrent;
using LanguageExt;

namespace Encina.Sharding.Migrations.Strategies;

/// <summary>
/// Applies the migration to all shards simultaneously, throttled by
/// <see cref="MigrationOptions.MaxParallelism"/> using a semaphore
/// (following the <c>ShardedQueryExecutor</c> pattern).
/// </summary>
internal sealed class ParallelMigrationStrategy : IMigrationStrategy
{
    public async Task<IReadOnlyDictionary<string, ShardMigrationStatus>> ExecuteAsync(
        IReadOnlyList<ShardInfo> shards,
        Func<ShardInfo, CancellationToken, Task<Either<EncinaError, Unit>>> migrationAction,
        MigrationOptions options,
        Action<string, ShardMigrationStatus> progressTracker,
        CancellationToken cancellationToken)
    {
        var results = new ConcurrentDictionary<string, ShardMigrationStatus>(StringComparer.OrdinalIgnoreCase);

        // Optional: cancel remaining shards on first failure
        using var failureCts = options.StopOnFirstFailure
            ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)
            : null;
        var effectiveToken = failureCts?.Token ?? cancellationToken;

        var maxParallelism = options.MaxParallelism <= 0
            ? shards.Count
            : Math.Min(options.MaxParallelism, shards.Count);

        using var semaphore = new SemaphoreSlim(maxParallelism, maxParallelism);

        var tasks = shards.Select(shard => ExecuteWithSemaphoreAsync(
            shard, migrationAction, options, semaphore, results, progressTracker, failureCts, effectiveToken));

        try
        {
            await Task.WhenAll(tasks);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw; // Propagate caller cancellation
        }
        catch (OperationCanceledException)
        {
            // StopOnFirstFailure triggered — mark un-started shards as pending
        }

        // Fill in any shards that didn't start
        foreach (var shard in shards.Where(s => !results.ContainsKey(s.ShardId)))
        {
            var pending = new ShardMigrationStatus(shard.ShardId, MigrationOutcome.Pending, TimeSpan.Zero);
            results[shard.ShardId] = pending;
            progressTracker(shard.ShardId, pending);
        }

        return results;
    }

    private static async Task ExecuteWithSemaphoreAsync(
        ShardInfo shard,
        Func<ShardInfo, CancellationToken, Task<Either<EncinaError, Unit>>> migrationAction,
        MigrationOptions options,
        SemaphoreSlim semaphore,
        ConcurrentDictionary<string, ShardMigrationStatus> results,
        Action<string, ShardMigrationStatus> progressTracker,
        CancellationTokenSource? failureCts,
        CancellationToken cancellationToken)
    {
        await semaphore.WaitAsync(cancellationToken);

        try
        {
            var status = await SequentialMigrationStrategy.ExecuteOnShardAsync(
                shard, migrationAction, options.PerShardTimeout, cancellationToken);

            results[shard.ShardId] = status;
            progressTracker(shard.ShardId, status);

            if (status.Outcome == MigrationOutcome.Failed && failureCts is not null)
            {
                await failureCts.CancelAsync();
            }
        }
        finally
        {
            semaphore.Release();
        }
    }
}
