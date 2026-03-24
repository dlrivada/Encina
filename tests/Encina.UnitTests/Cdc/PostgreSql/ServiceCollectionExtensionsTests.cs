using Encina.Cdc.Abstractions;
using Encina.Cdc.PostgreSql;
using Encina.Cdc.PostgreSql.Health;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Encina.UnitTests.Cdc.PostgreSql;

/// <summary>
/// Unit tests for PostgreSQL CDC <see cref="ServiceCollectionExtensions"/>.
/// </summary>
public sealed class ServiceCollectionExtensionsTests
{
    #region Null Guards

    [Fact]
    public void AddEncinaCdcPostgreSql_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;

        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaCdcPostgreSql(_ => { }));
    }

    [Fact]
    public void AddEncinaCdcPostgreSql_NullConfigure_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();

        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaCdcPostgreSql(null!));
    }

    #endregion

    #region Service Registrations

    [Fact]
    public void AddEncinaCdcPostgreSql_RegistersOptionsSingleton()
    {
        var services = new ServiceCollection();

        services.AddEncinaCdcPostgreSql(o => o.ConnectionString = "Host=localhost");

        services.ShouldContain(d =>
            d.ServiceType == typeof(PostgresCdcOptions) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddEncinaCdcPostgreSql_RegistersConnectorAsSingleton()
    {
        var services = new ServiceCollection();

        services.AddEncinaCdcPostgreSql(_ => { });

        services.ShouldContain(d =>
            d.ServiceType == typeof(ICdcConnector) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddEncinaCdcPostgreSql_RegistersHealthCheckAsSingleton()
    {
        var services = new ServiceCollection();

        services.AddEncinaCdcPostgreSql(_ => { });

        services.ShouldContain(d =>
            d.ServiceType == typeof(PostgresCdcHealthCheck) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddEncinaCdcPostgreSql_RegistersTimeProviderAsSingleton()
    {
        var services = new ServiceCollection();

        services.AddEncinaCdcPostgreSql(_ => { });

        services.ShouldContain(d =>
            d.ServiceType == typeof(TimeProvider) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddEncinaCdcPostgreSql_ConfigureActionIsInvoked()
    {
        var services = new ServiceCollection();
        var configured = false;

        services.AddEncinaCdcPostgreSql(_ => configured = true);

        configured.ShouldBeTrue();
    }

    [Fact]
    public void AddEncinaCdcPostgreSql_ConfigureActionSetsOptions()
    {
        var services = new ServiceCollection();

        services.AddEncinaCdcPostgreSql(o =>
        {
            o.ConnectionString = "Host=myhost";
            o.PublicationName = "my_pub";
        });

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<PostgresCdcOptions>();

        options.ConnectionString.ShouldBe("Host=myhost");
        options.PublicationName.ShouldBe("my_pub");
    }

    [Fact]
    public void AddEncinaCdcPostgreSql_ReturnsSameServiceCollection()
    {
        var services = new ServiceCollection();

        var result = services.AddEncinaCdcPostgreSql(_ => { });

        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddEncinaCdcPostgreSql_DoesNotDuplicateConnectorOnSecondCall()
    {
        var services = new ServiceCollection();

        services.AddEncinaCdcPostgreSql(_ => { });
        services.AddEncinaCdcPostgreSql(_ => { });

        var connectorRegistrations = services
            .Where(d => d.ServiceType == typeof(ICdcConnector))
            .ToList();

        // TryAddSingleton prevents duplicates for ICdcConnector
        connectorRegistrations.Count.ShouldBe(1);
    }

    #endregion
}
