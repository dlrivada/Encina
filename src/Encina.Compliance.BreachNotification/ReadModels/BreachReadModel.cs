using Encina.Compliance.BreachNotification.Model;
using Encina.Marten.Projections;

namespace Encina.Compliance.BreachNotification.ReadModels;

/// <summary>
/// Query-optimized projected view of a breach record, built from <see cref="Aggregates.BreachAggregate"/> events.
/// </summary>
/// <remarks>
/// <para>
/// This read model is materialized from the breach aggregate event stream by
/// <see cref="BreachProjection"/>. It provides an efficient query view without
/// replaying events, while the underlying event stream maintains the full audit trail
/// for GDPR Article 5(2) accountability requirements.
/// </para>
/// <para>
/// Properties have mutable setters because projections update them incrementally
/// as events are applied. The <see cref="LastModifiedAtUtc"/> timestamp is updated
/// on every event, enabling efficient change detection.
/// </para>
/// <para>
/// Used by breach notification query methods to return breach state to consumers.
/// Replaces the old entity-based breach record for query purposes in the event-sourced model.
/// </para>
/// </remarks>
public sealed class BreachReadModel : IReadModel
{
    /// <summary>
    /// Unique identifier for this breach (matches the aggregate stream ID).
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Description of the nature of the breach (e.g., "unauthorized access", "data exfiltration").
    /// </summary>
    public string Nature { get; set; } = string.Empty;

    /// <summary>
    /// Current severity assessment of the breach.
    /// </summary>
    /// <remarks>
    /// May be updated during assessment. Severity determines notification obligations:
    /// <see cref="BreachSeverity.High"/> or <see cref="BreachSeverity.Critical"/> require
    /// data subject notification under Art. 34(1).
    /// </remarks>
    public BreachSeverity Severity { get; set; }

    /// <summary>
    /// Current lifecycle status of the breach.
    /// </summary>
    public BreachStatus Status { get; set; }

    /// <summary>
    /// Name of the detection rule that identified the breach.
    /// </summary>
    /// <remarks>
    /// Provides traceability for the detection mechanism.
    /// </remarks>
    public string DetectedByRule { get; set; } = string.Empty;

    /// <summary>
    /// Approximate number of data subjects affected by the breach.
    /// </summary>
    /// <remarks>
    /// Per Art. 33(3)(a), the notification must include "the approximate number of data subjects
    /// concerned." This value may be updated during assessment as the scope becomes clearer.
    /// </remarks>
    public int EstimatedAffectedSubjects { get; set; }

    /// <summary>
    /// Detailed description of the breach circumstances.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Identifier of the user or system that detected the breach, or <c>null</c> if automated.
    /// </summary>
    public string? DetectedByUserId { get; set; }

    /// <summary>
    /// Timestamp when the breach was detected (UTC).
    /// </summary>
    /// <remarks>
    /// This is the point of "becoming aware" per Art. 33(1), which starts the 72-hour
    /// notification countdown.
    /// </remarks>
    public DateTimeOffset DetectedAtUtc { get; set; }

    /// <summary>
    /// Deadline for notifying the supervisory authority (UTC).
    /// </summary>
    /// <remarks>
    /// Calculated as <see cref="DetectedAtUtc"/> + 72 hours per Art. 33(1).
    /// </remarks>
    public DateTimeOffset DeadlineUtc { get; set; }

    /// <summary>
    /// Timestamp when the breach was formally assessed (UTC), or <c>null</c> if not yet assessed.
    /// </summary>
    public DateTimeOffset? AssessedAtUtc { get; set; }

    /// <summary>
    /// Summary of the assessment findings, or <c>null</c> if not yet assessed.
    /// </summary>
    public string? AssessmentSummary { get; set; }

    /// <summary>
    /// Name of the supervisory authority that was notified, or <c>null</c> if not yet reported.
    /// </summary>
    public string? AuthorityName { get; set; }

