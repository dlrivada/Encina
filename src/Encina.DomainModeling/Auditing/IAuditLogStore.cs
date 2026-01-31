namespace Encina.DomainModeling.Auditing;

/// <summary>
/// Defines the contract for storing and retrieving audit log entries.
/// </summary>
/// <remarks>
/// <para>
/// Implementations of this interface provide persistent or in-memory storage
/// for audit trail records. The store is responsible for maintaining the
/// history of all changes made to audited entities.
/// </para>
/// <para>
/// <b>Usage</b>: Register an implementation with the DI container to enable
/// automatic audit logging via <c>AuditInterceptor</c> in EF Core, or use
/// manually with non-EF providers.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register in DI
/// services.AddSingleton&lt;IAuditLogStore, InMemoryAuditLogStore&gt;();
///
/// // Or use a database-backed implementation
/// services.AddScoped&lt;IAuditLogStore, SqlAuditLogStore&gt;();
///
/// // Query history
/// var store = serviceProvider.GetRequiredService&lt;IAuditLogStore&gt;();
/// var history = await store.GetHistoryAsync("Order", orderId.ToString());
/// </code>
/// </example>
public interface IAuditLogStore
{
    /// <summary>
    /// Logs an audit entry to the store.
    /// </summary>
    /// <param name="entry">The audit log entry to store.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entry"/> is <c>null</c>.</exception>
    Task LogAsync(AuditLogEntry entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the audit history for a specific entity.
    /// </summary>
    /// <param name="entityType">The type name of the entity (e.g., "Order").</param>
    /// <param name="entityId">The string representation of the entity's primary key.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// A task containing the collection of audit log entries for the specified entity,
    /// ordered by <see cref="AuditLogEntry.TimestampUtc"/> descending (most recent first).
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entityType"/> or <paramref name="entityId"/> is <c>null</c>.</exception>
    Task<IEnumerable<AuditLogEntry>> GetHistoryAsync(
        string entityType,
        string entityId,
        CancellationToken cancellationToken = default);
}
