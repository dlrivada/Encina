using Encina.Sharding;
using Encina.Sharding.Routing;
using Encina.Sharding.TimeBased;

namespace Encina.GuardTests.Core.Sharding;

/// <summary>
/// Guard clause tests for <see cref="TimeBasedShardRouter"/>.
/// Verifies null parameter handling, overlap validation, and routing edge cases.
/// </summary>
public sealed class TimeBasedShardRouterGuardTests
{
    #region Constructor Guards

    /// <summary>
    /// Verifies that the constructor throws when topology is null.
    /// </summary>
    [Fact]
    public void Constructor_NullTopology_ThrowsArgumentNullException()
    {
        var tierInfos = CreateSingleTierInfo();

        Should.Throw<ArgumentNullException>(() =>
            new TimeBasedShardRouter(null!, tierInfos, ShardPeriod.Monthly));
    }

    /// <summary>
    /// Verifies that the constructor throws when tierInfos is null.
    /// </summary>
    [Fact]
    public void Constructor_NullTierInfos_ThrowsArgumentNullException()
    {
        var topology = CreateTopology("shard-jan");

        Should.Throw<ArgumentNullException>(() =>
            new TimeBasedShardRouter(topology, null!, ShardPeriod.Monthly));
    }

    /// <summary>
    /// Verifies that the constructor throws when overlapping periods are provided.
    /// </summary>
    [Fact]
    public void Constructor_OverlappingPeriods_ThrowsArgumentException()
    {
        var topology = CreateTopology("shard-a", "shard-b");
        var tierInfos = new[]
        {
            new ShardTierInfo("shard-a", ShardTier.Hot,
                new DateOnly(2026, 1, 1), new DateOnly(2026, 3, 1),
                false, "Server=a;Database=a;", DateTime.UtcNow),
            new ShardTierInfo("shard-b", ShardTier.Hot,
                new DateOnly(2026, 2, 1), new DateOnly(2026, 4, 1),
                false, "Server=b;Database=b;", DateTime.UtcNow),
        };

        Should.Throw<ArgumentException>(() =>
            new TimeBasedShardRouter(topology, tierInfos, ShardPeriod.Monthly));
    }

    /// <summary>
    /// Verifies that the constructor succeeds with valid non-overlapping periods.
    /// </summary>
    [Fact]
    public void Constructor_ValidNonOverlappingPeriods_Succeeds()
    {
        var topology = CreateTopology("shard-jan", "shard-feb");
        var tierInfos = new[]
        {
            new ShardTierInfo("shard-jan", ShardTier.Hot,
                new DateOnly(2026, 1, 1), new DateOnly(2026, 2, 1),
                false, "Server=jan;Database=db;", DateTime.UtcNow),
            new ShardTierInfo("shard-feb", ShardTier.Warm,
                new DateOnly(2026, 2, 1), new DateOnly(2026, 3, 1),
                true, "Server=feb;Database=db;", DateTime.UtcNow),
        };

        var router = new TimeBasedShardRouter(topology, tierInfos, ShardPeriod.Monthly);

        router.Period.ShouldBe(ShardPeriod.Monthly);
        router.WeekStart.ShouldBe(DayOfWeek.Monday);
    }

    /// <summary>
    /// Verifies that the constructor stores the WeekStart parameter correctly.
    /// </summary>
    [Fact]
    public void Constructor_CustomWeekStart_IsPreserved()
    {
        var topology = CreateTopology("shard-jan");
        var tierInfos = CreateSingleTierInfo();

        var router = new TimeBasedShardRouter(topology, tierInfos, ShardPeriod.Weekly, DayOfWeek.Sunday);

        router.WeekStart.ShouldBe(DayOfWeek.Sunday);
    }

    /// <summary>
    /// Verifies that the constructor succeeds with empty tier infos (no shards).
    /// </summary>
    [Fact]
    public void Constructor_EmptyTierInfos_Succeeds()
    {
        var topology = new ShardTopology(Array.Empty<ShardInfo>());
        var tierInfos = Array.Empty<ShardTierInfo>();

        var router = new TimeBasedShardRouter(topology, tierInfos, ShardPeriod.Monthly);

        router.GetAllShardIds().Count.ShouldBe(0);
    }

    #endregion

    #region GetShardId Guards

    /// <summary>
    /// Verifies that GetShardId(string) throws when shard key is null.
    /// </summary>
    [Fact]
    public void GetShardId_NullShardKey_ThrowsArgumentNullException()
    {
        var router = CreateRouterWithSingleShard();

        Should.Throw<ArgumentNullException>(() =>
            router.GetShardId((string)null!));
    }

    /// <summary>
    /// Verifies that GetShardId(CompoundShardKey) throws when key is null.
    /// </summary>
    [Fact]
    public void GetShardId_NullCompoundKey_ThrowsArgumentNullException()
    {
        var router = CreateRouterWithSingleShard();

        Should.Throw<ArgumentNullException>(() =>
            router.GetShardId((CompoundShardKey)null!));
    }

