using Encina.Messaging.Health;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;

namespace Encina.RabbitMQ.Health;

/// <summary>
/// Health check for RabbitMQ message broker connectivity.
/// </summary>
public sealed class RabbitMQHealthCheck : EncinaHealthCheck
{
    /// <summary>
    /// The default name for the RabbitMQ health check.
    /// </summary>
    public const string DefaultName = "encina-rabbitmq";

    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="RabbitMQHealthCheck"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider to resolve IConnection from.</param>
    /// <param name="options">Configuration for the health check. If null, default options are used.</param>
    public RabbitMQHealthCheck(
        IServiceProvider serviceProvider,
        ProviderHealthCheckOptions? options)
        : base(options?.Name ?? DefaultName, options?.Tags ?? ["encina", "messaging", "rabbitmq", "ready"])
    {
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc/>
    protected override Task<HealthCheckResult> CheckHealthCoreAsync(CancellationToken cancellationToken)
    {
        try
        {
            var connection = _serviceProvider.GetRequiredService<IConnection>();

            if (!connection.IsOpen)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy($"{Name} connection is closed"));
            }

            return Task.FromResult(HealthCheckResult.Healthy($"{Name} is connected"));
        }
        catch (global::RabbitMQ.Client.Exceptions.BrokerUnreachableException ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy($"{Name} broker unreachable: {ex.Message}"));
        }
    }
}
