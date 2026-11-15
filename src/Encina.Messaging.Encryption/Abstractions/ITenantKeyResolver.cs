namespace Encina.Messaging.Encryption.Abstractions;

/// <summary>
/// Maps a tenant identifier to an encryption key identifier.
/// </summary>
/// <remarks>
/// <para>
/// The message encryption pipeline does not call this interface yet; it is registered
/// so that applications can resolve it themselves.
/// </para>
/// <para>
/// The default implementation (<c>DefaultTenantKeyResolver</c>) uses the naming convention
/// <c>tenant-{tenantId}-key</c>. Custom implementations can map tenants to cloud KMS key
/// ARNs, Azure Key Vault key names, or any other key identification scheme.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Default convention: tenant-{tenantId}-key
/// var keyId = resolver.ResolveKeyId("acme-corp");
/// // Returns: "tenant-acme-corp-key"
///
/// // Custom implementation for cloud KMS
/// public class AzureKeyVaultTenantKeyResolver : ITenantKeyResolver
/// {
///     public string ResolveKeyId(string tenantId)
///         => $"https://vault.azure.net/keys/{tenantId}-encryption-key";
/// }
/// </code>
/// </example>
public interface ITenantKeyResolver
{
    /// <summary>
    /// Resolves the encryption key identifier for the specified tenant.
    /// </summary>
    /// <param name="tenantId">
    /// The unique identifier of the tenant whose encryption key should be resolved.
    /// Must not be <c>null</c> or empty.
    /// </param>
    /// <returns>
    /// The key identifier to use for encrypting/decrypting messages belonging to this tenant.
    /// The returned key ID is passed to <see cref="Security.Encryption.Abstractions.IKeyProvider.GetKeyAsync"/>
    /// for key material retrieval.
    /// </returns>
    string ResolveKeyId(string tenantId);
}
