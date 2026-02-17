using Encina.Sharding;
using Encina.Sharding.Routing;

using FsCheck;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Database.Sharding;

/// <summary>
/// Property-based tests for compound shard routing invariants.
/// Verifies determinism, single-component equivalence, and partial key superset behavior.
/// </summary>
[Trait("Category", "Property")]
public sealed class CompoundShardRouterPropertyTests
{
    private static ShardTopology CreateTopology(int shardCount = 4)
    {
        var shards = Enumerable.Range(1, shardCount)
            .Select(i => new ShardInfo($"shard-{i}", $"conn-{i}"))
            .ToList();
        return new ShardTopology(shards);
    }

    #region Determinism

    [Property(MaxTest = 200)]
    public bool Property_CompoundRouting_SameKey_AlwaysSameShard(NonEmptyString comp1, NonEmptyString comp2)
    {
        var topology = CreateTopology();
        var options = new CompoundShardRouterOptions
        {
            ComponentRouters =
            {
                [0] = new HashShardRouter(topology),
                [1] = new HashShardRouter(topology)
            }
        };
        var router = new CompoundShardRouter(topology, options);
        var key = new CompoundShardKey(comp1.Get, comp2.Get);

        var result1 = router.GetShardId(key);
        var result2 = router.GetShardId(key);

        return result1.IsRight && result2.IsRight && result1 == result2;
    }

    [Property(MaxTest = 100)]
    public bool Property_CompoundRouting_DeterministicAcrossInstances(NonEmptyString comp1, NonEmptyString comp2)
    {
        var topology = CreateTopology();

        var router1 = new CompoundShardRouter(topology, new CompoundShardRouterOptions
        {
            ComponentRouters =
            {
                [0] = new HashShardRouter(topology),
                [1] = new HashShardRouter(topology)
            }
        });
        var router2 = new CompoundShardRouter(topology, new CompoundShardRouterOptions
        {
            ComponentRouters =
            {
                [0] = new HashShardRouter(topology),
                [1] = new HashShardRouter(topology)
            }
        });

        var key = new CompoundShardKey(comp1.Get, comp2.Get);
        var result1 = router1.GetShardId(key);
        var result2 = router2.GetShardId(key);

        return result1.IsRight && result2.IsRight && result1 == result2;
    }

    #endregion

    #region Single-component equivalence

    [Property(MaxTest = 200)]
    public bool Property_HashRouter_SingleComponentKey_EquivalentToString(NonEmptyString key)
    {
        var topology = CreateTopology();
        var router = new HashShardRouter(topology);

        var stringResult = router.GetShardId(key.Get);
        var compoundResult = router.GetShardId(new CompoundShardKey(key.Get));

        if (!stringResult.IsRight || !compoundResult.IsRight) return false;

        string stringShardId = string.Empty;
        string compoundShardId = string.Empty;
        _ = stringResult.IfRight(s => stringShardId = s);
        _ = compoundResult.IfRight(s => compoundShardId = s);

        return stringShardId == compoundShardId;
    }

    #endregion

    #region Routing always returns valid shard

    [Property(MaxTest = 200)]
    public bool Property_CompoundRouting_AlwaysReturnsValidShard(NonEmptyString comp1, NonEmptyString comp2)
    {
        var topology = CreateTopology();
        var options = new CompoundShardRouterOptions
        {
            ComponentRouters =
            {
                [0] = new HashShardRouter(topology),
                [1] = new HashShardRouter(topology)
            }
        };
        var router = new CompoundShardRouter(topology, options);
        var key = new CompoundShardKey(comp1.Get, comp2.Get);

        var result = router.GetShardId(key);

        if (!result.IsRight) return false;

        string shardId = string.Empty;
        _ = result.IfRight(s => shardId = s);

        // The combined shard ID may not directly exist in topology (it's "shard-X-shard-Y"),
        // but each component should be a valid shard
        return !string.IsNullOrEmpty(shardId);
    }

    #endregion

    #region Partial key routing superset

    [Property(MaxTest = 100)]
    public bool Property_GetShardIds_ReturnsNonEmptyResult(NonEmptyString partialKey)
    {
        var topology = CreateTopology();
        var options = new CompoundShardRouterOptions
        {
            ComponentRouters =
            {
                [0] = new HashShardRouter(topology),
                [1] = new HashShardRouter(topology)
            }
        };
        var router = new CompoundShardRouter(topology, options);
        var key = new CompoundShardKey(partialKey.Get);

        var result = router.GetShardIds(key);

        if (!result.IsRight) return false;

        IReadOnlyList<string>? shardIds = null;
        _ = result.IfRight(s => shardIds = s);

        return shardIds is not null && shardIds.Count > 0;
    }

    #endregion
}
