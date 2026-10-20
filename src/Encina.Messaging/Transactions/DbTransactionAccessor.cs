using System.Data;
using System.Data.Common;
using Encina.Messaging.Diagnostics;
using LanguageExt;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace Encina.Messaging.Transactions;

/// <summary>
/// Scoped <see cref="IDbTransactionAccessor"/> for ADO.NET and Dapper: owns the transaction of the scope's
/// <see cref="IDbConnection"/> and lets every participant of the scope share it.
/// </summary>
/// <remarks>
/// <para>
/// The first <c>BeginOrJoinAsync</c> opens the connection when it is closed and begins the transaction; the
/// lease it returns owns it. Every later call joins: its lease commits as a no-op and rolls back by marking the
/// transaction rollback-only, so the owner's commit then rolls back and returns
/// <see cref="TransactionErrors.RollbackOnly"/>.
/// </para>
/// <para>
/// Everything is asynchronous and token aware (<see cref="DbConnection.OpenAsync(CancellationToken)"/>,
/// <see cref="DbConnection.BeginTransactionAsync(IsolationLevel, CancellationToken)"/>); a connection that is
/// not a <see cref="DbConnection"/> gets <see cref="TransactionErrors.NotSupported"/> instead of a synchronous
/// fallback. Commit and rollback run to completion regardless of the caller's token, so a canceled request still
/// cleans up. Disposing the accessor (the end of the scope) rolls back a transaction nobody finished.
/// </para>
/// </remarks>
public sealed class DbTransactionAccessor : IDbTransactionAccessor, IAsyncDisposable
{
    private readonly IDbConnection _connection;
    private readonly ILogger<DbTransactionAccessor> _logger;
    private OwnerLease? _owner;

    /// <summary>
    /// Initializes a new instance of the <see cref="DbTransactionAccessor"/> class.
    /// </summary>
    /// <param name="connection">The scope's database connection.</param>
    /// <param name="logger">The logger.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="connection"/> or <paramref name="logger"/> is null.</exception>
    public DbTransactionAccessor(IDbConnection connection, ILogger<DbTransactionAccessor> logger)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(logger);

