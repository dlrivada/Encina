using System.Data;
using System.Globalization;
using System.Reflection;
using Encina.ADO.SqlServer.Repository;
using Encina.DomainModeling;
using Encina.Messaging.Temporal;
using LanguageExt;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace Encina.ADO.SqlServer.Temporal;

/// <summary>
/// ADO.NET implementation of <see cref="ITemporalRepository{TEntity, TId}"/>
/// with support for SQL Server temporal tables (system-versioned tables).
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TId">The entity identifier type.</typeparam>
/// <remarks>
/// <para>
/// This repository provides point-in-time query capabilities for entities stored in
/// SQL Server temporal tables using raw ADO.NET commands for maximum performance.
/// </para>
/// <para>
/// <b>Requirements</b>:
/// <list type="bullet">
/// <item><description>SQL Server 2016 or later</description></item>
/// <item><description>Tables configured as system-versioned temporal tables</description></item>
/// <item><description>A valid <see cref="ITemporalEntityMapping{TEntity, TId}"/> configuration</description></item>
/// </list>
/// </para>
/// <para>
/// <b>SQL Server Temporal Tables</b>: SQL Server automatically maintains a history table
/// with all previous versions of rows. This repository uses the <c>FOR SYSTEM_TIME</c> clause
/// to query historical data.
/// </para>
/// <para>
/// <b>Important</b>: All DateTime parameters must be in UTC. When <see cref="TemporalTableOptions.ValidateUtcDateTime"/>
/// is enabled (default), the repository validates that DateTime parameters have <see cref="DateTimeKind.Utc"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Configure temporal mapping
/// services.AddEncinaTemporalRepository&lt;Order, Guid&gt;(mapping =&gt;
/// {
///     mapping.ToTable("Orders")
///         .HasId(o =&gt; o.Id)
///         .MapProperty(o =&gt; o.CustomerId, "CustomerId")
///         .WithPeriodColumns("PeriodStart", "PeriodEnd");
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
        EnsureConnectionOpen();

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
        EnsureConnectionOpen();

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

        EnsureConnectionOpen();

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

        EnsureConnectionOpen();

        var sqlBuilder = new SpecificationSqlBuilder<TEntity>(_mapping.ColumnMappings);
        var (whereClause, addParameters) = sqlBuilder.BuildWhereClause(specification);
        var columns = string.Join(", ", _mapping.ColumnMappings.Values.Select(c => $"[{c}]"));
        var sql = $"SELECT TOP 1 {columns} FROM {_mapping.TableName} {whereClause}";

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

        // For expression-based queries, filter in memory
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

        EnsureConnectionOpen();

        var sqlBuilder = new SpecificationSqlBuilder<TEntity>(_mapping.ColumnMappings);
        var (whereClause, addParameters) = sqlBuilder.BuildWhereClause(specification);
        var sql = $"SELECT CASE WHEN EXISTS (SELECT 1 FROM {_mapping.TableName} {whereClause}) THEN 1 ELSE 0 END";

        using var command = CreateCommand(sql);
        addParameters(command);

        var result = await ExecuteScalarAsync(command, cancellationToken);
        return Convert.ToInt32(result, CultureInfo.InvariantCulture) == 1;
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

        EnsureConnectionOpen();

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
        EnsureConnectionOpen();

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

        var columns = string.Join(", ", _mapping.ColumnMappings.Values.Select(c => $"[{c}]"));
        var sql = $"""
            SELECT {columns}
            FROM {_mapping.TableName}
            ORDER BY [{_mapping.IdColumnName}]
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """;

        EnsureConnectionOpen();

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

        var sqlBuilder = new SpecificationSqlBuilder<TEntity>(_mapping.ColumnMappings);
        var (whereClause, addParameters) = sqlBuilder.BuildWhereClause(specification);

        // Count total
        var totalCount = await CountAsync(specification, cancellationToken);

        // Fetch page
        var offset = (pageNumber - 1) * pageSize;
        var columns = string.Join(", ", _mapping.ColumnMappings.Values.Select(c => $"[{c}]"));
        var sql = $"""
            SELECT {columns}
            FROM {_mapping.TableName}
            {whereClause}
            ORDER BY [{_mapping.IdColumnName}]
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """;

        EnsureConnectionOpen();

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

            EnsureConnectionOpen();

            var columns = string.Join(", ", _mapping.ColumnMappings.Values.Select(c => $"[{c}]"));
            var sql = $"""
                SELECT {columns}
                FROM {_mapping.TableName}
                FOR SYSTEM_TIME AS OF @AsOfUtc
                WHERE [{_mapping.IdColumnName}] = @Id
                """;

            using var command = CreateCommand(sql);
            AddParameter(command, "@Id", id);
            AddParameter(command, "@AsOfUtc", asOfUtc);

            using var reader = await ExecuteReaderAsync(command, cancellationToken);
            if (await ReadAsync(reader, cancellationToken))
            {
                return Right<RepositoryError, TEntity>(MaterializeEntity(reader));
            }

            return Left<RepositoryError, TEntity>(
                RepositoryError.NotFound<TEntity, TId>(id!));
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

            EnsureConnectionOpen();

            var columns = string.Join(", ", _mapping.ColumnMappings.Values.Select(c => $"[{c}]"));
            var sql = $"""
                SELECT {columns}, [{_mapping.PeriodStartColumnName}], [{_mapping.PeriodEndColumnName}]
                FROM {_mapping.TableName}
                FOR SYSTEM_TIME ALL
                WHERE [{_mapping.IdColumnName}] = @Id
                ORDER BY [{_mapping.PeriodStartColumnName}] DESC
                """;

            using var command = CreateCommand(sql);
            AddParameter(command, "@Id", id);

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

            EnsureConnectionOpen();

            var columns = string.Join(", ", _mapping.ColumnMappings.Values.Select(c => $"[{c}]"));
            var sql = $"""
                SELECT {columns}
                FROM {_mapping.TableName}
                FOR SYSTEM_TIME BETWEEN @FromUtc AND @ToUtc
                """;

            using var command = CreateCommand(sql);
            AddParameter(command, "@FromUtc", fromUtc);
            AddParameter(command, "@ToUtc", toUtc);

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

            EnsureConnectionOpen();

            var sqlBuilder = new SpecificationSqlBuilder<TEntity>(_mapping.ColumnMappings);
            var (whereClause, addParameters) = sqlBuilder.BuildWhereClause(specification);

            var columns = string.Join(", ", _mapping.ColumnMappings.Values.Select(c => $"[{c}]"));
            var sql = $"""
                SELECT {columns}
                FROM {_mapping.TableName}
                FOR SYSTEM_TIME AS OF @AsOfUtc
                {whereClause}
                """;

            using var command = CreateCommand(sql);
            AddParameter(command, "@AsOfUtc", asOfUtc);
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

    private void EnsureConnectionOpen()
    {
        if (_connection.State != ConnectionState.Open)
        {
            _connection.Open();
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
        if (command is SqlCommand sqlCommand)
        {
            return await sqlCommand.ExecuteReaderAsync(cancellationToken);
        }

        return command.ExecuteReader();
    }

    private static async Task<object?> ExecuteScalarAsync(IDbCommand command, CancellationToken cancellationToken)
    {
        if (command is SqlCommand sqlCommand)
        {
            return await sqlCommand.ExecuteScalarAsync(cancellationToken);
        }

        return command.ExecuteScalar();
    }

    private static async Task<bool> ReadAsync(IDataReader reader, CancellationToken cancellationToken)
    {
        if (reader is SqlDataReader sqlReader)
        {
            return await sqlReader.ReadAsync(cancellationToken);
        }

        return reader.Read();
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
        var columns = string.Join(", ", _mapping.ColumnMappings.Values.Select(c => $"[{c}]"));
        return $"SELECT {columns} FROM {_mapping.TableName} WHERE [{_mapping.IdColumnName}] = @Id";
    }

    private string BuildSelectAllSql()
    {
        var columns = string.Join(", ", _mapping.ColumnMappings.Values.Select(c => $"[{c}]"));
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
        EventId = 400,
        Level = LogLevel.Debug,
        Message = "Temporal query (AsOf): Entity={EntityType}, Id={EntityId}, AsOfUtc={AsOfUtc:O}")]
    public static partial void TemporalQueryAsOf(
        ILogger logger,
        string entityType,
        string entityId,
        DateTime asOfUtc);

    [LoggerMessage(
        EventId = 401,
        Level = LogLevel.Debug,
        Message = "Temporal query (History): Entity={EntityType}, Id={EntityId}")]
    public static partial void TemporalQueryHistory(
        ILogger logger,
        string entityType,
        string entityId);

    [LoggerMessage(
        EventId = 402,
        Level = LogLevel.Debug,
        Message = "Temporal query (Between): Entity={EntityType}, FromUtc={FromUtc:O}, ToUtc={ToUtc:O}")]
    public static partial void TemporalQueryBetween(
        ILogger logger,
        string entityType,
        DateTime fromUtc,
        DateTime toUtc);

    [LoggerMessage(
        EventId = 403,
        Level = LogLevel.Debug,
        Message = "Temporal query (ListAsOf): Entity={EntityType}, AsOfUtc={AsOfUtc:O}")]
    public static partial void TemporalQueryListAsOf(
        ILogger logger,
        string entityType,
        DateTime asOfUtc);
}
