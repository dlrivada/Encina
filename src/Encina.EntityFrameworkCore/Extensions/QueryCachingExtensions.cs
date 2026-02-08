using Encina.Caching;
using Encina.EntityFrameworkCore.Caching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Encina.EntityFrameworkCore;

/// <summary>
/// Extension methods for configuring EF Core second-level query caching.
/// </summary>
/// <remarks>
/// <para>
/// Query caching provides automatic second-level caching for EF Core database queries.
/// When enabled, query results are cached and automatically invalidated when related
/// entities are modified via <c>SaveChanges</c>.
/// </para>
/// <para>
/// <b>Two-step registration</b>:
/// <list type="number">
/// <item><description><see cref="AddQueryCaching"/>: Registers services in the DI container</description></item>
/// <item><description><see cref="UseQueryCaching"/>: Adds the interceptor to DbContext options</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Recommended approach</b>: Use <see cref="ServiceCollectionExtensions.AddEncinaEntityFrameworkCore{TDbContext}(IServiceCollection, Action{Encina.Messaging.MessagingConfiguration})"/>
/// with <c>UseQueryCache = true</c>, which handles both steps automatically.
/// </para>
/// </remarks>
public static class QueryCachingExtensions
{
    /// <summary>
    /// Adds query caching services to the service collection with default options.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This registers:
    /// <list type="bullet">
    /// <item><description><see cref="QueryCacheOptions"/> configured via the <c>IOptions</c> pattern</description></item>
    /// <item><description><see cref="IQueryCacheKeyGenerator"/> as a singleton (<see cref="DefaultQueryCacheKeyGenerator"/>)</description></item>
    /// <item><description><see cref="QueryCacheInterceptor"/> as a singleton</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Requires</b>: An <see cref="ICacheProvider"/> must be registered in the service collection.
    /// Use one of the Encina caching packages (e.g., <c>Encina.Caching.Memory</c>,
    /// <c>Encina.Caching.Redis</c>) to register a cache provider.
    /// </para>
    /// <para>
    /// After calling this method, you still need to add the interceptor to your DbContext
    /// using <see cref="UseQueryCaching"/>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddQueryCaching();
    ///
    /// services.AddDbContext&lt;AppDbContext&gt;((sp, options) =>
    /// {
    ///     options.UseSqlServer(connectionString)
    ///            .UseQueryCaching(sp);
    /// });
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is <c>null</c>.</exception>
    public static IServiceCollection AddQueryCaching(this IServiceCollection services)
    {
        return services.AddQueryCaching(_ => { });
    }

    /// <summary>
    /// Adds query caching services to the service collection with custom options.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Action to configure the query cache options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This registers:
    /// <list type="bullet">
    /// <item><description><see cref="QueryCacheOptions"/> configured via the <c>IOptions</c> pattern (with provided configuration)</description></item>
    /// <item><description><see cref="IQueryCacheKeyGenerator"/> as a singleton (<see cref="DefaultQueryCacheKeyGenerator"/>)</description></item>
    /// <item><description><see cref="QueryCacheInterceptor"/> as a singleton</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Requires</b>: An <see cref="ICacheProvider"/> must be registered in the service collection.
    /// An <see cref="InvalidOperationException"/> will be thrown at runtime if no cache provider
    /// is available when the interceptor is resolved.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddQueryCaching(options =>
    /// {
    ///     options.Enabled = true;
    ///     options.DefaultExpiration = TimeSpan.FromMinutes(10);
    ///     options.KeyPrefix = "myapp:qc";
    ///     options.ExcludeType&lt;AuditLogEntry&gt;();
    /// });
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> or <paramref name="configure"/> is <c>null</c>.
    /// </exception>
    public static IServiceCollection AddQueryCaching(
        this IServiceCollection services,
        Action<QueryCacheOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        // Register options via IOptions pattern
        services.Configure(configure);

        // Register the default cache key generator (users can override with their own)
        services.TryAddSingleton<IQueryCacheKeyGenerator, DefaultQueryCacheKeyGenerator>();

        // Register the interceptor as singleton
        services.TryAddSingleton<QueryCacheInterceptor>();

        return services;
    }

    /// <summary>
    /// Adds the query cache interceptor to the DbContext options.
    /// </summary>
    /// <param name="optionsBuilder">The DbContext options builder.</param>
    /// <param name="serviceProvider">The service provider to resolve the interceptor from.</param>
    /// <returns>The options builder for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method retrieves the <see cref="QueryCacheInterceptor"/> from the
    /// service provider and adds it to the DbContext's interceptors collection.
    /// </para>
    /// <para>
    /// You must call <see cref="AddQueryCaching(IServiceCollection)"/> or
    /// <see cref="AddQueryCaching(IServiceCollection, Action{QueryCacheOptions})"/>
    /// before using this method to ensure the interceptor is registered.
    /// </para>
    /// <para>
    /// <b>Validation</b>: This method validates that an <see cref="ICacheProvider"/> is
    /// registered in the service collection. A descriptive <see cref="InvalidOperationException"/>
    /// is thrown if no cache provider is found.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddQueryCaching(options =>
    /// {
    ///     options.Enabled = true;
    ///     options.DefaultExpiration = TimeSpan.FromMinutes(10);
    /// });
    ///
    /// services.AddDbContext&lt;AppDbContext&gt;((sp, options) =>
    /// {
    ///     options.UseSqlServer(connectionString)
    ///            .UseQueryCaching(sp);
    /// });
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="optionsBuilder"/> or <paramref name="serviceProvider"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no <see cref="ICacheProvider"/> is registered in the service collection.
    /// </exception>
    public static DbContextOptionsBuilder UseQueryCaching(
        this DbContextOptionsBuilder optionsBuilder,
        IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(optionsBuilder);
        ArgumentNullException.ThrowIfNull(serviceProvider);

        // Validate that a cache provider is registered
        var cacheProvider = serviceProvider.GetService<ICacheProvider>();
        if (cacheProvider is null)
        {
            throw new InvalidOperationException(
                "No ICacheProvider is registered in the service collection. " +
                "Query caching requires a cache provider. Register one using an Encina caching package " +
                "(e.g., services.AddEncinaCachingMemory(), services.AddEncinaCachingRedis(), " +
                "services.AddEncinaCachingHybrid()).");
        }

        var interceptor = serviceProvider.GetRequiredService<QueryCacheInterceptor>();
        optionsBuilder.AddInterceptors(interceptor);

        return optionsBuilder;
    }
}
