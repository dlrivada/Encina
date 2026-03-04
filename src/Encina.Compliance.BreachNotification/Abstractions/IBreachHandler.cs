using Encina.Compliance.BreachNotification.Model;

using LanguageExt;

namespace Encina.Compliance.BreachNotification;

/// <summary>
/// High-level orchestrator for the breach notification workflow, coordinating detection,
/// notification, phased reporting, and resolution across the entire breach lifecycle.
/// </summary>
/// <remarks>
/// <para>
/// The breach handler is the primary entry point for application code interacting with
/// the breach notification system. It coordinates between <see cref="IBreachRecordStore"/>,
/// <see cref="IBreachNotifier"/>, and <see cref="IBreachAuditStore"/> to implement the
/// complete GDPR Articles 33 and 34 workflow.
/// </para>
/// <para>
/// The typical breach lifecycle orchestrated by this handler:
/// <list type="number">
/// <item><description><see cref="HandleDetectedBreachAsync"/> — Creates a formal breach record from a potential breach finding.</description></item>
/// <item><description><see cref="NotifyAuthorityAsync"/> — Notifies the supervisory authority per Article 33 (within 72 hours).</description></item>
/// <item><description><see cref="AddPhasedReportAsync"/> — Submits additional information as it becomes available per Article 33(4).</description></item>
/// <item><description><see cref="NotifySubjectsAsync"/> — Notifies affected data subjects per Article 34 (when high risk).</description></item>
/// <item><description><see cref="ResolveBreachAsync"/> — Closes the breach with a resolution summary.</description></item>
/// </list>
/// </para>
/// <para>
/// All methods automatically record audit entries via <see cref="IBreachAuditStore"/>
/// and publish domain notifications (e.g., <see cref="BreachDetectedNotification"/>,
/// <see cref="AuthorityNotifiedNotification"/>) for downstream consumers.
/// </para>
/// <para>
/// All methods follow Railway Oriented Programming (ROP) using <c>Either&lt;EncinaError, T&gt;</c>
/// to provide explicit error handling without exceptions for business logic.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Handle a detected breach (creates formal record, publishes BreachDetectedNotification)
/// var breachResult = await handler.HandleDetectedBreachAsync(potentialBreach, cancellationToken);
///
/// // Notify the supervisory authority (publishes AuthorityNotifiedNotification)
/// await breachResult.MatchAsync(
///     RightAsync: async breach =>
///     {
///         var notifyResult = await handler.NotifyAuthorityAsync(breach.Id, cancellationToken);
///         return notifyResult;
///     },
///     Left: error => error);
///
/// // Check deadline status
/// var deadline = await handler.GetDeadlineStatusAsync(breachId, cancellationToken);
/// </code>
/// </example>
public interface IBreachHandler
{
    /// <summary>
    /// Creates a formal breach record from a potential breach finding and initiates
    /// the notification lifecycle.
    /// </summary>
    /// <param name="breach">The potential breach identified by the detection engine.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// The created <see cref="BreachRecord"/> with a 72-hour notification deadline,
    /// or an <see cref="EncinaError"/> if the breach could not be recorded.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method:
    /// <list type="number">
    /// <item><description>Creates a <see cref="BreachRecord"/> from the <see cref="PotentialBreach"/> data.</description></item>
    /// <item><description>Persists the record via <see cref="IBreachRecordStore"/>.</description></item>
    /// <item><description>Records an audit entry for the detection event.</description></item>
    /// <item><description>Publishes a <c>BreachDetectedNotification</c>.</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// The 72-hour notification deadline is calculated from the security event's
    /// occurrence timestamp, marking the moment of "awareness" per Article 33(1).
    /// </para>
    /// </remarks>
    ValueTask<Either<EncinaError, BreachRecord>> HandleDetectedBreachAsync(
        PotentialBreach breach,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Notifies the supervisory authority about a breach per GDPR Article 33.
    /// </summary>
    /// <param name="breachId">The identifier of the breach to notify about.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="NotificationResult"/> documenting the notification outcome,
    /// or an <see cref="EncinaError"/> if the breach was not found or notification failed.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method:
    /// <list type="number">
    /// <item><description>Retrieves the breach record from the store.</description></item>
    /// <item><description>Delegates notification delivery to <see cref="IBreachNotifier.NotifyAuthorityAsync"/>.</description></item>
    /// <item><description>Updates the breach record with <see cref="BreachRecord.NotifiedAuthorityAtUtc"/>.</description></item>
    /// <item><description>Updates the breach status to <see cref="BreachStatus.AuthorityNotified"/>.</description></item>
    /// <item><description>Records an audit entry for the notification.</description></item>
    /// <item><description>Publishes an <c>AuthorityNotifiedNotification</c>.</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Should be called within 72 hours of breach detection per Article 33(1).
    /// If the deadline has passed, the breach record's <see cref="BreachRecord.DelayReason"/>
    /// should be set before calling this method.
    /// </para>
    /// </remarks>
    ValueTask<Either<EncinaError, NotificationResult>> NotifyAuthorityAsync(
        string breachId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Notifies affected data subjects about a breach per GDPR Article 34.
    /// </summary>
    /// <param name="breachId">The identifier of the breach to notify about.</param>
    /// <param name="subjectIds">The identifiers of the data subjects to notify.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="NotificationResult"/> documenting the notification outcome,
    /// or an <see cref="EncinaError"/> if the breach was not found or notification failed.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method:
    /// <list type="number">
    /// <item><description>Retrieves the breach record from the store.</description></item>
    /// <item><description>Verifies no exemption applies per Article 34(3).</description></item>
    /// <item><description>Delegates notification delivery to <see cref="IBreachNotifier.NotifyDataSubjectsAsync"/>.</description></item>
    /// <item><description>Updates the breach record with <see cref="BreachRecord.NotifiedSubjectsAtUtc"/>.</description></item>
    /// <item><description>Updates the breach status to <see cref="BreachStatus.SubjectsNotified"/>.</description></item>
    /// <item><description>Records an audit entry for the notification.</description></item>
    /// <item><description>Publishes a <c>SubjectsNotifiedNotification</c>.</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Per Article 34(1), this is required only when the breach "is likely to result
    /// in a high risk to the rights and freedoms of natural persons."
    /// </para>
    /// </remarks>
    ValueTask<Either<EncinaError, NotificationResult>> NotifySubjectsAsync(
        string breachId,
        IEnumerable<string> subjectIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a phased report to a breach per GDPR Article 33(4).
    /// </summary>
    /// <param name="breachId">The identifier of the breach.</param>
    /// <param name="content">The content of the phased report.</param>
    /// <param name="userId">
    /// The identifier of the user submitting the report.
    /// <c>null</c> for system-generated reports.
    /// </param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// The created <see cref="PhasedReport"/> with an assigned report number,
    /// or an <see cref="EncinaError"/> if the breach was not found or the report
    /// could not be added.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Per Article 33(4), information may be provided in phases "without undue further
    /// delay" when it is not possible to provide all information at the same time.
    /// This method automatically assigns the next sequential report number.
    /// </para>
    /// </remarks>
    ValueTask<Either<EncinaError, PhasedReport>> AddPhasedReportAsync(
        string breachId,
        string content,
        string? userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves a breach, marking it as completed with a resolution summary.
    /// </summary>
    /// <param name="breachId">The identifier of the breach to resolve.</param>
    /// <param name="resolutionSummary">
    /// Summary of the resolution measures taken and their outcomes.
    /// </param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <see cref="Unit"/> on success, or an <see cref="EncinaError"/> if the breach
    /// was not found or was already resolved.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method:
    /// <list type="number">
    /// <item><description>Retrieves the breach record from the store.</description></item>
    /// <item><description>Verifies the breach is not already resolved.</description></item>
    /// <item><description>Updates the status to <see cref="BreachStatus.Resolved"/> with the resolution summary.</description></item>
    /// <item><description>Records an audit entry for the resolution.</description></item>
    /// <item><description>Publishes a <c>BreachResolvedNotification</c>.</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Per Article 33(3)(d), the controller must describe the measures taken to address
    /// the breach. The <paramref name="resolutionSummary"/> captures this information.
    /// </para>
    /// </remarks>
    ValueTask<Either<EncinaError, Unit>> ResolveBreachAsync(
        string breachId,
        string resolutionSummary,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the current deadline status for a breach.
    /// </summary>
    /// <param name="breachId">The identifier of the breach.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="DeadlineStatus"/> snapshot for the breach, or an <see cref="EncinaError"/>
    /// if the breach was not found.
    /// </returns>
    /// <remarks>
    /// Provides a point-in-time view of the 72-hour notification deadline per Article 33(1).
    /// Includes remaining hours, overdue status, and current breach lifecycle status.
    /// </remarks>
    ValueTask<Either<EncinaError, DeadlineStatus>> GetDeadlineStatusAsync(
        string breachId,
        CancellationToken cancellationToken = default);
}
