using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StackExchange.Redis;

namespace SimpleMediator.Redis.PubSub;

/// <summary>
/// Extension methods for configuring SimpleMediator Redis Pub/Sub integration.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds SimpleMediator Redis Pub/Sub integration services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSimpleMediatorRedisPubSub(
        this IServiceCollection services,
        Action<SimpleMediatorRedisPubSubOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new SimpleMediatorRedisPubSubOptions();
        configure?.Invoke(options);

        services.Configure<SimpleMediatorRedisPubSubOptions>(opt =>
        {
            opt.ConnectionString = options.ConnectionString;
            opt.ChannelPrefix = options.ChannelPrefix;
            opt.CommandChannel = options.CommandChannel;
            opt.EventChannel = options.EventChannel;
            opt.UsePatternSubscription = options.UsePatternSubscription;
            opt.ConnectTimeout = options.ConnectTimeout;
            opt.SyncTimeout = options.SyncTimeout;
        });

        services.TryAddSingleton<IConnectionMultiplexer>(sp =>
        {
            var configOptions = ConfigurationOptions.Parse(options.ConnectionString);
            configOptions.ConnectTimeout = options.ConnectTimeout;
            configOptions.SyncTimeout = options.SyncTimeout;
            return ConnectionMultiplexer.Connect(configOptions);
        });

        services.TryAddScoped<IRedisPubSubMessagePublisher, RedisPubSubMessagePublisher>();

        return services;
    }
}
