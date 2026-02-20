using System.Collections.Immutable;

namespace Encina.Security.Encryption;

/// <summary>
/// Encapsulates an encrypted value with its associated cryptographic metadata.
/// </summary>
/// <remarks>
/// <para>
/// This type carries everything needed to decrypt the value: the ciphertext, the algorithm
/// used, the key identifier for key lookup, and the nonce/IV used during encryption.
/// </para>
/// <para>
/// Designed as a <c>readonly record struct</c> for zero-allocation storage in hot paths.
/// All properties are immutable; use <c>with</c> expressions to create modified copies.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var encrypted = new EncryptedValue
/// {
///     Ciphertext = ciphertextBytes,
///     Algorithm = EncryptionAlgorithm.Aes256Gcm,
///     KeyId = "key-2024-01",
///     Nonce = nonceBytes
/// };
/// </code>
/// </example>
public readonly record struct EncryptedValue()
{
    /// <summary>
    /// The encrypted ciphertext bytes.
    /// </summary>
    public required ImmutableArray<byte> Ciphertext { get; init; }

    /// <summary>
    /// The encryption algorithm used to produce this ciphertext.
    /// </summary>
    public required EncryptionAlgorithm Algorithm { get; init; }

    /// <summary>
    /// The identifier of the key used for encryption.
    /// </summary>
    /// <remarks>
    /// Used during decryption to retrieve the correct key from <see cref="Abstractions.IKeyProvider"/>.
    /// Enables key rotation by tracking which key version was used.
    /// </remarks>
    public required string KeyId { get; init; }

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
}
