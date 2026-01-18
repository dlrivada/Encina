using System.Data;
using Encina.Dapper.PostgreSQL;
using Encina.Messaging;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Encina.UnitTests.Dapper.PostgreSQL;

/// <summary>
/// Unit tests for <see cref="ServiceCollectionExtensions"/>.
/// </summary>
public sealed class ServiceCollectionExtensionsTests
{
    private const string ValidConnectionString = "Host=localhost;Database=test;Username=postgres;Password=postgres";

    #region AddEncinaDapper(Action<MessagingConfiguration>) Tests

    [Fact]
    public void AddEncinaDapper_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services!.AddEncinaDapper(_ => { }));
    }

    [Fact]
    public void AddEncinaDapper_NullConfigure_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaDapper((Action<MessagingConfiguration>)null!));
    }

    [Fact]
    public void AddEncinaDapper_WithConfiguration_RegistersServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var configurationApplied = false;

        // Act
        var result = services.AddEncinaDapper(config =>
        {
            config.UseOutbox = true;
            configurationApplied = true;
        });

        // Assert
        result.ShouldBe(services);
        configurationApplied.ShouldBeTrue();
    }

    [Fact]
    public void AddEncinaDapper_WithHealthCheckEnabled_RegistersHealthCheck()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaDapper(config =>
        {
            config.ProviderHealthCheck.Enabled = true;
        });

        // Assert
        services.ShouldContain(sd => sd.ServiceType.Name == "IEncinaHealthCheck");
    }

    #endregion

    #region AddEncinaDapper(Func<IServiceProvider, IDbConnection>, Action<MessagingConfiguration>) Tests

    [Fact]
    public void AddEncinaDapper_WithConnectionFactory_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services!.AddEncinaDapper(_ => Substitute.For<IDbConnection>(), _ => { }));
    }

    [Fact]
    public void AddEncinaDapper_WithConnectionFactory_NullFactory_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaDapper((Func<IServiceProvider, IDbConnection>)null!, _ => { }));
    }

    [Fact]
    public void AddEncinaDapper_WithConnectionFactory_NullConfigure_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaDapper(_ => Substitute.For<IDbConnection>(), null!));
    }

    [Fact]
    public void AddEncinaDapper_WithConnectionFactory_RegistersConnectionAndServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockConnection = Substitute.For<IDbConnection>();

        // Act
        var result = services.AddEncinaDapper(
            _ => mockConnection,
            config => config.UseOutbox = true);

        // Assert
        result.ShouldBe(services);
        services.ShouldContain(sd => sd.ServiceType == typeof(IDbConnection));
    }

    #endregion

    #region AddEncinaDapper(string, Action<MessagingConfiguration>) Tests

    [Fact]
    public void AddEncinaDapper_WithConnectionString_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services!.AddEncinaDapper(ValidConnectionString, _ => { }));
    }

    [Fact]
    public void AddEncinaDapper_WithConnectionString_NullConnectionString_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaDapper((string)null!, _ => { }));
    }

    [Fact]
    public void AddEncinaDapper_WithConnectionString_NullConfigure_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaDapper(ValidConnectionString, null!));
    }

    [Fact]
    public void AddEncinaDapper_WithConnectionString_RegistersServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddEncinaDapper(
            ValidConnectionString,
            config => config.UseOutbox = true);

        // Assert
        result.ShouldBe(services);
        services.ShouldContain(sd => sd.ServiceType == typeof(IDbConnection));
    }

    #endregion
}
