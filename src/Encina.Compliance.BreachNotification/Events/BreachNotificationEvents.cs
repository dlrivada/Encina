using Encina.Compliance.BreachNotification.Model;

namespace Encina.Compliance.BreachNotification.Events;

// Event-sourced events implement INotification so they are automatically published
// by Encina.Marten's EventPublishingPipelineBehavior after successful command execution.
// This allows handlers to react to aggregate state changes without a separate notification layer.

/// <summary>
/// Raised when a personal data breach is first detected and recorded.
/// </summary>
/// <remarks>
/// <para>
/// Initiates the breach lifecycle. The aggregate transitions to <see cref="BreachStatus.Detected"/> status
/// and the 72-hour notification countdown starts per GDPR Article 33(1): "not later than 72 hours after
/// having become aware of it."
/// </para>
/// <para>
/// The <paramref name="DetectedByRule"/> identifies which detection rule triggered the breach,
/// providing traceability for the detection mechanism. The <paramref name="Severity"/> determines
/// subsequent notification obligations — breaches classified as <see cref="BreachSeverity.High"/> or
/// <see cref="BreachSeverity.Critical"/> require data subject notification under Art. 34(1).
/// </para>
/// </remarks>
/// <param name="BreachId">Unique identifier for this breach aggregate.</param>
/// <param name="Nature">Description of the nature of the breach (e.g., "unauthorized access", "data exfiltration").</param>
/// <param name="Severity">Initial severity assessment of the breach.</param>
/// <param name="DetectedByRule">Name of the detection rule that identified the breach.</param>
/// <param name="EstimatedAffectedSubjects">Approximate number of data subjects affected.</param>
/// <param name="Description">Detailed description of the breach circumstances.</param>
/// <param name="DetectedByUserId">Identifier of the user or system that detected the breach, or <c>null</c> if automated.</param>
/// <param name="DetectedAtUtc">Timestamp when the breach was detected (UTC) — starts the 72-hour Art. 33(1) deadline.</param>
/// <param name="TenantId">Tenant identifier for multi-tenancy scoping.</param>
/// <param name="ModuleId">Module identifier for modular monolith scoping.</param>
public sealed record BreachDetected(
    Guid BreachId,
    string Nature,
    BreachSeverity Severity,
    string DetectedByRule,
    int EstimatedAffectedSubjects,
    string Description,
    string? DetectedByUserId,
    DateTimeOffset DetectedAtUtc,
    string? TenantId,
    string? ModuleId) : INotification;

/// <summary>
/// Raised when a breach undergoes formal assessment, potentially updating its severity and scope.
/// </summary>
/// <remarks>
/// <para>
/// Transitions the breach from <see cref="BreachStatus.Detected"/> to <see cref="BreachStatus.Investigating"/>.
/// Per Art. 33(4), if the full scope is not yet determined at the time of initial notification,
/// information may be provided in phases. The assessment captures the evolving understanding of
/// the breach's impact.
/// </para>
/// <para>
/// A severity upgrade (e.g., from <see cref="BreachSeverity.Medium"/> to <see cref="BreachSeverity.High"/>)
/// may trigger additional notification obligations under Art. 34(1) — communication to data subjects
/// is required when the breach "is likely to result in a high risk."
/// </para>
/// </remarks>
/// <param name="BreachId">The breach aggregate identifier.</param>
/// <param name="UpdatedSeverity">Revised severity level after assessment.</param>
/// <param name="UpdatedAffectedSubjects">Revised estimate of affected data subjects.</param>
/// <param name="AssessmentSummary">Summary of the assessment findings.</param>
/// <param name="AssessedByUserId">Identifier of the user who performed the assessment.</param>
/// <param name="AssessedAtUtc">Timestamp when the assessment was completed (UTC).</param>
/// <param name="TenantId">Tenant identifier for multi-tenancy scoping.</param>
/// <param name="ModuleId">Module identifier for modular monolith scoping.</param>
public sealed record BreachAssessed(
    Guid BreachId,
    BreachSeverity UpdatedSeverity,
    int UpdatedAffectedSubjects,
    string AssessmentSummary,
    string AssessedByUserId,
    DateTimeOffset AssessedAtUtc,
    string? TenantId,
    string? ModuleId) : INotification;

