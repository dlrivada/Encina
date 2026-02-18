namespace Encina.Compliance.GDPR;

/// <summary>
/// Represents the result of a GDPR compliance validation check.
/// </summary>
/// <remarks>
/// <para>
/// A compliance result indicates whether a request meets GDPR requirements.
/// Results can be compliant (with optional warnings) or non-compliant (with errors).
/// </para>
/// <para>
/// Warnings indicate potential issues that do not block processing but should be reviewed,
/// while errors indicate violations that must be resolved.
/// </para>
/// </remarks>
public sealed record ComplianceResult
{
    private ComplianceResult(
        bool isCompliant,
        IReadOnlyList<string> errors,
        IReadOnlyList<string> warnings)
    {
        IsCompliant = isCompliant;
        Errors = errors;
        Warnings = warnings;
    }

    /// <summary>
    /// Whether the request is GDPR compliant.
    /// </summary>
    public bool IsCompliant { get; }

    /// <summary>
    /// Compliance errors that must be resolved before processing can proceed.
    /// </summary>
    /// <remarks>
    /// Empty when <see cref="IsCompliant"/> is <c>true</c>.
    /// </remarks>
    public IReadOnlyList<string> Errors { get; }

    /// <summary>
    /// Compliance warnings that do not block processing but should be reviewed.
    /// </summary>
    /// <remarks>
    /// Warnings may be present even when <see cref="IsCompliant"/> is <c>true</c>.
    /// Examples: retention period approaching limit, missing optional safeguards documentation.
    /// </remarks>
    public IReadOnlyList<string> Warnings { get; }

    /// <summary>
    /// Creates a compliant result with no errors or warnings.
    /// </summary>
    /// <returns>A compliant <see cref="ComplianceResult"/>.</returns>
    public static ComplianceResult Compliant() =>
        new(true, [], []);

    /// <summary>
    /// Creates a compliant result with warnings that should be reviewed.
    /// </summary>
    /// <param name="warnings">One or more compliance warnings.</param>
    /// <returns>A compliant <see cref="ComplianceResult"/> with warnings.</returns>
    public static ComplianceResult CompliantWithWarnings(params string[] warnings) =>
        new(true, [], warnings);

    /// <summary>
    /// Creates a non-compliant result with one or more errors.
    /// </summary>
    /// <param name="errors">One or more compliance errors.</param>
    /// <returns>A non-compliant <see cref="ComplianceResult"/>.</returns>
    public static ComplianceResult NonCompliant(params string[] errors) =>
        new(false, errors, []);

    /// <summary>
    /// Creates a non-compliant result with errors and warnings.
    /// </summary>
    /// <param name="errors">One or more compliance errors.</param>
    /// <param name="warnings">One or more compliance warnings.</param>
    /// <returns>A non-compliant <see cref="ComplianceResult"/> with warnings.</returns>
    public static ComplianceResult NonCompliant(IReadOnlyList<string> errors, IReadOnlyList<string> warnings) =>
        new(false, errors, warnings);
}
