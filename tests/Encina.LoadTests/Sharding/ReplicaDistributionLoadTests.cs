using System.Collections.Concurrent;
using System.Diagnostics;
using Encina.Sharding.ReplicaSelection;

namespace Encina.LoadTests.Sharding;

/// <summary>
/// Load tests for sharded replica selection under high concurrent traffic.
/// Validates distribution fairness and thread-safety of all selection strategies.
/// </summary>
/// <remarks>
/// Run via: <c>dotnet run --project tests/Encina.LoadTests -- --scenario replicas</c>
/// (requires integration with Program.cs scenario routing, or run directly via <see cref="RunAllAsync"/>).
/// </remarks>
internal static class ReplicaDistributionLoadTests
{
    private static readonly IReadOnlyList<string> Replicas =
    [
        "Server=replica1.db.local;Database=app;",
        "Server=replica2.db.local;Database=app;",
        "Server=replica3.db.local;Database=app;",
        "Server=replica4.db.local;Database=app;",
        "Server=replica5.db.local;Database=app;",
    ];

    private const int ConcurrentWorkers = 50;
    private const int SelectionsPerWorker = 10_000;

    public static async Task RunAllAsync()
    {
        Console.WriteLine();
        Console.WriteLine("=== Replica Distribution Load Tests ===");
        Console.WriteLine($"Workers: {ConcurrentWorkers}, Selections/worker: {SelectionsPerWorker:N0}");
        Console.WriteLine();

        await RunTestAsync("RoundRobin — Even Distribution", RoundRobin_ConcurrentLoad_DistributesEvenly);
        await RunTestAsync("Random — Reasonable Distribution", Random_ConcurrentLoad_DistributesReasonably);
        await RunTestAsync("LeastLatency — Prefers Fastest", LeastLatency_ConcurrentLoad_PrefersFastestReplica);
        await RunTestAsync("LeastConnections — No Negative Counts", LeastConnections_ConcurrentIncrementDecrement_NoNegativeCounts);
        await RunTestAsync("WeightedRandom — Respects Proportions", WeightedRandom_ConcurrentLoad_RespectsWeightProportions);
        await RunTestAsync("HealthTracker — Concurrent Consistency", HealthTracker_ConcurrentUpdates_MaintainsConsistency);

        Console.WriteLine();
        Console.WriteLine("=== All replica distribution tests completed ===");
    }

    // ────────────────────────────────────────────────────────────
    //  RoundRobin — Perfect Distribution Under Concurrent Load
    // ────────────────────────────────────────────────────────────

    private static async Task RoundRobin_ConcurrentLoad_DistributesEvenly()
    {
        var selector = new RoundRobinShardReplicaSelector();
        var counts = new ConcurrentDictionary<string, int>();

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(_ => Task.Run(() =>
        {
            for (var i = 0; i < SelectionsPerWorker; i++)
            {
                var replica = selector.SelectReplica(Replicas);
                counts.AddOrUpdate(replica, 1, (_, c) => c + 1);
            }
        }));

        await Task.WhenAll(tasks);

        var totalSelections = ConcurrentWorkers * SelectionsPerWorker;
        var expectedPerReplica = totalSelections / Replicas.Count;

        // RoundRobin should give exactly even distribution
        foreach (var (replica, count) in counts)
        {
            Assert(count == expectedPerReplica,
                $"Replica {replica}: got {count}, expected {expectedPerReplica}");
        }

        Assert(counts.Count == Replicas.Count,
            $"Expected {Replicas.Count} replicas used, got {counts.Count}");

        Console.WriteLine($"  Distribution: {string.Join(", ", counts.Select(kv => $"{kv.Value:N0}"))} (expected {expectedPerReplica:N0} each)");
    }

    // ────────────────────────────────────────────────────────────
    //  Random — Reasonable Distribution Under Concurrent Load
    // ────────────────────────────────────────────────────────────

