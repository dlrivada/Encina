using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Encina.GraphQL;

/// <summary>
/// Extension methods for configuring Encina GraphQL integration.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Encina GraphQL integration services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEncinaGraphQL(
        this IServiceCollection services,
        Action<EncinaGraphQLOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new EncinaGraphQLOptions();
        configure?.Invoke(options);

        services.Configure<EncinaGraphQLOptions>(opt =>
        {
            opt.Path = options.Path;
            opt.EnableGraphQLIDE = options.EnableGraphQLIDE;
            opt.EnableIntrospection = options.EnableIntrospection;
            opt.IncludeExceptionDetails = options.IncludeExceptionDetails;
            opt.MaxExecutionDepth = options.MaxExecutionDepth;
            opt.ExecutionTimeout = options.ExecutionTimeout;
            opt.EnableSubscriptions = options.EnableSubscriptions;
            opt.EnablePersistedQueries = options.EnablePersistedQueries;
        });

        services.TryAddScoped<IGraphQLMediatorBridge, GraphQLMediatorBridge>();

        return services;
    }
}
