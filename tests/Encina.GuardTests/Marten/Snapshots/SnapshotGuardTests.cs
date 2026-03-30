using Encina.Marten;
using Encina.Marten.Snapshots;
using Marten;
using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.GuardTests.Marten.Snapshots;

public class SnapshotGuardTests
{
    #region MartenSnapshotStore

    [Fact]
    public void MartenSnapshotStore_NullSession_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new MartenSnapshotStore<TestSnapAgg>(null!, NullLogger<MartenSnapshotStore<TestSnapAgg>>.Instance));

    [Fact]
    public void MartenSnapshotStore_NullLogger_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new MartenSnapshotStore<TestSnapAgg>(Substitute.For<IDocumentSession>(), null!));

    [Fact]
    public async Task MartenSnapshotStore_SaveAsync_NullSnapshot_Throws()
    {
        var store = new MartenSnapshotStore<TestSnapAgg>(
            Substitute.For<IDocumentSession>(), NullLogger<MartenSnapshotStore<TestSnapAgg>>.Instance);
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await store.SaveAsync(null!));
    }

    [Fact]
    public async Task MartenSnapshotStore_PruneAsync_NegativeKeepCount_Throws()
    {
        var store = new MartenSnapshotStore<TestSnapAgg>(
            Substitute.For<IDocumentSession>(), NullLogger<MartenSnapshotStore<TestSnapAgg>>.Instance);
        await Should.ThrowAsync<ArgumentOutOfRangeException>(async () =>
            await store.PruneAsync(Guid.NewGuid(), -1));
    }

    #endregion

    #region SnapshotEnvelope

    [Fact]
    public void SnapshotEnvelope_Create_NullSnapshot_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            SnapshotEnvelope.Create<TestSnapAgg>(null!));

    #endregion

    #region Snapshot Constructor

    [Fact]
    public void Snapshot_NullState_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new Snapshot<TestSnapAgg>(Guid.NewGuid(), 1, null!, DateTime.UtcNow));

    #endregion

    #region SnapshotAwareAggregateRepository

    [Fact]
    public void SnapshotAwareRepo_NullSession_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new SnapshotAwareAggregateRepository<TestSnapAgg>(
                null!, Substitute.For<ISnapshotStore<TestSnapAgg>>(),
                Substitute.For<IRequestContext>(),
                NullLogger<SnapshotAwareAggregateRepository<TestSnapAgg>>.Instance,
                Options.Create(new EncinaMartenOptions())));

    [Fact]
    public void SnapshotAwareRepo_NullSnapshotStore_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new SnapshotAwareAggregateRepository<TestSnapAgg>(
                Substitute.For<IDocumentSession>(), null!,
                Substitute.For<IRequestContext>(),
                NullLogger<SnapshotAwareAggregateRepository<TestSnapAgg>>.Instance,
                Options.Create(new EncinaMartenOptions())));

    [Fact]
    public void SnapshotAwareRepo_NullRequestContext_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new SnapshotAwareAggregateRepository<TestSnapAgg>(
                Substitute.For<IDocumentSession>(), Substitute.For<ISnapshotStore<TestSnapAgg>>(),
                null!,
                NullLogger<SnapshotAwareAggregateRepository<TestSnapAgg>>.Instance,
                Options.Create(new EncinaMartenOptions())));

    [Fact]
    public void SnapshotAwareRepo_NullLogger_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new SnapshotAwareAggregateRepository<TestSnapAgg>(
                Substitute.For<IDocumentSession>(), Substitute.For<ISnapshotStore<TestSnapAgg>>(),
                Substitute.For<IRequestContext>(),
                null!, Options.Create(new EncinaMartenOptions())));

    [Fact]
    public void SnapshotAwareRepo_NullOptions_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new SnapshotAwareAggregateRepository<TestSnapAgg>(
                Substitute.For<IDocumentSession>(), Substitute.For<ISnapshotStore<TestSnapAgg>>(),
                Substitute.For<IRequestContext>(),
                NullLogger<SnapshotAwareAggregateRepository<TestSnapAgg>>.Instance, null!));

    [Fact]
    public async Task SnapshotAwareRepo_SaveAsync_NullAggregate_Throws()
    {
        var repo = new SnapshotAwareAggregateRepository<TestSnapAgg>(
            Substitute.For<IDocumentSession>(), Substitute.For<ISnapshotStore<TestSnapAgg>>(),
            Substitute.For<IRequestContext>(),
            NullLogger<SnapshotAwareAggregateRepository<TestSnapAgg>>.Instance,
            Options.Create(new EncinaMartenOptions()));
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await repo.SaveAsync(null!));
    }

    [Fact]
    public async Task SnapshotAwareRepo_CreateAsync_NullAggregate_Throws()
    {
        var repo = new SnapshotAwareAggregateRepository<TestSnapAgg>(
            Substitute.For<IDocumentSession>(), Substitute.For<ISnapshotStore<TestSnapAgg>>(),
            Substitute.For<IRequestContext>(),
            NullLogger<SnapshotAwareAggregateRepository<TestSnapAgg>>.Instance,
            Options.Create(new EncinaMartenOptions()));
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await repo.CreateAsync(null!));
    }

    #endregion

    public class TestSnapAgg : global::Encina.DomainModeling.AggregateBase, ISnapshotable<TestSnapAgg>
    {
        protected override void Apply(object domainEvent) { }
    }
}
