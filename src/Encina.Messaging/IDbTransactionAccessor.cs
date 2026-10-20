using System.Data;

namespace Encina.Messaging;

/// <summary>
/// Exposes the business transaction that <see cref="TransactionPipelineBehavior{TRequest, TResponse}"/> has
/// opened on the request's scoped <see cref="IDbConnection"/>, so that stores sharing that connection can enlist
/// their commands in it.
/// </summary>
/// <remarks>
/// <para>
/// ADO.NET providers reject (SQL Server, MySQL) or silently join (PostgreSQL) a command that runs on a
/// connection with a pending transaction. A store that must take part in the business transaction (the inbox
/// marking a message processed together with the business effect) reads <see cref="Current"/> and assigns it to
/// its commands; a store that must survive the rollback (the inbox recording a failed attempt) runs on a
/// separate connection while a transaction is active. See ADR-048.
/// </para>
/// <para>
/// The accessor is scoped: one instance per request scope, shared by the behavior and the stores of that scope.
/// </para>
/// </remarks>
public interface IDbTransactionAccessor
{
    /// <summary>
    /// Gets or sets the business transaction active in this scope, or <see langword="null"/> when none is.
    /// </summary>
    IDbTransaction? Current { get; set; }
}

/// <summary>
/// Default scoped <see cref="IDbTransactionAccessor"/>.
/// </summary>
public sealed class DbTransactionAccessor : IDbTransactionAccessor
{
    /// <inheritdoc />
    public IDbTransaction? Current { get; set; }
}
