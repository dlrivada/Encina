using System.Diagnostics.Metrics;
using Encina.EntityFrameworkCore.Diagnostics;

namespace Encina.UnitTests.EntityFrameworkCore.Diagnostics;

/// <summary>
/// Unit tests for <see cref="SoftDeleteMetrics"/>.
/// </summary>
[Trait("Category", "Unit")]
public sealed class SoftDeleteMetricsTests
{
    #region Construction

    [Fact]
    public void Constructor_CreatesInstance()
    {
        // Act
        var metrics = new SoftDeleteMetrics();

        // Assert
        metrics.ShouldNotBeNull();
    }

    #endregion

    #region RecordOperation

    [Fact]
    public void RecordOperation_Delete_DoesNotThrow()
    {
        // Arrange
        var metrics = new SoftDeleteMetrics();

        // Act & Assert
        Should.NotThrow(() => metrics.RecordOperation("delete", "Order"));
    }

    [Fact]
    public void RecordOperation_Restore_DoesNotThrow()
    {
        // Arrange
        var metrics = new SoftDeleteMetrics();

        // Act & Assert
        Should.NotThrow(() => metrics.RecordOperation("restore", "Customer"));
    }

    [Fact]
    public void RecordOperation_HardDelete_DoesNotThrow()
    {
        // Arrange
        var metrics = new SoftDeleteMetrics();

        // Act & Assert
        Should.NotThrow(() => metrics.RecordOperation("hard_delete", "Product"));
    }

    [Fact]
    public void RecordOperation_MultipleTimes_DoesNotThrow()
    {
        // Arrange
        var metrics = new SoftDeleteMetrics();

        // Act & Assert
        Should.NotThrow(() =>
        {
            metrics.RecordOperation("delete", "Order");
            metrics.RecordOperation("delete", "Customer");
            metrics.RecordOperation("restore", "Order");
            metrics.RecordOperation("hard_delete", "Product");
        });
    }

    [Fact]
    public void RecordOperation_IncrementsMeterCounter()
    {
        // Arrange
        var metrics = new SoftDeleteMetrics();
        using var meterListener = new MeterListener();
        var measurements = new List<(long Value, string? Operation, string? EntityType)>();

        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Name == "encina.softdelete.operations_total")
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };

        meterListener.SetMeasurementEventCallback<long>((instrument, measurement, tags, state) =>
        {
            string? operation = null;
            string? entityType = null;
            foreach (var tag in tags)
            {
                if (tag.Key == "operation")
                {
                    operation = tag.Value?.ToString();
                }
                else if (tag.Key == "entity_type")
                {
                    entityType = tag.Value?.ToString();
                }
            }
            measurements.Add((measurement, operation, entityType));
        });

        meterListener.Start();

        // Act
        metrics.RecordOperation("delete", "Order");

        meterListener.RecordObservableInstruments();

        // Assert
        measurements.ShouldContain(m =>
            m.Value == 1 && m.Operation == "delete" && m.EntityType == "Order");
    }

    [Fact]
    public void RecordOperation_DifferentOperations_TrackedSeparately()
    {
        // Arrange
        var metrics = new SoftDeleteMetrics();
        using var meterListener = new MeterListener();
        var measurements = new List<(long Value, string? Operation, string? EntityType)>();

        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Name == "encina.softdelete.operations_total")
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };

        meterListener.SetMeasurementEventCallback<long>((instrument, measurement, tags, state) =>
        {
            string? operation = null;
            string? entityType = null;
            foreach (var tag in tags)
            {
                if (tag.Key == "operation")
                {
                    operation = tag.Value?.ToString();
                }
                else if (tag.Key == "entity_type")
                {
                    entityType = tag.Value?.ToString();
                }
            }
            measurements.Add((measurement, operation, entityType));
        });

        meterListener.Start();

        // Act
        metrics.RecordOperation("delete", "Order");
        metrics.RecordOperation("restore", "Order");

        meterListener.RecordObservableInstruments();

        // Assert
        measurements.Count.ShouldBe(2);
        measurements.ShouldContain(m =>
            m.Value == 1 && m.Operation == "delete" && m.EntityType == "Order");
        measurements.ShouldContain(m =>
            m.Value == 1 && m.Operation == "restore" && m.EntityType == "Order");
    }

    #endregion
}
