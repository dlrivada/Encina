using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Encina.Caching.Memory;

/// <summary>
/// Extension methods for configuring Encina in-memory caching services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Encina in-memory caching services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action for memory cache options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers:
    /// </para>
    /// <list type="bullet">
    /// <item><description><see cref="ICacheProvider"/> - Using <see cref="MemoryCacheProvider"/></description></item>
    /// <item><description><see cref="IPubSubProvider"/> - Using <see cref="MemoryPubSubProvider"/></description></item>
    /// <item><description><see cref="IDistributedLockProvider"/> - Using <see cref="MemoryDistributedLockProvider"/></description></item>
    /// </list>
    /// <para>
    /// Use this for single-instance applications or development/testing scenarios.
    /// For distributed caching, use Redis, Garnet, or NCache providers.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddEncinaCaching(options =>
    /// {
    ///     options.EnableQueryCaching = true;
    ///     options.EnableCacheInvalidation = true;
    /// });
    ///
    /// services.AddEncinaMemoryCache(options =>
    /// {
    ///     options.DefaultExpiration = TimeSpan.FromMinutes(10);
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddEncinaMemoryCache(
        this IServiceCollection services,
        Action<MemoryCacheOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Add Microsoft.Extensions.Caching.Memory
        services.AddMemoryCache();

        // Configure options
        if (configure is not null)
        {
            services.Configure(configure);
        }
        else
        {
            services.TryAddSingleton(Options.Create(new MemoryCacheOptions()));
        }

        // Register providers
        services.TryAddSingleton<ICacheProvider, MemoryCacheProvider>();
        services.TryAddSingleton<IPubSubProvider, MemoryPubSubProvider>();
        services.TryAddSingleton<IDistributedLockProvider, MemoryDistributedLockProvider>();

        return services;
    }
}
