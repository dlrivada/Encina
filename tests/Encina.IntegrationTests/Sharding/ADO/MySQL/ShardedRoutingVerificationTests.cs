using Encina.ADO.MySQL.Sharding;
using Encina.Sharding;
using Encina.TestInfrastructure.Fixtures.Sharding;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

using Xunit;

namespace Encina.IntegrationTests.Sharding.ADO.MySQL;

/// <summary>
/// Integration tests verifying routing consistency and data isolation using ADO.NET with MySQL.
/// </summary>
[Collection("Sharding-ADO-MySQL")]
[Trait("Category", "Integration")]
[Trait("Database", "MySQL")]
public sealed class ShardedRoutingVerificationTests : IAsyncLifetime
{
    private readonly ShardedMySqlFixture _fixture;
    private ServiceProvider _serviceProvider = null!;

    public ShardedRoutingVerificationTests(ShardedMySqlFixture fixture)
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

        services.AddEncinaADOSharding<ShardedTestEntity, string>(mapping =>
        {
            mapping.ToTable("ShardedEntities")
                .HasId(e => e.Id)
                .MapProperty(e => e.ShardKey, "ShardKey")
                .MapProperty(e => e.Name, "Name")
                .MapProperty(e => e.Value, "Value")
                .MapProperty(e => e.CreatedAtUtc, "CreatedAtUtc");
        });

        _serviceProvider = services.BuildServiceProvider();
    }

    public Task DisposeAsync()
    {
        _serviceProvider?.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task ConsistentRouting_SameKeyAlwaysRoutesToSameShard()
    {
        using var scope = _serviceProvider.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IFunctionalShardedRepository<ShardedTestEntity, string>>();

        var shardIds = new System.Collections.Generic.HashSet<string>();
        for (var i = 0; i < 10; i++)
        {
            var entity = new ShardedTestEntity { Id = $"id-{i}", ShardKey = "same-key", Name = $"Entity {i}" };
            var result = repo.GetShardIdForEntity(entity);
            _ = result.IfRight(s => shardIds.Add(s));
        }

        shardIds.Count.ShouldBe(1, "Same shard key should always route to the same shard");
    }

    [Fact]
    public async Task MultipleEntities_ShouldDistributeAcrossShards()
    {
        using var scope = _serviceProvider.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IFunctionalShardedRepository<ShardedTestEntity, string>>();

        var shardIds = new System.Collections.Generic.HashSet<string>();
        for (var i = 0; i < 100; i++)
        {
            var entity = new ShardedTestEntity { Id = $"dist-{i}", ShardKey = $"unique-key-{i}", Name = $"Entity {i}" };
            var result = repo.GetShardIdForEntity(entity);
            _ = result.IfRight(s => shardIds.Add(s));
        }

        shardIds.Count.ShouldBeGreaterThan(1, "100 unique keys should distribute across multiple shards");
    }

    [Fact]
    public async Task AddAndRetrieve_AcrossMultipleShards_ShouldWork()
    {
        using var scope = _serviceProvider.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IFunctionalShardedRepository<ShardedTestEntity, string>>();

        var entities = Enumerable.Range(0, 15).Select(i => new ShardedTestEntity
        {
            Id = Guid.NewGuid().ToString(),
            ShardKey = $"customer-{i}",
            Name = $"Cross-Shard Entity {i}",
            Value = $"value-{i}"
        }).ToList();

        foreach (var entity in entities)
        {
            var addResult = await repo.AddAsync(entity);
            addResult.IsRight.ShouldBeTrue($"Adding entity with key {entity.ShardKey} should succeed");
        }

        foreach (var entity in entities)
        {
            var getResult = await repo.GetByIdAsync(entity.Id, entity.ShardKey);
            getResult.IsRight.ShouldBeTrue($"Retrieving entity {entity.Id} should succeed");
        }
    }
}
