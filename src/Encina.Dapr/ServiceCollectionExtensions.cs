using Dapr.Client;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.Dapr;

/// <summary>
/// Extension methods for configuring Encina with Dapr.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Dapr integration to Encina.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// This method:
    /// - Registers DaprClient as a singleton (if not already registered)
    /// - Registers handlers for IDaprServiceInvocationRequest and IDaprPubSubRequest
    /// - Enables service-to-service calls and event publishing through Encina
    ///
    /// Note: The handlers are automatically discovered by Encina's assembly scanning
    /// when you call AddEncina().
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddEncina()
    ///     .AddEncinaDapr();
    ///
    /// // Optional: Configure DaprClient
    /// services.AddSingleton&lt;DaprClient&gt;(sp =>
    /// {
    ///     var daprClient = new DaprClientBuilder()
    ///         .UseHttpEndpoint("http://localhost:3500")
    ///         .UseGrpcEndpoint("http://localhost:50001")
    ///         .Build();
    ///     return daprClient;
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddEncinaDapr(this IServiceCollection services)
    {
        // Register DaprClient if not already registered
        // DaprClient is typically auto-registered by Dapr.AspNetCore or can be manually configured
        services.AddDaprClient();

        // Note: The handlers (DaprServiceInvocationHandler and DaprPubSubHandler)
        // are automatically registered by Encina's assembly scanning
        // because they implement IRequestHandler<,>

        return services;
    }

    /// <summary>
    /// Adds Dapr integration with custom DaprClient configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureDaprClient">Configuration callback for DaprClientBuilder.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddEncina()
    ///     .AddEncinaDapr(builder =>
    ///     {
    ///         builder.UseHttpEndpoint("http://localhost:3500")
    ///                .UseGrpcEndpoint("http://localhost:50001")
    ///                .UseTimeout(TimeSpan.FromSeconds(30))
    ///                .UseJsonSerializationOptions(new JsonSerializerOptions
    ///                {
    ///                    PropertyNameCaseInsensitive = true
    ///                });
    ///     });
    /// </code>
    /// </example>
    public static IServiceCollection AddEncinaDapr(
        this IServiceCollection services,
        Action<DaprClientBuilder> configureDaprClient)
    {
        // Register DaprClient with custom configuration
        services.AddSingleton(sp =>
        {
            var builder = new DaprClientBuilder();
            configureDaprClient(builder);
            return builder.Build();
        });

        return services;
    }
}
