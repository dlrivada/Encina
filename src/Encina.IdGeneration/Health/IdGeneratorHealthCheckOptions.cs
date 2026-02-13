namespace Encina.IdGeneration.Health;

/// <summary>
/// Configuration options for the <see cref="IdGeneratorHealthCheck"/>.
/// </summary>
public sealed class IdGeneratorHealthCheckOptions
{
    /// <summary>
    /// Gets or sets the clock drift threshold in milliseconds beyond which
    /// the health check reports <see cref="Messaging.Health.HealthStatus.Degraded"/>.
    /// </summary>
    /// <remarks>
    /// A clock drift value exceeding this threshold typically indicates NTP
    /// synchronization issues that may affect Snowflake ID generation accuracy.
    /// The default value of 500ms is conservative; production systems with
    /// strict ordering requirements may want a lower threshold.
    /// </remarks>
    /// <value>The default threshold is 500ms.</value>
    public long ClockDriftThresholdMs { get; set; } = 500;
}
