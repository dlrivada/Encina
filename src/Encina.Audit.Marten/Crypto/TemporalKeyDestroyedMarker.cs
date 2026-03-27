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
}
