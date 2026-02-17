using Encina.Sharding;
using Encina.Sharding.Migrations;
using LanguageExt;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Encina.MongoDB.Sharding.Migrations;

/// <summary>
/// MongoDB implementation of <see cref="IMigrationExecutor"/> that executes
/// database commands using <c>IMongoDatabase.RunCommand</c>.
/// </summary>
/// <remarks>
/// <para>
/// Since MongoDB is schema-less, "DDL" operations are typically index creation,
/// collection creation, or validator updates. The <c>sql</c> parameter is treated
/// as a JSON command document.
/// </para>
/// </remarks>
internal sealed class MongoMigrationExecutor : IMigrationExecutor
{
    private readonly IShardedMongoCollectionFactory _collectionFactory;

    public MongoMigrationExecutor(IShardedMongoCollectionFactory collectionFactory)
    {
        ArgumentNullException.ThrowIfNull(collectionFactory);
        _collectionFactory = collectionFactory;
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> ExecuteSqlAsync(
        ShardInfo shardInfo,
        string sql,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(shardInfo);
        ArgumentException.ThrowIfNullOrWhiteSpace(sql);

        try
        {
            // For MongoDB, "sql" is a JSON command document
            var commandDoc = BsonDocument.Parse(sql);

            // Use the collection factory to resolve the database for this shard
            // We use a dummy collection to access the database
            var collectionResult = _collectionFactory.GetCollectionForShard<BsonDocument>(
                shardInfo.ShardId, "__encina_migration_ping");

            return await collectionResult
                .MapAsync(async collection =>
                {
                    var database = collection.Database;
                    var command = new BsonDocumentCommand<BsonDocument>(commandDoc);
                    await database.RunCommandAsync(command, cancellationToken: cancellationToken)
                        .ConfigureAwait(false);
                    return Unit.Default;
                })
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return EncinaErrors.Create(
                MigrationErrorCodes.MigrationFailed,
                $"Failed to execute command on shard '{shardInfo.ShardId}': {ex.Message}",
                ex);
        }
    }
}
