using Encina.Caching;
using Encina.Caching.Hybrid;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Encina.UnitTests.Caching.Hybrid;

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

    #region AddEncinaHybridCache Tests

    [Fact]
    public void AddEncinaHybridCache_WithNullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>("services", () =>
            services.AddEncinaHybridCache());
    }

    [Fact]
    public void AddEncinaHybridCache_RegistersHybridCacheProvider()
    {
        // Arrange
        var services = CreateServicesWithLogging();

        // Act
        services.AddEncinaHybridCache();
        var provider = services.BuildServiceProvider();

        // Assert
        var cacheProvider = provider.GetRequiredService<ICacheProvider>();
        cacheProvider.ShouldBeOfType<HybridCacheProvider>();
    }

    [Fact]
    public void AddEncinaHybridCache_RegistersHybridCache()
    {
        // Arrange
        var services = CreateServicesWithLogging();

        // Act
        services.AddEncinaHybridCache();
        var provider = services.BuildServiceProvider();

        // Assert
        var hybridCache = provider.GetRequiredService<HybridCache>();
        hybridCache.ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaHybridCache_WithConfiguration_AppliesOptions()
    {
        // Arrange
        var services = CreateServicesWithLogging();
        var expectedExpiration = TimeSpan.FromMinutes(15);

        // Act
        services.AddEncinaHybridCache(options =>
        {
            options.DefaultExpiration = expectedExpiration;
        });
        var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetRequiredService<IOptions<HybridCacheProviderOptions>>();
        options.Value.DefaultExpiration.ShouldBe(expectedExpiration);
    }

    [Fact]
    public void AddEncinaHybridCache_WithoutConfiguration_UsesDefaults()
    {
        // Arrange
        var services = CreateServicesWithLogging();

        // Act
        services.AddEncinaHybridCache();
        var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetRequiredService<IOptions<HybridCacheProviderOptions>>();
        options.Value.DefaultExpiration.ShouldBe(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void AddEncinaHybridCache_CalledMultipleTimes_DoesNotDuplicateRegistrations()
    {
        // Arrange
        var services = CreateServicesWithLogging();

        // Act
        services.AddEncinaHybridCache();
        services.AddEncinaHybridCache();
        var provider = services.BuildServiceProvider();

        // Assert
        var cacheProviders = provider.GetServices<ICacheProvider>().ToList();
        cacheProviders.Count.ShouldBe(1);
    }

    [Fact]
    public void AddEncinaHybridCache_ReturnsServiceCollection()
    {
        // Arrange
        var services = CreateServicesWithLogging();

        // Act
        var result = services.AddEncinaHybridCache();

        // Assert
        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddEncinaHybridCache_CacheProviderIsSingleton()
    {
        // Arrange
        var services = CreateServicesWithLogging();

        // Act
        services.AddEncinaHybridCache();
        var provider = services.BuildServiceProvider();

        // Assert
        var cacheProvider1 = provider.GetRequiredService<ICacheProvider>();
        var cacheProvider2 = provider.GetRequiredService<ICacheProvider>();
        cacheProvider1.ShouldBeSameAs(cacheProvider2);
    }

    [Fact]
    public void AddEncinaHybridCache_WithLocalCacheExpiration_AppliesOption()
    {
        // Arrange
        var services = CreateServicesWithLogging();
        var localExpiration = TimeSpan.FromMinutes(2);

        // Act
        services.AddEncinaHybridCache(options =>
        {
            options.LocalCacheExpiration = localExpiration;
        });
        var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetRequiredService<IOptions<HybridCacheProviderOptions>>();
        options.Value.LocalCacheExpiration.ShouldBe(localExpiration);
    }

    #endregion

    #region AddEncinaHybridCacheWithOptions Tests

    [Fact]
    public void AddEncinaHybridCacheWithOptions_WithNullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>("services", () =>
            services.AddEncinaHybridCacheWithOptions(_ => { }));
    }

    [Fact]
    public void AddEncinaHybridCacheWithOptions_WithNullConfigureHybridCache_ThrowsArgumentNullException()
    {
        // Arrange
        var services = CreateServicesWithLogging();

        // Act & Assert
        Assert.Throws<ArgumentNullException>("configureHybridCache", () =>
            services.AddEncinaHybridCacheWithOptions(null!));
    }

    [Fact]
    public void AddEncinaHybridCacheWithOptions_ConfiguresHybridCacheOptions()
    {
        // Arrange
        var services = CreateServicesWithLogging();

        // Act
        services.AddEncinaHybridCacheWithOptions(options =>
        {
            options.MaximumPayloadBytes = 1024 * 1024; // 1MB
        });
        var provider = services.BuildServiceProvider();

        // Assert
        var cacheProvider = provider.GetRequiredService<ICacheProvider>();
        cacheProvider.ShouldBeOfType<HybridCacheProvider>();
    }

    [Fact]
    public void AddEncinaHybridCacheWithOptions_WithProviderOptions_AppliesBoth()
    {
        // Arrange
        var services = CreateServicesWithLogging();
        var expectedExpiration = TimeSpan.FromMinutes(20);

        // Act
        services.AddEncinaHybridCacheWithOptions(
            hybridOptions =>
            {
                hybridOptions.MaximumPayloadBytes = 2 * 1024 * 1024; // 2MB
            },
            providerOptions =>
            {
                providerOptions.DefaultExpiration = expectedExpiration;
            });
        var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetRequiredService<IOptions<HybridCacheProviderOptions>>();
        options.Value.DefaultExpiration.ShouldBe(expectedExpiration);

        var cacheProvider = provider.GetRequiredService<ICacheProvider>();
        cacheProvider.ShouldBeOfType<HybridCacheProvider>();
    }

    [Fact]
    public void AddEncinaHybridCacheWithOptions_ReturnsServiceCollection()
    {
        // Arrange
        var services = CreateServicesWithLogging();

        // Act
        var result = services.AddEncinaHybridCacheWithOptions(_ => { });

        // Assert
        result.ShouldBeSameAs(services);
    }

    #endregion
}
