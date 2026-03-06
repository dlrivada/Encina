using Encina.Messaging.Encryption.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Encina.Messaging.Encryption.DataProtection;

/// <summary>
/// Extension methods for registering Data Protection message encryption services.
/// </summary>
/// <remarks>
/// <para>
/// Registers <see cref="DataProtectionMessageEncryptionProvider"/> as
/// <see cref="IMessageEncryptionProvider"/>, replacing the default provider.
/// Data Protection handles both key management and encryption.
/// </para>
/// </remarks>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers ASP.NET Core Data Protection as the encryption provider for message encryption.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action for <see cref="DataProtectionEncryptionOptions"/>.</param>
    /// <param name="configureEncryption">Optional configuration action for <see cref="MessageEncryptionOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers <see cref="DataProtectionMessageEncryptionProvider"/> as
    /// <see cref="IMessageEncryptionProvider"/>. Unlike Azure/AWS KMS providers that implement
    /// <c>IKeyProvider</c>, Data Protection handles both key management and encryption, so it
    /// replaces the entire encryption provider.
    /// </para>
    /// <para>
    /// Ensure <c>services.AddDataProtection()</c> is called before this method if Data Protection
    /// is not already registered by the hosting framework.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddEncinaMessageEncryptionDataProtection(
        this IServiceCollection services,
        Action<DataProtectionEncryptionOptions>? configure = null,
        Action<MessageEncryptionOptions>? configureEncryption = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register Data Protection options
        if (configure is not null)
        {
            services.Configure(configure);
        }
        else
        {
            services.Configure<DataProtectionEncryptionOptions>(_ => { });
        }

        // Register IMessageEncryptionProvider → DataProtectionMessageEncryptionProvider
        // This replaces any previously registered provider
        services.TryAddSingleton<IMessageEncryptionProvider, DataProtectionMessageEncryptionProvider>();

        // Ensure base message encryption is registered (decorator, health check, etc.)
        services.AddEncinaMessageEncryption<DataProtectionMessageEncryptionProvider>(configureEncryption);

        return services;
    }
}
