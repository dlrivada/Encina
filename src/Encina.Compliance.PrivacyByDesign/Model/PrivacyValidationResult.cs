namespace Encina.Compliance.PrivacyByDesign.Model;

/// <summary>
/// The aggregate result of all Privacy by Design validations for a single request.
/// </summary>
/// <remarks>
/// <para>
/// Combines the results of data minimization, purpose limitation, and default privacy checks
/// into a single result record. The pipeline behavior uses this to decide whether to block,
/// warn, or allow the request based on the configured <see cref="PrivacyByDesignEnforcementMode"/>.
/// </para>
/// <para>
/// Per GDPR Article 25(1), measures must be designed to implement data-protection principles
/// "in an effective manner." This result provides the evidence trail for demonstrating compliance.
/// </para>
/// </remarks>
public sealed record PrivacyValidationResult
{
    /// <summary>
    /// The name of the request type that was validated.
    /// </summary>
    public required string RequestTypeName { get; init; }

    /// <summary>
    /// The list of violations detected during validation.
    /// </summary>
    /// <remarks>
    /// An empty list indicates full compliance with all Privacy by Design principles.
    /// </remarks>
    public required IReadOnlyList<PrivacyViolation> Violations { get; init; }

    /// <summary>
    /// The data minimization report for the request, if minimization analysis was performed.
    /// </summary>
    public MinimizationReport? MinimizationReport { get; init; }

    /// <summary>
    /// The purpose validation result, if purpose limitation was checked.
    /// </summary>
    public PurposeValidationResult? PurposeValidation { get; init; }

    /// <summary>
    /// Whether the validation passed with no violations.
    /// </summary>
    /// <remarks>
    /// Equivalent to <c>Violations.Count == 0</c>.
    /// </remarks>
    public bool IsCompliant => Violations.Count == 0;

    /// <summary>
    /// The UTC timestamp when this validation was performed.
    /// </summary>
    public required DateTimeOffset ValidatedAtUtc { get; init; }

    /// <summary>
    /// The tenant identifier for multi-tenancy support, or <see langword="null"/> when tenancy is not used.
    /// </summary>
    public string? TenantId { get; init; }

    /// <summary>
    /// The module identifier for modular monolith isolation, or <see langword="null"/>
    /// when module isolation is not used.
    /// </summary>
    public string? ModuleId { get; init; }
}
