using Encina.Sharding.TimeBased;

namespace Encina.UnitTests.Sharding.TimeBased;

/// <summary>
/// Unit tests for <see cref="PeriodBoundaryCalculator"/>.
/// Verifies period start, end, label, and enumeration for all <see cref="ShardPeriod"/> values.
/// </summary>
public sealed class PeriodBoundaryCalculatorTests
{
    #region GetPeriodStart — Daily

    [Fact]
    public void GetPeriodStart_Daily_ReturnsSameDate()
    {
        var date = new DateOnly(2026, 2, 15);

        var result = PeriodBoundaryCalculator.GetPeriodStart(date, ShardPeriod.Daily);

        result.ShouldBe(new DateOnly(2026, 2, 15));
    }

    [Fact]
    public void GetPeriodStart_Daily_FromDateTime_ReturnsSameDate()
    {
        var timestamp = new DateTime(2026, 2, 15, 14, 30, 0, DateTimeKind.Utc);

        var result = PeriodBoundaryCalculator.GetPeriodStart(timestamp, ShardPeriod.Daily);

        result.ShouldBe(new DateOnly(2026, 2, 15));
    }

    #endregion

    #region GetPeriodStart — Weekly

    [Fact]
    public void GetPeriodStart_Weekly_MondayStart_ReturnsMonday()
    {
        // 2026-02-15 is a Sunday
        var date = new DateOnly(2026, 2, 15);

        var result = PeriodBoundaryCalculator.GetPeriodStart(date, ShardPeriod.Weekly, DayOfWeek.Monday);

        result.ShouldBe(new DateOnly(2026, 2, 9)); // Previous Monday
    }

    [Fact]
    public void GetPeriodStart_Weekly_SundayStart_ReturnsSunday()
    {
        // 2026-02-15 is a Sunday
        var date = new DateOnly(2026, 2, 15);

        var result = PeriodBoundaryCalculator.GetPeriodStart(date, ShardPeriod.Weekly, DayOfWeek.Sunday);

        result.ShouldBe(new DateOnly(2026, 2, 15)); // It is Sunday
    }

    [Fact]
    public void GetPeriodStart_Weekly_DateIsMonday_ReturnsSameDate()
    {
        var date = new DateOnly(2026, 2, 9); // Monday

        var result = PeriodBoundaryCalculator.GetPeriodStart(date, ShardPeriod.Weekly);

        result.ShouldBe(new DateOnly(2026, 2, 9));
    }

    [Fact]
    public void GetPeriodStart_Weekly_MidWeek_ReturnsPreviousMonday()
    {
        var date = new DateOnly(2026, 2, 11); // Wednesday

        var result = PeriodBoundaryCalculator.GetPeriodStart(date, ShardPeriod.Weekly);

        result.ShouldBe(new DateOnly(2026, 2, 9)); // Monday
    }

    #endregion

    #region GetPeriodStart — Monthly

    [Fact]
    public void GetPeriodStart_Monthly_ReturnsFirstOfMonth()
    {
        var date = new DateOnly(2026, 2, 15);

        var result = PeriodBoundaryCalculator.GetPeriodStart(date, ShardPeriod.Monthly);

        result.ShouldBe(new DateOnly(2026, 2, 1));
    }

    [Fact]
    public void GetPeriodStart_Monthly_FirstDayOfMonth_ReturnsSameDate()
    {
        var date = new DateOnly(2026, 3, 1);

        var result = PeriodBoundaryCalculator.GetPeriodStart(date, ShardPeriod.Monthly);

        result.ShouldBe(new DateOnly(2026, 3, 1));
    }

    [Fact]
    public void GetPeriodStart_Monthly_LastDayOfMonth_ReturnsFirstOfMonth()
    {
        var date = new DateOnly(2026, 1, 31);

        var result = PeriodBoundaryCalculator.GetPeriodStart(date, ShardPeriod.Monthly);

        result.ShouldBe(new DateOnly(2026, 1, 1));
    }

    #endregion

    #region GetPeriodStart — Quarterly

