using Encina.Sharding;
using Encina.Sharding.Routing;
using Encina.Sharding.TimeBased;

using FsCheck;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Database.Sharding;

/// <summary>
/// Property-based tests for time-based sharding invariants.
/// Verifies determinism, boundary adjacency, period coverage, range completeness,
/// and tier transition monotonicity.
/// </summary>
[Trait("Category", "Property")]
public sealed class TimeBasedShardingPropertyTests
{
    #region Test Helpers

    private static TimeBasedShardRouter CreateMonthlyRouter(int months = 12, int startYear = 2026)
    {
        var tierInfos = Enumerable.Range(0, months).Select(i =>
        {
            var start = new DateOnly(startYear, 1, 1).AddMonths(i);
            var end = start.AddMonths(1);
            var tier = i == months - 1 ? ShardTier.Hot : ShardTier.Warm;
            return new ShardTierInfo(
                $"data-{start:yyyy-MM}",
                tier,
                start,
                end,
                tier != ShardTier.Hot,
                $"Server=test;Database=data_{start:yyyy_MM}",
                DateTime.UtcNow);
        }).ToArray();

        var shardInfos = tierInfos.Select(t => new ShardInfo(t.ShardId, t.ConnectionString));
        var topology = new ShardTopology(shardInfos);
        return new TimeBasedShardRouter(topology, tierInfos, ShardPeriod.Monthly);
    }

    #endregion

    #region Determinism

    [Property(MaxTest = 200)]
    public bool Property_RouteByTimestamp_SameTimestampAlwaysReturnsSameShard(int dayOffset)
    {
        var clampedDay = Math.Clamp(dayOffset, 0, 364);
        var timestamp = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc).AddDays(clampedDay);
        var router = CreateMonthlyRouter();

        var result1 = router.RouteByTimestampAsync(timestamp).GetAwaiter().GetResult();
        var result2 = router.RouteByTimestampAsync(timestamp).GetAwaiter().GetResult();

