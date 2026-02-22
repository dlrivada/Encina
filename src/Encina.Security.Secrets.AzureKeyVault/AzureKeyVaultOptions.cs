using Azure.Core;
using Azure.Security.KeyVault.Secrets;

namespace Encina.Security.Secrets.AzureKeyVault;

/// <summary>
/// Configuration options for the Azure Key Vault secret provider.
/// </summary>
/// <remarks>
/// <para>
/// Use this class to configure the Azure Key Vault connection, including the vault URI,
/// authentication credentials, and client behavior options.
/// </para>
/// <para>
/// When <see cref="Credential"/> is <c>null</c>, <see cref="Azure.Identity.DefaultAzureCredential"/>
/// is used, which supports managed identities, environment variables, Azure CLI, and other
/// Azure identity sources automatically.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.AddAzureKeyVaultSecrets(
///     new Uri("https://my-vault.vault.azure.net/"),
///     kvOptions =>
///     {
///         kvOptions.Credential = new ManagedIdentityCredential();
///         kvOptions.ClientOptions = new SecretClientOptions
///         {
///             Retry = { MaxRetries = 3 }
///         };
///     });
/// </code>
/// </example>
public sealed class AzureKeyVaultOptions
{
    /// <summary>
    /// Gets or sets the URI of the Azure Key Vault instance.
    /// </summary>
    /// <value>
    /// The vault URI, for example <c>https://my-vault.vault.azure.net/</c>.
    /// This is required and must be set before the provider can be used.
    /// </value>
    public Uri? VaultUri { get; set; }

    /// <summary>
    /// Gets or sets an optional <see cref="TokenCredential"/> for authenticating to the vault.
    /// </summary>
    /// <value>
    /// When <c>null</c> (default), <see cref="Azure.Identity.DefaultAzureCredential"/> is used,
    /// which automatically discovers credentials from the environment.
    /// </value>
    public TokenCredential? Credential { get; set; }

    /// <summary>
    /// Gets or sets optional <see cref="SecretClientOptions"/> for configuring the underlying
    /// <see cref="SecretClient"/> behavior, including retry policies and diagnostics.
    /// </summary>
    /// <value>
    /// When <c>null</c> (default), the Azure SDK default options are used.
    /// </value>
    public SecretClientOptions? ClientOptions { get; set; }
}