/// <summary>
/// Raised when the supervisory authority (DPA) is notified about the breach.
/// </summary>
/// <remarks>
/// <para>
/// Transitions the breach to <see cref="BreachStatus.AuthorityNotified"/>.
/// Per Art. 33(1), notification must occur "not later than 72 hours after having become aware"
/// of the breach. Per Art. 33(3), the notification must include: the nature of the breach,
/// approximate number of subjects and data records, DPO contact details, likely consequences,
/// and measures taken or proposed.
/// </para>
/// <para>
/// If the 72-hour deadline cannot be met, the notification must be accompanied by reasons
/// for the delay per Art. 33(1) second paragraph.
/// </para>
/// </remarks>
/// <param name="BreachId">The breach aggregate identifier.</param>
/// <param name="AuthorityName">Name of the supervisory authority notified.</param>
/// <param name="AuthorityContactInfo">Contact details of the authority.</param>
/// <param name="ReportSummary">Summary of the notification report submitted to the authority.</param>
/// <param name="ReportedByUserId">Identifier of the user who filed the notification.</param>
/// <param name="ReportedAtUtc">Timestamp when the authority was notified (UTC).</param>
/// <param name="TenantId">Tenant identifier for multi-tenancy scoping.</param>
/// <param name="ModuleId">Module identifier for modular monolith scoping.</param>
public sealed record BreachReportedToDPA(
    Guid BreachId,
    string AuthorityName,
    string AuthorityContactInfo,
    string ReportSummary,
    string ReportedByUserId,
    DateTimeOffset ReportedAtUtc,
    string? TenantId,
    string? ModuleId) : INotification;

/// <summary>
/// Raised when affected data subjects are notified about the breach.
/// </summary>
/// <remarks>
/// <para>
/// Transitions the breach to <see cref="BreachStatus.SubjectsNotified"/>.
/// Per Art. 34(1), communication to data subjects is required when the breach "is likely to
/// result in a high risk to the rights and freedoms of natural persons." The communication
/// must describe the nature of the breach in clear and plain language (Art. 34(2)).
/// </para>
/// <para>
/// Art. 34(3) provides exemptions when: (a) appropriate technical protection was applied
/// (e.g., encryption), (b) subsequent measures ensure the high risk is no longer likely,
/// or (c) individual notification would involve disproportionate effort (in which case a
/// public communication must be made instead).
/// </para>
/// </remarks>
/// <param name="BreachId">The breach aggregate identifier.</param>
/// <param name="SubjectCount">Number of data subjects notified.</param>
/// <param name="CommunicationMethod">Method used to notify subjects (e.g., "email", "letter", "public-notice").</param>
/// <param name="Exemption">Any Art. 34(3) exemption that applies, or <see cref="SubjectNotificationExemption.None"/>.</param>
/// <param name="NotifiedByUserId">Identifier of the user who initiated the notification.</param>
/// <param name="NotifiedAtUtc">Timestamp when data subjects were notified (UTC).</param>
/// <param name="TenantId">Tenant identifier for multi-tenancy scoping.</param>
/// <param name="ModuleId">Module identifier for modular monolith scoping.</param>
public sealed record BreachNotifiedToSubjects(
    Guid BreachId,
    int SubjectCount,
    string CommunicationMethod,
    SubjectNotificationExemption Exemption,
    string NotifiedByUserId,
    DateTimeOffset NotifiedAtUtc,
    string? TenantId,
    string? ModuleId) : INotification;

