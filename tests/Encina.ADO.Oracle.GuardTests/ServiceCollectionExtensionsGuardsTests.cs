using System.Data;
using Microsoft.Extensions.DependencyInjection;
using Encina.Messaging;

namespace Encina.ADO.Oracle.GuardTests;

/// <summary>
/// Guard tests for <see cref="ServiceCollectionExtensions"/> to verify null parameter handling.
/// </summary>
public class ServiceCollectionExtensionsGuardsTests
{
    /// <summary>
    /// Verifies that AddEncinaADO throws ArgumentNullException when services is null (using configure).
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
    /// Verifies that AddEncinaADO throws ArgumentNullException when services is null (using connection string).
    /// </summary>
    [Fact]
    public void AddEncinaADO_ConnectionString_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;
        var connectionString = "Data Source=localhost;User Id=test;Password=test;";
        Action<MessagingConfiguration> configure = _ => { };

        // Act & Assert
        var act = () => ServiceCollectionExtensions.AddEncinaADO(services, connectionString, configure);
        act.Should().Throw<ArgumentNullException>().WithParameterName("services");
    }

    /// <summary>
    /// Verifies that AddEncinaADO throws ArgumentNullException when connectionString is null.
    /// </summary>
    [Fact]
    public void AddEncinaADO_NullConnectionString_ThrowsArgumentNullException()
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
    /// Verifies that AddEncinaADO throws ArgumentNullException when configure is null (using connection string).
    /// </summary>
    [Fact]
    public void AddEncinaADO_ConnectionString_NullConfigure_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        var connectionString = "Data Source=localhost;User Id=test;Password=test;";
        Action<MessagingConfiguration> configure = null!;

        // Act & Assert
        var act = () => ServiceCollectionExtensions.AddEncinaADO(services, connectionString, configure);
        act.Should().Throw<ArgumentNullException>().WithParameterName("configure");
    }

    /// <summary>
    /// Verifies that AddEncinaADO throws ArgumentNullException when services is null (using factory).
    /// </summary>
    [Fact]
    public void AddEncinaADO_Factory_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;
        Func<IServiceProvider, IDbConnection> factory = _ => Substitute.For<IDbConnection>();
        Action<MessagingConfiguration> configure = _ => { };

        // Act & Assert
        var act = () => ServiceCollectionExtensions.AddEncinaADO(services, factory, configure);
        act.Should().Throw<ArgumentNullException>().WithParameterName("services");
    }

    /// <summary>
    /// Verifies that AddEncinaADO throws ArgumentNullException when connectionFactory is null.
    /// </summary>
    [Fact]
    public void AddEncinaADO_NullConnectionFactory_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        Func<IServiceProvider, IDbConnection> factory = null!;
        Action<MessagingConfiguration> configure = _ => { };

        // Act & Assert
        var act = () => ServiceCollectionExtensions.AddEncinaADO(services, factory, configure);
        act.Should().Throw<ArgumentNullException>().WithParameterName("connectionFactory");
    }

    /// <summary>
    /// Verifies that AddEncinaADO throws ArgumentNullException when configure is null (using factory).
    /// </summary>
    [Fact]
    public void AddEncinaADO_Factory_NullConfigure_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        Func<IServiceProvider, IDbConnection> factory = _ => Substitute.For<IDbConnection>();
        Action<MessagingConfiguration> configure = null!;

        // Act & Assert
        var act = () => ServiceCollectionExtensions.AddEncinaADO(services, factory, configure);
        act.Should().Throw<ArgumentNullException>().WithParameterName("configure");
    }
}