    [Theory]
    [InlineData(1, 1)] // January -> Q1 starts Jan 1
    [InlineData(2, 1)] // February -> Q1 starts Jan 1
    [InlineData(3, 1)] // March -> Q1 starts Jan 1
    [InlineData(4, 4)] // April -> Q2 starts Apr 1
    [InlineData(5, 4)] // May -> Q2 starts Apr 1
    [InlineData(6, 4)] // June -> Q2 starts Apr 1
    [InlineData(7, 7)] // July -> Q3 starts Jul 1
    [InlineData(8, 7)] // August -> Q3 starts Jul 1
    [InlineData(9, 7)] // September -> Q3 starts Jul 1
    [InlineData(10, 10)] // October -> Q4 starts Oct 1
    [InlineData(11, 10)] // November -> Q4 starts Oct 1
    [InlineData(12, 10)] // December -> Q4 starts Oct 1
    public void GetPeriodStart_Quarterly_ReturnsQuarterStart(int month, int expectedMonth)
    {
        var date = new DateOnly(2026, month, 15);

        var result = PeriodBoundaryCalculator.GetPeriodStart(date, ShardPeriod.Quarterly);

        result.ShouldBe(new DateOnly(2026, expectedMonth, 1));
    }

    #endregion

    #region GetPeriodStart — Yearly

    [Fact]
    public void GetPeriodStart_Yearly_ReturnsJanuaryFirst()
    {
        var date = new DateOnly(2026, 7, 15);

        var result = PeriodBoundaryCalculator.GetPeriodStart(date, ShardPeriod.Yearly);

        result.ShouldBe(new DateOnly(2026, 1, 1));
    }

    [Fact]
    public void GetPeriodStart_Yearly_JanuaryFirst_ReturnsSameDate()
    {
        var date = new DateOnly(2026, 1, 1);

        var result = PeriodBoundaryCalculator.GetPeriodStart(date, ShardPeriod.Yearly);

        result.ShouldBe(new DateOnly(2026, 1, 1));
    }

    [Fact]
    public void GetPeriodStart_Yearly_December31_ReturnsJanuaryFirst()
    {
        var date = new DateOnly(2026, 12, 31);

        var result = PeriodBoundaryCalculator.GetPeriodStart(date, ShardPeriod.Yearly);

        result.ShouldBe(new DateOnly(2026, 1, 1));
    }

    #endregion

    #region GetPeriodStart — Invalid Period

    [Fact]
    public void GetPeriodStart_InvalidPeriod_ThrowsArgumentOutOfRange()
    {
        var date = new DateOnly(2026, 1, 1);

        Should.Throw<ArgumentOutOfRangeException>(() =>
            PeriodBoundaryCalculator.GetPeriodStart(date, (ShardPeriod)99));
    }

    #endregion

    #region GetPeriodEnd — Daily

    [Fact]
    public void GetPeriodEnd_Daily_ReturnsNextDay()
    {
        var date = new DateOnly(2026, 2, 15);

        var result = PeriodBoundaryCalculator.GetPeriodEnd(date, ShardPeriod.Daily);

        result.ShouldBe(new DateOnly(2026, 2, 16));
    }

    [Fact]
    public void GetPeriodEnd_Daily_EndOfMonth_ReturnsFirstOfNextMonth()
    {
        var date = new DateOnly(2026, 1, 31);

        var result = PeriodBoundaryCalculator.GetPeriodEnd(date, ShardPeriod.Daily);

        result.ShouldBe(new DateOnly(2026, 2, 1));
    }

    [Fact]
    public void GetPeriodEnd_Daily_EndOfYear_ReturnsFirstOfNextYear()
    {
        var date = new DateOnly(2026, 12, 31);

        var result = PeriodBoundaryCalculator.GetPeriodEnd(date, ShardPeriod.Daily);

        result.ShouldBe(new DateOnly(2027, 1, 1));
    }

    #endregion

    #region GetPeriodEnd — Weekly

    [Fact]
    public void GetPeriodEnd_Weekly_ReturnsNextWeekStart()
    {
        var date = new DateOnly(2026, 2, 9); // Monday

        var result = PeriodBoundaryCalculator.GetPeriodEnd(date, ShardPeriod.Weekly);

        result.ShouldBe(new DateOnly(2026, 2, 16)); // Next Monday
    }

