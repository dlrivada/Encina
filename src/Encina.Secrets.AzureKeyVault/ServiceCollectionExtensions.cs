using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Encina.Secrets.AzureKeyVault.Health;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Encina.Secrets.AzureKeyVault;

/// <summary>
/// Extension methods for configuring Azure Key Vault secret provider services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Azure Key Vault <see cref="ISecretProvider"/> implementation.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configure">An action to configure <see cref="KeyVaultSecretProviderOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> or <paramref name="configure"/> is null.</exception>
    public static IServiceCollection AddEncinaKeyVaultSecrets(
        this IServiceCollection services,
        Action<KeyVaultSecretProviderOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new KeyVaultSecretProviderOptions();
        configure(options);

        services.Configure(configure);

        // Register SecretClient
        services.TryAddSingleton(_ =>
        {
            var credential = options.Credential ?? new DefaultAzureCredential();
            return new SecretClient(new Uri(options.VaultUri), credential);
        });

        // Register provider
        services.TryAddSingleton<ISecretProvider, KeyVaultSecretProvider>();

        // Register health check if enabled
        if (options.ProviderHealthCheck.Enabled)
        {
            services.AddHealthChecks()
                .AddCheck<KeyVaultHealthCheck>(
                    KeyVaultHealthCheck.DefaultName,
                    tags: options.ProviderHealthCheck.Tags);
        }

        return services;
    }
}
