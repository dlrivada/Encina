using Encina.DomainModeling;
using Encina.EntityFrameworkCore.Repository;
using Encina.Messaging.Temporal;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace Encina.EntityFrameworkCore.Temporal;

/// <summary>
/// Entity Framework Core implementation of <see cref="ITemporalRepository{TEntity, TId}"/>
/// with support for PostgreSQL temporal tables using the <c>temporal_tables</c> extension.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TId">The entity identifier type.</typeparam>
/// <remarks>
/// <para>
/// This repository provides point-in-time query capabilities for entities stored in
/// PostgreSQL temporal tables using the third-party <c>temporal_tables</c> extension.
/// </para>
/// <para>
/// <b>PostgreSQL Temporal Tables (Non-Native)</b>: Unlike SQL Server's native temporal tables
/// which use EF Core's built-in <c>TemporalAsOf</c>, <c>TemporalAll</c>, and <c>TemporalBetween</c>
/// methods, PostgreSQL requires the third-party <c>temporal_tables</c> extension. This extension
/// must be installed and configured before using this repository:
/// <code>
/// CREATE EXTENSION IF NOT EXISTS temporal_tables;
/// </code>
/// </para>
/// <para>
/// <b>Requirements</b>:
/// <list type="bullet">
/// <item><description>PostgreSQL 9.5 or later</description></item>
/// <item><description>The <c>temporal_tables</c> extension installed</description></item>
/// <item><description>Tables configured with versioning triggers and history tables</description></item>
/// <item><description>Npgsql.EntityFrameworkCore.PostgreSQL package</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Important</b>: All DateTime parameters must be in UTC. When <see cref="TemporalTableOptions.ValidateUtcDateTime"/>
/// is enabled (default), the repository validates that DateTime parameters have <see cref="DateTimeKind.Utc"/>.
/// </para>
/// <para>
/// <b>Extension Availability Warning</b>: The <c>temporal_tables</c> extension may not be available
/// in all PostgreSQL deployments, particularly managed cloud instances (e.g., some configurations
/// of Amazon RDS, Google Cloud SQL, or Azure Database for PostgreSQL). Verify extension availability
/// before using this feature.
/// </para>
/// <para>
/// <b>For SQL Server</b>: Use <see cref="TemporalRepositoryEF{TEntity, TId}"/> instead, which uses
/// EF Core's built-in temporal table support.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // DbContext configuration for PostgreSQL temporal table
/// protected override void OnModelCreating(ModelBuilder modelBuilder)
/// {
///     modelBuilder.Entity&lt;Order&gt;(entity =&gt;
///     {
///         entity.ToTable("orders");
///         entity.Property&lt;NpgsqlRange&lt;DateTime&gt;&gt;("sys_period");
///     });
/// }
///
/// // Register repository
/// services.AddEncinaTemporalRepositoryPostgreSql&lt;Order, OrderId&gt;(options =&gt;
/// {
///     options.HistoryTableName = "orders_history";
///     options.PeriodColumnName = "sys_period";
/// });
///
/// // Use repository
/// public class OrderAuditService(ITemporalRepository&lt;Order, OrderId&gt; repository)
/// {
///     public async Task&lt;Either&lt;RepositoryError, Order&gt;&gt; GetOrderStateLastWeekAsync(
///         OrderId id, CancellationToken ct)
///     {
///         var lastWeek = DateTime.UtcNow.AddDays(-7);
///         return await repository.GetAsOfAsync(id, lastWeek, ct);
///     }
/// }
/// </code>
/// </example>
public sealed class TemporalRepositoryPostgreSqlEF<TEntity, TId> : ITemporalRepository<TEntity, TId>
    where TEntity : class, IEntity<TId>
    where TId : notnull
{
    private readonly DbContext _dbContext;
    private readonly DbSet<TEntity> _dbSet;
    private readonly TemporalTableOptions _options;
    private readonly PostgreSqlTemporalOptions _pgOptions;
    private readonly ILogger<TemporalRepositoryPostgreSqlEF<TEntity, TId>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TemporalRepositoryPostgreSqlEF{TEntity, TId}"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    /// <param name="options">The temporal table configuration options.</param>
    /// <param name="pgOptions">PostgreSQL-specific temporal options.</param>
    /// <param name="logger">The logger.</param>
    /// <exception cref="ArgumentNullException">Thrown when any required parameter is null.</exception>
    public TemporalRepositoryPostgreSqlEF(
        DbContext dbContext,
        TemporalTableOptions options,
        PostgreSqlTemporalOptions pgOptions,
        ILogger<TemporalRepositoryPostgreSqlEF<TEntity, TId>> logger)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(pgOptions);
        ArgumentNullException.ThrowIfNull(logger);

        _dbContext = dbContext;
        _dbSet = dbContext.Set<TEntity>();
        _options = options;
        _pgOptions = pgOptions;
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
                Log.TemporalQueryAsOfPostgreSql(_logger, typeof(TEntity).Name, id?.ToString() ?? "null", asOfUtc);
            }

            // Use raw SQL to query with temporal_tables extension
            // The temporal_tables extension uses tstzrange for the period column
            var sql = $$"""
                SELECT * FROM (
                    SELECT *, lower("{{_pgOptions.PeriodColumnName}}") as period_start, upper("{{_pgOptions.PeriodColumnName}}") as period_end
                    FROM "{{_pgOptions.MainTableName}}"
                    WHERE "Id" = {0}
                    UNION ALL
                    SELECT *, lower("{{_pgOptions.PeriodColumnName}}") as period_start, upper("{{_pgOptions.PeriodColumnName}}") as period_end
                    FROM "{{_pgOptions.HistoryTableName}}"
                    WHERE "Id" = {0}
                ) AS temporal_data
                WHERE period_start <= {1} AND (period_end IS NULL OR period_end > {1})
                LIMIT 1
                """;

            var entity = await _dbContext.Set<TEntity>()
                .FromSqlRaw(sql, id!, asOfUtc)
                .AsNoTracking()
                .FirstOrDefaultAsync(cancellationToken)
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
                Log.TemporalQueryHistoryPostgreSql(_logger, typeof(TEntity).Name, id?.ToString() ?? "null");
            }

            // Query both current and history tables
            var sql = $$"""
                SELECT * FROM (
                    SELECT *, lower("{{_pgOptions.PeriodColumnName}}") as period_start
                    FROM "{{_pgOptions.MainTableName}}"
                    WHERE "Id" = {0}
                    UNION ALL
                    SELECT *, lower("{{_pgOptions.PeriodColumnName}}") as period_start
                    FROM "{{_pgOptions.HistoryTableName}}"
                    WHERE "Id" = {0}
                ) AS temporal_data
                ORDER BY period_start DESC
                """;

            var history = await _dbContext.Set<TEntity>()
                .FromSqlRaw(sql, id!)
                .AsNoTracking()
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
                Log.TemporalQueryBetweenPostgreSql(_logger, typeof(TEntity).Name, fromUtc, toUtc);
            }

            // Query records that were active at any point during the range
            var sql = $$"""
                SELECT * FROM (
                    SELECT *
                    FROM "{{_pgOptions.MainTableName}}"
                    UNION ALL
                    SELECT *
                    FROM "{{_pgOptions.HistoryTableName}}"
                ) AS temporal_data
                WHERE "{{_pgOptions.PeriodColumnName}}" && tstzrange({0}, {1}, '[]')
                """;

            var entities = await _dbContext.Set<TEntity>()
                .FromSqlRaw(sql, fromUtc, toUtc)
                .AsNoTracking()
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
                Log.TemporalQueryListAsOfPostgreSql(_logger, typeof(TEntity).Name, asOfUtc);
            }

            // First get temporal data, then apply specification filter in memory
            // This is less efficient than SQL Server's native temporal support but necessary for PostgreSQL
            var sql = $$"""
                SELECT * FROM (
                    SELECT *, lower("{{_pgOptions.PeriodColumnName}}") as period_start, upper("{{_pgOptions.PeriodColumnName}}") as period_end
                    FROM "{{_pgOptions.MainTableName}}"
                    UNION ALL
                    SELECT *, lower("{{_pgOptions.PeriodColumnName}}") as period_start, upper("{{_pgOptions.PeriodColumnName}}") as period_end
                    FROM "{{_pgOptions.HistoryTableName}}"
                ) AS temporal_data
                WHERE period_start <= {0} AND (period_end IS NULL OR period_end > {0})
                """;

            var temporalData = await _dbContext.Set<TEntity>()
                .FromSqlRaw(sql, asOfUtc)
                .AsNoTracking()
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            // Apply specification filter in memory
            var predicate = specification.ToExpression().Compile();
            var filtered = temporalData.Where(predicate).ToList();

            return Right<RepositoryError, IReadOnlyList<TEntity>>(filtered);
        }
        catch (Exception ex)
        {
            return Left<RepositoryError, IReadOnlyList<TEntity>>(
                RepositoryError.OperationFailed<TEntity>("ListAsOf", ex));
        }
    }

    #endregion

    #region Private Methods

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
/// PostgreSQL-specific options for temporal table configuration.
/// </summary>
/// <remarks>
/// <para>
/// These options are specific to PostgreSQL's <c>temporal_tables</c> extension and define
/// the table and column names used for temporal queries.
/// </para>
/// <para>
/// <b>Setup Instructions</b>: Before using temporal tables with PostgreSQL, you need to:
/// <list type="number">
/// <item><description>Install the extension: <c>CREATE EXTENSION IF NOT EXISTS temporal_tables;</c></description></item>
/// <item><description>Add a period column: <c>ALTER TABLE orders ADD COLUMN sys_period tstzrange NOT NULL DEFAULT tstzrange(current_timestamp, NULL);</c></description></item>
/// <item><description>Create a history table: <c>CREATE TABLE orders_history (LIKE orders);</c></description></item>
/// <item><description>Create the versioning trigger</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var pgOptions = new PostgreSqlTemporalOptions
/// {
///     MainTableName = "orders",
///     HistoryTableName = "orders_history",
///     PeriodColumnName = "sys_period"
/// };
/// </code>
/// </example>
public sealed class PostgreSqlTemporalOptions
{
    /// <summary>
    /// Gets or sets the name of the main (current) table.
    /// </summary>
    /// <remarks>
    /// This should match the table name configured in your DbContext's OnModelCreating.
    /// </remarks>
    public required string MainTableName { get; init; }