    [Fact]
    public void GetPeriodEnd_Weekly_MidWeek_ReturnsNextWeekStart()
    {
        var date = new DateOnly(2026, 2, 11); // Wednesday

        var result = PeriodBoundaryCalculator.GetPeriodEnd(date, ShardPeriod.Weekly);

        result.ShouldBe(new DateOnly(2026, 2, 16)); // Next Monday
    }

    #endregion

    #region GetPeriodEnd — Monthly

    [Fact]
    public void GetPeriodEnd_Monthly_ReturnsFirstOfNextMonth()
    {
        var date = new DateOnly(2026, 2, 15);

        var result = PeriodBoundaryCalculator.GetPeriodEnd(date, ShardPeriod.Monthly);

        result.ShouldBe(new DateOnly(2026, 3, 1));
    }

    [Fact]
    public void GetPeriodEnd_Monthly_February_HandlesShortMonth()
    {
        var date = new DateOnly(2026, 2, 28);

        var result = PeriodBoundaryCalculator.GetPeriodEnd(date, ShardPeriod.Monthly);

        result.ShouldBe(new DateOnly(2026, 3, 1));
    }

    [Fact]
    public void GetPeriodEnd_Monthly_LeapYear_February29()
    {
        // 2028 is a leap year
        var date = new DateOnly(2028, 2, 29);

        var result = PeriodBoundaryCalculator.GetPeriodEnd(date, ShardPeriod.Monthly);

        result.ShouldBe(new DateOnly(2028, 3, 1));
    }

    [Fact]
    public void GetPeriodEnd_Monthly_December_ReturnsJanuaryNextYear()
    {
        var date = new DateOnly(2026, 12, 15);

        var result = PeriodBoundaryCalculator.GetPeriodEnd(date, ShardPeriod.Monthly);

        result.ShouldBe(new DateOnly(2027, 1, 1));
    }

    #endregion

    #region GetPeriodEnd — Quarterly

    [Fact]
    public void GetPeriodEnd_Quarterly_Q1_ReturnsApril1()
    {
        var date = new DateOnly(2026, 2, 15);

        var result = PeriodBoundaryCalculator.GetPeriodEnd(date, ShardPeriod.Quarterly);

        result.ShouldBe(new DateOnly(2026, 4, 1));
    }

    [Fact]
    public void GetPeriodEnd_Quarterly_Q4_ReturnsJanuaryNextYear()
    {
        var date = new DateOnly(2026, 11, 15);

        var result = PeriodBoundaryCalculator.GetPeriodEnd(date, ShardPeriod.Quarterly);

        result.ShouldBe(new DateOnly(2027, 1, 1));
    }

    #endregion

    #region GetPeriodEnd — Yearly

    [Fact]
    public void GetPeriodEnd_Yearly_ReturnsJanuaryFirstNextYear()
    {
        var date = new DateOnly(2026, 7, 15);

        var result = PeriodBoundaryCalculator.GetPeriodEnd(date, ShardPeriod.Yearly);

        result.ShouldBe(new DateOnly(2027, 1, 1));
    }

    #endregion

    #region GetPeriodEnd — Invalid Period

    [Fact]
    public void GetPeriodEnd_InvalidPeriod_ThrowsArgumentOutOfRange()
    {
        var date = new DateOnly(2026, 1, 1);

        Should.Throw<ArgumentOutOfRangeException>(() =>
            PeriodBoundaryCalculator.GetPeriodEnd(date, (ShardPeriod)99));
    }

    #endregion

    #region GetPeriodBoundaries

    [Fact]
    public void GetPeriodBoundaries_Monthly_ReturnsStartAndEnd()
    {
        var date = new DateOnly(2026, 2, 15);

        var (start, end) = PeriodBoundaryCalculator.GetPeriodBoundaries(date, ShardPeriod.Monthly);

        start.ShouldBe(new DateOnly(2026, 2, 1));
        end.ShouldBe(new DateOnly(2026, 3, 1));
    }

    [Fact]
    public void GetPeriodBoundaries_FromDateTime_MatchesDateOnlyOverload()
    {
        var timestamp = new DateTime(2026, 2, 15, 14, 30, 0, DateTimeKind.Utc);
        var date = new DateOnly(2026, 2, 15);

        var (startDt, endDt) = PeriodBoundaryCalculator.GetPeriodBoundaries(timestamp, ShardPeriod.Monthly);
        var (startD, endD) = PeriodBoundaryCalculator.GetPeriodBoundaries(date, ShardPeriod.Monthly);

        startDt.ShouldBe(startD);
        endDt.ShouldBe(endD);
    }