    private static async Task Random_ConcurrentLoad_DistributesReasonably()
    {
        var selector = new RandomShardReplicaSelector();
        var counts = new ConcurrentDictionary<string, int>();

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(_ => Task.Run(() =>
        {
            for (var i = 0; i < SelectionsPerWorker; i++)
            {
                var replica = selector.SelectReplica(Replicas);
                counts.AddOrUpdate(replica, 1, (_, c) => c + 1);
            }
        }));

        await Task.WhenAll(tasks);

        var totalSelections = ConcurrentWorkers * SelectionsPerWorker;
        var expectedPerReplica = totalSelections / Replicas.Count;
        var tolerance = expectedPerReplica * 0.1; // 10% tolerance for random

        foreach (var (replica, count) in counts)
        {
            var lo = (int)(expectedPerReplica - tolerance);
            var hi = (int)(expectedPerReplica + tolerance);
            Assert(count >= lo && count <= hi,
                $"Replica {replica}: got {count}, expected [{lo}..{hi}]");
        }

        Assert(counts.Count == Replicas.Count,
            $"Expected {Replicas.Count} replicas used, got {counts.Count}");

        var minCount = counts.Values.Min();
        var maxCount = counts.Values.Max();
        Console.WriteLine($"  Distribution: min={minCount:N0}, max={maxCount:N0}, spread={maxCount - minCount:N0} (±10% of {expectedPerReplica:N0})");
    }

    // ────────────────────────────────────────────────────────────
    //  LeastLatency — Converges on Fastest Under Load
    // ────────────────────────────────────────────────────────────

    private static async Task LeastLatency_ConcurrentLoad_PrefersFastestReplica()
    {
        var selector = new LeastLatencyShardReplicaSelector();
        var counts = new ConcurrentDictionary<string, int>();

        // Set up latencies — replica1 is fastest
        selector.ReportLatency(Replicas[0], TimeSpan.FromMilliseconds(5));
        selector.ReportLatency(Replicas[1], TimeSpan.FromMilliseconds(50));
        selector.ReportLatency(Replicas[2], TimeSpan.FromMilliseconds(100));
        selector.ReportLatency(Replicas[3], TimeSpan.FromMilliseconds(200));
        selector.ReportLatency(Replicas[4], TimeSpan.FromMilliseconds(500));

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(_ => Task.Run(() =>
        {
            for (var i = 0; i < SelectionsPerWorker; i++)
            {
                var replica = selector.SelectReplica(Replicas);
                counts.AddOrUpdate(replica, 1, (_, c) => c + 1);
            }
        }));

        await Task.WhenAll(tasks);

        // The fastest replica should get all the traffic
        var fastestCount = counts.GetValueOrDefault(Replicas[0], 0);
        var totalSelections = ConcurrentWorkers * SelectionsPerWorker;

        Assert(fastestCount == totalSelections,
            $"Fastest replica got {fastestCount}, expected all {totalSelections}");

        Console.WriteLine($"  Fastest replica received {fastestCount:N0}/{totalSelections:N0} selections (100%)");
    }

    // ────────────────────────────────────────────────────────────
    //  LeastConnections — Selects Least Loaded Under Concurrency
    // ────────────────────────────────────────────────────────────

    private static async Task LeastConnections_ConcurrentIncrementDecrement_NoNegativeCounts()
    {
        var selector = new LeastConnectionsShardReplicaSelector();
        var incrementTasks = new List<Task>();
        var decrementTasks = new List<Task>();

        for (var w = 0; w < ConcurrentWorkers; w++)
        {
            incrementTasks.Add(Task.Run(() =>
            {
                for (var i = 0; i < 1000; i++)
                {
                    var replica = Replicas[i % Replicas.Count];
                    selector.IncrementConnections(replica);
                }
            }));

            decrementTasks.Add(Task.Run(() =>
            {
                for (var i = 0; i < 1000; i++)
                {
                    var replica = Replicas[i % Replicas.Count];
                    selector.DecrementConnections(replica);
                }
            }));
        }

        await Task.WhenAll(incrementTasks.Concat(decrementTasks));

        // Connection counts should never go negative
        foreach (var replica in Replicas)
        {
            var count = selector.GetConnectionCount(replica);
            Assert(count >= 0,
                $"Replica {replica} has negative connection count: {count}");
        }

        Console.WriteLine($"  All {Replicas.Count} replicas have non-negative connection counts after {ConcurrentWorkers * 2} concurrent workers");
    }

