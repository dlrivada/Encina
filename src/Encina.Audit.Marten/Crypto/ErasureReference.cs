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
/// shredded placeholder (<c>[SHREDDED]</c>) only for a period whose marker carries a reference. A destruction
/// without one (retention-period expiry, an error or tampering) is not a lawful erasure: the projection fails
/// with <see cref="Events.EncryptedFieldDecryptionException"/> and Marten's async daemon pauses the shard until an
/// operator resolves it (issue #2770, ADR-034).
/// </para>
/// <para>
/// Retention jobs call <c>DestroyKeysBeforeAsync</c> without a reference. The GDPR erasure flow calls it with one.
/// </para>
/// </remarks>
public sealed record ErasureReference(string RequestId, DateTimeOffset RequestedAtUtc, string Actor)
{
    /// <summary>
    /// Gets a value indicating whether the reference identifies a request: a non-blank request id and actor,
    /// and a request date.
    /// </summary>
    public bool IsValid =>
        !string.IsNullOrWhiteSpace(RequestId)
        && !string.IsNullOrWhiteSpace(Actor)
        && RequestedAtUtc != default;
}
