using Encina.Compliance.Retention.Model;

using LanguageExt;

namespace Encina.Compliance.Retention;

/// <summary>
/// Service for orchestrating automated retention enforcement and expiration queries.
/// </summary>
/// <remarks>
/// <para>
/// The retention enforcer is the central component responsible for automated data deletion.
/// It queries for expired retention records, checks legal hold status, delegates actual
/// deletion to <c>IDataErasureExecutor</c> (from <c>Encina.Compliance.DataSubjectRights</c>),
/// updates record statuses, records audit entries, and publishes domain notifications.
/// </para>
/// <para>
/// Per GDPR Article 5(1)(e) (storage limitation), personal data shall be kept for no longer
/// than is necessary. The enforcer automates this requirement by periodically evaluating
/// retention records and initiating deletion for expired data, while respecting legal holds
/// per Article 17(3)(e) (legal claims exemption).
/// </para>
/// <para>
/// The enforcement flow:
/// <list type="number">
/// <item><description>Query <see cref="IRetentionRecordStore.GetExpiredRecordsAsync"/> for expired records.</description></item>
/// <item><description>For each record, check <see cref="ILegalHoldStore.IsUnderHoldAsync"/> — if held, update status to <see cref="RetentionStatus.UnderLegalHold"/>.</description></item>
/// <item><description>For non-held records, delegate deletion to <c>IDataErasureExecutor</c> (if registered).</description></item>
/// <item><description>Update record statuses, record audit entries, publish notifications.</description></item>
/// <item><description>Return a comprehensive <see cref="DeletionResult"/> with all outcomes.</description></item>
/// </list>
/// </para>
/// <para>
/// All methods follow Railway Oriented Programming (ROP) using <c>Either&lt;EncinaError, T&gt;</c>
/// to provide explicit error handling without exceptions for business logic.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Run enforcement cycle (typically called by RetentionEnforcementService hosted service)
/// var result = await enforcer.EnforceRetentionAsync(cancellationToken);
///
/// result.Match(
///     Right: deletion => Console.WriteLine(
///         $"Enforcement complete: {deletion.RecordsDeleted} deleted, " +
///         $"{deletion.RecordsUnderHold} held, {deletion.RecordsFailed} failed"),
///     Left: error => Console.WriteLine($"Enforcement failed: {error.Message}"));
///
/// // Query data approaching expiration for proactive alerts
/// var expiring = await enforcer.GetExpiringDataAsync(TimeSpan.FromDays(30), cancellationToken);
/// </code>
/// </example>
public interface IRetentionEnforcer
{
    /// <summary>
    /// Executes a retention enforcement cycle, deleting all eligible expired data.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="DeletionResult"/> summarizing all enforcement outcomes,
    /// or an <see cref="EncinaError"/> if the enforcement cycle could not be executed.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method is idempotent: running it multiple times produces the same result
    /// (already-deleted records are not re-processed). It is safe to invoke concurrently,
    /// although the built-in <c>RetentionEnforcementService</c> serializes calls.
    /// </para>
    /// <para>
    /// If <c>IDataErasureExecutor</c> is not registered in the service container,
    /// the enforcer operates in degraded mode: records are marked as expired but not
    /// physically deleted. A warning is logged indicating that DSR integration is not configured.
    /// </para>
    /// </remarks>
    ValueTask<Either<EncinaError, DeletionResult>> EnforceRetentionAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves data entities that will expire within the specified time window.
    /// </summary>
    /// <param name="within">The time window to look ahead for upcoming expirations.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A read-only list of <see cref="ExpiringData"/> records representing entities
    /// approaching their retention deadline, or an <see cref="EncinaError"/> on failure.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Used to generate proactive expiration alerts. For example, querying with
    /// <c>TimeSpan.FromDays(30)</c> returns all data expiring within the next 30 days.
    /// </para>
    /// <para>
    /// Per Recital 39, appropriate measures should include establishing time limits
    /// for erasure or periodic review. This method supports the periodic review
    /// requirement by surfacing data approaching its retention deadline.
    /// </para>
    /// </remarks>
    ValueTask<Either<EncinaError, IReadOnlyList<ExpiringData>>> GetExpiringDataAsync(
        TimeSpan within,
        CancellationToken cancellationToken = default);
}
