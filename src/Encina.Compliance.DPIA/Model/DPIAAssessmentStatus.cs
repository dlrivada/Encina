namespace Encina.Compliance.DPIA.Model;

/// <summary>
/// Lifecycle status of a Data Protection Impact Assessment.
/// </summary>
/// <remarks>
/// <para>
/// Tracks the assessment through its complete lifecycle as required by GDPR Article 35(11),
/// which mandates that assessments be reviewed "at least when there is a change of the risk
/// represented by processing operations."
/// </para>
/// <para>
/// The typical lifecycle flow is:
/// <c>Draft → InReview → Approved → Expired</c> (happy path), or
/// <c>Draft → InReview → Rejected → Draft</c> (revision cycle), or
/// <c>Draft → InReview → RequiresRevision → InReview</c> (conditional approval cycle).
/// </para>
/// </remarks>
public enum DPIAAssessmentStatus
{
    /// <summary>
    /// Assessment is being prepared and has not yet been submitted for review.
    /// </summary>
    Draft = 0,

    /// <summary>
    /// Assessment has been submitted and is awaiting DPO or stakeholder review.
    /// </summary>
    /// <remarks>
    /// Per Article 35(2), the controller must seek the advice of the Data Protection Officer
    /// when carrying out an impact assessment.
    /// </remarks>
    InReview = 1,

    /// <summary>
    /// Assessment has been approved and the processing operation may proceed.
    /// </summary>
    /// <remarks>
    /// The assessment remains valid until its scheduled review date (<see cref="DPIAAssessment.NextReviewAtUtc"/>)
    /// or until processing operations change materially.
    /// </remarks>
    Approved = 2,

    /// <summary>
    /// Assessment has been rejected; the processing operation must not proceed.
    /// </summary>
    /// <remarks>
    /// The controller must either redesign the processing to reduce risk
    /// or consult the supervisory authority under Article 36.
    /// </remarks>
    Rejected = 3,

    /// <summary>
    /// Assessment requires revisions before it can be approved.
    /// </summary>
    /// <remarks>
    /// Typically set when the DPO identifies gaps in risk analysis or mitigation measures.
    /// The assessment returns to <see cref="Draft"/> status after revisions are applied.
    /// </remarks>
    RequiresRevision = 4,

    /// <summary>
    /// A previously approved assessment has passed its scheduled review date.
    /// </summary>
    /// <remarks>
    /// Per Article 35(11), the controller must carry out a review when there is a change
    /// in the risk. Expired assessments must be re-evaluated before processing can continue.
    /// </remarks>
    Expired = 5
}
