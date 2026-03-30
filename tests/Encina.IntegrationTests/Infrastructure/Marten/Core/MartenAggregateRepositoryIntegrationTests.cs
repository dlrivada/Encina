using Encina.IntegrationTests.Infrastructure.Marten.Fixtures;
using Encina.IntegrationTests.Infrastructure.Marten.Snapshots;
using Encina.Marten;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;

namespace Encina.IntegrationTests.Infrastructure.Marten.Core;

/// <summary>
/// Integration tests for <see cref="MartenAggregateRepository{TAggregate}"/> against a real PostgreSQL database.
/// </summary>
[Collection(MartenCollection.Name)]
[Trait("Category", "Integration")]
[Trait("Database", "PostgreSQL")]
public sealed class MartenAggregateRepositoryIntegrationTests : IAsyncLifetime
{
    private readonly MartenFixture _fixture;

    public MartenAggregateRepositoryIntegrationTests(MartenFixture fixture)
    {
        _fixture = fixture;
    }

    public ValueTask InitializeAsync()
    {
        Assert.SkipUnless(_fixture.IsAvailable, "Marten PostgreSQL container not available");
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static MartenAggregateRepository<TestSnapshotableAggregate> CreateRepo(global::Marten.IDocumentSession session)
    {
        return new MartenAggregateRepository<TestSnapshotableAggregate>(
            session,
            Substitute.For<IRequestContext>(),
            NullLogger<MartenAggregateRepository<TestSnapshotableAggregate>>.Instance,
            Microsoft.Extensions.Options.Options.Create(new EncinaMartenOptions()));
    }

    [Fact]
    public async Task CreateAsync_NewAggregate_PersistsEvents()
    {
        await using var session = _fixture.Store!.LightweightSession();
        var repo = CreateRepo(session);

        var aggregate = new TestSnapshotableAggregate(Guid.NewGuid(), "Test Order");
        aggregate.AddItem(50m);

        var result = await repo.CreateAsync(aggregate);

        result.IsRight.ShouldBeTrue($"CreateAsync should succeed: {result}");
    }

    [Fact]
    public async Task CreateAsync_ThenLoad_ReturnsAggregate()
    {
        await using var session = _fixture.Store!.LightweightSession();
        var repo = CreateRepo(session);

        var id = Guid.NewGuid();
        var aggregate = new TestSnapshotableAggregate(id, "LoadTest");
        aggregate.AddItem(100m);
        aggregate.AddItem(200m);

        await repo.CreateAsync(aggregate);

        // Load from a fresh session
        await using var loadSession = _fixture.Store.LightweightSession();
        var loadRepo = CreateRepo(loadSession);

        var loaded = await loadRepo.LoadAsync(id);

        loaded.IsRight.ShouldBeTrue($"LoadAsync should succeed: {loaded}");
        loaded.IfRight(agg =>
        {
            agg.Name.ShouldBe("LoadTest");
            agg.Total.ShouldBe(300m);
            agg.ItemCount.ShouldBe(2);
        });
    }

    [Fact]
    public async Task SaveAsync_ExistingAggregate_AppendsEvents()
    {
        await using var session = _fixture.Store!.LightweightSession();
        var repo = CreateRepo(session);

        var id = Guid.NewGuid();
        var aggregate = new TestSnapshotableAggregate(id, "SaveTest");
        await repo.CreateAsync(aggregate);

        // Load and modify
        await using var updateSession = _fixture.Store.LightweightSession();
        var updateRepo = CreateRepo(updateSession);
        var loadResult = await updateRepo.LoadAsync(id);

        loadResult.IsRight.ShouldBeTrue();
        loadResult.IfRight(async agg =>
        {
            agg.AddItem(500m);
            var saveResult = await updateRepo.SaveAsync(agg);
            saveResult.IsRight.ShouldBeTrue();
        });
    }

    [Fact]
    public async Task LoadAsync_NonExistentId_ReturnsLeft()
    {
        await using var session = _fixture.Store!.LightweightSession();
        var repo = CreateRepo(session);

        var result = await repo.LoadAsync(Guid.NewGuid());

        result.IsLeft.ShouldBeTrue("Loading non-existent aggregate should return Left");
    }

    [Fact]
    public async Task LoadAsync_AtVersion_ReturnsAggregateAtThatVersion()
    {
        await using var session = _fixture.Store!.LightweightSession();
        var repo = CreateRepo(session);

        var id = Guid.NewGuid();
        var aggregate = new TestSnapshotableAggregate(id, "VersionTest");
        aggregate.AddItem(10m);  // event 2
        aggregate.AddItem(20m);  // event 3
        aggregate.AddItem(30m);  // event 4

        await repo.CreateAsync(aggregate);

        // Load at version 2 (only Created + first AddItem)
        await using var loadSession = _fixture.Store.LightweightSession();
        var loadRepo = CreateRepo(loadSession);
        var result = await loadRepo.LoadAsync(id, 2);

        result.IsRight.ShouldBeTrue();
        result.IfRight(agg =>
        {
            agg.Name.ShouldBe("VersionTest");
            agg.Total.ShouldBe(10m);
            agg.ItemCount.ShouldBe(1);
        });
    }

    [Fact]
    public async Task CreateAsync_MultipleAggregates_AllPersist()
    {
        for (int i = 0; i < 3; i++)
        {
            await using var session = _fixture.Store!.LightweightSession();
            var repo = CreateRepo(session);

            var aggregate = new TestSnapshotableAggregate(Guid.NewGuid(), $"Multi-{i}");
            var result = await repo.CreateAsync(aggregate);
            result.IsRight.ShouldBeTrue($"Aggregate {i} should persist");
        }
    }
}
