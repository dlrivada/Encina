using Azure.Identity;
using Azure.Security.KeyVault.Keys;
using Encina.Security.Encryption.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Encina.Messaging.Encryption.AzureKeyVault;

/// <summary>
/// Extension methods for registering Azure Key Vault message encryption services.
/// </summary>
/// <remarks>
/// <para>
/// Registers <see cref="AzureKeyVaultKeyProvider"/> as <see cref="IKeyProvider"/>,
/// which integrates with <see cref="DefaultMessageEncryptionProvider"/> via
/// <c>AddEncinaMessageEncryption()</c>.
/// </para>
/// </remarks>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers Azure Key Vault as the key provider for message encryption.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action for <see cref="AzureKeyVaultOptions"/>.</param>
    /// <param name="configureEncryption">Optional configuration action for <see cref="MessageEncryptionOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEncinaMessageEncryptionAzureKeyVault(
        this IServiceCollection services,
        Action<AzureKeyVaultOptions> configure,
        Action<MessageEncryptionOptions>? configureEncryption = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var kvOptions = new AzureKeyVaultOptions();
        configure(kvOptions);

        // Register Azure options
        services.Configure(configure);

        // Register KeyClient as singleton (TryAdd allows pre-registration)
        services.TryAddSingleton(_ =>
        {
            if (kvOptions.VaultUri is null)
            {
                throw new InvalidOperationException(
                    "AzureKeyVaultOptions.VaultUri must be configured.");
            }

            var credential = kvOptions.Credential ?? new DefaultAzureCredential();
            return kvOptions.ClientOptions is not null
                ? new KeyClient(kvOptions.VaultUri, credential, kvOptions.ClientOptions)
                : new KeyClient(kvOptions.VaultUri, credential);
        });

        // Register IKeyProvider → AzureKeyVaultKeyProvider
        services.TryAddSingleton<IKeyProvider, AzureKeyVaultKeyProvider>();

        // Ensure base message encryption is registered
        services.AddEncinaMessageEncryption(configureEncryption);

        return services;
    }
}
