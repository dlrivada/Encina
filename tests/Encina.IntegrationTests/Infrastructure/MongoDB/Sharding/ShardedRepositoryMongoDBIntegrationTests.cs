using System.Linq.Expressions;
using Encina.IntegrationTests.Infrastructure.MongoDB;
using Encina.MongoDB.Sharding;
using Encina.TestInfrastructure.Fixtures;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Driver;
using Shouldly;
using Xunit;

namespace Encina.IntegrationTests.Infrastructure.MongoDB.Sharding;

/// <summary>
/// Integration tests for <see cref="FunctionalShardedRepositoryMongoDB{TEntity, TId}"/>
/// using native mongos routing against real MongoDB via Testcontainers.
/// </summary>
/// <remarks>
/// These tests exercise the native sharding mode, which delegates to a single
/// <see cref="IMongoCollection{TDocument}"/> and lets MongoDB handle routing.
/// This validates the full CRUD path through the sharded repository layer.
/// </remarks>
[Collection(MongoDbTestCollection.Name)]
[Trait("Category", "Integration")]
[Trait("Database", "MongoDB")]
public sealed class ShardedRepositoryMongoDBIntegrationTests : IAsyncLifetime
{
    private const string CollectionName = "sharded_test_orders";

    private readonly MongoDbFixture _fixture;
    private IMongoCollection<ShardedOrder>? _collection;
    private FunctionalShardedRepositoryMongoDB<ShardedOrder, Guid>? _repository;

    public ShardedRepositoryMongoDBIntegrationTests(MongoDbFixture fixture)
    {
        _fixture = fixture;
    }

    public ValueTask InitializeAsync()
    {
        if (!_fixture.IsAvailable)
        {
            return ValueTask.CompletedTask;
        }

        _collection = _fixture.Database!.GetCollection<ShardedOrder>(CollectionName);

        // Use native sharding mode (single MongoDB instance, no actual sharding cluster needed).
        // FunctionalShardedRepositoryMongoDB delegates to FunctionalRepositoryMongoDB for CRUD.
        var collectionFactory = new ShardedMongoCollectionFactory(
            _fixture.Client!,
            MongoDbFixture.DatabaseName);

        _repository = new FunctionalShardedRepositoryMongoDB<ShardedOrder, Guid>(
            collectionFactory,
            o => o.Id,
            CollectionName,
            NullLogger<FunctionalShardedRepositoryMongoDB<ShardedOrder, Guid>>.Instance);

        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        if (_collection is not null)
        {
            await _collection.DeleteManyAsync(Builders<ShardedOrder>.Filter.Empty);
        }
    }

    private async Task ClearDataAsync()
    {
        if (_collection is not null)
        {
            await _collection.DeleteManyAsync(Builders<ShardedOrder>.Filter.Empty);
        }
    }

    private void SkipIfNotAvailable()
    {
        if (!_fixture.IsAvailable || _repository is null)
        {
            Assert.Skip("MongoDB container is not available");
        }
    }

    #region AddAsync Tests

    [Fact]
    public async Task AddAsync_ValidEntity_PersistsToMongoDB()
    {
        SkipIfNotAvailable();
        await ClearDataAsync();

        // Arrange
        var order = CreateOrder("Widget", 42.50m);

        // Act
        var result = await _repository!.AddAsync(order);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(e =>
        {
            e.Id.ShouldBe(order.Id);
            e.ProductName.ShouldBe("Widget");
        });

        var stored = await _collection!.Find(o => o.Id == order.Id).FirstOrDefaultAsync();
        stored.ShouldNotBeNull();
        stored.Amount.ShouldBe(42.50m);
    }

