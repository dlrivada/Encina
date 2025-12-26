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
        options.Enabled.Should().BeFalse();
        options.ThrowOnUpcastFailure.Should().BeTrue();
        options.AssembliesToScan.Should().BeEmpty();
    }

    [Fact]
    public void Enabled_CanBeSet()
    {
        // Arrange
        var options = new EventVersioningOptions();

        // Act
        options.Enabled = true;

        // Assert
        options.Enabled.Should().BeTrue();
    }

    [Fact]
    public void ThrowOnUpcastFailure_CanBeDisabled()
    {
        // Arrange
        var options = new EventVersioningOptions();

        // Act
        options.ThrowOnUpcastFailure = false;

        // Assert
        options.ThrowOnUpcastFailure.Should().BeFalse();
    }

    [Fact]
    public void AddUpcaster_Generic_ReturnsSelfForChaining()
    {
        // Arrange
        var options = new EventVersioningOptions();

        // Act
        var result = options.AddUpcaster<OrderCreatedV1ToV2Upcaster>();

        // Assert
        result.Should().BeSameAs(options);
    }

    [Fact]
    public void AddUpcaster_ByType_ReturnsSelfForChaining()
    {
        // Arrange
        var options = new EventVersioningOptions();

        // Act
        var result = options.AddUpcaster(typeof(OrderCreatedV1ToV2Upcaster));

        // Assert
        result.Should().BeSameAs(options);
    }

    [Fact]
    public void AddUpcaster_ByType_NullType_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new EventVersioningOptions();

        // Act
        var act = () => options.AddUpcaster((Type)null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddUpcaster_ByType_NonUpcasterType_ThrowsArgumentException()
    {
        // Arrange
        var options = new EventVersioningOptions();

        // Act
        var act = () => options.AddUpcaster(typeof(string));

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*does not implement IEventUpcaster*");
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
        result.Should().BeSameAs(options);
    }

    [Fact]
    public void AddUpcaster_Instance_NullUpcaster_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new EventVersioningOptions();

        // Act
        var act = () => options.AddUpcaster((IEventUpcaster)null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
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
        result.Should().BeSameAs(options);
    }

    [Fact]
    public void AddUpcaster_Lambda_NullFunc_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new EventVersioningOptions();

        // Act
        var act = () => options.AddUpcaster<OrderCreatedV1, OrderCreatedV2>(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
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
        act.Should().NotThrow();
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
        options.AssembliesToScan.Should().Contain(assembly);
    }

    [Fact]
    public void ScanAssembly_ReturnsSelfForChaining()
    {
        // Arrange
        var options = new EventVersioningOptions();

        // Act
        var result = options.ScanAssembly(typeof(OrderCreatedV1ToV2Upcaster).Assembly);

        // Assert
        result.Should().BeSameAs(options);
    }

    [Fact]
    public void ScanAssembly_NullAssembly_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new EventVersioningOptions();

        // Act
        var act = () => options.ScanAssembly(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
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
        options.AssembliesToScan.Should().Contain(assembly1);
        options.AssembliesToScan.Should().Contain(assembly2);
    }

    [Fact]
    public void ScanAssemblies_NullArray_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new EventVersioningOptions();

        // Act
        var act = () => options.ScanAssemblies(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
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
        registry.Count.Should().Be(2);
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
        registry.Count.Should().Be(1);
        registry.HasUpcasterFor(nameof(SimpleEventV1)).Should().BeTrue();
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
        registry.Count.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ApplyTo_NullRegistry_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new EventVersioningOptions();

        // Act
        var act = () => options.ApplyTo(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
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
        result.Should().BeSameAs(options);
    }
}
