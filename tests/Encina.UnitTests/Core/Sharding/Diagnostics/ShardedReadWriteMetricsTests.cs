using Encina.Sharding;
using Encina.Sharding.Diagnostics;
using Encina.Sharding.ReplicaSelection;
using NSubstitute;

namespace Encina.UnitTests.Core.Sharding.Diagnostics;

public sealed class ShardedReadWriteMetricsTests
{
    // ────────────────────────────────────────────────────────────
    //  Helpers
    // ────────────────────────────────────────────────────────────

    private static ShardTopology CreateTopology(params ShardInfo[] shards)
        => new(shards);

    private static ShardTopology CreateSimpleTopology()
        => CreateTopology(new ShardInfo("shard-0", "Server=primary0;",
            ReplicaConnectionStrings: ["Server=replica0a;", "Server=replica0b;"]));

    private static ShardedReadWriteMetrics CreateMetrics(
        ShardTopology? topology = null,
        IReplicaHealthTracker? tracker = null)
    {
        return new ShardedReadWriteMetrics(
            topology ?? CreateSimpleTopology(),
            tracker ?? Substitute.For<IReplicaHealthTracker>());
    }

    // ────────────────────────────────────────────────────────────
    //  Constructor
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_NullTopology_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ShardedReadWriteMetrics(null!, Substitute.For<IReplicaHealthTracker>()));
    }

    [Fact]
    public void Constructor_NullHealthTracker_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ShardedReadWriteMetrics(CreateSimpleTopology(), null!));
    }

    [Fact]
    public void Constructor_ValidArgs_DoesNotThrow()
    {
        Should.NotThrow(() => CreateMetrics());
    }

    // ────────────────────────────────────────────────────────────
    //  RecordRoutingDecision — no throw
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void RecordRoutingDecision_ReadIntent_DoesNotThrow()
    {
        var metrics = CreateMetrics();
        Should.NotThrow(() => metrics.RecordRoutingDecision("shard-0", "read", "replica0a"));
    }

    [Fact]
    public void RecordRoutingDecision_WriteIntent_DoesNotThrow()
    {
        var metrics = CreateMetrics();
        Should.NotThrow(() => metrics.RecordRoutingDecision("shard-0", "write"));
    }

    [Fact]
    public void RecordRoutingDecision_NullReplicaId_DoesNotThrow()
    {
        var metrics = CreateMetrics();
        Should.NotThrow(() => metrics.RecordRoutingDecision("shard-0", "read", null));
    }

    // ────────────────────────────────────────────────────────────
    //  RecordReplicaSelectionDuration — no throw
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void RecordReplicaSelectionDuration_DoesNotThrow()
    {
        var metrics = CreateMetrics();
        Should.NotThrow(() =>
            metrics.RecordReplicaSelectionDuration("shard-0", "RoundRobin", 0.5));
    }

    // ────────────────────────────────────────────────────────────
    //  RecordReplicaLatency — no throw
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void RecordReplicaLatency_DoesNotThrow()
    {
        var metrics = CreateMetrics();
        Should.NotThrow(() =>
            metrics.RecordReplicaLatency("shard-0", "replica0a", 15.3));
    }

    // ────────────────────────────────────────────────────────────
    //  RecordFallbackToPrimary — no throw
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void RecordFallbackToPrimary_DoesNotThrow()
    {
        var metrics = CreateMetrics();
        Should.NotThrow(() =>
            metrics.RecordFallbackToPrimary("shard-0", "all_unhealthy"));
    }

    [Theory]
    [InlineData("no_replicas")]
    [InlineData("all_unhealthy")]
    [InlineData("all_stale")]
    public void RecordFallbackToPrimary_VariousReasons_DoesNotThrow(string reason)
    {
        var metrics = CreateMetrics();
        Should.NotThrow(() => metrics.RecordFallbackToPrimary("shard-0", reason));
    }
}
