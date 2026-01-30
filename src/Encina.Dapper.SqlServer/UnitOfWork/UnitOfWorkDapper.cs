using System.Collections.Concurrent;
using System.Data;
using Encina;
using Encina.Dapper.SqlServer.Repository;
using Encina.DomainModeling;
using LanguageExt;
using Microsoft.Data.SqlClient;
using static LanguageExt.Prelude;

namespace Encina.Dapper.SqlServer.UnitOfWork;

/// <summary>
/// Dapper implementation of <see cref="IUnitOfWork"/> for SQL Server.
/// </summary>
/// <remarks>
/// <para>
/// This implementation manages database transactions for coordinating changes across
/// multiple repositories. Unlike EF Core, Dapper does not track changes, so each
/// repository operation executes immediately against the database within the
/// transaction context.
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
public sealed class UnitOfWorkDapper : IUnitOfWork
{
    private readonly IDbConnection _connection;
    private readonly IServiceProvider _serviceProvider;
    private readonly ConcurrentDictionary<Type, object> _repositories = new();
    private IDbTransaction? _transaction;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnitOfWorkDapper"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="serviceProvider">The service provider for resolving dependencies.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="connection"/> or <paramref name="serviceProvider"/> is null.
    /// </exception>
    public UnitOfWorkDapper(IDbConnection connection, IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(serviceProvider);

        _connection = connection;
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc/>
    public bool HasActiveTransaction => _transaction is not null;

    /// <summary>
    /// Gets the current transaction, if one is active.
    /// </summary>
    /// <remarks>
    /// This property is used internally by repositories to participate in the transaction.
    /// </remarks>
    internal IDbTransaction? CurrentTransaction => _transaction;

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

                return new UnitOfWorkRepositoryDapper<TEntity, TId>(_connection, mapping, this);
            });
    }

    /// <inheritdoc/>
    /// <remarks>
    /// For Dapper, changes are executed immediately within the transaction.
    /// This method is provided for API consistency but always returns 0
    /// since Dapper does not track changes.
    /// </remarks>
    public Task<Either<EncinaError, int>> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // Dapper executes changes immediately - no tracking
        // Return 0 to indicate no pending changes were saved
        return Task.FromResult(Right<EncinaError, int>(0));
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, Unit>> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_transaction is not null)
        {
            return Left<EncinaError, Unit>(UnitOfWorkErrors.TransactionAlreadyActive());
        }

        try
        {
            await EnsureConnectionOpenAsync(cancellationToken).ConfigureAwait(false);
            _transaction = _connection.BeginTransaction();

            return Right<EncinaError, Unit>(Unit.Default);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, Unit>(UnitOfWorkErrors.TransactionStartFailed(ex));
        }
    }

    /// <inheritdoc/>
    public Task<Either<EncinaError, Unit>> CommitAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_transaction is null)
        {
            return Task.FromResult(Left<EncinaError, Unit>(UnitOfWorkErrors.NoActiveTransaction()));
        }

        try
        {
            _transaction.Commit();
            DisposeTransaction();

            return Task.FromResult(Right<EncinaError, Unit>(Unit.Default));
        }
        catch (Exception ex)
        {
            RollbackInternal();
            return Task.FromResult(Left<EncinaError, Unit>(UnitOfWorkErrors.CommitFailed(ex)));
        }
    }

    /// <inheritdoc/>
    public Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        RollbackInternal();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// This operation is not supported for Dapper providers because they lack change tracking.
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
    /// This operation is not supported for Dapper providers because they lack change tracking.
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
    public ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return ValueTask.CompletedTask;
        }

        _disposed = true;

        // Auto-rollback uncommitted transaction
        RollbackInternal();

        _repositories.Clear();

        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    private async Task EnsureConnectionOpenAsync(CancellationToken cancellationToken)
    {
        if (_connection.State == ConnectionState.Open)
            return;

        if (_connection is SqlConnection sqlConnection)
        {
            await sqlConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await Task.Run(_connection.Open, cancellationToken).ConfigureAwait(false);
        }
    }

    private void RollbackInternal()
    {
        if (_transaction is null)
        {
            return;
        }

        try
        {
            _transaction.Rollback();
        }
        catch
        {
            // Swallow rollback exceptions - we're already in an error path
        }
        finally
        {
            DisposeTransaction();
        }
    }

    private void DisposeTransaction()
    {
        _transaction?.Dispose();
        _transaction = null;
    }
}
