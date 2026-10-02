using Encina.Cdc.Abstractions;
using Encina.Cdc.MySql;
using Encina.Cdc.MySql.Health;
using Microsoft.Extensions.DependencyInjection;
using Encina.UnitTests.Validation.Endpoints;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;

namespace Encina.UnitTests.Cdc.MySql;

/// <summary>
/// Unit tests for MySQL CDC <see cref="ServiceCollectionExtensions"/>.
/// </summary>
public sealed class ServiceCollectionExtensionsTests
{
    private static void Valid(MySqlCdcOptions options)
    {
        options.Hostname = "mysql.example.com";
        options.ConnectionString = "Server=mysql.example.com;Database=app";
    }

    #region Null Guards

    [Fact]
    public void AddEncinaCdcMySql_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;

        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaCdcMySql(Valid));
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

        services.AddEncinaCdcMySql(Valid);

        services.ShouldContain(d =>
            d.ServiceType == typeof(MySqlCdcOptions) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddEncinaCdcMySql_RegistersConnectorAsSingleton()
    {
        var services = new ServiceCollection();

        services.AddEncinaCdcMySql(Valid);

        services.ShouldContain(d =>
            d.ServiceType == typeof(ICdcConnector) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddEncinaCdcMySql_RegistersHealthCheckAsSingleton()
    {
        var services = new ServiceCollection();

        services.AddEncinaCdcMySql(Valid);

        services.ShouldContain(d =>
            d.ServiceType == typeof(MySqlCdcHealthCheck) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddEncinaCdcMySql_RegistersTimeProviderAsSingleton()
    {
        var services = new ServiceCollection();

        services.AddEncinaCdcMySql(Valid);

        services.ShouldContain(d =>
            d.ServiceType == typeof(TimeProvider) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddEncinaCdcMySql_RegistersOptionsValidator()
    {
        var services = new ServiceCollection();

        services.AddEncinaCdcMySql(Valid);

        services.ShouldContain(d =>
            d.ServiceType == typeof(IValidateOptions<MySqlCdcOptions>) &&
            d.ImplementationType == typeof(MySqlCdcOptionsValidator));
    }

    [Fact]
    public void AddEncinaCdcMySql_ConfigureActionIsInvoked()
    {
        var services = new ServiceCollection();
        var configured = false;

        services.AddEncinaCdcMySql(o =>
        {
            Valid(o);
            configured = true;
        });

        configured.ShouldBeTrue();
    }

    [Fact]
    public void AddEncinaCdcMySql_ConfigureActionSetsOptions()
    {
        var services = new ServiceCollection();

        services.AddEncinaCdcMySql(o =>
        {
            Valid(o);
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

        var result = services.AddEncinaCdcMySql(Valid);

        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddEncinaCdcMySql_DoesNotDuplicateConnectorOnSecondCall()
    {
        var services = new ServiceCollection();

        services.AddEncinaCdcMySql(Valid);
        services.AddEncinaCdcMySql(Valid);

        var connectorRegistrations = services
            .Where(d => d.ServiceType == typeof(ICdcConnector))
            .ToList();

        connectorRegistrations.Count.ShouldBe(1);
    }

    #endregion

    #region Endpoint Validation

    [Fact]
    public void AddEncinaCdcMySql_DefaultLocalhostHostname_ThrowsOptionsValidationException()
    {
        var services = new ServiceCollection();

        var ex = Should.Throw<OptionsValidationException>(() =>
            services.AddEncinaCdcMySql(o => o.ConnectionString = "Server=mysql.example.com"));

        ex.Message.ShouldContain("Hostname");
        ex.Message.ShouldContain("AllowLocalEndpoints");
    }

    [Fact]
    public void AddEncinaCdcMySql_LoopbackServerInConnectionString_ThrowsOptionsValidationException()
    {
        var services = new ServiceCollection();

        var ex = Should.Throw<OptionsValidationException>(() =>
            services.AddEncinaCdcMySql(o =>
            {
                Valid(o);
                o.ConnectionString = "Server=127.0.0.1;Password=secret-value";
            }));

        ex.Message.ShouldContain("ConnectionString");
        ex.Message.ShouldNotContain("secret-value");
    }

    [Fact]
    public void AddEncinaCdcMySql_LocalEndpointsAllowed_Registers()
    {
        var services = new ServiceCollection();

        services.AddEncinaCdcMySql(o =>
        {
            o.ConnectionString = "Server=localhost";
            o.AllowLocalEndpoints = true;
        });

        services.ShouldContain(d => d.ServiceType == typeof(ICdcConnector));
    }

    [Fact]
    public void AddEncinaCdcMySql_ValidOptions_ProviderBuildsWithValidateOnBuildAndScopes()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<ICdcPositionStore>());
        services.AddEncinaCdcMySql(Valid);

        using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        provider.GetRequiredService<ICdcConnector>().ShouldNotBeNull();
        provider.GetRequiredService<IOptions<MySqlCdcOptions>>().Value.Hostname.ShouldBe("mysql.example.com");
    }

    [Fact]
    public void AddEncinaCdcMySql_LocalEndpointsAllowed_LogsWarningOnceAtResolution()
    {
        var logger = new CollectingLoggerProvider();
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddProvider(logger));
        services.AddEncinaCdcMySql(o =>
        {
            o.ConnectionString = "Server=localhost";
            o.AllowLocalEndpoints = true;
        });

        using var provider = services.BuildServiceProvider();
        _ = provider.GetRequiredService<IOptions<MySqlCdcOptions>>().Value;
        _ = provider.GetRequiredService<IOptionsMonitor<MySqlCdcOptions>>().CurrentValue;

        logger.Count(5400, LogLevel.Warning).ShouldBe(1);
    }

    #endregion
}