    /// <summary>
    /// Timestamp when the supervisory authority was notified (UTC), or <c>null</c> if not yet reported.
    /// </summary>
    public DateTimeOffset? ReportedToDPAAtUtc { get; set; }

    /// <summary>
    /// Number of data subjects notified about the breach.
    /// </summary>
    public int SubjectCount { get; set; }

    /// <summary>
    /// Method used to notify data subjects (e.g., "email", "letter", "public-notice"),
    /// or <c>null</c> if subjects have not been notified.
    /// </summary>
    public string? CommunicationMethod { get; set; }

    /// <summary>
    /// Any Art. 34(3) exemption applied when notifying subjects,
    /// or <c>null</c> if subjects have not been notified.
    /// </summary>
    public SubjectNotificationExemption? Exemption { get; set; }

    /// <summary>
    /// Timestamp when data subjects were notified (UTC), or <c>null</c> if not yet notified.
    /// </summary>
    public DateTimeOffset? NotifiedSubjectsAtUtc { get; set; }

    /// <summary>
    /// Phased reports submitted for this breach per Art. 33(4).
    /// </summary>
    /// <remarks>
    /// Each entry captures the phase number, content summary, submitter, and timestamp
    /// of a phased report submission.
    /// </remarks>
    public List<PhasedReportSummary> PhasedReports { get; set; } = [];

    /// <summary>
    /// Description of the containment measures applied, or <c>null</c> if not yet contained.
    /// </summary>
    /// <remarks>
    /// Per Art. 33(3)(d), the notification must include "the measures taken or proposed
    /// to be taken by the controller to address the personal data breach."
    /// </remarks>
    public string? ContainmentMeasures { get; set; }

    /// <summary>
    /// Timestamp when the breach was contained (UTC), or <c>null</c> if not yet contained.
    /// </summary>
    public DateTimeOffset? ContainedAtUtc { get; set; }

    /// <summary>
    /// Summary of the resolution including root cause analysis and remedial actions,
    /// or <c>null</c> if not yet closed.
    /// </summary>
    public string? ResolutionSummary { get; set; }

    /// <summary>
    /// Timestamp when the breach case was closed (UTC), or <c>null</c> if still open.
    /// </summary>
    public DateTimeOffset? ClosedAtUtc { get; set; }

    /// <summary>
    /// Tenant identifier for multi-tenancy scoping.
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// Module identifier for modular monolith scoping.
    /// </summary>
    public string? ModuleId { get; set; }

    /// <summary>
    /// Timestamp of the last modification to this breach record (UTC).
    /// </summary>
    /// <remarks>
    /// Updated on every event applied to the breach aggregate.
    /// Enables efficient change detection and cache invalidation.
    /// </remarks>
    public DateTimeOffset LastModifiedAtUtc { get; set; }

    /// <summary>
    /// Event stream version for optimistic concurrency.
    /// </summary>
    /// <remarks>
    /// Incremented on every event. Matches the aggregate's <see cref="DomainModeling.AggregateBase.Version"/>.
    /// </remarks>
    public int Version { get; set; }
}

/// <summary>
/// Summary of a phased report submitted for a breach per GDPR Art. 33(4).
/// </summary>
/// <remarks>
/// Phased reports supplement the initial notification with additional details
/// as the investigation progresses. Each report is sequentially numbered.
/// </remarks>
/// <param name="PhaseNumber">Sequential number of this phased report (1-based).</param>
/// <param name="ReportContent">Content of the phased report submission.</param>
/// <param name="SubmittedByUserId">Identifier of the user who submitted the report.</param>
/// <param name="SubmittedAtUtc">Timestamp when the phased report was submitted (UTC).</param>
public sealed record PhasedReportSummary(
    int PhaseNumber,
    string ReportContent,
    string SubmittedByUserId,
    DateTimeOffset SubmittedAtUtc);
