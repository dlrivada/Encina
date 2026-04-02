using System.Data.Common;
using Encina.ADO.PostgreSQL.Modules;
using Encina.Modules.Isolation;
using NSubstitute;
using Shouldly;

namespace Encina.GuardTests.ADO.PostgreSQL.Modules;

/// <summary>
/// Guard tests for <see cref="SchemaValidatingConnection"/> to verify null parameter handling.
/// </summary>
public class SchemaValidatingConnectionGuardTests
{
    [Fact]
    public void Constructor_NullInnerConnection_ThrowsArgumentNullException()
    {
        // Arrange
        var moduleContext = Substitute.For<IModuleExecutionContext>();
        var schemaRegistry = Substitute.For<IModuleSchemaRegistry>();
        var options = new ModuleIsolationOptions();

        // Act & Assert
        var act = () => new SchemaValidatingConnection(null!, moduleContext, schemaRegistry, options);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("innerConnection");
    }

    [Fact]
    public void Constructor_NullModuleContext_ThrowsArgumentNullException()
    {
        // Arrange
        var innerConnection = Substitute.For<DbConnection>();
        var schemaRegistry = Substitute.For<IModuleSchemaRegistry>();
        var options = new ModuleIsolationOptions();

        // Act & Assert
        var act = () => new SchemaValidatingConnection(innerConnection, null!, schemaRegistry, options);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("moduleContext");
    }

    [Fact]
    public void Constructor_NullSchemaRegistry_ThrowsArgumentNullException()
    {
        // Arrange
        var innerConnection = Substitute.For<DbConnection>();
        var moduleContext = Substitute.For<IModuleExecutionContext>();
        var options = new ModuleIsolationOptions();

        // Act & Assert
        var act = () => new SchemaValidatingConnection(innerConnection, moduleContext, null!, options);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("schemaRegistry");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var innerConnection = Substitute.For<DbConnection>();
        var moduleContext = Substitute.For<IModuleExecutionContext>();
        var schemaRegistry = Substitute.For<IModuleSchemaRegistry>();

        // Act & Assert
        var act = () => new SchemaValidatingConnection(innerConnection, moduleContext, schemaRegistry, null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_ValidParameters_DoesNotThrow()
    {
        // Arrange
        var innerConnection = Substitute.For<DbConnection>();
        var moduleContext = Substitute.For<IModuleExecutionContext>();
        var schemaRegistry = Substitute.For<IModuleSchemaRegistry>();
        var options = new ModuleIsolationOptions();

        // Act & Assert
        Should.NotThrow(() => new SchemaValidatingConnection(innerConnection, moduleContext, schemaRegistry, options));
    }
}
