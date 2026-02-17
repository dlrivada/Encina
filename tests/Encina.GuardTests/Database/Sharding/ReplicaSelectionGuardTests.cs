using Encina.Sharding.ReplicaSelection;
using Shouldly;

namespace Encina.GuardTests.Database.Sharding;

/// <summary>
/// Guard clause tests for replica selection types.
/// </summary>
public sealed class ReplicaSelectionGuardTests
{
    // ────────────────────────────────────────────────────────────
    //  IShardReplicaSelector — Null/Empty Arguments
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void RoundRobin_NullReplicas_ThrowsArgumentNullException()
    {
        var selector = new RoundRobinShardReplicaSelector();
        var ex = Should.Throw<ArgumentNullException>(() => selector.SelectReplica(null!));
        ex.ParamName.ShouldBe("availableReplicas");
    }

    [Fact]
    public void RoundRobin_EmptyReplicas_ThrowsArgumentException()
    {
        var selector = new RoundRobinShardReplicaSelector();
        Should.Throw<ArgumentException>(() => selector.SelectReplica([]));
    }

    [Fact]
    public void Random_NullReplicas_ThrowsArgumentNullException()
    {
        var selector = new RandomShardReplicaSelector();
        var ex = Should.Throw<ArgumentNullException>(() => selector.SelectReplica(null!));
        ex.ParamName.ShouldBe("availableReplicas");
    }

    [Fact]
    public void Random_EmptyReplicas_ThrowsArgumentException()
    {
        var selector = new RandomShardReplicaSelector();
        Should.Throw<ArgumentException>(() => selector.SelectReplica([]));
    }

    [Fact]
    public void LeastLatency_NullReplicas_ThrowsArgumentNullException()
    {
        var selector = new LeastLatencyShardReplicaSelector();
        var ex = Should.Throw<ArgumentNullException>(() => selector.SelectReplica(null!));
        ex.ParamName.ShouldBe("availableReplicas");
    }

    [Fact]
    public void LeastLatency_EmptyReplicas_ThrowsArgumentException()
    {
        var selector = new LeastLatencyShardReplicaSelector();
        Should.Throw<ArgumentException>(() => selector.SelectReplica([]));
    }

    [Fact]
    public void LeastConnections_NullReplicas_ThrowsArgumentNullException()
    {
        var selector = new LeastConnectionsShardReplicaSelector();
        var ex = Should.Throw<ArgumentNullException>(() => selector.SelectReplica(null!));
        ex.ParamName.ShouldBe("availableReplicas");
    }

    [Fact]
    public void LeastConnections_EmptyReplicas_ThrowsArgumentException()
    {
        var selector = new LeastConnectionsShardReplicaSelector();
        Should.Throw<ArgumentException>(() => selector.SelectReplica([]));
    }

    [Fact]
    public void WeightedRandom_NullReplicas_ThrowsArgumentNullException()
    {
        var selector = new WeightedRandomShardReplicaSelector();
        var ex = Should.Throw<ArgumentNullException>(() => selector.SelectReplica(null!));
        ex.ParamName.ShouldBe("availableReplicas");
    }

    [Fact]
    public void WeightedRandom_EmptyReplicas_ThrowsArgumentException()
    {
        var selector = new WeightedRandomShardReplicaSelector();
        Should.Throw<ArgumentException>(() => selector.SelectReplica([]));
    }

    // ────────────────────────────────────────────────────────────
    //  LeastLatency — ReportLatency null check
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void LeastLatency_ReportLatency_NullReplica_ThrowsArgumentNullException()
    {
        var selector = new LeastLatencyShardReplicaSelector();
        var ex = Should.Throw<ArgumentNullException>(() => selector.ReportLatency(null!, TimeSpan.FromMilliseconds(10)));
        ex.ParamName.ShouldBe("replicaConnectionString");
    }

    // ────────────────────────────────────────────────────────────
    //  LeastConnections — Null checks
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void LeastConnections_IncrementConnections_NullReplica_ThrowsArgumentNullException()
    {
        var selector = new LeastConnectionsShardReplicaSelector();
        var ex = Should.Throw<ArgumentNullException>(() => selector.IncrementConnections(null!));
        ex.ParamName.ShouldBe("replicaConnectionString");
    }

