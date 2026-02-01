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
/// var entries = await auditStore.GetByUserAsync("user-123", DateTime.UtcNow.AddDays(-7), null, cancellationToken);
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
}
