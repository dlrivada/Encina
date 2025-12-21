using Microsoft.Extensions.DependencyInjection;

namespace SimpleMediator.Caching.Memory.Tests;

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
    public void AddSimpleMediatorMemoryCache_WithNullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>("services", () =>
            services.AddSimpleMediatorMemoryCache());
    }

    [Fact]
    public void AddSimpleMediatorMemoryCache_RegistersMemoryCacheProvider()
    {
        // Arrange
        var services = CreateServicesWithLogging();

        // Act
        services.AddSimpleMediatorMemoryCache();
        var provider = services.BuildServiceProvider();

        // Assert
        var cacheProvider = provider.GetService<ICacheProvider>();
        cacheProvider.Should().NotBeNull();
        cacheProvider.Should().BeOfType<MemoryCacheProvider>();
    }

    [Fact]
    public void AddSimpleMediatorMemoryCache_RegistersPubSubProvider()
    {
        // Arrange
        var services = CreateServicesWithLogging();

        // Act
        services.AddSimpleMediatorMemoryCache();
        var provider = services.BuildServiceProvider();

        // Assert
        var pubSubProvider = provider.GetService<IPubSubProvider>();
        pubSubProvider.Should().NotBeNull();
        pubSubProvider.Should().BeOfType<MemoryPubSubProvider>();
    }

    [Fact]
    public void AddSimpleMediatorMemoryCache_RegistersDistributedLockProvider()
    {
        // Arrange
        var services = CreateServicesWithLogging();

        // Act
        services.AddSimpleMediatorMemoryCache();
        var provider = services.BuildServiceProvider();

        // Assert
        var lockProvider = provider.GetService<IDistributedLockProvider>();
        lockProvider.Should().NotBeNull();
        lockProvider.Should().BeOfType<MemoryDistributedLockProvider>();
    }

    [Fact]
    public void AddSimpleMediatorMemoryCache_WithConfiguration_AppliesOptions()
    {
        // Arrange
        var services = CreateServicesWithLogging();
        var expectedExpiration = TimeSpan.FromMinutes(15);

        // Act
        services.AddSimpleMediatorMemoryCache(options =>
        {
            options.DefaultExpiration = expectedExpiration;
        });
        var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetService<IOptions<MemoryCacheOptions>>();
        options.Should().NotBeNull();
        options!.Value.DefaultExpiration.Should().Be(expectedExpiration);
    }

    [Fact]
    public void AddSimpleMediatorMemoryCache_WithoutConfiguration_UsesDefaults()
    {
        // Arrange
        var services = CreateServicesWithLogging();

        // Act
        services.AddSimpleMediatorMemoryCache();
        var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetService<IOptions<MemoryCacheOptions>>();
        options.Should().NotBeNull();
        options!.Value.DefaultExpiration.Should().Be(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void AddSimpleMediatorMemoryCache_CalledMultipleTimes_DoesNotDuplicateRegistrations()
    {
        // Arrange
        var services = CreateServicesWithLogging();

        // Act
        services.AddSimpleMediatorMemoryCache();
        services.AddSimpleMediatorMemoryCache();
        var provider = services.BuildServiceProvider();

        // Assert
        var cacheProviders = provider.GetServices<ICacheProvider>().ToList();
        cacheProviders.Should().HaveCount(1);
    }

    [Fact]
    public void AddSimpleMediatorMemoryCache_ReturnsServiceCollection()
    {
        // Arrange
        var services = CreateServicesWithLogging();

        // Act
        var result = services.AddSimpleMediatorMemoryCache();

        // Assert
        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddSimpleMediatorMemoryCache_RegistersIMemoryCache()
    {
        // Arrange
        var services = CreateServicesWithLogging();

        // Act
        services.AddSimpleMediatorMemoryCache();
        var provider = services.BuildServiceProvider();

        // Assert
        var memoryCache = provider.GetService<IMemoryCache>();
        memoryCache.Should().NotBeNull();
    }

    [Fact]
    public void AddSimpleMediatorMemoryCache_CacheProviderIsSingleton()
    {
        // Arrange
        var services = CreateServicesWithLogging();

        // Act
        services.AddSimpleMediatorMemoryCache();
        var provider = services.BuildServiceProvider();

        // Assert
        var cacheProvider1 = provider.GetRequiredService<ICacheProvider>();
        var cacheProvider2 = provider.GetRequiredService<ICacheProvider>();
        cacheProvider1.Should().BeSameAs(cacheProvider2);
    }

    [Fact]
    public void AddSimpleMediatorMemoryCache_PubSubProviderIsSingleton()
    {
        // Arrange
        var services = CreateServicesWithLogging();

        // Act
        services.AddSimpleMediatorMemoryCache();
        var provider = services.BuildServiceProvider();

        // Assert
        var pubSubProvider1 = provider.GetRequiredService<IPubSubProvider>();
        var pubSubProvider2 = provider.GetRequiredService<IPubSubProvider>();
        pubSubProvider1.Should().BeSameAs(pubSubProvider2);
    }

    [Fact]
    public void AddSimpleMediatorMemoryCache_LockProviderIsSingleton()
    {
        // Arrange
        var services = CreateServicesWithLogging();

        // Act
        services.AddSimpleMediatorMemoryCache();
        var provider = services.BuildServiceProvider();

        // Assert
        var lockProvider1 = provider.GetRequiredService<IDistributedLockProvider>();
        var lockProvider2 = provider.GetRequiredService<IDistributedLockProvider>();
        lockProvider1.Should().BeSameAs(lockProvider2);
    }
}
