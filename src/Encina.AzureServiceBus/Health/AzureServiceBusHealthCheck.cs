using Azure.Messaging.ServiceBus;
using Encina.Messaging.Health;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.AzureServiceBus.Health;

/// <summary>
/// Health check for Azure Service Bus connectivity.
/// </summary>
public sealed class AzureServiceBusHealthCheck : EncinaHealthCheck
{
    /// <summary>
    /// The default name for the Azure Service Bus health check.
    /// </summary>
    public const string DefaultName = "encina-azure-servicebus";

    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureServiceBusHealthCheck"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider to resolve ServiceBusClient from.</param>
    /// <param name="options">Configuration for the health check. If null, default options are used.</param>
    public AzureServiceBusHealthCheck(
        IServiceProvider serviceProvider,
        ProviderHealthCheckOptions? options)
        : base(options?.Name ?? DefaultName, options?.Tags ?? ["encina", "messaging", "azure-servicebus", "ready"])
    {
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc/>
    protected override Task<HealthCheckResult> CheckHealthCoreAsync(CancellationToken cancellationToken)
    {
        try
        {
            var client = _serviceProvider.GetRequiredService<ServiceBusClient>();

            if (client.IsClosed)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy($"{Name} client is closed"));
            }

            return Task.FromResult(HealthCheckResult.Healthy($"{Name} is connected"));
        }
        catch (ServiceBusException ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy($"{Name} error: {ex.Reason} - {ex.Message}"));
        }
    }
}
