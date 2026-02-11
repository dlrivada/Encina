using Encina.ADO.PostgreSQL.Sharding;
using Encina.Dapper.PostgreSQL.Sharding;
using Encina.Sharding;
using Encina.TestInfrastructure.Fixtures.Sharding;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

using Xunit;

namespace Encina.IntegrationTests.Sharding.Dapper.PostgreSQL;

/// <summary>
/// Integration tests for sharded repository CRUD operations using Dapper with PostgreSQL.
/// </summary>
[Collection("Sharding-Dapper-PostgreSQL")]
[Trait("Category", "Integration")]
[Trait("Database", "PostgreSQL")]
public sealed class ShardedRepositoryCrudTests : IAsyncLifetime
{
    private readonly ShardedPostgreSqlFixture _fixture;
    private ServiceProvider _serviceProvider = null!;
    private IFunctionalShardedRepository<ShardedTestEntity, string> _repository = null!;

    public ShardedRepositoryCrudTests(ShardedPostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync()
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

        services.AddEncinaDapperSharding<ShardedTestEntity, string>(mapping =>
        {
            mapping.ToTable("sharded_entities")
                .HasId(e => e.Id, "id")
                .MapProperty(e => e.ShardKey, "shard_key")
                .MapProperty(e => e.Name, "name")
                .MapProperty(e => e.Value, "value")
                .MapProperty(e => e.CreatedAtUtc, "created_at_utc");
        });

        // ADO registration provides IShardedConnectionFactory required by Dapper
        services.AddEncinaADOSharding<ShardedTestEntity, string>(mapping =>
        {
            mapping.ToTable("sharded_entities")
                .HasId(e => e.Id, "id")
                .MapProperty(e => e.ShardKey, "shard_key")
                .MapProperty(e => e.Name, "name")
                .MapProperty(e => e.Value, "value")
                .MapProperty(e => e.CreatedAtUtc, "created_at_utc");
        });

        _serviceProvider = services.BuildServiceProvider();

        using var scope = _serviceProvider.CreateScope();
        _repository = scope.ServiceProvider.GetRequiredService<IFunctionalShardedRepository<ShardedTestEntity, string>>();
    }

    public ValueTask DisposeAsync()
    {
        _serviceProvider?.Dispose();
        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task AddAsync_ShouldPersistEntityToCorrectShard()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IFunctionalShardedRepository<ShardedTestEntity, string>>();

        var entity = new ShardedTestEntity
        {
            Id = Guid.NewGuid().ToString(),
            ShardKey = "customer-1",
            Name = "Test Entity 1",
            Value = "some-value"
        };

        // Act
        var result = await repo.AddAsync(entity);

        // Assert
        result.IsRight.ShouldBeTrue("AddAsync should succeed");
    }

    [Fact]
    public async Task GetByIdAsync_AfterAdd_ShouldReturnEntity()
    {
        // Arrange
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

        // Act
        var result = await repo.GetByIdAsync(entity.Id, entity.ShardKey);

        // Assert
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
        // Arrange
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

        // Act
        entity.Name = "Updated Name";
        entity.Value = "updated-value";
        var updateResult = await repo.UpdateAsync(entity);

        // Assert
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
        // Arrange
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

        // Act
        var deleteResult = await repo.DeleteAsync(entity.Id, entity.ShardKey);

        // Assert
        deleteResult.IsRight.ShouldBeTrue("DeleteAsync should succeed");

        var getResult = await repo.GetByIdAsync(entity.Id, entity.ShardKey);
        getResult.IsLeft.ShouldBeTrue("Entity should not be found after deletion");
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentEntity_ShouldReturnLeft()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IFunctionalShardedRepository<ShardedTestEntity, string>>();

        // Act
        var result = await repo.GetByIdAsync("non-existent-id", "some-key");

        // Assert
        result.IsLeft.ShouldBeTrue("Should return Left for non-existent entity");
    }

    [Fact]
    public async Task GetShardIdForEntity_ShouldReturnValidShardId()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IFunctionalShardedRepository<ShardedTestEntity, string>>();

        var entity = new ShardedTestEntity
        {
            Id = "test-id",
            ShardKey = "customer-5",
            Name = "Test"
        };

        // Act
        var result = repo.GetShardIdForEntity(entity);

        // Assert
        result.IsRight.ShouldBeTrue("Should resolve shard for entity");
        _ = result.IfRight(shardId =>
        {
            shardId.ShouldBeOneOf("shard-1", "shard-2", "shard-3");
        });
    }

    [Fact]
    public async Task AddAsync_SameKeyAlwaysGoesToSameShard()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IFunctionalShardedRepository<ShardedTestEntity, string>>();

        var entity1 = new ShardedTestEntity
        {
            Id = Guid.NewGuid().ToString(),
            ShardKey = "consistent-key",
            Name = "Entity 1"
        };

        var entity2 = new ShardedTestEntity
        {
            Id = Guid.NewGuid().ToString(),
            ShardKey = "consistent-key",
            Name = "Entity 2"
        };

        // Act
        var shard1 = repo.GetShardIdForEntity(entity1);
        var shard2 = repo.GetShardIdForEntity(entity2);

        // Assert — same shard key routes to same shard
        shard1.IsRight.ShouldBeTrue();
        shard2.IsRight.ShouldBeTrue();

        string shardId1 = string.Empty, shardId2 = string.Empty;
        _ = shard1.IfRight(s => shardId1 = s);
        _ = shard2.IfRight(s => shardId2 = s);

        shardId1.ShouldBe(shardId2, "Same shard key should route to same shard");
    }
}
