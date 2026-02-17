using Encina.Sharding.ReplicaSelection;

namespace Encina.UnitTests.Core.Sharding.ReplicaSelection;

public sealed class RoundRobinShardReplicaSelectorTests
{
    // ────────────────────────────────────────────────────────────
    //  Helpers
    // ────────────────────────────────────────────────────────────

    private static readonly IReadOnlyList<string> ThreeReplicas =
        ["Server=replica1;", "Server=replica2;", "Server=replica3;"];

    private static readonly IReadOnlyList<string> SingleReplica =
        ["Server=replica1;"];

    // ────────────────────────────────────────────────────────────
    //  SelectReplica — Cycling
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void SelectReplica_ThreeReplicas_CyclesInOrder()
    {
        // Arrange
        var selector = new RoundRobinShardReplicaSelector();

        // Act
        var first = selector.SelectReplica(ThreeReplicas);
        var second = selector.SelectReplica(ThreeReplicas);
        var third = selector.SelectReplica(ThreeReplicas);
        var fourth = selector.SelectReplica(ThreeReplicas);

        // Assert — wraps around after third
        first.ShouldBe("Server=replica1;");
        second.ShouldBe("Server=replica2;");
        third.ShouldBe("Server=replica3;");
        fourth.ShouldBe("Server=replica1;");
    }

    [Fact]
    public void SelectReplica_SingleReplica_AlwaysReturnsSame()
    {
        // Arrange
        var selector = new RoundRobinShardReplicaSelector();

        // Act & Assert
        for (var i = 0; i < 10; i++)
        {
            selector.SelectReplica(SingleReplica).ShouldBe("Server=replica1;");
        }
    }

    [Fact]
    public void SelectReplica_AfterManyIterations_StillCyclesCorrectly()
    {
        // Arrange
        var selector = new RoundRobinShardReplicaSelector();

        // Act — advance the counter many times
        for (var i = 0; i < 1_000; i++)
        {
            selector.SelectReplica(ThreeReplicas);
        }

        // Assert — still cycles correctly after the 1000th call
        var r1 = selector.SelectReplica(ThreeReplicas);
        var r2 = selector.SelectReplica(ThreeReplicas);
        var r3 = selector.SelectReplica(ThreeReplicas);

        // They should all be different from each other (cycling)
        r1.ShouldNotBe(r2);
        r2.ShouldNotBe(r3);
    }

    // ────────────────────────────────────────────────────────────
    //  SelectReplica — Distribution
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void SelectReplica_OverManyCalls_DistributesEvenly()
    {
        // Arrange
        var selector = new RoundRobinShardReplicaSelector();
        var counts = new Dictionary<string, int>();

        // Act
        for (var i = 0; i < 300; i++)
        {
            var replica = selector.SelectReplica(ThreeReplicas);
            counts[replica] = counts.GetValueOrDefault(replica) + 1;
        }

        // Assert — exactly even for round-robin
        counts["Server=replica1;"].ShouldBe(100);
        counts["Server=replica2;"].ShouldBe(100);
        counts["Server=replica3;"].ShouldBe(100);
    }

    // ────────────────────────────────────────────────────────────
    //  SelectReplica — Varying List Size
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void SelectReplica_VariableListSize_HandlesCorrectly()
    {
        // Arrange — simulates dynamic replica lists (e.g., health filtering)
        var selector = new RoundRobinShardReplicaSelector();
        IReadOnlyList<string> twoReplicas = ["Server=replica1;", "Server=replica2;"];

        // Act — use three replicas first
        selector.SelectReplica(ThreeReplicas);
        selector.SelectReplica(ThreeReplicas);

        // Then switch to two replicas — should not throw
        var result = selector.SelectReplica(twoReplicas);

        // Assert
        result.ShouldNotBeNull();
        twoReplicas.ShouldContain(result);
    }

    // ────────────────────────────────────────────────────────────
    //  SelectReplica — Validation
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void SelectReplica_NullList_ThrowsArgumentNullException()
    {
        // Arrange
        var selector = new RoundRobinShardReplicaSelector();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => selector.SelectReplica(null!));
    }

    [Fact]
    public void SelectReplica_EmptyList_ThrowsArgumentException()
    {
        // Arrange
        var selector = new RoundRobinShardReplicaSelector();
        IReadOnlyList<string> empty = [];

        // Act & Assert
        Should.Throw<ArgumentException>(() => selector.SelectReplica(empty));
    }

    // ────────────────────────────────────────────────────────────
    //  Thread Safety
    // ────────────────────────────────────────────────────────────

    [Fact]
    public async Task SelectReplica_ConcurrentCalls_AllReturnValidReplicas()
    {
        // Arrange
        var selector = new RoundRobinShardReplicaSelector();
        var tasks = new List<Task<string>>();

        // Act — 100 concurrent selections
        for (var i = 0; i < 100; i++)
        {
            tasks.Add(Task.Run(() => selector.SelectReplica(ThreeReplicas)));
        }

        var results = await Task.WhenAll(tasks);

        // Assert — all results are valid replicas
        foreach (var result in results)
        {
            ThreeReplicas.ShouldContain(result);
        }
    }
}
