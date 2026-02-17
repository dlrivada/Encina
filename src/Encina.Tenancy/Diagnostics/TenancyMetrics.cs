using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Encina.Tenancy.Diagnostics;

/// <summary>
/// Exposes multi-tenancy metrics via the <c>Encina</c> meter.
/// </summary>
/// <remarks>
/// <para>
/// The following instruments are registered:
/// <list type="bullet">
///   <item><description><c>encina.tenancy.resolutions_total</c> (Counter) —
///   Total number of tenant resolutions, tagged with <c>strategy</c> and <c>outcome</c>.</description></item>
///   <item><description><c>encina.tenancy.resolution_duration_ms</c> (Histogram) —
///   Duration of tenant resolution in milliseconds.</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class TenancyMetrics
{
    private static readonly Meter Meter = new("Encina", "1.0");

    private readonly Counter<long> _resolutionsTotal;
    private readonly Histogram<double> _resolutionDuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenancyMetrics"/> class,
    /// registering all multi-tenancy metric instruments.
    /// </summary>
    public TenancyMetrics()
    {
        _resolutionsTotal = Meter.CreateCounter<long>(
            "encina.tenancy.resolutions_total",
            unit: "{resolutions}",
            description: "Total number of tenant resolutions.");

        _resolutionDuration = Meter.CreateHistogram<double>(
            "encina.tenancy.resolution_duration_ms",
            unit: "ms",
            description: "Duration of tenant resolution in milliseconds.");
    }

    /// <summary>
    /// Records a tenant resolution.
    /// </summary>
    /// <param name="strategy">The resolution strategy name.</param>
    /// <param name="outcome">The resolution outcome (success, not_found, error).</param>
    /// <param name="durationMs">The resolution duration in milliseconds.</param>
    public void RecordResolution(string strategy, string outcome, double durationMs)
    {
        var tags = new TagList
        {
            { "strategy", strategy },
            { "outcome", outcome }
        };

        _resolutionsTotal.Add(1, tags);
        _resolutionDuration.Record(durationMs, new TagList
        {
            { "strategy", strategy }
        });
    }
}
