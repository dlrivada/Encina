namespace Encina.Compliance.DPIA.Model;

/// <summary>
/// Decision outcome from the Data Protection Officer's consultation on a DPIA.
/// </summary>
/// <remarks>
/// <para>
/// GDPR Article 35(2) requires the controller to seek the advice of the Data Protection Officer
/// when carrying out a Data Protection Impact Assessment. This enumeration captures the DPO's
/// formal response to the assessment.
/// </para>
/// <para>
/// The DPO's role (Article 39(1)(c)) includes providing advice regarding the impact assessment
/// and monitoring its performance. The DPO must be involved "in a timely manner" in all issues
/// relating to the protection of personal data (Article 38(1)).
/// </para>
/// </remarks>
public enum DPOConsultationDecision
{
    /// <summary>
    /// The DPO has not yet responded to the consultation request.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// The DPO approves the assessment and the processing may proceed.
    /// </summary>
    Approved = 1,

    /// <summary>
    /// The DPO rejects the assessment; the processing must not proceed without changes.
    /// </summary>
    Rejected = 2,

    /// <summary>
    /// The DPO approves the assessment subject to specific conditions being met.
    /// </summary>
    /// <remarks>
    /// The conditions are recorded in <see cref="DPOConsultation.Conditions"/>.
    /// Processing may proceed only after all conditions are satisfied.
    /// </remarks>
    ConditionallyApproved = 3
}
