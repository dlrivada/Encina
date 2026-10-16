namespace Encina.Compliance.DataSubjectRights;

/// <summary>
/// Standardised reason code for the refusal of a data subject request (GDPR Article 12(4) and 12(5)).
/// </summary>
/// <remarks>
/// <para>
/// The code is recorded next to the free-text explanation the controller gives the data subject,
/// so reports, dashboards and audits can aggregate refusals by ground. The controller always bears
/// the burden of demonstrating the ground (Article 12(5), last sentence).
/// </para>
/// <para>
/// Every member has an explicit, stable value and no member is <c>0</c>, so an uninitialised value
/// is never a valid reason. The same codes are reused by the per-category erasure refusal
/// (issue #1188) so that a whole-request refusal and a per-category refusal share one vocabulary.
/// </para>
/// </remarks>
public enum DSRRejectionReason
{
    /// <summary>
    /// The request is manifestly unfounded.
    /// </summary>
    /// <remarks>GDPR Article 12(5). The controller must demonstrate the manifestly unfounded character.</remarks>
    ManifestlyUnfounded = 1,

    /// <summary>
    /// The request is excessive, in particular because of its repetitive character.
    /// </summary>
    /// <remarks>
    /// GDPR Article 12(5). Encina only records the code: the controller judges repetition from the
    /// subject's request history and bears the burden of demonstration.
    /// </remarks>
    Excessive = 2,

    /// <summary>
    /// The controller demonstrates that it is not in a position to identify the data subject.
    /// </summary>
    /// <remarks>
    /// GDPR Article 12(2) with Article 11(2). Asking for additional identity information
    /// (Article 12(6)) precedes this refusal and is not itself a refusal ground.
    /// </remarks>
    IdentityNotVerifiable = 3,

    /// <summary>
    /// The right is restricted by Union or Member State law.
    /// </summary>
    /// <remarks>GDPR Article 23.</remarks>
    RestrictedByLaw = 4,

    /// <summary>
    /// The conditions of the exercised right are not met.
    /// </summary>
    /// <remarks>
    /// For example GDPR Article 17(3) (erasure exemptions), Article 20(1) (portability conditions)
    /// or Article 21(1) (objection grounds). Per-category detail belongs to issue #1188.
    /// </remarks>
    RightNotApplicable = 5,

    /// <summary>
    /// A ground that none of the other codes describes.
    /// </summary>
    /// <remarks>The explanation is still mandatory (GDPR Article 12(4)).</remarks>
    Other = 99,

    /// <summary>
    /// Draft law: access request used for purposes other than data protection ("abusive intent").
    /// </summary>
    /// <remarks>
    /// COM(2025) 837 (Digital Omnibus), draft amendment of GDPR Article 12(5). Off by default: the
    /// service refuses it with <c>dsr.rejection_reason_not_enabled</c> until
    /// <see cref="DataSubjectRightsOptions.EnableOmnibusArticle12RefusalGrounds"/> is set.
    /// </remarks>
    AbusiveIntent = 100
}
