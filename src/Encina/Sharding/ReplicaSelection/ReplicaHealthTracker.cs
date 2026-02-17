using System.Collections.Concurrent;

namespace Encina.Sharding.ReplicaSelection;

/// <summary>
/// Default implementation of <see cref="IReplicaHealthTracker"/> using a thread-safe
/// concurrent dictionary to track per-shard, per-replica health state.
/// </summary>
/// <remarks>
/// <para>
/// Unhealthy replicas are automatically reconsidered after the configured
/// recovery delay window has elapsed since their last failure.
/// This prevents a temporary issue from permanently removing a replica from the pool.
/// </para>
/// <para>
/// All operations use lock-free concurrent data structures for high throughput
/// under concurrent access.
/// </para>
/// </remarks>
public sealed class ReplicaHealthTracker : IReplicaHealthTracker
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, MutableHealthState>> _shardStates = new(StringComparer.OrdinalIgnoreCase);
    private readonly TimeSpan _recoveryDelay;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReplicaHealthTracker"/> class.
    /// </summary>
    /// <param name="recoveryDelay">
    /// The duration after which an unhealthy replica is automatically reconsidered for selection.
    /// Default is 30 seconds.
    /// </param>
    /// <param name="timeProvider">
    /// Optional time provider for testing. Defaults to <see cref="TimeProvider.System"/>.
    /// </param>
    public ReplicaHealthTracker(TimeSpan? recoveryDelay = null, TimeProvider? timeProvider = null)
    {
        _recoveryDelay = recoveryDelay ?? TimeSpan.FromSeconds(30);
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public void MarkHealthy(string shardId, string replicaConnectionString)
    {
        ArgumentNullException.ThrowIfNull(shardId);
        ArgumentNullException.ThrowIfNull(replicaConnectionString);

        var replicaStates = GetOrCreateShardStates(shardId);
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        replicaStates.AddOrUpdate(
            replicaConnectionString,
            _ => new MutableHealthState { IsHealthy = true, LastSuccess = now },
            (_, existing) =>
            {
                existing.IsHealthy = true;
                existing.LastSuccess = now;
                return existing;
            });
    }

    /// <inheritdoc />
    public void MarkUnhealthy(string shardId, string replicaConnectionString)
    {
        ArgumentNullException.ThrowIfNull(shardId);
        ArgumentNullException.ThrowIfNull(replicaConnectionString);

        var replicaStates = GetOrCreateShardStates(shardId);
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        replicaStates.AddOrUpdate(
            replicaConnectionString,
            _ => new MutableHealthState { IsHealthy = false, LastFailure = now, FailureCount = 1 },
            (_, existing) =>
            {
                existing.IsHealthy = false;
                existing.LastFailure = now;
                Interlocked.Increment(ref existing.FailureCount);
                return existing;
            });
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetAvailableReplicas(string shardId, IReadOnlyList<string> allReplicas)
    {
        ArgumentNullException.ThrowIfNull(shardId);
        ArgumentNullException.ThrowIfNull(allReplicas);

        if (!_shardStates.TryGetValue(shardId, out var replicaStates))
        {
            return allReplicas;
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var available = new List<string>(allReplicas.Count);

        for (var i = 0; i < allReplicas.Count; i++)
        {
            var replica = allReplicas[i];

            if (!replicaStates.TryGetValue(replica, out var state))
            {
                // No state recorded — assume healthy
                available.Add(replica);
                continue;
            }

            if (state.IsHealthy)
            {
                available.Add(replica);
                continue;
            }

            // Check if the recovery window has passed
            if (state.LastFailure.HasValue && (now - state.LastFailure.Value) >= _recoveryDelay)
            {
                available.Add(replica);
            }
        }

        return available;
    }

    /// <inheritdoc />
    public ReplicaHealthState GetHealthState(string shardId, string replicaConnectionString)
    {
        ArgumentNullException.ThrowIfNull(shardId);
        ArgumentNullException.ThrowIfNull(replicaConnectionString);

        if (!_shardStates.TryGetValue(shardId, out var replicaStates))
        {
            return ReplicaHealthState.Healthy;
        }

        if (!replicaStates.TryGetValue(replicaConnectionString, out var state))
        {
            return ReplicaHealthState.Healthy;
        }

        return new ReplicaHealthState(
            state.IsHealthy,
            state.LastFailure,
            state.FailureCount,
            state.LastSuccess,
            state.ObservedReplicationLag,
            state.LagObservedAtUtc);
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, ReplicaHealthState> GetAllHealthStates(string shardId)
    {
        ArgumentNullException.ThrowIfNull(shardId);

        if (!_shardStates.TryGetValue(shardId, out var replicaStates))
        {
            return new Dictionary<string, ReplicaHealthState>();
        }

        var result = new Dictionary<string, ReplicaHealthState>(replicaStates.Count, StringComparer.Ordinal);

        foreach (var (replica, state) in replicaStates)
        {
            result[replica] = new ReplicaHealthState(
                state.IsHealthy,
                state.LastFailure,
                state.FailureCount,
                state.LastSuccess,
                state.ObservedReplicationLag,
                state.LagObservedAtUtc);
        }

        return result;
    }

    /// <inheritdoc />
    public void ReportReplicationLag(string shardId, string replicaConnectionString, TimeSpan lag)
    {
        ArgumentNullException.ThrowIfNull(shardId);
        ArgumentNullException.ThrowIfNull(replicaConnectionString);
        ArgumentOutOfRangeException.ThrowIfLessThan(lag, TimeSpan.Zero);

        var replicaStates = GetOrCreateShardStates(shardId);
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        replicaStates.AddOrUpdate(
            replicaConnectionString,
            _ => new MutableHealthState { ObservedReplicationLag = lag, LagObservedAtUtc = now },
            (_, existing) =>
            {
                existing.ObservedReplicationLag = lag;
                existing.LagObservedAtUtc = now;
                return existing;
            });
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetAvailableReplicas(string shardId, IReadOnlyList<string> allReplicas, TimeSpan? maxAcceptableLag)
    {
        ArgumentNullException.ThrowIfNull(shardId);
        ArgumentNullException.ThrowIfNull(allReplicas);

        // First apply health-based filtering
        var healthyReplicas = GetAvailableReplicas(shardId, allReplicas);

        if (!maxAcceptableLag.HasValue || healthyReplicas.Count == 0)
        {
            return healthyReplicas;
        }

        if (!_shardStates.TryGetValue(shardId, out var replicaStates))
        {
            // No lag data recorded — assume all are within threshold
            return healthyReplicas;
        }

        var threshold = maxAcceptableLag.Value;
        var available = new List<string>(healthyReplicas.Count);

        for (var i = 0; i < healthyReplicas.Count; i++)
        {
            var replica = healthyReplicas[i];

            if (!replicaStates.TryGetValue(replica, out var state) || !state.ObservedReplicationLag.HasValue)
            {
                // No lag data — assume within threshold
                available.Add(replica);
                continue;
            }

            if (state.ObservedReplicationLag.Value <= threshold)
            {
                available.Add(replica);
            }
        }

        return available;
    }

    private ConcurrentDictionary<string, MutableHealthState> GetOrCreateShardStates(string shardId)
        => _shardStates.GetOrAdd(shardId, _ => new ConcurrentDictionary<string, MutableHealthState>(StringComparer.Ordinal));

    /// <summary>
    /// Internal mutable state tracked per replica. Fields are updated via Interlocked or direct assignment
    /// within ConcurrentDictionary update delegates.
    /// </summary>
    private sealed class MutableHealthState
    {
        public volatile bool IsHealthy = true;
        public DateTime? LastFailure;
        public int FailureCount;
        public DateTime? LastSuccess;
        public TimeSpan? ObservedReplicationLag;
        public DateTime? LagObservedAtUtc;
    }
}
