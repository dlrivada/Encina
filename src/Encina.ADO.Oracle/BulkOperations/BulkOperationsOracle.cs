using System.Data;
using System.Globalization;
using System.Reflection;
using System.Text;
using Encina;
using Encina.ADO.Oracle.Repository;
using Encina.DomainModeling;
using LanguageExt;
using Oracle.ManagedDataAccess.Client;
using static LanguageExt.Prelude;

namespace Encina.ADO.Oracle.BulkOperations;

/// <summary>
/// Oracle implementation of <see cref="IBulkOperations{TEntity}"/> using array binding
/// for high-performance bulk operations.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TId">The entity identifier type.</typeparam>
/// <remarks>
/// <para>
/// This implementation leverages Oracle's array binding feature for high-performance
/// bulk operations. Array binding allows multiple rows to be sent in a single round trip.
/// </para>
/// <list type="bullet">
/// <item><description>Array binding for BulkInsert operations</description></item>
/// <item><description>Global temporary table + MERGE for BulkUpdate operations</description></item>
/// <item><description>Multi-value DELETE with IN clause for BulkDelete operations</description></item>
/// <item><description>MERGE statement for BulkMerge (upsert) operations</description></item>
/// </list>
/// </remarks>
public sealed class BulkOperationsOracle<TEntity, TId> : IBulkOperations<TEntity>
    where TEntity : class, new()
    where TId : notnull
{
    private readonly IDbConnection _connection;
    private readonly IEntityMapping<TEntity, TId> _mapping;
    private readonly Dictionary<string, PropertyInfo> _propertyCache;

    /// <summary>
    /// Initializes a new instance of the <see cref="BulkOperationsOracle{TEntity, TId}"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="mapping">The entity mapping configuration.</param>
    public BulkOperationsOracle(IDbConnection connection, IEntityMapping<TEntity, TId> mapping)
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

            if (_connection is not OracleConnection oracleConnection)
            {
                return Left<EncinaError, int>(
                    RepositoryErrors.BulkInsertFailed<TEntity>("Array binding requires an OracleConnection"));
            }

            var columnsToInsert = GetFilteredProperties(config, _mapping.InsertExcludedProperties);
            var columnNames = string.Join(", ", columnsToInsert.Select(kvp => $"\"{kvp.Value}\""));
            var paramNames = string.Join(", ", columnsToInsert.Select((kvp, i) => $":p{i}"));

            var sql = $"INSERT INTO {_mapping.TableName} ({columnNames}) VALUES ({paramNames})";

            await using var cmd = oracleConnection.CreateCommand();
            cmd.CommandText = sql;
            cmd.ArrayBindCount = entityList.Count;

            // Create arrays for each column
            var paramIndex = 0;
            foreach (var (propertyName, _) in columnsToInsert)
            {
                if (_propertyCache.TryGetValue(propertyName, out var property))
                {
                    var values = entityList.Select(e => property.GetValue(e) ?? DBNull.Value).ToArray();
                    var param = new OracleParameter($":p{paramIndex}", GetOracleDbType(property.PropertyType))
                    {
                        Value = values
                    };
                    cmd.Parameters.Add(param);
                }
                paramIndex++;
            }

            var affected = await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            return Right<EncinaError, int>(affected);
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

            if (_connection is not OracleConnection oracleConnection)
            {
                return Left<EncinaError, int>(
                    RepositoryErrors.BulkUpdateFailed<TEntity>("Bulk update requires an OracleConnection"));
            }

            var columnsToUpdate = GetFilteredProperties(config, _mapping.UpdateExcludedProperties, forUpdate: true);

            // Use array binding with MERGE for updates
            var updateColumns = columnsToUpdate.Where(kvp => kvp.Value != _mapping.IdColumnName).ToList();

            var setClauses = updateColumns
                .Select((kvp, i) => $"t.\"{kvp.Value}\" = :p{i + 1}")
                .ToList();

            var sql = $@"
                UPDATE {_mapping.TableName} t
                SET {string.Join(", ", setClauses)}
                WHERE t.""{_mapping.IdColumnName}"" = :p0";

            await using var cmd = oracleConnection.CreateCommand();
            cmd.CommandText = sql;
            cmd.ArrayBindCount = entityList.Count;

            // ID parameter
            var idProperty = _propertyCache.Values.First(p =>
                _mapping.ColumnMappings.TryGetValue(p.Name, out var col) && col == _mapping.IdColumnName);
            var idValues = entityList.Select(e => idProperty.GetValue(e) ?? DBNull.Value).ToArray();
            cmd.Parameters.Add(new OracleParameter(":p0", GetOracleDbType(idProperty.PropertyType)) { Value = idValues });

            // Update column parameters
            var paramIndex = 1;
            foreach (var (propertyName, _) in updateColumns)
            {
                if (_propertyCache.TryGetValue(propertyName, out var property))
                {
                    var values = entityList.Select(e => property.GetValue(e) ?? DBNull.Value).ToArray();
                    cmd.Parameters.Add(new OracleParameter($":p{paramIndex}", GetOracleDbType(property.PropertyType)) { Value = values });
                }
                paramIndex++;
            }

            var affected = await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
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

            if (_connection is not OracleConnection oracleConnection)
            {
                return Left<EncinaError, int>(
                    RepositoryErrors.BulkDeleteFailed<TEntity>("Bulk delete requires an OracleConnection"));
            }

            var ids = entityList.Select(e => _mapping.GetId(e)).ToList();

            // Oracle has a limit of 1000 items in IN clause, batch if needed
            var batchSize = 1000;
            var totalDeleted = 0;

            for (var i = 0; i < ids.Count; i += batchSize)
            {
                var batch = ids.Skip(i).Take(batchSize).ToList();
                var parameters = new StringBuilder();

                await using var cmd = oracleConnection.CreateCommand();

                for (var j = 0; j < batch.Count; j++)
                {
                    var paramName = $":id{j}";
                    if (j > 0) parameters.Append(", ");
                    parameters.Append(paramName);
                    cmd.Parameters.Add(new OracleParameter(paramName, batch[j]));
                }

                cmd.CommandText = $"DELETE FROM {_mapping.TableName} WHERE \"{_mapping.IdColumnName}\" IN ({parameters})";
                totalDeleted += await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
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

            if (_connection is not OracleConnection oracleConnection)
            {
                return Left<EncinaError, int>(
                    RepositoryErrors.BulkMergeFailed<TEntity>("Bulk merge requires an OracleConnection"));
            }

            var columnsForInsert = GetFilteredProperties(config, _mapping.InsertExcludedProperties);
            var columnsForUpdate = GetFilteredProperties(config, _mapping.UpdateExcludedProperties, forUpdate: true)
                .Where(kvp => kvp.Value != _mapping.IdColumnName)
                .ToList();

            // Build MERGE statement with DUAL
            var insertColumnNames = string.Join(", ", columnsForInsert.Select(kvp => $"\"{kvp.Value}\""));
            var insertValues = string.Join(", ", columnsForInsert.Select((kvp, i) => $"s.c{i}"));
            var updateClauses = columnsForUpdate.Select((kvp, i) =>
            {
                var colIndex = columnsForInsert.FindIndex(c => c.Value == kvp.Value);
                return $"t.\"{kvp.Value}\" = s.c{colIndex}";
            });

            var dualColumns = string.Join(", ", columnsForInsert.Select((kvp, i) => $":p{i} AS c{i}"));
            var idColIndex = columnsForInsert.FindIndex(c => c.Value == _mapping.IdColumnName);

            var sql = $@"
                MERGE INTO {_mapping.TableName} t
                USING (SELECT {dualColumns} FROM DUAL) s
                ON (t.""{_mapping.IdColumnName}"" = s.c{idColIndex})
                WHEN MATCHED THEN
                    UPDATE SET {string.Join(", ", updateClauses)}
                WHEN NOT MATCHED THEN
                    INSERT ({insertColumnNames})
                    VALUES ({insertValues})";

            var totalAffected = 0;

            // Execute for each entity (Oracle MERGE doesn't support array binding well)
            foreach (var entity in entityList)
            {
                await using var cmd = oracleConnection.CreateCommand();
                cmd.CommandText = sql;

                var paramIndex = 0;
                foreach (var (propertyName, _) in columnsForInsert)
                {
                    if (_propertyCache.TryGetValue(propertyName, out var property))
                    {
                        var value = property.GetValue(entity) ?? DBNull.Value;
                        cmd.Parameters.Add(new OracleParameter($":p{paramIndex}", value));
                    }
                    paramIndex++;
                }

                totalAffected += await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
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

            if (_connection is not OracleConnection oracleConnection)
            {
                return Left<EncinaError, IReadOnlyList<TEntity>>(
                    RepositoryErrors.BulkReadFailed<TEntity>("Bulk read requires an OracleConnection"));
            }

            var columnNames = string.Join(", ", _mapping.ColumnMappings.Values.Select(c => $"\"{c}\""));
            var results = new List<TEntity>();

            // Oracle has a limit of 1000 items in IN clause
            var batchSize = 1000;

            for (var i = 0; i < idList.Count; i += batchSize)
            {
                var batch = idList.Skip(i).Take(batchSize).ToList();
                var parameters = new StringBuilder();

                await using var cmd = oracleConnection.CreateCommand();

                for (var j = 0; j < batch.Count; j++)
                {
                    var paramName = $":id{j}";
                    if (j > 0) parameters.Append(", ");
                    parameters.Append(paramName);
                    cmd.Parameters.Add(new OracleParameter(paramName, batch[j]));
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

    #region Helper Methods

    private async Task EnsureConnectionOpenAsync(CancellationToken cancellationToken)
    {
        if (_connection.State != ConnectionState.Open)
        {
            if (_connection is OracleConnection oracleConnection)
            {
                await oracleConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
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

    private TEntity MapReaderToEntity(OracleDataReader reader)
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

    private static OracleDbType GetOracleDbType(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        return type switch
        {
            _ when type == typeof(int) => OracleDbType.Int32,
            _ when type == typeof(long) => OracleDbType.Int64,
            _ when type == typeof(short) => OracleDbType.Int16,
            _ when type == typeof(byte) => OracleDbType.Byte,
            _ when type == typeof(bool) => OracleDbType.Int16, // Oracle doesn't have bool, use 0/1
            _ when type == typeof(decimal) => OracleDbType.Decimal,
            _ when type == typeof(double) => OracleDbType.Double,
            _ when type == typeof(float) => OracleDbType.Single,
            _ when type == typeof(string) => OracleDbType.Varchar2,
            _ when type == typeof(Guid) => OracleDbType.Raw, // Store GUID as RAW(16)
            _ when type == typeof(DateTime) => OracleDbType.TimeStamp,
            _ when type == typeof(DateTimeOffset) => OracleDbType.TimeStampTZ,
            _ when type == typeof(TimeSpan) => OracleDbType.IntervalDS,
            _ when type == typeof(byte[]) => OracleDbType.Blob,
            _ when type.IsEnum => OracleDbType.Int32,
            _ => OracleDbType.Varchar2
        };
    }

    private static bool IsDuplicateKeyException(Exception ex)
    {
        var message = ex.Message;
        return message.Contains("ORA-00001", StringComparison.OrdinalIgnoreCase) // unique constraint violated
            || message.Contains("unique constraint", StringComparison.OrdinalIgnoreCase)
            || message.Contains("primary key", StringComparison.OrdinalIgnoreCase);
    }

    #endregion
}