    // ────────────────────────────────────────────────────────────
    //  WeightedRandom — Respects Weights Under High Concurrency
    // ────────────────────────────────────────────────────────────

    private static async Task WeightedRandom_ConcurrentLoad_RespectsWeightProportions()
    {
        // 5:3:1:1:1 weights
        int[] weights = [5, 3, 1, 1, 1];
        var selector = new WeightedRandomShardReplicaSelector(weights);
        var counts = new ConcurrentDictionary<string, int>();

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(_ => Task.Run(() =>
        {
            for (var i = 0; i < SelectionsPerWorker; i++)
            {
                var replica = selector.SelectReplica(Replicas);
                counts.AddOrUpdate(replica, 1, (_, c) => c + 1);
            }
        }));

        await Task.WhenAll(tasks);

        var heaviestCount = counts.GetValueOrDefault(Replicas[0], 0);
        var lightestCount = counts.GetValueOrDefault(Replicas[4], 0);

        // Weight 5 should get roughly 5x the traffic of weight 1
        // Use conservative 2x minimum to account for randomness
        Assert(heaviestCount > lightestCount * 2,
            $"Heaviest ({heaviestCount}) should be >2x lightest ({lightestCount})");

        Console.WriteLine($"  Weights [5:3:1:1:1] → [{string.Join(":", counts.OrderBy(kv => Array.IndexOf(Replicas.ToArray(), kv.Key)).Select(kv => kv.Value))}]");
        Console.WriteLine($"  Ratio heaviest/lightest: {(double)heaviestCount / lightestCount:F1}x (expected ~5x)");
    }

    // ────────────────────────────────────────────────────────────
    //  HealthTracker — Concurrent Health Updates
    // ────────────────────────────────────────────────────────────

    private static async Task HealthTracker_ConcurrentUpdates_MaintainsConsistency()
    {
        var tracker = new ReplicaHealthTracker();
        const string shardId = "shard-0";

        var tasks = Enumerable.Range(0, ConcurrentWorkers).Select(w => Task.Run(() =>
        {
            for (var i = 0; i < 1000; i++)
            {
                var replica = Replicas[i % Replicas.Count];

                if (w % 2 == 0)
                {
                    tracker.MarkUnhealthy(shardId, replica);
                }
                else
                {
                    tracker.MarkHealthy(shardId, replica);
                }

                if (i % 3 == 0)
                {
                    tracker.ReportReplicationLag(shardId, replica, TimeSpan.FromMilliseconds(i % 100));
                }

                // Read operations interleaved
                _ = tracker.GetAvailableReplicas(shardId, Replicas);
                _ = tracker.GetHealthState(shardId, replica);
            }
        }));

        await Task.WhenAll(tasks);

        // All replicas should have a tracked state after concurrent updates
        var states = tracker.GetAllHealthStates(shardId);

        Assert(states.Count == Replicas.Count,
            $"Expected {Replicas.Count} tracked replicas, got {states.Count}");

        Console.WriteLine($"  {states.Count} replicas tracked after {ConcurrentWorkers} concurrent workers × 1000 operations each");
    }

    // ────────────────────────────────────────────────────────────
    //  Helpers
    // ────────────────────────────────────────────────────────────

    private static async Task RunTestAsync(string name, Func<Task> test)
    {
        Console.Write($"  [{name}] ...");
        var sw = Stopwatch.StartNew();
        try
        {
            await test();
            sw.Stop();
            Console.WriteLine($" PASS ({sw.ElapsedMilliseconds}ms)");
        }
        catch (Exception ex)
        {
            sw.Stop();
            Console.WriteLine($" FAIL ({sw.ElapsedMilliseconds}ms)");
            Console.WriteLine($"    Error: {ex.Message}");
        }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException($"Assertion failed: {message}");
        }
    }
}
