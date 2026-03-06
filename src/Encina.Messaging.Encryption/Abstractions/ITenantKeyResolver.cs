namespace Encina.Messaging.Encryption.Abstractions;

/// <summary>
/// Resolves encryption key identifiers based on tenant context for multi-tenant key isolation.
/// </summary>
/// <remarks>
/// <para>
/// In multi-tenant applications, each tenant's messages should be encrypted with isolated
/// keys to ensure that a key compromise affects only a single tenant. This interface maps
/// tenant identifiers to their corresponding encryption key IDs.
/// </para>
/// <para>
/// The default implementation (<c>DefaultTenantKeyResolver</c>) uses the naming convention
/// <c>tenant-{tenantId}-key</c>. Custom implementations can map tenants to cloud KMS key
/// ARNs, Azure Key Vault key names, or any other key identification scheme.
/// </para>
/// <para>
/// Activated per message type via <c>[EncryptedMessage(UseTenantKey = true)]</c> or globally
/// via <c>MessageEncryptionOptions.UseTenantKeys = true</c>.
/// </para>
/// <para>
/// <strong>Compliance</strong>: Per-tenant key isolation supports GDPR data isolation
/// requirements and SOC 2 access control principles.
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
