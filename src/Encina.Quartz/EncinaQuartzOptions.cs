using Encina.Messaging.Health;

namespace Encina.Quartz;

/// <summary>
/// Configuration options for Encina Quartz integration.
/// </summary>
public sealed class EncinaQuartzOptions
{
    /// <summary>
    /// Gets the provider health check options.
    /// </summary>
    public ProviderHealthCheckOptions ProviderHealthCheck { get; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="QuartzRequestJob{TRequest, TResponse}"/>
    /// places the handler's response on <c>IJobExecutionContext.Result</c> after a successful run.
    /// </summary>
    /// <value>
    /// <c>false</c> (the default) leaves <c>IJobExecutionContext.Result</c> unset.
    /// </value>
    /// <remarks>
    /// <para>
    /// <strong>Opt-in only.</strong> Quartz job listeners, triggers listeners and plugins can read
    /// <c>IJobExecutionContext.Result</c> and store it outside Encina's retention, erasure and
    /// encryption controls. Enable this option only for responses known not to carry personal or
    /// sensitive data. For requests whose response may contain personal data, keep it disabled and
    /// read the outcome through Encina's own stores (outbox, audit trail) instead.
    /// </para>
    /// <para>
    /// This mirrors <c>HangfireRequestJobAdapter.ExecuteAndReturnResultAsync</c>, the explicit
    /// opt-in of the Hangfire integration (#1173).
    /// </para>
    /// </remarks>
    public bool ExposeResponseInJobContext { get; set; }
}
