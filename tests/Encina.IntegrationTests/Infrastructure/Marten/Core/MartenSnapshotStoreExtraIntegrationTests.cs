using Encina.IntegrationTests.Infrastructure.Marten.Fixtures;
using Encina.IntegrationTests.Infrastructure.Marten.Snapshots;
using Encina.Marten.Snapshots;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace Encina.IntegrationTests.Infrastructure.Marten.Core;

/// <summary>
/// Additional integration tests for MartenSnapshotStore covering Prune, Exists, Count, DeleteAll.
/// </summary>
[Collection(MartenCollection.Name)]
[Trait("Category", "Integration")]
[Trait("Database", "PostgreSQL")]
public sealed class MartenSnapshotStoreExtraIntegrationTests : IAsyncLifetime
{
    private readonly MartenFixture _fixture;

    public MartenSnapshotStoreExtraIntegrationTests(MartenFixture fixture)
    {
        _fixture = fixture;
    }

    public ValueTask InitializeAsync()
    {
        Assert.SkipUnless(_fixture.IsAvailable, "Marten PostgreSQL container not available");
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static MartenSnapshotStore<TestSnapshotableAggregate> CreateStore(global::Marten.IDocumentSession session)
    {
        return new MartenSnapshotStore<TestSnapshotableAggregate>(
            session, Substitute.For<ILogger<MartenSnapshotStore<TestSnapshotableAggregate>>>());
    }

    [Fact]
    public async Task ExistsAsync_AfterSave_ReturnsTrue()
    {
        await using var session = _fixture.Store!.LightweightSession();
        var store = CreateStore(session);
        var id = Guid.NewGuid();

        var aggregate = new TestSnapshotableAggregate(id, "Exists Test");
        await store.SaveAsync(new Snapshot<TestSnapshotableAggregate>(id, 1, aggregate, DateTime.UtcNow));

        var result = await store.ExistsAsync(id);
        result.IsRight.ShouldBeTrue();
        result.IfRight(exists => exists.ShouldBeTrue());
    }

    [Fact]
    public async Task ExistsAsync_NoSnapshot_ReturnsFalse()
    {
        await using var session = _fixture.Store!.LightweightSession();
        var store = CreateStore(session);

        var result = await store.ExistsAsync(Guid.NewGuid());
        result.IsRight.ShouldBeTrue();
        result.IfRight(exists => exists.ShouldBeFalse());
    }

    [Fact]
    public async Task CountAsync_MultipleSnapshots_ReturnsCount()
    {
        await using var session = _fixture.Store!.LightweightSession();
        var store = CreateStore(session);
        var id = Guid.NewGuid();

        var aggregate = new TestSnapshotableAggregate(id, "Count Test");
        for (int v = 1; v <= 3; v++)
        {
            aggregate.AddItem(v * 10m);
            await store.SaveAsync(new Snapshot<TestSnapshotableAggregate>(id, v, aggregate, DateTime.UtcNow));
        }

        var result = await store.CountAsync(id);
        result.IsRight.ShouldBeTrue();
        result.IfRight(count => count.ShouldBe(3));
    }

    [Fact]
    public async Task DeleteAllAsync_RemovesAllSnapshots()
    {
        await using var session = _fixture.Store!.LightweightSession();
        var store = CreateStore(session);
        var id = Guid.NewGuid();

        var aggregate = new TestSnapshotableAggregate(id, "Delete Test");
        await store.SaveAsync(new Snapshot<TestSnapshotableAggregate>(id, 1, aggregate, DateTime.UtcNow));
        await store.SaveAsync(new Snapshot<TestSnapshotableAggregate>(id, 2, aggregate, DateTime.UtcNow));

        await store.DeleteAllAsync(id);

        var exists = await store.ExistsAsync(id);
        exists.IsRight.ShouldBeTrue();
        exists.IfRight(e => e.ShouldBeFalse());
    }

    [Fact]
    public async Task PruneAsync_KeepsOnlyLatest()
    {
        await using var session = _fixture.Store!.LightweightSession();
        var store = CreateStore(session);
        var id = Guid.NewGuid();

        var aggregate = new TestSnapshotableAggregate(id, "Prune Test");
        for (int v = 1; v <= 5; v++)
        {
            aggregate.AddItem(v);
            await store.SaveAsync(new Snapshot<TestSnapshotableAggregate>(id, v, aggregate, DateTime.UtcNow));
        }

        await store.PruneAsync(id, 2);

        var count = await store.CountAsync(id);
        count.IsRight.ShouldBeTrue();
        count.IfRight(c => c.ShouldBeLessThanOrEqualTo(2));
    }
}
