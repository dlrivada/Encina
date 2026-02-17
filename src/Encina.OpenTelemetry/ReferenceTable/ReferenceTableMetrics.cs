using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Encina.OpenTelemetry.ReferenceTable;

/// <summary>
/// Exposes reference table replication metrics via the <c>Encina</c> meter.
/// </summary>
/// <remarks>
/// <para>
/// The following instruments are registered:
/// <list type="bullet">
///   <item><description><c>encina.reference_table.replication_duration_ms</c> (Histogram) —
///   Duration of individual reference table replication operations, tagged with
///   <c>reference_table.entity_type</c>.</description></item>
///   <item><description><c>encina.reference_table.rows_synced_total</c> (Counter) —
///   Total number of rows synced across all reference table replications, tagged with
///   <c>reference_table.entity_type</c>.</description></item>
///   <item><description><c>encina.reference_table.sync_errors_total</c> (Counter) —
///   Total number of errors during reference table replication, tagged with
///   <c>reference_table.entity_type</c> and <c>error.type</c>.</description></item>
///   <item><description><c>encina.reference_table.active_replications</c> (UpDownCounter) —
///   Number of currently active replication operations.</description></item>
///   <item><description><c>encina.reference_table.staleness_ms</c> (ObservableGauge) —
///   Current staleness per reference table in milliseconds (time since last replication).</description></item>
/// </list>
/// </para>
/// <para>
/// All instruments use the same <c>"Encina"</c> meter for consistency with
/// <c>ShardedCdcMetrics</c>, <c>ColocationMetrics</c>, and <c>ShardRoutingMetrics</c>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Registered automatically via AddEncinaOpenTelemetry()
/// services.AddEncinaOpenTelemetry();
///
/// // Or manually:
/// var metrics = new ReferenceTableMetrics(
///     registeredTablesCallback: () => 3,
///     stalenessCallback: () => [("Country", 120.5), ("Currency", 45.0)]);
/// metrics.RecordReplicationDuration("Country", 250.0);
/// metrics.RecordRowsSynced("Country", 100);
/// metrics.RecordError("Country", "ConnectionTimeout");
/// </code>
/// </example>
public sealed class ReferenceTableMetrics
{
    private static readonly Meter Meter = new("Encina", "1.0");

    private readonly Histogram<double> _replicationDuration;
    private readonly Counter<long> _rowsSynced;
    private readonly Counter<long> _syncErrors;
    private readonly UpDownCounter<int> _activeReplications;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReferenceTableMetrics"/> class,
    /// registering all reference table metric instruments.
    /// </summary>
    /// <param name="registeredTablesCallback">
    /// Callback that returns the current number of registered reference tables.
    /// </param>
    /// <param name="stalenessCallback">
    /// Callback that returns per-table staleness measurements as tuples of (entityType, stalenessMs).
    /// </param>
    public ReferenceTableMetrics(
        Func<int> registeredTablesCallback,
        Func<IEnumerable<(string EntityType, double StalenessMs)>> stalenessCallback)
    {
        ArgumentNullException.ThrowIfNull(registeredTablesCallback);
        ArgumentNullException.ThrowIfNull(stalenessCallback);

        _replicationDuration = Meter.CreateHistogram<double>(
            "encina.reference_table.replication_duration_ms",
            unit: "ms",
            description: "Duration of reference table replication operations.");

        _rowsSynced = Meter.CreateCounter<long>(
            "encina.reference_table.rows_synced_total",
            unit: "{rows}",
            description: "Total number of rows synced during reference table replication.");

        _syncErrors = Meter.CreateCounter<long>(
            "encina.reference_table.sync_errors_total",
            unit: "{errors}",
            description: "Total number of errors during reference table replication.");

        _activeReplications = Meter.CreateUpDownCounter<int>(
            "encina.reference_table.active_replications",
            unit: "{replications}",
            description: "Number of currently active reference table replication operations.");

        Meter.CreateObservableGauge(
            "encina.reference_table.staleness_ms",
            () => stalenessCallback().Select(s =>
                new Measurement<double>(
                    s.StalenessMs,
                    new KeyValuePair<string, object?>(
                        ActivityTagNames.ReferenceTable.EntityType, s.EntityType))),
            unit: "ms",
            description: "Current staleness per reference table in milliseconds.");
    }

    /// <summary>
    /// Records the duration of a reference table replication operation.
    /// </summary>
    /// <param name="entityType">The entity type name of the reference table.</param>
    /// <param name="durationMs">The duration in milliseconds.</param>
    public void RecordReplicationDuration(string entityType, double durationMs)
    {
        _replicationDuration.Record(durationMs,
            new KeyValuePair<string, object?>(
                ActivityTagNames.ReferenceTable.EntityType, entityType));
    }

    /// <summary>
    /// Records the number of rows synced during replication.
    /// </summary>
    /// <param name="entityType">The entity type name of the reference table.</param>
    /// <param name="count">The number of rows synced.</param>
    public void RecordRowsSynced(string entityType, long count)
    {
        _rowsSynced.Add(count,
            new KeyValuePair<string, object?>(
                ActivityTagNames.ReferenceTable.EntityType, entityType));
    }

    /// <summary>
    /// Records an error during reference table replication.
    /// </summary>
    /// <param name="entityType">The entity type name of the reference table.</param>
    /// <param name="errorType">The type or category of the error.</param>
    public void RecordError(string entityType, string errorType)
    {
        var tags = new TagList
        {
            { ActivityTagNames.ReferenceTable.EntityType, entityType },
            { "error.type", errorType }
        };

        _syncErrors.Add(1, tags);
    }

    /// <summary>
    /// Increments the active replication count.
    /// </summary>
    public void IncrementActiveReplications()
    {
        _activeReplications.Add(1);
    }

    /// <summary>
    /// Decrements the active replication count.
    /// </summary>
    public void DecrementActiveReplications()
    {
        _activeReplications.Add(-1);
    }
}
