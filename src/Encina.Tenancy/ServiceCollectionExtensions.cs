using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Encina.Tenancy;

/// <summary>
/// Extension methods for configuring Encina multi-tenancy services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Encina multi-tenancy services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers the core tenancy abstractions:
    /// </para>
    /// <list type="bullet">
    /// <item><see cref="ITenantProvider"/> - For accessing current tenant context</item>
    /// <item><see cref="ITenantStore"/> - For tenant metadata (defaults to <see cref="InMemoryTenantStore"/>)</item>
    /// <item><see cref="TenancyOptions"/> - Configuration via IOptions pattern</item>
    /// </list>
    /// <para>
    /// For ASP.NET Core applications, also call <c>AddEncinaTenancyAspNetCore()</c> from
    /// the <c>Encina.Tenancy.AspNetCore</c> package to register tenant resolution middleware.
    /// </para>
    /// <para>
    /// For provider-specific integration, use the extension methods on each provider:
    /// </para>
    /// <list type="bullet">
    /// <item><c>AddEncinaEntityFrameworkCore(..., config => config.UseTenancy = true)</c></item>
    /// <item><c>AddEncinaDapperWithTenancy(...)</c></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Basic setup with static tenants
    /// services.AddEncinaTenancy(options =>
    /// {
    ///     options.DefaultStrategy = TenantIsolationStrategy.SharedSchema;
    ///     options.RequireTenant = true;
    ///
    ///     options.Tenants.Add(new TenantInfo(
    ///         TenantId: "tenant-1",
    ///         Name: "Acme Corp",
    ///         Strategy: TenantIsolationStrategy.SharedSchema));
    ///
    ///     options.Tenants.Add(new TenantInfo(
    ///         TenantId: "tenant-2",
    ///         Name: "Enterprise Inc",
    ///         Strategy: TenantIsolationStrategy.DatabasePerTenant,
    ///         ConnectionString: "Server=enterprise-db;Database=EnterpriseData;..."));
    /// });
    ///
    /// // With custom tenant store
    /// services.AddEncinaTenancy();
    /// services.AddSingleton&lt;ITenantStore, DatabaseTenantStore&gt;();
    /// </code>
    /// </example>
    public static IServiceCollection AddEncinaTenancy(
        this IServiceCollection services,
        Action<TenancyOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Configure options
        var options = new TenancyOptions();
        configure?.Invoke(options);

        services.Configure<TenancyOptions>(opt =>
        {
            opt.DefaultStrategy = options.DefaultStrategy;
            opt.RequireTenant = options.RequireTenant;
            opt.TenantIdPropertyName = options.TenantIdPropertyName;
            opt.DefaultConnectionString = options.DefaultConnectionString;
            opt.DefaultSchemaName = options.DefaultSchemaName;
            opt.ValidateTenantOnRequest = options.ValidateTenantOnRequest;

            foreach (var tenant in options.Tenants)
            {
                opt.Tenants.Add(tenant);
            }
        });

        // Register default tenant store if not already registered
        // Uses InMemoryTenantStore populated from options.Tenants
        services.TryAddSingleton<ITenantStore>(sp =>
        {
            var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<TenancyOptions>>().Value;
            return new InMemoryTenantStore(opts.Tenants);
        });

        // Register InMemoryTenantStore directly for programmatic access
        services.TryAddSingleton(sp =>
        {
            var store = sp.GetRequiredService<ITenantStore>();
            return store as InMemoryTenantStore
                ?? throw new InvalidOperationException(
                    "InMemoryTenantStore is not registered. " +
                    "If using a custom ITenantStore, inject ITenantStore instead.");
        });

        // Register tenant provider as scoped (depends on request context)
        services.TryAddScoped<ITenantProvider, DefaultTenantProvider>();

        // Register connection options
        services.TryAddSingleton<Microsoft.Extensions.Options.IOptions<TenantConnectionOptions>>(
            _ => Microsoft.Extensions.Options.Options.Create(new TenantConnectionOptions
            {
                DefaultConnectionString = options.DefaultConnectionString
            }));

        return services;
    }

    /// <summary>
    /// Adds a custom tenant store implementation.
    /// </summary>
    /// <typeparam name="TStore">The type of the tenant store.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// Use this to replace the default <see cref="InMemoryTenantStore"/> with a
    /// database-backed or external service implementation.
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddEncinaTenancy();
    /// services.AddTenantStore&lt;DatabaseTenantStore&gt;();
    /// </code>
    /// </example>
    public static IServiceCollection AddTenantStore<TStore>(this IServiceCollection services)
        where TStore : class, ITenantStore
    {
        ArgumentNullException.ThrowIfNull(services);

        services.RemoveAll<ITenantStore>();
        services.AddSingleton<ITenantStore, TStore>();

        return services;
    }

    /// <summary>
    /// Adds a custom tenant store implementation with a factory.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="factory">The factory function to create the tenant store.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddEncinaTenancy();
    /// services.AddTenantStore(sp =>
    /// {
    ///     var dbContext = sp.GetRequiredService&lt;TenantDbContext&gt;();
    ///     return new DatabaseTenantStore(dbContext);
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddTenantStore(
        this IServiceCollection services,
        Func<IServiceProvider, ITenantStore> factory)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(factory);

        services.RemoveAll<ITenantStore>();
        services.AddSingleton(factory);

        return services;
    }

    /// <summary>
    /// Configures tenant connection options.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">The configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddEncinaTenancy()
    ///     .ConfigureTenantConnections(options =>
    ///     {
    ///         options.DefaultConnectionString = "Server=localhost;...";
    ///         options.AutoOpenConnections = true;
    ///         options.ConnectionTimeoutSeconds = 60;
    ///     });
    /// </code>
    /// </example>
    public static IServiceCollection ConfigureTenantConnections(
        this IServiceCollection services,
        Action<TenantConnectionOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.Configure(configure);

        return services;
    }
}
