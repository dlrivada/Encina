using Encina.Marten.Projections;

namespace Encina.Marten.Tests.Projections;

public sealed class ProjectionHandlerInfoTests
{
    [Fact]
    public void InvokeCreate_CallsProjectionMethod()
    {
        // Arrange
        var projection = new OrderSummaryProjection();
        var createEvent = new OrderCreated("John Doe");
        var context = new ProjectionContext
        {
            StreamId = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
        };

        var registry = new ProjectionRegistry();
        registry.Register<OrderSummaryProjection, OrderSummary>();

        var registration = registry.GetProjectionForReadModel<OrderSummary>()!;
        var handlerInfo = registration.GetHandlerInfo(typeof(OrderCreated))!.Value;

        // Act
        var result = handlerInfo.InvokeCreate(projection, createEvent, context);

        // Assert
        var orderSummary = result.ShouldBeOfType<OrderSummary>();
        orderSummary.Id.ShouldBe(context.StreamId);
        orderSummary.CustomerName.ShouldBe("John Doe");
        orderSummary.Status.ShouldBe("Created");
    }

    [Fact]
    public void InvokeApply_CallsProjectionMethod()
    {
        // Arrange
        var projection = new OrderSummaryProjection();
        var addItemEvent = new OrderItemAdded("Widget", 10.00m, 2);
        var context = new ProjectionContext();
        var existing = new OrderSummary
        {
            Id = Guid.NewGuid(),
            TotalAmount = 0m,
            ItemCount = 0,
        };

        var registry = new ProjectionRegistry();
        registry.Register<OrderSummaryProjection, OrderSummary>();

        var registration = registry.GetProjectionForReadModel<OrderSummary>()!;
        var handlerInfo = registration.GetHandlerInfo(typeof(OrderItemAdded))!.Value;

        // Act
        var result = handlerInfo.InvokeApply(projection, addItemEvent, existing, context);

        // Assert
        var orderSummary = result.ShouldBeOfType<OrderSummary>();
        orderSummary.TotalAmount.ShouldBe(20.00m);
        orderSummary.ItemCount.ShouldBe(2);
    }

    [Fact]
    public void InvokeShouldDelete_CallsProjectionMethod()
    {
        // Arrange
        var projection = new OrderSummaryProjection();
        var cancelEvent = new OrderCancelled("Customer request");
        var context = new ProjectionContext();
        var existing = new OrderSummary
        {
            Id = Guid.NewGuid(),
            Status = "Created",
        };

        var registry = new ProjectionRegistry();
        registry.Register<OrderSummaryProjection, OrderSummary>();

        var registration = registry.GetProjectionForReadModel<OrderSummary>()!;
        var handlerInfo = registration.GetHandlerInfo(typeof(OrderCancelled))!.Value;

        // Act
        var result = handlerInfo.InvokeShouldDelete(projection, cancelEvent, existing, context);

        // Assert
        result.ShouldBeTrue();
    }
}
