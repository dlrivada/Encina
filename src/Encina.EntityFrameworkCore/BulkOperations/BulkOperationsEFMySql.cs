using System.Data;
using System.Globalization;
using System.Reflection;
using System.Text;
using Encina.DomainModeling;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using MySqlConnector;
using static LanguageExt.Prelude;

namespace Encina.EntityFrameworkCore.BulkOperations;

/// <summary>
/// MySQL implementation of <see cref="IBulkOperations{TEntity}"/> using batched operations
/// for high-performance bulk operations.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <remarks>
/// <para>
/// This implementation uses MySQL-specific features for optimized bulk operations:
/// </para>
/// <list type="bullet">
/// <item><description>BulkInsert: Uses batched INSERT statements with parameterized values</description></item>
/// <item><description>BulkUpdate: Uses UPDATE statements with CASE expressions</description></item>
/// <item><description>BulkDelete: Uses DELETE with WHERE IN clause</description></item>
/// <item><description>BulkMerge: Uses INSERT ... ON DUPLICATE KEY UPDATE for upsert semantics</description></item>
/// </list>
/// </remarks>
public sealed class BulkOperationsEFMySql<TEntity> : IBulkOperations<TEntity>
    where TEntity : class, new()
{
    private readonly DbContext _dbContext;
    private readonly Dictionary<string, PropertyInfo> _propertyCache;
    private readonly string _tableName;
    private readonly string _idColumnName;
    private readonly string _idPropertyName;
    private readonly IReadOnlyDictionary<string, string> _columnMappings;

    /// <summary>
    /// Initializes a new instance of the <see cref="BulkOperationsEFMySql{TEntity}"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    public BulkOperationsEFMySql(DbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        _dbContext = dbContext;

        var entityType = dbContext.Model.FindEntityType(typeof(TEntity))
            ?? throw new InvalidOperationException($"Entity type '{typeof(TEntity).Name}' is not registered in the DbContext model.");

        var table = entityType.GetTableName() ?? typeof(TEntity).Name;
        _tableName = $"`{table}`";

        var primaryKey = entityType.FindPrimaryKey()
            ?? throw new InvalidOperationException($"Entity type '{typeof(TEntity).Name}' does not have a primary key defined.");

        var pkProperty = primaryKey.Properties[0];
        _idColumnName = pkProperty.GetColumnName() ?? pkProperty.Name;
        _idPropertyName = pkProperty.Name;

        _columnMappings = entityType.GetProperties()
            .ToDictionary(
                p => p.Name,
                p => p.GetColumnName() ?? p.Name);

        _propertyCache = typeof(TEntity).GetProperties()
            .Where(p => _columnMappings.ContainsKey(p.Name))
            .ToDictionary(p => p.Name);
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, int>> BulkInsertAsync(
        IEnumerable<TEntity> entities,
        BulkConfig? config = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entities);

        var entityList = entities.ToList();
        if (entityList.Count == 0)
            return Right<EncinaError, int>(0);

        config ??= BulkConfig.Default;

        try
        {
            var connection = _dbContext.Database.GetDbConnection();

            if (connection is not MySqlConnection mysqlConnection)
            {
                return Left<EncinaError, int>(
                    RepositoryErrors.BulkInsertFailed<TEntity>("This implementation requires a MySQL connection"));
            }

            await EnsureConnectionOpenAsync(mysqlConnection, cancellationToken).ConfigureAwait(false);

            var entityType = _dbContext.Model.FindEntityType(typeof(TEntity))!;
            var propertiesToInclude = GetFilteredProperties(config, forInsert: true, entityType);

            var inserted = 0;
            var batches = entityList.Chunk(config.BatchSize);

            foreach (var batch in batches)
            {
                inserted += await InsertBatchAsync(mysqlConnection, batch.ToList(), propertiesToInclude, cancellationToken).ConfigureAwait(false);
            }

            if (config.TrackingEntities)
            {
                foreach (var entity in entityList)
                {
                    _dbContext.Entry(entity).State = EntityState.Unchanged;
                }
            }

            return Right<EncinaError, int>(inserted);
        }
        catch (Exception ex) when (IsDuplicateKeyException(ex))
        {
            return Left<EncinaError, int>(
                RepositoryErrors.AlreadyExists<TEntity>("One or more entities already exist"));
        }
        catch (Exception ex)
        {
            return Left<EncinaError, int>(
                RepositoryErrors.BulkInsertFailed<TEntity>(entityList.Count, ex));
        }
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, int>> BulkUpdateAsync(
        IEnumerable<TEntity> entities,
        BulkConfig? config = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entities);

        var entityList = entities.ToList();
        if (entityList.Count == 0)
            return Right<EncinaError, int>(0);

        config ??= BulkConfig.Default;

        try
        {
            var connection = _dbContext.Database.GetDbConnection();

            if (connection is not MySqlConnection mysqlConnection)
            {
                return Left<EncinaError, int>(
                    RepositoryErrors.BulkUpdateFailed<TEntity>("This implementation requires a MySQL connection"));
            }

            await EnsureConnectionOpenAsync(mysqlConnection, cancellationToken).ConfigureAwait(false);

            var entityType = _dbContext.Model.FindEntityType(typeof(TEntity))!;
            var propertiesToInclude = GetFilteredProperties(config, forInsert: false, entityType)
                .Where(kvp => kvp.Value != _idColumnName)
                .ToList();

            var updated = 0;
            var batches = entityList.Chunk(config.BatchSize);

            foreach (var batch in batches)
            {
                updated += await UpdateBatchAsync(mysqlConnection, batch.ToList(), propertiesToInclude, cancellationToken).ConfigureAwait(false);
            }

            if (config.TrackingEntities)
            {
                foreach (var entity in entityList)
                {
                    _dbContext.Entry(entity).State = EntityState.Unchanged;
                }
            }

            return Right<EncinaError, int>(updated);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, int>(
                RepositoryErrors.BulkUpdateFailed<TEntity>(entityList.Count, ex));
        }
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, int>> BulkDeleteAsync(
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entities);

        var entityList = entities.ToList();
        if (entityList.Count == 0)
            return Right<EncinaError, int>(0);

        try
        {
            var connection = _dbContext.Database.GetDbConnection();

            if (connection is not MySqlConnection mysqlConnection)
            {
                return Left<EncinaError, int>(
                    RepositoryErrors.BulkDeleteFailed<TEntity>("This implementation requires a MySQL connection"));
            }

            await EnsureConnectionOpenAsync(mysqlConnection, cancellationToken).ConfigureAwait(false);

            var ids = entityList.Select(GetEntityId).ToList();

            var deleted = await DeleteByIdsAsync(mysqlConnection, ids, cancellationToken).ConfigureAwait(false);

            foreach (var entity in entityList)
            {
                _dbContext.Entry(entity).State = EntityState.Detached;
            }

            return Right<EncinaError, int>(deleted);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, int>(
                RepositoryErrors.BulkDeleteFailed<TEntity>(entityList.Count, ex));
        }
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, int>> BulkMergeAsync(
        IEnumerable<TEntity> entities,
        BulkConfig? config = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entities);

        var entityList = entities.ToList();
        if (entityList.Count == 0)
            return Right<EncinaError, int>(0);

        config ??= BulkConfig.Default;

        try
        {
            var connection = _dbContext.Database.GetDbConnection();

            if (connection is not MySqlConnection mysqlConnection)
            {
                return Left<EncinaError, int>(
                    RepositoryErrors.BulkMergeFailed<TEntity>("This implementation requires a MySQL connection"));
            }

            await EnsureConnectionOpenAsync(mysqlConnection, cancellationToken).ConfigureAwait(false);

            var entityType = _dbContext.Model.FindEntityType(typeof(TEntity))!;
            var propertiesToInclude = GetFilteredProperties(config, forInsert: false, entityType);

            var affected = 0;
            var batches = entityList.Chunk(config.BatchSize);

            foreach (var batch in batches)
            {
                affected += await MergeBatchAsync(mysqlConnection, batch.ToList(), propertiesToInclude, cancellationToken).ConfigureAwait(false);
            }

            if (config.TrackingEntities)
            {
                foreach (var entity in entityList)
                {
                    _dbContext.Entry(entity).State = EntityState.Unchanged;
                }
            }

            return Right<EncinaError, int>(affected);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, int>(
                RepositoryErrors.BulkMergeFailed<TEntity>(entityList.Count, ex));
        }
    }

    /// <inheritdoc/>
    public async Task<Either<EncinaError, IReadOnlyList<TEntity>>> BulkReadAsync(
        IEnumerable<object> ids,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ids);

        var idList = ids.ToList();
        if (idList.Count == 0)
            return Right<EncinaError, IReadOnlyList<TEntity>>(System.Array.Empty<TEntity>());

        try
        {
            var connection = _dbContext.Database.GetDbConnection();

            if (connection is not MySqlConnection mysqlConnection)
            {
                return Left<EncinaError, IReadOnlyList<TEntity>>(
                    RepositoryErrors.BulkReadFailed<TEntity>("This implementation requires a MySQL connection"));
            }

            await EnsureConnectionOpenAsync(mysqlConnection, cancellationToken).ConfigureAwait(false);

            var entities = await ReadByIdsAsync(mysqlConnection, idList, cancellationToken).ConfigureAwait(false);

            return Right<EncinaError, IReadOnlyList<TEntity>>(entities);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, IReadOnlyList<TEntity>>(
                RepositoryErrors.BulkReadFailed<TEntity>(idList.Count, ex));
        }
    }

    #region Batch Operations

    private async Task<int> InsertBatchAsync(
        MySqlConnection connection,
        List<TEntity> entities,
        List<KeyValuePair<string, string>> properties,
        CancellationToken cancellationToken)
    {
        var columnNames = string.Join(", ", properties.Select(p => $"`{p.Value}`"));
        var sql = new StringBuilder();
        sql.Append(CultureInfo.InvariantCulture, $"INSERT INTO {_tableName} ({columnNames}) VALUES ");

        var parameters = new List<MySqlParameter>();
        var valueClauses = new List<string>();

        for (var i = 0; i < entities.Count; i++)
        {
            var entity = entities[i];
            var paramNames = new List<string>();

            foreach (var (propertyName, _) in properties)
            {
                var paramName = $"@p{i}_{propertyName}";
                paramNames.Add(paramName);

                if (_propertyCache.TryGetValue(propertyName, out var propertyInfo))
                {
                    var value = propertyInfo.GetValue(entity);
                    parameters.Add(new MySqlParameter(paramName, value ?? DBNull.Value));
                }
            }

            valueClauses.Add($"({string.Join(", ", paramNames)})");
        }

        sql.Append(string.Join(", ", valueClauses));

        await using var command = connection.CreateCommand();
        command.CommandText = sql.ToString();
        command.Parameters.AddRange(parameters.ToArray());

        var currentTransaction = _dbContext.Database.CurrentTransaction;
        if (currentTransaction is not null)
        {
            command.Transaction = currentTransaction.GetDbTransaction() as MySqlTransaction;
        }

        return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<int> UpdateBatchAsync(
        MySqlConnection connection,
        List<TEntity> entities,
        List<KeyValuePair<string, string>> properties,
        CancellationToken cancellationToken)
    {
        var updated = 0;

        foreach (var entity in entities)
        {
            var setClauses = new List<string>();
            var parameters = new List<MySqlParameter>();

            foreach (var (propertyName, columnName) in properties)
            {
                var paramName = $"@{propertyName}";
                setClauses.Add($"`{columnName}` = {paramName}");

                if (_propertyCache.TryGetValue(propertyName, out var propertyInfo))
                {
                    var value = propertyInfo.GetValue(entity);
                    parameters.Add(new MySqlParameter(paramName, value ?? DBNull.Value));
                }
            }

            var idValue = GetEntityId(entity);
            parameters.Add(new MySqlParameter("@Id", idValue));

            var sql = $"UPDATE {_tableName} SET {string.Join(", ", setClauses)} WHERE `{_idColumnName}` = @Id";

            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.Parameters.AddRange(parameters.ToArray());

            var currentTransaction = _dbContext.Database.CurrentTransaction;
            if (currentTransaction is not null)
            {
                command.Transaction = currentTransaction.GetDbTransaction() as MySqlTransaction;
            }

            updated += await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        return updated;
    }

    private async Task<int> DeleteByIdsAsync(
        MySqlConnection connection,
        List<object> ids,
        CancellationToken cancellationToken)
    {
        var paramNames = new List<string>();
        var parameters = new List<MySqlParameter>();

        for (var i = 0; i < ids.Count; i++)
        {
            var paramName = $"@id{i}";
            paramNames.Add(paramName);
            parameters.Add(new MySqlParameter(paramName, ids[i]));
        }

        var sql = $"DELETE FROM {_tableName} WHERE `{_idColumnName}` IN ({string.Join(", ", paramNames)})";

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddRange(parameters.ToArray());

        var currentTransaction = _dbContext.Database.CurrentTransaction;
        if (currentTransaction is not null)
        {
            command.Transaction = currentTransaction.GetDbTransaction() as MySqlTransaction;
        }

        return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<int> MergeBatchAsync(
        MySqlConnection connection,
        List<TEntity> entities,
        List<KeyValuePair<string, string>> properties,
        CancellationToken cancellationToken)
    {
        // MySQL uses INSERT ... ON DUPLICATE KEY UPDATE for upsert
        var columnNames = string.Join(", ", properties.Select(p => $"`{p.Value}`"));
        var updateColumns = properties.Where(p => p.Value != _idColumnName).ToList();
        var updateClauses = string.Join(", ", updateColumns.Select(p => $"`{p.Value}` = VALUES(`{p.Value}`)"));

        var sql = new StringBuilder();
        sql.Append(CultureInfo.InvariantCulture, $"INSERT INTO {_tableName} ({columnNames}) VALUES ");

        var parameters = new List<MySqlParameter>();
        var valueClauses = new List<string>();

        for (var i = 0; i < entities.Count; i++)
        {
            var entity = entities[i];
            var paramNames = new List<string>();

            foreach (var (propertyName, _) in properties)
            {
                var paramName = $"@p{i}_{propertyName}";
                paramNames.Add(paramName);

                if (_propertyCache.TryGetValue(propertyName, out var propertyInfo))
                {
                    var value = propertyInfo.GetValue(entity);
                    parameters.Add(new MySqlParameter(paramName, value ?? DBNull.Value));
                }
            }

            valueClauses.Add($"({string.Join(", ", paramNames)})");
        }

        sql.Append(string.Join(", ", valueClauses));
        sql.Append(CultureInfo.InvariantCulture, $" ON DUPLICATE KEY UPDATE {updateClauses}");

        await using var command = connection.CreateCommand();
        command.CommandText = sql.ToString();
        command.Parameters.AddRange(parameters.ToArray());

        var currentTransaction = _dbContext.Database.CurrentTransaction;
        if (currentTransaction is not null)
        {
            command.Transaction = currentTransaction.GetDbTransaction() as MySqlTransaction;
        }

        return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<List<TEntity>> ReadByIdsAsync(
        MySqlConnection connection,
        List<object> ids,
        CancellationToken cancellationToken)
    {
        var paramNames = new List<string>();
        var parameters = new List<MySqlParameter>();

        for (var i = 0; i < ids.Count; i++)
        {
            var paramName = $"@id{i}";
            paramNames.Add(paramName);
            parameters.Add(new MySqlParameter(paramName, ids[i]));
        }

        var columns = string.Join(", ", _columnMappings.Values.Select(c => $"`{c}`"));
        var sql = $"SELECT {columns} FROM {_tableName} WHERE `{_idColumnName}` IN ({string.Join(", ", paramNames)})";

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddRange(parameters.ToArray());

        var currentTransaction = _dbContext.Database.CurrentTransaction;
        if (currentTransaction is not null)
        {
            command.Transaction = currentTransaction.GetDbTransaction() as MySqlTransaction;
        }

        var entities = new List<TEntity>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            entities.Add(MapReaderToEntity(reader));
        }

        return entities;
    }

    #endregion

    #region Entity ID Extraction

    private object GetEntityId(TEntity entity)
    {
        if (_propertyCache.TryGetValue(_idPropertyName, out var property))
        {
            return property.GetValue(entity)!;
        }

        var entityType = _dbContext.Model.FindEntityType(typeof(TEntity))!;
        var primaryKey = entityType.FindPrimaryKey()!;
        var keyProperty = primaryKey.Properties[0];

        return _dbContext.Entry(entity).Property(keyProperty.Name).CurrentValue!;
    }

    #endregion

    #region Property Filtering

    private List<KeyValuePair<string, string>> GetFilteredProperties(
        BulkConfig config,
        bool forInsert,
        Microsoft.EntityFrameworkCore.Metadata.IEntityType entityType)
    {
        var properties = _columnMappings.ToList();

        if (forInsert)
        {
            var valueGenerated = entityType.GetProperties()
                .Where(p => p.ValueGenerated != Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.Never)
                .Select(p => p.Name)
                .ToHashSet();

            properties = properties
                .Where(kvp => !valueGenerated.Contains(kvp.Key))
                .ToList();
        }

        if (config.PropertiesToInclude is { Count: > 0 })
        {
            properties = properties
                .Where(kvp => config.PropertiesToInclude.Contains(kvp.Key))
                .ToList();
        }
        else if (config.PropertiesToExclude is { Count: > 0 })
        {
            properties = properties
                .Where(kvp => !config.PropertiesToExclude.Contains(kvp.Key))
                .ToList();
        }

        return properties;
    }

    #endregion

    #region Entity Materialization

    private TEntity MapReaderToEntity(MySqlDataReader reader)
    {
        var entity = new TEntity();

        foreach (var (propertyName, columnName) in _columnMappings)
        {
            if (!_propertyCache.TryGetValue(propertyName, out var property))
                continue;

            var ordinal = reader.GetOrdinal(columnName);
            if (reader.IsDBNull(ordinal))
                continue;

            var value = reader.GetValue(ordinal);
            var convertedValue = ConvertValue(value, property.PropertyType);
            property.SetValue(entity, convertedValue);
        }

        return entity;
    }

    private static object? ConvertValue(object value, Type targetType)
    {
        if (value is DBNull)
            return null;

        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (underlyingType == value.GetType())
            return value;

        if (underlyingType.IsEnum)
            return Enum.ToObject(underlyingType, value);

        // MySQL stores booleans as tinyint(1)
        if (underlyingType == typeof(bool))
        {
            if (value is sbyte sb)
                return sb != 0;
            if (value is byte b)
                return b != 0;
            if (value is short s)
                return s != 0;
            if (value is int i)
                return i != 0;
            if (value is long l)
                return l != 0;
        }

        return Convert.ChangeType(value, underlyingType, System.Globalization.CultureInfo.InvariantCulture);
    }

    #endregion

    #region Async Helpers

    private static async Task EnsureConnectionOpenAsync(MySqlConnection connection, CancellationToken cancellationToken)
    {
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private static bool IsDuplicateKeyException(Exception ex)
    {
        var message = ex.Message;
        return message.Contains("Duplicate entry", StringComparison.OrdinalIgnoreCase)
            || message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase)
            || message.Contains("1062", StringComparison.OrdinalIgnoreCase); // MySQL error code for duplicate entry
    }

    #endregion
}
