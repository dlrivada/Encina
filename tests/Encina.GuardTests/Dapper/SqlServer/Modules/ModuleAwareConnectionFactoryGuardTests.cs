using System.Data;
using System.Data.Common;
using Encina.Dapper.SqlServer.Modules;
using Encina.Modules.Isolation;
using NSubstitute;
using Shouldly;

namespace Encina.GuardTests.Dapper.SqlServer.Modules;

/// <summary>
/// Guard tests for <see cref="ModuleAwareConnectionFactory"/> to verify null parameter handling.
/// </summary>
public class ModuleAwareConnectionFactoryGuardTests
{
    [Fact]
    public void Constructor_NullInnerConnectionFactory_ThrowsArgumentNullException()
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

    [Fact]
    public void CreateConnection_NonDevelopmentMode_ReturnUnwrappedConnection()
    {
        // Arrange
        var innerConnection = Substitute.For<IDbConnection>();
        Func<IDbConnection> factory = () => innerConnection;
        var moduleContext = Substitute.For<IModuleExecutionContext>();
        var schemaRegistry = Substitute.For<IModuleSchemaRegistry>();
        var options = new ModuleIsolationOptions { Strategy = ModuleIsolationStrategy.SchemaWithPermissions };

        var sut = new ModuleAwareConnectionFactory(factory, moduleContext, schemaRegistry, options);

        // Act
        var connection = sut.CreateConnection();

        // Assert
        connection.ShouldBe(innerConnection, "Non-development mode should return unwrapped connection");
    }

    [Fact]
    public void CreateConnection_DevelopmentMode_NonDbConnection_ReturnsUnwrapped()
    {
        // Arrange — when inner connection is not DbConnection, it's returned as-is (no wrapping)
        var innerConnection = Substitute.For<IDbConnection>();
        Func<IDbConnection> factory = () => innerConnection;
        var moduleContext = Substitute.For<IModuleExecutionContext>();
        var schemaRegistry = Substitute.For<IModuleSchemaRegistry>();
        var options = new ModuleIsolationOptions { Strategy = ModuleIsolationStrategy.DevelopmentValidationOnly };

        var sut = new ModuleAwareConnectionFactory(factory, moduleContext, schemaRegistry, options);

        // Act
        var connection = sut.CreateConnection();

        // Assert — returned as-is since it's not a DbConnection
        connection.ShouldBe(innerConnection);
    }
}
