namespace Encina.Database;

/// <summary>
/// Represents a snapshot of connection pool statistics for a database provider.
/// </summary>
/// <remarks>
/// <para>
/// This immutable record provides a point-in-time view of the connection pool state.
/// Different providers may expose varying levels of detail; providers with limited
/// statistics support should use <see cref="CreateEmpty"/> to return a zero-valued snapshot.
/// </para>
/// <para>
/// The <see cref="PoolUtilization"/> property is computed as the ratio of
/// <see cref="TotalConnections"/> to <see cref="MaxPoolSize"/>, clamped to [0, 1].
/// When <see cref="MaxPoolSize"/> is zero, utilization is reported as 0.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var stats = monitor.GetPoolStatistics();
/// if (stats.PoolUtilization > 0.9)
/// {
///     logger.LogWarning("Connection pool nearing capacity: {Utilization:P0}", stats.PoolUtilization);
/// }
/// </code>
/// </example>
/// <param name="ActiveConnections">The number of connections currently in use.</param>
/// <param name="IdleConnections">The number of connections available in the pool.</param>
/// <param name="TotalConnections">The total number of connections (active + idle).</param>
/// <param name="PendingRequests">The number of requests waiting for a connection.</param>
/// <param name="MaxPoolSize">The maximum number of connections allowed in the pool.</param>
public sealed record ConnectionPoolStats(
    int ActiveConnections,
    int IdleConnections,
    int TotalConnections,
    int PendingRequests,
    int MaxPoolSize)
{
    /// <summary>
    /// Gets the pool utilization as a ratio between 0 and 1.
    /// </summary>
    /// <remarks>
    /// Calculated as <c>TotalConnections / MaxPoolSize</c>, clamped to [0, 1].
    /// Returns 0 when <see cref="MaxPoolSize"/> is zero.
    /// </remarks>
    /// <value>A value between 0.0 (empty) and 1.0 (fully utilized).</value>
    public double PoolUtilization => MaxPoolSize > 0
        ? Math.Clamp((double)TotalConnections / MaxPoolSize, 0.0, 1.0)
        : 0.0;

    /// <summary>
    /// Creates an empty statistics snapshot with all values set to zero.
    /// </summary>
    /// <remarks>
    /// Use this factory method for providers that do not expose pool statistics
    /// (e.g., SQLite in-memory databases) to return a valid, zero-valued result
    /// instead of throwing.
    /// </remarks>
    /// <returns>A <see cref="ConnectionPoolStats"/> instance with all properties set to zero.</returns>
    public static ConnectionPoolStats CreateEmpty() => new(0, 0, 0, 0, 0);
}
