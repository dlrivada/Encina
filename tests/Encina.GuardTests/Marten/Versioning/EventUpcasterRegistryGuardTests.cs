using System.Reflection;
using Encina.Marten.Versioning;

namespace Encina.GuardTests.Marten.Versioning;

/// <summary>
/// Guard tests for <see cref="EventUpcasterRegistry"/> covering null checks,
/// duplicate registration detection, and type validation.
/// </summary>
public class EventUpcasterRegistryGuardTests
{
    #region Register(IEventUpcaster) Guards

    [Fact]
    public void Register_NullUpcaster_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new EventUpcasterRegistry().Register((IEventUpcaster)null!));

    [Fact]
    public void Register_DuplicateSourceEventType_ThrowsInvalidOperation()
    {
        var registry = new EventUpcasterRegistry();
        var upcaster = new TestUpcaster("EventV1");
        registry.Register(upcaster);

        Should.Throw<InvalidOperationException>(() =>
            registry.Register(new TestUpcaster("EventV1")));
    }

    #endregion

    #region Register(Type, Factory) Guards

    [Fact]
    public void Register_NullType_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new EventUpcasterRegistry().Register((Type)null!));

    [Fact]
    public void Register_TypeNotImplementingIEventUpcaster_ThrowsArgument()
        => Should.Throw<ArgumentException>(() =>
            new EventUpcasterRegistry().Register(typeof(string)));

    #endregion

    #region TryRegister Guards

    [Fact]
    public void TryRegister_NullUpcaster_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new EventUpcasterRegistry().TryRegister(null!));

    [Fact]
    public void TryRegister_DuplicateSourceEventType_ReturnsFalse()
    {
        var registry = new EventUpcasterRegistry();
        var upcaster1 = new TestUpcaster("EventV1");
        var upcaster2 = new TestUpcaster("EventV1");

        registry.TryRegister(upcaster1).ShouldBeTrue();
        registry.TryRegister(upcaster2).ShouldBeFalse();
    }

    [Fact]
    public void TryRegister_UniqueSourceEventType_ReturnsTrue()
    {
        var registry = new EventUpcasterRegistry();
        registry.TryRegister(new TestUpcaster("EventV1")).ShouldBeTrue();
        registry.TryRegister(new TestUpcaster("EventV2")).ShouldBeTrue();
    }

    #endregion

    #region GetUpcasterForEventType Guards

    [Fact]
    public void GetUpcasterForEventType_NullEventTypeName_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new EventUpcasterRegistry().GetUpcasterForEventType(null!));

    [Fact]
    public void GetUpcasterForEventType_UnknownType_ReturnsNull()
    {
        var registry = new EventUpcasterRegistry();
        registry.GetUpcasterForEventType("NonExistentType").ShouldBeNull();
    }

    #endregion

    #region HasUpcasterFor Guards

    [Fact]
    public void HasUpcasterFor_NullEventTypeName_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new EventUpcasterRegistry().HasUpcasterFor(null!));

    [Fact]
    public void HasUpcasterFor_UnknownType_ReturnsFalse()
    {
        var registry = new EventUpcasterRegistry();
        registry.HasUpcasterFor("NonExistent").ShouldBeFalse();
    }

    [Fact]
    public void HasUpcasterFor_RegisteredType_ReturnsTrue()
    {
        var registry = new EventUpcasterRegistry();
        registry.Register(new TestUpcaster("MyEvent"));
        registry.HasUpcasterFor("MyEvent").ShouldBeTrue();
    }

    #endregion

    #region ScanAndRegister Guards

    [Fact]
    public void ScanAndRegister_NullAssembly_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new EventUpcasterRegistry().ScanAndRegister(null!));

    #endregion

    #region Count

    [Fact]
    public void Count_EmptyRegistry_IsZero()
    {
        var registry = new EventUpcasterRegistry();
        registry.Count.ShouldBe(0);
    }

    [Fact]
    public void Count_AfterRegistrations_ReflectsCount()
    {
        var registry = new EventUpcasterRegistry();
        registry.Register(new TestUpcaster("A"));
        registry.Register(new TestUpcaster("B"));
        registry.Count.ShouldBe(2);
    }

    #endregion

    private sealed class TestUpcaster : IEventUpcaster
    {
        public TestUpcaster() : this("Default_v1") { }

        public TestUpcaster(string sourceEventTypeName)
        {
            SourceEventTypeName = sourceEventTypeName;
        }

        public string SourceEventTypeName { get; }
        public Type TargetEventType => typeof(object);
        public Type SourceEventType => typeof(string);
    }
}
