namespace Encina.Marten.GDPR;

/// <summary>
/// Domain event published when a data subject has been cryptographically forgotten.
/// </summary>
/// <remarks>
/// <para>
/// This event is raised after all encryption keys for a data subject have been permanently
/// deleted via crypto-shredding. Once this event is published, the subject's PII in the
/// event store is permanently unreadable, satisfying GDPR Article 17 ("Right to be Forgotten").
/// </para>
/// <para>
/// Handlers implementing <c>INotificationHandler&lt;SubjectForgottenEvent&gt;</c> can
/// subscribe to this event to trigger downstream workflows such as:
/// <list type="bullet">
///   <item><description>Audit logging the erasure operation</description></item>
///   <item><description>Clearing projection caches for the forgotten subject</description></item>
///   <item><description>Notifying the data subject that erasure is complete</description></item>
///   <item><description>Updating compliance dashboards</description></item>
/// </list>
/// </para>
/// </remarks>
/// <param name="SubjectId">Identifier of the data subject who was forgotten.</param>
/// <param name="KeysDeleted">Number of encryption key versions that were permanently deleted.</param>
/// <param name="FieldsAffected">Number of PII fields across the event store that are now permanently unreadable.</param>
/// <param name="OccurredAtUtc">Timestamp when the crypto-shredding operation completed (UTC).</param>
public sealed record SubjectForgottenEvent(
    string SubjectId,
    int KeysDeleted,
    int FieldsAffected,
    DateTimeOffset OccurredAtUtc) : INotification;