    /// <summary>
    /// Verifies that GetShardId returns error when no shards are configured.
    /// </summary>
    [Fact]
    public void GetShardId_NoShardsConfigured_ReturnsLeftError()
    {
        var topology = new ShardTopology(Array.Empty<ShardInfo>());
        var router = new TimeBasedShardRouter(topology, [], ShardPeriod.Monthly);

        var result = router.GetShardId("2026-01-15");

        result.IsLeft.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that GetShardId returns error when key does not match any period.
    /// </summary>
    [Fact]
    public void GetShardId_KeyOutsideRange_ReturnsLeftError()
    {
        var router = CreateRouterWithSingleShard();

        var result = router.GetShardId("2099-12-31");

        result.IsLeft.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that GetShardId returns correct shard for matching key.
    /// </summary>
    [Fact]
    public void GetShardId_MatchingKey_ReturnsCorrectShard()
    {
        var router = CreateRouterWithSingleShard();

        var result = router.GetShardId("2026-01-15");

        result.IsRight.ShouldBeTrue();
        result.Match(Right: id => id, Left: _ => "").ShouldBe("shard-jan");
    }

    #endregion

    #region GetShardIds (CompoundShardKey prefix) Guards

    /// <summary>
    /// Verifies that GetShardIds throws when partial key is null.
    /// </summary>
    [Fact]
    public void GetShardIds_NullPartialKey_ThrowsArgumentNullException()
    {
        var router = CreateRouterWithSingleShard();

        Should.Throw<ArgumentNullException>(() =>
            router.GetShardIds(null!));
    }

    /// <summary>
    /// Verifies that GetShardIds returns error when no shards match prefix.
    /// </summary>
    [Fact]
    public void GetShardIds_NoMatchingPrefix_ReturnsLeftError()
    {
        var router = CreateRouterWithSingleShard();

        var result = router.GetShardIds(new CompoundShardKey("2099"));

        result.IsLeft.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that GetShardIds returns matching shards for a valid prefix.
    /// </summary>
    [Fact]
    public void GetShardIds_MatchingPrefix_ReturnsShards()
    {
        var router = CreateRouterWithSingleShard();

        var result = router.GetShardIds(new CompoundShardKey("2026"));

        result.IsRight.ShouldBeTrue();
    }

    #endregion

    #region GetShardTier Guards

    /// <summary>
    /// Verifies that GetShardTier throws when shard ID is null.
    /// </summary>
    [Fact]
    public void GetShardTier_NullShardId_ThrowsArgumentNullException()
    {
        var router = CreateRouterWithSingleShard();

        Should.Throw<ArgumentNullException>(() =>
            router.GetShardTier(null!));
    }

    /// <summary>
    /// Verifies that GetShardTier returns error for unknown shard.
    /// </summary>
    [Fact]
    public void GetShardTier_UnknownShardId_ReturnsLeftError()
    {
        var router = CreateRouterWithSingleShard();

        var result = router.GetShardTier("unknown-shard");

        result.IsLeft.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that GetShardTier returns correct tier for known shard.
    /// </summary>
    [Fact]
    public void GetShardTier_KnownShardId_ReturnsCorrectTier()
    {
        var router = CreateRouterWithSingleShard();

        var result = router.GetShardTier("shard-jan");

        result.IsRight.ShouldBeTrue();
        result.Match(Right: tier => tier, Left: _ => default).ShouldBe(ShardTier.Hot);
    }

    #endregion

    #region GetShardTierInfo Guards

    /// <summary>
    /// Verifies that GetShardTierInfo throws when shard ID is null.
    /// </summary>
    [Fact]
    public void GetShardTierInfo_NullShardId_ThrowsArgumentNullException()
    {
        var router = CreateRouterWithSingleShard();

        Should.Throw<ArgumentNullException>(() =>
            router.GetShardTierInfo(null!));
    }

    /// <summary>
    /// Verifies that GetShardTierInfo returns error for unknown shard.
    /// </summary>
    [Fact]
    public void GetShardTierInfo_UnknownShardId_ReturnsLeftError()
    {
        var router = CreateRouterWithSingleShard();

        var result = router.GetShardTierInfo("unknown-shard");

        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region GetShardConnectionString Guards

    /// <summary>
    /// Verifies that GetShardConnectionString throws when shard ID is null.
    /// </summary>
    [Fact]
    public void GetShardConnectionString_NullShardId_ThrowsArgumentNullException()
    {
        var router = CreateRouterWithSingleShard();

        Should.Throw<ArgumentNullException>(() =>
            router.GetShardConnectionString(null!));
    }

    /// <summary>
    /// Verifies that GetShardConnectionString returns correct string for known shard.
    /// </summary>
    [Fact]
    public void GetShardConnectionString_KnownShard_ReturnsConnectionString()
    {
        var router = CreateRouterWithSingleShard();

        var result = router.GetShardConnectionString("shard-jan");

        result.IsRight.ShouldBeTrue();
    }

    #endregion

    #region GetColocationGroup Guards

    /// <summary>
    /// Verifies that GetColocationGroup throws when entity type is null.
    /// </summary>
    [Fact]
    public void GetColocationGroup_NullEntityType_ThrowsArgumentNullException()
    {
        var router = CreateRouterWithSingleShard();

        Should.Throw<ArgumentNullException>(() =>
            router.GetColocationGroup(null!));
    }

    /// <summary>
    /// Verifies that GetColocationGroup returns null when no registry is configured.
    /// </summary>
    [Fact]
    public void GetColocationGroup_NoRegistry_ReturnsNull()
    {
        var router = CreateRouterWithSingleShard();

        var result = router.GetColocationGroup(typeof(string));

        result.ShouldBeNull();
    }

    #endregion

    #region RouteByTimestampAsync Guards

    /// <summary>
    /// Verifies that RouteByTimestampAsync returns error for empty router.
    /// </summary>
    [Fact]
    public async Task RouteByTimestampAsync_EmptyRouter_ReturnsLeftError()
    {
        var topology = new ShardTopology(Array.Empty<ShardInfo>());
        var router = new TimeBasedShardRouter(topology, [], ShardPeriod.Monthly);

        var result = await router.RouteByTimestampAsync(DateTime.UtcNow);

        result.IsLeft.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that RouteByTimestampAsync returns correct shard for matching timestamp.
    /// </summary>
    [Fact]
    public async Task RouteByTimestampAsync_MatchingTimestamp_ReturnsShardId()
    {
        var router = CreateRouterWithSingleShard();

        var result = await router.RouteByTimestampAsync(new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc));

        result.IsRight.ShouldBeTrue();
    }

    #endregion

    #region RouteWriteByTimestampAsync Guards

    /// <summary>
    /// Verifies that write route returns error when targeting a read-only shard.
    /// </summary>
    [Fact]
    public async Task RouteWriteByTimestampAsync_ReadOnlyShard_ReturnsLeftError()
    {
        var topology = CreateTopology("shard-warm");
        var tierInfos = new[]
        {
            new ShardTierInfo("shard-warm", ShardTier.Warm,
                new DateOnly(2026, 1, 1), new DateOnly(2026, 2, 1),
                true, "Server=warm;Database=db;", DateTime.UtcNow),
        };
        var router = new TimeBasedShardRouter(topology, tierInfos, ShardPeriod.Monthly);

        var result = await router.RouteWriteByTimestampAsync(new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc));

        result.IsLeft.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that write route succeeds when targeting a Hot shard.
    /// </summary>
    [Fact]
    public async Task RouteWriteByTimestampAsync_HotShard_ReturnsShardId()
    {
        var router = CreateRouterWithSingleShard();

        var result = await router.RouteWriteByTimestampAsync(new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc));

        result.IsRight.ShouldBeTrue();
    }

    #endregion

    #region GetShardsInRangeAsync Guards

    /// <summary>
    /// Verifies that GetShardsInRangeAsync returns error when no shards configured.
    /// </summary>
    [Fact]
    public async Task GetShardsInRangeAsync_EmptyRouter_ReturnsLeftError()
    {
        var topology = new ShardTopology(Array.Empty<ShardInfo>());
        var router = new TimeBasedShardRouter(topology, [], ShardPeriod.Monthly);

        var result = await router.GetShardsInRangeAsync(DateTime.UtcNow, DateTime.UtcNow.AddDays(30));

        result.IsLeft.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that GetShardsInRangeAsync returns error when range does not match any shard.
    /// </summary>
    [Fact]
    public async Task GetShardsInRangeAsync_NoMatchingRange_ReturnsLeftError()
    {
        var router = CreateRouterWithSingleShard();

        var result = await router.GetShardsInRangeAsync(
            new DateTime(2099, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2099, 2, 1, 0, 0, 0, DateTimeKind.Utc));

        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region Test Helpers

    private static ShardTierInfo[] CreateSingleTierInfo() =>
    [
        new ShardTierInfo("shard-jan", ShardTier.Hot,
            new DateOnly(2026, 1, 1), new DateOnly(2026, 2, 1),
            false, "Server=jan;Database=db;", DateTime.UtcNow),
    ];

    private static ShardTopology CreateTopology(params string[] shardIds)
    {
        var shards = shardIds.Select(id => new ShardInfo(id, $"Server={id};Database=db;")).ToList();
        return new ShardTopology(shards);
    }

    private static TimeBasedShardRouter CreateRouterWithSingleShard()
    {
        var topology = CreateTopology("shard-jan");
        var tierInfos = CreateSingleTierInfo();
        return new TimeBasedShardRouter(topology, tierInfos, ShardPeriod.Monthly);
    }

    #endregion
}
