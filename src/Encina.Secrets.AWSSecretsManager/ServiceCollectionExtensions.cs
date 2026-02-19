using Amazon;
using Amazon.SecretsManager;
using Encina.Secrets.AWSSecretsManager.Health;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Encina.Secrets.AWSSecretsManager;

/// <summary>
/// Extension methods for configuring AWS Secrets Manager secret provider services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the AWS Secrets Manager <see cref="ISecretProvider"/> implementation.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configure">An action to configure <see cref="AWSSecretsManagerOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> or <paramref name="configure"/> is null.</exception>
    public static IServiceCollection AddEncinaAWSSecretsManager(
        this IServiceCollection services,
        Action<AWSSecretsManagerOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new AWSSecretsManagerOptions();
        configure(options);

        services.Configure(configure);

        // Register IAmazonSecretsManager
        services.TryAddSingleton<IAmazonSecretsManager>(_ =>
        {
            var config = new AmazonSecretsManagerConfig();
            if (options.Region is not null)
            {
                config.RegionEndpoint = RegionEndpoint.GetBySystemName(options.Region);
            }

            return options.Credentials is not null
                ? new AmazonSecretsManagerClient(options.Credentials, config)
                : new AmazonSecretsManagerClient(config);
        });

        // Register provider
        services.TryAddSingleton<ISecretProvider, AWSSecretsManagerProvider>();

        // Register health check if enabled
        if (options.ProviderHealthCheck.Enabled)
        {
            services.AddHealthChecks()
                .AddCheck<AWSSecretsManagerHealthCheck>(
                    AWSSecretsManagerHealthCheck.DefaultName,
                    tags: options.ProviderHealthCheck.Tags);
        }

        return services;
    }
}