        return result1.IsRight && result2.IsRight && result1 == result2;
    }

    [Property(MaxTest = 100)]
    public bool Property_RouteByTimestamp_DeterministicAcrossInstances(int dayOffset)
    {
        var clampedDay = Math.Clamp(dayOffset, 0, 364);
        var timestamp = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc).AddDays(clampedDay);

        var router1 = CreateMonthlyRouter();
        var router2 = CreateMonthlyRouter();

        var result1 = router1.RouteByTimestampAsync(timestamp).GetAwaiter().GetResult();
        var result2 = router2.RouteByTimestampAsync(timestamp).GetAwaiter().GetResult();

        return result1.IsRight && result2.IsRight && result1 == result2;
    }

    #endregion

    #region Boundary Adjacency

    [Property(MaxTest = 100)]
    public bool Property_PeriodBoundaries_AreAdjacent_NoGaps(int monthOffset)
    {
        var clampedMonth = Math.Clamp(monthOffset, 0, 11);
        var date = new DateOnly(2026, 1, 1).AddMonths(clampedMonth);

        var (_, end1) = PeriodBoundaryCalculator.GetPeriodBoundaries(date, ShardPeriod.Monthly);
        var (start2, _) = PeriodBoundaryCalculator.GetPeriodBoundaries(end1, ShardPeriod.Monthly);

        // The end of one period should be the start of the next
        return end1 == start2;
    }

    [Property(MaxTest = 200)]
    public bool Property_PeriodBoundaries_Daily_AreAdjacent(int dayOffset)
    {
        var clampedDay = Math.Clamp(dayOffset, 0, 364);
        var date = new DateOnly(2026, 1, 1).AddDays(clampedDay);

        var (_, end1) = PeriodBoundaryCalculator.GetPeriodBoundaries(date, ShardPeriod.Daily);
        var (start2, _) = PeriodBoundaryCalculator.GetPeriodBoundaries(end1, ShardPeriod.Daily);

        return end1 == start2;
    }

    #endregion

    #region All Timestamps in Period Map to Same Shard

    [Property(MaxTest = 100)]
    public bool Property_AllTimestampsInPeriod_MapToSameShard(int monthOffset)
    {
        var clampedMonth = Math.Clamp(monthOffset, 0, 11);
        var router = CreateMonthlyRouter();

        var periodStart = new DateOnly(2026, 1, 1).AddMonths(clampedMonth);
        var periodEnd = periodStart.AddMonths(1);

        // Pick start, middle, and day-before-end timestamps
        var timestamps = new[]
        {
            periodStart.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            periodStart.AddDays(14).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            periodEnd.AddDays(-1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
        };

        var shardIds = timestamps
            .Select(t => router.RouteByTimestampAsync(t).GetAwaiter().GetResult())
            .Where(r => r.IsRight)
            .Select(r =>
            {
                string id = string.Empty;
                _ = r.IfRight(v => id = v);
                return id;
            })
            .Distinct()
            .ToList();

        return shardIds.Count == 1;
    }

    #endregion

    #region GetShardsInRange Coverage

    [Fact]
    [Trait("Category", "Property")]
    public void Property_GetShardsInRange_FullCoverage_ReturnsAllShards()
    {
        var router = CreateMonthlyRouter(12);

        var result = router.GetShardsInRangeAsync(
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc))
            .GetAwaiter().GetResult();

        result.IsRight.ShouldBeTrue();
        IReadOnlyList<string> shards = [];
        _ = result.IfRight(s => shards = s);
        shards.Count.ShouldBe(12);
    }

    [Property(MaxTest = 50)]
    public bool Property_GetShardsInRange_SubsetAlwaysSmaller(int startMonth, int rangeMonths)
    {
        var clampedStart = Math.Clamp(startMonth, 0, 11);
        var clampedRange = Math.Clamp(rangeMonths, 1, 12 - clampedStart);

        var router = CreateMonthlyRouter(12);

        var from = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(clampedStart);
        var to = from.AddMonths(clampedRange);

        var subsetResult = router.GetShardsInRangeAsync(from, to).GetAwaiter().GetResult();
        var fullResult = router.GetShardsInRangeAsync(
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc)).GetAwaiter().GetResult();

        if (!subsetResult.IsRight || !fullResult.IsRight) return false;

        IReadOnlyList<string> subset = [];
        IReadOnlyList<string> full = [];
        _ = subsetResult.IfRight(s => subset = s);
        _ = fullResult.IfRight(s => full = s);

        return subset.Count <= full.Count;
    }

    #endregion

    #region Tier Transitions Are Monotonic

    [Fact]
    [Trait("Category", "Property")]
    public void Property_TierTransition_NeverGoesBackwards()
    {
        // Valid transitions: Hot -> Warm -> Cold -> Archived
        var validTransitions = new (ShardTier From, ShardTier To)[]
        {
            (ShardTier.Hot, ShardTier.Warm),
            (ShardTier.Warm, ShardTier.Cold),
            (ShardTier.Cold, ShardTier.Archived),
            (ShardTier.Hot, ShardTier.Cold),      // Skip
            (ShardTier.Hot, ShardTier.Archived),   // Skip
            (ShardTier.Warm, ShardTier.Archived),  // Skip
        };

        foreach (var (from, to) in validTransitions)
        {
            ((int)to > (int)from).ShouldBeTrue(
                $"Transition {from} -> {to} should be forward");
        }

        // Invalid backwards transitions
        var invalidTransitions = new (ShardTier From, ShardTier To)[]
        {
            (ShardTier.Warm, ShardTier.Hot),
            (ShardTier.Cold, ShardTier.Warm),
            (ShardTier.Archived, ShardTier.Cold),
            (ShardTier.Archived, ShardTier.Hot),
        };

        foreach (var (from, to) in invalidTransitions)
        {
            Should.Throw<ArgumentException>(() =>
                new TierTransition(from, to, TimeSpan.FromDays(30)));
        }
    }

    #endregion

    #region Label Determinism

    [Property(MaxTest = 200)]
    public bool Property_PeriodLabel_IsDeterministic(int dayOffset)
    {
        var clampedDay = Math.Clamp(dayOffset, 0, 730);
        var date = new DateOnly(2025, 1, 1).AddDays(clampedDay);

        var label1 = PeriodBoundaryCalculator.GetPeriodLabel(date, ShardPeriod.Monthly);
        var label2 = PeriodBoundaryCalculator.GetPeriodLabel(date, ShardPeriod.Monthly);

        return label1 == label2;
    }

    [Property(MaxTest = 200)]
    public bool Property_PeriodLabel_AllDatesInPeriod_ProduceSameLabel(int dayOffset)
    {
        var clampedDay = Math.Clamp(dayOffset, 0, 364);
        var date = new DateOnly(2026, 1, 1).AddDays(clampedDay);

        var label = PeriodBoundaryCalculator.GetPeriodLabel(date, ShardPeriod.Monthly);

        // Get another date in the same period
        var start = PeriodBoundaryCalculator.GetPeriodStart(date, ShardPeriod.Monthly);
        var labelFromStart = PeriodBoundaryCalculator.GetPeriodLabel(start, ShardPeriod.Monthly);

        return label == labelFromStart;
    }

    #endregion

    #region EnumeratePeriods Completeness

    [Property(MaxTest = 50)]
    public bool Property_EnumeratePeriods_PeriodsAreContiguous(int rangeMonths)
    {
        var clampedRange = Math.Clamp(rangeMonths, 1, 24);
        var from = new DateOnly(2026, 1, 1);
        var to = from.AddMonths(clampedRange);

        var periods = PeriodBoundaryCalculator.EnumeratePeriods(from, to, ShardPeriod.Monthly).ToList();

        if (periods.Count < 2) return true;

        for (var i = 0; i < periods.Count - 1; i++)
        {
            if (periods[i].End != periods[i + 1].Start)
            {
                return false;
            }
        }

        return true;
    }

    [Property(MaxTest = 50)]
    public bool Property_EnumeratePeriods_FirstPeriodContainsFrom(int dayOffset)
    {
        var clampedDay = Math.Clamp(dayOffset, 0, 364);
        var from = new DateOnly(2026, 1, 1).AddDays(clampedDay);
        var to = from.AddMonths(3);

        var periods = PeriodBoundaryCalculator.EnumeratePeriods(from, to, ShardPeriod.Monthly).ToList();

        if (periods.Count == 0) return true;

        return periods[0].Start <= from && periods[0].End > from;
    }

    #endregion
}
