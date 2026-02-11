using Encina.Sharding;
using Encina.Sharding.Routing;

using Shouldly;

namespace Encina.ContractTests.Database.Sharding;

/// <summary>
/// Contract tests verifying that all router implementations handle compound shard keys consistently.
/// Each router must route single-component compound keys identically to equivalent simple string keys,
/// and support partial key routing via <see cref="IShardRouter.GetShardIds(CompoundShardKey)"/>.
/// </summary>
[Trait("Category", "Contract")]
public sealed class CompoundShardKeyRouterContractTests
{
    private static ShardTopology CreateTopology(int count = 4)
    {
        var shards = Enumerable.Range(1, count)
            .Select(i => new ShardInfo($"shard-{i}", $"conn-{i}"))
            .ToList();
        return new ShardTopology(shards);
    }

    private static List<ShardRange> CreateRanges() =>
    [
        new ShardRange("A", "H", "shard-1"),
        new ShardRange("H", "P", "shard-2"),
        new ShardRange("P", "X", "shard-3"),
        new ShardRange("X", null, "shard-4")
    ];

    #region Single-component compound key equivalence

    [Fact]
    public void Contract_HashRouter_SingleComponentCompoundKey_EquivalentToStringKey()
    {
        var topology = CreateTopology();
        var router = new HashShardRouter(topology);

        var stringResult = router.GetShardId("customer-42");
        var compoundResult = router.GetShardId(new CompoundShardKey("customer-42"));

        stringResult.IsRight.ShouldBeTrue();
        compoundResult.IsRight.ShouldBeTrue();

        string stringShardId = string.Empty;
        string compoundShardId = string.Empty;
        _ = stringResult.IfRight(s => stringShardId = s);
        _ = compoundResult.IfRight(s => compoundShardId = s);

        compoundShardId.ShouldBe(stringShardId);
    }

    [Fact]
    public void Contract_RangeRouter_SingleComponentCompoundKey_EquivalentToStringKey()
    {
        var topology = CreateTopology();
        var ranges = CreateRanges();
        var router = new RangeShardRouter(topology, ranges);

        var stringResult = router.GetShardId("Hello");
        var compoundResult = router.GetShardId(new CompoundShardKey("Hello"));

        stringResult.IsRight.ShouldBeTrue();
        compoundResult.IsRight.ShouldBeTrue();

        string stringShardId = string.Empty;
        string compoundShardId = string.Empty;
        _ = stringResult.IfRight(s => stringShardId = s);
        _ = compoundResult.IfRight(s => compoundShardId = s);

        compoundShardId.ShouldBe(stringShardId);
    }

    [Fact]
    public void Contract_DirectoryRouter_SingleComponentCompoundKey_EquivalentToStringKey()
    {
        var topology = CreateTopology();
        var store = new InMemoryShardDirectoryStore();
        store.AddMapping("key-1", "shard-1");
        var router = new DirectoryShardRouter(topology, store);

        var stringResult = router.GetShardId("key-1");
        var compoundResult = router.GetShardId(new CompoundShardKey("key-1"));

        stringResult.IsRight.ShouldBeTrue();
        compoundResult.IsRight.ShouldBeTrue();

        string stringShardId = string.Empty;
        string compoundShardId = string.Empty;
        _ = stringResult.IfRight(s => stringShardId = s);
        _ = compoundResult.IfRight(s => compoundShardId = s);

        compoundShardId.ShouldBe(stringShardId);
    }

    [Fact]
    public void Contract_GeoRouter_SingleComponentCompoundKey_EquivalentToStringKey()
    {
        var topology = CreateTopology();
        var regions = new[]
        {
            new GeoRegion("US", "shard-1"),
            new GeoRegion("EU", "shard-2")
        };
        var router = new GeoShardRouter(topology, regions, key => key.Split(':')[0],
            new GeoShardRouterOptions { DefaultRegion = "US" });

        var stringResult = router.GetShardId("EU:data");
        var compoundResult = router.GetShardId(new CompoundShardKey("EU:data"));

        stringResult.IsRight.ShouldBeTrue();
        compoundResult.IsRight.ShouldBeTrue();

        string stringShardId = string.Empty;
        string compoundShardId = string.Empty;
        _ = stringResult.IfRight(s => stringShardId = s);
        _ = compoundResult.IfRight(s => compoundShardId = s);

        compoundShardId.ShouldBe(stringShardId);
    }

