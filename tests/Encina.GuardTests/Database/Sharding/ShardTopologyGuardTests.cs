using Encina.Sharding;

using Shouldly;

namespace Encina.GuardTests.Database.Sharding;

/// <summary>
/// Guard clause tests for <see cref="ShardTopology"/>.
/// </summary>
public sealed class ShardTopologyGuardTests
{
    [Fact]
    public void Constructor_NullShards_ThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() => new ShardTopology(null!));
        ex.ParamName.ShouldBe("shards");
    }

    [Fact]
    public void GetShard_NullShardId_ThrowsArgumentNullException()
    {
        var topology = new ShardTopology([new ShardInfo("shard-1", "conn-1")]);

        var ex = Should.Throw<ArgumentNullException>(() => topology.GetShard(null!));
        ex.ParamName.ShouldBe("shardId");
    }

    [Fact]
    public void ContainsShard_NullShardId_ThrowsArgumentNullException()
    {
        var topology = new ShardTopology([new ShardInfo("shard-1", "conn-1")]);

        var ex = Should.Throw<ArgumentNullException>(() => topology.ContainsShard(null!));
        ex.ParamName.ShouldBe("shardId");
    }
}
