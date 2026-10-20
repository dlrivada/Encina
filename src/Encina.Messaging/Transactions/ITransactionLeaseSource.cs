using LanguageExt;

namespace Encina.Messaging.Transactions;

/// <summary>
/// Provider-neutral source of the one transaction a request scope shares: the first participant that asks owns
/// it, later participants join it.
/// </summary>
/// <remarks>
/// <para>
/// This is the contract the transactional outbox behavior depends on, so it stays in <c>Encina.Messaging</c>
/// without knowing how a provider represents a transaction. Relational providers expose the richer
/// <see cref="IDbTransactionAccessor"/>.
/// </para>
/// <para>
/// Join semantics: the first <see cref="BeginOrJoinAsync"/> in a scope owns the transaction and is the only
/// participant that commits it. Later calls return a non-owning lease whose commit is a no-op and whose
/// rollback marks the transaction rollback-only, so the owner's commit then rolls back and returns
/// <see cref="TransactionErrors.RollbackOnly"/>. The source is scoped and not thread safe.
/// </para>
/// </remarks>
public interface ITransactionLeaseSource
{
    /// <summary>
    /// Gets a value indicating whether a transaction is active in this scope, that is, whether the next
    /// <see cref="BeginOrJoinAsync"/> joins instead of beginning.
    /// </summary>
    bool HasActiveTransaction { get; }

    /// <summary>
    /// Begins the scope's transaction, or joins it when one is already active.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for beginning the transaction.</param>
    /// <returns>
    /// The lease, or a <c>transaction.*</c> error when the transaction cannot be begun.
    /// </returns>
    ValueTask<Either<EncinaError, ITransactionLease>> BeginOrJoinAsync(CancellationToken cancellationToken = default);
}
