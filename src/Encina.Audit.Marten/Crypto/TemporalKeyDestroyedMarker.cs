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
    /// The identifier of the retention policy whose execution destroyed the keys, or <c>null</c> without a
    /// retention reference.
    /// </summary>
    public string? RetentionPolicyId { get; set; }

    /// <summary>
    /// The cutoff the retention policy applied (UTC), or <c>null</c> without a retention reference.
    /// </summary>
    public DateTimeOffset? RetentionCutoffUtc { get; set; }

    /// <summary>
    /// When the retention execution ran (UTC), or <c>null</c> without a retention reference.
    /// </summary>
    public DateTimeOffset? RetentionExecutedAtUtc { get; set; }

    /// <summary>
    /// Who executed the retention, or <c>null</c> without a retention reference.
    /// </summary>
    public string? RetentionActor { get; set; }

    /// <summary>
    /// Returns whether this marker records an erasure request (request id, date and actor all present).
    /// Only then do the audit projections show the shredded placeholder.
    /// </summary>
    /// <returns><c>true</c> when the marker carries a complete erasure reference.</returns>
    public bool HasErasureReference() =>
        !string.IsNullOrWhiteSpace(ErasureRequestId)
        && !string.IsNullOrWhiteSpace(ErasureActor)
        && ErasureRequestedAtUtc is { } requestedAt
        && requestedAt != default;

    /// <summary>
    /// Returns whether this marker records a retention execution (policy id, cutoff, execution date and actor all
    /// present). Only then do the audit projections show the expired placeholder and keep running.
    /// </summary>
    /// <returns><c>true</c> when the marker carries a complete retention reference.</returns>
    public bool HasRetentionReference() =>
        !string.IsNullOrWhiteSpace(RetentionPolicyId)
        && !string.IsNullOrWhiteSpace(RetentionActor)
        && RetentionCutoffUtc is { } cutoff
        && cutoff != default
        && RetentionExecutedAtUtc is { } executedAt
        && executedAt != default;

    /// <summary>
    /// Classifies how the keys of this period were destroyed from the references the marker carries.
    /// </summary>
    /// <returns>
    /// <see cref="KeyDestructionKind.Erasure"/> with only a complete erasure reference,
    /// <see cref="KeyDestructionKind.Retention"/> with only a complete retention reference, and
    /// <see cref="KeyDestructionKind.Unrecorded"/> with neither or with both (a marker never carries both unless it
    /// was damaged or tampered with, so it is not trusted as either).
    /// </returns>
    public KeyDestructionKind Classify() => (HasErasureReference(), HasRetentionReference()) switch
    {
        (true, false) => KeyDestructionKind.Erasure,
        (false, true) => KeyDestructionKind.Retention,
        _ => KeyDestructionKind.Unrecorded
    };
}

/// <summary>
/// How the keys of a temporal period were destroyed, as recorded in its <see cref="TemporalKeyDestroyedMarker"/>.
/// </summary>
public enum KeyDestructionKind
{
    /// <summary>The marker carries neither a complete erasure nor a complete retention reference, or carries both.</summary>
    Unrecorded = 0,

    /// <summary>The destruction answers a recorded data-erasure request (fields read as the shredded placeholder).</summary>
    Erasure = 1,

    /// <summary>The destruction answers a recorded retention execution (fields read as the expired placeholder).</summary>
    Retention = 2,
}
