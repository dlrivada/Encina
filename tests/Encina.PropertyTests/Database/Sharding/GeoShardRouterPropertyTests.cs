using Encina.Sharding;
using Encina.Sharding.Routing;

using FsCheck;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Database.Sharding;

/// <summary>
/// Property-based tests for <see cref="GeoShardRouter"/> invariants.
/// Verifies region resolution determinism and fallback chain consistency.
/// </summary>
[Trait("Category", "Property")]
public sealed class GeoShardRouterPropertyTests
{
    private static ShardTopology CreateTopology() =>
        new([
            new ShardInfo("shard-us", "conn-us"),
            new ShardInfo("shard-eu", "conn-eu"),
            new ShardInfo("shard-ap", "conn-ap")
        ]);

    private static GeoShardRouter CreateRouter(
        string? defaultRegion = null,
        bool requireExactMatch = false)
    {
        var topology = CreateTopology();
        var regions = new[]
        {
            new GeoRegion("US", "shard-us"),
            new GeoRegion("EU", "shard-eu"),
            new GeoRegion("AP", "shard-ap")
        };
        var options = new GeoShardRouterOptions
        {
            DefaultRegion = defaultRegion,
            RequireExactMatch = requireExactMatch
        };
        return new GeoShardRouter(topology, regions, key => key.Split(':')[0], options);
    }

    #region Determinism

    [Property(MaxTest = 100)]
    public bool Property_GetShardId_SameKeyAlwaysReturnsSameResult(NonEmptyString suffix)
    {
        var router = CreateRouter(defaultRegion: "US");
        var key = $"US:{suffix.Get}";

        var result1 = router.GetShardId(key);
        var result2 = router.GetShardId(key);

        return result1.IsRight && result2.IsRight && result1 == result2;
    }

    [Property(MaxTest = 100)]
    public bool Property_GetShardId_DeterministicAcrossInstances(NonEmptyString suffix)
    {
        var key = $"EU:{suffix.Get}";

        var router1 = CreateRouter(defaultRegion: "US");
        var router2 = CreateRouter(defaultRegion: "US");

        var result1 = router1.GetShardId(key);
        var result2 = router2.GetShardId(key);

        return result1.IsRight && result2.IsRight && result1 == result2;
    }

    #endregion

    #region Region Resolution

    [Property(MaxTest = 50)]
    public bool Property_GetShardId_KnownRegionAlwaysReturnsCorrectShard(NonEmptyString suffix)
    {
        var router = CreateRouter();
        var key = $"US:{suffix.Get}";

        var result = router.GetShardId(key);

        if (!result.IsRight) return false;

        string shardId = string.Empty;
        _ = result.IfRight(s => shardId = s);

        return shardId == "shard-us";
    }

    [Property(MaxTest = 50)]
    public bool Property_GetShardId_UnknownRegionWithDefaultFallsBack(NonEmptyString suffix)
    {
        var router = CreateRouter(defaultRegion: "EU");
        var key = $"UNKNOWN:{suffix.Get}";

        var result = router.GetShardId(key);

        if (!result.IsRight) return false;

        string shardId = string.Empty;
        _ = result.IfRight(s => shardId = s);

        return shardId == "shard-eu";
    }

    [Property(MaxTest = 50)]
    public bool Property_GetShardId_UnknownRegionWithExactMatchReturnsError(NonEmptyString suffix)
    {
        var router = CreateRouter(requireExactMatch: true);
        var key = $"UNKNOWN:{suffix.Get}";

        var result = router.GetShardId(key);

        return result.IsLeft;
    }

    #endregion

    #region Case Insensitivity

    [Fact]
    [Trait("Category", "Property")]
    public void Property_GetShardId_RegionLookupIsCaseInsensitive()
    {
        var router = CreateRouter();

        var resultUpper = router.GetShardId("US:key1");
        var resultLower = router.GetShardId("us:key1");

        Assert.True(resultUpper.IsRight);
        Assert.True(resultLower.IsRight);

        string shardUpper = string.Empty, shardLower = string.Empty;
        _ = resultUpper.IfRight(s => shardUpper = s);
        _ = resultLower.IfRight(s => shardLower = s);

        Assert.Equal(shardUpper, shardLower);
    }

    #endregion

    #region GetAllShardIds

    [Fact]
    [Trait("Category", "Property")]
    public void Property_GetAllShardIds_MatchesTopology()
    {
        var router = CreateRouter();
        var allIds = router.GetAllShardIds();

        Assert.Equal(3, allIds.Count);
        Assert.Contains("shard-us", allIds);
        Assert.Contains("shard-eu", allIds);
        Assert.Contains("shard-ap", allIds);
    }

    #endregion
}
