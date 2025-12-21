using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace SimpleMediator.InMemory;

/// <summary>
/// Extension methods for configuring SimpleMediator In-Memory message bus.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds SimpleMediator In-Memory message bus services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSimpleMediatorInMemory(
        this IServiceCollection services,
        Action<SimpleMediatorInMemoryOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new SimpleMediatorInMemoryOptions();
        configure?.Invoke(options);

        services.Configure<SimpleMediatorInMemoryOptions>(opt =>
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
