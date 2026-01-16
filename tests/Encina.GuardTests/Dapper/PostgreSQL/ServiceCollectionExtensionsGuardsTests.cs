using Microsoft.Extensions.DependencyInjection;
using Encina.Dapper.PostgreSQL;
using Encina.Messaging;
using Shouldly;

namespace Encina.GuardTests.Dapper.PostgreSQL;

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
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("services");
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
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("configure");
    }

    /// <summary>
    /// Verifies that AddEncinaDapper with connection factory throws ArgumentNullException when services is null.
    /// </summary>
    [Fact]
    public void AddEncinaDapperWithFactory_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;
        Func<IServiceProvider, System.Data.IDbConnection> connectionFactory = _ => null!;
        Action<MessagingConfiguration> configure = _ => { };

        // Act & Assert
        var act = () => services.AddEncinaDapper(connectionFactory, configure);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("services");
    }

    /// <summary>
    /// Verifies that AddEncinaDapper with connection factory throws ArgumentNullException when connectionFactory is null.
    /// </summary>
    [Fact]
    public void AddEncinaDapperWithFactory_NullConnectionFactory_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        Func<IServiceProvider, System.Data.IDbConnection> connectionFactory = null!;
        Action<MessagingConfiguration> configure = _ => { };

        // Act & Assert
        var act = () => services.AddEncinaDapper(connectionFactory, configure);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("connectionFactory");
    }

    /// <summary>
    /// Verifies that AddEncinaDapper with connection factory throws ArgumentNullException when configure is null.
    /// </summary>
    [Fact]
    public void AddEncinaDapperWithFactory_NullConfigure_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        Func<IServiceProvider, System.Data.IDbConnection> connectionFactory = _ => null!;
        Action<MessagingConfiguration> configure = null!;

        // Act & Assert
        var act = () => services.AddEncinaDapper(connectionFactory, configure);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("configure");
    }

    /// <summary>
    /// Verifies that AddEncinaDapper with connection string throws ArgumentNullException when services is null.
    /// </summary>
    [Fact]
    public void AddEncinaDapperWithConnectionString_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;
        var connectionString = "Host=localhost;Database=test";
        Action<MessagingConfiguration> configure = _ => { };

        // Act & Assert
        var act = () => services.AddEncinaDapper(connectionString, configure);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("services");
    }

    /// <summary>
    /// Verifies that AddEncinaDapper with connection string throws ArgumentNullException when connectionString is null.
    /// </summary>
    [Fact]
    public void AddEncinaDapperWithConnectionString_NullConnectionString_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        string connectionString = null!;
        Action<MessagingConfiguration> configure = _ => { };

        // Act & Assert
        var act = () => services.AddEncinaDapper(connectionString, configure);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("connectionString");
    }

    /// <summary>
    /// Verifies that AddEncinaDapper with connection string throws ArgumentException when connectionString is empty.
    /// </summary>
    [Fact]
    public void AddEncinaDapperWithConnectionString_EmptyConnectionString_ThrowsArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();
        var connectionString = string.Empty;
        Action<MessagingConfiguration> configure = _ => { };

        // Act & Assert
        var act = () => services.AddEncinaDapper(connectionString, configure);
        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("connectionString");
    }

    /// <summary>
    /// Verifies that AddEncinaDapper with connection string throws ArgumentException when connectionString is whitespace.
    /// </summary>
    [Fact]
    public void AddEncinaDapperWithConnectionString_WhitespaceConnectionString_ThrowsArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();
        var connectionString = "   ";
        Action<MessagingConfiguration> configure = _ => { };

        // Act & Assert
        var act = () => services.AddEncinaDapper(connectionString, configure);
        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("connectionString");
    }

    /// <summary>
    /// Verifies that AddEncinaDapper with connection string throws ArgumentNullException when configure is null.
    /// </summary>
    [Fact]
    public void AddEncinaDapperWithConnectionString_NullConfigure_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        var connectionString = "Host=localhost;Database=test";
        Action<MessagingConfiguration> configure = null!;

        // Act & Assert
        var act = () => services.AddEncinaDapper(connectionString, configure);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("configure");
    }
}
