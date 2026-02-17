using Encina.Sharding;
using Encina.Sharding.Routing;
using Encina.Sharding.TimeBased;
using LanguageExt;

namespace Encina.UnitTests.Sharding.TimeBased;

/// <summary>
/// Unit tests for <see cref="TimeBasedShardRouter"/>.
/// Verifies routing by timestamp, binary search, tier-aware writes, range queries,
/// and scatter-gather prefix matching.
/// </summary>
public sealed class TimeBasedShardRouterTests
{
    #region Test Helpers

    private static ShardTierInfo CreateTierInfo(
        string shardId,
        ShardTier tier,
        DateOnly periodStart,
        DateOnly periodEnd,
        bool isReadOnly = false,
        string? connectionString = null)
    {
        return new ShardTierInfo(
            shardId,
            tier,
            periodStart,
            periodEnd,
            isReadOnly,
            connectionString ?? $"Server=test;Database={shardId}",
            DateTime.UtcNow);
    }

    private static TimeBasedShardRouter CreateRouter(
        IEnumerable<ShardTierInfo> tierInfos,
        ShardPeriod period = ShardPeriod.Monthly,
        DayOfWeek weekStart = DayOfWeek.Monday,
        IShardFallbackCreator? fallbackCreator = null)
    {
        var tierList = tierInfos.ToList();
        var shardInfos = tierList.Select(t => new ShardInfo(t.ShardId, t.ConnectionString));
        var topology = new ShardTopology(shardInfos);
        return new TimeBasedShardRouter(topology, tierList, period, weekStart, fallbackCreator: fallbackCreator);
    }

    private static TimeBasedShardRouter CreateMonthlyRouter()
    {
        var tierInfos = new[]
        {
            CreateTierInfo("orders-2026-01", ShardTier.Hot,
                new DateOnly(2026, 1, 1), new DateOnly(2026, 2, 1)),
            CreateTierInfo("orders-2025-12", ShardTier.Warm,
                new DateOnly(2025, 12, 1), new DateOnly(2026, 1, 1), isReadOnly: true),
            CreateTierInfo("orders-2025-11", ShardTier.Cold,
                new DateOnly(2025, 11, 1), new DateOnly(2025, 12, 1), isReadOnly: true),
        };

        return CreateRouter(tierInfos);
    }

    private static string ExtractRight(Either<EncinaError, string> result)
    {
        string value = string.Empty;
        _ = result.IfRight(v => value = v);
        return value;
    }

    private static EncinaError ExtractLeft(Either<EncinaError, string> result)
    {
        EncinaError error = default;
        _ = result.IfLeft(e => error = e);
        return error;
    }

    #endregion

    #region Constructor

    [Fact]
    public void Constructor_NullTopology_ThrowsArgumentNullException()
    {
        var tierInfos = Array.Empty<ShardTierInfo>();

        Should.Throw<ArgumentNullException>(() =>
            new TimeBasedShardRouter(null!, tierInfos, ShardPeriod.Monthly));
    }

    [Fact]
    public void Constructor_NullTierInfos_ThrowsArgumentNullException()
    {
        var topology = new ShardTopology(Array.Empty<ShardInfo>());

        Should.Throw<ArgumentNullException>(() =>
            new TimeBasedShardRouter(topology, null!, ShardPeriod.Monthly));
    }

    [Fact]
    public void Constructor_OverlappingPeriods_ThrowsArgumentException()
    {
        var tierInfos = new[]
        {
            CreateTierInfo("shard-1", ShardTier.Hot,
                new DateOnly(2026, 1, 1), new DateOnly(2026, 3, 1)),
            CreateTierInfo("shard-2", ShardTier.Hot,
                new DateOnly(2026, 2, 1), new DateOnly(2026, 4, 1)),
        };

        var shardInfos = tierInfos.Select(t => new ShardInfo(t.ShardId, t.ConnectionString));
        var topology = new ShardTopology(shardInfos);

        Should.Throw<ArgumentException>(() =>
            new TimeBasedShardRouter(topology, tierInfos, ShardPeriod.Monthly));
    }

