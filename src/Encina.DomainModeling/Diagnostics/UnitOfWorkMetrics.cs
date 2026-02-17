using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Encina.DomainModeling.Diagnostics;

/// <summary>
/// Exposes Unit of Work metrics via the <c>Encina</c> meter.
/// </summary>
/// <remarks>
/// <para>
/// The following instruments are registered:
/// <list type="bullet">
///   <item><description><c>encina.uow.transactions_total</c> (Counter) —
///   Total number of UoW transactions, tagged with <c>outcome</c> (committed, rolledback, error).</description></item>
///   <item><description><c>encina.uow.transaction_duration_ms</c> (Histogram) —
///   Duration of UoW transactions in milliseconds.</description></item>
///   <item><description><c>encina.uow.save_changes_rows</c> (Histogram) —
///   Number of rows affected per SaveChanges call.</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class UnitOfWorkMetrics
{
    private static readonly Meter Meter = new("Encina", "1.0");

    private readonly Counter<long> _transactionsTotal;
    private readonly Histogram<double> _transactionDuration;
    private readonly Histogram<long> _saveChangesRows;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnitOfWorkMetrics"/> class,
    /// registering all Unit of Work metric instruments.
    /// </summary>
    public UnitOfWorkMetrics()
    {
        _transactionsTotal = Meter.CreateCounter<long>(
            "encina.uow.transactions_total",
            unit: "{transactions}",
            description: "Total number of Unit of Work transactions.");

        _transactionDuration = Meter.CreateHistogram<double>(
            "encina.uow.transaction_duration_ms",
            unit: "ms",
            description: "Duration of Unit of Work transactions in milliseconds.");

        _saveChangesRows = Meter.CreateHistogram<long>(
            "encina.uow.save_changes_rows",
            unit: "{rows}",
            description: "Number of rows affected per SaveChanges call.");
    }

    /// <summary>
    /// Records a completed transaction.
    /// </summary>
    /// <param name="outcome">The transaction outcome (committed, rolledback, error).</param>
    /// <param name="durationMs">The transaction duration in milliseconds.</param>
    public void RecordTransaction(string outcome, double durationMs)
    {
        _transactionsTotal.Add(1,
            new KeyValuePair<string, object?>("outcome", outcome));
        _transactionDuration.Record(durationMs,
            new KeyValuePair<string, object?>("outcome", outcome));
    }

    /// <summary>
    /// Records the number of rows affected by a SaveChanges call.
    /// </summary>
    /// <param name="rowCount">The number of affected rows.</param>
    public void RecordSaveChangesRows(int rowCount)
    {
        _saveChangesRows.Record(rowCount);
    }
}
