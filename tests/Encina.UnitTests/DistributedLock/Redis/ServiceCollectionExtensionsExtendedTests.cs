using Encina.DistributedLock;
using Encina.DistributedLock.Redis;
using Encina.DistributedLock.Redis.Health;
using Encina.Messaging.Health;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using StackExchange.Redis;

namespace Encina.UnitTests.DistributedLock.Redis;

public class ServiceCollectionExtensionsExtendedTests
{
    #region Guard Clauses — Connection String Overloads

    [Fact]
    public void AddEncinaDistributedLockRedis_NullServices_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddEncinaDistributedLockRedis("localhost:6379"));
    }

    [Fact]
    public void AddEncinaDistributedLockRedis_NullConnectionString_Throws()
    {
        var services = new ServiceCollection();
        Should.Throw<ArgumentException>(() =>
            services.AddEncinaDistributedLockRedis((string)null!));
    }

    [Fact]
    public void AddEncinaDistributedLockRedis_EmptyConnectionString_Throws()
    {
        var services = new ServiceCollection();
        Should.Throw<ArgumentException>(() =>
            services.AddEncinaDistributedLockRedis(string.Empty));
    }

    [Fact]
    public void AddEncinaDistributedLockRedis_ConnectionStringWithConfigure_NullServices_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddEncinaDistributedLockRedis("localhost:6379", _ => { }));
    }

    [Fact]
    public void AddEncinaDistributedLockRedis_ConnectionStringWithConfigure_NullConnectionString_Throws()
    {
        var services = new ServiceCollection();
        Should.Throw<ArgumentException>(() =>
            services.AddEncinaDistributedLockRedis((string)null!, _ => { }));
    }

    [Fact]
    public void AddEncinaDistributedLockRedis_ConnectionStringWithConfigure_NullConfigure_Throws()
    {
        var services = new ServiceCollection();
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaDistributedLockRedis("localhost:6379", (Action<RedisLockOptions>)null!));
    }

    #endregion

    #region Guard Clauses — Multiplexer Overloads

    [Fact]
    public void AddEncinaDistributedLockRedis_WithMultiplexer_NullServices_Throws()
    {
        var mux = Substitute.For<IConnectionMultiplexer>();
        Should.Throw<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddEncinaDistributedLockRedis(mux));
    }

    [Fact]
    public void AddEncinaDistributedLockRedis_WithMultiplexer_NullMux_Throws()
    {
        var services = new ServiceCollection();
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaDistributedLockRedis((IConnectionMultiplexer)null!));
    }

    [Fact]
    public void AddEncinaDistributedLockRedis_WithMultiplexerAndConfigure_NullConfigure_Throws()
    {
        var mux = Substitute.For<IConnectionMultiplexer>();
        var services = new ServiceCollection();

        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaDistributedLockRedis(mux, null!));
    }

    [Fact]
    public void AddEncinaDistributedLockRedis_WithMultiplexerAndConfigure_NullServices_Throws()
    {
        var mux = Substitute.For<IConnectionMultiplexer>();
        Should.Throw<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddEncinaDistributedLockRedis(mux, _ => { }));
    }

    [Fact]
    public void AddEncinaDistributedLockRedis_WithMultiplexerAndConfigure_NullMux_Throws()
    {
        var services = new ServiceCollection();
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaDistributedLockRedis((IConnectionMultiplexer)null!, _ => { }));
    }

    #endregion

    #region Service Registration

    [Fact]
    public void AddEncinaDistributedLockRedis_WithMultiplexer_RegistersProvider()
    {
        var mux = Substitute.For<IConnectionMultiplexer>();
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaDistributedLockRedis(mux);

        var sp = services.BuildServiceProvider();
        sp.GetService<IDistributedLockProvider>().ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaDistributedLockRedis_ReturnsSameCollection()
    {
        var mux = Substitute.For<IConnectionMultiplexer>();
        var services = new ServiceCollection();
        var result = services.AddEncinaDistributedLockRedis(mux);
        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddEncinaDistributedLockRedis_RegistersTimeProvider()
    {
        var mux = Substitute.For<IConnectionMultiplexer>();
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaDistributedLockRedis(mux);

        var sp = services.BuildServiceProvider();
        sp.GetService<TimeProvider>().ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaDistributedLockRedis_RegistersConnectionMultiplexer()
    {
        var mux = Substitute.For<IConnectionMultiplexer>();
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaDistributedLockRedis(mux);

        var sp = services.BuildServiceProvider();
        sp.GetService<IConnectionMultiplexer>().ShouldNotBeNull();
        sp.GetService<IConnectionMultiplexer>().ShouldBeSameAs(mux);
    }

    [Fact]
    public void AddEncinaDistributedLockRedis_WithConfigure_RegistersProvider()
    {
        var mux = Substitute.For<IConnectionMultiplexer>();
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddEncinaDistributedLockRedis(mux, opts =>
        {
            opts.Database = 3;
            opts.KeyPrefix = "test";
        });

        var sp = services.BuildServiceProvider();
        sp.GetService<IDistributedLockProvider>().ShouldNotBeNull();
        sp.GetService<IDistributedLockProvider>().ShouldBeOfType<RedisDistributedLockProvider>();
    }

    #endregion

    #region Health Check Registration

    [Fact]
    public void AddEncinaDistributedLockRedis_DefaultOptions_RegistersHealthCheck()
    {
        // Arrange — default options have ProviderHealthCheck.Enabled = true
        var mux = Substitute.For<IConnectionMultiplexer>();
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaDistributedLockRedis(mux);

        // Assert
        var sp = services.BuildServiceProvider();
        var healthChecks = sp.GetServices<IEncinaHealthCheck>();
        healthChecks.ShouldContain(hc => hc is RedisDistributedLockHealthCheck);
    }

    [Fact]
    public void AddEncinaDistributedLockRedis_HealthCheckDisabled_DoesNotRegisterHealthCheck()
    {
        // Arrange
        var mux = Substitute.For<IConnectionMultiplexer>();
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaDistributedLockRedis(mux, opts =>
        {
            opts.ProviderHealthCheck.Enabled = false;
        });

        // Assert
        var sp = services.BuildServiceProvider();
        var healthChecks = sp.GetServices<IEncinaHealthCheck>();
        healthChecks.ShouldNotContain(hc => hc is RedisDistributedLockHealthCheck);
    }

    [Fact]
    public void AddEncinaDistributedLockRedis_HealthCheckEnabled_RegistersProviderHealthCheckOptions()
    {
        // Arrange
        var mux = Substitute.For<IConnectionMultiplexer>();
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaDistributedLockRedis(mux);

        // Assert
        var sp = services.BuildServiceProvider();
        var options = sp.GetService<ProviderHealthCheckOptions>();
        options.ShouldNotBeNull();
    }

    #endregion

    #region Idempotent Registration

    [Fact]
    public void AddEncinaDistributedLockRedis_CalledTwice_DoesNotDuplicateProvider()
    {
        // Arrange
        var mux = Substitute.For<IConnectionMultiplexer>();
        var services = new ServiceCollection();
        services.AddLogging();

        // Act — register twice
        services.AddEncinaDistributedLockRedis(mux);
        services.AddEncinaDistributedLockRedis(mux);

        // Assert — TryAddSingleton should prevent duplicates
        var sp = services.BuildServiceProvider();
        var providers = sp.GetServices<IDistributedLockProvider>().ToList();
        providers.Count.ShouldBe(1);
    }

    #endregion
}
