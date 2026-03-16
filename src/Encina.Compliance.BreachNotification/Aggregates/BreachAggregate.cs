using Encina.Compliance.BreachNotification.Events;
using Encina.Compliance.BreachNotification.Model;
using Encina.DomainModeling;

namespace Encina.Compliance.BreachNotification.Aggregates;

/// <summary>
/// Event-sourced aggregate representing the full lifecycle of a personal data breach
/// from detection through notification, containment, and closure.
/// </summary>
/// <remarks>
/// <para>
/// Each aggregate instance represents a single breach incident, tracking the complete
/// notification workflow mandated by GDPR Articles 33 and 34. The lifecycle progresses
/// through: <see cref="BreachStatus.Detected"/> → <see cref="BreachStatus.Investigating"/>
/// → <see cref="BreachStatus.AuthorityNotified"/> → <see cref="BreachStatus.SubjectsNotified"/>
/// → <see cref="BreachStatus.Resolved"/> → <see cref="BreachStatus.Closed"/>.
/// </para>
/// <para>
/// The 72-hour notification deadline (Art. 33(1)) is calculated from the detection timestamp
/// and stored as <see cref="DeadlineUtc"/>. Phased reporting (Art. 33(4)) is supported via
/// <see cref="AddPhasedReport"/> for progressive disclosure as the investigation evolves.
/// </para>
/// <para>
/// All state changes are captured as immutable events, providing a complete audit trail
/// for GDPR Article 5(2) accountability requirements. Events implement <see cref="INotification"/>
/// and are automatically published by <c>EventPublishingPipelineBehavior</c> after successful
/// Marten commit, enabling downstream handlers to react to breach lifecycle changes.
/// </para>
/// </remarks>
public sealed class BreachAggregate : AggregateBase
{
    /// <summary>
    /// Description of the nature of the breach.
    /// </summary>
    public string Nature { get; private set; } = string.Empty;

    /// <summary>
    /// Current severity assessment of the breach.
    /// </summary>
    /// <remarks>
    /// May be updated during assessment. Severity determines notification obligations:
    /// <see cref="BreachSeverity.High"/> or <see cref="BreachSeverity.Critical"/> require
    /// data subject notification under Art. 34(1).
    /// </remarks>
    public BreachSeverity Severity { get; private set; }

    /// <summary>
    /// Current lifecycle status of the breach.
    /// </summary>
    public BreachStatus Status { get; private set; }

    /// <summary>
    /// Approximate number of data subjects affected by the breach.
    /// </summary>
    /// <remarks>
    /// Per Art. 33(3)(a), the notification must include "the approximate number of data subjects
    /// concerned." This value may be updated during assessment as the scope becomes clearer.
    /// </remarks>
    public int EstimatedAffectedSubjects { get; private set; }

    /// <summary>
    /// Detailed description of the breach circumstances.
    /// </summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// Timestamp when the breach was detected (UTC).
    /// </summary>
    /// <remarks>
    /// This is the point of "becoming aware" per Art. 33(1), which starts the 72-hour
    /// notification countdown.
    /// </remarks>
    public DateTimeOffset DetectedAtUtc { get; private set; }

    /// <summary>
    /// Deadline for notifying the supervisory authority (UTC).
    /// </summary>
    /// <remarks>
    /// Calculated as <see cref="DetectedAtUtc"/> + 72 hours per Art. 33(1).
    /// </remarks>
    public DateTimeOffset DeadlineUtc { get; private set; }

    /// <summary>
    /// Timestamp when the breach was formally assessed (UTC), or <c>null</c> if not yet assessed.
    /// </summary>
    public DateTimeOffset? AssessedAtUtc { get; private set; }

    /// <summary>
    /// Timestamp when the supervisory authority was notified (UTC), or <c>null</c> if not yet reported.
    /// </summary>
    public DateTimeOffset? ReportedToDPAAtUtc { get; private set; }

