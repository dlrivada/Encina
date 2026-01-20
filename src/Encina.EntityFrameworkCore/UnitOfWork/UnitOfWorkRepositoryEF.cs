using Encina;
using Encina.DomainModeling;
using Encina.EntityFrameworkCore.Repository;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using static LanguageExt.Prelude;

namespace Encina.EntityFrameworkCore.UnitOfWork;

/// <summary>
/// Repository implementation for use within a Unit of Work.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TId">The entity identifier type.</typeparam>
/// <remarks>
/// <para>
/// Unlike <see cref="FunctionalRepositoryEF{TEntity, TId}"/>, this repository does NOT
/// call <c>SaveChangesAsync()</c> after write operations. Changes are only tracked
/// in the DbContext and persisted when <see cref="IUnitOfWork.SaveChangesAsync"/> is called.
/// </para>
/// <para>
/// This design allows multiple repository operations to be batched into a single
/// database transaction, enabling atomic operations across multiple aggregates.
/// </para>
/// </remarks>
internal sealed class UnitOfWorkRepositoryEF<TEntity, TId> : IFunctionalRepository<TEntity, TId>
    where TEntity : class
    where TId : notnull
{
    private readonly DbContext _dbContext;
    private readonly DbSet<TEntity> _dbSet;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnitOfWorkRepositoryEF{TEntity, TId}"/> class.
    /// </summary>
    /// <param name="dbContext">The shared database context.</param>
    public UnitOfWorkRepositoryEF(DbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
        _dbSet = dbContext.Set<TEntity>();
    }

    #region Read Operations

    /// <inheritdoc/>
    public async Task<Either<EncinaError, TEntity>> GetByIdAsync(
        TId id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _dbSet.FindAsync([id], cancellationToken).ConfigureAwait(false);

            return entity is not null
                ? Right<EncinaError, TEntity>(entity)
                : Left<EncinaError, TEntity>(RepositoryErrors.NotFound<TEntity, TId>(id));
        }
        catch (Exception ex)
        {
            return Left<EncinaError, TEntity>(
                RepositoryErrors.PersistenceError<TEntity, TId>(id, "GetById", ex));
        }
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, IReadOnlyList<TEntity>>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entities = await _dbSet
                .AsNoTracking()
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return Right<EncinaError, IReadOnlyList<TEntity>>(entities);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, IReadOnlyList<TEntity>>(
                RepositoryErrors.PersistenceError<TEntity>("List", ex));
        }
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, IReadOnlyList<TEntity>>> ListAsync(
        Specification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(specification);

        try
        {
            var query = SpecificationEvaluator.GetQuery(_dbSet.AsQueryable(), specification);
            var entities = await query.ToListAsync(cancellationToken).ConfigureAwait(false);

            return Right<EncinaError, IReadOnlyList<TEntity>>(entities);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, IReadOnlyList<TEntity>>(
                RepositoryErrors.PersistenceError<TEntity>("List", ex));
        }
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, TEntity>> FirstOrDefaultAsync(
        Specification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(specification);

        try
        {
            var query = SpecificationEvaluator.GetQuery(_dbSet.AsQueryable(), specification);
            var entity = await query.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

            return entity is not null
                ? Right<EncinaError, TEntity>(entity)
                : Left<EncinaError, TEntity>(
                    RepositoryErrors.NotFound<TEntity>($"specification: {specification.GetType().Name}"));
        }
        catch (Exception ex)
        {
            return Left<EncinaError, TEntity>(
                RepositoryErrors.PersistenceError<TEntity>("FirstOrDefault", ex));
        }
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, int>> CountAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var count = await _dbSet.CountAsync(cancellationToken).ConfigureAwait(false);
            return Right<EncinaError, int>(count);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, int>(
                RepositoryErrors.PersistenceError<TEntity>("Count", ex));
        }
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, int>> CountAsync(
        Specification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(specification);

        try
        {
            var count = await _dbSet
                .CountAsync(specification.ToExpression(), cancellationToken)
                .ConfigureAwait(false);

            return Right<EncinaError, int>(count);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, int>(
                RepositoryErrors.PersistenceError<TEntity>("Count", ex));
        }
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, bool>> AnyAsync(
        Specification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(specification);

        try
        {
            var exists = await _dbSet
                .AnyAsync(specification.ToExpression(), cancellationToken)
                .ConfigureAwait(false);

            return Right<EncinaError, bool>(exists);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, bool>(
                RepositoryErrors.PersistenceError<TEntity>("Any", ex));
        }
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, bool>> AnyAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var exists = await _dbSet.AnyAsync(cancellationToken).ConfigureAwait(false);
            return Right<EncinaError, bool>(exists);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, bool>(
                RepositoryErrors.PersistenceError<TEntity>("Any", ex));
        }
    }

    #endregion

    #region Write Operations (No SaveChanges - tracked only)

    /// <inheritdoc/>
    /// <remarks>
    /// This operation only tracks the entity in the DbContext.
    /// Call <see cref="IUnitOfWork.SaveChangesAsync"/> to persist changes.
    /// </remarks>
    public async Task<Either<EncinaError, TEntity>> AddAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        try
        {
            // Track the entity without saving
            await _dbSet.AddAsync(entity, cancellationToken).ConfigureAwait(false);
            return Right<EncinaError, TEntity>(entity);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, TEntity>(
                RepositoryErrors.PersistenceError<TEntity>("Add", ex));
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// This operation only marks the entity as modified in the DbContext.
    /// Call <see cref="IUnitOfWork.SaveChangesAsync"/> to persist changes.
    /// </remarks>
    public Task<Either<EncinaError, TEntity>> UpdateAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        try
        {
            // Mark as modified without saving
            _dbSet.Update(entity);
            return Task.FromResult(Right<EncinaError, TEntity>(entity));
        }
        catch (Exception ex)
        {
            return Task.FromResult(Left<EncinaError, TEntity>(
                RepositoryErrors.PersistenceError<TEntity>("Update", ex)));
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// This operation only marks the entity for removal in the DbContext.
    /// Call <see cref="IUnitOfWork.SaveChangesAsync"/> to persist changes.
    /// </remarks>
    public async Task<Either<EncinaError, Unit>> DeleteAsync(
        TId id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _dbSet.FindAsync([id], cancellationToken).ConfigureAwait(false);

            if (entity is null)
            {
                return Left<EncinaError, Unit>(RepositoryErrors.NotFound<TEntity, TId>(id));
            }

            // Mark for removal without saving
            _dbSet.Remove(entity);
            return Right<EncinaError, Unit>(Unit.Default);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, Unit>(
                RepositoryErrors.PersistenceError<TEntity, TId>(id, "Delete", ex));
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// This operation only marks the entity for removal in the DbContext.
    /// Call <see cref="IUnitOfWork.SaveChangesAsync"/> to persist changes.
    /// </remarks>
    public Task<Either<EncinaError, Unit>> DeleteAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        try
        {
            // Mark for removal without saving
            _dbSet.Remove(entity);
            return Task.FromResult(Right<EncinaError, Unit>(Unit.Default));
        }
        catch (Exception ex)
        {
            return Task.FromResult(Left<EncinaError, Unit>(
                RepositoryErrors.PersistenceError<TEntity>("Delete", ex)));
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// This operation only tracks the entities in the DbContext.
    /// Call <see cref="IUnitOfWork.SaveChangesAsync"/> to persist changes.
    /// </remarks>
    public async Task<Either<EncinaError, IReadOnlyList<TEntity>>> AddRangeAsync(
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entities);

        var entityList = entities.ToList();

        try
        {
            // Track all entities without saving
            await _dbSet.AddRangeAsync(entityList, cancellationToken).ConfigureAwait(false);
            return Right<EncinaError, IReadOnlyList<TEntity>>(entityList);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, IReadOnlyList<TEntity>>(
                RepositoryErrors.PersistenceError<TEntity>("AddRange", ex));
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// This operation only marks the entities as modified in the DbContext.
    /// Call <see cref="IUnitOfWork.SaveChangesAsync"/> to persist changes.
    /// </remarks>
    public Task<Either<EncinaError, Unit>> UpdateRangeAsync(
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entities);

        var entityList = entities.ToList();

        try
        {
            // Mark all as modified without saving
            _dbSet.UpdateRange(entityList);
            return Task.FromResult(Right<EncinaError, Unit>(Unit.Default));
        }
        catch (Exception ex)
        {
            return Task.FromResult(Left<EncinaError, Unit>(
                RepositoryErrors.PersistenceError<TEntity>("UpdateRange", ex)));
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// This operation executes immediately using EF Core's bulk delete.
    /// It does NOT use the Unit of Work transaction context.
    /// </remarks>
    public async Task<Either<EncinaError, int>> DeleteRangeAsync(
        Specification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(specification);

        try
        {
            // ExecuteDeleteAsync runs immediately, not tracked
            var deletedCount = await _dbSet
                .Where(specification.ToExpression())
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);

            return Right<EncinaError, int>(deletedCount);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, int>(
                RepositoryErrors.PersistenceError<TEntity>("DeleteRange", ex));
        }
    }

    #endregion
}
