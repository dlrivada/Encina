using BenchmarkDotNet.Attributes;
using Encina.Sharding.ReplicaSelection;

namespace Encina.Benchmarks.Sharding;

/// <summary>
/// Benchmarks comparing all five shard replica selection strategies.
/// Establishes performance baselines for each algorithm.
/// </summary>
/// <remarks>
/// <para>
/// Expected performance targets:
/// <list type="bullet">
/// <item>RoundRobin: &lt;50ns (Interlocked.Increment + modulo)</item>
/// <item>Random: &lt;100ns (Random.Shared.Next)</item>
/// <item>LeastLatency: &lt;200ns (ConcurrentDictionary lookup + min search)</item>
/// <item>LeastConnections: &lt;200ns (ConcurrentDictionary lookup + min search)</item>
/// <item>WeightedRandom: &lt;200ns (cumulative weight + Random.Shared.Next)</item>
/// </list>
/// </para>
/// </remarks>
[MemoryDiagnoser]
[RankColumn]
[SimpleJob]
public class ShardReplicaSelectionBenchmarks
{
    private readonly IReadOnlyList<string> _replicas =
    [
        "Server=replica1.db.local;Database=app;",
        "Server=replica2.db.local;Database=app;",
        "Server=replica3.db.local;Database=app;",
        "Server=replica4.db.local;Database=app;",
        "Server=replica5.db.local;Database=app;"
    ];

    private RoundRobinShardReplicaSelector _roundRobin = null!;
    private RandomShardReplicaSelector _random = null!;
    private LeastLatencyShardReplicaSelector _leastLatency = null!;
    private LeastConnectionsShardReplicaSelector _leastConnections = null!;
    private WeightedRandomShardReplicaSelector _weightedRandom = null!;

    /// <summary>
    /// Initializes all five replica selectors with the representative replica list.
    /// Seeds LeastLatency with latency data so it doesn't fall back to round-robin.
    /// </summary>
    [GlobalSetup]
    public void GlobalSetup()
    {
        _roundRobin = new RoundRobinShardReplicaSelector();
        _random = new RandomShardReplicaSelector();

        _leastLatency = new LeastLatencyShardReplicaSelector();
        // Seed latency data for all replicas
        _leastLatency.ReportLatency(_replicas[0], TimeSpan.FromMilliseconds(5));
        _leastLatency.ReportLatency(_replicas[1], TimeSpan.FromMilliseconds(10));
        _leastLatency.ReportLatency(_replicas[2], TimeSpan.FromMilliseconds(15));
        _leastLatency.ReportLatency(_replicas[3], TimeSpan.FromMilliseconds(20));
        _leastLatency.ReportLatency(_replicas[4], TimeSpan.FromMilliseconds(25));

        _leastConnections = new LeastConnectionsShardReplicaSelector();
        // Seed connection counts for all replicas
        foreach (var replica in _replicas)
        {
            _leastConnections.IncrementConnections(replica);
        }

        _weightedRandom = new WeightedRandomShardReplicaSelector([5, 3, 1, 1, 1]);
    }

    /// <summary>
    /// Benchmarks RoundRobin shard replica selection using Interlocked.Increment.
    /// This is the baseline as it's the simplest and fastest strategy.
    /// </summary>
    [Benchmark(Baseline = true, Description = "RoundRobin.SelectReplica")]
    public string RoundRobin_SelectReplica() => _roundRobin.SelectReplica(_replicas);

    /// <summary>
    /// Benchmarks Random shard replica selection using Random.Shared.
    /// Thread-safe but slightly more overhead than RoundRobin.
    /// </summary>
    [Benchmark(Description = "Random.SelectReplica")]
    public string Random_SelectReplica() => _random.SelectReplica(_replicas);

    /// <summary>
    /// Benchmarks LeastLatency shard replica selection.
    /// Uses ConcurrentDictionary lookup and min search with EMA smoothing.
    /// </summary>
    [Benchmark(Description = "LeastLatency.SelectReplica")]
    public string LeastLatency_SelectReplica() => _leastLatency.SelectReplica(_replicas);

    /// <summary>
    /// Benchmarks LeastConnections shard replica selection.
    /// Uses ConcurrentDictionary lookup and min search.
    /// </summary>
    [Benchmark(Description = "LeastConnections.SelectReplica")]
    public string LeastConnections_SelectReplica() => _leastConnections.SelectReplica(_replicas);

    /// <summary>
    /// Benchmarks WeightedRandom shard replica selection.
    /// Uses cumulative weight distribution with Random.Shared.
    /// </summary>
    [Benchmark(Description = "WeightedRandom.SelectReplica")]
    public string WeightedRandom_SelectReplica() => _weightedRandom.SelectReplica(_replicas);
}
