using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Encina.DomainModeling.Diagnostics;

/// <summary>
/// Exposes bulk operations metrics via the <c>Encina</c> meter.
/// </summary>
/// <remarks>
/// <para>
/// The following instruments are registered:
/// <list type="bullet">
///   <item><description><c>encina.bulk.operations_total</c> (Counter) —
///   Total number of bulk operations, tagged with <c>operation</c>, <c>entity_type</c>, and <c>provider</c>.</description></item>
///   <item><description><c>encina.bulk.rows_affected_total</c> (Counter) —
///   Total number of rows affected by bulk operations.</description></item>
///   <item><description><c>encina.bulk.operation_duration_ms</c> (Histogram) —
///   Duration of bulk operations in milliseconds.</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class BulkOperationsMetrics
{
    private static readonly Meter Meter = new("Encina", "1.0");

    private readonly Counter<long> _operationsTotal;
    private readonly Counter<long> _rowsAffectedTotal;
    private readonly Histogram<double> _operationDuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="BulkOperationsMetrics"/> class,
    /// registering all bulk operations metric instruments.
    /// </summary>
    public BulkOperationsMetrics()
    {
        _operationsTotal = Meter.CreateCounter<long>(
            "encina.bulk.operations_total",
            unit: "{operations}",
            description: "Total number of bulk operations.");

        _rowsAffectedTotal = Meter.CreateCounter<long>(
            "encina.bulk.rows_affected_total",
            unit: "{rows}",
            description: "Total number of rows affected by bulk operations.");

        _operationDuration = Meter.CreateHistogram<double>(
            "encina.bulk.operation_duration_ms",
            unit: "ms",
            description: "Duration of bulk operations in milliseconds.");
    }

    /// <summary>
    /// Records a completed bulk operation.
    /// </summary>
    /// <param name="operation">The operation type (insert, update, delete, merge, read).</param>
    /// <param name="entityType">The entity type name.</param>
    /// <param name="provider">The data access provider.</param>
    /// <param name="rowsAffected">The number of rows affected.</param>
    /// <param name="durationMs">The operation duration in milliseconds.</param>
    public void RecordOperation(string operation, string entityType, string provider, int rowsAffected, double durationMs)
    {
        var tags = new TagList
        {
            { "operation", operation },
            { "entity_type", entityType },
            { "provider", provider }
        };

        _operationsTotal.Add(1, tags);
        _operationDuration.Record(durationMs, tags);

        _rowsAffectedTotal.Add(rowsAffected, new TagList
        {
            { "operation", operation },
            { "entity_type", entityType }
        });
    }
}
