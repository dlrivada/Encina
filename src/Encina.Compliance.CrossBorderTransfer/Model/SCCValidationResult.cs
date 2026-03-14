namespace Encina.Compliance.CrossBorderTransfer.Model;

/// <summary>
/// Represents the result of validating an SCC agreement for a specific processor and module combination.
/// </summary>
/// <remarks>
/// <para>
/// Used by <c>ISCCService.ValidateAgreementAsync</c> to report whether a valid SCC agreement
/// exists for a given processor, including any issues or missing supplementary measures
/// that may need to be addressed before transfers can proceed.
/// </para>
/// </remarks>
public sealed record SCCValidationResult
{
    /// <summary>
    /// Indicates whether a valid, non-expired, non-revoked SCC agreement exists.
    /// </summary>
    public required bool IsValid { get; init; }

    /// <summary>
    /// Identifier of the matching SCC agreement, if found.
    /// </summary>
    /// <remarks>
    /// <c>null</c> if no matching agreement was found.
    /// </remarks>
    public Guid? AgreementId { get; init; }

    /// <summary>
    /// The SCC module of the matching agreement, if found.
    /// </summary>
    public SCCModule? Module { get; init; }

    /// <summary>
    /// Version of the SCC clauses in the matching agreement, if found.
    /// </summary>
    public string? Version { get; init; }

    /// <summary>
    /// Supplementary measures that are required but not yet implemented.
    /// </summary>
    /// <remarks>
    /// Per Schrems II, supplementary measures may be needed to ensure "essentially equivalent"
    /// protection. This list identifies measures that are pending implementation.
    /// </remarks>
    public required IReadOnlyList<string> MissingMeasures { get; init; }

    /// <summary>
    /// Validation issues found during the assessment.
    /// </summary>
    /// <remarks>
    /// May include warnings such as upcoming expiration, outdated SCC version,
    /// or missing supplementary measures.
    /// </remarks>
    public required IReadOnlyList<string> Issues { get; init; }
}
