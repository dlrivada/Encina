namespace Encina.Audit.Marten.Crypto;

/// <summary>
/// The reference to a data-erasure (right-to-erasure) request that a key destruction answers.
/// </summary>
/// <param name="RequestId">The identifier of the erasure request.</param>
/// <param name="RequestedAtUtc">When the erasure request was made (UTC).</param>
/// <param name="Actor">Who requested or approved the erasure (user, role or system identifier).</param>
/// <remarks>
/// <para>
/// <see cref="ITemporalKeyProvider.DestroyKeysBeforeAsync"/> records this reference in the
/// <see cref="TemporalKeyDestroyedMarker"/> of every period it destroys. The audit projections show the
/// shredded placeholder (<c>[SHREDDED]</c>) only for a period whose marker carries this reference. A retention
/// expiry carries a <see cref="RetentionReference"/> instead and reads <c>[EXPIRED]</c>. A destruction with neither
/// (an error or tampering) is not a lawful erasure: the projection fails with
/// <see cref="Events.EncryptedFieldDecryptionException"/> and Marten's async daemon pauses the shard until an
/// operator resolves it (issue #2770, ADR-034).
/// </para>
/// <para>
/// Retention jobs pass a <see cref="RetentionReference"/> instead (the field then reads <c>[EXPIRED]</c> and the
/// shard keeps running). The GDPR erasure flow calls <c>DestroyKeysBeforeAsync</c> with an erasure reference.
/// </para>
/// </remarks>
public sealed record ErasureReference(string RequestId, DateTimeOffset RequestedAtUtc, string Actor)
    : KeyDestructionReference
{
    /// <summary>
    /// Gets a value indicating whether the reference identifies a request: a non-blank request id and actor,
    /// and a request date.
    /// </summary>
    public override bool IsValid =>
        !string.IsNullOrWhiteSpace(RequestId)
        && !string.IsNullOrWhiteSpace(Actor)
        && RequestedAtUtc != default;
}
