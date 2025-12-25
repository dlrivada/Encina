using Encina.Messaging.Health;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet;

namespace Encina.MQTT.Health;

/// <summary>
/// Health check for MQTT broker connectivity.
/// </summary>
public sealed class MQTTHealthCheck : EncinaHealthCheck
{
    /// <summary>
    /// The default name for the MQTT health check.
    /// </summary>
    public const string DefaultName = "encina-mqtt";

    private readonly IServiceProvider _serviceProvider;
    private readonly ProviderHealthCheckOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="MQTTHealthCheck"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider to resolve MqttClient from.</param>
    /// <param name="options">Configuration for the health check. If null, default options are used.</param>
    public MQTTHealthCheck(
        IServiceProvider serviceProvider,
        ProviderHealthCheckOptions? options)
        : base(options?.Name ?? DefaultName, options?.Tags ?? ["encina", "messaging", "mqtt", "ready"])
    {
        _serviceProvider = serviceProvider;
        _options = options ?? new ProviderHealthCheckOptions();
    }

    /// <inheritdoc/>
    protected override Task<HealthCheckResult> CheckHealthCoreAsync(CancellationToken cancellationToken)
    {
        var client = _serviceProvider.GetRequiredService<IMqttClient>();

        if (client.IsConnected)
        {
            return Task.FromResult(HealthCheckResult.Healthy($"{Name} is connected"));
        }

        return Task.FromResult(HealthCheckResult.Unhealthy($"{Name} is disconnected"));
    }
}
