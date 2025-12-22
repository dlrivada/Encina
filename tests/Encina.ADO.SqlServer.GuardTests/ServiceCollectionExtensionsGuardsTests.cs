using Microsoft.Extensions.DependencyInjection;
using Encina.Messaging;

namespace Encina.ADO.SqlServer.GuardTests;

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
        var act = () => ServiceCollectionExtensions.AddEncinaADO(services, configure);
        act.Should().Throw<ArgumentNullException>().WithParameterName("services");
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
        var act = () => ServiceCollectionExtensions.AddEncinaADO(services, configure);
        act.Should().Throw<ArgumentNullException>().WithParameterName("configure");
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
        var act = () => ServiceCollectionExtensions.AddEncinaADO(services, connectionString, configure);
        act.Should().Throw<ArgumentNullException>().WithParameterName("services");
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
        var act = () => ServiceCollectionExtensions.AddEncinaADO(services, connectionString, configure);
        act.Should().Throw<ArgumentNullException>().WithParameterName("connectionString");
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
        var act = () => ServiceCollectionExtensions.AddEncinaADO(services, connectionString, configure);
        act.Should().Throw<ArgumentNullException>().WithParameterName("configure");
    }

    /// <summary>
    /// Verifies that AddEncinaADO with factory throws ArgumentNullException when services is null.
    /// </summary>
    [Fact]
    public void AddEncinaADO_WithFactory_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;
        Func<IServiceProvider, System.Data.IDbConnection> connectionFactory = _ => Substitute.For<System.Data.IDbConnection>();
        Action<MessagingConfiguration> configure = _ => { };

        // Act & Assert
        var act = () => ServiceCollectionExtensions.AddEncinaADO(services, connectionFactory, configure);
        act.Should().Throw<ArgumentNullException>().WithParameterName("services");
    }

    /// <summary>
    /// Verifies that AddEncinaADO with factory throws ArgumentNullException when connectionFactory is null.
    /// </summary>
    [Fact]
    public void AddEncinaADO_WithFactory_NullConnectionFactory_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        Func<IServiceProvider, System.Data.IDbConnection> connectionFactory = null!;
        Action<MessagingConfiguration> configure = _ => { };

        // Act & Assert
        var act = () => ServiceCollectionExtensions.AddEncinaADO(services, connectionFactory, configure);
        act.Should().Throw<ArgumentNullException>().WithParameterName("connectionFactory");
    }

    /// <summary>
    /// Verifies that AddEncinaADO with factory throws ArgumentNullException when configure is null.
    /// </summary>
    [Fact]
    public void AddEncinaADO_WithFactory_NullConfigure_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        Func<IServiceProvider, System.Data.IDbConnection> connectionFactory = _ => Substitute.For<System.Data.IDbConnection>();
        Action<MessagingConfiguration> configure = null!;

        // Act & Assert
        var act = () => ServiceCollectionExtensions.AddEncinaADO(services, connectionFactory, configure);
        act.Should().Throw<ArgumentNullException>().WithParameterName("configure");
    }
}
