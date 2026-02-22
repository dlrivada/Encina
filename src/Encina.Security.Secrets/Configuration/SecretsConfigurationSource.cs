using Microsoft.Extensions.Configuration;

namespace Encina.Security.Secrets.Configuration;

/// <summary>
/// An <see cref="IConfigurationSource"/> that loads secrets from an
/// <see cref="Abstractions.ISecretReader"/> into the .NET configuration system.
/// </summary>
/// <remarks>
/// <para>
/// This source creates a <see cref="SecretsConfigurationProvider"/> that retrieves secrets
/// from the registered <see cref="Abstractions.ISecretReader"/> and exposes them as configuration values.
/// </para>
/// </remarks>
internal sealed class SecretsConfigurationSource : IConfigurationSource
{
    private readonly IServiceProvider _serviceProvider;
    private readonly SecretsConfigurationOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="SecretsConfigurationSource"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider used to resolve <see cref="Abstractions.ISecretReader"/>.</param>
    /// <param name="options">The configuration options.</param>
    public SecretsConfigurationSource(IServiceProvider serviceProvider, SecretsConfigurationOptions options)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(options);

        _serviceProvider = serviceProvider;
        _options = options;
    }

    /// <inheritdoc />
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new SecretsConfigurationProvider(_serviceProvider, _options);
    }
}
