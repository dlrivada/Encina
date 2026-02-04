using System.Collections.Concurrent;
using System.Collections.Immutable;
using Encina;
using Encina.DomainModeling;
using Encina.DomainModeling.Concurrency;
using Encina.EntityFrameworkCore.Extensions;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;
using static LanguageExt.Prelude;

namespace Encina.EntityFrameworkCore.UnitOfWork;

/// <summary>
/// Entity Framework Core implementation of <see cref="IUnitOfWork"/>.
/// </summary>
/// <remarks>
/// <para>
/// This implementation leverages EF Core's built-in change tracking and transaction management.
/// Repositories obtained from this Unit of Work share the same <see cref="DbContext"/> instance
/// and transaction context.
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
/// public class TransferFundsHandler
/// {
///     private readonly IUnitOfWork _unitOfWork;
///
///     public async Task&lt;Either&lt;EncinaError, Unit&gt;&gt; HandleAsync(TransferCommand cmd, CancellationToken ct)
///     {
///         var accounts = _unitOfWork.Repository&lt;Account, AccountId&gt;();
///
///         // Start transaction
///         var beginResult = await _unitOfWork.BeginTransactionAsync(ct);
///         if (beginResult.IsLeft) return beginResult;
///
///         // Get accounts
///         var sourceResult = await accounts.GetByIdAsync(cmd.SourceId, ct);
///         if (sourceResult.IsLeft)
///         {
///             await _unitOfWork.RollbackAsync(ct);
///             return sourceResult.Map(_ =&gt; Unit.Default);
///         }
///
///         var targetResult = await accounts.GetByIdAsync(cmd.TargetId, ct);
///         if (targetResult.IsLeft)
///         {
///             await _unitOfWork.RollbackAsync(ct);
///             return targetResult.Map(_ =&gt; Unit.Default);
///         }
///
///         // Perform transfer
///         sourceResult.IfRight(source =&gt; source.Debit(cmd.Amount));
///         targetResult.IfRight(target =&gt; target.Credit(cmd.Amount));
///
///         // Save and commit
///         var saveResult = await _unitOfWork.SaveChangesAsync(ct);
///         if (saveResult.IsLeft)
///         {
///             await _unitOfWork.RollbackAsync(ct);
///             return saveResult.Map(_ =&gt; Unit.Default);
///         }
///
///         return await _unitOfWork.CommitAsync(ct);
///     }
/// }
/// </code>
/// </example>
public sealed class UnitOfWorkEF : IUnitOfWork
{
    private readonly DbContext _dbContext;
    private readonly IServiceProvider _serviceProvider;
    private readonly ConcurrentDictionary<Type, object> _repositories = new();
    private IDbContextTransaction? _transaction;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnitOfWorkEF"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    /// <param name="serviceProvider">The service provider for resolving dependencies.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="dbContext"/> or <paramref name="serviceProvider"/> is null.
    /// </exception>
    public UnitOfWorkEF(DbContext dbContext, IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(serviceProvider);

        _dbContext = dbContext;
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Gets the underlying database context for internal use.
    /// </summary>
    /// <remarks>
    /// This property is exposed internally to allow extension methods and helper classes
    /// to access the DbContext for operations like immutable entity updates.
    /// </remarks>
    internal DbContext DbContext => _dbContext;

    /// <inheritdoc/>
    public bool HasActiveTransaction => _transaction is not null;

    /// <inheritdoc/>
    public IFunctionalRepository<TEntity, TId> Repository<TEntity, TId>()
        where TEntity : class
        where TId : notnull
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var entityType = typeof(TEntity);

        return (IFunctionalRepository<TEntity, TId>)_repositories.GetOrAdd(
            entityType,
            _ => new UnitOfWorkRepositoryEF<TEntity, TId>(_dbContext));
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, int>> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        try
        {
            var affectedRows = await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Right<EncinaError, int>(affectedRows);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            var conflictDetails = await ExtractConflictDetailsAsync(ex, cancellationToken).ConfigureAwait(false);
            return Left<EncinaError, int>(
                UnitOfWorkErrors.SaveChangesFailed(ex, conflictDetails.EntityTypeNames, conflictDetails.Details));
        }
        catch (DbUpdateException ex)
        {
            return Left<EncinaError, int>(UnitOfWorkErrors.SaveChangesFailed(ex));
        }
        catch (Exception ex)
        {
            return Left<EncinaError, int>(UnitOfWorkErrors.SaveChangesFailed(ex));
        }
    }

