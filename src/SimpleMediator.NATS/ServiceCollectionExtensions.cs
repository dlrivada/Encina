using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NATS.Client.Core;
using NATS.Client.JetStream;

namespace SimpleMediator.NATS;

/// <summary>
/// Extension methods for configuring SimpleMediator NATS integration.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds SimpleMediator NATS integration services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSimpleMediatorNATS(
        this IServiceCollection services,
        Action<SimpleMediatorNATSOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new SimpleMediatorNATSOptions();
        configure?.Invoke(options);

        services.Configure<SimpleMediatorNATSOptions>(opt =>
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

        return services;
    }
}
