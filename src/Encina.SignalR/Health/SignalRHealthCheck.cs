using Encina.Messaging.Health;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.SignalR.Health;

/// <summary>
/// Health check for SignalR service availability.
/// </summary>
/// <remarks>
/// <para>
/// This health check verifies that SignalR services are properly configured
/// and available in the dependency injection container.
/// </para>
/// <para>
/// Unlike message broker health checks, SignalR doesn't have a persistent connection
/// to verify. Instead, this check ensures that <see cref="IHubContext{THub}"/> is
/// registered and can be resolved, which indicates SignalR is properly configured.
/// </para>
/// </remarks>
public sealed class SignalRHealthCheck : EncinaHealthCheck
{
    /// <summary>
    /// The default name for the SignalR health check.
    /// </summary>
    public const string DefaultName = "encina-signalr";

    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="SignalRHealthCheck"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider to resolve SignalR services from.</param>
    /// <param name="options">Configuration for the health check. If null, default options are used.</param>
    public SignalRHealthCheck(
        IServiceProvider serviceProvider,
        ProviderHealthCheckOptions? options)
        : base(options?.Name ?? DefaultName, options?.Tags ?? ["encina", "messaging", "signalr", "ready"])
    {
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc/>
    protected override Task<HealthCheckResult> CheckHealthCoreAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Verify SignalR broadcaster is registered (Encina.SignalR specific)
            var broadcaster = _serviceProvider.GetService<ISignalRNotificationBroadcaster>();

            if (broadcaster is null)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    $"{Name} is not configured. Call AddEncinaSignalR() to register SignalR services."));
            }

            // Verify generic hub context is available
            var hubContextType = typeof(IHubContext<>).MakeGenericType(typeof(Hub));
            var hubContext = _serviceProvider.GetService(hubContextType);

            if (hubContext is null)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    $"{Name} hub context is not available. Ensure AddSignalR() is called."));
            }

            return Task.FromResult(HealthCheckResult.Healthy($"{Name} is configured and ready"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy($"{Name} check failed: {ex.Message}"));
        }
    }
}
