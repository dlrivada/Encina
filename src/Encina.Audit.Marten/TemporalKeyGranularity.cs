namespace Encina.Audit.Marten;

/// <summary>
/// Defines the time-partitioning granularity for temporal encryption keys.
/// </summary>
/// <remarks>
/// <para>
/// Temporal keys are partitioned by time period. Each period has its own encryption key.
/// When <c>MartenAuditStore</c> purges audit entries via crypto-shredding, it destroys
/// the temporal keys for the affected periods, rendering all PII in those periods permanently
/// unreadable while preserving the immutable event stream.
/// </para>
/// <para>
/// The granularity determines how many keys are managed and how precisely the shredding
/// boundary aligns with the retention period:
/// <list type="bullet">
/// <item><see cref="Monthly"/> — 12 keys/year, ~30-day shredding precision (recommended for most apps)</item>
/// <item><see cref="Quarterly"/> — 4 keys/year, ~90-day shredding precision (lower key management overhead)</item>
/// <item><see cref="Yearly"/> — 1 key/year, ~365-day shredding precision (minimal overhead, coarse control)</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.AddEncinaAuditMarten(options =>
/// {
///     // Monthly keys — audit entries in March 2026 use key "2026-03"
///     options.TemporalGranularity = TemporalKeyGranularity.Monthly;
///
///     // Quarterly keys — audit entries in Q1 2026 use key "2026-Q1"
///     // options.TemporalGranularity = TemporalKeyGranularity.Quarterly;
///
///     // Yearly keys — all entries in 2026 use key "2026"
///     // options.TemporalGranularity = TemporalKeyGranularity.Yearly;
/// });
/// </code>
/// </example>
public enum TemporalKeyGranularity
{
    /// <summary>
    /// One encryption key per calendar month (e.g., <c>"2026-03"</c>).
    /// </summary>
    /// <remarks>
    /// Provides the finest shredding precision (~30 days). Recommended for applications
    /// with strict data minimization requirements (GDPR Art. 5(1)(e)).
    /// Generates 12 keys per year × retention years (e.g., 84 keys for 7-year retention).
    /// </remarks>
    Monthly = 0,

    /// <summary>
    /// One encryption key per calendar quarter (e.g., <c>"2026-Q1"</c>).
    /// </summary>
    /// <remarks>
    /// Provides moderate shredding precision (~90 days). Good balance between key management
    /// overhead and compliance granularity. Generates 4 keys per year.
    /// </remarks>
    Quarterly = 1,

    /// <summary>
    /// One encryption key per calendar year (e.g., <c>"2026"</c>).
    /// </summary>
    /// <remarks>
    /// Provides the coarsest shredding precision (~365 days). Suitable for applications
    /// with long retention periods (SOX 7-year) where per-year granularity is sufficient.
    /// Generates 1 key per year.
    /// </remarks>
    Yearly = 2
}
