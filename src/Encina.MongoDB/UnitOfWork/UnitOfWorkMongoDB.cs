using System.Collections.Concurrent;
using Encina;
using Encina.DomainModeling;
using Encina.Messaging.Transactions;
using Encina.MongoDB.Repository;
using Encina.MongoDB.Transactions;
using LanguageExt;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using static LanguageExt.Prelude;

namespace Encina.MongoDB.UnitOfWork;

/// <summary>
/// MongoDB implementation of <see cref="IUnitOfWork"/> using client sessions.
/// </summary>
/// <remarks>
/// <para>
/// This implementation manages MongoDB transactions for coordinating changes across
/// multiple repositories using client sessions. Each repository operation executes
/// within the session context when a transaction is active.
/// </para>
/// <para>
/// <strong>Important:</strong> MongoDB transactions require a replica set deployment.
/// Standalone MongoDB servers do not support multi-document transactions, so
/// <c>BeginTransactionAsync</c> fails with a transaction-start error there.
/// For development, consider using a single-node replica set.
/// </para>
/// <para>
/// The unit of work does not own a private session: it takes a lease on the scope's
/// <see cref="IMongoSessionAccessor"/>, so it shares one transaction with the transactional outbox and the
/// messaging stores. This type is disposed asynchronously only (<see cref="IAsyncDisposable"/>).
/// </para>
/// <para>
/// The implementation:
/// <list type="bullet">
/// <item><description>Creates client sessions for transaction management</description></item>
/// <item><description>Caches repository instances by entity type for reuse</description></item>
/// <item><description>Automatically aborts uncommitted transactions on dispose</description></item>
/// <item><description>Returns Railway Oriented Programming results for all operations</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class TransferFundsHandler(IUnitOfWork unitOfWork)
/// {
///     public async Task&lt;Either&lt;EncinaError, Unit&gt;&gt; HandleAsync(TransferCommand cmd, CancellationToken ct)
///     {
///         var beginResult = await unitOfWork.BeginTransactionAsync(ct);
///         if (beginResult.IsLeft) return beginResult;
///
///         var accounts = unitOfWork.Repository&lt;Account, AccountId&gt;();
///
///         // Operations execute within the session context
///         var sourceResult = await accounts.GetByIdAsync(cmd.SourceId, ct);
///         if (sourceResult.IsLeft)
///         {
///             await unitOfWork.RollbackAsync(ct);
///             return sourceResult.Map(_ =&gt; Unit.Default);
///         }
///
///         // ... perform transfer ...
///
///         return await unitOfWork.CommitAsync(ct);
///     }
/// }
/// </code>
/// </example>
public sealed class UnitOfWorkMongoDB : IUnitOfWork
{
    private readonly IMongoDatabase _database;
    private readonly IMongoSessionAccessor _sessionAccessor;
    private readonly IServiceProvider _serviceProvider;
    private readonly ConcurrentDictionary<Type, object> _repositories = new();
    private ITransactionLease? _lease;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnitOfWorkMongoDB"/> class.
    /// </summary>
    /// <param name="mongoClient">The MongoDB client.</param>
    /// <param name="options">The MongoDB options containing database configuration.</param>
    /// <param name="sessionAccessor">
    /// The scope's session accessor: the unit of work takes a lease on the transaction the scope shares with the
    /// transactional outbox and the messaging stores, instead of owning a private session.
    /// </param>
    /// <param name="serviceProvider">The service provider for resolving dependencies.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="mongoClient"/>, <paramref name="options"/>,
    /// <paramref name="sessionAccessor"/> or <paramref name="serviceProvider"/> is null.
    /// </exception>
    public UnitOfWorkMongoDB(
        IMongoClient mongoClient,
        IOptions<EncinaMongoDbOptions> options,
        IMongoSessionAccessor sessionAccessor,
        IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(mongoClient);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(sessionAccessor);
        ArgumentNullException.ThrowIfNull(serviceProvider);

        _database = mongoClient.GetDatabase(options.Value.DatabaseName);
        _sessionAccessor = sessionAccessor;
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <see langword="true"/> while this unit of work holds a lease on the scope's transaction, whether it began
    /// the transaction or joined one the transactional outbox already opened.
    /// </remarks>
    public bool HasActiveTransaction => _lease is not null;

    /// <summary>
    /// Gets the current client session, if one is active.
    /// </summary>
    /// <remarks>
    /// This property is used internally by repositories to participate in the transaction.
    /// Returns null when no session is active.
    /// </remarks>
    internal IClientSessionHandle? CurrentSession => _sessionAccessor.CurrentSession;

    /// <summary>
    /// Gets the MongoDB database.
    /// </summary>
    /// <remarks>
    /// This property is used internally by repositories to access collections.
    /// </remarks>
    internal IMongoDatabase Database => _database;

    /// <inheritdoc/>
    public IFunctionalRepository<TEntity, TId> Repository<TEntity, TId>()
        where TEntity : class
        where TId : notnull
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var entityType = typeof(TEntity);

        return (IFunctionalRepository<TEntity, TId>)_repositories.GetOrAdd(
            entityType,
            _ =>
            {
                // Resolve the repository options from DI
                var options = _serviceProvider.GetService(typeof(MongoDbRepositoryOptions<TEntity, TId>))
                    as MongoDbRepositoryOptions<TEntity, TId>
                    ?? throw new InvalidOperationException(
                        $"No MongoDB repository options found for {typeof(TEntity).Name}. " +
                        $"Register options using services.AddEncinaRepository<{typeof(TEntity).Name}, {typeof(TId).Name}>(...) " +
                        $"with UseUnitOfWork enabled.");

                var collectionName = options.GetEffectiveCollectionName();
                var collection = _database.GetCollection<TEntity>(collectionName);
                var idProperty = options.IdProperty
                    ?? throw new InvalidOperationException(
                        $"IdProperty not configured for {typeof(TEntity).Name}.");

                return new UnitOfWorkRepositoryMongoDB<TEntity, TId>(
                    collection,
                    idProperty,
                    this);
            });
    }

