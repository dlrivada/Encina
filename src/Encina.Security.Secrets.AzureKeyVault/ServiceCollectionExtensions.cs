using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Encina.Security.Secrets.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Encina.Security.Secrets.AzureKeyVault;

/// <summary>
/// Extension methods for registering the Azure Key Vault secrets provider with dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Azure Key Vault as the secrets provider for Encina.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="vaultUri">
    /// The URI of the Azure Key Vault instance (e.g., <c>https://my-vault.vault.azure.net/</c>).
    /// </param>
    /// <param name="configureKeyVault">Optional action to configure <see cref="AzureKeyVaultOptions"/>.</param>
    /// <param name="configureSecrets">Optional action to configure <see cref="SecretsOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers the following services:
    /// <list type="bullet">
    /// <item><see cref="SecretClient"/> — Singleton, using <see cref="DefaultAzureCredential"/> unless overridden</item>
    /// <item><see cref="ISecretReader"/> → <see cref="AzureKeyVaultSecretProvider"/> with decorator chain</item>
    /// <item><see cref="ISecretWriter"/> → <see cref="AzureKeyVaultSecretProvider"/></item>
    /// <item><see cref="ISecretRotator"/> → <see cref="AzureKeyVaultSecretProvider"/></item>
    /// </list>
    /// </para>
    /// <para>
    /// All registrations use <c>TryAdd</c>, allowing you to register custom implementations
    /// before calling this method.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Basic setup with DefaultAzureCredential
    /// services.AddAzureKeyVaultSecrets(
    ///     new Uri("https://my-vault.vault.azure.net/"));
    ///
    /// // With caching and custom credential
    /// services.AddAzureKeyVaultSecrets(
    ///     new Uri("https://my-vault.vault.azure.net/"),
    ///     kvOptions => kvOptions.Credential = new ManagedIdentityCredential(),
    ///     secretsOptions =>
    ///     {
    ///         secretsOptions.EnableCaching = true;
    ///         secretsOptions.DefaultCacheDuration = TimeSpan.FromMinutes(10);
    ///     });
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> or <paramref name="vaultUri"/> is <c>null</c>.
    /// </exception>
    public static IServiceCollection AddAzureKeyVaultSecrets(
        this IServiceCollection services,
        Uri vaultUri,
        Action<AzureKeyVaultOptions>? configureKeyVault = null,
        Action<SecretsOptions>? configureSecrets = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(vaultUri);

        var kvOptions = new AzureKeyVaultOptions { VaultUri = vaultUri };
        configureKeyVault?.Invoke(kvOptions);

        // Register SecretClient as singleton (TryAdd allows pre-registration)
        services.TryAddSingleton(_ =>
        {
            var credential = kvOptions.Credential ?? new DefaultAzureCredential();
            return kvOptions.ClientOptions is not null
                ? new SecretClient(kvOptions.VaultUri!, credential, kvOptions.ClientOptions)
                : new SecretClient(kvOptions.VaultUri!, credential);
        });

        // Register options for injection
        services.Configure<AzureKeyVaultOptions>(o =>
        {
            o.VaultUri = kvOptions.VaultUri;
            o.Credential = kvOptions.Credential;
            o.ClientOptions = kvOptions.ClientOptions;
        });

        // Register as ISecretWriter and ISecretRotator (TryAdd allows pre-registration)
        services.TryAddSingleton<AzureKeyVaultSecretProvider>();
        services.TryAddSingleton<ISecretWriter>(sp =>
            sp.GetRequiredService<AzureKeyVaultSecretProvider>());
        services.TryAddSingleton<ISecretRotator>(sp =>
            sp.GetRequiredService<AzureKeyVaultSecretProvider>());

        // Register as ISecretReader with the core decorator chain (caching, auditing)
        return services.AddEncinaSecrets<AzureKeyVaultSecretProvider>(configureSecrets);
    }
}