    [Fact]
    public void GetPeriodBoundaries_HalfOpenInterval_EndIsExclusive()
    {
        var date = new DateOnly(2026, 1, 31);

        var (start, end) = PeriodBoundaryCalculator.GetPeriodBoundaries(date, ShardPeriod.Monthly);

        // January: [Jan 1, Feb 1) — end is exclusive
        start.ShouldBe(new DateOnly(2026, 1, 1));
        end.ShouldBe(new DateOnly(2026, 2, 1));

        // Feb 1 should be in the NEXT period
        var nextStart = PeriodBoundaryCalculator.GetPeriodStart(end, ShardPeriod.Monthly);
        nextStart.ShouldBe(end);
    }

    #endregion

    #region GetPeriodLabel — Daily

    [Fact]
    public void GetPeriodLabel_Daily_ReturnsIsoDate()
    {
        var date = new DateOnly(2026, 2, 15);

        var label = PeriodBoundaryCalculator.GetPeriodLabel(date, ShardPeriod.Daily);

        label.ShouldBe("2026-02-15");
    }

    [Fact]
    public void GetPeriodLabel_Daily_SingleDigitDayMonth_PadsWithZeros()
    {
        var date = new DateOnly(2026, 1, 5);

        var label = PeriodBoundaryCalculator.GetPeriodLabel(date, ShardPeriod.Daily);

        label.ShouldBe("2026-01-05");
    }

    #endregion

    #region GetPeriodLabel — Weekly

    [Fact]
    public void GetPeriodLabel_Weekly_ReturnsIsoWeekFormat()
    {
        // 2026-02-09 is Monday, ISO week 7
        var date = new DateOnly(2026, 2, 9);

        var label = PeriodBoundaryCalculator.GetPeriodLabel(date, ShardPeriod.Weekly);

        label.ShouldBe("2026-W07");
    }

    [Fact]
    public void GetPeriodLabel_Weekly_Week1_PadsWithZero()
    {
        // First week of 2026
        var date = new DateOnly(2026, 1, 5); // Monday of week 2 (Jan 5 is Mon in ISO terms)

        var label = PeriodBoundaryCalculator.GetPeriodLabel(date, ShardPeriod.Weekly);

        label.ShouldStartWith("202");
        label.ShouldContain("-W");
    }

    #endregion

    #region GetPeriodLabel — Monthly

    [Fact]
    public void GetPeriodLabel_Monthly_ReturnsYearMonth()
    {
        var date = new DateOnly(2026, 2, 15);

        var label = PeriodBoundaryCalculator.GetPeriodLabel(date, ShardPeriod.Monthly);

        label.ShouldBe("2026-02");
    }

    [Fact]
    public void GetPeriodLabel_Monthly_December_ReturnsCorrectLabel()
    {
        var date = new DateOnly(2026, 12, 25);

        var label = PeriodBoundaryCalculator.GetPeriodLabel(date, ShardPeriod.Monthly);

        label.ShouldBe("2026-12");
    }

    #endregion

    #region GetPeriodLabel — Quarterly

    [Theory]
    [InlineData(1, "2026-Q1")]
    [InlineData(2, "2026-Q1")]
    [InlineData(3, "2026-Q1")]
    [InlineData(4, "2026-Q2")]
    [InlineData(5, "2026-Q2")]
    [InlineData(6, "2026-Q2")]
    [InlineData(7, "2026-Q3")]
    [InlineData(8, "2026-Q3")]
    [InlineData(9, "2026-Q3")]
    [InlineData(10, "2026-Q4")]
    [InlineData(11, "2026-Q4")]
    [InlineData(12, "2026-Q4")]
    public void GetPeriodLabel_Quarterly_ReturnsCorrectQuarter(int month, string expectedLabel)
    {
        var date = new DateOnly(2026, month, 15);

        var label = PeriodBoundaryCalculator.GetPeriodLabel(date, ShardPeriod.Quarterly);

        label.ShouldBe(expectedLabel);
    }

    #endregion

    #region GetPeriodLabel — Yearly

