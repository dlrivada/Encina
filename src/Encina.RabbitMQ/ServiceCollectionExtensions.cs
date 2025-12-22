using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RabbitMQ.Client;

namespace Encina.RabbitMQ;

/// <summary>
/// Extension methods for configuring Encina RabbitMQ integration.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Encina RabbitMQ integration services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEncinaRabbitMQ(
        this IServiceCollection services,
        Action<EncinaRabbitMQOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new EncinaRabbitMQOptions();
        configure?.Invoke(options);

        services.Configure<EncinaRabbitMQOptions>(opt =>
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

        services.TryAddSingleton(sp =>
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

        services.TryAddSingleton(sp =>
        {
            var connection = sp.GetRequiredService<IConnection>();
            return connection.CreateChannelAsync().GetAwaiter().GetResult();
        });

        services.TryAddScoped<IRabbitMQMessagePublisher, RabbitMQMessagePublisher>();

        return services;
    }
}