    /// <summary>
    /// Extracts detailed conflict information from a concurrency exception.
    /// </summary>
    private static async Task<(IReadOnlyList<string> EntityTypeNames, ImmutableDictionary<string, object?> Details)> ExtractConflictDetailsAsync(
        DbUpdateConcurrencyException ex,
        CancellationToken cancellationToken)
    {
        var entityTypeNames = ex.Entries
            .Select(e => e.Entity.GetType().Name)
            .Distinct()
            .ToList();

        var detailsBuilder = ImmutableDictionary.CreateBuilder<string, object?>();
        detailsBuilder.Add("ConflictingEntityTypes", entityTypeNames);

        // Try to extract detailed information about the first conflicting entry
        var firstEntry = ex.Entries.Count > 0 ? ex.Entries[0] : null;
        if (firstEntry is not null)
        {
            detailsBuilder.Add("FirstConflictEntityType", firstEntry.Entity.GetType().FullName);

            try
            {
                // Get proposed values (what we're trying to save)
                var proposedValues = GetPropertyValues(firstEntry.CurrentValues);
                detailsBuilder.Add(ConcurrencyConflictInfo<object>.ProposedEntityKey, proposedValues);

                // Get original values (what we started with)
                var originalValues = GetPropertyValues(firstEntry.OriginalValues);
                detailsBuilder.Add(ConcurrencyConflictInfo<object>.CurrentEntityKey, originalValues);

                // Try to reload to get database values
                await firstEntry.ReloadAsync(cancellationToken).ConfigureAwait(false);
                var databaseValues = GetPropertyValues(firstEntry.CurrentValues);
                detailsBuilder.Add(ConcurrencyConflictInfo<object>.DatabaseEntityKey, databaseValues);
            }
            catch
            {
                // Entity may have been deleted, or reload failed
                detailsBuilder.Add(ConcurrencyConflictInfo<object>.DatabaseEntityKey, null);
            }
        }

        return (entityTypeNames, detailsBuilder.ToImmutable());
    }

    /// <summary>
    /// Extracts property values from an entry as a dictionary.
    /// </summary>
    private static Dictionary<string, object?> GetPropertyValues(PropertyValues values)
    {
        var result = new Dictionary<string, object?>();
        foreach (var property in values.Properties)
        {
            result[property.Name] = values[property];
        }
        return result;
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
            _transaction = await _dbContext.Database
                .BeginTransactionAsync(cancellationToken)
                .ConfigureAwait(false);

            return Right<EncinaError, Unit>(Unit.Default);
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

        if (_transaction is null)
        {
            return Left<EncinaError, Unit>(UnitOfWorkErrors.NoActiveTransaction());
        }

        try
        {
            await _transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            await DisposeTransactionAsync().ConfigureAwait(false);

            return Right<EncinaError, Unit>(Unit.Default);
        }
        catch (Exception ex)
        {
            await RollbackInternalAsync().ConfigureAwait(false);
            return Left<EncinaError, Unit>(UnitOfWorkErrors.CommitFailed(ex));
        }
    }

    /// <inheritdoc/>
    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await RollbackInternalAsync().ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Either<EncinaError, Unit> UpdateImmutable<TEntity>(TEntity modified) where TEntity : class
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(modified);

        return _dbContext.UpdateImmutable(modified);
    }

    /// <inheritdoc/>
    public Task<Either<EncinaError, Unit>> UpdateImmutableAsync<TEntity>(
        TEntity modified,
        CancellationToken cancellationToken = default) where TEntity : class
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(modified);

        return _dbContext.UpdateImmutableAsync(modified, cancellationToken);
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

        // Clear tracked entities
        _dbContext.ChangeTracker.Clear();

        _repositories.Clear();

        GC.SuppressFinalize(this);
    }

    private async Task RollbackInternalAsync()
    {
        if (_transaction is null)
        {
            return;
        }

        try
        {
            await _transaction.RollbackAsync().ConfigureAwait(false);
        }
        catch
        {
            // Swallow rollback exceptions - we're already in an error path
        }
        finally
        {
            await DisposeTransactionAsync().ConfigureAwait(false);
        }
    }

    private async Task DisposeTransactionAsync()
    {
        if (_transaction is null)
        {
            return;
        }

        await _transaction.DisposeAsync().ConfigureAwait(false);
        _transaction = null;
    }
}
