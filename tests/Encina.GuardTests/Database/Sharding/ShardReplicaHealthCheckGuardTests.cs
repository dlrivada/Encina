using Encina.Sharding;
using Encina.Sharding.Health;
using Encina.Sharding.ReplicaSelection;
using NSubstitute;
using Shouldly;

namespace Encina.GuardTests.Database.Sharding;

/// <summary>
/// Guard clause tests for <see cref="ShardReplicaHealthCheck"/>.
/// </summary>
public sealed class ShardReplicaHealthCheckGuardTests
{
    private static ShardTopology CreateTopology()
        => new([new ShardInfo("shard-0", "conn-0")]);

    [Fact]
    public void Constructor_NullTopology_ThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() =>
            new ShardReplicaHealthCheck(null!, Substitute.For<IReplicaHealthTracker>(), new ShardedReadWriteOptions()));
        ex.ParamName.ShouldBe("topology");
    }

    [Fact]
    public void Constructor_NullHealthTracker_ThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() =>
            new ShardReplicaHealthCheck(CreateTopology(), null!, new ShardedReadWriteOptions()));
        ex.ParamName.ShouldBe("healthTracker");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() =>
            new ShardReplicaHealthCheck(CreateTopology(), Substitute.For<IReplicaHealthTracker>(), null!));
        ex.ParamName.ShouldBe("options");
    }
}
