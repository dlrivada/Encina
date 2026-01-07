using System.Data;
using Encina.ADO.Sqlite;
using Encina.Messaging;
using Encina.Messaging.Health;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Encina.ADO.Sqlite.Tests;

/// <summary>
/// Unit tests for <see cref="ServiceCollectionExtensions"/>.
/// </summary>
public sealed class ServiceCollectionExtensionsTests
{
    #region AddEncinaADO with Configuration

    [Fact]
    public void AddEncinaADO_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services!.AddEncinaADO(_ => { }));
    }

    [Fact]
    public void AddEncinaADO_NullConfigure_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaADO(null!));
    }

    [Fact]
    public void AddEncinaADO_ValidConfiguration_ReturnsServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<IDbConnection>());

        // Act
        var result = services.AddEncinaADO(_ => { });

        // Assert
        result.ShouldBeSameAs(services);
    }

    #endregion

    #region AddEncinaADO with Connection String

    [Fact]
    public void AddEncinaADO_WithConnectionString_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services!.AddEncinaADO("Data Source=:memory:", _ => { }));
    }

    [Fact]
    public void AddEncinaADO_WithConnectionString_NullConnectionString_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaADO((string)null!, _ => { }));
    }

    [Fact]
    public void AddEncinaADO_WithConnectionString_NullConfigure_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaADO("Data Source=:memory:", null!));
    }

    [Fact]
    public void AddEncinaADO_WithConnectionString_ValidParameters_ReturnsServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddEncinaADO("Data Source=:memory:", _ => { });

        // Assert
        result.ShouldBeSameAs(services);
    }

    #endregion

    #region AddEncinaADO with Connection Factory

    [Fact]
    public void AddEncinaADO_WithConnectionFactory_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services!.AddEncinaADO(_ => Substitute.For<IDbConnection>(), _ => { }));
    }

    [Fact]
    public void AddEncinaADO_WithConnectionFactory_NullFactory_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaADO((Func<IServiceProvider, IDbConnection>)null!, _ => { }));
    }

    [Fact]
    public void AddEncinaADO_WithConnectionFactory_NullConfigure_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaADO(_ => Substitute.For<IDbConnection>(), null!));
    }

    [Fact]
    public void AddEncinaADO_WithConnectionFactory_ValidParameters_ReturnsServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddEncinaADO(_ => Substitute.For<IDbConnection>(), _ => { });

        // Assert
        result.ShouldBeSameAs(services);
    }

    #endregion

    #region Health Check Registration

    [Fact]
    public void AddEncinaADO_WithProviderHealthCheckEnabled_RegistersHealthCheck()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<IDbConnection>());

        // Act
        services.AddEncinaADO(config =>
        {
            config.ProviderHealthCheck.Enabled = true;
        });
        using var provider = services.BuildServiceProvider();

        // Assert
        var healthCheck = provider.GetService<IEncinaHealthCheck>();
        healthCheck.ShouldNotBeNull();
    }

    #endregion
}
