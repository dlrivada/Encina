using Encina.Marten.Versioning;
using Shouldly;

namespace Encina.UnitTests.Marten.Versioning;

public class EventUpcasterRegistryTests
{
    // Register<TUpcaster> tests

    [Fact]
    public void RegisterGeneric_ValidUpcaster_AddsToRegistry()
    {
        // Arrange
        var registry = new EventUpcasterRegistry();

        // Act
        registry.Register<TestUpcaster>();

        // Assert
        registry.Count.ShouldBe(1);
        registry.HasUpcasterFor("OldEvent").ShouldBeTrue();
    }

    [Fact]
    public void RegisterGeneric_DuplicateSourceType_ThrowsInvalidOperationException()
    {
        // Arrange
        var registry = new EventUpcasterRegistry();
        registry.Register<TestUpcaster>();

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => registry.Register<TestUpcaster>());
    }

    // Register(IEventUpcaster) tests

    [Fact]
    public void RegisterInstance_NullUpcaster_ThrowsArgumentNullException()
    {
        var registry = new EventUpcasterRegistry();
        var act = () => registry.Register((IEventUpcaster)null!);
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void RegisterInstance_ValidUpcaster_AddsToRegistry()
    {
        // Arrange
        var registry = new EventUpcasterRegistry();
        var upcaster = new TestUpcaster();

        // Act
        registry.Register(upcaster);

        // Assert
        registry.Count.ShouldBe(1);
    }

    [Fact]
    public void RegisterInstance_DuplicateSourceType_ThrowsInvalidOperationException()
    {
        // Arrange
        var registry = new EventUpcasterRegistry();
        registry.Register(new TestUpcaster());

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => registry.Register(new TestUpcaster()));
    }

    // Register(Type) tests

    [Fact]
    public void RegisterByType_NullType_ThrowsArgumentNullException()
    {
        var registry = new EventUpcasterRegistry();
        var act = () => registry.Register((Type)null!);
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void RegisterByType_NonUpcasterType_ThrowsArgumentException()
    {
        var registry = new EventUpcasterRegistry();
        var act = () => registry.Register(typeof(string));
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void RegisterByType_ValidType_AddsToRegistry()
    {
        // Arrange
        var registry = new EventUpcasterRegistry();

        // Act
        registry.Register(typeof(TestUpcaster));

        // Assert
        registry.Count.ShouldBe(1);
    }

    [Fact]
    public void RegisterByType_WithFactory_UsesFactory()
    {
        // Arrange
        var registry = new EventUpcasterRegistry();
        var factoryCalled = false;

        // Act
        registry.Register(typeof(TestUpcaster), _ =>
        {
            factoryCalled = true;
            return new TestUpcaster();
        });

        // Assert
        factoryCalled.ShouldBeTrue();
        registry.Count.ShouldBe(1);
    }

    // TryRegister tests

    [Fact]
    public void TryRegister_NullUpcaster_ThrowsArgumentNullException()
    {
        var registry = new EventUpcasterRegistry();
        Should.Throw<ArgumentNullException>(() => { registry.TryRegister(null!); });
    }

    [Fact]
    public void TryRegister_NewUpcaster_ReturnsTrue()
    {
        // Arrange
        var registry = new EventUpcasterRegistry();

        // Act
        var result = registry.TryRegister(new TestUpcaster());

        // Assert
        result.ShouldBeTrue();
        registry.Count.ShouldBe(1);
    }

    [Fact]
    public void TryRegister_DuplicateSourceType_ReturnsFalse()
    {
        // Arrange
        var registry = new EventUpcasterRegistry();
        registry.Register(new TestUpcaster());

        // Act
        var result = registry.TryRegister(new TestUpcaster());

        // Assert
        result.ShouldBeFalse();
        registry.Count.ShouldBe(1);
    }

    // GetUpcasterForEventType tests

    [Fact]
    public void GetUpcasterForEventType_NullTypeName_ThrowsArgumentNullException()
    {
        var registry = new EventUpcasterRegistry();
        var act = () => registry.GetUpcasterForEventType(null!);
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void GetUpcasterForEventType_RegisteredType_ReturnsUpcaster()
    {
        // Arrange
        var registry = new EventUpcasterRegistry();
        registry.Register<TestUpcaster>();

        // Act
        var result = registry.GetUpcasterForEventType("OldEvent");

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<TestUpcaster>();
    }

    [Fact]
    public void GetUpcasterForEventType_UnregisteredType_ReturnsNull()
    {
        // Arrange
        var registry = new EventUpcasterRegistry();

        // Act
        var result = registry.GetUpcasterForEventType("NonExistentEvent");

        // Assert
        result.ShouldBeNull();
    }

    // GetAllUpcasters tests

    [Fact]
    public void GetAllUpcasters_EmptyRegistry_ReturnsEmpty()
    {
        var registry = new EventUpcasterRegistry();
        registry.GetAllUpcasters().Count.ShouldBe(0);
    }

    [Fact]
    public void GetAllUpcasters_WithRegistrations_ReturnsAll()
    {
        // Arrange
        var registry = new EventUpcasterRegistry();
        registry.Register<TestUpcaster>();
        registry.Register<TestUpcaster2>();

        // Act
        var all = registry.GetAllUpcasters();

        // Assert
        all.Count.ShouldBe(2);
    }

    // HasUpcasterFor tests

    [Fact]
    public void HasUpcasterFor_NullTypeName_ThrowsArgumentNullException()
    {
        var registry = new EventUpcasterRegistry();
        Should.Throw<ArgumentNullException>(() => { registry.HasUpcasterFor(null!); });
    }

    [Fact]
    public void HasUpcasterFor_RegisteredType_ReturnsTrue()
    {
        var registry = new EventUpcasterRegistry();
        registry.Register<TestUpcaster>();
        registry.HasUpcasterFor("OldEvent").ShouldBeTrue();
    }

    [Fact]
    public void HasUpcasterFor_UnregisteredType_ReturnsFalse()
    {
        var registry = new EventUpcasterRegistry();
        registry.HasUpcasterFor("NonExistent").ShouldBeFalse();
    }

    // Count tests

    [Fact]
    public void Count_EmptyRegistry_ReturnsZero()
    {
        var registry = new EventUpcasterRegistry();
        registry.Count.ShouldBe(0);
    }

    [Fact]
    public void Count_WithRegistrations_ReturnsCorrectCount()
    {
        var registry = new EventUpcasterRegistry();
        registry.Register<TestUpcaster>();
        registry.Register<TestUpcaster2>();
        registry.Count.ShouldBe(2);
    }

    // ScanAndRegister tests

    [Fact]
    public void ScanAndRegister_NullAssembly_ThrowsArgumentNullException()
    {
        var registry = new EventUpcasterRegistry();
        Should.Throw<ArgumentNullException>(() => { registry.ScanAndRegister(null!); });
    }

    [Fact]
    public void ScanAndRegister_CurrentAssembly_FindsUpcasters()
    {
        // Arrange
        var registry = new EventUpcasterRegistry();

        // Act
        var count = registry.ScanAndRegister(typeof(EventUpcasterRegistryTests).Assembly);

        // Assert
        count.ShouldBeGreaterThanOrEqualTo(2); // At least TestUpcaster and TestUpcaster2
    }

    [Fact]
    public void ScanAndRegister_WithFactory_UsesFactory()
    {
        // Arrange
        var registry = new EventUpcasterRegistry();
        var factoryCallCount = 0;

        // Act
        registry.ScanAndRegister(typeof(EventUpcasterRegistryTests).Assembly, type =>
        {
            factoryCallCount++;
            return (IEventUpcaster)Activator.CreateInstance(type)!;
        });

        // Assert
        factoryCallCount.ShouldBeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public void ScanAndRegister_DuplicateRegistrations_SkipsDuplicates()
    {
        // Arrange
        var registry = new EventUpcasterRegistry();
        registry.Register<TestUpcaster>(); // Pre-register

        // Act
        var count = registry.ScanAndRegister(typeof(EventUpcasterRegistryTests).Assembly);

        // Assert - TestUpcaster should be skipped, TestUpcaster2 added
        count.ShouldBeGreaterThanOrEqualTo(1);
        registry.HasUpcasterFor("OldEvent").ShouldBeTrue();
        registry.HasUpcasterFor("AnotherOldEvent").ShouldBeTrue();
    }

    // Clear tests

    [Fact]
    public void Clear_WithRegistrations_RemovesAll()
    {
        // Arrange
        var registry = new EventUpcasterRegistry();
        registry.Register<TestUpcaster>();
        registry.Register<TestUpcaster2>();
        registry.Count.ShouldBe(2);

        // Act
        registry.Clear();

        // Assert
        registry.Count.ShouldBe(0);
        registry.GetAllUpcasters().Count.ShouldBe(0);
    }

    // Test upcasters (must be public for assembly scanning, and have parameterless constructor)

    public sealed record OldEvent(string Data);
    public sealed record NewEvent(string Data, string Extra);
    public sealed record AnotherOldEvent(int Value);
    public sealed record AnotherNewEvent(int Value, string Label);

    public sealed class TestUpcaster : IEventUpcaster
    {
        public string SourceEventTypeName => "OldEvent";
        public Type TargetEventType => typeof(NewEvent);
        public Type SourceEventType => typeof(OldEvent);
    }

    public sealed class TestUpcaster2 : IEventUpcaster
    {
        public string SourceEventTypeName => "AnotherOldEvent";
        public Type TargetEventType => typeof(AnotherNewEvent);
        public Type SourceEventType => typeof(AnotherOldEvent);
    }
}