    [Fact]
    public void Constructor_ValidTierInfos_SetsPeriodAndWeekStart()
    {
        var tierInfos = new[]
        {
            CreateTierInfo("s1", ShardTier.Hot,
                new DateOnly(2026, 1, 1), new DateOnly(2026, 2, 1)),
        };

        var router = CreateRouter(tierInfos, ShardPeriod.Weekly, DayOfWeek.Sunday);

        router.Period.ShouldBe(ShardPeriod.Weekly);
        router.WeekStart.ShouldBe(DayOfWeek.Sunday);
    }

    #endregion

    #region RouteByTimestampAsync

    [Fact]
    public async Task RouteByTimestampAsync_TimestampInHotShard_ReturnsShardId()
    {
        var router = CreateMonthlyRouter();

        var result = await router.RouteByTimestampAsync(
            new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc));

        result.IsRight.ShouldBeTrue();
        ExtractRight(result).ShouldBe("orders-2026-01");
    }

    [Fact]
    public async Task RouteByTimestampAsync_TimestampInWarmShard_ReturnsShardId()
    {
        var router = CreateMonthlyRouter();

        var result = await router.RouteByTimestampAsync(
            new DateTime(2025, 12, 15, 0, 0, 0, DateTimeKind.Utc));

        result.IsRight.ShouldBeTrue();
        ExtractRight(result).ShouldBe("orders-2025-12");
    }

    [Fact]
    public async Task RouteByTimestampAsync_TimestampOutsideAllPeriods_ReturnsLeft()
    {
        var router = CreateMonthlyRouter();

        var result = await router.RouteByTimestampAsync(
            new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        result.IsLeft.ShouldBeTrue();
        ExtractLeft(result).GetCode().IfNone(string.Empty)
            .ShouldBe(ShardingErrorCodes.TimestampOutsideRange);
    }

    [Fact]
    public async Task RouteByTimestampAsync_NoShards_ReturnsNoTimeBasedShards()
    {
        var router = CreateRouter(Array.Empty<ShardTierInfo>());

        var result = await router.RouteByTimestampAsync(
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        result.IsLeft.ShouldBeTrue();
        ExtractLeft(result).GetCode().IfNone(string.Empty)
            .ShouldBe(ShardingErrorCodes.NoTimeBasedShards);
    }

    [Fact]
    public async Task RouteByTimestampAsync_PeriodStartBoundary_RoutesToShard()
    {
        var router = CreateMonthlyRouter();

        var result = await router.RouteByTimestampAsync(
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        result.IsRight.ShouldBeTrue();
        ExtractRight(result).ShouldBe("orders-2026-01");
    }

    [Fact]
    public async Task RouteByTimestampAsync_PeriodEndBoundary_RoutesToNextShard()
    {
        // The end of 2025-12 period is 2026-01-01, which belongs to the next period
        var router = CreateMonthlyRouter();

        var result = await router.RouteByTimestampAsync(
            new DateTime(2025, 11, 30, 23, 59, 59, DateTimeKind.Utc));

        result.IsRight.ShouldBeTrue();
        ExtractRight(result).ShouldBe("orders-2025-11");
    }

    #endregion

    #region RouteWriteByTimestampAsync

    [Fact]
    public async Task RouteWriteByTimestampAsync_HotShard_ReturnsShardId()
    {
        var router = CreateMonthlyRouter();

        var result = await router.RouteWriteByTimestampAsync(
            new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc));

        result.IsRight.ShouldBeTrue();
        ExtractRight(result).ShouldBe("orders-2026-01");
    }

    [Fact]
    public async Task RouteWriteByTimestampAsync_WarmShard_ReturnsShardReadOnly()
    {
        var router = CreateMonthlyRouter();

        var result = await router.RouteWriteByTimestampAsync(
            new DateTime(2025, 12, 15, 0, 0, 0, DateTimeKind.Utc));

        result.IsLeft.ShouldBeTrue();
        ExtractLeft(result).GetCode().IfNone(string.Empty)
            .ShouldBe(ShardingErrorCodes.ShardReadOnly);
    }

    [Fact]
    public async Task RouteWriteByTimestampAsync_ColdShard_ReturnsShardReadOnly()
    {
        var router = CreateMonthlyRouter();

        var result = await router.RouteWriteByTimestampAsync(
            new DateTime(2025, 11, 15, 0, 0, 0, DateTimeKind.Utc));

        result.IsLeft.ShouldBeTrue();
        ExtractLeft(result).GetCode().IfNone(string.Empty)
            .ShouldBe(ShardingErrorCodes.ShardReadOnly);
    }

    [Fact]
    public async Task RouteWriteByTimestampAsync_NoShards_ReturnsNoTimeBasedShards()
    {
        var router = CreateRouter(Array.Empty<ShardTierInfo>());

        var result = await router.RouteWriteByTimestampAsync(
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        result.IsLeft.ShouldBeTrue();
        ExtractLeft(result).GetCode().IfNone(string.Empty)
            .ShouldBe(ShardingErrorCodes.NoTimeBasedShards);
    }

    #endregion

    #region GetShardsInRangeAsync

    [Fact]
    public async Task GetShardsInRangeAsync_FullOverlap_ReturnsAllShards()
    {
        var router = CreateMonthlyRouter();

        var result = await router.GetShardsInRangeAsync(
            new DateTime(2025, 11, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc));

        result.IsRight.ShouldBeTrue();
        IReadOnlyList<string> shards = [];
        _ = result.IfRight(s => shards = s);
        shards.Count.ShouldBe(3);
    }

    [Fact]
    public async Task GetShardsInRangeAsync_PartialOverlap_ReturnsOverlappingShards()
    {
        var router = CreateMonthlyRouter();

        var result = await router.GetShardsInRangeAsync(
            new DateTime(2025, 12, 15, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc));

        result.IsRight.ShouldBeTrue();
        IReadOnlyList<string> shards = [];
        _ = result.IfRight(s => shards = s);
        shards.Count.ShouldBe(2);
        shards.ShouldContain("orders-2025-12");
        shards.ShouldContain("orders-2026-01");
    }

    [Fact]
    public async Task GetShardsInRangeAsync_NoOverlap_ReturnsTimestampOutsideRange()
    {
        var router = CreateMonthlyRouter();

        var result = await router.GetShardsInRangeAsync(
            new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc));

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task GetShardsInRangeAsync_NoShards_ReturnsNoTimeBasedShards()
    {
        var router = CreateRouter(Array.Empty<ShardTierInfo>());

        var result = await router.GetShardsInRangeAsync(
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc));

        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region GetShardId

    [Fact]
    public void GetShardId_ValidKeyInRange_ReturnsShardId()
    {
        var router = CreateMonthlyRouter();

        var result = router.GetShardId("2026-01-15");

        result.IsRight.ShouldBeTrue();
        ExtractRight(result).ShouldBe("orders-2026-01");
    }

    [Fact]
    public void GetShardId_KeyOutsideRange_ReturnsTimestampOutsideRange()
    {
        var router = CreateMonthlyRouter();

        var result = router.GetShardId("2024-01-01");

        result.IsLeft.ShouldBeTrue();
        ExtractLeft(result).GetCode().IfNone(string.Empty)
            .ShouldBe(ShardingErrorCodes.TimestampOutsideRange);
    }

    [Fact]
    public void GetShardId_NullKey_ThrowsArgumentNullException()
    {
        var router = CreateMonthlyRouter();

        Should.Throw<ArgumentNullException>(() => router.GetShardId((string)null!));
    }

    [Fact]
    public void GetShardId_CompoundKey_UsesToString()
    {
        var router = CreateMonthlyRouter();
        var key = new CompoundShardKey("2026-01-15");

        var result = router.GetShardId(key);

        result.IsRight.ShouldBeTrue();
        ExtractRight(result).ShouldBe("orders-2026-01");
    }

    #endregion

    #region GetShardIds (Prefix Matching)

    [Fact]
    public void GetShardIds_YearPrefix_MatchesShardsInYear()
    {
        var router = CreateMonthlyRouter();

        var result = router.GetShardIds(new CompoundShardKey("2026"));

        result.IsRight.ShouldBeTrue();
        IReadOnlyList<string> shards = [];
        _ = result.IfRight(s => shards = s);
        shards.ShouldContain("orders-2026-01");
    }

    [Fact]
    public void GetShardIds_NoMatch_ReturnsPartialKeyRoutingFailed()
    {
        var router = CreateMonthlyRouter();

        var result = router.GetShardIds(new CompoundShardKey("2019"));

        result.IsLeft.ShouldBeTrue();
        EncinaError error = default;
        _ = result.IfLeft(e => error = e);
        error.GetCode().IfNone(string.Empty)
            .ShouldBe(ShardingErrorCodes.PartialKeyRoutingFailed);
    }

    [Fact]
    public void GetShardIds_ExactMonthPrefix_MatchesSingleShard()
    {
        var router = CreateMonthlyRouter();

        var result = router.GetShardIds(new CompoundShardKey("2025-12"));

        result.IsRight.ShouldBeTrue();
        IReadOnlyList<string> shards = [];
        _ = result.IfRight(s => shards = s);
        shards.ShouldContain("orders-2025-12");
    }

    #endregion

    #region GetAllShardIds

    [Fact]
    public void GetAllShardIds_ReturnsTopologyShardIds()
    {
        var router = CreateMonthlyRouter();

        var shardIds = router.GetAllShardIds();

        shardIds.Count.ShouldBe(3);
        shardIds.ShouldContain("orders-2026-01");
        shardIds.ShouldContain("orders-2025-12");
        shardIds.ShouldContain("orders-2025-11");
    }

    #endregion

    #region GetShardTier

    [Fact]
    public void GetShardTier_ExistingShard_ReturnsTier()
    {
        var router = CreateMonthlyRouter();

        var result = router.GetShardTier("orders-2026-01");

        result.IsRight.ShouldBeTrue();
        ShardTier tier = default;
        _ = result.IfRight(t => tier = t);
        tier.ShouldBe(ShardTier.Hot);
    }

    [Fact]
    public void GetShardTier_NonExistingShard_ReturnsShardNotFound()
    {
        var router = CreateMonthlyRouter();

        var result = router.GetShardTier("non-existent");

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void GetShardTier_NullShardId_ThrowsArgumentNullException()
    {
        var router = CreateMonthlyRouter();

        Should.Throw<ArgumentNullException>(() => router.GetShardTier(null!));
    }

    #endregion

    #region GetShardTierInfo

    [Fact]
    public void GetShardTierInfo_ExistingShard_ReturnsTierInfo()
    {
        var router = CreateMonthlyRouter();

        var result = router.GetShardTierInfo("orders-2025-12");

        result.IsRight.ShouldBeTrue();
        ShardTierInfo? info = null;
        _ = result.IfRight(i => info = i);
        info!.CurrentTier.ShouldBe(ShardTier.Warm);
        info.IsReadOnly.ShouldBeTrue();
    }

    [Fact]
    public void GetShardTierInfo_NonExistingShard_ReturnsShardNotFound()
    {
        var router = CreateMonthlyRouter();

        var result = router.GetShardTierInfo("unknown");

        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region GetShardConnectionString

    [Fact]
    public void GetShardConnectionString_ExistingShard_ReturnsConnectionString()
    {
        var router = CreateMonthlyRouter();

        var result = router.GetShardConnectionString("orders-2026-01");

        result.IsRight.ShouldBeTrue();
        ExtractRight(result).ShouldContain("orders-2026-01");
    }

    [Fact]
    public void GetShardConnectionString_NullShardId_ThrowsArgumentNullException()
    {
        var router = CreateMonthlyRouter();

        Should.Throw<ArgumentNullException>(() => router.GetShardConnectionString(null!));
    }

    #endregion

    #region Binary Search Correctness

    [Fact]
    public async Task BinarySearch_ManyShards_RoutesCorrectly()
    {
        // Create 12 monthly shards for all of 2026
        var tierInfos = Enumerable.Range(1, 12).Select(m =>
            CreateTierInfo(
                $"data-2026-{m:D2}",
                m == 12 ? ShardTier.Hot : ShardTier.Warm,
                new DateOnly(2026, m, 1),
                m == 12 ? new DateOnly(2027, 1, 1) : new DateOnly(2026, m + 1, 1),
                isReadOnly: m != 12)).ToArray();

        var router = CreateRouter(tierInfos);

        // Route to each month
        for (var month = 1; month <= 12; month++)
        {
            var result = await router.RouteByTimestampAsync(
                new DateTime(2026, month, 15, 0, 0, 0, DateTimeKind.Utc));

            result.IsRight.ShouldBeTrue($"Failed for month {month}");
            ExtractRight(result).ShouldBe($"data-2026-{month:D2}");
        }
    }

    [Fact]
    public void BinarySearch_FirstEntry_MatchesCorrectly()
    {
        var router = CreateMonthlyRouter();

        // Orders-2025-11 has the earliest start key
        var result = router.GetShardId("2025-11-01");

        result.IsRight.ShouldBeTrue();
        ExtractRight(result).ShouldBe("orders-2025-11");
    }

    [Fact]
    public void BinarySearch_LastEntry_MatchesCorrectly()
    {
        var router = CreateMonthlyRouter();

        // Orders-2026-01 has the latest start key
        var result = router.GetShardId("2026-01-31");

        result.IsRight.ShouldBeTrue();
        ExtractRight(result).ShouldBe("orders-2026-01");
    }

    #endregion

    #region Fallback Creator

    [Fact]
    public async Task RouteByTimestampAsync_WithFallbackCreator_TimestampOutsideRange_CallsFallback()
    {
        var fallbackCreator = Substitute.For<IShardFallbackCreator>();
        var createdTierInfo = CreateTierInfo("orders-2026-02", ShardTier.Hot,
            new DateOnly(2026, 2, 1), new DateOnly(2026, 3, 1));

        fallbackCreator.CreateShardForTimestampAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Either<EncinaError, ShardTierInfo>.Right(createdTierInfo));

        var router = CreateRouter(
            new[]
            {
                CreateTierInfo("orders-2026-01", ShardTier.Hot,
                    new DateOnly(2026, 1, 1), new DateOnly(2026, 2, 1)),
            },
            fallbackCreator: fallbackCreator);

        var result = await router.RouteByTimestampAsync(
            new DateTime(2026, 2, 15, 0, 0, 0, DateTimeKind.Utc));

        result.IsRight.ShouldBeTrue();
        ExtractRight(result).ShouldBe("orders-2026-02");
        await fallbackCreator.Received(1)
            .CreateShardForTimestampAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RouteByTimestampAsync_WithFallbackCreator_InRange_DoesNotCallFallback()
    {
        var fallbackCreator = Substitute.For<IShardFallbackCreator>();
        var router = CreateRouter(
            new[]
            {
                CreateTierInfo("orders-2026-01", ShardTier.Hot,
                    new DateOnly(2026, 1, 1), new DateOnly(2026, 2, 1)),
            },
            fallbackCreator: fallbackCreator);

        await router.RouteByTimestampAsync(
            new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc));

        await fallbackCreator.DidNotReceive()
            .CreateShardForTimestampAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region GetColocationGroup

    [Fact]
    public void GetColocationGroup_NoRegistry_ReturnsNull()
    {
        var router = CreateMonthlyRouter();

        var group = router.GetColocationGroup(typeof(string));

        group.ShouldBeNull();
    }

    #endregion
}
