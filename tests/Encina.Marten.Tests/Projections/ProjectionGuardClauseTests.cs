using Encina.Marten.Projections;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace Encina.Marten.Tests.Projections;

/// <summary>
/// Guard clause tests for projection classes.
/// Verifies that all public methods properly validate their arguments.
/// </summary>
public sealed class ProjectionGuardClauseTests
{
    #region MartenReadModelRepository Guard Clauses

    [Fact]
    public void MartenReadModelRepository_NullSession_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = () => new MartenReadModelRepository<OrderSummary>(
            null!,
            Substitute.For<ILogger<MartenReadModelRepository<OrderSummary>>>());

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("session");
    }

    [Fact]
    public void MartenReadModelRepository_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = () => new MartenReadModelRepository<OrderSummary>(
            Substitute.For<IDocumentSession>(),
            null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
    }

    [Fact]
    public async Task MartenReadModelRepository_QueryAsync_NullPredicate_ThrowsArgumentNullException()
    {
        // Arrange
        var repo = CreateMockRepository();

        // Act & Assert
        Func<Task> act = () => repo.QueryAsync(null!);
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("predicate");
    }

    [Fact]
    public async Task MartenReadModelRepository_StoreAsync_NullReadModel_ThrowsArgumentNullException()
    {
        // Arrange
        var repo = CreateMockRepository();

        // Act & Assert
        Func<Task> act = () => repo.StoreAsync(null!);
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("readModel");
    }

    [Fact]
    public async Task MartenReadModelRepository_StoreManyAsync_NullReadModels_ThrowsArgumentNullException()
    {
        // Arrange
        var repo = CreateMockRepository();

        // Act & Assert
        Func<Task> act = () => repo.StoreManyAsync(null!);
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("readModels");
    }

    #endregion

    #region MartenProjectionManager Guard Clauses

    [Fact]
    public void MartenProjectionManager_NullStore_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = () => new MartenProjectionManager(
            null!,
            Substitute.For<IServiceProvider>(),
            Substitute.For<ILogger<MartenProjectionManager>>(),
            new ProjectionRegistry());

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("store");
    }

    [Fact]
    public void MartenProjectionManager_NullServiceProvider_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = () => new MartenProjectionManager(
            Substitute.For<IDocumentStore>(),
            null!,
            Substitute.For<ILogger<MartenProjectionManager>>(),
            new ProjectionRegistry());

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("serviceProvider");
    }

    [Fact]
    public void MartenProjectionManager_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = () => new MartenProjectionManager(
            Substitute.For<IDocumentStore>(),
            Substitute.For<IServiceProvider>(),
            null!,
            new ProjectionRegistry());

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
    }

    [Fact]
    public void MartenProjectionManager_NullRegistry_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = () => new MartenProjectionManager(
            Substitute.For<IDocumentStore>(),
            Substitute.For<IServiceProvider>(),
            Substitute.For<ILogger<MartenProjectionManager>>(),
            null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("registry");
    }

    #endregion

    #region MartenInlineProjectionDispatcher Guard Clauses

    [Fact]
    public void MartenInlineProjectionDispatcher_NullSession_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = () => new MartenInlineProjectionDispatcher(
            null!,
            Substitute.For<IServiceProvider>(),
            Substitute.For<ILogger<MartenInlineProjectionDispatcher>>(),
            new ProjectionRegistry());

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("session");
    }

    [Fact]
    public void MartenInlineProjectionDispatcher_NullServiceProvider_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = () => new MartenInlineProjectionDispatcher(
            Substitute.For<IDocumentSession>(),
            null!,
            Substitute.For<ILogger<MartenInlineProjectionDispatcher>>(),
            new ProjectionRegistry());

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("serviceProvider");
    }

    [Fact]
    public void MartenInlineProjectionDispatcher_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = () => new MartenInlineProjectionDispatcher(
            Substitute.For<IDocumentSession>(),
            Substitute.For<IServiceProvider>(),
            null!,
            new ProjectionRegistry());

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
    }

    [Fact]
    public void MartenInlineProjectionDispatcher_NullRegistry_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = () => new MartenInlineProjectionDispatcher(
            Substitute.For<IDocumentSession>(),
            Substitute.For<IServiceProvider>(),
            Substitute.For<ILogger<MartenInlineProjectionDispatcher>>(),
            null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("registry");
    }

    [Fact]
    public async Task MartenInlineProjectionDispatcher_DispatchAsync_NullEvent_ThrowsArgumentNullException()
    {
        // Arrange
        var dispatcher = CreateMockDispatcher();

        // Act & Assert
        Func<Task> act = () => dispatcher.DispatchAsync(null!, new ProjectionContext());
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("domainEvent");
    }

    [Fact]
    public async Task MartenInlineProjectionDispatcher_DispatchAsync_NullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var dispatcher = CreateMockDispatcher();

        // Act & Assert
        Func<Task> act = () => dispatcher.DispatchAsync(new OrderCreated("Test"), null!);
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("context");
    }

    [Fact]
    public async Task MartenInlineProjectionDispatcher_DispatchManyAsync_NullEvents_ThrowsArgumentNullException()
    {
        // Arrange
        var dispatcher = CreateMockDispatcher();

        // Act & Assert
        Func<Task> act = () => dispatcher.DispatchManyAsync(null!);
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("events");
    }

    #endregion

    #region ProjectionRegistry Guard Clauses

    [Fact]
    public void ProjectionRegistry_GetProjectionsForEvent_NullEventType_ThrowsArgumentNullException()
    {
        // Arrange
        var registry = new ProjectionRegistry();

        // Act
        var act = () => registry.GetProjectionsForEvent(null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("eventType");
    }

    #endregion

    #region Helper Methods

    private static MartenReadModelRepository<OrderSummary> CreateMockRepository()
    {
        return new MartenReadModelRepository<OrderSummary>(
            Substitute.For<IDocumentSession>(),
            Substitute.For<ILogger<MartenReadModelRepository<OrderSummary>>>());
    }

    private static MartenInlineProjectionDispatcher CreateMockDispatcher()
    {
        return new MartenInlineProjectionDispatcher(
            Substitute.For<IDocumentSession>(),
            Substitute.For<IServiceProvider>(),
            Substitute.For<ILogger<MartenInlineProjectionDispatcher>>(),
            new ProjectionRegistry());
    }

    #endregion
}