    [Fact]
    public void LeastConnections_DecrementConnections_NullReplica_ThrowsArgumentNullException()
    {
        var selector = new LeastConnectionsShardReplicaSelector();
        var ex = Should.Throw<ArgumentNullException>(() => selector.DecrementConnections(null!));
        ex.ParamName.ShouldBe("replicaConnectionString");
    }

    // ────────────────────────────────────────────────────────────
    //  ReplicaHealthTracker — Null checks
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void HealthTracker_MarkHealthy_NullShardId_ThrowsArgumentNullException()
    {
        var tracker = new ReplicaHealthTracker();
        var ex = Should.Throw<ArgumentNullException>(() => tracker.MarkHealthy(null!, "replica"));
        ex.ParamName.ShouldBe("shardId");
    }

    [Fact]
    public void HealthTracker_MarkHealthy_NullReplica_ThrowsArgumentNullException()
    {
        var tracker = new ReplicaHealthTracker();
        var ex = Should.Throw<ArgumentNullException>(() => tracker.MarkHealthy("shard-0", null!));
        ex.ParamName.ShouldBe("replicaConnectionString");
    }

    [Fact]
    public void HealthTracker_MarkUnhealthy_NullShardId_ThrowsArgumentNullException()
    {
        var tracker = new ReplicaHealthTracker();
        var ex = Should.Throw<ArgumentNullException>(() => tracker.MarkUnhealthy(null!, "replica"));
        ex.ParamName.ShouldBe("shardId");
    }

    [Fact]
    public void HealthTracker_MarkUnhealthy_NullReplica_ThrowsArgumentNullException()
    {
        var tracker = new ReplicaHealthTracker();
        var ex = Should.Throw<ArgumentNullException>(() => tracker.MarkUnhealthy("shard-0", null!));
        ex.ParamName.ShouldBe("replicaConnectionString");
    }

    [Fact]
    public void HealthTracker_GetAvailableReplicas_NullShardId_ThrowsArgumentNullException()
    {
        var tracker = new ReplicaHealthTracker();
        var ex = Should.Throw<ArgumentNullException>(() =>
            tracker.GetAvailableReplicas(null!, ["replica"]));
        ex.ParamName.ShouldBe("shardId");
    }

    [Fact]
    public void HealthTracker_GetAvailableReplicas_NullAllReplicas_ThrowsArgumentNullException()
    {
        var tracker = new ReplicaHealthTracker();
        var ex = Should.Throw<ArgumentNullException>(() =>
            tracker.GetAvailableReplicas("shard-0", null!));
        ex.ParamName.ShouldBe("allReplicas");
    }

    [Fact]
    public void HealthTracker_ReportReplicationLag_NegativeLag_ThrowsArgumentOutOfRangeException()
    {
        var tracker = new ReplicaHealthTracker();
        Should.Throw<ArgumentOutOfRangeException>(() =>
            tracker.ReportReplicationLag("shard-0", "replica", TimeSpan.FromMilliseconds(-1)));
    }

    // ────────────────────────────────────────────────────────────
    //  AcceptStaleReadsAttribute — Negative value
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void AcceptStaleReadsAttribute_NegativeMaxLag_ThrowsArgumentOutOfRangeException()
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
            new AcceptStaleReadsAttribute(-1));
    }

    // ────────────────────────────────────────────────────────────
    //  ShardReplicaSelectorFactory — Invalid strategy
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Factory_InvalidStrategy_ThrowsArgumentOutOfRangeException()
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
            ShardReplicaSelectorFactory.Create((ReplicaSelectionStrategy)999));
    }

    // ────────────────────────────────────────────────────────────
    //  WeightedRandom — Invalid weights
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void WeightedRandom_Constructor_ZeroWeight_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() =>
            new WeightedRandomShardReplicaSelector([1, 0, 3]));
    }

    [Fact]
    public void WeightedRandom_Constructor_NegativeWeight_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() =>
            new WeightedRandomShardReplicaSelector([1, -2, 3]));
    }
}
