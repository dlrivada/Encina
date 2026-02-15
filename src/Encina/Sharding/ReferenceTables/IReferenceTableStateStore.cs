namespace Encina.Sharding.ReferenceTables;

/// <summary>
/// Persists the last-known content hash for each reference table,
/// enabling the <see cref="PollingRefreshDetector"/> to detect changes
/// between polling cycles.
/// </summary>
/// <remarks>
/// <para>
/// Each polling cycle, the detector computes the current hash on the primary shard
/// and compares it to the value stored here. When they differ, the detector triggers
/// replication. After a successful replication, the new hash is saved.
/// </para>
/// <para>
/// The default in-memory implementation is suitable for single-instance deployments.
/// Distributed deployments should provide a durable implementation (e.g., database-backed)
/// to avoid unnecessary full replications after application restarts.
/// </para>
/// </remarks>
public interface IReferenceTableStateStore
{
    /// <summary>
    /// Gets the last-known content hash for a reference table entity type.
    /// </summary>
    /// <param name="entityType">The entity type of the reference table.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The stored hash, or <c>null</c> if no hash has been recorded yet.</returns>
    Task<string?> GetLastHashAsync(Type entityType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the current content hash for a reference table entity type.
    /// </summary>
    /// <param name="entityType">The entity type of the reference table.</param>
    /// <param name="hash">The content hash to store.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task SaveHashAsync(Type entityType, string hash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the last successful replication time for a reference table entity type.
    /// </summary>
    /// <param name="entityType">The entity type of the reference table.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The UTC timestamp of the last successful replication, or <c>null</c> if never replicated.</returns>
    Task<DateTime?> GetLastReplicationTimeAsync(Type entityType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the last successful replication time for a reference table entity type.
    /// </summary>
    /// <param name="entityType">The entity type of the reference table.</param>
    /// <param name="timeUtc">The UTC timestamp of the successful replication.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task SaveReplicationTimeAsync(Type entityType, DateTime timeUtc, CancellationToken cancellationToken = default);
}
