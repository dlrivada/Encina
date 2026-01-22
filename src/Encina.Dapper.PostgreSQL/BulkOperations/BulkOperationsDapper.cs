using System.Data;
using System.Globalization;
using System.Reflection;
using Dapper;
using Encina;
using Encina.Dapper.PostgreSQL.Repository;
using Encina.DomainModeling;
using LanguageExt;
using Npgsql;
using NpgsqlTypes;
using static LanguageExt.Prelude;

namespace Encina.Dapper.PostgreSQL.BulkOperations;

/// <summary>
/// Dapper implementation of <see cref="IBulkOperations{TEntity}"/> using PostgreSQL COPY command
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
/// <item><description>Dapper queries with ANY() for BulkRead</description></item>
/// </list>
/// <para>
/// The implementation supports transaction participation through <c>NpgsqlTransaction</c>,
/// and configurable batch sizes through <see cref="BulkConfig"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register via DI
/// services.AddEncinaBulkOperations&lt;Order, Guid&gt;();
///
/// // Use in service
/// var bulkOps = serviceProvider.GetRequiredService&lt;IBulkOperations&lt;Order&gt;&gt;();
/// var result = await bulkOps.BulkInsertAsync(orders, BulkConfig.Default with { BatchSize = 5000 });
/// </code>
/// </example>
public sealed class BulkOperationsDapper<TEntity, TId> : IBulkOperations<TEntity>
    where TEntity : class, new()
    where TId : notnull
{
    private readonly IDbConnection _connection;
    private readonly IEntityMapping<TEntity, TId> _mapping;
    private readonly Dictionary<string, PropertyInfo> _propertyCache;
    private readonly IDbTransaction? _transaction;

    /// <summary>
    /// Initializes a new instance of the <see cref="BulkOperationsDapper{TEntity, TId}"/> class.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="mapping">The entity mapping configuration.</param>
    /// <param name="transaction">Optional transaction for transaction participation.</param>
    public BulkOperationsDapper(
        IDbConnection connection,
        IEntityMapping<TEntity, TId> mapping,
        IDbTransaction? transaction = null)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(mapping);

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

            if (_connection is not NpgsqlConnection npgsqlConnection)
            {
                return Left<EncinaError, int>(
                    RepositoryErrors.BulkInsertFailed<TEntity>("COPY operations require a NpgsqlConnection"));
            }

            var columnsToInsert = GetFilteredProperties(config, _mapping.InsertExcludedProperties);
            var columnNames = string.Join(", ", columnsToInsert.Select(kvp => $"\"{kvp.Value}\""));

            var copyCommand = $"COPY {_mapping.TableName} ({columnNames}) FROM STDIN (FORMAT BINARY)";

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

            if (_connection is not NpgsqlConnection npgsqlConnection)
            {
                return Left<EncinaError, int>(
                    RepositoryErrors.BulkUpdateFailed<TEntity>("Bulk update requires a NpgsqlConnection"));
            }

            var tempTableName = $"_bulk_update_{Guid.NewGuid():N}";
            var columnsToUpdate = GetFilteredProperties(config, _mapping.UpdateExcludedProperties, forUpdate: true);

            // Create temp table
            await CreateTempTableAsync(npgsqlConnection, tempTableName, columnsToUpdate, cancellationToken)
                .ConfigureAwait(false);

            // COPY data to temp table
            await CopyToTempTableAsync(npgsqlConnection, tempTableName, entityList, columnsToUpdate, cancellationToken)
                .ConfigureAwait(false);

            // UPDATE from temp table using Dapper
            var setClauses = columnsToUpdate
                .Where(kvp => kvp.Value != _mapping.IdColumnName)
                .Select(kvp => $"t.\"{kvp.Value}\" = s.\"{kvp.Value}\"");

            var updateSql = $@"
                UPDATE {_mapping.TableName} t
                SET {string.Join(", ", setClauses)}
                FROM {tempTableName} s
                WHERE t.""{_mapping.IdColumnName}"" = s.""{_mapping.IdColumnName}""";

            var command = new CommandDefinition(
                updateSql,
                transaction: _transaction as NpgsqlTransaction,
                cancellationToken: cancellationToken);

            var affected = await npgsqlConnection.ExecuteAsync(command).ConfigureAwait(false);

            // Drop temp table
            await npgsqlConnection.ExecuteAsync(
                $"DROP TABLE IF EXISTS {tempTableName}",
                transaction: _transaction as NpgsqlTransaction).ConfigureAwait(false);

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
            var ids = entityList.Select(e => _mapping.GetId(e)).ToList();

            // Create temp table for IDs
            await CreateIdTempTableAsync(npgsqlConnection, tempTableName, cancellationToken)
                .ConfigureAwait(false);

            // COPY IDs to temp table
            await CopyIdsToTempTableAsync(npgsqlConnection, tempTableName, ids, cancellationToken)
                .ConfigureAwait(false);

            // DELETE using temp table with Dapper
            var deleteSql = $@"
                DELETE FROM {_mapping.TableName} t
                USING {tempTableName} s
                WHERE t.""{_mapping.IdColumnName}"" = s.id";

            var command = new CommandDefinition(
                deleteSql,
                transaction: _transaction as NpgsqlTransaction,
                cancellationToken: cancellationToken);

            var affected = await npgsqlConnection.ExecuteAsync(command).ConfigureAwait(false);

            // Drop temp table
            await npgsqlConnection.ExecuteAsync(
                $"DROP TABLE IF EXISTS {tempTableName}",
                transaction: _transaction as NpgsqlTransaction).ConfigureAwait(false);

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
            var columnsForInsert = GetFilteredProperties(config, _mapping.InsertExcludedProperties);
            var columnsForUpdate = GetFilteredProperties(config, _mapping.UpdateExcludedProperties, forUpdate: true);

            // Create temp table with all columns needed for insert
            await CreateTempTableAsync(npgsqlConnection, tempTableName, columnsForInsert, cancellationToken)
                .ConfigureAwait(false);

            // COPY data to temp table
            await CopyToTempTableAsync(npgsqlConnection, tempTableName, entityList, columnsForInsert, cancellationToken)
                .ConfigureAwait(false);

            // INSERT ... ON CONFLICT DO UPDATE with Dapper
            var insertColumnNames = string.Join(", ", columnsForInsert.Select(kvp => $"\"{kvp.Value}\""));
            var insertValues = string.Join(", ", columnsForInsert.Select(kvp => $"s.\"{kvp.Value}\""));
            var updateClauses = columnsForUpdate
                .Where(kvp => kvp.Value != _mapping.IdColumnName)
                .Select(kvp => $"\"{kvp.Value}\" = EXCLUDED.\"{kvp.Value}\"");

            var mergeSql = $@"
                INSERT INTO {_mapping.TableName} ({insertColumnNames})
                SELECT {insertValues} FROM {tempTableName} s
                ON CONFLICT (""{_mapping.IdColumnName}"") DO UPDATE SET
                {string.Join(", ", updateClauses)}";

            var command = new CommandDefinition(
                mergeSql,
                transaction: _transaction as NpgsqlTransaction,
                cancellationToken: cancellationToken);

            var affected = await npgsqlConnection.ExecuteAsync(command).ConfigureAwait(false);

            // Drop temp table
            await npgsqlConnection.ExecuteAsync(
                $"DROP TABLE IF EXISTS {tempTableName}",
                transaction: _transaction as NpgsqlTransaction).ConfigureAwait(false);

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

            var columnNames = string.Join(", ", _mapping.ColumnMappings.Values.Select(c => $"\"{c}\""));
            var sql = $@"
                SELECT {columnNames}
                FROM {_mapping.TableName}
                WHERE ""{_mapping.IdColumnName}"" = ANY(@ids)";

            var command = new CommandDefinition(
                sql,
                new { ids = idList.ToArray() },
                _transaction,
                cancellationToken: cancellationToken);

            var entities = await _connection.QueryAsync<TEntity>(command).ConfigureAwait(false);
            return Right<EncinaError, IReadOnlyList<TEntity>>(entities.ToList());
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
        var columnDefs = new System.Text.StringBuilder();
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

        var command = new CommandDefinition(
            $"CREATE TEMP TABLE {tempTableName} ({columnDefs})",
            transaction: _transaction as NpgsqlTransaction,
            cancellationToken: cancellationToken);

        await connection.ExecuteAsync(command).ConfigureAwait(false);
    }

    private async Task CreateIdTempTableAsync(
        NpgsqlConnection connection,
        string tempTableName,
        CancellationToken cancellationToken)
    {
        var idType = typeof(TId);
        var pgType = GetPostgreSqlType(idType);

        var command = new CommandDefinition(
            $"CREATE TEMP TABLE {tempTableName} (id {pgType})",
            transaction: _transaction as NpgsqlTransaction,
            cancellationToken: cancellationToken);

        await connection.ExecuteAsync(command).ConfigureAwait(false);
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
            _ when type == typeof(DateTime) => NpgsqlDbType.Timestamp,
            _ when type == typeof(DateTimeOffset) => NpgsqlDbType.TimestampTz,
            _ when type == typeof(TimeSpan) => NpgsqlDbType.Interval,
            _ when type == typeof(byte[]) => NpgsqlDbType.Bytea,
            _ when type.IsEnum => NpgsqlDbType.Integer,
            _ => NpgsqlDbType.Unknown
        };
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
            _ when type == typeof(DateTime) => "TIMESTAMP",
            _ when type == typeof(DateTimeOffset) => "TIMESTAMPTZ",
            _ when type == typeof(TimeSpan) => "INTERVAL",
            _ when type == typeof(byte[]) => "BYTEA",
            _ when type.IsEnum => "INTEGER",
            _ => "TEXT"
        };
    }

    private static bool IsDuplicateKeyException(Exception ex)
    {
        var message = ex.Message;
        return message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase)
            || message.Contains("unique constraint", StringComparison.OrdinalIgnoreCase)
            || message.Contains("23505", StringComparison.OrdinalIgnoreCase); // PostgreSQL unique_violation error code
    }

    #endregion
}
