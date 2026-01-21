using System.Collections.Concurrent;

namespace Encina.Messaging.ReadWriteSeparation;

/// <summary>
/// Selects the read replica with the fewest active connections.
/// </summary>
/// <remarks>
/// <para>
/// This selector tracks the number of active connections to each replica and routes
/// new requests to the replica with the lowest current load. This provides adaptive
/// load balancing that responds to varying query execution times and replica capacities.
/// </para>
/// <para>
/// <b>Usage Pattern:</b>
/// Callers should use <see cref="AcquireReplica"/> instead of <see cref="SelectReplica"/>
/// to properly track connection counts. The returned <see cref="ReplicaLease"/> must be
/// disposed when the connection is released to decrement the counter.
/// </para>
/// <para>
/// This strategy adds some overhead compared to round-robin or random selection,
/// but provides better load distribution when replicas have different performance
/// characteristics or when query execution times vary significantly.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var replicas = new List&lt;string&gt;
/// {
///     "Server=replica1;...",
///     "Server=replica2;..."
/// };
/// var selector = new LeastConnectionsReplicaSelector(replicas);
///
/// // Recommended: Use AcquireReplica for proper connection tracking
/// using (var lease = selector.AcquireReplica())
/// {
///     // Use lease.ConnectionString to create connection
///     // Connection count is automatically decremented when lease is disposed
/// }
///
/// // Alternative: SelectReplica without tracking (use when you manage counts manually)
/// var connectionString = selector.SelectReplica();
/// // ... use connection ...
/// selector.ReleaseReplica(connectionString); // Manual release
/// </code>
/// </example>
public sealed class LeastConnectionsReplicaSelector : IReplicaSelector
{
    private readonly IReadOnlyList<string> _replicas;
    private readonly ConcurrentDictionary<string, int> _connectionCounts;
    private readonly object _selectionLock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="LeastConnectionsReplicaSelector"/> class.
    /// </summary>
    /// <param name="replicas">The list of read replica connection strings.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="replicas"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="replicas"/> is empty.
    /// </exception>
    public LeastConnectionsReplicaSelector(IReadOnlyList<string> replicas)
    {
        ArgumentNullException.ThrowIfNull(replicas);

        if (replicas.Count == 0)
        {
            throw new ArgumentException("At least one replica connection string must be provided.", nameof(replicas));
        }

        _replicas = replicas;
        _connectionCounts = new ConcurrentDictionary<string, int>(
            replicas.Select(r => new KeyValuePair<string, int>(r, 0)));
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// Selects the replica with the lowest connection count. If multiple replicas
    /// have the same count, the first one encountered is selected.
    /// </para>
    /// <para>
    /// <b>Important:</b> This method does NOT automatically increment the connection count.
    /// For automatic tracking, use <see cref="AcquireReplica"/> instead, or call
    /// <see cref="ReleaseReplica"/> when the connection is closed.
    /// </para>
    /// </remarks>
    public string SelectReplica()
    {
        lock (_selectionLock)
        {
            var minConnections = int.MaxValue;
            string? selectedReplica = null;

            foreach (var replica in _replicas)
            {
                var count = _connectionCounts.GetValueOrDefault(replica, 0);
                if (count < minConnections)
                {
                    minConnections = count;
                    selectedReplica = replica;
                }
            }

            // Increment the count for the selected replica
            if (selectedReplica is not null)
            {
                _connectionCounts.AddOrUpdate(selectedReplica, 1, (_, current) => current + 1);
            }

            return selectedReplica ?? _replicas[0];
        }
    }

    /// <summary>
    /// Acquires a replica lease that automatically tracks connection count.
    /// </summary>
    /// <returns>
    /// A <see cref="ReplicaLease"/> containing the selected replica connection string.
    /// The lease must be disposed to release the connection.
    /// </returns>
    /// <remarks>
    /// This is the recommended method for selecting replicas when using the
    /// least connections strategy, as it ensures proper connection tracking.
    /// </remarks>
    /// <example>
    /// <code>
    /// using (var lease = selector.AcquireReplica())
    /// {
    ///     var connection = new SqlConnection(lease.ConnectionString);
    ///     // Use connection...
    /// } // Connection count is automatically decremented
    /// </code>
    /// </example>
    public ReplicaLease AcquireReplica()
    {
        var connectionString = SelectReplica();
        return new ReplicaLease(connectionString, this);
    }

    /// <summary>
    /// Releases a previously acquired replica connection.
    /// </summary>
    /// <param name="connectionString">The connection string of the replica to release.</param>
    /// <remarks>
    /// Call this method when a connection is closed to decrement the connection count.
    /// If using <see cref="AcquireReplica"/>, this is called automatically when the
    /// <see cref="ReplicaLease"/> is disposed.
    /// </remarks>
    public void ReleaseReplica(string connectionString)
    {
        ArgumentNullException.ThrowIfNull(connectionString);

        _connectionCounts.AddOrUpdate(
            connectionString,
            0,
            (_, current) => Math.Max(0, current - 1));
    }

    /// <summary>
    /// Gets the current connection count for a specific replica.
    /// </summary>
    /// <param name="connectionString">The replica connection string.</param>
    /// <returns>The current number of active connections to the replica.</returns>
    /// <remarks>
    /// This method is useful for monitoring and diagnostics.
    /// </remarks>
    public int GetConnectionCount(string connectionString)
    {
        ArgumentNullException.ThrowIfNull(connectionString);
        return _connectionCounts.GetValueOrDefault(connectionString, 0);
    }

    /// <summary>
    /// Gets the current connection counts for all replicas.
    /// </summary>
    /// <returns>
    /// A read-only dictionary mapping replica connection strings to their current connection counts.
    /// </returns>
    /// <remarks>
    /// This method is useful for monitoring and diagnostics to understand
    /// the current load distribution across replicas.
    /// </remarks>
    public IReadOnlyDictionary<string, int> GetAllConnectionCounts()
    {
        return _connectionCounts.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    /// <summary>
    /// Represents a lease for a replica connection that automatically releases
    /// the connection when disposed.
    /// </summary>
    /// <remarks>
    /// Use this struct with a <c>using</c> statement to ensure proper connection
    /// count tracking with the <see cref="LeastConnectionsReplicaSelector"/>.
    /// </remarks>
    public readonly struct ReplicaLease : IDisposable
    {
        private readonly LeastConnectionsReplicaSelector _selector;

        /// <summary>
        /// Gets the connection string for the leased replica.
        /// </summary>
        public string ConnectionString { get; }

        internal ReplicaLease(string connectionString, LeastConnectionsReplicaSelector selector)
        {
            ConnectionString = connectionString;
            _selector = selector;
        }

        /// <summary>
        /// Releases the replica lease, decrementing the connection count.
        /// </summary>
        public void Dispose()
        {
            _selector.ReleaseReplica(ConnectionString);
        }
    }
}
