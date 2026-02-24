namespace Encina.Compliance.GDPR;

/// <summary>
/// Represents the outcome of a Legitimate Interest Assessment (LIA) under Article 6(1)(f).
/// </summary>
/// <remarks>
/// <para>
/// A LIA follows the EDPB three-part test (purpose, necessity, and balancing) to determine
/// whether the controller's legitimate interests are overridden by the data subject's
/// fundamental rights and freedoms.
/// </para>
/// <para>
/// The outcome governs whether processing under legitimate interests can proceed:
/// </para>
/// <list type="bullet">
/// <item><see cref="Approved"/>: the LIA has been reviewed and processing is permitted</item>
/// <item><see cref="Rejected"/>: the balancing test failed; processing is not permitted</item>
/// <item><see cref="RequiresReview"/>: the LIA is pending review by the DPO or relevant authority</item>
/// </list>
/// </remarks>
public enum LIAOutcome
{
    /// <summary>
    /// The LIA has been reviewed and approved. Processing under legitimate interests is permitted.
    /// </summary>
    Approved = 0,

    /// <summary>
    /// The LIA balancing test determined that the data subject's rights override the legitimate interest.
    /// Processing is not permitted under this basis.
    /// </summary>
    Rejected = 1,

    /// <summary>
    /// The LIA has not yet been reviewed or requires additional review before a determination can be made.
    /// Processing should not proceed until the review is complete.
    /// </summary>
    RequiresReview = 2
}
