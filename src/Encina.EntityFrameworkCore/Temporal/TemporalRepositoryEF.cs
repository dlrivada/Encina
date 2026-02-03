using Encina.DomainModeling;
using Encina.EntityFrameworkCore.Repository;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace Encina.EntityFrameworkCore.Temporal;

/// <summary>
/// Entity Framework Core implementation of <see cref="ITemporalRepository{TEntity, TId}"/>
/// with support for SQL Server temporal tables (system-versioned tables).
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TId">The entity identifier type.</typeparam>
/// <remarks>
/// <para>
/// This repository provides point-in-time query capabilities for entities stored in
/// SQL Server temporal tables. Temporal tables automatically track the full history
/// of data changes, enabling:
/// <list type="bullet">
/// <item><description><see cref="GetAsOfAsync"/>: Query entity state at a specific point in time</description></item>
/// <item><description><see cref="GetHistoryAsync"/>: Retrieve all historical versions of an entity</description></item>
/// <item><description><see cref="GetChangedBetweenAsync"/>: Query changes within a time range</description></item>
/// <item><description><see cref="ListAsOfAsync"/>: Combine point-in-time queries with specifications</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Requirements</b>:
/// <list type="bullet">
/// <item><description>SQL Server 2016 or later</description></item>
/// <item><description>Tables configured as temporal using <c>ConfigureTemporalTable</c></description></item>
/// <item><description>Microsoft.EntityFrameworkCore.SqlServer package</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Important</b>: All DateTime parameters must be in UTC. When <see cref="Messaging.Temporal.TemporalTableOptions.ValidateUtcDateTime"/>
/// is enabled (default), the repository validates that DateTime parameters have <see cref="DateTimeKind.Utc"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class OrderAuditService
/// {
///     private readonly ITemporalRepository&lt;Order, OrderId&gt; _repository;
///
///     public async Task&lt;Either&lt;RepositoryError, Order&gt;&gt; GetOrderStateLastWeekAsync(
///         OrderId id, CancellationToken ct)
///     {
///         var lastWeek = DateTime.UtcNow.AddDays(-7);
///         return await _repository.GetAsOfAsync(id, lastWeek, ct);
///     }
///
///     public async Task&lt;Either&lt;RepositoryError, IReadOnlyList&lt;Order&gt;&gt;&gt; GetOrderHistoryAsync(
///         OrderId id, CancellationToken ct)
///     {
///         return await _repository.GetHistoryAsync(id, ct);
///     }
/// }
/// </code>
/// </example>
public sealed class TemporalRepositoryEF<TEntity, TId> : ITemporalRepository<TEntity, TId>
    where TEntity : class, IEntity<TId>
    where TId : notnull
{
    private readonly DbContext _dbContext;
    private readonly DbSet<TEntity> _dbSet;
    private readonly Messaging.Temporal.TemporalTableOptions _options;
    private readonly ILogger<TemporalRepositoryEF<TEntity, TId>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TemporalRepositoryEF{TEntity, TId}"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    /// <param name="options">The temporal table configuration options.</param>
    /// <param name="logger">The logger.</param>
    /// <exception cref="ArgumentNullException">Thrown when any required parameter is null.</exception>
    public TemporalRepositoryEF(
        DbContext dbContext,
        Messaging.Temporal.TemporalTableOptions options,
        ILogger<TemporalRepositoryEF<TEntity, TId>> logger)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _dbContext = dbContext;
        _dbSet = dbContext.Set<TEntity>();
        _options = options;
        _logger = logger;
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

    #region ITemporalRepository Implementation

    /// <inheritdoc/>
    public async Task<Either<RepositoryError, TEntity>> GetAsOfAsync(
        TId id,
        DateTime asOfUtc,
        CancellationToken cancellationToken = default)
    {
        var validationResult = ValidateUtcDateTime(asOfUtc, nameof(asOfUtc));
        if (validationResult.IsLeft)
        {
            return validationResult.Map(_ => default(TEntity)!);
        }

        try
        {
            if (_options.LogTemporalQueries)
            {
                Log.TemporalQueryAsOf(_logger, typeof(TEntity).Name, id?.ToString() ?? "null", asOfUtc);
            }

            var entity = await _dbSet
                .TemporalAsOf(asOfUtc)
                .FirstOrDefaultAsync(e => EF.Property<TId>(e, "Id")!.Equals(id), cancellationToken)
                .ConfigureAwait(false);

            if (entity is null)
            {
                return Left<RepositoryError, TEntity>(
                    RepositoryError.NotFound<TEntity, TId>(id!));
            }

            return Right<RepositoryError, TEntity>(entity);
        }
        catch (Exception ex)
        {
            return Left<RepositoryError, TEntity>(
                RepositoryError.OperationFailed<TEntity>("GetAsOf", ex));
        }
    }

    /// <inheritdoc/>
    public async Task<Either<RepositoryError, IReadOnlyList<TEntity>>> GetHistoryAsync(
        TId id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (_options.LogTemporalQueries)
            {
                Log.TemporalQueryHistory(_logger, typeof(TEntity).Name, id?.ToString() ?? "null");
            }

            // Get all historical versions using TemporalAll()
            // Order by PeriodStart descending (newest first)
            var history = await _dbSet
                .TemporalAll()
                .Where(e => EF.Property<TId>(e, "Id")!.Equals(id))
                .OrderByDescending(e => EF.Property<DateTime>(e, _options.DefaultPeriodStartColumnName))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return Right<RepositoryError, IReadOnlyList<TEntity>>(history);
        }
        catch (Exception ex)
        {
            return Left<RepositoryError, IReadOnlyList<TEntity>>(
                RepositoryError.OperationFailed<TEntity>("GetHistory", ex));
        }
    }

    /// <inheritdoc/>
    public async Task<Either<RepositoryError, IReadOnlyList<TEntity>>> GetChangedBetweenAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default)
    {
        var fromValidation = ValidateUtcDateTime(fromUtc, nameof(fromUtc));
        if (fromValidation.IsLeft)
        {
            return fromValidation.Map(_ => (IReadOnlyList<TEntity>)[]);
        }

        var toValidation = ValidateUtcDateTime(toUtc, nameof(toUtc));
        if (toValidation.IsLeft)
        {
            return toValidation.Map(_ => (IReadOnlyList<TEntity>)[]);
        }

        if (fromUtc > toUtc)
        {
            return Left<RepositoryError, IReadOnlyList<TEntity>>(
                new RepositoryError(
                    $"Invalid time range: fromUtc ({fromUtc:O}) must be less than or equal to toUtc ({toUtc:O})",
                    "REPOSITORY_INVALID_TIME_RANGE",
                    typeof(TEntity)));
        }

        try
        {
            if (_options.LogTemporalQueries)
            {
                Log.TemporalQueryBetween(_logger, typeof(TEntity).Name, fromUtc, toUtc);
            }

            // TemporalBetween returns rows that were active at any point during the range
            var entities = await _dbSet
                .TemporalBetween(fromUtc, toUtc)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return Right<RepositoryError, IReadOnlyList<TEntity>>(entities);
        }
        catch (Exception ex)
        {
            return Left<RepositoryError, IReadOnlyList<TEntity>>(
                RepositoryError.OperationFailed<TEntity>("GetChangedBetween", ex));
        }
    }

    /// <inheritdoc/>
    public async Task<Either<RepositoryError, IReadOnlyList<TEntity>>> ListAsOfAsync(
        Specification<TEntity> specification,
        DateTime asOfUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(specification);

        var validationResult = ValidateUtcDateTime(asOfUtc, nameof(asOfUtc));
        if (validationResult.IsLeft)
        {
            return validationResult.Map(_ => (IReadOnlyList<TEntity>)[]);
        }

        try
        {
            if (_options.LogTemporalQueries)
            {
                Log.TemporalQueryListAsOf(_logger, typeof(TEntity).Name, asOfUtc);
            }

            // Combine TemporalAsOf with specification filtering
            var query = SpecificationEvaluator.GetQuery(
                _dbSet.TemporalAsOf(asOfUtc).AsQueryable(),
                specification);

            var entities = await query.ToListAsync(cancellationToken).ConfigureAwait(false);

            return Right<RepositoryError, IReadOnlyList<TEntity>>(entities);
        }
        catch (Exception ex)
        {
            return Left<RepositoryError, IReadOnlyList<TEntity>>(
                RepositoryError.OperationFailed<TEntity>("ListAsOf", ex));
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Validates that a DateTime parameter is in UTC.
    /// </summary>
    private Either<RepositoryError, Unit> ValidateUtcDateTime(DateTime dateTime, string parameterName)
    {
        if (_options.ValidateUtcDateTime && dateTime.Kind != DateTimeKind.Utc)
        {
            return Left<RepositoryError, Unit>(
                new RepositoryError(
                    $"DateTime parameter '{parameterName}' must be UTC. Received Kind: {dateTime.Kind}. " +
                    $"Use DateTime.UtcNow or DateTimeOffset.UtcNow.UtcDateTime for correct behavior.",
                    "REPOSITORY_INVALID_DATETIME_KIND",
                    typeof(TEntity)));
        }

        return Right<RepositoryError, Unit>(unit);
    }

    #endregion
}

/// <summary>
/// High-performance logging for the temporal repository using LoggerMessage.
/// </summary>
internal static partial class Log
{
    [LoggerMessage(
        EventId = 200,
        Level = LogLevel.Debug,
        Message = "Temporal query (AsOf): Entity={EntityType}, Id={EntityId}, AsOfUtc={AsOfUtc:O}")]
    public static partial void TemporalQueryAsOf(
        ILogger logger,
        string entityType,
        string entityId,
        DateTime asOfUtc);

    [LoggerMessage(
        EventId = 201,
        Level = LogLevel.Debug,
        Message = "Temporal query (History): Entity={EntityType}, Id={EntityId}")]
    public static partial void TemporalQueryHistory(
        ILogger logger,
        string entityType,
        string entityId);

    [LoggerMessage(
        EventId = 202,
        Level = LogLevel.Debug,
        Message = "Temporal query (Between): Entity={EntityType}, FromUtc={FromUtc:O}, ToUtc={ToUtc:O}")]
    public static partial void TemporalQueryBetween(
        ILogger logger,
        string entityType,
        DateTime fromUtc,
        DateTime toUtc);

    [LoggerMessage(
        EventId = 203,
        Level = LogLevel.Debug,
        Message = "Temporal query (ListAsOf): Entity={EntityType}, AsOfUtc={AsOfUtc:O}")]
    public static partial void TemporalQueryListAsOf(
        ILogger logger,
        string entityType,
        DateTime asOfUtc);
}
