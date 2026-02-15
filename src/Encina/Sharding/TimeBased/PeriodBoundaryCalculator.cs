using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Encina.Sharding.TimeBased;

/// <summary>
/// Computes period start and end boundaries for time-based shard partitioning.
/// </summary>
/// <remarks>
/// <para>
/// All boundaries use <see cref="DateOnly"/> to avoid timezone ambiguity. The start date
/// is inclusive and the end date is exclusive, forming a half-open interval
/// <c>[PeriodStart, PeriodEnd)</c>.
/// </para>
/// <para>
/// Weekly boundaries follow ISO 8601 (Monday as the first day of the week) by default.
/// US-style weeks (starting on Sunday) are supported via the <c>weekStart</c>
/// parameter on applicable methods.
/// </para>
/// </remarks>
[SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters",
    Justification = "Pre-1.0: DateTime/DateOnly overloads are intentional for developer convenience.")]
public static class PeriodBoundaryCalculator
{
    /// <summary>
    /// Computes the inclusive start date of the period containing the given timestamp.
    /// </summary>
    /// <param name="timestamp">The timestamp to locate within a period.</param>
    /// <param name="period">The period granularity.</param>
    /// <param name="weekStart">
    /// The first day of the week for <see cref="ShardPeriod.Weekly"/> calculations.
    /// Defaults to <see cref="DayOfWeek.Monday"/> (ISO 8601). Ignored for other periods.
    /// </param>
    /// <returns>The inclusive start date of the period.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="period"/> is not a valid enum value.</exception>
    public static DateOnly GetPeriodStart(DateTime timestamp, ShardPeriod period, DayOfWeek weekStart = DayOfWeek.Monday)
    {
        var date = DateOnly.FromDateTime(timestamp);
        return GetPeriodStart(date, period, weekStart);
    }

    /// <summary>
    /// Computes the inclusive start date of the period containing the given date.
    /// </summary>
    /// <param name="date">The date to locate within a period.</param>
    /// <param name="period">The period granularity.</param>
    /// <param name="weekStart">
    /// The first day of the week for <see cref="ShardPeriod.Weekly"/> calculations.
    /// Defaults to <see cref="DayOfWeek.Monday"/> (ISO 8601). Ignored for other periods.
    /// </param>
    /// <returns>The inclusive start date of the period.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="period"/> is not a valid enum value.</exception>
    public static DateOnly GetPeriodStart(DateOnly date, ShardPeriod period, DayOfWeek weekStart = DayOfWeek.Monday)
    {
        return period switch
        {
            ShardPeriod.Daily => date,
            ShardPeriod.Weekly => GetWeekStart(date, weekStart),
            ShardPeriod.Monthly => new DateOnly(date.Year, date.Month, 1),
            ShardPeriod.Quarterly => GetQuarterStart(date),
            ShardPeriod.Yearly => new DateOnly(date.Year, 1, 1),
            _ => throw new ArgumentOutOfRangeException(nameof(period), period, "Unknown shard period.")
        };
    }

    /// <summary>
    /// Computes the exclusive end date of the period containing the given timestamp.
    /// </summary>
    /// <param name="timestamp">The timestamp to locate within a period.</param>
    /// <param name="period">The period granularity.</param>
    /// <param name="weekStart">
    /// The first day of the week for <see cref="ShardPeriod.Weekly"/> calculations.
    /// Defaults to <see cref="DayOfWeek.Monday"/> (ISO 8601). Ignored for other periods.
    /// </param>
    /// <returns>The exclusive end date of the period (first day of the next period).</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="period"/> is not a valid enum value.</exception>
    public static DateOnly GetPeriodEnd(DateTime timestamp, ShardPeriod period, DayOfWeek weekStart = DayOfWeek.Monday)
    {
        var date = DateOnly.FromDateTime(timestamp);
        return GetPeriodEnd(date, period, weekStart);
    }

    /// <summary>
    /// Computes the exclusive end date of the period containing the given date.
    /// </summary>
    /// <param name="date">The date to locate within a period.</param>
    /// <param name="period">The period granularity.</param>
    /// <param name="weekStart">
    /// The first day of the week for <see cref="ShardPeriod.Weekly"/> calculations.
    /// Defaults to <see cref="DayOfWeek.Monday"/> (ISO 8601). Ignored for other periods.
    /// </param>
    /// <returns>The exclusive end date of the period (first day of the next period).</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="period"/> is not a valid enum value.</exception>
    public static DateOnly GetPeriodEnd(DateOnly date, ShardPeriod period, DayOfWeek weekStart = DayOfWeek.Monday)
    {
        var start = GetPeriodStart(date, period, weekStart);

        return period switch
        {
            ShardPeriod.Daily => start.AddDays(1),
            ShardPeriod.Weekly => start.AddDays(7),
            ShardPeriod.Monthly => start.AddMonths(1),
            ShardPeriod.Quarterly => start.AddMonths(3),
            ShardPeriod.Yearly => new DateOnly(start.Year + 1, 1, 1),
            _ => throw new ArgumentOutOfRangeException(nameof(period), period, "Unknown shard period.")
        };
    }

