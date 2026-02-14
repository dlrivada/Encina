using Encina.Sharding.ReplicaSelection;

namespace Encina.UnitTests.Core.Sharding.ReplicaSelection;

public sealed class RandomShardReplicaSelectorTests
{
    // ────────────────────────────────────────────────────────────
    //  Helpers
    // ────────────────────────────────────────────────────────────

    private static readonly IReadOnlyList<string> ThreeReplicas =
        ["Server=replica1;", "Server=replica2;", "Server=replica3;"];

    private static readonly IReadOnlyList<string> SingleReplica =
        ["Server=replica1;"];

    // ────────────────────────────────────────────────────────────
    //  SelectReplica — Basic
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void SelectReplica_ThreeReplicas_ReturnsValidReplica()
    {
        // Arrange
        var selector = new RandomShardReplicaSelector();

        // Act
        var result = selector.SelectReplica(ThreeReplicas);

        // Assert
        ThreeReplicas.ShouldContain(result);
    }

    [Fact]
    public void SelectReplica_SingleReplica_AlwaysReturnsSame()
    {
        // Arrange
        var selector = new RandomShardReplicaSelector();

        // Act & Assert
        for (var i = 0; i < 20; i++)
        {
            selector.SelectReplica(SingleReplica).ShouldBe("Server=replica1;");
        }
    }

    // ────────────────────────────────────────────────────────────
    //  SelectReplica — Distribution
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void SelectReplica_OverManyCalls_HitsAllReplicas()
    {
        // Arrange
        var selector = new RandomShardReplicaSelector();
        var seen = new System.Collections.Generic.HashSet<string>();

        // Act — random should eventually hit all 3
        for (var i = 0; i < 1_000; i++)
        {
            seen.Add(selector.SelectReplica(ThreeReplicas));
            if (seen.Count == 3)
            {
                break;
            }
        }

        // Assert
        seen.Count.ShouldBe(3);
    }

    [Fact]
    public void SelectReplica_OverManyCalls_DistributesReasonably()
    {
        // Arrange
        var selector = new RandomShardReplicaSelector();
        var counts = new Dictionary<string, int>();

        // Act
        const int iterations = 9_000;
        for (var i = 0; i < iterations; i++)
        {
            var replica = selector.SelectReplica(ThreeReplicas);
            counts[replica] = counts.GetValueOrDefault(replica) + 1;
        }

        // Assert — each should get roughly 33% (within 20% tolerance)
        foreach (var count in counts.Values)
        {
            count.ShouldBeGreaterThan(iterations / 3 - iterations / 5); // > ~13%
            count.ShouldBeLessThan(iterations / 3 + iterations / 5);    // < ~53%
        }
    }

    // ────────────────────────────────────────────────────────────
    //  SelectReplica — Validation
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void SelectReplica_NullList_ThrowsArgumentNullException()
    {
        var selector = new RandomShardReplicaSelector();
        Should.Throw<ArgumentNullException>(() => selector.SelectReplica(null!));
    }

    [Fact]
    public void SelectReplica_EmptyList_ThrowsArgumentException()
    {
        var selector = new RandomShardReplicaSelector();
        Should.Throw<ArgumentException>(() => selector.SelectReplica(Array.Empty<string>()));
    }

    // ────────────────────────────────────────────────────────────
    //  Thread Safety
    // ────────────────────────────────────────────────────────────

    [Fact]
    public async Task SelectReplica_ConcurrentCalls_AllReturnValidReplicas()
    {
        var selector = new RandomShardReplicaSelector();
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
