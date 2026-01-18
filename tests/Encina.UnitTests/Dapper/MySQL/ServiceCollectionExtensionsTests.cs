using System.Data;
using Encina.Dapper.MySQL;
using Encina.Messaging;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Encina.UnitTests.Dapper.MySQL;

/// <summary>
/// Unit tests for <see cref="ServiceCollectionExtensions"/>.
/// </summary>
public sealed class ServiceCollectionExtensionsTests
{
    private const string ValidConnectionString = "Server=localhost;Database=test;User=root;Password=mysql";

    [Fact]
    public void AddEncinaDapper_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection? services = null;
        Should.Throw<ArgumentNullException>(() => services!.AddEncinaDapper(_ => { }));
    }

    [Fact]
    public void AddEncinaDapper_NullConfigure_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();
        Should.Throw<ArgumentNullException>(() => services.AddEncinaDapper((Action<MessagingConfiguration>)null!));
    }

    [Fact]
    public void AddEncinaDapper_WithConfiguration_RegistersServices()
    {
        var services = new ServiceCollection();
        var configApplied = false;
        var result = services.AddEncinaDapper(config => { config.UseOutbox = true; configApplied = true; });
        result.ShouldBe(services);
        configApplied.ShouldBeTrue();
    }

    [Fact]
    public void AddEncinaDapper_WithHealthCheckEnabled_RegistersHealthCheck()
    {
        var services = new ServiceCollection();
        services.AddEncinaDapper(config => config.ProviderHealthCheck.Enabled = true);
        services.ShouldContain(sd => sd.ServiceType.Name == "IEncinaHealthCheck");
    }

    [Fact]
    public void AddEncinaDapper_WithConnectionFactory_RegistersConnection()
    {
        var services = new ServiceCollection();
        var result = services.AddEncinaDapper(_ => Substitute.For<IDbConnection>(), config => config.UseOutbox = true);
        result.ShouldBe(services);
        services.ShouldContain(sd => sd.ServiceType == typeof(IDbConnection));
    }

    [Fact]
    public void AddEncinaDapper_WithConnectionString_RegistersServices()
    {
        var services = new ServiceCollection();
        var result = services.AddEncinaDapper(ValidConnectionString, config => config.UseOutbox = true);
        result.ShouldBe(services);
        services.ShouldContain(sd => sd.ServiceType == typeof(IDbConnection));
    }
}
