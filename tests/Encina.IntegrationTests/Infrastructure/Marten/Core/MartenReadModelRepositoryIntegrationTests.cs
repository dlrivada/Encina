using Encina.IntegrationTests.Infrastructure.Marten.Fixtures;
using Encina.Marten.Projections;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace Encina.IntegrationTests.Infrastructure.Marten.Core;

/// <summary>
/// Integration tests for <see cref="MartenReadModelRepository{TReadModel}"/> against real PostgreSQL.
/// </summary>
[Collection(MartenCollection.Name)]
[Trait("Category", "Integration")]
[Trait("Database", "PostgreSQL")]
public sealed class MartenReadModelRepositoryIntegrationTests : IAsyncLifetime
{
    private readonly MartenFixture _fixture;

    public MartenReadModelRepositoryIntegrationTests(MartenFixture fixture)
    {
        _fixture = fixture;
    }

    public ValueTask InitializeAsync()
    {
        Assert.SkipUnless(_fixture.IsAvailable, "Marten PostgreSQL container not available");
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task StoreAsync_ThenGetById_ReturnsReadModel()
    {
        // Use same session — Marten commits on SaveChangesAsync within StoreAsync
        await using var session = _fixture.Store!.LightweightSession();
        var repo = new MartenReadModelRepository<RmTestDoc>(
            session, NullLogger<MartenReadModelRepository<RmTestDoc>>.Instance);

        var id = Guid.NewGuid();
        var model = new RmTestDoc { Id = id, Name = "Test", Value = 42 };

        var storeResult = await repo.StoreAsync(model);
        storeResult.IsRight.ShouldBeTrue($"StoreAsync should succeed: {storeResult}");

        // Read from a new session to verify persistence
        await using var readSession = _fixture.Store.LightweightSession();
        var readRepo = new MartenReadModelRepository<RmTestDoc>(
            readSession, NullLogger<MartenReadModelRepository<RmTestDoc>>.Instance);

        var result = await readRepo.GetByIdAsync(id);
        result.IsRight.ShouldBeTrue($"GetByIdAsync should find the model: {result}");
        result.IfRight(m =>
        {
            m.Name.ShouldBe("Test");
            m.Value.ShouldBe(42);
        });
    }

    [Fact]
    public async Task StoreManyAsync_ThenGetByIds_ReturnsAll()
    {
        await using var session = _fixture.Store!.LightweightSession();
        var repo = new MartenReadModelRepository<RmTestDoc>(
            session, NullLogger<MartenReadModelRepository<RmTestDoc>>.Instance);

        var models = Enumerable.Range(1, 3).Select(i => new RmTestDoc
        {
            Id = Guid.NewGuid(), Name = $"Item-{i}", Value = i * 10
        }).ToList();

        await repo.StoreManyAsync(models);

        await using var readSession = _fixture.Store.LightweightSession();
        var readRepo = new MartenReadModelRepository<RmTestDoc>(
            readSession, NullLogger<MartenReadModelRepository<RmTestDoc>>.Instance);

        var result = await readRepo.GetByIdsAsync(models.Select(m => m.Id));
        result.IsRight.ShouldBeTrue();
        result.IfRight(list => list.Count.ShouldBe(3));
    }

    [Fact]
    public async Task DeleteAsync_RemovesReadModel()
    {
        await using var session = _fixture.Store!.LightweightSession();
        var repo = new MartenReadModelRepository<RmTestDoc>(
            session, NullLogger<MartenReadModelRepository<RmTestDoc>>.Instance);

        var id = Guid.NewGuid();
        await repo.StoreAsync(new RmTestDoc { Id = id, Name = "ToDelete", Value = 0 });

        await repo.DeleteAsync(id);

        await using var readSession = _fixture.Store.LightweightSession();
        var readRepo = new MartenReadModelRepository<RmTestDoc>(
            readSession, NullLogger<MartenReadModelRepository<RmTestDoc>>.Instance);

        var result = await readRepo.GetByIdAsync(id);
        result.IsLeft.ShouldBeTrue("Deleted read model should not be found");
    }

    [Fact]
    public async Task QueryAsync_WithPredicate_FiltersResults()
    {
        await using var session = _fixture.Store!.LightweightSession();
        var repo = new MartenReadModelRepository<RmTestDoc>(
            session, NullLogger<MartenReadModelRepository<RmTestDoc>>.Instance);

        var prefix = Guid.NewGuid().ToString()[..8];
        await repo.StoreAsync(new RmTestDoc { Id = Guid.NewGuid(), Name = $"{prefix}-A", Value = 100 });
        await repo.StoreAsync(new RmTestDoc { Id = Guid.NewGuid(), Name = $"{prefix}-B", Value = 200 });
        await repo.StoreAsync(new RmTestDoc { Id = Guid.NewGuid(), Name = "Other", Value = 300 });

        await using var readSession = _fixture.Store.LightweightSession();
        var readRepo = new MartenReadModelRepository<RmTestDoc>(
            readSession, NullLogger<MartenReadModelRepository<RmTestDoc>>.Instance);

        var result = await readRepo.QueryAsync(q => q.Where(m => m.Name.StartsWith(prefix)));
        result.IsRight.ShouldBeTrue();
        result.IfRight(list => list.Count.ShouldBe(2));
    }

    [Fact]
    public async Task ExistsAsync_ExistingId_ReturnsTrue()
    {
        await using var session = _fixture.Store!.LightweightSession();
        var repo = new MartenReadModelRepository<RmTestDoc>(
            session, NullLogger<MartenReadModelRepository<RmTestDoc>>.Instance);

        var id = Guid.NewGuid();
        await repo.StoreAsync(new RmTestDoc { Id = id, Name = "Exists", Value = 1 });

        await using var readSession = _fixture.Store.LightweightSession();
        var readRepo = new MartenReadModelRepository<RmTestDoc>(
            readSession, NullLogger<MartenReadModelRepository<RmTestDoc>>.Instance);

        var result = await readRepo.ExistsAsync(id);
        result.IsRight.ShouldBeTrue();
        result.IfRight(exists => exists.ShouldBeTrue());
    }

    [Fact]
    public async Task CountAsync_ReturnsCorrectCount()
    {
        var prefix = Guid.NewGuid().ToString()[..8];

        await using var session = _fixture.Store!.LightweightSession();
        var repo = new MartenReadModelRepository<RmTestDoc>(
            session, NullLogger<MartenReadModelRepository<RmTestDoc>>.Instance);

        await repo.StoreAsync(new RmTestDoc { Id = Guid.NewGuid(), Name = $"{prefix}-1", Value = 1 });
        await repo.StoreAsync(new RmTestDoc { Id = Guid.NewGuid(), Name = $"{prefix}-2", Value = 2 });

        await using var readSession = _fixture.Store.LightweightSession();
        var readRepo = new MartenReadModelRepository<RmTestDoc>(
            readSession, NullLogger<MartenReadModelRepository<RmTestDoc>>.Instance);

        var result = await readRepo.CountAsync(q => q.Where(m => m.Name.StartsWith(prefix)));
        result.IsRight.ShouldBeTrue();
        result.IfRight(count => count.ShouldBe(2));
    }

}

/// <summary>Short name to avoid PostgreSQL identifier length limit.</summary>
public sealed class RmTestDoc : IReadModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public int Value { get; set; }
}
