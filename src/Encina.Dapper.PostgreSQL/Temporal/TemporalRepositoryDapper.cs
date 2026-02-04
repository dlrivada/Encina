using System.Data;
using Dapper;
using Encina.Dapper.PostgreSQL.Repository;
using Encina.DomainModeling;
using Encina.Messaging.Temporal;
using LanguageExt;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace Encina.Dapper.PostgreSQL.Temporal;

/// <summary>
/// Dapper implementation of <see cref="ITemporalRepository{TEntity, TId}"/>
/// with support for PostgreSQL temporal tables using the <c>temporal_tables</c> extension.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TId">The entity identifier type.</typeparam>
/// <remarks>
/// <para>
/// This repository provides point-in-time query capabilities for entities stored in
/// PostgreSQL temporal tables using raw SQL queries via Dapper.
/// </para>
/// <para>
/// <b>PostgreSQL Temporal Tables (Non-Native)</b>: Unlike SQL Server's native temporal tables,
/// PostgreSQL requires the third-party <c>temporal_tables</c> extension. This extension must be
/// installed and configured before using this repository:
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
/// <item><description>A valid <see cref="ITemporalEntityMapping{TEntity, TId}"/> configuration</description></item>
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
/// </remarks>
/// <example>
/// <code>
/// // Configure temporal mapping
/// services.AddEncinaTemporalRepository&lt;Order, Guid&gt;(mapping =&gt;
/// {
///     mapping.ToTable("orders")
///         .HasId(o =&gt; o.Id, "id")
///         .MapProperty(o =&gt; o.CustomerId, "customer_id")
///         .WithPeriodColumns("sys_period_start", "sys_period_end")
///         .WithHistoryTable("orders_history");
/// });
///
/// // Use repository
/// public class OrderAuditService(ITemporalRepository&lt;Order, Guid&gt; repository)
/// {
///     public Task&lt;Either&lt;RepositoryError, Order&gt;&gt; GetOrderStateLastWeekAsync(Guid id)
///     {
///         var lastWeek = DateTime.UtcNow.AddDays(-7);
///         return repository.GetAsOfAsync(id, lastWeek);
///     }
/// }
/// </code>
/// </example>
public sealed class TemporalRepositoryDapper<TEntity, TId> : ITemporalRepository<TEntity, TId>
    where TEntity : class, IEntity<TId>
    where TId : notnull
{
    private readonly IDbConnection _connection;
    private readonly ITemporalEntityMapping<TEntity, TId> _mapping;
    private readonly TemporalTableOptions _options;
    private readonly ILogger<TemporalRepositoryDapper<TEntity, TId>> _logger;

    // Cached SQL statements
    private readonly string _selectByIdSql;
    private readonly string _selectAllSql;
    private readonly string _countSql;

    /// <summary>
    /// Initializes a new instance of the <see cref="TemporalRepositoryDapper{TEntity, TId}"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="mapping">The temporal entity mapping configuration.</param>
    /// <param name="options">The temporal table configuration options.</param>
    /// <param name="logger">The logger.</param>
    /// <exception cref="ArgumentNullException">Thrown when any required parameter is null.</exception>
    public TemporalRepositoryDapper(
        IDbConnection connection,
        ITemporalEntityMapping<TEntity, TId> mapping,
        TemporalTableOptions options,
        ILogger<TemporalRepositoryDapper<TEntity, TId>> logger)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(mapping);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _connection = connection;
        _mapping = mapping;
        _options = options;
        _logger = logger;

        // Pre-build SQL statements for current data queries
        _selectByIdSql = BuildSelectByIdSql();
        _selectAllSql = BuildSelectAllSql();
        _countSql = BuildCountSql();
    }

    #region IReadOnlyRepository Implementation

    /// <inheritdoc/>
    public async Task<Option<TEntity>> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
    {
        var entity = await _connection.QuerySingleOrDefaultAsync<TEntity>(
            _selectByIdSql,
            new { Id = id });

        return entity is not null ? Some(entity) : None;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _connection.QueryAsync<TEntity>(_selectAllSql);
        return entities.ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<TEntity>> FindAsync(
        Specification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(specification);

        var sqlBuilder = new SpecificationSqlBuilder<TEntity>(_mapping.ColumnMappings);
        var (sql, parameters) = sqlBuilder.BuildSelectStatement(_mapping.TableName, specification);
        var entities = await _connection.QueryAsync<TEntity>(sql, parameters);
        return entities.ToList();
    }

    /// <inheritdoc/>
    public async Task<Option<TEntity>> FindOneAsync(
        Specification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(specification);

        var sqlBuilder = new SpecificationSqlBuilder<TEntity>(_mapping.ColumnMappings);
        var (whereClause, parameters) = sqlBuilder.BuildWhereClause(specification);
        var columns = string.Join(", ", _mapping.ColumnMappings.Values.Select(c => $"\"{c}\""));
        var sql = $"SELECT {columns} FROM {_mapping.TableName} {whereClause} LIMIT 1";

        var entity = await _connection.QuerySingleOrDefaultAsync<TEntity>(sql, parameters);
        return entity is not null ? Some(entity) : None;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<TEntity>> FindAsync(
        System.Linq.Expressions.Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        var allEntities = await GetAllAsync(cancellationToken);
        var compiled = predicate.Compile();
        return allEntities.Where(compiled).ToList();
    }

    /// <inheritdoc/>
    public async Task<bool> AnyAsync(
        Specification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(specification);

        var sqlBuilder = new SpecificationSqlBuilder<TEntity>(_mapping.ColumnMappings);
        var (whereClause, parameters) = sqlBuilder.BuildWhereClause(specification);
        var sql = $"SELECT EXISTS (SELECT 1 FROM {_mapping.TableName} {whereClause})";

        return await _connection.ExecuteScalarAsync<bool>(sql, parameters);
    }

    /// <inheritdoc/>
    public async Task<bool> AnyAsync(
        System.Linq.Expressions.Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        var allEntities = await GetAllAsync(cancellationToken);
        var compiled = predicate.Compile();
        return allEntities.Any(compiled);
    }

    /// <inheritdoc/>
    public async Task<int> CountAsync(
        Specification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(specification);

        var sqlBuilder = new SpecificationSqlBuilder<TEntity>(_mapping.ColumnMappings);
        var (whereClause, parameters) = sqlBuilder.BuildWhereClause(specification);
        var sql = $"SELECT COUNT(*) FROM {_mapping.TableName} {whereClause}";

        return await _connection.ExecuteScalarAsync<int>(sql, parameters);
    }

    /// <inheritdoc/>
    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return await _connection.ExecuteScalarAsync<int>(_countSql);
    }

    /// <inheritdoc/>
    public async Task<PagedResult<TEntity>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var totalCount = await CountAsync(cancellationToken);
        var offset = (pageNumber - 1) * pageSize;

        var columns = string.Join(", ", _mapping.ColumnMappings.Values.Select(c => $"\"{c}\""));
        var sql = $"""
            SELECT {columns}
            FROM {_mapping.TableName}
            ORDER BY "{_mapping.IdColumnName}"
            LIMIT @PageSize OFFSET @Offset
            """;

        var items = await _connection.QueryAsync<TEntity>(sql, new { Offset = offset, PageSize = pageSize });
        return new PagedResult<TEntity>(items.ToList(), pageNumber, pageSize, totalCount);
    }

    /// <inheritdoc/>
    public async Task<PagedResult<TEntity>> GetPagedAsync(
        Specification<TEntity> specification,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(specification);

        var sqlBuilder = new SpecificationSqlBuilder<TEntity>(_mapping.ColumnMappings);
        var (whereClause, parameters) = sqlBuilder.BuildWhereClause(specification);

        var totalCount = await CountAsync(specification, cancellationToken);

        var offset = (pageNumber - 1) * pageSize;
        var columns = string.Join(", ", _mapping.ColumnMappings.Values.Select(c => $"\"{c}\""));
        var sql = $"""
            SELECT {columns}
            FROM {_mapping.TableName}
            {whereClause}
            ORDER BY "{_mapping.IdColumnName}"
            LIMIT @PageSize OFFSET @Offset
            """;

        var dynamicParams = new DynamicParameters(parameters);
        dynamicParams.Add("Offset", offset);
        dynamicParams.Add("PageSize", pageSize);

        var items = await _connection.QueryAsync<TEntity>(sql, dynamicParams);
        return new PagedResult<TEntity>(items.ToList(), pageNumber, pageSize, totalCount);
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

            var columns = string.Join(", ", _mapping.ColumnMappings.Values.Select(c => $"\"{c}\""));

            // PostgreSQL temporal_tables: Query current table + history table with period range check
            // The temporal_tables extension stores period as tstzrange
            var sql = $"""
                SELECT {columns}
                FROM (
                    SELECT {columns}, lower("{_mapping.PeriodStartColumnName}") as period_start, upper("{_mapping.PeriodStartColumnName}") as period_end
                    FROM {_mapping.TableName}
                    WHERE "{_mapping.IdColumnName}" = @Id
                    UNION ALL
                    SELECT {columns}, lower("{_mapping.PeriodStartColumnName}") as period_start, upper("{_mapping.PeriodStartColumnName}") as period_end
                    FROM {_mapping.HistoryTableName}
                    WHERE "{_mapping.IdColumnName}" = @Id
                ) AS temporal_data
                WHERE period_start <= @AsOfUtc AND (period_end IS NULL OR period_end > @AsOfUtc)
                LIMIT 1
                """;

            var entity = await _connection.QuerySingleOrDefaultAsync<TEntity>(
                sql,
                new { Id = id, AsOfUtc = asOfUtc });

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

            var columns = string.Join(", ", _mapping.ColumnMappings.Values.Select(c => $"\"{c}\""));

            // Query both current and history tables
            var sql = $"""
                SELECT {columns}, lower("{_mapping.PeriodStartColumnName}") as period_start
                FROM (
                    SELECT {columns}, "{_mapping.PeriodStartColumnName}"
                    FROM {_mapping.TableName}
                    WHERE "{_mapping.IdColumnName}" = @Id
                    UNION ALL
                    SELECT {columns}, "{_mapping.PeriodStartColumnName}"
                    FROM {_mapping.HistoryTableName}
                    WHERE "{_mapping.IdColumnName}" = @Id
                ) AS temporal_data
                ORDER BY period_start DESC
                """;

            var history = await _connection.QueryAsync<TEntity>(sql, new { Id = id });
            return Right<RepositoryError, IReadOnlyList<TEntity>>(history.ToList());
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

            var columns = string.Join(", ", _mapping.ColumnMappings.Values.Select(c => $"\"{c}\""));

            // Query records that were active at any point during the range
            var sql = $"""
                SELECT {columns}
                FROM (
                    SELECT {columns}, lower("{_mapping.PeriodStartColumnName}") as period_start, upper("{_mapping.PeriodStartColumnName}") as period_end
                    FROM {_mapping.TableName}
                    UNION ALL
                    SELECT {columns}, lower("{_mapping.PeriodStartColumnName}") as period_start, upper("{_mapping.PeriodStartColumnName}") as period_end
                    FROM {_mapping.HistoryTableName}
                ) AS temporal_data
                WHERE "{_mapping.PeriodStartColumnName}" && tstzrange(@FromUtc, @ToUtc, '[]')
                """;

            var entities = await _connection.QueryAsync<TEntity>(
                sql,
                new { FromUtc = fromUtc, ToUtc = toUtc });

            return Right<RepositoryError, IReadOnlyList<TEntity>>(entities.ToList());
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

            var sqlBuilder = new SpecificationSqlBuilder<TEntity>(_mapping.ColumnMappings);
            var (whereClause, parameters) = sqlBuilder.BuildWhereClause(specification);

            var columns = string.Join(", ", _mapping.ColumnMappings.Values.Select(c => $"\"{c}\""));

            // Build temporal query with specification filter
            var sql = $"""
                SELECT {columns}
                FROM (
                    SELECT {columns}, lower("{_mapping.PeriodStartColumnName}") as period_start, upper("{_mapping.PeriodStartColumnName}") as period_end
                    FROM {_mapping.TableName}
                    UNION ALL
                    SELECT {columns}, lower("{_mapping.PeriodStartColumnName}") as period_start, upper("{_mapping.PeriodStartColumnName}") as period_end
                    FROM {_mapping.HistoryTableName}
                ) AS temporal_data
                WHERE period_start <= @AsOfUtc AND (period_end IS NULL OR period_end > @AsOfUtc)
                {(string.IsNullOrWhiteSpace(whereClause) ? "" : $"AND {whereClause.Replace("WHERE ", "", StringComparison.OrdinalIgnoreCase)}")}
                """;

            var dynamicParams = new DynamicParameters(parameters);
            dynamicParams.Add("AsOfUtc", asOfUtc);

            var entities = await _connection.QueryAsync<TEntity>(sql, dynamicParams);
            return Right<RepositoryError, IReadOnlyList<TEntity>>(entities.ToList());
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

    private string BuildSelectByIdSql()
    {
        var columns = string.Join(", ", _mapping.ColumnMappings.Values.Select(c => $"\"{c}\""));
        return $"SELECT {columns} FROM {_mapping.TableName} WHERE \"{_mapping.IdColumnName}\" = @Id";
    }

    private string BuildSelectAllSql()
    {
        var columns = string.Join(", ", _mapping.ColumnMappings.Values.Select(c => $"\"{c}\""));
        return $"SELECT {columns} FROM {_mapping.TableName}";
    }

    private string BuildCountSql()
    {
        return $"SELECT COUNT(*) FROM {_mapping.TableName}";
    }

    #endregion
}

