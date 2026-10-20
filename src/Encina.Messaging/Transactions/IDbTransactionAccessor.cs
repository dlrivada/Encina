using System.Data;
using LanguageExt;

namespace Encina.Messaging.Transactions;

/// <summary>
/// The relational transaction of a request scope: owns the transaction on the scope's
/// <see cref="IDbConnection"/> and lets every participant (transaction behavior, unit of work, messaging
/// stores) share it.
/// </summary>
/// <remarks>
/// <para>
/// ADO.NET providers reject (SQL Server, MySQL) or silently join (PostgreSQL) a command that runs on a
/// connection with a pending transaction. A store that must take part in the business transaction reads
/// <see cref="Current"/> and assigns it to its commands (<see cref="DbLease.Enlisted"/>); a store that must
/// survive the rollback (the inbox recording a failed attempt) runs on a separate connection while a
/// transaction is active (<see cref="DbLease.IndependentAsync"/>). See ADR-048.
/// </para>
/// <para>
/// Only the accessor starts and ends the transaction: participants call
/// <see cref="ITransactionLeaseSource.BeginOrJoinAsync(CancellationToken)"/> or
/// <see cref="BeginOrJoinAsync(IsolationLevel?, CancellationToken)"/> and hold the lease. The accessor is
/// scoped: one instance per request scope, shared by every participant of that scope.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// await using var lease = (await accessor.BeginOrJoinAsync(IsolationLevel.ReadCommitted, ct))
///     .IfLeft(error => throw new InvalidOperationException(error.GetCode().IfNone("transaction.failed")));
///
/// using var command = accessor.Connection.CreateCommand();
/// command.Transaction = lease.Transaction;
/// // ... run the command ...
///
/// var committed = await lease.CommitAsync(ct); // a no-op when this lease only joined
/// </code>
/// </example>
public interface IDbTransactionAccessor : ITransactionLeaseSource
{
    /// <summary>Gets the scope's connection, the one the active transaction runs on.</summary>
    IDbConnection Connection { get; }

    /// <summary>
    /// Gets the transaction active in this scope, or <see langword="null"/> when none is. Only the accessor
    /// sets it.
    /// </summary>
    IDbTransaction? Current { get; }

    /// <summary>
    /// Begins the scope's transaction at <paramref name="isolationLevel"/>, or joins it when one is active.
    /// </summary>
    /// <param name="isolationLevel">
    /// The isolation level to begin with, or <see langword="null"/> for the connection's default. A joiner that
    /// asks for a level different from the active transaction's gets
    /// <see cref="TransactionErrors.IsolationLevelMismatch"/>.
    /// </param>
    /// <param name="cancellationToken">Cancellation token for opening the connection and beginning.</param>
    /// <returns>The lease, or a <c>transaction.*</c> error.</returns>
    ValueTask<Either<EncinaError, IDbTransactionLease>> BeginOrJoinAsync(
        IsolationLevel? isolationLevel,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// A lease on a relational transaction.
/// </summary>
public interface IDbTransactionLease : ITransactionLease
{
    /// <summary>Gets the transaction commands must be assigned to.</summary>
    IDbTransaction Transaction { get; }
}
