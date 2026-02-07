using System.Data;
using System.Globalization;
using System.Reflection;
using System.Text;
using Encina;
using Encina.ADO.PostgreSQL.Repository;
using Encina.DomainModeling;
using LanguageExt;
using Npgsql;
using NpgsqlTypes;
using static LanguageExt.Prelude;

namespace Encina.ADO.PostgreSQL.BulkOperations;

/// <summary>
/// PostgreSQL implementation of <see cref="IBulkOperations{TEntity}"/> using COPY command
/// for high-performance bulk operations.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TId">The entity identifier type.</typeparam>
/// <remarks>
/// <para>
/// This implementation leverages PostgreSQL's native COPY command for high-performance
/// bulk operations. COPY is significantly faster than row-by-row INSERT statements
/// as it bypasses the SQL parser and executor.
/// </para>
/// <list type="bullet">
/// <item><description><c>COPY ... FROM STDIN (BINARY)</c> for BulkInsert operations</description></item>
/// <item><description>Temp table + UPDATE FROM for BulkUpdate operations</description></item>
/// <item><description>Temp table + DELETE USING for BulkDelete operations</description></item>
/// <item><description>INSERT ... ON CONFLICT for BulkMerge (upsert) operations</description></item>
/// </list>
/// <para>
/// The implementation supports transaction participation through <c>NpgsqlTransaction</c>.
/// </para>
/// </remarks>
public sealed class BulkOperationsPostgreSQL<TEntity, TId> : IBulkOperations<TEntity>
    where TEntity : class, new()
    where TId : notnull
{
    private readonly IDbConnection _connection;
    private readonly IEntityMapping<TEntity, TId> _mapping;
    private readonly Dictionary<string, PropertyInfo> _propertyCache;

    /// <summary>
    /// Initializes a new instance of the <see cref="BulkOperationsPostgreSQL{TEntity, TId}"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="mapping">The entity mapping configuration.</param>
    public BulkOperationsPostgreSQL(IDbConnection connection, IEntityMapping<TEntity, TId> mapping)
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

            if (_connection is not NpgsqlConnection npgsqlConnection)
            {
                return Left<EncinaError, int>(
                    RepositoryErrors.BulkInsertFailed<TEntity>("COPY operations require a NpgsqlConnection"));
            }

            var targetTableName = GetQualifiedTableName();
            var columnsToInsert = GetFilteredProperties(config, _mapping.InsertExcludedProperties);
            var columnNames = string.Join(", ", columnsToInsert.Select(kvp => $"\"{kvp.Value}\""));

            var copyCommand = $"COPY {targetTableName} ({columnNames}) FROM STDIN (FORMAT BINARY)";

            await using var writer = await npgsqlConnection.BeginBinaryImportAsync(copyCommand, cancellationToken)
                .ConfigureAwait(false);

            foreach (var entity in entityList)
            {
                await writer.StartRowAsync(cancellationToken).ConfigureAwait(false);

                foreach (var (propertyName, _) in columnsToInsert)
                {
                    if (_propertyCache.TryGetValue(propertyName, out var property))
                    {
                        var value = property.GetValue(entity);
                        var npgsqlType = GetNpgsqlDbType(property.PropertyType);
                        await writer.WriteAsync(value ?? DBNull.Value, npgsqlType, cancellationToken)
                            .ConfigureAwait(false);
                    }
                }
            }

            await writer.CompleteAsync(cancellationToken).ConfigureAwait(false);

            return Right<EncinaError, int>(entityList.Count);
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

            if (_connection is not NpgsqlConnection npgsqlConnection)
            {
                return Left<EncinaError, int>(
                    RepositoryErrors.BulkUpdateFailed<TEntity>("Bulk update requires a NpgsqlConnection"));
            }

            var tempTableName = $"_bulk_update_{Guid.NewGuid():N}";
            var quotedTempTableName = QuoteIdentifier(tempTableName);
            var targetTableName = GetQualifiedTableName();
            var columnsToUpdate = GetFilteredProperties(config, _mapping.UpdateExcludedProperties, forUpdate: true);

            // Create temp table
            await CreateTempTableAsync(npgsqlConnection, quotedTempTableName, columnsToUpdate, cancellationToken)
                .ConfigureAwait(false);

            // COPY data to temp table
            await CopyToTempTableAsync(npgsqlConnection, quotedTempTableName, entityList, columnsToUpdate, cancellationToken)
                .ConfigureAwait(false);

            // UPDATE from temp table
            var setClauses = columnsToUpdate
                .Where(kvp => kvp.Value != _mapping.IdColumnName)
                .Select(kvp => $"\"{kvp.Value}\" = s.\"{kvp.Value}\"");

            var updateSql = $@"
                UPDATE {targetTableName} t
                SET {string.Join(", ", setClauses)}
                FROM {quotedTempTableName} s
                WHERE t.""{_mapping.IdColumnName}"" = s.""{_mapping.IdColumnName}""";

            await using var cmd = npgsqlConnection.CreateCommand();
            cmd.CommandText = updateSql;
            var affected = await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

            // Drop temp table
            cmd.CommandText = $"DROP TABLE IF EXISTS {quotedTempTableName}";
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

            if (_connection is not NpgsqlConnection npgsqlConnection)
            {
                return Left<EncinaError, int>(
                    RepositoryErrors.BulkDeleteFailed<TEntity>("Bulk delete requires a NpgsqlConnection"));
            }

            var tempTableName = $"_bulk_delete_{Guid.NewGuid():N}";
            var quotedTempTableName = QuoteIdentifier(tempTableName);
            var targetTableName = GetQualifiedTableName();
            var ids = entityList.Select(e => _mapping.GetId(e)).ToList();

            // Create temp table for IDs
            await CreateIdTempTableAsync(npgsqlConnection, quotedTempTableName, cancellationToken)
                .ConfigureAwait(false);

            // COPY IDs to temp table
            await CopyIdsToTempTableAsync(npgsqlConnection, quotedTempTableName, ids, cancellationToken)
                .ConfigureAwait(false);

            // DELETE using temp table
            var deleteSql = $@"
                DELETE FROM {targetTableName} t
                USING {quotedTempTableName} s
                WHERE t.""{_mapping.IdColumnName}"" = s.id";

            await using var cmd = npgsqlConnection.CreateCommand();
            cmd.CommandText = deleteSql;
            var affected = await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

            // Drop temp table
            cmd.CommandText = $"DROP TABLE IF EXISTS {quotedTempTableName}";
            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

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

            if (_connection is not NpgsqlConnection npgsqlConnection)
            {
                return Left<EncinaError, int>(
                    RepositoryErrors.BulkMergeFailed<TEntity>("Bulk merge requires a NpgsqlConnection"));
            }

            var tempTableName = $"_bulk_merge_{Guid.NewGuid():N}";
            var quotedTempTableName = QuoteIdentifier(tempTableName);
            var targetTableName = GetQualifiedTableName();
            var columnsForInsert = GetFilteredProperties(config, _mapping.InsertExcludedProperties);
            var columnsForUpdate = GetFilteredProperties(config, _mapping.UpdateExcludedProperties, forUpdate: true);

            // Create temp table with all columns needed for insert
            await CreateTempTableAsync(npgsqlConnection, quotedTempTableName, columnsForInsert, cancellationToken)
                .ConfigureAwait(false);

            // COPY data to temp table
            await CopyToTempTableAsync(npgsqlConnection, quotedTempTableName, entityList, columnsForInsert, cancellationToken)
                .ConfigureAwait(false);

            // INSERT ... ON CONFLICT DO UPDATE
            var insertColumnNames = string.Join(", ", columnsForInsert.Select(kvp => $"\"{kvp.Value}\""));
            var insertValues = string.Join(", ", columnsForInsert.Select(kvp => $"s.\"{kvp.Value}\""));
            var updateClauses = columnsForUpdate
                .Where(kvp => kvp.Value != _mapping.IdColumnName)
                .Select(kvp => $"\"{kvp.Value}\" = EXCLUDED.\"{kvp.Value}\"");

            var mergeSql = $@"
                INSERT INTO {targetTableName} ({insertColumnNames})
                SELECT {insertValues} FROM {quotedTempTableName} s
                ON CONFLICT (""{_mapping.IdColumnName}"") DO UPDATE SET
                {string.Join(", ", updateClauses)}";

            await using var cmd = npgsqlConnection.CreateCommand();
            cmd.CommandText = mergeSql;
            var affected = await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

            // Drop temp table
            cmd.CommandText = $"DROP TABLE IF EXISTS {quotedTempTableName}";
            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

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
            await EnsureConnectionOpenAsync(cancellationToken).ConfigureAwait(false);

            if (_connection is not NpgsqlConnection npgsqlConnection)
            {
                return Left<EncinaError, IReadOnlyList<TEntity>>(
                    RepositoryErrors.BulkReadFailed<TEntity>("Bulk read requires a NpgsqlConnection"));
            }

            var targetTableName = GetQualifiedTableName();
            var columnNames = string.Join(", ", _mapping.ColumnMappings.Values.Select(c => $"\"{c}\""));
            var sql = $@"
                SELECT {columnNames}
                FROM {targetTableName}
                WHERE ""{_mapping.IdColumnName}"" = ANY(@ids)";

            await using var cmd = npgsqlConnection.CreateCommand();
            cmd.CommandText = sql;

            var typedIds = idList.Cast<TId>().ToArray();
            var idsParam = new NpgsqlParameter("@ids", typedIds)
            {
                NpgsqlDbType = GetArrayNpgsqlDbType(typeof(TId))
            };
            cmd.Parameters.Add(idsParam);

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
            if (_connection is NpgsqlConnection npgsqlConnection)
            {
                await npgsqlConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
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

        // For Update operations, we need to include the Id column even if it's in excludedProperties
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

    private async Task CreateTempTableAsync(
        NpgsqlConnection connection,
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
                var pgType = GetPostgreSqlType(property.PropertyType);
                columnDefs.Append(CultureInfo.InvariantCulture, $"\"{columnName}\" {pgType}");
            }
        }

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"CREATE TEMP TABLE {tempTableName} ({columnDefs})";
        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task CreateIdTempTableAsync(
        NpgsqlConnection connection,
        string tempTableName,
        CancellationToken cancellationToken)
    {
        var idType = typeof(TId);
        var pgType = GetPostgreSqlType(idType);

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"CREATE TEMP TABLE {tempTableName} (id {pgType})";
        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task CopyToTempTableAsync(
        NpgsqlConnection connection,
        string tempTableName,
        List<TEntity> entities,
        List<KeyValuePair<string, string>> columns,
        CancellationToken cancellationToken)
    {
        var columnNames = string.Join(", ", columns.Select(kvp => $"\"{kvp.Value}\""));
        var copyCommand = $"COPY {tempTableName} ({columnNames}) FROM STDIN (FORMAT BINARY)";

        await using var writer = await connection.BeginBinaryImportAsync(copyCommand, cancellationToken)
            .ConfigureAwait(false);

        foreach (var entity in entities)
        {
            await writer.StartRowAsync(cancellationToken).ConfigureAwait(false);

            foreach (var (propertyName, _) in columns)
            {
                if (_propertyCache.TryGetValue(propertyName, out var property))
                {
                    var value = property.GetValue(entity);
                    var npgsqlType = GetNpgsqlDbType(property.PropertyType);
                    await writer.WriteAsync(value ?? DBNull.Value, npgsqlType, cancellationToken)
                        .ConfigureAwait(false);
                }
            }
        }

        await writer.CompleteAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task CopyIdsToTempTableAsync(
        NpgsqlConnection connection,
        string tempTableName,
        List<TId> ids,
        CancellationToken cancellationToken)
    {
        var copyCommand = $"COPY {tempTableName} (id) FROM STDIN (FORMAT BINARY)";

        await using var writer = await connection.BeginBinaryImportAsync(copyCommand, cancellationToken)
            .ConfigureAwait(false);

        var npgsqlType = GetNpgsqlDbType(typeof(TId));

        foreach (var id in ids)
        {
            await writer.StartRowAsync(cancellationToken).ConfigureAwait(false);
            await writer.WriteAsync(id, npgsqlType, cancellationToken).ConfigureAwait(false);
        }

        await writer.CompleteAsync(cancellationToken).ConfigureAwait(false);
    }

    private TEntity MapReaderToEntity(NpgsqlDataReader reader)
    {
        var entity = Activator.CreateInstance<TEntity>();

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

    private static NpgsqlDbType GetNpgsqlDbType(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        return type switch
        {
            _ when type == typeof(int) => NpgsqlDbType.Integer,
            _ when type == typeof(long) => NpgsqlDbType.Bigint,
            _ when type == typeof(short) => NpgsqlDbType.Smallint,
            _ when type == typeof(byte) => NpgsqlDbType.Smallint,
            _ when type == typeof(bool) => NpgsqlDbType.Boolean,
            _ when type == typeof(decimal) => NpgsqlDbType.Numeric,
            _ when type == typeof(double) => NpgsqlDbType.Double,
            _ when type == typeof(float) => NpgsqlDbType.Real,
            _ when type == typeof(string) => NpgsqlDbType.Text,
            _ when type == typeof(Guid) => NpgsqlDbType.Uuid,
            _ when type == typeof(DateTime) => NpgsqlDbType.TimestampTz,
            _ when type == typeof(DateTimeOffset) => NpgsqlDbType.TimestampTz,
            _ when type == typeof(TimeSpan) => NpgsqlDbType.Interval,
            _ when type == typeof(byte[]) => NpgsqlDbType.Bytea,
            _ when type.IsEnum => NpgsqlDbType.Integer,
            _ => NpgsqlDbType.Unknown
        };
    }

    private static NpgsqlDbType GetArrayNpgsqlDbType(Type elementType)
    {
        var elementDbType = GetNpgsqlDbType(elementType);
        return elementDbType == NpgsqlDbType.Unknown
            ? NpgsqlDbType.Array
            : NpgsqlDbType.Array | elementDbType;
    }

    private static string GetPostgreSqlType(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        return type switch
        {
            _ when type == typeof(int) => "INTEGER",
            _ when type == typeof(long) => "BIGINT",
            _ when type == typeof(short) => "SMALLINT",
            _ when type == typeof(byte) => "SMALLINT",
            _ when type == typeof(bool) => "BOOLEAN",
            _ when type == typeof(decimal) => "NUMERIC(18,2)",
            _ when type == typeof(double) => "DOUBLE PRECISION",
            _ when type == typeof(float) => "REAL",
            _ when type == typeof(string) => "TEXT",
            _ when type == typeof(Guid) => "UUID",
            _ when type == typeof(DateTime) => "TIMESTAMPTZ",
            _ when type == typeof(DateTimeOffset) => "TIMESTAMPTZ",
            _ when type == typeof(TimeSpan) => "INTERVAL",
            _ when type == typeof(byte[]) => "BYTEA",
            _ when type.IsEnum => "INTEGER",
            _ => "TEXT"
        };
    }

    private string GetQualifiedTableName()
    {
        var tableName = _mapping.TableName;
        if (tableName.Contains('.', StringComparison.Ordinal))
        {
            return QuoteCompositeIdentifier(tableName);
        }

        var schemaName = GetSchemaName();
        return string.IsNullOrWhiteSpace(schemaName)
            ? QuoteCompositeIdentifier(tableName)
            : QuoteCompositeIdentifier($"{schemaName}.{tableName}");
    }

    private string? GetSchemaName()
    {
        var schemaProperty = _mapping.GetType().GetProperty("SchemaName", BindingFlags.Public | BindingFlags.Instance);
        if (schemaProperty?.PropertyType != typeof(string))
        {
            return null;
        }

        var schemaValue = schemaProperty.GetValue(_mapping) as string;
        return string.IsNullOrWhiteSpace(schemaValue) ? null : schemaValue;
    }

    private static string QuoteCompositeIdentifier(string identifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);

        var parts = identifier.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return string.Join(".", parts.Select(QuoteIdentifierIfNeeded));
    }

    private static string QuoteIdentifierIfNeeded(string identifier)
    {
        if (identifier.StartsWith('"') &&
            identifier.EndsWith('"') &&
            identifier.Length >= 2)
        {
            return identifier;
        }

        return QuoteIdentifier(identifier);
    }

    private static string QuoteIdentifier(string identifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        return "\"" + identifier.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
    }

    #endregion
}
