using Encina.Marten.Versioning;

namespace Encina.Marten.Tests.Versioning;

/// <summary>
/// Contract tests for event versioning interfaces and base classes.
/// Verifies that implementations correctly fulfill their contracts.
/// </summary>
public sealed class VersioningContractTests
{
    #region IEventUpcaster Contract

    [Fact]
    public void IEventUpcaster_SourceEventTypeName_MustNotBeNull()
    {
        // Arrange
        var upcaster = GetAsInterface(new OrderCreatedV1ToV2Upcaster());

        // Act
        var name = upcaster.SourceEventTypeName;

        // Assert
        name.Should().NotBeNull();
        name.Should().NotBeEmpty();
    }

    [Fact]
    public void IEventUpcaster_TargetEventType_MustNotBeNull()
    {
        // Arrange
        var upcaster = GetAsInterface(new OrderCreatedV1ToV2Upcaster());

        // Act
        var type = upcaster.TargetEventType;

        // Assert
        type.Should().NotBeNull();
    }

    [Fact]
    public void IEventUpcaster_SourceEventType_MustNotBeNull()
    {
        // Arrange
        var upcaster = GetAsInterface(new OrderCreatedV1ToV2Upcaster());

        // Act
        var type = upcaster.SourceEventType;

        // Assert
        type.Should().NotBeNull();
    }

    [Fact]
    public void IEventUpcaster_SourceAndTargetTypes_MustBeDifferent()
    {
        // Arrange
        var upcaster = GetAsInterface(new OrderCreatedV1ToV2Upcaster());

        // Act
        var sourceType = upcaster.SourceEventType;
        var targetType = upcaster.TargetEventType;

        // Assert
        sourceType.Should().NotBe(targetType);
    }

    /// <summary>
    /// Helper to cast upcaster to interface type (needed for contract testing).
    /// </summary>
    private static IEventUpcaster GetAsInterface(IEventUpcaster upcaster) => upcaster;

    #endregion

    #region IEventUpcaster<TFrom, TTo> Contract

    [Fact]
    public void GenericIEventUpcaster_Upcast_MustReturnNewInstance()
    {
        // Arrange
        var upcaster = new OrderCreatedV1ToV2Upcaster();
        IEventUpcaster<OrderCreatedV1, OrderCreatedV2> typedUpcaster = upcaster;
        var oldEvent = new OrderCreatedV1(Guid.NewGuid(), "Test");

        // Act
        var newEvent = typedUpcaster.Upcast(oldEvent);

        // Assert
        newEvent.Should().NotBeNull();
        newEvent.Should().BeOfType<OrderCreatedV2>();
    }

    [Fact]
    public void GenericIEventUpcaster_Upcast_MustPreserveIdentity()
    {
        // Arrange
        var upcaster = new OrderCreatedV1ToV2Upcaster();
        IEventUpcaster<OrderCreatedV1, OrderCreatedV2> typedUpcaster = upcaster;
        var orderId = Guid.NewGuid();
        var customerName = "Test Customer";
        var oldEvent = new OrderCreatedV1(orderId, customerName);

        // Act
        var newEvent = typedUpcaster.Upcast(oldEvent);

        // Assert - Identity fields should be preserved
        newEvent.OrderId.Should().Be(orderId);
        newEvent.CustomerName.Should().Be(customerName);
    }

    #endregion

    #region EventUpcasterBase Contract

    [Fact]
    public void EventUpcasterBase_MustImplementIEventUpcaster()
    {
        // Arrange & Act
        var upcaster = new OrderCreatedV1ToV2Upcaster();

        // Assert
        upcaster.Should().BeAssignableTo<IEventUpcaster>();
    }

    [Fact]
    public void EventUpcasterBase_MustImplementGenericIEventUpcaster()
    {
        // Arrange & Act
        var upcaster = new OrderCreatedV1ToV2Upcaster();

        // Assert
        upcaster.Should().BeAssignableTo<IEventUpcaster<OrderCreatedV1, OrderCreatedV2>>();
    }

    [Fact]
    public void EventUpcasterBase_SourceEventTypeName_MustMatchTypeName()
    {
        // Arrange
        var upcaster = new OrderCreatedV1ToV2Upcaster();

        // Act
        var name = upcaster.SourceEventTypeName;

        // Assert - Default behavior is to use type name
        name.Should().Be(nameof(OrderCreatedV1));
    }

