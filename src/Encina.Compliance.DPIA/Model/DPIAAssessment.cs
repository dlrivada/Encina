namespace Encina.Compliance.DPIA.Model;

/// <summary>
/// Represents a Data Protection Impact Assessment for a specific processing operation.
/// </summary>
/// <remarks>
/// <para>
/// GDPR Article 35(1) requires the controller to carry out an assessment of the impact
/// of the envisaged processing operations on the protection of personal data "where a type
/// of processing [...] is likely to result in a high risk to the rights and freedoms of
/// natural persons."
/// </para>
/// <para>
/// This record tracks the full assessment lifecycle:
/// </para>
/// <list type="number">
/// <item><description><b>Creation</b>: Assessment initiated for a specific <see cref="RequestTypeName"/>.</description></item>
/// <item><description><b>Evaluation</b>: Risk analysis produces a <see cref="Result"/>.</description></item>
/// <item><description><b>DPO Consultation</b>: The DPO reviews and provides a <see cref="DPOConsultation"/> (Article 35(2)).</description></item>
/// <item><description><b>Approval/Rejection</b>: Status transitions based on risk and DPO decision.</description></item>
/// <item><description><b>Review</b>: Periodic re-evaluation per <see cref="NextReviewAtUtc"/> (Article 35(11)).</description></item>
/// <item><description><b>Expiration</b>: Approved assessments expire when their review date passes.</description></item>
/// </list>
/// <para>
/// The <see cref="IsCurrent"/> method provides a quick check for the pipeline behavior
/// to determine if the processing operation has a valid, unexpired approval.
/// </para>
/// </remarks>
public sealed record DPIAAssessment
{
    /// <summary>
    /// Unique identifier for this assessment.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// The fully-qualified type name of the request (command/query) this assessment covers.
    /// </summary>
    /// <remarks>
    /// Used as the lookup key by the pipeline behavior. The type name is stored as a string
    /// to support persistence and cross-assembly resolution.
    /// </remarks>
    public required string RequestTypeName { get; init; }

    /// <summary>
    /// The CLR <see cref="Type"/> of the request, if available at runtime.
    /// </summary>
    /// <remarks>
    /// This property is populated when the assessment is loaded in-process but is not persisted.
    /// It is <see langword="null"/> when the assessment is deserialized from storage.
    /// </remarks>
    public Type? RequestType { get; init; }

    /// <summary>
    /// The current lifecycle status of this assessment.
    /// </summary>
    public required DPIAAssessmentStatus Status { get; init; }

    /// <summary>
    /// The assessment result containing risk analysis and mitigations.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> when the assessment is in <see cref="DPIAAssessmentStatus.Draft"/>
    /// status and has not yet been evaluated.
    /// </remarks>
    public DPIAResult? Result { get; init; }

    /// <summary>
    /// The DPO consultation record for this assessment.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> when DPO consultation has not yet been initiated.
    /// Per Article 35(2), the controller must seek the DPO's advice when carrying out a DPIA.
    /// </remarks>
    public DPOConsultation? DPOConsultation { get; init; }

    /// <summary>
    /// The type of processing covered by this assessment (e.g., "AutomatedDecisionMaking").
    /// </summary>
    public string? ProcessingType { get; init; }

    /// <summary>
    /// The reason or justification for conducting this assessment.
    /// </summary>
    public string? Reason { get; init; }

    /// <summary>
    /// The UTC timestamp when this assessment was created.
    /// </summary>
    public required DateTimeOffset CreatedAtUtc { get; init; }

    /// <summary>
    /// The UTC timestamp when this assessment was approved, or <see langword="null"/> if not yet approved.
    /// </summary>
    public DateTimeOffset? ApprovedAtUtc { get; init; }

    /// <summary>
    /// The UTC timestamp for the next scheduled review of this assessment.
    /// </summary>
    /// <remarks>
    /// Per Article 35(11), the controller must carry out a review "at least when there is
    /// a change of the risk represented by processing operations." When this date passes,
    /// the assessment transitions to <see cref="DPIAAssessmentStatus.Expired"/>.
    /// </remarks>
    public DateTimeOffset? NextReviewAtUtc { get; init; }

    /// <summary>
    /// The tenant identifier for multi-tenancy support, or <see langword="null"/> when tenancy is not used.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When multi-tenancy is enabled, assessments are scoped to a specific tenant.
    /// The pipeline behavior populates this from <see cref="IRequestContext.TenantId"/>,
    /// and management endpoints resolve it from the HTTP request context.
    /// </para>
    /// <para>
    /// This is a soft dependency: DPIA works identically with or without multi-tenancy.
    /// </para>
    /// </remarks>
    public string? TenantId { get; init; }

    /// <summary>
    /// The module identifier for modular monolith isolation, or <see langword="null"/> when module isolation is not used.
    /// </summary>
    /// <remarks>
    /// <para>
    /// In modular monolith architectures, assessments can be scoped to a specific module.
    /// This ensures that DPIA assessments for the same request type in different modules
    /// are tracked independently.
    /// </para>
    /// <para>
    /// This is a soft dependency: DPIA works identically with or without module isolation.
    /// </para>
    /// </remarks>
    public string? ModuleId { get; init; }

    /// <summary>
    /// The chronological audit trail of all actions taken on this assessment.
    /// </summary>
    /// <remarks>
    /// Supports the accountability principle (Article 5(2)) by providing a complete
    /// record of the assessment lifecycle.
    /// </remarks>
    public IReadOnlyList<DPIAAuditEntry> AuditTrail { get; init; } = [];

    /// <summary>
    /// Determines whether this assessment is currently valid for allowing processing to proceed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An assessment is current when:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Its <see cref="Status"/> is <see cref="DPIAAssessmentStatus.Approved"/>.</description></item>
    /// <item><description>Its <see cref="NextReviewAtUtc"/> has not passed (or is <see langword="null"/>, indicating no scheduled review).</description></item>
    /// </list>
    /// <para>
    /// The pipeline behavior calls this method to determine whether to allow, warn, or block
    /// the processing operation based on the configured <see cref="DPIAEnforcementMode"/>.
    /// </para>
    /// </remarks>
    /// <param name="nowUtc">The current UTC time for comparison against <see cref="NextReviewAtUtc"/>.</param>
    /// <returns><see langword="true"/> if the assessment is approved and has not expired; otherwise, <see langword="false"/>.</returns>
    public bool IsCurrent(DateTimeOffset nowUtc) =>
        Status == DPIAAssessmentStatus.Approved &&
        (NextReviewAtUtc is null || NextReviewAtUtc > nowUtc);
}
