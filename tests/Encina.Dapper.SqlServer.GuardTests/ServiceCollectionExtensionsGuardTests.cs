using System.Data;
using Microsoft.Extensions.DependencyInjection;
using Encina.Dapper.SqlServer;
using Encina.Messaging;

namespace Encina.Dapper.SqlServer.GuardTests;

/// <summary>
/// Guard clause tests for <see cref="ServiceCollectionExtensions"/>.
/// Verifies that all null/invalid parameters are properly guarded.
/// </summary>
public sealed class ServiceCollectionExtensionsGuardTests
{
    /// <summary>
    /// Tests that AddEncinaDapper throws ArgumentNullException when services is null.
    /// </summary>
    [Fact]
    public void AddEncinaDapper_NullServices_ShouldThrowArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;
        Action<MessagingConfiguration> configure = _ => { };

        // Act
        var act = () => services.AddEncinaDapper(configure);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("services");
    }

    /// <summary>
    /// Tests that AddEncinaDapper throws ArgumentNullException when configure is null.
    /// </summary>
    [Fact]
    public void AddEncinaDapper_NullConfigure_ShouldThrowArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        Action<MessagingConfiguration> configure = null!;

        // Act
        var act = () => services.AddEncinaDapper(configure);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("configure");
    }

    /// <summary>
    /// Tests that AddEncinaDapper with connection factory throws ArgumentNullException when services is null.
    /// </summary>
    [Fact]
    public void AddEncinaDapperWithFactory_NullServices_ShouldThrowArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;
        Func<IServiceProvider, IDbConnection> connectionFactory = _ => Substitute.For<IDbConnection>();
        Action<MessagingConfiguration> configure = _ => { };

        // Act
        var act = () => services.AddEncinaDapper(connectionFactory, configure);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("services");
    }

    /// <summary>
    /// Tests that AddEncinaDapper with connection factory throws ArgumentNullException when connectionFactory is null.
    /// </summary>
    [Fact]
    public void AddEncinaDapperWithFactory_NullConnectionFactory_ShouldThrowArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        Func<IServiceProvider, IDbConnection> connectionFactory = null!;
        Action<MessagingConfiguration> configure = _ => { };

        // Act
        var act = () => services.AddEncinaDapper(connectionFactory, configure);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("connectionFactory");
    }

    /// <summary>
    /// Tests that AddEncinaDapper with connection factory throws ArgumentNullException when configure is null.
    /// </summary>
    [Fact]
    public void AddEncinaDapperWithFactory_NullConfigure_ShouldThrowArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        Func<IServiceProvider, IDbConnection> connectionFactory = _ => Substitute.For<IDbConnection>();
        Action<MessagingConfiguration> configure = null!;

        // Act
        var act = () => services.AddEncinaDapper(connectionFactory, configure);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("configure");
    }

    /// <summary>
    /// Tests that AddEncinaDapper with connection string throws ArgumentNullException when services is null.
    /// </summary>
    [Fact]
    public void AddEncinaDapperWithString_NullServices_ShouldThrowArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;
        const string connectionString = "Server=localhost;Database=test;";
        Action<MessagingConfiguration> configure = _ => { };

        // Act
        var act = () => services.AddEncinaDapper(connectionString, configure);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("services");
    }

    /// <summary>
    /// Tests that AddEncinaDapper with connection string throws ArgumentNullException when connectionString is null.
    /// </summary>
    [Fact]
    public void AddEncinaDapperWithString_NullConnectionString_ShouldThrowArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        string connectionString = null!;
        Action<MessagingConfiguration> configure = _ => { };

        // Act
        var act = () => services.AddEncinaDapper(connectionString, configure);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("connectionString");
    }

    /// <summary>
    /// Tests that AddEncinaDapper with connection string throws ArgumentException when connectionString is empty.
    /// </summary>
    [Fact]
    public void AddEncinaDapperWithString_EmptyConnectionString_ShouldThrowArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();
        const string connectionString = "";
        Action<MessagingConfiguration> configure = _ => { };

        // Act
        var act = () => services.AddEncinaDapper(connectionString, configure);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("connectionString");
    }

    /// <summary>
    /// Tests that AddEncinaDapper with connection string throws ArgumentException when connectionString is whitespace.
    /// </summary>
    [Fact]
    public void AddEncinaDapperWithString_WhitespaceConnectionString_ShouldThrowArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();
        const string connectionString = "   ";
        Action<MessagingConfiguration> configure = _ => { };

        // Act
        var act = () => services.AddEncinaDapper(connectionString, configure);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("connectionString");
    }

    /// <summary>
    /// Tests that AddEncinaDapper with connection string throws ArgumentNullException when configure is null.
    /// </summary>
    [Fact]
    public void AddEncinaDapperWithString_NullConfigure_ShouldThrowArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        const string connectionString = "Server=localhost;Database=test;";
        Action<MessagingConfiguration> configure = null!;

        // Act
        var act = () => services.AddEncinaDapper(connectionString, configure);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("configure");
    }
}
