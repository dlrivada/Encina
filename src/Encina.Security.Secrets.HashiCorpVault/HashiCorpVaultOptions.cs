using VaultSharp.V1.AuthMethods;

namespace Encina.Security.Secrets.HashiCorpVault;

/// <summary>
/// Configuration options for the HashiCorp Vault secret provider.
/// </summary>
/// <remarks>
/// <para>
/// Use this class to configure the HashiCorp Vault connection, including the server address,
/// authentication method, and KV v2 mount point.
/// </para>
/// <para>
/// Both <see cref="VaultAddress"/> and <see cref="AuthMethod"/> are required. The provider
/// will throw <see cref="InvalidOperationException"/> at startup if either is missing.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.AddHashiCorpVaultSecrets(
///     vault =>
///     {
///         vault.VaultAddress = "https://vault.example.com:8200";
///         vault.AuthMethod = new TokenAuthMethodInfo("hvs.my-token");
///         vault.MountPoint = "secret";
///     });
/// </code>
/// </example>
public sealed class HashiCorpVaultOptions
{
    /// <summary>
    /// Gets or sets the Vault server address.
    /// </summary>
    /// <value>
    /// The full URL of the Vault server (e.g., <c>https://vault.example.com:8200</c>).
    /// This property is <b>required</b>.
    /// </value>
    public string VaultAddress { get; set; } = "";

    /// <summary>
    /// Gets or sets the authentication method to use when connecting to Vault.
    /// </summary>
    /// <value>
    /// An <see cref="IAuthMethodInfo"/> implementation such as <c>TokenAuthMethodInfo</c>,
    /// <c>AppRoleAuthMethodInfo</c>, or <c>KubernetesAuthMethodInfo</c>.
    /// This property is <b>required</b>.
    /// </value>
    public IAuthMethodInfo? AuthMethod { get; set; }

    /// <summary>
    /// Gets or sets the mount point of the KV v2 secrets engine.
    /// </summary>
    /// <value>
    /// The mount point path. Defaults to <c>"secret"</c>.
    /// </value>
    public string MountPoint { get; set; } = "secret";
}
