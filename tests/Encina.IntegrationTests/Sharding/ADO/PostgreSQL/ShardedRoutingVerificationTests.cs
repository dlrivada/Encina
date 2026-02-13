using Encina.ADO.PostgreSQL.Sharding;
using Encina.Sharding;
using Encina.TestInfrastructure.Fixtures.Sharding;

using Microsoft.Extensions.DependencyInjection;

using Npgsql;

using Shouldly;

using Xunit;

namespace Encina.IntegrationTests.Sharding.ADO.PostgreSQL;

/// <summary>
/// Integration tests verifying routing consistency and data isolation using ADO.NET with PostgreSQL.
/// </summary>
[Collection("Sharding-ADO-PostgreSQL")]
[Trait("Category", "Integration")]
[Trait("Database", "PostgreSQL")]
public sealed class ShardedRoutingVerificationTests : IAsyncLifetime
{
    private readonly ShardedPostgreSqlFixture _fixture;
    private ServiceProvider _serviceProvider = null!;

    public ShardedRoutingVerificationTests(ShardedPostgreSqlFixture fixture)
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
    }

    public ValueTask DisposeAsync()
    {
        _serviceProvider?.Dispose();
        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task DataIsolation_EntityOnlyExistsInRoutedShard()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IFunctionalShardedRepository<ShardedTestEntity, string>>();

        var entity = new ShardedTestEntity
        {
            Id = Guid.NewGuid().ToString(),
            ShardKey = "isolation-test-key",
            Name = "Isolated Entity"
        };

        // Determine which shard this entity goes to
        var shardResult = repo.GetShardIdForEntity(entity);
        shardResult.IsRight.ShouldBeTrue();

        string targetShard = string.Empty;
        _ = shardResult.IfRight(s => targetShard = s);

        // Act — add the entity
        await repo.AddAsync(entity);

        // Assert — verify entity exists only in the target shard
        var allShards = new[] { "shard-1", "shard-2", "shard-3" };
        foreach (var shardId in allShards)
        {
            var count = await CountEntitiesInShardAsync(shardId, entity.Id);
            if (shardId == targetShard)
            {
                count.ShouldBe(1, $"Entity should exist in target shard {shardId}");
            }
            else
            {
                count.ShouldBe(0, $"Entity should NOT exist in non-target shard {shardId}");
            }
        }
    }

    [Fact]
    public async Task ConsistentRouting_SameKeyAlwaysRoutesToSameShard()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IFunctionalShardedRepository<ShardedTestEntity, string>>();

        var shardKey = "deterministic-key";

        // Act — check routing 10 times
        var shardIds = new System.Collections.Generic.HashSet<string>();
        for (var i = 0; i < 10; i++)
        {
            var entity = new ShardedTestEntity
            {
                Id = $"routing-{i}",
                ShardKey = shardKey,
                Name = $"Entity {i}"
            };

            var result = repo.GetShardIdForEntity(entity);
            result.IsRight.ShouldBeTrue();
            _ = result.IfRight(s => shardIds.Add(s));
        }

        // Assert — all routings should go to the same shard
        shardIds.Count.ShouldBe(1, "Same shard key should always route to the same shard");
    }

    [Fact]
    public async Task MultipleEntities_DistributedAcrossShards()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IFunctionalShardedRepository<ShardedTestEntity, string>>();

        // Act — add many entities with different shard keys
        var shardDistribution = new Dictionary<string, int>();
        for (var i = 0; i < 30; i++)
        {
            var entity = new ShardedTestEntity
            {
                Id = $"dist-{i}",
                ShardKey = $"unique-key-{i}",
                Name = $"Distributed Entity {i}"
            };

            var shardResult = repo.GetShardIdForEntity(entity);
            shardResult.IsRight.ShouldBeTrue();

            string shardId = string.Empty;
            _ = shardResult.IfRight(s => shardId = s);

            if (!shardDistribution.TryGetValue(shardId, out var count))
            {
                count = 0;
            }

            shardDistribution[shardId] = count + 1;

            await repo.AddAsync(entity);
        }

        // Assert — entities should be distributed (hash routing distributes reasonably)
        shardDistribution.Count.ShouldBeGreaterThan(1, "Entities should be distributed across multiple shards");
    }

    [Fact]
    public async Task AddAndRetrieve_AcrossMultipleShards()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IFunctionalShardedRepository<ShardedTestEntity, string>>();

        var entities = Enumerable.Range(0, 5).Select(i => new ShardedTestEntity
        {
            Id = $"multi-{i}",
            ShardKey = $"multi-key-{i}",
            Name = $"Multi Entity {i}",
            Value = $"value-{i}"
        }).ToList();

        // Act — add all entities
        foreach (var entity in entities)
        {
            var addResult = await repo.AddAsync(entity);
            addResult.IsRight.ShouldBeTrue($"Failed to add entity {entity.Id}");
        }

        // Assert — retrieve each from correct shard
        foreach (var entity in entities)
        {
            var getResult = await repo.GetByIdAsync(entity.Id, entity.ShardKey);
            getResult.IsRight.ShouldBeTrue($"Failed to retrieve entity {entity.Id}");
            _ = getResult.IfRight(retrieved =>
            {
                retrieved.Name.ShouldBe(entity.Name);
            });
        }
    }

    private async Task<int> CountEntitiesInShardAsync(string shardId, string entityId)
    {
        using var connection = (NpgsqlConnection)_fixture.CreateConnection(shardId);
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sharded_entities WHERE id = @Id";
        command.Parameters.AddWithValue("@Id", entityId);
        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result, System.Globalization.CultureInfo.InvariantCulture);
    }
}
