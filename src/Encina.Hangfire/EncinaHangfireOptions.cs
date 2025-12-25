using Encina.Messaging.Health;

namespace Encina.Hangfire;

/// <summary>
/// Configuration options for Encina Hangfire integration.
/// </summary>
public sealed class EncinaHangfireOptions
{
    /// <summary>
    /// Gets the provider health check options.
    /// </summary>
    public ProviderHealthCheckOptions ProviderHealthCheck { get; } = new();
}
