namespace Encina.Messaging.ReadWriteSeparation;

/// <summary>
/// Specifies the intended database operation type for routing purposes.
/// </summary>
/// <remarks>
/// <para>
/// This enum is used by the <see cref="DatabaseRoutingContext"/> to determine
/// whether a request should be routed to the primary (write) database or a
/// read replica.
/// </para>
/// <para>
/// The routing pipeline behavior automatically sets the intent based on the
/// request type (ICommand vs IQuery) and the presence of the
/// <see cref="ForceWriteDatabaseAttribute"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Automatic routing based on request type
/// // ICommand → DatabaseIntent.Write
/// // IQuery → DatabaseIntent.Read
///
/// // Manual override for specific scenarios
/// using (var scope = new DatabaseRoutingScope(DatabaseIntent.ForceWrite))
/// {
///     // All operations in this scope use the primary database
///     var result = await _repository.GetByIdAsync(id);
/// }
/// </code>
/// </example>
public enum DatabaseIntent
{
    /// <summary>
    /// Indicates a write operation that must use the primary database.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Commands (implementing <c>ICommand</c>) automatically use this intent.
    /// All INSERT, UPDATE, and DELETE operations should use this intent.
    /// </para>
    /// </remarks>
    Write = 0,

    /// <summary>
    /// Indicates a read operation that can use a read replica.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Queries (implementing <c>IQuery</c>) automatically use this intent unless
    /// marked with <see cref="ForceWriteDatabaseAttribute"/>.
    /// </para>
    /// <para>
    /// Read replicas may have replication lag, so this intent is only suitable
    /// for operations that can tolerate eventual consistency.
    /// </para>
    /// </remarks>
    Read = 1,

    /// <summary>
    /// Forces a read operation to use the primary database for consistency.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use this intent when a query requires read-after-write consistency,
    /// such as reading data immediately after a write operation. This ensures
    /// the query sees the latest committed data without replication lag.
    /// </para>
    /// <para>
    /// Queries marked with <see cref="ForceWriteDatabaseAttribute"/> automatically
    /// use this intent. It can also be set manually using <see cref="DatabaseRoutingScope"/>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Example: Read immediately after write
    /// await _repository.CreateAsync(newEntity);
    ///
    /// // Use ForceWrite to ensure we read from primary
    /// using (var scope = new DatabaseRoutingScope(DatabaseIntent.ForceWrite))
    /// {
    ///     var created = await _repository.GetByIdAsync(newEntity.Id);
    ///     // 'created' is guaranteed to reflect the write we just made
    /// }
    /// </code>
    /// </example>
    ForceWrite = 2
}
