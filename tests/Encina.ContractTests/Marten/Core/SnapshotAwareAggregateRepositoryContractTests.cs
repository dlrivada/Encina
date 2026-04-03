using Encina.Marten;
using Encina.Marten.Snapshots;
using Marten;
using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.ContractTests.Marten.Core;

/// <summary>
/// Behavioral contract tests for <see cref="SnapshotAwareAggregateRepository{TAggregate}"/>
/// verifying IAggregateRepository contract compliance when snapshot-awareness is enabled.
/// </summary>
[Trait("Category", "Contract")]
[Trait("Provider", "Marten")]
public sealed class SnapshotAwareAggregateRepositoryContractTests
{
    #region Structural Contracts

    [Fact]
    public void SnapshotAwareAggregateRepository_ImplementsIAggregateRepository()
    {
        typeof(SnapshotAwareAggregateRepository<>).GetInterfaces()
            .ShouldContain(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IAggregateRepository<>));
    }

    [Fact]
    public void SnapshotAwareAggregateRepository_IsSealed()
    {
        typeof(SnapshotAwareAggregateRepository<>).IsSealed.ShouldBeTrue();
    }

    #endregion

    #region SaveAsync Contract

    [Fact]
    public async Task SaveAsync_NullAggregate_ThrowsArgumentNull()
    {
        // Contract: SaveAsync must reject null aggregate
        var repo = CreateRepository();
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await repo.SaveAsync(null!));
    }

    [Fact]
    public async Task SaveAsync_AggregateWithNoUncommittedEvents_ReturnsRight()
    {
        // Contract: saving aggregate without uncommitted events is a no-op success
        var repo = CreateRepository();
        var aggregate = new ContractTestSnapshotableAggregate();

        var result = await repo.SaveAsync(aggregate);
        result.IsRight.ShouldBeTrue();
    }

    #endregion

    #region CreateAsync Contract

    [Fact]
    public async Task CreateAsync_NullAggregate_ThrowsArgumentNull()
    {
        var repo = CreateRepository();
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await repo.CreateAsync(null!));
    }

    [Fact]
    public async Task CreateAsync_AggregateWithNoEvents_ReturnsLeft()
    {
        // Contract: creating aggregate without events must return error
        var repo = CreateRepository();
        var aggregate = new ContractTestSnapshotableAggregate();

        var result = await repo.CreateAsync(aggregate);
        result.IsLeft.ShouldBeTrue();
        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: error => error.Message.ShouldContain("without any events"));
    }

    #endregion

    #region ISnapshotStore Contract

    [Fact]
    public void ISnapshotStore_HasGetAtVersionAsync()
    {
        var type = typeof(ISnapshotStore<>);
        var methods = type.GetMethods();
        methods.ShouldContain(m => m.Name == "GetAtVersionAsync");
    }

    [Fact]
    public void ISnapshotStore_HasPruneAsync()
    {
        var type = typeof(ISnapshotStore<>);
        var methods = type.GetMethods();
        methods.ShouldContain(m => m.Name == "PruneAsync");
    }

    #endregion

    #region Helpers

    private static SnapshotAwareAggregateRepository<ContractTestSnapshotableAggregate> CreateRepository()
    {
        return new SnapshotAwareAggregateRepository<ContractTestSnapshotableAggregate>(
            NSubstitute.Substitute.For<IDocumentSession>(),
            NSubstitute.Substitute.For<ISnapshotStore<ContractTestSnapshotableAggregate>>(),
            NSubstitute.Substitute.For<IRequestContext>(),
            NullLogger<SnapshotAwareAggregateRepository<ContractTestSnapshotableAggregate>>.Instance,
            Microsoft.Extensions.Options.Options.Create(new EncinaMartenOptions()));
    }

    #endregion

    public sealed class ContractTestSnapshotableAggregate : global::Encina.DomainModeling.AggregateBase,
        ISnapshotable<ContractTestSnapshotableAggregate>
    {
        protected override void Apply(object domainEvent) { }

        public Snapshot<ContractTestSnapshotableAggregate> CreateSnapshot()
            => new(Id, Version, this, DateTime.UtcNow);

        public static ContractTestSnapshotableAggregate RestoreFromSnapshot(
            Snapshot<ContractTestSnapshotableAggregate> snapshot)
            => snapshot.State;
    }
}
