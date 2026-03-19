using System.Diagnostics.Metrics;
using Encina.Security.Secrets.Diagnostics;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.UnitTests.Security.Secrets.Resilience;

public sealed class SecretsResilienceMetricsTests : IDisposable
{
    private readonly SecretsMetrics _metrics;
    private readonly MeterListener _listener;
    private readonly Dictionary<string, long> _counterValues = new();
    private readonly Dictionary<string, double> _histogramValues = new();

    public SecretsResilienceMetricsTests()
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

    #region RecordRetry

    [Fact]
    public void RecordRetry_ValidParameters_IncrementsCounter()
    {
        _metrics.RecordRetry(1, "transient_error");
        _listener.RecordObservableInstruments();

        _counterValues.Should().ContainKey("secrets.resilience.retry.count");
        _counterValues["secrets.resilience.retry.count"].Should().Be(1);
    }

    [Fact]
    public void RecordRetry_MultipleCalls_AccumulatesCount()
    {
        _metrics.RecordRetry(1, "timeout");
        _metrics.RecordRetry(2, "timeout");
        _metrics.RecordRetry(3, "timeout");
        _listener.RecordObservableInstruments();

        _counterValues["secrets.resilience.retry.count"].Should().Be(3);
    }

    #endregion

    #region RecordCircuitBreakerTransition

    [Theory]
    [InlineData("opened")]
    [InlineData("closed")]
    [InlineData("half_open")]
    public void RecordCircuitBreakerTransition_ValidState_IncrementsCounter(string state)
    {
        _metrics.RecordCircuitBreakerTransition(state);
        _listener.RecordObservableInstruments();

        _counterValues.Should().ContainKey("secrets.resilience.circuit_breaker.transitions");
        _counterValues["secrets.resilience.circuit_breaker.transitions"].Should().Be(1);
    }

    #endregion

    #region RecordTimeout

    [Fact]
    public void RecordTimeout_ValidTimeout_IncrementsCounter()
    {
        _metrics.RecordTimeout(30.0);
        _listener.RecordObservableInstruments();

        _counterValues.Should().ContainKey("secrets.resilience.timeout.count");
        _counterValues["secrets.resilience.timeout.count"].Should().Be(1);
    }

    #endregion

    #region RecordStaleFallback

    [Fact]
    public void RecordStaleFallback_ValidSecretName_IncrementsCounter()
    {
        _metrics.RecordStaleFallback("database-connection-string");
        _listener.RecordObservableInstruments();

        _counterValues.Should().ContainKey("secrets.resilience.stale_fallback.count");
        _counterValues["secrets.resilience.stale_fallback.count"].Should().Be(1);
    }

    #endregion

    #region RecordResilienceDuration

    [Fact]
    public void RecordResilienceDuration_Success_RecordsDuration()
    {
        _metrics.RecordResilienceDuration("my-secret", success: true, TimeSpan.FromMilliseconds(150));
        _listener.RecordObservableInstruments();

        _histogramValues.Should().ContainKey("secrets.resilience.duration");
        _histogramValues["secrets.resilience.duration"].Should().BeApproximately(150.0, 0.1);
    }

    [Fact]
    public void RecordResilienceDuration_Failure_RecordsDuration()
    {
        _metrics.RecordResilienceDuration("my-secret", success: false, TimeSpan.FromMilliseconds(5000));
        _listener.RecordObservableInstruments();

        _histogramValues.Should().ContainKey("secrets.resilience.duration");
        _histogramValues["secrets.resilience.duration"].Should().BeApproximately(5000.0, 0.1);
    }

    #endregion

    #region All Resilience Counters Independent

    [Fact]
    public void AllResilienceCounters_AreIndependent()
    {
        _metrics.RecordRetry(1, "transient_error");
        _metrics.RecordCircuitBreakerTransition("opened");
        _metrics.RecordTimeout(10.0);
        _metrics.RecordStaleFallback("api-key");
        _metrics.RecordResilienceDuration("api-key", true, TimeSpan.FromMilliseconds(200));
        _listener.RecordObservableInstruments();

        _counterValues["secrets.resilience.retry.count"].Should().Be(1);
        _counterValues["secrets.resilience.circuit_breaker.transitions"].Should().Be(1);
        _counterValues["secrets.resilience.timeout.count"].Should().Be(1);
        _counterValues["secrets.resilience.stale_fallback.count"].Should().Be(1);
        _histogramValues["secrets.resilience.duration"].Should().BeApproximately(200.0, 0.1);
    }

    #endregion
}
