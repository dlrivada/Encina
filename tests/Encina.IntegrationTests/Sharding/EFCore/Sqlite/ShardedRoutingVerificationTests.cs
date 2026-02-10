using Encina.EntityFrameworkCore.Sharding;
using Encina.Sharding;
using Encina.Sharding.Data;
using Encina.TestInfrastructure.Fixtures.Sharding;

using LanguageExt;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Shouldly;

using Xunit;

namespace Encina.IntegrationTests.Sharding.EFCore.Sqlite;

/// <summary>
/// Integration tests verifying routing correctness using EF Core with SQLite.
/// </summary>
[Collection("Sharding-EFCore-Sqlite")]
[Trait("Category", "Integration")]
[Trait("Database", "SQLite")]
public sealed class ShardedRoutingVerificationTests : IAsyncLifetime
{
    private readonly ShardedSqliteFixture _fixture;
    private ServiceProvider _serviceProvider = null!;

    public ShardedRoutingVerificationTests(ShardedSqliteFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ClearAllDataAsync();

        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaSharding<ShardedTestEntity>(options =>
        {
            options.UseHashRouting()
                .AddShard("shard-1", _fixture.Shard1ConnectionString)
                .AddShard("shard-2", _fixture.Shard2ConnectionString)
                .AddShard("shard-3", _fixture.Shard3ConnectionString);
        });

        services.AddEncinaEFCoreShardingSqlite<ShardedTestDbContext, ShardedTestEntity, string>();

        _serviceProvider = services.BuildServiceProvider();
    }

    public Task DisposeAsync()
    {
        _serviceProvider?.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task DataIsolation_EntitiesOnlyExistInRoutedShard()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IFunctionalShardedRepository<ShardedTestEntity, string>>();
        var contextFactory = scope.ServiceProvider.GetRequiredService<IShardedDbContextFactory<ShardedTestDbContext>>();

        var entity = new ShardedTestEntity
        {
            Id = Guid.NewGuid().ToString(),
            ShardKey = "isolation-test",
            Name = "Isolated Entity"
        };

        await repo.AddAsync(entity);

        var shardResult = repo.GetShardIdForEntity(entity);
        shardResult.IsRight.ShouldBeTrue();

        string targetShard = string.Empty;
        _ = shardResult.IfRight(s => targetShard = s);

        // Act & Assert — query all shards and verify entity only exists in the target shard
        var allShards = new[] { "shard-1", "shard-2", "shard-3" };
        foreach (var shardId in allShards)
        {
            var ctxResult = contextFactory.CreateContextForShard(shardId);
            _ = await ctxResult.MatchAsync(
                RightAsync: async ctx =>
                {
                    await using (ctx)
                    {
                        var found = await ctx.ShardedEntities
                            .FirstOrDefaultAsync(e => e.Id == entity.Id);

                        if (shardId == targetShard)
                        {
                            found.ShouldNotBeNull($"Entity should exist in shard {shardId}");
                        }
                        else
                        {
                            found.ShouldBeNull($"Entity should NOT exist in shard {shardId}");
                        }
                    }

                    return LanguageExt.Unit.Default;
                },
                Left: _ => LanguageExt.Unit.Default);
        }
    }

    [Fact]
    public async Task ConsistentRouting_SameKeyAlwaysRoutesToSameShard()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IFunctionalShardedRepository<ShardedTestEntity, string>>();

        // Act
        var shardIds = new System.Collections.Generic.HashSet<string>();
        for (var i = 0; i < 10; i++)
        {
            var entity = new ShardedTestEntity { Id = $"id-{i}", ShardKey = "same-key", Name = $"Entity {i}" };
            var result = repo.GetShardIdForEntity(entity);
            _ = result.IfRight(s => shardIds.Add(s));
        }

        // Assert
        shardIds.Count.ShouldBe(1, "Same shard key should always route to the same shard");
    }

    [Fact]
    public async Task MultipleEntities_ShouldDistributeAcrossShards()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IFunctionalShardedRepository<ShardedTestEntity, string>>();

        // Act
        var shardIds = new System.Collections.Generic.HashSet<string>();
        for (var i = 0; i < 100; i++)
        {
            var entity = new ShardedTestEntity { Id = $"dist-{i}", ShardKey = $"unique-key-{i}", Name = $"Entity {i}" };
            var result = repo.GetShardIdForEntity(entity);
            _ = result.IfRight(s => shardIds.Add(s));
        }

        // Assert
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

        // Act — add all entities
        foreach (var entity in entities)
        {
            var addResult = await repo.AddAsync(entity);
            addResult.IsRight.ShouldBeTrue($"Adding entity with key {entity.ShardKey} should succeed");
        }

        // Assert — retrieve each from correct shard
        foreach (var entity in entities)
        {
            var getResult = await repo.GetByIdAsync(entity.Id, entity.ShardKey);
            getResult.IsRight.ShouldBeTrue($"Retrieving entity {entity.Id} should succeed");
        }
    }
}
