using Encina.Sharding;
using Encina.Sharding.Routing;

using FsCheck;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Database.Sharding;

/// <summary>
/// Property-based tests for <see cref="HashShardRouter"/> invariants.
/// Verifies determinism, coverage, and distribution across all generated inputs.
/// </summary>
[Trait("Category", "Property")]
public sealed class HashShardRouterPropertyTests
{
    private static ShardTopology CreateTopology(int shardCount = 3)
    {
        var shards = Enumerable.Range(1, shardCount)
            .Select(i => new ShardInfo($"shard-{i}", $"conn-{i}"))
            .ToList();
        return new ShardTopology(shards);
    }

    #region Determinism

    [Property(MaxTest = 200)]
    public bool Property_GetShardId_SameKeyAlwaysReturnsSameShard(NonEmptyString key)
    {
        var topology = CreateTopology();
        var router = new HashShardRouter(topology);

        var result1 = router.GetShardId(key.Get);
        var result2 = router.GetShardId(key.Get);

        return result1.IsRight && result2.IsRight &&
               result1 == result2;
    }

    [Property(MaxTest = 100)]
    public bool Property_GetShardId_DeterministicAcrossInstances(NonEmptyString key)
    {
        var topology = CreateTopology();
        var router1 = new HashShardRouter(topology);
        var router2 = new HashShardRouter(topology);

        var result1 = router1.GetShardId(key.Get);
        var result2 = router2.GetShardId(key.Get);

        return result1.IsRight && result2.IsRight &&
               result1 == result2;
    }

    #endregion

    #region Coverage

    [Property(MaxTest = 200)]
    public bool Property_GetShardId_AlwaysReturnsValidShard(NonEmptyString key)
    {
        var topology = CreateTopology();
        var router = new HashShardRouter(topology);

        var result = router.GetShardId(key.Get);

        if (!result.IsRight) return false;

        string shardId = string.Empty;
        _ = result.IfRight(s => shardId = s);

        return topology.AllShardIds.Contains(shardId);
    }

    [Property(MaxTest = 100)]
    public bool Property_GetShardId_DifferentShardCountsAlwaysReturnValid(PositiveInt shardCount, NonEmptyString key)
    {
        var clampedCount = Math.Clamp(shardCount.Get, 1, 20);
        var topology = CreateTopology(clampedCount);
        var router = new HashShardRouter(topology);

        var result = router.GetShardId(key.Get);

        if (!result.IsRight) return false;

        string shardId = string.Empty;
        _ = result.IfRight(s => shardId = s);

        return topology.AllShardIds.Contains(shardId);
    }

    #endregion

    #region Distribution

    [Fact]
    [Trait("Category", "Property")]
    public void Property_GetShardId_DistributesAcrossMultipleShards()
    {
        // Generate many keys and verify they don't all land on the same shard
        var topology = CreateTopology(5);
        var router = new HashShardRouter(topology);

        var shardHits = new System.Collections.Generic.HashSet<string>();

        for (var i = 0; i < 500; i++)
        {
            var result = router.GetShardId($"key-{i}-{Guid.NewGuid()}");
            _ = result.IfRight(s => shardHits.Add(s));
        }

        // With 500 random keys and 5 shards, we should hit at least 2 shards
        Assert.True(shardHits.Count >= 2,
            $"Expected at least 2 shards to be used, but only {shardHits.Count} were used");
    }

    #endregion

    #region GetAllShardIds

    [Property(MaxTest = 50)]
    public bool Property_GetAllShardIds_MatchesTopology(PositiveInt shardCount)
    {
        var clampedCount = Math.Clamp(shardCount.Get, 1, 10);
        var topology = CreateTopology(clampedCount);
        var router = new HashShardRouter(topology);

        var routerShardIds = router.GetAllShardIds();

        return routerShardIds.Count == clampedCount &&
               topology.AllShardIds.All(id => routerShardIds.Contains(id));
    }

    #endregion

    #region GetShardConnectionString

    [Property(MaxTest = 50)]
    public bool Property_GetShardConnectionString_ValidShardIdReturnsRight(PositiveInt shardIndex)
    {
        var topology = CreateTopology(5);
        var router = new HashShardRouter(topology);

        var index = (shardIndex.Get % 5) + 1;
        var shardId = $"shard-{index}";

        var result = router.GetShardConnectionString(shardId);

        return result.IsRight;
    }

    [Property(MaxTest = 50)]
    public bool Property_GetShardConnectionString_UnknownShardIdReturnsLeft(NonEmptyString unknownId)
    {
        var topology = CreateTopology(3);
        var router = new HashShardRouter(topology);

        // Only test with IDs that don't match our pattern
        if (unknownId.Get.StartsWith("shard-", StringComparison.Ordinal)) return true; // Skip known IDs

        var result = router.GetShardConnectionString(unknownId.Get);

        return result.IsLeft;
    }

    #endregion
}
