using Encina.Compliance.BreachNotification.Events;
using Encina.Compliance.BreachNotification.Model;
using Encina.Marten.Projections;

namespace Encina.Compliance.BreachNotification.ReadModels;

/// <summary>
/// Marten inline projection that transforms breach aggregate events into <see cref="BreachReadModel"/> state.
/// </summary>
/// <remarks>
/// <para>
/// This projection implements the "Query" side of CQRS for breach notification management. It handles all 7
/// breach event types, creating or updating the <see cref="BreachReadModel"/> as events are applied.
/// </para>
/// <para>
/// <b>Event Handling</b>:
/// <list type="bullet">
///   <item><description><see cref="BreachDetected"/> — Creates a new read model (first event in stream)</description></item>
///   <item><description><see cref="BreachAssessed"/> — Updates severity, scope, and transitions to <see cref="BreachStatus.Investigating"/></description></item>
///   <item><description><see cref="BreachReportedToDPA"/> — Records DPA notification; transitions to <see cref="BreachStatus.AuthorityNotified"/></description></item>
///   <item><description><see cref="BreachNotifiedToSubjects"/> — Records subject notification; transitions to <see cref="BreachStatus.SubjectsNotified"/></description></item>
///   <item><description><see cref="BreachPhasedReportAdded"/> — Appends phased report summary per Art. 33(4)</description></item>
///   <item><description><see cref="BreachContained"/> — Records containment measures; transitions to <see cref="BreachStatus.Resolved"/></description></item>
///   <item><description><see cref="BreachClosed"/> — Records resolution; transitions to <see cref="BreachStatus.Closed"/></description></item>
/// </list>
/// </para>
/// <para>
/// <b>Idempotency</b>: The projection is idempotent — applying the same event multiple times
/// produces the same result. This enables safe replay and rebuild of the read model.
/// </para>
/// </remarks>
public sealed class BreachProjection :
    IProjection<BreachReadModel>,
    IProjectionCreator<BreachDetected, BreachReadModel>,
    IProjectionHandler<BreachAssessed, BreachReadModel>,
    IProjectionHandler<BreachReportedToDPA, BreachReadModel>,
    IProjectionHandler<BreachNotifiedToSubjects, BreachReadModel>,
    IProjectionHandler<BreachPhasedReportAdded, BreachReadModel>,
    IProjectionHandler<BreachContained, BreachReadModel>,
    IProjectionHandler<BreachClosed, BreachReadModel>
{
    /// <inheritdoc />
    public string ProjectionName => "BreachProjection";

    /// <summary>
    /// Creates a new <see cref="BreachReadModel"/> from a <see cref="BreachDetected"/> event.
    /// </summary>
    /// <remarks>
    /// This is the first event in a breach aggregate stream. It initializes all fields
    /// including the 72-hour Art. 33(1) deadline calculation.
    /// </remarks>
    /// <param name="domainEvent">The breach detected event.</param>
    /// <param name="context">Projection context with stream metadata.</param>
    /// <returns>A new <see cref="BreachReadModel"/> in <see cref="BreachStatus.Detected"/> status.</returns>
    public BreachReadModel Create(BreachDetected domainEvent, ProjectionContext context)
    {
        return new BreachReadModel
        {
            Id = domainEvent.BreachId,
            Nature = domainEvent.Nature,
            Severity = domainEvent.Severity,
            Status = BreachStatus.Detected,
            DetectedByRule = domainEvent.DetectedByRule,
            EstimatedAffectedSubjects = domainEvent.EstimatedAffectedSubjects,
            Description = domainEvent.Description,
            DetectedByUserId = domainEvent.DetectedByUserId,
            DetectedAtUtc = domainEvent.DetectedAtUtc,
            DeadlineUtc = domainEvent.DetectedAtUtc.AddHours(72),
            TenantId = domainEvent.TenantId,
            ModuleId = domainEvent.ModuleId,
            LastModifiedAtUtc = domainEvent.DetectedAtUtc,
            Version = 1
        };
    }

    /// <summary>
    /// Updates the read model when a breach is formally assessed.
    /// </summary>
    /// <remarks>
    /// Updates severity and scope estimates. Transitions to <see cref="BreachStatus.Investigating"/>.
    /// A severity upgrade may trigger additional notification obligations under Art. 34(1).
    /// </remarks>
    /// <param name="domainEvent">The breach assessed event.</param>
    /// <param name="current">The current read model state.</param>
    /// <param name="context">Projection context with stream metadata.</param>
    /// <returns>The updated read model.</returns>
    public BreachReadModel Apply(BreachAssessed domainEvent, BreachReadModel current, ProjectionContext context)
    {
        current.Severity = domainEvent.UpdatedSeverity;
        current.EstimatedAffectedSubjects = domainEvent.UpdatedAffectedSubjects;
        current.AssessmentSummary = domainEvent.AssessmentSummary;
        current.AssessedAtUtc = domainEvent.AssessedAtUtc;
        current.Status = BreachStatus.Investigating;
        current.LastModifiedAtUtc = domainEvent.AssessedAtUtc;
        current.Version++;
        return current;
    }

    /// <summary>
    /// Updates the read model when the supervisory authority is notified.
    /// </summary>
    /// <remarks>
    /// Transitions to <see cref="BreachStatus.AuthorityNotified"/>.
    /// Per Art. 33(1), this must occur within 72 hours of becoming aware of the breach.
    /// </remarks>
    /// <param name="domainEvent">The breach reported to DPA event.</param>
    /// <param name="current">The current read model state.</param>
    /// <param name="context">Projection context with stream metadata.</param>
    /// <returns>The updated read model.</returns>
    public BreachReadModel Apply(BreachReportedToDPA domainEvent, BreachReadModel current, ProjectionContext context)
    {
        current.AuthorityName = domainEvent.AuthorityName;
        current.ReportedToDPAAtUtc = domainEvent.ReportedAtUtc;
        current.Status = BreachStatus.AuthorityNotified;
        current.LastModifiedAtUtc = domainEvent.ReportedAtUtc;
        current.Version++;
        return current;
    }

    /// <summary>
    /// Updates the read model when data subjects are notified.
    /// </summary>
    /// <remarks>
    /// Transitions to <see cref="BreachStatus.SubjectsNotified"/>.
    /// Per Art. 34(1), communication to data subjects is required when the breach
    /// "is likely to result in a high risk to the rights and freedoms of natural persons."
    /// </remarks>
    /// <param name="domainEvent">The breach notified to subjects event.</param>
    /// <param name="current">The current read model state.</param>
    /// <param name="context">Projection context with stream metadata.</param>
    /// <returns>The updated read model.</returns>
    public BreachReadModel Apply(BreachNotifiedToSubjects domainEvent, BreachReadModel current, ProjectionContext context)
    {
        current.SubjectCount = domainEvent.SubjectCount;
        current.CommunicationMethod = domainEvent.CommunicationMethod;
        current.Exemption = domainEvent.Exemption;
        current.NotifiedSubjectsAtUtc = domainEvent.NotifiedAtUtc;
        current.Status = BreachStatus.SubjectsNotified;
        current.LastModifiedAtUtc = domainEvent.NotifiedAtUtc;
        current.Version++;
        return current;
    }

    /// <summary>
    /// Updates the read model when a phased report is added.
    /// </summary>
    /// <remarks>
    /// Per Art. 33(4), information may be provided in phases without undue further delay.
    /// Each phased report is appended to the <see cref="BreachReadModel.PhasedReports"/> list.
    /// </remarks>
    /// <param name="domainEvent">The breach phased report added event.</param>
    /// <param name="current">The current read model state.</param>
    /// <param name="context">Projection context with stream metadata.</param>
    /// <returns>The updated read model.</returns>
    public BreachReadModel Apply(BreachPhasedReportAdded domainEvent, BreachReadModel current, ProjectionContext context)
    {
        current.PhasedReports.Add(new PhasedReportSummary(
            domainEvent.PhaseNumber,
            domainEvent.ReportContent,
            domainEvent.SubmittedByUserId,
            domainEvent.SubmittedAtUtc));
        current.LastModifiedAtUtc = domainEvent.SubmittedAtUtc;
        current.Version++;
        return current;
    }

    /// <summary>
    /// Updates the read model when containment measures are applied.
    /// </summary>
    /// <remarks>
    /// Transitions to <see cref="BreachStatus.Resolved"/>.
    /// Per Art. 33(3)(d), the notification must include the measures taken to address the breach.
    /// </remarks>
    /// <param name="domainEvent">The breach contained event.</param>
    /// <param name="current">The current read model state.</param>
    /// <param name="context">Projection context with stream metadata.</param>
    /// <returns>The updated read model.</returns>
    public BreachReadModel Apply(BreachContained domainEvent, BreachReadModel current, ProjectionContext context)
    {
        current.ContainmentMeasures = domainEvent.ContainmentMeasures;
        current.ContainedAtUtc = domainEvent.ContainedAtUtc;
        current.Status = BreachStatus.Resolved;
        current.LastModifiedAtUtc = domainEvent.ContainedAtUtc;
        current.Version++;
        return current;
    }

    /// <summary>
    /// Updates the read model when the breach case is closed.
    /// </summary>
    /// <remarks>
    /// Transitions to <see cref="BreachStatus.Closed"/>.
    /// Per Art. 33(5), the controller must document all personal data breaches.
    /// The closed read model — together with the full event stream — serves as this
    /// documentation for Art. 5(2) accountability.
    /// </remarks>
    /// <param name="domainEvent">The breach closed event.</param>
    /// <param name="current">The current read model state.</param>
    /// <param name="context">Projection context with stream metadata.</param>
    /// <returns>The updated read model.</returns>
    public BreachReadModel Apply(BreachClosed domainEvent, BreachReadModel current, ProjectionContext context)
    {
        current.ResolutionSummary = domainEvent.ResolutionSummary;
        current.ClosedAtUtc = domainEvent.ClosedAtUtc;
        current.Status = BreachStatus.Closed;
        current.LastModifiedAtUtc = domainEvent.ClosedAtUtc;
        current.Version++;
        return current;
    }
}