    /// <summary>
    /// Computes both the inclusive start and exclusive end dates of the period containing the given timestamp.
    /// </summary>
    /// <param name="timestamp">The timestamp to locate within a period.</param>
    /// <param name="period">The period granularity.</param>
    /// <param name="weekStart">
    /// The first day of the week for <see cref="ShardPeriod.Weekly"/> calculations.
    /// Defaults to <see cref="DayOfWeek.Monday"/> (ISO 8601). Ignored for other periods.
    /// </param>
    /// <returns>A tuple of (inclusive start, exclusive end) dates for the period.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="period"/> is not a valid enum value.</exception>
    public static (DateOnly Start, DateOnly End) GetPeriodBoundaries(
        DateTime timestamp,
        ShardPeriod period,
        DayOfWeek weekStart = DayOfWeek.Monday)
    {
        var date = DateOnly.FromDateTime(timestamp);
        return GetPeriodBoundaries(date, period, weekStart);
    }

    /// <summary>
    /// Computes both the inclusive start and exclusive end dates of the period containing the given date.
    /// </summary>
    /// <param name="date">The date to locate within a period.</param>
    /// <param name="period">The period granularity.</param>
    /// <param name="weekStart">
    /// The first day of the week for <see cref="ShardPeriod.Weekly"/> calculations.
    /// Defaults to <see cref="DayOfWeek.Monday"/> (ISO 8601). Ignored for other periods.
    /// </param>
    /// <returns>A tuple of (inclusive start, exclusive end) dates for the period.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="period"/> is not a valid enum value.</exception>
    public static (DateOnly Start, DateOnly End) GetPeriodBoundaries(
        DateOnly date,
        ShardPeriod period,
        DayOfWeek weekStart = DayOfWeek.Monday)
    {
        var start = GetPeriodStart(date, period, weekStart);
        var end = GetPeriodEnd(date, period, weekStart);
        return (start, end);
    }

    /// <summary>
    /// Generates a deterministic shard ID label for the period containing the given date.
    /// </summary>
    /// <param name="date">The date to generate a label for.</param>
    /// <param name="period">The period granularity.</param>
    /// <param name="weekStart">
    /// The first day of the week for <see cref="ShardPeriod.Weekly"/> calculations.
    /// Defaults to <see cref="DayOfWeek.Monday"/> (ISO 8601). Ignored for other periods.
    /// </param>
    /// <returns>
    /// A stable, sortable string label for the period. Examples:
    /// <c>"2026-02-15"</c> (daily), <c>"2026-W07"</c> (weekly), <c>"2026-02"</c> (monthly),
    /// <c>"2026-Q1"</c> (quarterly), <c>"2026"</c> (yearly).
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="period"/> is not a valid enum value.</exception>
    public static string GetPeriodLabel(DateOnly date, ShardPeriod period, DayOfWeek weekStart = DayOfWeek.Monday)
    {
        var start = GetPeriodStart(date, period, weekStart);

        return period switch
        {
            ShardPeriod.Daily => start.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ShardPeriod.Weekly => FormatIsoWeek(start),
            ShardPeriod.Monthly => start.ToString("yyyy-MM", CultureInfo.InvariantCulture),
            ShardPeriod.Quarterly => $"{start.Year}-Q{((start.Month - 1) / 3) + 1}",
            ShardPeriod.Yearly => start.Year.ToString(CultureInfo.InvariantCulture),
            _ => throw new ArgumentOutOfRangeException(nameof(period), period, "Unknown shard period.")
        };
    }

    /// <summary>
    /// Enumerates all period boundaries within a date range, yielding each period's
    /// start and end dates.
    /// </summary>
    /// <param name="from">The inclusive start of the range.</param>
    /// <param name="to">The exclusive end of the range.</param>
    /// <param name="period">The period granularity.</param>
    /// <param name="weekStart">
    /// The first day of the week for <see cref="ShardPeriod.Weekly"/> calculations.
    /// Defaults to <see cref="DayOfWeek.Monday"/> (ISO 8601). Ignored for other periods.
    /// </param>
    /// <returns>An enumerable of (start, end) tuples for each period that overlaps the range.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="period"/> is not a valid enum value.</exception>
    public static IEnumerable<(DateOnly Start, DateOnly End)> EnumeratePeriods(
        DateOnly from,
        DateOnly to,
        ShardPeriod period,
        DayOfWeek weekStart = DayOfWeek.Monday)
    {
        if (from >= to)
        {
            yield break;
        }

        // Start at the period containing 'from'
        var current = GetPeriodStart(from, period, weekStart);

        while (current < to)
        {
            var periodEnd = GetPeriodEnd(current, period, weekStart);
            yield return (current, periodEnd);
            current = periodEnd;
        }
    }

    private static DateOnly GetWeekStart(DateOnly date, DayOfWeek weekStart)
    {
        var daysFromWeekStart = ((int)date.DayOfWeek - (int)weekStart + 7) % 7;
        return date.AddDays(-daysFromWeekStart);
    }

    private static DateOnly GetQuarterStart(DateOnly date)
    {
        var quarterMonth = ((date.Month - 1) / 3 * 3) + 1;
        return new DateOnly(date.Year, quarterMonth, 1);
    }

    private static string FormatIsoWeek(DateOnly periodStart)
    {
        // ISO 8601 week number from the period start date
        var weekNumber = ISOWeek.GetWeekOfYear(periodStart.ToDateTime(TimeOnly.MinValue));
        var year = ISOWeek.GetYear(periodStart.ToDateTime(TimeOnly.MinValue));
        return $"{year}-W{weekNumber:D2}";
    }
}
