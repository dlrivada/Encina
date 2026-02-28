namespace Encina.Compliance.Anonymization.Model;

/// <summary>
/// Cryptographic algorithms available for pseudonymization operations.
/// </summary>
/// <remarks>
/// <para>
/// Pseudonymization replaces identifying data with a pseudonym that can be reversed
/// only with access to the corresponding cryptographic key. Per GDPR Article 4(5),
/// pseudonymized data remains personal data because re-identification is possible.
/// </para>
/// <para>
/// The choice of algorithm determines whether the pseudonymization is deterministic
/// (same input always produces same output, enabling search) or randomized
/// (each operation produces a different output, providing stronger security).
/// </para>
/// <para>
/// Follows EDPB Guidelines 01/2025 recommendations for strong cryptographic
/// pseudonymization using well-established algorithms.
/// </para>
/// </remarks>
public enum PseudonymizationAlgorithm
{
    /// <summary>
    /// AES-256-GCM authenticated encryption — randomized, reversible pseudonymization.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Provides IND-CCA2 security with authenticated encryption. Each pseudonymization
    /// operation generates a unique nonce, so the same plaintext produces different
    /// ciphertexts. This is the recommended algorithm for maximum security.
    /// </para>
    /// <para>
    /// Supports full reversibility: pseudonymized data can be depseudonymized
    /// back to the original value using the same key.
    /// </para>
    /// <para>
    /// <b>Trade-off:</b> Non-deterministic output means you cannot search for a specific
    /// pseudonymized value without decrypting all records first.
    /// </para>
    /// </remarks>
    Aes256Gcm = 0,

    /// <summary>
    /// HMAC-SHA256 keyed hash — deterministic, non-reversible pseudonymization.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Produces a deterministic pseudonym: the same input and key always yield the same
    /// output. This enables searching and joining on pseudonymized values without decryption.
    /// </para>
    /// <para>
    /// <b>Trade-off:</b> HMAC is a one-way function — pseudonymized values cannot be reversed
    /// to the original. Use this when searchability is required and irreversibility is acceptable.
    /// </para>
    /// <para>
    /// The pseudonym is Base64-encoded for safe storage and transport.
    /// </para>
    /// </remarks>
    HmacSha256 = 1
}
