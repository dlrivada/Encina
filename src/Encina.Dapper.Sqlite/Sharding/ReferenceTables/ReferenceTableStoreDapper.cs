using System.Data;
using Dapper;
using Encina.Sharding.ReferenceTables;
using LanguageExt;

namespace Encina.Dapper.Sqlite.Sharding.ReferenceTables;

/// <summary>
/// SQLite Dapper implementation of <see cref="IReferenceTableStore"/> using
/// <c>INSERT OR REPLACE</c> for bulk upsert operations.
/// </summary>
/// <remarks>
/// <para>
/// SQLite has a 999 parameter limit per statement. The store automatically batches
/// upsert operations to stay within this limit.
/// </para>
/// </remarks>
public sealed class ReferenceTableStoreDapper(IDbConnection connection) : IReferenceTableStore, IDisposable
{
    private const int MaxParametersPerStatement = 999;

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
                var affected = await UpsertBatchAsync(metadata, batch).ConfigureAwait(false);
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
            var columns = string.Join(", ", metadata.AllProperties.Select(p => p.ColumnName));
            var sql = $"SELECT {columns} FROM {metadata.TableName}";

            var results = (await _connection.QueryAsync<TEntity>(sql).ConfigureAwait(false)).ToList();

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
        List<TEntity> batch)
        where TEntity : class
    {
        var columns = string.Join(", ", metadata.AllProperties.Select(p => p.ColumnName));

        var valueSets = new List<string>(batch.Count);
        var parameters = new DynamicParameters();

        for (var i = 0; i < batch.Count; i++)
        {
            var entity = batch[i];
            var paramNames = new List<string>(metadata.AllProperties.Count);

            foreach (var prop in metadata.AllProperties)
            {
                var paramName = $"p{i}_{prop.ColumnName}";
                paramNames.Add($"@{paramName}");
                parameters.Add(paramName, prop.Property.GetValue(entity));
            }

            valueSets.Add($"({string.Join(", ", paramNames)})");
        }

        var sql = $"INSERT OR REPLACE INTO {metadata.TableName} ({columns}) VALUES {string.Join(", ", valueSets)}";

        return await _connection.ExecuteAsync(sql, parameters).ConfigureAwait(false);
    }
}
