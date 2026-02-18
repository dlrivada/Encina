namespace Encina.Compliance.GDPR;

/// <summary>
/// The six lawful bases for processing personal data under GDPR Article 6(1).
/// </summary>
/// <remarks>
/// <para>
/// At least one lawful basis must apply whenever personal data is processed.
/// The appropriate basis depends on the purpose and relationship with the data subject.
/// </para>
/// <para>
/// Once a lawful basis is determined, it should not be swapped for another.
/// If the original basis no longer applies, processing must cease unless
/// another basis legitimately covers the processing.
/// </para>
/// </remarks>
public enum LawfulBasis
{
    /// <summary>
    /// The data subject has given consent to the processing for one or more specific purposes.
    /// </summary>
    /// <remarks>Article 6(1)(a). Consent must be freely given, specific, informed, and unambiguous.</remarks>
    Consent,

    /// <summary>
    /// Processing is necessary for the performance of a contract with the data subject,
    /// or to take steps at the request of the data subject prior to entering into a contract.
    /// </summary>
    /// <remarks>Article 6(1)(b). Most common basis for e-commerce and service delivery.</remarks>
    Contract,

    /// <summary>
    /// Processing is necessary for compliance with a legal obligation to which the controller is subject.
    /// </summary>
    /// <remarks>Article 6(1)(c). Examples: tax reporting, anti-money laundering, employment law.</remarks>
    LegalObligation,

    /// <summary>
    /// Processing is necessary to protect the vital interests of the data subject or another natural person.
    /// </summary>
    /// <remarks>Article 6(1)(d). Generally limited to life-or-death situations.</remarks>
    VitalInterests,

    /// <summary>
    /// Processing is necessary for the performance of a task carried out in the public interest
    /// or in the exercise of official authority vested in the controller.
    /// </summary>
    /// <remarks>Article 6(1)(e). Typically applies to public authorities and bodies.</remarks>
    PublicTask,

    /// <summary>
    /// Processing is necessary for the purposes of the legitimate interests pursued by the controller
    /// or by a third party, except where overridden by the interests or fundamental rights of the data subject.
    /// </summary>
    /// <remarks>Article 6(1)(f). Requires a balancing test (Legitimate Interest Assessment).</remarks>
    LegitimateInterests
}
