using Encina.Marten.Versioning;
using System.Reflection;

namespace Encina.GuardTests.Marten.Versioning;

public class VersioningGuardTests
{
    #region LambdaEventUpcaster

    [Fact]
    public void LambdaEventUpcaster_NullFunc_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new LambdaEventUpcaster<OldEvent, NewEvent>(null!));

    [Fact]
    public void LambdaEventUpcaster_Upcast_NullEvent_Throws()
    {
        IEventUpcaster<OldEvent, NewEvent> upcaster = new LambdaEventUpcaster<OldEvent, NewEvent>(e => new NewEvent { Name = e.Name });
        Should.Throw<ArgumentNullException>(() => upcaster.Upcast(null!));
    }

    #endregion

    #region EventUpcasterRegistry

    [Fact]
    public void EventUpcasterRegistry_Register_NullUpcaster_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new EventUpcasterRegistry().Register((IEventUpcaster)null!));

    [Fact]
    public void EventUpcasterRegistry_Register_NullType_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new EventUpcasterRegistry().Register(null!));

    [Fact]
    public void EventUpcasterRegistry_TryRegister_NullUpcaster_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new EventUpcasterRegistry().TryRegister(null!));

    [Fact]
    public void EventUpcasterRegistry_GetUpcasterForEventType_NullName_Throws()
        => Should.Throw<ArgumentException>(() =>
            new EventUpcasterRegistry().GetUpcasterForEventType(null!));

    [Fact]
    public void EventUpcasterRegistry_GetUpcasterForEventType_EmptyName_ReturnsNull()
    {
        var result = new EventUpcasterRegistry().GetUpcasterForEventType("");
        result.ShouldBeNull();
    }

    [Fact]
    public void EventUpcasterRegistry_HasUpcasterFor_NullName_Throws()
        => Should.Throw<ArgumentException>(() =>
            new EventUpcasterRegistry().HasUpcasterFor(null!));

    [Fact]
    public void EventUpcasterRegistry_HasUpcasterFor_EmptyName_ReturnsFalse()
    {
        var result = new EventUpcasterRegistry().HasUpcasterFor("");
        result.ShouldBeFalse();
    }

    [Fact]
    public void EventUpcasterRegistry_ScanAndRegister_NullAssembly_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new EventUpcasterRegistry().ScanAndRegister(null!));

    #endregion

    public record OldEvent { public string Name { get; init; } = ""; }
    public record NewEvent { public string Name { get; init; } = ""; public int Version { get; init; } = 2; }
}
