namespace Encina.Compliance.DataSubjectRights;

/// <summary>
/// Exemptions to the right to erasure as defined in GDPR Article 17(3).
/// </summary>
/// <remarks>
/// <para>
/// Even when a valid erasure request is received, the controller may retain personal data
/// if one of these exemptions applies. The applicable exemptions must be documented
/// and communicated to the data subject.
/// </para>
/// </remarks>
public enum ErasureExemption
{
    /// <summary>
    /// Processing is necessary for exercising the right of freedom of expression and information.
    /// </summary>
    /// <remarks>Article 17(3)(a).</remarks>
    FreedomOfExpression,

    /// <summary>
    /// Processing is necessary for compliance with a legal obligation which requires processing
    /// by Union or Member State law, or for the performance of a task carried out in the
    /// public interest or in the exercise of official authority.
    /// </summary>
    /// <remarks>Article 17(3)(b).</remarks>
    LegalObligation,

    /// <summary>
    /// Processing is necessary for reasons of public interest in the area of public health
    /// in accordance with Articles 9(2)(h) and (i) as well as Article 9(3).
    /// </summary>
    /// <remarks>Article 17(3)(c).</remarks>
    PublicHealth,

    /// <summary>
    /// Processing is necessary for archiving purposes in the public interest, scientific
    /// or historical research purposes, or statistical purposes in accordance with Article 89(1).
    /// </summary>
    /// <remarks>Article 17(3)(d). Erasure must not render impossible or seriously impair the achievement of the objectives.</remarks>
    Archiving,

    /// <summary>
    /// Processing is necessary for the establishment, exercise, or defence of legal claims.
    /// </summary>
    /// <remarks>Article 17(3)(e).</remarks>
    LegalClaims
}
