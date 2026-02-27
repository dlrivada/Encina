namespace Encina.Compliance.DataSubjectRights;

/// <summary>
/// Notification published when personal data has been erased for a data subject.
/// </summary>
/// <remarks>
/// <para>
/// Published after a successful erasure operation (Article 17). Per Article 19,
/// the controller must communicate the erasure to each recipient to whom the personal
/// data has been disclosed, unless this proves impossible or involves disproportionate effort.
/// </para>
/// <para>
/// Handlers implementing <c>INotificationHandler&lt;DataErasedNotification&gt;</c> can
/// subscribe to this notification to propagate erasure to third-party systems,
/// downstream databases, or external processors.
/// </para>
/// </remarks>
/// <param name="SubjectId">Identifier of the data subject whose data was erased.</param>
/// <param name="AffectedFields">Names of the fields that were erased.</param>
/// <param name="DSRRequestId">Identifier of the DSR request that triggered the erasure.</param>
/// <param name="OccurredAtUtc">Timestamp when the erasure was performed (UTC).</param>
public sealed record DataErasedNotification(
    string SubjectId,
    IReadOnlyList<string> AffectedFields,
    string DSRRequestId,
    DateTimeOffset OccurredAtUtc) : INotification;
