using Encina.Cdc.Abstractions;
using Encina.Cdc.MongoDb;
using Encina.Cdc.MongoDb.Health;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Encina.UnitTests.Cdc.MongoDb;

/// <summary>
/// Unit tests for MongoDB CDC <see cref="ServiceCollectionExtensions"/>.
/// </summary>
public sealed class ServiceCollectionExtensionsTests
{
    #region Null Guards

    [Fact]
    public void AddEncinaCdcMongoDb_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;

        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaCdcMongoDb(_ => { }));
    }

    [Fact]
    public void AddEncinaCdcMongoDb_NullConfigure_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();

        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaCdcMongoDb(null!));
    }

    #endregion

    #region Service Registrations

    [Fact]
    public void AddEncinaCdcMongoDb_RegistersOptionsSingleton()
    {
        var services = new ServiceCollection();

        services.AddEncinaCdcMongoDb(o => o.ConnectionString = "mongodb://localhost");

        services.ShouldContain(d =>
            d.ServiceType == typeof(MongoCdcOptions) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddEncinaCdcMongoDb_RegistersConnectorAsSingleton()
    {
        var services = new ServiceCollection();

        services.AddEncinaCdcMongoDb(_ => { });

        services.ShouldContain(d =>
            d.ServiceType == typeof(ICdcConnector) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddEncinaCdcMongoDb_RegistersHealthCheckAsSingleton()
    {
        var services = new ServiceCollection();

        services.AddEncinaCdcMongoDb(_ => { });

        services.ShouldContain(d =>
            d.ServiceType == typeof(MongoCdcHealthCheck) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddEncinaCdcMongoDb_RegistersTimeProviderAsSingleton()
    {
        var services = new ServiceCollection();

        services.AddEncinaCdcMongoDb(_ => { });

        services.ShouldContain(d =>
            d.ServiceType == typeof(TimeProvider) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddEncinaCdcMongoDb_ConfigureActionIsInvoked()
    {
        var services = new ServiceCollection();
        var configured = false;

        services.AddEncinaCdcMongoDb(_ => configured = true);

        configured.ShouldBeTrue();
    }

    [Fact]
    public void AddEncinaCdcMongoDb_ConfigureActionSetsOptions()
    {
        var services = new ServiceCollection();

        services.AddEncinaCdcMongoDb(o =>
        {
            o.ConnectionString = "mongodb://myhost:27017";
            o.DatabaseName = "mydb";
            o.WatchDatabase = false;
        });

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<MongoCdcOptions>();

        options.ConnectionString.ShouldBe("mongodb://myhost:27017");
        options.DatabaseName.ShouldBe("mydb");
        options.WatchDatabase.ShouldBeFalse();
    }

    [Fact]
    public void AddEncinaCdcMongoDb_ReturnsSameServiceCollection()
    {
        var services = new ServiceCollection();

        var result = services.AddEncinaCdcMongoDb(_ => { });

        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddEncinaCdcMongoDb_DoesNotDuplicateConnectorOnSecondCall()
    {
        var services = new ServiceCollection();

        services.AddEncinaCdcMongoDb(_ => { });
        services.AddEncinaCdcMongoDb(_ => { });

        var connectorRegistrations = services
            .Where(d => d.ServiceType == typeof(ICdcConnector))
            .ToList();

        connectorRegistrations.Count.ShouldBe(1);
    }

    #endregion
}
