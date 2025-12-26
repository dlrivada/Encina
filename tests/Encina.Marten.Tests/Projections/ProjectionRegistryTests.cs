using Encina.Marten.Projections;

namespace Encina.Marten.Tests.Projections;

public sealed class ProjectionRegistryTests
{
    [Fact]
    public void Register_ValidProjection_RegistersSuccessfully()
    {
        // Arrange
        var registry = new ProjectionRegistry();

        // Act
        registry.Register<OrderSummaryProjection, OrderSummary>();

        // Assert
        var registration = registry.GetProjectionForReadModel<OrderSummary>();
        registration.ShouldNotBeNull();
        registration.ProjectionType.ShouldBe(typeof(OrderSummaryProjection));
        registration.ReadModelType.ShouldBe(typeof(OrderSummary));
    }

    [Fact]
    public void GetProjectionsForEvent_RegisteredEvent_ReturnsRegistrations()
    {
        // Arrange
        var registry = new ProjectionRegistry();
        registry.Register<OrderSummaryProjection, OrderSummary>();

        // Act
        var registrations = registry.GetProjectionsForEvent(typeof(OrderCreated));

        // Assert
        registrations.ShouldNotBeEmpty();
        registrations.Count.ShouldBe(1);
        registrations[0].ProjectionType.ShouldBe(typeof(OrderSummaryProjection));
    }

    [Fact]
    public void GetProjectionsForEvent_UnregisteredEvent_ReturnsEmpty()
    {
        // Arrange
        var registry = new ProjectionRegistry();
        registry.Register<OrderSummaryProjection, OrderSummary>();

        // Act
        var registrations = registry.GetProjectionsForEvent(typeof(string));

        // Assert
        registrations.ShouldBeEmpty();
    }

    [Fact]
    public void GetProjectionForReadModel_UnregisteredType_ReturnsNull()
    {
        // Arrange
        var registry = new ProjectionRegistry();

        // Act
        var registration = registry.GetProjectionForReadModel<OrderSummary>();

        // Assert
        registration.ShouldBeNull();
    }

    [Fact]
    public void GetAllProjections_MultipleRegistrations_ReturnsAll()
    {
        // Arrange
        var registry = new ProjectionRegistry();
        registry.Register<OrderSummaryProjection, OrderSummary>();

        // Act
        var all = registry.GetAllProjections();

        // Assert
        all.Count.ShouldBe(1);
    }

    [Fact]
    public void Register_DetectsCreatorHandler()
    {
        // Arrange
        var registry = new ProjectionRegistry();
        registry.Register<OrderSummaryProjection, OrderSummary>();

        // Act
        var registration = registry.GetProjectionForReadModel<OrderSummary>();
        var handlerInfo = registration?.GetHandlerInfo(typeof(OrderCreated));

        // Assert
        handlerInfo.ShouldNotBeNull();
        handlerInfo.Value.HandlerType.ShouldBe(ProjectionHandlerType.Creator);
    }

    [Fact]
    public void Register_DetectsApplyHandler()
    {
        // Arrange
        var registry = new ProjectionRegistry();
        registry.Register<OrderSummaryProjection, OrderSummary>();

        // Act
        var registration = registry.GetProjectionForReadModel<OrderSummary>();
        var handlerInfo = registration?.GetHandlerInfo(typeof(OrderItemAdded));

        // Assert
        handlerInfo.ShouldNotBeNull();
        handlerInfo.Value.HandlerType.ShouldBe(ProjectionHandlerType.Handler);
    }

    [Fact]
    public void Register_DetectsDeleterHandler()
    {
        // Arrange
        var registry = new ProjectionRegistry();
        registry.Register<OrderSummaryProjection, OrderSummary>();

        // Act
        var registration = registry.GetProjectionForReadModel<OrderSummary>();
        var handlerInfo = registration?.GetHandlerInfo(typeof(OrderCancelled));

        // Assert
        handlerInfo.ShouldNotBeNull();
        handlerInfo.Value.HandlerType.ShouldBe(ProjectionHandlerType.Deleter);
    }

    [Fact]
    public void GetHandlerInfo_UnhandledEvent_ReturnsNull()
    {
        // Arrange
        var registry = new ProjectionRegistry();
        registry.Register<OrderSummaryProjection, OrderSummary>();

        // Act
        var registration = registry.GetProjectionForReadModel<OrderSummary>();
        var handlerInfo = registration?.GetHandlerInfo(typeof(string));

        // Assert
        handlerInfo.ShouldBeNull();
    }
}
