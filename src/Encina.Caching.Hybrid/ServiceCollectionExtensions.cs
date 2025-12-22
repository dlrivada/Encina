using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Encina.Caching.Hybrid;

/// <summary>
/// Extension methods for registering HybridCache caching services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the HybridCache provider to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Optional action to configure the provider options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers <see cref="HybridCacheProvider"/> as the <see cref="ICacheProvider"/> implementation.
    /// </para>
    /// <para>
    /// HybridCache must be configured separately using <c>AddHybridCache</c> from Microsoft.Extensions.Caching.Hybrid.
    /// </para>
    /// <example>
    /// <code>
    /// services.AddHybridCache(options =>
    /// {
    ///     options.MaximumPayloadBytes = 1024 * 1024; // 1MB
    ///     options.MaximumKeyLength = 1024;
    ///     options.DefaultEntryOptions = new HybridCacheEntryOptions
    ///     {
    ///         Expiration = TimeSpan.FromMinutes(5),
    ///         LocalCacheExpiration = TimeSpan.FromMinutes(1)
    ///     };
    /// });
    ///
    /// // Add Redis as L2 distributed cache
    /// services.AddStackExchangeRedisCache(options =>
    /// {
    ///     options.Configuration = "localhost:6379";
    /// });
    ///
    /// // Register the Encina HybridCache provider
    /// services.AddEncinaHybridCache();
    /// </code>
    /// </example>
    /// </remarks>
    public static IServiceCollection AddEncinaHybridCache(
        this IServiceCollection services,
        Action<HybridCacheProviderOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register HybridCache with default settings if not already registered
#pragma warning disable EXTEXP0018 // HybridCache is experimental
        services.AddHybridCache();
#pragma warning restore EXTEXP0018

        // Register distributed memory cache as L2 if not already registered
        services.AddDistributedMemoryCache();

        if (configureOptions is not null)
        {
            services.Configure(configureOptions);
        }
        else
        {
            services.TryAddSingleton(Options.Create(new HybridCacheProviderOptions()));
        }

        services.TryAddSingleton<ICacheProvider, HybridCacheProvider>();

        return services;
    }

    /// <summary>
    /// Adds the HybridCache provider with full configuration including HybridCache setup.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureHybridCache">Action to configure HybridCache.</param>
    /// <param name="configureProvider">Optional action to configure the provider options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddEncinaHybridCacheWithOptions(
    ///     hybridCache => hybridCache
    ///         .MaximumPayloadBytes = 1024 * 1024,
    ///     provider =>
    ///     {
    ///         provider.DefaultExpiration = TimeSpan.FromMinutes(10);
    ///         provider.LocalCacheExpiration = TimeSpan.FromMinutes(2);
    ///     });
    /// </code>
    /// </example>
    public static IServiceCollection AddEncinaHybridCacheWithOptions(
        this IServiceCollection services,
        Action<HybridCacheOptions> configureHybridCache,
        Action<HybridCacheProviderOptions>? configureProvider = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureHybridCache);

#pragma warning disable EXTEXP0018 // HybridCache is experimental
        services.AddHybridCache(configureHybridCache);
#pragma warning restore EXTEXP0018

        return services.AddEncinaHybridCache(configureProvider);
    }
}
