using Encina.Marten.Projections;
using FluentAssertions;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;

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
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("session");
    }

    [Fact]
    public void MartenReadModelRepository_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = () => new MartenReadModelRepository<OrderSummary>(
            Substitute.For<IDocumentSession>(),
            null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("logger");
    }

    [Fact]
    public async Task MartenReadModelRepository_QueryAsync_NullPredicate_ThrowsArgumentNullException()
    {
        // Arrange
        var repo = CreateMockRepository();

        // Act
        var act = async () => await repo.QueryAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("predicate");
    }

    [Fact]
    public async Task MartenReadModelRepository_StoreAsync_NullReadModel_ThrowsArgumentNullException()
    {
        // Arrange
        var repo = CreateMockRepository();

        // Act
        var act = async () => await repo.StoreAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("readModel");
    }

    [Fact]
    public async Task MartenReadModelRepository_StoreManyAsync_NullReadModels_ThrowsArgumentNullException()
    {
        // Arrange
        var repo = CreateMockRepository();

        // Act
        var act = async () => await repo.StoreManyAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("readModels");
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
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("store");
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
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("serviceProvider");
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
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("logger");
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
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("registry");
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
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("session");
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
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("serviceProvider");
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
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("logger");
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
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("registry");
    }

    [Fact]
    public async Task MartenInlineProjectionDispatcher_DispatchAsync_NullEvent_ThrowsArgumentNullException()
    {
        // Arrange
        var dispatcher = CreateMockDispatcher();

        // Act
        var act = async () => await dispatcher.DispatchAsync(null!, new ProjectionContext());

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("domainEvent");
    }

    [Fact]
    public async Task MartenInlineProjectionDispatcher_DispatchAsync_NullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var dispatcher = CreateMockDispatcher();

        // Act
        var act = async () => await dispatcher.DispatchAsync(new OrderCreated("Test"), null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("context");
    }

    [Fact]
    public async Task MartenInlineProjectionDispatcher_DispatchManyAsync_NullEvents_ThrowsArgumentNullException()
    {
        // Arrange
        var dispatcher = CreateMockDispatcher();

        // Act
        var act = async () => await dispatcher.DispatchManyAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("events");
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
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("eventType");
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
