using Encina.AzureFunctions.Health;
using Encina.Messaging.Health;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Encina.AzureFunctions;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/> to register Encina Azure Functions integration.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Encina Azure Functions integration services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers:
    /// <list type="bullet">
    /// <item><description>Default configuration options</description></item>
    /// <item><description>Middleware for request context enrichment</description></item>
    /// <item><description>Health check (if enabled)</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// After calling this method, use <c>builder.UseEncinaMiddleware()</c> to enable the middleware.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var host = new HostBuilder()
    ///     .ConfigureFunctionsWorkerDefaults(builder =>
    ///     {
    ///         builder.UseEncinaMiddleware();
    ///     })
    ///     .ConfigureServices(services =>
    ///     {
    ///         services.AddEncina(typeof(Program).Assembly);
    ///         services.AddEncinaAzureFunctions();
    ///     })
    ///     .Build();
    /// </code>
    /// </example>
    public static IServiceCollection AddEncinaAzureFunctions(this IServiceCollection services)
    {
        return services.AddEncinaAzureFunctions(_ => { });
    }

    /// <summary>
    /// Adds Encina Azure Functions integration services with custom configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Action to configure options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddEncinaAzureFunctions(options =>
    /// {
    ///     options.CorrelationIdHeader = "X-Request-ID";
    ///     options.EnableRequestContextEnrichment = true;
    ///     options.IncludeExceptionDetailsInResponse = false;
    ///     options.ProviderHealthCheck.Enabled = true;
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddEncinaAzureFunctions(
        this IServiceCollection services,
        Action<EncinaAzureFunctionsOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        // Build options to check health check configuration
        var options = new EncinaAzureFunctionsOptions();
        configureOptions(options);

        // Register options
        services.Configure(configureOptions);

        // Register middleware
        services.TryAddSingleton<EncinaFunctionMiddleware>();

        // Register health check if enabled
        if (options.ProviderHealthCheck.Enabled)
        {
            services.TryAddSingleton<IEncinaHealthCheck, AzureFunctionsHealthCheck>();
        }

        return services;
    }
}
