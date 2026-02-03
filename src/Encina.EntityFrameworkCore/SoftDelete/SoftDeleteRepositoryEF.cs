using Encina.DomainModeling;
using Encina.EntityFrameworkCore.Repository;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using static LanguageExt.Prelude;

namespace Encina.EntityFrameworkCore.SoftDelete;

/// <summary>
/// Entity Framework Core implementation of <see cref="ISoftDeleteRepository{TEntity, TId}"/>
/// with support for soft delete operations.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TId">The entity identifier type.</typeparam>
/// <remarks>
/// <para>
/// This repository extends the standard repository pattern with soft delete operations:
/// <list type="bullet">
/// <item><description><see cref="ListWithDeletedAsync"/>: Query including soft-deleted entities</description></item>
/// <item><description><see cref="GetByIdWithDeletedAsync"/>: Retrieve entity even if soft-deleted</description></item>
/// <item><description><see cref="RestoreAsync"/>: Restore a soft-deleted entity</description></item>
/// <item><description><see cref="HardDeleteAsync"/>: Permanently delete an entity</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Note</b>: This repository requires entities to implement <see cref="ISoftDeletableEntity"/>
/// (with public setters) to support the restore operation. Entities implementing only
/// <see cref="ISoftDeletable"/> (getter-only) should use domain methods for restoration.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class OrderService
/// {
///     private readonly ISoftDeleteRepository&lt;Order, OrderId&gt; _repository;
///
///     public async Task&lt;Either&lt;RepositoryError, Order&gt;&gt; RestoreOrderAsync(OrderId id, CancellationToken ct)
///     {
///         return await _repository.RestoreAsync(id, ct);
///     }
///
///     public async Task&lt;Either&lt;RepositoryError, IReadOnlyList&lt;Order&gt;&gt;&gt; GetAllOrdersIncludingDeletedAsync(
///         Specification&lt;Order&gt; spec, CancellationToken ct)
///     {
///         return await _repository.ListWithDeletedAsync(spec, ct);
///     }
/// }
/// </code>
/// </example>
public sealed class SoftDeleteRepositoryEF<TEntity, TId> : ISoftDeleteRepository<TEntity, TId>
    where TEntity : class, IEntity<TId>, ISoftDeletable
    where TId : notnull
{
    private readonly DbContext _dbContext;
    private readonly DbSet<TEntity> _dbSet;

    /// <summary>
    /// Initializes a new instance of the <see cref="SoftDeleteRepositoryEF{TEntity, TId}"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="dbContext"/> is null.</exception>
    public SoftDeleteRepositoryEF(DbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
        _dbSet = dbContext.Set<TEntity>();
    }

    #region IReadOnlyRepository Implementation

    /// <inheritdoc/>
    public async Task<Option<TEntity>> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbSet.FindAsync([id], cancellationToken).ConfigureAwait(false);
        return entity is not null ? Some(entity) : None;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<TEntity>> FindAsync(
        Specification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(specification);

        var query = SpecificationEvaluator.GetQuery(_dbSet.AsQueryable(), specification);
        return await query.ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<Option<TEntity>> FindOneAsync(
        Specification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(specification);

        var query = SpecificationEvaluator.GetQuery(_dbSet.AsQueryable(), specification);
        var entity = await query.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        return entity is not null ? Some(entity) : None;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<TEntity>> FindAsync(
        System.Linq.Expressions.Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        return await _dbSet
            .Where(predicate)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<bool> AnyAsync(
        Specification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(specification);

        return await _dbSet
            .AnyAsync(specification.ToExpression(), cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<bool> AnyAsync(
        System.Linq.Expressions.Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        return await _dbSet
            .AnyAsync(predicate, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<int> CountAsync(
        Specification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(specification);

        return await _dbSet
            .CountAsync(specification.ToExpression(), cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.CountAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<PagedResult<TEntity>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var totalCount = await _dbSet.CountAsync(cancellationToken).ConfigureAwait(false);

        var items = await _dbSet
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResult<TEntity>(items, pageNumber, pageSize, totalCount);
    }

    /// <inheritdoc/>
    public async Task<PagedResult<TEntity>> GetPagedAsync(
        Specification<TEntity> specification,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(specification);

        var baseQuery = _dbSet.Where(specification.ToExpression());
        var totalCount = await baseQuery.CountAsync(cancellationToken).ConfigureAwait(false);

        var query = SpecificationEvaluator.GetQuery(_dbSet.AsQueryable(), specification);
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResult<TEntity>(items, pageNumber, pageSize, totalCount);
    }

    #endregion

    #region IRepository Implementation

    /// <inheritdoc/>
    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        await _dbSet.AddAsync(entity, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entities);
        await _dbSet.AddRangeAsync(entities, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public void Update(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        _dbSet.Update(entity);
    }

    /// <inheritdoc/>
    public void UpdateRange(IEnumerable<TEntity> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);
        _dbSet.UpdateRange(entities);
    }

    /// <inheritdoc/>
    public void Remove(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        // This will trigger soft delete via the SoftDeleteInterceptor if the entity implements ISoftDeletableEntity
        _dbSet.Remove(entity);
    }

    /// <inheritdoc/>
    public void RemoveRange(IEnumerable<TEntity> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);
        // This will trigger soft delete via the SoftDeleteInterceptor for entities implementing ISoftDeletableEntity
        _dbSet.RemoveRange(entities);
    }

    /// <inheritdoc/>
    public async Task<bool> RemoveByIdAsync(TId id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbSet.FindAsync([id], cancellationToken).ConfigureAwait(false);

        if (entity is null)
        {
            return false;
        }

        // This will trigger soft delete via the SoftDeleteInterceptor if the entity implements ISoftDeletableEntity
        _dbSet.Remove(entity);
        return true;
    }

    #endregion

    #region ISoftDeleteRepository Implementation

    /// <inheritdoc/>
    public async Task<Either<RepositoryError, IReadOnlyList<TEntity>>> ListWithDeletedAsync(
        Specification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(specification);

        try
        {
            var query = SpecificationEvaluator.GetQuery(
                _dbSet.IgnoreQueryFilters().AsQueryable(),
                specification);

            var entities = await query.ToListAsync(cancellationToken).ConfigureAwait(false);

            return Right<RepositoryError, IReadOnlyList<TEntity>>(entities);
        }
        catch (Exception ex)
        {
            return Left<RepositoryError, IReadOnlyList<TEntity>>(
                RepositoryError.OperationFailed<TEntity>("ListWithDeleted", ex));
        }
    }

    /// <inheritdoc/>
    public async Task<Either<RepositoryError, TEntity>> GetByIdWithDeletedAsync(
        TId id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _dbSet
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(e => EF.Property<TId>(e, "Id")!.Equals(id), cancellationToken)
                .ConfigureAwait(false);

            if (entity is null)
            {
                return Left<RepositoryError, TEntity>(
                    RepositoryError.NotFound<TEntity, TId>(id));
            }

            return Right<RepositoryError, TEntity>(entity);
        }
        catch (Exception ex)
        {
            return Left<RepositoryError, TEntity>(
                RepositoryError.OperationFailed<TEntity>("GetByIdWithDeleted", ex));
        }
    }

    /// <inheritdoc/>
    public async Task<Either<RepositoryError, TEntity>> RestoreAsync(
        TId id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // First, get the entity including soft-deleted
            var entity = await _dbSet
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(e => EF.Property<TId>(e, "Id")!.Equals(id), cancellationToken)
                .ConfigureAwait(false);

            if (entity is null)
            {
                return Left<RepositoryError, TEntity>(
                    RepositoryError.NotFound<TEntity, TId>(id));
            }

            if (!entity.IsDeleted)
            {
                return Left<RepositoryError, TEntity>(
                    new RepositoryError(
                        $"Entity of type '{typeof(TEntity).Name}' with ID '{id}' is not soft-deleted and cannot be restored",
                        "REPOSITORY_INVALID_OPERATION",
                        typeof(TEntity),
                        id));
            }

            // Restore the entity - requires ISoftDeletableEntity (with setters)
            if (entity is ISoftDeletableEntity softDeletableEntity)
            {
                softDeletableEntity.IsDeleted = false;
                softDeletableEntity.DeletedAtUtc = null;
                softDeletableEntity.DeletedBy = null;

                await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                return Right<RepositoryError, TEntity>(entity);
            }

            // Entity only implements ISoftDeletable (getter-only), cannot restore via interceptor
            return Left<RepositoryError, TEntity>(
                new RepositoryError(
                    $"Entity of type '{typeof(TEntity).Name}' implements ISoftDeletable but not ISoftDeletableEntity. Use domain methods to restore.",
                    "REPOSITORY_INVALID_OPERATION",
                    typeof(TEntity),
                    id));
        }
        catch (DbUpdateConcurrencyException)
        {
            return Left<RepositoryError, TEntity>(
                RepositoryError.ConcurrencyConflict<TEntity, TId>(id));
        }
        catch (Exception ex)
        {
            return Left<RepositoryError, TEntity>(
                RepositoryError.OperationFailed<TEntity>("Restore", ex));
        }
    }

    /// <inheritdoc/>
    public async Task<Either<RepositoryError, Unit>> HardDeleteAsync(
        TId id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get the entity including soft-deleted ones
            var entity = await _dbSet
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(e => EF.Property<TId>(e, "Id")!.Equals(id), cancellationToken)
                .ConfigureAwait(false);

            if (entity is null)
            {
                return Left<RepositoryError, Unit>(
                    RepositoryError.NotFound<TEntity, TId>(id));
            }

            // Use ExecuteDelete to bypass the SoftDeleteInterceptor
            var deletedCount = await _dbSet
                .IgnoreQueryFilters()
                .Where(e => EF.Property<TId>(e, "Id")!.Equals(id))
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);

            if (deletedCount == 0)
            {
                return Left<RepositoryError, Unit>(
                    RepositoryError.NotFound<TEntity, TId>(id));
            }

            return Right<RepositoryError, Unit>(Unit.Default);
        }
        catch (Exception ex)
        {
            return Left<RepositoryError, Unit>(
                RepositoryError.OperationFailed<TEntity>("HardDelete", ex));
        }
    }

    #endregion
}
