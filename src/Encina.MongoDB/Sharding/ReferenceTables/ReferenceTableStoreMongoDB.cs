using System.Collections.Concurrent;
using Encina.Sharding.ReferenceTables;
using LanguageExt;
using MongoDB.Driver;

namespace Encina.MongoDB.Sharding.ReferenceTables;

/// <summary>
/// MongoDB implementation of <see cref="IReferenceTableStore"/> using
/// <see cref="IMongoCollection{TDocument}.BulkWriteAsync(System.Collections.Generic.IEnumerable{WriteModel{TDocument}}, BulkWriteOptions?, CancellationToken)"/>
/// with <see cref="ReplaceOneModel{TDocument}"/> for upsert operations.
/// </summary>
/// <remarks>
/// <para>
/// MongoDB uses the entity's primary key property value to build <c>ReplaceOne</c> filters.
/// The collection name is derived from <see cref="EntityMetadataCache"/>, matching the
/// table name convention used by other providers.
/// </para>
/// </remarks>
public sealed class ReferenceTableStoreMongoDB(IMongoDatabase database) : IReferenceTableStore
{
    private readonly IMongoDatabase _database = database ?? throw new ArgumentNullException(nameof(database));

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

            var collection = _database.GetCollection<TEntity>(metadata.TableName);
            var models = new List<WriteModel<TEntity>>(entityList.Count);

            foreach (var entity in entityList)
            {
                var pkValue = metadata.PrimaryKey.Property.GetValue(entity);
                var filter = Builders<TEntity>.Filter.Eq(metadata.PrimaryKey.Property.Name, pkValue);

                models.Add(new ReplaceOneModel<TEntity>(filter, entity) { IsUpsert = true });
            }

            var result = await collection
                .BulkWriteAsync(models, new BulkWriteOptions { IsOrdered = false }, cancellationToken)
                .ConfigureAwait(false);

            return (int)(result.ModifiedCount + result.Upserts.Count);
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
            var collection = _database.GetCollection<TEntity>(metadata.TableName);

            var results = await collection
                .Find(FilterDefinition<TEntity>.Empty)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

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
}
