using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Reflection;
using Encina.ADO.PostgreSQL.Repository;
using Encina.DomainModeling;
using Encina.Messaging.Temporal;
using LanguageExt;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace Encina.ADO.PostgreSQL.Temporal;

/// <summary>
/// ADO.NET implementation of <see cref="ITemporalRepository{TEntity, TId}"/>
/// with support for PostgreSQL temporal tables using the <c>temporal_tables</c> extension.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TId">The entity identifier type.</typeparam>
/// <remarks>
/// <para>
/// This repository provides point-in-time query capabilities for entities stored in
/// PostgreSQL temporal tables using raw ADO.NET commands for maximum performance.
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
/// in all PostgreSQL deployments, particularly managed cloud instances. Verify extension availability
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
/// public class OrderAuditService(ITemporalRepository&lt;Order, Guid&gt; repository, TimeProvider timeProvider)
/// {
///     public Task&lt;Either&lt;RepositoryError, Order&gt;&gt; GetOrderStateLastWeekAsync(Guid id)
///     {
///         var lastWeek = timeProvider.GetUtcNow().UtcDateTime.AddDays(-7);
///         return repository.GetAsOfAsync(id, lastWeek);
///     }
/// }
/// </code>
/// </example>
public sealed class TemporalRepositoryADO<TEntity, TId> : ITemporalRepository<TEntity, TId>
    where TEntity : class, IEntity<TId>, new()
    where TId : notnull
{
    private readonly IDbConnection _connection;
    private readonly ITemporalEntityMapping<TEntity, TId> _mapping;
    private readonly TemporalTableOptions _options;
    private readonly ILogger<TemporalRepositoryADO<TEntity, TId>> _logger;
    private readonly Dictionary<string, PropertyInfo> _propertyCache;

    // Cached SQL statements
    private readonly string _selectByIdSql;
    private readonly string _selectAllSql;
    private readonly string _countSql;

    /// <summary>
    /// Initializes a new instance of the <see cref="TemporalRepositoryADO{TEntity, TId}"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="mapping">The temporal entity mapping configuration.</param>
    /// <param name="options">The temporal table configuration options.</param>
    /// <param name="logger">The logger.</param>
    /// <exception cref="ArgumentNullException">Thrown when any required parameter is null.</exception>
    public TemporalRepositoryADO(
        IDbConnection connection,
        ITemporalEntityMapping<TEntity, TId> mapping,
        TemporalTableOptions options,
        ILogger<TemporalRepositoryADO<TEntity, TId>> logger)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(mapping);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _connection = connection;
        _mapping = mapping;
        _options = options;
        _logger = logger;

        // Build property cache for entity materialization
        _propertyCache = typeof(TEntity).GetProperties()
            .Where(p => mapping.ColumnMappings.ContainsKey(p.Name))
            .ToDictionary(p => p.Name);

        // Pre-build SQL statements for current data queries
        _selectByIdSql = BuildSelectByIdSql();
        _selectAllSql = BuildSelectAllSql();
        _countSql = BuildCountSql();
    }

    #region IReadOnlyRepository Implementation

    /// <inheritdoc/>
    public async Task<Option<TEntity>> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
    {
        await EnsureConnectionOpenAsync(cancellationToken).ConfigureAwait(false);

        using var command = CreateCommand(_selectByIdSql);
        AddParameter(command, "@Id", id);

        using var reader = await ExecuteReaderAsync(command, cancellationToken);
        if (await ReadAsync(reader, cancellationToken))
        {
            return Some(MaterializeEntity(reader));
        }

        return None;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await EnsureConnectionOpenAsync(cancellationToken).ConfigureAwait(false);

        using var command = CreateCommand(_selectAllSql);
        using var reader = await ExecuteReaderAsync(command, cancellationToken);

        var results = new List<TEntity>();
        while (await ReadAsync(reader, cancellationToken))
        {
            results.Add(MaterializeEntity(reader));
        }

        return results;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<TEntity>> FindAsync(
        Specification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(specification);

        await EnsureConnectionOpenAsync(cancellationToken).ConfigureAwait(false);

        var sqlBuilder = new SpecificationSqlBuilder<TEntity>(_mapping.ColumnMappings);
        var (sql, addParameters) = sqlBuilder.BuildSelectStatement(_mapping.TableName, specification);

        using var command = CreateCommand(sql);
        addParameters(command);

        using var reader = await ExecuteReaderAsync(command, cancellationToken);

        var results = new List<TEntity>();
        while (await ReadAsync(reader, cancellationToken))
        {
            results.Add(MaterializeEntity(reader));
        }

        return results;
    }

    /// <inheritdoc/>
    public async Task<Option<TEntity>> FindOneAsync(
        Specification<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(specification);

        await EnsureConnectionOpenAsync(cancellationToken).ConfigureAwait(false);

        var sqlBuilder = new SpecificationSqlBuilder<TEntity>(_mapping.ColumnMappings);
        var (whereClause, addParameters) = sqlBuilder.BuildWhereClause(specification);
        var columns = string.Join(", ", _mapping.ColumnMappings.Values.Select(c => $"\"{c}\""));
        var sql = $"SELECT {columns} FROM {_mapping.TableName} {whereClause} LIMIT 1";

        using var command = CreateCommand(sql);
        addParameters(command);

        using var reader = await ExecuteReaderAsync(command, cancellationToken);
        if (await ReadAsync(reader, cancellationToken))
        {
            return Some(MaterializeEntity(reader));
        }

        return None;
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

        await EnsureConnectionOpenAsync(cancellationToken).ConfigureAwait(false);

        var sqlBuilder = new SpecificationSqlBuilder<TEntity>(_mapping.ColumnMappings);
        var (whereClause, addParameters) = sqlBuilder.BuildWhereClause(specification);
        var sql = $"SELECT EXISTS (SELECT 1 FROM {_mapping.TableName} {whereClause})";

        using var command = CreateCommand(sql);
        addParameters(command);

        var result = await ExecuteScalarAsync(command, cancellationToken);
        return Convert.ToBoolean(result, CultureInfo.InvariantCulture);
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

        await EnsureConnectionOpenAsync(cancellationToken).ConfigureAwait(false);

        var sqlBuilder = new SpecificationSqlBuilder<TEntity>(_mapping.ColumnMappings);
        var (whereClause, addParameters) = sqlBuilder.BuildWhereClause(specification);
        var sql = $"SELECT COUNT(*) FROM {_mapping.TableName} {whereClause}";

        using var command = CreateCommand(sql);
        addParameters(command);

        var result = await ExecuteScalarAsync(command, cancellationToken);
        return Convert.ToInt32(result, CultureInfo.InvariantCulture);
    }

    /// <inheritdoc/>
    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        await EnsureConnectionOpenAsync(cancellationToken).ConfigureAwait(false);

        using var command = CreateCommand(_countSql);
        var result = await ExecuteScalarAsync(command, cancellationToken);
        return Convert.ToInt32(result, CultureInfo.InvariantCulture);
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

        await EnsureConnectionOpenAsync(cancellationToken).ConfigureAwait(false);

        using var command = CreateCommand(sql);
        AddParameter(command, "@Offset", offset);
        AddParameter(command, "@PageSize", pageSize);

        using var reader = await ExecuteReaderAsync(command, cancellationToken);

        var items = new List<TEntity>();
        while (await ReadAsync(reader, cancellationToken))
        {
            items.Add(MaterializeEntity(reader));
        }

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

        var totalCount = await CountAsync(specification, cancellationToken);

        var sqlBuilder = new SpecificationSqlBuilder<TEntity>(_mapping.ColumnMappings);
        var (whereClause, addParameters) = sqlBuilder.BuildWhereClause(specification);

        var offset = (pageNumber - 1) * pageSize;
        var columns = string.Join(", ", _mapping.ColumnMappings.Values.Select(c => $"\"{c}\""));
        var sql = $"""
            SELECT {columns}
            FROM {_mapping.TableName}
            {whereClause}
            ORDER BY "{_mapping.IdColumnName}"
            LIMIT @PageSize OFFSET @Offset
            """;

        await EnsureConnectionOpenAsync(cancellationToken).ConfigureAwait(false);

        using var command = CreateCommand(sql);
        addParameters(command);
        AddParameter(command, "@Offset", offset);
        AddParameter(command, "@PageSize", pageSize);

        using var reader = await ExecuteReaderAsync(command, cancellationToken);

        var items = new List<TEntity>();
        while (await ReadAsync(reader, cancellationToken))
        {
            items.Add(MaterializeEntity(reader));
        }

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
        var preCheck = ValidateAndLog(
            asOfUtc,
            nameof(asOfUtc),
            () => Log.TemporalQueryAsOf(_logger, typeof(TEntity).Name, id?.ToString() ?? "null", asOfUtc));
        if (preCheck.IsLeft)
        {
            return preCheck.Map(_ => default(TEntity)!);
        }

        var columns = string.Join(", ", _mapping.ColumnMappings.Values.Select(c => $"\"{c}\""));

        // PostgreSQL temporal_tables: Query current table + history table with period range check
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

        return await ExecuteSingleEntityQueryAsync(
            sql,
            command =>
            {
                AddParameter(command, "@Id", id);
                AddParameter(command, "@AsOfUtc", asOfUtc);
            },
            "GetAsOf",
            () => RepositoryError.NotFound<TEntity, TId>(id!),
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<Either<RepositoryError, IReadOnlyList<TEntity>>> GetHistoryAsync(
        TId id,
        CancellationToken cancellationToken = default)
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

        return await ExecuteListQueryAsync(
            command => AddParameter(command, "@Id", id),
            sql,
            "GetHistory",
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<Either<RepositoryError, IReadOnlyList<TEntity>>> GetChangedBetweenAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default)
    {
        var preCheck = ValidateChangedBetweenRange(
            fromUtc,
            toUtc,
            () => Log.TemporalQueryBetween(_logger, typeof(TEntity).Name, fromUtc, toUtc));
        if (preCheck.IsLeft)
        {
            return preCheck.Map(_ => (IReadOnlyList<TEntity>)[]);
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

        return await ExecuteListQueryAsync(
            command =>
            {
                AddParameter(command, "@FromUtc", fromUtc);
                AddParameter(command, "@ToUtc", toUtc);
            },
            sql,
            "GetChangedBetween",
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<Either<RepositoryError, IReadOnlyList<TEntity>>> ListAsOfAsync(
        Specification<TEntity> specification,
        DateTime asOfUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(specification);

        var preCheck = ValidateAndLog(
            asOfUtc,
            nameof(asOfUtc),
            () => Log.TemporalQueryListAsOf(_logger, typeof(TEntity).Name, asOfUtc));
        if (preCheck.IsLeft)
        {
            return preCheck.Map(_ => (IReadOnlyList<TEntity>)[]);
        }

        // SpecificationSqlBuilder.BuildWhereClause can throw NotSupportedException for an
        // expression it cannot translate; build the query string inside its own try/catch so that
        // failure is reported the same way as any other query failure (Either.Left), matching the
        // pre-refactor behavior where this was part of the method's single try block.
        string sql;
        Action<IDbCommand> addParameters;
        try
        {
            var sqlBuilder = new SpecificationSqlBuilder<TEntity>(_mapping.ColumnMappings);
            var (whereClause, addSpecificationParameters) = sqlBuilder.BuildWhereClause(specification);

            var columns = string.Join(", ", _mapping.ColumnMappings.Values.Select(c => $"\"{c}\""));
            var extraFilter = string.IsNullOrWhiteSpace(whereClause)
                ? string.Empty
                : $"AND {whereClause.Replace("WHERE ", "", StringComparison.OrdinalIgnoreCase)}";

            // Build temporal query with specification filter
            sql = $"""
                SELECT {columns}
                FROM (
                    SELECT {columns}, lower("{_mapping.PeriodStartColumnName}") as period_start, upper("{_mapping.PeriodStartColumnName}") as period_end
                    FROM {_mapping.TableName}
                    UNION ALL
                    SELECT {columns}, lower("{_mapping.PeriodStartColumnName}") as period_start, upper("{_mapping.PeriodStartColumnName}") as period_end
                    FROM {_mapping.HistoryTableName}
                ) AS temporal_data
                WHERE period_start <= @AsOfUtc AND (period_end IS NULL OR period_end > @AsOfUtc)
                {extraFilter}
                """;

            addParameters = command =>
            {
                AddParameter(command, "@AsOfUtc", asOfUtc);
                addSpecificationParameters(command);
            };
        }
        catch (Exception ex)
        {
            return Left<RepositoryError, IReadOnlyList<TEntity>>(
                RepositoryError.OperationFailed<TEntity>("ListAsOf", ex));
        }

        return await ExecuteListQueryAsync(addParameters, sql, "ListAsOf", cancellationToken).ConfigureAwait(false);
    }

    #endregion

    #region Shared Query Execution

    // Shared by GetAsOfAsync: opens the connection, runs the query, materializes at most one
    // entity from the reader, and maps a missing row / a thrown exception to the caller's Either.
    // Extracted so the four temporal query methods do not each repeat the try/catch and reader
    // loop, which is what pushed their cyclomatic complexity into the CRAP gate (#1325 follow-up).
    private async Task<Either<RepositoryError, TEntity>> ExecuteSingleEntityQueryAsync(
        string sql,
        Action<IDbCommand> addParameters,
        string operationName,
        Func<RepositoryError> notFoundError,
        CancellationToken cancellationToken)
    {
        try
        {
            await EnsureConnectionOpenAsync(cancellationToken).ConfigureAwait(false);

            using var command = CreateCommand(sql);
            addParameters(command);

            using var reader = await ExecuteReaderAsync(command, cancellationToken);
            if (await ReadAsync(reader, cancellationToken))
            {
                return Right<RepositoryError, TEntity>(MaterializeEntity(reader));
            }

            return Left<RepositoryError, TEntity>(notFoundError());
        }
        catch (Exception ex)
        {
            return Left<RepositoryError, TEntity>(
                RepositoryError.OperationFailed<TEntity>(operationName, ex));
        }
    }

    // Shared by GetHistoryAsync, GetChangedBetweenAsync and ListAsOfAsync: same shape as
    // ExecuteSingleEntityQueryAsync but materializes every row into a list.
    private async Task<Either<RepositoryError, IReadOnlyList<TEntity>>> ExecuteListQueryAsync(
        Action<IDbCommand> addParameters,
        string sql,
        string operationName,
        CancellationToken cancellationToken)
    {
        try
        {
            await EnsureConnectionOpenAsync(cancellationToken).ConfigureAwait(false);

            using var command = CreateCommand(sql);
            addParameters(command);

            using var reader = await ExecuteReaderAsync(command, cancellationToken);

            var results = new List<TEntity>();
            while (await ReadAsync(reader, cancellationToken))
            {
                results.Add(MaterializeEntity(reader));
            }

            return Right<RepositoryError, IReadOnlyList<TEntity>>(results);
        }
        catch (Exception ex)
        {
            return Left<RepositoryError, IReadOnlyList<TEntity>>(
                RepositoryError.OperationFailed<TEntity>(operationName, ex));
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
                    $"Use a TimeProvider's GetUtcNow().UtcDateTime for correct behavior.",
                    "REPOSITORY_INVALID_DATETIME_KIND",
                    typeof(TEntity)));
        }

        return Right<RepositoryError, Unit>(unit);
    }

    // Combines the UTC validation and the "log if enabled" check into a single synchronous call so
    // the async query methods (GetAsOfAsync, ListAsOfAsync) each carry a single branch instead of two
    // — the branching itself still happens here, in a plain method, rather than being duplicated
    // inside every async state machine the CRAP gate measures (#1325 follow-up).
    private Either<RepositoryError, Unit> ValidateAndLog(DateTime dateTime, string parameterName, Action logAction)
    {
        var validationResult = ValidateUtcDateTime(dateTime, parameterName);
        if (validationResult.IsLeft)
        {
            return validationResult;
        }

        if (_options.LogTemporalQueries)
        {
            logAction();
        }

        return Right<RepositoryError, Unit>(unit);
    }

    // Same purpose as ValidateAndLog, for GetChangedBetweenAsync's two-date range plus its
    // fromUtc/toUtc ordering check.
    private Either<RepositoryError, Unit> ValidateChangedBetweenRange(DateTime fromUtc, DateTime toUtc, Action logAction)
    {
        var fromValidation = ValidateUtcDateTime(fromUtc, nameof(fromUtc));
        if (fromValidation.IsLeft)
        {
            return fromValidation;
        }

        var toValidation = ValidateUtcDateTime(toUtc, nameof(toUtc));
        if (toValidation.IsLeft)
        {
            return toValidation;
        }

        if (fromUtc > toUtc)
        {
            return Left<RepositoryError, Unit>(
                new RepositoryError(
                    $"Invalid time range: fromUtc ({fromUtc:O}) must be less than or equal to toUtc ({toUtc:O})",
                    "REPOSITORY_INVALID_TIME_RANGE",
                    typeof(TEntity)));
        }

        if (_options.LogTemporalQueries)
        {
            logAction();
        }

        return Right<RepositoryError, Unit>(unit);
    }

    private async Task EnsureConnectionOpenAsync(CancellationToken cancellationToken)
    {
        if (_connection.State != ConnectionState.Open)
        {
            if (_connection is DbConnection dbConnection)
            {
                await dbConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await Task.Run(_connection.Open, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private IDbCommand CreateCommand(string sql)
    {
        var command = _connection.CreateCommand();
        command.CommandText = sql;
        command.CommandType = CommandType.Text;
        return command;
    }

    private static void AddParameter(IDbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }

    private static async Task<IDataReader> ExecuteReaderAsync(IDbCommand command, CancellationToken cancellationToken)
    {
        if (command is DbCommand dbCommand)
        {
            return await dbCommand.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        }

        return await Task.Run(command.ExecuteReader, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<object?> ExecuteScalarAsync(IDbCommand command, CancellationToken cancellationToken)
    {
        if (command is DbCommand dbCommand)
        {
            return await dbCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        }

        return await Task.Run(command.ExecuteScalar, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<bool> ReadAsync(IDataReader reader, CancellationToken cancellationToken)
    {
        if (reader is DbDataReader dbReader)
        {
            return await dbReader.ReadAsync(cancellationToken).ConfigureAwait(false);
        }

        return await Task.Run(reader.Read, cancellationToken).ConfigureAwait(false);
    }

    private TEntity MaterializeEntity(IDataReader reader)
    {
        var entity = new TEntity();

        foreach (var (propertyName, columnName) in _mapping.ColumnMappings)
        {
            if (!_propertyCache.TryGetValue(propertyName, out var property))
            {
                continue;
            }

            var ordinal = reader.GetOrdinal(columnName);
            if (reader.IsDBNull(ordinal))
            {
                continue;
            }

            var value = reader.GetValue(ordinal);
            var targetType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

            // Handle type conversions
            if (value.GetType() != targetType)
            {
                value = Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
            }

            property.SetValue(entity, value);
        }

        return entity;
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
        EventId = 3251,
        Level = LogLevel.Debug,
        Message = "Temporal query (AsOf): Entity={EntityType}, Id={EntityId}, AsOfUtc={AsOfUtc:O}")]
    public static partial void TemporalQueryAsOf(
        ILogger logger,
        string entityType,
        string entityId,
        DateTime asOfUtc);

    [LoggerMessage(
        EventId = 3252,
        Level = LogLevel.Debug,
        Message = "Temporal query (History): Entity={EntityType}, Id={EntityId}")]
    public static partial void TemporalQueryHistory(
        ILogger logger,
        string entityType,
        string entityId);

    [LoggerMessage(
        EventId = 3253,
        Level = LogLevel.Debug,
        Message = "Temporal query (Between): Entity={EntityType}, FromUtc={FromUtc:O}, ToUtc={ToUtc:O}")]
    public static partial void TemporalQueryBetween(
        ILogger logger,
        string entityType,
        DateTime fromUtc,
        DateTime toUtc);

    [LoggerMessage(
        EventId = 3254,
        Level = LogLevel.Debug,
        Message = "Temporal query (ListAsOf): Entity={EntityType}, AsOfUtc={AsOfUtc:O}")]
    public static partial void TemporalQueryListAsOf(
        ILogger logger,
        string entityType,
        DateTime asOfUtc);
}
