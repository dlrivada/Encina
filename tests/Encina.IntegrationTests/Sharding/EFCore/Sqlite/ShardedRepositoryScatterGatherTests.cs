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
/// Integration tests for sharded repository scatter-gather operations using EF Core with SQLite.
/// </summary>
[Collection("Sharding-EFCore-Sqlite")]
[Trait("Category", "Integration")]
[Trait("Database", "SQLite")]
public sealed class ShardedRepositoryScatterGatherTests : IAsyncLifetime
{
    private readonly ShardedSqliteFixture _fixture;
    private ServiceProvider _serviceProvider = null!;

    public ShardedRepositoryScatterGatherTests(ShardedSqliteFixture fixture)
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
        var contextFactory = scope.ServiceProvider.GetRequiredService<IShardedDbContextFactory<ShardedTestDbContext>>();

        // Act
        var result = await repo.QueryAllShardsAsync(
            async (shardId, ct) =>
            {
                var ctxResult = contextFactory.CreateContextForShard(shardId);

                return await ctxResult.MatchAsync(
                    RightAsync: async ctx =>
                    {
                        await using (ctx)
                        {
                            var entities = await ctx.ShardedEntities.ToListAsync(ct);
                            return Either<EncinaError, IReadOnlyList<ShardedTestEntity>>.Right(entities);
                        }
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
        // Arrange
        await _fixture.ClearAllDataAsync();

        using var scope = _serviceProvider.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IFunctionalShardedRepository<ShardedTestEntity, string>>();
        var contextFactory = scope.ServiceProvider.GetRequiredService<IShardedDbContextFactory<ShardedTestDbContext>>();

        // Act
        var result = await repo.QueryAllShardsAsync(
            async (shardId, ct) =>
            {
                var ctxResult = contextFactory.CreateContextForShard(shardId);

                return await ctxResult.MatchAsync(
                    RightAsync: async ctx =>
                    {
                        await using (ctx)
                        {
                            var entities = await ctx.ShardedEntities.ToListAsync(ct);
                            return Either<EncinaError, IReadOnlyList<ShardedTestEntity>>.Right(entities);
                        }
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
