using Microsoft.Extensions.Configuration;

namespace Encina.Secrets.Configuration;

/// <summary>
/// An <see cref="IConfigurationSource"/> that loads secrets from an <see cref="ISecretProvider"/>
/// into the .NET configuration system.
/// </summary>
/// <remarks>
/// <para>
/// This source creates a <see cref="SecretConfigurationProvider"/> that retrieves secrets
/// from the registered <see cref="ISecretProvider"/> and exposes them as configuration values.
/// </para>
/// <para>
/// Secrets are loaded synchronously during application startup using
/// <c>GetAwaiter().GetResult()</c>, consistent with other configuration providers
/// and Encina startup patterns.
/// </para>
/// </remarks>
internal sealed class SecretConfigurationSource : IConfigurationSource
{
    private readonly IServiceProvider _serviceProvider;
    private readonly SecretConfigurationOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="SecretConfigurationSource"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider used to resolve <see cref="ISecretProvider"/>.</param>
    /// <param name="options">The configuration options.</param>
    public SecretConfigurationSource(IServiceProvider serviceProvider, SecretConfigurationOptions options)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(options);

        _serviceProvider = serviceProvider;
        _options = options;
    }

    /// <inheritdoc />
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new SecretConfigurationProvider(_serviceProvider, _options);
    }
}
