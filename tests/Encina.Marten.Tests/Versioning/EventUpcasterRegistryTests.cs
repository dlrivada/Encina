using Encina.Marten.Versioning;

namespace Encina.Marten.Tests.Versioning;

public sealed class EventUpcasterRegistryTests
{
    [Fact]
    public void Register_Generic_AddsUpcaster()
    {
        // Arrange
        var registry = new EventUpcasterRegistry();

        // Act
        registry.Register<OrderCreatedV1ToV2Upcaster>();

        // Assert
        registry.Count.Should().Be(1);
        registry.HasUpcasterFor(nameof(OrderCreatedV1)).Should().BeTrue();
    }

    [Fact]
    public void Register_Instance_AddsUpcaster()
    {
        // Arrange
        var registry = new EventUpcasterRegistry();
        var upcaster = new OrderCreatedV1ToV2Upcaster();

        // Act
        registry.Register(upcaster);

        // Assert
        registry.Count.Should().Be(1);
    }

    [Fact]
    public void Register_NullInstance_ThrowsArgumentNullException()
    {
        // Arrange
        var registry = new EventUpcasterRegistry();

        // Act
        var act = () => registry.Register(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Register_DuplicateSourceEventType_ThrowsInvalidOperationException()
    {
        // Arrange
        var registry = new EventUpcasterRegistry();
        registry.Register<OrderCreatedV1ToV2Upcaster>();

        // Act - register another upcaster for the same source type
        var act = () => registry.Register<OrderCreatedV1ToV2Upcaster>();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*already registered*");
    }

    [Fact]
    public void Register_ByType_AddsUpcaster()
    {
        // Arrange
        var registry = new EventUpcasterRegistry();

        // Act
        registry.Register(typeof(OrderCreatedV1ToV2Upcaster));

        // Assert
        registry.Count.Should().Be(1);
    }

    [Fact]
    public void Register_ByType_NullType_ThrowsArgumentNullException()
    {
        // Arrange
        var registry = new EventUpcasterRegistry();

        // Act
        var act = () => registry.Register((Type)null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Register_ByType_NonUpcasterType_ThrowsArgumentException()
    {
        // Arrange
        var registry = new EventUpcasterRegistry();

        // Act
        var act = () => registry.Register(typeof(string));

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*does not implement IEventUpcaster*");
    }

    [Fact]
    public void TryRegister_ReturnsTrueOnSuccess()
    {
        // Arrange
        var registry = new EventUpcasterRegistry();
        var upcaster = new OrderCreatedV1ToV2Upcaster();

        // Act
        var result = registry.TryRegister(upcaster);

        // Assert
        result.Should().BeTrue();
        registry.Count.Should().Be(1);
    }

    [Fact]
    public void TryRegister_ReturnsFalseForDuplicate()
    {
        // Arrange
        var registry = new EventUpcasterRegistry();
        registry.Register<OrderCreatedV1ToV2Upcaster>();

        // Act
        var result = registry.TryRegister(new OrderCreatedV1ToV2Upcaster());

        // Assert
        result.Should().BeFalse();
        registry.Count.Should().Be(1);
    }

    [Fact]
    public void TryRegister_NullUpcaster_ThrowsArgumentNullException()
    {
        // Arrange
        var registry = new EventUpcasterRegistry();

        // Act
        var act = () => registry.TryRegister(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetUpcasterForEventType_ReturnsUpcaster()
    {
        // Arrange
        var registry = new EventUpcasterRegistry();
        registry.Register<OrderCreatedV1ToV2Upcaster>();

        // Act
        var upcaster = registry.GetUpcasterForEventType(nameof(OrderCreatedV1));

        // Assert
        upcaster.Should().NotBeNull();
        upcaster.Should().BeOfType<OrderCreatedV1ToV2Upcaster>();
    }

    [Fact]
    public void GetUpcasterForEventType_ReturnsNullForUnknownType()
    {
        // Arrange
        var registry = new EventUpcasterRegistry();

        // Act
        var upcaster = registry.GetUpcasterForEventType("UnknownEvent");

        // Assert
        upcaster.Should().BeNull();
    }

    [Fact]
    public void GetUpcasterForEventType_NullName_ThrowsArgumentNullException()
    {
        // Arrange
        var registry = new EventUpcasterRegistry();

        // Act
        var act = () => registry.GetUpcasterForEventType(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetAllUpcasters_ReturnsAllRegistered()
    {
        // Arrange
        var registry = new EventUpcasterRegistry();
        registry.Register<OrderCreatedV1ToV2Upcaster>();
        registry.Register<OrderCreatedV2ToV3Upcaster>();

        // Act
        var upcasters = registry.GetAllUpcasters();

        // Assert
        upcasters.Should().HaveCount(2);
    }

    [Fact]
    public void GetAllUpcasters_ReturnsEmptyListWhenEmpty()
    {
        // Arrange
        var registry = new EventUpcasterRegistry();

        // Act
        var upcasters = registry.GetAllUpcasters();

        // Assert
        upcasters.Should().BeEmpty();
    }

    [Fact]
    public void HasUpcasterFor_ReturnsTrueWhenExists()
    {
        // Arrange
        var registry = new EventUpcasterRegistry();
        registry.Register<OrderCreatedV1ToV2Upcaster>();

        // Act
        var result = registry.HasUpcasterFor(nameof(OrderCreatedV1));

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void HasUpcasterFor_ReturnsFalseWhenNotExists()
    {
        // Arrange
        var registry = new EventUpcasterRegistry();

        // Act
        var result = registry.HasUpcasterFor(nameof(OrderCreatedV1));

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void HasUpcasterFor_NullName_ThrowsArgumentNullException()
    {
        // Arrange
        var registry = new EventUpcasterRegistry();

        // Act
        var act = () => registry.HasUpcasterFor(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Count_ReturnsZeroWhenEmpty()
    {
        // Arrange
        var registry = new EventUpcasterRegistry();

        // Act
        var count = registry.Count;

        // Assert
        count.Should().Be(0);
    }

    [Fact]
    public void Count_ReturnsCorrectCount()
    {
        // Arrange
        var registry = new EventUpcasterRegistry();
        registry.Register<OrderCreatedV1ToV2Upcaster>();
        registry.Register<OrderCreatedV2ToV3Upcaster>();

        // Act
        var count = registry.Count;

        // Assert
        count.Should().Be(2);
    }

    [Fact]
    public void ScanAndRegister_RegistersUpcastersFromAssembly()
    {
        // Arrange
        var registry = new EventUpcasterRegistry();

        // Act
        var count = registry.ScanAndRegister(typeof(OrderCreatedV1ToV2Upcaster).Assembly);

        // Assert
        count.Should().BeGreaterThan(0);
        registry.Count.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ScanAndRegister_NullAssembly_ThrowsArgumentNullException()
    {
        // Arrange
        var registry = new EventUpcasterRegistry();

        // Act
        var act = () => registry.ScanAndRegister(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ScanAndRegister_SkipsDuplicates()
    {
        // Arrange
        var registry = new EventUpcasterRegistry();
        registry.Register<OrderCreatedV1ToV2Upcaster>();

        // Act - scan the same assembly again
        var count = registry.ScanAndRegister(typeof(OrderCreatedV1ToV2Upcaster).Assembly);

        // Assert - should not throw, but count might be lower
        count.Should().BeGreaterThanOrEqualTo(0);
    }
}
