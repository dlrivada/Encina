using Azure.Core;

namespace Encina.Secrets.AzureKeyVault;

/// <summary>
/// Configuration options for the Azure Key Vault secret provider.
/// </summary>
public sealed class KeyVaultSecretProviderOptions
{
    /// <summary>
    /// Gets or sets the URI of the Azure Key Vault (e.g., <c>https://my-vault.vault.azure.net/</c>).
    /// </summary>
    public string VaultUri { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the token credential to authenticate with Azure Key Vault.
    /// When <c>null</c>, <see cref="Azure.Identity.DefaultAzureCredential"/> is used.
    /// </summary>
    public TokenCredential? Credential { get; set; }

    /// <summary>
    /// Gets or sets the health check configuration.
    /// </summary>
    public ProviderHealthCheckOptions ProviderHealthCheck { get; set; } = new();
}