    /// <summary>
    /// Gets or sets the name of the history table.
    /// </summary>
    /// <remarks>
    /// This table stores all previous versions of rows. It must have the same structure
    /// as the main table plus the period column.
    /// Common naming convention: <c>"{tablename}_history"</c>
    /// </remarks>
    public required string HistoryTableName { get; init; }

    /// <summary>
    /// Gets or sets the name of the period column.
    /// </summary>
    /// <remarks>
    /// This column stores the validity period as a <c>tstzrange</c> (timestamp with time zone range).
    /// Default: <c>"sys_period"</c>
    /// </remarks>
    public string PeriodColumnName { get; init; } = "sys_period";
}

/// <summary>
/// High-performance logging for the PostgreSQL temporal repository using LoggerMessage.
/// </summary>
internal static partial class Log
{
    [LoggerMessage(
        EventId = 210,
        Level = LogLevel.Debug,
        Message = "PostgreSQL Temporal query (AsOf): Entity={EntityType}, Id={EntityId}, AsOfUtc={AsOfUtc:O}")]
    public static partial void TemporalQueryAsOfPostgreSql(
        ILogger logger,
        string entityType,
        string entityId,
        DateTime asOfUtc);

    [LoggerMessage(
        EventId = 211,
        Level = LogLevel.Debug,
        Message = "PostgreSQL Temporal query (History): Entity={EntityType}, Id={EntityId}")]
    public static partial void TemporalQueryHistoryPostgreSql(
        ILogger logger,
        string entityType,
        string entityId);

    [LoggerMessage(
        EventId = 212,
        Level = LogLevel.Debug,
        Message = "PostgreSQL Temporal query (Between): Entity={EntityType}, FromUtc={FromUtc:O}, ToUtc={ToUtc:O}")]
    public static partial void TemporalQueryBetweenPostgreSql(
        ILogger logger,
        string entityType,
        DateTime fromUtc,
        DateTime toUtc);

    [LoggerMessage(
        EventId = 213,
        Level = LogLevel.Debug,
        Message = "PostgreSQL Temporal query (ListAsOf): Entity={EntityType}, AsOfUtc={AsOfUtc:O}")]
    public static partial void TemporalQueryListAsOfPostgreSql(
        ILogger logger,
        string entityType,
        DateTime asOfUtc);
}
