using Azure.Identity;
using Azure.Security.KeyVault.Keys;
using Encina.Security.Encryption.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

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
    /// <exception cref="OptionsValidationException">
    /// Thrown when <see cref="AzureKeyVaultOptions.VaultUri"/> is missing, is not an absolute
    /// <c>https</c> URI (unless <see cref="AzureKeyVaultOptions.AllowInsecureHttp"/> is set), targets a
    /// loopback address (unless <see cref="AzureKeyVaultOptions.AllowLocalEndpoints"/> is set) or
    /// targets a link-local, cloud metadata or unspecified address. The same validation runs again at
    /// host startup (<c>ValidateOnStart</c>).
    /// </exception>
    public static IServiceCollection AddEncinaMessageEncryptionAzureKeyVault(
        this IServiceCollection services,
        Action<AzureKeyVaultOptions> configure,
        Action<MessageEncryptionOptions>? configureEncryption = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        // Validate eagerly so a plain BuildServiceProvider (no host, no ValidateOnStart) is protected too.
        var kvOptions = new AzureKeyVaultOptions();
        configure(kvOptions);
        var validation = new AzureKeyVaultOptionsValidator().Validate(Options.DefaultName, kvOptions);
        if (validation.Failed)
        {
            throw new OptionsValidationException(Options.DefaultName, typeof(AzureKeyVaultOptions), validation.Failures);
        }

        // Register Azure options, validated at startup and on first resolution
        services.AddOptions<AzureKeyVaultOptions>()
            .Configure(configure)
            .ValidateOnStart();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<AzureKeyVaultOptions>, AzureKeyVaultOptionsValidator>());

        // Register KeyClient as singleton (TryAdd allows pre-registration)
        services.TryAddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<AzureKeyVaultOptions>>().Value;
            var credential = options.Credential ?? new DefaultAzureCredential();
            return options.ClientOptions is not null
                ? new KeyClient(options.VaultUri!, credential, options.ClientOptions)
                : new KeyClient(options.VaultUri!, credential);
        });

        // Register IKeyProvider → AzureKeyVaultKeyProvider
        services.TryAddSingleton<IKeyProvider, AzureKeyVaultKeyProvider>();

        // Ensure base message encryption is registered
        services.AddEncinaMessageEncryption(configureEncryption);

        return services;
    }
}