/// <summary>
/// Raised when a phased report is added to the breach record.
/// </summary>
/// <remarks>
/// <para>
/// Per Art. 33(4), where it is not possible to provide all required information at the same time,
/// the information may be provided in phases without undue further delay. Each phased report
/// supplements the initial notification with additional details as the investigation progresses.
/// </para>
/// <para>
/// Phased reports are sequentially numbered and appended to the breach record. The breach
/// must not be in <see cref="BreachStatus.Closed"/> status to accept additional reports.
/// </para>
/// </remarks>
/// <param name="BreachId">The breach aggregate identifier.</param>
/// <param name="PhaseNumber">Sequential number of this phased report (1-based).</param>
/// <param name="ReportContent">Content of the phased report submission.</param>
/// <param name="SubmittedByUserId">Identifier of the user who submitted the report.</param>
/// <param name="SubmittedAtUtc">Timestamp when the phased report was submitted (UTC).</param>
/// <param name="TenantId">Tenant identifier for multi-tenancy scoping.</param>
/// <param name="ModuleId">Module identifier for modular monolith scoping.</param>
public sealed record BreachPhasedReportAdded(
    Guid BreachId,
    int PhaseNumber,
    string ReportContent,
    string SubmittedByUserId,
    DateTimeOffset SubmittedAtUtc,
    string? TenantId,
    string? ModuleId) : INotification;

/// <summary>
/// Raised when containment measures have been applied to stop or limit the breach.
/// </summary>
/// <remarks>
/// <para>
/// Transitions the breach to <see cref="BreachStatus.Resolved"/>.
/// Per Art. 33(3)(d), the notification to the supervisory authority must include
/// "the measures taken or proposed to be taken by the controller to address the personal
/// data breach, including, where appropriate, measures to mitigate its possible adverse effects."
/// This event records the actual containment measures applied.
/// </para>
/// </remarks>
/// <param name="BreachId">The breach aggregate identifier.</param>
/// <param name="ContainmentMeasures">Description of the containment measures applied.</param>
/// <param name="ContainedByUserId">Identifier of the user who applied containment measures.</param>
/// <param name="ContainedAtUtc">Timestamp when containment was achieved (UTC).</param>
/// <param name="TenantId">Tenant identifier for multi-tenancy scoping.</param>
/// <param name="ModuleId">Module identifier for modular monolith scoping.</param>
public sealed record BreachContained(
    Guid BreachId,
    string ContainmentMeasures,
    string ContainedByUserId,
    DateTimeOffset ContainedAtUtc,
    string? TenantId,
    string? ModuleId) : INotification;

/// <summary>
/// Raised when the breach case is formally closed after all obligations have been fulfilled.
/// </summary>
/// <remarks>
/// <para>
/// Transitions the breach to <see cref="BreachStatus.Closed"/>.
/// Per Art. 33(5), the controller must document all personal data breaches, comprising
/// the facts relating to the breach, its effects, and the remedial action taken.
/// The closed breach record — together with its full event history — serves as this
/// documentation and demonstrates accountability under Art. 5(2).
/// </para>
/// <para>
/// Once closed, no further state changes are permitted on the aggregate. The event stream
/// provides a complete, immutable timeline of the breach lifecycle for regulatory review.
/// </para>
/// </remarks>
/// <param name="BreachId">The breach aggregate identifier.</param>
/// <param name="ResolutionSummary">Summary of the resolution, including root cause analysis and remedial actions.</param>
/// <param name="ClosedByUserId">Identifier of the user who closed the breach case.</param>
/// <param name="ClosedAtUtc">Timestamp when the breach case was closed (UTC).</param>
/// <param name="TenantId">Tenant identifier for multi-tenancy scoping.</param>
/// <param name="ModuleId">Module identifier for modular monolith scoping.</param>
public sealed record BreachClosed(
    Guid BreachId,
    string ResolutionSummary,
    string ClosedByUserId,
    DateTimeOffset ClosedAtUtc,
    string? TenantId,
    string? ModuleId) : INotification;
