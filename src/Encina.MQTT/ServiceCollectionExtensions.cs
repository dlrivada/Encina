using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MQTTnet;

namespace Encina.MQTT;

/// <summary>
/// Extension methods for configuring Encina MQTT integration.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Encina MQTT integration services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEncinaMQTT(
        this IServiceCollection services,
        Action<EncinaMQTTOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new EncinaMQTTOptions();
        configure?.Invoke(options);

        services.Configure<EncinaMQTTOptions>(opt =>
        {
            opt.Host = options.Host;
            opt.Port = options.Port;
            opt.ClientId = options.ClientId;
            opt.TopicPrefix = options.TopicPrefix;
            opt.Username = options.Username;
            opt.Password = options.Password;
            opt.QualityOfService = options.QualityOfService;
            opt.UseTls = options.UseTls;
            opt.CleanSession = options.CleanSession;
            opt.KeepAliveSeconds = options.KeepAliveSeconds;
        });

        services.TryAddSingleton(sp =>
        {
            var factory = new MqttClientFactory();
            var client = factory.CreateMqttClient();

            var connectOptionsBuilder = new MqttClientOptionsBuilder()
                .WithTcpServer(options.Host, options.Port)
                .WithClientId(options.ClientId)
                .WithCleanSession(options.CleanSession)
                .WithKeepAlivePeriod(TimeSpan.FromSeconds(options.KeepAliveSeconds));

            if (!string.IsNullOrEmpty(options.Username))
            {
                connectOptionsBuilder.WithCredentials(options.Username, options.Password);
            }

            if (options.UseTls)
            {
                connectOptionsBuilder.WithTlsOptions(o => o.UseTls());
            }

            var connectOptions = connectOptionsBuilder.Build();

            // Connect synchronously during registration
            client.ConnectAsync(connectOptions).GetAwaiter().GetResult();

            return client;
        });

        services.TryAddScoped<IMQTTMessagePublisher, MQTTMessagePublisher>();

        return services;
    }
}
