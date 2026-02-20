using LanguageExt;

namespace Encina.Security.Encryption.Abstractions;

/// <summary>
/// Provides cryptographic key management operations including retrieval, rotation, and current key identification.
/// </summary>
/// <remarks>
/// <para>
/// Implementations manage the lifecycle of encryption keys, supporting key rotation
/// without downtime. Multiple keys can coexist: one current key for new encryption
/// operations and previous keys for decrypting existing data.
/// </para>
/// <para>
/// Built-in implementations include <c>InMemoryKeyProvider</c> for testing.
/// Cloud KMS integrations (Azure Key Vault, AWS KMS, HashiCorp Vault) are available
/// as separate packages.
/// </para>
/// <para>
/// All methods follow the Railway Oriented Programming (ROP) pattern, returning
/// <see cref="Either{EncinaError, T}"/> to represent success or failure without exceptions.
/// </para>
/// </remarks>
public interface IKeyProvider
{
    /// <summary>
    /// Retrieves the encryption key material for the specified key identifier.
    /// </summary>
    /// <param name="keyId">The unique identifier of the key to retrieve.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// <c>Right&lt;byte[]&gt;</c> containing the key material on success, or
    /// <c>Left&lt;EncinaError&gt;</c> if the key is not found or retrieval fails.
    /// </returns>
    /// <remarks>
    /// Key material should be treated as sensitive data and cleared from memory
    /// when no longer needed. Implementations should consider using secure memory
    /// handling practices.
    /// </remarks>
    ValueTask<Either<EncinaError, byte[]>> GetKeyAsync(
        string keyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the identifier of the current active key used for new encryption operations.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// <c>Right&lt;string&gt;</c> containing the current key identifier on success, or
    /// <c>Left&lt;EncinaError&gt;</c> if no current key is configured.
    /// </returns>
    /// <remarks>
    /// The current key is used for all new encryption operations. Previous keys remain
    /// available for decryption through <see cref="GetKeyAsync"/> to support key rotation.
    /// </remarks>
    ValueTask<Either<EncinaError, string>> GetCurrentKeyIdAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Rotates the active encryption key, making a new key the current default.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// <c>Right&lt;string&gt;</c> containing the new current key identifier on success, or
    /// <c>Left&lt;EncinaError&gt;</c> if rotation fails.
    /// </returns>
    /// <remarks>
    /// <para>
    /// After rotation, new encryption operations use the new key while existing ciphertext
    /// remains decryptable using the previous key. Re-encryption of existing data must be
    /// performed separately if required by compliance policies.
    /// </para>
    /// <para>
    /// Implementations should ensure atomicity: either the rotation completes fully or
    /// the previous key remains active.
    /// </para>
    /// </remarks>
    ValueTask<Either<EncinaError, string>> RotateKeyAsync(
        CancellationToken cancellationToken = default);
}
