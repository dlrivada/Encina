using LanguageExt;

namespace Encina.Security.Audit;

/// <summary>
/// Abstraction for audit trail storage and retrieval.
/// </summary>
/// <remarks>
/// <para>
/// Implementations must be thread-safe for concurrent audit recording.
/// For production use, consider persistent stores like SQL databases or specialized
/// audit logging services.
/// </para>
/// <para>
/// All methods return <c>Either&lt;EncinaError, T&gt;</c> following the Encina
/// functional error handling pattern.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Recording an audit entry
/// var result = await auditStore.RecordAsync(entry, cancellationToken);
/// result.Match(
///     Right: _ => logger.LogDebug("Audit entry recorded"),
///     Left: error => logger.LogWarning("Failed to record audit: {Message}", error.Message)
/// );
///
/// // Querying audit entries
/// var entries = await auditStore.GetByUserAsync("user-123", TimeProvider.System.GetUtcNow().UtcDateTime.AddDays(-7), null, cancellationToken);
/// </code>
/// </example>
public interface IAuditStore
{
    /// <summary>
    /// Records a new audit entry.
    /// </summary>
    /// <param name="entry">The audit entry to record.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// <c>Either.Right(Unit)</c> on success, or <c>Either.Left(EncinaError)</c> on failure.
    /// </returns>
    /// <remarks>
    /// Implementations should handle duplicate IDs gracefully (e.g., update or reject).
    /// Recording failures should not affect the original request processing.
    /// </remarks>
    ValueTask<Either<EncinaError, Unit>> RecordAsync(AuditEntry entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves audit entries for a specific entity.
    /// </summary>
    /// <param name="entityType">The type of entity to query (e.g., "Order", "Customer").</param>
    /// <param name="entityId">The specific entity identifier, or <c>null</c> to get all entries for the entity type.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// <c>Either.Right</c> with the matching entries ordered by timestamp descending,
    /// or <c>Either.Left(EncinaError)</c> on failure.
    /// </returns>
    /// <remarks>
    /// Returns an empty list (not an error) when no entries match the criteria.
    /// </remarks>
    ValueTask<Either<EncinaError, IReadOnlyList<AuditEntry>>> GetByEntityAsync(
        string entityType,
        string? entityId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves audit entries for a specific user within an optional date range.
    /// </summary>
    /// <param name="userId">The user identifier to query.</param>
    /// <param name="fromUtc">Optional start date (inclusive). <c>null</c> for no lower bound.</param>
    /// <param name="toUtc">Optional end date (inclusive). <c>null</c> for no upper bound.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// <c>Either.Right</c> with the matching entries ordered by timestamp descending,
    /// or <c>Either.Left(EncinaError)</c> on failure.
    /// </returns>
    /// <remarks>
    /// Returns an empty list (not an error) when no entries match the criteria.
    /// </remarks>
    ValueTask<Either<EncinaError, IReadOnlyList<AuditEntry>>> GetByUserAsync(
        string userId,
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all audit entries associated with a specific correlation ID.
    /// </summary>
    /// <param name="correlationId">The correlation ID to query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// <c>Either.Right</c> with the matching entries ordered by timestamp ascending,
    /// or <c>Either.Left(EncinaError)</c> on failure.
    /// </returns>
    /// <remarks>
    /// Useful for tracing a complete request chain across multiple operations.
    /// Returns an empty list (not an error) when no entries match the correlation ID.
    /// </remarks>
    ValueTask<Either<EncinaError, IReadOnlyList<AuditEntry>>> GetByCorrelationIdAsync(
        string correlationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries audit entries with flexible filtering and pagination.
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
    /// Results are ordered by <see cref="AuditEntry.TimestampUtc"/> descending (newest first).
    /// </para>
    /// <para>
    /// Returns an empty <see cref="PagedResult{T}"/> (not an error) when no entries match the query.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var query = AuditQuery.Builder()
    ///     .ForUser("user-123")
    ///     .WithOutcome(AuditOutcome.Failure)
    ///     .InDateRange(TimeProvider.System.GetUtcNow().UtcDateTime.AddDays(-7), null)
    ///     .WithPageSize(100)
    ///     .Build();
    ///
    /// var result = await auditStore.QueryAsync(query, cancellationToken);
    /// </code>
    /// </example>
    ValueTask<Either<EncinaError, PagedResult<AuditEntry>>> QueryAsync(
        AuditQuery query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Purges audit entries older than the specified date.
    /// </summary>
    /// <param name="olderThanUtc">Entries with <see cref="AuditEntry.TimestampUtc"/> before this date will be deleted.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// <c>Either.Right</c> with the number of entries purged,
    /// or <c>Either.Left(EncinaError)</c> on failure.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Use this method to implement data retention policies. Typically called from a background
    /// service at regular intervals (e.g., daily) to remove entries older than
    /// <see cref="AuditOptions.RetentionDays"/>.
    /// </para>
    /// <para>
    /// This operation may be slow for large datasets. Consider running during off-peak hours.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Purge entries older than 7 years (2555 days)
    /// var retentionDate = TimeProvider.System.GetUtcNow().UtcDateTime.AddDays(-options.Value.RetentionDays);
    /// var result = await auditStore.PurgeEntriesAsync(retentionDate, cancellationToken);
    ///
    /// result.Match(
    ///     Right: count => logger.LogInformation("Purged {Count} audit entries", count),
    ///     Left: error => logger.LogError("Failed to purge audit entries: {Message}", error.Message)
    /// );
    /// </code>
    /// </example>
    ValueTask<Either<EncinaError, int>> PurgeEntriesAsync(
        DateTime olderThanUtc,
        CancellationToken cancellationToken = default);
}
