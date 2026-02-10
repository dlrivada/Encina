using Encina.ADO.MySQL.Sharding;
using Encina.Sharding;
using Encina.TestInfrastructure.Fixtures.Sharding;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

using Xunit;

namespace Encina.IntegrationTests.Sharding.ADO.MySQL;

/// <summary>
/// Integration tests for sharded repository CRUD operations using ADO.NET with MySQL.
/// </summary>
[Collection("Sharding-ADO-MySQL")]
[Trait("Category", "Integration")]
[Trait("Database", "MySQL")]
public sealed class ShardedRepositoryCrudTests : IAsyncLifetime
{
    private readonly ShardedMySqlFixture _fixture;
    private ServiceProvider _serviceProvider = null!;

    public ShardedRepositoryCrudTests(ShardedMySqlFixture fixture)
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
    public async Task AddAsync_ShouldPersistEntityToCorrectShard()
    {
        using var scope = _serviceProvider.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IFunctionalShardedRepository<ShardedTestEntity, string>>();

        var entity = new ShardedTestEntity
        {
            Id = Guid.NewGuid().ToString(),
            ShardKey = "customer-1",
            Name = "Test Entity 1",
            Value = "some-value"
        };

        var result = await repo.AddAsync(entity);

        result.IsRight.ShouldBeTrue("AddAsync should succeed");
    }

    [Fact]
    public async Task GetByIdAsync_AfterAdd_ShouldReturnEntity()
    {
        using var scope = _serviceProvider.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IFunctionalShardedRepository<ShardedTestEntity, string>>();

        var entity = new ShardedTestEntity
        {
            Id = Guid.NewGuid().ToString(),
            ShardKey = "customer-2",
            Name = "Test Entity 2",
            Value = "another-value"
        };

        await repo.AddAsync(entity);

        var result = await repo.GetByIdAsync(entity.Id, entity.ShardKey);

        result.IsRight.ShouldBeTrue("GetByIdAsync should succeed");
        _ = result.IfRight(retrieved =>
        {
            retrieved.Id.ShouldBe(entity.Id);
            retrieved.Name.ShouldBe(entity.Name);
            retrieved.Value.ShouldBe(entity.Value);
        });
    }

    [Fact]
    public async Task UpdateAsync_ShouldModifyExistingEntity()
    {
        using var scope = _serviceProvider.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IFunctionalShardedRepository<ShardedTestEntity, string>>();

        var entity = new ShardedTestEntity
        {
            Id = Guid.NewGuid().ToString(),
            ShardKey = "customer-3",
            Name = "Original Name",
            Value = "original-value"
        };

        await repo.AddAsync(entity);

        entity.Name = "Updated Name";
        entity.Value = "updated-value";
        var updateResult = await repo.UpdateAsync(entity);

        updateResult.IsRight.ShouldBeTrue("UpdateAsync should succeed");

        var getResult = await repo.GetByIdAsync(entity.Id, entity.ShardKey);
        _ = getResult.IfRight(retrieved =>
        {
            retrieved.Name.ShouldBe("Updated Name");
            retrieved.Value.ShouldBe("updated-value");
        });
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveEntity()
    {
        using var scope = _serviceProvider.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IFunctionalShardedRepository<ShardedTestEntity, string>>();

        var entity = new ShardedTestEntity
        {
            Id = Guid.NewGuid().ToString(),
            ShardKey = "customer-4",
            Name = "To Be Deleted",
            Value = "delete-me"
        };

        await repo.AddAsync(entity);

        var deleteResult = await repo.DeleteAsync(entity.Id, entity.ShardKey);

        deleteResult.IsRight.ShouldBeTrue("DeleteAsync should succeed");

        var getResult = await repo.GetByIdAsync(entity.Id, entity.ShardKey);
        getResult.IsLeft.ShouldBeTrue("Entity should not be found after deletion");
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentEntity_ShouldReturnLeft()
    {
        using var scope = _serviceProvider.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IFunctionalShardedRepository<ShardedTestEntity, string>>();

        var result = await repo.GetByIdAsync("non-existent-id", "some-key");

        result.IsLeft.ShouldBeTrue("Should return Left for non-existent entity");
    }

    [Fact]
    public async Task GetShardIdForEntity_ShouldReturnValidShardId()
    {
        using var scope = _serviceProvider.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IFunctionalShardedRepository<ShardedTestEntity, string>>();

        var entity = new ShardedTestEntity
        {
            Id = "test-id",
            ShardKey = "customer-5",
            Name = "Test"
        };

        var result = repo.GetShardIdForEntity(entity);

        result.IsRight.ShouldBeTrue("Should resolve shard for entity");
        _ = result.IfRight(shardId =>
        {
            shardId.ShouldBeOneOf("shard-1", "shard-2", "shard-3");
        });
    }

    [Fact]
    public async Task AddAsync_SameKeyAlwaysGoesToSameShard()
    {
        using var scope = _serviceProvider.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IFunctionalShardedRepository<ShardedTestEntity, string>>();

        var entity1 = new ShardedTestEntity { Id = Guid.NewGuid().ToString(), ShardKey = "consistent-key", Name = "Entity 1" };
        var entity2 = new ShardedTestEntity { Id = Guid.NewGuid().ToString(), ShardKey = "consistent-key", Name = "Entity 2" };

        var shard1 = repo.GetShardIdForEntity(entity1);
        var shard2 = repo.GetShardIdForEntity(entity2);

        shard1.IsRight.ShouldBeTrue();
        shard2.IsRight.ShouldBeTrue();

        string shardId1 = string.Empty, shardId2 = string.Empty;
        _ = shard1.IfRight(s => shardId1 = s);
        _ = shard2.IfRight(s => shardId2 = s);

        shardId1.ShouldBe(shardId2, "Same shard key should route to same shard");
    }
}
