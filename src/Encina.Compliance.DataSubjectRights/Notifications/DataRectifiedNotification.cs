namespace Encina.Compliance.DataSubjectRights;

/// <summary>
/// Notification published when personal data has been rectified for a data subject.
/// </summary>
/// <remarks>
/// <para>
/// Published after a successful rectification operation (Article 16). Per Article 19,
/// the controller must communicate the rectification to each recipient to whom the
/// personal data has been disclosed.
/// </para>
/// <para>
/// Handlers implementing <c>INotificationHandler&lt;DataRectifiedNotification&gt;</c> can
/// subscribe to propagate the correction to third-party systems and downstream databases.
/// </para>
/// </remarks>
/// <param name="SubjectId">Identifier of the data subject whose data was rectified.</param>
/// <param name="FieldName">The name of the field that was rectified.</param>
/// <param name="DSRRequestId">Identifier of the DSR request that triggered the rectification.</param>
/// <param name="OccurredAtUtc">Timestamp when the rectification was performed (UTC).</param>
public sealed record DataRectifiedNotification(
    string SubjectId,
    string FieldName,
    string DSRRequestId,
    DateTimeOffset OccurredAtUtc) : INotification;
