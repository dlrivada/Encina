using System.Data;
using Encina.Dapper.MySQL.Modules;
using Encina.Modules.Isolation;
using NSubstitute;
using Shouldly;

namespace Encina.GuardTests.Dapper.MySQL.Modules;

/// <summary>
/// Guard tests for <see cref="ModuleAwareConnectionFactory"/> to verify null parameter handling.
/// </summary>
public class ModuleAwareConnectionFactoryGuardTests
{
    [Fact]
    public void Constructor_NullInnerFactory_ThrowsArgumentNullException()
    {
        // Arrange
        var moduleContext = Substitute.For<IModuleExecutionContext>();
        var schemaRegistry = Substitute.For<IModuleSchemaRegistry>();
        var options = new ModuleIsolationOptions();

        // Act & Assert
        var act = () => new ModuleAwareConnectionFactory(null!, moduleContext, schemaRegistry, options);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("innerFactory");
    }

    [Fact]
    public void Constructor_NullModuleContext_ThrowsArgumentNullException()
    {
        // Arrange
        Func<IDbConnection> factory = () => Substitute.For<IDbConnection>();
        var schemaRegistry = Substitute.For<IModuleSchemaRegistry>();
        var options = new ModuleIsolationOptions();

        // Act & Assert
        var act = () => new ModuleAwareConnectionFactory(factory, null!, schemaRegistry, options);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("moduleContext");
    }

    [Fact]
    public void Constructor_NullSchemaRegistry_ThrowsArgumentNullException()
    {
        // Arrange
        Func<IDbConnection> factory = () => Substitute.For<IDbConnection>();
        var moduleContext = Substitute.For<IModuleExecutionContext>();
        var options = new ModuleIsolationOptions();

        // Act & Assert
        var act = () => new ModuleAwareConnectionFactory(factory, moduleContext, null!, options);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("schemaRegistry");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        Func<IDbConnection> factory = () => Substitute.For<IDbConnection>();
        var moduleContext = Substitute.For<IModuleExecutionContext>();
        var schemaRegistry = Substitute.For<IModuleSchemaRegistry>();

        // Act & Assert
        var act = () => new ModuleAwareConnectionFactory(factory, moduleContext, schemaRegistry, null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_ValidParameters_DoesNotThrow()
    {
        // Arrange
        Func<IDbConnection> factory = () => Substitute.For<IDbConnection>();
        var moduleContext = Substitute.For<IModuleExecutionContext>();
        var schemaRegistry = Substitute.For<IModuleSchemaRegistry>();
        var options = new ModuleIsolationOptions();

        // Act & Assert
        Should.NotThrow(() => new ModuleAwareConnectionFactory(factory, moduleContext, schemaRegistry, options));
    }
}
