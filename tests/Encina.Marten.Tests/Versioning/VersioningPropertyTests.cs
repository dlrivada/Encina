using Encina.Marten.Versioning;
using FsCheck;
using FsCheck.Xunit;

namespace Encina.Marten.Tests.Versioning;

/// <summary>
/// Property-based tests for event versioning components.
/// Verifies invariants hold for various inputs.
/// </summary>
public sealed class VersioningPropertyTests
{
    #region Upcasting Invariants

    [Property(MaxTest = 100)]
    public bool Upcast_PreservesOrderId(Guid orderId, NonEmptyString customerName)
    {
        var upcaster = new OrderCreatedV1ToV2Upcaster();
        IEventUpcaster<OrderCreatedV1, OrderCreatedV2> typedUpcaster = upcaster;
        var oldEvent = new OrderCreatedV1(orderId, customerName.Get);

        var newEvent = typedUpcaster.Upcast(oldEvent);

        return newEvent.OrderId == orderId;
    }

    [Property(MaxTest = 100)]
    public bool Upcast_PreservesCustomerName(Guid orderId, NonEmptyString customerName)
    {
        var upcaster = new OrderCreatedV1ToV2Upcaster();
        IEventUpcaster<OrderCreatedV1, OrderCreatedV2> typedUpcaster = upcaster;
        var oldEvent = new OrderCreatedV1(orderId, customerName.Get);

        var newEvent = typedUpcaster.Upcast(oldEvent);

        return newEvent.CustomerName == customerName.Get;
    }

    [Property(MaxTest = 100)]
    public bool ChainedUpcast_PreservesIdentity(Guid orderId, NonEmptyString customerName)
    {
        var upcasterV1ToV2 = new OrderCreatedV1ToV2Upcaster();
        var upcasterV2ToV3 = new OrderCreatedV2ToV3Upcaster();
        IEventUpcaster<OrderCreatedV1, OrderCreatedV2> typedV1ToV2 = upcasterV1ToV2;
        IEventUpcaster<OrderCreatedV2, OrderCreatedV3> typedV2ToV3 = upcasterV2ToV3;

        var v1Event = new OrderCreatedV1(orderId, customerName.Get);
        var v2Event = typedV1ToV2.Upcast(v1Event);
        var v3Event = typedV2ToV3.Upcast(v2Event);

        return v3Event.OrderId == orderId && v3Event.CustomerName == customerName.Get;
    }

    #endregion

    #region Registry Invariants

    [Property(MaxTest = 100)]
    public bool Registry_RegisterThenGet_ReturnsUpcaster(PositiveInt _)
    {
        var registry = new EventUpcasterRegistry();
        registry.Register<OrderCreatedV1ToV2Upcaster>();

        var upcaster = registry.GetUpcasterForEventType(nameof(OrderCreatedV1));

        return upcaster != null && upcaster is OrderCreatedV1ToV2Upcaster;
    }

    [Property(MaxTest = 100)]
    public bool Registry_HasUpcaster_ConsistentWithGet(NonEmptyString eventTypeName)
    {
        var registry = new EventUpcasterRegistry();
        registry.Register<OrderCreatedV1ToV2Upcaster>();

        var name = eventTypeName.Get;
        var hasUpcaster = registry.HasUpcasterFor(name);
        var getUpcaster = registry.GetUpcasterForEventType(name);

        return hasUpcaster == (getUpcaster != null);
    }

    [Property(MaxTest = 100)]
    public bool Registry_Count_EqualsRegisteredCount(PositiveInt _)
    {
        var registry = new EventUpcasterRegistry();
        registry.TryRegister(new OrderCreatedV1ToV2Upcaster());
        registry.TryRegister(new OrderCreatedV2ToV3Upcaster());

        return registry.Count == 2;
    }

    [Property(MaxTest = 100)]
    public bool Registry_GetAllUpcasters_CountMatchesCount(PositiveInt _)
    {
        var registry = new EventUpcasterRegistry();
        registry.Register<OrderCreatedV1ToV2Upcaster>();
        registry.Register<OrderCreatedV2ToV3Upcaster>();

        var all = registry.GetAllUpcasters();

        return all.Count == registry.Count;
    }

    #endregion

    #region LambdaEventUpcaster Invariants

    [Property(MaxTest = 100)]
    public bool LambdaUpcaster_InvokesFunction(NonEmptyString value)
    {
        const int expectedNumber = 42;
        var upcaster = new LambdaEventUpcaster<SimpleEventV1, SimpleEventV2>(
            old => new SimpleEventV2(old.Value, expectedNumber));
        IEventUpcaster<SimpleEventV1, SimpleEventV2> typedUpcaster = upcaster;

        var oldEvent = new SimpleEventV1(value.Get);
        var newEvent = typedUpcaster.Upcast(oldEvent);

        return newEvent.Value == value.Get && newEvent.Number == expectedNumber;
    }

    [Property(MaxTest = 100)]
    public bool LambdaUpcaster_CustomEventTypeName_IsUsed(NonEmptyString customName)
    {
        var upcaster = new LambdaEventUpcaster<SimpleEventV1, SimpleEventV2>(
            old => new SimpleEventV2(old.Value, 42),
            eventTypeName: customName.Get);

        return upcaster.SourceEventTypeName == customName.Get &&
               upcaster.EventTypeName == customName.Get;
    }

    #endregion

    #region EventVersioningOptions Invariants

    [Property(MaxTest = 100)]
    public bool Options_AddUpcaster_ReturnsSelf(PositiveInt _)
    {
        var options = new EventVersioningOptions();
        var result = options.AddUpcaster<OrderCreatedV1ToV2Upcaster>();

        return ReferenceEquals(result, options);
    }

    [Property(MaxTest = 100)]
    public bool Options_ScanAssembly_AddsToList(PositiveInt _)
    {
        var options = new EventVersioningOptions();
        var assembly = typeof(VersioningPropertyTests).Assembly;

        var countBefore = options.AssembliesToScan.Count;
        options.ScanAssembly(assembly);
        var countAfter = options.AssembliesToScan.Count;

        return countAfter == countBefore + 1 &&
               options.AssembliesToScan.Contains(assembly);
    }

    #endregion

    #region Type Information Invariants

    [Property(MaxTest = 100)]
    public bool Upcaster_SourceType_MatchesGenericParameter(PositiveInt _)
    {
        var upcaster = new OrderCreatedV1ToV2Upcaster();

        return upcaster.SourceEventType == typeof(OrderCreatedV1);
    }

    [Property(MaxTest = 100)]
    public bool Upcaster_TargetType_MatchesGenericParameter(PositiveInt _)
    {
        var upcaster = new OrderCreatedV1ToV2Upcaster();

        return upcaster.TargetEventType == typeof(OrderCreatedV2);
    }

    #endregion
}
