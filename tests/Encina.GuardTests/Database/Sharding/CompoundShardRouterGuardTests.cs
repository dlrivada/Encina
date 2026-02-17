using Encina.Sharding;
using Encina.Sharding.Routing;

using Shouldly;

namespace Encina.GuardTests.Database.Sharding;

/// <summary>
/// Guard clause tests for <see cref="CompoundShardRouter"/>.
/// </summary>
public sealed class CompoundShardRouterGuardTests
{
    private static ShardTopology CreateTopology() =>
        new([new ShardInfo("shard-1", "conn-1"), new ShardInfo("shard-2", "conn-2")]);

    [Fact]
    public void Constructor_NullTopology_ThrowsArgumentNullException()
    {
        var options = new CompoundShardRouterOptions
        {
            ComponentRouters = { [0] = new HashShardRouter(CreateTopology()) }
        };

        var ex = Should.Throw<ArgumentNullException>(() => new CompoundShardRouter(null!, options));
        ex.ParamName.ShouldBe("topology");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() =>
            new CompoundShardRouter(CreateTopology(), null!));
        ex.ParamName.ShouldBe("options");
    }

    [Fact]
    public void GetShardId_NullStringKey_ThrowsArgumentNullException()
    {
        var topology = CreateTopology();
        var options = new CompoundShardRouterOptions
        {
            ComponentRouters = { [0] = new HashShardRouter(topology) }
        };
        var router = new CompoundShardRouter(topology, options);

        var ex = Should.Throw<ArgumentNullException>(() => router.GetShardId((string)null!));
        ex.ParamName.ShouldBe("shardKey");
    }

    [Fact]
    public void GetShardId_NullCompoundKey_ThrowsArgumentNullException()
    {
        var topology = CreateTopology();
        var options = new CompoundShardRouterOptions
        {
            ComponentRouters = { [0] = new HashShardRouter(topology) }
        };
        var router = new CompoundShardRouter(topology, options);

        var ex = Should.Throw<ArgumentNullException>(() => router.GetShardId((CompoundShardKey)null!));
        ex.ParamName.ShouldBe("key");
    }

    [Fact]
    public void GetShardIds_NullPartialKey_ThrowsArgumentNullException()
    {
        var topology = CreateTopology();
        var options = new CompoundShardRouterOptions
        {
            ComponentRouters = { [0] = new HashShardRouter(topology) }
        };
        var router = new CompoundShardRouter(topology, options);

        var ex = Should.Throw<ArgumentNullException>(() => router.GetShardIds(null!));
        ex.ParamName.ShouldBe("partialKey");
    }

    [Fact]
    public void GetShardConnectionString_NullShardId_ThrowsArgumentNullException()
    {
        var topology = CreateTopology();
        var options = new CompoundShardRouterOptions
        {
            ComponentRouters = { [0] = new HashShardRouter(topology) }
        };
        var router = new CompoundShardRouter(topology, options);

        var ex = Should.Throw<ArgumentNullException>(() => router.GetShardConnectionString(null!));
        ex.ParamName.ShouldBe("shardId");
    }
}
