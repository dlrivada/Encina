using System.Reflection;
using Encina.Sharding;
using Encina.Sharding.Routing;
using Encina.Sharding.TimeBased;

namespace Encina.ContractTests.Database.Sharding;

/// <summary>
/// Contract tests for <see cref="TimeBasedShardRouter"/> verifying it fulfills
/// the <see cref="IShardRouter"/> and <see cref="ITimeBasedShardRouter"/> contracts.
/// </summary>
[Trait("Category", "Contract")]
public sealed class TimeBasedShardRouterContractTests
{
    #region Test Helpers

    private static TimeBasedShardRouter CreateRouter()
    {
        var tierInfos = new[]
        {
            new ShardTierInfo("orders-2026-01", ShardTier.Hot,
                new DateOnly(2026, 1, 1), new DateOnly(2026, 2, 1),
                false, "Server=hot;Database=orders_01", DateTime.UtcNow),
            new ShardTierInfo("orders-2025-12", ShardTier.Warm,
                new DateOnly(2025, 12, 1), new DateOnly(2026, 1, 1),
                true, "Server=warm;Database=orders_12", DateTime.UtcNow),
        };

        var shards = tierInfos.Select(t => new ShardInfo(t.ShardId, t.ConnectionString));
        var topology = new ShardTopology(shards);
        return new TimeBasedShardRouter(topology, tierInfos, ShardPeriod.Monthly);
    }

    #endregion

    #region IShardRouter Contract

    [Fact]
    public void Contract_ImplementsIShardRouter()
    {
        typeof(TimeBasedShardRouter).GetInterfaces()
            .ShouldContain(typeof(IShardRouter));
    }

    [Fact]
    public void Contract_ImplementsITimeBasedShardRouter()
    {
        typeof(TimeBasedShardRouter).GetInterfaces()
            .ShouldContain(typeof(ITimeBasedShardRouter));
    }

