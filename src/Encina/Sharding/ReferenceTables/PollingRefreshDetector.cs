using LanguageExt;
using Microsoft.Extensions.Logging;

namespace Encina.Sharding.ReferenceTables;

/// <summary>
/// Detects changes in reference tables on the primary shard by comparing content hashes
/// between polling cycles and triggers replication when changes are detected.
/// </summary>
/// <remarks>
/// <para>
/// The detector computes a content hash on the primary shard via
/// <see cref="IReferenceTableStore.GetHashAsync{TEntity}"/> and compares it to the
/// last-known hash stored in <see cref="IReferenceTableStateStore"/>. When the hashes
/// differ, <see cref="IReferenceTableReplicator.ReplicateAsync{TEntity}"/> is invoked
/// and the new hash is persisted.
/// </para>
/// <para>
/// This class is used by <see cref="ReferenceTableReplicationService"/> for reference
/// tables configured with <see cref="RefreshStrategy.Polling"/>.
/// </para>
/// </remarks>
internal sealed class PollingRefreshDetector(
    IReferenceTableReplicator replicator,
    IReferenceTableStoreFactory storeFactory,
    IShardTopologyProvider topologyProvider,
    IReferenceTableStateStore stateStore,
    IReferenceTableRegistry registry,
    ILogger<PollingRefreshDetector> logger)
{
    private readonly IReferenceTableReplicator _replicator = replicator ?? throw new ArgumentNullException(nameof(replicator));
    private readonly IReferenceTableStoreFactory _storeFactory = storeFactory ?? throw new ArgumentNullException(nameof(storeFactory));
    private readonly IShardTopologyProvider _topologyProvider = topologyProvider ?? throw new ArgumentNullException(nameof(topologyProvider));
    private readonly IReferenceTableStateStore _stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
    private readonly IReferenceTableRegistry _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    private readonly ILogger<PollingRefreshDetector> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Checks a single reference table for changes and replicates if needed.
    /// </summary>
    /// <param name="config">The reference table configuration to check.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// Right with <see cref="ReplicationResult"/> if replication was triggered (or no changes detected);
    /// Left with an error if the check or replication failed.
    /// </returns>
    public async Task<Either<EncinaError, ReplicationResult?>> CheckAndReplicateAsync(
        ReferenceTableConfiguration config,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(config);

        using var activity = ReferenceTableDiagnostics.StartDetectChangesActivity(config.EntityType.Name);

        var topology = _topologyProvider.GetTopology();

        // Determine primary shard
        var allShardIds = topology.AllShardIds;
        var primaryShardId = config.Options.PrimaryShardId ?? (allShardIds.Count > 0 ? allShardIds[0] : null);

        if (primaryShardId is null)
        {
            var error = EncinaErrors.Create(
                ReferenceTableErrorCodes.PrimaryShardNotFound,
                $"No primary shard found for reference table '{config.EntityType.Name}'.");
            ReferenceTableDiagnostics.Complete(activity, false, error.Message);
            return error;
        }

        var primaryShardResult = topology.GetShard(primaryShardId);

        if (primaryShardResult.IsLeft)
        {
            var error = EncinaErrors.Create(
                ReferenceTableErrorCodes.PrimaryShardNotFound,
                $"Primary shard '{primaryShardId}' not found in topology.");
            ReferenceTableDiagnostics.Complete(activity, false, error.Message);
            return error;
        }

        var primaryShard = primaryShardResult.Match(
            Right: s => s,
            Left: _ => null!);
        var primaryStore = _storeFactory.CreateForShard(primaryShard.ConnectionString);

        // Compute current hash on primary shard via reflection (we only have Type at runtime)
        var currentHashResult = await ComputeHashAsync(primaryStore, config.EntityType, cancellationToken)
            .ConfigureAwait(false);

        if (currentHashResult.IsLeft)
        {
            var error = currentHashResult.Match<Either<EncinaError, ReplicationResult?>>(
                Right: _ => null,
                Left: e => e);
            ReferenceTableDiagnostics.Complete(activity, false, "Hash computation failed");
            return error;
        }

        var currentHash = currentHashResult.Match(
            Right: h => h,
            Left: _ => string.Empty);

        // Compare with last known hash
        var lastHash = await _stateStore.GetLastHashAsync(config.EntityType, cancellationToken)
            .ConfigureAwait(false);

        if (string.Equals(currentHash, lastHash, StringComparison.Ordinal))
        {
            ReferenceTableLog.NoChangesDetected(_logger, config.EntityType.Name, currentHash);
            activity?.SetTag("reference_table.change_detected", false);
            activity?.SetTag("reference_table.hash", currentHash);
            ReferenceTableDiagnostics.Complete(activity, true);
            return (ReplicationResult?)null;
        }

        ReferenceTableLog.ChangeDetected(
            _logger,
            config.EntityType.Name,
            lastHash ?? "(none)",
            currentHash);
        activity?.SetTag("reference_table.change_detected", true);
        activity?.SetTag("reference_table.hash", currentHash);

        // Trigger replication via reflection (we only have Type at runtime)
        var replicationResult = await ReplicateByTypeAsync(config.EntityType, cancellationToken)
            .ConfigureAwait(false);

        // Save new hash and replication time on successful replication
        if (replicationResult.IsRight)
        {
            await _stateStore.SaveHashAsync(config.EntityType, currentHash, cancellationToken)
                .ConfigureAwait(false);
            await _stateStore.SaveReplicationTimeAsync(config.EntityType, DateTime.UtcNow, cancellationToken)
                .ConfigureAwait(false);
        }

        ReferenceTableDiagnostics.Complete(activity, replicationResult.IsRight);
        return replicationResult.Map<ReplicationResult?>(r => r);
    }

    private static Task<Either<EncinaError, string>> ComputeHashAsync(
        IReferenceTableStore store,
        Type entityType,
        CancellationToken cancellationToken)
    {
        var method = typeof(IReferenceTableStore)
            .GetMethod(nameof(IReferenceTableStore.GetHashAsync))!
            .MakeGenericMethod(entityType);

        return (Task<Either<EncinaError, string>>)method.Invoke(store, [cancellationToken])!;
    }

    private Task<Either<EncinaError, ReplicationResult>> ReplicateByTypeAsync(
        Type entityType,
        CancellationToken cancellationToken)
    {
        var method = typeof(IReferenceTableReplicator)
            .GetMethod(nameof(IReferenceTableReplicator.ReplicateAsync))!
            .MakeGenericMethod(entityType);

        return (Task<Either<EncinaError, ReplicationResult>>)method.Invoke(
            _replicator,
            [cancellationToken])!;
    }
}
