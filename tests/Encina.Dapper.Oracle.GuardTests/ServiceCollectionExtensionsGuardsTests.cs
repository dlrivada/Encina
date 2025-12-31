using Microsoft.Extensions.DependencyInjection;
using Encina.Dapper.Oracle;
using Encina.Messaging;
using NSubstitute;
using Shouldly;

namespace Encina.Dapper.Oracle.GuardTests;

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
        var act = () => ServiceCollectionExtensions.AddEncinaDapper(services, configure);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("services");
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
        var act = () => ServiceCollectionExtensions.AddEncinaDapper(services, configure);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("configure");
    }

    /// <summary>
    /// Verifies that AddEncinaDapper with factory throws ArgumentNullException when services is null.
    /// </summary>
    [Fact]
    public void AddEncinaDapperWithFactory_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;
        Func<IServiceProvider, System.Data.IDbConnection> factory = _ => Substitute.For<System.Data.IDbConnection>();
        Action<MessagingConfiguration> configure = _ => { };

        // Act & Assert
        var act = () => ServiceCollectionExtensions.AddEncinaDapper(services, factory, configure);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("services");
    }

    /// <summary>
    /// Verifies that AddEncinaDapper with factory throws ArgumentNullException when factory is null.
    /// </summary>
    [Fact]
    public void AddEncinaDapperWithFactory_NullFactory_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        Func<IServiceProvider, System.Data.IDbConnection> factory = null!;
        Action<MessagingConfiguration> configure = _ => { };

        // Act & Assert
        var act = () => ServiceCollectionExtensions.AddEncinaDapper(services, factory, configure);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("connectionFactory");
    }

    /// <summary>
    /// Verifies that AddEncinaDapper with factory throws ArgumentNullException when configure is null.
    /// </summary>
    [Fact]
    public void AddEncinaDapperWithFactory_NullConfigure_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        Func<IServiceProvider, System.Data.IDbConnection> factory = _ => Substitute.For<System.Data.IDbConnection>();
        Action<MessagingConfiguration> configure = null!;

        // Act & Assert
        var act = () => ServiceCollectionExtensions.AddEncinaDapper(services, factory, configure);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("configure");
    }

    /// <summary>
    /// Verifies that AddEncinaDapper with connection string throws ArgumentNullException when services is null.
    /// </summary>
    [Fact]
    public void AddEncinaDapperWithConnectionString_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;
        var connectionString = "Data Source=localhost;User Id=test;Password=test;";
        Action<MessagingConfiguration> configure = _ => { };

        // Act & Assert
        var act = () => ServiceCollectionExtensions.AddEncinaDapper(services, connectionString, configure);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("services");
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
        var act = () => ServiceCollectionExtensions.AddEncinaDapper(services, connectionString, configure);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("connectionString");
    }

    /// <summary>
    /// Verifies that AddEncinaDapper with connection string throws ArgumentNullException when configure is null.
    /// </summary>
    [Fact]
    public void AddEncinaDapperWithConnectionString_NullConfigure_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        var connectionString = "Data Source=localhost;User Id=test;Password=test;";
        Action<MessagingConfiguration> configure = null!;

        // Act & Assert
        var act = () => ServiceCollectionExtensions.AddEncinaDapper(services, connectionString, configure);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("configure");
    }
}
