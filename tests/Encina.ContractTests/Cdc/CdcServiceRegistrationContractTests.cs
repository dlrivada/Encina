using Encina.Cdc.Abstractions;
using Encina.Cdc.Debezium;
using Encina.Cdc.Debezium.Health;
using Encina.Cdc.MongoDb;
using Encina.Cdc.MongoDb.Health;
using Encina.Cdc.MySql;
using Encina.Cdc.MySql.Health;
using Encina.Cdc.PostgreSql;
using Encina.Cdc.PostgreSql.Health;
using Encina.Cdc.SqlServer;
using Encina.Cdc.SqlServer.Health;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.ContractTests.Cdc;

/// <summary>
/// Contract tests verifying that all CDC provider <c>ServiceCollectionExtensions</c>
/// register the expected services: <see cref="ICdcConnector"/> (singleton) and the
/// provider-specific health check. Covers all 4 CDC providers plus Debezium.
/// </summary>
[Trait("Category", "Contract")]
public sealed class CdcServiceRegistrationContractTests
{
    #region SqlServer Service Registration

    /// <summary>
    /// Contract: <see cref="Encina.Cdc.SqlServer.ServiceCollectionExtensions.AddEncinaCdcSqlServer"/>
    /// must register <see cref="ICdcConnector"/> as singleton.
    /// </summary>
    [Fact]
    public void Contract_SqlServer_RegistersICdcConnector_AsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaCdcSqlServer(options =>
        {
            options.ConnectionString = "Server=.;Database=Test;Trusted_Connection=True";
        });