    /// <inheritdoc/>
    /// <remarks>
    /// For MongoDB, changes are executed immediately within the session.
    /// This method is provided for API consistency but always returns 0
    /// since MongoDB does not track changes like EF Core.
    /// </remarks>
    public Task<Either<EncinaError, int>> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // MongoDB executes changes immediately - no tracking
        // Return 0 to indicate no pending changes were saved
        return Task.FromResult(Right<EncinaError, int>(0));
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, Unit>> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // TransactionAlreadyActive only when THIS unit of work holds a lease; a transaction another participant
        // of the scope owns is joined, not refused.
        if (_lease is not null)
        {
            return Left<EncinaError, Unit>(UnitOfWorkErrors.TransactionAlreadyActive());
        }

        try
        {
            var begun = await _sessionAccessor.BeginOrJoinAsync(cancellationToken).ConfigureAwait(false);

            return begun.Match(
                Right: lease =>
                {
                    _lease = lease;
                    return Right<EncinaError, Unit>(Unit.Default);
                },
                Left: error => Left<EncinaError, Unit>(
                    UnitOfWorkErrors.TransactionStartFailed(TransactionErrors.AsException(error))));
        }
        catch (Exception ex)
        {
            return Left<EncinaError, Unit>(UnitOfWorkErrors.TransactionStartFailed(ex));
        }
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, Unit>> CommitAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_lease is null)
        {
            return Left<EncinaError, Unit>(UnitOfWorkErrors.NoActiveTransaction());
        }

        // A joined lease's commit is a no-op; only the owner commits, and a rollback-only transaction is
        // aborted and refused.
        var committed = await _lease.CommitAsync(cancellationToken).ConfigureAwait(false);
        await ReleaseLeaseAsync().ConfigureAwait(false);

        return committed.MapLeft(error => UnitOfWorkErrors.CommitFailed(TransactionErrors.AsException(error)));
    }

    /// <inheritdoc/>
    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await RollbackInternalAsync().ConfigureAwait(false);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// This operation is not supported for MongoDB providers because they lack change tracking.
    /// Use <see cref="ImmutableAggregateHelper.PrepareForUpdate{TAggregate}"/> followed by the
    /// standard <c>UpdateAsync</c> method instead.
    /// </remarks>
    public Either<EncinaError, Unit> UpdateImmutable<TEntity>(TEntity modified) where TEntity : class
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(modified);

        return RepositoryErrors.OperationNotSupported<TEntity>("UpdateImmutable");
    }

    /// <inheritdoc/>
    /// <remarks>
    /// This operation is not supported for MongoDB providers because they lack change tracking.
    /// Use <see cref="ImmutableAggregateHelper.PrepareForUpdate{TAggregate}"/> followed by the
    /// standard <c>UpdateAsync</c> method instead.
    /// </remarks>
    public Task<Either<EncinaError, Unit>> UpdateImmutableAsync<TEntity>(
        TEntity modified,
        CancellationToken cancellationToken = default) where TEntity : class
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(modified);

        return Task.FromResult<Either<EncinaError, Unit>>(
            RepositoryErrors.OperationNotSupported<TEntity>("UpdateImmutableAsync"));
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        // Auto-abort uncommitted transaction
        await RollbackInternalAsync().ConfigureAwait(false);

        _repositories.Clear();

        GC.SuppressFinalize(this);
    }

    private async Task RollbackInternalAsync()
    {
        if (_lease is null)
        {
            return;
        }

        // The lease's rollback never throws and runs to completion; a joined lease marks the shared
        // transaction rollback-only instead.
        await _lease.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
        await ReleaseLeaseAsync().ConfigureAwait(false);
    }

    private async Task ReleaseLeaseAsync()
    {
        var lease = _lease;
        _lease = null;

        if (lease is not null)
        {
            await lease.DisposeAsync().ConfigureAwait(false);
        }
    }
}
