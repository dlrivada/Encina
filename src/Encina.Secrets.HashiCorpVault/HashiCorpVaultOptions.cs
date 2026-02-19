using VaultSharp.V1.AuthMethods;

namespace Encina.Secrets.HashiCorpVault;

/// <summary>
/// Configuration options for the HashiCorp Vault secret provider.
/// </summary>
public sealed class HashiCorpVaultOptions
{
    /// <summary>
    /// Gets or sets the Vault server address (e.g., <c>https://vault.example.com:8200</c>).
    /// </summary>
    public string VaultAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the mount point for the KV v2 secrets engine.
    /// </summary>
    /// <remarks>
    /// Default is <c>secret</c>, which is the default mount point for the KV v2 engine.
    /// </remarks>
    public string MountPoint { get; set; } = "secret";

    /// <summary>
    /// Gets or sets the authentication method to use with Vault.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Supported authentication methods include:
    /// <list type="bullet">
    /// <item><see cref="VaultSharp.V1.AuthMethods.Token.TokenAuthMethodInfo"/> for token-based authentication.</item>
    /// <item><see cref="VaultSharp.V1.AuthMethods.AppRole.AppRoleAuthMethodInfo"/> for AppRole authentication.</item>
    /// <item><see cref="VaultSharp.V1.AuthMethods.Kubernetes.KubernetesAuthMethodInfo"/> for Kubernetes authentication.</item>
    /// </list>
    /// </para>
    /// </remarks>
    public IAuthMethodInfo? AuthMethod { get; set; }

    /// <summary>
    /// Gets or sets the health check configuration.
    /// </summary>
    public ProviderHealthCheckOptions ProviderHealthCheck { get; set; } = new();
}
