using Encina.Sharding;
using Encina.Sharding.Routing;

using FsCheck;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Database.Sharding;

/// <summary>
/// Property-based tests for <see cref="DirectoryShardRouter"/> invariants.
/// Verifies add-then-lookup consistency and remove-then-lookup behavior.
/// </summary>
[Trait("Category", "Property")]
public sealed class DirectoryShardRouterPropertyTests
{
    private static ShardTopology CreateTopology() =>
        new([
            new ShardInfo("shard-1", "conn-1"),
            new ShardInfo("shard-2", "conn-2"),
            new ShardInfo("shard-3", "conn-3")
        ]);

    private static InMemoryShardDirectoryStore CreateStore() => new();

    #region Add Then Lookup

    [Property(MaxTest = 100)]
    public bool Property_AddMapping_ThenGetShardId_AlwaysSucceeds(NonEmptyString key)
    {
        var store = CreateStore();
        var router = new DirectoryShardRouter(CreateTopology(), store);

        router.AddMapping(key.Get, "shard-1");
        var result = router.GetShardId(key.Get);

        if (!result.IsRight) return false;

        string shardId = string.Empty;
        _ = result.IfRight(s => shardId = s);

        return shardId == "shard-1";
    }

    [Property(MaxTest = 100)]
    public bool Property_AddMapping_OverwriteMapping_ReturnsLatest(NonEmptyString key)
    {
        var store = CreateStore();
        var router = new DirectoryShardRouter(CreateTopology(), store);

        router.AddMapping(key.Get, "shard-1");
        router.AddMapping(key.Get, "shard-2");
        var result = router.GetShardId(key.Get);

        if (!result.IsRight) return false;

        string shardId = string.Empty;
        _ = result.IfRight(s => shardId = s);

        return shardId == "shard-2";
    }

    #endregion

    #region Remove Then Lookup

    [Property(MaxTest = 100)]
    public bool Property_RemoveMapping_ThenGetShardId_ReturnsError(NonEmptyString key)
    {
        var store = CreateStore();
        var router = new DirectoryShardRouter(CreateTopology(), store);

        router.AddMapping(key.Get, "shard-1");
        router.RemoveMapping(key.Get);
        var result = router.GetShardId(key.Get);

        return result.IsLeft;
    }

    [Property(MaxTest = 100)]
    public bool Property_RemoveMapping_ThenGetShardId_WithDefault_ReturnsDefault(NonEmptyString key)
    {
        var store = CreateStore();
        var router = new DirectoryShardRouter(CreateTopology(), store, defaultShardId: "shard-3");

        router.AddMapping(key.Get, "shard-1");
        router.RemoveMapping(key.Get);
        var result = router.GetShardId(key.Get);

        if (!result.IsRight) return false;

        string shardId = string.Empty;
        _ = result.IfRight(s => shardId = s);

        return shardId == "shard-3";
    }

    #endregion

    #region GetMapping Consistency

    [Property(MaxTest = 100)]
    public bool Property_GetMapping_AfterAdd_ReturnsCorrectShardId(NonEmptyString key)
    {
        var store = CreateStore();
        var router = new DirectoryShardRouter(CreateTopology(), store);

        router.AddMapping(key.Get, "shard-2");
        var mapping = router.GetMapping(key.Get);

        return mapping == "shard-2";
    }

    [Property(MaxTest = 100)]
    public bool Property_GetMapping_AfterRemove_ReturnsNull(NonEmptyString key)
    {
        var store = CreateStore();
        var router = new DirectoryShardRouter(CreateTopology(), store);

        router.AddMapping(key.Get, "shard-1");
        router.RemoveMapping(key.Get);
        var mapping = router.GetMapping(key.Get);

        return mapping is null;
    }

    #endregion
}
