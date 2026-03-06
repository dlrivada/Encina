namespace Encina.Marten.GDPR;

/// <summary>
/// Contains the outcome of a crypto-shredding operation (subject forgetting).
/// </summary>
/// <remarks>
/// <para>
/// A crypto-shredding operation permanently deletes all encryption keys for a data subject,
/// rendering their PII in the event store unreadable. This is the recommended approach for
/// implementing GDPR Article 17 ("Right to be Forgotten") in event-sourced systems,
/// as it preserves event stream integrity while ensuring data erasure.
/// </para>
/// <para>
/// This result is returned by <c>ISubjectKeyProvider.DeleteSubjectKeysAsync</c> and
/// published via <see cref="SubjectForgottenEvent"/> for downstream processing.
/// </para>
/// </remarks>
public sealed record CryptoShreddingResult
{
    /// <summary>
    /// Identifier of the data subject whose keys were deleted.
    /// </summary>
    public required string SubjectId { get; init; }

    /// <summary>
    /// Number of encryption key versions that were permanently deleted.
    /// </summary>
    public required int KeysDeleted { get; init; }

    /// <summary>
    /// Number of PII fields across the event store that are now permanently unreadable.
    /// </summary>
    public required int FieldsAffected { get; init; }

    /// <summary>
    /// Timestamp when the crypto-shredding operation was completed (UTC).
    /// </summary>
    public required DateTimeOffset ShreddedAtUtc { get; init; }
}
