using Microsoft.Extensions.DependencyInjection;

namespace SimpleMediator.Caching.Hybrid.Tests;

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

    #region AddSimpleMediatorHybridCache Tests

    [Fact]
    public void AddSimpleMediatorHybridCache_WithNullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>("services", () =>
            services.AddSimpleMediatorHybridCache());
    }

    [Fact]
    public void AddSimpleMediatorHybridCache_RegistersHybridCacheProvider()
    {
        // Arrange
        var services = CreateServicesWithLogging();

        // Act
        services.AddSimpleMediatorHybridCache();
        var provider = services.BuildServiceProvider();

        // Assert
        var cacheProvider = provider.GetService<ICacheProvider>();
        cacheProvider.Should().NotBeNull();
        cacheProvider.Should().BeOfType<HybridCacheProvider>();
    }

    [Fact]
    public void AddSimpleMediatorHybridCache_RegistersHybridCache()
    {
        // Arrange
        var services = CreateServicesWithLogging();

        // Act
        services.AddSimpleMediatorHybridCache();
        var provider = services.BuildServiceProvider();

        // Assert
        var hybridCache = provider.GetService<HybridCache>();
        hybridCache.Should().NotBeNull();
    }

    [Fact]
    public void AddSimpleMediatorHybridCache_WithConfiguration_AppliesOptions()
    {
        // Arrange
        var services = CreateServicesWithLogging();
        var expectedExpiration = TimeSpan.FromMinutes(15);

        // Act
        services.AddSimpleMediatorHybridCache(options =>
        {
            options.DefaultExpiration = expectedExpiration;
        });
        var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetService<IOptions<HybridCacheProviderOptions>>();
        options.Should().NotBeNull();
        options!.Value.DefaultExpiration.Should().Be(expectedExpiration);
    }

    [Fact]
    public void AddSimpleMediatorHybridCache_WithoutConfiguration_UsesDefaults()
    {
        // Arrange
        var services = CreateServicesWithLogging();

        // Act
        services.AddSimpleMediatorHybridCache();
        var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetService<IOptions<HybridCacheProviderOptions>>();
        options.Should().NotBeNull();
        options!.Value.DefaultExpiration.Should().Be(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void AddSimpleMediatorHybridCache_CalledMultipleTimes_DoesNotDuplicateRegistrations()
    {
        // Arrange
        var services = CreateServicesWithLogging();

        // Act
        services.AddSimpleMediatorHybridCache();
        services.AddSimpleMediatorHybridCache();
        var provider = services.BuildServiceProvider();

        // Assert
        var cacheProviders = provider.GetServices<ICacheProvider>().ToList();
        cacheProviders.Should().HaveCount(1);
    }

    [Fact]
    public void AddSimpleMediatorHybridCache_ReturnsServiceCollection()
    {
        // Arrange
        var services = CreateServicesWithLogging();

        // Act
        var result = services.AddSimpleMediatorHybridCache();

        // Assert
        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddSimpleMediatorHybridCache_CacheProviderIsSingleton()
    {
        // Arrange
        var services = CreateServicesWithLogging();

        // Act
        services.AddSimpleMediatorHybridCache();
        var provider = services.BuildServiceProvider();

        // Assert
        var cacheProvider1 = provider.GetRequiredService<ICacheProvider>();
        var cacheProvider2 = provider.GetRequiredService<ICacheProvider>();
        cacheProvider1.Should().BeSameAs(cacheProvider2);
    }

    [Fact]
    public void AddSimpleMediatorHybridCache_WithLocalCacheExpiration_AppliesOption()
    {
        // Arrange
        var services = CreateServicesWithLogging();
        var localExpiration = TimeSpan.FromMinutes(2);

        // Act
        services.AddSimpleMediatorHybridCache(options =>
        {
            options.LocalCacheExpiration = localExpiration;
        });
        var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetService<IOptions<HybridCacheProviderOptions>>();
        options.Should().NotBeNull();
        options!.Value.LocalCacheExpiration.Should().Be(localExpiration);
    }

    #endregion

    #region AddSimpleMediatorHybridCacheWithOptions Tests

    [Fact]
    public void AddSimpleMediatorHybridCacheWithOptions_WithNullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>("services", () =>
            services.AddSimpleMediatorHybridCacheWithOptions(_ => { }));
    }

    [Fact]
    public void AddSimpleMediatorHybridCacheWithOptions_WithNullConfigureHybridCache_ThrowsArgumentNullException()
    {
        // Arrange
        var services = CreateServicesWithLogging();

        // Act & Assert
        Assert.Throws<ArgumentNullException>("configureHybridCache", () =>
            services.AddSimpleMediatorHybridCacheWithOptions(null!));
    }

    [Fact]
    public void AddSimpleMediatorHybridCacheWithOptions_ConfiguresHybridCacheOptions()
    {
        // Arrange
        var services = CreateServicesWithLogging();

        // Act
        services.AddSimpleMediatorHybridCacheWithOptions(options =>
        {
            options.MaximumPayloadBytes = 1024 * 1024; // 1MB
        });
        var provider = services.BuildServiceProvider();

        // Assert
        var cacheProvider = provider.GetService<ICacheProvider>();
        cacheProvider.Should().NotBeNull();
        cacheProvider.Should().BeOfType<HybridCacheProvider>();
    }

    [Fact]
    public void AddSimpleMediatorHybridCacheWithOptions_WithProviderOptions_AppliesBoth()
    {
        // Arrange
        var services = CreateServicesWithLogging();
        var expectedExpiration = TimeSpan.FromMinutes(20);

        // Act
        services.AddSimpleMediatorHybridCacheWithOptions(
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
        var options = provider.GetService<IOptions<HybridCacheProviderOptions>>();
        options.Should().NotBeNull();
        options!.Value.DefaultExpiration.Should().Be(expectedExpiration);

        var cacheProvider = provider.GetService<ICacheProvider>();
        cacheProvider.Should().NotBeNull();
    }

    [Fact]
    public void AddSimpleMediatorHybridCacheWithOptions_ReturnsServiceCollection()
    {
        // Arrange
        var services = CreateServicesWithLogging();

        // Act
        var result = services.AddSimpleMediatorHybridCacheWithOptions(_ => { });

        // Assert
        result.Should().BeSameAs(services);
    }

    #endregion
}
