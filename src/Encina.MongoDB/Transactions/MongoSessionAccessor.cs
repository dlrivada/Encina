using System.Diagnostics.CodeAnalysis;
using Encina.Messaging.Diagnostics;
using Encina.Messaging.Transactions;
using LanguageExt;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using static LanguageExt.Prelude;

namespace Encina.MongoDB.Transactions;

/// <summary>
/// Scoped <see cref="IMongoSessionAccessor"/>: owns the <see cref="IClientSessionHandle"/> and its transaction for
/// the scope and shares them between the unit of work, the transactional outbox and the messaging stores.
/// </summary>
/// <remarks>
/// <para>
/// The first <c>BeginOrJoinAsync</c> starts a session and a transaction and returns the owning lease; every later
/// call joins: its lease commits as a no-op and rolls back by marking the transaction rollback-only, so the
/// owner's commit then aborts and returns <see cref="TransactionErrors.RollbackOnlyCode"/>.
/// </para>
/// <para>
/// <b>Transaction options.</b> Only the read preference is set (<see cref="ReadPreference.Primary"/>, the one
/// the server requires inside a transaction); the read and write concern are inherited from the client, so the
/// application's durability choice is respected. A client configured with read concern <c>available</c> or
/// <c>linearizable</c> is rejected by the server inside a transaction.
/// </para>
/// <para>
/// <b>Standalone server.</b> Transactions need a replica set or a sharded cluster. On a standalone server the
/// lease is refused with <see cref="TransactionErrors.NotSupportedCode"/> before any transaction starts (fail
/// closed).
/// </para>
/// <para>
/// <b>Commit failures.</b> A failed commit aborts the transaction and returns
/// <see cref="TransactionErrors.CommitFailedCode"/> carrying the driver's error label
/// (<c>TransientTransactionError</c> or <c>UnknownTransactionCommitResult</c>) as metadata. The accessor never
/// retries the commit: whether re-running the request is safe is the application's decision. A transaction is also
/// bounded by the server's <c>transactionLifetimeLimitSeconds</c> (60 s by default) and a write conflict surfaces
/// as a <c>TransientTransactionError</c>.
/// </para>
/// <para>
/// Abort cleanup runs with <see cref="CancellationToken.None"/>, so a canceled request still cleans up.
/// Disposing the accessor (the end of the scope) aborts a transaction nobody finished and disposes the session.
/// </para>
/// </remarks>
public sealed class MongoSessionAccessor : IMongoSessionAccessor, IAsyncDisposable, IDisposable
{
    internal const string MongoDbSystem = "mongodb";
    internal const string TransientTransactionErrorLabel = "TransientTransactionError";
    internal const string UnknownCommitResultLabel = "UnknownTransactionCommitResult";
    private const string StandaloneRejectionFragment = "Transaction numbers";

