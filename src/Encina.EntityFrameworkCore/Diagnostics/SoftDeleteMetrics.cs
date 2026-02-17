using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Encina.EntityFrameworkCore.Diagnostics;

/// <summary>
/// Exposes soft delete metrics via the <c>Encina</c> meter.
/// </summary>
/// <remarks>
/// <para>
/// The following instruments are registered:
/// <list type="bullet">
///   <item><description><c>encina.softdelete.operations_total</c> (Counter) —
///   Total number of soft delete operations, tagged with <c>operation</c> and <c>entity_type</c>.</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class SoftDeleteMetrics
{
    private static readonly Meter Meter = new("Encina", "1.0");

    private readonly Counter<long> _operationsTotal;

    /// <summary>
    /// Initializes a new instance of the <see cref="SoftDeleteMetrics"/> class,
    /// registering all soft delete metric instruments.
    /// </summary>
    public SoftDeleteMetrics()
    {
        _operationsTotal = Meter.CreateCounter<long>(
            "encina.softdelete.operations_total",
            unit: "{operations}",
            description: "Total number of soft delete operations.");
    }

    /// <summary>
    /// Records a soft delete operation.
    /// </summary>
    /// <param name="operation">The operation type (delete, restore, hard_delete).</param>
    /// <param name="entityType">The entity type name.</param>
    public void RecordOperation(string operation, string entityType)
    {
        _operationsTotal.Add(1, new TagList
        {
            { "operation", operation },
            { "entity_type", entityType }
        });
    }
}
