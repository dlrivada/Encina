using Encina.Sharding;
using Encina.Sharding.Routing;

namespace Encina.UnitTests.Core.Sharding;

/// <summary>
/// Unit tests for <see cref="CompoundShardRouter"/>.
/// </summary>
public sealed class CompoundShardRouterTests
{
    private static ShardTopology CreateTopology(int shardCount = 4)
    {
        var shards = Enumerable.Range(1, shardCount)
            .Select(i => new ShardInfo($"shard-{i}", $"conn-{i}"))
            .ToList();
        return new ShardTopology(shards);
    }

    // ────────────────────────────────────────────────────────────
    //  Hierarchical routing: range + hash
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void GetShardId_CompoundKey_RoutesEachComponentThroughDedicatedRouter()
    {
        var topology = CreateTopology();
        var rangeRouter = new RangeShardRouter(topology, new[]
        {
            new ShardRange("A", "N", "shard-1"),
            new ShardRange("N", null, "shard-2")
        });
        var hashRouter = new HashShardRouter(topology);

        var options = new CompoundShardRouterOptions
        {
            ComponentRouters = { [0] = rangeRouter, [1] = hashRouter },
            ShardIdCombiner = parts => string.Join("-", parts)
        };

        var router = new CompoundShardRouter(topology, options);
        var key = new CompoundShardKey("Alpha", "customer-123");

        var result = router.GetShardId(key);

        result.IsRight.ShouldBeTrue();
        _ = result.IfRight(shardId =>
        {
            // Range routes "Alpha" to "shard-1", hash routes "customer-123"
            shardId.ShouldStartWith("shard-1-");
        });
    }

    [Fact]
    public void GetShardId_CompoundKey_CombinerJoinsResults()
    {
        var topology = CreateTopology();
        var hashRouter1 = new HashShardRouter(topology);
        var hashRouter2 = new HashShardRouter(topology);

        var options = new CompoundShardRouterOptions
        {
            ComponentRouters = { [0] = hashRouter1, [1] = hashRouter2 },
            ShardIdCombiner = parts => string.Join("::", parts)
        };

        var router = new CompoundShardRouter(topology, options);
        var key = new CompoundShardKey("region-a", "customer-b");

        var result = router.GetShardId(key);

        result.IsRight.ShouldBeTrue();
        _ = result.IfRight(shardId =>
        {
            shardId.ShouldContain("::");
        });
    }

    // ────────────────────────────────────────────────────────────
    //  Simple string key delegates to first router
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void GetShardId_StringKey_DelegatesToFirstRouter()
    {
        var topology = CreateTopology();
        var hashRouter = new HashShardRouter(topology);

        var options = new CompoundShardRouterOptions
        {
            ComponentRouters = { [0] = hashRouter }
        };

        var router = new CompoundShardRouter(topology, options);
        var stringResult = router.GetShardId("customer-123");
        var firstRouterResult = hashRouter.GetShardId("customer-123");

        stringResult.IsRight.ShouldBeTrue();
        firstRouterResult.IsRight.ShouldBeTrue();

        string stringShardId = string.Empty;
        string firstRouterShardId = string.Empty;
        _ = stringResult.IfRight(s => stringShardId = s);
        _ = firstRouterResult.IfRight(s => firstRouterShardId = s);

        stringShardId.ShouldBe(firstRouterShardId);
    }

    // ────────────────────────────────────────────────────────────
    //  Partial key routing
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void GetShardIds_EmptyPartialKey_ReturnsAllShards()
    {
        var topology = CreateTopology();
        var hashRouter = new HashShardRouter(topology);

        var options = new CompoundShardRouterOptions
        {
            ComponentRouters = { [0] = hashRouter, [1] = hashRouter }
        };

        var router = new CompoundShardRouter(topology, options);
        var emptyKey = new CompoundShardKey("placeholder"); // single component as "partial"

        // GetShardIds with 0-component key returns all
        var result = router.GetShardIds(new CompoundShardKey("any"));

        result.IsRight.ShouldBeTrue();
        _ = result.IfRight(shardIds =>
        {
            shardIds.Count.ShouldBe(topology.AllShardIds.Count);
        });
    }

    // ────────────────────────────────────────────────────────────
    //  GetAllShardIds
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void GetAllShardIds_ReturnsTopologyShardIds()
    {
        var topology = CreateTopology(3);
        var hashRouter = new HashShardRouter(topology);

        var options = new CompoundShardRouterOptions
        {
            ComponentRouters = { [0] = hashRouter }
        };

        var router = new CompoundShardRouter(topology, options);

        var shardIds = router.GetAllShardIds();

        shardIds.Count.ShouldBe(3);
        shardIds.ShouldContain("shard-1");
        shardIds.ShouldContain("shard-2");
        shardIds.ShouldContain("shard-3");
    }

    // ────────────────────────────────────────────────────────────
    //  GetShardConnectionString
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void GetShardConnectionString_ValidShardId_ReturnsConnectionString()
    {
        var topology = CreateTopology(2);
        var hashRouter = new HashShardRouter(topology);

        var options = new CompoundShardRouterOptions
        {
            ComponentRouters = { [0] = hashRouter }
        };

        var router = new CompoundShardRouter(topology, options);
        var result = router.GetShardConnectionString("shard-1");

        result.IsRight.ShouldBeTrue();
        _ = result.IfRight(conn => conn.ShouldBe("conn-1"));
    }

    [Fact]
    public void GetShardConnectionString_UnknownShardId_ReturnsLeft()
    {
        var topology = CreateTopology(2);
        var hashRouter = new HashShardRouter(topology);

        var options = new CompoundShardRouterOptions
        {
            ComponentRouters = { [0] = hashRouter }
        };

        var router = new CompoundShardRouter(topology, options);
        var result = router.GetShardConnectionString("nonexistent");

        result.IsLeft.ShouldBeTrue();
    }

    // ────────────────────────────────────────────────────────────
    //  Construction validation
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_NoComponentRouters_ThrowsArgumentException()
    {
        var topology = CreateTopology();
        var options = new CompoundShardRouterOptions();

        Should.Throw<ArgumentException>(() => new CompoundShardRouter(topology, options));
    }

    [Fact]
    public void Constructor_NonContiguousIndices_ThrowsArgumentException()
    {
        var topology = CreateTopology();
        var hashRouter = new HashShardRouter(topology);

        var options = new CompoundShardRouterOptions
        {
            ComponentRouters = { [0] = hashRouter, [2] = hashRouter } // Gap at index 1
        };

        Should.Throw<ArgumentException>(() => new CompoundShardRouter(topology, options));
    }
}