    #endregion

    #region LambdaEventUpcaster Contract

    [Fact]
    public void LambdaEventUpcaster_MustImplementIEventUpcaster()
    {
        // Arrange & Act
        var upcaster = new LambdaEventUpcaster<SimpleEventV1, SimpleEventV2>(
            old => new SimpleEventV2(old.Value, 42));

        // Assert
        upcaster.Should().BeAssignableTo<IEventUpcaster>();
    }

    [Fact]
    public void LambdaEventUpcaster_MustImplementGenericIEventUpcaster()
    {
        // Arrange & Act
        var upcaster = new LambdaEventUpcaster<SimpleEventV1, SimpleEventV2>(
            old => new SimpleEventV2(old.Value, 42));

        // Assert
        upcaster.Should().BeAssignableTo<IEventUpcaster<SimpleEventV1, SimpleEventV2>>();
    }

    [Fact]
    public void LambdaEventUpcaster_EventTypeName_MustMatchSourceEventTypeName()
    {
        // Arrange
        var upcaster = new LambdaEventUpcaster<SimpleEventV1, SimpleEventV2>(
            old => new SimpleEventV2(old.Value, 42));

        // Act
        var eventTypeName = upcaster.EventTypeName;
        var sourceEventTypeName = upcaster.SourceEventTypeName;

        // Assert
        eventTypeName.Should().Be(sourceEventTypeName);
    }

    #endregion

    #region EventUpcasterRegistry Contract

    [Fact]
    public void Registry_GetAllUpcasters_MustReturnAllRegistered()
    {
        // Arrange
        var registry = new EventUpcasterRegistry();
        registry.Register<OrderCreatedV1ToV2Upcaster>();
        registry.Register<OrderCreatedV2ToV3Upcaster>();

        // Act
        var upcasters = registry.GetAllUpcasters();

        // Assert
        upcasters.Should().HaveCount(2);
        upcasters.Should().Contain(u => u is OrderCreatedV1ToV2Upcaster);
        upcasters.Should().Contain(u => u is OrderCreatedV2ToV3Upcaster);
    }

    [Fact]
    public void Registry_Count_MustMatchRegisteredCount()
    {
        // Arrange
        var registry = new EventUpcasterRegistry();

        // Act & Assert
        registry.Count.Should().Be(0);

        registry.Register<OrderCreatedV1ToV2Upcaster>();
        registry.Count.Should().Be(1);

        registry.Register<OrderCreatedV2ToV3Upcaster>();
        registry.Count.Should().Be(2);
    }

    [Fact]
    public void Registry_HasUpcasterFor_MustBeConsistentWithGet()
    {
        // Arrange
        var registry = new EventUpcasterRegistry();
        registry.Register<OrderCreatedV1ToV2Upcaster>();

        var registeredType = nameof(OrderCreatedV1);
        var unregisteredType = "UnknownEvent";

        // Act & Assert
        registry.HasUpcasterFor(registeredType).Should().BeTrue();
        registry.GetUpcasterForEventType(registeredType).Should().NotBeNull();

        registry.HasUpcasterFor(unregisteredType).Should().BeFalse();
        registry.GetUpcasterForEventType(unregisteredType).Should().BeNull();
    }

    #endregion

    #region EventVersioningOptions Contract

    [Fact]
    public void Options_FluentMethods_MustReturnSameInstance()
    {
        // Arrange
        var options = new EventVersioningOptions();

        // Act & Assert
        options.AddUpcaster<OrderCreatedV1ToV2Upcaster>().Should().BeSameAs(options);
        options.AddUpcaster(typeof(OrderCreatedV2ToV3Upcaster)).Should().BeSameAs(options);
        options.ScanAssembly(GetType().Assembly).Should().BeSameAs(options);
    }

    [Fact]
    public void Options_ApplyTo_MustRegisterAllConfiguredUpcasters()
    {
        // Arrange
        var options = new EventVersioningOptions();
        options.AddUpcaster<OrderCreatedV1ToV2Upcaster>();
        options.AddUpcaster<SimpleEventV1, SimpleEventV2>(old => new SimpleEventV2(old.Value, 42));

        var registry = new EventUpcasterRegistry();

        // Act
        options.ApplyTo(registry);

        // Assert
        registry.Count.Should().Be(2);
    }

    #endregion
}
