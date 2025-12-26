using Encina.Marten.Versioning;

namespace Encina.Marten.Tests.Versioning;

/// <summary>
/// Test upcasters for versioning tests.
/// </summary>
public sealed class OrderCreatedV1ToV2Upcaster : EventUpcasterBase<OrderCreatedV1, OrderCreatedV2>
{
    protected override OrderCreatedV2 Upcast(OrderCreatedV1 oldEvent)
        => new(oldEvent.OrderId, oldEvent.CustomerName, Email: "unknown@example.com");
}

public sealed class OrderCreatedV2ToV3Upcaster : EventUpcasterBase<OrderCreatedV2, OrderCreatedV3>
{
    protected override OrderCreatedV3 Upcast(OrderCreatedV2 oldEvent)
        => new(oldEvent.OrderId, oldEvent.CustomerName, oldEvent.Email, ShippingAddress: "Unknown");
}

public sealed class SimpleEventV1ToV2Upcaster : EventUpcasterBase<SimpleEventV1, SimpleEventV2>
{
    protected override SimpleEventV2 Upcast(SimpleEventV1 oldEvent)
        => new(oldEvent.Value, Number: 42);
}

/// <summary>
/// Test upcaster with a custom event type name.
/// </summary>
public sealed class CustomNameUpcaster : EventUpcasterBase<SimpleEventV1, SimpleEventV2>
{
    public override string SourceEventTypeName => "CustomEventName";

    protected override SimpleEventV2 Upcast(SimpleEventV1 oldEvent)
        => new(oldEvent.Value, Number: 100);
}
