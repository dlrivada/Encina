using Encina.Messaging.Health;

namespace Encina.Quartz;

/// <summary>
/// Configuration options for Encina Quartz integration.
/// </summary>
public sealed class EncinaQuartzOptions
{
    /// <summary>
    /// Gets the provider health check options.
    /// </summary>
    public ProviderHealthCheckOptions ProviderHealthCheck { get; } = new();
}
