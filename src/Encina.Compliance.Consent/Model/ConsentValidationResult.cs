namespace Encina.Compliance.Consent;

/// <summary>
/// Represents the result of a consent validation check against required processing purposes.
/// </summary>
/// <remarks>
/// <para>
/// A consent validation result indicates whether a data subject has given valid consent
/// for all required processing purposes. Results can be valid (all purposes consented)
/// or invalid (one or more purposes missing or with expired/withdrawn consent).
/// </para>
/// <para>
/// Warnings indicate potential issues that do not block processing but should be reviewed,
/// such as consent approaching expiration or a consent version that is about to change.
/// Errors indicate violations that must be resolved before processing can proceed.
/// </para>
/// <para>
/// This type follows the same factory method pattern as
/// <c>Encina.Compliance.GDPR.ComplianceResult</c> for consistency
/// across compliance modules.
/// </para>
/// </remarks>
public sealed record ConsentValidationResult
{
    private ConsentValidationResult(
        bool isValid,
        IReadOnlyList<string> errors,
        IReadOnlyList<string> warnings,
        IReadOnlyList<string> missingPurposes)
    {
        IsValid = isValid;
        Errors = errors;
        Warnings = warnings;
        MissingPurposes = missingPurposes;
    }

    /// <summary>
    /// Whether valid consent exists for all required processing purposes.
    /// </summary>
    public bool IsValid { get; }

    /// <summary>
    /// Consent validation errors that must be resolved before processing can proceed.
    /// </summary>
    /// <remarks>
    /// Empty when <see cref="IsValid"/> is <c>true</c>.
    /// </remarks>
    public IReadOnlyList<string> Errors { get; }

    /// <summary>
    /// Consent validation warnings that do not block processing but should be reviewed.
    /// </summary>
    /// <remarks>
    /// Warnings may be present even when <see cref="IsValid"/> is <c>true</c>.
    /// Examples: consent expiring soon, consent version approaching end-of-life.
    /// </remarks>
    public IReadOnlyList<string> Warnings { get; }

    /// <summary>
    /// Processing purposes for which valid consent is missing.
    /// </summary>
    /// <remarks>
    /// Contains the purpose identifiers (e.g., "marketing", "analytics") that the data subject
    /// has not consented to, or for which consent has been withdrawn or expired.
    /// Empty when <see cref="IsValid"/> is <c>true</c>.
    /// </remarks>
    public IReadOnlyList<string> MissingPurposes { get; }

    /// <summary>
    /// Creates a valid result indicating all required consents are present and active.
    /// </summary>
    /// <returns>A valid <see cref="ConsentValidationResult"/>.</returns>
    public static ConsentValidationResult Valid() =>
        new(true, [], [], []);

    /// <summary>
    /// Creates a valid result with warnings that should be reviewed.
    /// </summary>
    /// <param name="warnings">One or more consent validation warnings.</param>
    /// <returns>A valid <see cref="ConsentValidationResult"/> with warnings.</returns>
    public static ConsentValidationResult ValidWithWarnings(params string[] warnings) =>
        new(true, [], warnings, []);

    /// <summary>
    /// Creates an invalid result with one or more errors and missing purposes.
    /// </summary>
    /// <param name="errors">One or more consent validation errors.</param>
    /// <param name="missingPurposes">Processing purposes for which valid consent is missing.</param>
    /// <returns>An invalid <see cref="ConsentValidationResult"/>.</returns>
    public static ConsentValidationResult Invalid(
        IReadOnlyList<string> errors,
        IReadOnlyList<string> missingPurposes) =>
        new(false, errors, [], missingPurposes);

    /// <summary>
    /// Creates an invalid result with errors, warnings, and missing purposes.
    /// </summary>
    /// <param name="errors">One or more consent validation errors.</param>
    /// <param name="warnings">One or more consent validation warnings.</param>
    /// <param name="missingPurposes">Processing purposes for which valid consent is missing.</param>
    /// <returns>An invalid <see cref="ConsentValidationResult"/> with warnings.</returns>
    public static ConsentValidationResult Invalid(
        IReadOnlyList<string> errors,
        IReadOnlyList<string> warnings,
        IReadOnlyList<string> missingPurposes) =>
        new(false, errors, warnings, missingPurposes);
}
