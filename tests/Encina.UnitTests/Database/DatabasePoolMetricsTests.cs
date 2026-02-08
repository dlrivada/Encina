using System.Diagnostics.Metrics;

using Encina.Database;
using Encina.Diagnostics;

namespace Encina.UnitTests.Database;

/// <summary>
/// Unit tests for <see cref="DatabasePoolMetrics"/>.
/// </summary>
public sealed class DatabasePoolMetricsTests
{
    [Fact]
    public void Constructor_NullMonitor_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new DatabasePoolMetrics(null!));
    }

    [Fact]
    public void Constructor_ValidMonitor_DoesNotThrow()
    {
        // Arrange
        var monitor = CreateMonitor();

        // Act & Assert
        Should.NotThrow(() => new DatabasePoolMetrics(monitor));
    }

    [Fact]
    public void Metrics_RegisterActiveConnectionsGauge()
    {
        // Arrange
        var monitor = CreateMonitor(active: 5);

        using var meterListener = new MeterListener();
        var activeConnections = -1;

        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Name == "Encina.db.pool.connections.active" && instrument.Meter.Name == "Encina")
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };

        meterListener.SetMeasurementEventCallback<int>((instrument, measurement, tags, state) =>
        {
            if (instrument.Name == "Encina.db.pool.connections.active")
            {
                activeConnections = measurement;
            }
        });

        meterListener.Start();

        // Act
        _ = new DatabasePoolMetrics(monitor);
        meterListener.RecordObservableInstruments();

        // Assert
        activeConnections.ShouldBe(5);
    }

    [Fact]
    public void Metrics_RegisterIdleConnectionsGauge()
    {
        // Arrange
        var monitor = CreateMonitor(idle: 10);

        using var meterListener = new MeterListener();
        var idleConnections = -1;

        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Name == "Encina.db.pool.connections.idle" && instrument.Meter.Name == "Encina")
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };

        meterListener.SetMeasurementEventCallback<int>((instrument, measurement, tags, state) =>
        {
            if (instrument.Name == "Encina.db.pool.connections.idle")
            {
                idleConnections = measurement;
            }
        });

        meterListener.Start();

        // Act
        _ = new DatabasePoolMetrics(monitor);
        meterListener.RecordObservableInstruments();

        // Assert
        idleConnections.ShouldBe(10);
    }

    [Fact]
    public void Metrics_RegisterPoolUtilizationGauge()
    {
        // Arrange
        var monitor = CreateMonitor(total: 50, maxPool: 100);

        using var meterListener = new MeterListener();
        var utilization = -1.0;

        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Name == "Encina.db.pool.utilization" && instrument.Meter.Name == "Encina")
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };

        meterListener.SetMeasurementEventCallback<double>((instrument, measurement, tags, state) =>
        {
            if (instrument.Name == "Encina.db.pool.utilization")
            {
                utilization = measurement;
            }
        });

        meterListener.Start();

        // Act
        _ = new DatabasePoolMetrics(monitor);
        meterListener.RecordObservableInstruments();

        // Assert
        utilization.ShouldBe(0.5);
    }

    [Fact]
    public void Metrics_RegisterCircuitBreakerStateGauge()
    {
        // Arrange
        var monitor = CreateMonitor();
        monitor.IsCircuitOpen.Returns(true);

        using var meterListener = new MeterListener();
        var circuitState = -1;

        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Name == "Encina.db.circuit_breaker.state" && instrument.Meter.Name == "Encina")
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };

        meterListener.SetMeasurementEventCallback<int>((instrument, measurement, tags, state) =>
        {
            if (instrument.Name == "Encina.db.circuit_breaker.state")
            {
                circuitState = measurement;
            }
        });

        meterListener.Start();

        // Act
        _ = new DatabasePoolMetrics(monitor);
        meterListener.RecordObservableInstruments();

        // Assert
        circuitState.ShouldBe(1);
    }

    [Fact]
    public void Metrics_IncludeProviderTag()
    {
        // Arrange
        var monitor = CreateMonitor();
        monitor.ProviderName.Returns("ado-sqlserver");

        using var meterListener = new MeterListener();
        string? providerTag = null;

        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Name == "Encina.db.pool.connections.active" && instrument.Meter.Name == "Encina")
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };

        meterListener.SetMeasurementEventCallback<int>((instrument, measurement, tags, state) =>
        {
            if (instrument.Name == "Encina.db.pool.connections.active")
            {
                foreach (var tag in tags)
                {
                    if (tag.Key == "db.provider")
                    {
                        providerTag = tag.Value?.ToString();
                    }
                }
            }
        });

        meterListener.Start();

        // Act
        _ = new DatabasePoolMetrics(monitor);
        meterListener.RecordObservableInstruments();

        // Assert
        providerTag.ShouldBe("ado-sqlserver");
    }

    private static IDatabaseHealthMonitor CreateMonitor(
        int active = 0, int idle = 0, int total = 0, int pending = 0, int maxPool = 100)
    {
        var monitor = Substitute.For<IDatabaseHealthMonitor>();
        monitor.ProviderName.Returns("test-provider");
        monitor.IsCircuitOpen.Returns(false);
        monitor.GetPoolStatistics().Returns(
            new ConnectionPoolStats(active, idle, total, pending, maxPool));
        return monitor;
    }
}
