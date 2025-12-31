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
        name.ShouldNotBeNull();
        name.ShouldNotBeEmpty();
    }

    [Fact]
    public void IEventUpcaster_TargetEventType_MustNotBeNull()
    {
        // Arrange
        var upcaster = GetAsInterface(new OrderCreatedV1ToV2Upcaster());

        // Act
        var type = upcaster.TargetEventType;

        // Assert
        type.ShouldNotBeNull();
    }

    [Fact]
    public void IEventUpcaster_SourceEventType_MustNotBeNull()
    {
        // Arrange
        var upcaster = GetAsInterface(new OrderCreatedV1ToV2Upcaster());

        // Act
        var type = upcaster.SourceEventType;

        // Assert
        type.ShouldNotBeNull();
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
        sourceType.ShouldNotBe(targetType);
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
        newEvent.ShouldNotBeNull();
        newEvent.ShouldBeOfType<OrderCreatedV2>();
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
        newEvent.OrderId.ShouldBe(orderId);
        newEvent.CustomerName.ShouldBe(customerName);
    }

    #endregion

    #region EventUpcasterBase Contract

    [Fact]
    public void EventUpcasterBase_MustImplementIEventUpcaster()
    {
        // Arrange & Act
        var upcaster = new OrderCreatedV1ToV2Upcaster();

        // Assert
        upcaster.ShouldBeAssignableTo<IEventUpcaster>();
    }

    [Fact]
    public void EventUpcasterBase_MustImplementGenericIEventUpcaster()
    {
        // Arrange & Act
        var upcaster = new OrderCreatedV1ToV2Upcaster();

        // Assert
        upcaster.ShouldBeAssignableTo<IEventUpcaster<OrderCreatedV1, OrderCreatedV2>>();
    }

    [Fact]
    public void EventUpcasterBase_SourceEventTypeName_MustMatchTypeName()
    {
        // Arrange
        var upcaster = new OrderCreatedV1ToV2Upcaster();

        // Act
        var name = upcaster.SourceEventTypeName;

        // Assert - Default behavior is to use type name
        name.ShouldBe(nameof(OrderCreatedV1));
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
        upcaster.ShouldBeAssignableTo<IEventUpcaster>();
    }

    [Fact]
    public void LambdaEventUpcaster_MustImplementGenericIEventUpcaster()
    {
        // Arrange & Act
        var upcaster = new LambdaEventUpcaster<SimpleEventV1, SimpleEventV2>(
            old => new SimpleEventV2(old.Value, 42));

        // Assert
        upcaster.ShouldBeAssignableTo<IEventUpcaster<SimpleEventV1, SimpleEventV2>>();
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
        eventTypeName.ShouldBe(sourceEventTypeName);
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
        upcasters.Count.ShouldBe(2);
        upcasters.ShouldContain(u => u is OrderCreatedV1ToV2Upcaster);
        upcasters.ShouldContain(u => u is OrderCreatedV2ToV3Upcaster);
    }

    [Fact]
    public void Registry_Count_MustMatchRegisteredCount()
    {
        // Arrange
        var registry = new EventUpcasterRegistry();

        // Act & Assert
        registry.Count.ShouldBe(0);

        registry.Register<OrderCreatedV1ToV2Upcaster>();
        registry.Count.ShouldBe(1);

        registry.Register<OrderCreatedV2ToV3Upcaster>();
        registry.Count.ShouldBe(2);
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
        registry.HasUpcasterFor(registeredType).ShouldBeTrue();
        registry.GetUpcasterForEventType(registeredType).ShouldNotBeNull();

        registry.HasUpcasterFor(unregisteredType).ShouldBeFalse();
        registry.GetUpcasterForEventType(unregisteredType).ShouldBeNull();
    }

    #endregion

    #region EventVersioningOptions Contract

    [Fact]
    public void Options_FluentMethods_MustReturnSameInstance()
    {
        // Arrange
        var options = new EventVersioningOptions();

        // Act & Assert
        options.AddUpcaster<OrderCreatedV1ToV2Upcaster>().ShouldBeSameAs(options);
        options.AddUpcaster(typeof(OrderCreatedV2ToV3Upcaster)).ShouldBeSameAs(options);
        options.ScanAssembly(GetType().Assembly).ShouldBeSameAs(options);
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
        registry.Count.ShouldBe(2);
    }

    #endregion
}