        _connection = connection;
        _logger = logger;
    }

    /// <inheritdoc />
    public IDbConnection Connection => _connection;

    /// <inheritdoc />
    public IDbTransaction? Current => _owner?.Transaction;

    /// <inheritdoc />
    public bool HasActiveTransaction => _owner is not null;

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, ITransactionLease>> BeginOrJoinAsync(CancellationToken cancellationToken = default)
    {
        var lease = await BeginOrJoinAsync(isolationLevel: null, cancellationToken).ConfigureAwait(false);
        return lease.Map(static l => (ITransactionLease)l);
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, IDbTransactionLease>> BeginOrJoinAsync(
        IsolationLevel? isolationLevel,
        CancellationToken cancellationToken = default)
    {
        return _owner is { } owner
            ? Join(owner, isolationLevel)
            : await BeginAsync(isolationLevel, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => _owner?.DisposeAsync() ?? ValueTask.CompletedTask;

    private static Either<EncinaError, IDbTransactionLease> Join(OwnerLease owner, IsolationLevel? requested)
    {
        // Unspecified means "whatever the connection defaults to", so it can join any level.
        if (requested is { } level && level != IsolationLevel.Unspecified && level != owner.Transaction.IsolationLevel)
        {
            return Left<EncinaError, IDbTransactionLease>(
                TransactionErrors.IsolationLevelMismatch(level, owner.Transaction.IsolationLevel));
        }

        owner.State.Join();
        return Right<EncinaError, IDbTransactionLease>(new JoinerLease(owner.State, owner.Transaction));
    }

    private async ValueTask<Either<EncinaError, IDbTransactionLease>> BeginAsync(
        IsolationLevel? isolationLevel,
        CancellationToken cancellationToken)
    {
        if (_connection is not DbConnection dbConnection)
        {
            return Left<EncinaError, IDbTransactionLease>(TransactionErrors.NotSupported(_connection.GetType()));
        }

        try
        {
            if (dbConnection.State != ConnectionState.Open)
            {
                await dbConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
            }

            var transaction = await dbConnection
                .BeginTransactionAsync(isolationLevel ?? IsolationLevel.Unspecified, cancellationToken)
                .ConfigureAwait(false);

            var state = new TransactionLeaseState(DbSystemName.From(_connection), _logger);
            _owner = new OwnerLease(this, transaction, state);
            return Right<EncinaError, IDbTransactionLease>(_owner);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Left<EncinaError, IDbTransactionLease>(TransactionErrors.BeginFailed(ex));
        }
    }

    private void Release(OwnerLease owner)
    {
        if (ReferenceEquals(_owner, owner))
        {
            _owner = null;
        }
    }

    private sealed class OwnerLease : IDbTransactionLease
    {
        private readonly DbTransactionAccessor _accessor;
        private readonly DbTransaction _transaction;
        private bool _completed;
        private bool _disposed;

        internal OwnerLease(DbTransactionAccessor accessor, DbTransaction transaction, TransactionLeaseState state)
        {
            _accessor = accessor;
            _transaction = transaction;
            State = state;
        }

        internal TransactionLeaseState State { get; }

        public bool IsOwner => true;

        public IDbTransaction Transaction => _transaction;

        public async ValueTask<Either<EncinaError, Unit>> CommitAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed || _completed)
            {
                return Left<EncinaError, Unit>(TransactionErrors.LeaseDisposed());
            }

            if (State.RollbackOnly)
            {
                State.RecordCommitRefused();
                await RollbackCoreAsync(TransactionActivitySource.OutcomeRollbackOnly).ConfigureAwait(false);
                return Left<EncinaError, Unit>(TransactionErrors.RollbackOnly());
            }

            try
            {
                await _transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                State.RecordCommitFailed(ex);
                await RollbackCoreAsync(TransactionActivitySource.OutcomeCommitFailed).ConfigureAwait(false);
                return Left<EncinaError, Unit>(TransactionErrors.CommitFailed(ex));
            }

            Complete(TransactionActivitySource.OutcomeCommitted);
            return Right<EncinaError, Unit>(Unit.Default);
        }

        public ValueTask RollbackAsync(CancellationToken cancellationToken = default)
            => _disposed || _completed
                ? ValueTask.CompletedTask
                : new ValueTask(RollbackCoreAsync(TransactionActivitySource.OutcomeRolledBack));

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            if (!_completed)
            {
                await RollbackCoreAsync(TransactionActivitySource.OutcomeRolledBack).ConfigureAwait(false);
            }

            _disposed = true;
            await _transaction.DisposeAsync().ConfigureAwait(false);
        }

        // The rollback runs with CancellationToken.None: cleanup must finish even when the caller canceled.
        private async Task RollbackCoreAsync(string outcome)
        {
            try
            {
                await _transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // The transaction is already broken (for example the connection dropped); the database discards
                // it on its own, and the caller is already on a failure path.
            }
            finally
            {
                Complete(outcome);
            }
        }

        private void Complete(string outcome)
        {
            _completed = true;
            State.Close(outcome);
            _accessor.Release(this);
        }
    }

    private sealed class JoinerLease : IDbTransactionLease
    {
        private readonly TransactionLeaseState _state;
        private bool _disposed;

        internal JoinerLease(TransactionLeaseState state, IDbTransaction transaction)
        {
            _state = state;
            Transaction = transaction;
        }

        public bool IsOwner => false;

        public IDbTransaction Transaction { get; }

        public ValueTask<Either<EncinaError, Unit>> CommitAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(
                _disposed || _state.IsClosed
                    ? Left<EncinaError, Unit>(TransactionErrors.LeaseDisposed())
                    : Right<EncinaError, Unit>(Unit.Default));

        public ValueTask RollbackAsync(CancellationToken cancellationToken = default)
        {
            if (!_disposed)
            {
                _state.MarkRollbackOnly();
            }

            return ValueTask.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                _disposed = true;
                _state.Leave();
            }

            return ValueTask.CompletedTask;
        }
    }
}
