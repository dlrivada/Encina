using Encina.Sharding.ReplicaSelection;

namespace Encina.UnitTests.Core.Sharding.ReplicaSelection;

public sealed class LeastLatencyShardReplicaSelectorTests
{
    // ────────────────────────────────────────────────────────────
    //  Helpers
    // ────────────────────────────────────────────────────────────

    private static readonly IReadOnlyList<string> ThreeReplicas =
        ["Server=replica1;", "Server=replica2;", "Server=replica3;"];

    private static readonly IReadOnlyList<string> SingleReplica =
        ["Server=replica1;"];

    // ────────────────────────────────────────────────────────────
    //  SelectReplica — No Latency Data (Fallback to Round-Robin)
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void SelectReplica_NoLatencyData_FallsBackToRoundRobin()
    {
        // Arrange
        var selector = new LeastLatencyShardReplicaSelector();

        // Act — should cycle round-robin since there's no latency data
        var first = selector.SelectReplica(ThreeReplicas);
        var second = selector.SelectReplica(ThreeReplicas);
        var third = selector.SelectReplica(ThreeReplicas);
        var fourth = selector.SelectReplica(ThreeReplicas);

        // Assert — round-robin cycling
        first.ShouldBe("Server=replica1;");
        second.ShouldBe("Server=replica2;");
        third.ShouldBe("Server=replica3;");
        fourth.ShouldBe("Server=replica1;");
    }

    // ────────────────────────────────────────────────────────────
    //  SelectReplica — With Latency Data
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void SelectReplica_WithLatencyData_SelectsLowestLatencyReplica()
    {
        // Arrange
        var selector = new LeastLatencyShardReplicaSelector();
        selector.ReportLatency("Server=replica1;", TimeSpan.FromMilliseconds(10));
        selector.ReportLatency("Server=replica2;", TimeSpan.FromMilliseconds(5));
        selector.ReportLatency("Server=replica3;", TimeSpan.FromMilliseconds(15));

        // Act
        var selected = selector.SelectReplica(ThreeReplicas);

        // Assert — replica2 has the lowest latency
        selected.ShouldBe("Server=replica2;");
    }

    [Fact]
    public void SelectReplica_WithPartialLatencyData_SelectsKnownLowest()
    {
        // Arrange — only replica3 has latency data
        var selector = new LeastLatencyShardReplicaSelector();
        selector.ReportLatency("Server=replica3;", TimeSpan.FromMilliseconds(8));

        // Act
        var selected = selector.SelectReplica(ThreeReplicas);

        // Assert — replica3 is the only one with data
        selected.ShouldBe("Server=replica3;");
    }

    [Fact]
    public void SelectReplica_SingleReplica_AlwaysReturnsSame()
    {
        // Arrange
        var selector = new LeastLatencyShardReplicaSelector();

        // Act & Assert
        for (var i = 0; i < 10; i++)
        {
            selector.SelectReplica(SingleReplica).ShouldBe("Server=replica1;");
        }
    }

    // ────────────────────────────────────────────────────────────
    //  ReportLatency — Exponential Moving Average
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void ReportLatency_MultipleReports_UsesExponentialMovingAverage()
    {
        // Arrange
        var selector = new LeastLatencyShardReplicaSelector();
        IReadOnlyList<string> twoReplicas = ["Server=fast;", "Server=slow;"];

        // Report initial latencies
        selector.ReportLatency("Server=fast;", TimeSpan.FromMilliseconds(100));
        selector.ReportLatency("Server=slow;", TimeSpan.FromMilliseconds(200));

        // fast replica gets worse (spike), but EMA smooths it
        selector.ReportLatency("Server=fast;", TimeSpan.FromMilliseconds(300));
        // EMA: 0.3 * 300 + 0.7 * 100 = 90 + 70 = 160

        // Act — fast should still be preferred because EMA(160) < slow(200)
        var selected = selector.SelectReplica(twoReplicas);

        // Assert
        selected.ShouldBe("Server=fast;");
    }

    [Fact]
    public void ReportLatency_NullConnectionString_ThrowsArgumentNullException()
    {
        var selector = new LeastLatencyShardReplicaSelector();
        Should.Throw<ArgumentNullException>(() =>
            selector.ReportLatency(null!, TimeSpan.FromMilliseconds(5)));
    }

    // ────────────────────────────────────────────────────────────
    //  Reset
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Reset_ClearsAllLatencyData_FallsBackToRoundRobin()
    {
        // Arrange
        var selector = new LeastLatencyShardReplicaSelector();
        selector.ReportLatency("Server=replica2;", TimeSpan.FromMilliseconds(1));

        // Act
        selector.Reset();

        // Assert — should use round-robin fallback
        var first = selector.SelectReplica(ThreeReplicas);
        var second = selector.SelectReplica(ThreeReplicas);
        first.ShouldBe("Server=replica1;");
        second.ShouldBe("Server=replica2;");
    }

    // ────────────────────────────────────────────────────────────
    //  Validation
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void SelectReplica_NullList_ThrowsArgumentNullException()
    {
        var selector = new LeastLatencyShardReplicaSelector();
        Should.Throw<ArgumentNullException>(() => selector.SelectReplica(null!));
    }

    [Fact]
    public void SelectReplica_EmptyList_ThrowsArgumentException()
    {
        var selector = new LeastLatencyShardReplicaSelector();
        Should.Throw<ArgumentException>(() => selector.SelectReplica(Array.Empty<string>()));
    }

    // ────────────────────────────────────────────────────────────
    //  Thread Safety
    // ────────────────────────────────────────────────────────────

    [Fact]
    public async Task SelectReplica_ConcurrentCallsWithLatencyReports_NoExceptions()
    {
        // Arrange
        var selector = new LeastLatencyShardReplicaSelector();

        // Act — concurrent reads and writes
        var tasks = new List<Task>();
        for (var i = 0; i < 50; i++)
        {
            var replica = ThreeReplicas[i % 3];
            tasks.Add(Task.Run(() => selector.ReportLatency(replica, TimeSpan.FromMilliseconds(Random.Shared.Next(1, 100)))));
            tasks.Add(Task.Run(() => selector.SelectReplica(ThreeReplicas)));
        }

        // Assert — no exceptions
        await Task.WhenAll(tasks);
    }
}
