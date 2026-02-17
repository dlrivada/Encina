using Encina.Sharding;
using Encina.Sharding.Routing;

using NSubstitute;

using Shouldly;

namespace Encina.GuardTests.Database.Sharding;

/// <summary>
/// Guard clause tests for <see cref="DirectoryShardRouter"/>.
/// </summary>
public sealed class DirectoryShardRouterGuardTests
{
    private static ShardTopology CreateTopology() =>
        new([new ShardInfo("shard-1", "conn-1")]);

    [Fact]
    public void Constructor_NullTopology_ThrowsArgumentNullException()
    {
        var store = Substitute.For<IShardDirectoryStore>();
        var ex = Should.Throw<ArgumentNullException>(() => new DirectoryShardRouter(null!, store));
        ex.ParamName.ShouldBe("topology");
    }

    [Fact]
    public void Constructor_NullStore_ThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() => new DirectoryShardRouter(CreateTopology(), null!));
        ex.ParamName.ShouldBe("store");
    }

    [Fact]
    public void GetShardId_NullKey_ThrowsArgumentNullException()
    {
        var store = Substitute.For<IShardDirectoryStore>();
        var router = new DirectoryShardRouter(CreateTopology(), store);
        var ex = Should.Throw<ArgumentNullException>(() => router.GetShardId(null!));
        ex.ParamName.ShouldBe("shardKey");
    }
}
