using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using Encina.Messaging.ReadWriteSeparation;

namespace Encina.Benchmarks.ReadWriteSeparation;

/// <summary>
/// Thread-safety benchmarks with ThreadingDiagnoser to measure contention behavior
/// under varying parallelism levels.
/// </summary>
/// <remarks>
/// <para>
/// These benchmarks measure how each replica selection strategy performs under
/// concurrent access. The ThreadingDiagnoser provides insights into thread
/// contention and lock behavior.
/// </para>
/// <para>
/// Key metrics to observe:
/// <list type="bullet">
/// <item>RoundRobin: Should scale linearly (uses Interlocked)</item>
/// <item>Random: Should scale linearly (Random.Shared is thread-safe)</item>
/// <item>LeastConnections: May show contention (uses lock and ConcurrentDictionary)</item>
/// </list>
/// </para>
/// </remarks>
[MemoryDiagnoser]
[ThreadingDiagnoser]
#pragma warning disable CA1001 // BenchmarkDotNet handles disposal via GlobalCleanup
public class ConcurrentReplicaSelectionBenchmarks
#pragma warning restore CA1001
{
    private readonly IReadOnlyList<string> _replicas =
    [
        "Server=replica1.db.local;Database=app;",
        "Server=replica2.db.local;Database=app;",
        "Server=replica3.db.local;Database=app;",
        "Server=replica4.db.local;Database=app;",
        "Server=replica5.db.local;Database=app;"
    ];

    private RoundRobinReplicaSelector _roundRobin = null!;
    private RandomReplicaSelector _random = null!;
    private LeastConnectionsReplicaSelector _leastConnections = null!;

    /// <summary>
    /// Number of threads to use for concurrent selection.
    /// Tests scaling from single-threaded to high contention scenarios.
    /// </summary>
    [Params(1, 4, 8, 16)]
    public int ThreadCount { get; set; }

    /// <summary>
    /// Initializes all three replica selectors.
    /// </summary>
    [GlobalSetup]
    public void GlobalSetup()
    {
        _roundRobin = new RoundRobinReplicaSelector(_replicas);
        _random = new RandomReplicaSelector(_replicas);
        _leastConnections = new LeastConnectionsReplicaSelector(_replicas);
    }

    /// <summary>
    /// Benchmarks concurrent RoundRobin selection.
    /// Uses Interlocked.Increment which should scale well with thread count.
    /// </summary>
    [Benchmark(Baseline = true, Description = "Concurrent RoundRobin")]
    public void Concurrent_RoundRobin()
    {
        Parallel.For(0, 1000, new ParallelOptions { MaxDegreeOfParallelism = ThreadCount },
            _ => _roundRobin.SelectReplica());
    }

    /// <summary>
    /// Benchmarks concurrent Random selection.
    /// Random.Shared is thread-safe and should scale well.
    /// </summary>
    [Benchmark(Description = "Concurrent Random")]
    public void Concurrent_Random()
    {
        Parallel.For(0, 1000, new ParallelOptions { MaxDegreeOfParallelism = ThreadCount },
            _ => _random.SelectReplica());
    }

    /// <summary>
    /// Benchmarks concurrent LeastConnections selection.
    /// Uses lock and ConcurrentDictionary, may show contention under high parallelism.
    /// </summary>
    [Benchmark(Description = "Concurrent LeastConnections")]
    public void Concurrent_LeastConnections()
    {
        Parallel.For(0, 1000, new ParallelOptions { MaxDegreeOfParallelism = ThreadCount },
            _ => _leastConnections.SelectReplica());
    }

    /// <summary>
    /// Benchmarks concurrent LeastConnections with full acquire/release lease pattern.
    /// This is the realistic usage pattern that tracks connection counts properly.
    /// </summary>
    [Benchmark(Description = "Concurrent LeastConnections (lease)")]
    public void Concurrent_LeastConnections_WithLease()
    {
        Parallel.For(0, 1000, new ParallelOptions { MaxDegreeOfParallelism = ThreadCount },
            (int i) =>
            {
                using var lease = _leastConnections.AcquireReplica();
                var connectionString = lease.ConnectionString;
            });
    }
}
