namespace Encina.Marten.GDPR;

/// <summary>
/// Represents the lifecycle status of a subject's encryption key used in crypto-shredding.
/// </summary>
/// <remarks>
/// <para>
/// Each data subject has one or more encryption keys versioned over time.
/// When a key is rotated, the previous version transitions to <see cref="Rotated"/>
/// while remaining available for decrypting old events. When a subject exercises their
/// right to be forgotten (GDPR Article 17), all key versions transition to <see cref="Deleted"/>,
/// rendering all encrypted PII permanently unreadable.
/// </para>
/// </remarks>
public enum SubjectKeyStatus
{
    /// <summary>
    /// The key is active and used for encrypting new events.
    /// Only one key version per subject can be active at any time.
    /// </summary>
    Active,

    /// <summary>
    /// The key has been rotated and replaced by a newer version.
    /// Rotated keys remain available for decrypting events encrypted with this version
    /// but are never used for new encryptions.
    /// </summary>
    Rotated,

    /// <summary>
    /// The key has been permanently deleted as part of a crypto-shredding operation.
    /// Events encrypted with this key are permanently unreadable (GDPR Article 17 compliance).
    /// </summary>
    Deleted
}
