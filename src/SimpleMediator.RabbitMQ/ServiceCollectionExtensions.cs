using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RabbitMQ.Client;

namespace SimpleMediator.RabbitMQ;

/// <summary>
/// Extension methods for configuring SimpleMediator RabbitMQ integration.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds SimpleMediator RabbitMQ integration services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSimpleMediatorRabbitMQ(
        this IServiceCollection services,
        Action<SimpleMediatorRabbitMQOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new SimpleMediatorRabbitMQOptions();
        configure?.Invoke(options);

        services.Configure<SimpleMediatorRabbitMQOptions>(opt =>
        {
            opt.HostName = options.HostName;
            opt.Port = options.Port;
            opt.VirtualHost = options.VirtualHost;
            opt.UserName = options.UserName;
            opt.Password = options.Password;
            opt.ExchangeName = options.ExchangeName;
            opt.UsePublisherConfirms = options.UsePublisherConfirms;
            opt.PrefetchCount = options.PrefetchCount;
            opt.Durable = options.Durable;
        });

        services.TryAddSingleton<IConnection>(sp =>
        {
            var factory = new ConnectionFactory
            {
                HostName = options.HostName,
                Port = options.Port,
                VirtualHost = options.VirtualHost,
                UserName = options.UserName,
                Password = options.Password
            };

            return factory.CreateConnectionAsync().GetAwaiter().GetResult();
        });

        services.TryAddSingleton<IChannel>(sp =>
        {
            var connection = sp.GetRequiredService<IConnection>();
            return connection.CreateChannelAsync().GetAwaiter().GetResult();
        });

        services.TryAddScoped<IRabbitMQMessagePublisher, RabbitMQMessagePublisher>();

        return services;
    }
}
