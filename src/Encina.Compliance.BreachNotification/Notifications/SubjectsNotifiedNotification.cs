namespace Encina.Compliance.BreachNotification;

/// <summary>
/// Notification published when data subjects have been notified about a personal
/// data breach that is likely to result in a high risk to their rights and freedoms.
/// </summary>
/// <remarks>
/// <para>
/// Published after <c>IBreachNotifier.NotifyDataSubjectsAsync</c> successfully delivers
/// the breach notification to affected data subjects per GDPR Article 34(1).
/// </para>
/// <para>
/// Per Art. 34(1), the controller must communicate the personal data breach to the
/// data subject "without undue delay" when the breach "is likely to result in a high
/// risk to the rights and freedoms of natural persons."
/// </para>
/// <para>
/// Handlers implementing <c>INotificationHandler&lt;SubjectsNotifiedNotification&gt;</c>
/// can use this to update breach records, log the notification for audit purposes, or
/// trigger follow-up communication workflows.
/// </para>
/// </remarks>
/// <param name="BreachId">Identifier of the breach that was communicated.</param>
/// <param name="NotifiedAtUtc">Timestamp when data subjects were notified (UTC).</param>
/// <param name="SubjectCount">Number of data subjects that were notified.</param>
public sealed record SubjectsNotifiedNotification(
    string BreachId,
    DateTimeOffset NotifiedAtUtc,
    int SubjectCount) : INotification;
