using System.Data;
using Encina.ADO.SqlServer;
using Encina.Messaging;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;

namespace Encina.GuardTests.ADO.SqlServer;

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
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe(nameof(services));
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
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe(nameof(configure));
    }

    /// <summary>
    /// Verifies that AddEncinaADO with connection string throws ArgumentNullException when services is null.
    /// </summary>
    [Fact]
    public void AddEncinaADO_WithConnectionString_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;
        var connectionString = "Server=localhost;Database=test;";
        Action<MessagingConfiguration> configure = _ => { };

        // Act & Assert
        var act = () => services.AddEncinaADO(connectionString, configure);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe(nameof(services));
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
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe(nameof(connectionString));
    }

    /// <summary>
    /// Verifies that AddEncinaADO with connection string throws ArgumentNullException when configure is null.
    /// </summary>
    [Fact]
    public void AddEncinaADO_WithConnectionString_NullConfigure_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        var connectionString = "Server=localhost;Database=test;";
        Action<MessagingConfiguration> configure = null!;

        // Act & Assert
        var act = () => services.AddEncinaADO(connectionString, configure);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe(nameof(configure));
    }

    /// <summary>
    /// Verifies that AddEncinaADO with connection factory throws ArgumentNullException when services is null.
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
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe(nameof(services));
    }

    /// <summary>
    /// Verifies that AddEncinaADO with connection factory throws ArgumentNullException when connectionFactory is null.
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
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe(nameof(connectionFactory));
    }

    /// <summary>
    /// Verifies that AddEncinaADO with connection factory throws ArgumentNullException when configure is null.
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
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe(nameof(configure));
    }
}
