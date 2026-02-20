using LanguageExt;

namespace Encina.Security.Encryption.Abstractions;

/// <summary>
/// Provides field-level encryption and decryption operations.
/// </summary>
/// <remarks>
/// <para>
/// Implementations handle the actual cryptographic operations for encrypting and decrypting
/// individual field values. The default implementation uses AES-256-GCM for authenticated
/// encryption with associated data (AEAD).
/// </para>
/// <para>
/// All methods follow the Railway Oriented Programming (ROP) pattern, returning
/// <see cref="Either{EncinaError, T}"/> to represent success or failure without exceptions.
/// </para>
/// <para>
/// Implementations must be thread-safe and suitable for use in concurrent pipeline execution.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Encrypting a string field
/// var context = new EncryptionContext { Purpose = "UserProfile.Email" };
/// var result = await encryptor.EncryptStringAsync("user@example.com", context);
/// var encrypted = result.Match(
///     Right: value => value,
///     Left: error => throw new InvalidOperationException(error.Message));
///
/// // Decrypting back
/// var decrypted = await encryptor.DecryptStringAsync(encrypted, context);
/// </code>
/// </example>
public interface IFieldEncryptor
{
    /// <summary>
    /// Encrypts a string value.
    /// </summary>
    /// <param name="plaintext">The plaintext string to encrypt.</param>
    /// <param name="context">The encryption context specifying key, purpose, and associated data.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// <c>Right&lt;EncryptedValue&gt;</c> on success, or <c>Left&lt;EncinaError&gt;</c> on failure
    /// (e.g., key not found, algorithm not supported).
    /// </returns>
    ValueTask<Either<EncinaError, EncryptedValue>> EncryptStringAsync(
        string plaintext,
        EncryptionContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Decrypts an encrypted value back to its original string representation.
    /// </summary>
    /// <param name="encryptedValue">The encrypted value containing ciphertext and metadata.</param>
    /// <param name="context">The encryption context used during encryption.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// <c>Right&lt;string&gt;</c> containing the decrypted plaintext on success, or
    /// <c>Left&lt;EncinaError&gt;</c> on failure (e.g., decryption failed, invalid ciphertext).
    /// </returns>
    ValueTask<Either<EncinaError, string>> DecryptStringAsync(
        EncryptedValue encryptedValue,
        EncryptionContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Encrypts a byte array.
    /// </summary>
    /// <param name="plaintext">The plaintext bytes to encrypt.</param>
    /// <param name="context">The encryption context specifying key, purpose, and associated data.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// <c>Right&lt;EncryptedValue&gt;</c> on success, or <c>Left&lt;EncinaError&gt;</c> on failure.
    /// </returns>
    ValueTask<Either<EncinaError, EncryptedValue>> EncryptBytesAsync(
        ReadOnlyMemory<byte> plaintext,
        EncryptionContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Decrypts an encrypted value back to its original byte representation.
    /// </summary>
    /// <param name="encryptedValue">The encrypted value containing ciphertext and metadata.</param>
    /// <param name="context">The encryption context used during encryption.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// <c>Right&lt;byte[]&gt;</c> containing the decrypted bytes on success, or
    /// <c>Left&lt;EncinaError&gt;</c> on failure.
    /// </returns>
    ValueTask<Either<EncinaError, byte[]>> DecryptBytesAsync(
        EncryptedValue encryptedValue,
        EncryptionContext context,
        CancellationToken cancellationToken = default);
}