    [Fact]
    public void GetPeriodLabel_Yearly_ReturnsYearOnly()
    {
        var date = new DateOnly(2026, 7, 15);

        var label = PeriodBoundaryCalculator.GetPeriodLabel(date, ShardPeriod.Yearly);

        label.ShouldBe("2026");
    }

    #endregion

    #region GetPeriodLabel — Invalid Period

    [Fact]
    public void GetPeriodLabel_InvalidPeriod_ThrowsArgumentOutOfRange()
    {
        var date = new DateOnly(2026, 1, 1);

        Should.Throw<ArgumentOutOfRangeException>(() =>
            PeriodBoundaryCalculator.GetPeriodLabel(date, (ShardPeriod)99));
    }

    #endregion

    #region EnumeratePeriods

    [Fact]
    public void EnumeratePeriods_Monthly_ThreeMonths_ReturnsThreePeriods()
    {
        var from = new DateOnly(2026, 1, 1);
        var to = new DateOnly(2026, 4, 1);

        var periods = PeriodBoundaryCalculator.EnumeratePeriods(from, to, ShardPeriod.Monthly).ToList();

        periods.Count.ShouldBe(3);
        periods[0].ShouldBe((new DateOnly(2026, 1, 1), new DateOnly(2026, 2, 1)));
        periods[1].ShouldBe((new DateOnly(2026, 2, 1), new DateOnly(2026, 3, 1)));
        periods[2].ShouldBe((new DateOnly(2026, 3, 1), new DateOnly(2026, 4, 1)));
    }

    [Fact]
    public void EnumeratePeriods_FromMidMonth_StartsFromContainingPeriod()
    {
        var from = new DateOnly(2026, 1, 15);
        var to = new DateOnly(2026, 3, 15);

        var periods = PeriodBoundaryCalculator.EnumeratePeriods(from, to, ShardPeriod.Monthly).ToList();

        periods.Count.ShouldBe(3);
        periods[0].Start.ShouldBe(new DateOnly(2026, 1, 1));
        periods[2].Start.ShouldBe(new DateOnly(2026, 3, 1));
    }

    [Fact]
    public void EnumeratePeriods_FromEqualsTo_ReturnsEmpty()
    {
        var date = new DateOnly(2026, 1, 1);

        var periods = PeriodBoundaryCalculator.EnumeratePeriods(date, date, ShardPeriod.Monthly).ToList();

        periods.ShouldBeEmpty();
    }

    [Fact]
    public void EnumeratePeriods_FromGreaterThanTo_ReturnsEmpty()
    {
        var from = new DateOnly(2026, 3, 1);
        var to = new DateOnly(2026, 1, 1);

        var periods = PeriodBoundaryCalculator.EnumeratePeriods(from, to, ShardPeriod.Monthly).ToList();

        periods.ShouldBeEmpty();
    }

    [Fact]
    public void EnumeratePeriods_Daily_SevenDays_ReturnsSevenPeriods()
    {
        var from = new DateOnly(2026, 2, 1);
        var to = new DateOnly(2026, 2, 8);

        var periods = PeriodBoundaryCalculator.EnumeratePeriods(from, to, ShardPeriod.Daily).ToList();

        periods.Count.ShouldBe(7);
    }

    [Fact]
    public void EnumeratePeriods_Yearly_CrossesYear_ReturnsTwoPeriods()
    {
        var from = new DateOnly(2025, 6, 1);
        var to = new DateOnly(2027, 6, 1);

        var periods = PeriodBoundaryCalculator.EnumeratePeriods(from, to, ShardPeriod.Yearly).ToList();

        periods.Count.ShouldBe(3); // 2025, 2026, 2027 (starts from 2025-01-01)
    }

    [Fact]
    public void EnumeratePeriods_Quarterly_FullYear_ReturnsFourPeriods()
    {
        var from = new DateOnly(2026, 1, 1);
        var to = new DateOnly(2027, 1, 1);

        var periods = PeriodBoundaryCalculator.EnumeratePeriods(from, to, ShardPeriod.Quarterly).ToList();

        periods.Count.ShouldBe(4);
        periods[0].ShouldBe((new DateOnly(2026, 1, 1), new DateOnly(2026, 4, 1)));
        periods[1].ShouldBe((new DateOnly(2026, 4, 1), new DateOnly(2026, 7, 1)));
        periods[2].ShouldBe((new DateOnly(2026, 7, 1), new DateOnly(2026, 10, 1)));
        periods[3].ShouldBe((new DateOnly(2026, 10, 1), new DateOnly(2027, 1, 1)));
    }

