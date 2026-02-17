using Encina.Sharding;

using FsCheck;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Database.Sharding;

/// <summary>
/// Property-based tests for <see cref="ShardTopology"/> invariants.
/// Verifies ContainsShard ↔ GetShard consistency and active ⊆ all shards.
/// </summary>
[Trait("Category", "Property")]
public sealed class ShardTopologyPropertyTests
{
    #region ContainsShard ↔ GetShard Consistency

    [Property(MaxTest = 100)]
    public bool Property_ContainsShard_TrueImpliesGetShardReturnsRight(PositiveInt shardCount)
    {
        var count = Math.Clamp(shardCount.Get, 1, 20);
        var shards = Enumerable.Range(1, count)
            .Select(i => new ShardInfo($"shard-{i}", $"conn-{i}"))
            .ToList();
        var topology = new ShardTopology(shards);

        // For every shard in the topology, ContainsShard → GetShard succeeds
        foreach (var shard in shards)
        {
            if (!topology.ContainsShard(shard.ShardId)) return false;
            if (!topology.GetShard(shard.ShardId).IsRight) return false;
        }

        return true;
    }

    [Property(MaxTest = 100)]
    public bool Property_ContainsShard_FalseImpliesGetShardReturnsLeft(NonEmptyString unknownId)
    {
        var topology = new ShardTopology([new ShardInfo("shard-1", "conn-1")]);

        // Skip if it matches existing shard
        if (string.Equals(unknownId.Get, "shard-1", StringComparison.OrdinalIgnoreCase))
            return true;

        return !topology.ContainsShard(unknownId.Get) &&
               topology.GetShard(unknownId.Get).IsLeft;
    }

    #endregion

    #region Active Subset ⊆ All Shards

    [Property(MaxTest = 100)]
    public bool Property_ActiveShardIds_IsSubsetOfAllShardIds(PositiveInt shardCount, bool someInactive)
    {
        var count = Math.Clamp(shardCount.Get, 1, 10);
        var shards = Enumerable.Range(1, count)
            .Select(i => new ShardInfo(
                $"shard-{i}",
                $"conn-{i}",
                IsActive: someInactive ? i % 2 == 0 : true))
            .ToList();
        var topology = new ShardTopology(shards);

        // Active shards must be a subset of all shards
        return topology.ActiveShardIds.All(id => topology.AllShardIds.Contains(id));
    }

    [Property(MaxTest = 50)]
    public bool Property_ActiveShardIds_CountLessThanOrEqualToAll(PositiveInt shardCount)
    {
        var count = Math.Clamp(shardCount.Get, 1, 10);
        var shards = Enumerable.Range(1, count)
            .Select(i => new ShardInfo($"shard-{i}", $"conn-{i}", IsActive: i % 2 == 0))
            .ToList();
        var topology = new ShardTopology(shards);

        return topology.ActiveShardIds.Count <= topology.AllShardIds.Count;
    }

    #endregion

    #region Count Consistency

    [Property(MaxTest = 100)]
    public bool Property_Count_MatchesAllShardIds(PositiveInt shardCount)
    {
        var count = Math.Clamp(shardCount.Get, 1, 20);
        var shards = Enumerable.Range(1, count)
            .Select(i => new ShardInfo($"shard-{i}", $"conn-{i}"))
            .ToList();
        var topology = new ShardTopology(shards);

        return topology.Count == count &&
               topology.AllShardIds.Count == count;
    }

    #endregion

    #region Case Insensitivity

    [Property(MaxTest = 50)]
    public bool Property_ContainsShard_IsCaseInsensitive(NonEmptyString id)
    {
        var topology = new ShardTopology([new ShardInfo(id.Get, "conn-1")]);

        return topology.ContainsShard(id.Get.ToUpperInvariant()) &&
               topology.ContainsShard(id.Get.ToLowerInvariant());
    }

    [Property(MaxTest = 50)]
    public bool Property_GetShard_IsCaseInsensitive(NonEmptyString id)
    {
        var topology = new ShardTopology([new ShardInfo(id.Get, "conn-1")]);

        return topology.GetShard(id.Get.ToUpperInvariant()).IsRight &&
               topology.GetShard(id.Get.ToLowerInvariant()).IsRight;
    }

    #endregion

    #region GetConnectionString Consistency

    [Property(MaxTest = 50)]
    public bool Property_GetConnectionString_MatchesShardInfo(PositiveInt shardIndex)
    {
        var shards = Enumerable.Range(1, 5)
            .Select(i => new ShardInfo($"shard-{i}", $"connection-string-{i}"))
            .ToList();
        var topology = new ShardTopology(shards);

        var index = (shardIndex.Get % 5) + 1;
        var expectedConn = $"connection-string-{index}";

        var result = topology.GetConnectionString($"shard-{index}");

        if (!result.IsRight) return false;

        string conn = string.Empty;
        _ = result.IfRight(s => conn = s);

        return conn == expectedConn;
    }

    #endregion

    #region Duplicate Detection

    [Property(MaxTest = 50)]
    public bool Property_Constructor_DuplicateShardIds_Throws(NonEmptyString id)
    {
        try
        {
            _ = new ShardTopology([
                new ShardInfo(id.Get, "conn-1"),
                new ShardInfo(id.Get, "conn-2")
            ]);
            return false; // Should have thrown
        }
        catch (ArgumentException)
        {
            return true;
        }
    }

    [Property(MaxTest = 50)]
    public bool Property_Constructor_DuplicateShardIds_CaseInsensitive_Throws(NonEmptyString id)
    {
        try
        {
            _ = new ShardTopology([
                new ShardInfo(id.Get.ToLowerInvariant(), "conn-1"),
                new ShardInfo(id.Get.ToUpperInvariant(), "conn-2")
            ]);
            return false; // Should have thrown
        }
        catch (ArgumentException)
        {
            return true;
        }
    }

    #endregion
}
