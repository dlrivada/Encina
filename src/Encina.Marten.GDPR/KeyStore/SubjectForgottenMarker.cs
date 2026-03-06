namespace Encina.Marten.GDPR;

/// <summary>
/// Marten document that records when a data subject has been cryptographically forgotten.
/// </summary>
/// <remarks>
/// <para>
/// When <see cref="PostgreSqlSubjectKeyProvider.DeleteSubjectKeysAsync"/> hard-deletes all
/// key documents for a subject, this marker is stored to distinguish "forgotten" from
/// "never existed". Without this marker, a forgotten subject would appear as a new subject
/// and a new key would be erroneously created on the next <c>GetOrCreateSubjectKeyAsync</c> call.
/// </para>
/// <para>
/// The document ID follows the convention <c>"forgotten:{subjectId}"</c>.
/// </para>
/// </remarks>
internal sealed class SubjectForgottenMarker
{
    /// <summary>
    /// Unique document identifier following the convention <c>"forgotten:{subjectId}"</c>.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Identifier of the data subject who was forgotten.
    /// </summary>
    public string SubjectId { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the subject was cryptographically forgotten (UTC).
    /// </summary>
    public DateTimeOffset ForgottenAtUtc { get; set; }

    /// <summary>
    /// Number of encryption key versions that were deleted during the forgetting operation.
    /// </summary>
    public int KeysDeleted { get; set; }
}
