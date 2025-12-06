using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using Shouldly;
using SimpleMediator;

namespace SimpleMediator.Tests;

public sealed class MediatorMetricsTests
{
    [Fact]
    public void TrackSuccess_EmitsCounterAndDurationHistogram()
    {
        var metrics = new MediatorMetrics();
        var successMeasurements = new List<(long value, Dictionary<string, object?> tags)>();
        var durationMeasurements = new List<(double value, Dictionary<string, object?> tags)>();

        using var listener = CreateListener(
            onLongMeasurement: (instrument, measurement, tags) =>
            {
                if (instrument.Name == "simplemediator.request.success")
                {
                    successMeasurements.Add((measurement, tags));
                }
            },
            onDoubleMeasurement: (instrument, measurement, tags) =>
            {
                if (instrument.Name == "simplemediator.request.duration")
                {
                    durationMeasurements.Add((measurement, tags));
                }
            });

        metrics.TrackSuccess("command", "Ping", TimeSpan.FromMilliseconds(42));

        successMeasurements.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            item => item.value.ShouldBe(1),
            item => item.tags["request.kind"].ShouldBe("command"),
            item => item.tags["request.name"].ShouldBe("Ping"));

        durationMeasurements.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            item => item.value.ShouldBe(42),
            item => item.tags["request.kind"].ShouldBe("command"));
    }

    [Fact]
    public void TrackFailure_EmitsCounterWithReason()
    {
        var metrics = new MediatorMetrics();
        var failureMeasurements = new List<(long value, Dictionary<string, object?> tags)>();

        using var listener = CreateListener(
            onLongMeasurement: (instrument, measurement, tags) =>
            {
                if (instrument.Name == "simplemediator.request.failure")
                {
                    failureMeasurements.Add((measurement, tags));
                }
            });

        metrics.TrackFailure("query", "FindOrders", TimeSpan.FromMilliseconds(10), "timeout");

        failureMeasurements.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            item => item.value.ShouldBe(1),
            item => item.tags["request.kind"].ShouldBe("query"),
            item => item.tags["failure.reason"].ShouldBe("timeout"));
    }

    private static MeterListener CreateListener(
        Action<Instrument, long, Dictionary<string, object?>>? onLongMeasurement = null,
        Action<Instrument, double, Dictionary<string, object?>>? onDoubleMeasurement = null)
    {
        var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Meter.Name == "SimpleMediator")
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
