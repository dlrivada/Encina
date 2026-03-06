using System.Collections.Immutable;
using Encina.Messaging.Encryption.Model;
using LanguageExt;

namespace Encina.Messaging.Encryption.Abstractions;

/// <summary>
/// Provides message-level encryption and decryption operations for outbox/inbox payloads.
/// </summary>
/// <remarks>
/// <para>
/// This interface is the primary facade for encrypting serialized message content before
/// database persistence and decrypting it upon retrieval. Unlike field-level encryption
/// (<see cref="Security.Encryption.Abstractions.IFieldEncryptor"/>), which operates on
/// individual properties, this interface encrypts entire serialized message payloads.
/// </para>
/// <para>
/// The default implementation (<c>DefaultMessageEncryptionProvider</c>) delegates to
/// <see cref="Security.Encryption.Abstractions.IFieldEncryptor"/> for cryptographic
/// operations and <see cref="Security.Encryption.Abstractions.IKeyProvider"/> for key management,
/// using AES-256-GCM authenticated encryption.
/// </para>
/// <para>
/// All methods follow the Railway Oriented Programming (ROP) pattern, returning
/// <see cref="Either{EncinaError, T}"/> to represent success or failure without exceptions.
/// </para>
/// <para>
/// Implementations must be thread-safe and suitable for use in concurrent message processing.
/// </para>
/// <para>
/// <strong>Compliance</strong>: Supports GDPR (Article 32), HIPAA (§164.312(a)(2)(iv)),
/// and PCI-DSS (Requirement 3) encryption-at-rest requirements.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Encrypting a serialized message payload
/// var context = new MessageEncryptionContext { KeyId = "msg-key-2024" };
/// var plaintext = Encoding.UTF8.GetBytes(serializedJson);
/// var result = await provider.EncryptAsync(plaintext, context);
/// var encrypted = result.Match(
///     Right: payload => payload,
///     Left: error => throw new InvalidOperationException(error.Message));
///
/// // Decrypting back
/// var decrypted = await provider.DecryptAsync(encrypted, context);
/// </code>
/// </example>
public interface IMessageEncryptionProvider
{
    /// <summary>
    /// Encrypts a serialized message payload.
    /// </summary>
    /// <param name="plaintext">The plaintext bytes to encrypt (typically UTF-8 encoded JSON).</param>
    /// <param name="context">
    /// The encryption context specifying key selection, tenant isolation, and associated data.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// <c>Right&lt;EncryptedPayload&gt;</c> containing the encrypted payload with metadata on success, or
    /// <c>Left&lt;EncinaError&gt;</c> on failure (e.g., key not found, encryption error).
    /// </returns>
    ValueTask<Either<EncinaError, EncryptedPayload>> EncryptAsync(
        ReadOnlyMemory<byte> plaintext,
        MessageEncryptionContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Decrypts an encrypted message payload back to its original bytes.
    /// </summary>
    /// <param name="payload">The encrypted payload containing ciphertext and cryptographic metadata.</param>
    /// <param name="context">
    /// The encryption context used during encryption (key ID is typically extracted from the payload).
    /// </param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// <c>Right&lt;ImmutableArray&lt;byte&gt;&gt;</c> containing the decrypted plaintext on success, or
    /// <c>Left&lt;EncinaError&gt;</c> on failure (e.g., decryption failed, invalid ciphertext, key not found).
    /// </returns>
    ValueTask<Either<EncinaError, ImmutableArray<byte>>> DecryptAsync(
        EncryptedPayload payload,
        MessageEncryptionContext context,
        CancellationToken cancellationToken = default);
}
