namespace Encina.Compliance.GDPR;

/// <summary>
/// Represents the result of a lawful basis validation check for a request type.
/// </summary>
/// <remarks>
/// <para>
/// A validation result indicates whether a request has a valid lawful basis declared
/// and whether any basis-specific requirements are satisfied (e.g., LIA for legitimate interests,
/// active consent for consent-based processing).
/// </para>
/// <para>
/// Warnings indicate potential issues that do not block processing but should be reviewed,
/// while errors indicate violations that must be resolved.
/// </para>
/// </remarks>
public sealed record LawfulBasisValidationResult
{
    private LawfulBasisValidationResult(
        bool isValid,
        LawfulBasis? basis,
        IReadOnlyList<string> errors,
        IReadOnlyList<string> warnings)
    {
        IsValid = isValid;
        Basis = basis;
        Errors = errors;
        Warnings = warnings;
    }

    /// <summary>
    /// Whether the request has a valid lawful basis.
    /// </summary>
    public bool IsValid { get; }

    /// <summary>
    /// The lawful basis that was validated, if found.
    /// </summary>
    public LawfulBasis? Basis { get; }

    /// <summary>
    /// Validation errors that must be resolved before processing can proceed.
    /// </summary>
    /// <remarks>
    /// Empty when <see cref="IsValid"/> is <c>true</c>.
    /// </remarks>
    public IReadOnlyList<string> Errors { get; }

    /// <summary>
    /// Validation warnings that do not block processing but should be reviewed.
    /// </summary>
    /// <remarks>
    /// Warnings may be present even when <see cref="IsValid"/> is <c>true</c>.
    /// Examples: missing optional LIA reference, approaching consent expiration.
    /// </remarks>
    public IReadOnlyList<string> Warnings { get; }

    /// <summary>
    /// Creates a valid result with the confirmed lawful basis.
    /// </summary>
    /// <param name="basis">The validated lawful basis.</param>
    /// <returns>A valid <see cref="LawfulBasisValidationResult"/>.</returns>
    public static LawfulBasisValidationResult Valid(LawfulBasis basis) =>
        new(true, basis, [], []);

    /// <summary>
    /// Creates a valid result with warnings that should be reviewed.
    /// </summary>
    /// <param name="basis">The validated lawful basis.</param>
    /// <param name="warnings">One or more validation warnings.</param>
    /// <returns>A valid <see cref="LawfulBasisValidationResult"/> with warnings.</returns>
    public static LawfulBasisValidationResult ValidWithWarnings(LawfulBasis basis, params string[] warnings) =>
        new(true, basis, [], warnings);

    /// <summary>
    /// Creates an invalid result indicating no lawful basis was found.
    /// </summary>
    /// <param name="errors">One or more validation errors.</param>
    /// <returns>An invalid <see cref="LawfulBasisValidationResult"/>.</returns>
    public static LawfulBasisValidationResult Invalid(params string[] errors) =>
        new(false, null, errors, []);

    /// <summary>
    /// Creates an invalid result with errors and warnings.
    /// </summary>
    /// <param name="basis">The lawful basis that was found but failed validation.</param>
    /// <param name="errors">One or more validation errors.</param>
    /// <param name="warnings">One or more validation warnings.</param>
    /// <returns>An invalid <see cref="LawfulBasisValidationResult"/> with warnings.</returns>
    public static LawfulBasisValidationResult Invalid(LawfulBasis basis, IReadOnlyList<string> errors, IReadOnlyList<string> warnings) =>
        new(false, basis, errors, warnings);
}
