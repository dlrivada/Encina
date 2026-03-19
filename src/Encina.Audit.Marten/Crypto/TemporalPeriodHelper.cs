using System.Globalization;

namespace Encina.Audit.Marten.Crypto;

/// <summary>
/// Utility methods for computing temporal key period identifiers from timestamps.
/// </summary>
/// <remarks>
/// <para>
/// Period identifiers are formatted based on the configured <see cref="TemporalKeyGranularity"/>:
/// <list type="bullet">
/// <item><see cref="TemporalKeyGranularity.Monthly"/> — <c>"2026-03"</c> (ISO 8601 year-month)</item>
/// <item><see cref="TemporalKeyGranularity.Quarterly"/> — <c>"2026-Q1"</c> (year-quarter)</item>
/// <item><see cref="TemporalKeyGranularity.Yearly"/> — <c>"2026"</c> (year only)</item>
/// </list>
/// </para>
/// <para>
/// All period identifiers are deterministic: the same timestamp and granularity always
/// produce the same period string. This enables consistent key lookup during both
/// encryption (write path) and decryption (read path).
/// </para>
/// </remarks>
public static class TemporalPeriodHelper
{
    /// <summary>
    /// Computes the period identifier for a given timestamp and granularity.
    /// </summary>
    /// <param name="timestamp">The UTC timestamp to compute the period for.</param>
    /// <param name="granularity">The time-partitioning granularity.</param>
    /// <returns>
    /// A period identifier string (e.g., <c>"2026-03"</c>, <c>"2026-Q1"</c>, or <c>"2026"</c>).
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="granularity"/> is not a valid <see cref="TemporalKeyGranularity"/> value.
    /// </exception>
    /// <example>
    /// <code>
    /// var period = TemporalPeriodHelper.GetPeriod(
    ///     new DateTimeOffset(2026, 3, 15, 10, 30, 0, TimeSpan.Zero),
    ///     TemporalKeyGranularity.Monthly);
    /// // Returns: "2026-03"
    ///
    /// var quarterPeriod = TemporalPeriodHelper.GetPeriod(
    ///     new DateTimeOffset(2026, 3, 15, 10, 30, 0, TimeSpan.Zero),
    ///     TemporalKeyGranularity.Quarterly);
    /// // Returns: "2026-Q1"
    /// </code>
    /// </example>
    public static string GetPeriod(DateTimeOffset timestamp, TemporalKeyGranularity granularity) =>
        granularity switch
        {
            TemporalKeyGranularity.Monthly => timestamp.ToString("yyyy-MM", CultureInfo.InvariantCulture),
            TemporalKeyGranularity.Quarterly => $"{timestamp.Year}-Q{GetQuarter(timestamp.Month)}",
            TemporalKeyGranularity.Yearly => timestamp.Year.ToString(CultureInfo.InvariantCulture),
            _ => throw new ArgumentOutOfRangeException(nameof(granularity), granularity, "Invalid temporal key granularity.")
        };

    /// <summary>
    /// Computes the period identifier for a given <see cref="DateTime"/> and granularity.
    /// </summary>
    /// <param name="timestampUtc">The UTC timestamp to compute the period for.</param>
    /// <param name="granularity">The time-partitioning granularity.</param>
    /// <returns>
    /// A period identifier string (e.g., <c>"2026-03"</c>, <c>"2026-Q1"</c>, or <c>"2026"</c>).
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="granularity"/> is not a valid <see cref="TemporalKeyGranularity"/> value.
    /// </exception>
    public static string GetPeriod(DateTime timestampUtc, TemporalKeyGranularity granularity) =>
        GetPeriod(new DateTimeOffset(timestampUtc, TimeSpan.Zero), granularity);

    /// <summary>
    /// Formats a temporal key identifier following the convention <c>"temporal:{period}:v{version}"</c>.
    /// </summary>
    /// <param name="period">The time period (e.g., <c>"2026-03"</c>).</param>
    /// <param name="version">The key version number.</param>
    /// <returns>The formatted key identifier.</returns>
    public static string FormatKeyId(string period, int version) =>
        $"temporal:{period}:v{version}";

    /// <summary>
    /// Formats the destroyed marker document ID for a time period.
    /// </summary>
    /// <param name="period">The time period (e.g., <c>"2026-03"</c>).</param>
    /// <returns>The formatted destroyed marker identifier.</returns>
    public static string FormatDestroyedMarkerId(string period) =>
        $"temporal-destroyed:{period}";

    /// <summary>
    /// Enumerates all period identifiers between two timestamps (inclusive) for the given granularity.
    /// </summary>
    /// <param name="fromUtc">The start timestamp (inclusive).</param>
    /// <param name="toUtc">The end timestamp (inclusive).</param>
    /// <param name="granularity">The time-partitioning granularity.</param>
    /// <returns>An enumerable of period identifier strings, in chronological order.</returns>
    /// <remarks>
    /// Used by <c>DestroyKeysBeforeAsync</c> to determine which periods need to be destroyed
    /// when given a cutoff date.
    /// </remarks>
    public static IEnumerable<string> EnumeratePeriods(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        TemporalKeyGranularity granularity)
    {
        var current = GetPeriodStart(fromUtc, granularity);
        var end = toUtc;

        while (current <= end)
        {
            yield return GetPeriod(current, granularity);
            current = AdvancePeriod(current, granularity);
        }
    }

    /// <summary>
    /// Returns the calendar quarter (1–4) for a given month.
    /// </summary>
    private static int GetQuarter(int month) => (month - 1) / 3 + 1;

    /// <summary>
    /// Gets the start of the period containing the given timestamp.
    /// </summary>
    private static DateTimeOffset GetPeriodStart(DateTimeOffset timestamp, TemporalKeyGranularity granularity) =>
        granularity switch
        {
            TemporalKeyGranularity.Monthly => new DateTimeOffset(timestamp.Year, timestamp.Month, 1, 0, 0, 0, TimeSpan.Zero),
            TemporalKeyGranularity.Quarterly => new DateTimeOffset(timestamp.Year, (GetQuarter(timestamp.Month) - 1) * 3 + 1, 1, 0, 0, 0, TimeSpan.Zero),
            TemporalKeyGranularity.Yearly => new DateTimeOffset(timestamp.Year, 1, 1, 0, 0, 0, TimeSpan.Zero),
            _ => throw new ArgumentOutOfRangeException(nameof(granularity), granularity, "Invalid temporal key granularity.")
        };

    /// <summary>
    /// Advances a timestamp to the start of the next period.
    /// </summary>
    private static DateTimeOffset AdvancePeriod(DateTimeOffset current, TemporalKeyGranularity granularity) =>
        granularity switch
        {
            TemporalKeyGranularity.Monthly => current.AddMonths(1),
            TemporalKeyGranularity.Quarterly => current.AddMonths(3),
            TemporalKeyGranularity.Yearly => current.AddYears(1),
            _ => throw new ArgumentOutOfRangeException(nameof(granularity), granularity, "Invalid temporal key granularity.")
        };
}
