using Encina.Sharding;
using Encina.Sharding.Routing;

using Shouldly;

namespace Encina.GuardTests.Database.Sharding;

/// <summary>
/// Guard clause tests for <see cref="RangeShardRouter"/>.
/// </summary>
public sealed class RangeShardRouterGuardTests
{
    private static ShardTopology CreateTopology() =>
        new([new ShardInfo("shard-1", "conn-1"), new ShardInfo("shard-2", "conn-2")]);

    [Fact]
    public void Constructor_NullTopology_ThrowsArgumentNullException()
    {
        var ranges = new[] { new ShardRange("A", "M", "shard-1") };
        var ex = Should.Throw<ArgumentNullException>(() => new RangeShardRouter(null!, ranges));
        ex.ParamName.ShouldBe("topology");
    }

    [Fact]
    public void Constructor_NullRanges_ThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() => new RangeShardRouter(CreateTopology(), null!));
        ex.ParamName.ShouldBe("ranges");
    }

    [Fact]
    public void GetShardId_NullKey_ThrowsArgumentNullException()
    {
        var ranges = new[] { new ShardRange("A", "Z", "shard-1") };
        var router = new RangeShardRouter(CreateTopology(), ranges);
        var ex = Should.Throw<ArgumentNullException>(() => router.GetShardId(null!));
        ex.ParamName.ShouldBe("shardKey");
    }
}
