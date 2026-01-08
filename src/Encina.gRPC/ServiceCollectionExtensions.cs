using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Encina.gRPC;

/// <summary>
/// Extension methods for configuring Encina gRPC integration.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Encina gRPC integration services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEncinaGrpc(
        this IServiceCollection services,
        Action<EncinaGrpcOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new EncinaGrpcOptions();
        configure?.Invoke(options);

        services.Configure<EncinaGrpcOptions>(opt =>
        {
            opt.EnableReflection = options.EnableReflection;
            opt.EnableHealthChecks = options.EnableHealthChecks;
            opt.MaxReceiveMessageSize = options.MaxReceiveMessageSize;
            opt.MaxSendMessageSize = options.MaxSendMessageSize;
            opt.EnableLoggingInterceptor = options.EnableLoggingInterceptor;
            opt.DefaultDeadline = options.DefaultDeadline;
            opt.EnableCompression = options.EnableCompression;
        });

        services.TryAddSingleton<ITypeResolver, CachingTypeResolver>();
        services.TryAddScoped<IGrpcEncinaService, GrpcEncinaService>();

        return services;
    }
}
