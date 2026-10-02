using Encina.Messaging.Health;
using Encina.NATS.Health;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
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
    /// <exception cref="OptionsValidationException">
    /// Thrown when a server URL in <see cref="EncinaNATSOptions.Url"/> is not absolute, does not use the
    /// <c>nats</c>, <c>tls</c>, <c>ws</c> or <c>wss</c> scheme, targets a loopback address (including the
    /// default <c>nats://localhost:4222</c>) without <see cref="EncinaNATSOptions.AllowLocalEndpoints"/>,
    /// or targets a link-local, cloud metadata or unspecified address. The same validation runs again
    /// at host startup (<c>ValidateOnStart</c>).
    /// </exception>
    public static IServiceCollection AddEncinaNATS(
        this IServiceCollection services,
        Action<EncinaNATSOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Validate eagerly so a plain BuildServiceProvider (no host, no ValidateOnStart) is protected too.
        var options = new EncinaNATSOptions();
        configure?.Invoke(options);
        var validation = new EncinaNATSOptionsValidator().Validate(Options.DefaultName, options);
        if (validation.Failed)
        {
            throw new OptionsValidationException(Options.DefaultName, typeof(EncinaNATSOptions), validation.Failures);
        }

        services.AddOptions<EncinaNATSOptions>()
            .Configure(opt => configure?.Invoke(opt))
            .ValidateOnStart();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<EncinaNATSOptions>, EncinaNATSOptionsValidator>());

        services.TryAddSingleton<INatsConnection>(sp =>
        {
            var natsOptions = new NatsOpts
            {
                Url = sp.GetRequiredService<IOptions<EncinaNATSOptions>>().Value.Url
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
