namespace Encina.Compliance.Anonymization.Model;

/// <summary>
/// Represents the persistent mapping between a token and its original sensitive value.
/// </summary>
/// <remarks>
/// <para>
/// Token mappings are stored in an <c>ITokenMappingStore</c> to enable reversible tokenization
/// (detokenization). The original value is encrypted at rest using the key identified by
/// <see cref="KeyId"/>, and a hash of the original value (<see cref="OriginalValueHash"/>)
/// enables deduplication — the same input value always maps to the same token.
/// </para>
/// <para>
/// When a key is rotated, all token mappings referencing the old key should be re-encrypted
/// with the new key. The <see cref="ExpiresAtUtc"/> field enables automatic cleanup of
/// expired mappings.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var mapping = TokenMapping.Create(
///     token: "tok_a1b2c3d4-e5f6-7890-abcd-ef1234567890",
///     originalValueHash: "hmac_sha256_base64_hash",
///     encryptedOriginalValue: encryptedBytes,
///     keyId: "key-2025-01");
/// </code>
/// </example>
public sealed record TokenMapping
{
    /// <summary>
    /// Unique identifier for this token mapping record.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The generated token that replaces the original sensitive value.
    /// </summary>
    /// <remarks>
    /// Tokens are unique within the store. The format depends on the
    /// <see cref="TokenFormat"/> used during generation.
    /// </remarks>
    public required string Token { get; init; }

    /// <summary>
    /// HMAC-SHA256 hash of the original value, used for deduplication.
    /// </summary>
    /// <remarks>
    /// When tokenizing a value that has already been tokenized, the store is queried
    /// by this hash to return the existing token instead of creating a duplicate.
    /// The hash is computed using the active key to prevent rainbow table attacks.
    /// </remarks>
    public required string OriginalValueHash { get; init; }

    /// <summary>
    /// The original sensitive value, encrypted with the key identified by <see cref="KeyId"/>.
    /// </summary>
    /// <remarks>
    /// Encrypted using AES-256-GCM for authenticated encryption at rest.
    /// Decryption requires the corresponding key from the <c>IKeyProvider</c>.
    /// </remarks>
    public required byte[] EncryptedOriginalValue { get; init; }

    /// <summary>
    /// Identifier of the cryptographic key used to encrypt <see cref="EncryptedOriginalValue"/>
    /// and compute <see cref="OriginalValueHash"/>.
    /// </summary>
    /// <remarks>
    /// References a key managed by the <c>IKeyProvider</c>. When this key is rotated,
    /// the mapping must be re-encrypted with the new key.
    /// </remarks>
    public required string KeyId { get; init; }

    /// <summary>
    /// Timestamp when this token mapping was created (UTC).
    /// </summary>
    public required DateTimeOffset CreatedAtUtc { get; init; }

    /// <summary>
    /// Optional expiration timestamp for this token mapping (UTC).
    /// </summary>
    /// <remarks>
    /// When set, the mapping is considered expired after this time and may be cleaned up
    /// by maintenance processes. <c>null</c> indicates the mapping does not expire.
    /// </remarks>
    public DateTimeOffset? ExpiresAtUtc { get; init; }

    /// <summary>
    /// Creates a new token mapping with a generated unique identifier and the current UTC timestamp.
    /// </summary>
    /// <param name="token">The generated token value.</param>
    /// <param name="originalValueHash">HMAC-SHA256 hash of the original value.</param>
    /// <param name="encryptedOriginalValue">The AES-256-GCM encrypted original value.</param>
    /// <param name="keyId">Identifier of the encryption key.</param>
    /// <param name="expiresAtUtc">Optional expiration timestamp.</param>
    /// <returns>A new <see cref="TokenMapping"/> with a generated GUID identifier.</returns>
    public static TokenMapping Create(
        string token,
        string originalValueHash,
        byte[] encryptedOriginalValue,
        string keyId,
        DateTimeOffset? expiresAtUtc = null) =>
        new()
        {
            Id = Guid.NewGuid().ToString("N"),
            Token = token,
            OriginalValueHash = originalValueHash,
            EncryptedOriginalValue = encryptedOriginalValue,
            KeyId = keyId,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            ExpiresAtUtc = expiresAtUtc
        };
}
