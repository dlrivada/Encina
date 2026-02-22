using Amazon.SecretsManager;
using Encina.Security.Secrets.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Encina.Security.Secrets.AwsSecretsManager;

/// <summary>
/// Extension methods for registering the AWS Secrets Manager secrets provider with dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds AWS Secrets Manager as the secrets provider for Encina.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configureAws">Optional action to configure <see cref="AwsSecretsManagerOptions"/>.</param>
    /// <param name="configureSecrets">Optional action to configure <see cref="SecretsOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers the following services:
    /// <list type="bullet">
    /// <item><see cref="IAmazonSecretsManager"/> — Singleton, using the default credential chain unless overridden</item>
    /// <item><see cref="ISecretReader"/> → <see cref="AwsSecretsManagerProvider"/> with decorator chain</item>
    /// <item><see cref="ISecretWriter"/> → <see cref="AwsSecretsManagerProvider"/></item>
    /// <item><see cref="ISecretRotator"/> → <see cref="AwsSecretsManagerProvider"/></item>
    /// </list>
    /// </para>
    /// <para>
    /// All registrations use <c>TryAdd</c>, allowing you to register custom implementations
    /// before calling this method.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Basic setup with default credential chain
    /// services.AddAwsSecretsManager();
    ///
    /// // With explicit region and credentials
    /// services.AddAwsSecretsManager(
    ///     aws =>
    ///     {
    ///         aws.Region = RegionEndpoint.USEast1;
    ///         aws.Credentials = new EnvironmentVariablesAWSCredentials();
    ///     },
    ///     secrets =>
    ///     {
    ///         secrets.EnableCaching = true;
    ///         secrets.DefaultCacheDuration = TimeSpan.FromMinutes(10);
    ///     });
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> is <c>null</c>.
    /// </exception>
    public static IServiceCollection AddAwsSecretsManager(
        this IServiceCollection services,
        Action<AwsSecretsManagerOptions>? configureAws = null,
        Action<SecretsOptions>? configureSecrets = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var awsOptions = new AwsSecretsManagerOptions();
        configureAws?.Invoke(awsOptions);

        // Register IAmazonSecretsManager as singleton (TryAdd allows pre-registration)
        services.TryAddSingleton<IAmazonSecretsManager>(_ => CreateClient(awsOptions));

        // Register options for injection
        services.Configure<AwsSecretsManagerOptions>(o =>
        {
            o.Region = awsOptions.Region;
            o.Credentials = awsOptions.Credentials;
            o.ClientConfig = awsOptions.ClientConfig;
        });

        // Register as ISecretWriter and ISecretRotator (TryAdd allows pre-registration)
        services.TryAddSingleton<AwsSecretsManagerProvider>();
        services.TryAddSingleton<ISecretWriter>(sp =>
            sp.GetRequiredService<AwsSecretsManagerProvider>());
        services.TryAddSingleton<ISecretRotator>(sp =>
            sp.GetRequiredService<AwsSecretsManagerProvider>());

        // Register as ISecretReader with the core decorator chain (caching, auditing)
        return services.AddEncinaSecrets<AwsSecretsManagerProvider>(configureSecrets);
    }

    private static AmazonSecretsManagerClient CreateClient(AwsSecretsManagerOptions options)
    {
        var config = options.ClientConfig ?? new AmazonSecretsManagerConfig();

        if (options.Region is not null)
        {
            config.RegionEndpoint = options.Region;
        }

        return options.Credentials is not null
            ? new AmazonSecretsManagerClient(options.Credentials, config)
            : new AmazonSecretsManagerClient(config);
    }
}
