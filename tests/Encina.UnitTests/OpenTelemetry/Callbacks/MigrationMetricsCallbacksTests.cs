using Encina.OpenTelemetry.Migrations;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.OpenTelemetry.Callbacks;

/// <summary>
/// Unit tests for <see cref="MigrationMetricsCallbacks"/>.
/// </summary>
public sealed class MigrationMetricsCallbacksTests
{
    [Fact]
    public void Constructor_NullDriftDetectedCountCallback_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new MigrationMetricsCallbacks(null!));
    }

    [Fact]
    public void Constructor_ValidCallback_DoesNotThrow()
    {
        var ex = Record.Exception(() =>
            new MigrationMetricsCallbacks(() => 3));
        ex.ShouldBeNull();
    }
}
