using Encina.Marten;
using Encina.Marten.Snapshots;
using Marten;
using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.GuardTests.Marten.Snapshots;

/// <summary>
/// Guard tests for <see cref="SnapshotAwareAggregateRepository{TAggregate}"/>
/// covering constructor and method-level null/invalid argument validation.
/// </summary>
public class SnapshotAwareAggregateRepositoryGuardTests
{
    private static readonly IDocumentSession Session = Substitute.For<IDocumentSession>();
    private static readonly ISnapshotStore<TestSnapshotableAggregate> SnapshotStore = Substitute.For<ISnapshotStore<TestSnapshotableAggregate>>();
    private static readonly IRequestContext RequestContext = Substitute.For<IRequestContext>();
    private static readonly ILogger<SnapshotAwareAggregateRepository<TestSnapshotableAggregate>> Logger
        = NullLogger<SnapshotAwareAggregateRepository<TestSnapshotableAggregate>>.Instance;
    private static readonly IOptions<EncinaMartenOptions> Options = Microsoft.Extensions.Options.Options.Create(new EncinaMartenOptions());

    #region Constructor Guards

    [Fact]
    public void Constructor_NullSession_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new SnapshotAwareAggregateRepository<TestSnapshotableAggregate>(
                null!, SnapshotStore, RequestContext, Logger, Options));

    [Fact]
    public void Constructor_NullSnapshotStore_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new SnapshotAwareAggregateRepository<TestSnapshotableAggregate>(
                Session, null!, RequestContext, Logger, Options));

    [Fact]
    public void Constructor_NullRequestContext_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new SnapshotAwareAggregateRepository<TestSnapshotableAggregate>(
                Session, SnapshotStore, null!, Logger, Options));

    [Fact]
    public void Constructor_NullLogger_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new SnapshotAwareAggregateRepository<TestSnapshotableAggregate>(
                Session, SnapshotStore, RequestContext, null!, Options));

    [Fact]
    public void Constructor_NullOptions_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new SnapshotAwareAggregateRepository<TestSnapshotableAggregate>(
                Session, SnapshotStore, RequestContext, Logger, null!));

    [Fact]
    public void Constructor_NullEnrichers_DoesNotThrow()
    {
        // Null enrichers is acceptable (defaults to empty)
        var repo = new SnapshotAwareAggregateRepository<TestSnapshotableAggregate>(
            Session, SnapshotStore, RequestContext, Logger, Options, enrichers: null);
        repo.ShouldNotBeNull();
    }

    [Fact]
    public void Constructor_NullTimeProvider_DoesNotThrow()
    {
        var repo = new SnapshotAwareAggregateRepository<TestSnapshotableAggregate>(
            Session, SnapshotStore, RequestContext, Logger, Options, timeProvider: null);
        repo.ShouldNotBeNull();
    }

    #endregion

    #region SaveAsync Guards

    [Fact]
    public async Task SaveAsync_NullAggregate_Throws()
    {
        var repo = new SnapshotAwareAggregateRepository<TestSnapshotableAggregate>(
            Session, SnapshotStore, RequestContext, Logger, Options);
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await repo.SaveAsync(null!));
    }

    #endregion

    #region CreateAsync Guards

    [Fact]
    public async Task CreateAsync_NullAggregate_Throws()
    {
        var repo = new SnapshotAwareAggregateRepository<TestSnapshotableAggregate>(
            Session, SnapshotStore, RequestContext, Logger, Options);
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await repo.CreateAsync(null!));
    }

    [Fact]
    public async Task CreateAsync_AggregateWithNoEvents_ReturnsLeft()
    {
        var repo = new SnapshotAwareAggregateRepository<TestSnapshotableAggregate>(
            Session, SnapshotStore, RequestContext, Logger, Options);
        var aggregate = new TestSnapshotableAggregate();

        var result = await repo.CreateAsync(aggregate);
        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region SaveAsync Behavioral Guards

    [Fact]
    public async Task SaveAsync_AggregateWithNoUncommittedEvents_ReturnsRight()
    {
        var repo = new SnapshotAwareAggregateRepository<TestSnapshotableAggregate>(
            Session, SnapshotStore, RequestContext, Logger, Options);
        var aggregate = new TestSnapshotableAggregate();

        var result = await repo.SaveAsync(aggregate);
        result.IsRight.ShouldBeTrue();
    }

    #endregion

    public class TestSnapshotableAggregate : global::Encina.DomainModeling.AggregateBase,
        ISnapshotable<TestSnapshotableAggregate>
    {
        protected override void Apply(object domainEvent) { }

        public Snapshot<TestSnapshotableAggregate> CreateSnapshot()
            => new(Id, Version, this, DateTime.UtcNow);

        public static TestSnapshotableAggregate RestoreFromSnapshot(
            Snapshot<TestSnapshotableAggregate> snapshot)
            => snapshot.State;
    }
}
