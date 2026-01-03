using Encina.Marten.Versioning;

namespace Encina.Marten.Tests.Versioning;

public sealed class EventVersioningOptionsTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var options = new EventVersioningOptions();

        // Assert
        options.Enabled.ShouldBeFalse();
        options.ThrowOnUpcastFailure.ShouldBeTrue();
        options.AssembliesToScan.ShouldBeEmpty();
    }

    [Fact]
    public void Enabled_CanBeSet()
    {
        // Arrange
        var options = new EventVersioningOptions();

        // Act
        options.Enabled = true;

        // Assert
        options.Enabled.ShouldBeTrue();
    }

    [Fact]
    public void ThrowOnUpcastFailure_CanBeDisabled()
    {
        // Arrange
        var options = new EventVersioningOptions();

        // Act
        options.ThrowOnUpcastFailure = false;

        // Assert
        options.ThrowOnUpcastFailure.ShouldBeFalse();
    }

    [Fact]
    public void AddUpcaster_Generic_ReturnsSelfForChaining()
    {
        // Arrange
        var options = new EventVersioningOptions();

        // Act
        var result = options.AddUpcaster<OrderCreatedV1ToV2Upcaster>();

        // Assert
        result.ShouldBeSameAs(options);
    }

    [Fact]
    public void AddUpcaster_ByType_ReturnsSelfForChaining()
    {
        // Arrange
        var options = new EventVersioningOptions();

        // Act
        var result = options.AddUpcaster(typeof(OrderCreatedV1ToV2Upcaster));

        // Assert
        result.ShouldBeSameAs(options);
    }

    [Fact]
    public void AddUpcaster_ByType_NullType_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new EventVersioningOptions();

        // Act
        var act = () => options.AddUpcaster((Type)null!);

        // Assert
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void AddUpcaster_ByType_NonUpcasterType_ThrowsArgumentException()
    {
        // Arrange
        var options = new EventVersioningOptions();

        // Act
        var act = () => options.AddUpcaster(typeof(string));

        // Assert
        var ex = Should.Throw<ArgumentException>(act);
        ex.Message.ShouldContain("does not implement IEventUpcaster");
    }

    [Fact]
    public void AddUpcaster_Instance_ReturnsSelfForChaining()
    {
        // Arrange
        var options = new EventVersioningOptions();
        var upcaster = new OrderCreatedV1ToV2Upcaster();

        // Act
        var result = options.AddUpcaster(upcaster);

        // Assert
        result.ShouldBeSameAs(options);
    }

    [Fact]
    public void AddUpcaster_Instance_NullUpcaster_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new EventVersioningOptions();

        // Act
        var act = () => options.AddUpcaster((IEventUpcaster)null!);

        // Assert
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void AddUpcaster_Lambda_ReturnsSelfForChaining()
    {
        // Arrange
        var options = new EventVersioningOptions();

        // Act
        var result = options.AddUpcaster<OrderCreatedV1, OrderCreatedV2>(
            old => new OrderCreatedV2(old.OrderId, old.CustomerName, "test@example.com"));

        // Assert
        result.ShouldBeSameAs(options);
    }

    [Fact]
    public void AddUpcaster_Lambda_NullFunc_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new EventVersioningOptions();

        // Act
        var act = () => options.AddUpcaster<OrderCreatedV1, OrderCreatedV2>(null!);

        // Assert
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void AddUpcaster_Lambda_WithCustomEventTypeName_DoesNotThrow()
    {
        // Arrange
        var options = new EventVersioningOptions();

        // Act
        var act = () => options.AddUpcaster<OrderCreatedV1, OrderCreatedV2>(
            old => new OrderCreatedV2(old.OrderId, old.CustomerName, "test@example.com"),
            eventTypeName: "CustomEventName");

        // Assert
        Should.NotThrow(act);
    }

    [Fact]
    public void ScanAssembly_AddsAssemblyToList()
    {
        // Arrange
        var options = new EventVersioningOptions();
        var assembly = typeof(OrderCreatedV1ToV2Upcaster).Assembly;

        // Act
        options.ScanAssembly(assembly);

        // Assert
        options.AssembliesToScan.ShouldContain(assembly);
    }

    [Fact]
    public void ScanAssembly_ReturnsSelfForChaining()
    {
        // Arrange
        var options = new EventVersioningOptions();

        // Act
        var result = options.ScanAssembly(typeof(OrderCreatedV1ToV2Upcaster).Assembly);

        // Assert
        result.ShouldBeSameAs(options);
    }

    [Fact]
    public void ScanAssembly_NullAssembly_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new EventVersioningOptions();

        // Act
        var act = () => options.ScanAssembly(null!);

        // Assert
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void ScanAssemblies_AddsAllAssemblies()
    {
        // Arrange
        var options = new EventVersioningOptions();
        var assembly1 = typeof(OrderCreatedV1ToV2Upcaster).Assembly;
        var assembly2 = typeof(EventVersioningOptions).Assembly;

        // Act
        options.ScanAssemblies(assembly1, assembly2);

        // Assert
        options.AssembliesToScan.ShouldContain(assembly1);
        options.AssembliesToScan.ShouldContain(assembly2);
    }

    [Fact]
    public void ScanAssemblies_NullArray_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new EventVersioningOptions();

        // Act
        var act = () => options.ScanAssemblies(null!);

        // Assert
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void ApplyTo_RegistersAllUpcasters()
    {
        // Arrange
        var options = new EventVersioningOptions();
        options.AddUpcaster<OrderCreatedV1ToV2Upcaster>();
        options.AddUpcaster<OrderCreatedV2ToV3Upcaster>();

        var registry = new EventUpcasterRegistry();

        // Act
        options.ApplyTo(registry);

        // Assert
        registry.Count.ShouldBe(2);
    }

    [Fact]
    public void ApplyTo_RegistersLambdaUpcasters()
    {
        // Arrange
        var options = new EventVersioningOptions();
        options.AddUpcaster<SimpleEventV1, SimpleEventV2>(
            old => new SimpleEventV2(old.Value, 42));

        var registry = new EventUpcasterRegistry();

        // Act
        options.ApplyTo(registry);

        // Assert
        registry.Count.ShouldBe(1);
        registry.HasUpcasterFor(nameof(SimpleEventV1)).ShouldBeTrue();
    }

    [Fact]
    public void ApplyTo_ScansAssemblies()
    {
        // Arrange
        var options = new EventVersioningOptions();
        options.ScanAssembly(typeof(OrderCreatedV1ToV2Upcaster).Assembly);

        var registry = new EventUpcasterRegistry();

        // Act
        options.ApplyTo(registry);

        // Assert
        registry.Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void ApplyTo_NullRegistry_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new EventVersioningOptions();

        // Act
        var act = () => options.ApplyTo(null!);

        // Assert
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void FluentConfiguration_Works()
    {
        // Arrange & Act
        var options = new EventVersioningOptions();

        var result = options
            .AddUpcaster<OrderCreatedV1ToV2Upcaster>()
            .AddUpcaster<OrderCreatedV2ToV3Upcaster>()
            .AddUpcaster<SimpleEventV1, SimpleEventV2>(old => new SimpleEventV2(old.Value, 42))
            .ScanAssembly(typeof(EventVersioningOptions).Assembly);

        // Assert
        result.ShouldBeSameAs(options);
    }
}
