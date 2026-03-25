using Encina.OpenTelemetry.Sharding;
using Encina.Sharding.Colocation;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.OpenTelemetry.Sharding;

/// <summary>
/// Unit tests for <see cref="ColocationMetrics"/>.
/// </summary>
public sealed class ColocationMetricsTests
{
    [Fact]
    public void Constructor_NullRegistry_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ColocationMetrics(null!));
    }

    [Fact]
    public void Constructor_ValidRegistry_DoesNotThrow()
    {
        var registry = new ColocationGroupRegistry();

        var ex = Record.Exception(() => new ColocationMetrics(registry));
        ex.ShouldBeNull();
    }

    [Fact]
    public void RecordValidationFailure_DoesNotThrow()
    {
        var registry = new ColocationGroupRegistry();
        var metrics = new ColocationMetrics(registry);

        var ex = Record.Exception(() =>
            metrics.RecordValidationFailure("Order", "OrderItem"));
        ex.ShouldBeNull();
    }

    [Fact]
    public void RecordLocalJoin_DoesNotThrow()
    {
        var registry = new ColocationGroupRegistry();
        var metrics = new ColocationMetrics(registry);

        var ex = Record.Exception(() =>
            metrics.RecordLocalJoin("Order"));
        ex.ShouldBeNull();
    }
}
