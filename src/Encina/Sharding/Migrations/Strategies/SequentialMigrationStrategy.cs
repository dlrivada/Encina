using System.Diagnostics;
using LanguageExt;

namespace Encina.Sharding.Migrations.Strategies;

/// <summary>
/// Applies the migration to one shard at a time, in order.
/// Stops immediately on first failure when <see cref="MigrationOptions.StopOnFirstFailure"/> is set.
/// </summary>
internal sealed class SequentialMigrationStrategy : IMigrationStrategy
{
    public async Task<IReadOnlyDictionary<string, ShardMigrationStatus>> ExecuteAsync(
        IReadOnlyList<ShardInfo> shards,
        Func<ShardInfo, CancellationToken, Task<Either<EncinaError, Unit>>> migrationAction,
        MigrationOptions options,
        Action<string, ShardMigrationStatus> progressTracker,
        CancellationToken cancellationToken)
    {
        var results = new Dictionary<string, ShardMigrationStatus>(StringComparer.OrdinalIgnoreCase);

        foreach (var shard in shards)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var status = await ExecuteOnShardAsync(shard, migrationAction, options.PerShardTimeout, cancellationToken);
            results[shard.ShardId] = status;
            progressTracker(shard.ShardId, status);

            if (status.Outcome == MigrationOutcome.Failed && options.StopOnFirstFailure)
            {
                // Mark remaining shards as pending
                foreach (var remaining in shards.Where(s => !results.ContainsKey(s.ShardId)))
                {
                    var pending = new ShardMigrationStatus(remaining.ShardId, MigrationOutcome.Pending, TimeSpan.Zero);
                    results[remaining.ShardId] = pending;
                    progressTracker(remaining.ShardId, pending);
                }

                break;
            }
        }

        return results;
    }

    internal static async Task<ShardMigrationStatus> ExecuteOnShardAsync(
        ShardInfo shard,
        Func<ShardInfo, CancellationToken, Task<Either<EncinaError, Unit>>> migrationAction,
        TimeSpan perShardTimeout,
        CancellationToken cancellationToken)
    {
        using var timeoutCts = new CancellationTokenSource(perShardTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        var start = Stopwatch.GetTimestamp();

        try
        {
            var result = await migrationAction(shard, linkedCts.Token);
            var elapsed = Stopwatch.GetElapsedTime(start);

            return result.Match(
                Right: _ => new ShardMigrationStatus(shard.ShardId, MigrationOutcome.Succeeded, elapsed),
                Left: error => new ShardMigrationStatus(shard.ShardId, MigrationOutcome.Failed, elapsed, error));
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            var elapsed = Stopwatch.GetElapsedTime(start);
            var error = EncinaErrors.Create(
                MigrationErrorCodes.MigrationTimeout,
                $"Migration timed out on shard '{shard.ShardId}' after {perShardTimeout}.");
            return new ShardMigrationStatus(shard.ShardId, MigrationOutcome.Failed, elapsed, error);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw; // Propagate caller cancellation
        }
        catch (Exception ex)
        {
            var elapsed = Stopwatch.GetElapsedTime(start);
            var error = EncinaErrors.Create(
                MigrationErrorCodes.MigrationFailed,
                $"Migration failed on shard '{shard.ShardId}': {ex.Message}",
                ex);
            return new ShardMigrationStatus(shard.ShardId, MigrationOutcome.Failed, elapsed, error);
        }
    }
}
