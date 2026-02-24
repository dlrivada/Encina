namespace Encina.Compliance.GDPR;

/// <summary>
/// Represents the result of validating a Legitimate Interest Assessment (LIA).
/// </summary>
/// <remarks>
/// <para>
/// This result is returned by <see cref="ILegitimateInterestAssessment.ValidateAsync"/>
/// and indicates whether a LIA exists, is approved, and can support processing under
/// <see cref="LawfulBasis.LegitimateInterests"/>.
/// </para>
/// </remarks>
public sealed record LIAValidationResult
{
    private LIAValidationResult(
        bool isValid,
        LIAOutcome? outcome,
        string? rejectionReason,
        bool requiresReview)
    {
        IsValid = isValid;
        Outcome = outcome;
        RejectionReason = rejectionReason;
        RequiresReview = requiresReview;
    }

    /// <summary>
    /// Whether the LIA is valid and supports processing under legitimate interests.
    /// </summary>
    /// <remarks>
    /// <c>true</c> only when the LIA exists and its <see cref="Outcome"/> is <see cref="LIAOutcome.Approved"/>.
    /// </remarks>
    public bool IsValid { get; }

    /// <summary>
    /// The outcome of the LIA, if the LIA was found.
    /// </summary>
    public LIAOutcome? Outcome { get; }

    /// <summary>
    /// The reason for rejection, if the LIA was rejected.
    /// </summary>
    public string? RejectionReason { get; }

    /// <summary>
    /// Whether the LIA requires review before processing can proceed.
    /// </summary>
    public bool RequiresReview { get; }

    /// <summary>
    /// Creates a valid result indicating the LIA is approved.
    /// </summary>
    /// <returns>A valid <see cref="LIAValidationResult"/>.</returns>
    public static LIAValidationResult Approved() =>
        new(true, LIAOutcome.Approved, null, false);

    /// <summary>
    /// Creates an invalid result indicating the LIA was rejected.
    /// </summary>
    /// <param name="reason">The reason for rejection.</param>
    /// <returns>An invalid <see cref="LIAValidationResult"/>.</returns>
    public static LIAValidationResult Rejected(string? reason = null) =>
        new(false, LIAOutcome.Rejected, reason, false);

    /// <summary>
    /// Creates an invalid result indicating the LIA requires review.
    /// </summary>
    /// <returns>An invalid <see cref="LIAValidationResult"/> with <see cref="RequiresReview"/> set to <c>true</c>.</returns>
    public static LIAValidationResult PendingReview() =>
        new(false, LIAOutcome.RequiresReview, null, true);

    /// <summary>
    /// Creates an invalid result indicating no LIA was found for the given reference.
    /// </summary>
    /// <returns>An invalid <see cref="LIAValidationResult"/> with no outcome.</returns>
    public static LIAValidationResult NotFound() =>
        new(false, null, null, false);
}
