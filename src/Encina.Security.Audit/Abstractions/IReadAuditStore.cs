using LanguageExt;

namespace Encina.Security.Audit;

/// <summary>
/// Abstraction for read audit trail storage and retrieval.
/// </summary>
/// <remarks>
/// <para>
/// Tracks read access to sensitive entities marked with <c>IReadAuditable</c>.
/// This is separate from <see cref="IAuditStore"/> which handles CUD (Create, Update, Delete)
/// operations at the CQRS pipeline level.
/// </para>
/// <para>
/// Implementations must be thread-safe for concurrent audit recording.
/// For production use, consider persistent stores like SQL databases or specialized
/// audit logging services.
/// </para>
/// <para>
/// All methods return <c>Either&lt;EncinaError, T&gt;</c> following the Encina
/// functional error handling pattern.
/// </para>
/// <para>
/// Supports compliance requirements across multiple regulations:
/// <list type="bullet">
/// <item><b>GDPR Art. 15</b> — Right of access: track who viewed personal data and why</item>
/// <item><b>HIPAA §164.312(b)</b> — Audit controls: record access to electronic protected health information</item>
/// <item><b>SOX §302/§404</b> — Internal controls: track access to financial records</item>
/// <item><b>PCI-DSS Req. 10.2</b> — Logging: monitor access to cardholder data environments</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Recording a read audit entry
/// var entry = new ReadAuditEntry
/// {
///     Id = Guid.NewGuid(),
///     EntityType = "Patient",
///     EntityId = "PAT-12345",
///     UserId = context.UserId,
///     TenantId = context.TenantId,
///     AccessedAtUtc = timeProvider.GetUtcNow(),
///     CorrelationId = context.CorrelationId,
///     Purpose = "Patient care review",
///     AccessMethod = ReadAccessMethod.Repository,
///     EntityCount = 1
/// };
///
/// var result = await readAuditStore.LogReadAsync(entry, cancellationToken);
///
/// // Querying access history for compliance
/// var query = ReadAuditQuery.Builder()
///     .ForEntity("Patient", "PAT-12345")
///     .InDateRange(DateTimeOffset.UtcNow.AddDays(-30), DateTimeOffset.UtcNow)
///     .Build();
///
/// var history = await readAuditStore.QueryAsync(query, cancellationToken);
/// </code>
/// </example>
public interface IReadAuditStore
{
    /// <summary>
    /// Records a new read audit entry.
    /// </summary>
    /// <param name="entry">The read audit entry to record.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// <c>Either.Right(Unit)</c> on success, or <c>Either.Left(EncinaError)</c> on failure.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Implementations should handle duplicate IDs gracefully (e.g., update or reject).
    /// Recording failures should not affect the original read operation — the repository
    /// decorator uses fire-and-forget semantics to ensure audit failures never block reads.
    /// </para>
    /// </remarks>
    ValueTask<Either<EncinaError, Unit>> LogReadAsync(
        ReadAuditEntry entry,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves read audit entries for a specific entity.
    /// </summary>
    /// <param name="entityType">The type of entity to query (e.g., "Patient", "FinancialRecord").</param>
    /// <param name="entityId">The specific entity identifier to query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// <c>Either.Right</c> with the matching entries ordered by <see cref="ReadAuditEntry.AccessedAtUtc"/> descending,
    /// or <c>Either.Left(EncinaError)</c> on failure.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Returns an empty list (not an error) when no entries match the criteria.
    /// Answers the GDPR Art. 15 question: "Who accessed this entity's data?"
    /// </para>
    /// </remarks>
    ValueTask<Either<EncinaError, IReadOnlyList<ReadAuditEntry>>> GetAccessHistoryAsync(
        string entityType,
        string entityId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves read audit entries for a specific user within a date range.
    /// </summary>
    /// <param name="userId">The user identifier to query.</param>
    /// <param name="fromUtc">Start of the date range (inclusive).</param>
    /// <param name="toUtc">End of the date range (inclusive).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// <c>Either.Right</c> with the matching entries ordered by <see cref="ReadAuditEntry.AccessedAtUtc"/> descending,
    /// or <c>Either.Left(EncinaError)</c> on failure.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Returns an empty list (not an error) when no entries match the criteria.
    /// Answers the HIPAA question: "What did this user access in this time period?"
    /// </para>
    /// </remarks>
    ValueTask<Either<EncinaError, IReadOnlyList<ReadAuditEntry>>> GetUserAccessHistoryAsync(
        string userId,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries read audit entries with flexible filtering and pagination.
    /// </summary>
    /// <param name="query">The query parameters for filtering and pagination.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// <c>Either.Right</c> with a paginated result set,
    /// or <c>Either.Left(EncinaError)</c> on failure.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This is the primary query method that supports all filtering options.
    /// Results are ordered by <see cref="ReadAuditEntry.AccessedAtUtc"/> descending (newest first).
    /// </para>
    /// <para>
    /// Returns an empty <see cref="PagedResult{T}"/> (not an error) when no entries match the query.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var query = ReadAuditQuery.Builder()
    ///     .ForUser("user-123")
    ///     .WithAccessMethod(ReadAccessMethod.Export)
    ///     .InDateRange(DateTimeOffset.UtcNow.AddDays(-7), DateTimeOffset.UtcNow)
    ///     .WithPageSize(100)
    ///     .Build();
    ///
    /// var result = await readAuditStore.QueryAsync(query, cancellationToken);
    /// </code>
    /// </example>
    ValueTask<Either<EncinaError, PagedResult<ReadAuditEntry>>> QueryAsync(
        ReadAuditQuery query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Purges read audit entries older than the specified date.
    /// </summary>
    /// <param name="olderThanUtc">Entries with <see cref="ReadAuditEntry.AccessedAtUtc"/> before this date will be deleted.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// <c>Either.Right</c> with the number of entries purged,
    /// or <c>Either.Left(EncinaError)</c> on failure.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Use this method to implement data retention policies. Typically called from a background
    /// service at regular intervals (e.g., daily) to remove entries older than
    /// <see cref="ReadAuditOptions.RetentionDays"/>.
    /// </para>
    /// <para>
    /// This operation may be slow for large datasets. Consider running during off-peak hours.
    /// </para>
    /// </remarks>
    ValueTask<Either<EncinaError, int>> PurgeEntriesAsync(
        DateTimeOffset olderThanUtc,
        CancellationToken cancellationToken = default);
}
