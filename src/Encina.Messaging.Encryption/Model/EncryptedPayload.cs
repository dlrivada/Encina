using System.Collections.Immutable;

namespace Encina.Messaging.Encryption.Model;

/// <summary>
/// Encapsulates an encrypted message payload with its associated cryptographic metadata.
/// </summary>
/// <remarks>
/// <para>
/// This type carries everything needed to decrypt a message payload: the ciphertext, the
/// algorithm used, the key identifier for key lookup, the nonce/IV, and the authentication tag.
/// </para>
/// <para>
/// Unlike <see cref="Security.Encryption.EncryptedValue"/> (which encrypts individual property values),
/// this type encrypts entire serialized message payloads (e.g., the <c>OutboxMessage.Content</c> field).
/// </para>
/// <para>
/// When serialized for storage, the payload is formatted as:
/// <c>ENC:v{Version}:{KeyId}:{Algorithm}:{base64(Nonce)}:{base64(Tag)}:{base64(Ciphertext)}</c>
/// </para>
/// <para>
/// The <see cref="Version"/> field enables forward-compatible format changes without breaking
/// decryption of existing messages.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var payload = new EncryptedPayload
/// {
///     Ciphertext = ciphertextBytes.ToImmutableArray(),
///     KeyId = "msg-key-2024-v1",
///     Algorithm = "AES-256-GCM",
///     Nonce = nonceBytes.ToImmutableArray(),
///     Tag = tagBytes.ToImmutableArray()
/// };
/// </code>
/// </example>
public sealed record EncryptedPayload
{
    /// <summary>
    /// The encrypted ciphertext bytes of the serialized message payload.
    /// </summary>
    public required ImmutableArray<byte> Ciphertext { get; init; }

    /// <summary>
    /// The identifier of the encryption key used to produce this ciphertext.
    /// </summary>
    /// <remarks>
    /// Used during decryption to retrieve the correct key from
    /// <see cref="Security.Encryption.Abstractions.IKeyProvider"/>.
    /// Enables key rotation by tracking which key version was used.
    /// </remarks>
    public required string KeyId { get; init; }

    /// <summary>
    /// The encryption algorithm identifier used to produce this ciphertext.
    /// </summary>
    /// <remarks>
    /// Stored as a string (e.g., <c>"AES-256-GCM"</c>) to allow future algorithm additions
    /// without breaking serialized payload compatibility.
    /// </remarks>
    public required string Algorithm { get; init; }

    /// <summary>
    /// The nonce (number used once) or initialization vector used during encryption.
    /// </summary>
    /// <remarks>
    /// Must be unique per encryption operation with the same key.
    /// For AES-256-GCM, this is typically 12 bytes (96 bits).
    /// </remarks>
    public required ImmutableArray<byte> Nonce { get; init; }

    /// <summary>
    /// The authentication tag produced by authenticated encryption algorithms.
    /// </summary>
    /// <remarks>
    /// For AES-256-GCM, this is typically 16 bytes (128 bits).
    /// Used to verify ciphertext integrity during decryption.
    /// Empty for non-AEAD algorithms.
    /// </remarks>
    public ImmutableArray<byte> Tag { get; init; } = [];

    /// <summary>
    /// The encrypted payload format version.
    /// </summary>
    /// <remarks>
    /// Defaults to <c>1</c>. Incremented when the serialization format changes
    /// to enable forward-compatible deserialization of older payloads.
    /// </remarks>
    public int Version { get; init; } = 1;
}
