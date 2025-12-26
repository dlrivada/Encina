using Encina.Marten.Versioning;
using System.Reflection;

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
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Registry_RegisterType_NullType_Throws()
    {
        // Arrange
        var registry = new EventUpcasterRegistry();

        // Act
        var act = () => registry.Register((Type)null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Registry_TryRegister_NullUpcaster_Throws()
    {
        // Arrange
        var registry = new EventUpcasterRegistry();

        // Act
        var act = () => registry.TryRegister(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Registry_GetUpcasterForEventType_NullName_Throws()
    {
        // Arrange
        var registry = new EventUpcasterRegistry();

        // Act
        var act = () => registry.GetUpcasterForEventType(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Registry_HasUpcasterFor_NullName_Throws()
    {
        // Arrange
        var registry = new EventUpcasterRegistry();

        // Act
        var act = () => registry.HasUpcasterFor(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Registry_ScanAndRegister_NullAssembly_Throws()
    {
        // Arrange
        var registry = new EventUpcasterRegistry();

        // Act
        var act = () => registry.ScanAndRegister(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().NotBeNullOrEmpty();
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
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Options_AddUpcasterInstance_NullUpcaster_Throws()
    {
        // Arrange
        var options = new EventVersioningOptions();

        // Act
        var act = () => options.AddUpcaster((IEventUpcaster)null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Options_AddUpcasterLambda_NullFunc_Throws()
    {
        // Arrange
        var options = new EventVersioningOptions();

        // Act
        var act = () => options.AddUpcaster<OrderCreatedV1, OrderCreatedV2>(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Options_ScanAssembly_NullAssembly_Throws()
    {
        // Arrange
        var options = new EventVersioningOptions();

        // Act
        var act = () => options.ScanAssembly(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Options_ScanAssemblies_NullArray_Throws()
    {
        // Arrange
        var options = new EventVersioningOptions();

        // Act
        var act = () => options.ScanAssemblies(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Options_ApplyTo_NullRegistry_Throws()
    {
        // Arrange
        var options = new EventVersioningOptions();

        // Act
        var act = () => options.ApplyTo(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region LambdaEventUpcaster Guards

    [Fact]
    public void LambdaUpcaster_Constructor_NullFunc_Throws()
    {
        // Arrange & Act
        var act = () => new LambdaEventUpcaster<OrderCreatedV1, OrderCreatedV2>(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().NotBeNullOrEmpty();
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
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().NotBeNullOrEmpty();
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
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().NotBeNullOrEmpty();
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
        act.Should().Throw<ArgumentException>()
            .WithMessage("*does not implement IEventUpcaster*");
    }

    [Fact]
    public void Options_AddUpcasterType_NonUpcasterType_ThrowsArgumentException()
    {
        // Arrange
        var options = new EventVersioningOptions();

        // Act
        var act = () => options.AddUpcaster(typeof(int));

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*does not implement IEventUpcaster*");
    }

    #endregion
}
