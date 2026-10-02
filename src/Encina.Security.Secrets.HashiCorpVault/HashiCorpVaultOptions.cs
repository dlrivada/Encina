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
/// Both <see cref="VaultAddress"/> and <see cref="AuthMethod"/> are required. The address must be
/// an absolute <c>https</c> URL that does not target a loopback, link-local, cloud metadata or
/// unspecified address; <see cref="AllowInsecureHttp"/> and <see cref="AllowLocalEndpoints"/> relax
/// the scheme and loopback rules for local development. Invalid options throw
/// <see cref="Microsoft.Extensions.Options.OptionsValidationException"/> at registration and at startup.
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

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="VaultAddress"/> may use plain <c>http</c>.
    /// </summary>
    /// <value>
    /// Defaults to <c>false</c>: the address must use <c>https</c>. Set to <c>true</c> only for local
    /// development (for example a Vault dev server); a warning is logged at startup when it is set.
    /// </value>
    public bool AllowInsecureHttp { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="VaultAddress"/> may target <c>localhost</c>
    /// or a loopback address.
    /// </summary>
    /// <value>
    /// Defaults to <c>false</c>. Set to <c>true</c> for local development or a Vault Agent sidecar on
    /// the same host; a warning is logged at startup when it is set. Link-local, cloud metadata and
    /// unspecified addresses are always rejected.
    /// </value>
    public bool AllowLocalEndpoints { get; set; }
}
