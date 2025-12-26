namespace Encina.Messaging.RoutingSlip;

/// <summary>
/// Configuration options for routing slip processing.
/// </summary>
public sealed class RoutingSlipOptions
{
    /// <summary>
    /// Gets or sets the default timeout for routing slips.
    /// </summary>
    /// <value>Defaults to 30 minutes.</value>
    public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Gets or sets how often to check for stuck routing slips.
    /// </summary>
    /// <value>Defaults to 5 minutes.</value>
    public TimeSpan StuckCheckInterval { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets the threshold for considering a routing slip stuck.
    /// </summary>
    /// <value>Defaults to 10 minutes.</value>
    public TimeSpan StuckThreshold { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Gets or sets the maximum number of routing slips to process in a single batch.
    /// </summary>
    /// <value>Defaults to 100.</value>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets whether to continue compensation on failure.
    /// </summary>
    /// <remarks>
    /// When true, if a compensation step fails, the runner will continue
    /// with the remaining compensation steps. When false, it will stop
    /// immediately and mark the routing slip as failed.
    /// </remarks>
    /// <value>Defaults to true.</value>
    public bool ContinueCompensationOnFailure { get; set; } = true;
}
