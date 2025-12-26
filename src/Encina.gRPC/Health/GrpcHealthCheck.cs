using Encina.Messaging.Health;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.gRPC.Health;

/// <summary>
/// Health check for gRPC service availability.
/// </summary>
/// <remarks>
/// <para>
/// This health check verifies that gRPC services are properly configured
/// and available in the dependency injection container.
/// </para>
/// <para>
/// The check validates that <see cref="IGrpcEncinaService"/> is registered,
/// which indicates the gRPC service layer is properly configured.
/// </para>
/// </remarks>
public sealed class GrpcHealthCheck : EncinaHealthCheck
{
    /// <summary>
    /// The default name for the gRPC health check.
    /// </summary>
    public const string DefaultName = "encina-grpc";

    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="GrpcHealthCheck"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider to resolve gRPC services from.</param>
    /// <param name="options">Configuration for the health check. If null, default options are used.</param>
    public GrpcHealthCheck(
        IServiceProvider serviceProvider,
        ProviderHealthCheckOptions? options)
        : base(options?.Name ?? DefaultName, options?.Tags ?? ["encina", "messaging", "grpc", "ready"])
    {
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc/>
    protected override Task<HealthCheckResult> CheckHealthCoreAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Verify gRPC service is registered
            var grpcService = _serviceProvider.GetService<IGrpcEncinaService>();

            if (grpcService is null)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    $"{Name} is not configured. Call AddEncinaGrpc() to register gRPC services."));
            }

            return Task.FromResult(HealthCheckResult.Healthy($"{Name} is configured and ready"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy($"{Name} check failed: {ex.Message}"));
        }
    }
}
