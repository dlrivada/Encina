using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Encina.Recoverability;

namespace Encina.UnitTests.Recoverability;

/// <summary>
/// Builds outbound errors the way the Refit handler does, and captures the retry telemetry of the <c>Encina</c> meter and source.
/// </summary>
internal static class OutboundTestSupport
{
    internal static EncinaError Outbound(
        string code,
        bool transient,
        int? status = null,
        TimeSpan? retryAfter = null,
        bool ambiguous = false)
    {
        var details = new Dictionary<string, object?> { [OutboundErrorMetadataKeys.Transient] = transient };
        if (status is { } s)
        {
            details[OutboundErrorMetadataKeys.HttpStatus] = s;
        }

        if (retryAfter is { } r)
        {
            details[OutboundErrorMetadataKeys.HttpRetryAfter] = r;
        }

        if (ambiguous)
        {
            details[OutboundErrorMetadataKeys.Ambiguous] = true;
        }

        return EncinaErrors.Create(code, "Outbound failure", details: details);
    }
}

/// <summary>
/// Captures the counters of the <c>Encina</c> meter whose name is given; filter the result by request name to
/// isolate a test from the others that share the static meter.
/// </summary>
internal sealed class MetricCapture : IDisposable
{
    private readonly MeterListener _listener = new();
    private readonly ConcurrentQueue<(string Instrument, long Value, Dictionary<string, object?> Tags)> _measurements = new();

    internal MetricCapture(params string[] instruments)
    {
        _listener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == "Encina" && instruments.Contains(instrument.Name))
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        _listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) =>
        {
            var dictionary = new Dictionary<string, object?>();
            foreach (var tag in tags)
            {
                dictionary[tag.Key] = tag.Value;
            }

            _measurements.Enqueue((instrument.Name, value, dictionary));
        });
        _listener.Start();
    }

    internal IReadOnlyList<Dictionary<string, object?>> Tags(string instrument, string requestName) =>
        _measurements
            .Where(m => m.Instrument == instrument && (string?)m.Tags.GetValueOrDefault("request_name") == requestName)
            .Select(m => m.Tags)
            .ToList();

    public void Dispose() => _listener.Dispose();
}

/// <summary>
/// Captures the activities of the <c>Encina</c> source with the given operation name.
/// </summary>
internal sealed class ActivityCapture : IDisposable
{
    private readonly ActivityListener _listener;
    private readonly ConcurrentQueue<Activity> _activities = new();

    internal ActivityCapture(string operationName)
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Encina",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity =>
            {
                if (activity.OperationName == operationName)
                {
                    _activities.Enqueue(activity);
                }
            }
        };
        ActivitySource.AddActivityListener(_listener);
    }

    internal IReadOnlyList<Activity> Activities(string requestName) =>
        _activities.Where(a => (string?)a.GetTagItem("Encina.request_name") == requestName).ToList();

    public void Dispose() => _listener.Dispose();
}