    private readonly IMongoClient _mongoClient;
    private readonly ILogger<MongoSessionAccessor> _logger;
    private OwnerLease? _owner;

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoSessionAccessor"/> class.
    /// </summary>
    /// <param name="mongoClient">The MongoDB client that starts the sessions.</param>
    /// <param name="logger">The logger.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="mongoClient"/> or <paramref name="logger"/> is null.</exception>
    public MongoSessionAccessor(IMongoClient mongoClient, ILogger<MongoSessionAccessor> logger)
    {
        ArgumentNullException.ThrowIfNull(mongoClient);
        ArgumentNullException.ThrowIfNull(logger);

        _mongoClient = mongoClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public bool HasActiveTransaction => _owner is not null;

    /// <inheritdoc />
    public string DbSystem => MongoDbSystem;

    /// <inheritdoc />
    public IClientSessionHandle? CurrentSession => _owner?.Session;

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, ITransactionLease>> BeginOrJoinAsync(CancellationToken cancellationToken = default)
    {
        if (_owner is { } owner)
        {
            return Join(owner);
        }

        return await BeginAsync(cancellationToken).ConfigureAwait(false);
    }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope",
        Justification = "The joiner lease is returned to the participant, which disposes it (await using); it holds no unmanaged resource.")]
    private static Either<EncinaError, ITransactionLease> Join(OwnerLease owner)
    {
        owner.State.Join();
        return Right<EncinaError, ITransactionLease>(new JoinerLease(owner.State));
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => _owner?.DisposeAsync() ?? ValueTask.CompletedTask;

    /// <summary>
    /// Ends a transaction nobody finished when the scope is disposed synchronously: the driver aborts an open
    /// transaction when its session is disposed.
    /// </summary>
    public void Dispose() => _owner?.DisposeSynchronously();

    /// <summary>
    /// Gets a value indicating whether <paramref name="exception"/> is the server's rejection of a transaction on a
    /// deployment that has no transaction support (a standalone server).
    /// </summary>
    internal static bool IsStandaloneRejection(Exception exception)
        => exception.Message.Contains(StandaloneRejectionFragment, StringComparison.Ordinal);

    internal static EncinaError StandaloneNotSupported()
        => EncinaErrors.Create(
            TransactionErrors.NotSupportedCode,
            "MongoDB transactions require a replica set or a sharded cluster; the server is a standalone deployment, so " +
            "the request cannot write its outbox messages atomically. Run a single-node replica set for development.");

    private async ValueTask<Either<EncinaError, ITransactionLease>> BeginAsync(CancellationToken cancellationToken)
    {
        IClientSessionHandle? session = null;
        try
        {
            session = await _mongoClient.StartSessionAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

            if (session.Client.Cluster.Description.Type == ClusterType.Standalone)
            {
                session.Dispose();
                Log.TransactionsUnavailable(_logger, TransactionErrors.NotSupportedCode);
                return Left<EncinaError, ITransactionLease>(StandaloneNotSupported());
            }

            session.StartTransaction(new TransactionOptions(readPreference: ReadPreference.Primary));

            var state = new TransactionLeaseState(MongoDbSystem, _logger);
            _owner = new OwnerLease(this, session, state);
            return Right<EncinaError, ITransactionLease>(_owner);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            session?.Dispose();
            throw;
        }
        catch (Exception ex)
        {
            session?.Dispose();
            return Left<EncinaError, ITransactionLease>(BeginFailure(ex));
        }
    }

    private EncinaError BeginFailure(Exception exception)
    {
        if (!IsStandaloneRejection(exception))
        {
            return TransactionErrors.BeginFailed(exception);
        }

        Log.TransactionsUnavailable(_logger, TransactionErrors.NotSupportedCode);
        return StandaloneNotSupported();
    }

    private void Release(OwnerLease owner)
    {
        if (ReferenceEquals(_owner, owner))
        {
            _owner = null;
        }
    }

    private sealed class OwnerLease : ITransactionLease
    {
        private readonly MongoSessionAccessor _accessor;
        private bool _completed;
        private bool _disposed;

        internal OwnerLease(MongoSessionAccessor accessor, IClientSessionHandle session, TransactionLeaseState state)
        {
            _accessor = accessor;
            Session = session;
            State = state;
        }

        internal IClientSessionHandle Session { get; }

        internal TransactionLeaseState State { get; }

        public bool IsOwner => true;

        public async ValueTask<Either<EncinaError, Unit>> CommitAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed || _completed)
            {
                return Left<EncinaError, Unit>(TransactionErrors.LeaseDisposed());
            }

            if (State.RollbackOnly)
            {
                State.RecordCommitRefused();
                await AbortAsync(TransactionActivitySource.OutcomeRollbackOnly).ConfigureAwait(false);
                return Left<EncinaError, Unit>(TransactionErrors.RollbackOnly());
            }

            try
            {
                await Session.CommitTransactionAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                State.RecordCommitFailed(ex);
                await AbortAsync(TransactionActivitySource.OutcomeCommitFailed).ConfigureAwait(false);
                return Left<EncinaError, Unit>(CommitFailure(ex));
            }

            Complete(TransactionActivitySource.OutcomeCommitted);
            return Right<EncinaError, Unit>(Unit.Default);
        }

        public ValueTask RollbackAsync(CancellationToken cancellationToken = default)
            => _disposed || _completed
                ? ValueTask.CompletedTask
                : new ValueTask(AbortAsync(TransactionActivitySource.OutcomeRolledBack));

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            if (!_completed)
            {
                await AbortAsync(TransactionActivitySource.OutcomeRolledBack).ConfigureAwait(false);
            }

            _disposed = true;
            Session.Dispose();
        }

        internal void DisposeSynchronously()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            Complete(_completed ? TransactionActivitySource.OutcomeCommitted : TransactionActivitySource.OutcomeRolledBack);

            // Disposing a session with an open transaction aborts it.
            Session.Dispose();
        }

        private EncinaError CommitFailure(Exception exception)
        {
            if (IsStandaloneRejection(exception))
            {
                Log.TransactionsUnavailable(_accessor._logger, TransactionErrors.NotSupportedCode);
                return StandaloneNotSupported();
            }

            var label = LabelOf(exception);
            if (label is not null)
            {
                Log.TransientTransactionLabel(_accessor._logger, label);
            }

            return TransactionErrors.CommitFailed(exception, label);
        }

        private static string? LabelOf(Exception exception)
        {
            if (exception is not MongoException mongo)
            {
                return null;
            }

            if (mongo.HasErrorLabel(UnknownCommitResultLabel))
            {
                return UnknownCommitResultLabel;
            }

            return mongo.HasErrorLabel(TransientTransactionErrorLabel) ? TransientTransactionErrorLabel : null;
        }

        // The abort runs with CancellationToken.None: cleanup must finish even when the caller canceled.
        private async Task AbortAsync(string outcome)
        {
            try
            {
                if (Session.IsInTransaction)
                {
                    await Session.AbortTransactionAsync(CancellationToken.None).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                // The transaction is already broken (for example the connection dropped); the server discards it
                // on its own, and the caller is already on a failure path. Log it, never rethrow.
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

            // The transaction has ended either way; release the server session now instead of at scope end.
            Session.Dispose();
        }
    }

    private sealed class JoinerLease : ITransactionLease
    {
        private readonly TransactionLeaseState _state;
        private bool _disposed;

        internal JoinerLease(TransactionLeaseState state) => _state = state;

        public bool IsOwner => false;

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
