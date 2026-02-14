namespace Encina.Sharding.ReplicaSelection;

/// <summary>
/// Declares that a query handler tolerates stale reads up to the specified replication lag.
/// </summary>
/// <remarks>
/// <para>
/// Apply this attribute to query handler classes or methods to indicate that the query
/// can tolerate reading from a replica even when the observed replication lag exceeds
/// the global <see cref="ShardedReadWriteOptions.MaxAcceptableReplicationLag"/> threshold.
/// </para>
/// <para>
/// When the attribute is present, the replica selection logic uses the per-query
/// <see cref="MaxLagMilliseconds"/> instead of the global threshold, allowing stale-tolerant
/// queries to continue using replicas while strict queries fall back to the primary.
/// </para>
/// <para>
/// A value of <c>0</c> means the query requires fully up-to-date data (equivalent to
/// not using replicas). Use <see cref="int.MaxValue"/> (or a very large value) to accept
/// any amount of lag.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Accept up to 5 seconds of replication lag
/// [AcceptStaleReads(5000)]
/// public class DashboardStatsQueryHandler : IQueryHandler&lt;DashboardStatsQuery, DashboardStats&gt;
/// {
///     public async Task&lt;Either&lt;EncinaError, DashboardStats&gt;&gt; HandleAsync(
///         DashboardStatsQuery query, CancellationToken ct)
///     {
///         // This query tolerates up to 5 s of lag
///         return await _repo.GetStatsAsync(ct);
///     }
/// }
///
/// // Accept any staleness — best-effort reads
/// [AcceptStaleReads(int.MaxValue)]
/// public class CachedCatalogQueryHandler : IQueryHandler&lt;CatalogQuery, Catalog&gt;
/// {
///     // ...
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class AcceptStaleReadsAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AcceptStaleReadsAttribute"/> class.
    /// </summary>
    /// <param name="maxLagMilliseconds">
    /// The maximum acceptable replication lag in milliseconds. Replicas with observed lag
    /// below this threshold are eligible for selection.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="maxLagMilliseconds"/> is negative.
    /// </exception>
    public AcceptStaleReadsAttribute(int maxLagMilliseconds)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maxLagMilliseconds);
        MaxLagMilliseconds = maxLagMilliseconds;
    }

    /// <summary>
    /// Gets the maximum acceptable replication lag in milliseconds.
    /// </summary>
    public int MaxLagMilliseconds { get; }

    /// <summary>
    /// Gets the maximum acceptable replication lag as a <see cref="TimeSpan"/>.
    /// </summary>
    public TimeSpan MaxLag => TimeSpan.FromMilliseconds(MaxLagMilliseconds);
}
