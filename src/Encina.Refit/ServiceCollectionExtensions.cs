using Microsoft.Extensions.DependencyInjection;
using Refit;

namespace Encina.Refit;

/// <summary>
/// Extension methods for configuring Encina with Refit.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds a Refit client and registers the corresponding handler for Encina requests.
    /// </summary>
    /// <typeparam name="TApiClient">The Refit API client interface type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration for the HTTP client.</param>
    /// <returns>An <see cref="IHttpClientBuilder"/> for further configuration.</returns>
    /// <remarks>
    /// This method combines Refit's AddRefitClient with Encina handler registration.
    /// You can chain additional configuration like resilience policies, headers, etc.
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddEncinaRefitClient&lt;IGitHubApi&gt;(client =>
    ///     {
    ///         client.BaseAddress = new Uri("https://api.github.com");
    ///         client.Timeout = TimeSpan.FromSeconds(30);
    ///     })
    ///     .AddStandardResilienceHandler(); // Optional: Add resilience
    /// </code>
    /// </example>
    public static IHttpClientBuilder AddEncinaRefitClient<TApiClient>(
        this IServiceCollection services,
        Action<HttpClient>? configure = null)
        where TApiClient : class
    {
        // Register the Refit client
        var httpClientBuilder = services.AddRefitClient<TApiClient>();

        if (configure != null)
        {
            httpClientBuilder.ConfigureHttpClient(configure);
        }

        // Note: The handler is registered automatically by Encina's assembly scanning
        // when you call AddEncina() because it implements IRequestHandler<,>

        return httpClientBuilder;
    }

    /// <summary>
    /// Adds a Refit client with RefitSettings and registers the corresponding handler.
    /// </summary>
    /// <typeparam name="TApiClient">The Refit API client interface type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="refitSettings">The Refit settings to use.</param>
    /// <param name="configure">Optional configuration for the HTTP client.</param>
    /// <returns>An <see cref="IHttpClientBuilder"/> for further configuration.</returns>
    /// <example>
    /// <code>
    /// var refitSettings = new RefitSettings
    /// {
    ///     ContentSerializer = new SystemTextJsonContentSerializer(
    ///         new JsonSerializerOptions
    ///         {
    ///             PropertyNameCaseInsensitive = true
    ///         })
    /// };
    ///
    /// services.AddEncinaRefitClient&lt;IGitHubApi&gt;(
    ///     refitSettings,
    ///     client => client.BaseAddress = new Uri("https://api.github.com"));
    /// </code>
    /// </example>
    public static IHttpClientBuilder AddEncinaRefitClient<TApiClient>(
        this IServiceCollection services,
        RefitSettings refitSettings,
        Action<HttpClient>? configure = null)
        where TApiClient : class
    {
        // Register the Refit client with custom settings
        var httpClientBuilder = services.AddRefitClient<TApiClient>(refitSettings);

        if (configure != null)
        {
            httpClientBuilder.ConfigureHttpClient(configure);
        }

        return httpClientBuilder;
    }

    /// <summary>
    /// Adds a Refit client with a settings provider function.
    /// </summary>
    /// <typeparam name="TApiClient">The Refit API client interface type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="settingsProvider">Function to create Refit settings from service provider.</param>
    /// <param name="configure">Optional configuration for the HTTP client.</param>
    /// <returns>An <see cref="IHttpClientBuilder"/> for further configuration.</returns>
    /// <example>
    /// <code>
    /// services.AddEncinaRefitClient&lt;IGitHubApi&gt;(
    ///     sp =>
    ///     {
    ///         var logger = sp.GetRequiredService&lt;ILogger&lt;IGitHubApi&gt;&gt;();
    ///         return new RefitSettings
    ///         {
    ///             ContentSerializer = new SystemTextJsonContentSerializer()
    ///         };
    ///     },
    ///     client => client.BaseAddress = new Uri("https://api.github.com"));
    /// </code>
    /// </example>
    public static IHttpClientBuilder AddEncinaRefitClient<TApiClient>(
        this IServiceCollection services,
        Func<IServiceProvider, RefitSettings> settingsProvider,
        Action<HttpClient>? configure = null)
        where TApiClient : class
    {
        // Register the Refit client with settings from service provider
        var httpClientBuilder = services.AddRefitClient<TApiClient>(settingsProvider);

        if (configure != null)
        {
            httpClientBuilder.ConfigureHttpClient(configure);
        }

        return httpClientBuilder;
    }
}
