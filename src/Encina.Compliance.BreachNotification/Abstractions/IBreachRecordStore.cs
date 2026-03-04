using Encina.Compliance.BreachNotification.Model;

using LanguageExt;

namespace Encina.Compliance.BreachNotification;

/// <summary>
/// Store for managing the lifecycle of personal data breach records.
/// </summary>
/// <remarks>
/// <para>
/// The breach record store manages <see cref="BreachRecord"/> instances throughout
/// the entire breach notification lifecycle: from initial detection through authority
/// notification, data subject notification, phased reporting, and resolution.
/// </para>
/// <para>
/// Per GDPR Article 33(5), the controller must document "any personal data breaches,
/// comprising the facts relating to the personal data breach, its effects and the
/// remedial action taken." This store provides the persistence layer for that
/// documentation requirement.
/// </para>
/// <para>
/// Per GDPR Article 33(4), "where, and in so far as, it is not possible to provide
/// the information at the same time, the information may be provided in phases without
/// undue further delay." The <see cref="AddPhasedReportAsync"/> method supports this
/// progressive disclosure requirement by attaching phased reports to a breach record.
/// </para>
/// <para>
/// All methods follow Railway Oriented Programming (ROP) using <c>Either&lt;EncinaError, T&gt;</c>
/// to provide explicit error handling without exceptions for business logic.
/// </para>
/// <para>
/// Implementations may store records in-memory (for development/testing), in a database
/// (for production), or in any other suitable backing store. All 13 database providers
/// are supported: ADO.NET (4), Dapper (4), EF Core (4), and MongoDB (1).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Record a new breach
/// var breach = BreachRecord.Create(
///     nature: "Unauthorized access to customer database",
///     approximateSubjectsAffected: 5000,
///     categoriesOfDataAffected: new[] { "names", "email addresses", "phone numbers" },
///     dpoContactDetails: "dpo@example.com",
///     likelyConsequences: "Identity theft, phishing attacks",
///     measuresTaken: "Database credentials rotated, affected accounts locked",
///     detectedAtUtc: DateTimeOffset.UtcNow,
///     severity: BreachSeverity.High);
///
/// await store.RecordBreachAsync(breach, cancellationToken);
///
/// // Query breaches approaching their deadline
/// var approaching = await store.GetApproachingDeadlineAsync(hoursRemaining: 12, cancellationToken);
/// </code>
/// </example>
public interface IBreachRecordStore
{
    /// <summary>
    /// Records a new personal data breach.
    /// </summary>
    /// <param name="breach">The breach record to persist.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <see cref="Unit"/> on success, or an <see cref="EncinaError"/> if the breach
    /// could not be stored (e.g., duplicate ID).
    /// </returns>
    ValueTask<Either<EncinaError, Unit>> RecordBreachAsync(
        BreachRecord breach,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a breach record by its unique identifier.
    /// </summary>
    /// <param name="breachId">The unique identifier of the breach.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <c>Some(breach)</c> if a breach with the given ID exists,
    /// <c>None</c> if no breach is found, or an <see cref="EncinaError"/> on failure.
    /// </returns>
    ValueTask<Either<EncinaError, Option<BreachRecord>>> GetBreachAsync(
        string breachId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing breach record with new information.
    /// </summary>
    /// <param name="breach">The updated breach record.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <see cref="Unit"/> on success, or an <see cref="EncinaError"/> if the breach
    /// was not found or the update failed.
    /// </returns>
    /// <remarks>
    /// Used to update breach status, add notification timestamps, set delay reasons,
    /// apply exemptions, and record resolution information as the breach progresses
    /// through its lifecycle.
    /// </remarks>
    ValueTask<Either<EncinaError, Unit>> UpdateBreachAsync(
        BreachRecord breach,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all breach records with the specified lifecycle status.
    /// </summary>
    /// <param name="status">The breach status to filter by.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A read-only list of breach records matching the status, or an <see cref="EncinaError"/>
    /// on failure. Returns an empty list if no breaches match.
    /// </returns>
    /// <remarks>
    /// Common queries:
    /// <list type="bullet">
    /// <item><description><see cref="BreachStatus.Detected"/> — newly detected breaches awaiting triage.</description></item>
    /// <item><description><see cref="BreachStatus.Investigating"/> — breaches under active investigation.</description></item>
    /// <item><description><see cref="BreachStatus.AuthorityNotified"/> — breaches where authority has been notified but subjects have not.</description></item>
    /// </list>
    /// </remarks>
    ValueTask<Either<EncinaError, IReadOnlyList<BreachRecord>>> GetBreachesByStatusAsync(
        BreachStatus status,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all breach records where the 72-hour notification deadline has passed
    /// and the supervisory authority has not yet been notified.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A read-only list of overdue breach records, or an <see cref="EncinaError"/> on failure.
    /// Returns an empty list if no breaches are overdue.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Returns breaches where <see cref="BreachRecord.NotificationDeadlineUtc"/> is in the past
    /// AND <see cref="BreachRecord.NotifiedAuthorityAtUtc"/> is <c>null</c>.
    /// </para>
    /// <para>
    /// Per GDPR Article 33(1), notifications made after the 72-hour deadline must be
    /// accompanied by reasons for the delay. This method identifies breaches requiring
    /// such justification.
    /// </para>
    /// </remarks>
    ValueTask<Either<EncinaError, IReadOnlyList<BreachRecord>>> GetOverdueBreachesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves deadline status information for breaches approaching their notification deadline.
    /// </summary>
    /// <param name="hoursRemaining">
    /// The threshold in hours. Returns breaches with fewer remaining hours than this value.
    /// </param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A read-only list of <see cref="DeadlineStatus"/> snapshots for breaches approaching
    /// their deadline, or an <see cref="EncinaError"/> on failure.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method is the primary query used by <c>BreachDeadlineMonitorService</c>
    /// to identify breaches that need deadline warning notifications. It must be
    /// efficient as it is called at regular intervals (e.g., every 15 minutes).
    /// </para>
    /// <para>
    /// Only returns breaches that have not yet been notified to the authority
    /// (i.e., <see cref="BreachRecord.NotifiedAuthorityAtUtc"/> is <c>null</c>).
    /// </para>
    /// </remarks>
    ValueTask<Either<EncinaError, IReadOnlyList<DeadlineStatus>>> GetApproachingDeadlineAsync(
        int hoursRemaining,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a phased report to an existing breach record.
    /// </summary>
    /// <param name="breachId">The identifier of the breach to add the report to.</param>
    /// <param name="report">The phased report to attach.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <see cref="Unit"/> on success, or an <see cref="EncinaError"/> if the breach
    /// was not found or the report could not be added.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Per GDPR Article 33(4), "where, and in so far as, it is not possible to provide
    /// the information at the same time, the information may be provided in phases without
    /// undue further delay." Phased reports allow progressive disclosure as new information
    /// becomes available during the investigation.
    /// </para>
    /// <para>
    /// The <see cref="PhasedReport.ReportNumber"/> should increment sequentially for each
    /// breach, starting at 1 for the initial report.
    /// </para>
    /// </remarks>
    ValueTask<Either<EncinaError, Unit>> AddPhasedReportAsync(
        string breachId,
        PhasedReport report,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all breach records.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A read-only list of all breach records, or an <see cref="EncinaError"/> on failure.
    /// </returns>
    /// <remarks>
    /// Primarily used for compliance reporting and regulatory audits per Article 33(5).
    /// For large datasets, consider implementing pagination in provider-specific extensions.
    /// </remarks>
    ValueTask<Either<EncinaError, IReadOnlyList<BreachRecord>>> GetAllAsync(
        CancellationToken cancellationToken = default);
}
