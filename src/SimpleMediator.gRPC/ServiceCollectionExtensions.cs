using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace SimpleMediator.gRPC;

/// <summary>
/// Extension methods for configuring SimpleMediator gRPC integration.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds SimpleMediator gRPC integration services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSimpleMediatorGrpc(
        this IServiceCollection services,
        Action<SimpleMediatorGrpcOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new SimpleMediatorGrpcOptions();
        configure?.Invoke(options);

        services.Configure<SimpleMediatorGrpcOptions>(opt =>
        {
            opt.EnableReflection = options.EnableReflection;
            opt.EnableHealthChecks = options.EnableHealthChecks;
            opt.MaxReceiveMessageSize = options.MaxReceiveMessageSize;
            opt.MaxSendMessageSize = options.MaxSendMessageSize;
            opt.EnableLoggingInterceptor = options.EnableLoggingInterceptor;
            opt.DefaultDeadline = options.DefaultDeadline;
            opt.EnableCompression = options.EnableCompression;
        });

        services.TryAddScoped<IGrpcMediatorService, GrpcMediatorService>();

        return services;
    }
}
