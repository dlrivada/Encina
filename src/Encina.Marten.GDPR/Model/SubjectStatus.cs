namespace Encina.Marten.GDPR;

/// <summary>
/// Represents the GDPR compliance status of a data subject within the crypto-shredding system.
/// </summary>
/// <remarks>
/// <para>
/// This status tracks whether a subject's PII can still be accessed or has been
/// cryptographically shredded. The transition from <see cref="Active"/> to <see cref="Forgotten"/>
/// is irreversible — once a subject's encryption keys are deleted, their PII in the event store
/// becomes permanently unreadable.
/// </para>
/// <para>
/// The <see cref="PendingDeletion"/> state enables asynchronous processing of
/// right-to-erasure requests (GDPR Article 17), supporting the 30-day response SLA
/// defined in Article 12(3).
/// </para>
/// </remarks>
public enum SubjectStatus
{
    /// <summary>
    /// The subject's PII is actively encrypted and accessible.
    /// Encryption keys are available for both encryption and decryption.
    /// </summary>
    Active,

    /// <summary>
    /// The subject has been cryptographically forgotten.
    /// All encryption keys have been permanently deleted, rendering PII in the event store
    /// unreadable. This state is irreversible and satisfies GDPR Article 17.
    /// </summary>
    Forgotten,

    /// <summary>
    /// The subject's erasure request has been received and is being processed.
    /// This transitional state supports asynchronous processing of right-to-erasure
    /// requests within the 30-day SLA (GDPR Article 12(3)).
    /// </summary>
    PendingDeletion
}
