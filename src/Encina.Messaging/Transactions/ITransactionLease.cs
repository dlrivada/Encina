using LanguageExt;

namespace Encina.Messaging.Transactions;

/// <summary>
/// A participant's hold on the scope's transaction, returned by
/// <see cref="ITransactionLeaseSource.BeginOrJoinAsync"/>.
/// </summary>
/// <remarks>
/// Disposing an owning lease that was neither committed nor rolled back rolls the transaction back, so a
/// participant that returns early never leaves a transaction open.
/// </remarks>
public interface ITransactionLease : IAsyncDisposable
{
    /// <summary>
    /// Gets a value indicating whether this lease began the transaction and is therefore the only one that
    /// commits it.
    /// </summary>
    bool IsOwner { get; }

    /// <summary>
    /// Commits the transaction when this lease owns it; a joiner's commit is a no-op that succeeds.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the commit.</param>
    /// <returns>
    /// <see cref="Unit"/> on success, or <see cref="TransactionErrors.RollbackOnly"/> when a participant rolled
    /// back (the transaction is rolled back), or <see cref="TransactionErrors.CommitFailed"/> when the commit
    /// itself failed.
    /// </returns>
    ValueTask<Either<EncinaError, Unit>> CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls the transaction back when this lease owns it; a joiner's rollback marks the transaction
    /// rollback-only.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the rollback.</param>
    /// <returns>A task that completes when the rollback was issued or recorded.</returns>
    ValueTask RollbackAsync(CancellationToken cancellationToken = default);
}
