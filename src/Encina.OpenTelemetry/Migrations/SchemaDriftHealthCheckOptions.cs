namespace Encina.OpenTelemetry.Migrations;

/// <summary>
/// Configuration options for the <see cref="SchemaDriftHealthCheck"/>.
/// </summary>
/// <remarks>
/// <para>
/// Controls the health check timeout, baseline shard selection, and which tables
/// are considered critical for determining <c>Unhealthy</c> vs <c>Degraded</c> status.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var options = new SchemaDriftHealthCheckOptions
/// {
///     Timeout = TimeSpan.FromSeconds(30),
///     CriticalTables = ["orders", "customers", "payments"]
/// };
/// </code>
/// </example>
public sealed class SchemaDriftHealthCheckOptions
{
    /// <summary>
    /// Gets or sets the timeout for the drift detection health check.
    /// </summary>
    /// <value>Defaults to 30 seconds.</value>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the shard ID to use as the baseline for drift comparison.
    /// When <see langword="null"/>, the coordinator selects the first active shard.
    /// </summary>
    public string? BaselineShardId { get; set; }

    /// <summary>
    /// Gets or sets the list of critical table names. Drift on these tables causes
    /// the health check to return <c>Unhealthy</c>; drift on other tables returns <c>Degraded</c>.
    /// </summary>
    /// <value>Defaults to an empty list (all drift is considered degraded, not unhealthy).</value>
    public IList<string> CriticalTables { get; set; } = [];
}
