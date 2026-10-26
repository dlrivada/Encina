namespace Encina.Audit.Marten.Crypto;

/// <summary>
/// Marten document that records when a temporal period's encryption keys have been destroyed
/// via crypto-shredding.
/// </summary>
/// <remarks>
/// <para>
/// When <c>MartenTemporalKeyProvider.DestroyKeysBeforeAsync</c> hard-deletes all key documents
/// for a time period, this marker is stored to distinguish "destroyed" from "never existed".
/// Without this marker, a destroyed period would appear as a new period and a new key would
/// be erroneously created on the next <c>GetOrCreateKeyAsync</c> call.
/// </para>
/// <para>
/// The document ID follows the convention <c>"temporal-destroyed:{period}"</c>.
/// </para>
/// </remarks>
public sealed class TemporalKeyDestroyedMarker
{
    /// <summary>
    /// Unique document identifier following the convention <c>"temporal-destroyed:{period}"</c>.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The time period whose keys were destroyed.
    /// </summary>
    public string Period { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the period's keys were destroyed (UTC).
    /// </summary>
    public DateTimeOffset DestroyedAtUtc { get; set; }

    /// <summary>
    /// Number of encryption key versions that were deleted during the destruction operation.
    /// </summary>
    public int KeyVersionsDestroyed { get; set; }

    /// <summary>
    /// The identifier of the data-erasure request this destruction answers, or <c>null</c> when the keys were
    /// destroyed without an erasure reference (retention expiry, error or tampering).
    /// </summary>
    public string? ErasureRequestId { get; set; }

    /// <summary>
    /// When the data-erasure request was made (UTC), or <c>null</c> without an erasure reference.
    /// </summary>
    public DateTimeOffset? ErasureRequestedAtUtc { get; set; }

    /// <summary>
    /// Who requested or approved the erasure, or <c>null</c> without an erasure reference.
    /// </summary>
    public string? ErasureActor { get; set; }

    /// <summary>
    /// Returns whether this marker records an erasure request (request id, date and actor all present).
    /// Only then do the audit projections show the shredded placeholder; otherwise they fail with
    /// <see cref="Events.EncryptedFieldDecryptionException"/>.
    /// </summary>
    /// <returns><c>true</c> when the marker carries a complete erasure reference.</returns>
    public bool HasErasureReference() =>
        !string.IsNullOrWhiteSpace(ErasureRequestId)
        && !string.IsNullOrWhiteSpace(ErasureActor)
        && ErasureRequestedAtUtc is { } requestedAt
        && requestedAt != default;
}
