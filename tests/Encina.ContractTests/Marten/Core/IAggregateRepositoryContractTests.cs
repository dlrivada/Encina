using Encina.Marten;
using Encina.Marten.Projections;
using Encina.Marten.Snapshots;
using Encina.Marten.Versioning;

using Shouldly;

namespace Encina.ContractTests.Marten.Core;

/// <summary>
/// Contract tests verifying that Marten implementations conform to interface contracts.
/// These tests verify structural compliance, not behavioral — behavioral tests are in unit/integration.
/// </summary>
[Trait("Category", "Contract")]
[Trait("Provider", "Marten")]
public sealed class MartenContractTests
{
    [Fact]
    public void MartenAggregateRepository_ImplementsIAggregateRepository()
    {
        typeof(MartenAggregateRepository<>).GetInterfaces()
            .ShouldContain(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IAggregateRepository<>));
    }

    [Fact]
    public void MartenReadModelRepository_ImplementsIReadModelRepository()
    {
        typeof(MartenReadModelRepository<>).GetInterfaces()
            .ShouldContain(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IReadModelRepository<>));
    }

    [Fact]
    public void MartenProjectionManager_ImplementsIProjectionManager()
    {
        typeof(MartenProjectionManager).GetInterfaces()
            .ShouldContain(typeof(IProjectionManager));
    }

    [Fact]
    public void MartenSnapshotStore_ImplementsISnapshotStore()
    {
        typeof(MartenSnapshotStore<>).GetInterfaces()
            .ShouldContain(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ISnapshotStore<>));
    }

    [Fact]
    public void SnapshotAwareAggregateRepository_ImplementsIAggregateRepository()
    {
        typeof(SnapshotAwareAggregateRepository<>).GetInterfaces()
            .ShouldContain(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IAggregateRepository<>));
    }

    [Fact]
    public void MartenInlineProjectionDispatcher_ImplementsIInlineProjectionDispatcher()
    {
        typeof(MartenInlineProjectionDispatcher).GetInterfaces()
            .ShouldContain(typeof(IInlineProjectionDispatcher));
    }

    [Fact]
    public void EventUpcasterRegistry_AllMethods_ArePublic()
    {
        var type = typeof(EventUpcasterRegistry);
        type.GetMethod("Register", [typeof(IEventUpcaster)]).ShouldNotBeNull();
        type.GetMethod("Register", [typeof(Type), typeof(Func<Type, IEventUpcaster>)]).ShouldNotBeNull();
        type.GetMethod("TryRegister").ShouldNotBeNull();
        type.GetMethod("GetUpcasterForEventType").ShouldNotBeNull();
        type.GetMethod("HasUpcasterFor").ShouldNotBeNull();
        type.GetMethod("ScanAndRegister").ShouldNotBeNull();
    }

    [Fact]
    public void IAggregateRepository_HasRequiredMethods()
    {
        var type = typeof(IAggregateRepository<>);
        var methods = type.GetMethods();
        methods.ShouldContain(m => m.Name == "LoadAsync");
        methods.ShouldContain(m => m.Name == "SaveAsync");
        methods.ShouldContain(m => m.Name == "CreateAsync");
    }

    [Fact]
    public void IReadModelRepository_HasRequiredMethods()
    {
        var type = typeof(IReadModelRepository<>);
        var methods = type.GetMethods();
        methods.ShouldContain(m => m.Name == "GetByIdAsync");
        methods.ShouldContain(m => m.Name == "GetByIdsAsync");
        methods.ShouldContain(m => m.Name == "StoreAsync");
        methods.ShouldContain(m => m.Name == "DeleteAsync");
        methods.ShouldContain(m => m.Name == "QueryAsync");
    }

    [Fact]
    public void IProjectionManager_HasRequiredMethods()
    {
        var type = typeof(IProjectionManager);
        var methods = type.GetMethods();
        methods.ShouldContain(m => m.Name == "RebuildAsync");
        methods.ShouldContain(m => m.Name == "GetStatusAsync");
        methods.ShouldContain(m => m.Name == "GetAllStatusesAsync");
        methods.ShouldContain(m => m.Name == "StartAsync");
        methods.ShouldContain(m => m.Name == "StopAsync");
    }

    [Fact]
    public void ISnapshotStore_HasRequiredMethods()
    {
        var type = typeof(ISnapshotStore<>);
        var methods = type.GetMethods();
        methods.ShouldContain(m => m.Name == "SaveAsync");
        methods.ShouldContain(m => m.Name == "GetLatestAsync");
        methods.ShouldContain(m => m.Name == "DeleteAllAsync");
    }

    #region Behavioral Contract — MartenAggregateRepository

    [Fact]
    public async Task AggregateRepository_SaveAsync_NullAggregate_ThrowsArgumentNull()
    {
        // Contract: SaveAsync must reject null aggregate
        var session = NSubstitute.Substitute.For<global::Marten.IDocumentSession>();
        var repo = new MartenAggregateRepository<ContractTestAggregate>(
            session,
            NSubstitute.Substitute.For<IRequestContext>(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<MartenAggregateRepository<ContractTestAggregate>>.Instance,
            Microsoft.Extensions.Options.Options.Create(new EncinaMartenOptions()));

        await Shouldly.Should.ThrowAsync<ArgumentNullException>(async () =>
            await repo.SaveAsync(null!));
    }

    [Fact]
    public async Task AggregateRepository_CreateAsync_NullAggregate_ThrowsArgumentNull()
    {
        var session = NSubstitute.Substitute.For<global::Marten.IDocumentSession>();
        var repo = new MartenAggregateRepository<ContractTestAggregate>(
            session,
            NSubstitute.Substitute.For<IRequestContext>(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<MartenAggregateRepository<ContractTestAggregate>>.Instance,
            Microsoft.Extensions.Options.Options.Create(new EncinaMartenOptions()));

        await Shouldly.Should.ThrowAsync<ArgumentNullException>(async () =>
            await repo.CreateAsync(null!));
    }

    [Fact]
    public async Task AggregateRepository_CreateAsync_NoEvents_ReturnsLeft()
    {
        // Contract: creating aggregate without events must return error
        var session = NSubstitute.Substitute.For<global::Marten.IDocumentSession>();
        var repo = new MartenAggregateRepository<ContractTestAggregate>(
            session,
            NSubstitute.Substitute.For<IRequestContext>(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<MartenAggregateRepository<ContractTestAggregate>>.Instance,
            Microsoft.Extensions.Options.Options.Create(new EncinaMartenOptions()));

        var aggregate = new ContractTestAggregate(); // no events raised
        var result = await repo.CreateAsync(aggregate);

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task AggregateRepository_SaveAsync_NoUncommittedEvents_ReturnsRight()
    {
        // Contract: saving aggregate without changes is a no-op success
        var session = NSubstitute.Substitute.For<global::Marten.IDocumentSession>();
        var repo = new MartenAggregateRepository<ContractTestAggregate>(
            session,
            NSubstitute.Substitute.For<IRequestContext>(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<MartenAggregateRepository<ContractTestAggregate>>.Instance,
            Microsoft.Extensions.Options.Options.Create(new EncinaMartenOptions()));

        var aggregate = new ContractTestAggregate();
        var result = await repo.SaveAsync(aggregate);

        result.IsRight.ShouldBeTrue();
    }

    #endregion

    #region Behavioral Contract — MartenReadModelRepository

    [Fact]
    public async Task ReadModelRepository_StoreAsync_NullModel_ThrowsArgumentNull()
    {
        var session = NSubstitute.Substitute.For<global::Marten.IDocumentSession>();
        var repo = new MartenReadModelRepository<ContractTestReadModel>(
            session,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<MartenReadModelRepository<ContractTestReadModel>>.Instance);

        await Shouldly.Should.ThrowAsync<ArgumentNullException>(async () =>
            await repo.StoreAsync(null!));
    }

    [Fact]
    public async Task ReadModelRepository_GetByIdsAsync_NullIds_ThrowsArgumentNull()
    {
        var session = NSubstitute.Substitute.For<global::Marten.IDocumentSession>();
        var repo = new MartenReadModelRepository<ContractTestReadModel>(
            session,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<MartenReadModelRepository<ContractTestReadModel>>.Instance);

        await Shouldly.Should.ThrowAsync<ArgumentNullException>(async () =>
            await repo.GetByIdsAsync(null!));
    }

    [Fact]
    public async Task ReadModelRepository_QueryAsync_NullPredicate_ThrowsArgumentNull()
    {
        var session = NSubstitute.Substitute.For<global::Marten.IDocumentSession>();
        var repo = new MartenReadModelRepository<ContractTestReadModel>(
            session,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<MartenReadModelRepository<ContractTestReadModel>>.Instance);

        await Shouldly.Should.ThrowAsync<ArgumentNullException>(async () =>
            await repo.QueryAsync(null!));
    }

    #endregion

    public sealed class ContractTestAggregate : global::Encina.DomainModeling.AggregateBase
    {
        protected override void Apply(object domainEvent) { }
    }

    public sealed class ContractTestReadModel : IReadModel
    {
        public Guid Id { get; set; }
    }
}
