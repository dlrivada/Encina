using Encina.OpenTelemetry.ReferenceTable;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.OpenTelemetry.ReferenceTable;

/// <summary>
/// Unit tests for <see cref="ReferenceTableMetrics"/>.
/// </summary>
public sealed class ReferenceTableMetricsTests
{
    [Fact]
    public void Constructor_NullRegisteredTablesCallback_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ReferenceTableMetrics(null!, () => []));
    }

    [Fact]
    public void Constructor_NullStalenessCallback_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ReferenceTableMetrics(() => 0, null!));
    }

    [Fact]
    public void Constructor_ValidCallbacks_DoesNotThrow()
    {
        var ex = Record.Exception(() =>
            new ReferenceTableMetrics(() => 3, () => [("Country", 50.0)]));
        ex.ShouldBeNull();
    }

    [Fact]
    public void RecordReplicationDuration_DoesNotThrow()
    {
        var metrics = new ReferenceTableMetrics(() => 0, () => []);

        var ex = Record.Exception(() =>
            metrics.RecordReplicationDuration("Country", 250.0));
        ex.ShouldBeNull();
    }

    [Fact]
    public void RecordRowsSynced_DoesNotThrow()
    {
        var metrics = new ReferenceTableMetrics(() => 0, () => []);

        var ex = Record.Exception(() =>
            metrics.RecordRowsSynced("Country", 100));
        ex.ShouldBeNull();
    }

    [Fact]
    public void RecordError_DoesNotThrow()
    {
        var metrics = new ReferenceTableMetrics(() => 0, () => []);

        var ex = Record.Exception(() =>
            metrics.RecordError("Country", "ConnectionTimeout"));
        ex.ShouldBeNull();
    }

    [Fact]
    public void IncrementActiveReplications_DoesNotThrow()
    {
        var metrics = new ReferenceTableMetrics(() => 0, () => []);

        var ex = Record.Exception(() =>
            metrics.IncrementActiveReplications());
        ex.ShouldBeNull();
    }

    [Fact]
    public void DecrementActiveReplications_DoesNotThrow()
    {
        var metrics = new ReferenceTableMetrics(() => 0, () => []);

        var ex = Record.Exception(() =>
            metrics.DecrementActiveReplications());
        ex.ShouldBeNull();
    }
}
