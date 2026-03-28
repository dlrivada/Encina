using System.Data;
using System.Globalization;
using System.Reflection;
using System.Text;
using Dapper;
using Encina;
using Encina.Dapper.Sqlite.Repository;
using Encina.Dapper.Sqlite.TypeHandlers;
using Encina.DomainModeling;
using LanguageExt;
using Microsoft.Data.Sqlite;
using static LanguageExt.Prelude;

namespace Encina.Dapper.Sqlite.BulkOperations;

/// <summary>
/// Dapper implementation of <see cref="IBulkOperations{TEntity}"/> for SQLite.
/// </summary>
public sealed class BulkOperationsDapper<TEntity, TId> : IBulkOperations<TEntity>
    where TEntity : class, new()
    where TId : notnull
{
    private readonly IDbConnection _connection;
    private readonly IEntityMapping<TEntity, TId> _mapping;
    private readonly Dictionary<string, PropertyInfo> _propertyCache;
    private readonly IDbTransaction? _transaction;
    private const int MaxParametersPerBatch = 999;

    /// <summary>
    /// Initializes a new instance of the <see cref="BulkOperationsDapper{TEntity, TId}"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="mapping">The entity mapping configuration.</param>
    /// <param name="transaction">An optional transaction to use for all operations.</param>
    public BulkOperationsDapper(
        IDbConnection connection,
        IEntityMapping<TEntity, TId> mapping,
        IDbTransaction? transaction = null)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(mapping);

        // Ensure Guid TypeHandler is registered for SQLite TEXT-to-Guid conversion
        GuidTypeHandler.EnsureRegistered();

        _connection = connection;
        _mapping = mapping;
        _transaction = transaction;
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

            var columnsToInsert = GetFilteredProperties(config, _mapping.InsertExcludedProperties);
            var columnCount = columnsToInsert.Count;
            var batchSize = Math.Max(1, MaxParametersPerBatch / columnCount);
            var totalInserted = 0;

            for (var i = 0; i < entityList.Count; i += batchSize)
            {
                var batch = entityList.Skip(i).Take(batchSize).ToList();
                var columnNames = string.Join(", ", columnsToInsert.Select(kvp => $"\"{kvp.Value}\""));

                var valuePlaceholders = new StringBuilder();
                var parameters = new DynamicParameters();
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
                            parameters.Add(paramName, value);
                        }
                    }
                    valuePlaceholders.Append(')');
                }

                var sql = $"INSERT INTO {_mapping.TableName} ({columnNames}) VALUES {valuePlaceholders}";

                var command = new CommandDefinition(
                    sql,
                    parameters,
                    _transaction,
                    cancellationToken: cancellationToken);

                totalInserted += await _connection.ExecuteAsync(command).ConfigureAwait(false);
            }

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

            var columnsToUpdate = GetFilteredProperties(config, _mapping.UpdateExcludedProperties, forUpdate: true);
            var totalUpdated = 0;

            foreach (var entity in entityList)
            {
                var setClauses = new List<string>();
                var parameters = new DynamicParameters();
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
                        parameters.Add(paramName, value);
                    }
                }

                // Convert ID to string for SQLite
                object idValue = _mapping.GetId(entity);
                if (idValue is Guid idGuid)
                {
                    idValue = idGuid.ToString();
                }
                parameters.Add("@id", idValue);

                var sql = $@"
                    UPDATE {_mapping.TableName}
                    SET {string.Join(", ", setClauses)}
                    WHERE ""{_mapping.IdColumnName}"" = @id";

                var command = new CommandDefinition(
                    sql,
                    parameters,
                    _transaction,
                    cancellationToken: cancellationToken);

                totalUpdated += await _connection.ExecuteAsync(command).ConfigureAwait(false);
            }

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

            // Convert GUIDs to strings for SQLite consistency
            var ids = entityList.Select(e =>
            {
                object id = _mapping.GetId(e);
                return id is Guid guid ? (object)guid.ToString() : id;
            }).ToList();
            var totalDeleted = 0;

            for (var i = 0; i < ids.Count; i += MaxParametersPerBatch)
            {
                var batch = ids.Skip(i).Take(MaxParametersPerBatch).ToList();
                var sql = $"DELETE FROM {_mapping.TableName} WHERE \"{_mapping.IdColumnName}\" IN @Ids";

                var command = new CommandDefinition(
                    sql,
                    new { Ids = batch },
                    _transaction,
                    cancellationToken: cancellationToken);

                totalDeleted += await _connection.ExecuteAsync(command).ConfigureAwait(false);
            }

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

            var columnsForInsert = GetFilteredProperties(config, _mapping.InsertExcludedProperties);
            var columnsForUpdate = GetFilteredProperties(config, _mapping.UpdateExcludedProperties, forUpdate: true)
                .Where(kvp => kvp.Value != _mapping.IdColumnName)
                .ToList();

            var columnNames = string.Join(", ", columnsForInsert.Select(kvp => $"\"{kvp.Value}\""));
            var updateClauses = columnsForUpdate.Select(kvp => $"\"{kvp.Value}\" = excluded.\"{kvp.Value}\"");

            var totalAffected = 0;

            foreach (var entity in entityList)
            {
                var parameters = new DynamicParameters();
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
                        parameters.Add(paramName, value);
                    }
                }

                var sql = $@"
                    INSERT INTO {_mapping.TableName} ({columnNames})
                    VALUES ({paramPlaceholders})
                    ON CONFLICT(""{_mapping.IdColumnName}"") DO UPDATE SET
                    {string.Join(", ", updateClauses)}";

                var command = new CommandDefinition(
                    sql,
                    parameters,
                    _transaction,
                    cancellationToken: cancellationToken);

                totalAffected += await _connection.ExecuteAsync(command).ConfigureAwait(false);
            }

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

            var columnNames = string.Join(", ", _mapping.ColumnMappings.Values.Select(c => $"\"{c}\""));
            var results = new List<TEntity>();

            for (var i = 0; i < idList.Count; i += MaxParametersPerBatch)
            {
                var batch = idList.Skip(i).Take(MaxParametersPerBatch).ToList();

                // Convert GUIDs to strings for SQLite comparison (SQLite stores GUIDs as TEXT)
                var normalizedBatch = batch.Select(id => id is Guid guid ? (object)guid.ToString() : id).ToList();

                var sql = $"SELECT {columnNames} FROM {_mapping.TableName} WHERE \"{_mapping.IdColumnName}\" IN @Ids";

                var command = new CommandDefinition(
                    sql,
                    new { Ids = normalizedBatch },
                    _transaction,
                    cancellationToken: cancellationToken);

                var entities = await _connection.QueryAsync<TEntity>(command).ConfigureAwait(false);
                results.AddRange(entities);
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

    private static bool IsDuplicateKeyException(Exception ex)
    {
        var message = ex.Message;
        return message.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase)
            || message.Contains("PRIMARY KEY constraint failed", StringComparison.OrdinalIgnoreCase);
    }
}
