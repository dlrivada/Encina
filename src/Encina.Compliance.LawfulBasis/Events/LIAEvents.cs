using Encina.Compliance.GDPR;

// Event-sourced events implement INotification so they are automatically published
// by Encina.Marten's EventPublishingPipelineBehavior after successful command execution.
// This allows handlers to react to aggregate state changes without a separate notification layer.

namespace Encina.Compliance.LawfulBasis.Events;

/// <summary>
/// Raised when a new Legitimate Interest Assessment (LIA) is created following the EDPB three-part test.
/// </summary>
/// <remarks>
/// <para>
/// Article 6(1)(f) allows processing based on legitimate interests, provided those interests
/// are not overridden by the data subject's fundamental rights and freedoms. A LIA documents
/// the three-part test recommended by the European Data Protection Board (EDPB):
/// </para>
/// <list type="number">
/// <item><b>Purpose Test</b>: Is the interest legitimate? (<paramref name="LegitimateInterest"/>,
/// <paramref name="Benefits"/>, <paramref name="ConsequencesIfNotProcessed"/>)</item>
/// <item><b>Necessity Test</b>: Is the processing necessary? (<paramref name="NecessityJustification"/>,
/// <paramref name="AlternativesConsidered"/>, <paramref name="DataMinimisationNotes"/>)</item>
/// <item><b>Balancing Test</b>: Do individual rights override the interest? (<paramref name="NatureOfData"/>,
/// <paramref name="ReasonableExpectations"/>, <paramref name="ImpactAssessment"/>, <paramref name="Safeguards"/>)</item>
/// </list>
/// <para>
/// The initial outcome is <see cref="LIAOutcome.RequiresReview"/> — the LIA must be explicitly
/// approved or rejected via <see cref="LIAApproved"/> or <see cref="LIARejected"/>.
/// </para>
/// </remarks>
/// <param name="LIAId">Unique identifier for this LIA.</param>
/// <param name="Reference">Document reference identifier (e.g., "LIA-2024-FRAUD-001").</param>
/// <param name="Name">Human-readable name for this LIA.</param>
/// <param name="Purpose">The processing purpose this LIA covers.</param>
/// <param name="LegitimateInterest">Description of the legitimate interest being pursued (Purpose Test).</param>
/// <param name="Benefits">Benefits of the processing to the controller, data subject, or third parties (Purpose Test).</param>
/// <param name="ConsequencesIfNotProcessed">Consequences of not carrying out the processing (Purpose Test).</param>
/// <param name="NecessityJustification">Justification for why the processing is necessary (Necessity Test).</param>
/// <param name="AlternativesConsidered">Alternative approaches considered before choosing this processing (Necessity Test).</param>
/// <param name="DataMinimisationNotes">Notes on data minimisation measures applied (Necessity Test).</param>
/// <param name="NatureOfData">Description of the nature of the personal data being processed (Balancing Test).</param>
/// <param name="ReasonableExpectations">Assessment of data subject's reasonable expectations (Balancing Test).</param>
/// <param name="ImpactAssessment">Assessment of impact on data subjects' rights and freedoms (Balancing Test).</param>
/// <param name="Safeguards">Safeguards implemented to mitigate impact on data subjects (Balancing Test).</param>
/// <param name="AssessedBy">Name or role of the person who conducted the assessment.</param>
/// <param name="DPOInvolvement">Whether the DPO was involved in or consulted during the assessment.</param>
/// <param name="AssessedAtUtc">Timestamp when the assessment was conducted (UTC).</param>
/// <param name="Conditions">Any conditions attached to the assessment, or <c>null</c> if none.</param>
/// <param name="TenantId">Tenant identifier for multi-tenancy scoping.</param>
/// <param name="ModuleId">Module identifier for modular monolith scoping.</param>
public sealed record LIACreated(
    Guid LIAId,
    string Reference,
    string Name,
    string Purpose,
    string LegitimateInterest,
    string Benefits,
    string ConsequencesIfNotProcessed,
    string NecessityJustification,
    IReadOnlyList<string> AlternativesConsidered,
    string DataMinimisationNotes,
    string NatureOfData,
    string ReasonableExpectations,
    string ImpactAssessment,
    IReadOnlyList<string> Safeguards,
    string AssessedBy,
    bool DPOInvolvement,
    DateTimeOffset AssessedAtUtc,
    string? Conditions,
    string? TenantId,
    string? ModuleId) : INotification;

