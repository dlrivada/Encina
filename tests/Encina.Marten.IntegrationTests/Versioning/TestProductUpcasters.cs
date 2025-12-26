using Encina.Marten.Versioning;

namespace Encina.Marten.IntegrationTests.Versioning;

/// <summary>
/// Test upcasters for integration tests.
/// </summary>
public sealed class ProductCreatedV1ToV2Upcaster : EventUpcasterBase<ProductCreatedV1, ProductCreatedV2>
{
    protected override ProductCreatedV2 Upcast(ProductCreatedV1 oldEvent)
        => new(oldEvent.ProductId, oldEvent.Name, Price: 0m);
}

public sealed class ProductUpdatedV1ToV2Upcaster : EventUpcasterBase<ProductUpdatedV1, ProductUpdatedV2>
{
    protected override ProductUpdatedV2 Upcast(ProductUpdatedV1 oldEvent)
        => new(oldEvent.ProductId, oldEvent.NewName, UpdatedAtUtc: DateTime.UtcNow);
}
