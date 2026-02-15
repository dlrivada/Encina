namespace Encina.Sharding.ReferenceTables.Health;

/// <summary>
/// Configuration options for the reference table replication health check.
/// </summary>
/// <remarks>
/// <para>
/// The health check compares each registered table's last replication time against
/// these thresholds to determine health status:
/// <list type="bullet">
///   <item><description><b>Healthy</b>: All tables replicated within the degraded threshold.</description></item>
///   <item><description><b>Degraded</b>: Some tables replicated later than the degraded threshold
///   but within the unhealthy threshold.</description></item>
///   <item><description><b>Unhealthy</b>: Some tables have not been replicated within the
///   unhealthy threshold (or have never been replicated).</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class ReferenceTableHealthCheckOptions
{
    /// <summary>
    /// Gets or sets the time threshold after which a reference table is considered unhealthy.
    /// </summary>
    /// <value>Default: 5 minutes.</value>
    public TimeSpan UnhealthyThreshold { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets the time threshold after which a reference table is considered degraded.
    /// </summary>
    /// <value>Default: 1 minute.</value>
    public TimeSpan DegradedThreshold { get; set; } = TimeSpan.FromMinutes(1);
}
