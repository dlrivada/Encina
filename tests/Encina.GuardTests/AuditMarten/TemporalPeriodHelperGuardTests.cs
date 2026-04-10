using Encina.Audit.Marten;
using Encina.Audit.Marten.Crypto;

namespace Encina.GuardTests.AuditMarten;

/// <summary>
/// Guard tests exercising <see cref="TemporalPeriodHelper"/> utility methods and branches.
/// </summary>
public class TemporalPeriodHelperGuardTests
{
    [Fact]
    public void GetPeriod_Monthly_ReturnsExpected()
    {
        var ts = new DateTimeOffset(2026, 3, 15, 0, 0, 0, TimeSpan.Zero);
        TemporalPeriodHelper.GetPeriod(ts, TemporalKeyGranularity.Monthly).ShouldBe("2026-03");
    }

    [Fact]
    public void GetPeriod_Quarterly_ReturnsExpected()
    {
        var ts = new DateTimeOffset(2026, 5, 15, 0, 0, 0, TimeSpan.Zero);
        TemporalPeriodHelper.GetPeriod(ts, TemporalKeyGranularity.Quarterly).ShouldBe("2026-Q2");
    }

    [Fact]
    public void GetPeriod_Yearly_ReturnsExpected()
    {
        var ts = new DateTimeOffset(2026, 3, 15, 0, 0, 0, TimeSpan.Zero);
        TemporalPeriodHelper.GetPeriod(ts, TemporalKeyGranularity.Yearly).ShouldBe("2026");
    }

    [Fact]
    public void GetPeriod_InvalidGranularity_Throws()
    {
        var ts = new DateTimeOffset(2026, 3, 15, 0, 0, 0, TimeSpan.Zero);
        Should.Throw<ArgumentOutOfRangeException>(() =>
            TemporalPeriodHelper.GetPeriod(ts, (TemporalKeyGranularity)999));
    }

    [Fact]
    public void GetPeriod_DateTimeOverload_Works()
    {
        var ts = new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc);
        TemporalPeriodHelper.GetPeriod(ts, TemporalKeyGranularity.Monthly).ShouldBe("2026-03");
    }

    [Fact]
    public void FormatKeyId_ReturnsExpectedFormat()
    {
        TemporalPeriodHelper.FormatKeyId("2026-03", 2).ShouldBe("temporal:2026-03:v2");
    }

    [Fact]
    public void FormatDestroyedMarkerId_ReturnsExpectedFormat()
    {
        TemporalPeriodHelper.FormatDestroyedMarkerId("2026-03").ShouldBe("temporal-destroyed:2026-03");
    }

    [Fact]
    public void EnumeratePeriods_Monthly_ReturnsAllMonthsInRange()
    {
        var from = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var to = new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero);

        var periods = TemporalPeriodHelper.EnumeratePeriods(from, to, TemporalKeyGranularity.Monthly).ToList();

        periods.ShouldContain("2026-01");
        periods.ShouldContain("2026-02");
        periods.ShouldContain("2026-03");
    }

    [Fact]
    public void EnumeratePeriods_Yearly_ReturnsYears()
    {
        var from = new DateTimeOffset(2024, 6, 1, 0, 0, 0, TimeSpan.Zero);
        var to = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);

        var periods = TemporalPeriodHelper.EnumeratePeriods(from, to, TemporalKeyGranularity.Yearly).ToList();

        periods.ShouldContain("2024");
        periods.ShouldContain("2025");
        periods.ShouldContain("2026");
    }

    [Fact]
    public void EnumeratePeriods_Quarterly_ReturnsQuarters()
    {
        var from = new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero);
        var to = new DateTimeOffset(2026, 8, 15, 0, 0, 0, TimeSpan.Zero);

        var periods = TemporalPeriodHelper.EnumeratePeriods(from, to, TemporalKeyGranularity.Quarterly).ToList();

        periods.ShouldContain("2026-Q1");
        periods.ShouldContain("2026-Q2");
        periods.ShouldContain("2026-Q3");
    }

    [Fact]
    public void TryParsePeriodToDate_MonthlyValid_ReturnsTrue()
    {
        var ok = TemporalPeriodHelper.TryParsePeriodToDate("2026-03", TemporalKeyGranularity.Monthly, out var date);
        ok.ShouldBeTrue();
        date.Year.ShouldBe(2026);
        date.Month.ShouldBe(3);
    }

    [Fact]
    public void TryParsePeriodToDate_MonthlyInvalid_ReturnsFalse()
    {
        var ok = TemporalPeriodHelper.TryParsePeriodToDate("not-a-date", TemporalKeyGranularity.Monthly, out _);
        ok.ShouldBeFalse();
    }

    [Fact]
    public void TryParsePeriodToDate_QuarterlyValid_ReturnsTrue()
    {
        var ok = TemporalPeriodHelper.TryParsePeriodToDate("2026-Q2", TemporalKeyGranularity.Quarterly, out var date);
        ok.ShouldBeTrue();
        date.Year.ShouldBe(2026);
        date.Month.ShouldBe(4);
    }

    [Fact]
    public void TryParsePeriodToDate_QuarterlyInvalidQuarter_ReturnsFalse()
    {
        var ok = TemporalPeriodHelper.TryParsePeriodToDate("2026-Q9", TemporalKeyGranularity.Quarterly, out _);
        ok.ShouldBeFalse();
    }

    [Fact]
    public void TryParsePeriodToDate_YearlyValid_ReturnsTrue()
    {
        var ok = TemporalPeriodHelper.TryParsePeriodToDate("2026", TemporalKeyGranularity.Yearly, out var date);
        ok.ShouldBeTrue();
        date.Year.ShouldBe(2026);
    }

    [Fact]
    public void TryParsePeriodToDate_YearlyInvalid_ReturnsFalse()
    {
        var ok = TemporalPeriodHelper.TryParsePeriodToDate("notyear", TemporalKeyGranularity.Yearly, out _);
        ok.ShouldBeFalse();
    }

    [Fact]
    public void TryParsePeriodToDate_Null_ReturnsFalse()
    {
        var ok = TemporalPeriodHelper.TryParsePeriodToDate(null!, TemporalKeyGranularity.Monthly, out _);
        ok.ShouldBeFalse();
    }

    [Fact]
    public void TryParsePeriodToDate_Empty_ReturnsFalse()
    {
        var ok = TemporalPeriodHelper.TryParsePeriodToDate("", TemporalKeyGranularity.Monthly, out _);
        ok.ShouldBeFalse();
    }

    [Fact]
    public void TryParsePeriodToDate_InvalidGranularity_ReturnsFalse()
    {
        var ok = TemporalPeriodHelper.TryParsePeriodToDate("2026", (TemporalKeyGranularity)999, out _);
        ok.ShouldBeFalse();
    }
}
