using Encina.IdGeneration;
using Encina.IdGeneration.Generators;
using Encina.MongoDB.Serializers;
using Encina.TestInfrastructure.Fixtures;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace Encina.IntegrationTests.Infrastructure.MongoDB.IdGeneration;

/// <summary>
/// Test document with all four ID generation types.
/// </summary>
public class IdGenerationDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    public SnowflakeId SnowflakeCol { get; set; }
    public UlidId UlidCol { get; set; }
    public UuidV7Id UuidV7Col { get; set; }
    public ShardPrefixedId ShardPrefixedCol { get; set; }
}

/// <summary>
/// Integration tests for MongoDB BSON serializers with all four ID types.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Provider", "MongoDB")]
[Collection("MongoDB")]
public sealed class IdGenerationSerializerTests : IAsyncLifetime
{
    private const string CollectionName = "id_generation_test";
    private readonly MongoDbFixture _fixture;

    public IdGenerationSerializerTests(MongoDbFixture fixture)
    {
        _fixture = fixture;

        // Register all ID generation serializers
        IdGenerationSerializerRegistration.EnsureRegistered();
    }

    public async ValueTask InitializeAsync()
    {
        if (!_fixture.IsAvailable) return;

        var collection = GetCollection();
        await collection.DeleteManyAsync(Builders<IdGenerationDocument>.Filter.Empty);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private IMongoCollection<IdGenerationDocument> GetCollection()
        => _fixture.Database!.GetCollection<IdGenerationDocument>(CollectionName);

    // ────────────────────────────────────────────────────────────
    //  SnowflakeId
    // ────────────────────────────────────────────────────────────

    [Fact]
    public async Task SnowflakeId_PersistAndRetrieve_RoundtripsCorrectly()
    {
        Assert.SkipUnless(_fixture.IsAvailable, "MongoDB container not available");

        var collection = GetCollection();
        var original = new SnowflakeId(123456789L);

        var doc = new IdGenerationDocument { SnowflakeCol = original };
        await collection.InsertOneAsync(doc);

        var retrieved = await collection
            .Find(Builders<IdGenerationDocument>.Filter.Eq(d => d.Id, doc.Id))
            .FirstAsync();

        retrieved.SnowflakeCol.ShouldBe(original);
    }

    // ────────────────────────────────────────────────────────────
    //  UlidId
    // ────────────────────────────────────────────────────────────

    [Fact]
    public async Task UlidId_PersistAndRetrieve_RoundtripsCorrectly()
    {
        Assert.SkipUnless(_fixture.IsAvailable, "MongoDB container not available");

        var collection = GetCollection();
        var original = UlidId.NewUlid();

        var doc = new IdGenerationDocument { UlidCol = original };
        await collection.InsertOneAsync(doc);

        var retrieved = await collection
            .Find(Builders<IdGenerationDocument>.Filter.Eq(d => d.Id, doc.Id))
            .FirstAsync();

        retrieved.UlidCol.ShouldBe(original);
    }

    // ────────────────────────────────────────────────────────────
    //  UuidV7Id
    // ────────────────────────────────────────────────────────────

    [Fact]
    public async Task UuidV7Id_PersistAndRetrieve_RoundtripsCorrectly()
    {
        Assert.SkipUnless(_fixture.IsAvailable, "MongoDB container not available");

        var collection = GetCollection();
        var original = new UuidV7IdGenerator().Generate().Match(id => id, _ => default);

        var doc = new IdGenerationDocument { UuidV7Col = original };
        await collection.InsertOneAsync(doc);

        var retrieved = await collection
            .Find(Builders<IdGenerationDocument>.Filter.Eq(d => d.Id, doc.Id))
            .FirstAsync();

        retrieved.UuidV7Col.Value.ShouldBe(original.Value);
    }

    // ────────────────────────────────────────────────────────────
    //  ShardPrefixedId
    // ────────────────────────────────────────────────────────────

    [Fact]
    public async Task ShardPrefixedId_PersistAndRetrieve_RoundtripsCorrectly()
    {
        Assert.SkipUnless(_fixture.IsAvailable, "MongoDB container not available");

        var collection = GetCollection();
        var original = ShardPrefixedId.Parse($"shard-01:{UlidId.NewUlid()}");

        var doc = new IdGenerationDocument { ShardPrefixedCol = original };
        await collection.InsertOneAsync(doc);

        var retrieved = await collection
            .Find(Builders<IdGenerationDocument>.Filter.Eq(d => d.Id, doc.Id))
            .FirstAsync();

        retrieved.ShardPrefixedCol.ToString().ShouldBe(original.ToString());
    }

    // ────────────────────────────────────────────────────────────
    //  All types in same document
    // ────────────────────────────────────────────────────────────

    [Fact]
    public async Task AllIdTypes_InSameDocument_RoundtripCorrectly()
    {
        Assert.SkipUnless(_fixture.IsAvailable, "MongoDB container not available");

        var collection = GetCollection();
        var snowflake = new SnowflakeId(999L);
        var ulid = UlidId.NewUlid();
        var uuid = new UuidV7IdGenerator().Generate().Match(id => id, _ => default);
        var shardPrefixed = ShardPrefixedId.Parse($"tenant-a:{ulid}");

        var doc = new IdGenerationDocument
        {
            SnowflakeCol = snowflake,
            UlidCol = ulid,
            UuidV7Col = uuid,
            ShardPrefixedCol = shardPrefixed
        };
        await collection.InsertOneAsync(doc);

        var retrieved = await collection
            .Find(Builders<IdGenerationDocument>.Filter.Eq(d => d.Id, doc.Id))
            .FirstAsync();

        retrieved.SnowflakeCol.ShouldBe(snowflake);
        retrieved.UlidCol.ShouldBe(ulid);
        retrieved.UuidV7Col.Value.ShouldBe(uuid.Value);
        retrieved.ShardPrefixedCol.ToString().ShouldBe(shardPrefixed.ToString());
    }
}
