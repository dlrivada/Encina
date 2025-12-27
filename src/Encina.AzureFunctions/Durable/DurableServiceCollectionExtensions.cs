using Encina.Messaging.Health;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Encina.AzureFunctions.Durable;

/// <summary>
/// Extension methods for registering Durable Functions services.
/// </summary>
public static class DurableServiceCollectionExtensions
{
    /// <summary>
    /// Adds Encina Durable Functions integration services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional action to configure options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddEncinaDurableFunctions(options =>
    /// {
    ///     options.DefaultMaxRetries = 5;
    ///     options.DefaultFirstRetryInterval = TimeSpan.FromSeconds(10);
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddEncinaDurableFunctions(
        this IServiceCollection services,
        Action<DurableFunctionsOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var optionsBuilder = services.AddOptions<DurableFunctionsOptions>();

        if (configure != null)
        {
            optionsBuilder.Configure(configure);
        }

        // Register health check
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IEncinaHealthCheck, DurableFunctionsHealthCheck>());

        return services;
    }
}
