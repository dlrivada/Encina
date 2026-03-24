using Encina.Cdc.Abstractions;
using Encina.Cdc.MySql;
using Encina.Cdc.MySql.Health;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Encina.UnitTests.Cdc.MySql;

/// <summary>
/// Unit tests for MySQL CDC <see cref="ServiceCollectionExtensions"/>.
/// </summary>
public sealed class ServiceCollectionExtensionsTests
{
    #region Null Guards

    [Fact]
    public void AddEncinaCdcMySql_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;

        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaCdcMySql(_ => { }));
    }

    [Fact]
    public void AddEncinaCdcMySql_NullConfigure_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();

        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaCdcMySql(null!));
    }

    #endregion

    #region Service Registrations

    [Fact]
    public void AddEncinaCdcMySql_RegistersOptionsSingleton()
    {
        var services = new ServiceCollection();

        services.AddEncinaCdcMySql(o => o.ConnectionString = "Server=localhost");

        services.ShouldContain(d =>
            d.ServiceType == typeof(MySqlCdcOptions) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddEncinaCdcMySql_RegistersConnectorAsSingleton()
    {
        var services = new ServiceCollection();

        services.AddEncinaCdcMySql(_ => { });

        services.ShouldContain(d =>
            d.ServiceType == typeof(ICdcConnector) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddEncinaCdcMySql_RegistersHealthCheckAsSingleton()
    {
        var services = new ServiceCollection();

        services.AddEncinaCdcMySql(_ => { });

        services.ShouldContain(d =>
            d.ServiceType == typeof(MySqlCdcHealthCheck) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddEncinaCdcMySql_RegistersTimeProviderAsSingleton()
    {
        var services = new ServiceCollection();

        services.AddEncinaCdcMySql(_ => { });

        services.ShouldContain(d =>
            d.ServiceType == typeof(TimeProvider) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddEncinaCdcMySql_ConfigureActionIsInvoked()
    {
        var services = new ServiceCollection();
        var configured = false;

        services.AddEncinaCdcMySql(_ => configured = true);

        configured.ShouldBeTrue();
    }

    [Fact]
    public void AddEncinaCdcMySql_ConfigureActionSetsOptions()
    {
        var services = new ServiceCollection();

        services.AddEncinaCdcMySql(o =>
        {
            o.Hostname = "db.example.com";
            o.Port = 3307;
            o.ServerId = 99;
        });

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<MySqlCdcOptions>();

        options.Hostname.ShouldBe("db.example.com");
        options.Port.ShouldBe(3307);
        options.ServerId.ShouldBe(99);
    }

    [Fact]
    public void AddEncinaCdcMySql_ReturnsSameServiceCollection()
    {
        var services = new ServiceCollection();

        var result = services.AddEncinaCdcMySql(_ => { });

        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddEncinaCdcMySql_DoesNotDuplicateConnectorOnSecondCall()
    {
        var services = new ServiceCollection();

        services.AddEncinaCdcMySql(_ => { });
        services.AddEncinaCdcMySql(_ => { });

        var connectorRegistrations = services
            .Where(d => d.ServiceType == typeof(ICdcConnector))
            .ToList();

        connectorRegistrations.Count.ShouldBe(1);
    }

    #endregion
}
