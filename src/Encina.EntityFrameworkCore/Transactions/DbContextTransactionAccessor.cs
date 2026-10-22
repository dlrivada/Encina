using System.Data;
using System.Data.Common;
using Encina.Messaging.Diagnostics;
using Encina.Messaging.Transactions;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace Encina.EntityFrameworkCore.Transactions;

/// <summary>
/// Scoped <see cref="IDbTransactionAccessor"/> for Entity Framework Core: owns the
/// <see cref="IDbContextTransaction"/> of the scope's <see cref="DbContext"/> and lets every participant of the
/// scope (transaction behavior, unit of work, transactional outbox) share it.
/// </summary>
/// <remarks>
/// <para>
/// The first <c>BeginOrJoinAsync</c> begins the transaction through <c>Database.BeginTransactionAsync</c>; the
/// lease it returns owns it. Every later call joins: its lease commits as a no-op and rolls back by marking the
/// transaction rollback-only, so the owner's commit then rolls back and returns
/// <see cref="TransactionErrors.RollbackOnlyCode"/>.
/// </para>
/// <para>
/// <b>Application-begun transactions.</b> When the application began <c>Database.CurrentTransaction</c> itself,
/// the lease is non-owning: Encina never commits it, and a participant that rolls back really rolls it back
/// (fail closed: a failed request must leave no outbox row and no business change).
/// </para>
/// <para>
/// <b>Retrying execution strategy.</b> A transaction Encina would begin on a context whose execution strategy
/// retries on failure (<c>EnableRetryOnFailure</c>) is refused with <see cref="TransactionErrors.NotSupportedCode"/>:
/// EF Core only allows a user-initiated transaction inside <c>strategy.ExecuteAsync</c>, and re-running the
/// business handler is the application's decision. Begin the transaction yourself, or disable the retrying
/// strategy for contexts that use the transactional outbox.
/// </para>
/// <para>
/// Rollback cleanup runs with <see cref="CancellationToken.None"/>, so a canceled request still cleans up.
/// Disposing the accessor (the end of the scope) rolls back a transaction nobody finished.
/// </para>
/// </remarks>
public sealed class DbContextTransactionAccessor : IDbTransactionAccessor, IAsyncDisposable, IDisposable
{
    private readonly DbContext _dbContext;
    private readonly ILogger<DbContextTransactionAccessor> _logger;
    private OwnerLease? _owner;
    private DbTransaction? _externalRolledBack;

    /// <summary>
    /// Initializes a new instance of the <see cref="DbContextTransactionAccessor"/> class.
    /// </summary>
    /// <param name="dbContext">The scope's database context.</param>
    /// <param name="logger">The logger.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="dbContext"/> or <paramref name="logger"/> is null.</exception>
    public DbContextTransactionAccessor(DbContext dbContext, ILogger<DbContextTransactionAccessor> logger)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(logger);

        _dbContext = dbContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public IDbConnection Connection => _dbContext.Database.GetDbConnection();

    /// <inheritdoc />
    public IDbTransaction? Current => _dbContext.Database.CurrentTransaction?.GetDbTransaction();

    /// <inheritdoc />
    public bool HasActiveTransaction => _dbContext.Database.CurrentTransaction is not null;

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
        if (_owner is { } owner)
        {
            return Join(owner, isolationLevel);
        }

        // A participant rolled the application's transaction back: fail closed for the rest of the scope instead
        // of beginning a new transaction that would commit on its own.
        if (_externalRolledBack is not null)
        {
            return Left<EncinaError, IDbTransactionLease>(TransactionErrors.RollbackOnly());
        }

