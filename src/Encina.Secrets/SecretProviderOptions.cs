namespace Encina.Secrets;

/// <summary>
/// Base configuration options for secret providers.
/// </summary>
/// <remarks>
/// <para>
/// Each concrete secret provider (Azure Key Vault, AWS Secrets Manager, etc.) should
/// create its own options class with provider-specific settings, and include a
/// <see cref="ProviderHealthCheckOptions"/> instance to control health check behavior.
/// </para>
/// <para>
/// This class is sealed to prevent inheritance. Provider-specific options should
/// compose this pattern rather than inherit from it.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Provider-specific options compose ProviderHealthCheckOptions
/// public sealed class AzureKeyVaultOptions
/// {
///     public string VaultUri { get; set; } = string.Empty;
///     public ProviderHealthCheckOptions ProviderHealthCheck { get; set; } = new();
/// }
/// </code>
/// </example>
public sealed class SecretProviderOptions
{
    /// <summary>
    /// Gets or sets the health check configuration for the secret provider.
    /// </summary>
    public ProviderHealthCheckOptions ProviderHealthCheck { get; set; } = new();
}

/// <summary>
/// Configuration options for provider health checks.
/// </summary>
/// <remarks>
/// Controls whether a health check is registered and which tags are applied.
/// Default tags are <c>["encina", "secrets", "ready"]</c>.
/// </remarks>
public sealed class ProviderHealthCheckOptions
{
    private static readonly string[] DefaultHealthCheckTags = ["encina", "secrets", "ready"];

    /// <summary>
    /// Gets or sets whether to register a health check for the provider.
    /// </summary>
    /// <remarks>
    /// Default is <c>false</c>.
    /// </remarks>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the tags to apply to the health check.
    /// </summary>
    /// <remarks>
    /// Default is <c>["encina", "secrets", "ready"]</c>.
    /// </remarks>
    public IReadOnlyList<string> Tags { get; set; } = DefaultHealthCheckTags;
}
