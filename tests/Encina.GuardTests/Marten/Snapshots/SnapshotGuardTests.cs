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

    public class TestSnapAgg : global::Encina.DomainModeling.AggregateBase, ISnapshotable<TestSnapAgg>
    {
        protected override void Apply(object domainEvent) { }
    }
}
