using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Encina.DistributedLock.InMemory;

/// <summary>
/// Extension methods for configuring Encina in-memory distributed lock services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Encina in-memory distributed lock services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This provider is designed for testing and single-instance scenarios.
    /// It is NOT suitable for production multi-instance deployments.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // For testing
    /// services.AddEncinaDistributedLockInMemory();
    ///
    /// // In production, use Redis or SQL Server instead
    /// services.AddEncinaDistributedLockRedis("localhost:6379");
    /// </code>
    /// </example>
    public static IServiceCollection AddEncinaDistributedLockInMemory(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddEncinaDistributedLockInMemoryCore(null);
    }

    /// <summary>
    /// Adds Encina in-memory distributed lock services with options.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">The configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEncinaDistributedLockInMemory(
        this IServiceCollection services,
        Action<InMemoryLockOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        return services.AddEncinaDistributedLockInMemoryCore(configure);
    }

    private static IServiceCollection AddEncinaDistributedLockInMemoryCore(
        this IServiceCollection services,
        Action<InMemoryLockOptions>? configure)
    {
        var options = new InMemoryLockOptions();
        if (configure is not null)
        {
            configure(options);
            services.Configure(configure);
        }
        else
        {
            services.TryAddSingleton(Options.Create(options));
        }

        services.TryAddSingleton<IDistributedLockProvider, InMemoryDistributedLockProvider>();

        return services;
    }
}
