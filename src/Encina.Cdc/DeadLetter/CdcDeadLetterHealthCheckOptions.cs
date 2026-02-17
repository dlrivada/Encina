namespace Encina.Cdc.DeadLetter;

/// <summary>
/// Configuration options for the CDC dead letter queue health check.
/// Controls the thresholds at which the health check reports degraded
/// or unhealthy status based on the number of pending dead letter entries.
/// </summary>
public sealed class CdcDeadLetterHealthCheckOptions
{
    /// <summary>
    /// Gets or sets the warning threshold for pending dead letter entries.
    /// When the pending count exceeds this value, the health check reports
    /// <see cref="Messaging.Health.HealthStatus.Degraded"/>.
    /// Default is 10.
    /// </summary>
    public int WarningThreshold { get; set; } = 10;

    /// <summary>
    /// Gets or sets the critical threshold for pending dead letter entries.
    /// When the pending count exceeds this value, the health check reports
    /// <see cref="Messaging.Health.HealthStatus.Unhealthy"/>.
    /// Default is 100.
    /// </summary>
    public int CriticalThreshold { get; set; } = 100;
}
