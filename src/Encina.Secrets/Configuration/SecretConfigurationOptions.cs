namespace Encina.Secrets.Configuration;

/// <summary>
/// Configuration options for the secret configuration provider.
/// </summary>
/// <remarks>
/// Controls how secrets from an <see cref="ISecretProvider"/> are exposed
/// through the .NET <see cref="Microsoft.Extensions.Configuration.IConfiguration"/> system.
/// </remarks>
/// <example>
/// <code>
/// builder.Configuration.AddEncinaSecrets(builder.Services, options =>
/// {
///     options.SecretPrefix = "myapp/";
///     options.KeyDelimiter = "/";
///     options.ReloadInterval = TimeSpan.FromMinutes(5);
/// });
/// </code>
/// </example>
public sealed class SecretConfigurationOptions
{
    /// <summary>
    /// Gets or sets an optional prefix to filter secrets by name.
    /// </summary>
    /// <remarks>
    /// When set, only secrets whose names start with this prefix are loaded.
    /// The prefix is stripped from the configuration key if <see cref="StripPrefix"/> is <c>true</c>.
    /// </remarks>
    /// <example>
    /// Setting <c>SecretPrefix = "myapp/"</c> loads only secrets starting with <c>"myapp/"</c>.
    /// </example>
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
    /// <para>
    /// The delimiter is replaced with the standard configuration section separator <c>":"</c>.
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
