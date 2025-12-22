using System.Diagnostics.Metrics;
using System.Reflection;
using Shouldly;

namespace Encina.Tests;

public sealed class MediatorMetricsTests
{
    [Fact]
    public void MeterConfiguration_UsesExpectedNameAndVersion()
    {
        var meterField = typeof(MediatorMetrics).GetField("Meter", BindingFlags.NonPublic | BindingFlags.Static);
        meterField.ShouldNotBeNull();

        var meter = meterField.GetValue(null).ShouldBeOfType<Meter>();
        meter.Name.ShouldBe("Encina");
        meter.Version.ShouldBe("1.0");
    }

    [Fact]
    public void TrackSuccess_EmitsCounterAndDurationHistogram()
    {
        var metrics = new MediatorMetrics();
        var successMeasurements = new List<(long value, Dictionary<string, object?> tags)>();
        var durationMeasurements = new List<(double value, Dictionary<string, object?> tags)>();

        using var listener = CreateListener(
            onLongMeasurement: (instrument, measurement, tags) =>
            {
                if (instrument.Name == "Encina.request.success")
                {
                    successMeasurements.Add((measurement, tags));
                }
            },
            onDoubleMeasurement: (instrument, measurement, tags) =>
            {
                if (instrument.Name == "Encina.request.duration")
                {
                    durationMeasurements.Add((measurement, tags));
                }
            });

        metrics.TrackSuccess("command", "Ping", TimeSpan.FromMilliseconds(42));

        successMeasurements.Count.ShouldBe(1);
        var success = successMeasurements[0];
        success.value.ShouldBe(1);
        success.tags["request.kind"].ShouldBe("command");
        success.tags["request.name"].ShouldBe("Ping");

        durationMeasurements.Count.ShouldBe(1);
        var successDuration = durationMeasurements[0];
        successDuration.value.ShouldBe(42);
        successDuration.tags["request.kind"].ShouldBe("command");
        successDuration.tags["request.name"].ShouldBe("Ping");
    }

    [Fact]
    public void TrackFailure_EmitsCounterWithReason()
    {
        var metrics = new MediatorMetrics();
        var failureMeasurements = new List<(long value, Dictionary<string, object?> tags)>();
        var durationMeasurements = new List<(double value, Dictionary<string, object?> tags)>();

        using var listener = CreateListener(
            onLongMeasurement: (instrument, measurement, tags) =>
            {
                if (instrument.Name == "Encina.request.failure")
                {
                    failureMeasurements.Add((measurement, tags));
                }
            },
            onDoubleMeasurement: (instrument, measurement, tags) =>
            {
                if (instrument.Name == "Encina.request.duration")
                {
                    durationMeasurements.Add((measurement, tags));
                }
            });

        metrics.TrackFailure("query", "FindOrders", TimeSpan.FromMilliseconds(10), "timeout");

        failureMeasurements.Count.ShouldBe(1);
        var failure = failureMeasurements[0];
        failure.value.ShouldBe(1);
        failure.tags["request.kind"].ShouldBe("query");
        failure.tags["request.name"].ShouldBe("FindOrders");
        failure.tags["failure.reason"].ShouldBe("timeout");

        durationMeasurements.Count.ShouldBe(1);
        var failureDuration = durationMeasurements[0];
        failureDuration.value.ShouldBe(10);
        failureDuration.tags["request.kind"].ShouldBe("query");
        failureDuration.tags["request.name"].ShouldBe("FindOrders");
        failureDuration.tags["failure.reason"].ShouldBe("timeout");
    }

    [Fact]
    public void TrackFailure_OmitsReasonTagWhenBlank()
    {
        var metrics = new MediatorMetrics();
        var failureMeasurements = new List<(long value, Dictionary<string, object?> tags)>();

        using var listener = CreateListener(
            onLongMeasurement: (instrument, measurement, tags) =>
            {
                if (instrument.Name == "Encina.request.failure")
                {
                    failureMeasurements.Add((measurement, tags));
                }
            });

        metrics.TrackFailure("command", "Ping", TimeSpan.Zero, string.Empty);

        failureMeasurements.Count.ShouldBe(1);
        var failure = failureMeasurements[0];
        failure.value.ShouldBe(1);
        failure.tags["request.kind"].ShouldBe("command");
        failure.tags["request.name"].ShouldBe("Ping");
        failure.tags.ContainsKey("failure.reason").ShouldBeFalse();
    }

    [Fact]
    public void DurationHistogram_UsesMillisecondsUnit()
    {
        var histogramField = typeof(MediatorMetrics)
            .GetField("_durationHistogram", BindingFlags.NonPublic | BindingFlags.Instance);
        histogramField.ShouldNotBeNull();

        var metrics = new MediatorMetrics();
        var histogram = histogramField.GetValue(metrics).ShouldBeOfType<Histogram<double>>();
        histogram.Unit.ShouldBe("ms");
    }

    private static MeterListener CreateListener(
        Action<Instrument, long, Dictionary<string, object?>>? onLongMeasurement = null,
        Action<Instrument, double, Dictionary<string, object?>>? onDoubleMeasurement = null)
    {
        var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Meter.Name == "Encina")
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };

        if (onLongMeasurement is not null)
        {
            listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, _) =>
            {
                onLongMeasurement(instrument, measurement, ToDictionary(tags));
            });
        }

        if (onDoubleMeasurement is not null)
        {
            listener.SetMeasurementEventCallback<double>((instrument, measurement, tags, _) =>
            {
                onDoubleMeasurement(instrument, measurement, ToDictionary(tags));
            });
        }

        listener.Start();
        return listener;
    }

    private static Dictionary<string, object?> ToDictionary(ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        var dictionary = new Dictionary<string, object?>();
        foreach (var tag in tags)
        {
            dictionary[tag.Key] = tag.Value;
        }

        return dictionary;
    }
}
