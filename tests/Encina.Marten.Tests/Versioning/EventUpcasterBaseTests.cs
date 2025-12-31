using Encina.Marten.Versioning;
using Shouldly;

namespace Encina.Marten.Tests.Versioning;

public sealed class EventUpcasterBaseTests
{
    [Fact]
    public void SourceEventTypeName_ReturnsSourceTypeName()
    {
        // Arrange
        var upcaster = new OrderCreatedV1ToV2Upcaster();

        // Act
        var name = upcaster.SourceEventTypeName;

        // Assert
        name.ShouldBe(nameof(OrderCreatedV1));
    }

    [Fact]
    public void TargetEventType_ReturnsCorrectType()
    {
        // Arrange
        var upcaster = new OrderCreatedV1ToV2Upcaster();

        // Act
        var type = upcaster.TargetEventType;

        // Assert
        type.ShouldBe(typeof(OrderCreatedV2));
    }

    [Fact]
    public void SourceEventType_ReturnsCorrectType()
    {
        // Arrange
        var upcaster = new OrderCreatedV1ToV2Upcaster();

        // Act
        var type = upcaster.SourceEventType;

        // Assert
        type.ShouldBe(typeof(OrderCreatedV1));
    }

    [Fact]
    public void Upcast_TransformsEventCorrectly()
    {
        // Arrange
        var upcaster = new OrderCreatedV1ToV2Upcaster();
        var oldEvent = new OrderCreatedV1(Guid.NewGuid(), "John Doe");

        // Act
        IEventUpcaster<OrderCreatedV1, OrderCreatedV2> typedUpcaster = upcaster;
        var newEvent = typedUpcaster.Upcast(oldEvent);

        // Assert
        newEvent.OrderId.ShouldBe(oldEvent.OrderId);
        newEvent.CustomerName.ShouldBe(oldEvent.CustomerName);
        newEvent.Email.ShouldBe("unknown@example.com");
    }

    [Fact]
    public void Upcast_ThrowsForNullEvent()
    {
        // Arrange
        var upcaster = new OrderCreatedV1ToV2Upcaster();
        IEventUpcaster<OrderCreatedV1, OrderCreatedV2> typedUpcaster = upcaster;

        // Act
        var act = () => typedUpcaster.Upcast(null!);

        // Assert
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void CustomSourceEventTypeName_IsUsed()
    {
        // Arrange
        var upcaster = new CustomNameUpcaster();

        // Act
        var name = upcaster.SourceEventTypeName;

        // Assert
        name.ShouldBe("CustomEventName");
    }

    [Fact]
    public void ChainedUpcasting_WorksCorrectly()
    {
        // Arrange
        var upcasterV1ToV2 = new OrderCreatedV1ToV2Upcaster();
        var upcasterV2ToV3 = new OrderCreatedV2ToV3Upcaster();
        var v1Event = new OrderCreatedV1(Guid.NewGuid(), "Jane Doe");

        // Act
        IEventUpcaster<OrderCreatedV1, OrderCreatedV2> typedV1ToV2 = upcasterV1ToV2;
        IEventUpcaster<OrderCreatedV2, OrderCreatedV3> typedV2ToV3 = upcasterV2ToV3;

        var v2Event = typedV1ToV2.Upcast(v1Event);
        var v3Event = typedV2ToV3.Upcast(v2Event);

        // Assert
        v3Event.OrderId.ShouldBe(v1Event.OrderId);
        v3Event.CustomerName.ShouldBe(v1Event.CustomerName);
        v3Event.Email.ShouldBe("unknown@example.com");
        v3Event.ShippingAddress.ShouldBe("Unknown");
    }

    [Fact]
    public void ImplementsIEventUpcaster_Interface()
    {
        // Arrange
        var upcaster = new OrderCreatedV1ToV2Upcaster();

        // Act & Assert
        upcaster.ShouldBeAssignableTo<IEventUpcaster>();
        upcaster.ShouldBeAssignableTo<IEventUpcaster<OrderCreatedV1, OrderCreatedV2>>();
    }
}
