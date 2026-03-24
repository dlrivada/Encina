using Encina.Cdc.Abstractions;
using Encina.Cdc.SqlServer;
using Encina.Cdc.SqlServer.Health;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Encina.UnitTests.Cdc.SqlServer;

/// <summary>
/// Unit tests for SQL Server CDC <see cref="ServiceCollectionExtensions"/>.
/// </summary>
public sealed class ServiceCollectionExtensionsTests
{
    private static readonly string[] SalesOrders = ["sales.Orders"];
    #region Null Guards

    [Fact]
    public void AddEncinaCdcSqlServer_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;

        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaCdcSqlServer(_ => { }));
    }

    [Fact]
    public void AddEncinaCdcSqlServer_NullConfigure_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();

        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaCdcSqlServer(null!));
    }

    #endregion

    #region Service Registrations

    [Fact]
    public void AddEncinaCdcSqlServer_RegistersOptionsSingleton()
    {
        var services = new ServiceCollection();

        services.AddEncinaCdcSqlServer(o => o.ConnectionString = "Server=.");

        services.ShouldContain(d =>
            d.ServiceType == typeof(SqlServerCdcOptions) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddEncinaCdcSqlServer_RegistersConnectorAsSingleton()
    {
        var services = new ServiceCollection();

        services.AddEncinaCdcSqlServer(_ => { });

        services.ShouldContain(d =>
            d.ServiceType == typeof(ICdcConnector) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddEncinaCdcSqlServer_RegistersHealthCheckAsSingleton()
    {
        var services = new ServiceCollection();

        services.AddEncinaCdcSqlServer(_ => { });

        services.ShouldContain(d =>
            d.ServiceType == typeof(SqlServerCdcHealthCheck) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddEncinaCdcSqlServer_RegistersTimeProviderAsSingleton()
    {
        var services = new ServiceCollection();

        services.AddEncinaCdcSqlServer(_ => { });

        services.ShouldContain(d =>
            d.ServiceType == typeof(TimeProvider) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddEncinaCdcSqlServer_ConfigureActionIsInvoked()
    {
        var services = new ServiceCollection();
        var configured = false;

        services.AddEncinaCdcSqlServer(_ => configured = true);

        configured.ShouldBeTrue();
    }

    [Fact]
    public void AddEncinaCdcSqlServer_ConfigureActionSetsOptions()
    {
        var services = new ServiceCollection();

        services.AddEncinaCdcSqlServer(o =>
        {
            o.ConnectionString = "Server=myserver";
            o.SchemaName = "sales";
            o.TrackedTables = ["sales.Orders"];
        });

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<SqlServerCdcOptions>();

        options.ConnectionString.ShouldBe("Server=myserver");
        options.SchemaName.ShouldBe("sales");
        options.TrackedTables.ShouldBe(SalesOrders);
    }

    [Fact]
    public void AddEncinaCdcSqlServer_ReturnsSameServiceCollection()
    {
        var services = new ServiceCollection();

        var result = services.AddEncinaCdcSqlServer(_ => { });

        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddEncinaCdcSqlServer_DoesNotDuplicateConnectorOnSecondCall()
    {
        var services = new ServiceCollection();

        services.AddEncinaCdcSqlServer(_ => { });
        services.AddEncinaCdcSqlServer(_ => { });

        var connectorRegistrations = services
            .Where(d => d.ServiceType == typeof(ICdcConnector))
            .ToList();

        connectorRegistrations.Count.ShouldBe(1);
    }

    #endregion
}
