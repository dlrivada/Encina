namespace Encina.Compliance.CrossBorderTransfer.Model;

/// <summary>
/// Lifecycle status of a Transfer Impact Assessment (TIA) as recommended by the
/// European Data Protection Board (EDPB) Recommendations 01/2020.
/// </summary>
/// <remarks>
/// <para>
/// A Transfer Impact Assessment evaluates whether the legal framework of a third country
/// provides "essentially equivalent" protection to the EU/EEA standard, as required by
/// the Schrems II judgment (CJEU C-311/18). The TIA lifecycle progresses through these
/// statuses as the assessment is drafted, evaluated, reviewed, and completed.
/// </para>
/// <para>
/// The lifecycle is: <see cref="Draft"/> → <see cref="InProgress"/> →
/// <see cref="PendingDPOReview"/> → <see cref="Completed"/> (or back to <see cref="InProgress"/>
/// if rejected). A completed TIA may later transition to <see cref="Expired"/> when the
/// assessment period elapses or the legal landscape changes materially.
/// </para>
/// </remarks>
public enum TIAStatus
{
    /// <summary>
    /// TIA has been created but risk assessment has not yet begun.
    /// </summary>
    /// <remarks>
    /// Initial state when a TIA is opened for a new transfer route (source → destination).
    /// The assessor has not yet evaluated the destination country's legal framework.
    /// </remarks>
    Draft = 0,

    /// <summary>
    /// Risk assessment is underway — the destination country's legal framework is being evaluated.
    /// </summary>
    /// <remarks>
    /// The assessor is analyzing government surveillance laws, data protection authorities,
    /// judicial redress mechanisms, and other factors relevant to the level of protection
    /// in the destination country. Supplementary measures may be identified during this phase.
    /// </remarks>
    InProgress = 1,

    /// <summary>
    /// Assessment is complete and awaiting review by the Data Protection Officer (DPO).
    /// </summary>
    /// <remarks>
    /// The risk assessment findings and recommended supplementary measures have been documented.
    /// The DPO must review and either approve or reject the assessment before the TIA
    /// can be used to authorize transfers.
    /// </remarks>
    PendingDPOReview = 2,

    /// <summary>
    /// TIA has been completed and approved — the assessment can be used to authorize transfers.
    /// </summary>
    /// <remarks>
    /// The DPO has approved the risk assessment and any required supplementary measures.
    /// Transfers to the assessed destination may proceed using the identified legal basis
    /// and supplementary measures. The TIA should be periodically reassessed.
    /// </remarks>
    Completed = 3,

    /// <summary>
    /// TIA has expired and is no longer valid for authorizing transfers.
    /// </summary>
    /// <remarks>
    /// A TIA expires when its validity period elapses, or when material changes in the
    /// destination country's legal framework (e.g., new surveillance legislation, court rulings)
    /// invalidate the previous assessment. A new TIA must be conducted before transfers
    /// can resume.
    /// </remarks>
    Expired = 4
}
