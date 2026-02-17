using System.Collections.Concurrent;

namespace Encina.Sharding.ReplicaSelection;

/// <summary>
/// Selects the replica with the lowest observed latency.
/// </summary>
/// <remarks>
/// <para>
/// Tracks per-replica latency measurements using a thread-safe concurrent dictionary.
/// When no latency data is available for any replica, falls back to round-robin selection.
/// </para>
/// <para>
/// Callers should report latency measurements via <see cref="ReportLatency"/> after each
/// query execution to keep the selector's view of replica performance up to date.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var selector = new LeastLatencyShardReplicaSelector();
///
/// // After executing a query, report the observed latency
/// selector.ReportLatency("Server=replica1;...", TimeSpan.FromMilliseconds(5));
/// selector.ReportLatency("Server=replica2;...", TimeSpan.FromMilliseconds(12));
///
/// // Future selections will prefer replica1 (lower latency)
/// var selected = selector.SelectReplica(replicas);
/// </code>
/// </example>
public sealed class LeastLatencyShardReplicaSelector : IShardReplicaSelector
{
    private readonly ConcurrentDictionary<string, double> _latencies = new(StringComparer.Ordinal);
    private int _fallbackCounter = -1;

    /// <inheritdoc />
    public string SelectReplica(IReadOnlyList<string> availableReplicas)
    {
        ArgumentNullException.ThrowIfNull(availableReplicas);

        if (availableReplicas.Count == 0)
        {
            throw new ArgumentException("Available replicas list must contain at least one element.", nameof(availableReplicas));
        }

        if (availableReplicas.Count == 1)
        {
            return availableReplicas[0];
        }

        // Find the replica with the lowest latency among available replicas
        string? bestReplica = null;
        var bestLatency = double.MaxValue;
        var hasAnyLatencyData = false;

        for (var i = 0; i < availableReplicas.Count; i++)
        {
            if (_latencies.TryGetValue(availableReplicas[i], out var latency))
            {
                hasAnyLatencyData = true;
                if (latency < bestLatency)
                {
                    bestLatency = latency;
                    bestReplica = availableReplicas[i];
                }
            }
        }

        if (hasAnyLatencyData && bestReplica is not null)
        {
            return bestReplica;
        }

        // Fallback to round-robin when no latency data is available
        var next = Interlocked.Increment(ref _fallbackCounter);
        var index = (int)((uint)next % (uint)availableReplicas.Count);
        return availableReplicas[index];
    }

    /// <summary>
    /// Reports a latency measurement for a replica.
    /// </summary>
    /// <param name="replicaConnectionString">The replica connection string.</param>
    /// <param name="latency">The observed latency.</param>
    /// <remarks>
    /// Uses exponential moving average (alpha = 0.3) to smooth latency observations
    /// and avoid overreacting to transient spikes.
    /// </remarks>
    public void ReportLatency(string replicaConnectionString, TimeSpan latency)
    {
        ArgumentNullException.ThrowIfNull(replicaConnectionString);

        var ms = latency.TotalMilliseconds;
        const double alpha = 0.3;

        _latencies.AddOrUpdate(
            replicaConnectionString,
            ms,
            (_, existing) => (alpha * ms) + ((1 - alpha) * existing));
    }

    /// <summary>
    /// Resets all latency measurements.
    /// </summary>
    public void Reset() => _latencies.Clear();
}
