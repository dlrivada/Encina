namespace Encina.Sharding.ReplicaSelection;

/// <summary>
/// Configuration options for replica staleness tolerance during read operations.
/// </summary>
/// <remarks>
/// <para>
/// These options control how observed replication lag affects replica selection.
/// When a replica's lag exceeds the configured threshold, it is excluded from selection
/// and the request falls back to the primary (if <see cref="FallbackToPrimaryWhenStale"/>
/// is enabled) or returns an error.
/// </para>
/// <para>
/// Staleness checking is disabled by default (<see cref="MaxAcceptableReplicationLag"/> is
/// <see langword="null"/>). Enable it by setting a maximum lag value.
/// </para>
/// <para>
/// Per-query overrides are supported via the <see cref="AcceptStaleReadsAttribute"/>,
/// which allows individual query handlers to declare higher (or lower) staleness tolerance
/// than the global threshold.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var options = new StalenessOptions
/// {
///     MaxAcceptableReplicationLag = TimeSpan.FromSeconds(5),
///     FallbackToPrimaryWhenStale = true,
/// };
/// </code>
/// </example>
public sealed class StalenessOptions
{
    /// <summary>
    /// Gets or sets the maximum acceptable replication lag before a replica is considered stale.
    /// </summary>
    /// <remarks>
    /// When set, replicas with observed replication lag exceeding this value are excluded
    /// from selection. Set to <see langword="null"/> to disable staleness checking entirely.
    /// </remarks>
    /// <value>Default: <see langword="null"/> (staleness checking disabled).</value>
    public TimeSpan? MaxAcceptableReplicationLag { get; set; }

    /// <summary>
    /// Gets or sets whether to fall back to the primary when all replicas are stale.
    /// </summary>
    /// <value>Default: <see langword="true"/>.</value>
    public bool FallbackToPrimaryWhenStale { get; set; } = true;

    /// <summary>
    /// Gets a value indicating whether staleness checking is enabled.
    /// </summary>
    public bool IsEnabled => MaxAcceptableReplicationLag.HasValue;
}
