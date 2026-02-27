namespace Encina.Compliance.DataSubjectRights;

/// <summary>
/// Notification published when processing has been restricted for a data subject.
/// </summary>
/// <remarks>
/// <para>
/// Published after a processing restriction is applied (Article 18). Per Article 19,
/// the controller must communicate the restriction to each recipient to whom the
/// personal data has been disclosed.
/// </para>
/// <para>
/// While restriction is active, data may only be stored — not processed — except
/// with consent, for legal claims, for protecting rights, or for important public interest
/// (Article 18(2)).
/// </para>
/// </remarks>
/// <param name="SubjectId">Identifier of the data subject whose processing was restricted.</param>
/// <param name="DSRRequestId">Identifier of the DSR request that triggered the restriction.</param>
/// <param name="OccurredAtUtc">Timestamp when the restriction was applied (UTC).</param>
public sealed record ProcessingRestrictedNotification(
    string SubjectId,
    string DSRRequestId,
    DateTimeOffset OccurredAtUtc) : INotification;
