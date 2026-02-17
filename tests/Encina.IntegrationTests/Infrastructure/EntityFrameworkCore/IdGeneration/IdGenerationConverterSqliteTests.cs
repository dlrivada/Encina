using Encina.IdGeneration;
using Encina.IdGeneration.Generators;
using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.IdGeneration;

/// <summary>
/// EF Core integration tests for ID generation value converters with SQLite.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Provider", "EFCore.Sqlite")]
[Collection("EFCore-Sqlite")]
public sealed class IdGenerationConverterSqliteTests : IAsyncLifetime
{
    private readonly EFCoreSqliteFixture _fixture;

    public IdGenerationConverterSqliteTests(EFCoreSqliteFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync()
    {
        await using var context = CreateDbContext();

        // Use raw SQL instead of EnsureCreatedAsync() because the shared
        // SQLite in-memory DB already has tables from other tests, and
        // EnsureCreated skips creation if any tables exist.
        await context.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS IdGenerationEntities (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                SnowflakeCol INTEGER NULL,
                UlidCol TEXT NULL,
                UuidV7Col TEXT NULL,
                ShardPrefixedCol TEXT NULL
            )
            """);
        await context.Database.ExecuteSqlRawAsync("DELETE FROM IdGenerationEntities");
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private IdGenerationDbContext CreateDbContext() => _fixture.CreateDbContext<IdGenerationDbContext>();

    [Fact]
    public async Task SnowflakeId_PersistAndRetrieve_RoundtripsCorrectly()
    {
        var original = new SnowflakeId(123456789L);

        await using (var context = CreateDbContext())
        {
            context.IdGenerationEntities.Add(new IdGenerationEntity { SnowflakeCol = original });
            await context.SaveChangesAsync();
        }

        await using (var context = CreateDbContext())
        {
            var entity = await context.IdGenerationEntities
                .OrderByDescending(e => e.Id)
                .FirstAsync();
            entity.SnowflakeCol!.Value.ShouldBe(original);
        }
    }

    [Fact]
    public async Task UlidId_PersistAndRetrieve_RoundtripsCorrectly()
    {
        var original = UlidId.NewUlid();

        await using (var context = CreateDbContext())
        {
            context.IdGenerationEntities.Add(new IdGenerationEntity { UlidCol = original });
            await context.SaveChangesAsync();
        }

        await using (var context = CreateDbContext())
        {
            var entity = await context.IdGenerationEntities
                .OrderByDescending(e => e.Id)
                .FirstAsync();
            entity.UlidCol!.Value.ShouldBe(original);
        }
    }

    [Fact]
    public async Task UuidV7Id_PersistAndRetrieve_RoundtripsCorrectly()
    {
        var original = new UuidV7IdGenerator().Generate().Match(id => id, _ => default);

        await using (var context = CreateDbContext())
        {
            context.IdGenerationEntities.Add(new IdGenerationEntity { UuidV7Col = original });
            await context.SaveChangesAsync();
        }

        await using (var context = CreateDbContext())
        {
            var entity = await context.IdGenerationEntities
                .OrderByDescending(e => e.Id)
                .FirstAsync();
            entity.UuidV7Col!.Value.Value.ShouldBe(original.Value);
        }
    }

    [Fact]
    public async Task ShardPrefixedId_PersistAndRetrieve_RoundtripsCorrectly()
    {
        var original = ShardPrefixedId.Parse($"shard-01:{UlidId.NewUlid()}");

        await using (var context = CreateDbContext())
        {
            context.IdGenerationEntities.Add(new IdGenerationEntity { ShardPrefixedCol = original });
            await context.SaveChangesAsync();
        }

        await using (var context = CreateDbContext())
        {
            var entity = await context.IdGenerationEntities
                .OrderByDescending(e => e.Id)
                .FirstAsync();
            entity.ShardPrefixedCol!.Value.ToString().ShouldBe(original.ToString());
        }
    }

    [Fact]
    public async Task AllIdTypes_InSameEntity_RoundtripCorrectly()
    {
        var snowflake = new SnowflakeId(999L);
        var ulid = UlidId.NewUlid();
        var uuid = new UuidV7IdGenerator().Generate().Match(id => id, _ => default);
        var shardPrefixed = ShardPrefixedId.Parse($"tenant-a:{ulid}");

        await using (var context = CreateDbContext())
        {
            context.IdGenerationEntities.Add(new IdGenerationEntity
            {
                SnowflakeCol = snowflake,
                UlidCol = ulid,
                UuidV7Col = uuid,
                ShardPrefixedCol = shardPrefixed
            });
            await context.SaveChangesAsync();
        }

        await using (var context = CreateDbContext())
        {
            var entity = await context.IdGenerationEntities
                .OrderByDescending(e => e.Id)
                .FirstAsync();

            entity.SnowflakeCol!.Value.ShouldBe(snowflake);
            entity.UlidCol!.Value.ShouldBe(ulid);
            entity.UuidV7Col!.Value.Value.ShouldBe(uuid.Value);
            entity.ShardPrefixedCol!.Value.ToString().ShouldBe(shardPrefixed.ToString());
        }
    }
}