    #endregion

    #region Multi-component compound key routing

    [Fact]
    public void Contract_HashRouter_MultiComponentKey_RoutesSuccessfully()
    {
        var topology = CreateTopology();
        var router = new HashShardRouter(topology);

        var key = new CompoundShardKey("us-east", "customer-123");
        var result = router.GetShardId(key);

        result.IsRight.ShouldBeTrue();
        _ = result.IfRight(shardId =>
        {
            topology.AllShardIds.ShouldContain(shardId);
        });
    }

    [Fact]
    public void Contract_RangeRouter_MultiComponentKey_RoutesUsingPrimaryComponent()
    {
        var topology = CreateTopology();
        var ranges = CreateRanges();
        var router = new RangeShardRouter(topology, ranges);

        var key = new CompoundShardKey("Alpha", "secondary-data");
        var result = router.GetShardId(key);

        result.IsRight.ShouldBeTrue();
        _ = result.IfRight(shardId =>
        {
            // "Alpha" starts with A, should go to shard-1 (A-H range)
            shardId.ShouldBe("shard-1");
        });
    }

    #endregion

    #region Partial key routing (GetShardIds)

    [Fact]
    public void Contract_HashRouter_GetShardIds_ReturnsAllShards()
    {
        var topology = CreateTopology();
        var router = new HashShardRouter(topology);

        var partialKey = new CompoundShardKey("us-east");
        var result = router.GetShardIds(partialKey);

        result.IsRight.ShouldBeTrue();
        _ = result.IfRight(shardIds =>
        {
            // Hash router can't narrow by partial key, returns all
            shardIds.Count.ShouldBe(topology.AllShardIds.Count);
        });
    }

    [Fact]
    public void Contract_RangeRouter_GetShardIds_NarrowsByPrimaryComponent()
    {
        var topology = CreateTopology();
        var ranges = CreateRanges();
        var router = new RangeShardRouter(topology, ranges);

        var partialKey = new CompoundShardKey("Alpha");
        var result = router.GetShardIds(partialKey);

        result.IsRight.ShouldBeTrue();
        _ = result.IfRight(shardIds =>
        {
            // Range router can narrow to matching ranges
            shardIds.Count.ShouldBeGreaterThan(0);
        });
    }

    [Fact]
    public void Contract_GeoRouter_GetShardIds_NarrowsByRegion()
    {
        var topology = CreateTopology();
        var regions = new[]
        {
            new GeoRegion("US", "shard-1"),
            new GeoRegion("EU", "shard-2"),
            new GeoRegion("AP", "shard-3")
        };
        var router = new GeoShardRouter(topology, regions, key => key,
            new GeoShardRouterOptions { DefaultRegion = "US" });

        var partialKey = new CompoundShardKey("EU");
        var result = router.GetShardIds(partialKey);

        result.IsRight.ShouldBeTrue();
        _ = result.IfRight(shardIds =>
        {
            shardIds.ShouldContain("shard-2");
        });
    }

    #endregion

    #region All routers implement GetAllShardIds consistently

    [Fact]
    public void Contract_AllRouters_GetAllShardIds_MatchesTopology()
    {
        var topology = CreateTopology();
        IShardRouter[] routers =
        [
            new HashShardRouter(topology),
            new RangeShardRouter(topology, CreateRanges()),
            new CompoundShardRouter(topology, new CompoundShardRouterOptions
            {
                ComponentRouters = { [0] = new HashShardRouter(topology) }
            })
        ];

        foreach (var router in routers)
        {
            var shardIds = router.GetAllShardIds();
            shardIds.Count.ShouldBe(topology.AllShardIds.Count);
            foreach (var id in topology.AllShardIds)
            {
                shardIds.ShouldContain(id);
            }
        }
    }

    #endregion
}
