using System.Data;
using System.Globalization;
using System.Reflection;
using System.Text;
using Encina;
using Encina.ADO.MySQL.Repository;
using Encina.DomainModeling;
using LanguageExt;
using MySqlConnector;
using static LanguageExt.Prelude;

namespace Encina.ADO.MySQL.BulkOperations;

/// <summary>
/// MySQL implementation of <see cref="IBulkOperations{TEntity}"/> using MySqlBulkCopy
/// for high-performance bulk operations.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TId">The entity identifier type.</typeparam>
/// <remarks>
/// <para>
/// This implementation leverages MySQL's bulk loading mechanisms for high-performance
/// bulk operations. Uses MySqlBulkCopy for inserts and multi-row statements for
/// other operations.
/// </para>
/// <list type="bullet">
/// <item><description><c>MySqlBulkCopy</c> for BulkInsert operations</description></item>
/// <item><description>Multi-row UPDATE with temporary table for BulkUpdate operations</description></item>
/// <item><description>Multi-row DELETE with IN clause for BulkDelete operations</description></item>
/// <item><description>INSERT ... ON DUPLICATE KEY UPDATE for BulkMerge (upsert) operations</description></item>
/// </list>
/// <para>
/// The implementation supports transaction participation through <c>MySqlTransaction</c>.
/// </para>
/// </remarks>
public sealed class BulkOperationsMySQL<TEntity, TId> : IBulkOperations<TEntity>
    where TEntity : class, new()
    where TId : notnull
{
    private readonly IDbConnection _connection;
    private readonly IEntityMapping<TEntity, TId> _mapping;
    private readonly Dictionary<string, PropertyInfo> _propertyCache;

    /// <summary>
    /// Initializes a new instance of the <see cref="BulkOperationsMySQL{TEntity, TId}"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="mapping">The entity mapping configuration.</param>
    public BulkOperationsMySQL(IDbConnection connection, IEntityMapping<TEntity, TId> mapping)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(mapping);

        _connection = connection;
        _mapping = mapping;
        _propertyCache = typeof(TEntity)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead)
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
            await EnsureConnectionOpenAsync(cancellationToken).ConfigureAwait(false);

            if (_connection is not MySqlConnection mysqlConnection)
            {
                return Left<EncinaError, int>(
                    RepositoryErrors.BulkInsertFailed<TEntity>("MySqlBulkCopy requires a MySqlConnection"));
            }

            var columnsToInsert = GetFilteredProperties(config, _mapping.InsertExcludedProperties);
            using var dataTable = CreateDataTable(entityList, columnsToInsert);

            var bulkCopy = new MySqlBulkCopy(mysqlConnection)
            {
                DestinationTableName = _mapping.TableName,
                BulkCopyTimeout = config.BulkCopyTimeout ?? 0
            };

            foreach (var (_, columnName) in columnsToInsert)
            {
                var ordinal = dataTable.Columns.IndexOf(columnName);
                if (ordinal >= 0)
                {
                    bulkCopy.ColumnMappings.Add(new MySqlBulkCopyColumnMapping(ordinal, columnName));
                }
            }

            var result = await bulkCopy.WriteToServerAsync(dataTable, cancellationToken).ConfigureAwait(false);

            return Right<EncinaError, int>(result.RowsInserted);
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
            await EnsureConnectionOpenAsync(cancellationToken).ConfigureAwait(false);

            if (_connection is not MySqlConnection mysqlConnection)
            {
                return Left<EncinaError, int>(
                    RepositoryErrors.BulkUpdateFailed<TEntity>("Bulk update requires a MySqlConnection"));
            }

            var tempTableName = $"_bulk_update_{Guid.NewGuid():N}";
            var columnsToUpdate = GetFilteredProperties(config, _mapping.UpdateExcludedProperties, forUpdate: true);

            // Create temp table
            await CreateTempTableAsync(mysqlConnection, tempTableName, columnsToUpdate, cancellationToken)
                .ConfigureAwait(false);

            // Insert data to temp table using MySqlBulkCopy
            await BulkInsertToTempTableAsync(mysqlConnection, tempTableName, entityList, columnsToUpdate, config, cancellationToken)
                .ConfigureAwait(false);

            // UPDATE from temp table
            var setClauses = columnsToUpdate
                .Where(kvp => kvp.Value != _mapping.IdColumnName)
                .Select(kvp => $"t.`{kvp.Value}` = s.`{kvp.Value}`");

            var updateSql = $@"
                UPDATE {_mapping.TableName} t
                INNER JOIN {tempTableName} s ON t.`{_mapping.IdColumnName}` = s.`{_mapping.IdColumnName}`
                SET {string.Join(", ", setClauses)}";

            await using var cmd = mysqlConnection.CreateCommand();
            cmd.CommandText = updateSql;
            var affected = await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

            // Drop temp table
            cmd.CommandText = $"DROP TABLE IF EXISTS {tempTableName}";
            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

            return Right<EncinaError, int>(affected);
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
            await EnsureConnectionOpenAsync(cancellationToken).ConfigureAwait(false);

            if (_connection is not MySqlConnection mysqlConnection)
            {
                return Left<EncinaError, int>(
                    RepositoryErrors.BulkDeleteFailed<TEntity>("Bulk delete requires a MySqlConnection"));
            }

            var ids = entityList.Select(e => _mapping.GetId(e)).ToList();

            // Use parameterized query with IN clause for delete
            var parameters = new StringBuilder();
            await using var cmd = mysqlConnection.CreateCommand();

            for (var i = 0; i < ids.Count; i++)
            {
                var paramName = $"@id{i}";
                if (i > 0) parameters.Append(", ");
                parameters.Append(paramName);
                cmd.Parameters.AddWithValue(paramName, ids[i]);
            }

            cmd.CommandText = $"DELETE FROM {_mapping.TableName} WHERE `{_mapping.IdColumnName}` IN ({parameters})";
            var affected = await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

            return Right<EncinaError, int>(affected);
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
            await EnsureConnectionOpenAsync(cancellationToken).ConfigureAwait(false);

            if (_connection is not MySqlConnection mysqlConnection)
            {
                return Left<EncinaError, int>(
                    RepositoryErrors.BulkMergeFailed<TEntity>("Bulk merge requires a MySqlConnection"));
            }

            var columnsForInsert = GetFilteredProperties(config, _mapping.InsertExcludedProperties);
            var columnsForUpdate = GetFilteredProperties(config, _mapping.UpdateExcludedProperties, forUpdate: true);

            // Build INSERT ... ON DUPLICATE KEY UPDATE
            var columnNames = string.Join(", ", columnsForInsert.Select(kvp => $"`{kvp.Value}`"));
            var updateClauses = columnsForUpdate
                .Where(kvp => kvp.Value != _mapping.IdColumnName)
                .Select(kvp => $"`{kvp.Value}` = VALUES(`{kvp.Value}`)");

            var valuePlaceholders = new StringBuilder();
            await using var cmd = mysqlConnection.CreateCommand();
            var paramIndex = 0;

            foreach (var entity in entityList)
            {
                if (valuePlaceholders.Length > 0)
                    valuePlaceholders.Append(", ");

                valuePlaceholders.Append('(');
                var first = true;
                foreach (var (propertyName, _) in columnsForInsert)
                {
                    if (!first) valuePlaceholders.Append(", ");
                    first = false;

                    var paramName = $"@p{paramIndex++}";
                    valuePlaceholders.Append(paramName);

                    if (_propertyCache.TryGetValue(propertyName, out var property))
                    {
                        var value = property.GetValue(entity);
                        cmd.Parameters.AddWithValue(paramName, value ?? DBNull.Value);
                    }
                }
                valuePlaceholders.Append(')');
            }

            cmd.CommandText = $@"
                INSERT INTO {_mapping.TableName} ({columnNames})
                VALUES {valuePlaceholders}
                ON DUPLICATE KEY UPDATE {string.Join(", ", updateClauses)}";

            var affected = await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

            // MySQL returns 1 for insert, 2 for update in ON DUPLICATE KEY UPDATE
            // So we normalize to count of actual entities affected
            return Right<EncinaError, int>(Math.Min(affected, entityList.Count));
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
            await EnsureConnectionOpenAsync(cancellationToken).ConfigureAwait(false);

            if (_connection is not MySqlConnection mysqlConnection)
            {
                return Left<EncinaError, IReadOnlyList<TEntity>>(
                    RepositoryErrors.BulkReadFailed<TEntity>("Bulk read requires a MySqlConnection"));
            }

            var columnNames = string.Join(", ", _mapping.ColumnMappings.Values.Select(c => $"`{c}`"));

            var parameters = new StringBuilder();
            await using var cmd = mysqlConnection.CreateCommand();

            for (var i = 0; i < idList.Count; i++)
            {
                var paramName = $"@id{i}";
                if (i > 0) parameters.Append(", ");
                parameters.Append(paramName);
                cmd.Parameters.AddWithValue(paramName, idList[i]);
            }

            cmd.CommandText = $"SELECT {columnNames} FROM {_mapping.TableName} WHERE `{_mapping.IdColumnName}` IN ({parameters})";

            var results = new List<TEntity>();
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var entity = MapReaderToEntity(reader);
                results.Add(entity);
            }

            return Right<EncinaError, IReadOnlyList<TEntity>>(results);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, IReadOnlyList<TEntity>>(
                RepositoryErrors.BulkReadFailed<TEntity>(idList.Count, ex));
        }
    }

    #region Helper Methods

    private async Task EnsureConnectionOpenAsync(CancellationToken cancellationToken)
    {
        if (_connection.State != ConnectionState.Open)
        {
            if (_connection is MySqlConnection mysqlConnection)
            {
                await mysqlConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
            }
            else
            {
                _connection.Open();
            }
        }
    }

    private List<KeyValuePair<string, string>> GetFilteredProperties(
        BulkConfig config,
        IReadOnlySet<string> excludedProperties,
        bool forUpdate = false)
    {
        var properties = _mapping.ColumnMappings
            .Where(kvp => !excludedProperties.Contains(kvp.Key))
            .ToList();

        if (forUpdate && !properties.Any(kvp => kvp.Key == GetIdPropertyName()))
        {
            var idProperty = _mapping.ColumnMappings
                .FirstOrDefault(kvp => kvp.Value == _mapping.IdColumnName);
            if (idProperty.Key is not null)
            {
                properties.Insert(0, idProperty);
            }
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

    private string GetIdPropertyName()
    {
        return _mapping.ColumnMappings
            .FirstOrDefault(kvp => kvp.Value == _mapping.IdColumnName)
            .Key ?? string.Empty;
    }

    private DataTable CreateDataTable(List<TEntity> entities, List<KeyValuePair<string, string>> columns)
    {
        var dataTable = new DataTable();

        foreach (var (propertyName, columnName) in columns)
        {
            if (_propertyCache.TryGetValue(propertyName, out var property))
            {
                var columnType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                dataTable.Columns.Add(columnName, columnType);
            }
        }

        foreach (var entity in entities)
        {
            var row = dataTable.NewRow();
            foreach (var (propertyName, columnName) in columns)
            {
                if (_propertyCache.TryGetValue(propertyName, out var property))
                {
                    var value = property.GetValue(entity);
                    row[columnName] = value ?? DBNull.Value;
                }
            }
            dataTable.Rows.Add(row);
        }

        return dataTable;
    }

    private async Task CreateTempTableAsync(
        MySqlConnection connection,
        string tempTableName,
        List<KeyValuePair<string, string>> columns,
        CancellationToken cancellationToken)
    {
        var columnDefs = new StringBuilder();
        foreach (var (propertyName, columnName) in columns)
        {
            if (columnDefs.Length > 0)
                columnDefs.Append(", ");

            if (_propertyCache.TryGetValue(propertyName, out var property))
            {
                var mysqlType = GetMySqlType(property.PropertyType);
                columnDefs.Append(CultureInfo.InvariantCulture, $"`{columnName}` {mysqlType}");
            }
        }

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"CREATE TEMPORARY TABLE {tempTableName} ({columnDefs})";
        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task BulkInsertToTempTableAsync(
        MySqlConnection connection,
        string tempTableName,
        List<TEntity> entities,
        List<KeyValuePair<string, string>> columns,
        BulkConfig config,
        CancellationToken cancellationToken)
    {
        using var dataTable = CreateDataTable(entities, columns);

        var bulkCopy = new MySqlBulkCopy(connection)
        {
            DestinationTableName = tempTableName,
            BulkCopyTimeout = config.BulkCopyTimeout ?? 0
        };

        foreach (var (_, columnName) in columns)
        {
            var ordinal = dataTable.Columns.IndexOf(columnName);
            if (ordinal >= 0)
            {
                bulkCopy.ColumnMappings.Add(new MySqlBulkCopyColumnMapping(ordinal, columnName));
            }
        }

        await bulkCopy.WriteToServerAsync(dataTable, cancellationToken).ConfigureAwait(false);
    }

    private TEntity MapReaderToEntity(MySqlDataReader reader)
    {
        var entity = new TEntity();

        foreach (var (propertyName, columnName) in _mapping.ColumnMappings)
        {
            if (_propertyCache.TryGetValue(propertyName, out var property) && property.CanWrite)
            {
                var ordinal = reader.GetOrdinal(columnName);
                if (!reader.IsDBNull(ordinal))
                {
                    var value = reader.GetValue(ordinal);
                    var targetType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

                    if (value.GetType() != targetType)
                    {
                        value = Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
                    }

                    property.SetValue(entity, value);
                }
            }
        }

        return entity;
    }

    private static string GetMySqlType(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        return type switch
        {
            _ when type == typeof(int) => "INT",
            _ when type == typeof(long) => "BIGINT",
            _ when type == typeof(short) => "SMALLINT",
            _ when type == typeof(byte) => "TINYINT UNSIGNED",
            _ when type == typeof(bool) => "TINYINT(1)",
            _ when type == typeof(decimal) => "DECIMAL(18,2)",
            _ when type == typeof(double) => "DOUBLE",
            _ when type == typeof(float) => "FLOAT",
            _ when type == typeof(string) => "TEXT",
            _ when type == typeof(Guid) => "CHAR(36)",
            _ when type == typeof(DateTime) => "DATETIME(6)",
            _ when type == typeof(DateTimeOffset) => "DATETIME(6)",
            _ when type == typeof(TimeSpan) => "TIME(6)",
            _ when type == typeof(byte[]) => "BLOB",
            _ when type.IsEnum => "INT",
            _ => "TEXT"
        };
    }

    private static bool IsDuplicateKeyException(Exception ex)
    {
        // Recursively search for MySqlException with error code 1062 (ER_DUP_ENTRY)
        var current = ex;
        while (current is not null)
        {
            if (current is MySqlException mysqlEx && mysqlEx.Number == 1062)
            {
                return true;
            }

            // Check message-based detection at each level
            if (current.Message.Contains("Duplicate entry", StringComparison.OrdinalIgnoreCase)
                || current.Message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            current = current.InnerException;
        }

        // Check AggregateException inner exceptions
        if (ex is AggregateException aggEx)
        {
            foreach (var inner in aggEx.InnerExceptions)
            {
                if (IsDuplicateKeyException(inner))
                {
                    return true;
                }
            }
        }

        return false;
    }

    #endregion
}
