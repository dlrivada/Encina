using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Encina.DomainModeling.Diagnostics;

/// <summary>
/// Exposes repository pattern metrics via the <c>Encina</c> meter.
/// </summary>
/// <remarks>
/// <para>
/// The following instruments are registered:
/// <list type="bullet">
///   <item><description><c>encina.repository.operations_total</c> (Counter) —
///   Total number of repository operations, tagged with <c>operation</c>, <c>entity_type</c>, and <c>provider</c>.</description></item>
///   <item><description><c>encina.repository.operation_duration_ms</c> (Histogram) —
///   Duration of repository operations in milliseconds.</description></item>
///   <item><description><c>encina.repository.errors_total</c> (Counter) —
///   Total number of repository operation errors.</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class RepositoryMetrics
{
    private static readonly Meter Meter = new("Encina", "1.0");

    private readonly Counter<long> _operationsTotal;
    private readonly Histogram<double> _operationDuration;
    private readonly Counter<long> _errorsTotal;

    /// <summary>
    /// Initializes a new instance of the <see cref="RepositoryMetrics"/> class,
    /// registering all repository metric instruments.
    /// </summary>
    public RepositoryMetrics()
    {
        _operationsTotal = Meter.CreateCounter<long>(
            "encina.repository.operations_total",
            unit: "{operations}",
            description: "Total number of repository operations.");

        _operationDuration = Meter.CreateHistogram<double>(
            "encina.repository.operation_duration_ms",
            unit: "ms",
            description: "Duration of repository operations in milliseconds.");

        _errorsTotal = Meter.CreateCounter<long>(
            "encina.repository.errors_total",
            unit: "{errors}",
            description: "Total number of repository operation errors.");
    }

    /// <summary>
    /// Records a successful repository operation.
    /// </summary>
    /// <param name="operation">The operation name (e.g., "get_by_id", "find", "add", "update", "remove").</param>
    /// <param name="entityType">The entity type name.</param>
    /// <param name="provider">The data access provider (e.g., "ef_core", "dapper", "ado", "mongodb").</param>
    /// <param name="durationMs">The operation duration in milliseconds.</param>
    public void RecordOperation(string operation, string entityType, string provider, double durationMs)
    {
        var tags = new TagList
        {
            { "operation", operation },
            { "entity_type", entityType },
            { "provider", provider }
        };

        _operationsTotal.Add(1, tags);
        _operationDuration.Record(durationMs, tags);
    }

    /// <summary>
    /// Records a repository operation error.
    /// </summary>
    /// <param name="operation">The operation name.</param>
    /// <param name="entityType">The entity type name.</param>
    /// <param name="errorCode">The error code.</param>
    public void RecordError(string operation, string entityType, string errorCode)
    {
        _errorsTotal.Add(1, new TagList
        {
            { "operation", operation },
            { "entity_type", entityType },
            { "error_code", errorCode }
        });
    }
}
