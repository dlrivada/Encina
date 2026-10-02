using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Encina.Security.Secrets.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

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
    /// <exception cref="OptionsValidationException">
    /// Thrown when the vault URI is not an absolute <c>https</c> URI (unless
    /// <see cref="AzureKeyVaultOptions.AllowInsecureHttp"/> is set), targets a loopback address (unless
    /// <see cref="AzureKeyVaultOptions.AllowLocalEndpoints"/> is set) or targets a link-local, cloud
    /// metadata or unspecified address. The same validation runs again at host startup
    /// (<c>ValidateOnStart</c>).
    /// </exception>
    public static IServiceCollection AddAzureKeyVaultSecrets(
        this IServiceCollection services,
        Uri vaultUri,
        Action<AzureKeyVaultOptions>? configureKeyVault = null,
        Action<SecretsOptions>? configureSecrets = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(vaultUri);

        void ApplyConfiguration(AzureKeyVaultOptions options)
        {
            options.VaultUri = vaultUri;
            configureKeyVault?.Invoke(options);
        }

        // Validate eagerly so a plain BuildServiceProvider (no host, no ValidateOnStart) is protected too.
        var kvOptions = new AzureKeyVaultOptions();
        ApplyConfiguration(kvOptions);
        var validation = new AzureKeyVaultOptionsValidator().Validate(Options.DefaultName, kvOptions);
        if (validation.Failed)
        {
            throw new OptionsValidationException(Options.DefaultName, typeof(AzureKeyVaultOptions), validation.Failures);
        }

        // Register options for injection, validated at startup and on first resolution
        services.AddOptions<AzureKeyVaultOptions>()
            .Configure(ApplyConfiguration)
            .ValidateOnStart();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<AzureKeyVaultOptions>, AzureKeyVaultOptionsValidator>());

        // Register SecretClient as singleton (TryAdd allows pre-registration)
        services.TryAddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<AzureKeyVaultOptions>>().Value;
            var credential = options.Credential ?? new DefaultAzureCredential();
            return options.ClientOptions is not null
                ? new SecretClient(options.VaultUri!, credential, options.ClientOptions)
                : new SecretClient(options.VaultUri!, credential);
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
