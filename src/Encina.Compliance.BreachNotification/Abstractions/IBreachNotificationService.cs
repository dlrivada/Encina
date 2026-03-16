using Encina.Compliance.BreachNotification.Model;
using Encina.Compliance.BreachNotification.ReadModels;
using LanguageExt;

namespace Encina.Compliance.BreachNotification.Abstractions;

/// <summary>
/// Service interface for managing breach notification lifecycle operations via event-sourced aggregates.
/// </summary>
/// <remarks>
/// <para>
/// Provides a clean API for recording, assessing, notifying, containing, and querying breaches.
/// The implementation wraps the event-sourced <c>BreachAggregate</c> via
/// <c>IAggregateRepository&lt;BreachAggregate&gt;</c>, handling aggregate loading,
/// command execution, persistence, and cache management.
/// </para>
/// <para>
/// This service replaces the legacy <c>IBreachHandler</c>, <c>IBreachRecordStore</c>, and
/// <c>IBreachAuditStore</c> interfaces with a single CQRS-oriented API. The event stream
/// serves as the audit trail, eliminating the need for a separate audit store.
/// </para>
/// <para>
/// <b>Commands</b> (write operations via aggregate):
/// <list type="bullet">
///   <item><description><see cref="RecordBreachAsync"/> — Detects and records a new breach (Art. 33(1))</description></item>
///   <item><description><see cref="AssessBreachAsync"/> — Formally assesses the breach scope</description></item>
///   <item><description><see cref="ReportToDPAAsync"/> — Notifies supervisory authority (Art. 33)</description></item>
///   <item><description><see cref="NotifySubjectsAsync"/> — Notifies affected data subjects (Art. 34)</description></item>
///   <item><description><see cref="AddPhasedReportAsync"/> — Adds phased report (Art. 33(4))</description></item>
///   <item><description><see cref="ContainBreachAsync"/> — Records containment measures</description></item>
///   <item><description><see cref="CloseBreachAsync"/> — Closes the breach case</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Queries</b> (read operations via read model repository):
/// <list type="bullet">
///   <item><description><see cref="GetBreachAsync"/> — Retrieves a breach by ID</description></item>
///   <item><description><see cref="GetBreachesByStatusAsync"/> — Lists breaches by status</description></item>
///   <item><description><see cref="GetBreachesByTenantAsync"/> — Lists breaches for a tenant</description></item>
///   <item><description><see cref="GetApproachingDeadlineBreachesAsync"/> — Finds breaches approaching 72-hour deadline</description></item>
///   <item><description><see cref="GetBreachHistoryAsync"/> — Retrieves full event history</description></item>
/// </list>
/// </para>
/// </remarks>
public interface IBreachNotificationService
{
    // ========================================================================
    // Command operations (write-side via BreachAggregate)
    // ========================================================================

