using System.Data;
using Encina.Dapper.Sqlite;
using Encina.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.GuardTests.Dapper.Sqlite;

/// <summary>
/// Guard tests for <see cref="ServiceCollectionExtensions"/> to verify null parameter handling.
/// </summary>
public class ServiceCollectionExtensionsGuardsTests
{
    /// <summary>
    /// Verifies that AddEncinaDapper throws ArgumentNullException when services is null.
    /// </summary>
    [Fact]
    public void AddEncinaDapper_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;
        Action<MessagingConfiguration> configure = _ => { };

        // Act & Assert
        var act = () => services.AddEncinaDapper(configure);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe(nameof(services));
    }

    /// <summary>
    /// Verifies that AddEncinaDapper throws ArgumentNullException when configure is null.
    /// </summary>
    [Fact]
    public void AddEncinaDapper_NullConfigure_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        Action<MessagingConfiguration> configure = null!;

        // Act & Assert
        var act = () => services.AddEncinaDapper(configure);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe(nameof(configure));
    }

    /// <summary>
    /// Verifies that AddEncinaDapper with connection factory throws ArgumentNullException when services is null.
    /// </summary>
    [Fact]
    public void AddEncinaDapper_WithFactory_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;
        Func<IServiceProvider, System.Data.IDbConnection> connectionFactory = _ => null!;
        Action<MessagingConfiguration> configure = _ => { };

        // Act & Assert
        var act = () => services.AddEncinaDapper(connectionFactory, configure);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe(nameof(services));
    }

    /// <summary>
    /// Verifies that AddEncinaDapper with connection factory throws ArgumentNullException when connectionFactory is null.
    /// </summary>
    [Fact]
    public void AddEncinaDapper_WithFactory_NullConnectionFactory_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        Func<IServiceProvider, System.Data.IDbConnection> connectionFactory = null!;
        Action<MessagingConfiguration> configure = _ => { };

        // Act & Assert
        var act = () => services.AddEncinaDapper(connectionFactory, configure);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe(nameof(connectionFactory));
    }

    /// <summary>
    /// Verifies that AddEncinaDapper with connection factory throws ArgumentNullException when configure is null.
    /// </summary>
    [Fact]
    public void AddEncinaDapper_WithFactory_NullConfigure_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        Func<IServiceProvider, System.Data.IDbConnection> connectionFactory = _ => null!;
        Action<MessagingConfiguration> configure = null!;

        // Act & Assert
        var act = () => services.AddEncinaDapper(connectionFactory, configure);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe(nameof(configure));
    }

    /// <summary>
    /// Verifies that AddEncinaDapper with connection string throws ArgumentNullException when services is null.
    /// </summary>
    [Fact]
    public void AddEncinaDapper_WithConnectionString_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;
        var connectionString = "Data Source=test.db";
        Action<MessagingConfiguration> configure = _ => { };

        // Act & Assert
        var act = () => services.AddEncinaDapper(connectionString, configure);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe(nameof(services));
    }

    /// <summary>
    /// Verifies that AddEncinaDapper with connection string throws ArgumentNullException when connectionString is null.
    /// </summary>
    [Fact]
    public void AddEncinaDapper_WithConnectionString_NullConnectionString_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        string connectionString = null!;
        Action<MessagingConfiguration> configure = _ => { };

        // Act & Assert
        var act = () => services.AddEncinaDapper(connectionString, configure);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe(nameof(connectionString));
    }

    /// <summary>
    /// Verifies that AddEncinaDapper with connection string throws ArgumentNullException when configure is null.
    /// </summary>
    [Fact]
    public void AddEncinaDapper_WithConnectionString_NullConfigure_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        var connectionString = "Data Source=test.db";
        Action<MessagingConfiguration> configure = null!;

        // Act & Assert
        var act = () => services.AddEncinaDapper(connectionString, configure);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe(nameof(configure));
    }
}
