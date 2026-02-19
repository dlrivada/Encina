using Encina.Secrets.HashiCorpVault.Health;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using VaultSharp;
using VaultSharp.V1.AuthMethods.Token;

namespace Encina.Secrets.HashiCorpVault;

/// <summary>
/// Extension methods for configuring HashiCorp Vault secret provider services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the HashiCorp Vault <see cref="ISecretProvider"/> implementation.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configure">An action to configure <see cref="HashiCorpVaultOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> or <paramref name="configure"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when <see cref="HashiCorpVaultOptions.AuthMethod"/> is not configured.</exception>
    public static IServiceCollection AddEncinaHashiCorpVault(
        this IServiceCollection services,
        Action<HashiCorpVaultOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new HashiCorpVaultOptions();
        configure(options);

        services.Configure(configure);

        // Register IVaultClient
        services.TryAddSingleton<IVaultClient>(_ =>
        {
            var authMethod = options.AuthMethod
                ?? throw new InvalidOperationException(
                    "HashiCorp Vault authentication method must be configured. " +
                    "Set AuthMethod to a valid IAuthMethodInfo (e.g., TokenAuthMethodInfo, AppRoleAuthMethodInfo, KubernetesAuthMethodInfo).");

            var settings = new VaultClientSettings(options.VaultAddress, authMethod);

            return new VaultClient(settings);
        });

        // Register provider
        services.TryAddSingleton<ISecretProvider, HashiCorpVaultProvider>();

        // Register health check if enabled
        if (options.ProviderHealthCheck.Enabled)
        {
            services.AddHealthChecks()
                .AddCheck<HashiCorpVaultHealthCheck>(
                    HashiCorpVaultHealthCheck.DefaultName,
                    tags: options.ProviderHealthCheck.Tags);
        }

        return services;
    }
}
