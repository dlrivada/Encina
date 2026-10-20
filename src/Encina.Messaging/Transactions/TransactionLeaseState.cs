using System.Diagnostics;
using Encina.Diagnostics;
using Encina.Messaging.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Encina.Messaging.Transactions;

/// <summary>
/// The join state of one transaction: how many participants joined the owner, whether one of them rolled back
/// (rollback-only), whether the transaction has ended, and the tracing activity that spans it.
/// </summary>
/// <remarks>
/// <para>
/// One instance is created per transaction, by the accessor that begins it, so the join, rollback-only and
/// observability rules exist once for every provider (the relational accessor here; the EF Core and MongoDB
/// accessors reuse it through <c>InternalsVisibleTo</c>). It is not thread safe: a scope's participants run one
/// after another.
/// </para>
/// <para>
/// Logs and tags carry only counts, error codes and <c>db.system</c>; never an error message or a payload.
/// </para>
/// </remarks>
internal sealed class TransactionLeaseState
{
    private readonly ILogger _logger;
    private readonly string _dbSystem;
    private readonly Activity? _activity;
    private int _joiners;

    /// <summary>
    /// Starts the state, and the transaction's activity, for a transaction that was just begun.
    /// </summary>
    /// <param name="dbSystem">The <c>db.system</c> value of the transaction's connection.</param>
    /// <param name="logger">The logger of the accessor that began the transaction.</param>
    internal TransactionLeaseState(string dbSystem, ILogger logger)
    {
        _dbSystem = dbSystem;
        _logger = logger;
        _activity = TransactionActivitySource.StartTransaction(dbSystem);
    }

    /// <summary>Gets a value indicating whether the transaction has ended (committed or rolled back).</summary>
    internal bool IsClosed { get; private set; }

    /// <summary>Gets a value indicating whether a joined participant rolled back.</summary>
    internal bool RollbackOnly { get; private set; }

    /// <summary>Gets the number of participants holding the transaction, the owner included.</summary>
    internal int Depth => IsClosed ? 0 : _joiners + 1;

    /// <summary>
    /// Registers a participant that joins the owner's transaction.
    /// </summary>
    internal void Join()
    {
        _joiners++;
        MessagingLog.TransactionJoined(_logger, Depth);
    }

    /// <summary>
    /// Releases a joined participant.
    /// </summary>
    internal void Leave()
    {
        if (_joiners > 0)
        {
            _joiners--;
        }
    }

    /// <summary>
    /// Records that a joined participant rolled back, so the owner's commit is refused.
    /// </summary>
    internal void MarkRollbackOnly()
    {
        if (IsClosed || RollbackOnly)
        {
            return;
        }

        RollbackOnly = true;
        MessagingLog.TransactionMarkedRollbackOnly(_logger);
        OutboxProcessorMetrics.Instance.RecordRollbackOnly(_dbSystem);
    }

    /// <summary>
    /// Records that the owner's commit was refused because the transaction is rollback-only.
    /// </summary>
    internal void RecordCommitRefused()
        => MessagingLog.TransactionRollbackOnlyCommitRefused(_logger, TransactionErrors.RollbackOnlyCode);

    /// <summary>
    /// Records that the commit failed.
    /// </summary>
    /// <param name="exception">The exception raised by the commit.</param>
    internal void RecordCommitFailed(Exception exception)
        => MessagingLog.TransactionCommitFailed(_logger, exception.ForLogging(), TransactionErrors.CommitFailedCode);

    /// <summary>
    /// Ends the transaction: later joiner operations fail with <see cref="TransactionErrors.LeaseDisposed"/>.
    /// </summary>
    /// <param name="outcome">One of the <c>Outcome*</c> constants of <see cref="TransactionActivitySource"/>.</param>
    internal void Close(string outcome)
    {
        if (IsClosed)
        {
            return;
        }

        IsClosed = true;
        TransactionActivitySource.Complete(_activity, outcome);
    }
}
