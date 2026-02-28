using Encina.Compliance.Anonymization.Model;
using MongoDB.Bson.Serialization.Attributes;

namespace Encina.MongoDB.Anonymization;

/// <summary>
/// MongoDB document representation of a <see cref="TokenMapping"/>.
/// </summary>
/// <remarks>
/// <para>
/// Uses BSON attributes for MongoDB-specific serialization. All property names use
/// snake_case to follow MongoDB naming conventions.
/// </para>
/// <para>
/// DateTimeOffset values are stored as <see cref="DateTime"/> with <see cref="DateTimeKind.Utc"/>
/// since MongoDB's BSON DateTime type represents UTC timestamps.
/// </para>
/// </remarks>
public sealed class TokenMappingDocument
{
    /// <summary>
    /// Unique identifier for this token mapping record.
    /// </summary>
    [BsonId]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The generated token that replaces the original sensitive value.
    /// </summary>
    [BsonElement("token")]
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// HMAC-SHA256 hash of the original value, used for deduplication lookups.
    /// </summary>
    [BsonElement("original_value_hash")]
    public string OriginalValueHash { get; set; } = string.Empty;

    /// <summary>
    /// The original sensitive value, encrypted with AES-256-GCM.
    /// </summary>
    [BsonElement("encrypted_original_value")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "MongoDB BSON document requires byte[] for binary data storage")]
    public byte[] EncryptedOriginalValue { get; set; } = [];

    /// <summary>
    /// Identifier of the cryptographic key used for encryption.
    /// </summary>
    [BsonElement("key_id")]
    public string KeyId { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when this token mapping was created (UTC).
    /// </summary>
    [BsonElement("created_at_utc")]
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Optional expiration timestamp for this token mapping (UTC).
    /// </summary>
    [BsonElement("expires_at_utc")]
    [BsonIgnoreIfNull]
    public DateTime? ExpiresAtUtc { get; set; }

    /// <summary>
    /// Converts a domain <see cref="TokenMapping"/> to a MongoDB document.
    /// </summary>
    /// <param name="mapping">The domain mapping to convert.</param>
    /// <returns>A <see cref="TokenMappingDocument"/> suitable for MongoDB persistence.</returns>
    public static TokenMappingDocument FromDomain(TokenMapping mapping)
    {
        ArgumentNullException.ThrowIfNull(mapping);

        return new TokenMappingDocument
        {
            Id = mapping.Id,
            Token = mapping.Token,
            OriginalValueHash = mapping.OriginalValueHash,
            EncryptedOriginalValue = mapping.EncryptedOriginalValue,
            KeyId = mapping.KeyId,
            CreatedAtUtc = mapping.CreatedAtUtc.UtcDateTime,
            ExpiresAtUtc = mapping.ExpiresAtUtc?.UtcDateTime
        };
    }

    /// <summary>
    /// Converts this MongoDB document back to a domain <see cref="TokenMapping"/>.
    /// </summary>
    /// <returns>A <see cref="TokenMapping"/> domain record.</returns>
    public TokenMapping ToDomain()
    {
        return new TokenMapping
        {
            Id = Id,
            Token = Token,
            OriginalValueHash = OriginalValueHash,
            EncryptedOriginalValue = EncryptedOriginalValue,
            KeyId = KeyId,
            CreatedAtUtc = new DateTimeOffset(CreatedAtUtc, TimeSpan.Zero),
            ExpiresAtUtc = ExpiresAtUtc.HasValue
                ? new DateTimeOffset(ExpiresAtUtc.Value, TimeSpan.Zero)
                : null
        };
    }
}
