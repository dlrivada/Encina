using Encina;
using Encina.DomainModeling;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using static LanguageExt.Prelude;

namespace Encina.EntityFrameworkCore.Repository;

/// <summary>
/// Entity Framework Core implementation of <see cref="IFunctionalRepository{TEntity, TId}"/>
/// with Railway Oriented Programming error handling.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TId">The entity identifier type.</typeparam>
/// <remarks>
/// <para>
/// This repository provides full CRUD operations with explicit error handling using
/// <see cref="Either{EncinaError, T}"/>. All database exceptions are mapped to
/// appropriate <see cref="EncinaError"/> instances using <see cref="RepositoryErrors"/>.
/// </para>
/// <para>
/// The implementation uses:
/// <list type="bullet">
/// <item><description><see cref="DbContext.FindAsync{TEntity}(object?[])"/> for GetByIdAsync</description></item>
/// <item><description><see cref="DbSet{TEntity}.AddAsync"/> for inserts</description></item>
/// <item><description>EF Core change tracking for updates</description></item>
/// <item><description><see cref="SpecificationEvaluator"/> for specification-based queries</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class OrderService
/// {
///     private readonly IFunctionalRepository&lt;Order, OrderId&gt; _repository;
///
///     public async Task&lt;Either&lt;EncinaError, Order&gt;&gt; GetOrderAsync(OrderId id, CancellationToken ct)
///     {
///         return await _repository.GetByIdAsync(id, ct);
///     }
///
///     public async Task&lt;Either&lt;EncinaError, Order&gt;&gt; CreateOrderAsync(Order order, CancellationToken ct)
///     {
///         return await _repository.AddAsync(order, ct);
///     }
/// }
/// </code>
/// </example>
public sealed class FunctionalRepositoryEF<TEntity, TId> : IFunctionalRepository<TEntity, TId>
    where TEntity : class
    where TId : notnull
{
    private readonly DbContext _dbContext;
    private readonly DbSet<TEntity> _dbSet;

    /// <summary>
    /// Initializes a new instance of the <see cref="FunctionalRepositoryEF{TEntity, TId}"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="dbContext"/> is null.</exception>
    public FunctionalRepositoryEF(DbContext dbContext)
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

    #region Write Operations

    /// <inheritdoc/>
    public async Task<Either<EncinaError, TEntity>> AddAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        try
        {
            await _dbSet.AddAsync(entity, cancellationToken).ConfigureAwait(false);
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return Right<EncinaError, TEntity>(entity);
        }
        catch (DbUpdateException ex) when (IsDuplicateKeyException(ex))
        {
            return Left<EncinaError, TEntity>(
                RepositoryErrors.AlreadyExists<TEntity>(GetEntityIdDescription(entity)));
        }
        catch (Exception ex)
        {
            return Left<EncinaError, TEntity>(
                RepositoryErrors.PersistenceError<TEntity>("Add", ex));
        }
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, TEntity>> UpdateAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        try
        {
            _dbSet.Update(entity);
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return Right<EncinaError, TEntity>(entity);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            return Left<EncinaError, TEntity>(
                RepositoryErrors.ConcurrencyConflict<TEntity>(ex));
        }
        catch (Exception ex)
        {
            return Left<EncinaError, TEntity>(
                RepositoryErrors.PersistenceError<TEntity>("Update", ex));
        }
    }

    /// <inheritdoc/>
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

            _dbSet.Remove(entity);
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return Right<EncinaError, Unit>(Unit.Default);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            return Left<EncinaError, Unit>(
                RepositoryErrors.ConcurrencyConflict<TEntity, TId>(id, ex));
        }
        catch (Exception ex)
        {
            return Left<EncinaError, Unit>(
                RepositoryErrors.PersistenceError<TEntity, TId>(id, "Delete", ex));
        }
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, Unit>> DeleteAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        try
        {
            _dbSet.Remove(entity);
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return Right<EncinaError, Unit>(Unit.Default);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            return Left<EncinaError, Unit>(
                RepositoryErrors.ConcurrencyConflict<TEntity>(ex));
        }
        catch (Exception ex)
        {
            return Left<EncinaError, Unit>(
                RepositoryErrors.PersistenceError<TEntity>("Delete", ex));
        }
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, IReadOnlyList<TEntity>>> AddRangeAsync(
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entities);

        var entityList = entities.ToList();

        try
        {
            await _dbSet.AddRangeAsync(entityList, cancellationToken).ConfigureAwait(false);
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return Right<EncinaError, IReadOnlyList<TEntity>>(entityList);
        }
        catch (DbUpdateException ex) when (IsDuplicateKeyException(ex))
        {
            return Left<EncinaError, IReadOnlyList<TEntity>>(
                RepositoryErrors.AlreadyExists<TEntity>("One or more entities already exist"));
        }
        catch (Exception ex)
        {
            return Left<EncinaError, IReadOnlyList<TEntity>>(
                RepositoryErrors.PersistenceError<TEntity>("AddRange", ex));
        }
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, Unit>> UpdateRangeAsync(
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entities);

        var entityList = entities.ToList();

        try
        {
            _dbSet.UpdateRange(entityList);
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return Right<EncinaError, Unit>(Unit.Default);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            return Left<EncinaError, Unit>(
                RepositoryErrors.ConcurrencyConflict<TEntity>(ex));
        }
        catch (Exception ex)
        {
            return Left<EncinaError, Unit>(
                RepositoryErrors.PersistenceError<TEntity>("UpdateRange", ex));
        }
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, int>> DeleteRangeAsync(
        Specification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(specification);

        try
        {
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

    #region Helper Methods

    private static bool IsDuplicateKeyException(DbUpdateException ex)
    {
        // Check for common duplicate key violation patterns across database providers
        var message = ex.InnerException?.Message ?? ex.Message;

        return message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase)
            || message.Contains("unique constraint", StringComparison.OrdinalIgnoreCase)
            || message.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase)
            || message.Contains("PRIMARY KEY constraint", StringComparison.OrdinalIgnoreCase)
            || message.Contains("violation of PRIMARY KEY", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetEntityIdDescription(TEntity entity)
    {
        // Try to extract ID if entity implements IHasId<TId>
        if (entity is IHasId<TId> hasId)
        {
            return $"ID: {hasId.Id}";
        }

        // Fallback to entity type name
        return entity.GetType().Name;
    }

    #endregion
}
