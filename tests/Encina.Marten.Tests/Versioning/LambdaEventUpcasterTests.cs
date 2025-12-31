using Encina.Marten.Versioning;
using Shouldly;

namespace Encina.Marten.Tests.Versioning;

public sealed class LambdaEventUpcasterTests
{
    [Fact]
    public void Constructor_WithValidFunc_DoesNotThrow()
    {
        // Arrange & Act
        var act = () => new LambdaEventUpcaster<OrderCreatedV1, OrderCreatedV2>(
            old => new OrderCreatedV2(old.OrderId, old.CustomerName, "test@example.com"));

        // Assert
        Should.NotThrow(act);
    }

    [Fact]
    public void Constructor_NullFunc_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = () => new LambdaEventUpcaster<OrderCreatedV1, OrderCreatedV2>(null!);

        // Assert
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void SourceEventTypeName_ReturnsDefaultTypeName()
    {
        // Arrange
        var upcaster = new LambdaEventUpcaster<OrderCreatedV1, OrderCreatedV2>(
            old => new OrderCreatedV2(old.OrderId, old.CustomerName, "test@example.com"));

        // Act
        var name = upcaster.SourceEventTypeName;

        // Assert
        name.ShouldBe(nameof(OrderCreatedV1));
    }

    [Fact]
    public void SourceEventTypeName_WithCustomName_ReturnsCustomName()
    {
        // Arrange
        var upcaster = new LambdaEventUpcaster<OrderCreatedV1, OrderCreatedV2>(
            old => new OrderCreatedV2(old.OrderId, old.CustomerName, "test@example.com"),
            eventTypeName: "MyCustomEventName");

        // Act
        var name = upcaster.SourceEventTypeName;

        // Assert
        name.ShouldBe("MyCustomEventName");
    }

    [Fact]
    public void TargetEventType_ReturnsCorrectType()
    {
        // Arrange
        var upcaster = new LambdaEventUpcaster<OrderCreatedV1, OrderCreatedV2>(
            old => new OrderCreatedV2(old.OrderId, old.CustomerName, "test@example.com"));

        // Act
        var type = upcaster.TargetEventType;

        // Assert
        type.ShouldBe(typeof(OrderCreatedV2));
    }

    [Fact]
    public void SourceEventType_ReturnsCorrectType()
    {
        // Arrange
        var upcaster = new LambdaEventUpcaster<OrderCreatedV1, OrderCreatedV2>(
            old => new OrderCreatedV2(old.OrderId, old.CustomerName, "test@example.com"));

        // Act
        var type = upcaster.SourceEventType;

        // Assert
        type.ShouldBe(typeof(OrderCreatedV1));
    }

    [Fact]
    public void EventTypeName_MatchesSourceEventTypeName()
    {
        // Arrange
        var upcaster = new LambdaEventUpcaster<OrderCreatedV1, OrderCreatedV2>(
            old => new OrderCreatedV2(old.OrderId, old.CustomerName, "test@example.com"));

        // Act
        var eventTypeName = upcaster.EventTypeName;
        var sourceEventTypeName = upcaster.SourceEventTypeName;

        // Assert
        eventTypeName.ShouldBe(sourceEventTypeName);
    }

    [Fact]
    public void Upcast_InvokesLambdaCorrectly()
    {
        // Arrange
        var upcaster = new LambdaEventUpcaster<OrderCreatedV1, OrderCreatedV2>(
            old => new OrderCreatedV2(old.OrderId, old.CustomerName, "lambda@example.com"));
        var oldEvent = new OrderCreatedV1(Guid.NewGuid(), "Test Customer");

        // Act
        IEventUpcaster<OrderCreatedV1, OrderCreatedV2> typedUpcaster = upcaster;
        var newEvent = typedUpcaster.Upcast(oldEvent);

        // Assert
        newEvent.OrderId.ShouldBe(oldEvent.OrderId);
        newEvent.CustomerName.ShouldBe(oldEvent.CustomerName);
        newEvent.Email.ShouldBe("lambda@example.com");
    }

    [Fact]
    public void Upcast_NullEvent_ThrowsArgumentNullException()
    {
        // Arrange
        var upcaster = new LambdaEventUpcaster<OrderCreatedV1, OrderCreatedV2>(
            old => new OrderCreatedV2(old.OrderId, old.CustomerName, "test@example.com"));
        IEventUpcaster<OrderCreatedV1, OrderCreatedV2> typedUpcaster = upcaster;

        // Act
        var act = () => typedUpcaster.Upcast(null!);

        // Assert
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void ImplementsIEventUpcaster_Interface()
    {
        // Arrange
        var upcaster = new LambdaEventUpcaster<OrderCreatedV1, OrderCreatedV2>(
            old => new OrderCreatedV2(old.OrderId, old.CustomerName, "test@example.com"));

        // Act & Assert
        upcaster.ShouldBeAssignableTo<IEventUpcaster>();
        upcaster.ShouldBeAssignableTo<IEventUpcaster<OrderCreatedV1, OrderCreatedV2>>();
    }

    [Fact]
    public void Lambda_CanAccessClosure()
    {
        // Arrange
        var defaultEmail = "closure@example.com";
        var upcaster = new LambdaEventUpcaster<OrderCreatedV1, OrderCreatedV2>(
            old => new OrderCreatedV2(old.OrderId, old.CustomerName, defaultEmail));
        var oldEvent = new OrderCreatedV1(Guid.NewGuid(), "Test");

        // Act
        IEventUpcaster<OrderCreatedV1, OrderCreatedV2> typedUpcaster = upcaster;
        var newEvent = typedUpcaster.Upcast(oldEvent);

        // Assert
        newEvent.Email.ShouldBe(defaultEmail);
    }

    [Fact]
    public void Lambda_CanPerformComplexTransformation()
    {
        // Arrange
        var upcaster = new LambdaEventUpcaster<OrderCreatedV1, OrderCreatedV2>(
            old => new OrderCreatedV2(
                old.OrderId,
                old.CustomerName.ToUpperInvariant(),
                $"{old.CustomerName.ToLowerInvariant().Replace(" ", ".")}@example.com"));
        var oldEvent = new OrderCreatedV1(Guid.NewGuid(), "John Doe");

        // Act
        IEventUpcaster<OrderCreatedV1, OrderCreatedV2> typedUpcaster = upcaster;
        var newEvent = typedUpcaster.Upcast(oldEvent);

        // Assert
        newEvent.CustomerName.ShouldBe("JOHN DOE");
        newEvent.Email.ShouldBe("john.doe@example.com");
    }
}
