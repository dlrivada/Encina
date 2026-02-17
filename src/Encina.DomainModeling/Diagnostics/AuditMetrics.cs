using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Encina.DomainModeling.Diagnostics;

/// <summary>
/// Exposes audit trail metrics via the <c>Encina</c> meter.
/// </summary>
/// <remarks>
/// <para>
/// The following instruments are registered:
/// <list type="bullet">
///   <item><description><c>encina.audit.entries_recorded_total</c> (Counter) —
///   Total number of audit entries recorded, tagged with <c>entity_type</c> and <c>action</c>.</description></item>
///   <item><description><c>encina.audit.query_duration_ms</c> (Histogram) —
///   Duration of audit log queries in milliseconds.</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class AuditMetrics
{
    private static readonly Meter Meter = new("Encina", "1.0");

    private readonly Counter<long> _entriesRecordedTotal;
    private readonly Histogram<double> _queryDuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditMetrics"/> class,
    /// registering all audit metric instruments.
    /// </summary>
    public AuditMetrics()
    {
        _entriesRecordedTotal = Meter.CreateCounter<long>(
            "encina.audit.entries_recorded_total",
            unit: "{entries}",
            description: "Total number of audit entries recorded.");

        _queryDuration = Meter.CreateHistogram<double>(
            "encina.audit.query_duration_ms",
            unit: "ms",
            description: "Duration of audit log queries in milliseconds.");
    }

    /// <summary>
    /// Records an audit entry being written.
    /// </summary>
    /// <param name="entityType">The audited entity type name.</param>
    /// <param name="action">The audit action (created, updated, deleted).</param>
    public void RecordEntry(string entityType, string action)
    {
        _entriesRecordedTotal.Add(1, new TagList
        {
            { "entity_type", entityType },
            { "action", action }
        });
    }

    /// <summary>
    /// Records an audit log query duration.
    /// </summary>
    /// <param name="queryType">The query type (by_entity, by_date_range, all).</param>
    /// <param name="durationMs">The query duration in milliseconds.</param>
    public void RecordQueryDuration(string queryType, double durationMs)
    {
        _queryDuration.Record(durationMs,
            new KeyValuePair<string, object?>("query_type", queryType));
    }
}
