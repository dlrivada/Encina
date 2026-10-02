using Encina.Security.Secrets.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
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
    /// // With token auth against a local dev server (both opt-outs log a warning at startup)
    /// services.AddHashiCorpVaultSecrets(
    ///     vault =>
    ///     {
    ///         vault.VaultAddress = "http://localhost:8200";
    ///         vault.AllowInsecureHttp = true;
    ///         vault.AllowLocalEndpoints = true;
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
    /// <exception cref="OptionsValidationException">
    /// Thrown when <see cref="HashiCorpVaultOptions.VaultAddress"/> is missing, is not an absolute
    /// <c>https</c> URL (unless <see cref="HashiCorpVaultOptions.AllowInsecureHttp"/> is set), targets a
    /// loopback address (unless <see cref="HashiCorpVaultOptions.AllowLocalEndpoints"/> is set) or a
    /// link-local, cloud metadata or unspecified address, or when
    /// <see cref="HashiCorpVaultOptions.AuthMethod"/> is <c>null</c>. The same validation runs again at
    /// host startup (<c>ValidateOnStart</c>).
    /// </exception>
    public static IServiceCollection AddHashiCorpVaultSecrets(
        this IServiceCollection services,
        Action<HashiCorpVaultOptions> configureVault,
        Action<SecretsOptions>? configureSecrets = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureVault);

        // Validate eagerly so a plain BuildServiceProvider (no host, no ValidateOnStart) is protected too.
        var vaultOptions = new HashiCorpVaultOptions();
        configureVault(vaultOptions);
        var validation = new HashiCorpVaultOptionsValidator().Validate(Options.DefaultName, vaultOptions);
        if (validation.Failed)
        {
            throw new OptionsValidationException(Options.DefaultName, typeof(HashiCorpVaultOptions), validation.Failures);
        }

        services.AddOptions<HashiCorpVaultOptions>()
            .Configure(configureVault)
            .ValidateOnStart();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<HashiCorpVaultOptions>, HashiCorpVaultOptionsValidator>());

        // Register options for injection (resolved through IOptions, so validation always runs)
        services.TryAddSingleton(sp => sp.GetRequiredService<IOptions<HashiCorpVaultOptions>>().Value);

        // Register IVaultClient as singleton (TryAdd allows pre-registration)
        // Built from IOptions (not the concrete registration) so a pre-registered options instance cannot skip validation.
        services.TryAddSingleton<IVaultClient>(sp => CreateClient(sp.GetRequiredService<IOptions<HashiCorpVaultOptions>>().Value));

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