    [Fact]
    public void Contract_GetShardId_StringKey_ReturnsEither()
    {
        var router = CreateRouter();

        var result = router.GetShardId("2026-01-15");

        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public void Contract_GetShardId_CompoundKey_ReturnsEither()
    {
        var router = CreateRouter();

        var result = router.GetShardId(new CompoundShardKey("2026-01-15"));

        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public void Contract_GetShardIds_PrefixMatching_ReturnsEither()
    {
        var router = CreateRouter();

        var result = router.GetShardIds(new CompoundShardKey("2026"));

        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public void Contract_GetAllShardIds_ReturnsNonNull()
    {
        var router = CreateRouter();

        var shardIds = router.GetAllShardIds();

        shardIds.ShouldNotBeNull();
        shardIds.Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void Contract_GetShardConnectionString_ValidShard_ReturnsRight()
    {
        var router = CreateRouter();

        var result = router.GetShardConnectionString("orders-2026-01");

        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public void Contract_GetShardConnectionString_InvalidShard_ReturnsLeft()
    {
        var router = CreateRouter();

        var result = router.GetShardConnectionString("non-existent-shard");

        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region ITimeBasedShardRouter Contract

    [Fact]
    public async Task Contract_RouteByTimestampAsync_ReturnsEither()
    {
        var router = CreateRouter();

        var result = await router.RouteByTimestampAsync(
            new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc));

        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task Contract_RouteWriteByTimestampAsync_HotShard_ReturnsRight()
    {
        var router = CreateRouter();

        var result = await router.RouteWriteByTimestampAsync(
            new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc));

        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task Contract_RouteWriteByTimestampAsync_NonHotShard_ReturnsLeft()
    {
        var router = CreateRouter();

        var result = await router.RouteWriteByTimestampAsync(
            new DateTime(2025, 12, 15, 0, 0, 0, DateTimeKind.Utc));

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task Contract_GetShardsInRangeAsync_ValidRange_ReturnsRight()
    {
        var router = CreateRouter();

        var result = await router.GetShardsInRangeAsync(
            new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc));

        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public void Contract_GetShardTier_ValidShard_ReturnsRight()
    {
        var router = CreateRouter();

        var result = router.GetShardTier("orders-2026-01");

        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public void Contract_GetShardTierInfo_ValidShard_ReturnsRight()
    {
        var router = CreateRouter();

        var result = router.GetShardTierInfo("orders-2026-01");

        result.IsRight.ShouldBeTrue();
    }

    #endregion

    #region Error Code Contract

    [Fact]
    public void Contract_ErrorCodes_TimestampOutsideRange_IsDefinedInShardingErrorCodes()
    {
        var field = typeof(ShardingErrorCodes).GetField(nameof(ShardingErrorCodes.TimestampOutsideRange),
            BindingFlags.Public | BindingFlags.Static);

        field.ShouldNotBeNull();
        field.GetValue(null).ShouldBe("encina.sharding.timestamp_outside_range");
    }

    [Fact]
    public void Contract_ErrorCodes_ShardReadOnly_IsDefinedInShardingErrorCodes()
    {
        var field = typeof(ShardingErrorCodes).GetField(nameof(ShardingErrorCodes.ShardReadOnly),
            BindingFlags.Public | BindingFlags.Static);

        field.ShouldNotBeNull();
        field.GetValue(null).ShouldBe("encina.sharding.shard_read_only");
    }

    [Fact]
    public void Contract_ErrorCodes_NoTimeBasedShards_IsDefinedInShardingErrorCodes()
    {
        var field = typeof(ShardingErrorCodes).GetField(nameof(ShardingErrorCodes.NoTimeBasedShards),
            BindingFlags.Public | BindingFlags.Static);

        field.ShouldNotBeNull();
        field.GetValue(null).ShouldBe("encina.sharding.no_time_based_shards");
    }

    [Fact]
    public void Contract_ErrorCodes_TierTransitionFailed_IsDefinedInShardingErrorCodes()
    {
        var field = typeof(ShardingErrorCodes).GetField(nameof(ShardingErrorCodes.TierTransitionFailed),
            BindingFlags.Public | BindingFlags.Static);

        field.ShouldNotBeNull();
        field.GetValue(null).ShouldBe("encina.sharding.tier_transition_failed");
    }

    [Fact]
    public void Contract_ErrorCodes_PartialKeyRoutingFailed_IsDefinedInShardingErrorCodes()
    {
        var field = typeof(ShardingErrorCodes).GetField(nameof(ShardingErrorCodes.PartialKeyRoutingFailed),
            BindingFlags.Public | BindingFlags.Static);

        field.ShouldNotBeNull();
        field.GetValue(null).ShouldBe("encina.sharding.partial_key_routing_failed");
    }

    #endregion

    #region IShardRouter Method Signatures

    [Fact]
    public void Contract_IShardRouter_HasGetShardIdStringOverload()
    {
        var method = typeof(IShardRouter).GetMethod(
            nameof(IShardRouter.GetShardId),
            [typeof(string)]);

        method.ShouldNotBeNull();
    }

    [Fact]
    public void Contract_IShardRouter_HasGetShardIdCompoundKeyOverload()
    {
        var method = typeof(IShardRouter).GetMethod(
            nameof(IShardRouter.GetShardId),
            [typeof(CompoundShardKey)]);

        method.ShouldNotBeNull();
    }

    [Fact]
    public void Contract_IShardRouter_HasGetAllShardIds()
    {
        var method = typeof(IShardRouter).GetMethod(nameof(IShardRouter.GetAllShardIds));

        method.ShouldNotBeNull();
        method.ReturnType.ShouldBe(typeof(IReadOnlyList<string>));
    }

    [Fact]
    public void Contract_IShardRouter_HasGetShardConnectionString()
    {
        var method = typeof(IShardRouter).GetMethod(nameof(IShardRouter.GetShardConnectionString));

        method.ShouldNotBeNull();
    }

    #endregion
}
