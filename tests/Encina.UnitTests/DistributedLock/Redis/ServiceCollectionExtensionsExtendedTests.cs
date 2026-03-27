using Encina.DistributedLock;
using Encina.DistributedLock.Redis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using StackExchange.Redis;

namespace Encina.UnitTests.DistributedLock.Redis;

public class ServiceCollectionExtensionsExtendedTests
{
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
    public void AddEncinaDistributedLockRedis_WithMultiplexerAndConfigure_NullConfigure_Throws()
    {
        var mux = Substitute.For<IConnectionMultiplexer>();
        var services = new ServiceCollection();

        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaDistributedLockRedis(mux, null!));
    }

    [Fact]
    public void AddEncinaDistributedLockRedis_ReturnsSameCollection()
    {
        var mux = Substitute.For<IConnectionMultiplexer>();
        var services = new ServiceCollection();
        var result = services.AddEncinaDistributedLockRedis(mux);
        result.ShouldBeSameAs(services);
    }
}
