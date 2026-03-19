using System.Diagnostics.Metrics;

namespace Encina.Audit.Marten.Diagnostics;

/// <summary>
/// Provides OpenTelemetry metrics for Marten event-sourced audit operations.
/// </summary>
/// <remarks>
/// <para>
/// Meter name: <c>Encina.Audit.Marten</c>. Instruments follow OpenTelemetry semantic conventions
/// with dimensional tags for entity_type, action, outcome, and period.
/// </para>
/// <para>
/// Counters:
/// <list type="bullet">
/// <item><c>encina.audit.marten.entries_recorded</c> — Total audit entries recorded</item>
/// <item><c>encina.audit.marten.entries_queried</c> — Total audit queries executed</item>
/// <item><c>encina.audit.marten.periods_shredded</c> — Total temporal key periods destroyed</item>
/// <item><c>encina.audit.marten.encryption_operations</c> — Total PII encryption operations</item>
/// <item><c>encina.audit.marten.decryption_operations</c> — Total PII decryption operations</item>
/// </list>
/// </para>
/// <para>
/// Histograms:
/// <list type="bullet">
/// <item><c>encina.audit.marten.record_duration_ms</c> — Time to record an audit entry</item>
/// <item><c>encina.audit.marten.query_duration_ms</c> — Time to execute an audit query</item>
/// <item><c>encina.audit.marten.purge_duration_ms</c> — Time to crypto-shred temporal keys</item>
/// </list>
/// </para>
/// </remarks>
internal static class MartenAuditMeter
{
    internal const string MeterName = "Encina.Audit.Marten";
    internal const string MeterVersion = "1.0";

    private static readonly Meter Meter = new(MeterName, MeterVersion);

    // ── Counters ─────────────────────────────────────────────────────────

    /// <summary>Total audit entries recorded (tags: entity_type, action, store_type).</summary>
    internal static readonly Counter<long> EntriesRecorded =
        Meter.CreateCounter<long>(
            "encina.audit.marten.entries_recorded",
            unit: "{entries}",
            description: "Total audit entries recorded to Marten event store");

    /// <summary>Total audit queries executed (tags: query_type, store_type).</summary>
    internal static readonly Counter<long> EntriesQueried =
        Meter.CreateCounter<long>(
            "encina.audit.marten.entries_queried",
            unit: "{queries}",
            description: "Total audit queries executed against Marten projections");

    /// <summary>Total temporal key periods crypto-shredded.</summary>
    internal static readonly Counter<long> PeriodsShredded =
        Meter.CreateCounter<long>(
            "encina.audit.marten.periods_shredded",
            unit: "{periods}",
            description: "Total temporal key periods destroyed via crypto-shredding");

    /// <summary>Total PII encryption operations (tags: outcome).</summary>
    internal static readonly Counter<long> EncryptionOperations =
        Meter.CreateCounter<long>(
            "encina.audit.marten.encryption_operations",
            unit: "{operations}",
            description: "Total PII field encryption operations");

    /// <summary>Total PII decryption operations (tags: outcome).</summary>
    internal static readonly Counter<long> DecryptionOperations =
        Meter.CreateCounter<long>(
            "encina.audit.marten.decryption_operations",
            unit: "{operations}",
            description: "Total PII field decryption operations");

    // ── Histograms ───────────────────────────────────────────────────────

    /// <summary>Time to record an audit entry (encrypt + append + save), in milliseconds.</summary>
    internal static readonly Histogram<double> RecordDuration =
        Meter.CreateHistogram<double>(
            "encina.audit.marten.record_duration_ms",
            unit: "ms",
            description: "Duration of audit entry recording (encrypt + append + save)");

    /// <summary>Time to execute an audit query, in milliseconds.</summary>
    internal static readonly Histogram<double> QueryDuration =
        Meter.CreateHistogram<double>(
            "encina.audit.marten.query_duration_ms",
            unit: "ms",
            description: "Duration of audit query execution against projections");

    /// <summary>Time to crypto-shred temporal keys, in milliseconds.</summary>
    internal static readonly Histogram<double> PurgeDuration =
        Meter.CreateHistogram<double>(
            "encina.audit.marten.purge_duration_ms",
            unit: "ms",
            description: "Duration of temporal key crypto-shredding operation");
}
