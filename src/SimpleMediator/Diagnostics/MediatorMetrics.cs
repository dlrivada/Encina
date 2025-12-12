using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace SimpleMediator;

/// <summary>
/// Defines the metrics exposed by SimpleMediator.
/// </summary>
/// <remarks>
/// Can be customized to integrate with other observability systems (Application
/// Insights, Prometheus, and so on). The default implementation uses <see cref="Meter"/>.
/// </remarks>
public interface IMediatorMetrics
{
    /// <summary>
    /// Records a successful request execution.
    /// </summary>
    /// <param name="requestKind">Logical request kind (for example, <c>command</c> or <c>query</c>).</param>
    /// <param name="requestName">Friendly name for the request.</param>
    /// <param name="duration">Total time spent by the pipeline.</param>
    void TrackSuccess(string requestKind, string requestName, TimeSpan duration);

    /// <summary>
    /// Records a failed request execution, either functional or exceptional.
    /// </summary>
    /// <param name="requestKind">Logical request kind.</param>
    /// <param name="requestName">Friendly name for the request.</param>
    /// <param name="duration">Elapsed time before the failure.</param>
    /// <param name="reason">Code or description of the failure reason.</param>
    void TrackFailure(string requestKind, string requestName, TimeSpan duration, string reason);
}

/// <summary>
/// Default implementation that exposes metrics via <see cref="System.Diagnostics.Metrics"/>.
/// </summary>
/// <remarks>
/// The following instruments are created:
/// <list type="bullet">
/// <item><description><c>simplemediator.request.success</c> (Counter)</description></item>
/// <item><description><c>simplemediator.request.failure</c> (Counter)</description></item>
/// <item><description><c>simplemediator.request.duration</c> (Histogram in milliseconds)</description></item>
/// </list>
/// </remarks>
public sealed class MediatorMetrics : IMediatorMetrics
{
    private static readonly Meter Meter = new("SimpleMediator", "1.0");
    private readonly Counter<long> _successCounter = Meter.CreateCounter<long>("simplemediator.request.success");
    private readonly Counter<long> _failureCounter = Meter.CreateCounter<long>("simplemediator.request.failure");
    private readonly Histogram<double> _durationHistogram = Meter.CreateHistogram<double>(
        "simplemediator.request.duration",
        unit: "ms");

    /// <inheritdoc />
    public void TrackSuccess(string requestKind, string requestName, TimeSpan duration)
    {
        var tags = new TagList
        {
            { "request.kind", requestKind },
            { "request.name", requestName }
        };

        _successCounter.Add(1, tags);
        _durationHistogram.Record(duration.TotalMilliseconds, tags);
    }

    /// <inheritdoc />
    public void TrackFailure(string requestKind, string requestName, TimeSpan duration, string reason)
    {
        var tags = new TagList
        {
            { "request.kind", requestKind },
            { "request.name", requestName }
        };

        if (!string.IsNullOrWhiteSpace(reason))
        {
            tags.Add("failure.reason", reason);
        }

        _failureCounter.Add(1, tags);
        _durationHistogram.Record(duration.TotalMilliseconds, tags);
    }
}
