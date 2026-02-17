using Encina.Sharding;
using Encina.Sharding.Migrations;
using LanguageExt;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Encina.MongoDB.Sharding.Migrations;

/// <summary>
/// MongoDB implementation of <see cref="ISchemaIntrospector"/> that reads schema
/// metadata by listing collections and their indexes/validators.
/// </summary>
/// <remarks>
/// Since MongoDB is schema-less, the introspector inspects collection names,
/// indexes, and JSON Schema validators as a proxy for "schema".
/// </remarks>
internal sealed class MongoSchemaIntrospector : ISchemaIntrospector
{
    private readonly IShardedMongoCollectionFactory _collectionFactory;

    public MongoSchemaIntrospector(IShardedMongoCollectionFactory collectionFactory)
    {
        ArgumentNullException.ThrowIfNull(collectionFactory);
        _collectionFactory = collectionFactory;
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, ShardSchemaDiff>> CompareAsync(
        ShardInfo shard,
        ShardInfo baselineShard,
        bool includeColumnDiffs,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(shard);
        ArgumentNullException.ThrowIfNull(baselineShard);

        try
        {
            var shardSchemaResult = await IntrospectAsync(shard, includeColumnDiffs, cancellationToken)
                .ConfigureAwait(false);
            var baselineSchemaResult = await IntrospectAsync(baselineShard, includeColumnDiffs, cancellationToken)
                .ConfigureAwait(false);

            return from shardSchema in shardSchemaResult
                   from baselineSchema in baselineSchemaResult
                   select SchemaComparer.Compare(
                       shard.ShardId,
                       baselineShard.ShardId,
                       shardSchema,
                       baselineSchema,
                       includeColumnDiffs);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return EncinaErrors.Create(
                MigrationErrorCodes.SchemaComparisonFailed,
                $"Schema comparison failed between shard '{shard.ShardId}' and baseline '{baselineShard.ShardId}': {ex.Message}",
                ex);
        }
    }

    private async Task<Either<EncinaError, ShardSchema>> IntrospectAsync(
        ShardInfo shard,
        bool includeIndexes,
        CancellationToken cancellationToken)
    {
        // Get the database for this shard via a dummy collection
        var collectionResult = _collectionFactory.GetCollectionForShard<BsonDocument>(
            shard.ShardId, "__encina_introspect_ping");

        return await collectionResult
            .MapAsync(async collection =>
            {
                var database = collection.Database;
                var tables = new List<TableSchema>();

                // List all collection names
                using var cursor = await database
                    .ListCollectionNamesAsync(cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                var collectionNames = await cursor.ToListAsync(cancellationToken).ConfigureAwait(false);

                foreach (var collectionName in collectionNames.Where(n => !n.StartsWith("system.", StringComparison.Ordinal)))
                {
                    var columns = includeIndexes
                        ? await GetIndexesAsColumnsAsync(database, collectionName, cancellationToken).ConfigureAwait(false)
                        : [];

                    tables.Add(new TableSchema(collectionName, columns));
                }

                return new ShardSchema(shard.ShardId, tables, DateTimeOffset.UtcNow);
            })
            .ConfigureAwait(false);
    }

    private static async Task<IReadOnlyList<ColumnSchema>> GetIndexesAsColumnsAsync(
        IMongoDatabase database,
        string collectionName,
        CancellationToken cancellationToken)
    {
        // For MongoDB, we represent indexes as "columns" to fit the schema model
        var coll = database.GetCollection<BsonDocument>(collectionName);
        var columns = new List<ColumnSchema>();

        using var cursor = await coll.Indexes
            .ListAsync(cancellationToken)
            .ConfigureAwait(false);

        var indexes = await cursor.ToListAsync(cancellationToken).ConfigureAwait(false);

        foreach (var index in indexes)
        {
            var indexName = index.GetValue("name", "unknown").AsString;
            var keyDoc = index.GetValue("key", new BsonDocument()).AsBsonDocument;
            var isUnique = index.GetValue("unique", false).AsBoolean;

            foreach (var key in keyDoc.Elements)
            {
                columns.Add(new ColumnSchema(
                    Name: $"{indexName}.{key.Name}",
                    DataType: $"index({key.Value})",
                    IsNullable: !isUnique,
                    DefaultValue: null));
            }
        }

        return columns;
    }
}
