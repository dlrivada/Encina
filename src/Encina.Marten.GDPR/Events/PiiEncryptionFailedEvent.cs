namespace Encina.Marten.GDPR;

/// <summary>
/// Domain event published when encryption of a PII field fails during event serialization.
/// </summary>
/// <remarks>
/// <para>
/// This event is raised when the crypto-shredding serializer cannot encrypt a property
/// marked with <c>[CryptoShredded]</c>. Possible causes include key provider failures,
/// encryption algorithm errors, or misconfigured attributes.
/// </para>
/// <para>
/// Handlers implementing <c>INotificationHandler&lt;PiiEncryptionFailedEvent&gt;</c> should
/// treat this as a high-severity alert, as unencrypted PII in the event store undermines
/// GDPR compliance. Consider implementing retry logic or circuit-breaker patterns
/// to handle transient key provider failures.
/// </para>
/// </remarks>
/// <param name="SubjectId">Identifier of the data subject whose PII could not be encrypted.</param>
/// <param name="PropertyName">Name of the property that failed to encrypt.</param>
/// <param name="ErrorMessage">Description of the encryption failure.</param>
/// <param name="OccurredAtUtc">Timestamp when the encryption failure occurred (UTC).</param>
public sealed record PiiEncryptionFailedEvent(
    string SubjectId,
    string PropertyName,
    string ErrorMessage,
    DateTimeOffset OccurredAtUtc) : INotification;
