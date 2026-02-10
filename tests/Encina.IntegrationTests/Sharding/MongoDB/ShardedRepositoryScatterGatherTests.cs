using Encina.MongoDB.Sharding;
using Encina.Sharding;
using Encina.TestInfrastructure.Fixtures.Sharding;

using LanguageExt;

using Microsoft.Extensions.DependencyInjection;

using MongoDB.Driver;

using Shouldly;

using Xunit;

namespace Encina.IntegrationTests.Sharding.MongoDB;

/// <summary>
/// Integration tests for sharded repository scatter-gather operations using MongoDB.
/// </summary>
[Collection("Sharding-MongoDB")]
[Trait("Category", "Integration")]
[Trait("Database", "MongoDB")]
public sealed class ShardedRepositoryScatterGatherTests : IAsyncLifetime
{
    private readonly ShardedMongoDbFixture _fixture;
    private ServiceProvider _serviceProvider = null!;

    public ShardedRepositoryScatterGatherTests(ShardedMongoDbFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
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

        // Seed data across shards
        await SeedTestDataAsync();
    }

    public Task DisposeAsync()
    {
        _serviceProvider?.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task QueryAllShardsAsync_ShouldReturnResultsFromAllShards()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IFunctionalShardedRepository<ShardedTestEntity, string>>();
        var collectionFactory = scope.ServiceProvider.GetRequiredService<IShardedMongoCollectionFactory>();

        // Act
        var result = await repo.QueryAllShardsAsync(
            async (shardId, ct) =>
            {
                var collResult = collectionFactory.GetCollectionForShard<ShardedTestEntity>(shardId, "sharded_entities");

                return await collResult.MatchAsync(
                    RightAsync: async collection =>
                    {
                        var entities = await collection.Find(FilterDefinition<ShardedTestEntity>.Empty)
                            .ToListAsync(ct);
                        return Either<EncinaError, IReadOnlyList<ShardedTestEntity>>.Right(entities);
                    },
                    Left: error => Either<EncinaError, IReadOnlyList<ShardedTestEntity>>.Left(error));
            },
            CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue("QueryAllShardsAsync should succeed");
        _ = result.IfRight(queryResult =>
        {
            queryResult.Results.Count.ShouldBeGreaterThan(0, "Should have results from seeded data");
            queryResult.IsComplete.ShouldBeTrue("All shards should succeed");
        });
    }

    [Fact]
    public async Task QueryAllShardsAsync_EmptyShards_ShouldReturnEmptyResults()
    {
        // Arrange -- clear all data first
        await _fixture.ClearAllDataAsync();

        using var scope = _serviceProvider.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IFunctionalShardedRepository<ShardedTestEntity, string>>();
        var collectionFactory = scope.ServiceProvider.GetRequiredService<IShardedMongoCollectionFactory>();

        // Act
        var result = await repo.QueryAllShardsAsync(
            async (shardId, ct) =>
            {
                var collResult = collectionFactory.GetCollectionForShard<ShardedTestEntity>(shardId, "sharded_entities");

                return await collResult.MatchAsync(
                    RightAsync: async collection =>
                    {
                        var entities = await collection.Find(FilterDefinition<ShardedTestEntity>.Empty)
                            .ToListAsync(ct);
                        return Either<EncinaError, IReadOnlyList<ShardedTestEntity>>.Right(entities);
                    },
                    Left: error => Either<EncinaError, IReadOnlyList<ShardedTestEntity>>.Left(error));
            },
            CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue("QueryAllShardsAsync on empty shards should succeed");
        _ = result.IfRight(queryResult =>
        {
            queryResult.Results.Count.ShouldBe(0, "Should have no results");
            queryResult.IsComplete.ShouldBeTrue("All shards should succeed even with no data");
        });
    }

    private async Task SeedTestDataAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IFunctionalShardedRepository<ShardedTestEntity, string>>();

        // Seed 10 entities with different shard keys to distribute across shards
        for (var i = 0; i < 10; i++)
        {
            var entity = new ShardedTestEntity
            {
                Id = $"seed-{i}",
                ShardKey = $"customer-{i}",
                Name = $"Seeded Entity {i}",
                Value = $"value-{i}"
            };

            await repo.AddAsync(entity);
        }
    }
}
