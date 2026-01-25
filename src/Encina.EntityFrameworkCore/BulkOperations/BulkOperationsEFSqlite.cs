using System.Data;
using System.Globalization;
using System.Reflection;
using System.Text;
using Encina.DomainModeling;
using LanguageExt;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using static LanguageExt.Prelude;

namespace Encina.EntityFrameworkCore.BulkOperations;

/// <summary>
/// SQLite implementation of <see cref="IBulkOperations{TEntity}"/> using batched INSERT statements
/// for high-performance bulk operations.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <remarks>
/// <para>
/// This implementation uses transaction-batched INSERT/UPDATE/DELETE statements optimized for SQLite.
/// SQLite doesn't have native bulk copy, but batched operations within a transaction provide
/// significant performance improvements over row-by-row operations.
/// </para>
/// <list type="bullet">
/// <item><description>BulkInsert: Uses batched INSERT statements with parameterized values</description></item>
/// <item><description>BulkUpdate: Uses UPDATE statements with WHERE IN clause</description></item>
/// <item><description>BulkDelete: Uses DELETE with WHERE IN clause</description></item>
/// <item><description>BulkMerge: Uses INSERT OR REPLACE for upsert semantics</description></item>
/// </list>
/// </remarks>
public sealed class BulkOperationsEFSqlite<TEntity> : IBulkOperations<TEntity>
    where TEntity : class, new()
{
    private readonly DbContext _dbContext;
    private readonly Dictionary<string, PropertyInfo> _propertyCache;
    private readonly string _tableName;
    private readonly string _idColumnName;
    private readonly string _idPropertyName;
    private readonly IReadOnlyDictionary<string, string> _columnMappings;

    /// <summary>
    /// Initializes a new instance of the <see cref="BulkOperationsEFSqlite{TEntity}"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    public BulkOperationsEFSqlite(DbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        _dbContext = dbContext;

        var entityType = dbContext.Model.FindEntityType(typeof(TEntity))
            ?? throw new InvalidOperationException($"Entity type '{typeof(TEntity).Name}' is not registered in the DbContext model.");

        _tableName = entityType.GetTableName() ?? typeof(TEntity).Name;

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

            if (connection is not SqliteConnection sqliteConnection)
            {
                return Left<EncinaError, int>(
                    RepositoryErrors.BulkInsertFailed<TEntity>("This implementation requires a SQLite connection"));
            }

            await EnsureConnectionOpenAsync(sqliteConnection, cancellationToken).ConfigureAwait(false);

            var entityType = _dbContext.Model.FindEntityType(typeof(TEntity))!;
            var propertiesToInclude = GetFilteredProperties(config, forInsert: true, entityType);

            var inserted = 0;
            var batches = entityList.Chunk(config.BatchSize);

            foreach (var batch in batches)
            {
                inserted += await InsertBatchAsync(sqliteConnection, batch.ToList(), propertiesToInclude, cancellationToken).ConfigureAwait(false);
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

            if (connection is not SqliteConnection sqliteConnection)
            {
                return Left<EncinaError, int>(
                    RepositoryErrors.BulkUpdateFailed<TEntity>("This implementation requires a SQLite connection"));
            }

            await EnsureConnectionOpenAsync(sqliteConnection, cancellationToken).ConfigureAwait(false);

            var entityType = _dbContext.Model.FindEntityType(typeof(TEntity))!;
            var propertiesToInclude = GetFilteredProperties(config, forInsert: false, entityType)
                .Where(kvp => kvp.Value != _idColumnName)
                .ToList();

            var updated = 0;
            var batches = entityList.Chunk(config.BatchSize);

            foreach (var batch in batches)
            {
                updated += await UpdateBatchAsync(sqliteConnection, batch.ToList(), propertiesToInclude, cancellationToken).ConfigureAwait(false);
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

            if (connection is not SqliteConnection sqliteConnection)
            {
                return Left<EncinaError, int>(
                    RepositoryErrors.BulkDeleteFailed<TEntity>("This implementation requires a SQLite connection"));
            }

            await EnsureConnectionOpenAsync(sqliteConnection, cancellationToken).ConfigureAwait(false);

            var ids = entityList.Select(GetEntityId).ToList();

            var deleted = await DeleteByIdsAsync(sqliteConnection, ids, cancellationToken).ConfigureAwait(false);

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

            if (connection is not SqliteConnection sqliteConnection)
            {
                return Left<EncinaError, int>(
                    RepositoryErrors.BulkMergeFailed<TEntity>("This implementation requires a SQLite connection"));
            }

            await EnsureConnectionOpenAsync(sqliteConnection, cancellationToken).ConfigureAwait(false);

            var entityType = _dbContext.Model.FindEntityType(typeof(TEntity))!;
            var propertiesToInclude = GetFilteredProperties(config, forInsert: false, entityType);

            var affected = 0;
            var batches = entityList.Chunk(config.BatchSize);

            foreach (var batch in batches)
            {
                affected += await MergeBatchAsync(sqliteConnection, batch.ToList(), propertiesToInclude, cancellationToken).ConfigureAwait(false);
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

            if (connection is not SqliteConnection sqliteConnection)
            {
                return Left<EncinaError, IReadOnlyList<TEntity>>(
                    RepositoryErrors.BulkReadFailed<TEntity>("This implementation requires a SQLite connection"));
            }

            await EnsureConnectionOpenAsync(sqliteConnection, cancellationToken).ConfigureAwait(false);

            var entities = await ReadByIdsAsync(sqliteConnection, idList, cancellationToken).ConfigureAwait(false);

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
        SqliteConnection connection,
        List<TEntity> entities,
        List<KeyValuePair<string, string>> properties,
        CancellationToken cancellationToken)
    {
        var columnNames = string.Join(", ", properties.Select(p => $"\"{p.Value}\""));
        var sql = new StringBuilder();
        sql.Append(CultureInfo.InvariantCulture, $"INSERT INTO \"{_tableName}\" ({columnNames}) VALUES ");

        var parameters = new List<SqliteParameter>();
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
                    // Convert GUIDs to strings for SQLite storage consistency
                    if (value is Guid guid)
                    {
                        value = guid.ToString();
                    }
                    parameters.Add(new SqliteParameter(paramName, value ?? DBNull.Value));
                }
            }

            valueClauses.Add($"({string.Join(", ", paramNames)})");
        }

        sql.Append(string.Join(", ", valueClauses));

        using var command = connection.CreateCommand();
        command.CommandText = sql.ToString();
        command.Parameters.AddRange(parameters.ToArray());

        var currentTransaction = _dbContext.Database.CurrentTransaction;
        if (currentTransaction is not null)
        {
            command.Transaction = currentTransaction.GetDbTransaction() as SqliteTransaction;
        }

        return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<int> UpdateBatchAsync(
        SqliteConnection connection,
        List<TEntity> entities,
        List<KeyValuePair<string, string>> properties,
        CancellationToken cancellationToken)
    {
        var updated = 0;

        foreach (var entity in entities)
        {
            var setClauses = new List<string>();
            var parameters = new List<SqliteParameter>();

            foreach (var (propertyName, columnName) in properties)
            {
                var paramName = $"@{propertyName}";
                setClauses.Add($"\"{columnName}\" = {paramName}");

                if (_propertyCache.TryGetValue(propertyName, out var propertyInfo))
                {
                    var value = propertyInfo.GetValue(entity);
                    // Convert GUIDs to strings for SQLite storage consistency
                    if (value is Guid guid)
                    {
                        value = guid.ToString();
                    }
                    parameters.Add(new SqliteParameter(paramName, value ?? DBNull.Value));
                }
            }

            object idValue = GetEntityId(entity);
            // Convert GUIDs to strings for SQLite storage consistency
            if (idValue is Guid idGuid)
            {
                idValue = idGuid.ToString();
            }
            parameters.Add(new SqliteParameter("@Id", idValue));

            var sql = $"UPDATE \"{_tableName}\" SET {string.Join(", ", setClauses)} WHERE \"{_idColumnName}\" = @Id";

            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.Parameters.AddRange(parameters.ToArray());

            var currentTransaction = _dbContext.Database.CurrentTransaction;
            if (currentTransaction is not null)
            {
                command.Transaction = currentTransaction.GetDbTransaction() as SqliteTransaction;
            }

            updated += await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        return updated;
    }

    private async Task<int> DeleteByIdsAsync(
        SqliteConnection connection,
        List<object> ids,
        CancellationToken cancellationToken)
    {
        var paramNames = new List<string>();
        var parameters = new List<SqliteParameter>();

        for (var i = 0; i < ids.Count; i++)
        {
            var paramName = $"@id{i}";
            paramNames.Add(paramName);
            object idValue = ids[i];
            // Convert GUIDs to strings for SQLite storage consistency
            if (idValue is Guid idGuid)
            {
                idValue = idGuid.ToString();
            }
            parameters.Add(new SqliteParameter(paramName, idValue));
        }

        var sql = $"DELETE FROM \"{_tableName}\" WHERE \"{_idColumnName}\" IN ({string.Join(", ", paramNames)})";

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddRange(parameters.ToArray());

        var currentTransaction = _dbContext.Database.CurrentTransaction;
        if (currentTransaction is not null)
        {
            command.Transaction = currentTransaction.GetDbTransaction() as SqliteTransaction;
        }

        return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<int> MergeBatchAsync(
        SqliteConnection connection,
        List<TEntity> entities,
        List<KeyValuePair<string, string>> properties,
        CancellationToken cancellationToken)
    {
        // SQLite uses INSERT OR REPLACE for upsert
        var columnNames = string.Join(", ", properties.Select(p => $"\"{p.Value}\""));
        var sql = new StringBuilder();
        sql.Append(CultureInfo.InvariantCulture, $"INSERT OR REPLACE INTO \"{_tableName}\" ({columnNames}) VALUES ");

        var parameters = new List<SqliteParameter>();
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
                    // Convert GUIDs to strings for SQLite storage consistency
                    if (value is Guid guid)
                    {
                        value = guid.ToString();
                    }
                    parameters.Add(new SqliteParameter(paramName, value ?? DBNull.Value));
                }
            }

            valueClauses.Add($"({string.Join(", ", paramNames)})");
        }

        sql.Append(string.Join(", ", valueClauses));

        using var command = connection.CreateCommand();
        command.CommandText = sql.ToString();
        command.Parameters.AddRange(parameters.ToArray());

        var currentTransaction = _dbContext.Database.CurrentTransaction;
        if (currentTransaction is not null)
        {
            command.Transaction = currentTransaction.GetDbTransaction() as SqliteTransaction;
        }

        return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<List<TEntity>> ReadByIdsAsync(
        SqliteConnection connection,
        List<object> ids,
        CancellationToken cancellationToken)
    {
        var paramNames = new List<string>();
        var parameters = new List<SqliteParameter>();

        for (var i = 0; i < ids.Count; i++)
        {
            var paramName = $"@id{i}";
            paramNames.Add(paramName);
            object idValue = ids[i];
            // Convert GUIDs to strings for SQLite storage consistency
            if (idValue is Guid idGuid)
            {
                idValue = idGuid.ToString();
            }
            parameters.Add(new SqliteParameter(paramName, idValue));
        }

        var columns = string.Join(", ", _columnMappings.Values.Select(c => $"\"{c}\""));
        var sql = $"SELECT {columns} FROM \"{_tableName}\" WHERE \"{_idColumnName}\" IN ({string.Join(", ", paramNames)})";

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddRange(parameters.ToArray());

        var currentTransaction = _dbContext.Database.CurrentTransaction;
        if (currentTransaction is not null)
        {
            command.Transaction = currentTransaction.GetDbTransaction() as SqliteTransaction;
        }

        var entities = new List<TEntity>();
        using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

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

    private TEntity MapReaderToEntity(SqliteDataReader reader)
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

        // SQLite stores booleans as integers
        if (underlyingType == typeof(bool) && value is long longVal)
            return longVal != 0;

        // SQLite stores decimals as REAL (double)
        if (underlyingType == typeof(decimal) && value is double doubleVal)
            return Convert.ToDecimal(doubleVal);

        // SQLite stores DateTime as TEXT or REAL
        if (underlyingType == typeof(DateTime) && value is string strVal)
            return DateTime.Parse(strVal, System.Globalization.CultureInfo.InvariantCulture);

        // SQLite stores Guid as BLOB or TEXT
        if (underlyingType == typeof(Guid))
        {
            if (value is byte[] bytes)
                return new Guid(bytes);
            if (value is string guidStr)
                return Guid.Parse(guidStr);
        }

        return Convert.ChangeType(value, underlyingType, System.Globalization.CultureInfo.InvariantCulture);
    }

    #endregion

    #region Async Helpers

    private static async Task EnsureConnectionOpenAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private static bool IsDuplicateKeyException(Exception ex)
    {
        var message = ex.Message;
        return message.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase)
            || message.Contains("PRIMARY KEY constraint failed", StringComparison.OrdinalIgnoreCase)
            || message.Contains("duplicate", StringComparison.OrdinalIgnoreCase);
    }

    #endregion
}
