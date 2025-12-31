using Encina.DistributedLock;

using Microsoft.Extensions.DependencyInjection;

namespace Encina.Caching.Memory.Tests;

/// <summary>
/// Unit tests for <see cref="ServiceCollectionExtensions"/>.
/// </summary>
public sealed class ServiceCollectionExtensionsTests
{
    private static ServiceCollection CreateServicesWithLogging()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        return services;
    }

    [Fact]
    public void AddEncinaMemoryCache_WithNullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>("services", () =>
            services.AddEncinaMemoryCache());
    }

    [Fact]
    public void AddEncinaMemoryCache_RegistersMemoryCacheProvider()
    {
        // Arrange
        var services = CreateServicesWithLogging();

        // Act
        services.AddEncinaMemoryCache();
        var provider = services.BuildServiceProvider();

        // Assert
        var cacheProvider = provider.GetService<ICacheProvider>();
        cacheProvider.ShouldNotBeNull();
        cacheProvider.ShouldBeOfType<MemoryCacheProvider>();
    }

    [Fact]
    public void AddEncinaMemoryCache_RegistersPubSubProvider()
    {
        // Arrange
        var services = CreateServicesWithLogging();

        // Act
        services.AddEncinaMemoryCache();
        var provider = services.BuildServiceProvider();

        // Assert
        var pubSubProvider = provider.GetService<IPubSubProvider>();
        pubSubProvider.ShouldNotBeNull();
        pubSubProvider.ShouldBeOfType<MemoryPubSubProvider>();
    }

    [Fact]
    public void AddEncinaMemoryCache_RegistersDistributedLockProvider()
    {
        // Arrange
        var services = CreateServicesWithLogging();

        // Act
        services.AddEncinaMemoryCache();
        var provider = services.BuildServiceProvider();

        // Assert
        var lockProvider = provider.GetService<IDistributedLockProvider>();
        lockProvider.ShouldNotBeNull();
        lockProvider.ShouldBeOfType<MemoryDistributedLockProvider>();
    }

    [Fact]
    public void AddEncinaMemoryCache_WithConfiguration_AppliesOptions()
    {
        // Arrange
        var services = CreateServicesWithLogging();
        var expectedExpiration = TimeSpan.FromMinutes(15);

        // Act
        services.AddEncinaMemoryCache(options =>
        {
            options.DefaultExpiration = expectedExpiration;
        });
        var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetService<IOptions<MemoryCacheOptions>>();
        options.ShouldNotBeNull();
        options!.Value.DefaultExpiration.ShouldBe(expectedExpiration);
    }

    [Fact]
    public void AddEncinaMemoryCache_WithoutConfiguration_UsesDefaults()
    {
        // Arrange
        var services = CreateServicesWithLogging();

        // Act
        services.AddEncinaMemoryCache();
        var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetService<IOptions<MemoryCacheOptions>>();
        options.ShouldNotBeNull();
        options!.Value.DefaultExpiration.ShouldBe(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void AddEncinaMemoryCache_CalledMultipleTimes_DoesNotDuplicateRegistrations()
    {
        // Arrange
        var services = CreateServicesWithLogging();

        // Act
        services.AddEncinaMemoryCache();
        services.AddEncinaMemoryCache();
        var provider = services.BuildServiceProvider();

        // Assert
        var cacheProviders = provider.GetServices<ICacheProvider>().ToList();
        cacheProviders.Count.ShouldBe(1);
    }

    [Fact]
    public void AddEncinaMemoryCache_ReturnsServiceCollection()
    {
        // Arrange
        var services = CreateServicesWithLogging();

        // Act
        var result = services.AddEncinaMemoryCache();

        // Assert
        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddEncinaMemoryCache_RegistersIMemoryCache()
    {
        // Arrange
        var services = CreateServicesWithLogging();

        // Act
        services.AddEncinaMemoryCache();
        var provider = services.BuildServiceProvider();

        // Assert
        var memoryCache = provider.GetService<IMemoryCache>();
        memoryCache.ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaMemoryCache_CacheProviderIsSingleton()
    {
        // Arrange
        var services = CreateServicesWithLogging();

        // Act
        services.AddEncinaMemoryCache();
        var provider = services.BuildServiceProvider();

        // Assert
        var cacheProvider1 = provider.GetRequiredService<ICacheProvider>();
        var cacheProvider2 = provider.GetRequiredService<ICacheProvider>();
        cacheProvider1.ShouldBeSameAs(cacheProvider2);
    }

    [Fact]
    public void AddEncinaMemoryCache_PubSubProviderIsSingleton()
    {
        // Arrange
        var services = CreateServicesWithLogging();

        // Act
        services.AddEncinaMemoryCache();
        var provider = services.BuildServiceProvider();

        // Assert
        var pubSubProvider1 = provider.GetRequiredService<IPubSubProvider>();
        var pubSubProvider2 = provider.GetRequiredService<IPubSubProvider>();
        pubSubProvider1.ShouldBeSameAs(pubSubProvider2);
    }

    [Fact]
    public void AddEncinaMemoryCache_LockProviderIsSingleton()
    {
        // Arrange
        var services = CreateServicesWithLogging();

        // Act
        services.AddEncinaMemoryCache();
        var provider = services.BuildServiceProvider();

        // Assert
        var lockProvider1 = provider.GetRequiredService<IDistributedLockProvider>();
        var lockProvider2 = provider.GetRequiredService<IDistributedLockProvider>();
        lockProvider1.ShouldBeSameAs(lockProvider2);
    }
}
