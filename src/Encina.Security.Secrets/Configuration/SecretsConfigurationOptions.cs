using Microsoft.Extensions.Configuration;

namespace Encina.Security.Secrets.Configuration;

/// <summary>
/// Configuration options for the secrets configuration provider.
/// </summary>
/// <remarks>
/// Controls how secrets from an <see cref="Abstractions.ISecretReader"/> are exposed
/// through the .NET <see cref="IConfiguration"/> system.
/// </remarks>
/// <example>
/// <code>
/// builder.Configuration.AddEncinaSecrets(builder.Services, options =>
/// {
///     options.SecretNames = ["database-connection-string", "api-key"];
///     options.KeyDelimiter = "--";
///     options.ReloadInterval = TimeSpan.FromMinutes(5);
/// });
/// </code>
/// </example>
public sealed class SecretsConfigurationOptions
{
    /// <summary>
    /// Gets or sets the list of secret names to load into configuration.
    /// </summary>
    /// <remarks>
    /// Each secret name is resolved via <see cref="Abstractions.ISecretReader.GetSecretAsync"/>
    /// and added to the configuration dictionary.
    /// </remarks>
    public IReadOnlyList<string> SecretNames { get; set; } = [];

    /// <summary>
    /// Gets or sets an optional prefix to prepend when resolving secret names.
    /// </summary>
    /// <remarks>
    /// When set, the prefix is prepended to each secret name before querying the reader.
    /// The prefix is optionally stripped from the configuration key if <see cref="StripPrefix"/> is <c>true</c>.
    /// </remarks>
    public string? SecretPrefix { get; set; }

    /// <summary>
    /// Gets or sets whether to strip the <see cref="SecretPrefix"/> from configuration keys.
    /// </summary>
    /// <remarks>
    /// Default is <c>true</c>. When <c>true</c> and <see cref="SecretPrefix"/> is <c>"myapp/"</c>,
    /// a secret named <c>"myapp/ConnectionString"</c> becomes available as <c>"ConnectionString"</c>.
    /// </remarks>
    public bool StripPrefix { get; set; } = true;

    /// <summary>
    /// Gets or sets the delimiter used to map secret names to hierarchical configuration sections.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Default is <c>"--"</c>. Secret names containing this delimiter are split into
    /// configuration sections. For example, with the default delimiter, a secret named
    /// <c>"Database--ConnectionString"</c> maps to <c>Configuration["Database:ConnectionString"]</c>.
    /// </para>
    /// </remarks>
    public string KeyDelimiter { get; set; } = "--";

    /// <summary>
    /// Gets or sets the optional interval for periodic reloading of secrets.
    /// </summary>
    /// <remarks>
    /// When <c>null</c> (default), secrets are loaded once during application startup.
    /// When set, a background timer periodically reloads all secrets at the specified interval.
    /// </remarks>
    public TimeSpan? ReloadInterval { get; set; }
}
