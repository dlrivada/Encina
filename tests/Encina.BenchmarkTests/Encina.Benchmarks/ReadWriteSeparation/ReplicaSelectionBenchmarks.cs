using BenchmarkDotNet.Attributes;
using Encina.Messaging.ReadWriteSeparation;

namespace Encina.Benchmarks.ReadWriteSeparation;

/// <summary>
/// Benchmarks comparing all three replica selection strategies.
/// Establishes performance baselines for each algorithm.
/// </summary>
/// <remarks>
/// <para>
/// Expected performance targets:
/// <list type="bullet">
/// <item>RoundRobin: &lt;50ns (Interlocked.Increment + modulo)</item>
/// <item>Random: &lt;100ns (Random.Shared.Next)</item>
/// <item>LeastConnections: &lt;500ns (lock + min search)</item>
/// </list>
/// </para>
/// </remarks>
[MemoryDiagnoser]
[RankColumn]
[SimpleJob]
#pragma warning disable CA1001 // BenchmarkDotNet handles disposal via GlobalCleanup
public class ReplicaSelectionBenchmarks
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
    /// Initializes all three replica selectors with the representative replica list.
    /// </summary>
    [GlobalSetup]
    public void GlobalSetup()
    {
        _roundRobin = new RoundRobinReplicaSelector(_replicas);
        _random = new RandomReplicaSelector(_replicas);
        _leastConnections = new LeastConnectionsReplicaSelector(_replicas);
    }

    /// <summary>
    /// Benchmarks RoundRobin replica selection using Interlocked.Increment.
    /// This is the baseline as it's the simplest and fastest strategy.
    /// </summary>
    [Benchmark(Baseline = true, Description = "RoundRobin.SelectReplica")]
    public string RoundRobin_SelectReplica() => _roundRobin.SelectReplica();

    /// <summary>
    /// Benchmarks Random replica selection using Random.Shared.
    /// Thread-safe but slightly more overhead than RoundRobin.
    /// </summary>
    [Benchmark(Description = "Random.SelectReplica")]
    public string Random_SelectReplica() => _random.SelectReplica();

    /// <summary>
    /// Benchmarks LeastConnections replica selection.
    /// Uses lock and iterates through replicas to find minimum.
    /// </summary>
    [Benchmark(Description = "LeastConnections.SelectReplica")]
    public string LeastConnections_SelectReplica() => _leastConnections.SelectReplica();

    /// <summary>
    /// Benchmarks the full acquire/release cycle with LeastConnections.
    /// This is the recommended usage pattern for this strategy.
    /// </summary>
    [Benchmark(Description = "LeastConnections.AcquireReplica (lease)")]
    public string LeastConnections_AcquireReplica_WithLease()
    {
        using var lease = _leastConnections.AcquireReplica();
        return lease.ConnectionString;
    }
}
