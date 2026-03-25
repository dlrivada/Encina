using Encina.DistributedLock;
using Encina.DistributedLock.Redis;
using Encina.Messaging.Health;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Shouldly;

namespace Encina.UnitTests.DistributedLock.Redis;

/// <summary>
/// Extended DI registration tests for Redis distributed lock covering
/// overload null guards, health check registration, and option configuration.
/// </summary>
public sealed class ServiceCollectionExtensionsExtendedTests
{
    [Fact]
    public void AddEncinaDistributedLockRedis_WithConnectionStringAndOptions_NullConfigure_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaDistributedLockRedis("localhost:6379", null!));
    }

    [Fact]
    public void AddEncinaDistributedLockRedis_WithConnectionStringAndOptions_NullConnectionString_ThrowsArgumentException()
    {
        var services = new ServiceCollection();
        Should.Throw<ArgumentException>(() =>
            services.AddEncinaDistributedLockRedis((string)null!, _ => { }));
    }

    [Fact]
    public void AddEncinaDistributedLockRedis_WithMultiplexerAndOptions_NullMultiplexer_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();
        IConnectionMultiplexer? multiplexer = null;
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaDistributedLockRedis(multiplexer!, _ => { }));
    }

    [Fact]
    public void AddEncinaDistributedLockRedis_WithMultiplexerAndOptions_NullConfigure_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();
        var multiplexer = Substitute.For<IConnectionMultiplexer>();
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaDistributedLockRedis(multiplexer, null!));
    }

    [Fact]
    public void AddEncinaDistributedLockRedis_WithHealthCheckEnabled_RegistersHealthCheck()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var multiplexer = Substitute.For<IConnectionMultiplexer>();

        // Act
        services.AddEncinaDistributedLockRedis(multiplexer, options =>
        {
            options.ProviderHealthCheck.Enabled = true;
        });

        // Assert
        var sp = services.BuildServiceProvider();
        var healthChecks = sp.GetServices<IEncinaHealthCheck>();
        healthChecks.ShouldNotBeEmpty();
    }

    [Fact]
    public void AddEncinaDistributedLockRedis_WithHealthCheckDisabled_DoesNotRegisterHealthCheck()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var multiplexer = Substitute.For<IConnectionMultiplexer>();

        // Act
        services.AddEncinaDistributedLockRedis(multiplexer, options =>
        {
            options.ProviderHealthCheck.Enabled = false;
        });

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IEncinaHealthCheck));
        descriptor.ShouldBeNull();
    }

    [Fact]
    public void AddEncinaDistributedLockRedis_WithMultiplexer_RegistersSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var multiplexer = Substitute.For<IConnectionMultiplexer>();

        // Act
        services.AddEncinaDistributedLockRedis(multiplexer);

        // Assert
        var sp = services.BuildServiceProvider();
        var resolvedMultiplexer = sp.GetService<IConnectionMultiplexer>();
        resolvedMultiplexer.ShouldBeSameAs(multiplexer);
    }

    [Fact]
    public void AddEncinaDistributedLockRedis_WithConnectionStringAndOptions_ConfiguresOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var multiplexer = Substitute.For<IConnectionMultiplexer>();

        // Act
        services.AddEncinaDistributedLockRedis(multiplexer, options =>
        {
            options.Database = 3;
            options.KeyPrefix = "test";
            options.DefaultExpiry = TimeSpan.FromMinutes(5);
        });

        // Assert
        var sp = services.BuildServiceProvider();
        var opts = sp.GetRequiredService<IOptions<RedisLockOptions>>();
        opts.Value.Database.ShouldBe(3);
        opts.Value.KeyPrefix.ShouldBe("test");
        opts.Value.DefaultExpiry.ShouldBe(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void AddEncinaDistributedLockRedis_WithConnectionMultiplexerNoOptions_RegistersDefaultOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var multiplexer = Substitute.For<IConnectionMultiplexer>();

        // Act
        services.AddEncinaDistributedLockRedis(multiplexer);

        // Assert
        var sp = services.BuildServiceProvider();
        var provider = sp.GetService<IDistributedLockProvider>();
        provider.ShouldNotBeNull();
        provider.ShouldBeOfType<RedisDistributedLockProvider>();
    }
}