    /// <summary>
    /// Timestamp when data subjects were notified (UTC), or <c>null</c> if not yet notified.
    /// </summary>
    public DateTimeOffset? NotifiedSubjectsAtUtc { get; private set; }

    /// <summary>
    /// Timestamp when the breach was contained (UTC), or <c>null</c> if not yet contained.
    /// </summary>
    public DateTimeOffset? ContainedAtUtc { get; private set; }

    /// <summary>
    /// Timestamp when the breach case was closed (UTC), or <c>null</c> if still open.
    /// </summary>
    public DateTimeOffset? ClosedAtUtc { get; private set; }

    /// <summary>
    /// Name of the supervisory authority that was notified, or <c>null</c> if not yet reported.
    /// </summary>
    public string? AuthorityName { get; private set; }

    /// <summary>
    /// Number of data subjects notified about the breach.
    /// </summary>
    public int SubjectCount { get; private set; }

    /// <summary>
    /// Number of phased reports submitted for this breach.
    /// </summary>
    /// <remarks>
    /// Per Art. 33(4), information may be provided in phases. This counter tracks
    /// how many phased reports have been submitted.
    /// </remarks>
    public int PhasedReportCount { get; private set; }

    /// <summary>
    /// Tenant identifier for multi-tenancy scoping.
    /// </summary>
    public string? TenantId { get; private set; }

    /// <summary>
    /// Module identifier for modular monolith scoping.
    /// </summary>
    public string? ModuleId { get; private set; }

