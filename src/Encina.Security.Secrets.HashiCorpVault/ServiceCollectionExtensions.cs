using Encina.Security.Secrets.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using VaultSharp;

namespace Encina.Security.Secrets.HashiCorpVault;

/// <summary>
/// Extension methods for registering the HashiCorp Vault secrets provider with dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds HashiCorp Vault as the secrets provider for Encina.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configureVault">Action to configure <see cref="HashiCorpVaultOptions"/>.</param>
    /// <param name="configureSecrets">Optional action to configure <see cref="SecretsOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers the following services:
    /// <list type="bullet">
    /// <item><see cref="IVaultClient"/> — Singleton, configured with the specified auth method</item>
    /// <item><see cref="ISecretReader"/> → <see cref="HashiCorpVaultSecretProvider"/> with decorator chain</item>
    /// <item><see cref="ISecretWriter"/> → <see cref="HashiCorpVaultSecretProvider"/></item>
    /// <item><see cref="ISecretRotator"/> → <see cref="HashiCorpVaultSecretProvider"/></item>
    /// </list>
    /// </para>
    /// <para>
    /// All registrations use <c>TryAdd</c>, allowing you to register custom implementations
    /// before calling this method.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // With token auth (development)
    /// services.AddHashiCorpVaultSecrets(
    ///     vault =>
    ///     {
    ///         vault.VaultAddress = "http://localhost:8200";
    ///         vault.AuthMethod = new TokenAuthMethodInfo("hvs.dev-root-token");
    ///     },
    ///     secrets =>
    ///     {
    ///         secrets.EnableCaching = true;
    ///         secrets.DefaultCacheDuration = TimeSpan.FromMinutes(5);
    ///     });
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> or <paramref name="configureVault"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="HashiCorpVaultOptions.VaultAddress"/> is empty or
    /// <see cref="HashiCorpVaultOptions.AuthMethod"/> is <c>null</c>.
    /// </exception>
    public static IServiceCollection AddHashiCorpVaultSecrets(
        this IServiceCollection services,
        Action<HashiCorpVaultOptions> configureVault,
        Action<SecretsOptions>? configureSecrets = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureVault);

        var vaultOptions = new HashiCorpVaultOptions();
        configureVault(vaultOptions);

        if (string.IsNullOrWhiteSpace(vaultOptions.VaultAddress))
        {
            throw new InvalidOperationException(
                "HashiCorpVaultOptions.VaultAddress is required. " +
                "Provide the Vault server address (e.g., 'https://vault.example.com:8200').");
        }

        if (vaultOptions.AuthMethod is null)
        {
            throw new InvalidOperationException(
                "HashiCorpVaultOptions.AuthMethod is required. " +
                "Provide an IAuthMethodInfo implementation (e.g., TokenAuthMethodInfo, AppRoleAuthMethodInfo).");
        }

        // Register IVaultClient as singleton (TryAdd allows pre-registration)
        services.TryAddSingleton<IVaultClient>(_ => CreateClient(vaultOptions));

        // Register options for injection
        services.TryAddSingleton(vaultOptions);

        // Register as ISecretWriter and ISecretRotator (TryAdd allows pre-registration)
        services.TryAddSingleton<HashiCorpVaultSecretProvider>();
        services.TryAddSingleton<ISecretWriter>(sp =>
            sp.GetRequiredService<HashiCorpVaultSecretProvider>());
        services.TryAddSingleton<ISecretRotator>(sp =>
            sp.GetRequiredService<HashiCorpVaultSecretProvider>());

        // Register as ISecretReader with the core decorator chain (caching, auditing)
        return services.AddEncinaSecrets<HashiCorpVaultSecretProvider>(configureSecrets);
    }

    private static VaultClient CreateClient(HashiCorpVaultOptions options)
    {
        var settings = new VaultClientSettings(options.VaultAddress, options.AuthMethod);
        return new VaultClient(settings);
    }
}
