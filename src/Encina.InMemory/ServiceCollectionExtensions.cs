using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Encina.InMemory;

/// <summary>
/// Extension methods for configuring Encina In-Memory message bus.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Encina In-Memory message bus services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEncinaInMemory(
        this IServiceCollection services,
        Action<EncinaInMemoryOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new EncinaInMemoryOptions();
        configure?.Invoke(options);

        services.Configure<EncinaInMemoryOptions>(opt =>
        {
            opt.BoundedCapacity = options.BoundedCapacity;
            opt.UseUnboundedChannel = options.UseUnboundedChannel;
            opt.FullMode = options.FullMode;
            opt.WorkerCount = options.WorkerCount;
            opt.AllowSynchronousContinuations = options.AllowSynchronousContinuations;
        });

        services.TryAddSingleton<IInMemoryMessageBus, InMemoryMessageBus>();

        return services;
    }
}
