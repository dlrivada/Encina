using Encina.IdGeneration;
using Encina.IdGeneration.Generators;
using Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.IdGeneration;
using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.SqlServer.IdGeneration;

/// <summary>
/// EF Core integration tests for ID generation value converters with SQL Server.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Provider", "EFCore.SqlServer")]
[Collection("EFCore-SqlServer")]
public sealed class IdGenerationConverterSqlServerTests : IAsyncLifetime
{
    private readonly EFCoreSqlServerFixture _fixture;

    public IdGenerationConverterSqlServerTests(EFCoreSqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync()
    {
        if (!_fixture.IsAvailable) return;

        await using var context = CreateDbContext();
        await context.Database.EnsureCreatedAsync();
        context.IdGenerationEntities.RemoveRange(context.IdGenerationEntities);
        await context.SaveChangesAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private IdGenerationDbContext CreateDbContext() => _fixture.CreateDbContext<IdGenerationDbContext>();

    [Fact]
    public async Task SnowflakeId_PersistAndRetrieve_RoundtripsCorrectly()
    {
        Assert.SkipUnless(_fixture.IsAvailable, "SQL Server container not available");

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
        Assert.SkipUnless(_fixture.IsAvailable, "SQL Server container not available");

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
        Assert.SkipUnless(_fixture.IsAvailable, "SQL Server container not available");

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
        Assert.SkipUnless(_fixture.IsAvailable, "SQL Server container not available");

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
}
