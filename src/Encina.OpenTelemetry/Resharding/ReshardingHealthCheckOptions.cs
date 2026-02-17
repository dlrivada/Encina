namespace Encina.OpenTelemetry.Resharding;

/// <summary>
/// Configuration options for the <see cref="ReshardingHealthCheck"/>.
/// </summary>
/// <remarks>
/// <para>
/// Controls how the health check classifies resharding operation health:
/// <list type="bullet">
///   <item><description><c>Healthy</c> — No active resharding operations.</description></item>
///   <item><description><c>Degraded</c> — Active resharding with progress within time limit.</description></item>
///   <item><description><c>Unhealthy</c> — Resharding exceeds <see cref="MaxReshardingDuration"/>
///   or is in a <c>Failed</c> state without rollback.</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var options = new ReshardingHealthCheckOptions
/// {
///     MaxReshardingDuration = TimeSpan.FromHours(4),
///     Timeout = TimeSpan.FromSeconds(15)
/// };
/// </code>
/// </example>
public sealed class ReshardingHealthCheckOptions
{
    /// <summary>
    /// Gets or sets the maximum expected duration for a resharding operation.
    /// If an active resharding exceeds this duration, the health check returns <c>Unhealthy</c>.
    /// </summary>
    /// <value>Defaults to 2 hours.</value>
    public TimeSpan MaxReshardingDuration { get; set; } = TimeSpan.FromHours(2);

    /// <summary>
    /// Gets or sets the timeout for the health check query.
    /// </summary>
    /// <value>Defaults to 30 seconds.</value>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
}
