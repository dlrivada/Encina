using Microsoft.Extensions.DependencyInjection;

namespace Encina.DistributedLock;

/// <summary>
/// Extension methods for configuring Encina distributed lock services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Encina distributed lock core services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method only registers core services. You must also register a provider:
    /// </para>
    /// <list type="bullet">
    /// <item><description><c>AddEncinaDistributedLockInMemory()</c> - For testing and single-instance scenarios</description></item>
    /// <item><description><c>AddEncinaDistributedLockRedis()</c> - For production multi-instance scenarios</description></item>
    /// <item><description><c>AddEncinaDistributedLockSqlServer()</c> - For SQL Server-based locking</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddEncinaDistributedLock();
    /// services.AddEncinaDistributedLockRedis("localhost:6379");
    /// </code>
    /// </example>
    public static IServiceCollection AddEncinaDistributedLock(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Core services are registered by individual providers
        // This method is a marker for documentation purposes

        return services;
    }

    /// <summary>
    /// Adds Encina distributed lock core services with options.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">The configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEncinaDistributedLock(
        this IServiceCollection services,
        Action<DistributedLockOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.Configure(configure);

        return services;
    }
}
