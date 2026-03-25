using Encina.OpenTelemetry.ReferenceTable;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.OpenTelemetry.Callbacks;

/// <summary>
/// Unit tests for <see cref="ReferenceTableMetricsCallbacks"/>.
/// </summary>
public sealed class ReferenceTableMetricsCallbacksTests
{
    [Fact]
    public void Constructor_NullRegisteredTablesCallback_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ReferenceTableMetricsCallbacks(
                null!,
                () => Enumerable.Empty<(string, double)>()));
    }

    [Fact]
    public void Constructor_NullStalenessCallback_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ReferenceTableMetricsCallbacks(() => 3, null!));
    }

    [Fact]
    public void Constructor_ValidCallbacks_DoesNotThrow()
    {
        var ex = Record.Exception(() =>
            new ReferenceTableMetricsCallbacks(
                () => 5,
                () => [("Country", 120.0)]));
        ex.ShouldBeNull();
    }
}
