namespace Encina.Compliance.DataSubjectRights;

/// <summary>
/// Grounds for erasure of personal data as defined in GDPR Article 17(1)(a-f).
/// </summary>
/// <remarks>
/// <para>
/// The data subject has the right to obtain erasure of personal data without undue delay
/// when one of these grounds applies. The controller must assess the applicable ground
/// and verify that no exemptions under Article 17(3) prevent the erasure.
/// </para>
/// </remarks>
public enum ErasureReason
{
    /// <summary>
    /// Personal data is no longer necessary in relation to the purposes for which it was
    /// collected or otherwise processed.
    /// </summary>
    /// <remarks>Article 17(1)(a).</remarks>
    NoLongerNecessary,

    /// <summary>
    /// The data subject withdraws consent on which the processing is based and there is
    /// no other legal ground for the processing.
    /// </summary>
    /// <remarks>Article 17(1)(b). Applies when processing was based on Article 6(1)(a) or Article 9(2)(a).</remarks>
    ConsentWithdrawn,

    /// <summary>
    /// The data subject objects to the processing pursuant to Article 21(1) and there are
    /// no overriding legitimate grounds, or the data subject objects to direct marketing
    /// processing pursuant to Article 21(2).
    /// </summary>
    /// <remarks>Article 17(1)(c).</remarks>
    ObjectionToProcessing,

    /// <summary>
    /// The personal data has been unlawfully processed.
    /// </summary>
    /// <remarks>Article 17(1)(d).</remarks>
    UnlawfulProcessing,

    /// <summary>
    /// The personal data has to be erased for compliance with a legal obligation in Union
    /// or Member State law to which the controller is subject.
    /// </summary>
    /// <remarks>Article 17(1)(e).</remarks>
    LegalObligation,

    /// <summary>
    /// The personal data was collected in relation to the offer of information society
    /// services to a child referred to in Article 8(1).
    /// </summary>
    /// <remarks>Article 17(1)(f). Special protection for children's data.</remarks>
    ChildData
}
