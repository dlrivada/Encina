using Encina.MongoDB.Sharding;
using Encina.Sharding;
using Encina.TestInfrastructure.Fixtures.Sharding;

using Microsoft.Extensions.DependencyInjection;

using MongoDB.Driver;

using Shouldly;

using Xunit;

namespace Encina.IntegrationTests.Sharding.MongoDB;

/// <summary>
/// Integration tests verifying routing correctness using MongoDB.
/// </summary>
[Collection("Sharding-MongoDB")]
[Trait("Category", "Integration")]
[Trait("Database", "MongoDB")]
public sealed class ShardedRoutingVerificationTests : IAsyncLifetime
{
    private readonly ShardedMongoDbFixture _fixture;
    private ServiceProvider _serviceProvider = null!;

    public ShardedRoutingVerificationTests(ShardedMongoDbFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync()
    {
        await _fixture.ClearAllDataAsync();

        var services = new ServiceCollection();
        services.AddLogging();

        services.AddSingleton<IMongoClient>(_fixture.Client!);

        services.AddEncinaSharding<ShardedTestEntity>(options =>
        {
            options.UseHashRouting()
                .AddShard("shard-1", _fixture.Shard1ConnectionString)
                .AddShard("shard-2", _fixture.Shard2ConnectionString)
                .AddShard("shard-3", _fixture.Shard3ConnectionString);
        });

        services.AddEncinaMongoDBSharding<ShardedTestEntity, string>(options =>
        {
            options.UseNativeSharding = false;
            options.CollectionName = "sharded_entities";
            options.IdProperty = e => e.Id;
            options.DatabaseName = "encina_shard1";
        });

        _serviceProvider = services.BuildServiceProvider();
    }

    public ValueTask DisposeAsync()
    {
        _serviceProvider?.Dispose();
        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task DataIsolation_EntitiesOnlyExistInRoutedShard()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IFunctionalShardedRepository<ShardedTestEntity, string>>();

        var entity = new ShardedTestEntity
        {
            Id = Guid.NewGuid().ToString(),
            ShardKey = "isolation-test",
            Name = "Isolated Entity"
        };

        // Determine which shard this entity goes to
        var shardResult = repo.GetShardIdForEntity(entity);
        shardResult.IsRight.ShouldBeTrue();

        string targetShard = string.Empty;
        _ = shardResult.IfRight(s => targetShard = s);

        // Act -- add the entity
        await repo.AddAsync(entity);

        // Assert -- verify entity only exists in the target shard by checking each database directly
        var allShards = new[] { "shard-1", "shard-2", "shard-3" };
        foreach (var shardId in allShards)
        {
            var db = _fixture.GetDatabase(shardId);
            var collection = db.GetCollection<ShardedTestEntity>("sharded_entities");
            var found = await collection.Find(
                Builders<ShardedTestEntity>.Filter.Eq(e => e.Id, entity.Id))
                .FirstOrDefaultAsync();

            if (shardId == targetShard)
            {
                found.ShouldNotBeNull($"Entity should exist in shard {shardId}");
            }
            else
            {
                found.ShouldBeNull($"Entity should NOT exist in shard {shardId}");
            }
        }
    }

    [Fact]
    public async Task ConsistentRouting_SameKeyAlwaysRoutesToSameShard()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IFunctionalShardedRepository<ShardedTestEntity, string>>();

        // Act -- check routing 10 times with the same key
        var shardIds = new System.Collections.Generic.HashSet<string>();
        for (var i = 0; i < 10; i++)
        {
            var entity = new ShardedTestEntity { Id = $"id-{i}", ShardKey = "same-key", Name = $"Entity {i}" };
            var result = repo.GetShardIdForEntity(entity);
            _ = result.IfRight(s => shardIds.Add(s));
        }

        // Assert -- all routings should go to the same shard
        shardIds.Count.ShouldBe(1, "Same shard key should always route to the same shard");
    }

    [Fact]
    public async Task MultipleEntities_ShouldDistributeAcrossShards()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IFunctionalShardedRepository<ShardedTestEntity, string>>();

        // Act -- check routing for 100 unique keys
        var shardIds = new System.Collections.Generic.HashSet<string>();
        for (var i = 0; i < 100; i++)
        {
            var entity = new ShardedTestEntity { Id = $"dist-{i}", ShardKey = $"unique-key-{i}", Name = $"Entity {i}" };
            var result = repo.GetShardIdForEntity(entity);
            _ = result.IfRight(s => shardIds.Add(s));
        }

        // Assert -- entities should be distributed across multiple shards
        shardIds.Count.ShouldBeGreaterThan(1, "100 unique keys should distribute across multiple shards");
    }

    [Fact]
    public async Task AddAndRetrieve_AcrossMultipleShards_ShouldWork()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IFunctionalShardedRepository<ShardedTestEntity, string>>();

        var entities = Enumerable.Range(0, 15).Select(i => new ShardedTestEntity
        {
            Id = Guid.NewGuid().ToString(),
            ShardKey = $"customer-{i}",
            Name = $"Cross-Shard Entity {i}",
            Value = $"value-{i}"
        }).ToList();

        // Act -- add all entities
        foreach (var entity in entities)
        {
            var addResult = await repo.AddAsync(entity);
            addResult.IsRight.ShouldBeTrue($"Adding entity with key {entity.ShardKey} should succeed");
        }

        // Assert -- retrieve each from correct shard
        foreach (var entity in entities)
        {
            var getResult = await repo.GetByIdAsync(entity.Id, entity.ShardKey);
            getResult.IsRight.ShouldBeTrue($"Retrieving entity {entity.Id} should succeed");
        }
    }
}
