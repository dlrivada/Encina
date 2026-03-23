using System.Text.Json.Serialization;

using Azure.Core;
using Azure.Security.KeyVault.Keys;

namespace Encina.Messaging.Encryption.AzureKeyVault;

/// <summary>
/// Configuration options for the Azure Key Vault key provider.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="VaultUri"/> and <see cref="KeyName"/> are required.
/// When <see cref="Credential"/> is <c>null</c>, <c>DefaultAzureCredential</c> is used.
/// </para>
/// </remarks>
public sealed class AzureKeyVaultOptions
{
    /// <summary>
    /// Gets or sets the URI of the Azure Key Vault instance.
    /// </summary>
    public Uri? VaultUri { get; set; }

    /// <summary>
    /// Gets or sets the name of the key in Azure Key Vault.
    /// </summary>
    public string? KeyName { get; set; }

    /// <summary>
    /// Gets or sets the specific key version to use. When <c>null</c>, the latest version is used.
    /// </summary>
    public string? KeyVersion { get; set; }

    /// <summary>
    /// Gets or sets the Azure credential to use for authentication.
    /// When <c>null</c>, <c>DefaultAzureCredential</c> is used.
    /// </summary>
    /// <remarks>
    /// WARNING: Contains sensitive credential data. Never log or serialize.
    /// </remarks>
    [JsonIgnore]
    public TokenCredential? Credential { get; set; }

    /// <summary>
    /// Gets or sets the client options for the <see cref="KeyClient"/>.
    /// </summary>
    public KeyClientOptions? ClientOptions { get; set; }

    /// <inheritdoc/>
    public override string ToString() =>
        $"AzureKeyVaultOptions {{ VaultUri={VaultUri}, Key={KeyName} }}";
}
