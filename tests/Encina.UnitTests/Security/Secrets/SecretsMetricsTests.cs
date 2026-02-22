using System.Diagnostics.Metrics;
using Encina.Security.Secrets.Diagnostics;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.UnitTests.Security.Secrets;

public sealed class SecretsMetricsTests : IDisposable
{
    private readonly SecretsMetrics _metrics;
    private readonly MeterListener _listener;
    private readonly Dictionary<string, long> _counterValues = new();
    private readonly Dictionary<string, double> _histogramValues = new();

    public SecretsMetricsTests()
    {
        var services = new ServiceCollection();
        services.AddMetrics();
        var provider = services.BuildServiceProvider();
        var meterFactory = provider.GetRequiredService<IMeterFactory>();
        _metrics = new SecretsMetrics(meterFactory);

        _listener = new MeterListener();
        _listener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == SecretsMetrics.MeterName)
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        _listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, state) =>
        {
            _counterValues[instrument.Name] = _counterValues.GetValueOrDefault(instrument.Name) + measurement;
        });
        _listener.SetMeasurementEventCallback<double>((instrument, measurement, tags, state) =>
        {
            _histogramValues[instrument.Name] = measurement;
        });
        _listener.Start();
    }

    public void Dispose()
    {
        _listener.Dispose();
    }

    #region Constructor

    [Fact]
    public void Constructor_NullMeterFactory_ThrowsArgumentNullException()
    {
        var act = () => new SecretsMetrics(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region Constants

    [Fact]
    public void MeterName_IsCorrect()
    {
        SecretsMetrics.MeterName.Should().Be("Encina.Security.Secrets");
    }

    [Fact]
    public void MeterVersion_IsCorrect()
    {
        SecretsMetrics.MeterVersion.Should().Be("1.0.0");
    }

    #endregion

    #region RecordGetSecret

    [Fact]
    public void RecordGetSecret_Success_IncrementsCounter()
    {
        _metrics.RecordGetSecret("my-secret", success: true, TimeSpan.FromMilliseconds(42));
        _listener.RecordObservableInstruments();

        _counterValues.Should().ContainKey("secrets.get.count");
        _counterValues["secrets.get.count"].Should().Be(1);
    }

    [Fact]
    public void RecordGetSecret_RecordsDuration()
    {
        _metrics.RecordGetSecret("my-secret", success: true, TimeSpan.FromMilliseconds(42));
        _listener.RecordObservableInstruments();

        _histogramValues.Should().ContainKey("secrets.get.duration");
        _histogramValues["secrets.get.duration"].Should().BeApproximately(42.0, 0.1);
    }

    [Fact]
    public void RecordGetSecret_Failure_IncrementsCounter()
    {
        _metrics.RecordGetSecret("my-secret", success: false, TimeSpan.FromMilliseconds(10), "not_found");
        _listener.RecordObservableInstruments();

        _counterValues.Should().ContainKey("secrets.get.count");
    }

    [Fact]
    public void RecordGetSecret_MultipleCalls_AccumulatesCount()
    {
        _metrics.RecordGetSecret("secret-1", true, TimeSpan.FromMilliseconds(10));
        _metrics.RecordGetSecret("secret-2", true, TimeSpan.FromMilliseconds(20));
        _listener.RecordObservableInstruments();

        _counterValues["secrets.get.count"].Should().Be(2);
    }

    #endregion

    #region RecordSetSecret

    [Fact]
    public void RecordSetSecret_IncrementsCounter()
    {
        _metrics.RecordSetSecret("my-secret", success: true);
        _listener.RecordObservableInstruments();

        _counterValues.Should().ContainKey("secrets.set.count");
        _counterValues["secrets.set.count"].Should().Be(1);
    }

    [Fact]
    public void RecordSetSecret_Failure_IncrementsCounter()
    {
        _metrics.RecordSetSecret("my-secret", success: false, "provider_unavailable");
        _listener.RecordObservableInstruments();

        _counterValues.Should().ContainKey("secrets.set.count");
    }

    #endregion

    #region RecordRotation

    [Fact]
    public void RecordRotation_IncrementsCounter()
    {
        _metrics.RecordRotation("my-secret", success: true);
        _listener.RecordObservableInstruments();

        _counterValues.Should().ContainKey("secrets.rotation.count");
        _counterValues["secrets.rotation.count"].Should().Be(1);
    }

    #endregion

    #region RecordCacheHit / RecordCacheMiss

    [Fact]
    public void RecordCacheHit_IncrementsCounter()
    {
        _metrics.RecordCacheHit("my-secret");
        _listener.RecordObservableInstruments();

        _counterValues.Should().ContainKey("secrets.cache.hits");
        _counterValues["secrets.cache.hits"].Should().Be(1);
    }

    [Fact]
    public void RecordCacheMiss_IncrementsCounter()
    {
        _metrics.RecordCacheMiss("my-secret");
        _listener.RecordObservableInstruments();

        _counterValues.Should().ContainKey("secrets.cache.misses");
        _counterValues["secrets.cache.misses"].Should().Be(1);
    }

    #endregion

    #region RecordInjection

    [Fact]
    public void RecordInjection_IncrementsCounter()
    {
        _metrics.RecordInjection(typeof(string), success: true, TimeSpan.FromMilliseconds(5), propertiesInjected: 3);
        _listener.RecordObservableInstruments();

        _counterValues.Should().ContainKey("secrets.injection.count");
        _counterValues["secrets.injection.count"].Should().Be(1);
    }

    [Fact]
    public void RecordInjection_RecordsDuration()
    {
        _metrics.RecordInjection(typeof(string), success: true, TimeSpan.FromMilliseconds(15.5));
        _listener.RecordObservableInstruments();

        _histogramValues.Should().ContainKey("secrets.injection.duration");
        _histogramValues["secrets.injection.duration"].Should().BeApproximately(15.5, 0.1);
    }

    #endregion

    #region RecordFailover

    [Fact]
    public void RecordFailover_IncrementsCounter()
    {
        _metrics.RecordFailover("my-secret", "EnvironmentSecretProvider");
        _listener.RecordObservableInstruments();

        _counterValues.Should().ContainKey("secrets.failover.count");
        _counterValues["secrets.failover.count"].Should().Be(1);
    }

    #endregion

    #region All Counters Independent

    [Fact]
    public void AllCounters_AreIndependent()
    {
        _metrics.RecordGetSecret("s1", true, TimeSpan.FromMilliseconds(1));
        _metrics.RecordSetSecret("s2", true);
        _metrics.RecordRotation("s3", true);
        _metrics.RecordCacheHit("s4");
        _metrics.RecordCacheMiss("s5");
        _metrics.RecordInjection(typeof(string), true, TimeSpan.FromMilliseconds(1));
        _metrics.RecordFailover("s6", "Provider");
        _listener.RecordObservableInstruments();

        _counterValues["secrets.get.count"].Should().Be(1);
        _counterValues["secrets.set.count"].Should().Be(1);
        _counterValues["secrets.rotation.count"].Should().Be(1);
        _counterValues["secrets.cache.hits"].Should().Be(1);
        _counterValues["secrets.cache.misses"].Should().Be(1);
        // injection.count includes propertiesInjected (0 in this case) + 1
        _counterValues["secrets.injection.count"].Should().Be(1);
        _counterValues["secrets.failover.count"].Should().Be(1);
    }

    #endregion
}
