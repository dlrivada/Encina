using Encina.Sharding.ReplicaSelection;
using FsCheck;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Database.Sharding;

/// <summary>
/// Property-based tests for replica selection strategy invariants.
/// </summary>
[Trait("Category", "Property")]
public sealed class ReplicaSelectionPropertyTests
{
    private static List<string> CreateReplicas(int count)
        => Enumerable.Range(1, count).Select(i => $"Server=replica{i};").ToList();

    // ────────────────────────────────────────────────────────────
    //  RoundRobin — Even Distribution
    // ────────────────────────────────────────────────────────────

    [Property(MaxTest = 50)]
    public bool Property_RoundRobin_VisitsAllReplicas_Over_N_Selections(PositiveInt replicaCount)
    {
        var count = Math.Min(replicaCount.Get, 20); // Cap at 20 for performance
        if (count < 1)
        {
            return true;
        }

        var replicas = CreateReplicas(count);
        var selector = new RoundRobinShardReplicaSelector();
        var selected = new System.Collections.Generic.HashSet<string>();

        // After exactly N selections, all N replicas should have been visited
        for (var i = 0; i < count; i++)
        {
            selected.Add(selector.SelectReplica(replicas));
        }

        return selected.Count == count;
    }

    [Property(MaxTest = 50)]
    public bool Property_RoundRobin_ExactlyEvenDistribution(PositiveInt cycles)
    {
        var cycleCount = Math.Min(cycles.Get, 100);
        var replicas = CreateReplicas(3);
        var selector = new RoundRobinShardReplicaSelector();
        var counts = new Dictionary<string, int>();

        var totalSelections = 3 * cycleCount;

        for (var i = 0; i < totalSelections; i++)
        {
            var r = selector.SelectReplica(replicas);

            if (!counts.TryAdd(r, 1))
            {
                counts[r]++;
            }
        }

        // Each replica should be selected exactly cycleCount times
        return counts.Values.All(c => c == cycleCount);
    }

    // ────────────────────────────────────────────────────────────
    //  Random — Covers All Replicas
    // ────────────────────────────────────────────────────────────

    [Property(MaxTest = 30)]
    public bool Property_Random_AlwaysReturnsValidReplica(PositiveInt iterations)
    {
        var iterCount = Math.Min(iterations.Get, 200);
        var replicas = CreateReplicas(3);
        var selector = new RandomShardReplicaSelector();

        for (var i = 0; i < iterCount; i++)
        {
            var result = selector.SelectReplica(replicas);
            if (!replicas.Contains(result))
            {
                return false;
            }
        }

        return true;
    }

    // ────────────────────────────────────────────────────────────
    //  LeastLatency — Prefers Fastest Replica
    // ────────────────────────────────────────────────────────────

    [Property(MaxTest = 30)]
    public bool Property_LeastLatency_PrefersFastestReplica(PositiveInt selections)
    {
        var selectCount = Math.Min(selections.Get, 100);
        var replicas = CreateReplicas(3);
        var selector = new LeastLatencyShardReplicaSelector();

        // Report distinct latencies
        selector.ReportLatency(replicas[0], TimeSpan.FromMilliseconds(100));
        selector.ReportLatency(replicas[1], TimeSpan.FromMilliseconds(10)); // Fastest
        selector.ReportLatency(replicas[2], TimeSpan.FromMilliseconds(50));

        var counts = new Dictionary<string, int>();

        for (var i = 0; i < selectCount; i++)
        {
            var r = selector.SelectReplica(replicas);

            if (!counts.TryAdd(r, 1))
            {
                counts[r]++;
            }
        }

        // The fastest replica should always be selected when latencies are distinct
        return counts.GetValueOrDefault(replicas[1]) == selectCount;
    }

    // ────────────────────────────────────────────────────────────
    //  LeastConnections — Prefers Least Loaded
    // ────────────────────────────────────────────────────────────

    [Property(MaxTest = 30)]
    public bool Property_LeastConnections_PreferencesLeastLoaded(PositiveInt selections)
    {
        var selectCount = Math.Min(selections.Get, 50);
        var replicas = CreateReplicas(3);
        var selector = new LeastConnectionsShardReplicaSelector();

        // Load up two replicas
        for (var i = 0; i < 10; i++)
        {
            selector.IncrementConnections(replicas[0]);
            selector.IncrementConnections(replicas[2]);
        }

        // replicas[1] has 0 connections, should be preferred
        for (var i = 0; i < selectCount; i++)
        {
            var r = selector.SelectReplica(replicas);
            if (r != replicas[1])
            {
                return false;
            }
        }

        return true;
    }

    // ────────────────────────────────────────────────────────────
    //  WeightedRandom — Higher Weight Gets More Selections
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Property_WeightedRandom_HigherWeightGetsMoreSelections()
    {
        int[] weights = [10, 1]; // 10:1 ratio
        var replicas = CreateReplicas(2);
        var selector = new WeightedRandomShardReplicaSelector(weights);
        var counts = new Dictionary<string, int>();

        const int totalSelections = 10_000;

        for (var i = 0; i < totalSelections; i++)
        {
            var r = selector.SelectReplica(replicas);

            if (!counts.TryAdd(r, 1))
            {
                counts[r]++;
            }
        }

        // The heavily-weighted replica should get significantly more selections
        var heavyCount = counts.GetValueOrDefault(replicas[0], 0);
        var lightCount = counts.GetValueOrDefault(replicas[1], 0);

        // With 10:1 ratio, heavy should get at least 5x more (very conservative)
        heavyCount.ShouldBeGreaterThan(lightCount * 5);
    }

    // ────────────────────────────────────────────────────────────
    //  All Selectors — Determinism (same list, same type)
    // ────────────────────────────────────────────────────────────

    [Property(MaxTest = 50)]
    public bool Property_AllSelectors_ReturnValueFromProvidedList(PositiveInt replicaCount)
    {
        var count = Math.Clamp(replicaCount.Get, 1, 10);
        var replicas = CreateReplicas(count);

        var selectors = new IShardReplicaSelector[]
        {
            new RoundRobinShardReplicaSelector(),
            new RandomShardReplicaSelector(),
            new LeastLatencyShardReplicaSelector(),
            new LeastConnectionsShardReplicaSelector(),
            new WeightedRandomShardReplicaSelector(),
        };

        foreach (var selector in selectors)
        {
            var result = selector.SelectReplica(replicas);
            if (!replicas.Contains(result))
            {
                return false;
            }
        }

        return true;
    }
}
