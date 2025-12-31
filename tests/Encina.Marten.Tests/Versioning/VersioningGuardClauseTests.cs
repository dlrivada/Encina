using System.Reflection;

using Encina.Marten.Versioning;
using Shouldly;

namespace Encina.Marten.Tests.Versioning;

/// <summary>
/// Guard clause tests for event versioning components.
/// Verifies that all public methods properly validate their parameters.
/// </summary>
public sealed class VersioningGuardClauseTests
{
    #region EventUpcasterRegistry Guards

    [Fact]
    public void Registry_Register_NullUpcaster_Throws()
    {
        // Arrange
        var registry = new EventUpcasterRegistry();

        // Act
        var act = () => registry.Register(null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("upcaster");
    }

    [Fact]
    public void Registry_RegisterType_NullType_Throws()
    {
        // Arrange
        var registry = new EventUpcasterRegistry();

        // Act
        var act = () => registry.Register((Type)null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("upcasterType");
    }

    [Fact]
    public void Registry_TryRegister_NullUpcaster_Throws()
    {
        // Arrange
        var registry = new EventUpcasterRegistry();

        // Act
        Action act = () => _ = registry.TryRegister(null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("upcaster");
    }

    [Fact]
    public void Registry_GetUpcasterForEventType_NullName_Throws()
    {
        // Arrange
        var registry = new EventUpcasterRegistry();

        // Act
        var act = () => registry.GetUpcasterForEventType(null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("eventTypeName");
    }

    [Fact]
    public void Registry_HasUpcasterFor_NullName_Throws()
    {
        // Arrange
        var registry = new EventUpcasterRegistry();

        // Act
        Action act = () => _ = registry.HasUpcasterFor(null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("eventTypeName");
    }

    [Fact]
    public void Registry_ScanAndRegister_NullAssembly_Throws()
    {
        // Arrange
        var registry = new EventUpcasterRegistry();

        // Act
        Action act = () => _ = registry.ScanAndRegister(null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("assembly");
    }

    #endregion

    #region EventVersioningOptions Guards

    [Fact]
    public void Options_AddUpcasterType_NullType_Throws()
    {
        // Arrange
        var options = new EventVersioningOptions();

        // Act
        var act = () => options.AddUpcaster((Type)null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void Options_AddUpcasterInstance_NullUpcaster_Throws()
    {
        // Arrange
        var options = new EventVersioningOptions();

        // Act
        var act = () => options.AddUpcaster((IEventUpcaster)null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void Options_AddUpcasterLambda_NullFunc_Throws()
    {
        // Arrange
        var options = new EventVersioningOptions();

        // Act
        var act = () => options.AddUpcaster<OrderCreatedV1, OrderCreatedV2>(null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void Options_ScanAssembly_NullAssembly_Throws()
    {
        // Arrange
        var options = new EventVersioningOptions();

        // Act
        var act = () => options.ScanAssembly(null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void Options_ScanAssemblies_NullArray_Throws()
    {
        // Arrange
        var options = new EventVersioningOptions();

        // Act
        var act = () => options.ScanAssemblies(null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void Options_ApplyTo_NullRegistry_Throws()
    {
        // Arrange
        var options = new EventVersioningOptions();

        // Act
        var act = () => options.ApplyTo(null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldNotBeNullOrEmpty();
    }

    #endregion

    #region LambdaEventUpcaster Guards

    [Fact]
    public void LambdaUpcaster_Constructor_NullFunc_Throws()
    {
        // Arrange & Act
        var act = () => new LambdaEventUpcaster<OrderCreatedV1, OrderCreatedV2>(null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void LambdaUpcaster_Upcast_NullEvent_Throws()
    {
        // Arrange
        var upcaster = new LambdaEventUpcaster<OrderCreatedV1, OrderCreatedV2>(
            old => new OrderCreatedV2(old.OrderId, old.CustomerName, "test@example.com"));
        IEventUpcaster<OrderCreatedV1, OrderCreatedV2> typedUpcaster = upcaster;

        // Act
        var act = () => typedUpcaster.Upcast(null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldNotBeNullOrEmpty();
    }

    #endregion

    #region EventUpcasterBase Guards

    [Fact]
    public void EventUpcasterBase_Upcast_NullEvent_Throws()
    {
        // Arrange
        var upcaster = new OrderCreatedV1ToV2Upcaster();
        IEventUpcaster<OrderCreatedV1, OrderCreatedV2> typedUpcaster = upcaster;

        // Act
        var act = () => typedUpcaster.Upcast(null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldNotBeNullOrEmpty();
    }

    #endregion

    #region Invalid Type Guards

    [Fact]
    public void Registry_RegisterType_NonUpcasterType_ThrowsArgumentException()
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
    public void Options_AddUpcasterType_NonUpcasterType_ThrowsArgumentException()
    {
        // Arrange
        var options = new EventVersioningOptions();

        // Act
        var act = () => options.AddUpcaster(typeof(int));

        // Assert
        var ex = Should.Throw<ArgumentException>(act);
        ex.Message.ShouldMatch("*does not implement IEventUpcaster*");
    }

    #endregion
}
