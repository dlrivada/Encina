using Encina.Marten.Versioning;
using Shouldly;

namespace Encina.UnitTests.Marten.Versioning;

public class LambdaEventUpcasterTests
{
    // Constructor tests

    [Fact]
    public void Constructor_NullUpcastFunc_ThrowsArgumentNullException()
    {
        var act = () => new LambdaEventUpcaster<OldEvent, NewEvent>(null!);
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void Constructor_DefaultEventTypeName_UsesSourceTypeName()
    {
        // Arrange & Act
        var upcaster = new LambdaEventUpcaster<OldEvent, NewEvent>(
            old => new NewEvent(old.Data, "default"));

        // Assert
        upcaster.SourceEventTypeName.ShouldBe(nameof(OldEvent));
    }

    [Fact]
    public void Constructor_CustomEventTypeName_UsesProvidedName()
    {
        // Arrange & Act
        var upcaster = new LambdaEventUpcaster<OldEvent, NewEvent>(
            old => new NewEvent(old.Data, "default"),
            eventTypeName: "CustomEventName");

        // Assert
        upcaster.SourceEventTypeName.ShouldBe("CustomEventName");
    }

    // Property tests

    [Fact]
    public void SourceEventType_ReturnsCorrectType()
    {
        var upcaster = new LambdaEventUpcaster<OldEvent, NewEvent>(
            old => new NewEvent(old.Data, "default"));

        upcaster.SourceEventType.ShouldBe(typeof(OldEvent));
    }

    [Fact]
    public void TargetEventType_ReturnsCorrectType()
    {
        var upcaster = new LambdaEventUpcaster<OldEvent, NewEvent>(
            old => new NewEvent(old.Data, "default"));

        upcaster.TargetEventType.ShouldBe(typeof(NewEvent));
    }

    [Fact]
    public void EventTypeName_MatchesSourceEventTypeName()
    {
        var upcaster = new LambdaEventUpcaster<OldEvent, NewEvent>(
            old => new NewEvent(old.Data, "default"));

        upcaster.EventTypeName.ShouldBe(upcaster.SourceEventTypeName);
    }

    [Fact]
    public void EventTypeName_WithCustomName_ReturnsCustomName()
    {
        var upcaster = new LambdaEventUpcaster<OldEvent, NewEvent>(
            old => new NewEvent(old.Data, "default"),
            eventTypeName: "MyCustomName");

        upcaster.EventTypeName.ShouldBe("MyCustomName");
    }

    // IEventUpcaster<TFrom, TTo>.Upcast tests

    [Fact]
    public void Upcast_NullOldEvent_ThrowsArgumentNullException()
    {
        IEventUpcaster<OldEvent, NewEvent> upcaster = new LambdaEventUpcaster<OldEvent, NewEvent>(
            old => new NewEvent(old.Data, "default"));

        var act = () => upcaster.Upcast(null!);
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void Upcast_ValidOldEvent_AppliesTransformation()
    {
        // Arrange
        IEventUpcaster<OldEvent, NewEvent> upcaster = new LambdaEventUpcaster<OldEvent, NewEvent>(
            old => new NewEvent(old.Data, "extra-data"));

        var oldEvent = new OldEvent("hello");

        // Act
        var result = upcaster.Upcast(oldEvent);

        // Assert
        result.ShouldNotBeNull();
        result.Data.ShouldBe("hello");
        result.Extra.ShouldBe("extra-data");
    }

    [Fact]
    public void Upcast_ComplexTransformation_WorksCorrectly()
    {
        // Arrange
        IEventUpcaster<OrderCreatedV1, OrderCreatedV2> upcaster =
            new LambdaEventUpcaster<OrderCreatedV1, OrderCreatedV2>(
                old => new OrderCreatedV2(old.OrderId, old.CustomerName, "unknown@example.com"));

        var v1Event = new OrderCreatedV1(Guid.NewGuid(), "John Doe");

        // Act
        var result = upcaster.Upcast(v1Event);

        // Assert
        result.OrderId.ShouldBe(v1Event.OrderId);
        result.CustomerName.ShouldBe("John Doe");
        result.Email.ShouldBe("unknown@example.com");
    }

    // Test types

    public sealed record OldEvent(string Data);
    public sealed record NewEvent(string Data, string Extra);
    public sealed record OrderCreatedV1(Guid OrderId, string CustomerName);
    public sealed record OrderCreatedV2(Guid OrderId, string CustomerName, string Email);
}
