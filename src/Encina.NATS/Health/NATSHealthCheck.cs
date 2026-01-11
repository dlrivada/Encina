using Encina.Messaging.Health;
using Microsoft.Extensions.DependencyInjection;
using NATS.Client.Core;

namespace Encina.NATS.Health;

/// <summary>
/// Health check for NATS message broker connectivity.
/// </summary>
public sealed class NATSHealthCheck : EncinaHealthCheck
{
    /// <summary>
    /// The default name for the NATS health check.
    /// </summary>
    public const string DefaultName = "encina-nats";

    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="NATSHealthCheck"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider to resolve INatsConnection from.</param>
    /// <param name="options">Configuration for the health check. If null, default options are used.</param>
    public NATSHealthCheck(
        IServiceProvider serviceProvider,
        ProviderHealthCheckOptions? options)
        : base(options?.Name ?? DefaultName, options?.Tags ?? ["encina", "messaging", "nats", "ready"])
    {
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc/>
    protected override Task<HealthCheckResult> CheckHealthCoreAsync(CancellationToken cancellationToken)
    {
        try
        {
            var connection = _serviceProvider.GetRequiredService<INatsConnection>();

            if (connection.ConnectionState == NatsConnectionState.Open)
            {
                return Task.FromResult(HealthCheckResult.Healthy($"{Name} is connected"));
            }

            return Task.FromResult(HealthCheckResult.Unhealthy($"{Name} connection state: {connection.ConnectionState}"));
        }
        catch (NatsException ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy($"{Name} error: {ex.Message}"));
        }
    }
}
