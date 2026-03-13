namespace Encina.Compliance.ProcessorAgreements.Model;

/// <summary>
/// The result of validating a processor's Data Processing Agreement compliance.
/// </summary>
/// <remarks>
/// <para>
/// Produced by the <c>IDPAValidator</c> when validating a processor's agreement status.
/// Captures both overall validity and granular term-level compliance per Article 28(3).
/// </para>
/// <para>
/// The <see cref="DPAId"/> is nullable because a processor may not have any agreement
/// on file, in which case validation still produces a result indicating non-compliance.
/// </para>
/// </remarks>
public sealed record DPAValidationResult
{
    /// <summary>
    /// The identifier of the processor that was validated.
    /// </summary>
    public required string ProcessorId { get; init; }

    /// <summary>
    /// The identifier of the DPA that was validated, or <see langword="null"/> if no agreement exists.
    /// </summary>
    public string? DPAId { get; init; }

    /// <summary>
    /// Whether the processor has a valid, active, and fully compliant Data Processing Agreement.
    /// </summary>
    public required bool IsValid { get; init; }

    /// <summary>
    /// The current status of the validated agreement, or <see langword="null"/> if no agreement exists.
    /// </summary>
    public DPAStatus? Status { get; init; }

    /// <summary>
    /// The list of mandatory terms from Article 28(3)(a)-(h) that are missing from the agreement.
    /// </summary>
    /// <remarks>
    /// Empty when all mandatory terms are present or when no agreement exists
    /// (in which case <see cref="IsValid"/> is <see langword="false"/>).
    /// </remarks>
    public required IReadOnlyList<string> MissingTerms { get; init; }

    /// <summary>
    /// Advisory warnings about the agreement (e.g., approaching expiration, missing SCCs for cross-border transfer).
    /// </summary>
    public required IReadOnlyList<string> Warnings { get; init; }

    /// <summary>
    /// The number of days until the agreement expires, or <see langword="null"/> if the agreement
    /// has no expiration date or does not exist.
    /// </summary>
    public int? DaysUntilExpiration { get; init; }

    /// <summary>
    /// The UTC timestamp when this validation was performed.
    /// </summary>
    public required DateTimeOffset ValidatedAtUtc { get; init; }
}