        // Assert
        services.ShouldContain(d =>
            d.ServiceType == typeof(ICdcConnector)
            && d.Lifetime == ServiceLifetime.Singleton,
            "AddEncinaCdcSqlServer must register ICdcConnector as singleton");
    }

    /// <summary>
    /// Contract: <see cref="Encina.Cdc.SqlServer.ServiceCollectionExtensions.AddEncinaCdcSqlServer"/>
    /// must register the provider-specific health check.
    /// </summary>
    [Fact]
    public void Contract_SqlServer_RegistersHealthCheck()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaCdcSqlServer(options =>
        {
            options.ConnectionString = "Server=.;Database=Test;Trusted_Connection=True";
        });

        // Assert
        services.ShouldContain(d =>
            d.ServiceType == typeof(SqlServerCdcHealthCheck)
            && d.Lifetime == ServiceLifetime.Singleton,
            "AddEncinaCdcSqlServer must register SqlServerCdcHealthCheck as singleton");
    }

    #endregion

    #region PostgreSQL Service Registration

    /// <summary>
    /// Contract: <see cref="Encina.Cdc.PostgreSql.ServiceCollectionExtensions.AddEncinaCdcPostgreSql"/>
    /// must register <see cref="ICdcConnector"/> as singleton.
    /// </summary>
    [Fact]
    public void Contract_PostgreSql_RegistersICdcConnector_AsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaCdcPostgreSql(options =>
        {
            options.ConnectionString = "Host=localhost;Database=test";
        });

        // Assert
        services.ShouldContain(d =>
            d.ServiceType == typeof(ICdcConnector)
            && d.Lifetime == ServiceLifetime.Singleton,
            "AddEncinaCdcPostgreSql must register ICdcConnector as singleton");
    }

    /// <summary>
    /// Contract: <see cref="Encina.Cdc.PostgreSql.ServiceCollectionExtensions.AddEncinaCdcPostgreSql"/>
    /// must register the provider-specific health check.
    /// </summary>
    [Fact]
    public void Contract_PostgreSql_RegistersHealthCheck()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaCdcPostgreSql(options =>
        {
            options.ConnectionString = "Host=localhost;Database=test";
        });

        // Assert
        services.ShouldContain(d =>
            d.ServiceType == typeof(PostgresCdcHealthCheck)
            && d.Lifetime == ServiceLifetime.Singleton,
            "AddEncinaCdcPostgreSql must register PostgresCdcHealthCheck as singleton");
    }

    #endregion

    #region MySQL Service Registration

    /// <summary>
    /// Contract: <see cref="Encina.Cdc.MySql.ServiceCollectionExtensions.AddEncinaCdcMySql"/>
    /// must register <see cref="ICdcConnector"/> as singleton.
    /// </summary>
    [Fact]
    public void Contract_MySql_RegistersICdcConnector_AsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaCdcMySql(options =>
        {
            options.ConnectionString = "Server=localhost;Database=test";
        });

        // Assert
        services.ShouldContain(d =>
            d.ServiceType == typeof(ICdcConnector)
            && d.Lifetime == ServiceLifetime.Singleton,
            "AddEncinaCdcMySql must register ICdcConnector as singleton");
    }

    /// <summary>
    /// Contract: <see cref="Encina.Cdc.MySql.ServiceCollectionExtensions.AddEncinaCdcMySql"/>
    /// must register the provider-specific health check.
    /// </summary>
    [Fact]
    public void Contract_MySql_RegistersHealthCheck()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaCdcMySql(options =>
        {
            options.ConnectionString = "Server=localhost;Database=test";
        });

        // Assert
        services.ShouldContain(d =>
            d.ServiceType == typeof(MySqlCdcHealthCheck)
            && d.Lifetime == ServiceLifetime.Singleton,
            "AddEncinaCdcMySql must register MySqlCdcHealthCheck as singleton");
    }

    #endregion

    #region MongoDB Service Registration

    /// <summary>
    /// Contract: <see cref="Encina.Cdc.MongoDb.ServiceCollectionExtensions.AddEncinaCdcMongoDb"/>
    /// must register <see cref="ICdcConnector"/> as singleton.
    /// </summary>
    [Fact]
    public void Contract_MongoDb_RegistersICdcConnector_AsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaCdcMongoDb(options =>
        {
            options.ConnectionString = "mongodb://localhost:27017";
            options.DatabaseName = "test";
        });

        // Assert
        services.ShouldContain(d =>
            d.ServiceType == typeof(ICdcConnector)
            && d.Lifetime == ServiceLifetime.Singleton,
            "AddEncinaCdcMongoDb must register ICdcConnector as singleton");
    }

    /// <summary>
    /// Contract: <see cref="Encina.Cdc.MongoDb.ServiceCollectionExtensions.AddEncinaCdcMongoDb"/>
    /// must register the provider-specific health check.
    /// </summary>
    [Fact]
    public void Contract_MongoDb_RegistersHealthCheck()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaCdcMongoDb(options =>
        {
            options.ConnectionString = "mongodb://localhost:27017";
            options.DatabaseName = "test";
        });

        // Assert
        services.ShouldContain(d =>
            d.ServiceType == typeof(MongoCdcHealthCheck)
            && d.Lifetime == ServiceLifetime.Singleton,
            "AddEncinaCdcMongoDb must register MongoCdcHealthCheck as singleton");
    }

    #endregion

    #region Debezium Service Registration

    /// <summary>
    /// Contract: <see cref="Encina.Cdc.Debezium.ServiceCollectionExtensions.AddEncinaCdcDebezium"/>
    /// must register <see cref="ICdcConnector"/> as singleton.
    /// </summary>
    [Fact]
    public void Contract_Debezium_RegistersICdcConnector_AsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaCdcDebezium(options =>
        {
            options.ListenPort = 8080;
        });

        // Assert
        services.ShouldContain(d =>
            d.ServiceType == typeof(ICdcConnector)
            && d.Lifetime == ServiceLifetime.Singleton,
            "AddEncinaCdcDebezium must register ICdcConnector as singleton");
    }

    /// <summary>
    /// Contract: <see cref="Encina.Cdc.Debezium.ServiceCollectionExtensions.AddEncinaCdcDebezium"/>
    /// must register the provider-specific health check.
    /// </summary>
    [Fact]
    public void Contract_Debezium_RegistersHealthCheck()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaCdcDebezium(options =>
        {
            options.ListenPort = 8080;
        });

        // Assert
        services.ShouldContain(d =>
            d.ServiceType == typeof(DebeziumCdcHealthCheck)
            && d.Lifetime == ServiceLifetime.Singleton,
            "AddEncinaCdcDebezium must register DebeziumCdcHealthCheck as singleton");
    }

    #endregion

    #region Cross-Provider Consistency Contract

    /// <summary>
    /// Contract: All CDC registration extensions must register <see cref="ICdcConnector"/>
    /// with <c>TryAddSingleton</c> semantics (only first registration wins).
    /// Verifying this by registering two providers and checking only one connector exists.
    /// </summary>
    [Fact]
    public void Contract_AllProviders_UseTryAddSingleton_FirstWins()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act - register two providers
        services.AddEncinaCdcSqlServer(o => o.ConnectionString = "Server=.;Database=Test");
        services.AddEncinaCdcPostgreSql(o => o.ConnectionString = "Host=localhost");

        // Assert - only one ICdcConnector should be registered
        var connectorDescriptors = services
            .Where(d => d.ServiceType == typeof(ICdcConnector))
            .ToList();

        connectorDescriptors.Count.ShouldBe(1,
            "ICdcConnector must use TryAddSingleton semantics; only the first registered provider wins");
    }

    /// <summary>
    /// Contract: All CDC registration extensions must accept null-safe arguments.
    /// </summary>
    [Fact]
    public void Contract_AllProviders_ThrowOnNullServices()
    {
        Should.Throw<ArgumentNullException>(() =>
            ServiceCollectionExtensionsHelper.SqlServer(null!, _ => { }));
        Should.Throw<ArgumentNullException>(() =>
            ServiceCollectionExtensionsHelper.PostgreSql(null!, _ => { }));
        Should.Throw<ArgumentNullException>(() =>
            ServiceCollectionExtensionsHelper.MySql(null!, _ => { }));
        Should.Throw<ArgumentNullException>(() =>
            ServiceCollectionExtensionsHelper.MongoDb(null!, _ => { }));
        Should.Throw<ArgumentNullException>(() =>
            ServiceCollectionExtensionsHelper.Debezium(null!, _ => { }));
    }

    /// <summary>
    /// Contract: All CDC registration extensions must reject null configure action.
    /// </summary>
    [Fact]
    public void Contract_AllProviders_ThrowOnNullConfigure()
    {
        var services = new ServiceCollection();

        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaCdcSqlServer(null!));
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaCdcPostgreSql(null!));
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaCdcMySql(null!));
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaCdcMongoDb(null!));
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaCdcDebezium(null!));
    }

    #endregion

    /// <summary>
    /// Static helper to invoke extension methods as regular methods for null-checking.
    /// </summary>
    private static class ServiceCollectionExtensionsHelper
    {
        public static IServiceCollection SqlServer(IServiceCollection s, Action<SqlServerCdcOptions> c)
            => s.AddEncinaCdcSqlServer(c);
        public static IServiceCollection PostgreSql(IServiceCollection s, Action<PostgresCdcOptions> c)
            => s.AddEncinaCdcPostgreSql(c);
        public static IServiceCollection MySql(IServiceCollection s, Action<MySqlCdcOptions> c)
            => s.AddEncinaCdcMySql(c);
        public static IServiceCollection MongoDb(IServiceCollection s, Action<MongoCdcOptions> c)
            => s.AddEncinaCdcMongoDb(c);
        public static IServiceCollection Debezium(IServiceCollection s, Action<DebeziumCdcOptions> c)
            => s.AddEncinaCdcDebezium(c);
    }
}
