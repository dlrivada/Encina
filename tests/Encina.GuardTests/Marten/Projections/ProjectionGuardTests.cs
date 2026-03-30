using Encina.Marten;
using Encina.Marten.Projections;
using Marten;
using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.GuardTests.Marten.Projections;

public class ProjectionGuardTests
{
    #region ProjectionRegistry

    [Fact]
    public void ProjectionRegistry_Register_NullProjectionType_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new ProjectionRegistry().Register(null!, typeof(object)));

    [Fact]
    public void ProjectionRegistry_Register_NullReadModelType_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new ProjectionRegistry().Register(typeof(object), null!));

    [Fact]
    public void ProjectionRegistry_GetProjectionsForEvent_NullType_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new ProjectionRegistry().GetProjectionsForEvent(null!));

    [Fact]
    public void ProjectionRegistry_GetProjectionForReadModel_NullType_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new ProjectionRegistry().GetProjectionForReadModel(null!));

    #endregion

    #region MartenInlineProjectionDispatcher

    [Fact]
    public void MartenInlineProjectionDispatcher_NullSession_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new MartenInlineProjectionDispatcher(
                null!, Substitute.For<IServiceProvider>(),
                NullLogger<MartenInlineProjectionDispatcher>.Instance, new ProjectionRegistry()));

    [Fact]
    public void MartenInlineProjectionDispatcher_NullServiceProvider_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new MartenInlineProjectionDispatcher(
                Substitute.For<IDocumentSession>(), null!,
                NullLogger<MartenInlineProjectionDispatcher>.Instance, new ProjectionRegistry()));

    [Fact]
    public void MartenInlineProjectionDispatcher_NullLogger_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new MartenInlineProjectionDispatcher(
                Substitute.For<IDocumentSession>(), Substitute.For<IServiceProvider>(),
                null!, new ProjectionRegistry()));

    [Fact]
    public void MartenInlineProjectionDispatcher_NullRegistry_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new MartenInlineProjectionDispatcher(
                Substitute.For<IDocumentSession>(), Substitute.For<IServiceProvider>(),
                NullLogger<MartenInlineProjectionDispatcher>.Instance, null!));

    [Fact]
    public async Task MartenInlineProjectionDispatcher_DispatchAsync_NullEvent_Throws()
    {
        var dispatcher = new MartenInlineProjectionDispatcher(
            Substitute.For<IDocumentSession>(), Substitute.For<IServiceProvider>(),
            NullLogger<MartenInlineProjectionDispatcher>.Instance, new ProjectionRegistry());
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await dispatcher.DispatchAsync(null!, new ProjectionContext()));
    }

    [Fact]
    public async Task MartenInlineProjectionDispatcher_DispatchAsync_NullContext_Throws()
    {
        var dispatcher = new MartenInlineProjectionDispatcher(
            Substitute.For<IDocumentSession>(), Substitute.For<IServiceProvider>(),
            NullLogger<MartenInlineProjectionDispatcher>.Instance, new ProjectionRegistry());
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await dispatcher.DispatchAsync(new object(), null!));
    }

    [Fact]
    public async Task MartenInlineProjectionDispatcher_DispatchManyAsync_NullEvents_Throws()
    {
        var dispatcher = new MartenInlineProjectionDispatcher(
            Substitute.For<IDocumentSession>(), Substitute.For<IServiceProvider>(),
            NullLogger<MartenInlineProjectionDispatcher>.Instance, new ProjectionRegistry());
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await dispatcher.DispatchManyAsync(null!));
    }

    #endregion

    #region MartenProjectionManager

    [Fact]
    public void MartenProjectionManager_NullStore_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new MartenProjectionManager(
                null!, Substitute.For<IServiceProvider>(),
                NullLogger<MartenProjectionManager>.Instance, new ProjectionRegistry()));

    [Fact]
    public void MartenProjectionManager_NullServiceProvider_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new MartenProjectionManager(
                Substitute.For<IDocumentStore>(), null!,
                NullLogger<MartenProjectionManager>.Instance, new ProjectionRegistry()));

    [Fact]
    public void MartenProjectionManager_NullLogger_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new MartenProjectionManager(
                Substitute.For<IDocumentStore>(), Substitute.For<IServiceProvider>(),
                null!, new ProjectionRegistry()));

    [Fact]
    public void MartenProjectionManager_NullRegistry_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new MartenProjectionManager(
                Substitute.For<IDocumentStore>(), Substitute.For<IServiceProvider>(),
                NullLogger<MartenProjectionManager>.Instance, null!));

    #endregion

    #region MartenReadModelRepository

    [Fact]
    public void MartenReadModelRepository_NullSession_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new MartenReadModelRepository<TestReadModel>(
                null!, NullLogger<MartenReadModelRepository<TestReadModel>>.Instance));

    [Fact]
    public void MartenReadModelRepository_NullLogger_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new MartenReadModelRepository<TestReadModel>(
                Substitute.For<IDocumentSession>(), null!));

    [Fact]
    public async Task MartenReadModelRepository_GetByIdsAsync_NullIds_Throws()
    {
        var repo = new MartenReadModelRepository<TestReadModel>(
            Substitute.For<IDocumentSession>(), NullLogger<MartenReadModelRepository<TestReadModel>>.Instance);
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await repo.GetByIdsAsync(null!));
    }

    [Fact]
    public async Task MartenReadModelRepository_QueryAsync_NullPredicate_Throws()
    {
        var repo = new MartenReadModelRepository<TestReadModel>(
            Substitute.For<IDocumentSession>(), NullLogger<MartenReadModelRepository<TestReadModel>>.Instance);
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await repo.QueryAsync(null!));
    }

    [Fact]
    public async Task MartenReadModelRepository_StoreAsync_NullReadModel_Throws()
    {
        var repo = new MartenReadModelRepository<TestReadModel>(
            Substitute.For<IDocumentSession>(), NullLogger<MartenReadModelRepository<TestReadModel>>.Instance);
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await repo.StoreAsync(null!));
    }

    [Fact]
    public async Task MartenReadModelRepository_StoreManyAsync_NullModels_Throws()
    {
        var repo = new MartenReadModelRepository<TestReadModel>(
            Substitute.For<IDocumentSession>(), NullLogger<MartenReadModelRepository<TestReadModel>>.Instance);
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await repo.StoreManyAsync(null!));
    }

    [Fact]
    public async Task MartenReadModelRepository_CountAsync_NullPredicate_Throws()
    {
        var repo = new MartenReadModelRepository<TestReadModel>(
            Substitute.For<IDocumentSession>(), NullLogger<MartenReadModelRepository<TestReadModel>>.Instance);
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await repo.CountAsync(null!));
    }

    #endregion

    public class TestReadModel : global::Encina.Marten.Projections.IReadModel
    {
        public Guid Id { get; set; }
    }
}
