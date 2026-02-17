using System.Data;
using Encina.Sharding.ReferenceTables;
using LanguageExt;
using MySqlConnector;

namespace Encina.ADO.MySQL.Sharding.ReferenceTables;

/// <summary>
/// MySQL ADO.NET implementation of <see cref="IReferenceTableStore"/> using
/// <c>INSERT ... ON DUPLICATE KEY UPDATE</c> for bulk upsert operations.
/// </summary>
/// <remarks>
/// <para>
/// The store discovers entity metadata (table name, columns, primary key) via
/// reflection at runtime, caching it per entity type in <see cref="EntityMetadataCache"/>.
/// </para>
/// <para>
/// MySQL supports up to 65535 parameters per statement. The store batches
/// upsert operations to stay within this limit.
/// </para>
/// </remarks>
public sealed class ReferenceTableStoreADO(IDbConnection connection) : IReferenceTableStore, IDisposable
{
    private const int MaxParametersPerStatement = 65535;

    private readonly IDbConnection _connection = connection ?? throw new ArgumentNullException(nameof(connection));

    /// <inheritdoc />
    public async Task<Either<EncinaError, int>> UpsertAsync<TEntity>(
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(entities);

        try
        {
            var metadata = EntityMetadataCache.GetOrCreate<TEntity>();
            var entityList = entities as IList<TEntity> ?? entities.ToList();

            if (entityList.Count == 0)
            {
                return 0;
            }

            var columnCount = metadata.AllProperties.Count;
            var batchSize = Math.Max(1, MaxParametersPerStatement / columnCount);
            var totalAffected = 0;

            for (var offset = 0; offset < entityList.Count; offset += batchSize)
            {
                var batch = entityList.Skip(offset).Take(batchSize).ToList();
                var affected = await UpsertBatchAsync(metadata, batch, cancellationToken)
                    .ConfigureAwait(false);
                totalAffected += affected;
            }

            return totalAffected;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return EncinaErrors.Create(
                ReferenceTableErrorCodes.UpsertFailed,
                $"Failed to upsert reference table '{typeof(TEntity).Name}': {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, IReadOnlyList<TEntity>>> GetAllAsync<TEntity>(
        CancellationToken cancellationToken = default)
        where TEntity : class
    {
        try
        {
            var metadata = EntityMetadataCache.GetOrCreate<TEntity>();
            var columns = string.Join(", ", metadata.AllProperties.Select(p => $"`{p.ColumnName}`"));
            var sql = $"SELECT {columns} FROM `{metadata.TableName}`";

            using var command = _connection.CreateCommand();
            command.CommandText = sql;

            var results = new List<TEntity>();

            if (command is MySqlCommand mysqlCommand)
            {
                await using var reader = await mysqlCommand.ExecuteReaderAsync(cancellationToken)
                    .ConfigureAwait(false);

                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    results.Add(MapEntity<TEntity>(reader, metadata));
                }
            }
            else
            {
                using var reader = command.ExecuteReader();

                while (reader.Read())
                {
                    results.Add(MapEntity<TEntity>(reader, metadata));
                }
            }

            return results.AsReadOnly();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return EncinaErrors.Create(
                ReferenceTableErrorCodes.GetAllFailed,
                $"Failed to read reference table '{typeof(TEntity).Name}': {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, string>> GetHashAsync<TEntity>(
        CancellationToken cancellationToken = default)
        where TEntity : class
    {
        var result = await GetAllAsync<TEntity>(cancellationToken).ConfigureAwait(false);

        return result.Map(entities => ReferenceTableHashComputer.ComputeHash(entities));
    }

    /// <inheritdoc cref="IDisposable.Dispose" />
    public void Dispose() => _connection.Dispose();

    private async Task<int> UpsertBatchAsync<TEntity>(
        EntityMetadata metadata,
        List<TEntity> batch,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        // INSERT INTO `Table` (`col1`, `col2`, ...)
        // VALUES (@p0_col1, @p0_col2, ...), (@p1_col1, @p1_col2, ...)
        // ON DUPLICATE KEY UPDATE `col` = VALUES(`col`), ...
        var columns = metadata.AllProperties.Select(p => $"`{p.ColumnName}`").ToList();

        var valueSets = new List<string>(batch.Count);
        using var command = _connection.CreateCommand();

        for (var i = 0; i < batch.Count; i++)
        {
            var entity = batch[i];
            var paramNames = new List<string>(metadata.AllProperties.Count);

            foreach (var prop in metadata.AllProperties)
            {
                var paramName = $"@p{i}_{prop.ColumnName}";
                paramNames.Add(paramName);
                AddParameter(command, paramName, prop.Property.GetValue(entity));
            }

            valueSets.Add($"({string.Join(", ", paramNames)})");
        }

        var updateSet = string.Join(", ",
            metadata.NonKeyProperties.Select(p => $"`{p.ColumnName}` = VALUES(`{p.ColumnName}`)"));

        var sql = $"""
            INSERT INTO `{metadata.TableName}` ({string.Join(", ", columns)})
            VALUES {string.Join(", ", valueSets)}
            ON DUPLICATE KEY UPDATE {updateSet}
            """;

        command.CommandText = sql;

        if (command is MySqlCommand mysqlCommand)
        {
            return await mysqlCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        return await Task.Run(command.ExecuteNonQuery, cancellationToken).ConfigureAwait(false);
    }

    private static TEntity MapEntity<TEntity>(IDataReader reader, EntityMetadata metadata)
        where TEntity : class
    {
        var entity = Activator.CreateInstance<TEntity>();

        for (var i = 0; i < metadata.AllProperties.Count; i++)
        {
            var prop = metadata.AllProperties[i];
            var value = reader.GetValue(i);

            if (value is DBNull)
            {
                prop.Property.SetValue(entity, null);
            }
            else
            {
                var targetType = Nullable.GetUnderlyingType(prop.Property.PropertyType)
                    ?? prop.Property.PropertyType;
                var converted = Convert.ChangeType(value, targetType,
                    System.Globalization.CultureInfo.InvariantCulture);
                prop.Property.SetValue(entity, converted);
            }
        }

        return entity;
    }

    private static void AddParameter(IDbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }
}
