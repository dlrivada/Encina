using Encina.Messaging.Health;
using Encina.NATS.Health;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NATS.Client.Core;
using NATS.Client.JetStream;

namespace Encina.NATS;

/// <summary>
/// Extension methods for configuring Encina NATS integration.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Encina NATS integration services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEncinaNATS(
        this IServiceCollection services,
        Action<EncinaNATSOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new EncinaNATSOptions();
        configure?.Invoke(options);

        services.Configure<EncinaNATSOptions>(opt =>
        {
            opt.Url = options.Url;
            opt.SubjectPrefix = options.SubjectPrefix;
            opt.UseJetStream = options.UseJetStream;
            opt.StreamName = options.StreamName;
            opt.ConsumerName = options.ConsumerName;
            opt.UseDurableConsumer = options.UseDurableConsumer;
            opt.AckWait = options.AckWait;
            opt.MaxDeliver = options.MaxDeliver;
        });

        services.TryAddSingleton<INatsConnection>(sp =>
        {
            var natsOptions = new NatsOpts
            {
                Url = options.Url
            };
            return new NatsConnection(natsOptions);
        });

        if (options.UseJetStream)
        {
            services.TryAddSingleton<INatsJSContext>(sp =>
            {
                var connection = sp.GetRequiredService<INatsConnection>();
                return new NatsJSContext((NatsConnection)connection);
            });
        }
        // JetStream is optional - only register if configured

        services.TryAddScoped<INATSMessagePublisher, NATSMessagePublisher>();

        // Register health check if enabled
        if (options.ProviderHealthCheck.Enabled)
        {
            services.AddSingleton(options.ProviderHealthCheck);
            services.AddSingleton<IEncinaHealthCheck, NATSHealthCheck>();
        }

        return services;
    }
}