    [Fact]
    public async Task AddAsync_DuplicateId_ReturnsError()
    {
        SkipIfNotAvailable();
        await ClearDataAsync();

        // Arrange
        var order = CreateOrder("Duplicate");
        await _repository!.AddAsync(order);

        var duplicate = new ShardedOrder
        {
            Id = order.Id,
            ProductName = "Duplicate2",
            Amount = 10m,
            Region = "US"
        };

        // Act
        var result = await _repository.AddAsync(duplicate);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ExistingEntity_ReturnsRight()
    {
        SkipIfNotAvailable();
        await ClearDataAsync();

        // Arrange
        var order = CreateOrder("Findable");
        await _repository!.AddAsync(order);

        // Act - native sharding uses "mongos" as shard key
        var result = await _repository.GetByIdAsync(order.Id, "mongos");

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(e =>
        {
            e.Id.ShouldBe(order.Id);
            e.ProductName.ShouldBe("Findable");
        });
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingEntity_ReturnsLeft()
    {
        SkipIfNotAvailable();
        await ClearDataAsync();

        // Act
        var result = await _repository!.GetByIdAsync(Guid.NewGuid(), "mongos");

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ExistingEntity_PersistsChanges()
    {
        SkipIfNotAvailable();
        await ClearDataAsync();

        // Arrange
        var order = CreateOrder("OriginalProduct", 10m);
        await _repository!.AddAsync(order);

        order.ProductName = "UpdatedProduct";
        order.Amount = 99.99m;

        // Act
        var result = await _repository.UpdateAsync(order);

        // Assert
        result.IsRight.ShouldBeTrue();

        var stored = await _collection!.Find(o => o.Id == order.Id).FirstOrDefaultAsync();
        stored.ShouldNotBeNull();
        stored.ProductName.ShouldBe("UpdatedProduct");
        stored.Amount.ShouldBe(99.99m);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingEntity_ReturnsError()
    {
        SkipIfNotAvailable();
        await ClearDataAsync();

        // Arrange
        var order = CreateOrder("Ghost");

        // Act
        var result = await _repository!.UpdateAsync(order);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_ExistingEntity_RemovesFromMongoDB()
    {
        SkipIfNotAvailable();
        await ClearDataAsync();

        // Arrange
        var order = CreateOrder("Deletable");
        await _repository!.AddAsync(order);

        // Act
        var result = await _repository.DeleteAsync(order.Id, "mongos");

        // Assert
        result.IsRight.ShouldBeTrue();

        var stored = await _collection!.Find(o => o.Id == order.Id).FirstOrDefaultAsync();
        stored.ShouldBeNull();
    }

    [Fact]
    public async Task DeleteAsync_NonExistingEntity_ReturnsError()
    {
        SkipIfNotAvailable();
        await ClearDataAsync();

        // Act
        var result = await _repository!.DeleteAsync(Guid.NewGuid(), "mongos");

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region QueryAllShardsAsync Tests

    [Fact]
    public async Task QueryAllShardsAsync_NativeMode_ReturnsAllDocuments()
    {
        SkipIfNotAvailable();
        await ClearDataAsync();

        // Arrange
        await _repository!.AddAsync(CreateOrder("Order1", 10m));
        await _repository.AddAsync(CreateOrder("Order2", 20m));
        await _repository.AddAsync(CreateOrder("Order3", 30m));

        // Act
        var result = await _repository.QueryAllShardsAsync(
            async (shardId, ct) =>
            {
                // In native mode, shardId is "mongos"
                shardId.ShouldBe("mongos");

                var collection = _fixture.Database!.GetCollection<ShardedOrder>(CollectionName);
                var list = await collection.Find(Builders<ShardedOrder>.Filter.Empty)
                    .ToListAsync(ct);
                return (LanguageExt.Either<EncinaError, IReadOnlyList<ShardedOrder>>)list.AsReadOnly();
            });

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(queryResult =>
        {
            queryResult.Results.ShouldNotBeEmpty();
            queryResult.Results.Count.ShouldBe(3);
        });
    }

    #endregion

    #region GetShardIdForEntity Tests

    [Fact]
    public void GetShardIdForEntity_NativeMode_ReturnsMongos()
    {
        SkipIfNotAvailable();

        // Arrange
        var order = CreateOrder("AnyOrder");

        // Act
        var result = _repository!.GetShardIdForEntity(order);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(shardId => shardId.ShouldBe("mongos"));
    }

    #endregion

    #region Test Helpers

    private static ShardedOrder CreateOrder(string productName = "TestProduct", decimal amount = 100m)
    {
        return new ShardedOrder
        {
            Id = Guid.NewGuid(),
            ProductName = productName,
            Amount = amount,
            Region = "US-East"
        };
    }

    #endregion
}

#region Test Entity

/// <summary>
/// Test entity for sharded repository integration tests.
/// </summary>
public class ShardedOrder
{
    [global::MongoDB.Bson.Serialization.Attributes.BsonGuidRepresentation(global::MongoDB.Bson.GuidRepresentation.Standard)]
    public Guid Id { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    /// <summary>
    /// Shard key property (used in application-level sharding scenarios).
    /// </summary>
    public string Region { get; set; } = string.Empty;
}

#endregion
