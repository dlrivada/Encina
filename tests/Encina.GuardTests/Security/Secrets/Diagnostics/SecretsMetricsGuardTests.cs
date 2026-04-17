using System.Diagnostics.Metrics;
using Encina.Security.Secrets.Diagnostics;
using Shouldly;

namespace Encina.GuardTests.Security.Secrets.Diagnostics;

/// <summary>
/// Guard clause tests for <see cref="SecretsMetrics"/>.
/// Verifies that null arguments are properly rejected in the constructor.
/// </summary>
public sealed class SecretsMetricsGuardTests
{
    #region Constructor Guards

    [Fact]
    public void Constructor_NullMeterFactory_ThrowsArgumentNullException()
    {
        var act = () => new SecretsMetrics(null!);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("meterFactory");
    }

    [Fact]
    public void Constructor_ValidMeterFactory_DoesNotThrow()
    {
        var factory = new TestMeterFactory();

        var act = () => new SecretsMetrics(factory);

        Should.NotThrow(act);
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Minimal <see cref="IMeterFactory"/> implementation for guard tests.
    /// </summary>
    private sealed class TestMeterFactory : IMeterFactory
    {
        private readonly List<Meter> _meters = [];

        public Meter Create(MeterOptions options)
        {
            var meter = new Meter(options);
            _meters.Add(meter);
            return meter;
        }

        public void Dispose()
        {
            foreach (var meter in _meters)
            {
                meter.Dispose();
            }
        }
    }

    #endregion
}
