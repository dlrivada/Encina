using Encina.Security.Secrets.Abstractions;
using Google.Cloud.SecretManager.V1;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Encina.Security.Secrets.GoogleCloudSecretManager;

/// <summary>
/// Extension methods for registering the Google Cloud Secret Manager secrets provider
/// with dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Google Cloud Secret Manager as the secrets provider for Encina.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configureGcp">Action to configure <see cref="GoogleCloudSecretManagerOptions"/>.</param>
    /// <param name="configureSecrets">Optional action to configure <see cref="SecretsOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers the following services:
    /// <list type="bullet">
    /// <item><see cref="SecretManagerServiceClient"/> — Singleton, using Application Default Credentials</item>
    /// <item><see cref="ISecretReader"/> → <see cref="GoogleCloudSecretManagerProvider"/> with decorator chain</item>
    /// <item><see cref="ISecretWriter"/> → <see cref="GoogleCloudSecretManagerProvider"/></item>
    /// <item><see cref="ISecretRotator"/> → <see cref="GoogleCloudSecretManagerProvider"/></item>
    /// </list>
    /// </para>
    /// <para>
    /// All registrations use <c>TryAdd</c>, allowing you to register custom implementations
    /// before calling this method. For example, pre-register a configured
    /// <see cref="SecretManagerServiceClient"/> to use custom credentials instead of
    /// Application Default Credentials.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Basic setup with Application Default Credentials
    /// services.AddGoogleCloudSecretManager(
    ///     gcp => gcp.ProjectId = "my-gcp-project");
    ///
    /// // With caching configuration
    /// services.AddGoogleCloudSecretManager(
    ///     gcp => gcp.ProjectId = "my-gcp-project",
    ///     secrets =>
    ///     {
    ///         secrets.EnableCaching = true;
    ///         secrets.DefaultCacheDuration = TimeSpan.FromMinutes(10);
    ///     });
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> or <paramref name="configureGcp"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="GoogleCloudSecretManagerOptions.ProjectId"/> is empty.
    /// </exception>
    public static IServiceCollection AddGoogleCloudSecretManager(
        this IServiceCollection services,
        Action<GoogleCloudSecretManagerOptions> configureGcp,
        Action<SecretsOptions>? configureSecrets = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureGcp);

        var gcpOptions = new GoogleCloudSecretManagerOptions();
        configureGcp(gcpOptions);

        if (string.IsNullOrWhiteSpace(gcpOptions.ProjectId))
        {
            throw new InvalidOperationException(
                "GoogleCloudSecretManagerOptions.ProjectId is required. " +
                "Provide the GCP project ID (e.g., 'my-gcp-project').");
        }

        // Register SecretManagerServiceClient as singleton (TryAdd allows pre-registration)
        services.TryAddSingleton(_ => SecretManagerServiceClient.Create());

        // Register options for injection
        services.TryAddSingleton(gcpOptions);

        // Register as ISecretWriter and ISecretRotator (TryAdd allows pre-registration)
        services.TryAddSingleton<GoogleCloudSecretManagerProvider>();
        services.TryAddSingleton<ISecretWriter>(sp =>
            sp.GetRequiredService<GoogleCloudSecretManagerProvider>());
        services.TryAddSingleton<ISecretRotator>(sp =>
            sp.GetRequiredService<GoogleCloudSecretManagerProvider>());

        // Register as ISecretReader with the core decorator chain (caching, auditing)
        return services.AddEncinaSecrets<GoogleCloudSecretManagerProvider>(configureSecrets);
    }
}