/// <summary>
/// Raised when a LIA is approved, confirming that the legitimate interest outweighs
/// the data subject's rights and freedoms.
/// </summary>
/// <remarks>
/// <para>
/// Approval transitions the LIA from <see cref="LIAOutcome.RequiresReview"/> to
/// <see cref="LIAOutcome.Approved"/>, making it valid for use as a LIA reference
/// in lawful basis registrations.
/// </para>
/// <para>
/// The <paramref name="Conclusion"/> should summarize why the balancing test outcome
/// favors the controller's legitimate interest.
/// </para>
/// </remarks>
/// <param name="LIAId">The LIA identifier.</param>
/// <param name="Conclusion">Summary conclusion of the assessment outcome.</param>
/// <param name="ApprovedBy">Identifier of the person who approved the LIA.</param>
/// <param name="ApprovedAtUtc">Timestamp when the LIA was approved (UTC).</param>
/// <param name="TenantId">Tenant identifier for multi-tenancy scoping.</param>
/// <param name="ModuleId">Module identifier for modular monolith scoping.</param>
public sealed record LIAApproved(
    Guid LIAId,
    string Conclusion,
    string ApprovedBy,
    DateTimeOffset ApprovedAtUtc,
    string? TenantId,
    string? ModuleId) : INotification;

/// <summary>
/// Raised when a LIA is rejected, indicating that the data subject's rights and freedoms
/// override the legitimate interest.
/// </summary>
/// <remarks>
/// <para>
/// Rejection transitions the LIA from <see cref="LIAOutcome.RequiresReview"/> to
/// <see cref="LIAOutcome.Rejected"/>. A rejected LIA cannot be used as a LIA reference
/// in lawful basis registrations.
/// </para>
/// <para>
/// The <paramref name="Conclusion"/> should document why the balancing test outcome
/// does not support the claimed legitimate interest.
/// </para>
/// </remarks>
/// <param name="LIAId">The LIA identifier.</param>
/// <param name="Conclusion">Summary conclusion explaining the rejection.</param>
/// <param name="RejectedBy">Identifier of the person who rejected the LIA.</param>
/// <param name="RejectedAtUtc">Timestamp when the LIA was rejected (UTC).</param>
/// <param name="TenantId">Tenant identifier for multi-tenancy scoping.</param>
/// <param name="ModuleId">Module identifier for modular monolith scoping.</param>
public sealed record LIARejected(
    Guid LIAId,
    string Conclusion,
    string RejectedBy,
    DateTimeOffset RejectedAtUtc,
    string? TenantId,
    string? ModuleId) : INotification;

/// <summary>
/// Raised when a periodic review is scheduled for an approved LIA.
/// </summary>
/// <remarks>
/// <para>
/// LIAs should be reviewed periodically to ensure the assessment remains valid as
/// circumstances change. This event records when the next review is due, enabling
/// governance dashboards and automated reminders to track review schedules.
/// </para>
/// </remarks>
/// <param name="LIAId">The LIA identifier.</param>
/// <param name="NextReviewAtUtc">Timestamp when the next review is due (UTC).</param>
/// <param name="ScheduledBy">Identifier of the person who scheduled the review.</param>
/// <param name="ScheduledAtUtc">Timestamp when the review was scheduled (UTC).</param>
/// <param name="TenantId">Tenant identifier for multi-tenancy scoping.</param>
/// <param name="ModuleId">Module identifier for modular monolith scoping.</param>
public sealed record LIAReviewScheduled(
    Guid LIAId,
    DateTimeOffset NextReviewAtUtc,
    string ScheduledBy,
    DateTimeOffset ScheduledAtUtc,
    string? TenantId,
    string? ModuleId) : INotification;
