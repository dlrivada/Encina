using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Encina.Tenancy.AspNetCore;

/// <summary>
/// Extension methods for configuring Encina tenancy ASP.NET Core integration.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Encina tenancy ASP.NET Core services including tenant resolvers.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> is null.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method registers:
    /// <list type="bullet">
    /// <item>Default tenant resolvers (header, claim, route, subdomain)</item>
    /// <item><see cref="TenancyAspNetCoreOptions"/> configuration</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Note:</b> This method does NOT register core tenancy services.
    /// Call <see cref="Tenancy.ServiceCollectionExtensions.AddEncinaTenancy"/>
    /// before this method.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddEncinaTenancy();          // Core tenancy (required first)
    /// services.AddEncinaTenancyAspNetCore(); // ASP.NET Core integration
    /// </code>
    /// </example>
    public static IServiceCollection AddEncinaTenancyAspNetCore(this IServiceCollection services)
    {
        return services.AddEncinaTenancyAspNetCore(_ => { });
    }

    /// <summary>
    /// Adds Encina tenancy ASP.NET Core services with custom configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Action to configure tenancy options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> or <paramref name="configure"/> is null.
    /// </exception>
    /// <example>
    /// <code>
    /// services.AddEncinaTenancy();
    /// services.AddEncinaTenancyAspNetCore(options =>
    /// {
    ///     options.HeaderResolver.HeaderName = "X-Organization-ID";
    ///     options.SubdomainResolver.Enabled = true;
    ///     options.SubdomainResolver.BaseDomain = "example.com";
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddEncinaTenancyAspNetCore(
        this IServiceCollection services,
        Action<TenancyAspNetCoreOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        // Configure options
        services.Configure(configure);

        // Register built-in resolvers (they check Enabled flag internally)
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ITenantResolver, HeaderTenantResolver>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ITenantResolver, ClaimTenantResolver>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ITenantResolver, RouteTenantResolver>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ITenantResolver, SubdomainTenantResolver>());

        return services;
    }

    /// <summary>
    /// Adds a custom tenant resolver to the resolver chain.
    /// </summary>
    /// <typeparam name="TResolver">The resolver type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> is null.
    /// </exception>
    /// <example>
    /// <code>
    /// services.AddEncinaTenancyAspNetCore()
    ///         .AddTenantResolver&lt;ApiKeyTenantResolver&gt;();
    /// </code>
    /// </example>
    public static IServiceCollection AddTenantResolver<TResolver>(this IServiceCollection services)
        where TResolver : class, ITenantResolver
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddEnumerable(ServiceDescriptor.Singleton<ITenantResolver, TResolver>());

        return services;
    }

    /// <summary>
    /// Adds a custom tenant resolver instance to the resolver chain.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="resolver">The resolver instance.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> or <paramref name="resolver"/> is null.
    /// </exception>
    /// <example>
    /// <code>
    /// services.AddEncinaTenancyAspNetCore()
    ///         .AddTenantResolver(new QueryStringTenantResolver("tenant"));
    /// </code>
    /// </example>
    public static IServiceCollection AddTenantResolver(
        this IServiceCollection services,
        ITenantResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(resolver);

        services.AddSingleton<ITenantResolver>(resolver);

        return services;
    }

    /// <summary>
    /// Adds a custom tenant resolver using a factory function.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="factory">Factory function to create the resolver.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> or <paramref name="factory"/> is null.
    /// </exception>
    /// <example>
    /// <code>
    /// services.AddEncinaTenancyAspNetCore()
    ///         .AddTenantResolver(sp =>
    ///         {
    ///             var cache = sp.GetRequiredService&lt;IMemoryCache&gt;();
    ///             return new CachedTenantResolver(cache);
    ///         });
    /// </code>
    /// </example>
    public static IServiceCollection AddTenantResolver(
        this IServiceCollection services,
        Func<IServiceProvider, ITenantResolver> factory)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(factory);

        services.AddSingleton<ITenantResolver>(factory);

        return services;
    }
}