/// <summary>
/// High-performance logging for the temporal repository using LoggerMessage.
/// </summary>
internal static partial class Log
{
    [LoggerMessage(
        EventId = 500,
        Level = LogLevel.Debug,
        Message = "Temporal query (AsOf): Entity={EntityType}, Id={EntityId}, AsOfUtc={AsOfUtc:O}")]
    public static partial void TemporalQueryAsOf(
        ILogger logger,
        string entityType,
        string entityId,
        DateTime asOfUtc);

    [LoggerMessage(
        EventId = 501,
        Level = LogLevel.Debug,
        Message = "Temporal query (History): Entity={EntityType}, Id={EntityId}")]
    public static partial void TemporalQueryHistory(
        ILogger logger,
        string entityType,
        string entityId);

    [LoggerMessage(
        EventId = 502,
        Level = LogLevel.Debug,
        Message = "Temporal query (Between): Entity={EntityType}, FromUtc={FromUtc:O}, ToUtc={ToUtc:O}")]
    public static partial void TemporalQueryBetween(
        ILogger logger,
        string entityType,
        DateTime fromUtc,
        DateTime toUtc);

    [LoggerMessage(
        EventId = 503,
        Level = LogLevel.Debug,
        Message = "Temporal query (ListAsOf): Entity={EntityType}, AsOfUtc={AsOfUtc:O}")]
    public static partial void TemporalQueryListAsOf(
        ILogger logger,
        string entityType,
        DateTime asOfUtc);
}
