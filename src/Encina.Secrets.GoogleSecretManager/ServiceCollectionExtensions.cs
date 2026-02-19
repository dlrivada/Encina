using Encina.Secrets.GoogleSecretManager.Health;
using Google.Cloud.SecretManager.V1;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Encina.Secrets.GoogleSecretManager;

/// <summary>
/// Extension methods for configuring Google Cloud Secret Manager secret provider services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Google Cloud Secret Manager <see cref="ISecretProvider"/> implementation.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configure">An action to configure <see cref="GoogleSecretManagerOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> or <paramref name="configure"/> is null.</exception>
    public static IServiceCollection AddEncinaGoogleSecretManager(
        this IServiceCollection services,
        Action<GoogleSecretManagerOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new GoogleSecretManagerOptions();
        configure(options);

        services.Configure(configure);

        // Register SecretManagerServiceClient
        // Uses Application Default Credentials (ADC) by default
        services.TryAddSingleton(_ => SecretManagerServiceClient.Create());

        // Register provider
        services.TryAddSingleton<ISecretProvider, GoogleSecretManagerProvider>();

        // Register health check if enabled
        if (options.ProviderHealthCheck.Enabled)
        {
            services.AddHealthChecks()
                .AddCheck<GoogleSecretManagerHealthCheck>(
                    GoogleSecretManagerHealthCheck.DefaultName,
                    tags: options.ProviderHealthCheck.Tags);
        }

        return services;
    }
}
