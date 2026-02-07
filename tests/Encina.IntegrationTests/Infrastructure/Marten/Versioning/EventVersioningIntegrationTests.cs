using Encina.IntegrationTests.Infrastructure.Marten.Fixtures;
using Encina.Marten.Versioning;
using Shouldly;
using Xunit;

namespace Encina.IntegrationTests.Infrastructure.Marten.Versioning;

/// <summary>
/// Integration tests for event versioning with a real PostgreSQL database.
/// </summary>
[Collection(MartenCollection.Name)]
[Trait("Category", "Integration")]
[Trait("Database", "PostgreSQL")]
public sealed class EventVersioningIntegrationTests
{
    private readonly MartenFixture _fixture;

    public EventVersioningIntegrationTests(MartenFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void Registry_CanRegisterMultipleUpcasters()
    {
        // Arrange
        var registry = new EventUpcasterRegistry();

        // Act
        registry.Register<ProductCreatedV1ToV2Upcaster>();
        registry.Register<ProductUpdatedV1ToV2Upcaster>();

        // Assert
        registry.Count.ShouldBe(2);
        registry.HasUpcasterFor(nameof(ProductCreatedV1)).ShouldBeTrue();
        registry.HasUpcasterFor(nameof(ProductUpdatedV1)).ShouldBeTrue();
    }

    [Fact]
    public void Registry_ScanAndRegister_FindsUpcastersInAssembly()
    {
        // Arrange
        var registry = new EventUpcasterRegistry();

        // Act
        var count = registry.ScanAndRegister(typeof(ProductCreatedV1ToV2Upcaster).Assembly);

        // Assert
        count.ShouldBeGreaterThan(0);
        registry.HasUpcasterFor(nameof(ProductCreatedV1)).ShouldBeTrue();
    }

    [Fact]
    public void Upcaster_TransformsEventCorrectly()
    {
        // Arrange
        var upcaster = new ProductCreatedV1ToV2Upcaster();
        var productId = Guid.NewGuid();
        var v1Event = new ProductCreatedV1(productId, "Test Product");

        // Act
        IEventUpcaster<ProductCreatedV1, ProductCreatedV2> typedUpcaster = upcaster;
        var v2Event = typedUpcaster.Upcast(v1Event);

        // Assert
        v2Event.ProductId.ShouldBe(productId);
        v2Event.Name.ShouldBe("Test Product");
        v2Event.Price.ShouldBe(0m);
    }

    [Fact]
    public void LambdaUpcaster_CanBeCreatedAndUsed()
    {
        // Arrange
        var upcaster = new LambdaEventUpcaster<ProductCreatedV1, ProductCreatedV2>(
            old => new ProductCreatedV2(old.ProductId, old.Name, Price: 9.99m));
        var productId = Guid.NewGuid();
        var v1Event = new ProductCreatedV1(productId, "Lambda Product");

        // Act
        IEventUpcaster<ProductCreatedV1, ProductCreatedV2> typedUpcaster = upcaster;
        var v2Event = typedUpcaster.Upcast(v1Event);

        // Assert
        v2Event.ProductId.ShouldBe(productId);
        v2Event.Name.ShouldBe("Lambda Product");
        v2Event.Price.ShouldBe(9.99m);
    }

    [Fact]
    public void Options_CanConfigureUpcastersFluidly()
    {
        // Arrange
        var options = new EventVersioningOptions();

        // Act
        options
            .AddUpcaster<ProductCreatedV1ToV2Upcaster>()
            .AddUpcaster<ProductUpdatedV1ToV2Upcaster>()
            .ScanAssembly(typeof(ProductCreatedV1ToV2Upcaster).Assembly);

        var registry = new EventUpcasterRegistry();
        options.ApplyTo(registry);

        // Assert
        registry.Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void Registry_GetAllUpcasters_ReturnsCorrectInstances()
    {
        // Arrange
        var registry = new EventUpcasterRegistry();
        registry.Register<ProductCreatedV1ToV2Upcaster>();
        registry.Register<ProductUpdatedV1ToV2Upcaster>();

        // Act
        var upcasters = registry.GetAllUpcasters();

        // Assert
        upcasters.Count.ShouldBe(2);
        upcasters.ShouldContain(u => u is ProductCreatedV1ToV2Upcaster);
        upcasters.ShouldContain(u => u is ProductUpdatedV1ToV2Upcaster);
    }

    [Fact]
    public void Fixture_IsAvailable_WhenContainerRunning()
    {
        // Skip if Docker/container is not available

        // Arrange & Act - verify fixture is configured
        var store = _fixture.Store;
        var connectionString = _fixture.ConnectionString;

        // Assert
        store.ShouldNotBeNull();
        connectionString.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void Fixture_Store_IsConfiguredForEventVersioning()
    {
        // Skip if Docker/container is not available

        // Arrange & Act
        var store = _fixture.Store;

        // Assert - verify the store is available for event versioning tests
        store.ShouldNotBeNull();
    }
}
