namespace Encina.Marten.GDPR;

/// <summary>
/// Provides a summary of a data subject's encryption state within the crypto-shredding system.
/// </summary>
/// <remarks>
/// <para>
/// This record aggregates key lifecycle information for a data subject, including the current
/// compliance status and key version history. It is returned by
/// <c>ISubjectKeyProvider.GetSubjectInfoAsync</c> to support audit reporting and
/// compliance dashboards.
/// </para>
/// <para>
/// When <see cref="Status"/> is <see cref="SubjectStatus.Forgotten"/>, the subject's
/// PII in the event store is permanently unreadable, satisfying GDPR Article 17.
/// </para>
/// </remarks>
public sealed record SubjectEncryptionInfo
{
    /// <summary>
    /// Identifier of the data subject.
    /// </summary>
    public required string SubjectId { get; init; }

    /// <summary>
    /// Current GDPR compliance status of the data subject.
    /// </summary>
    public required SubjectStatus Status { get; init; }

    /// <summary>
    /// Version number of the currently active encryption key, or 0 if the subject has been forgotten.
    /// </summary>
    public required int ActiveKeyVersion { get; init; }

    /// <summary>
    /// Total number of key versions created for this subject (including rotated and deleted versions).
    /// </summary>
    public required int TotalKeyVersions { get; init; }

    /// <summary>
    /// Timestamp when the first encryption key was created for this subject (UTC).
    /// </summary>
    public required DateTimeOffset CreatedAtUtc { get; init; }

    /// <summary>
    /// Timestamp when the subject was cryptographically forgotten (UTC), or <c>null</c>
    /// if the subject has not been forgotten.
    /// </summary>
    public DateTimeOffset? ForgottenAtUtc { get; init; }
}
