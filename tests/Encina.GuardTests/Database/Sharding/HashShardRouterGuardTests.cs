using Encina.Sharding;
using Encina.Sharding.Routing;

using Shouldly;

namespace Encina.GuardTests.Database.Sharding;

/// <summary>
/// Guard clause tests for <see cref="HashShardRouter"/>.
/// </summary>
public sealed class HashShardRouterGuardTests
{
    private static ShardTopology CreateTopology() =>
        new([new ShardInfo("shard-1", "conn-1"), new ShardInfo("shard-2", "conn-2")]);

    [Fact]
    public void Constructor_NullTopology_ThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() => new HashShardRouter(null!));
        ex.ParamName.ShouldBe("topology");
    }

    [Fact]
    public void GetShardId_NullKey_ThrowsArgumentNullException()
    {
        var router = new HashShardRouter(CreateTopology());
        var ex = Should.Throw<ArgumentNullException>(() => router.GetShardId(null!));
        ex.ParamName.ShouldBe("shardKey");
    }
}
