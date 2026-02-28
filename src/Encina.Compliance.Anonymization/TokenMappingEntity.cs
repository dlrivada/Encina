namespace Encina.Compliance.Anonymization;

/// <summary>
/// Persistence entity for <see cref="Model.TokenMapping"/>.
/// </summary>
/// <remarks>
/// <para>
/// This entity provides a database-agnostic representation of a token mapping,
/// using primitive types suitable for any storage provider (ADO.NET, Dapper, EF Core, MongoDB).
/// </para>
/// <para>
/// All properties map directly to <see cref="Model.TokenMapping"/> without type transformations,
/// since the domain model already uses primitive types (strings, byte arrays, DateTimeOffset).
/// </para>
/// <para>
/// Use <see cref="TokenMappingMapper"/> to convert between this entity and
/// <see cref="Model.TokenMapping"/>.
/// </para>
/// </remarks>
public sealed class TokenMappingEntity
{
    /// <summary>
    /// Unique identifier for this token mapping record.
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// The generated token that replaces the original sensitive value.
    /// </summary>
    /// <remarks>
    /// Must be unique within the store. A UNIQUE index should be created on this column.
    /// </remarks>
    public required string Token { get; set; }

    /// <summary>
    /// HMAC-SHA256 hash of the original value, used for deduplication lookups.
    /// </summary>
    /// <remarks>
    /// An INDEX should be created on this column for efficient deduplication queries.
    /// </remarks>
    public required string OriginalValueHash { get; set; }

    /// <summary>
    /// The original sensitive value, encrypted with AES-256-GCM using the key
    /// identified by <see cref="KeyId"/>.
    /// </summary>
    public required byte[] EncryptedOriginalValue { get; set; }

    /// <summary>
    /// Identifier of the cryptographic key used to encrypt
    /// <see cref="EncryptedOriginalValue"/> and compute <see cref="OriginalValueHash"/>.
    /// </summary>
    public required string KeyId { get; set; }

    /// <summary>
    /// Timestamp when this token mapping was created (UTC).
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>
    /// Optional expiration timestamp for this token mapping (UTC).
    /// </summary>
    /// <remarks>
    /// <c>null</c> indicates the mapping does not expire.
    /// </remarks>
    public DateTimeOffset? ExpiresAtUtc { get; set; }
}
