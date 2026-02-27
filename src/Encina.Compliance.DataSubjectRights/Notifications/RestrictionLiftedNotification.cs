namespace Encina.Compliance.DataSubjectRights;

/// <summary>
/// Notification published when a processing restriction has been lifted for a data subject.
/// </summary>
/// <remarks>
/// <para>
/// Published when a previously restricted data subject's processing is unblocked.
/// Per Article 18(3), the controller must inform the data subject before the restriction
/// is lifted.
/// </para>
/// <para>
/// Handlers implementing <c>INotificationHandler&lt;RestrictionLiftedNotification&gt;</c>
/// can subscribe to resume processing operations and notify third-party recipients.
/// </para>
/// </remarks>
/// <param name="SubjectId">Identifier of the data subject whose restriction was lifted.</param>
/// <param name="DSRRequestId">Identifier of the DSR request associated with the restriction.</param>
/// <param name="OccurredAtUtc">Timestamp when the restriction was lifted (UTC).</param>
public sealed record RestrictionLiftedNotification(
    string SubjectId,
    string DSRRequestId,
    DateTimeOffset OccurredAtUtc) : INotification;
