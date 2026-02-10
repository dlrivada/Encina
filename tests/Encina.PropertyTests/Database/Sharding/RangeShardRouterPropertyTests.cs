using Encina.Sharding;
using Encina.Sharding.Routing;

using FsCheck;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Database.Sharding;

/// <summary>
/// Property-based tests for <see cref="RangeShardRouter"/> invariants.
/// Verifies monotonic routing and boundary consistency across generated inputs.
/// </summary>
[Trait("Category", "Property")]
public sealed class RangeShardRouterPropertyTests
{
    private static ShardTopology CreateTopology() =>
        new([
            new ShardInfo("shard-az", "conn-1"),
            new ShardInfo("shard-mz", "conn-2"),
            new ShardInfo("shard-default", "conn-3")
        ]);

    private static RangeShardRouter CreateRouter()
    {
        var topology = CreateTopology();
        var ranges = new[]
        {
            new ShardRange("A", "M", "shard-az"),
            new ShardRange("M", "Z", "shard-mz")
        };
        return new RangeShardRouter(topology, ranges);
    }

    #region Monotonic Routing

    [Property(MaxTest = 100)]
    public bool Property_GetShardId_KeysInSameRangeGoToSameShard(byte suffix1, byte suffix2)
    {
        var router = CreateRouter();

        // Both keys start with 'B' so they're in the A-M range
        var key1 = $"B{suffix1}";
        var key2 = $"B{suffix2}";

        var result1 = router.GetShardId(key1);
        var result2 = router.GetShardId(key2);

        if (!result1.IsRight || !result2.IsRight) return false;

        string shard1 = string.Empty, shard2 = string.Empty;
        _ = result1.IfRight(s => shard1 = s);
        _ = result2.IfRight(s => shard2 = s);

        return shard1 == shard2;
    }

    [Property(MaxTest = 100)]
    public bool Property_GetShardId_DeterministicForSameKey(NonEmptyString suffix)
    {
        var router = CreateRouter();

        // Prefix with 'A' to ensure it's in range
        var key = $"A{suffix.Get}";

        var result1 = router.GetShardId(key);
        var result2 = router.GetShardId(key);

        return result1.IsRight && result2.IsRight && result1 == result2;
    }

    #endregion

    #region Boundary Consistency

    [Fact]
    [Trait("Category", "Property")]
    public void Property_GetShardId_BoundaryKeyGoesToCorrectRange()
    {
        var router = CreateRouter();

        // "A" is start of first range (inclusive) → should be in shard-az
        var resultA = router.GetShardId("A");
        Assert.True(resultA.IsRight);

        string shardA = string.Empty;
        _ = resultA.IfRight(s => shardA = s);
        Assert.Equal("shard-az", shardA);

        // "M" is start of second range → should be in shard-mz
        var resultM = router.GetShardId("M");
        Assert.True(resultM.IsRight);

        string shardM = string.Empty;
        _ = resultM.IfRight(s => shardM = s);
        Assert.Equal("shard-mz", shardM);
    }

    [Fact]
    [Trait("Category", "Property")]
    public void Property_GetShardId_KeyOutsideAllRangesReturnsLeft()
    {
        var router = CreateRouter();

        // Keys starting with digits are outside the A-Z ranges
        var result = router.GetShardId("0something");
        Assert.True(result.IsLeft);
    }

    #endregion

    #region Coverage

    [Property(MaxTest = 100)]
    public bool Property_GetShardId_ValidRangeKeyAlwaysReturnsKnownShard(byte charIndex)
    {
        var router = CreateRouter();

        // Generate keys that fall in A-Z range
        var ch = (char)('A' + (charIndex % 25)); // A through Y
        var key = $"{ch}key";

        var result = router.GetShardId(key);

        if (!result.IsRight) return false;

        string shardId = string.Empty;
        _ = result.IfRight(s => shardId = s);

        return shardId == "shard-az" || shardId == "shard-mz";
    }

    [Property(MaxTest = 50)]
    public bool Property_GetAllShardIds_MatchesTopology()
    {
        var topology = CreateTopology();
        var ranges = new[] { new ShardRange("A", "Z", "shard-az") };
        var router = new RangeShardRouter(topology, ranges);

        return router.GetAllShardIds().Count == topology.AllShardIds.Count;
    }

    #endregion
}
