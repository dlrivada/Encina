using Encina.AwsLambda.Health;
using Encina.Messaging.Health;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Encina.AwsLambda;

/// <summary>
/// Extension methods for configuring Encina AWS Lambda services in the dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Encina AWS Lambda integration services to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// var services = new ServiceCollection();
    /// services.AddEncina(typeof(Program).Assembly);
    /// services.AddEncinaAwsLambda();
    /// </code>
    /// </example>
    public static IServiceCollection AddEncinaAwsLambda(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddEncinaAwsLambda(_ => { });
    }

    /// <summary>
    /// Adds Encina AWS Lambda integration services to the service collection with configuration.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configureOptions">The action to configure options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// var services = new ServiceCollection();
    /// services.AddEncina(typeof(Program).Assembly);
    /// services.AddEncinaAwsLambda(options =>
    /// {
    ///     options.EnableRequestContextEnrichment = true;
    ///     options.CorrelationIdHeader = "X-Request-ID";
    ///     options.EnableSqsBatchItemFailures = true;
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddEncinaAwsLambda(
        this IServiceCollection services,
        Action<EncinaAwsLambdaOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        services.Configure(configureOptions);

        // Register health check
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IEncinaHealthCheck, AwsLambdaHealthCheck>());

        return services;
    }
}
