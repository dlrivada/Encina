using Encina.Sharding;

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

using Testcontainers.MongoDb;

using Xunit;

namespace Encina.TestInfrastructure.Fixtures.Sharding;

/// <summary>
/// Sharding fixture that creates 3 separate databases within a single MongoDB container.
/// Each database represents a shard for integration testing of sharding features.
/// </summary>
public sealed class ShardedMongoDbFixture : IAsyncLifetime
{
    private static readonly object s_serializerLock = new();
    private static bool s_serializerRegistered;

    private MongoDbContainer? _container;

    private const string Shard1DbName = "encina_shard1";
    private const string Shard2DbName = "encina_shard2";
    private const string Shard3DbName = "encina_shard3";

    private const string CollectionName = "sharded_entities";

    /// <summary>
    /// Gets the MongoDB client.
    /// </summary>
    public IMongoClient? Client { get; private set; }

    /// <summary>
    /// Gets the connection string for the MongoDB container.
    /// </summary>
    public string ConnectionString => _container?.GetConnectionString() ?? string.Empty;

    /// <summary>
    /// Gets the connection string for shard 1.
    /// </summary>
    public string Shard1ConnectionString => $"{ConnectionString}/{Shard1DbName}";

    /// <summary>
    /// Gets the connection string for shard 2.
    /// </summary>
    public string Shard2ConnectionString => $"{ConnectionString}/{Shard2DbName}";

    /// <summary>
    /// Gets the connection string for shard 3.
    /// </summary>
    public string Shard3ConnectionString => $"{ConnectionString}/{Shard3DbName}";

    /// <summary>
    /// Gets the database for the specified shard.
    /// </summary>
    /// <param name="shardId">The shard identifier (shard-1, shard-2, or shard-3).</param>
    /// <returns>The MongoDB database for the shard.</returns>
    public IMongoDatabase GetDatabase(string shardId)
    {
        var databaseName = shardId switch
        {
            "shard-1" => Shard1DbName,
            "shard-2" => Shard2DbName,
            "shard-3" => Shard3DbName,
            _ => throw new ArgumentException($"Unknown shard ID: {shardId}", nameof(shardId))
        };

        return Client!.GetDatabase(databaseName);
    }

    /// <summary>
    /// Gets the sharded entities collection for the specified shard.
    /// </summary>
    /// <param name="shardId">The shard identifier.</param>
    /// <returns>The MongoDB collection for the shard.</returns>
    public IMongoCollection<BsonDocument> GetCollection(string shardId)
    {
        return GetDatabase(shardId).GetCollection<BsonDocument>(CollectionName);
    }

    /// <summary>
    /// Creates a <see cref="ShardTopology"/> with all 3 shards.
    /// </summary>
    public ShardTopology CreateTopology() => new([
        new ShardInfo("shard-1", Shard1ConnectionString),
        new ShardInfo("shard-2", Shard2ConnectionString),
        new ShardInfo("shard-3", Shard3ConnectionString)
    ]);

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        RegisterGuidSerializer();

        _container = new MongoDbBuilder()
            .WithImage("mongo:7")
            .WithCleanUp(true)
            .Build();

        await _container.StartAsync();
        Client = new MongoClient(ConnectionString);

        // Create collections in each shard database to ensure they exist
        await CreateShardCollectionAsync(Shard1DbName);
        await CreateShardCollectionAsync(Shard2DbName);
        await CreateShardCollectionAsync(Shard3DbName);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    /// <summary>
    /// Clears all data from all 3 shard databases.
    /// </summary>
    public async Task ClearAllDataAsync()
    {
        await ClearShardDataAsync(Shard1DbName);
        await ClearShardDataAsync(Shard2DbName);
        await ClearShardDataAsync(Shard3DbName);
    }

    private async Task CreateShardCollectionAsync(string databaseName)
    {
        var database = Client!.GetDatabase(databaseName);
        await database.CreateCollectionAsync(CollectionName);
    }

    private async Task ClearShardDataAsync(string databaseName)
    {
        var database = Client!.GetDatabase(databaseName);
        var collection = database.GetCollection<BsonDocument>(CollectionName);
        await collection.DeleteManyAsync(FilterDefinition<BsonDocument>.Empty);
    }

    private static void RegisterGuidSerializer()
    {
        lock (s_serializerLock)
        {
            if (!s_serializerRegistered)
            {
                try
                {
                    BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
                    s_serializerRegistered = true;
                }
                catch (BsonSerializationException)
                {
                    s_serializerRegistered = true;
                }
            }
        }
    }
}
