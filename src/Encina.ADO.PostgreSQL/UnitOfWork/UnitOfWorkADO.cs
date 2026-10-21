using System.Collections.Concurrent;
using System.Data;
using Encina;
using Encina.ADO.PostgreSQL.Repository;
using Encina.DomainModeling;
using Encina.Messaging.Transactions;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.ADO.PostgreSQL.UnitOfWork;

/// <summary>
/// ADO.NET implementation of <see cref="IUnitOfWork"/> for PostgreSQL.
/// </summary>
/// <remarks>
/// <para>
/// This implementation manages database transactions for coordinating changes across
/// multiple repositories using raw ADO.NET. Each repository operation executes
/// immediately against the database within the transaction context.
/// </para>
/// <para>
/// The implementation:
/// <list type="bullet">
/// <item><description>Caches repository instances by entity type for reuse</description></item>
/// <item><description>Manages database transactions with proper rollback on errors</description></item>
/// <item><description>Automatically rolls back uncommitted transactions on dispose</description></item>
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
///         // Operations execute immediately within transaction
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
public sealed class UnitOfWorkADO : IUnitOfWork
{
    private readonly IDbTransactionAccessor _transactionAccessor;
    private readonly IServiceProvider _serviceProvider;
    private readonly ConcurrentDictionary<Type, object> _repositories = new();
    private IDbTransactionLease? _lease;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnitOfWorkADO"/> class.
    /// </summary>
    /// <param name="transactionAccessor">
    /// The scope's transaction accessor: the unit of work takes a lease on the transaction the scope shares
    /// with the transaction behavior and the messaging stores, instead of owning a private one.
    /// </param>
    /// <param name="serviceProvider">The service provider for resolving dependencies.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="transactionAccessor"/> or <paramref name="serviceProvider"/> is null.
    /// </exception>
    public UnitOfWorkADO(IDbTransactionAccessor transactionAccessor, IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(transactionAccessor);
        ArgumentNullException.ThrowIfNull(serviceProvider);

        _transactionAccessor = transactionAccessor;
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <see langword="true"/> while this unit of work holds a lease on the scope's transaction, whether it
    /// began the transaction or joined one a transaction behavior already opened.
    /// </remarks>
    public bool HasActiveTransaction => _lease is not null;

    /// <summary>
    /// Gets the current transaction of the scope, if one is active.
    /// </summary>
    /// <remarks>
    /// This property is used internally by repositories to participate in the transaction.
    /// </remarks>
    internal IDbTransaction? CurrentTransaction => _transactionAccessor.Current;

    /// <summary>
    /// Gets the database connection.
    /// </summary>
    /// <remarks>
    /// This property is used internally by repositories for database operations.
    /// </remarks>
    internal IDbConnection Connection => _transactionAccessor.Connection;

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
                // Resolve the mapping from DI
                var mapping = _serviceProvider.GetService(typeof(IEntityMapping<TEntity, TId>))
                    as IEntityMapping<TEntity, TId>
                    ?? throw new InvalidOperationException(
                        $"No entity mapping found for {typeof(TEntity).Name}. " +
                        $"Register a mapping using services.AddEncinaRepository<{typeof(TEntity).Name}, {typeof(TId).Name}>(...)");

                return new UnitOfWorkRepositoryADO<TEntity, TId>(Connection, mapping, this);
            });
    }

    /// <inheritdoc/>
    /// <remarks>
    /// For ADO.NET, changes are executed immediately within the transaction.
    /// This method is provided for API consistency but always returns 0
    /// since ADO.NET does not track changes.
    /// </remarks>
    public Task<Either<EncinaError, int>> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // ADO.NET executes changes immediately - no tracking
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
            var begun = await _transactionAccessor.BeginOrJoinAsync(isolationLevel: null, cancellationToken).ConfigureAwait(false);

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
        // rolled back and refused.
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
    /// This operation is not supported for ADO.NET providers because they lack change tracking.
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
    /// This operation is not supported for ADO.NET providers because they lack change tracking.
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

        // Auto-rollback uncommitted transaction
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
