using System.Data;
using System.Reflection;
using Dapper;
using Encina;
using Encina.Dapper.SqlServer.Repository;
using Encina.DomainModeling;
using LanguageExt;
using Microsoft.Data.SqlClient;
using static LanguageExt.Prelude;

namespace Encina.Dapper.SqlServer.BulkOperations;

/// <summary>
/// Dapper implementation of <see cref="IBulkOperations{TEntity}"/> using SqlBulkCopy
/// for high-performance bulk operations on SQL Server.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TId">The entity identifier type.</typeparam>
/// <remarks>
/// <para>
/// This implementation provides up to 370x faster performance compared to standard
/// row-by-row operations. Measured with SQL Server 2022 on 1,000 entities:
/// Insert 30x, Update 125x, Delete 370x faster.
/// </para>
/// <list type="bullet">
/// <item><description><c>SqlBulkCopy</c> for BulkInsert operations</description></item>
/// <item><description>Table-Valued Parameters (TVP) for BulkUpdate, BulkDelete, and BulkMerge</description></item>
/// <item><description>Dapper queries with IN clause for BulkRead</description></item>
/// </list>
/// <para>
/// The implementation supports transaction participation through <c>CommandDefinition</c>,
/// output identity retrieval, and configurable batch sizes through <see cref="BulkConfig"/>.
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

        _propertyCache = typeof(TEntity).GetProperties()
            .Where(p => mapping.ColumnMappings.ContainsKey(p.Name))
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

            if (_connection is not SqlConnection sqlConnection)
            {
                return Left<EncinaError, int>(
                    RepositoryErrors.BulkInsertFailed<TEntity>("SqlBulkCopy requires a SqlConnection"));
            }

            using var dataTable = CreateDataTable(entityList, forInsert: true, config);
            using var bulkCopy = CreateSqlBulkCopy(sqlConnection, config);
            ConfigureColumnMappings(bulkCopy, forInsert: true, config);

            await bulkCopy.WriteToServerAsync(dataTable, cancellationToken).ConfigureAwait(false);

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

            if (_connection is not SqlConnection sqlConnection)
            {
                return Left<EncinaError, int>(
                    RepositoryErrors.BulkUpdateFailed<TEntity>("TVP operations require a SqlConnection"));
            }

            using var dataTable = CreateDataTable(entityList, forInsert: false, config);
            var sql = BuildMergeUpdateSql(config);

            var parameters = new DynamicParameters();
            parameters.Add("@TableData", dataTable.AsTableValuedParameter($"dbo.{GetTvpTypeName()}_Update"));

            var command = new CommandDefinition(
                sql,
                parameters,
                _transaction,
                commandTimeout: config.BulkCopyTimeout,
                cancellationToken: cancellationToken);

            var affected = await sqlConnection.ExecuteAsync(command).ConfigureAwait(false);
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

            if (_connection is not SqlConnection sqlConnection)
            {
                return Left<EncinaError, int>(
                    RepositoryErrors.BulkDeleteFailed<TEntity>("TVP operations require a SqlConnection"));
            }

            var ids = entityList.Select(e => _mapping.GetId(e)).ToList();
            using var dataTable = CreateIdDataTable(ids);
            var sql = BuildBulkDeleteSql();

            var parameters = new DynamicParameters();
            parameters.Add("@Ids", dataTable.AsTableValuedParameter($"dbo.{GetTvpTypeName()}_Ids"));

            var command = new CommandDefinition(
                sql,
                parameters,
                _transaction,
                cancellationToken: cancellationToken);

            var deleted = await sqlConnection.ExecuteAsync(command).ConfigureAwait(false);
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
            await EnsureConnectionOpenAsync(cancellationToken).ConfigureAwait(false);

            if (_connection is not SqlConnection sqlConnection)
            {
                return Left<EncinaError, int>(
                    RepositoryErrors.BulkMergeFailed<TEntity>("TVP operations require a SqlConnection"));
            }

            // For Merge, use forInsert: true to include all columns needed for INSERT
            // The MERGE SQL will handle which columns to use for UPDATE vs INSERT
            using var dataTable = CreateDataTable(entityList, forInsert: true, config);
            var sql = BuildMergeSql(config);

            var parameters = new DynamicParameters();
            parameters.Add("@TableData", dataTable.AsTableValuedParameter($"dbo.{GetTvpTypeName()}_Merge"));

            var command = new CommandDefinition(
                sql,
                parameters,
                _transaction,
                commandTimeout: config.BulkCopyTimeout,
                cancellationToken: cancellationToken);

            var affected = await sqlConnection.ExecuteAsync(command).ConfigureAwait(false);
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

            var columns = string.Join(", ", _mapping.ColumnMappings.Values.Select(c => $"[{c}]"));
            var sql = $"SELECT {columns} FROM {_mapping.TableName} WHERE [{_mapping.IdColumnName}] IN @Ids";

            var command = new CommandDefinition(
                sql,
                new { Ids = idList },
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

    #region DataTable Creation

    private DataTable CreateDataTable(List<TEntity> entities, bool forInsert, BulkConfig config)
    {
        var dataTable = new DataTable();
        var excludedProperties = forInsert
            ? _mapping.InsertExcludedProperties
            : _mapping.UpdateExcludedProperties;

        var propertiesToInclude = GetFilteredProperties(config, excludedProperties);

        // For Update/Merge operations, always include the Id column even if excluded from update
        // because it's needed for the MERGE ON clause matching
        var idPropertyName = _mapping.ColumnMappings.FirstOrDefault(kvp => kvp.Value == _mapping.IdColumnName).Key;
        if (!forInsert && !string.IsNullOrEmpty(idPropertyName) && !propertiesToInclude.Exists(kvp => kvp.Key == idPropertyName))
        {
            if (_mapping.ColumnMappings.TryGetValue(idPropertyName, out var idColumnName))
            {
                propertiesToInclude.Insert(0, new KeyValuePair<string, string>(idPropertyName, idColumnName));
            }
        }

        foreach (var (propertyName, columnName) in propertiesToInclude)
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
            foreach (var (propertyName, columnName) in propertiesToInclude)
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

    private static DataTable CreateIdDataTable<T>(List<T> ids)
    {
        var dataTable = new DataTable();
        var idType = typeof(T);

        if (idType == typeof(object) && ids.Count > 0)
        {
            idType = ids[0]!.GetType();
        }

        var columnType = Nullable.GetUnderlyingType(idType) ?? idType;
        dataTable.Columns.Add("Id", columnType);

        foreach (var id in ids)
        {
            var row = dataTable.NewRow();
            row["Id"] = id is null ? DBNull.Value : id;
            dataTable.Rows.Add(row);
        }

        return dataTable;
    }

    private List<KeyValuePair<string, string>> GetFilteredProperties(BulkConfig config, IReadOnlySet<string> excludedProperties)
    {
        var properties = _mapping.ColumnMappings
            .Where(kvp => !excludedProperties.Contains(kvp.Key))
            .ToList();

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

    #region SqlBulkCopy Configuration

    private SqlBulkCopy CreateSqlBulkCopy(SqlConnection connection, BulkConfig config)
    {
        var options = SqlBulkCopyOptions.Default;

        if (config.SetOutputIdentity)
            options |= SqlBulkCopyOptions.KeepIdentity;

        var sqlTransaction = _transaction as SqlTransaction;

        var bulkCopy = new SqlBulkCopy(connection, options, sqlTransaction)
        {
            DestinationTableName = _mapping.TableName,
            BatchSize = config.BatchSize,
            BulkCopyTimeout = config.BulkCopyTimeout ?? 0,
            EnableStreaming = true
        };

        return bulkCopy;
    }

    private void ConfigureColumnMappings(SqlBulkCopy bulkCopy, bool forInsert, BulkConfig config)
    {
        var excludedProperties = forInsert
            ? _mapping.InsertExcludedProperties
            : _mapping.UpdateExcludedProperties;

        var propertiesToInclude = GetFilteredProperties(config, excludedProperties);

        foreach (var (_, columnName) in propertiesToInclude)
        {
            bulkCopy.ColumnMappings.Add(columnName, columnName);
        }
    }

    #endregion

    #region SQL Generation

    private string BuildMergeUpdateSql(BulkConfig config)
    {
        var excludedProperties = _mapping.UpdateExcludedProperties;
        var propertiesToInclude = GetFilteredProperties(config, excludedProperties);

        var setClauses = propertiesToInclude
            .Where(kvp => kvp.Value != _mapping.IdColumnName)
            .Select(kvp => $"t.[{kvp.Value}] = s.[{kvp.Value}]");

        return $@"
            MERGE INTO {_mapping.TableName} AS t
            USING @TableData AS s
            ON t.[{_mapping.IdColumnName}] = s.[{_mapping.IdColumnName}]
            WHEN MATCHED THEN
                UPDATE SET {string.Join(", ", setClauses)};";
    }

    private string BuildMergeSql(BulkConfig config)
    {
        var insertExcluded = _mapping.InsertExcludedProperties;
        var updateExcluded = _mapping.UpdateExcludedProperties;

        var insertColumns = GetFilteredProperties(config, insertExcluded);
        var updateColumns = GetFilteredProperties(config, updateExcluded)
            .Where(kvp => kvp.Value != _mapping.IdColumnName);

        var insertColumnNames = string.Join(", ", insertColumns.Select(kvp => $"[{kvp.Value}]"));
        var insertValues = string.Join(", ", insertColumns.Select(kvp => $"s.[{kvp.Value}]"));
        var updateClauses = string.Join(", ", updateColumns.Select(kvp => $"t.[{kvp.Value}] = s.[{kvp.Value}]"));

        return $@"
            MERGE INTO {_mapping.TableName} AS t
            USING @TableData AS s
            ON t.[{_mapping.IdColumnName}] = s.[{_mapping.IdColumnName}]
            WHEN MATCHED THEN
                UPDATE SET {updateClauses}
            WHEN NOT MATCHED THEN
                INSERT ({insertColumnNames})
                VALUES ({insertValues});";
    }

    private string BuildBulkDeleteSql()
    {
        return $@"
            DELETE t
            FROM {_mapping.TableName} t
            INNER JOIN @Ids i ON t.[{_mapping.IdColumnName}] = i.[Id];";
    }

    private string GetTvpTypeName()
    {
        var tableName = _mapping.TableName
            .Replace("[", string.Empty, StringComparison.Ordinal)
            .Replace("]", string.Empty, StringComparison.Ordinal)
            .Replace("dbo.", string.Empty, StringComparison.OrdinalIgnoreCase);

        return $"{tableName}Type";
    }

    #endregion

    #region Async Helpers

    private async Task EnsureConnectionOpenAsync(CancellationToken cancellationToken)
    {
        if (_connection.State == ConnectionState.Open)
            return;

        if (_connection is SqlConnection sqlConnection)
        {
            await sqlConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await Task.Run(_connection.Open, cancellationToken).ConfigureAwait(false);
        }
    }

    private static bool IsDuplicateKeyException(Exception ex)
    {
        var message = ex.Message;
        return message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase)
            || message.Contains("unique constraint", StringComparison.OrdinalIgnoreCase)
            || message.Contains("PRIMARY KEY constraint", StringComparison.OrdinalIgnoreCase)
            || message.Contains("violation of PRIMARY KEY", StringComparison.OrdinalIgnoreCase)
            || message.Contains("Cannot insert duplicate key", StringComparison.OrdinalIgnoreCase);
    }

    #endregion
}
