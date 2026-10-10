using System.Data;
using System.Data.Common;

namespace Encina.Messaging;

/// <summary>
/// The connection and transaction a store command must use: either the request's shared connection enlisted in
/// the active business transaction, or, when the write must outlive that transaction's rollback, a separate
/// connection of its own (disposed with the lease).
/// </summary>
/// <remarks>
/// <see cref="Enlisted"/> is for writes that must commit or roll back with the business transaction;
/// <see cref="IndependentAsync"/> is for writes that must survive its rollback. When no transaction is active
/// both use the shared connection directly, so the extra connection is only paid while a transaction is open.
/// </remarks>
public sealed class DbLease : IDisposable
{
    private readonly IDbConnection? _ownedConnection;

    private DbLease(IDbConnection connection, IDbTransaction? transaction, IDbConnection? ownedConnection)
    {
        Connection = connection;
        Transaction = transaction;
        _ownedConnection = ownedConnection;
    }

    /// <summary>Gets the connection to run commands on.</summary>
    public IDbConnection Connection { get; }

    /// <summary>Gets the transaction commands must be assigned to, or <see langword="null"/> when there is none.</summary>
    public IDbTransaction? Transaction { get; }

    /// <summary>
    /// Leases the shared <paramref name="connection"/>, enlisted in the active business transaction of
    /// <paramref name="accessor"/> when that transaction runs on this connection.
    /// </summary>
    /// <param name="connection">The request's scoped connection.</param>
    /// <param name="accessor">The scope's transaction accessor, or <see langword="null"/> when none is registered.</param>
    /// <returns>A lease whose commands join the active transaction.</returns>
    public static DbLease Enlisted(IDbConnection connection, IDbTransactionAccessor? accessor)
    {
        ArgumentNullException.ThrowIfNull(connection);
        return new DbLease(connection, ActiveTransactionOn(connection, accessor), ownedConnection: null);
    }

    /// <summary>
    /// Leases a connection whose writes are not part of the active business transaction: the shared
    /// <paramref name="connection"/> when no transaction is active, otherwise an opened clone of it.
    /// </summary>
    /// <param name="connection">The request's scoped connection.</param>
    /// <param name="accessor">The scope's transaction accessor, or <see langword="null"/> when none is registered.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A lease whose commands autocommit outside the business transaction.</returns>
    /// <exception cref="InvalidOperationException">
    /// A business transaction is active and <paramref name="connection"/> cannot be cloned
    /// (it does not implement <see cref="ICloneable"/>).
    /// </exception>
    public static async Task<DbLease> IndependentAsync(
        IDbConnection connection,
        IDbTransactionAccessor? accessor,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(connection);

        if (ActiveTransactionOn(connection, accessor) is null)
        {
            return new DbLease(connection, transaction: null, ownedConnection: null);
        }

        // A decorating connection (module isolation) is not cloneable itself: clone the connection it wraps.
        var source = Innermost(connection);
        if (source is not ICloneable cloneable)
        {
            throw new InvalidOperationException(
                $"{source.GetType().Name} cannot be cloned, so a write that must survive the business transaction's rollback has no connection of its own.");
        }

        var clone = (IDbConnection)cloneable.Clone();

        try
        {
            if (clone is DbConnection dbClone)
            {
                await dbClone.OpenAsync(cancellationToken).ConfigureAwait(false);
            }
            else
            {
                clone.Open();
            }
        }
        catch
        {
            clone.Dispose();
            throw;
        }

        return new DbLease(clone, transaction: null, ownedConnection: clone);
    }

    /// <summary>
    /// Creates a command on <see cref="Connection"/> assigned to <see cref="Transaction"/>.
    /// </summary>
    /// <returns>The command; the caller disposes it.</returns>
    public IDbCommand CreateCommand()
    {
        var command = Connection.CreateCommand();
        command.Transaction = Transaction;
        return command;
    }

    /// <inheritdoc />
    public void Dispose() => _ownedConnection?.Dispose();

    // The transaction belongs to this connection when it runs on it or on any connection it decorates.
    private static IDbTransaction? ActiveTransactionOn(IDbConnection connection, IDbTransactionAccessor? accessor)
    {
        var current = accessor?.Current;
        if (current is null)
        {
            return null;
        }

        for (var candidate = connection; candidate is not null; candidate = (candidate as IWrappedDbConnection)?.InnerConnection)
        {
            if (ReferenceEquals(current.Connection, candidate))
            {
                return current;
            }
        }

        return null;
    }

    private static IDbConnection Innermost(IDbConnection connection)
    {
        while (connection is IWrappedDbConnection wrapper)
        {
            connection = wrapper.InnerConnection;
        }

        return connection;
    }
}
