using Encina.Sharding.ReplicaSelection;

namespace Encina.UnitTests.Core.Sharding.ReplicaSelection;

public sealed class WeightedRandomShardReplicaSelectorTests
{
    // ────────────────────────────────────────────────────────────
    //  Helpers
    // ────────────────────────────────────────────────────────────

    private static readonly IReadOnlyList<string> ThreeReplicas =
        ["Server=replica1;", "Server=replica2;", "Server=replica3;"];

    private static readonly IReadOnlyList<string> SingleReplica =
        ["Server=replica1;"];

    // ────────────────────────────────────────────────────────────
    //  Constructor
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_NullWeights_DoesNotThrow()
    {
        // Act
        var selector = new WeightedRandomShardReplicaSelector(null);

        // Assert
        selector.ShouldNotBeNull();
    }

    [Fact]
    public void Constructor_EmptyWeights_DoesNotThrow()
    {
        var selector = new WeightedRandomShardReplicaSelector([]);
        selector.ShouldNotBeNull();
    }

    [Fact]
    public void Constructor_ValidWeights_DoesNotThrow()
    {
        var selector = new WeightedRandomShardReplicaSelector([3, 2, 1]);
        selector.ShouldNotBeNull();
    }

    [Fact]
    public void Constructor_ZeroWeight_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => new WeightedRandomShardReplicaSelector([3, 0, 1]));
    }

    [Fact]
    public void Constructor_NegativeWeight_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => new WeightedRandomShardReplicaSelector([3, -1, 1]));
    }

    // ────────────────────────────────────────────────────────────
    //  SelectReplica — Basic
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void SelectReplica_SingleReplica_AlwaysReturnsSame()
    {
        var selector = new WeightedRandomShardReplicaSelector([5]);

        for (var i = 0; i < 20; i++)
        {
            selector.SelectReplica(SingleReplica).ShouldBe("Server=replica1;");
        }
    }

    [Fact]
    public void SelectReplica_NoWeightsConfigured_TreatsAllEqually()
    {
        // Arrange — null weights = all equal (weight 1)
        var selector = new WeightedRandomShardReplicaSelector(null);
        var counts = new Dictionary<string, int>();

        // Act
        const int iterations = 9_000;
        for (var i = 0; i < iterations; i++)
        {
            var replica = selector.SelectReplica(ThreeReplicas);
            counts[replica] = counts.GetValueOrDefault(replica) + 1;
        }

        // Assert — roughly even distribution within 20% tolerance
        foreach (var count in counts.Values)
        {
            count.ShouldBeGreaterThan(iterations / 3 - iterations / 5);
            count.ShouldBeLessThan(iterations / 3 + iterations / 5);
        }
    }

    // ────────────────────────────────────────────────────────────
    //  SelectReplica — Weighted Distribution
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void SelectReplica_WeightedDistribution_RespectsWeights()
    {
        // Arrange — replica1 gets 3x, replica2 gets 1x
        IReadOnlyList<string> twoReplicas = ["Server=heavy;", "Server=light;"];
        var selector = new WeightedRandomShardReplicaSelector([3, 1]);
        var counts = new Dictionary<string, int>();

        // Act
        const int iterations = 10_000;
        for (var i = 0; i < iterations; i++)
        {
            var replica = selector.SelectReplica(twoReplicas);
            counts[replica] = counts.GetValueOrDefault(replica) + 1;
        }

        // Assert — heavy should get ~75%, light ~25%
        var heavyRatio = counts["Server=heavy;"] / (double)iterations;
        var lightRatio = counts["Server=light;"] / (double)iterations;

        heavyRatio.ShouldBeGreaterThan(0.60); // At least 60% (expected ~75%)
        lightRatio.ShouldBeLessThan(0.40);    // At most 40% (expected ~25%)
    }

    [Fact]
    public void SelectReplica_FewerWeightsThanReplicas_DefaultsToWeightOne()
    {
        // Arrange — only 1 weight for 3 replicas; others default to weight 1
        var selector = new WeightedRandomShardReplicaSelector([5]);
        var counts = new Dictionary<string, int>();

        // Act
        const int iterations = 10_000;
        for (var i = 0; i < iterations; i++)
        {
            var replica = selector.SelectReplica(ThreeReplicas);
            counts[replica] = counts.GetValueOrDefault(replica) + 1;
        }

        // Assert — replica1 should get ~71% (5/7), replica2 and replica3 ~14% each (1/7)
        var r1Ratio = counts.GetValueOrDefault("Server=replica1;") / (double)iterations;
        r1Ratio.ShouldBeGreaterThan(0.55); // At least 55% (expected ~71%)
    }

    // ────────────────────────────────────────────────────────────
    //  Validation
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void SelectReplica_NullList_ThrowsArgumentNullException()
    {
        var selector = new WeightedRandomShardReplicaSelector();
        Should.Throw<ArgumentNullException>(() => selector.SelectReplica(null!));
    }

    [Fact]
    public void SelectReplica_EmptyList_ThrowsArgumentException()
    {
        var selector = new WeightedRandomShardReplicaSelector();
        Should.Throw<ArgumentException>(() => selector.SelectReplica(Array.Empty<string>()));
    }

    // ────────────────────────────────────────────────────────────
    //  Thread Safety
    // ────────────────────────────────────────────────────────────

    [Fact]
    public async Task SelectReplica_ConcurrentCalls_AllReturnValidReplicas()
    {
        var selector = new WeightedRandomShardReplicaSelector([3, 2, 1]);
        var tasks = Enumerable.Range(0, 100)
            .Select(_ => Task.Run(() => selector.SelectReplica(ThreeReplicas)))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        foreach (var result in results)
        {
            ThreeReplicas.ShouldContain(result);
        }
    }
}
