using Encina.TestInfrastructure.Fixtures;
using MongoDB.Bson;
using MongoDB.Driver;
using Shouldly;
using Xunit;

namespace Encina.IntegrationTests.Infrastructure.MongoDB.ReadWriteSeparation;

/// <summary>
/// Tests to verify the <see cref="MongoDbReplicaSetFixture"/> infrastructure works correctly.
/// </summary>
[Collection(MongoDbReplicaSetCollection.Name)]
[Trait("Category", "Integration")]
[Trait("Database", "MongoDB")]
public sealed class MongoDbReplicaSetFixtureTests
{
    private readonly MongoDbReplicaSetFixture _fixture;

    public MongoDbReplicaSetFixtureTests(MongoDbReplicaSetFixture fixture)
    {
        _fixture = fixture;
    }

    [SkippableFact]
    public void Fixture_WhenDockerAvailable_ShouldBeAvailable()
    {
        Skip.IfNot(_fixture.IsAvailable, "MongoDB replica set container not available");

        _fixture.IsAvailable.ShouldBeTrue();
        _fixture.Client.ShouldNotBeNull();
        _fixture.Database.ShouldNotBeNull();
        _fixture.ConnectionString.ShouldNotBeNullOrEmpty();
    }

    [SkippableFact]
    public async Task ReplicaSet_WhenInitialized_ShouldHavePrimary()
    {
        Skip.IfNot(_fixture.IsAvailable, "MongoDB replica set container not available");

        // Act
        var status = await _fixture.GetReplicaSetStatusAsync();

        // Assert
        status.ShouldNotBeNull();
        status!.Contains("set").ShouldBeTrue();
        status["set"].AsString.ShouldBe(MongoDbReplicaSetFixture.ReplicaSetName);

        // Verify we have a PRIMARY member
        var members = status["members"].AsBsonArray;
        var hasPrimary = members.Any(m => m.AsBsonDocument["stateStr"].AsString == "PRIMARY");
        hasPrimary.ShouldBeTrue();
    }

    [SkippableFact]
    public async Task ReplicaSet_ShouldSupportReadPreferences()
    {
        Skip.IfNot(_fixture.IsAvailable, "MongoDB replica set container not available");

        // Arrange
        var database = _fixture.Database!;
        var collectionName = $"test_read_pref_{Guid.NewGuid():N}";

        // Act - Create collection with Primary read preference
        var primaryCollection = database
            .GetCollection<BsonDocument>(collectionName)
            .WithReadPreference(ReadPreference.Primary);

        // Insert a document
        await primaryCollection.InsertOneAsync(new BsonDocument("test", "value"));

        // Read with different read preferences
        var primaryReadCollection = database
            .GetCollection<BsonDocument>(collectionName)
            .WithReadPreference(ReadPreference.Primary);

        var secondaryPreferredCollection = database
            .GetCollection<BsonDocument>(collectionName)
            .WithReadPreference(ReadPreference.SecondaryPreferred);

        // Assert - Both should be able to read (single-node RS, primary serves all reads)
        var primaryResult = await primaryReadCollection.Find(FilterDefinition<BsonDocument>.Empty).FirstOrDefaultAsync();
        var secondaryPreferredResult = await secondaryPreferredCollection.Find(FilterDefinition<BsonDocument>.Empty).FirstOrDefaultAsync();

        primaryResult.ShouldNotBeNull();
        secondaryPreferredResult.ShouldNotBeNull();
        primaryResult["test"].AsString.ShouldBe("value");
        secondaryPreferredResult["test"].AsString.ShouldBe("value");

        // Cleanup
        await database.DropCollectionAsync(collectionName);
    }

    [SkippableFact]
    public async Task ReplicaSet_ShouldSupportTransactions()
    {
        Skip.IfNot(_fixture.IsAvailable, "MongoDB replica set container not available");

        // Arrange
        var database = _fixture.Database!;
        var collectionName = $"test_transaction_{Guid.NewGuid():N}";
        var collection = database.GetCollection<BsonDocument>(collectionName);

        // Create collection first (required for transactions)
        await database.CreateCollectionAsync(collectionName);

        // Act - Use a transaction
        using var session = await _fixture.Client!.StartSessionAsync();
        session.StartTransaction();

        try
        {
            await collection.InsertOneAsync(session, new BsonDocument("txn", "test"));
            await session.CommitTransactionAsync();
        }
        catch
        {
            await session.AbortTransactionAsync();
            throw;
        }

        // Assert
        var result = await collection.Find(FilterDefinition<BsonDocument>.Empty).FirstOrDefaultAsync();
        result.ShouldNotBeNull();
        result["txn"].AsString.ShouldBe("test");

        // Cleanup
        await database.DropCollectionAsync(collectionName);
    }
}
