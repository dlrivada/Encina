namespace Encina.Compliance.DataSubjectRights;

/// <summary>
/// The eight data subject rights under GDPR Articles 15-22.
/// </summary>
/// <remarks>
/// <para>
/// Data subjects have the right to exercise any of these rights by submitting a request
/// to the data controller. The controller must respond within 30 days (Article 12(3)),
/// with the possibility of a 2-month extension for complex or numerous requests.
/// </para>
/// <para>
/// Each right has specific conditions and exemptions defined in the corresponding
/// GDPR article. Not all rights apply in all circumstances.
/// </para>
/// </remarks>
public enum DataSubjectRight
{
    /// <summary>
    /// Right of access — the data subject can obtain confirmation of whether personal data
    /// is being processed and, if so, access to the data and supplementary information.
    /// </summary>
    /// <remarks>Article 15. The controller must provide a copy of the personal data undergoing processing.</remarks>
    Access,

    /// <summary>
    /// Right to rectification — the data subject can have inaccurate personal data corrected
    /// and incomplete data completed.
    /// </summary>
    /// <remarks>Article 16. The controller must rectify without undue delay.</remarks>
    Rectification,

    /// <summary>
    /// Right to erasure ("right to be forgotten") — the data subject can have personal data
    /// erased when certain conditions apply.
    /// </summary>
    /// <remarks>Article 17. Subject to exemptions in Article 17(3) (legal obligations, public health, etc.).</remarks>
    Erasure,

    /// <summary>
    /// Right to restriction of processing — the data subject can restrict the processing of
    /// their personal data in specific circumstances.
    /// </summary>
    /// <remarks>Article 18. Data may be stored but not processed while restriction is active.</remarks>
    Restriction,

    /// <summary>
    /// Right to data portability — the data subject can receive their personal data in a
    /// structured, commonly used, and machine-readable format.
    /// </summary>
    /// <remarks>Article 20. Applies only to data processed by automated means based on consent or contract.</remarks>
    Portability,

    /// <summary>
    /// Right to object — the data subject can object to processing based on legitimate
    /// interests, direct marketing, or research/statistics.
    /// </summary>
    /// <remarks>Article 21. For direct marketing, the right to object is absolute.</remarks>
    Objection,

    /// <summary>
    /// Right not to be subject to automated individual decision-making, including profiling,
    /// which produces legal effects or similarly significant effects.
    /// </summary>
    /// <remarks>Article 22. Exemptions apply for contract performance, legal authorization, or explicit consent.</remarks>
    AutomatedDecisionMaking,

    /// <summary>
    /// Obligation to notify third parties about rectification, erasure, or restriction
    /// of processing.
    /// </summary>
    /// <remarks>Article 19. The controller must communicate actions to each recipient unless impossible or disproportionate.</remarks>
    Notification
}
