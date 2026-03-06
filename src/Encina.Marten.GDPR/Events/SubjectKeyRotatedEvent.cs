namespace Encina.Marten.GDPR;

/// <summary>
/// Domain event published when a data subject's encryption key has been rotated.
/// </summary>
/// <remarks>
/// <para>
/// Key rotation creates a new version of the subject's encryption key. The previous key
/// version transitions to <see cref="SubjectKeyStatus.Rotated"/> and remains available
/// for decrypting events encrypted with that version. New events are encrypted with the
/// latest key version (forward-only encryption).
/// </para>
/// <para>
/// Handlers implementing <c>INotificationHandler&lt;SubjectKeyRotatedEvent&gt;</c> can
/// subscribe to this event for audit logging, compliance reporting, or triggering
/// optional re-encryption workflows.
/// </para>
/// </remarks>
/// <param name="SubjectId">Identifier of the data subject whose key was rotated.</param>
/// <param name="OldKeyId">Key identifier of the previous (now rotated) key version.</param>
/// <param name="NewKeyId">Key identifier of the new active key version.</param>
/// <param name="OccurredAtUtc">Timestamp when the key rotation completed (UTC).</param>
public sealed record SubjectKeyRotatedEvent(
    string SubjectId,
    string OldKeyId,
    string NewKeyId,
    DateTimeOffset OccurredAtUtc) : INotification;
