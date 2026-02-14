using Encina.Sharding.ReplicaSelection;

namespace Encina.UnitTests.Core.Sharding.ReplicaSelection;

public sealed class LeastConnectionsShardReplicaSelectorTests
{
    // ────────────────────────────────────────────────────────────
    //  Helpers
    // ────────────────────────────────────────────────────────────

    private static readonly IReadOnlyList<string> ThreeReplicas =
        ["Server=replica1;", "Server=replica2;", "Server=replica3;"];

    private static readonly IReadOnlyList<string> SingleReplica =
        ["Server=replica1;"];

    // ────────────────────────────────────────────────────────────
    //  SelectReplica — No Connection Data
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void SelectReplica_NoConnectionData_SelectsFirstReplica()
    {
        // Arrange
        var selector = new LeastConnectionsShardReplicaSelector();

        // Act — all have 0 connections, so first one wins
        var selected = selector.SelectReplica(ThreeReplicas);

        // Assert
        selected.ShouldBe("Server=replica1;");
    }

    [Fact]
    public void SelectReplica_SingleReplica_AlwaysReturnsSame()
    {
        var selector = new LeastConnectionsShardReplicaSelector();

        for (var i = 0; i < 10; i++)
        {
            selector.SelectReplica(SingleReplica).ShouldBe("Server=replica1;");
        }
    }

    // ────────────────────────────────────────────────────────────
    //  SelectReplica — With Connection Counts
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void SelectReplica_WithConnectionCounts_SelectsLowest()
    {
        // Arrange
        var selector = new LeastConnectionsShardReplicaSelector();
        selector.IncrementConnections("Server=replica1;"); // 1
        selector.IncrementConnections("Server=replica1;"); // 2
        selector.IncrementConnections("Server=replica3;"); // 1

        // Act — replica2 has 0 connections
        var selected = selector.SelectReplica(ThreeReplicas);

        // Assert
        selected.ShouldBe("Server=replica2;");
    }

    [Fact]
    public void SelectReplica_AfterDecrement_SelectsNewlyFreed()
    {
        // Arrange
        var selector = new LeastConnectionsShardReplicaSelector();

        // Simulate: replica1 busy, replica2 free, replica3 busy
        selector.IncrementConnections("Server=replica1;"); // 1
        selector.IncrementConnections("Server=replica2;"); // 1
        selector.IncrementConnections("Server=replica3;"); // 1

        // Release replica3
        selector.DecrementConnections("Server=replica3;"); // 0

        // Act
        var selected = selector.SelectReplica(ThreeReplicas);

        // Assert — replica3 is now at 0
        selected.ShouldBe("Server=replica3;");
    }

    // ────────────────────────────────────────────────────────────
    //  IncrementConnections / DecrementConnections
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void IncrementConnections_IncreasesCount()
    {
        var selector = new LeastConnectionsShardReplicaSelector();

        selector.IncrementConnections("Server=replica1;");
        selector.IncrementConnections("Server=replica1;");

        selector.GetConnectionCount("Server=replica1;").ShouldBe(2);
    }

    [Fact]
    public void DecrementConnections_DecreasesCount()
    {
        var selector = new LeastConnectionsShardReplicaSelector();

        selector.IncrementConnections("Server=replica1;");
        selector.IncrementConnections("Server=replica1;");
        selector.DecrementConnections("Server=replica1;");

        selector.GetConnectionCount("Server=replica1;").ShouldBe(1);
    }

    [Fact]
    public void DecrementConnections_NeverBelowZero()
    {
        var selector = new LeastConnectionsShardReplicaSelector();

        selector.DecrementConnections("Server=replica1;");
        selector.DecrementConnections("Server=replica1;");

        selector.GetConnectionCount("Server=replica1;").ShouldBeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void GetConnectionCount_UnknownReplica_ReturnsZero()
    {
        var selector = new LeastConnectionsShardReplicaSelector();

        selector.GetConnectionCount("Server=unknown;").ShouldBe(0);
    }

    [Fact]
    public void IncrementConnections_NullConnectionString_ThrowsArgumentNullException()
    {
        var selector = new LeastConnectionsShardReplicaSelector();
        Should.Throw<ArgumentNullException>(() => selector.IncrementConnections(null!));
    }

    [Fact]
    public void DecrementConnections_NullConnectionString_ThrowsArgumentNullException()
    {
        var selector = new LeastConnectionsShardReplicaSelector();
        Should.Throw<ArgumentNullException>(() => selector.DecrementConnections(null!));
    }

    // ────────────────────────────────────────────────────────────
    //  Reset
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Reset_ClearsAllConnectionCounts()
    {
        var selector = new LeastConnectionsShardReplicaSelector();
        selector.IncrementConnections("Server=replica1;");
        selector.IncrementConnections("Server=replica2;");

        selector.Reset();

        selector.GetConnectionCount("Server=replica1;").ShouldBe(0);
        selector.GetConnectionCount("Server=replica2;").ShouldBe(0);
    }

    // ────────────────────────────────────────────────────────────
    //  Validation
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void SelectReplica_NullList_ThrowsArgumentNullException()
    {
        var selector = new LeastConnectionsShardReplicaSelector();
        Should.Throw<ArgumentNullException>(() => selector.SelectReplica(null!));
    }

    [Fact]
    public void SelectReplica_EmptyList_ThrowsArgumentException()
    {
        var selector = new LeastConnectionsShardReplicaSelector();
        Should.Throw<ArgumentException>(() => selector.SelectReplica(Array.Empty<string>()));
    }

    // ────────────────────────────────────────────────────────────
    //  Thread Safety
    // ────────────────────────────────────────────────────────────

    [Fact]
    public async Task ConcurrentIncrementAndSelect_NoExceptions()
    {
        var selector = new LeastConnectionsShardReplicaSelector();
        var tasks = new List<Task>();

        for (var i = 0; i < 100; i++)
        {
            var replica = ThreeReplicas[i % 3];
            tasks.Add(Task.Run(() =>
            {
                selector.IncrementConnections(replica);
                selector.SelectReplica(ThreeReplicas);
                selector.DecrementConnections(replica);
            }));
        }

        await Task.WhenAll(tasks);
    }
}