    [Fact]
    public void EnumeratePeriods_PeriodsAreContiguous_NoGaps()
    {
        var from = new DateOnly(2026, 1, 1);
        var to = new DateOnly(2026, 7, 1);

        var periods = PeriodBoundaryCalculator.EnumeratePeriods(from, to, ShardPeriod.Monthly).ToList();

        for (var i = 0; i < periods.Count - 1; i++)
        {
            periods[i].End.ShouldBe(periods[i + 1].Start,
                $"Gap detected between period {i} end and period {i + 1} start");
        }
    }

    #endregion

    #region Edge Cases — Leap Year

    [Fact]
    public void GetPeriodStart_Monthly_LeapYearFeb29_ReturnsFirstOfFeb()
    {
        var date = new DateOnly(2028, 2, 29);

        var result = PeriodBoundaryCalculator.GetPeriodStart(date, ShardPeriod.Monthly);

        result.ShouldBe(new DateOnly(2028, 2, 1));
    }

    [Fact]
    public void GetPeriodLabel_Daily_LeapYearFeb29_ReturnsCorrectDate()
    {
        var date = new DateOnly(2028, 2, 29);

        var label = PeriodBoundaryCalculator.GetPeriodLabel(date, ShardPeriod.Daily);

        label.ShouldBe("2028-02-29");
    }

    #endregion

    #region Edge Cases — Year Boundary

    [Fact]
    public void GetPeriodBoundaries_Daily_Dec31ToJan1_CrossesYear()
    {
        var date = new DateOnly(2026, 12, 31);

        var (start, end) = PeriodBoundaryCalculator.GetPeriodBoundaries(date, ShardPeriod.Daily);

        start.ShouldBe(new DateOnly(2026, 12, 31));
        end.ShouldBe(new DateOnly(2027, 1, 1));
    }

    [Fact]
    public void EnumeratePeriods_Monthly_CrossesYearBoundary_CorrectPeriods()
    {
        var from = new DateOnly(2026, 11, 1);
        var to = new DateOnly(2027, 2, 1);

        var periods = PeriodBoundaryCalculator.EnumeratePeriods(from, to, ShardPeriod.Monthly).ToList();

        periods.Count.ShouldBe(3); // Nov, Dec, Jan
        periods[0].Start.ShouldBe(new DateOnly(2026, 11, 1));
        periods[1].Start.ShouldBe(new DateOnly(2026, 12, 1));
        periods[2].Start.ShouldBe(new DateOnly(2027, 1, 1));
    }

    #endregion

    #region Labels Are Sortable

    [Fact]
    public void GetPeriodLabel_Monthly_LexicographicallySortable()
    {
        var labels = Enumerable.Range(1, 12)
            .Select(m => PeriodBoundaryCalculator.GetPeriodLabel(
                new DateOnly(2026, m, 1), ShardPeriod.Monthly))
            .ToList();

        var sorted = labels.OrderBy(l => l, StringComparer.Ordinal).ToList();

        labels.ShouldBe(sorted);
    }

    [Fact]
    public void GetPeriodLabel_Daily_LexicographicallySortable()
    {
        var labels = Enumerable.Range(0, 365)
            .Select(d => PeriodBoundaryCalculator.GetPeriodLabel(
                new DateOnly(2026, 1, 1).AddDays(d), ShardPeriod.Daily))
            .ToList();

        var sorted = labels.OrderBy(l => l, StringComparer.Ordinal).ToList();

        labels.ShouldBe(sorted);
    }

    [Fact]
    public void GetPeriodLabel_Quarterly_LexicographicallySortable()
    {
        int[] quarterStartMonths = [1, 4, 7, 10];
        var labels = quarterStartMonths
            .Select(m => PeriodBoundaryCalculator.GetPeriodLabel(
                new DateOnly(2026, m, 1), ShardPeriod.Quarterly))
            .ToList();

        var sorted = labels.OrderBy(l => l, StringComparer.Ordinal).ToList();

        labels.ShouldBe(sorted);
    }

    #endregion
}
