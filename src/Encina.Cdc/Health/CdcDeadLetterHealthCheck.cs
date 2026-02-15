using Encina.Cdc.DeadLetter;
using Encina.Cdc.DeadLetter.Diagnostics;
using Encina.Messaging.Health;

namespace Encina.Cdc.Health;

/// <summary>
/// Health check that monitors the CDC dead letter queue by reporting the number
/// of pending entries against configurable warning and critical thresholds.
/// </summary>
/// <remarks>
/// <para>
/// Status is determined by comparing the pending entry count against the configured
/// thresholds in <see cref="CdcDeadLetterHealthCheckOptions"/>:
/// <list type="bullet">
///   <item><description><b>Healthy</b>: Pending count ≤ warning threshold</description></item>
///   <item><description><b>Degraded</b>: Pending count &gt; warning threshold but ≤ critical threshold</description></item>
///   <item><description><b>Unhealthy</b>: Pending count &gt; critical threshold</description></item>
/// </list>
/// </para>
/// <para>
/// Tags: "encina", "cdc", "dead-letter", "ready" for Kubernetes readiness probes.
/// </para>
/// </remarks>
internal sealed class CdcDeadLetterHealthCheck : EncinaHealthCheck
{
    private static readonly string[] DefaultTags = ["encina", "cdc", "dead-letter", "ready"];

    private readonly ICdcDeadLetterStore _store;
    private readonly CdcDeadLetterHealthCheckOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="CdcDeadLetterHealthCheck"/> class.
    /// </summary>
    /// <param name="store">The dead letter store to query for pending entries.</param>
    /// <param name="options">Threshold configuration for health status determination.</param>
    public CdcDeadLetterHealthCheck(
        ICdcDeadLetterStore store,
        CdcDeadLetterHealthCheckOptions options)
        : base("encina-cdc-dead-letter", DefaultTags)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(options);

        _store = store;
        _options = options;
    }

    /// <inheritdoc />
    protected override async Task<HealthCheckResult> CheckHealthCoreAsync(CancellationToken cancellationToken)
    {
        // Query a batch large enough to determine threshold status.
        // We only need to know if the count exceeds the critical threshold.
        var maxQuery = _options.CriticalThreshold + 1;
        var result = await _store.GetPendingAsync(maxQuery, cancellationToken).ConfigureAwait(false);

        return result.Match(
            Right: pending =>
            {
                var pendingCount = pending.Count;

                // Update the observable gauge with the current pending count
                CdcDeadLetterMetrics.UpdatePendingCount(pendingCount);

                var data = new Dictionary<string, object>
                {
                    ["pending_count"] = pendingCount,
                    ["warning_threshold"] = _options.WarningThreshold,
                    ["critical_threshold"] = _options.CriticalThreshold
                };

                if (pendingCount > _options.CriticalThreshold)
                {
                    return HealthCheckResult.Unhealthy(
                        $"CDC dead letter queue has {pendingCount} pending entries (critical threshold: {_options.CriticalThreshold})",
                        data: data);
                }

                if (pendingCount > _options.WarningThreshold)
                {
                    return HealthCheckResult.Degraded(
                        $"CDC dead letter queue has {pendingCount} pending entries (warning threshold: {_options.WarningThreshold})",
                        data: data);
                }

                return HealthCheckResult.Healthy(
                    $"CDC dead letter queue has {pendingCount} pending entries",
                    data: data);
            },
            Left: error =>
            {
                var data = new Dictionary<string, object>
                {
                    ["store_error"] = error.ToString()
                };

                return HealthCheckResult.Unhealthy(
                    "Failed to query CDC dead letter store",
                    data: data);
            });
    }
}
