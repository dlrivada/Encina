using Amazon;
using Amazon.KeyManagementService;
using Encina.Security.Encryption.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Encina.Messaging.Encryption.AwsKms;

/// <summary>
/// Extension methods for registering AWS KMS message encryption services.
/// </summary>
/// <remarks>
/// <para>
/// Registers <see cref="AwsKmsKeyProvider"/> as <see cref="IKeyProvider"/>,
/// which integrates with <see cref="DefaultMessageEncryptionProvider"/> via
/// <c>AddEncinaMessageEncryption()</c>.
/// </para>
/// </remarks>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers AWS KMS as the key provider for message encryption.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action for <see cref="AwsKmsOptions"/>.</param>
    /// <param name="configureEncryption">Optional configuration action for <see cref="MessageEncryptionOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEncinaMessageEncryptionAwsKms(
        this IServiceCollection services,
        Action<AwsKmsOptions> configure,
        Action<MessageEncryptionOptions>? configureEncryption = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var kmsOptions = new AwsKmsOptions();
        configure(kmsOptions);

        // Register AWS options
        services.Configure(configure);

        // Register IAmazonKeyManagementService as singleton (TryAdd allows pre-registration)
        services.TryAddSingleton<IAmazonKeyManagementService>(_ =>
        {
            if (kmsOptions.ClientConfig is not null)
            {
                return new AmazonKeyManagementServiceClient(kmsOptions.ClientConfig);
            }

            if (!string.IsNullOrWhiteSpace(kmsOptions.Region))
            {
                return new AmazonKeyManagementServiceClient(RegionEndpoint.GetBySystemName(kmsOptions.Region));
            }

            return new AmazonKeyManagementServiceClient();
        });

        // Register IKeyProvider → AwsKmsKeyProvider
        services.TryAddSingleton<IKeyProvider, AwsKmsKeyProvider>();

        // Ensure base message encryption is registered
        services.AddEncinaMessageEncryption(configureEncryption);

        return services;
    }
}