    /// <summary>
    /// Records the detection of a new personal data breach.
    /// </summary>
    /// <param name="nature">Description of the nature of the breach.</param>
    /// <param name="severity">Initial severity assessment.</param>
    /// <param name="detectedByRule">Name of the detection rule that identified the breach.</param>
    /// <param name="estimatedAffectedSubjects">Approximate number of affected data subjects.</param>
    /// <param name="description">Detailed description of the breach circumstances.</param>
    /// <param name="detectedByUserId">Identifier of the user or system that detected the breach, or <c>null</c> if automated.</param>
    /// <param name="tenantId">Optional tenant identifier for multi-tenancy.</param>
    /// <param name="moduleId">Optional module identifier for modular monolith.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or the identifier of the newly created breach aggregate.</returns>
    /// <remarks>
    /// Per GDPR Article 33(1), the 72-hour notification countdown starts from the moment
    /// the controller becomes "aware" of the breach. The <see cref="Aggregates.BreachAggregate.DeadlineUtc"/>
    /// is calculated as detection time + 72 hours.
    /// </remarks>
    ValueTask<Either<EncinaError, Guid>> RecordBreachAsync(
        string nature,
        BreachSeverity severity,
        string detectedByRule,
        int estimatedAffectedSubjects,
        string description,
        string? detectedByUserId = null,
        string? tenantId = null,
        string? moduleId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records the formal assessment of a breach, potentially updating severity and scope.
    /// </summary>
    /// <param name="breachId">The breach aggregate identifier.</param>
    /// <param name="updatedSeverity">Revised severity level after assessment.</param>
    /// <param name="updatedAffectedSubjects">Revised estimate of affected data subjects.</param>
    /// <param name="assessmentSummary">Summary of the assessment findings.</param>
    /// <param name="assessedByUserId">Identifier of the user who performed the assessment.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or Unit on success.</returns>
    ValueTask<Either<EncinaError, Unit>> AssessBreachAsync(
        Guid breachId,
        BreachSeverity updatedSeverity,
        int updatedAffectedSubjects,
        string assessmentSummary,
        string assessedByUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records that the supervisory authority (DPA) has been notified about the breach.
    /// </summary>
    /// <param name="breachId">The breach aggregate identifier.</param>
    /// <param name="authorityName">Name of the supervisory authority notified.</param>
    /// <param name="authorityContactInfo">Contact details of the authority.</param>
    /// <param name="reportSummary">Summary of the notification report submitted.</param>
    /// <param name="reportedByUserId">Identifier of the user who filed the notification.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or Unit on success.</returns>
    /// <remarks>
    /// Per GDPR Article 33(1), notification must occur within 72 hours of becoming aware
    /// of the breach. Per Article 33(3), the notification must include nature, approximate
    /// number of subjects, DPO contact details, likely consequences, and measures taken.
    /// </remarks>
    ValueTask<Either<EncinaError, Unit>> ReportToDPAAsync(
        Guid breachId,
        string authorityName,
        string authorityContactInfo,
        string reportSummary,
        string reportedByUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records that affected data subjects have been notified about the breach.
    /// </summary>
    /// <param name="breachId">The breach aggregate identifier.</param>
    /// <param name="subjectCount">Number of data subjects notified.</param>
    /// <param name="communicationMethod">Method used to notify subjects (e.g., "email", "letter", "public-notice").</param>
    /// <param name="exemption">Any Art. 34(3) exemption that applies, or <see cref="SubjectNotificationExemption.None"/>.</param>
    /// <param name="notifiedByUserId">Identifier of the user who initiated the notification.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or Unit on success.</returns>
    /// <remarks>
    /// Per GDPR Article 34(1), communication to data subjects is required when the breach
    /// is likely to result in a high risk. Article 34(3) provides exemptions for encryption,
    /// subsequent risk mitigation, or disproportionate effort (with public communication).
    /// </remarks>
    ValueTask<Either<EncinaError, Unit>> NotifySubjectsAsync(
        Guid breachId,
        int subjectCount,
        string communicationMethod,
        SubjectNotificationExemption exemption,
        string notifiedByUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a phased report to the breach record.
    /// </summary>
    /// <param name="breachId">The breach aggregate identifier.</param>
    /// <param name="reportContent">Content of the phased report submission.</param>
    /// <param name="submittedByUserId">Identifier of the user who submitted the report.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or Unit on success.</returns>
    /// <remarks>
    /// Per GDPR Article 33(4), where it is not possible to provide all required information
    /// at the same time, the information may be provided in phases without undue further delay.
    /// </remarks>
    ValueTask<Either<EncinaError, Unit>> AddPhasedReportAsync(
        Guid breachId,
        string reportContent,
        string submittedByUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records that containment measures have been applied to stop or limit the breach.
    /// </summary>
    /// <param name="breachId">The breach aggregate identifier.</param>
    /// <param name="containmentMeasures">Description of the containment measures applied.</param>
    /// <param name="containedByUserId">Identifier of the user who applied containment measures.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or Unit on success.</returns>
    ValueTask<Either<EncinaError, Unit>> ContainBreachAsync(
        Guid breachId,
        string containmentMeasures,
        string containedByUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Closes the breach case after all notification obligations have been fulfilled.
    /// </summary>
    /// <param name="breachId">The breach aggregate identifier.</param>
    /// <param name="resolutionSummary">Summary of the resolution, including root cause analysis and remedial actions.</param>
    /// <param name="closedByUserId">Identifier of the user who closed the breach case.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or Unit on success.</returns>
    /// <remarks>
    /// Per GDPR Article 33(5), the controller must document all personal data breaches.
    /// Once closed, no further state changes are permitted on the aggregate.
    /// </remarks>
    ValueTask<Either<EncinaError, Unit>> CloseBreachAsync(
        Guid breachId,
        string resolutionSummary,
        string closedByUserId,
        CancellationToken cancellationToken = default);

    // ========================================================================
    // Query operations (read-side via BreachReadModel)
    // ========================================================================

    /// <summary>
    /// Retrieves a breach by its aggregate identifier.
    /// </summary>
    /// <param name="breachId">The breach aggregate identifier.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error (including not-found) or the breach read model.</returns>
    ValueTask<Either<EncinaError, BreachReadModel>> GetBreachAsync(
        Guid breachId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all breaches with a given lifecycle status.
    /// </summary>
    /// <param name="status">The breach status to filter by.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A read-only list of breach read models matching the status.</returns>
    ValueTask<Either<EncinaError, IReadOnlyList<BreachReadModel>>> GetBreachesByStatusAsync(
        BreachStatus status,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all breaches for a specific tenant.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A read-only list of breach read models for the tenant.</returns>
    ValueTask<Either<EncinaError, IReadOnlyList<BreachReadModel>>> GetBreachesByTenantAsync(
        string tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves breaches that are approaching or have exceeded their 72-hour notification deadline.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>
    /// A read-only list of breach read models where the authority has not been notified
    /// and the deadline is within the next 24 hours or already past.
    /// </returns>
    /// <remarks>
    /// Used by the deadline monitoring service to identify breaches requiring urgent attention.
    /// </remarks>
    ValueTask<Either<EncinaError, IReadOnlyList<BreachReadModel>>> GetApproachingDeadlineBreachesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the full event history for a breach aggregate.
    /// </summary>
    /// <param name="breachId">The breach aggregate identifier.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>
    /// Either an error or the list of domain events that have been applied to this breach,
    /// ordered chronologically. Provides a complete audit trail for GDPR Article 5(2)
    /// accountability and Article 33(5) breach documentation requirements.
    /// </returns>
    ValueTask<Either<EncinaError, IReadOnlyList<object>>> GetBreachHistoryAsync(
        Guid breachId,
        CancellationToken cancellationToken = default);
}