        return _dbContext.Database.CurrentTransaction is { } external
            ? JoinExternal(external, isolationLevel)
            : await BeginAsync(isolationLevel, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => _owner?.DisposeAsync() ?? ValueTask.CompletedTask;

    /// <summary>
    /// Ends a transaction nobody finished when the scope is disposed synchronously. Disposing an EF Core
    /// transaction that was not committed discards it, which is a rollback, so nothing is left open.
    /// </summary>
    public void Dispose() => _owner?.DisposeSynchronously();

    private static bool Mismatches(IsolationLevel? requested, IsolationLevel active)
        => requested is { } level && level != IsolationLevel.Unspecified && level != active;

    private static Either<EncinaError, IDbTransactionLease> Join(OwnerLease owner, IsolationLevel? requested)
    {
        var active = owner.Transaction.IsolationLevel;
        if (Mismatches(requested, active))
        {
            return Left<EncinaError, IDbTransactionLease>(TransactionErrors.IsolationLevelMismatch(requested!.Value, active));
        }

        owner.State.Join();
        return Right<EncinaError, IDbTransactionLease>(new JoinerLease(owner.State, owner.Transaction));
    }

    private Either<EncinaError, IDbTransactionLease> JoinExternal(IDbContextTransaction external, IsolationLevel? requested)
    {
        var dbTransaction = external.GetDbTransaction();
        var active = dbTransaction.IsolationLevel;
        if (Mismatches(requested, active))
        {
            return Left<EncinaError, IDbTransactionLease>(TransactionErrors.IsolationLevelMismatch(requested!.Value, active));
        }

        Log.ExternalTransactionJoined(_logger, active);
        return Right<EncinaError, IDbTransactionLease>(new ExternalLease(this, external, dbTransaction));
    }

    private async ValueTask<Either<EncinaError, IDbTransactionLease>> BeginAsync(
        IsolationLevel? isolationLevel,
        CancellationToken cancellationToken)
    {
        var strategy = _dbContext.Database.CreateExecutionStrategy();
        if (strategy.RetriesOnFailure)
        {
            Log.ExecutionStrategyRejectsTransaction(_logger, strategy.GetType().Name);
            return Left<EncinaError, IDbTransactionLease>(RetryingStrategyRejected());
        }

        try
        {
            var transaction = isolationLevel is { } level
                ? await _dbContext.Database.BeginTransactionAsync(level, cancellationToken).ConfigureAwait(false)
                : await _dbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

            var state = new TransactionLeaseState(DbSystemName.From(Connection), _logger);
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

    private static EncinaError RetryingStrategyRejected()
        => EncinaErrors.Create(
            TransactionErrors.NotSupportedCode,
            "The DbContext uses a retrying execution strategy (EnableRetryOnFailure), which does not allow Encina to begin a " +
            "transaction: retrying would re-run the request handler. Disable the retrying strategy for contexts that use the " +
            "transactional outbox, or begin the transaction yourself and Encina will join it.");

    private void Release(OwnerLease owner)
    {
        if (ReferenceEquals(_owner, owner))
        {
            _owner = null;
        }
    }

    private sealed class OwnerLease : IDbTransactionLease
    {
        private readonly DbContextTransactionAccessor _accessor;
        private readonly IDbContextTransaction _transaction;
        private bool _completed;
        private bool _disposed;

        internal OwnerLease(DbContextTransactionAccessor accessor, IDbContextTransaction transaction, TransactionLeaseState state)
        {
            _accessor = accessor;
            _transaction = transaction;
            Transaction = transaction.GetDbTransaction();
            State = state;
        }

        internal TransactionLeaseState State { get; }

        public bool IsOwner => true;

        public IDbTransaction Transaction { get; }

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

        internal void DisposeSynchronously()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            Complete(_completed ? TransactionActivitySource.OutcomeCommitted : TransactionActivitySource.OutcomeRolledBack);
            _transaction.Dispose();
        }

        // The rollback runs with CancellationToken.None: cleanup must finish even when the caller canceled.
        private async Task RollbackCoreAsync(string outcome)
        {
            try
            {
                await _transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // The transaction is already broken (for example the connection dropped); the database discards
                // it on its own, and the caller is already on a failure path. Log it, never rethrow.
                State.RecordRollbackFailed(ex);
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

    // A lease on a transaction the application began: Encina never commits it, but a rollback is real.
    private sealed class ExternalLease : IDbTransactionLease
    {
        private readonly DbContextTransactionAccessor _accessor;
        private readonly IDbContextTransaction _transaction;
        private bool _disposed;

        internal ExternalLease(DbContextTransactionAccessor accessor, IDbContextTransaction transaction, DbTransaction dbTransaction)
        {
            _accessor = accessor;
            _transaction = transaction;
            Transaction = dbTransaction;
        }

        public bool IsOwner => false;

        public IDbTransaction Transaction { get; }

        public ValueTask<Either<EncinaError, Unit>> CommitAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed)
            {
                return ValueTask.FromResult(Left<EncinaError, Unit>(TransactionErrors.LeaseDisposed()));
            }

            // Some participant rolled the application's transaction back: the request must not report success.
            return ValueTask.FromResult(
                ReferenceEquals(_accessor._externalRolledBack, Transaction)
                    ? Left<EncinaError, Unit>(TransactionErrors.RollbackOnly())
                    : Right<EncinaError, Unit>(Unit.Default));
        }

        public async ValueTask RollbackAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed || ReferenceEquals(_accessor._externalRolledBack, Transaction))
            {
                return;
            }

            _accessor._externalRolledBack = (DbTransaction)Transaction;

            try
            {
                await _transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // The transaction is already broken; the database discards it and the caller is on a failure path.
            }
        }

        public ValueTask DisposeAsync()
        {
            _disposed = true;
            return ValueTask.CompletedTask;
        }
    }
}
