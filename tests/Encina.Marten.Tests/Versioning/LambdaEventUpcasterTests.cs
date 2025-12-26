using Encina.Marten.Versioning;

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
        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_NullFunc_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = () => new LambdaEventUpcaster<OrderCreatedV1, OrderCreatedV2>(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
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
        name.Should().Be(nameof(OrderCreatedV1));
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
        name.Should().Be("MyCustomEventName");
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
        type.Should().Be(typeof(OrderCreatedV2));
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
        type.Should().Be(typeof(OrderCreatedV1));
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
        eventTypeName.Should().Be(sourceEventTypeName);
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
        newEvent.OrderId.Should().Be(oldEvent.OrderId);
        newEvent.CustomerName.Should().Be(oldEvent.CustomerName);
        newEvent.Email.Should().Be("lambda@example.com");
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
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ImplementsIEventUpcaster_Interface()
    {
        // Arrange
        var upcaster = new LambdaEventUpcaster<OrderCreatedV1, OrderCreatedV2>(
            old => new OrderCreatedV2(old.OrderId, old.CustomerName, "test@example.com"));

        // Act & Assert
        upcaster.Should().BeAssignableTo<IEventUpcaster>();
        upcaster.Should().BeAssignableTo<IEventUpcaster<OrderCreatedV1, OrderCreatedV2>>();
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
        newEvent.Email.Should().Be(defaultEmail);
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
        newEvent.CustomerName.Should().Be("JOHN DOE");
        newEvent.Email.Should().Be("john.doe@example.com");
    }
}
