using System.Diagnostics;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static LanguageExt.Prelude;

namespace Encina.Sharding.ReferenceTables;

/// <summary>
/// Default implementation of <see cref="IReferenceTableReplicator"/> that reads data from
/// the primary shard and upserts it to all active target shards in parallel.
/// </summary>
/// <remarks>
/// <para>
/// The replicator follows the scatter-gather pattern used elsewhere in the sharding
/// infrastructure, honoring <see cref="ReferenceTableGlobalOptions.MaxParallelShards"/>
/// for parallelism control.
/// </para>
/// <para>
/// Partial failures are tracked per-shard and returned in the <see cref="ReplicationResult"/>.
/// The replicator continues to remaining shards even if some fail, unless the primary
/// read itself fails (which aborts the entire operation).
/// </para>
/// </remarks>
internal sealed class ReferenceTableReplicator(
    IReferenceTableRegistry registry,
    IShardTopologyProvider topologyProvider,
    IReferenceTableStoreFactory storeFactory,
    IOptions<ReferenceTableGlobalOptions> globalOptions,
    ILogger<ReferenceTableReplicator> logger) : IReferenceTableReplicator
{
    private readonly IReferenceTableRegistry _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    private readonly IShardTopologyProvider _topologyProvider = topologyProvider ?? throw new ArgumentNullException(nameof(topologyProvider));
    private readonly IReferenceTableStoreFactory _storeFactory = storeFactory ?? throw new ArgumentNullException(nameof(storeFactory));
    private readonly ReferenceTableGlobalOptions _globalOptions = globalOptions?.Value ?? throw new ArgumentNullException(nameof(globalOptions));
    private readonly ILogger<ReferenceTableReplicator> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async Task<Either<EncinaError, ReplicationResult>> ReplicateAsync<TEntity>(
        CancellationToken cancellationToken = default)
        where TEntity : class
    {
        if (!_registry.IsRegistered<TEntity>())
        {
            return EncinaErrors.Create(
                ReferenceTableErrorCodes.EntityNotRegistered,
                $"Entity type '{typeof(TEntity).Name}' is not registered as a reference table.");
        }

        var config = _registry.GetConfiguration<TEntity>();
        var topology = _topologyProvider.GetTopology();

        return await ReplicateEntityAsync<TEntity>(config, topology, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, ReplicationResult>> ReplicateAllAsync(
        CancellationToken cancellationToken = default)
    {
        var configurations = _registry.GetAllConfigurations();

        if (configurations.Count == 0)
        {
            return new ReplicationResult(0, TimeSpan.Zero, [], []);
        }

        var topology = _topologyProvider.GetTopology();
        var stopwatch = Stopwatch.StartNew();

        var totalRowsSynced = 0;
        var allShardResults = new List<ShardReplicationResult>();
        var allFailedShards = new List<ShardFailure>();

        foreach (var config in configurations)
        {
            var result = await ReplicateEntityCoreAsync(config, topology, cancellationToken)
                .ConfigureAwait(false);

            result.Match(
                Right: rep =>
                {
                    totalRowsSynced += rep.RowsSynced;
                    allShardResults.AddRange(rep.ShardResults);
                    allFailedShards.AddRange(rep.FailedShards);
                },
                Left: error =>
                {
                    ReferenceTableLog.ReplicationFailed(
                        _logger,
                        config.EntityType.Name,
                        error.Message);
                });
        }

        stopwatch.Stop();

        return new ReplicationResult(
            totalRowsSynced,
            stopwatch.Elapsed,
            allShardResults.AsReadOnly(),
            allFailedShards.AsReadOnly());
    }

    private async Task<Either<EncinaError, ReplicationResult>> ReplicateEntityAsync<TEntity>(
        ReferenceTableConfiguration config,
        ShardTopology topology,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        using var replicateActivity = ReferenceTableDiagnostics.StartReplicateActivity(typeof(TEntity).Name);
        var stopwatch = Stopwatch.StartNew();

        // Determine primary shard
        var allShardIds = topology.AllShardIds;
        var primaryShardId = config.Options.PrimaryShardId ?? (allShardIds.Count > 0 ? allShardIds[0] : null);

        if (primaryShardId is null)
        {
            return EncinaErrors.Create(
                ReferenceTableErrorCodes.PrimaryShardNotFound,
                $"No primary shard found for reference table '{typeof(TEntity).Name}'.");
        }

        var primaryShardResult = topology.GetShard(primaryShardId);

        if (primaryShardResult.IsLeft)
        {
            return EncinaErrors.Create(
                ReferenceTableErrorCodes.PrimaryShardNotFound,
                $"Primary shard '{primaryShardId}' for reference table '{typeof(TEntity).Name}' " +
                "does not exist in the topology.");
        }

        var primaryShard = primaryShardResult.Match(
            Right: s => s,
            Left: _ => null!);

        // Get target shards (all active shards except the primary)
        var targetShards = topology.GetActiveShards()
            .Where(s => !string.Equals(s.ShardId, primaryShardId, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (targetShards.Count == 0)
        {
            _logger.LogDebug(
                "No target shards for reference table '{EntityType}' — primary is the only active shard",
                typeof(TEntity).Name);

            stopwatch.Stop();
            return new ReplicationResult(0, stopwatch.Elapsed, [], []);
        }

        // Read all data from the primary shard
        var primaryStore = _storeFactory.CreateForShard(primaryShard.ConnectionString);
        var readResult = await primaryStore.GetAllAsync<TEntity>(cancellationToken).ConfigureAwait(false);

        if (readResult.IsLeft)
        {
            return EncinaErrors.Create(
                ReferenceTableErrorCodes.PrimaryReadFailed,
                $"Failed to read reference table data for '{typeof(TEntity).Name}' " +
                $"from primary shard '{primaryShardId}'.");
        }

        var entities = readResult.Match(
            Right: r => r,
            Left: _ => (IReadOnlyList<TEntity>)[]);

        _logger.LogDebug(
            "Read {RowCount} rows from primary shard '{PrimaryShardId}' for reference table '{EntityType}'",
            entities.Count,
            primaryShardId,
            typeof(TEntity).Name);

        // Replicate to target shards in parallel
        var parallelism = _globalOptions.MaxParallelShards > 0
            ? _globalOptions.MaxParallelShards
            : Environment.ProcessorCount;

        var semaphore = new SemaphoreSlim(parallelism, parallelism);
        var shardResults = new List<ShardReplicationResult>();
        var failedShards = new List<ShardFailure>();
        var lockObj = new object();

        var tasks = targetShards.Select(async shard =>
        {
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                using var syncActivity = ReferenceTableDiagnostics.StartSyncShardActivity(
                    typeof(TEntity).Name, shard.ShardId);

                var shardStopwatch = Stopwatch.StartNew();
                var shardStore = _storeFactory.CreateForShard(shard.ConnectionString);

                var upsertResult = await shardStore
                    .UpsertAsync<TEntity>(entities, cancellationToken)
                    .ConfigureAwait(false);

                shardStopwatch.Stop();

                upsertResult.Match(
                    Right: rows =>
                    {
                        syncActivity?.SetTag("reference_table.rows_synced", rows);
                        ReferenceTableDiagnostics.Complete(syncActivity, true);

                        lock (lockObj)
                        {
                            shardResults.Add(new ShardReplicationResult(
                                shard.ShardId,
                                rows,
                                shardStopwatch.Elapsed));
                        }
                    },
                    Left: error =>
                    {
                        ReferenceTableDiagnostics.Complete(syncActivity, false, error.Message);

                        lock (lockObj)
                        {
                            failedShards.Add(new ShardFailure(shard.ShardId, error));
                        }
                    });
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(
                    ex,
                    "Unexpected error replicating to shard '{ShardId}' for reference table '{EntityType}'",
                    shard.ShardId,
                    typeof(TEntity).Name);

                lock (lockObj)
                {
                    failedShards.Add(new ShardFailure(
                        shard.ShardId,
                        EncinaErrors.FromException(
                            ReferenceTableErrorCodes.ReplicationFailed,
                            ex,
                            $"Unexpected error replicating to shard '{shard.ShardId}'.")));
                }
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks).ConfigureAwait(false);

        stopwatch.Stop();

        var totalRowsSynced = shardResults.Sum(r => r.RowsUpserted);

        if (failedShards.Count > 0 && shardResults.Count == 0)
        {
            ReferenceTableDiagnostics.Complete(replicateActivity, false,
                $"All {failedShards.Count} target shards failed");

            return EncinaErrors.Create(
                ReferenceTableErrorCodes.ReplicationFailed,
                $"Replication failed on all {failedShards.Count} target shards " +
                $"for reference table '{typeof(TEntity).Name}'.");
        }

        var result = new ReplicationResult(
            totalRowsSynced,
            stopwatch.Elapsed,
            shardResults.AsReadOnly(),
            failedShards.AsReadOnly());

        replicateActivity?.SetTag("reference_table.rows_synced", totalRowsSynced);
        replicateActivity?.SetTag("reference_table.shard_count", shardResults.Count);
        replicateActivity?.SetTag("reference_table.duration_ms", stopwatch.Elapsed.TotalMilliseconds);

        if (result.IsPartial)
        {
            ReferenceTableLog.ReplicationPartialFailure(
                _logger,
                typeof(TEntity).Name,
                shardResults.Count,
                result.TotalShardsTargeted,
                failedShards.Count);

            ReferenceTableDiagnostics.Complete(replicateActivity, false,
                $"{failedShards.Count} of {result.TotalShardsTargeted} shards failed");
        }
        else
        {
            ReferenceTableLog.ReplicationCompleted(
                _logger,
                typeof(TEntity).Name,
                totalRowsSynced,
                shardResults.Count,
                stopwatch.Elapsed.TotalMilliseconds);

            ReferenceTableDiagnostics.Complete(replicateActivity, true);
        }

        return result;
    }

    private Task<Either<EncinaError, ReplicationResult>> ReplicateEntityCoreAsync(
        ReferenceTableConfiguration config,
        ShardTopology topology,
        CancellationToken cancellationToken)
    {
        // Use reflection to call the generic ReplicateEntityAsync<TEntity> method
        // since we only have the Type at runtime from the registry
        var method = typeof(ReferenceTableReplicator)
            .GetMethod(nameof(ReplicateEntityAsync),
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .MakeGenericMethod(config.EntityType);

        return (Task<Either<EncinaError, ReplicationResult>>)method.Invoke(
            this,
            [config, topology, cancellationToken])!;
    }
}