    /// <summary>
    /// Records the detection of a personal data breach, initiating the breach lifecycle
    /// and the 72-hour Art. 33(1) notification deadline.
    /// </summary>
    /// <remarks>
    /// Creates a new breach aggregate in <see cref="BreachStatus.Detected"/> status.
    /// The deadline is calculated as <paramref name="detectedAtUtc"/> + 72 hours.
    /// </remarks>
    /// <param name="id">Unique identifier for the new breach aggregate.</param>
    /// <param name="nature">Description of the nature of the breach.</param>
    /// <param name="severity">Initial severity assessment.</param>
    /// <param name="detectedByRule">Name of the detection rule that identified the breach.</param>
    /// <param name="estimatedAffectedSubjects">Approximate number of affected data subjects.</param>
    /// <param name="description">Detailed description of the breach circumstances.</param>
    /// <param name="detectedByUserId">Identifier of the user or system that detected the breach, or <c>null</c> if automated.</param>
    /// <param name="detectedAtUtc">Timestamp when the breach was detected (UTC).</param>
    /// <param name="tenantId">Optional tenant identifier for multi-tenancy scoping.</param>
    /// <param name="moduleId">Optional module identifier for modular monolith scoping.</param>
    /// <returns>A new <see cref="BreachAggregate"/> in <see cref="BreachStatus.Detected"/> status.</returns>
    public static BreachAggregate Detect(
        Guid id,
        string nature,
        BreachSeverity severity,
        string detectedByRule,
        int estimatedAffectedSubjects,
        string description,
        string? detectedByUserId,
        DateTimeOffset detectedAtUtc,
        string? tenantId = null,
        string? moduleId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nature);
        ArgumentException.ThrowIfNullOrWhiteSpace(detectedByRule);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        var aggregate = new BreachAggregate();
        aggregate.RaiseEvent(new BreachDetected(
            id,
            nature,
            severity,
            detectedByRule,
            estimatedAffectedSubjects,
            description,
            detectedByUserId,
            detectedAtUtc,
            tenantId,
            moduleId));
        return aggregate;
    }

    /// <summary>
    /// Records the formal assessment of the breach, potentially updating severity and scope.
    /// </summary>
    /// <remarks>
    /// Transitions the breach from <see cref="BreachStatus.Detected"/> to
    /// <see cref="BreachStatus.Investigating"/>. Per Art. 33(4), ongoing investigation
    /// should not delay the initial notification to the supervisory authority.
    /// </remarks>
    /// <param name="updatedSeverity">Revised severity level after assessment.</param>
    /// <param name="updatedAffectedSubjects">Revised estimate of affected data subjects.</param>
    /// <param name="assessmentSummary">Summary of the assessment findings.</param>
    /// <param name="assessedByUserId">Identifier of the user who performed the assessment.</param>
    /// <param name="assessedAtUtc">Timestamp when the assessment was completed (UTC).</param>
    /// <exception cref="InvalidOperationException">Thrown when the breach is not in <see cref="BreachStatus.Detected"/> status.</exception>
    public void Assess(
        BreachSeverity updatedSeverity,
        int updatedAffectedSubjects,
        string assessmentSummary,
        string assessedByUserId,
        DateTimeOffset assessedAtUtc)
    {
        if (Status != BreachStatus.Detected)
        {
            throw new InvalidOperationException(
                $"Cannot assess breach when it is in '{Status}' status. Assessment is only allowed from Detected status.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(assessmentSummary);
        ArgumentException.ThrowIfNullOrWhiteSpace(assessedByUserId);

        RaiseEvent(new BreachAssessed(
            Id,
            updatedSeverity,
            updatedAffectedSubjects,
            assessmentSummary,
            assessedByUserId,
            assessedAtUtc,
            TenantId,
            ModuleId));
    }

    /// <summary>
    /// Records that the supervisory authority (DPA) has been notified about the breach.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Transitions the breach to <see cref="BreachStatus.AuthorityNotified"/>.
    /// Per Art. 33(1), this must occur within 72 hours of becoming aware of the breach.
    /// Per Art. 33(3), the notification must include: the nature of the breach,
    /// approximate number of subjects, DPO contact details, likely consequences,
    /// and measures taken or proposed.
    /// </para>
    /// <para>
    /// This method is valid from <see cref="BreachStatus.Detected"/> or
    /// <see cref="BreachStatus.Investigating"/> status — the authority may be notified
    /// before formal assessment is complete (which is encouraged by Art. 33(1)).
    /// </para>
    /// </remarks>
    /// <param name="authorityName">Name of the supervisory authority notified.</param>
    /// <param name="authorityContactInfo">Contact details of the authority.</param>
    /// <param name="reportSummary">Summary of the notification report submitted.</param>
    /// <param name="reportedByUserId">Identifier of the user who filed the notification.</param>
    /// <param name="reportedAtUtc">Timestamp when the authority was notified (UTC).</param>
    /// <exception cref="InvalidOperationException">Thrown when the breach is not in <see cref="BreachStatus.Detected"/> or <see cref="BreachStatus.Investigating"/> status.</exception>
    public void ReportToDPA(
        string authorityName,
        string authorityContactInfo,
        string reportSummary,
        string reportedByUserId,
        DateTimeOffset reportedAtUtc)
    {
        if (Status is not (BreachStatus.Detected or BreachStatus.Investigating))
        {
            throw new InvalidOperationException(
                $"Cannot report breach to DPA when it is in '{Status}' status. Reporting is only allowed from Detected or Investigating status.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(authorityName);
        ArgumentException.ThrowIfNullOrWhiteSpace(authorityContactInfo);
        ArgumentException.ThrowIfNullOrWhiteSpace(reportSummary);
        ArgumentException.ThrowIfNullOrWhiteSpace(reportedByUserId);

        RaiseEvent(new BreachReportedToDPA(
            Id,
            authorityName,
            authorityContactInfo,
            reportSummary,
            reportedByUserId,
            reportedAtUtc,
            TenantId,
            ModuleId));
    }

    /// <summary>
    /// Records that affected data subjects have been notified about the breach.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Transitions the breach to <see cref="BreachStatus.SubjectsNotified"/>.
    /// Per Art. 34(1), communication to data subjects is required when the breach
    /// "is likely to result in a high risk to the rights and freedoms of natural persons."
    /// </para>
    /// <para>
    /// Art. 34(3) exemptions are captured via the <paramref name="exemption"/> parameter.
    /// Even when an exemption applies, this event should be recorded to document the decision
    /// for Art. 5(2) accountability.
    /// </para>
    /// </remarks>
    /// <param name="subjectCount">Number of data subjects notified.</param>
    /// <param name="communicationMethod">Method used to notify subjects (e.g., "email", "letter", "public-notice").</param>
    /// <param name="exemption">Any Art. 34(3) exemption that applies, or <see cref="SubjectNotificationExemption.None"/>.</param>
    /// <param name="notifiedByUserId">Identifier of the user who initiated the notification.</param>
    /// <param name="notifiedAtUtc">Timestamp when data subjects were notified (UTC).</param>
    /// <exception cref="InvalidOperationException">Thrown when the breach is not in <see cref="BreachStatus.AuthorityNotified"/> status.</exception>
    public void NotifySubjects(
        int subjectCount,
        string communicationMethod,
        SubjectNotificationExemption exemption,
        string notifiedByUserId,
        DateTimeOffset notifiedAtUtc)
    {
        if (Status != BreachStatus.AuthorityNotified)
        {
            throw new InvalidOperationException(
                $"Cannot notify subjects when breach is in '{Status}' status. Subject notification is only allowed from AuthorityNotified status.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(communicationMethod);
        ArgumentException.ThrowIfNullOrWhiteSpace(notifiedByUserId);

        RaiseEvent(new BreachNotifiedToSubjects(
            Id,
            subjectCount,
            communicationMethod,
            exemption,
            notifiedByUserId,
            notifiedAtUtc,
            TenantId,
            ModuleId));
    }

    /// <summary>
    /// Adds a phased report to the breach record, providing additional information
    /// as the investigation progresses.
    /// </summary>
    /// <remarks>
    /// Per Art. 33(4), where it is not possible to provide all information at the same time,
    /// it may be provided in phases without undue further delay. Phased reports are sequentially
    /// numbered starting from 1.
    /// </remarks>
    /// <param name="reportContent">Content of the phased report submission.</param>
    /// <param name="submittedByUserId">Identifier of the user who submitted the report.</param>
    /// <param name="submittedAtUtc">Timestamp when the phased report was submitted (UTC).</param>
    /// <exception cref="InvalidOperationException">Thrown when the breach is in <see cref="BreachStatus.Closed"/> status.</exception>
    public void AddPhasedReport(
        string reportContent,
        string submittedByUserId,
        DateTimeOffset submittedAtUtc)
    {
        if (Status == BreachStatus.Closed)
        {
            throw new InvalidOperationException(
                "Cannot add a phased report to a closed breach. The breach case has been finalized.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(reportContent);
        ArgumentException.ThrowIfNullOrWhiteSpace(submittedByUserId);

        RaiseEvent(new BreachPhasedReportAdded(
            Id,
            PhasedReportCount + 1,
            reportContent,
            submittedByUserId,
            submittedAtUtc,
            TenantId,
            ModuleId));
    }

    /// <summary>
    /// Records that containment measures have been applied to stop or limit the breach.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Transitions the breach to <see cref="BreachStatus.Resolved"/>.
    /// Per Art. 33(3)(d), the notification must include "the measures taken or proposed to be
    /// taken by the controller to address the personal data breach, including, where appropriate,
    /// measures to mitigate its possible adverse effects."
    /// </para>
    /// </remarks>
    /// <param name="containmentMeasures">Description of the containment measures applied.</param>
    /// <param name="containedByUserId">Identifier of the user who applied containment measures.</param>
    /// <param name="containedAtUtc">Timestamp when containment was achieved (UTC).</param>
    /// <exception cref="InvalidOperationException">Thrown when the breach is in <see cref="BreachStatus.Closed"/> status.</exception>
    public void Contain(
        string containmentMeasures,
        string containedByUserId,
        DateTimeOffset containedAtUtc)
    {
        if (Status == BreachStatus.Closed)
        {
            throw new InvalidOperationException(
                "Cannot contain a closed breach. The breach case has been finalized.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(containmentMeasures);
        ArgumentException.ThrowIfNullOrWhiteSpace(containedByUserId);

        RaiseEvent(new BreachContained(
            Id,
            containmentMeasures,
            containedByUserId,
            containedAtUtc,
            TenantId,
            ModuleId));
    }

    /// <summary>
    /// Closes the breach case after all notification obligations have been fulfilled.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Transitions the breach to <see cref="BreachStatus.Closed"/>.
    /// Per Art. 33(5), the controller must document all personal data breaches, including
    /// their effects and the remedial action taken. The closed aggregate — together with its
    /// complete event stream — serves as this documentation for Art. 5(2) accountability.
    /// </para>
    /// <para>
    /// Once closed, no further state changes are permitted on the aggregate.
    /// </para>
    /// </remarks>
    /// <param name="resolutionSummary">Summary of the resolution, including root cause analysis and remedial actions.</param>
    /// <param name="closedByUserId">Identifier of the user who closed the breach case.</param>
    /// <param name="closedAtUtc">Timestamp when the breach case was closed (UTC).</param>
    /// <exception cref="InvalidOperationException">Thrown when the breach is not in <see cref="BreachStatus.SubjectsNotified"/> or <see cref="BreachStatus.Resolved"/> status.</exception>
    public void Close(
        string resolutionSummary,
        string closedByUserId,
        DateTimeOffset closedAtUtc)
    {
        if (Status is not (BreachStatus.SubjectsNotified or BreachStatus.Resolved))
        {
            throw new InvalidOperationException(
                $"Cannot close breach when it is in '{Status}' status. Closure is only allowed from SubjectsNotified or Resolved status.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(resolutionSummary);
        ArgumentException.ThrowIfNullOrWhiteSpace(closedByUserId);

        RaiseEvent(new BreachClosed(
            Id,
            resolutionSummary,
            closedByUserId,
            closedAtUtc,
            TenantId,
            ModuleId));
    }

    /// <inheritdoc />
    protected override void Apply(object domainEvent)
    {
        switch (domainEvent)
        {
            case BreachDetected e:
                Id = e.BreachId;
                Nature = e.Nature;
                Severity = e.Severity;
                EstimatedAffectedSubjects = e.EstimatedAffectedSubjects;
                Description = e.Description;
                DetectedAtUtc = e.DetectedAtUtc;
                DeadlineUtc = e.DetectedAtUtc.AddHours(72);
                Status = BreachStatus.Detected;
                TenantId = e.TenantId;
                ModuleId = e.ModuleId;
                break;

            case BreachAssessed e:
                Severity = e.UpdatedSeverity;
                EstimatedAffectedSubjects = e.UpdatedAffectedSubjects;
                AssessedAtUtc = e.AssessedAtUtc;
                Status = BreachStatus.Investigating;
                break;

            case BreachReportedToDPA e:
                AuthorityName = e.AuthorityName;
                ReportedToDPAAtUtc = e.ReportedAtUtc;
                Status = BreachStatus.AuthorityNotified;
                break;

            case BreachNotifiedToSubjects e:
                SubjectCount = e.SubjectCount;
                NotifiedSubjectsAtUtc = e.NotifiedAtUtc;
                Status = BreachStatus.SubjectsNotified;
                break;

            case BreachPhasedReportAdded:
                PhasedReportCount++;
                break;

            case BreachContained e:
                ContainedAtUtc = e.ContainedAtUtc;
                Status = BreachStatus.Resolved;
                break;

            case BreachClosed e:
                ClosedAtUtc = e.ClosedAtUtc;
                Status = BreachStatus.Closed;
                break;
        }
    }
}
