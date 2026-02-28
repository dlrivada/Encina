using Encina.Compliance.Anonymization.Model;

using LanguageExt;

namespace Encina.Compliance.Anonymization;

/// <summary>
/// Abstraction for managing cryptographic keys used in pseudonymization and tokenization operations.
/// </summary>
/// <remarks>
/// <para>
/// The key provider manages the lifecycle of cryptographic keys: creation, retrieval, rotation,
/// and listing. Keys are identified by a string <c>keyId</c> and are used by
/// <see cref="IPseudonymizer"/> for encryption/decryption and by <see cref="ITokenizer"/>
/// for token mapping encryption.
/// </para>
/// <para>
/// Key rotation is a first-class operation: when a key is rotated, a new key is generated
/// and the old key is marked as inactive. All existing pseudonymized data and token mappings
/// referencing the old key should be re-encrypted with the new key.
/// </para>
/// <para>
/// Built-in implementations include:
/// <list type="bullet">
/// <item><b>InMemoryKeyProvider</b>: For testing and development (keys stored in memory)</item>
/// <item><b>DataProtectionKeyProvider</b>: For simple production use (.NET Data Protection API)</item>
/// </list>
/// </para>
/// <para>
/// Users can implement this interface for external key management systems:
/// Azure Key Vault, AWS KMS, HashiCorp Vault, Google Cloud KMS, etc.
/// </para>
/// <para>
/// Per EDPB Guidelines 01/2025 Section 4.3, key management must ensure that keys are:
/// (1) stored separately from the pseudonymized data, (2) protected with appropriate
/// access controls, and (3) rotatable without data loss.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Get the currently active key
/// var activeKeyId = await keyProvider.GetActiveKeyIdAsync(cancellationToken);
///
/// // Retrieve key material for encryption
/// var keyBytes = await keyProvider.GetKeyAsync(activeKeyId, cancellationToken);
///
/// // Rotate a key (generates new key, marks old as inactive)
/// var newKeyInfo = await keyProvider.RotateKeyAsync(activeKeyId, cancellationToken);
/// </code>
/// </example>
public interface IKeyProvider
{
    /// <summary>
    /// Retrieves the raw key material for the specified key identifier.
    /// </summary>
    /// <param name="keyId">The unique identifier of the key to retrieve.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// The raw key bytes (256 bits for AES-256-GCM, variable for HMAC-SHA256),
    /// or an <see cref="EncinaError"/> if the key was not found or access was denied.
    /// </returns>
    /// <remarks>
    /// Key material should be handled with care and never logged, serialized to disk,
    /// or transmitted over insecure channels.
    /// </remarks>
    ValueTask<Either<EncinaError, byte[]>> GetKeyAsync(
        string keyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Rotates the specified key by generating a new key and marking the old one as inactive.
    /// </summary>
    /// <param name="keyId">The identifier of the key to rotate.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="KeyInfo"/> describing the newly generated key,
    /// or an <see cref="EncinaError"/> if the rotation failed.
    /// </returns>
    /// <remarks>
    /// <para>
    /// After rotation, the old key remains accessible for decryption of existing data
    /// but is marked as inactive (<see cref="KeyInfo.IsActive"/> = <c>false</c>).
    /// New encryption operations should use the new key.
    /// </para>
    /// <para>
    /// Callers are responsible for re-encrypting existing data with the new key
    /// to complete the rotation. The <see cref="ITokenMappingStore"/> stores a
    /// <see cref="TokenMapping.KeyId"/> reference that must be updated.
    /// </para>
    /// </remarks>
    ValueTask<Either<EncinaError, KeyInfo>> RotateKeyAsync(
        string keyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the identifier of the currently active key for new encryption operations.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// The identifier of the active key, or an <see cref="EncinaError"/> if no active
    /// key exists (e.g., key provider not initialized).
    /// </returns>
    /// <remarks>
    /// There is always exactly one active key at any given time. The active key is the
    /// one used for new pseudonymization and tokenization operations.
    /// </remarks>
    ValueTask<Either<EncinaError, string>> GetActiveKeyIdAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all keys (active and inactive) managed by this provider.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A read-only list of <see cref="KeyInfo"/> records describing all managed keys,
    /// or an <see cref="EncinaError"/> on failure.
    /// </returns>
    /// <remarks>
    /// Useful for key inventory, audit reporting, and identifying keys that need rotation
    /// (e.g., keys approaching their <see cref="KeyInfo.ExpiresAtUtc"/>).
    /// </remarks>
    ValueTask<Either<EncinaError, IReadOnlyList<KeyInfo>>> ListKeysAsync(
        CancellationToken cancellationToken = default);
}
