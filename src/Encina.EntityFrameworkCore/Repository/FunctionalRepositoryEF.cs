using Encina;
using Encina.DomainModeling;
using Encina.DomainModeling.Concurrency;
using Encina.EntityFrameworkCore.Extensions;
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

        // Store original values before update attempt for conflict info
        TEntity? originalEntity = null;
        if (entity is IHasId<TId> hasId)
        {
            originalEntity = await _dbSet.AsNoTracking()
                .FirstOrDefaultAsync(e => EF.Property<TId>(e, "Id")!.Equals(hasId.Id), cancellationToken)
                .ConfigureAwait(false);
        }

        try
        {
            _dbSet.Update(entity);
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return Right<EncinaError, TEntity>(entity);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            var conflictInfo = await CreateConcurrencyConflictInfoAsync(
                originalEntity ?? entity,
                entity,
                ex,
                cancellationToken).ConfigureAwait(false);

            return Left<EncinaError, TEntity>(
                RepositoryErrors.ConcurrencyConflict(conflictInfo, ex));
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
        TEntity? originalEntity = null;

        try
        {
            var entity = await _dbSet.FindAsync([id], cancellationToken).ConfigureAwait(false);

            if (entity is null)
            {
                return Left<EncinaError, Unit>(RepositoryErrors.NotFound<TEntity, TId>(id));
            }

            originalEntity = entity;
            _dbSet.Remove(entity);
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return Right<EncinaError, Unit>(Unit.Default);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            if (originalEntity is not null)
            {
                var conflictInfo = await CreateConcurrencyConflictInfoAsync(
                    originalEntity,
                    originalEntity,
                    ex,
                    cancellationToken).ConfigureAwait(false);

                return Left<EncinaError, Unit>(
                    RepositoryErrors.ConcurrencyConflict(conflictInfo, ex));
            }

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
            var conflictInfo = await CreateConcurrencyConflictInfoAsync(
                entity,
                entity,
                ex,
                cancellationToken).ConfigureAwait(false);

            return Left<EncinaError, Unit>(
                RepositoryErrors.ConcurrencyConflict(conflictInfo, ex));
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
            // For range updates, try to identify the first conflicting entity
            var conflictingEntry = ex.Entries.Count > 0 ? ex.Entries[0] : null;
            if (conflictingEntry?.Entity is TEntity conflictingEntity)
            {
                var conflictInfo = await CreateConcurrencyConflictInfoAsync(
                    conflictingEntity,
                    conflictingEntity,
                    ex,
                    cancellationToken).ConfigureAwait(false);

                return Left<EncinaError, Unit>(
                    RepositoryErrors.ConcurrencyConflict(conflictInfo, ex));
            }

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

    /// <inheritdoc/>
    public async Task<Either<EncinaError, Unit>> UpdateImmutableAsync(
        TEntity modified,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(modified);

        try
        {
            var updateResult = await _dbContext.UpdateImmutableAsync(modified, cancellationToken)
                .ConfigureAwait(false);

            if (updateResult.IsLeft)
            {
                return updateResult;
            }

            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Right<EncinaError, Unit>(Unit.Default);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            var conflictInfo = await CreateConcurrencyConflictInfoAsync(
                modified,
                modified,
                ex,
                cancellationToken).ConfigureAwait(false);

            return Left<EncinaError, Unit>(
                RepositoryErrors.ConcurrencyConflict(conflictInfo, ex));
        }
        catch (Exception ex)
        {
            return Left<EncinaError, Unit>(
                RepositoryErrors.PersistenceError<TEntity>("UpdateImmutable", ex));
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

    /// <summary>
    /// Creates a <see cref="ConcurrencyConflictInfo{TEntity}"/> by fetching the current database state.
    /// </summary>
    /// <param name="currentEntity">The entity state when originally loaded.</param>
    /// <param name="proposedEntity">The entity state being saved.</param>
    /// <param name="ex">The concurrency exception containing entry information.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="ConcurrencyConflictInfo{TEntity}"/> with all three entity states.</returns>
    private async Task<ConcurrencyConflictInfo<TEntity>> CreateConcurrencyConflictInfoAsync(
        TEntity currentEntity,
        TEntity proposedEntity,
        DbUpdateConcurrencyException ex,
        CancellationToken cancellationToken)
    {
        TEntity? databaseEntity = null;

        // Try to get the current database state
        var entry = ex.Entries.FirstOrDefault(e => e.Entity is TEntity);
        if (entry is not null)
        {
            try
            {
                // Reload the entry to get current database values
                await entry.ReloadAsync(cancellationToken).ConfigureAwait(false);
                databaseEntity = entry.Entity as TEntity;
            }
            catch
            {
                // Entity may have been deleted - databaseEntity stays null
            }
        }

        // If we couldn't get it from the entry, try by ID
        if (databaseEntity is null && proposedEntity is IHasId<TId> hasId)
        {
            try
            {
                databaseEntity = await _dbSet.AsNoTracking()
                    .FirstOrDefaultAsync(
                        e => EF.Property<TId>(e, "Id")!.Equals(hasId.Id),
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch
            {
                // Entity may have been deleted - databaseEntity stays null
            }
        }

        return new ConcurrencyConflictInfo<TEntity>(
            CurrentEntity: currentEntity,
            ProposedEntity: proposedEntity,
            DatabaseEntity: databaseEntity);
    }

    #endregion
}
