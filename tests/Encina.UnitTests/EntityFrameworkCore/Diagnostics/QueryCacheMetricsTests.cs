using System.Diagnostics.Metrics;
using Encina.EntityFrameworkCore.Diagnostics;

namespace Encina.UnitTests.EntityFrameworkCore.Diagnostics;

/// <summary>
/// Unit tests for <see cref="QueryCacheMetrics"/>.
/// </summary>
[Trait("Category", "Unit")]
public sealed class QueryCacheMetricsTests
{
    #region Construction

    [Fact]
    public void Constructor_CreatesInstance()
    {
        // Act
        var metrics = new QueryCacheMetrics();

        // Assert
        metrics.ShouldNotBeNull();
    }

    #endregion

    #region RecordHit

    [Fact]
    public void RecordHit_WithEntityType_DoesNotThrow()
    {
        // Arrange
        var metrics = new QueryCacheMetrics();

        // Act & Assert
        Should.NotThrow(() => metrics.RecordHit("Order"));
    }

    [Fact]
    public void RecordHit_MultipleTimes_DoesNotThrow()
    {
        // Arrange
        var metrics = new QueryCacheMetrics();

        // Act & Assert
        Should.NotThrow(() =>
        {
            metrics.RecordHit("Order");
            metrics.RecordHit("Order");
            metrics.RecordHit("Product");
        });
    }

    [Fact]
    public void RecordHit_IncrementsMeterCounter()
    {
        // Arrange
        var metrics = new QueryCacheMetrics();
        using var meterListener = new MeterListener();
        var measurements = new List<(long Value, string? EntityType)>();

        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Name == "encina.querycache.hits_total")
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };

        meterListener.SetMeasurementEventCallback<long>((instrument, measurement, tags, state) =>
        {
            string? entityType = null;
            foreach (var tag in tags)
            {
                if (tag.Key == "entity_type")
                {
                    entityType = tag.Value?.ToString();
                }
            }
            measurements.Add((measurement, entityType));
        });

        meterListener.Start();

        // Act
        metrics.RecordHit("Order");

        meterListener.RecordObservableInstruments();

        // Assert
        measurements.ShouldContain(m => m.Value == 1 && m.EntityType == "Order");
    }

    #endregion

    #region RecordMiss

    [Fact]
    public void RecordMiss_WithEntityType_DoesNotThrow()
    {
        // Arrange
        var metrics = new QueryCacheMetrics();

        // Act & Assert
        Should.NotThrow(() => metrics.RecordMiss("Customer"));
    }

    [Fact]
    public void RecordMiss_MultipleTimes_DoesNotThrow()
    {
        // Arrange
        var metrics = new QueryCacheMetrics();

        // Act & Assert
        Should.NotThrow(() =>
        {
            metrics.RecordMiss("Customer");
            metrics.RecordMiss("Customer");
            metrics.RecordMiss("Product");
        });
    }

    [Fact]
    public void RecordMiss_IncrementsMeterCounter()
    {
        // Arrange
        var metrics = new QueryCacheMetrics();
        using var meterListener = new MeterListener();
        var measurements = new List<(long Value, string? EntityType)>();

        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Name == "encina.querycache.misses_total")
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };

        meterListener.SetMeasurementEventCallback<long>((instrument, measurement, tags, state) =>
        {
            string? entityType = null;
            foreach (var tag in tags)
            {
                if (tag.Key == "entity_type")
                {
                    entityType = tag.Value?.ToString();
                }
            }
            measurements.Add((measurement, entityType));
        });

        meterListener.Start();

        // Act
        metrics.RecordMiss("Customer");

        meterListener.RecordObservableInstruments();

        // Assert
        measurements.ShouldContain(m => m.Value == 1 && m.EntityType == "Customer");
    }

    #endregion

    #region RecordEviction

    [Fact]
    public void RecordEviction_WithReason_DoesNotThrow()
    {
        // Arrange
        var metrics = new QueryCacheMetrics();

        // Act & Assert
        Should.NotThrow(() => metrics.RecordEviction("ttl"));
    }

    [Fact]
    public void RecordEviction_MultipleTimes_DoesNotThrow()
    {
        // Arrange
        var metrics = new QueryCacheMetrics();

        // Act & Assert
        Should.NotThrow(() =>
        {
            metrics.RecordEviction("ttl");
            metrics.RecordEviction("invalidation");
            metrics.RecordEviction("manual");
        });
    }

    [Fact]
    public void RecordEviction_IncrementsMeterCounter()
    {
        // Arrange
        var metrics = new QueryCacheMetrics();
        using var meterListener = new MeterListener();
        var measurements = new List<(long Value, string? Reason)>();

        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Name == "encina.querycache.evictions_total")
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };

        meterListener.SetMeasurementEventCallback<long>((instrument, measurement, tags, state) =>
        {
            string? reason = null;
            foreach (var tag in tags)
            {
                if (tag.Key == "reason")
                {
                    reason = tag.Value?.ToString();
                }
            }
            measurements.Add((measurement, reason));
        });

        meterListener.Start();

        // Act
        metrics.RecordEviction("invalidation");

        meterListener.RecordObservableInstruments();

        // Assert
        measurements.ShouldContain(m => m.Value == 1 && m.Reason == "invalidation");
    }

    #endregion

    #region Multiple Operations

    [Fact]
    public void MixedOperations_AllSucceed()
    {
        // Arrange
        var metrics = new QueryCacheMetrics();

        // Act & Assert - a typical cache workflow
        Should.NotThrow(() =>
        {
            metrics.RecordMiss("Order");
            metrics.RecordHit("Order");
            metrics.RecordHit("Order");
            metrics.RecordEviction("ttl");
            metrics.RecordMiss("Product");
            metrics.RecordHit("Product");
        });
    }

    #endregion
}
