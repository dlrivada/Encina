using System.Data;
using System.Globalization;
using System.Reflection;
using System.Text;
using Encina;
using Encina.ADO.Sqlite.Repository;
using Encina.DomainModeling;
using LanguageExt;
using Microsoft.Data.Sqlite;
using static LanguageExt.Prelude;

namespace Encina.ADO.Sqlite.BulkOperations;

/// <summary>
/// SQLite implementation of <see cref="IBulkOperations{TEntity}"/> using multi-row INSERT
/// statements for optimized bulk operations.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TId">The entity identifier type.</typeparam>
/// <remarks>
/// <para>
/// SQLite doesn't have a native bulk copy mechanism, but this implementation uses
/// optimized multi-row INSERT statements within a single transaction for better performance.
/// </para>
/// </remarks>
public sealed class BulkOperationsSqlite<TEntity, TId> : IBulkOperations<TEntity>
    where TEntity : class, new()
    where TId : notnull
{
    private readonly IDbConnection _connection;
    private readonly IEntityMapping<TEntity, TId> _mapping;
    private readonly Dictionary<string, PropertyInfo> _propertyCache;
    private const int MaxParametersPerBatch = 999; // SQLite parameter limit

    /// <summary>
    /// Initializes a new instance of the <see cref="BulkOperationsSqlite{TEntity, TId}"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="mapping">The entity mapping configuration.</param>
    public BulkOperationsSqlite(IDbConnection connection, IEntityMapping<TEntity, TId> mapping)
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

            if (_connection is not SqliteConnection sqliteConnection)
            {
                return Left<EncinaError, int>(
                    RepositoryErrors.BulkInsertFailed<TEntity>("Bulk insert requires a SqliteConnection"));
            }

            var columnsToInsert = GetFilteredProperties(config, _mapping.InsertExcludedProperties);
            var columnCount = columnsToInsert.Count;
            var batchSize = Math.Max(1, MaxParametersPerBatch / columnCount);
            var totalInserted = 0;

            await using var transaction = (SqliteTransaction)await sqliteConnection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

            for (var i = 0; i < entityList.Count; i += batchSize)
            {
                var batch = entityList.Skip(i).Take(batchSize).ToList();
                var columnNames = string.Join(", ", columnsToInsert.Select(kvp => $"\"{kvp.Value}\""));

                var valuePlaceholders = new StringBuilder();
                await using var cmd = sqliteConnection.CreateCommand();
                cmd.Transaction = transaction;

                var paramIndex = 0;
                foreach (var entity in batch)
                {
                    if (valuePlaceholders.Length > 0)
                        valuePlaceholders.Append(", ");

                    valuePlaceholders.Append('(');
                    var first = true;
                    foreach (var (propertyName, _) in columnsToInsert)
                    {
                        if (!first) valuePlaceholders.Append(", ");
                        first = false;

                        var paramName = $"@p{paramIndex++}";
                        valuePlaceholders.Append(paramName);

                        if (_propertyCache.TryGetValue(propertyName, out var property))
                        {
                            var value = property.GetValue(entity);
                            // Convert GUIDs to strings for SQLite storage consistency
                            if (value is Guid guid)
                            {
                                value = guid.ToString();
                            }
                            cmd.Parameters.AddWithValue(paramName, value ?? DBNull.Value);
                        }
                    }
                    valuePlaceholders.Append(')');
                }

                cmd.CommandText = $"INSERT INTO {_mapping.TableName} ({columnNames}) VALUES {valuePlaceholders}";
                totalInserted += await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return Right<EncinaError, int>(totalInserted);
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

            if (_connection is not SqliteConnection sqliteConnection)
            {
                return Left<EncinaError, int>(
                    RepositoryErrors.BulkUpdateFailed<TEntity>("Bulk update requires a SqliteConnection"));
            }

            var columnsToUpdate = GetFilteredProperties(config, _mapping.UpdateExcludedProperties, forUpdate: true);
            var totalUpdated = 0;

            await using var transaction = (SqliteTransaction)await sqliteConnection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

            foreach (var entity in entityList)
            {
                await using var cmd = sqliteConnection.CreateCommand();
                cmd.Transaction = transaction;

                var setClauses = new List<string>();
                var paramIndex = 0;

                foreach (var (propertyName, columnName) in columnsToUpdate)
                {
                    if (columnName == _mapping.IdColumnName)
                        continue;

                    var paramName = $"@p{paramIndex++}";
                    setClauses.Add($"\"{columnName}\" = {paramName}");

                    if (_propertyCache.TryGetValue(propertyName, out var property))
                    {
                        var value = property.GetValue(entity);
                        // Convert GUIDs to strings for SQLite storage consistency
                        if (value is Guid guid)
                        {
                            value = guid.ToString();
                        }
                        cmd.Parameters.AddWithValue(paramName, value ?? DBNull.Value);
                    }
                }

                // Add ID parameter (convert GUID to string for SQLite)
                var idParam = $"@id";
                object idValue = _mapping.GetId(entity);
                if (idValue is Guid idGuid)
                {
                    idValue = idGuid.ToString();
                }
                cmd.Parameters.AddWithValue(idParam, idValue);

                cmd.CommandText = $@"
                    UPDATE {_mapping.TableName}
                    SET {string.Join(", ", setClauses)}
                    WHERE ""{_mapping.IdColumnName}"" = {idParam}";

                totalUpdated += await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return Right<EncinaError, int>(totalUpdated);
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

            if (_connection is not SqliteConnection sqliteConnection)
            {
                return Left<EncinaError, int>(
                    RepositoryErrors.BulkDeleteFailed<TEntity>("Bulk delete requires a SqliteConnection"));
            }

            var ids = entityList.Select(e => _mapping.GetId(e)).ToList();
            var batchSize = MaxParametersPerBatch;
            var totalDeleted = 0;

            await using var transaction = (SqliteTransaction)await sqliteConnection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

            for (var i = 0; i < ids.Count; i += batchSize)
            {
                var batch = ids.Skip(i).Take(batchSize).ToList();
                await using var cmd = sqliteConnection.CreateCommand();
                cmd.Transaction = transaction;

                var parameters = new StringBuilder();
                for (var j = 0; j < batch.Count; j++)
                {
                    var paramName = $"@id{j}";
                    if (j > 0) parameters.Append(", ");
                    parameters.Append(paramName);
                    // Convert GUIDs to strings for SQLite consistency
                    var value = batch[j] is Guid guid ? (object)guid.ToString() : batch[j];
                    cmd.Parameters.AddWithValue(paramName, value);
                }

                cmd.CommandText = $"DELETE FROM {_mapping.TableName} WHERE \"{_mapping.IdColumnName}\" IN ({parameters})";
                totalDeleted += await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return Right<EncinaError, int>(totalDeleted);
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

            if (_connection is not SqliteConnection sqliteConnection)
            {
                return Left<EncinaError, int>(
                    RepositoryErrors.BulkMergeFailed<TEntity>("Bulk merge requires a SqliteConnection"));
            }

            var columnsForInsert = GetFilteredProperties(config, _mapping.InsertExcludedProperties);
            var columnsForUpdate = GetFilteredProperties(config, _mapping.UpdateExcludedProperties, forUpdate: true)
                .Where(kvp => kvp.Value != _mapping.IdColumnName)
                .ToList();

            var columnNames = string.Join(", ", columnsForInsert.Select(kvp => $"\"{kvp.Value}\""));
            var updateClauses = columnsForUpdate.Select(kvp => $"\"{kvp.Value}\" = excluded.\"{kvp.Value}\"");

            var totalAffected = 0;

            await using var transaction = (SqliteTransaction)await sqliteConnection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

            // Use INSERT OR REPLACE or INSERT ... ON CONFLICT
            foreach (var entity in entityList)
            {
                await using var cmd = sqliteConnection.CreateCommand();
                cmd.Transaction = transaction;

                var paramPlaceholders = new StringBuilder();
                var paramIndex = 0;

                foreach (var (propertyName, _) in columnsForInsert)
                {
                    if (paramIndex > 0) paramPlaceholders.Append(", ");
                    var paramName = $"@p{paramIndex++}";
                    paramPlaceholders.Append(paramName);

                    if (_propertyCache.TryGetValue(propertyName, out var property))
                    {
                        var value = property.GetValue(entity);
                        // Convert GUIDs to strings for SQLite storage consistency
                        if (value is Guid guid)
                        {
                            value = guid.ToString();
                        }
                        cmd.Parameters.AddWithValue(paramName, value ?? DBNull.Value);
                    }
                }

                cmd.CommandText = $@"
                    INSERT INTO {_mapping.TableName} ({columnNames})
                    VALUES ({paramPlaceholders})
                    ON CONFLICT(""{_mapping.IdColumnName}"") DO UPDATE SET
                    {string.Join(", ", updateClauses)}";

                totalAffected += await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return Right<EncinaError, int>(totalAffected);
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

            if (_connection is not SqliteConnection sqliteConnection)
            {
                return Left<EncinaError, IReadOnlyList<TEntity>>(
                    RepositoryErrors.BulkReadFailed<TEntity>("Bulk read requires a SqliteConnection"));
            }

            var columnNames = string.Join(", ", _mapping.ColumnMappings.Values.Select(c => $"\"{c}\""));
            var results = new List<TEntity>();
            var batchSize = MaxParametersPerBatch;

            for (var i = 0; i < idList.Count; i += batchSize)
            {
                var batch = idList.Skip(i).Take(batchSize).ToList();
                await using var cmd = sqliteConnection.CreateCommand();

                var parameters = new StringBuilder();
                for (var j = 0; j < batch.Count; j++)
                {
                    var paramName = $"@id{j}";
                    if (j > 0) parameters.Append(", ");
                    parameters.Append(paramName);
                    // Convert GUIDs to strings for SQLite comparison (SQLite stores GUIDs as TEXT)
                    var value = batch[j] is Guid guid ? (object)guid.ToString() : batch[j];
                    cmd.Parameters.AddWithValue(paramName, value);
                }

                cmd.CommandText = $"SELECT {columnNames} FROM {_mapping.TableName} WHERE \"{_mapping.IdColumnName}\" IN ({parameters})";

                await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    var entity = MapReaderToEntity(reader);
                    results.Add(entity);
                }
            }

            return Right<EncinaError, IReadOnlyList<TEntity>>(results);
        }
        catch (Exception ex)
        {
            return Left<EncinaError, IReadOnlyList<TEntity>>(
                RepositoryErrors.BulkReadFailed<TEntity>(idList.Count, ex));
        }
    }

    private async Task EnsureConnectionOpenAsync(CancellationToken cancellationToken)
    {
        if (_connection.State != ConnectionState.Open)
        {
            if (_connection is SqliteConnection sqliteConnection)
            {
                await sqliteConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
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
            var idProperty = _mapping.ColumnMappings.FirstOrDefault(kvp => kvp.Value == _mapping.IdColumnName);
            if (idProperty.Key is not null)
            {
                properties.Insert(0, idProperty);
            }
        }

        if (config.PropertiesToInclude is { Count: > 0 })
        {
            properties = properties.Where(kvp => config.PropertiesToInclude.Contains(kvp.Key)).ToList();
        }
        else if (config.PropertiesToExclude is { Count: > 0 })
        {
            properties = properties.Where(kvp => !config.PropertiesToExclude.Contains(kvp.Key)).ToList();
        }

        return properties;
    }

    private string GetIdPropertyName()
    {
        return _mapping.ColumnMappings.FirstOrDefault(kvp => kvp.Value == _mapping.IdColumnName).Key ?? string.Empty;
    }

    private TEntity MapReaderToEntity(SqliteDataReader reader)
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
                        value = ConvertValue(value, targetType);
                    }

                    property.SetValue(entity, value);
                }
            }
        }

        return entity;
    }

    private static object ConvertValue(object value, Type targetType)
    {
        // Special handling for Guid (SQLite stores as TEXT)
        if (targetType == typeof(Guid) && value is string stringValue)
        {
            return Guid.Parse(stringValue);
        }

        // Special handling for bool (SQLite stores as INTEGER)
        if (targetType == typeof(bool) && value is long longValue)
        {
            return longValue != 0;
        }

        // Special handling for DateTime (SQLite stores as TEXT)
        if (targetType == typeof(DateTime) && value is string dateString)
        {
            return DateTime.Parse(dateString, CultureInfo.InvariantCulture);
        }

        return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
    }

    private static bool IsDuplicateKeyException(Exception ex)
    {
        var message = ex.Message;
        return message.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase)
            || message.Contains("PRIMARY KEY constraint failed", StringComparison.OrdinalIgnoreCase);
    }
}
