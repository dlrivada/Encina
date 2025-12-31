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
        registry.Count.ShouldBe(1);
        registry.HasUpcasterFor(nameof(OrderCreatedV1)).ShouldBeTrue();
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
        registry.Count.ShouldBe(1);
    }

    [Fact]
    public void Register_NullInstance_ThrowsArgumentNullException()
    {
        // Arrange
        var registry = new EventUpcasterRegistry();

        // Act
        var act = () => registry.Register(null!);

        // Assert
        Should.Throw<ArgumentNullException>(act);
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
        var ex = Should.Throw<InvalidOperationException>(act);
        ex.Message.ShouldMatch("*already registered*");
    }

    [Fact]
    public void Register_ByType_AddsUpcaster()
    {
        // Arrange
        var registry = new EventUpcasterRegistry();

        // Act
        registry.Register(typeof(OrderCreatedV1ToV2Upcaster));

        // Assert
        registry.Count.ShouldBe(1);
    }

    [Fact]
    public void Register_ByType_NullType_ThrowsArgumentNullException()
    {
        // Arrange
        var registry = new EventUpcasterRegistry();

        // Act
        var act = () => registry.Register((Type)null!);

        // Assert
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void Register_ByType_NonUpcasterType_ThrowsArgumentException()
    {
        // Arrange
        var registry = new EventUpcasterRegistry();

        // Act
        var act = () => registry.Register(typeof(string));

        // Assert
        var ex = Should.Throw<ArgumentException>(act);
        ex.Message.ShouldMatch("*does not implement IEventUpcaster*");
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
        result.ShouldBeTrue();
        registry.Count.ShouldBe(1);
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
        result.ShouldBeFalse();
        registry.Count.ShouldBe(1);
    }

    [Fact]
    public void TryRegister_NullUpcaster_ThrowsArgumentNullException()
    {
        // Arrange
        var registry = new EventUpcasterRegistry();

        // Act
        Action act = () => _ = registry.TryRegister(null!);

        // Assert
        Should.Throw<ArgumentNullException>(act);
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
        upcaster.ShouldNotBeNull();
        upcaster.ShouldBeOfType<OrderCreatedV1ToV2Upcaster>();
    }

    [Fact]
    public void GetUpcasterForEventType_ReturnsNullForUnknownType()
    {
        // Arrange
        var registry = new EventUpcasterRegistry();

        // Act
        var upcaster = registry.GetUpcasterForEventType("UnknownEvent");

        // Assert
        upcaster.ShouldBeNull();
    }

    [Fact]
    public void GetUpcasterForEventType_NullName_ThrowsArgumentNullException()
    {
        // Arrange
        var registry = new EventUpcasterRegistry();

        // Act
        var act = () => registry.GetUpcasterForEventType(null!);

        // Assert
        Should.Throw<ArgumentNullException>(act);
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
        upcasters.Count.ShouldBe(2);
    }

    [Fact]
    public void GetAllUpcasters_ReturnsEmptyListWhenEmpty()
    {
        // Arrange
        var registry = new EventUpcasterRegistry();

        // Act
        var upcasters = registry.GetAllUpcasters();

        // Assert
        upcasters.ShouldBeEmpty();
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
        result.ShouldBeTrue();
    }

    [Fact]
    public void HasUpcasterFor_ReturnsFalseWhenNotExists()
    {
        // Arrange
        var registry = new EventUpcasterRegistry();

        // Act
        var result = registry.HasUpcasterFor(nameof(OrderCreatedV1));

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void HasUpcasterFor_NullName_ThrowsArgumentNullException()
    {
        // Arrange
        var registry = new EventUpcasterRegistry();

        // Act
        Action act = () => _ = registry.HasUpcasterFor(null!);

        // Assert
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void Count_ReturnsZeroWhenEmpty()
    {
        // Arrange
        var registry = new EventUpcasterRegistry();

        // Act
        var count = registry.Count;

        // Assert
        count.ShouldBe(0);
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
        count.ShouldBe(2);
    }

    [Fact]
    public void ScanAndRegister_RegistersUpcastersFromAssembly()
    {
        // Arrange
        var registry = new EventUpcasterRegistry();

        // Act
        var count = registry.ScanAndRegister(typeof(OrderCreatedV1ToV2Upcaster).Assembly);

        // Assert
        count.ShouldBeGreaterThan(0);
        registry.Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void ScanAndRegister_NullAssembly_ThrowsArgumentNullException()
    {
        // Arrange
        var registry = new EventUpcasterRegistry();

        // Act
        Action act = () => _ = registry.ScanAndRegister(null!);

        // Assert
        Should.Throw<ArgumentNullException>(act);
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
        count.ShouldBeGreaterThanOrEqualTo(0);
    }
}
