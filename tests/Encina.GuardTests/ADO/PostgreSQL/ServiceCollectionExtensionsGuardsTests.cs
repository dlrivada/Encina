using System.Data;
using Microsoft.Extensions.DependencyInjection;
using Encina.ADO.PostgreSQL;
using Encina.Messaging;

namespace Encina.GuardTests.ADO.PostgreSQL;

/// <summary>
/// Guard tests for <see cref="ServiceCollectionExtensions"/> to verify null parameter handling.
/// </summary>
public class ServiceCollectionExtensionsGuardsTests
{
    /// <summary>
    /// Verifies that AddEncinaADO throws ArgumentNullException when services is null.
    /// </summary>
    [Fact]
    public void AddEncinaADO_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;
        Action<MessagingConfiguration> configure = _ => { };

        // Act & Assert
        var act = () => services.AddEncinaADO(configure);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("services");
    }

    /// <summary>
    /// Verifies that AddEncinaADO throws ArgumentNullException when configure is null.
    /// </summary>
    [Fact]
    public void AddEncinaADO_NullConfigure_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        Action<MessagingConfiguration> configure = null!;

        // Act & Assert
        var act = () => services.AddEncinaADO(configure);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("configure");
    }

    /// <summary>
    /// Verifies that AddEncinaADO with connection string throws ArgumentNullException when services is null.
    /// </summary>
    [Fact]
    public void AddEncinaADO_WithConnectionString_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;
        var connectionString = "Host=localhost;Database=test;";
        Action<MessagingConfiguration> configure = _ => { };

        // Act & Assert
        var act = () => services.AddEncinaADO(connectionString, configure);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("services");
    }

    /// <summary>
    /// Verifies that AddEncinaADO with connection string throws ArgumentNullException when connectionString is null.
    /// </summary>
    [Fact]
    public void AddEncinaADO_WithConnectionString_NullConnectionString_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        string connectionString = null!;
        Action<MessagingConfiguration> configure = _ => { };

        // Act & Assert
        var act = () => services.AddEncinaADO(connectionString, configure);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("connectionString");
    }

    /// <summary>
    /// Verifies that AddEncinaADO with connection string throws ArgumentException when connectionString is empty.
    /// </summary>
    [Fact]
    public void AddEncinaADO_WithConnectionString_EmptyConnectionString_ThrowsArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();
        var connectionString = string.Empty;
        Action<MessagingConfiguration> configure = _ => { };

        // Act & Assert
        var act = () => services.AddEncinaADO(connectionString, configure);
        var ex = Should.Throw<ArgumentException>(act);
        ex.ParamName.ShouldBe("connectionString");
    }

    /// <summary>
    /// Verifies that AddEncinaADO with connection string throws ArgumentNullException when configure is null.
    /// </summary>
    [Fact]
    public void AddEncinaADO_WithConnectionString_NullConfigure_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        var connectionString = "Host=localhost;Database=test;";
        Action<MessagingConfiguration> configure = null!;

        // Act & Assert
        var act = () => services.AddEncinaADO(connectionString, configure);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("configure");
    }

    /// <summary>
    /// Verifies that AddEncinaADO with factory throws ArgumentNullException when services is null.
    /// </summary>
    [Fact]
    public void AddEncinaADO_WithFactory_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;
        Func<IServiceProvider, IDbConnection> connectionFactory = _ => Substitute.For<IDbConnection>();
        Action<MessagingConfiguration> configure = _ => { };

        // Act & Assert
        var act = () => services.AddEncinaADO(connectionFactory, configure);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("services");
    }

    /// <summary>
    /// Verifies that AddEncinaADO with factory throws ArgumentNullException when connectionFactory is null.
    /// </summary>
    [Fact]
    public void AddEncinaADO_WithFactory_NullConnectionFactory_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        Func<IServiceProvider, IDbConnection> connectionFactory = null!;
        Action<MessagingConfiguration> configure = _ => { };

        // Act & Assert
        var act = () => services.AddEncinaADO(connectionFactory, configure);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("connectionFactory");
    }

    /// <summary>
    /// Verifies that AddEncinaADO with factory throws ArgumentNullException when configure is null.
    /// </summary>
    [Fact]
    public void AddEncinaADO_WithFactory_NullConfigure_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        Func<IServiceProvider, IDbConnection> connectionFactory = _ => Substitute.For<IDbConnection>();
        Action<MessagingConfiguration> configure = null!;

        // Act & Assert
        var act = () => services.AddEncinaADO(connectionFactory, configure);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("configure");
    }
}
