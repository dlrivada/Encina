using Encina.Marten.Versioning;

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
        name.Should().Be(nameof(OrderCreatedV1));
    }

    [Fact]
    public void TargetEventType_ReturnsCorrectType()
    {
        // Arrange
        var upcaster = new OrderCreatedV1ToV2Upcaster();

        // Act
        var type = upcaster.TargetEventType;

        // Assert
        type.Should().Be(typeof(OrderCreatedV2));
    }

    [Fact]
    public void SourceEventType_ReturnsCorrectType()
    {
        // Arrange
        var upcaster = new OrderCreatedV1ToV2Upcaster();

        // Act
        var type = upcaster.SourceEventType;

        // Assert
        type.Should().Be(typeof(OrderCreatedV1));
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
        newEvent.OrderId.Should().Be(oldEvent.OrderId);
        newEvent.CustomerName.Should().Be(oldEvent.CustomerName);
        newEvent.Email.Should().Be("unknown@example.com");
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
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void CustomSourceEventTypeName_IsUsed()
    {
        // Arrange
        var upcaster = new CustomNameUpcaster();

        // Act
        var name = upcaster.SourceEventTypeName;

        // Assert
        name.Should().Be("CustomEventName");
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
        v3Event.OrderId.Should().Be(v1Event.OrderId);
        v3Event.CustomerName.Should().Be(v1Event.CustomerName);
        v3Event.Email.Should().Be("unknown@example.com");
        v3Event.ShippingAddress.Should().Be("Unknown");
    }

    [Fact]
    public void ImplementsIEventUpcaster_Interface()
    {
        // Arrange
        var upcaster = new OrderCreatedV1ToV2Upcaster();

        // Act & Assert
        upcaster.Should().BeAssignableTo<IEventUpcaster>();
        upcaster.Should().BeAssignableTo<IEventUpcaster<OrderCreatedV1, OrderCreatedV2>>();
    }
}
