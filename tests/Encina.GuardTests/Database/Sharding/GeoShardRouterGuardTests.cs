using Encina.Sharding;
using Encina.Sharding.Routing;

using Shouldly;

namespace Encina.GuardTests.Database.Sharding;

/// <summary>
/// Guard clause tests for <see cref="GeoShardRouter"/>.
/// </summary>
public sealed class GeoShardRouterGuardTests
{
    private static ShardTopology CreateTopology() =>
        new([new ShardInfo("shard-1", "conn-1")]);

    [Fact]
    public void Constructor_NullTopology_ThrowsArgumentNullException()
    {
        var regions = new[] { new GeoRegion("US", "shard-1") };
        Func<string, string> resolver = key => "US";
        var ex = Should.Throw<ArgumentNullException>(() => new GeoShardRouter(null!, regions, resolver));
        ex.ParamName.ShouldBe("topology");
    }

    [Fact]
    public void Constructor_NullRegions_ThrowsArgumentNullException()
    {
        Func<string, string> resolver = key => "US";
        var ex = Should.Throw<ArgumentNullException>(() => new GeoShardRouter(CreateTopology(), null!, resolver));
        ex.ParamName.ShouldBe("regions");
    }

    [Fact]
    public void Constructor_NullResolver_ThrowsArgumentNullException()
    {
        var regions = new[] { new GeoRegion("US", "shard-1") };
        var ex = Should.Throw<ArgumentNullException>(() => new GeoShardRouter(CreateTopology(), regions, null!));
        ex.ParamName.ShouldBe("regionResolver");
    }

    [Fact]
    public void GetShardId_NullKey_ThrowsArgumentNullException()
    {
        var regions = new[] { new GeoRegion("US", "shard-1") };
        var router = new GeoShardRouter(CreateTopology(), regions, key => "US");
        var ex = Should.Throw<ArgumentNullException>(() => router.GetShardId(null!));
        ex.ParamName.ShouldBe("shardKey");
    }
}
