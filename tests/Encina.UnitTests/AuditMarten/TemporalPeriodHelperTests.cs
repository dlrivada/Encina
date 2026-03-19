using Encina.Audit.Marten;
using Encina.Audit.Marten.Crypto;

using Shouldly;

namespace Encina.UnitTests.AuditMarten;

/// <summary>
/// Unit tests for <see cref="TemporalPeriodHelper"/> period formatting and enumeration.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Provider", "Marten")]
public sealed class TemporalPeriodHelperTests
{
    // ── GetPeriod ─────────────────────────────────────────────────────────

    [Theory]
    [InlineData(2026, 1, 15, TemporalKeyGranularity.Monthly, "2026-01")]
    [InlineData(2026, 3, 1, TemporalKeyGranularity.Monthly, "2026-03")]
    [InlineData(2026, 12, 31, TemporalKeyGranularity.Monthly, "2026-12")]
    public void GetPeriod_Monthly_ReturnsYearMonth(int year, int month, int day, TemporalKeyGranularity granularity, string expected)
    {
        var timestamp = new DateTime(year, month, day, 12, 0, 0, DateTimeKind.Utc);
        TemporalPeriodHelper.GetPeriod(timestamp, granularity).ShouldBe(expected);
    }

    [Theory]
    [InlineData(2026, 1, 15, "2026-Q1")]
    [InlineData(2026, 3, 31, "2026-Q1")]
    [InlineData(2026, 4, 1, "2026-Q2")]
    [InlineData(2026, 6, 30, "2026-Q2")]
    [InlineData(2026, 7, 1, "2026-Q3")]
    [InlineData(2026, 10, 1, "2026-Q4")]
    [InlineData(2026, 12, 31, "2026-Q4")]
    public void GetPeriod_Quarterly_ReturnsYearQuarter(int year, int month, int day, string expected)
    {
        var timestamp = new DateTime(year, month, day, 12, 0, 0, DateTimeKind.Utc);
        TemporalPeriodHelper.GetPeriod(timestamp, TemporalKeyGranularity.Quarterly).ShouldBe(expected);
    }

    [Theory]
    [InlineData(2026, 1, 1, "2026")]
    [InlineData(2026, 6, 15, "2026")]
    [InlineData(2026, 12, 31, "2026")]
    public void GetPeriod_Yearly_ReturnsYear(int year, int month, int day, string expected)
    {
        var timestamp = new DateTime(year, month, day, 12, 0, 0, DateTimeKind.Utc);
        TemporalPeriodHelper.GetPeriod(timestamp, TemporalKeyGranularity.Yearly).ShouldBe(expected);
    }

    [Fact]
    public void GetPeriod_DateTimeOffset_ReturnsSameAsDateTime()
    {
        var dtUtc = new DateTime(2026, 3, 15, 12, 0, 0, DateTimeKind.Utc);
        var dto = new DateTimeOffset(dtUtc);

        var fromDt = TemporalPeriodHelper.GetPeriod(dtUtc, TemporalKeyGranularity.Monthly);
        var fromDto = TemporalPeriodHelper.GetPeriod(dto, TemporalKeyGranularity.Monthly);

        fromDt.ShouldBe(fromDto);
    }

    // ── FormatKeyId ────────────────────────────────────────────────────────

    [Fact]
    public void FormatKeyId_ReturnsExpectedFormat()
    {
        TemporalPeriodHelper.FormatKeyId("2026-03", 1).ShouldBe("temporal:2026-03:v1");
        TemporalPeriodHelper.FormatKeyId("2026-Q1", 3).ShouldBe("temporal:2026-Q1:v3");
        TemporalPeriodHelper.FormatKeyId("2026", 10).ShouldBe("temporal:2026:v10");
    }

    // ── FormatDestroyedMarkerId ────────────────────────────────────────────

    [Fact]
    public void FormatDestroyedMarkerId_ReturnsExpectedFormat()
    {
        TemporalPeriodHelper.FormatDestroyedMarkerId("2026-03").ShouldBe("temporal-destroyed:2026-03");
    }

    // ── EnumeratePeriods ──────────────────────────────────────────────────

    [Fact]
    public void EnumeratePeriods_Monthly_ReturnsAllMonths()
    {
        var from = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var to = new DateTimeOffset(2026, 3, 31, 23, 59, 59, TimeSpan.Zero);

        var periods = TemporalPeriodHelper.EnumeratePeriods(from, to, TemporalKeyGranularity.Monthly).ToList();

        periods.ShouldBe(["2026-01", "2026-02", "2026-03"]);
    }

    [Fact]
    public void EnumeratePeriods_Quarterly_ReturnsAllQuarters()
    {
        var from = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var to = new DateTimeOffset(2026, 12, 31, 23, 59, 59, TimeSpan.Zero);

        var periods = TemporalPeriodHelper.EnumeratePeriods(from, to, TemporalKeyGranularity.Quarterly).ToList();

        periods.ShouldBe(["2026-Q1", "2026-Q2", "2026-Q3", "2026-Q4"]);
    }

    [Fact]
    public void EnumeratePeriods_Yearly_ReturnsAllYears()
    {
        var from = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var to = new DateTimeOffset(2026, 12, 31, 23, 59, 59, TimeSpan.Zero);

        var periods = TemporalPeriodHelper.EnumeratePeriods(from, to, TemporalKeyGranularity.Yearly).ToList();

        periods.ShouldBe(["2024", "2025", "2026"]);
    }

    [Fact]
    public void EnumeratePeriods_SamePeriod_ReturnsSinglePeriod()
    {
        var from = new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero);
        var to = new DateTimeOffset(2026, 3, 31, 23, 59, 59, TimeSpan.Zero);

        var periods = TemporalPeriodHelper.EnumeratePeriods(from, to, TemporalKeyGranularity.Monthly).ToList();

        periods.ShouldBe(["2026-03"]);
    }
}
