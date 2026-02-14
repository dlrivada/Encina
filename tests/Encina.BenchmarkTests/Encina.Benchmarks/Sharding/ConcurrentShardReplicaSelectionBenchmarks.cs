using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using Encina.Sharding.ReplicaSelection;

namespace Encina.Benchmarks.Sharding;

/// <summary>
/// Thread-safety benchmarks for shard replica selection strategies with ThreadingDiagnoser
/// to measure contention behavior under varying parallelism levels.
/// </summary>
/// <remarks>
/// <para>
/// Key metrics to observe:
/// <list type="bullet">
/// <item>RoundRobin: Should scale linearly (uses Interlocked)</item>
/// <item>Random: Should scale linearly (Random.Shared is thread-safe)</item>
/// <item>LeastLatency: May show contention (ConcurrentDictionary reads)</item>
/// <item>LeastConnections: May show contention (ConcurrentDictionary reads)</item>
/// <item>WeightedRandom: Should scale well (read-only weights + Random.Shared)</item>
/// </list>
/// </para>
/// </remarks>
[MemoryDiagnoser]
[ThreadingDiagnoser]
public class ConcurrentShardReplicaSelectionBenchmarks
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
    /// Number of threads to use for concurrent selection.
    /// Tests scaling from single-threaded to high contention scenarios.
    /// </summary>
    [Params(1, 4, 8, 16)]
    public int ThreadCount { get; set; }

    /// <summary>
    /// Initializes all five replica selectors with seeded state.
    /// </summary>
    [GlobalSetup]
    public void GlobalSetup()
    {
        _roundRobin = new RoundRobinShardReplicaSelector();
        _random = new RandomShardReplicaSelector();

        _leastLatency = new LeastLatencyShardReplicaSelector();
        _leastLatency.ReportLatency(_replicas[0], TimeSpan.FromMilliseconds(5));
        _leastLatency.ReportLatency(_replicas[1], TimeSpan.FromMilliseconds(10));
        _leastLatency.ReportLatency(_replicas[2], TimeSpan.FromMilliseconds(15));
        _leastLatency.ReportLatency(_replicas[3], TimeSpan.FromMilliseconds(20));
        _leastLatency.ReportLatency(_replicas[4], TimeSpan.FromMilliseconds(25));

        _leastConnections = new LeastConnectionsShardReplicaSelector();
        foreach (var replica in _replicas)
        {
            _leastConnections.IncrementConnections(replica);
        }

        _weightedRandom = new WeightedRandomShardReplicaSelector([5, 3, 1, 1, 1]);
    }

    /// <summary>
    /// Benchmarks concurrent RoundRobin selection.
    /// Uses Interlocked.Increment which should scale well with thread count.
    /// </summary>
    [Benchmark(Baseline = true, Description = "Concurrent RoundRobin")]
    public void Concurrent_RoundRobin()
    {
        Parallel.For(0, 1000, new ParallelOptions { MaxDegreeOfParallelism = ThreadCount },
            _ => _roundRobin.SelectReplica(_replicas));
    }

    /// <summary>
    /// Benchmarks concurrent Random selection.
    /// Random.Shared is thread-safe and should scale well.
    /// </summary>
    [Benchmark(Description = "Concurrent Random")]
    public void Concurrent_Random()
    {
        Parallel.For(0, 1000, new ParallelOptions { MaxDegreeOfParallelism = ThreadCount },
            _ => _random.SelectReplica(_replicas));
    }

    /// <summary>
    /// Benchmarks concurrent LeastLatency selection.
    /// Uses ConcurrentDictionary reads which may show contention under high parallelism.
    /// </summary>
    [Benchmark(Description = "Concurrent LeastLatency")]
    public void Concurrent_LeastLatency()
    {
        Parallel.For(0, 1000, new ParallelOptions { MaxDegreeOfParallelism = ThreadCount },
            _ => _leastLatency.SelectReplica(_replicas));
    }

    /// <summary>
    /// Benchmarks concurrent LeastConnections selection.
    /// Uses ConcurrentDictionary reads which may show contention under high parallelism.
    /// </summary>
    [Benchmark(Description = "Concurrent LeastConnections")]
    public void Concurrent_LeastConnections()
    {
        Parallel.For(0, 1000, new ParallelOptions { MaxDegreeOfParallelism = ThreadCount },
            _ => _leastConnections.SelectReplica(_replicas));
    }

    /// <summary>
    /// Benchmarks concurrent WeightedRandom selection.
    /// Uses read-only weight array + Random.Shared which should scale well.
    /// </summary>
    [Benchmark(Description = "Concurrent WeightedRandom")]
    public void Concurrent_WeightedRandom()
    {
        Parallel.For(0, 1000, new ParallelOptions { MaxDegreeOfParallelism = ThreadCount },
            _ => _weightedRandom.SelectReplica(_replicas));
    }
}
