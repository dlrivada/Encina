namespace Encina.Compliance.BreachNotification.Model;

/// <summary>
/// Exemptions from the obligation to notify data subjects about a breach.
/// </summary>
/// <remarks>
/// <para>
/// Per GDPR Article 34(3), notification to data subjects is not required if any
/// of the following conditions are met. When an exemption applies, the controller
/// must still document the reasons for not notifying subjects (Art. 33(5)).
/// </para>
/// <para>
/// If none of the individual notification exemptions apply but notification would
/// involve "disproportionate effort," the controller must instead make a public
/// communication or similar measure ensuring data subjects are informed equally
/// effectively (Art. 34(3)(c)).
/// </para>
/// </remarks>
public enum SubjectNotificationExemption
{
    /// <summary>
    /// No exemption applies — data subject notification is required.
    /// </summary>
    None = 0,

    /// <summary>
    /// The controller has applied appropriate technical and organisational protection
    /// measures to the affected data, in particular encryption.
    /// </summary>
    /// <remarks>
    /// Per Art. 34(3)(a), if data was encrypted or otherwise rendered unintelligible
    /// to any person not authorised to access it, subject notification is not required.
    /// The encryption must be strong enough that the data is effectively protected.
    /// </remarks>
    EncryptionProtected = 1,

    /// <summary>
    /// The controller has taken subsequent measures ensuring the high risk is no longer
    /// likely to materialise.
    /// </summary>
    /// <remarks>
    /// Per Art. 34(3)(b), if the controller has taken subsequent measures that ensure
    /// that the high risk to rights and freedoms of data subjects is no longer likely
    /// to materialise (e.g., revoking credentials, blocking compromised accounts).
    /// </remarks>
    MitigatingMeasures = 2,

    /// <summary>
    /// Individual notification would involve disproportionate effort.
    /// </summary>
    /// <remarks>
    /// Per Art. 34(3)(c), when individual notification is impractical (e.g., the number
    /// of affected subjects is very large or contact information is not available),
    /// the controller must instead make a public communication or similar measure
    /// whereby the data subjects are informed equally effectively.
    /// </remarks>
    DisproportionateEffort = 3
}
