using Encina.Sharding;
using Encina.Sharding.Migrations;
using LanguageExt;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace Encina.MongoDB.Sharding.Migrations;

/// <summary>
/// MongoDB implementation of <see cref="IMigrationHistoryStore"/> that stores
/// migration history in an <c>__encina_migration_history</c> collection.
/// </summary>
internal sealed class MongoMigrationHistoryStore : IMigrationHistoryStore
{
    private const string CollectionName = "__encina_migration_history";

    private readonly IShardedMongoCollectionFactory _collectionFactory;

    public MongoMigrationHistoryStore(IShardedMongoCollectionFactory collectionFactory)
    {
        ArgumentNullException.ThrowIfNull(collectionFactory);
        _collectionFactory = collectionFactory;
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, IReadOnlyList<string>>> GetAppliedAsync(
        string shardId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shardId);

        try
        {
            var collectionResult = GetHistoryCollection(shardId);
            return await collectionResult.MapAsync(async collection =>
            {
                var filter = Builders<MigrationHistoryDocument>.Filter.Eq(x => x.RolledBackAtUtc, null);
                var sort = Builders<MigrationHistoryDocument>.Sort.Ascending(x => x.AppliedAtUtc);

                var cursor = await collection
                    .Find(filter)
                    .Sort(sort)
                    .Project(x => x.MigrationId)
                    .ToCursorAsync(cancellationToken)
                    .ConfigureAwait(false);

                var results = await cursor.ToListAsync(cancellationToken).ConfigureAwait(false);
                return (IReadOnlyList<string>)results;
            }).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return EncinaErrors.Create(MigrationErrorCodes.MigrationFailed,
                $"Failed to get applied migrations for shard '{shardId}': {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> RecordAppliedAsync(
        string shardId, MigrationScript script, TimeSpan duration, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shardId);
        ArgumentNullException.ThrowIfNull(script);

        try
        {
            var collectionResult = GetHistoryCollection(shardId);
            return await collectionResult.MapAsync(async collection =>
            {
                var doc = new MigrationHistoryDocument
                {
                    MigrationId = script.Id,
                    Description = script.Description,
                    Checksum = script.Checksum,
                    AppliedAtUtc = DateTime.UtcNow,
                    DurationMs = (long)duration.TotalMilliseconds
                };

                await collection.InsertOneAsync(doc, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                return Unit.Default;
            }).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return EncinaErrors.Create(MigrationErrorCodes.MigrationFailed,
                $"Failed to record migration '{script.Id}' for shard '{shardId}': {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> RecordRolledBackAsync(
        string shardId, string migrationId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shardId);
        ArgumentException.ThrowIfNullOrWhiteSpace(migrationId);

        try
        {
            var collectionResult = GetHistoryCollection(shardId);
            return await collectionResult.MapAsync(async collection =>
            {
                var filter = Builders<MigrationHistoryDocument>.Filter.And(
                    Builders<MigrationHistoryDocument>.Filter.Eq(x => x.MigrationId, migrationId),
                    Builders<MigrationHistoryDocument>.Filter.Eq(x => x.RolledBackAtUtc, null));

                var update = Builders<MigrationHistoryDocument>.Update
                    .Set(x => x.RolledBackAtUtc, DateTime.UtcNow);

                await collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                return Unit.Default;
            }).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return EncinaErrors.Create(MigrationErrorCodes.RollbackFailed,
                $"Failed to record rollback '{migrationId}' for shard '{shardId}': {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> EnsureHistoryTableExistsAsync(
        ShardInfo shardInfo, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(shardInfo);

        try
        {
            var collectionResult = GetHistoryCollection(shardInfo.ShardId);
            return await collectionResult.MapAsync(async collection =>
            {
                // Create unique index on MigrationId
                var indexModel = new CreateIndexModel<MigrationHistoryDocument>(
                    Builders<MigrationHistoryDocument>.IndexKeys.Ascending(x => x.MigrationId),
                    new CreateIndexOptions { Unique = true, Name = "IX_MigrationId" });

                await collection.Indexes
                    .CreateOneAsync(indexModel, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                return Unit.Default;
            }).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return EncinaErrors.Create(MigrationErrorCodes.MigrationFailed,
                $"Failed to ensure history collection on shard '{shardInfo.ShardId}': {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<Either<EncinaError, Unit>> ApplyHistoricalMigrationsAsync(
        string shardId, IReadOnlyList<MigrationScript> scripts, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shardId);
        ArgumentNullException.ThrowIfNull(scripts);

        try
        {
            var collectionResult = GetHistoryCollection(shardId);
            return await collectionResult.MapAsync(async collection =>
            {
                foreach (var script in scripts)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var filter = Builders<MigrationHistoryDocument>.Filter.Eq(x => x.MigrationId, script.Id);
                    var exists = await collection.Find(filter).AnyAsync(cancellationToken).ConfigureAwait(false);

                    if (!exists)
                    {
                        var doc = new MigrationHistoryDocument
                        {
                            MigrationId = script.Id,
                            Description = script.Description,
                            Checksum = script.Checksum,
                            AppliedAtUtc = DateTime.UtcNow,
                            DurationMs = 0
                        };

                        await collection.InsertOneAsync(doc, cancellationToken: cancellationToken)
                            .ConfigureAwait(false);
                    }
                }

                return Unit.Default;
            }).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return EncinaErrors.Create(MigrationErrorCodes.MigrationFailed,
                $"Failed to apply historical migrations for shard '{shardId}': {ex.Message}", ex);
        }
    }

    private Either<EncinaError, IMongoCollection<MigrationHistoryDocument>> GetHistoryCollection(string shardId)
    {
        return _collectionFactory
            .GetCollectionForShard<MigrationHistoryDocument>(shardId, CollectionName);
    }

    [BsonIgnoreExtraElements]
    internal sealed class MigrationHistoryDocument
    {
        [BsonId]
        public string MigrationId { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string Checksum { get; set; } = string.Empty;

        public DateTime AppliedAtUtc { get; set; }

        public long DurationMs { get; set; }

        public DateTime? RolledBackAtUtc { get; set; }
    }
}
